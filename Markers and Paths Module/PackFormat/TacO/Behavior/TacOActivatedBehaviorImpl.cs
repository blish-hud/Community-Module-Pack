using Microsoft.Xna.Framework;
using System;
using Blish_HUD.Pathing.Behaviors.Activator;
using Blish_HUD.Pathing.Entities;
using Markers_and_Paths_Module.PackFormat.TacO.Pathables;

namespace Markers_and_Paths_Module.PackFormat.TacO.Behavior {
    public abstract class TacOActivatedBehaviorImpl<TActivator> : TacOBehavior
        where TActivator : Activator<TacOMarkerPathable, Marker> {

        protected TActivator TacOActivator {
            get => (TActivator)this.Activator;
            set {
                if (Equals(this.Activator, value)) return;

                this.Activator?.Dispose();

                this.Activator = value;
            }
        }

        protected TacOActivatedBehaviorImpl(TacOMarkerPathable managedPathable) : base(managedPathable) { /* NOOP */ }

    }
}
