using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gw2Sharp.WebApi.V2.Models;
using Universal_Search_Module.Controls.SearchResultItems;

namespace Universal_Search_Module.Services.SearchHandler {
    public class LandmarkSearchHandler : SearchHandler<ContinentFloorRegionMapPoi> {
        private readonly MapApiService _mapApiService;
        private HashSet<ContinentFloorRegionMapPoi> _landmarks;


        public LandmarkSearchHandler(MapApiService mapApiService) {
            _mapApiService = mapApiService;
        }

        protected override HashSet<ContinentFloorRegionMapPoi> SearchItems => _landmarks;

        public override Task Initialize(Action<string> progress) {
            _landmarks = _mapApiService.LoadedLandmarks;
            return Task.CompletedTask;
        }

        protected override SearchResultItem CreateSearchResultItem(ContinentFloorRegionMapPoi item)
            => new LandmarkSearchResultItem() { Landmark = item };

        protected override string GetSearchableProperty(ContinentFloorRegionMapPoi item)
            => item.Name;
    }

}
