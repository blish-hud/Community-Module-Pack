using System;
using Blish_HUD;
using Blish_HUD.Entities;
using Blish_HUD.Pathing;
using Blish_HUD.Pathing.Behaviors;
using Gw2Sharp.Models;
using System.Collections.Generic;

namespace Markers_and_Paths_Module.PackFormat.TacO.Behavior.Extended {

    [PathingBehavior("race")]
    public class RaceFilter<TPathable, TEntity> : FilterBehavior<TPathable, TEntity>
        where TPathable : ManagedPathable<TEntity>
        where TEntity : Entity {

        private RaceType? _race;

        public RaceFilter(TPathable managedPathable) : base(managedPathable) { /* NOOP */ }

        protected override void InitFilter() {
            if (_race.HasValue) {
                HandleRaceCheck();

                GameService.Gw2Mumble.PlayerCharacter.NameChanged += delegate { HandleRaceCheck(); };
            }
        }

        private void HandleRaceCheck() {
            this.Filtered = _race.Value != GameService.Gw2Mumble.PlayerCharacter.Race;
        }

        public override void LoadWithAttributes(IEnumerable<PathableAttribute> attributes) {
            foreach (var attr in attributes) {
                switch (attr.Name.ToLowerInvariant()) {
                    case "race":
                        if (Enum.TryParse<RaceType>(attr.Value, true, out var race)) {
                            _race = race;
                        }
                        break;
                }
            }
        }

    }
}
