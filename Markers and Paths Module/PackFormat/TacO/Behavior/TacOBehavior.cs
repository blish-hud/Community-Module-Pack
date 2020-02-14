using Blish_HUD.Pathing.Behaviors;
using Blish_HUD.Pathing.Entities;
using Blish_HUD.PersistentStore;
using Markers_and_Paths_Module.PackFormat.TacO.Pathables;
using Microsoft.Xna.Framework;

namespace Markers_and_Paths_Module.PackFormat.TacO.Behavior {
    
    public abstract class TacOBehavior : PathingBehavior<TacOMarkerPathable, Marker> {

        private const string TACOBEHAVIOR_STORENAME = "TacOBehaviors";

        private bool _hiddenByBehavior = false;

        protected bool HiddenByBehavior {
            get => _hiddenByBehavior;
            set {
                _hiddenByBehavior = value;
                this.ManagedPathable.ManagedEntity.Visible = !_hiddenByBehavior;
            }
        }

        private Store _tacOBehaviorStore;
        protected Store TacOStore => _tacOBehaviorStore ?? (_tacOBehaviorStore = this.BehaviorStore.GetSubstore(TACOBEHAVIOR_STORENAME).GetSubstore(this.GetType().Name));

        protected TacOBehavior(TacOMarkerPathable managedPathable) : base(managedPathable) {
            PrepareTacOBehavior();
        }

        public override void UpdateBehavior(GameTime gameTime) {
            base.UpdateBehavior(gameTime);

            if (_hiddenByBehavior) {
                CheckWhileHidden(gameTime);
            }
        }

        protected abstract void CheckWhileHidden(GameTime gameTime);

        protected abstract void PrepareTacOBehavior();

        public virtual void PushPathableStateChange(string propertyChanged) { /* NOOP */ }

        public static TacOBehavior FromBehaviorId(TacOMarkerPathable managedPathable, TacOBehaviorId behavior) {
            switch (behavior) {
                case TacOBehaviorId.AlwaysVisible: return new AlwaysVisible_0(managedPathable);
                case TacOBehaviorId.ReappearOnMapChange: return new ReappearOnMapChange_1(managedPathable);
                case TacOBehaviorId.ReappearOnDailyReset: return new ReappearOnDailyReset_2(managedPathable);
                case TacOBehaviorId.OnlyVisibleBeforeActivation: return new OnlyVisibleBeforeActivation_3(managedPathable);
                case TacOBehaviorId.ReappearAfterTimer: return new ReappearAfterTimer_4(managedPathable);
                case TacOBehaviorId.ReappearOnMapReset:
                case TacOBehaviorId.OncePerInstance:
                case TacOBehaviorId.DailyPerChar:
                case TacOBehaviorId.OncePerInstancePerChar:
                case TacOBehaviorId.WvWObjective:
                default:
                    return null;
            }
        }

    }
}
