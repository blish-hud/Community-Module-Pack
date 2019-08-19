using System.Collections.Generic;
using Blish_HUD;
using Blish_HUD.Entities;
using Blish_HUD.Pathing;
using Blish_HUD.Pathing.Content;
using Markers_and_Paths_Module.PackFormat.TacO.Prototypes;
using NanoXml;

namespace Markers_and_Paths_Module.PackFormat.TacO.Builders {
    public static class PoiBuilder {

        private static readonly Logger Logger = Logger.GetLogger(typeof(PoiBuilder));

        private const string ELEMENT_POITYPE_POI   = "poi";
        private const string ELEMENT_POITYPE_TRAIL = "trail";
        private const string ELEMENT_POITYPE_ROUTE = "route";

        private const string REQUIRED_ATTRIBUTE_MAPID     = "mapid";
        private const string REQUIRED_ATTRIBUTE_TRAILDATA = "traildata";

        public static PrototypePathable UnpackPathable(NanoXmlNode pathableNode, PathableResourceManager pathableResourceManager, PathingCategory rootCategory) {
            switch (pathableNode.Name.ToLower()) {
                case ELEMENT_POITYPE_POI:
                    var poiAttributes = AttributeBuilder.FromNanoXmlNode(pathableNode);

                    if (poiAttributes.Contains(REQUIRED_ATTRIBUTE_MAPID)) {
                        return new PrototypePathable(pathableResourceManager, PathableType.Marker, int.Parse(poiAttributes[REQUIRED_ATTRIBUTE_MAPID].Value), poiAttributes);
                    }

                    break;

                case ELEMENT_POITYPE_TRAIL:
                    var trailAttributes = AttributeBuilder.FromNanoXmlNode(pathableNode);

                    if (trailAttributes.Contains(REQUIRED_ATTRIBUTE_TRAILDATA)) {
                        string trailDataPath = trailAttributes[REQUIRED_ATTRIBUTE_TRAILDATA].Value.Trim();

                        using (var trlStream = pathableResourceManager.DataReader.GetFileStream(trailDataPath)) {
                            if (trlStream != null) {
                                List<PrototypeTrailSection> sectionData = Readers.TrlReader.ReadStream(trlStream);

                                if (sectionData.Count > 0) {
                                    return new PrototypePathable(pathableResourceManager, PathableType.Trail, sectionData[0].MapId, trailAttributes);
                                }
                            }
                        }
                    }

                    break;

                case ELEMENT_POITYPE_ROUTE:
                    Logger.Warn("Support for routes has not been added yet. They have been skipped.");
                    break;

                default:
                    Logger.Warn("Tried to load {pathableNodeName} as a POI!", pathableNode.Name);
                    break;
            }

            return null;
        }

    }

}
