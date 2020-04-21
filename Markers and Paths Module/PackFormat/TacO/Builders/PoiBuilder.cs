using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Blish_HUD;
using Blish_HUD.Pathing.Content;
using NanoXml;

namespace Markers_and_Paths_Module.PackFormat.TacO.Builders {
    public static class PoiBuilder {

        private static readonly Logger Logger = Logger.GetLogger(typeof(PoiBuilder));

        private const string ELEMENT_POITYPE_POI   = "poi";
        private const string ELEMENT_POITYPE_TRAIL = "trail";
        private const string ELEMENT_POITYPE_ROUTE = "route";

        public static void UnpackPathable(NanoXmlNode pathableNode, PathableResourceManager pathableResourceManager, PathingCategory rootCategory) {
            switch (pathableNode.Name.ToLowerInvariant()) {
                case ELEMENT_POITYPE_POI:
                    var poiAttributes = AttributeBuilder.FromNanoXmlNode(pathableNode);

                    var newPoiMarker = new Pathables.TacOMarkerPathable(poiAttributes, pathableResourceManager, rootCategory);

                    if (newPoiMarker.SuccessfullyLoaded) {
                        MarkersAndPathsModule.ModuleInstance._currentReader.RegisterPathable(newPoiMarker);
                    } else {
                        Logger.Warn("Failed to load marker: {markerInfo}", poiAttributes);
                    }
                    break;

                case ELEMENT_POITYPE_TRAIL:
                    var trailAttributes = AttributeBuilder.FromNanoXmlNode(pathableNode);

                    var newPathTrail = new Pathables.TacOTrailPathable(trailAttributes, pathableResourceManager, rootCategory);

                    if (newPathTrail.SuccessfullyLoaded) {
                        MarkersAndPathsModule.ModuleInstance._currentReader.RegisterPathable(newPathTrail);
                    } else {
                        Logger.Warn("Failed to load trail: {trailInfo}", trailAttributes);
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
