using Blish_HUD.PersistentStore;
using Markers_and_Paths_Module.PackFormat.TacO.Pathables;
using Microsoft.Xna.Framework;
using System;

namespace Markers_and_Paths_Module.PackFormat.TacO.Behavior {
    public class ReappearAfterTimer_4 : TacOInteractBehaviorImpl {

        private const string STATE_RESETLENGTH  = nameof(TacOMarkerPathable.ResetLength);
        private const string STATE_TRIGGERRANGE = nameof(TacOMarkerPathable.TriggerRange);

        private StoreValue<DateTime> _lastInteract;

        public ReappearAfterTimer_4(TacOMarkerPathable managedPathable) : base(managedPathable) { /* NOOP */ }

        private bool IsOnCooldown() {
            return (DateTime.UtcNow - _lastInteract.Value).TotalSeconds <= this.ManagedPathable.ResetLength;
        }

        private void ResetRecordedLastInteract() {
            // Reset the store value so that it is the "default" value again (so it doesn't stay stuck in PersistentStore forever)
            this.TacOStore.RemoveValueByName(this.ManagedPathable.Guid);

            _lastInteract = this.TacOStore.GetOrSetValue<DateTime>(this.ManagedPathable.Guid);
        }

        private void UpdateTriggerRange() {
            this.TacOActivator.ActivationDistance = this.ManagedPathable.TriggerRange;
        }

        public override void PushPathableStateChange(string propertyChanged) {
            switch (propertyChanged) {
                case STATE_RESETLENGTH: ResetRecordedLastInteract(); return;
                case STATE_TRIGGERRANGE: UpdateTriggerRange(); return;
                default: break;
            }

            base.PushPathableStateChange(propertyChanged);
        }

        protected override void CheckWhileHidden(GameTime gameTime) {
            if (!IsOnCooldown()) {
                this.HiddenByBehavior = false;
                ResetRecordedLastInteract();
            }
        }

        protected override void OnInteract() {
            base.OnInteract();

            _lastInteract.Value = DateTime.UtcNow;

            HandleCooldown();
        }

        private void HandleCooldown() {
            this.HiddenByBehavior = true;
        }

        protected override void PrepareTacOBehavior() {
            _lastInteract = this.TacOStore.GetOrSetValue<DateTime>(this.ManagedPathable.Guid);

            if (!_lastInteract.IsDefaultValue && IsOnCooldown()) {
                HandleCooldown();
            }
        }
    }
}
