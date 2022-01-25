using Blish_HUD;
using Blish_HUD.Content;
using Gw2Sharp.WebApi.V2.Models;

namespace Universal_Search_Module.Controls.SearchResultItems {
    public class SkillSearchResultItem : SearchResultItem {
        private Skill _skill;

        public Skill Skill {
            get => _skill;
            set {
                if (SetProperty(ref _skill, value)) {
                    if (_skill != null) {
                        Icon = _skill.Icon != null ? Content.GetRenderServiceTexture(_skill.Icon) : (AsyncTexture2D)ContentService.Textures.Error;
                        Name = _skill.Name;
                        Description = _skill.Description;

                        Show();
                    } else {
                        Hide();
                    }
                }
            }
        }

        protected override string ChatLink => Skill?.ChatLink;
    }
}
