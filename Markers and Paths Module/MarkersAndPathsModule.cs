using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Entities;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Pathing;
using Blish_HUD.Pathing.Content;
using Blish_HUD.PersistentStore;
using Blish_HUD.Settings;
using Markers_and_Paths_Module.PackFormat;
using Markers_and_Paths_Module.PackFormat.TacO;
using Markers_and_Paths_Module.PackFormat.TacO.Readers;

namespace Markers_and_Paths_Module {

    [Export(typeof(Module))]
    public class MarkersAndPathsModule : Module {

        private static readonly Logger Logger = Logger.GetLogger(typeof(MarkersAndPathsModule));

        internal static MarkersAndPathsModule ModuleInstance;

        // Service Managers
        internal SettingsManager    SettingsManager    => this.ModuleParameters.SettingsManager;
        internal ContentsManager    ContentsManager    => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager      Gw2ApiManager      => this.ModuleParameters.Gw2ApiManager;

        private string _markerDirectory;

        private EventHandler<EventArgs> _onNewMapLoaded;

        private Store _persistentStore;

        private CornerIcon _mapIcon;
        private ContextMenuStrip _mapIconMenu;

        private List<PackManager> _packManagers;

        [ImportingConstructor]
        public MarkersAndPathsModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) {
            ModuleInstance = this;
        }

        protected override void DefineSettings(SettingCollection settings) {

        }

        protected override void Initialize() {
            _markerDirectory = DirectoriesManager.GetFullDirectoryPath("markers");
            _persistentStore = GameService.Store.RegisterStore(this.Namespace);

            _mapIcon = new CornerIcon() {
                BasicTooltipText = "Markers & Paths",
                Icon             = ContentsManager.GetTexture("marker-pathing-icon.png"),
                Priority         = "Markers & Paths".GetHashCode(),
                Parent           = GameService.Graphics.SpriteScreen
            };

            _packManagers = new List<PackManager> {
                new TacOManager(_persistentStore)
            };

            _onNewMapLoaded = delegate {
                if (!this.Loaded) return;

                foreach (var packManager in _packManagers) {
                    if (packManager.Loaded) {
                        packManager.UpdateState();
                    }
                }
            };

            _mapIconMenu = new ContextMenuStrip();

            _mapIcon.Click += delegate { _mapIconMenu.Show(_mapIcon); };
        }

        protected override async Task LoadAsync() {
            foreach (var packManager in _packManagers) {
                string timerName = $"PackManager {packManager.PackTypeName}";

                GameService.Debug.StartTimeFunc(timerName);
                packManager.LoadPacks(_markerDirectory, new Progress<string>((report) => { _mapIcon.LoadingMessage = report; }));
                GameService.Debug.StopTimeFuncAndOutput(timerName);
            }

            BuildMenu();
        }

        private void ClearMenu() {
            // TODO: Actually clear the menu
        }

        private void BuildMenu() {
            ClearMenu();

            BuildCategoryMenus();
            BuildOptionMenus();
        }

        private void BuildOptionMenus() {
            var reloadMarkersItem = _mapIconMenu.AddMenuItem("Reload All");

            reloadMarkersItem.Click += delegate {
                ClearMenu();

                UnloadAllPackManagers();

                var loadTask = Task.Factory.StartNew(LoadAsync, TaskCreationOptions.LongRunning);
                loadTask.ContinueWith((taskResult) => { GameService.Overlay.QueueMainThreadUpdate((gameTime) => { FinalizeLoad(); }); });
            };
        }

        private void BuildCategoryMenus() {
            _mapIcon.LoadingMessage = "Building category menus...";

            var rootCategoryMenu = new ContextMenuStrip();

            var allMarkersCMS = new ContextMenuStripItem() {
                Text     = "All markers",
                Submenu  = rootCategoryMenu,
                CanCheck = false
            };

            foreach (var packManager in _packManagers) {
                packManager.BuildCategoryMenu(rootCategoryMenu);
            }

            allMarkersCMS.Parent = _mapIconMenu;
        }

        private void FinalizeLoad() {
            _mapIcon.LoadingMessage = null;

            foreach (var packManager in _packManagers) {
                packManager.FinalizeLoad();
            }

            GameService.Pathing.NewMapLoaded += _onNewMapLoaded;
        }

        protected override void OnModuleLoaded(EventArgs e) {
            FinalizeLoad();

            base.OnModuleLoaded(e);
        }

        private void UnloadAllPackManagers() {
            foreach (var packManager in _packManagers) {
                packManager.UnloadPacks();
            }
        }

        protected override void Unload() {
            // Unsubscribe from events
            GameService.Pathing.NewMapLoaded -= _onNewMapLoaded;

            // Dispose all controls
            _mapIcon.Dispose();
            _mapIconMenu.Dispose();

            // Unload and dispose all loaded pathables
            UnloadAllPackManagers();

            // Release static reference to this module instance
            ModuleInstance = null;
        }


    }

}
