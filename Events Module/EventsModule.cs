using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.GameServices.Content;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Events_Module {

    [Export(typeof(Module))]
    public class EventsModule : Module {

        internal static EventsModule ModuleInstance;

        // Service Managers
        internal SettingsManager    SettingsManager    => this.ModuleParameters.SettingsManager;
        internal ContentsManager    ContentsManager    => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager      Gw2ApiManager      => this.ModuleParameters.Gw2ApiManager;

        private const string DD_ALPHABETICAL = "Alphabetical";
        private const string DD_NEXTUP       = "Next Up";

        private const string EC_ALLEVENTS = "All Events";
        private const string EC_HIDDEN    = "Hidden Events";

        private const int TIMER_RECALC_RATE = 5;

        private List<DetailsButton> _displayedEvents;

        private WindowTab _eventsTab;

        private Panel _tabPanel;

        private SettingCollection  _watchCollection;
        private SettingEntry<bool> _settingNotificationsEnabled;

        private Texture2D _textureWatch;
        private Texture2D _textureWatchActive;

        public bool NotificationsEnabled {
            get => _settingNotificationsEnabled.Value;
            set => _settingNotificationsEnabled.Value = value;
        }

        [ImportingConstructor]
        public EventsModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) {
            ModuleInstance = this;
        }

        protected override void DefineSettings(SettingCollection settings) {
            _watchCollection             = settings.AddSubCollection("Watching");
            _settingNotificationsEnabled = settings.DefineSetting("notificationsEnabled", true);
        }

        protected override void Initialize() {
            _displayedEvents = new List<DetailsButton>();
        }

        private void LoadTextures() {
            _textureWatch       = ContentsManager.GetTexture(@"textures\605021.png");
            _textureWatchActive = ContentsManager.GetTexture(@"textures\605019.png");
        }

        protected override async Task LoadAsync() {
            Meta.Load(this.ContentsManager);
            LoadTextures();

            _tabPanel = BuildSettingPanel(GameService.Overlay.BlishHudWindow.ContentRegion);
        }

        protected override void OnModuleLoaded(EventArgs e) {
            _eventsTab = GameService.Overlay.BlishHudWindow.AddTab("Events and Metas", this.ContentsManager.GetTexture(@"textures\1466345.png"), _tabPanel);

            base.OnModuleLoaded(e);
        }

        private Panel BuildSettingPanel(Rectangle panelBounds) {
            var etPanel = new Panel() {
                CanScroll = false,
                Size = panelBounds.Size
            };

            var ddSortMethod = new Dropdown() {
                Location = new Point(etPanel.Right - 150 - Dropdown.Standard.ControlOffset.X, Dropdown.Standard.ControlOffset.Y),
                Width    = 150,
                Parent   = etPanel,
            };

            var notificationToggle = new Checkbox() {
                Text     = "Enable Notifications",
                Checked  = this.NotificationsEnabled,
                Parent   = etPanel
            };

            notificationToggle.Location = new Point(ddSortMethod.Left - notificationToggle.Width - 10, ddSortMethod.Top + 6);

            notificationToggle.CheckedChanged += delegate(object sender, CheckChangedEvent e) { this.NotificationsEnabled = e.Checked; };

            int topOffset = ddSortMethod.Bottom + Panel.MenuStandard.ControlOffset.Y;

            var menuSection = new Panel {
                Title      = "Event Categories",
                ShowBorder = true,
                Size       = Panel.MenuStandard.Size - new Point(0, topOffset + Panel.MenuStandard.ControlOffset.Y),
                Location   = new Point(Panel.MenuStandard.PanelOffset.X, topOffset),
                Parent     = etPanel
            };

            var eventPanel = new FlowPanel() {
                FlowDirection  = ControlFlowDirection.LeftToRight,
                ControlPadding = new Vector2(8, 8),
                Location       = new Point(menuSection.Right + Panel.MenuStandard.ControlOffset.X, menuSection.Top),
                Size           = new Point(ddSortMethod.Right - menuSection.Right - Control.ControlStandard.ControlOffset.X, menuSection.Height),
                CanScroll      = true,
                Parent         = etPanel
            };

            var searchBox = new TextBox() {
                PlaceholderText = "Event Search",
                Width           = menuSection.Width,
                Location        = new Point(ddSortMethod.Top, menuSection.Left),
                Parent          = etPanel
            };

            searchBox.TextChanged += delegate (object sender, EventArgs args) {
                eventPanel.FilterChildren<DetailsButton>(db => db.Text.ToLower().Contains(searchBox.Text.ToLower()));
            };

            foreach (var meta in Meta.Events) {
                var setting = _watchCollection.DefineSetting("watchEvent:" + meta.Name, true);

                meta.IsWatched = setting.Value;

                var es2 = new DetailsButton {
                    Parent           = eventPanel,
                    BasicTooltipText = meta.Category,
                    Text             = meta.Name,
                    IconSize         = DetailsIconSize.Small,
                    ShowVignette     = false,
                    HighlightType    = DetailsHighlightType.LightHighlight,
                    ShowToggleButton = true
                };

                if (meta.Texture.HasTexture) {
                    es2.Icon = meta.Texture;
                }

                var nextTimeLabel = new Label() {
                    Size                = new Point(65, es2.ContentRegion.Height),
                    Text                = meta.NextTime.ToShortTimeString(),
                    BasicTooltipText    = GetTimeDetails(meta),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Middle,
                    Parent              = es2,
                };

                Adhesive.Binding.CreateOneWayBinding(() => nextTimeLabel.Height, () => es2.ContentRegion, (rectangle => rectangle.Height), true);

                if (!string.IsNullOrEmpty(meta.Wiki)) {
                    var glowWikiBttn = new GlowButton {
                        Icon = GameService.Content.GetTexture("102530"),
                        ActiveIcon = GameService.Content.GetTexture("glow-wiki"),
                        BasicTooltipText = "Read about this event on the wiki.",
                        Parent = es2,
                        GlowColor = Color.White * 0.1f
                    };

                    glowWikiBttn.Click += delegate {
                        if (UrlIsValid(meta.Wiki)) {
                            Process.Start(meta.Wiki);
                        }
                    };
                }

                if (!string.IsNullOrEmpty(meta.Waypoint)) {
                    var glowWaypointBttn = new GlowButton {
                        Icon = GameService.Content.GetTexture("waypoint"),
                        ActiveIcon = GameService.Content.GetTexture("glow-waypoint"),
                        BasicTooltipText = $"Nearby waypoint: {meta.Waypoint}",
                        Parent = es2,
                        GlowColor = Color.White * 0.1f
                    };

                    glowWaypointBttn.Click += delegate {
                        ClipboardUtil.WindowsClipboardService.SetTextAsync(meta.Waypoint)
                                     .ContinueWith((clipboardResult) => {
                                           if (clipboardResult.IsFaulted) {
                                               ScreenNotification.ShowNotification("Failed to copy waypoint to clipboard. Try again.", ScreenNotification.NotificationType.Red, duration: 2);
                                           } else {
                                               ScreenNotification.ShowNotification("Copied waypoint to clipboard!", duration: 2);
                                           }
                                       });
                    };
                }

                var toggleFollowBttn = new GlowButton() {
                    Icon             = _textureWatch,
                    ActiveIcon       = _textureWatchActive,
                    BasicTooltipText = "Click to toggle tracking for this event.",
                    ToggleGlow       = true,
                    Checked          = meta.IsWatched,
                    Parent           = es2,
                };

                toggleFollowBttn.Click += delegate {
                    meta.IsWatched = toggleFollowBttn.Checked;
                    setting.Value  = toggleFollowBttn.Checked;
                };

                meta.OnNextRunTimeChanged += delegate {
                    UpdateSort(ddSortMethod, EventArgs.Empty);

                    nextTimeLabel.Text             = meta.NextTime.ToShortTimeString();
                    nextTimeLabel.BasicTooltipText = GetTimeDetails(meta);
                };

                _displayedEvents.Add(es2);
            }

            // Add menu items for each category (and built-in categories)
            var eventCategories = new Menu {
                Size           = menuSection.ContentRegion.Size,
                MenuItemHeight = 40,
                Parent         = menuSection,
                CanSelect      = true
            };

            List<IGrouping<string, Meta>> submetas = Meta.Events.GroupBy(e => e.Category).ToList();

            var evAll = eventCategories.AddMenuItem(EC_ALLEVENTS);
            evAll.Select();
            evAll.Click += delegate {
                eventPanel.FilterChildren<DetailsButton>(db => true);
            };

            foreach (IGrouping<string, Meta> e in submetas) {
                var ev = eventCategories.AddMenuItem(e.Key);
                ev.Click += delegate {
                    eventPanel.FilterChildren<DetailsButton>(db => string.Equals(db.BasicTooltipText, e.Key));
                };
            }

            // TODO: Hidden events/timers to be added later
            //eventCategories.AddMenuItem(EC_HIDDEN);

            // Add dropdown for sorting events
            ddSortMethod.Items.Add(DD_ALPHABETICAL);
            ddSortMethod.Items.Add(DD_NEXTUP);

            ddSortMethod.ValueChanged += delegate (object sender, ValueChangedEventArgs args) {
                switch (args.CurrentValue) {
                    case DD_ALPHABETICAL:
                        eventPanel.SortChildren<DetailsButton>((db1, db2) => string.Compare(db1.Text, db2.Text, StringComparison.CurrentCultureIgnoreCase));
                        break;
                    case DD_NEXTUP:
                        break;
                }
            };

            ddSortMethod.SelectedItem = DD_NEXTUP;
            //UpdateSort(ddSortMethod, EventArgs.Empty);

            return etPanel;
        }

        private void RepositionES() {
            int pos = 0;
            foreach (var es in _displayedEvents) {
                int x = pos % 2;
                int y = pos / 2;

                es.Location = new Point(x * 308, y * 108);

                if (es.Visible) pos++;

                // TODO: Just expose the panel to the module so that we don't have to do it this dumb way:
                ((Panel)es.Parent).VerticalScrollOffset = 0;
                es.Parent.Invalidate();
            }
        }

        private string GetTimeDetails(Meta assignedMeta) {
            var timeUntil = assignedMeta.NextTime - DateTime.Now;

            var msg = new StringBuilder();

            msg.AppendLine("Starts in " +
                           timeUntil.Humanize(maxUnit: Humanizer.Localisation.TimeUnit.Hour,
                                              minUnit: Humanizer.Localisation.TimeUnit.Minute,
                                              precision: 2,
                                              collectionSeparator: null)
                          );

            msg.Append(Environment.NewLine + "Upcoming Event Times:");
            foreach (var utime in assignedMeta.Times.Select(time => time > DateTime.UtcNow ? time.ToLocalTime() : time.ToLocalTime() + 1.Days()).OrderBy(time => time.Ticks).ToList()) {
                msg.Append(Environment.NewLine + utime.ToShortTimeString());
            }

            return msg.ToString();
        }

        private void UpdateSort(object sender, EventArgs e) {
            switch (((Dropdown)sender).SelectedItem) {
                case DD_ALPHABETICAL:

                    //displayedEvents.Sort((e1, e2) => e1.AssignedMeta.Name.CompareTo(e2.AssignedMeta.Name));
                    break;
                case DD_NEXTUP:
                    //displayedEvents.Sort((e1, e2) => e1.AssignedMeta.NextTime.CompareTo(e2.AssignedMeta.NextTime));
                    break;
            }

            RepositionES();
        }

        // Utility
        private static bool UrlIsValid(string source) => Uri.TryCreate(source, UriKind.Absolute, out Uri uriResult) && uriResult.Scheme == Uri.UriSchemeHttps;

        private double _elapsedSeconds = 0;

        protected override void Update(GameTime gameTime) {
            _elapsedSeconds += gameTime.ElapsedGameTime.TotalSeconds;

            if (_elapsedSeconds > TIMER_RECALC_RATE) {
                Meta.UpdateEventSchedules();
                _elapsedSeconds = 0;
            }
        }

        protected override void Unload() {
            ModuleInstance = null;

            GameService.Overlay.BlishHudWindow.RemoveTab(_eventsTab);
            _displayedEvents.ForEach(de => de.Dispose());
            _displayedEvents.Clear();
        }

    }

}
