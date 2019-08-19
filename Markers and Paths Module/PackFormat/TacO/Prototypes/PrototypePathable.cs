using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Entities;
using Blish_HUD.Pathing;
using Blish_HUD.Pathing.Content;
using Blish_HUD.Pathing.Format;
using Markers_and_Paths_Module.PackFormat.TacO.Pathables;

namespace Markers_and_Paths_Module.PackFormat.TacO.Prototypes {
    public class PrototypePathable {

        private static readonly Logger Logger = Logger.GetLogger(typeof(PrototypePathable));

        private readonly PathableResourceManager     _resourceManager;
        private readonly PathableType                _type;
        private readonly int                         _mapId;
        private readonly PathableAttributeCollection _attributes;

        public PathableResourceManager     ResourceManager => _resourceManager;
        public PathableType                Type            => _type;
        public int                         MapId           => _mapId;
        public PathableAttributeCollection Attributes      => _attributes;

        public PrototypePathable(PathableResourceManager resourceManager, PathableType type, int mapId, PathableAttributeCollection attributes) {
            _resourceManager = resourceManager;
            _type            = type;
            _mapId           = mapId;
            _attributes      = attributes;
        }

        public IPathable<Entity> LoadPathable(PathingCategory rootCategory) {
            switch (_type) {
                case PathableType.Marker:
                    return LoadMarkerPathable(rootCategory);
                case PathableType.Trail:
                    return LoadTrailPathable(rootCategory);
                default:
                    Logger.Warn("Unknown pathable type of {pathableType} could not be loaded!", _type);
                    return null;
            }
        }

        private LoadedMarkerPathable LoadMarkerPathable(PathingCategory rootCategory) {
            var newMarker = new TacOMarkerPathable(_attributes, _resourceManager, rootCategory);

            if (newMarker.SuccessfullyLoaded) {
                return newMarker;
            } else {
                Logger.Warn("Failed to load marker: {markerInfo}", _attributes);
                return null;
            }
        }

        private LoadedTrailPathable LoadTrailPathable(PathingCategory rootCategory) {
            var newTrail = new TacOTrailPathable(_attributes, _resourceManager, rootCategory);

            if (newTrail.SuccessfullyLoaded) {
                return newTrail;
            } else {
                Logger.Warn("Failed to load trail: {trailInfo}", _attributes);
                return null;
            }
        }

    }
}
