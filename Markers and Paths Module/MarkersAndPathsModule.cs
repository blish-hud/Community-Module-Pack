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
using Blish_HUD.Modules;
using Blish_HUD.Pathing.Content;

namespace Markers_and_Paths_Module {

    [Export(typeof(ExternalModule))]
    public class MarkersAndPathsModule : Blish_HUD.Modules.ExternalModule {

        private string _markerDirectory;

        [ImportingConstructor]
        public MarkersAndPathsModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { /* NOOP */ }

        protected override void DefineSettings(SettingsManager settingsManager) {
            settingsManager.DefineSetting("Test Setting", true, true, true, "This is a test setting. Should be removed before next community release.");
        }

        protected override void Initialize() {
            _markerDirectory = DirectoriesManager.GetFullDirectoryPath("markers");

            GameService.Pathing.NewMapLoaded += delegate { PackFormat.TacO.Readers.MarkerPackReader.UpdatePathableStates(); };
        }

        protected override async Task LoadAsync() {
            GameService.Debug.StartTimeFunc("Markers and Paths");

            LoadPacks();
            BuildCategoryMenus();
        }

        private void LoadPacks() {
            var dirDataReader      = new DirectoryReader(_markerDirectory);
            var dirResourceManager = new PathableResourceManager(dirDataReader);
            GameService.Pathing.RegisterPathableResourceManager(dirResourceManager);
            dirDataReader.LoadOnFileType((Stream fileStream, IDataReader dataReader) => {
                PackFormat.TacO.Readers.MarkerPackReader.ReadFromXmlPack(fileStream, dirResourceManager);
            }, ".xml");

            // TODO: Cleanup
            string[] packFiles = Directory.GetFiles(_markerDirectory, "*.zip", SearchOption.AllDirectories);
            foreach (string packFile in packFiles) {
                // Potentially contains many packs within
                var zipDataReader      = new ZipArchiveReader(packFile);
                var zipResourceManager = new PathableResourceManager(zipDataReader);
                GameService.Pathing.RegisterPathableResourceManager(zipResourceManager);
                zipDataReader.LoadOnFileType((Stream fileStream, IDataReader dataReader) => {
                    PackFormat.TacO.Readers.MarkerPackReader.ReadFromXmlPack(fileStream, zipResourceManager);
                }, ".xml");
            }
        }

        private void AddCategoryToMenuStrip(ContextMenuStrip parentMenuStrip, PackFormat.TacO.PathingCategory newCategory) {
            var newCategoryMenuItem = parentMenuStrip.AddMenuItem(newCategory.DisplayName);
            newCategoryMenuItem.CanCheck = true;
            newCategoryMenuItem.Checked  = newCategory.Visible;

            newCategoryMenuItem.CheckedChanged += delegate (object sender, CheckChangedEvent e) { newCategory.Visible = e.Checked; };

            if (newCategory.Any()) {
                var childMenuStrip = new ContextMenuStrip();
                newCategoryMenuItem.Submenu = childMenuStrip;

                foreach (var childCategory in newCategory) {
                    AddCategoryToMenuStrip(childMenuStrip, childCategory);
                }
            }
        }

        private void BuildCategoryMenus() {
            GameService.Pathing.Icon.LoadingMessage = "Building category menus...";

            GameService.Director.QueueMainThreadUpdate((gameTime) => {
                var rootCategoryMenu = new ContextMenuStrip();

                var allMarkersCMS = new ContextMenuStripItem() {
                    Text     = "All markers",
                    Submenu  = rootCategoryMenu,
                    CanCheck = false
                };

                foreach (var childCategory in PackFormat.TacO.Readers.MarkerPackReader.Categories) {
                    AddCategoryToMenuStrip(rootCategoryMenu, childCategory);
                }

                allMarkersCMS.Parent = GameService.Pathing.IconContextMenu;
            });
        }

        protected override void OnModuleLoaded(EventArgs e) {
            GameService.Pathing.Icon.LoadingMessage = null;
            GameService.Debug.StopTimeFuncAndOutput("Markers and Paths");

            base.OnModuleLoaded(e);
        }


    }

}
