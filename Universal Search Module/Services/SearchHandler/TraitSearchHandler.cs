using Blish_HUD.Modules.Managers;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Universal_Search_Module.Controls.SearchResultItems;

namespace Universal_Search_Module.Services.SearchHandler {
    public class TraitSearchHandler : SearchHandler<Trait> {
        private readonly Gw2ApiManager _gw2ApiManager;
        private readonly HashSet<Trait> _traits = new HashSet<Trait>();
        
        public override string Name => Strings.Common.SearchHandler_Traits;

        public override string Prefix => "t";

        protected override HashSet<Trait> SearchItems => _traits;

        public TraitSearchHandler(Gw2ApiManager gw2ApiManager) {
            _gw2ApiManager = gw2ApiManager;
        }

        public override async Task Initialize(Action<string> progress) {
            progress("Loading Skills ...");
            _traits.UnionWith(await _gw2ApiManager.Gw2ApiClient.V2.Traits.AllAsync());
        }

        protected override SearchResultItem CreateSearchResultItem(Trait item)
            => new TraitSearchResultItem() { Trait = item };

        protected override string GetSearchableProperty(Trait item)
            => item.Name;
    }
}
