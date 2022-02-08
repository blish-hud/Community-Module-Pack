using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blish_HUD.Modules.Managers;
using Gw2Sharp.WebApi.V2.Models;
using Universal_Search_Module.Controls.SearchResultItems;

namespace Universal_Search_Module.Services.SearchHandler {
    public class LandmarkSearchHandler : SearchHandler<Landmark> {
        private readonly Gw2ApiManager _gw2ApiManager;
        private readonly HashSet<Landmark> _landmarks = new HashSet<Landmark>();

        public override string Name => Strings.Common.SearchHandler_Landmarks;

        public override string Prefix => "l";

        public LandmarkSearchHandler(Gw2ApiManager gw2ApiManager) {
            _gw2ApiManager = gw2ApiManager;
        }

        protected override HashSet<Landmark> SearchItems => _landmarks;

        public override async Task Initialize(Action<string> progress) {
            // Continent 1 = Tyria
            // Continent 2 = Mists
            // Fetching a single floor will return all nested subresources as well, so fetch all floors
            var floors = await _gw2ApiManager.Gw2ApiClient.V2.Continents[1].Floors.IdsAsync();

            foreach (var floorId in floors) {
                progress($"Loading floor {floorId}...");
                var floor = await _gw2ApiManager.Gw2ApiClient.V2.Continents[1].Floors[floorId].GetAsync();
                foreach (var regionPair in floor.Regions) {
                    foreach (var mapPair in regionPair.Value.Maps) {
                        foreach (var landmark in mapPair.Value.PointsOfInterest.Values.Where(landmark => landmark.Name != null).Select(x => new Landmark() { PointOfInterest = x, Map = mapPair.Value })) {
                            var existingLandmark = _landmarks.FirstOrDefault(x => x.PointOfInterest.ChatLink == landmark.PointOfInterest.ChatLink);
                            if (existingLandmark == null) {
                                _landmarks.Add(landmark);
                            } else {
                                if(!existingLandmark.Map.PointsOfInterest.Any(x => x.Value.Type.ToEnum() == PoiType.Waypoint) && landmark.Map.PointsOfInterest.Any(x => x.Value.Type.ToEnum() == PoiType.Waypoint)) {
                                    _landmarks.Remove(existingLandmark);
                                    _landmarks.Add(landmark);
                                }
                            }
                        }
                    }
                }
            }
        }

        protected override SearchResultItem CreateSearchResultItem(Landmark item) {
            var possibleWaypoints = _landmarks.Where(x => x.Map == item.Map && x.PointOfInterest.Type.ToEnum() == PoiType.Waypoint);
            // For the case where a landmark exists only in an instance where no waypoint is, just take the closest waypoint from all waypoints
            if (!possibleWaypoints.Any()) {
                possibleWaypoints = _landmarks.Where(x => x.PointOfInterest.Type.ToEnum() == PoiType.Waypoint);
            }
            return new LandmarkSearchResultItem(possibleWaypoints) { Landmark = item };
        }

        protected override string GetSearchableProperty(Landmark item)
            => item.PointOfInterest.Name;
    }

    public class Landmark {
        public ContinentFloorRegionMapPoi PointOfInterest { get; set; }

        public ContinentFloorRegionMap Map { get; set; }
    }

}
