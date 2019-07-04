using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;

namespace Universal_Search_Module {
    public static class LandmarkUtil {

        public static ContinentFloorRegionMapPoi GetClosestWaypoint(IEnumerable<ContinentFloorRegionMapPoi> waypoints, ContinentFloorRegionMapPoi landmark) {
            ContinentFloorRegionMapPoi closestWaypoint = null;
            float                      distance        = float.MaxValue;

            var staticPos = new Vector2((float) landmark.Coord.X, (float) landmark.Coord.Y);

            foreach (var waypoint in waypoints) {
                var pos = new Vector2((float) waypoint.Coord.X, (float) waypoint.Coord.Y);

                var netDistance = Vector2.Distance(staticPos, pos);

                if (netDistance < distance) {
                    closestWaypoint = waypoint;
                    distance        = netDistance;
                }
            }

            return closestWaypoint;
        }

    }
}
