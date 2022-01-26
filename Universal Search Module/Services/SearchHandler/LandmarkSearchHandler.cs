using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blish_HUD.Modules.Managers;
using Gw2Sharp.WebApi.V2.Models;
using Universal_Search_Module.Controls.SearchResultItems;

namespace Universal_Search_Module.Services.SearchHandler {
    public class LandmarkSearchHandler : SearchHandler<ContinentFloorRegionMapPoi> {
        private readonly Gw2ApiManager _gw2ApiManager;
        private readonly HashSet<ContinentFloorRegionMapPoi> _landmarks = new HashSet<ContinentFloorRegionMapPoi>();

        public override string Name => "Landmarks";

        public LandmarkSearchHandler(Gw2ApiManager gw2ApiManager) {
            _gw2ApiManager = gw2ApiManager;
        }

        protected override HashSet<ContinentFloorRegionMapPoi> SearchItems => _landmarks;

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
                        _landmarks.UnionWith(mapPair.Value.PointsOfInterest.Values.Where(landmark => landmark.Name != null));
                    }
                }
            }
        }

        protected override SearchResultItem CreateSearchResultItem(ContinentFloorRegionMapPoi item)
            => new LandmarkSearchResultItem() { Landmark = item };

        protected override string GetSearchableProperty(ContinentFloorRegionMapPoi item)
            => item.Name;
    }

}
