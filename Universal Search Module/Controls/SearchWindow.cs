using System;
using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Universal_Search_Module.Services.SearchHandler;
using Universal_Search_Module.Controls.SearchResultItems;

namespace Universal_Search_Module.Controls {

    public class SearchWindow : WindowBase2 {

        private const int WINDOW_WIDTH = 512;
        private const int WINDOW_HEIGHT = 178;

        private const int TITLEBAR_HEIGHT = 32;
        private readonly IEnumerable<SearchHandler> _searchHandlers;

        #region Load Static

        private static Texture2D _textureWindowBackground;

        static SearchWindow() {
            _textureWindowBackground = UniversalSearchModule.ModuleInstance.ContentsManager.GetTexture(@"textures\156390.png");
        }

        #endregion

        private SearchResultItem _activeSearchResult;
        public SearchResultItem ActiveSearchResult {
            get => _activeSearchResult;
            set {
                _activeSearchResult = value;

                _results.ForEach(r => r.Active = r == _activeSearchResult);
            }
        }

        private List<SearchResultItem> _results;

        private Tooltip _resultDetails;
        private TextBox _searchbox;
        private LoadingSpinner _spinner;
        private Label _noneLabel;

        private Label _ttDetailsName;
        private Label _ttDetailsInfRes1;

        public SearchWindow(IEnumerable<SearchHandler> searchHandlers) : base() {
            _searchHandlers = searchHandlers;
            BuildWindow();
            BuildContents();
        }

        private void BuildWindow() {
            this.Title = "Chat Code Searchs";

            ConstructWindow(_textureWindowBackground,
                            new Rectangle(0, 0, WINDOW_WIDTH, WINDOW_HEIGHT),
                            new Rectangle(0, TITLEBAR_HEIGHT, WINDOW_WIDTH, WINDOW_HEIGHT - TITLEBAR_HEIGHT));
        }

        private void BuildContents() {
            _searchbox = new TextBox() {
                Size = new Point(_size.X, TextBox.Standard.Size.Y),
                PlaceholderText = "Search",
                Parent = this
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
                Text = "Enter: Copy chat code to clipboard.",
                Font = Content.DefaultFont16,
                Location = new Point(10, _ttDetailsName.Bottom + 5),
                TextColor = Color.White,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = _resultDetails
            };

            var ttDetailsInf1 = new Label() {
                Text = "Closest Waypoint",
                Font = Content.DefaultFont16,
                Location = new Point(10, ttDetailsInfHint1.Bottom + 12),
                Height = 11,
                TextColor = ContentService.Colors.Chardonnay,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = _resultDetails
            };

            _ttDetailsInfRes1 = new Label() {
                Text = " ",
                Font = Content.DefaultFont14,
                Location = new Point(10, ttDetailsInf1.Bottom + 5),
                TextColor = Color.White,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = _resultDetails
            };

            var ttDetailsInfHint2 = new Label() {
                Text = "Shift + Enter: Copy closest waypoint to clipboard.",
                Font = Content.DefaultFont14,
                Location = new Point(10, _ttDetailsInfRes1.Bottom + 5),
                TextColor = Color.White,
                ShadowColor = Color.Black,
                ShowShadow = true,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Visible = false,
                Parent = _resultDetails
            };

            _spinner = new LoadingSpinner() {
                Location = ContentRegion.Size / new Point(2) - new Point(32, 32),
                Visible = false,
                Parent = this
            };

            _noneLabel = new Label() {
                Size = ContentRegion.Size - new Point(0, TextBox.Standard.Size.Y * 2),
                Location = new Point(0, TextBox.Standard.Size.Y),
                HorizontalAlignment = HorizontalAlignment.Center,
                Visible = false,
                Text = "No Results",
                Parent = this
            };

            _results = new List<SearchResultItem>(SearchHandler.MAX_RESULT_COUNT);
            _searchbox.TextChanged += SearchboxOnTextChanged;
        }

        private void AddSearchResultItems(IEnumerable<SearchResultItem> items) {
            int lastResultBottom = _searchbox.Bottom;

            foreach (var searchItem in items) {
                searchItem.Width = _size.X - 4;
                searchItem.Tooltip = _resultDetails;
                searchItem.Parent = this;
                searchItem.Location = new Point(2, lastResultBottom + 3);
                searchItem.Activated += ResultActivated;
                searchItem.MouseEntered += ResultActivated;

                lastResultBottom = searchItem.Bottom;

                _results.Add(searchItem);
            }
        }

        private void SearchboxOnTextChanged(object sender, EventArgs e) {
            _results.ForEach(r => r.Dispose());
            _results.Clear();
            string searchText = _searchbox.Text;

            if (!(searchText.Length >= 2)) {
                _noneLabel.Show();
                return;
            }

            _noneLabel.Hide();
            _spinner.Show();

            var searchHandler = _searchHandlers.Skip(1).First();

            AddSearchResultItems(searchHandler.Search(searchText));

            _spinner.Hide();

            if (!_results.Any()) {
                _noneLabel.Show();
            }
        }

        private void ResultActivated(object sender, EventArgs e) {
            if (sender is SearchResultItem activatedSearchResult && activatedSearchResult.Active) {
                this.ActiveSearchResult = activatedSearchResult;

                _ttDetailsName.Text = activatedSearchResult.Name;
                _ttDetailsInfRes1.Text = "none found";

                _resultDetails.CurrentControl = activatedSearchResult;

                if (!activatedSearchResult.MouseOver) {
                    _resultDetails.Show(activatedSearchResult.AbsoluteBounds.Location + new Point(activatedSearchResult.Width + 5, 0));
                }
            }
        }

        /// <inheritdoc />
        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds) {
            spriteBatch.DrawOnCtrl(this,
                                   _textureWindowBackground,
                                   bounds);

            // Paints exit button
            base.PaintBeforeChildren(spriteBatch, bounds);
        }

    }
}
