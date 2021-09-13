using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Events_Module.Properties;
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

        private string _ddAlphabetical = Resources.Alphabetical;
        private string _ddNextup = Resources.Next_Up;

        private string _ecAllevents = Resources.All_Events;
        private string _ecHidden    = Resources.Hidden_Events;

        private const int TIMER_RECALC_RATE = 5;

        private List<DetailsButton> _displayedEvents;

        private WindowTab _eventsTab;

        private Panel _tabPanel;

        private SettingCollection  _watchCollection;
        private SettingEntry<bool> _settingNotificationsEnabled;
        private SettingEntry<bool> _settingChimeEnabled;

        private Texture2D _textureWatch;
        private Texture2D _textureWatchActive;

        public bool NotificationsEnabled {
            get => _settingNotificationsEnabled.Value;
            set => _settingNotificationsEnabled.Value = value;
        }

        public bool ChimeEnabled {
            get => _settingChimeEnabled.Value;
            set => _settingChimeEnabled.Value = value;
        }

        [ImportingConstructor]
        public EventsModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) {
            ModuleInstance = this;
        }

        protected override void DefineSettings(SettingCollection settings) {
            var selfManagedSettings = settings.AddSubCollection(@"Managed Settings");

            _settingNotificationsEnabled = selfManagedSettings.DefineSetting(@"notificationsEnabled", true);
            _settingChimeEnabled         = selfManagedSettings.DefineSetting(@"chimeEnabled",         true);

            _watchCollection = settings.AddSubCollection(@"Watching");
        }

        protected override void Initialize() {
            _displayedEvents = new List<DetailsButton>();
            GameService.Overlay.UserLocaleChanged += ChangeLocalization;
        }

        private void LoadTextures() {
            _textureWatch       = ContentsManager.GetTexture(@"textures\605021.png");
            _textureWatchActive = ContentsManager.GetTexture(@"textures\605019.png");
        }

        protected override Task LoadAsync() {
            Meta.Load(this.ContentsManager);
            LoadTextures();

            _tabPanel = BuildSettingPanel(GameService.Overlay.BlishHudWindow.ContentRegion);

            return Task.CompletedTask;
        }

        protected override void OnModuleLoaded(EventArgs e) {
            _eventsTab = GameService.Overlay.BlishHudWindow.AddTab(Resources.Events_and_Metas, this.ContentsManager.GetTexture(@"textures\1466345.png"), _tabPanel);

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
                Text     = Resources.Enable_Notifications,
                Checked  = this.NotificationsEnabled,
                Parent   = etPanel
            };

            notificationToggle.Location = new Point(ddSortMethod.Left - notificationToggle.Width - 10, ddSortMethod.Top + 6);

            var chimeToggle = new Checkbox {
                Text    = Resources.Mute_Notifications,
                Checked = !this.ChimeEnabled,
                Parent  = etPanel,
                Top     = notificationToggle.Top,
                Right   = notificationToggle.Left - 10
            };

            notificationToggle.CheckedChanged += delegate (object sender, CheckChangedEvent e) { this.NotificationsEnabled = e.Checked; };
            chimeToggle.CheckedChanged        += delegate (object sender, CheckChangedEvent e) { this.ChimeEnabled         = !e.Checked; };

            int topOffset = ddSortMethod.Bottom + Panel.MenuStandard.ControlOffset.Y;

            var menuSection = new Panel {
                Title      = Resources.Event_Categories,
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
                PlaceholderText = Resources.Event_Search,
                Width           = menuSection.Width,
                Location        = new Point(ddSortMethod.Top, menuSection.Left),
                Parent          = etPanel
            };

            searchBox.TextChanged += delegate (object sender, EventArgs args) {
                eventPanel.FilterChildren<DetailsButton>(db => db.Text.ToLower().Contains(searchBox.Text.ToLower()));
            };

            foreach (var meta in Meta.Events) {
                var setting = _watchCollection.DefineSetting(@"watchEvent:" + meta.Name, true);

                meta.IsWatched = setting.Value;

                var es2 = new DetailsButton {
                    Parent           = eventPanel,
                    BasicTooltipText = Resources.ResourceManager.GetString(meta.Category) ?? meta.Category,
                    Text             = Resources.ResourceManager.GetString(meta.Name) ?? meta.Name,
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
                        Icon = GameService.Content.GetTexture(@"102530"),
                        ActiveIcon = GameService.Content.GetTexture(@"glow-wiki"),
                        BasicTooltipText = Resources.Read_about_this_event_on_the_wiki_,
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
                        Icon = GameService.Content.GetTexture(@"waypoint"),
                        ActiveIcon = GameService.Content.GetTexture(@"glow-waypoint"),
                        BasicTooltipText = string.Format(Resources.Nearby_waypoint___0_, meta.Waypoint),
                        Parent = es2,
                        GlowColor = Color.White * 0.1f
                    };

                    glowWaypointBttn.Click += delegate {
                        ClipboardUtil.WindowsClipboardService.SetTextAsync(meta.Waypoint)
                                     .ContinueWith((clipboardResult) => {
                                           if (clipboardResult.IsFaulted) {
                                               ScreenNotification.ShowNotification(Resources.Failed_to_copy_waypoint_to_clipboard__Try_again_, ScreenNotification.NotificationType.Red, duration: 2);
                                           } else {
                                               ScreenNotification.ShowNotification(Resources.Copied_waypoint_to_clipboard_, duration: 2);
                                           }
                                       });
                    };
                }

                var toggleFollowBttn = new GlowButton() {
                    Icon             = _textureWatch,
                    ActiveIcon       = _textureWatchActive,
                    BasicTooltipText = Resources.Click_to_toggle_tracking_for_this_event_,
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
                    SortEventPanel(ddSortMethod.SelectedItem, ref eventPanel);

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

            var evAll = eventCategories.AddMenuItem(_ecAllevents);
            evAll.Select();
            evAll.Click += delegate {
                eventPanel.FilterChildren<DetailsButton>(db => true);
            };

            foreach (IGrouping<string, Meta> e in submetas) {
                var category = Resources.ResourceManager.GetString(e.Key) ?? e.Key;
                var ev = eventCategories.AddMenuItem(category);
                ev.Click += delegate {
                    eventPanel.FilterChildren<DetailsButton>(db => string.Equals(db.BasicTooltipText, category));
                };
            }

            // TODO: Hidden events/timers to be added later
            //eventCategories.AddMenuItem(EC_HIDDEN);

            // Add dropdown for sorting events
            ddSortMethod.Items.Add(_ddAlphabetical);
            ddSortMethod.Items.Add(_ddNextup);

            ddSortMethod.ValueChanged += delegate (object sender, ValueChangedEventArgs args) {
                SortEventPanel(args.CurrentValue, ref eventPanel);
            };

            ddSortMethod.SelectedItem = _ddNextup;
            UpdateSort(ddSortMethod, EventArgs.Empty);

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

            msg.AppendLine(string.Format(Resources.Starts_in__0_,
                                         timeUntil.Humanize(maxUnit: Humanizer.Localisation.TimeUnit.Hour,
                                                            minUnit: Humanizer.Localisation.TimeUnit.Minute,
                                                            precision: 2,
                                                            collectionSeparator: null))
            );

            msg.Append(Environment.NewLine + Resources.Upcoming_Event_Times_);
            foreach (var utime in assignedMeta.Times.Select(time => time > DateTime.UtcNow ? time.ToLocalTime() : time.ToLocalTime() + 1.Days()).OrderBy(time => time.Ticks).ToList()) {
                msg.Append(Environment.NewLine + utime.ToShortTimeString());
            }

            return msg.ToString();
        }

        private void UpdateSort(object sender, EventArgs e) {
            var item = ((Dropdown)sender).SelectedItem;
            if (item == _ddAlphabetical) {
                _displayedEvents.Sort((e1, e2) => string.Compare(e1.Text, e2.Text, StringComparison.CurrentCultureIgnoreCase));
            } else if (item == _ddNextup) {
                var orderedEvents = GetOrderedNextUpEventNames();
                _displayedEvents.Sort((db1, db2) => {
                    return orderedEvents.IndexOf(db1.Text) - orderedEvents.IndexOf(db2.Text);
                });
            }

            RepositionES();
        }

        private void SortEventPanel(string ddSortMethodValue, ref FlowPanel eventPanel) {
            if (ddSortMethodValue == _ddAlphabetical) {
                eventPanel.SortChildren<DetailsButton>((db1, db2) => string.Compare(db1.Text, db2.Text, StringComparison.CurrentCultureIgnoreCase));
            } else if (ddSortMethodValue == _ddNextup) {
                var orderedEvents = GetOrderedNextUpEventNames();
                eventPanel.SortChildren<DetailsButton>((db1, db2) => {
                    return orderedEvents.IndexOf(db1.Text) - orderedEvents.IndexOf(db2.Text);
                });
            }
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

            GameService.Overlay.UserLocaleChanged -= ChangeLocalization;
            GameService.Overlay.BlishHudWindow.RemoveTab(_eventsTab);
            _displayedEvents.ForEach(de => de.Dispose());
            _displayedEvents.Clear();
        }

        private IList<string> GetOrderedNextUpEventNames() {
            return Meta.Events.OrderBy(el => el.NextTime)
                       .Select(el => Resources.ResourceManager.GetString(el.Name) ?? el.Name)
                       .ToList();
        }

        private void ChangeLocalization(object sender, EventArgs e) {
            _ddAlphabetical = Resources.Alphabetical;
            _ddNextup = Resources.Next_Up;
            _ecAllevents = Resources.All_Events;
            _ecHidden = Resources.Hidden_Events;

            //TODO: Implement as View so panel reloads automatically.
            if (_tabPanel != null) {
                _tabPanel?.Dispose();
                _tabPanel = BuildSettingPanel(GameService.Overlay.BlishHudWindow.ContentRegion);

                if (_eventsTab != null)
                    GameService.Overlay.BlishHudWindow.RemoveTab(_eventsTab);

                _eventsTab = GameService.Overlay.BlishHudWindow.AddTab(Resources.Events_and_Metas, this.ContentsManager.GetTexture(@"textures\1466345.png"), _tabPanel);
            }
        }

    }

}
