using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
using Blish_HUD.Settings;
using NLog;

namespace Markers_and_Paths_Module {

    [Export(typeof(Module))]
    public class MarkersAndPathsModule : Module {

        internal static MarkersAndPathsModule ModuleInstance;

        // Service Managers
        internal SettingsManager    SettingsManager    => this.ModuleParameters.SettingsManager;
        internal ContentsManager    ContentsManager    => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager      Gw2ApiManager      => this.ModuleParameters.Gw2ApiManager;

        private string _markerDirectory;

        private List<Control> _moduleControls;

        private EventHandler<EventArgs> _onNewMapLoaded;

        private PersistentStore _pathableToggleStates;

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

            _onNewMapLoaded = delegate { PackFormat.TacO.Readers.MarkerPackReader.UpdatePathableStates(); };
            GameService.Pathing.NewMapLoaded += _onNewMapLoaded;
        }

        protected override async Task LoadAsync() {
            GameService.Debug.StartTimeFunc("Markers and Paths");

            LoadPacks();
            BuildCategoryMenus();
        }

        private List<PathableResourceManager> _allPathableResourceManagers;

        private void LoadPacks() {
            _allPathableResourceManagers = new List<PathableResourceManager>();

            var dirDataReader      = new DirectoryReader(_markerDirectory);
            var dirResourceManager = new PathableResourceManager(dirDataReader);
            _allPathableResourceManagers.Add(dirResourceManager);
            dirDataReader.LoadOnFileType((Stream fileStream, IDataReader dataReader) => {
                PackFormat.TacO.Readers.MarkerPackReader.ReadFromXmlPack(fileStream, dirResourceManager);
            }, ".xml");

            // TODO: Cleanup
            string[] packFiles = Directory.GetFiles(_markerDirectory, "*.zip", SearchOption.AllDirectories);
            foreach (string packFile in packFiles) {
                // Potentially contains many packs within
                var zipDataReader      = new ZipArchiveReader(packFile);
                var zipResourceManager = new PathableResourceManager(zipDataReader);
                _allPathableResourceManagers.Add(zipResourceManager);
                zipDataReader.LoadOnFileType((Stream fileStream, IDataReader dataReader) => {
                    PackFormat.TacO.Readers.MarkerPackReader.ReadFromXmlPack(fileStream, zipResourceManager);
                }, ".xml");
            }
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

        private void BuildCategoryMenus() {
            GameService.Pathing.Icon.LoadingMessage = "Building category menus...";

            GameService.Overlay.QueueMainThreadUpdate((gameTime) => {
                var rootCategoryMenu = new ContextMenuStrip();

                _moduleControls.Add(rootCategoryMenu);

                var allMarkersCMS = new ContextMenuStripItem() {
                    Text     = "All markers",
                    Submenu  = rootCategoryMenu,
                    CanCheck = false
                };

                _moduleControls.Add(allMarkersCMS);

                foreach (var childCategory in PackFormat.TacO.Readers.MarkerPackReader.Categories) {
                    AddCategoryToMenuStrip(rootCategoryMenu, childCategory);
                }

                allMarkersCMS.Parent = GameService.Pathing.IconContextMenu;
            });
        }

        protected override void OnModuleLoaded(EventArgs e) {
            GameService.Pathing.Icon.LoadingMessage = null;

            _allPathableResourceManagers.ForEach(GameService.Pathing.RegisterPathableResourceManager);

            GameService.Debug.StopTimeFuncAndOutput("Markers and Paths");
            
            PackFormat.TacO.Readers.MarkerPackReader.UpdatePathableStates();

            base.OnModuleLoaded(e);
        }

        protected override void Unload() {
            ModuleInstance = null;

            GameService.Pathing.NewMapLoaded -= _onNewMapLoaded;
            _moduleControls.ForEach(c => c.Dispose());
            _moduleControls.Clear();
            _allPathableResourceManagers.ForEach(GameService.Pathing.UnregisterPathableResourceManager);

            foreach (IPathable<Entity> pathable in PackFormat.TacO.Readers.MarkerPackReader.Pathables) {
                GameService.Pathing.UnregisterPathable(pathable);
            }

            PackFormat.TacO.Readers.MarkerPackReader.Pathables.Clear();
            PackFormat.TacO.Readers.MarkerPackReader.Categories.Clear();
        }


    }

}
