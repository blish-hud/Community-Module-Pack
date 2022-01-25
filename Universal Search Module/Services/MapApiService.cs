using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blish_HUD.Modules.Managers;
using Gw2Sharp.WebApi.V2.Models;

namespace Universal_Search_Module {
    public class MapApiService {
        private readonly Gw2ApiManager _gw2ApiManager;
        
        public HashSet<ContinentFloorRegionMapPoi> LoadedLandmarks { get; } = new HashSet<ContinentFloorRegionMapPoi>();

        public HashSet<ContinentFloorRegionMapSkillChallenge> HeroPoints { get; } = new HashSet<ContinentFloorRegionMapSkillChallenge>();
        
        public HashSet<ContinentFloorRegionMapMasteryPoint> MasteryPoints { get; } = new HashSet<ContinentFloorRegionMapMasteryPoint>();
        
        public HashSet<ContinentFloorRegionMapTask> HeroHearts { get; } = new HashSet<ContinentFloorRegionMapTask>();
        
        public HashSet<ContinentFloorRegionMapSector> Areas { get; } = new HashSet<ContinentFloorRegionMapSector>();

        public MapApiService(Gw2ApiManager gw2ApiManager) {
            _gw2ApiManager = gw2ApiManager;
        }

        public async Task Initialize(Action<string> progress) {
            // Continent 1 = Tyria
            // Continent 2 = Mists
            // Fetching a single floor will return all nested subresources as well, so fetch all floors
            var floors = await _gw2ApiManager.Gw2ApiClient.V2.Continents[1].Floors.IdsAsync();

            foreach (var floorId in floors) {
                progress($"Loading floor {floorId}...");
                var floor = await _gw2ApiManager.Gw2ApiClient.V2.Continents[1].Floors[floorId].GetAsync();
                foreach (var regionPair in floor.Regions) {
                    foreach (var mapPair in regionPair.Value.Maps) {
                        LoadedLandmarks.UnionWith(mapPair.Value.PointsOfInterest.Values.Where(landmark => landmark.Name != null));
                        HeroPoints.UnionWith(mapPair.Value.SkillChallenges);
                        MasteryPoints.UnionWith(mapPair.Value.MasteryPoints);
                        HeroHearts.UnionWith(mapPair.Value.Tasks.Values);
                        Areas.UnionWith(mapPair.Value.Sectors.Values);
                    }
                }
            }
        }
    }

}
