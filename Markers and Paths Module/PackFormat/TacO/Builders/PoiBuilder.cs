using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Blish_HUD.Pathing.Content;

namespace Markers_and_Paths_Module.PackFormat.TacO.Builders {
    public static class PoiBuilder {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private const string ELEMENT_POITYPE_POI   = "poi";
        private const string ELEMENT_POITYPE_TRAIL = "trail";
        private const string ELEMENT_POITYPE_ROUTE = "route";

        public static void UnpackPathable(XmlNode pathableNode, PathableResourceManager pathableResourceManager, PathingCategory rootCategory) {
            switch (pathableNode.Name.ToLower()) {
                case ELEMENT_POITYPE_POI:
                    var newPoiMarker = new Pathables.TacOMarkerPathable(pathableNode, pathableResourceManager, rootCategory);

                    if (newPoiMarker.SuccessfullyLoaded) {
                        Logger.Info("Marker {markerGuid} was successfully loaded!", newPoiMarker.Guid);
                        Readers.MarkerPackReader.RegisterPathable(newPoiMarker);
                    } else {
                        Logger.Warn("Failed to load marker!");
                    }
                    break;
                case ELEMENT_POITYPE_TRAIL:
                    var newPathTrail = new Pathables.TacOTrailPathable(pathableNode, pathableResourceManager, rootCategory);

                    if (newPathTrail.SuccessfullyLoaded) {
                        Logger.Info("Trail {trailGuid} was successfully loaded!", newPathTrail.Guid);
                        Readers.MarkerPackReader.RegisterPathable(newPathTrail);
                    } else {
                        Logger.Warn("Failed to load trail!");
                    }

                    break;
                case ELEMENT_POITYPE_ROUTE:
                    Logger.Warn("Support for routes has not been added yet. They have been skipped.");

                    break;
                default:
                    Logger.Warn("Tried to pack {pathableNodeName} as a POI!", pathableNode.Name);

                    break;
            }
        }

    }

}
