using System;
using Blish_HUD;
using Blish_HUD.Entities;
using Blish_HUD.Pathing;
using Blish_HUD.Pathing.Behaviors;
using Gw2Sharp.Models;
using System.Collections.Generic;

namespace Markers_and_Paths_Module.PackFormat.TacO.Behavior.Extended {

    [PathingBehavior("mount")]
    public class MountFilter<TPathable, TEntity> : FilterBehavior<TPathable, TEntity>
        where TPathable : ManagedPathable<TEntity>
        where TEntity : Entity {

        private MountType? _mount;

        public MountFilter(TPathable managedPathable) : base(managedPathable) { /* NOOP */ }

        protected override void InitFilter() {
            if (_mount.HasValue) {
                HandleMountCheck();

                GameService.Gw2Mumble.PlayerCharacter.CurrentMountChanged += delegate { HandleMountCheck(); };
            }
        }

        private void HandleMountCheck() {
            this.Filtered = _mount.Value != GameService.Gw2Mumble.PlayerCharacter.CurrentMount;
        }

        public override void LoadWithAttributes(IEnumerable<PathableAttribute> attributes) {
            foreach (var attr in attributes) {
                switch (attr.Name.ToLowerInvariant()) {
                    case "mount":
                        if (Enum.TryParse<MountType>(attr.Value, true, out var mount)) {
                            _mount = mount;
                        }
                        break;
                }
            }
        }

    }
}
