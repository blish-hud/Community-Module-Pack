using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework.Graphics;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Universal_Search_Module.Controls {
    public class SearchWindow : WindowBase {

        private const int WINDOW_WIDTH  = 256;
        private const int WINDOW_HEIGHT = 178;

        private const int TITLEBAR_HEIGHT = 32;

        private const int MAX_RESULT_COUNT = 3;

        #region Load Static

        private static Texture2D _textureWindowBackground;

        static SearchWindow() {
            _textureWindowBackground = UniversalSearchModule.ModuleInstance.ContentsManager.GetTexture(@"textures\156390.png");
        }

        #endregion

        public struct WordScoreResult {

            public ContinentFloorRegionMapPoi Landmark  { get; set; }
            public int                        DiffScore { get; set; }

            public WordScoreResult(ContinentFloorRegionMapPoi landmark, int diffScore) {
                this.Landmark  = landmark;
                this.DiffScore = diffScore;
            }

        }

        private SearchResultItem _activeSearchResult;
        public SearchResultItem ActiveSearchResult {
            get => _activeSearchResult;
            set {
                _activeSearchResult = value;

                _results.ForEach(r => r.Active = r == _activeSearchResult);
            }
        }

        private List<SearchResultItem> _results;

        private Tooltip        _resultDetails;
        private TextBox        _searchbox;
        private LoadingSpinner _spinner;
        private Label          _noneLabel;

        private Label _ttDetailsName;
        private Label _ttDetailsInfRes1;

        public SearchWindow() : base() {
            BuildWindow();
            BuildContents();
        }

        private void BuildWindow() {
            this.Title  = "Landmark Search";
            this.ZIndex = Screen.TOOLWINDOW_BASEZINDEX;

            ConstructWindow(_textureWindowBackground,
                            new Vector2(0, TITLEBAR_HEIGHT),
                            new Rectangle(0, 0, WINDOW_WIDTH, WINDOW_HEIGHT),
                            Thickness.Zero,
                            TITLEBAR_HEIGHT,
                            false);

            this.ContentRegion = new Rectangle(0, TITLEBAR_HEIGHT, _size.X, _size.Y - TITLEBAR_HEIGHT);
        }

        private void BuildContents() {
            _searchbox = new TextBox() {
                Size            = new Point(_size.X, TextBox.Standard.Size.Y),
                PlaceholderText = "Search",
                Parent          = this
            };

            _resultDetails = new Tooltip();

            _ttDetailsName = new Label() {
                Text = "Name Loading...",
                Font = Content.DefaultFont16,
                Location = new Point(10, 10),
                Height = 11,
                TextColor = ContentService.Colors.Chardonnay,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                VerticalAlignment = VerticalAlignment.Middle,
                Parent = _resultDetails
            };

            var ttDetailsInfHint1 = new Label() {
                Text           = "Enter: Copy landmark to clipboard.",
                Font           = Content.DefaultFont16,
                Location       = new Point(10, _ttDetailsName.Bottom + 5),
                TextColor      = Color.White,
                ShowShadow     = true,
                AutoSizeWidth  = true,
                AutoSizeHeight = true,
                Parent         = _resultDetails
            };

            var ttDetailsInf1 = new Label() {
                Text           = "Closest Waypoint",
                Font           = Content.DefaultFont16,
                Location       = new Point(10, ttDetailsInfHint1.Bottom + 12),
                Height         = 11,
                TextColor      = ContentService.Colors.Chardonnay,
                ShadowColor    = Color.Black,
                ShowShadow     = true,
                AutoSizeWidth  = true,
                AutoSizeHeight = true,
                Parent         = _resultDetails
            };

            _ttDetailsInfRes1 = new Label() {
                Text           = " ",
                Font           = Content.DefaultFont14,
                Location       = new Point(10, ttDetailsInf1.Bottom + 5),
                TextColor      = Color.White,
                ShadowColor    = Color.Black,
                ShowShadow     = true,
                AutoSizeWidth  = true,
                AutoSizeHeight = true,
                Parent         = _resultDetails
            };

            var ttDetailsInfHint2 = new Label() {
                Text           = "Shift + Enter: Copy closest waypoint to clipboard.",
                Font           = Content.DefaultFont14,
                Location       = new Point(10, _ttDetailsInfRes1.Bottom + 5),
                TextColor      = Color.White,
                ShadowColor    = Color.Black,
                ShowShadow     = true,
                AutoSizeWidth  = true,
                AutoSizeHeight = true,
                Visible        = false,
                Parent         = _resultDetails
            };

            _spinner = new LoadingSpinner() {
                Location = ContentRegion.Size / new Point(2) - new Point(32, 32),
                Visible  = false,
                Parent   = this
            };

            _noneLabel = new Label() {
                Size                = ContentRegion.Size - new Point(0, TextBox.Standard.Size.Y * 2),
                Location            = new Point(0, TextBox.Standard.Size.Y),
                HorizontalAlignment = HorizontalAlignment.Center,
                Visible             = false,
                Text                = "No Results",
                Parent              = this
            };

            _results = new List<SearchResultItem>(MAX_RESULT_COUNT);

            int lastResultBottom = _searchbox.Bottom;

            for (int i = 0; i < MAX_RESULT_COUNT; i++) {
                var sri = BuildSearchResultItem(i);

                sri.Location = new Point(2, lastResultBottom + 3);
                sri.Activated    += ResultActivated;
                sri.MouseEntered += ResultActivated;

                lastResultBottom = sri.Bottom;

                _results.Add(sri);
            }

            _searchbox.TextChanged += SearchboxOnTextChanged;
        }

        private async void SearchboxOnTextChanged(object sender, EventArgs e) {
            await Task.Yield();

            _results.ForEach(r => r.Landmark = null);

            string searchText = _searchbox.Text;

            if (!(searchText.Length >= 2)) {
                _noneLabel.Show();
                return;
            }

            _noneLabel.Hide();
            _spinner.Show();

            var landmarkDiffs = new List<WordScoreResult>();

            foreach (var landmark in UniversalSearchModule.ModuleInstance.LoadedLandmarks) {
                int score;

                if (landmark.Name.StartsWith(searchText, StringComparison.CurrentCultureIgnoreCase)) {
                    score = 0;
                } else if (landmark.Name.EndsWith(searchText, StringComparison.CurrentCultureIgnoreCase)) {
                    score = 3;
                } else {
                    score = StringUtil.ComputeLevenshteinDistance(searchText.ToLower(), landmark.Name.Substring(0, Math.Min(searchText.Length, landmark.Name.Length)).ToLower());
                }

                landmarkDiffs.Add(new WordScoreResult(landmark, score));
            }

            var possibleLandmarks = landmarkDiffs.OrderBy(x => x.DiffScore).Take(MAX_RESULT_COUNT).ToList();

            _spinner.Hide();

            if (possibleLandmarks.Any()) {
                for (int i = 0; i < Math.Max(MAX_RESULT_COUNT, possibleLandmarks.Count); i++) {
                    _results[i].Landmark = possibleLandmarks[i].Landmark;
                }
            } else {
                _noneLabel.Show();
            }
        }

        private SearchResultItem BuildSearchResultItem(int index) {
            return new SearchResultItem() {
                Width   = _size.X - 4,
                Tooltip = _resultDetails,
                Visible = false,
                Parent  = this
            };
        }

        private void ResultActivated(object sender, EventArgs e) {
            if (sender is SearchResultItem activatedSearchResult && activatedSearchResult.Active) {
                this.ActiveSearchResult = activatedSearchResult;

                _ttDetailsName.Text    = activatedSearchResult.Name;
                _ttDetailsInfRes1.Text = "none found";

                _resultDetails.CurrentControl = activatedSearchResult;

                if (!activatedSearchResult.MouseOver) {
                    _resultDetails.Show(activatedSearchResult.AbsoluteBounds.Location + new Point(activatedSearchResult.Width + 5, 0));
                }
            }
        }

        private Rectangle _layoutTitleTextBounds;

        /// <inheritdoc />
        public override void RecalculateLayout() {
            _layoutTitleTextBounds = new Rectangle(8, 0, 32, TITLEBAR_HEIGHT);

            base.RecalculateLayout();
        }

        /// <inheritdoc />
        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds) {
            spriteBatch.DrawOnCtrl(this,
                                   _textureWindowBackground,
                                   bounds);

            // Paints exit button
            base.PaintBeforeChildren(spriteBatch, bounds);

            spriteBatch.DrawStringOnCtrl(this,
                                         "Landmark Search",
                                         Content.DefaultFont14,
                                         _layoutTitleTextBounds,
                                         Color.White);
        }

    }
}
