﻿using Blish_HUD;
using Blish_HUD.Entities;
using Blish_HUD.Pathing;
using Blish_HUD.Pathing.Behaviors;
using Gw2Sharp.Models;
using System;
using System.Collections.Generic;

namespace Markers_and_Paths_Module.PackFormat.TacO.Behavior.Extended {

    [PathingBehavior("profession")]
    public class ProfessionFilter<TPathable, TEntity> : FilterBehavior<TPathable, TEntity>
        where TPathable : ManagedPathable<TEntity>
        where TEntity : Entity {

        private ProfessionType? _profession;

        public ProfessionFilter(TPathable managedPathable) : base(managedPathable) { /* NOOP */ }

        protected override void InitFilter() {
            if (_profession.HasValue) {
                HandleProfessionCheck();

                GameService.Gw2Mumble.PlayerCharacter.NameChanged += delegate { HandleProfessionCheck(); };
            }
        }

        private void HandleProfessionCheck() {
            this.Filtered = _profession.Value != GameService.Gw2Mumble.PlayerCharacter.Profession;
        }

        public override void LoadWithAttributes(IEnumerable<PathableAttribute> attributes) {
            foreach (var attr in attributes) {
                switch (attr.Name.ToLowerInvariant()) {
                    case "profession":
                        if (Enum.TryParse<ProfessionType>(attr.Value, true, out var profession)) {
                            _profession = profession;
                        }
                        break;
                }
            }
        }

    }
}
