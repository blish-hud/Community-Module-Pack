using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

        private List<Control> _moduleControls;

        private EventHandler<EventArgs> _onNewMapLoaded;

        private Store _pathableToggleStates;

        internal MarkerPackReader _currentReader;

        private CornerIcon _mapIcon;
        private ContextMenuStrip _mapIconMenu;

        private bool _packsLoaded;

        [ImportingConstructor]
        public MarkersAndPathsModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) {
            ModuleInstance = this;
        }

        protected override void DefineSettings(SettingCollection settings) {

        }

        protected override void Initialize() {
            _markerDirectory = DirectoriesManager.GetFullDirectoryPath("markers");

            _moduleControls = new List<Control>();
            _pathableToggleStates = GameService.Store.RegisterStore(this.Namespace);

            _mapIcon = new CornerIcon() {
                BasicTooltipText = "Markers & Paths",
                Icon             = ContentsManager.GetTexture("marker-pathing-icon.png"),
                Priority         = "Markers & Paths".GetHashCode(),
                Parent           = GameService.Graphics.SpriteScreen
            };

            _onNewMapLoaded = delegate {
                if (this.Loaded && _packsLoaded) {
                    _currentReader?.UpdatePathableStates();
                }
            };

            _mapIconMenu = new ContextMenuStrip();

            _mapIcon.Click += delegate { _mapIconMenu.Show(_mapIcon); };

            var loadingMenuItem = _mapIconMenu.AddMenuItem("Loading...");
            loadingMenuItem.Enabled = false;
        }

        protected override async Task LoadAsync() {
            GameService.Debug.StartTimeFunc("Markers and Paths");

            LoadPacks();
            BuildMenu();
            
            Logger.Info("Loaded {pathableCount} markers!", _currentReader.Pathables.Count);
        }

        private List<PathableResourceManager> _allPathableResourceManagers;

        private void LoadPacks() {
            _currentReader = new MarkerPackReader();

            _allPathableResourceManagers = new List<PathableResourceManager>();

            var iconProgressIndicator = new Progress<string>((report) => { _mapIcon.LoadingMessage = report; });

            var dirDataReader      = new DirectoryReader(_markerDirectory);
            var dirResourceManager = new PathableResourceManager(dirDataReader);
            _allPathableResourceManagers.Add(dirResourceManager);
            dirDataReader.LoadOnFileType((Stream fileStream, IDataReader dataReader) => {
                _currentReader.ReadFromXmlPack(fileStream, dirResourceManager);
            }, ".xml", iconProgressIndicator);

            // TODO: Cleanup
            string[] packFiles = Directory.GetFiles(_markerDirectory, "*.zip", SearchOption.AllDirectories);
            foreach (string packFile in packFiles) {
                // Potentially contains many packs within
                var zipDataReader      = new ZipArchiveReader(packFile);
                var zipResourceManager = new PathableResourceManager(zipDataReader);
                _allPathableResourceManagers.Add(zipResourceManager);
                zipDataReader.LoadOnFileType((Stream fileStream, IDataReader dataReader) => {
                    _currentReader.ReadFromXmlPack(fileStream, zipResourceManager);
                }, ".xml", iconProgressIndicator);
            }

            _packsLoaded = true;
        }

        private void AddCategoryToMenuStrip(ContextMenuStrip parentMenuStrip, PackFormat.TacO.PathingCategory newCategory) {
            var newCategoryMenuItem = parentMenuStrip.AddMenuItem(newCategory.DisplayName);

            _moduleControls.Add(newCategoryMenuItem);

            StoreValue<bool> categoryStoreState = _pathableToggleStates.GetOrSetValue(newCategory.Namespace, true);
            newCategory.Visible = categoryStoreState.Value;

            newCategoryMenuItem.CanCheck = true;
            newCategoryMenuItem.Checked  = newCategory.Visible;

            newCategoryMenuItem.CheckedChanged += delegate(object sender, CheckChangedEvent e) {
                newCategory.Visible = e.Checked;
                categoryStoreState.Value = e.Checked;
            };

            if (newCategory.Any()) {
                var childMenuStrip = new ContextMenuStrip();

                _moduleControls.Add(childMenuStrip);

                newCategoryMenuItem.Submenu = childMenuStrip;

                foreach (var childCategory in newCategory) {
                    AddCategoryToMenuStrip(childMenuStrip, childCategory);
                }
            }
        }

        private void ClearMenu() {
            foreach (var control in _mapIconMenu.Children.ToArray()) {
                control.Dispose();
            }
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

                UnloadAllPathables();

                var loadTask = Task.Factory.StartNew(LoadAsync, TaskCreationOptions.LongRunning);
                loadTask.ContinueWith((taskResult) => { GameService.Overlay.QueueMainThreadUpdate((gameTime) => { FinalizeLoad(); }); });
            };
        }

        private void BuildCategoryMenus() {
            _mapIcon.LoadingMessage = "Building category menus...";

            var rootCategoryMenu = new ContextMenuStrip();

            _moduleControls.Add(rootCategoryMenu);

            var allMarkersCMS = new ContextMenuStripItem() {
                Text     = "All markers",
                Submenu  = rootCategoryMenu,
                CanCheck = false
            };

            _moduleControls.Add(allMarkersCMS);

            foreach (var childCategory in _currentReader.Categories) {
                AddCategoryToMenuStrip(rootCategoryMenu, childCategory);
            }

            allMarkersCMS.Parent = _mapIconMenu;
        }

        private void FinalizeLoad() {
            _mapIcon.LoadingMessage = null;

            _allPathableResourceManagers.ForEach(GameService.Pathing.RegisterPathableResourceManager);

            _currentReader.UpdatePathableStates();
            GameService.Pathing.NewMapLoaded += _onNewMapLoaded;

            GameService.Debug.StopTimeFuncAndOutput("Markers and Paths");
        }

        protected override void OnModuleLoaded(EventArgs e) {
            FinalizeLoad();

            base.OnModuleLoaded(e);
        }

        private void UnloadAllPathables() {
            _packsLoaded = false;

            // Unregister all pathable resource managers
            _allPathableResourceManagers.ForEach(GameService.Pathing.UnregisterPathableResourceManager);

            // Unregister all pathables
            foreach (IPathable<Entity> pathable in _currentReader.Pathables) {
                GameService.Pathing.UnregisterPathable(pathable);
            }

            // Dispose all pathable resource managers
            _allPathableResourceManagers.ForEach(m => m.Dispose());

            // Dipose marker pack reader
            _currentReader.Dispose();
            _currentReader = null;
        }

        protected override void Unload() {
            // Unsubscribe from events
            GameService.Pathing.NewMapLoaded -= _onNewMapLoaded;

            // Dispose all controls
            _moduleControls.ForEach(c => c.Dispose());
            _moduleControls.Clear();
            _mapIcon.Dispose();
            _mapIconMenu.Dispose();

            _mapIcon     = null;
            _mapIconMenu = null;

            // Unload and dispose all loaded pathables
            UnloadAllPathables();

            // Release static reference to this module instance
            ModuleInstance = null;
        }


    }

}
