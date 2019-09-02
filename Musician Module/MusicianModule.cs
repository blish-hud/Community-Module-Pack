using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Blish_HUD.Modules.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Musician_Module.Controls;
using Musician_Module.Notation.Persistance;
using Musician_Module.Player;
using Musician_Module.Controls.Instrument;

namespace Musician_Module
{

    [Export(typeof(Module))]
    public class MusicianModule : Module
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(MusicianModule));

        internal static MusicianModule ModuleInstance;

        // Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;

        private Texture2D ICON;
        private const int TOP_MARGIN = 0;
        private const int RIGHT_MARGIN = 5;
        private const int BOTTOM_MARGIN = 10;
        private const int LEFT_MARGIN = 8;
        private const string DD_TITLE = "Title";
        private const string DD_ARTIST = "Artist";
        private const string DD_USER = "User";
        private const string DD_HARP = "Harp";
        private const string DD_FLUTE = "Flute";
        private const string DD_LUTE = "Lute";
        private const string DD_HORN = "Horn";
        private const string DD_BASS = "Bass";
        private const string DD_BELL = "Bell";
        private const string DD_BELL2 = "Bell2";
        private readonly List<string> Instruments = new List<string>{
           "Harp", "Flute", "Lute", "Horn", "Bell", "Bell2", "Bass"
        };

        private WindowTab MusicianTab;
        public MusicPlayer MusicPlayer;
        private HealthPoolButton StopButton;
        private XmlMusicSheetReader xmlParser;
        private List<SheetButton> displayedSheets;
        private List<RawMusicSheet> Sheets;

        public Conveyor Conveyor { get; private set; }

        /// <summary>
        /// Ideally you should keep the constructor as is.
        /// Use <see cref="Initialize"/> to handle initializing the module.
        /// </summary>
        [ImportingConstructor]
        public MusicianModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
        {
            ModuleInstance = this;
        }

        #region Settings

        private SettingEntry<bool> settingBackgroundPlayback;

        protected override void DefineSettings(SettingCollection settingsManager)
        {
            settingBackgroundPlayback = settingsManager.DefineSetting<bool>("backgroundPlayback", false, "No background playback", "Stop key emulation when GW2 is in the background");
        }

        #endregion

        protected override void Initialize()
        {
            ICON = ICON ?? ContentsManager.GetTexture("musician_icon.png");
            Conveyor = new Conveyor() { Parent = ContentService.Graphics.SpriteScreen, Visible = false };
            xmlParser = new XmlMusicSheetReader();
            displayedSheets = new List<SheetButton>();
            StopButton = new HealthPoolButton()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Text = "Stop Playback",
                ZIndex = -1,
                Visible = false
            };
            StopButton.LeftMouseButtonReleased += delegate
            {
                this.StopPlayback();
            };
            GameService.GameIntegration.Gw2LostFocus += GameIntegrationOnGw2LostFocus;
        }

        protected override async Task LoadAsync()
        {
            // Load local sheet music (*.xml) files.
            await Task.Run(() => Sheets = xmlParser.LoadDirectory(DirectoriesManager.GetFullDirectoryPath("musician")));
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            MusicianTab = GameService.Overlay.BlishHudWindow.AddTab("Musician", ICON, BuildHomePanel(GameService.Overlay.BlishHudWindow), 0);
            base.OnModuleLoaded(e);
        }

        private void GameIntegrationOnGw2LostFocus(object sender, EventArgs e) {
            if (settingBackgroundPlayback.Value) {
                this.StopPlayback();
            }
        }

        protected override void Unload()
        {
            this.StopButton.Dispose();
            this.StopButton = null;
            this.Conveyor.Dispose();
            this.Conveyor = null;
            this.StopPlayback();
            GameService.Overlay.BlishHudWindow.RemoveTab(MusicianTab);
            ModuleInstance = null;
        }

        private void StopPlayback()
        {
            if (Conveyor != null)
            {
                Conveyor.Visible = false;
            }
            if (StopButton != null)
            {
                StopButton.Visible = false;
            }
            if (MusicPlayer != null)
            {
                MusicPlayer.Dispose();
                MusicPlayer = null;
            }
            foreach (SheetButton sheetButton in displayedSheets)
            {
                sheetButton.IsPreviewing = false;
            }
        }

        /*######################################
          # PANEL RELATED STUFF BELOW.
          ######################################*/
        private Panel BuildHomePanel(WindowBase wndw)
        {
            var hPanel = new Panel()
            {
                CanScroll = false,
                Size = wndw.ContentRegion.Size
            };

            var contentPanel = new Panel()
            {
                Location = new Point(hPanel.Width - 630, 50),
                Size = new Point(630, hPanel.Size.Y - 50 - MusicianModule.BOTTOM_MARGIN),
                Parent = hPanel,
                CanScroll = true
            };
            var menuSection = new Panel
            {
                ShowBorder = true,
                //Title = "Musician Panel",
                Size = new Point(hPanel.Width - contentPanel.Width - 10, contentPanel.Height + MusicianModule.BOTTOM_MARGIN),
                Location = new Point(MusicianModule.LEFT_MARGIN, 20),
                Parent = hPanel,
            };
            var musicianCategories = new Menu
            {
                Size = menuSection.ContentRegion.Size,
                MenuItemHeight = 40,
                Parent = menuSection
            };
            var lPanel = BuildLibraryPanel(wndw);
            var library = musicianCategories.AddMenuItem("Library");
            library.LeftMouseButtonReleased += delegate { wndw.Navigate(lPanel); };
            var cPanel = BuildComposerPanel(wndw);
            var composer = musicianCategories.AddMenuItem("Composer");
            composer.LeftMouseButtonReleased += delegate { wndw.Navigate(cPanel); };

            return hPanel;
        }
        private Panel BuildLibraryPanel(WindowBase wndw)
        {
            var lPanel = new Panel()
            {
                CanScroll = false,
                Size = wndw.ContentRegion.Size
            };
            var backButton = new BackButton(wndw)
            {
                Text = "Musician",
                NavTitle = "Library",
                Parent = lPanel,
                Location = new Point(20, 20),
            };
            var melodyPanel = new Panel()
            {
                Location = new Point(0, MusicianModule.BOTTOM_MARGIN + backButton.Bottom),
                Size = new Point(lPanel.Width, lPanel.Size.Y - 50 - MusicianModule.BOTTOM_MARGIN),
                Parent = lPanel,
                ShowTint = true,
                ShowBorder = true,
                CanScroll = true
            };

            // TODO: Load a list from online database.
            foreach (RawMusicSheet sheet in Sheets)
            {
                var melody = new SheetButton
                {
                    Parent = melodyPanel,
                    Icon = ContentsManager.GetTexture(@"instruments\" + sheet.Instrument.ToLowerInvariant() + ".png"),
                    IconSize = DetailsIconSize.Small,
                    Artist = sheet.Artist,
                    Title = sheet.Title,
                    User = sheet.User,
                    MusicSheet = sheet
                };
                displayedSheets.Add(melody);
                melody.LeftMouseButtonPressed += delegate
                { 
                    if (melody.MouseOverPlay)
                    {
                        this.StopPlayback();
                        GameService.Overlay.BlishHudWindow.Hide();
                        MusicPlayer = MusicPlayerFactory.Create(
                            melody.MusicSheet,
                            InstrumentMode.Preview
                        );
                        MusicPlayer.Worker.Start();
                        Conveyor.Visible = true;
                        StopButton.Visible = true;
                    }
                    if (melody.MouseOverEmulate)
                    {
                        this.StopPlayback();
                        GameService.Overlay.BlishHudWindow.Hide();
                        MusicPlayer = MusicPlayerFactory.Create(
                            melody.MusicSheet,
                            InstrumentMode.Emulate
                        );
                        MusicPlayer.Worker.Start();
                        StopButton.Visible = true;
                    }
                    if (melody.MouseOverPreview)
                    {
                        if (melody.IsPreviewing)
                        {
                            this.StopPlayback();
                        }
                        else
                        {
                            this.StopPlayback();
                            melody.IsPreviewing = true;
                            MusicPlayer = MusicPlayerFactory.Create(
                                melody.MusicSheet,
                                InstrumentMode.Preview
                            );
                            MusicPlayer.Worker.Start();
                        }
                    }
                };
            }
            var ddSortMethod = new Dropdown()
            {
                Parent = lPanel,
                Visible = melodyPanel.Visible,
                Location = new Point(lPanel.Right - 150 - 10, 5),
                Width = 150
            };
            ddSortMethod.Items.Add(DD_TITLE);
            ddSortMethod.Items.Add(DD_ARTIST);
            ddSortMethod.Items.Add(DD_USER);
            ddSortMethod.Items.Add("------------------");
            ddSortMethod.Items.Add(DD_HARP);
            ddSortMethod.Items.Add(DD_FLUTE);
            ddSortMethod.Items.Add(DD_LUTE);
            ddSortMethod.Items.Add(DD_HORN);
            ddSortMethod.Items.Add(DD_BASS);
            ddSortMethod.Items.Add(DD_BELL);
            ddSortMethod.Items.Add(DD_BELL2);
            ddSortMethod.ValueChanged += UpdateSort;
            ddSortMethod.SelectedItem = DD_TITLE;

            UpdateSort(ddSortMethod, EventArgs.Empty);
            backButton.LeftMouseButtonReleased += (object sender, MouseEventArgs e) => { wndw.NavigateHome(); };

            return lPanel;
        }
        private Panel BuildComposerPanel(WindowBase wndw)
        {
            var cPanel = new Panel()
            {
                CanScroll = false,
                Size = wndw.ContentRegion.Size
            };
            var backButton = new BackButton(wndw)
            {
                Text = "Musician",
                NavTitle = "Composer",
                Parent = cPanel,
                Location = new Point(20, 20),
            };
            var composerPanel = new Panel()
            {
                Location = new Point(MusicianModule.LEFT_MARGIN + 20, MusicianModule.BOTTOM_MARGIN + backButton.Bottom),
                Size = new Point(cPanel.Size.X - 50 - MusicianModule.LEFT_MARGIN, cPanel.Size.Y - 50 - MusicianModule.BOTTOM_MARGIN),
                Parent = cPanel,
                CanScroll = false
            };
            var titleTextBox = new TextBox
            {
                Size = new Point(150, 20),
                Location = new Point(0, 20),
                PlaceholderText = "Title",
                Parent = composerPanel
            };
            var titleArtistLabel = new Label
            {
                Size = new Point(20, 20),
                Location = new Point(titleTextBox.Left + titleTextBox.Width + 20, titleTextBox.Top),
                Text = " - ",
                Parent = composerPanel
            };
            var artistTextBox = new TextBox
            {
                Size = new Point(150, 20),
                Location = new Point(titleArtistLabel.Left + titleArtistLabel.Width + 20, titleArtistLabel.Top),
                PlaceholderText = "Artist",
                Parent = composerPanel
            };
            var userLabel = new Label
            {
                Size = new Point(150, 20),
                Location = new Point(0, titleTextBox.Top + 20 + MusicianModule.BOTTOM_MARGIN),
                Text = "Created by",
                Parent = composerPanel
            };

            var userTextBox = new TextBox
            {
                Size = new Point(150, 20),
                Location = new Point(titleArtistLabel.Left + titleArtistLabel.Width + 20, userLabel.Top),
                PlaceholderText = "User (Nekres.1038)",
                Parent = composerPanel
            };
            var ddInstrumentSelection = new Dropdown()
            {
                Parent = composerPanel,
                Location = new Point(0, userTextBox.Top + 20 + MusicianModule.BOTTOM_MARGIN),
                Width = 150,
            };
            foreach (string item in Instruments)
            {
                ddInstrumentSelection.Items.Add(item);
            }
            var ddAlgorithmSelection = new Dropdown()
            {
                Parent = composerPanel,
                Location = new Point(titleArtistLabel.Left + titleArtistLabel.Width + 20, ddInstrumentSelection.Top),
                Width = 150,
            };
            ddAlgorithmSelection.Items.Add("Favor Notes");
            ddAlgorithmSelection.Items.Add("Favor Chords");
            var tempoLabel = new Label()
            {
                Parent = composerPanel,
                Location = new Point(0, ddInstrumentSelection.Top + 22 + MusicianModule.BOTTOM_MARGIN),
                Size = new Point(150, 20),
                Text = "Beats per minute:"
            };
            var tempoCounterBox = new CounterBox()
            {
                Parent = composerPanel,
                Location = new Point(titleArtistLabel.Left + titleArtistLabel.Width + 20, tempoLabel.Top),
                ValueWidth = 50,
                MaxValue = 200,
                MinValue = 40,
                Numerator = 5,
                Value = 90
            };
            var meterLabel = new Label()
            {
                Parent = composerPanel,
                Location = new Point(0, tempoLabel.Top + 22 + MusicianModule.BOTTOM_MARGIN),
                Size = new Point(150, 20),
                Text = "Notes per beat:"
            };
            var meterCounterBox = new CounterBox()
            {
                Parent = composerPanel,
                Location = new Point(titleArtistLabel.Left + titleArtistLabel.Width + 20, meterLabel.Top),
                ValueWidth = 50,
                MaxValue = 16,
                MinValue = 1,
                Prefix = @"1\",
                Exponential = true,
                Value = 1
            };

            // TODO: Draw notation multilined.
            var notationTextBox = new Label
            {
                Size = new Point(composerPanel.Width, composerPanel.Height - 300),
                Location = new Point(0, meterCounterBox.Top + 22 + MusicianModule.BOTTOM_MARGIN),
                Parent = composerPanel
            };

            var saveBttn = new StandardButton()
            {
                Text = "Save",
                Location = new Point(composerPanel.Width - 128 - MusicianModule.RIGHT_MARGIN, notationTextBox.Bottom + 5),
                Width = 128,
                Height = 26,
                Parent = composerPanel
            };

            saveBttn.LeftMouseButtonReleased += (sender, args) => {
                // TODO: Save the notation as XML locally.
            };
            backButton.LeftMouseButtonReleased += (object sender, MouseEventArgs e) => { wndw.NavigateHome(); };
            return cPanel;
        }
        private void UpdateSort(object sender, EventArgs e)
        {
            switch (((Dropdown)sender).SelectedItem)
            {
                case DD_TITLE:
                    displayedSheets.Sort((e1, e2) => e1.Title.CompareTo(e2.Title));
                    foreach (SheetButton e1 in displayedSheets) { e1.Visible = true; }
                    break;
                case DD_ARTIST:
                    displayedSheets.Sort((e1, e2) => e1.Artist.CompareTo(e2.Artist));
                    foreach (SheetButton e1 in displayedSheets) { e1.Visible = true; }
                    break;
                case DD_USER:
                    displayedSheets.Sort((e1, e2) => e1.User.CompareTo(e2.User));
                    foreach (SheetButton e1 in displayedSheets) { e1.Visible = true; }
                    break;
                case DD_HARP:
                    displayedSheets.Sort((e1, e2) => e1.MusicSheet.Instrument.CompareTo(e2.MusicSheet.Instrument));
                    foreach (SheetButton e1 in displayedSheets)
                    {
                        e1.Visible = string.Equals(e1.MusicSheet.Instrument, DD_HARP, StringComparison.InvariantCultureIgnoreCase);
                    }
                    break;
                case DD_FLUTE:
                    foreach (SheetButton e1 in displayedSheets)
                    {
                        e1.Visible = string.Equals(e1.MusicSheet.Instrument, DD_FLUTE, StringComparison.InvariantCultureIgnoreCase);
                    }
                    break;
                case DD_LUTE:
                    foreach (SheetButton e1 in displayedSheets)
                    {
                        e1.Visible = string.Equals(e1.MusicSheet.Instrument, DD_LUTE, StringComparison.InvariantCultureIgnoreCase);
                    }
                    break;
                case DD_HORN:
                    foreach (SheetButton e1 in displayedSheets)
                    {
                        e1.Visible = string.Equals(e1.MusicSheet.Instrument, DD_HORN, StringComparison.InvariantCultureIgnoreCase);
                    }
                    break;
                case DD_BASS:
                    foreach (SheetButton e1 in displayedSheets)
                    {
                        e1.Visible = string.Equals(e1.MusicSheet.Instrument, DD_BASS, StringComparison.InvariantCultureIgnoreCase);
                    }
                    break;
                case DD_BELL:
                    foreach (SheetButton e1 in displayedSheets)
                    {
                        e1.Visible = string.Equals(e1.MusicSheet.Instrument, DD_BELL, StringComparison.InvariantCultureIgnoreCase);
                    }
                    break;
                case DD_BELL2:
                    foreach (SheetButton e1 in displayedSheets)
                    {
                        e1.Visible = string.Equals(e1.MusicSheet.Instrument, DD_BELL2, StringComparison.InvariantCultureIgnoreCase);
                    }
                    break;
            }

            RepositionMel();
        }
        private void RepositionMel()
        {
            int pos = 0;
            foreach (var mel in displayedSheets)
            {
                int x = pos % 3;
                int y = pos / 3;
                mel.Location = new Point(x * 335, y * 108);

                ((Panel)mel.Parent).VerticalScrollOffset = 0;
                mel.Parent.Invalidate();
                if (mel.Visible) pos++;
            }
        }
    }

}
