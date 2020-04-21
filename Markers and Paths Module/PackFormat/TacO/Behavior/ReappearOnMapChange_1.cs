using Blish_HUD.PersistentStore;
using Markers_and_Paths_Module.PackFormat.TacO.Pathables;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Markers_and_Paths_Module.PackFormat.TacO.Behavior {
    public class ReappearOnMapChange_1 : TacOInteractBehaviorImpl {

        private bool _triggered = false;

        public ReappearOnMapChange_1(TacOMarkerPathable managedPathable) : base(managedPathable) { /* NOOP */ }

        protected override void CheckWhileHidden(GameTime gameTime) { }

        protected override void OnInteract() {
            base.OnInteract();

            _triggered = true;
            this.HiddenByBehavior = true;
        }

        protected override void PrepareTacOBehavior() {
            Blish_HUD.GameService.Player.MapIdChanged += Player_MapIdChanged;
        }

        private void Player_MapIdChanged(object sender, EventArgs e) {
            _triggered = false;
            this.HiddenByBehavior = false;
        }
    }
}
