using System;
using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WikiClientLibrary.Sites;
using Color = Gw2Sharp.WebApi.V2.Models.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Universal_Search_Module.Controls {
    public class SearchResultItem : Control {

        // TODO: Abstract the control out more so that it can represent more than just landmarks

        private const string POI_FILE      = "https://render.guildwars2.com/file/25B230711176AB5728E86F5FC5F0BFAE48B32F6E/97461.png";
        private const string WAYPOINT_FILE = "https://render.guildwars2.com/file/32633AF8ADEA696A1EF56D3AE32D617B10D3AC57/157353.png";
        private const string VISTA_FILE = "https://render.guildwars2.com/file/A2C16AF497BA3A0903A0499FFBAF531477566F10/358415.png";


        private const int ICON_SIZE = 32;
        private const int ICON_PADDING = 2;

        private const int DEFAULT_WIDTH = 100;
        private const int DEFAULT_HEIGHT = ICON_SIZE + ICON_PADDING * 2;

        #region Load Static

        private static Texture2D _textureItemHover;

        static SearchResultItem() {
            _textureItemHover = UniversalSearchModule.ModuleInstance.ContentsManager.GetTexture(@"textures\1234875.png");
        }

        #endregion

        public event EventHandler<EventArgs> Activated;

        private void OnActivated(EventArgs e) {
            Activated?.Invoke(this, e);
        }

        private AsyncTexture2D _icon;
        public AsyncTexture2D Icon {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        private string _name;
        public string Name {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _description;
        public string Description {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private bool _active;
        public bool Active {
            get => _active;
            set {
                if (SetProperty(ref _active, value)) {
                    OnActivated(EventArgs.Empty);
                }
            }
        }

        private ContinentFloorRegionMapPoi _landmark;
        public ContinentFloorRegionMapPoi Landmark
        {
            get => _landmark;
            set
            {
                if (SetProperty(ref _landmark, value))
                {
                    if (_landmark != null)
                    {
                        _icon = GetTextureForLandmarkAsync(_landmark);
                        _name = _landmark.Name;
                        _description = _landmark.ChatLink;
                        this.Show();
                    }
                    else
                    {
                        this.Hide();
                    }
                }
            }
        }

        private OpenSearchResultEntry? _wikiSearchResult;
        public OpenSearchResultEntry? WikiSearchResult
        {
            get => _wikiSearchResult;
            set
            {
                if (SetProperty(ref _wikiSearchResult, value))
                {
                    if (_wikiSearchResult != null)
                    {
                        _icon = UniversalSearchModule.ModuleInstance.ContentsManager.GetTexture(@"textures\wiki.png");
                        _name = _wikiSearchResult.Value.Title;
                        _description = _wikiSearchResult.Value.Description;
                        this.Show();
                    }
                    else
                    {
                        this.Hide();
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

        public SearchResultItem() {
            this.Size = new Point(DEFAULT_WIDTH, DEFAULT_HEIGHT);
        }

        protected override void OnClick(MouseEventArgs e) {
            if (_landmark != null) {
                ClipboardUtil.WindowsClipboardService.SetTextAsync(_landmark.ChatLink)
                    .ContinueWith((clipboardResult) => {
                        if (clipboardResult.IsFaulted) {
                            ScreenNotification.ShowNotification("Failed to copy waypoint to clipboard. Try again.", ScreenNotification.NotificationType.Red, duration: 2);
                        } else {
                            if (UniversalSearchModule.ModuleInstance._settingShowNotificationWhenLandmarkIsCopied.Value) {
                                ScreenNotification.ShowNotification("Copied waypoint to clipboard!", duration: 2);
                            }
                            if (UniversalSearchModule.ModuleInstance._settingHideWindowAfterSelection.Value) {
                                this.Parent.Hide();
                            }
                        }
                    });
            }

            if (_wikiSearchResult != null) {
                System.Diagnostics.Process.Start(_wikiSearchResult.Value.Url);
            }
            base.OnClick(e);
        }

        protected override void OnMouseEntered(MouseEventArgs e) {
            this.Active = true;

            base.OnMouseEntered(e);
        }

        private Rectangle _layoutIconBounds;
        private Rectangle _layoutNameBounds;
        private Rectangle _layoutDescriptionBounds;

        public override void RecalculateLayout() {
            _layoutIconBounds = new Rectangle(ICON_PADDING, ICON_PADDING, ICON_SIZE, ICON_SIZE);

            int iconRight = _layoutIconBounds.Right + ICON_PADDING;

            _layoutNameBounds        = new Rectangle(iconRight, 0,                        _size.X - iconRight, 20);
            _layoutDescriptionBounds = new Rectangle(iconRight, _layoutNameBounds.Bottom, _size.X - iconRight, 16);
        }

        /// <inheritdoc />
        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds) {
            if (_mouseOver) {
                spriteBatch.DrawOnCtrl(this, _textureItemHover, bounds, Microsoft.Xna.Framework.Color.White * 0.5f);
            }

            if (_icon != null) {
                spriteBatch.DrawOnCtrl(this, _icon, _layoutIconBounds);
            }

            spriteBatch.DrawStringOnCtrl(this, _name,        Content.DefaultFont14, _layoutNameBounds,        Microsoft.Xna.Framework.Color.White, false, false, verticalAlignment: VerticalAlignment.Bottom);
            spriteBatch.DrawStringOnCtrl(this, _description, Content.DefaultFont14, _layoutDescriptionBounds, ContentService.Colors.Chardonnay,    false, false, verticalAlignment: VerticalAlignment.Top);
        }

    }
}
