using Blish_HUD.Entities;
using Blish_HUD.Pathing;
using Blish_HUD.Pathing.Behaviors;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Markers_and_Paths_Module.PackFormat.TacO.Behavior.Extended {
    public abstract class FilterBehavior<TPathable, TEntity> : PathingBehavior<TPathable, TEntity>, ILoadableBehavior
        where TPathable : ManagedPathable<TEntity>
        where TEntity : Entity {

        private bool _filtered = false;

        public bool Filtered {
            get => _filtered;
            protected set {
                _filtered = value;

                UpdateFilter(value);
            }
        }

        private List<PathingBehavior> _blockedBehaviors = new List<PathingBehavior>(0);

        private void UpdateFilter(bool filter) {
            if (filter) {
                _blockedBehaviors = new List<PathingBehavior>(this.ManagedPathable.Behavior);
                _blockedBehaviors.Remove(this);

                this.ManagedPathable.Behavior.RemoveAll(b => _blockedBehaviors.Contains(b));

                this.ManagedPathable.ManagedEntity.Visible = false;
            } else {
                this.ManagedPathable.ManagedEntity.Visible = true;

                foreach (var behavior in _blockedBehaviors) {
                    this.ManagedPathable.Behavior.Add(behavior);
                }

                _blockedBehaviors.Clear();
            }
        }

        public FilterBehavior(TPathable managedPathable) : base(managedPathable) { /* NOOP */ }

        private bool _firstUpdate = true;

        public override void UpdateBehavior(GameTime gameTime) {
            base.UpdateBehavior(gameTime);

            if (_firstUpdate) {
                _firstUpdate = false;
                InitFilter();
            }
        }

        protected abstract void InitFilter();

        public abstract void LoadWithAttributes(IEnumerable<PathableAttribute> attributes);

    }
}
