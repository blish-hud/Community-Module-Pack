using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Entities;
using Blish_HUD.Pathing;
using Blish_HUD.Pathing.Content;
using Blish_HUD.PersistentStore;
using Markers_and_Paths_Module.PackFormat.TacO.Readers;

namespace Markers_and_Paths_Module.PackFormat.TacO {
    public class TacOManager : PackManager {

        private const string PACKTYPENAME = "TacO";

        private bool                          _loaded = false;
        private MarkerPackReader              _currentReader;
        private List<PathableResourceManager> _allPathableResourceManagers;

        public override string PackTypeName => PACKTYPENAME;
        public override bool   Loaded       => _loaded;

        public TacOManager(Store modulePersistentStore) : base(modulePersistentStore) { /* NOOP */ }

        public override void LoadPacks(string directoryPath, IProgress<string> progressIndicator) {
            _currentReader = new MarkerPackReader();

            _allPathableResourceManagers = new List<PathableResourceManager>();

            var dirDataReader      = new DirectoryReader(directoryPath);
            var dirResourceManager = new PathableResourceManager(dirDataReader);
            _allPathableResourceManagers.Add(dirResourceManager);
            dirDataReader.LoadOnFileType((Stream fileStream, IDataReader dataReader) => {
                _currentReader.ReadFromXmlPack(fileStream, dirResourceManager);
            }, ".xml", progressIndicator);

            // Load archive marker packs
            var zipPackFiles = new List<string>();
            zipPackFiles.AddRange(Directory.GetFiles(directoryPath, "*.zip",  SearchOption.AllDirectories));
            zipPackFiles.AddRange(Directory.GetFiles(directoryPath, "*.taco", SearchOption.AllDirectories));

            foreach (string packFile in zipPackFiles) {
                // Potentially contains many packs within
                var zipDataReader      = new ZipArchiveReader(packFile);
                var zipResourceManager = new PathableResourceManager(zipDataReader);
                _allPathableResourceManagers.Add(zipResourceManager);
                zipDataReader.LoadOnFileType((Stream fileStream, IDataReader dataReader) => {
                    _currentReader.ReadFromXmlPack(fileStream, zipResourceManager);
                }, ".xml", progressIndicator);
            }

            _loaded = true;
        }

        private void AddCategoryToMenuStrip(ContextMenuStrip parentMenuStrip, PathingCategory newCategory) {
            var newCategoryMenuItem = parentMenuStrip.AddMenuItem(newCategory.DisplayName);
            
            StoreValue<bool> categoryStoreState = this.PackPersistentStore.GetOrSetValue(newCategory.Namespace, true);
            newCategory.Visible = categoryStoreState.Value;

            newCategoryMenuItem.CanCheck = true;
            newCategoryMenuItem.Checked  = newCategory.Visible;

            newCategoryMenuItem.CheckedChanged += delegate (object sender, CheckChangedEvent e) {
                newCategory.Visible      = e.Checked;
                categoryStoreState.Value = e.Checked;
            };

            if (newCategory.Count > 0) {
                var childMenuStrip = new ContextMenuStrip();

                newCategoryMenuItem.Submenu = childMenuStrip;

                foreach (var childCategory in newCategory) {
                    AddCategoryToMenuStrip(childMenuStrip, childCategory);
                }
            }
        }

        public override void BuildCategoryMenu(ContextMenuStrip rootCategoryMenu) {
            for (int i = 0; i < _currentReader.Categories.Count; i++) {
                AddCategoryToMenuStrip(rootCategoryMenu, _currentReader.Categories[i]);
            }
        }

        public override void FinalizeLoad() {
            _allPathableResourceManagers.ForEach(GameService.Pathing.RegisterPathableResourceManager);

            _currentReader.UpdatePrototypePathableStates();
        }

        public override void UpdateState() {
            //_currentReader.UpdatePathableStates();
            _currentReader.UpdatePrototypePathableStates();
        }

        public override void UnloadPacks() {
            // Unregister all pathable resource managers
            _allPathableResourceManagers.ForEach(GameService.Pathing.UnregisterPathableResourceManager);

            // Unregister all pathables
            _currentReader.UnloadPack();

            // Dispose all pathable resource managers
            _allPathableResourceManagers.ForEach(m => m.Dispose());
        }

    }
}
