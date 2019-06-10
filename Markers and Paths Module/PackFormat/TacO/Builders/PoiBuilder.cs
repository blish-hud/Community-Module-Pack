using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Blish_HUD.Pathing.Content;

namespace Markers_and_Paths_Module.PackFormat.TacO.Builders {
    public static class PoiBuilder {

        private const string ELEMENT_POITYPE_POI   = "poi";
        private const string ELEMENT_POITYPE_TRAIL = "trail";
        private const string ELEMENT_POITYPE_ROUTE = "route";

        public static void UnpackPathable(XmlNode pathableNode, PathableResourceManager pathableResourceManager, PathingCategory rootCategory) {
            switch (pathableNode.Name.ToLower()) {
                case ELEMENT_POITYPE_POI:
                    var newPoiMarker = new Pathables.TacOMarkerPathable(pathableNode, pathableResourceManager, rootCategory);

                    if (newPoiMarker.SuccessfullyLoaded) {
                        Readers.MarkerPackReader.RegisterPathable(newPoiMarker);
                    } else {
                        Console.WriteLine("Failed to load marker: ");
                        //Console.WriteLine(string.Join("; ", pathableNode.Attributes.Select(s => ((XmlAttribute)s).Name + " = " + ((XmlAttribute)s).Value)));
                    }
                    break;
                case ELEMENT_POITYPE_TRAIL:
                    var newPathTrail = new Pathables.TacOTrailPathable(pathableNode, pathableResourceManager, rootCategory);

                    if (newPathTrail.SuccessfullyLoaded) {
                        Readers.MarkerPackReader.RegisterPathable(newPathTrail);
                    } else {
                        Console.WriteLine("Failed to load trail: ");
                        //Console.WriteLine(string.Join("; ", pathableNode.Attributes.Select(s => ((XmlAttribute)s).Name + " = " + ((XmlAttribute)s).Value)));
                    }

                    break;
                case ELEMENT_POITYPE_ROUTE:
                    Console.WriteLine("Skipped loading route.");
                    //RouteBuilder.UnpackNode(pathableNode);

                    break;
                default:
                    Console.WriteLine($"Tried to unpack '{pathableNode.Name}' as POI!");
                    break;
            }
        }

    }

}
