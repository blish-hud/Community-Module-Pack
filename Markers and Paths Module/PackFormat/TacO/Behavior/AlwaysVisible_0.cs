using Markers_and_Paths_Module.PackFormat.TacO.Pathables;
using Microsoft.Xna.Framework;

namespace Markers_and_Paths_Module.PackFormat.TacO.Behavior {
    public class AlwaysVisible_0 : TacOBehavior {

        public AlwaysVisible_0(TacOMarkerPathable managedPathable) : base(managedPathable) { /* NOOP */ }

        protected override void CheckWhileHidden(GameTime gameTime) { /* NOOP */ }

        protected override void PrepareTacOBehavior() { /* NOOP */ }

        public override void PushPathableStateChange(string propertyChanged) { /* NOOP */ }

    }
}
