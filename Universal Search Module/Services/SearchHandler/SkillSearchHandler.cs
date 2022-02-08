using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blish_HUD.Modules.Managers;
using Gw2Sharp.WebApi.V2.Models;
using Universal_Search_Module.Controls;
using Universal_Search_Module.Controls.SearchResultItems;

namespace Universal_Search_Module.Services.SearchHandler {
    public class SkillSearchHandler : SearchHandler<Skill> {
        private readonly Gw2ApiManager _gw2ApiManager;
        private readonly HashSet<Skill> _skills = new HashSet<Skill>();

        public override string Name => Strings.Common.SearchHandler_Skills;

        public override string Prefix => "s";

        public SkillSearchHandler(Gw2ApiManager gw2ApiManager) {
            _gw2ApiManager = gw2ApiManager;
        }

        protected override HashSet<Skill> SearchItems => _skills;

        public override async Task Initialize(Action<string> progress) {
            progress(Strings.Common.SearchHandler_Skills_SkillLoading);
            _skills.UnionWith(await _gw2ApiManager.Gw2ApiClient.V2.Skills.AllAsync());

        }

        protected override SearchResultItem CreateSearchResultItem(Skill item)
            => new SkillSearchResultItem() { Skill = item };

        protected override string GetSearchableProperty(Skill item)
            => item.Name;
    }
}
