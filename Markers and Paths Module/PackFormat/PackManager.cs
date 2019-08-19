using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blish_HUD.Controls;
using Blish_HUD.PersistentStore;

namespace Markers_and_Paths_Module.PackFormat {
    public abstract class PackManager {

        private readonly Store _packPersistentStore;

        protected Store PackPersistentStore => _packPersistentStore;

        public abstract string PackTypeName { get; }

        public abstract bool Loaded { get; }

        protected PackManager(Store modulePersistentStore) {
            _packPersistentStore = modulePersistentStore.GetSubstore($"PackManager-{this.PackTypeName}");
        }

        public abstract void LoadPacks(string directoryPath, IProgress<string> progressIndicator);

        public abstract void BuildCategoryMenu(ContextMenuStrip rootCategoryMenu);

        public abstract void FinalizeLoad();

        public abstract void UpdateState();

        public abstract void UnloadPacks();

    }
}
