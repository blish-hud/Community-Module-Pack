using Blish_HUD;
using Blish_HUD.Content;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Universal_Search_Module.Controls.SearchResultItems {
    public class TraitSearchResultItem : SearchResultItem {
        private Trait _trait;

        public Trait Trait {
            get => _trait;
            set {
                if (SetProperty(ref _trait, value)) {
                    if (_trait != null) {
                        Icon = _trait.Icon != null ? Content.GetRenderServiceTexture(_trait.Icon) : (AsyncTexture2D)ContentService.Textures.Error;
                        Name = _trait.Name;
                        Description = _trait.Description;

                        Show();
                    } else {
                        Hide();
                    }
                }
            }
        }

        protected override string ChatLink => GenerateChatLink(Trait);

        private static string GenerateChatLink(Trait trait) {
            const byte TRAIT_CHATLINK_TYPE = 0x07;
            
            var result = new List<byte> {
                TRAIT_CHATLINK_TYPE,
            };

            result.AddRange(BitConverter.GetBytes(trait.Id));
            return $"[&{Convert.ToBase64String(result.ToArray())}]";
        }
    }
}
