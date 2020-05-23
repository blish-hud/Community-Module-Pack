using Blish_HUD.Contexts;
using Blish_HUD.Entities;
using Blish_HUD.Pathing;
using Blish_HUD.Pathing.Behaviors;
using System.Collections.Generic;

namespace Markers_and_Paths_Module.PackFormat.TacO.Behavior.Extended {

    [PathingBehavior("festival")]
    public class FestivalFilter<TPathable, TEntity> : FilterBehavior<TPathable, TEntity>
        where TPathable : ManagedPathable<TEntity>
        where TEntity : Entity {

        private FestivalContext.Festival _festival;

        public FestivalFilter(TPathable managedPathable) : base(managedPathable) { /* NOOP */ }

        protected override void InitFilter() {
            if (!_festival.IsActive()) {
                this.Filtered = true;
            }
        }

        public override void LoadWithAttributes(IEnumerable<PathableAttribute> attributes) {
            foreach (var attr in attributes) {
                switch (attr.Name.ToLowerInvariant()) {
                    case "festival":
                        _festival = FestivalContext.Festival.FromName(attr.Value.Trim());
                        break;
                }
            }
        }

    }
}
