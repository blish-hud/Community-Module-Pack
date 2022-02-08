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
using System.Threading.Tasks;
using System.Threading;

namespace Universal_Search_Module.Controls {

    public class SearchWindow : WindowBase2 {

        private const int WINDOW_WIDTH = 512;
        private const int WINDOW_HEIGHT = 178;
        private const int TITLEBAR_HEIGHT = 32;
        private const int DROPDOWN_WIDTH = 100;

        private readonly IDictionary<string, SearchHandler> _searchHandlers;

        #region Load Static

        private static Texture2D _textureWindowBackground;

        static SearchWindow() {
            _textureWindowBackground = UniversalSearchModule.ModuleInstance.ContentsManager.GetTexture(@"textures\156390.png");
        }

        #endregion

        private List<SearchResultItem> _results;
        private SearchHandler _selectedSearchHandler;

        private TextBox _searchbox;
        private LoadingSpinner _spinner;
        private Label _noneLabel;
        private Dropdown _searchHandlerSelect;

        private Task _delayTask;
        private CancellationTokenSource _delayCancellationToken;
        private readonly SemaphoreSlim _searchSemaphore = new SemaphoreSlim(1, 1);


        public SearchWindow(IEnumerable<SearchHandler> searchHandlers) : base() {
            _searchHandlers = searchHandlers.ToDictionary(x => x.Name, y => y);
            _selectedSearchHandler = _searchHandlers.First().Value;
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
            _searchHandlerSelect = new Dropdown() {
                Size = new Point(DROPDOWN_WIDTH, Dropdown.Standard.Size.Y),
                SelectedItem = _selectedSearchHandler.Name,
                Parent = this,
            };

            foreach (var searchHandler in _searchHandlers) {
                _searchHandlerSelect.Items.Add(searchHandler.Key);
            }

            _searchHandlerSelect.ValueChanged += SearchHandlerSelectValueChanged;

            _searchbox = new TextBox() {
                Location = new Point(DROPDOWN_WIDTH, 0),
                Size = new Point(_size.X - DROPDOWN_WIDTH, TextBox.Standard.Size.Y),
                PlaceholderText = "Search",
                Parent = this
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

        private void SearchHandlerSelectValueChanged(object sender, ValueChangedEventArgs e) {
            _selectedSearchHandler = _searchHandlers[e.CurrentValue];
            Search();

        }

        private void AddSearchResultItems(IEnumerable<SearchResultItem> items) {
            int lastResultBottom = _searchbox.Bottom;

            foreach (var searchItem in items) {
                searchItem.Width = _size.X - 4;
                searchItem.Parent = this;
                searchItem.Location = new Point(2, lastResultBottom + 3);

                lastResultBottom = searchItem.Bottom;

                _results.Add(searchItem);
            }
        }

        private bool HandlePrefix(string searchText) {
            const int MAX_PREFIX_LENGTH = 2;

            if (searchText.Length > 1 && searchText.Length <= MAX_PREFIX_LENGTH && searchText.EndsWith(" ")) {
                searchText = searchText.Replace(" ", string.Empty);
                foreach (var possibleSearchHandler in _searchHandlers) {
                    if (possibleSearchHandler.Value.Prefix.Equals(searchText, StringComparison.OrdinalIgnoreCase)) {

                        // Temporarily remove event handler to prevent another search on combox change
                        _searchHandlerSelect.ValueChanged -= SearchHandlerSelectValueChanged;
                        _searchHandlerSelect.SelectedItem = possibleSearchHandler.Value.Name;
                        _selectedSearchHandler = possibleSearchHandler.Value;
                        _searchHandlerSelect.ValueChanged += SearchHandlerSelectValueChanged;

                        _searchbox.Text = string.Empty;
                        return false;
                    }
                }
            }

            return true;
        }

        private async Task SearchAsync(CancellationToken cancellationToken = default) {
            await _searchSemaphore.WaitAsync(cancellationToken);
            try {
                _results.ForEach(r => r.Dispose());
                _results.Clear();

                cancellationToken.ThrowIfCancellationRequested();

                string searchText = _searchbox.Text;

                if (!HandlePrefix(searchText) || searchText.Length <= 2) {
                    _noneLabel.Show();
                    return;
                }

                _noneLabel.Hide();
                _spinner.Show();

                AddSearchResultItems(_selectedSearchHandler.Search(searchText));

                _spinner.Hide();

                if (!_results.Any()) {
                    _noneLabel.Show();
                }
            } finally {
                _searchSemaphore.Release();
            }
        }

        private void SearchboxOnTextChanged(object sender, EventArgs e) {
            Search();
        }

        private void Search() {
            try {
                if (!HandlePrefix(_searchbox.Text)) {
                    return;
                }

                if (_delayTask != null) {
                    _delayCancellationToken.Cancel();
                    _delayTask = null;
                    _delayCancellationToken = null;
                }

                _delayCancellationToken = new CancellationTokenSource();
                _delayTask = new Task(async () => await DelaySeach(_delayCancellationToken.Token), _delayCancellationToken.Token);
                _delayTask.Start();
            } catch (OperationCanceledException) {
            }
        }

        private async Task DelaySeach(CancellationToken cancellationToken) {
            try {
                await Task.Delay(300, cancellationToken);
                await SearchAsync(cancellationToken);
            } catch (OperationCanceledException) { }
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
