using Blish_HUD.PersistentStore;
using Markers_and_Paths_Module.PackFormat.TacO.Pathables;
using Microsoft.Xna.Framework;

namespace Markers_and_Paths_Module.PackFormat.TacO.Behavior {
    public class OnlyVisibleBeforeActivation_3 : TacOInteractBehaviorImpl {

        private StoreValue<bool> _hasBeenActivated;

        public OnlyVisibleBeforeActivation_3(TacOMarkerPathable managedPathable) : base(managedPathable) { /* NOOP */ }
        
        protected override void CheckWhileHidden(GameTime gameTime) { /* NOOP */ }

        protected override void OnInteract() {
            base.OnInteract();

            _hasBeenActivated.Value = true;

            HandleCooldown();
        }

        private void HandleCooldown() {
            this.HiddenByBehavior = true;
        }

        protected override void PrepareTacOBehavior() {
            _hasBeenActivated = this.TacOStore.GetOrSetValue<bool>(this.ManagedPathable.Guid);

            if (!_hasBeenActivated.IsDefaultValue) {
                HandleCooldown();
            }
        }

    }
}
