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
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Pathing.Behaviors;
using Blish_HUD.Settings;
using Blish_HUD.Utils;
using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Events_Module {

    [Export(typeof(Module))]
    public class Events_Module : Module {

        private const string DD_ALPHABETICAL = "Alphabetical";
        private const string DD_NEXTUP       = "Next Up";

        private const string EC_ALLEVENTS = "All Events";
        private const string EC_HIDDEN    = "Hidden Events";

        private List<DetailsButton> _displayedEvents;

        private WindowTab _eventsTab;

        private Panel _tabPanel;

        private SettingCollection _watchCollection;

        [ImportingConstructor]
        public Events_Module([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { /* NOOP */ }

        protected override void DefineSettings(SettingCollection settings) {
            _watchCollection = settings.AddSubCollection("Watching");
        }

        protected override void Initialize() {
            _displayedEvents = new List<DetailsButton>();
        }

        protected override async Task LoadAsync() {
            Meta.Load(this.ContentsManager);

            _tabPanel = BuildSettingPanel(GameService.Overlay.BlishHudWindow.ContentRegion);
        }

        protected override void OnModuleLoaded(EventArgs e) {
            GameService.Overlay.BlishHudWindow.AddTab("Events and Metas", this.ContentsManager.GetTexture(@"textures\1466345.png"), _tabPanel);

            base.OnModuleLoaded(e);
        }

        private Panel BuildSettingPanel(Rectangle panelBounds) {
            var etPanel = new Panel() {
                CanScroll = false,
                Size = panelBounds.Size
            };

            var eventPanel = new FlowPanel() {
                FlowDirection = ControlFlowDirection.LeftToRight,
                ControlPadding = new Vector2(8, 8),
                Location = new Point(etPanel.Width - 720 - 10 - 20, 50),
                Size = new Point(748, etPanel.Size.Y - 50 - Panel.BOTTOM_MARGIN),
                Parent = etPanel,
                CanScroll = true,
            };

            var ddSortMethod = new Dropdown() {
                Parent = etPanel,
                Location = new Point(etPanel.Right - 150 - 10, 5),
                Width = 150
            };

            foreach (var meta in Meta.Events) {
                var setting = _watchCollection.DefineSetting("watchEvent:" + meta.Name, true);

                meta.IsWatched = setting.Value;

                var es2 = new DetailsButton {
                    Parent = eventPanel,
                    BasicTooltipText = meta.Category,
                    Text = meta.Name,
                    IconSize = DetailsIconSize.Small,
                    Icon = string.IsNullOrEmpty(meta.Icon) ? null : GameService.Content.GetTexture(meta.Icon),
                    ShowVignette = false,
                    HighlightType = DetailsHighlightType.LightHighlight,
                    ShowToggleButton = true
                };

                var nextTimeLabel = new Label() {
                    Size = new Point(65, es2.ContentRegion.Height),
                    Text = meta.NextTime.ToShortTimeString(),
                    BasicTooltipText = GetTimeDetails(meta),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Middle,
                    Parent = es2,
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
                        System.Windows.Forms.Clipboard.SetText(meta.Waypoint);

                        ScreenNotification.ShowNotification("Waypoint copied to clipboard.");
                    };
                }

                var toggleFollowBttn = new GlowButton() {
                    Icon = GameService.Content.GetTexture("605021"),
                    ActiveIcon = GameService.Content.GetTexture("605019"),
                    BasicTooltipText = "Click to toggle tracking for this event.",
                    ToggleGlow = true,
                    Checked = meta.IsWatched,
                    Parent = es2,
                };

                toggleFollowBttn.Click += delegate {
                    meta.IsWatched = toggleFollowBttn.Checked;
                    setting.Value = toggleFollowBttn.Checked;
                };

                meta.OnNextRunTimeChanged += delegate {
                    UpdateSort(ddSortMethod, EventArgs.Empty);

                    nextTimeLabel.Text = meta.NextTime.ToShortTimeString();
                    nextTimeLabel.BasicTooltipText = GetTimeDetails(meta);
                };

                _displayedEvents.Add(es2);
            }

            var menuSection = new Panel {
                ShowBorder = true,
                Size = new Point(etPanel.Width - 720 - 10 - 10 - 5 - 20, eventPanel.Height + Panel.BOTTOM_MARGIN),
                Location = new Point(5, 50),
                Parent = etPanel,
                Title = "Event Categories"
            };

            // Add menu items for each category (and built-in categories)
            var eventCategories = new Menu {
                Size = menuSection.ContentRegion.Size,
                MenuItemHeight = 40,
                Parent = menuSection,
                CanSelect = true
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

            //Console.WriteLine("Main Panel is: " + etPanel.Location.ToString() + " :: " + etPanel.Size.ToString());
            //Console.WriteLine("Event Panel is: " + eventPanel.Location.ToString() + " :: " + eventPanel.Size.ToString());
            //Console.WriteLine("Menu Section Panel is: " + menuSection.Location.ToString() + " :: " + eventPanel.Size.ToString());

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
        public static bool UrlIsValid(string source) => Uri.TryCreate(source, UriKind.Absolute, out Uri uriResult) && uriResult.Scheme == Uri.UriSchemeHttps;

        protected override void Update(GameTime gameTime) {
            Meta.UpdateEventSchedules();
        }

        protected override void Unload() {
            _displayedEvents.ForEach(de => de.Dispose());
            _displayedEvents.Clear();
        }

    }

}
