using Blish_HUD.PersistentStore;
using Markers_and_Paths_Module.PackFormat.TacO.Pathables;
using Microsoft.Xna.Framework;
using System;

namespace Markers_and_Paths_Module.PackFormat.TacO.Behavior {
    public class ReappearOnDailyReset_2 : TacOInteractBehaviorImpl {

        private StoreValue<DateTime> _nextReset;

        public ReappearOnDailyReset_2(TacOMarkerPathable managedPathable) : base(managedPathable) { /* NOOP */ }

        private bool IsOnCooldown() {
            return DateTime.UtcNow <= _nextReset.Value;
        }

        private void ResetRecordedNextReset() {
            // Reset the store value so that it is the "default" value again (so it doesn't stay stuck in PersistentStore forever)
            this.TacOStore.RemoveValueByName(this.ManagedPathable.Guid);

            _nextReset = this.TacOStore.GetOrSetValue<DateTime>(this.ManagedPathable.Guid);
        }

        protected override void CheckWhileHidden(GameTime gameTime) {
            if (!IsOnCooldown()) {
                this.HiddenByBehavior = false;
                ResetRecordedNextReset();
            }
        }

        protected override void OnInteract() {
            base.OnInteract();

            _nextReset.Value = DateTime.UtcNow.Date.AddDays(1);

            HandleCooldown();
        }

        private void HandleCooldown() {
            this.HiddenByBehavior = true;
        }

        protected override void PrepareTacOBehavior() {
            _nextReset = this.TacOStore.GetOrSetValue<DateTime>(this.ManagedPathable.Guid);

            if (!_nextReset.IsDefaultValue && IsOnCooldown()) {
                HandleCooldown();
            }
        }
    }
}
