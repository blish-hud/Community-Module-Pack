using Blish_HUD;
using Blish_HUD.Content;
using Gw2Sharp.WebApi.V2.Models;

namespace Universal_Search_Module.Controls.SearchResultItems {
    public class LandmarkSearchResultItem : SearchResultItem {
        private const string POI_FILE = "https://render.guildwars2.com/file/25B230711176AB5728E86F5FC5F0BFAE48B32F6E/97461.png";
        private const string WAYPOINT_FILE = "https://render.guildwars2.com/file/32633AF8ADEA696A1EF56D3AE32D617B10D3AC57/157353.png";
        private const string VISTA_FILE = "https://render.guildwars2.com/file/A2C16AF497BA3A0903A0499FFBAF531477566F10/358415.png";

        protected override string ChatLink => Landmark?.ChatLink;

        private ContinentFloorRegionMapPoi _landmark;
        public ContinentFloorRegionMapPoi Landmark {
            get => _landmark;
            set {
                if (SetProperty(ref _landmark, value)) {
                    if (_landmark != null) {
                        Icon = GetTextureForLandmarkAsync(_landmark);
                        Name = _landmark.Name;
                        Description = _landmark.ChatLink;

                        Show();
                    } else {
                        Hide();
                    }
                }
            }
        }


        private AsyncTexture2D GetTextureForLandmarkAsync(ContinentFloorRegionMapPoi landmark) {
            string imgUrl = string.Empty;

            switch (landmark.Type.Value) {
                case PoiType.Landmark:
                    imgUrl = POI_FILE;
                    break;
                case PoiType.Waypoint:
                    imgUrl = WAYPOINT_FILE;
                    break;
                case PoiType.Vista:
                    imgUrl = VISTA_FILE;
                    break;
                case PoiType.Unknown:
                case PoiType.Unlock:
                    if (!string.IsNullOrEmpty(landmark.Icon)) {
                        imgUrl = landmark.Icon;
                    } else {
                        return ContentService.Textures.Error;
                    }
                    break;
            }

            return Content.GetRenderServiceTexture(imgUrl);
        }
    }
}
