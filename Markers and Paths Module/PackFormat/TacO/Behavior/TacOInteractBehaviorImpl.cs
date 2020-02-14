using Blish_HUD.Pathing.Behaviors.Activator;
using Blish_HUD.Pathing.Entities;
using Markers_and_Paths_Module.PackFormat.TacO.Pathables;
using System;

namespace Markers_and_Paths_Module.PackFormat.TacO.Behavior {
    public abstract class TacOInteractBehaviorImpl : TacOActivatedBehaviorImpl<ZoneActivator<TacOMarkerPathable, Marker>> {

        private const string STATE_TRIGGERRANGE = nameof(TacOMarkerPathable.TriggerRange);
        private const string STATE_AUTOTRIGGER = nameof(TacOMarkerPathable.AutoTrigger);

        protected TacOInteractBehaviorImpl(TacOMarkerPathable managedPathable) : base(managedPathable) {
            this.TacOActivator = new ZoneActivator<TacOMarkerPathable, Marker>(this) {
                DistanceFrom = DistanceFrom.Player,
                ActivationDistance = managedPathable.TriggerRange
            };

            this.Activator.Activated += Activator_Activated;
            this.Activator.Deactivated += Activator_Deactivated;
        }

        private void Activator_Activated(object sender, EventArgs e) {
            if (this.ManagedPathable.AutoTrigger) {
                Blish_HUD.Common.Gw2.KeyBindings.Interact.Activated -= Interact_Activated;

                Interact_Activated(sender, e);
            } else {
                Blish_HUD.Common.Gw2.KeyBindings.Interact.Activated += Interact_Activated;
            }
        }

        private void Activator_Deactivated(object sender, EventArgs e) {
            Blish_HUD.Common.Gw2.KeyBindings.Interact.Activated -= Interact_Activated;
        }

        private void UpdateStateFromTriggerRange() {
            this.TacOActivator.ActivationDistance = this.ManagedPathable.TriggerRange;
        }

        private void UpdateStateFromAutoTrigger() {
            if (this.ManagedPathable.AutoTrigger) {
                // Covers corner case where user is within the trigger range of a marker when "AutoTrigger" is enabled.
                if (this.Activator.Active) {
                    OnInteract();
                }

                Blish_HUD.Common.Gw2.KeyBindings.Interact.Activated -= Interact_Activated;
            }
        }

        public override void PushPathableStateChange(string propertyChanged) {
            switch (propertyChanged) {
                case STATE_TRIGGERRANGE: UpdateStateFromTriggerRange(); return;
                case STATE_AUTOTRIGGER: UpdateStateFromAutoTrigger(); return;
                default: break;
            }

            base.PushPathableStateChange(propertyChanged);
        }

        private void Interact_Activated(object sender, EventArgs e) {
            if (this.Activator.Active && !this.HiddenByBehavior) {
                OnInteract();
            }
        }

        protected virtual void OnInteract() { /* NOOP */ }

    }
}
