using Blish_HUD;
using Blish_HUD.Entities;
using Blish_HUD.Pathing;
using Blish_HUD.Pathing.Behaviors;
using System.Collections.Generic;

namespace Markers_and_Paths_Module.PackFormat.TacO.Behavior.Extended {

    [PathingBehavior("specialization")]
    public class SpecializationFilter<TPathable, TEntity> : FilterBehavior<TPathable, TEntity>
        where TPathable : ManagedPathable<TEntity>
        where TEntity : Entity {

        private int? _specialization;

        public SpecializationFilter(TPathable managedPathable) : base(managedPathable) { /* NOOP */ }

        protected override void InitFilter() {
            if (_specialization.HasValue) {
                HandleSpecializationCheck();

                GameService.Gw2Mumble.PlayerCharacter.SpecializationChanged += delegate { HandleSpecializationCheck(); };
            }
        }

        private void HandleSpecializationCheck() {
            this.Filtered = _specialization.Value != GameService.Gw2Mumble.PlayerCharacter.Specialization;
        }

        public override void LoadWithAttributes(IEnumerable<PathableAttribute> attributes) {
            foreach (var attr in attributes) {
                switch (attr.Name.ToLowerInvariant()) {
                    case "specialization":
                        if (InvariantUtil.TryParseInt(attr.Value, out int specialization)) {
                            _specialization = specialization;
                        }
                        break;
                }
            }
        }
    }
