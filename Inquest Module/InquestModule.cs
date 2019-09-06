using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Common.Gw2;
using Blish_HUD.Controls;
using Blish_HUD.Controls.Extern;
using Blish_HUD.Controls.Intern;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Inquest_Module
{

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class InquestModule : Blish_HUD.Modules.Module {

        private static readonly Logger Logger = Logger.GetLogger(typeof(InquestModule));

        internal static InquestModule ModuleInstance;

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        #region Settings
        private SettingEntry<bool> KillProofDeceiverEnabled;
        private SettingEntry<Dictionary<string, int>> TokenQuantity;
        private SettingEntry<bool> SkillFramesEnabled;
        private SettingEntry<Dictionary<string, bool>> SkillFramesSettings;
        private SettingEntry<bool> EmotePanelEnabled;
        #endregion

        private List<Control> _moduleControls;

        private Panel DeceiverPanel;
        private Panel QueuePanel;
        private Panel EmotePanel;
        private CornerIcon InquestIcon;
        private ContextMenuStrip InquestIconMenu;
        private Random Randomizer;

        private static Dictionary<string, int> TokenIdRepository = new Dictionary<string, int>()
        {
            {"Legendary Insight", 77302},
            {"Unstable Cosmic Essence", 81743},
            {"Legendary Divination", 88485},
            {"W1 | Vale Guardian Fragment", 77705},
            {"W1 | Gorseval Tentacle Piece", 77751},
            {"W1 | Sabetha Flamethrower Fragment Piece", 77728},
            {"W2 | Slothasor Mushroom", 77722},
            {"W2 | White Mantle Abomination Crystal", 77761},
            {"W3 | Turret Fragment", 78873},
            {"W3 | Keep Construct Rubble", 78905},
            {"W3 | Ribbon Scrap", 78942},
            {"W4 | Cairn Fragment", 80623},
            {"W4 | Recreation Room Floor Fragment",  80269},
            {"W4 | Impaled Prisoner Token", 80087},
            {"W4 | Fragment of Saul's Burden", 80189},
            {"W5 | Desmina's Token", 85993},
            {"W5 | River of Souls Token", 85785},
            {"W5 | Statue Token", 85800},
            {"W5 | Dhuum's Token", 85633},
            {"W6 | Conjured Amalgamate Token", 88543},
            {"W6 | Twin Largos Token", 88860},
            {"W6 | Qadim's Token", 88645},
            {"W7 | Cardinal Adina's Token", 91246},
            {"W7 | Cardinal Sabir's Token", 91270},
            {"W7 | Ether Djinn's Token", 91175}
        };
        private static List<string> RaidWings = new List<string>()
        {
           "W1",
           "W2",
           "W3",
           "W4",
           "W5",
           "W6",
           "W7",
        };
        private static Dictionary<string, int> Abilities = new Dictionary<string, int>()
        {
            { "Weapon Skill 1", 602 },
            { "Weapon Skill 2", 2 },
            { "Weapon Skill 3", 2 },
            { "Weapon Skill 4", 2 },
            { "Weapon Skill 5", 2 },
            { "Healing Skill",2 },
            { "Utility Skill 1", 2 },
            { "Utility Skill 2", 2 },
            { "Utility Skill 3", 2 },
            { "Elite Skill", 2 }
        };
        private static Dictionary<string, Texture2D> EmoteRepository;

        private Dictionary<string, Control> SkillFrames;
        private Dictionary<string, int> TokenQuantityRepository;
        private Dictionary<string, bool> SkillFramesSettingsRepository;
        private Thread QueueWorker;
        private bool QueueAbort;
        private Stopwatch QueueStopWatch;
        private Label QueueTimeLabel;

        [ImportingConstructor]
        public InquestModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { ModuleInstance = this; }

        protected override void DefineSettings(SettingCollection settings)
        {
            KillProofDeceiverEnabled = settings.DefineSetting("KillProofDeceiverEnabled", false, "Kill Proof Deceiver Panel.", "Deceive players by showing off with fake kill proofs.");
            TokenQuantity = settings.DefineSetting<Dictionary<string, int>>("TokenQuantity", new Dictionary<string, int>());

            SkillFramesEnabled = settings.DefineSetting("SkillFramesEnabled", false, "Customizeable Skill Frames", "Shows customizeable skill frames.");
            SkillFramesSettings = settings.DefineSetting<Dictionary<string, bool>>("SkillFramesSettings", new Dictionary<string, bool>());

            EmotePanelEnabled = settings.DefineSetting("EmotePanelEnabled", false, "Emote Panel.", "Express a variety of emotes in a press of a button.");
        }

        protected override void Initialize(){
            Randomizer = new Random();
            _moduleControls = new List<Control>();
            TokenQuantityRepository = TokenQuantity.Value;
            SkillFramesSettingsRepository = SkillFramesSettings.Value;
            SkillFrames = new Dictionary<string, Control>();
            QueueStopWatch = new Stopwatch();
            EmoteRepository = new Dictionary<string, Texture2D>() {
                { "/beckon", ContentsManager.GetTexture("emotes/Beckon.png") },
                { "/bow", ContentsManager.GetTexture("emotes/Bow.png") },
                { "/cheer", ContentsManager.GetTexture("emotes/Cheer.png") },
                { "/cower", ContentsManager.GetTexture("emotes/Tremble.png") },
                { "/cry", ContentsManager.GetTexture("emotes/Cry.png") },
                { "/dance", ContentsManager.GetTexture("emotes/Dance.png") },
                { "/kneel", ContentsManager.GetTexture("emotes/Kneel.png") },
                { "/laugh", ContentsManager.GetTexture("emotes/Laugh.png") },
                { "/no", ContentsManager.GetTexture("emotes/No.png") },
                { "/point", ContentsManager.GetTexture("emotes/Point.png") },
                { "/ponder", ContentsManager.GetTexture("emotes/Think.png") },
                { "/salute", ContentsManager.GetTexture("emotes/Salute.png") },
                { "/shrug", ContentsManager.GetTexture("emotes/Shrug.png") },
                { "/sit", ContentsManager.GetTexture("emotes/Sit.png") },
                { "/sleep", ContentsManager.GetTexture("emotes/Doze.png") },
                { "/surprised", ContentsManager.GetTexture("emotes/Surprised.png") },
                { "/threaten", ContentsManager.GetTexture("emotes/Furious.png") },
                { "/wave", ContentsManager.GetTexture("emotes/Wave.png") },
                { "/yes", ContentsManager.GetTexture("emotes/Yes.png") },
                { "/crossarms", ContentsManager.GetTexture("emotes/At_Ease.png") },
                { "/sad", ContentsManager.GetTexture("emotes/Disappointed.png") },
                { "/talk", ContentsManager.GetTexture("emotes/Converse.png") },
                { "/thanks", ContentsManager.GetTexture("emotes/Thumbs_Up.png") },
                { "/upset", ContentsManager.GetTexture("emotes/Facepalm.png") }
            };
            if (KillProofDeceiverEnabled.Value) { DeceiverPanel = BuildDeceiverPanel(); }
            if (EmotePanelEnabled.Value) { EmotePanel = BuildEmotePanel(); }

            InquestIconMenu = new ContextMenuStrip();

            BuildCategoryMenus();

            InquestIcon = new CornerIcon()
            {
                IconName = "Inquest Chipset",
                Icon = ContentsManager.GetTexture("assault_cube_icon.png"),
                Priority = "Inquest Chipset".GetHashCode()
            };
            InquestIcon.Click += delegate {
                InquestIconMenu.Show(InquestIcon);
            };
        }
        protected override async Task LoadAsync()
        {
        }
        protected override void OnModuleLoaded(EventArgs e)
        {
            // Base handler must be called
            base.OnModuleLoaded(e);
        }
        protected override void Update(GameTime gameTime)
        {
            if (DeceiverPanel != null)
            {
                DeceiverPanel.Visible = GameService.GameIntegration.IsInGame;
            }

            if (EmotePanel != null) {
                EmotePanel.Visible = GameService.GameIntegration.IsInGame;
            }

            if (!GameService.GameIntegration.IsInGame)
            {
                if (!QueueAbort) {
                    QueueAbort = true;
                    if (QueuePanel != null) {
                        QueuePanel.Dispose();
                        QueuePanel = null;
                    }
                }
            } else {
                if (QueueTimeLabel != null && QueueTimeLabel.Visible && QueueStopWatch.IsRunning)
                {
                    QueueTimeLabel.Text = "In instance queue: " + string.Format(string.Format("{0:mm\\:ss}", QueueStopWatch.Elapsed));
                }
            }
        }
        /// <inheritdoc />
        protected override void Unload()
        {
            // Unload
            QueueAbort = true;
            foreach (Control c in _moduleControls)
            {
                if (c != null) c.Dispose();
            }
            if (DeceiverPanel != null) { DeceiverPanel.Dispose(); DeceiverPanel = null; }
            if (EmotePanel != null) { EmotePanel.Dispose(); EmotePanel = null; }
            if (QueuePanel != null) { QueuePanel.Dispose(); QueuePanel = null; }
            if (QueueWorker != null) { QueueWorker.Abort(); QueueWorker = null; }
            InquestIcon.Dispose();
            // All static members must be manually unset
            ModuleInstance = null;
        }
        private void SendToChat(string message) {

            System.Windows.Forms.Clipboard.SetText(message);

            Task.Run(() =>
            {
                Keyboard.Press(VirtualKeyShort.RETURN, true);
                Keyboard.Release(VirtualKeyShort.RETURN, true);
                Keyboard.Press(VirtualKeyShort.LCONTROL, true);
                Keyboard.Press(VirtualKeyShort.KEY_V, true);
                Thread.Sleep(50);
                Keyboard.Release(VirtualKeyShort.LCONTROL, true);
                Keyboard.Release(VirtualKeyShort.KEY_V, true);
                Keyboard.Press(VirtualKeyShort.RETURN, true);
                Keyboard.Release(VirtualKeyShort.RETURN, true);
            });
        }
        private Panel BuildEmotePanel() {
            var emotePanel = new Panel() {
                Parent = GameService.Graphics.SpriteScreen,
                Location = new Point(10, 180),
                Size = new Point(124, 708),
                ShowBorder = true,
                ShowTint = true,
                Opacity = 0.0f
            };

            var pos = new Point(0, 0);
            foreach (KeyValuePair<string, Texture2D> emote in EmoteRepository) {
                var icon = new Image() {
                    Parent = emotePanel,
                    Texture = emote.Value,
                    Size = new Point(56, 56),
                    Location = pos,
                    BasicTooltipText = emote.Key
                };

                pos.X += icon.Width + 2;
                if (pos.X + 56 > emotePanel.Width) {
                    pos.Y += icon.Height + 2;
                    pos.X = 0;
                }

                icon.LeftMouseButtonPressed += delegate {
                    icon.Size = new Point(icon.Size.X - 2, icon.Size.Y - 2);
                    icon.Location = new Point(icon.Location.X + 2, icon.Location.Y + 2);
                };
                icon.LeftMouseButtonReleased += delegate {
                    icon.Size = new Point(icon.Size.X + 2, icon.Size.Y + 2);
                    icon.Location = new Point(icon.Location.X - 2, icon.Location.Y - 2);

                    SendToChat(emote.Key + " @");

                };
            }
            var fadeIn = AnimationService.Animation.Tweener.Tween(emotePanel, new { Opacity = 1.0f }, 0.2f);
            return emotePanel;
        }
        private void TryMapJoin(int x, int y)
        {
            var playerContextMenuItemJoinMapPos = new Point(x, y);
            var errorMapFullOkButton = new Point(0, 0);
            switch (GameService.Graphics.UIScale) {
                case GraphicsService.UiScale.Small:
                    playerContextMenuItemJoinMapPos.X += 78;
                    playerContextMenuItemJoinMapPos.Y += 78;
                    errorMapFullOkButton.X = 1085;
                    errorMapFullOkButton.Y = 586;
                    break;
                case GraphicsService.UiScale.Normal:
                    playerContextMenuItemJoinMapPos.X += 72;
                    playerContextMenuItemJoinMapPos.Y += 81;
                    errorMapFullOkButton.X = 1096;
                    errorMapFullOkButton.Y = 590;
                    break;
                case GraphicsService.UiScale.Large:
                    playerContextMenuItemJoinMapPos.X += 71;
                    playerContextMenuItemJoinMapPos.Y += 97;
                    errorMapFullOkButton.X = 1107;
                    errorMapFullOkButton.Y = 597;
                    break;
                case GraphicsService.UiScale.Larger:
                    playerContextMenuItemJoinMapPos.X += 81;
                    playerContextMenuItemJoinMapPos.Y += 108;
                    errorMapFullOkButton.X = 1126;
                    errorMapFullOkButton.Y = 603;
                    break;
            }
            int tries = 0;
            while (!QueueAbort)
            {
                Mouse.SetPosition(x, y);
                Mouse.Click(MouseButton.RIGHT, x, y);
                Thread.Sleep(10);
                Mouse.SetPosition(playerContextMenuItemJoinMapPos.X, playerContextMenuItemJoinMapPos.Y);
                Mouse.Click(MouseButton.LEFT, playerContextMenuItemJoinMapPos.X, playerContextMenuItemJoinMapPos.Y);
                Thread.Sleep(50);
                Mouse.SetPosition(errorMapFullOkButton.X, errorMapFullOkButton.Y);
                Mouse.Click(MouseButton.LEFT, errorMapFullOkButton.X, errorMapFullOkButton.Y);

                if (QueuePanel != null) {
                    tries++;
                    QueuePanel.BasicTooltipText = "Map join attempts: " + tries;
                }
                Thread.Sleep(Randomizer.Next(700,1000));
            }
            QueueWorker = null;
        }
        private Panel BuildMapQueuePanel()
        {
            var mainPanel = new Panel()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Size = new Point(700, 260),
                Location = new Point(GameService.Graphics.SpriteScreen.Width / 2 - 350, GameService.Graphics.SpriteScreen.Height / 2 - 130),
                ShowBorder = true,
                ShowTint = true,
                Title = GameService.Player.Map.Name != null ? "Map Instance Queue - " + GameService.Player.Map.Name : "Map Instance Queue",
                Opacity = 0.0f
            };
            QueueTimeLabel = new Label()
            {
                Parent = mainPanel,
                Size = mainPanel.ContentRegion.Size,
                Location = new Point(0, 20),
                Text = "Left Click a player in your squad to get his slot coordinates.\nMake sure it's static when squad size changes.",
                Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size24, ContentService.FontStyle.Regular),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                StrokeText = true,
                ShowShadow = true
            };
            mainPanel.Disposed += delegate
            {
                QueueStopWatch.Stop();
            };
            var queuePosLabel = new Label()
            {
                Parent = mainPanel,
                Size = mainPanel.ContentRegion.Size,
                Text = "",
                Location = new Point(0, 90),
                Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size24, ContentService.FontStyle.Regular),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                StrokeText = true,
                ShowShadow = true
            };

            int x = 0, y = 0;

            var queueSpinner = new LoadingSpinner()
            {
                Parent = mainPanel,
                Visible = false
            };
            queueSpinner.Location = new Point(mainPanel.ContentRegion.Width / 2 - queueSpinner.Width / 2, mainPanel.ContentRegion.Height / 2 - queueSpinner.Height / 2);

            var enqueueButton = new StandardButton()
            {
                Parent = mainPanel,
                Size = new Point(100, 50),
                Location = new Point(mainPanel.ContentRegion.Width / 2 - 50, mainPanel.ContentRegion.Height - 60),
                Visible = false,
                Text = "Enqueue"
            };
            GameService.Input.LeftMouseButtonPressed += delegate (object sender, MouseEventArgs e)
            {
                if (mainPanel == null) return;

                if (e.MouseState.X < mainPanel.AbsoluteBounds.X + mainPanel.AbsoluteBounds.Width && e.MouseState.X > mainPanel.AbsoluteBounds.X &&
                    e.MouseState.Y < mainPanel.AbsoluteBounds.Y + mainPanel.AbsoluteBounds.Height && e.MouseState.Y > mainPanel.AbsoluteBounds.Y) return;

                var pos = Mouse.GetPosition();
                x = pos.X;
                y = pos.Y;
                queuePosLabel.Text = "X: " + x + " Y: " + y;
                enqueueButton.Visible = true;
            };
            var closeButton = new Image()
            {
                Parent = mainPanel,
                Texture = ContentService.Content.GetTexture("button-exit"),
                Size = new Point(32, 32),
                Location = new Point(mainPanel.Width - 37, 0)
            };
            closeButton.LeftMouseButtonPressed += delegate
            {
                closeButton.Size = new Point(closeButton.Size.X - 2, closeButton.Size.Y - 2);
                closeButton.Location = new Point(closeButton.Location.X + 2, closeButton.Location.Y + 2);
                closeButton.Texture = ContentService.Content.GetTexture("button-exit-active");
            };
            closeButton.LeftMouseButtonReleased += delegate
            {
                closeButton.Size = new Point(closeButton.Size.X + 2, closeButton.Size.Y + 2);
                closeButton.Location = new Point(closeButton.Location.X - 2, closeButton.Location.Y - 2);
                closeButton.Texture = ContentService.Content.GetTexture("button-exit");

                mainPanel.Dispose();
                QueuePanel = null;
            };
            enqueueButton.LeftMouseButtonPressed += delegate
            {
                enqueueButton.Size = new Point(enqueueButton.Size.X - 2, enqueueButton.Size.Y - 2);
                enqueueButton.Location = new Point(enqueueButton.Location.X + 2, enqueueButton.Location.Y + 2);
            };
            enqueueButton.LeftMouseButtonReleased += delegate
            {
                enqueueButton.Size = new Point(enqueueButton.Size.X + 2, enqueueButton.Size.Y + 2);
                enqueueButton.Location = new Point(enqueueButton.Location.X - 2, enqueueButton.Location.Y - 2);

                if (QueueWorker != null)
                {
                    QueueAbort = true;
                    mainPanel.Dispose();
                    QueuePanel = null;
                }
                else
                {
                    queuePosLabel.Dispose();
                    closeButton.Dispose();
                    queueSpinner.Visible = true;
                    enqueueButton.Text = "Leave";
                    enqueueButton.BackgroundColor = Color.DarkRed;
                    QueueAbort = false;
                    QueueWorker = new Thread(() => TryMapJoin(x, y));
                    QueueWorker.Start();
                    QueueStopWatch.Restart();
                }
            };
            var fadeIn = AnimationService.Animation.Tweener.Tween(mainPanel, new { Opacity = 1.0f }, 0.2f);
            return mainPanel;
        }
        private void BuildCategoryMenus()
        {
            var queueItem = new ContextMenuStripItem()
            {
                Text = "Map Queue",
                Parent = InquestIconMenu
            };
            queueItem.LeftMouseButtonPressed += delegate
            {
                if (GameService.GameIntegration.IsInGame && QueuePanel == null) QueuePanel = BuildMapQueuePanel();
            };
            _moduleControls.Add(queueItem);

            var deceiverItem = new ContextMenuStripItem()
            {
                Text = "Kill Proof Deceiver",
                CanCheck = true,
                Checked = KillProofDeceiverEnabled.Value,
                Parent = InquestIconMenu
            };
            deceiverItem.CheckedChanged += delegate(object sender, CheckChangedEvent e)
            {
                KillProofDeceiverEnabled.Value = e.Checked;

                if (e.Checked)
                {
                    DeceiverPanel = BuildDeceiverPanel();
                }
                else
                {
                    DeceiverPanel.Dispose();
                    DeceiverPanel = null;
                }
            };
            _moduleControls.Add(deceiverItem);

            var emoteItem = new ContextMenuStripItem() {
                Text = "Emote Panel",
                CanCheck = true,
                Checked = EmotePanelEnabled.Value,
                Parent = InquestIconMenu
            };
            emoteItem.CheckedChanged += delegate (object sender, CheckChangedEvent e) {
                EmotePanelEnabled.Value = e.Checked;

                if (e.Checked) {
                    EmotePanel = BuildEmotePanel();
                } else {
                    EmotePanel.Dispose();
                    EmotePanel = null;
                }
            };
            _moduleControls.Add(emoteItem);

            //BuildSkillFramesMenu();
        }
        private void BuildSkillFramesMenu()
        {
            var skillFramesRootMenu = new ContextMenuStrip();
            _moduleControls.Add(skillFramesRootMenu);
            var skillFramesItem = new ContextMenuStripItem()
            {
                Text = "Skill Frames",
                Submenu = skillFramesRootMenu,
                CanCheck = true,
                Parent = InquestIconMenu
            };
            _moduleControls.Add(skillFramesItem);

            foreach (KeyValuePair<string, int> ability in Abilities)
            {
                var abilitySubmenu = new ContextMenuStrip();
                _moduleControls.Add(abilitySubmenu);
                var abilityItem = new ContextMenuStripItem()
                {
                    Text = ability.Key,
                    Submenu = abilitySubmenu,
                    CanCheck = true,
                    Parent = skillFramesRootMenu
                };
                _moduleControls.Add(abilityItem);

                var glassSetting = ability.Key + "|glass";
                var glassItem = new ContextMenuStripItem()
                {
                    Text = "Glass",
                    CanCheck = true,
                    Parent = abilitySubmenu
                };
                try
                {
                    var glassEnabled = SkillFramesSettingsRepository[glassSetting];
                    glassItem.Checked = glassEnabled;
                } catch (KeyNotFoundException ex)
                {
                    glassItem.Checked = false;
                }
                glassItem.CheckedChanged += delegate (object sender, CheckChangedEvent e)
                {
                    if (!SkillFramesSettingsRepository.ContainsKey(glassSetting))
                    {
                        SkillFramesSettingsRepository.Add(glassSetting, e.Checked);
                    }
                    else
                    {
                        SkillFramesSettingsRepository[glassSetting] = e.Checked;
                    }
                    SkillFramesSettings.Value = DictionaryExtension.MergeLeft(SkillFramesSettings.Value, SkillFramesSettingsRepository);

                    if (e.Checked) {
                        var glass = new Image()
                        {
                            Parent = GameService.Graphics.SpriteScreen,
                            Texture = ContentsManager.GetTexture("skill_glass.png"),
                            Size = new Point(58, 58),
                            Location = new Point(ability.Value, 998)
                        };
                        if (!SkillFrames.ContainsKey(glassSetting))
                        {
                            SkillFrames.Add(glassSetting, glass);
                        } else
                        {
                            SkillFrames[glassSetting] = glass;
                        }
                    } else if (SkillFrames.ContainsKey(glassSetting) && SkillFrames[glassSetting] != null) {
                        SkillFrames[glassSetting].Dispose();
                    }
                };
                var frameSetting = ability.Key + "|frame";
                var frameItem = new ContextMenuStripItem()
                {
                    Text = "Frame",
                    CanCheck = true,
                    Parent = abilitySubmenu
                };
                try
                {
                    var frameEnabled = SkillFramesSettingsRepository[frameSetting];
                    frameItem.Checked = frameEnabled;
                }
                catch (KeyNotFoundException ex)
                {
                    frameItem.Checked = false;
                }
                frameItem.CheckedChanged += delegate (object sender, CheckChangedEvent e)
                {
                    if (!SkillFramesSettingsRepository.ContainsKey(frameSetting))
                    {
                        SkillFramesSettingsRepository.Add(frameSetting, e.Checked);
                    }
                    else
                    {
                        SkillFramesSettingsRepository[frameSetting] = e.Checked;
                    }
                    SkillFramesSettings.Value = DictionaryExtension.MergeLeft(SkillFramesSettings.Value, SkillFramesSettingsRepository);

                    if (e.Checked)
                    {
                        var frame = new Image()
                        {
                            Parent = GameService.Graphics.SpriteScreen,
                            Texture = ability.Key.Equals("Elite Skill") ? ContentsManager.GetTexture("skill_elite_frame.png") : ContentsManager.GetTexture("skill_frame.png"),
                            Size = new Point(58, 58)

                        };
                        if (!SkillFrames.ContainsKey(frameSetting))
                        {
                            SkillFrames.Add(frameSetting, frame);
                        }
                        else
                        {
                            SkillFrames[frameSetting] = frame;
                        }
                    }
                    else if (SkillFrames.ContainsKey(frameSetting) && SkillFrames[frameSetting] != null)
                    {
                        SkillFrames[frameSetting].Dispose();
                    }
                };
            }
        }
        private Panel BuildDeceiverPanel()
        {
            var bgPanel = new Panel()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Location = new Point(10, 38),
                Size = new Point(410, 40),
                Opacity = 0.0f,
                ShowBorder = true
            };
            bgPanel.Resized += delegate (object sender, ResizedEventArgs args)
            {
                bgPanel.Location = new Point(10, 38);
            };
            var leftBracket = new Label()
            {
                Parent = bgPanel,
                Size = bgPanel.Size,
                Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size20, ContentService.FontStyle.Regular),
                Text = "[",
                Location = new Point(0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            var quantity = new CounterBox()
            {
                Parent = bgPanel,
                Size = new Point(60, 30),
                Location = new Point(10, 4),
                MinValue = 1,
                MaxValue = 250,
                ValueWidth = 24,
                Numerator = 1,
            };
            var dropdown = new Dropdown()
            {
                Parent = bgPanel,
                Size = new Point(260, 20),
                Location = new Point(quantity.Right + 2, 3)
            };
            var rightBracket = new Label()
            {
                Parent = bgPanel,
                Size = new Point(10, bgPanel.Height),
                Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size20, ContentService.FontStyle.Regular),
                Text = "]",
                Location = new Point(dropdown.Right, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            foreach (KeyValuePair<string, int> pair in TokenIdRepository)
            {
                dropdown.Items.Add(pair.Key);
            }
            dropdown.SelectedItem = "Legendary Insight";
            quantity.Value = TokenQuantityRepository.ContainsKey(dropdown.SelectedItem) ? TokenQuantityRepository[dropdown.SelectedItem] : 125;
            dropdown.ValueChanged += delegate
            {
                try
                {
                    var amount = TokenQuantityRepository[dropdown.SelectedItem];
                    quantity.Value = amount;
                }
                catch (KeyNotFoundException ex)
                {
                    Logger.Warn(ex.Message);
                    quantity.Value = 125;
                }
            };
            var sendButton = new Image()
            {
                Parent = bgPanel,
                Size = new Point(24, 24),
                Location = new Point(rightBracket.Right + 1, 0),
                Texture = GameService.Content.GetTexture("784268"),
                SpriteEffects = SpriteEffects.FlipHorizontally,
                BasicTooltipText = "Send"
            };
            var randomizeButton = new StandardButton()
            {
                Parent = bgPanel,
                Size = new Point(29, 24),
                Location = new Point(sendButton.Right + 7, 0),
                Text = "W1",
                BackgroundColor = Color.Gray,
                BasicTooltipText = "Random token from selected wing when pressing send.\nLeft-Click: Toggle\nRight-Click: Iterate wings"
            };
            randomizeButton.LeftMouseButtonPressed += delegate
            {
                randomizeButton.Size = new Point(27, 22);
                randomizeButton.Location = new Point(sendButton.Right + 5, 2);
            };

            var tokens = new List<string>();
            foreach (KeyValuePair<string, int> pair in TokenIdRepository)
            {
                if (pair.Key.StartsWith(randomizeButton.Text))
                {
                    tokens.Add(pair.Key);
                }
            }

            randomizeButton.LeftMouseButtonReleased += delegate
            {
                randomizeButton.Size = new Point(29, 24);
                randomizeButton.Location = new Point(sendButton.Right + 7, 0);
                randomizeButton.BackgroundColor = randomizeButton.BackgroundColor == Color.Gray ? Color.LightGreen : Color.Gray;
            };
            randomizeButton.RightMouseButtonPressed += delegate
            {
                randomizeButton.Size = new Point(27, 22);
                randomizeButton.Location = new Point(sendButton.Right + 5, 2);
            };
            randomizeButton.RightMouseButtonReleased += delegate
            {
                randomizeButton.Size = new Point(29, 24);
                randomizeButton.Location = new Point(sendButton.Right + 7, 0);
                var current = RaidWings.FindIndex(x => x.Equals(randomizeButton.Text));
                var next = current + 1 <= RaidWings.Count - 1 ? current + 1 : 0;
                randomizeButton.Text = RaidWings[next];
                tokens = new List<string>();
                foreach (KeyValuePair<string, int> pair in TokenIdRepository)
                {
                    if (pair.Key.StartsWith(randomizeButton.Text))
                    {
                        tokens.Add(pair.Key);
                    }
                }
            };
            sendButton.LeftMouseButtonPressed += delegate
            {
                sendButton.Size = new Point(22, 22);
                sendButton.Location = new Point(rightBracket.Right + 3, 2);
            };
            sendButton.LeftMouseButtonReleased += delegate
            {
                sendButton.Size = new Point(24, 24);
                sendButton.Location = new Point(rightBracket.Right + 1, 0);

                if (randomizeButton.BackgroundColor == Color.LightGreen)
                {
                    var rand = Randomizer.Next(0, tokens.Count);
                    var amount = quantity.Value;
                    try
                    {
                        amount = TokenQuantityRepository[tokens[rand]];

                    }
                    catch (KeyNotFoundException ex)
                    {
                        Logger.Warn(ex.Message);
                        amount = 125;
                    }

                    SendToChat(ChatCode.GenerateChatCode(TokenIdRepository[tokens[rand]], amount));

                } else {

                    SendToChat(ChatCode.GenerateChatCode(TokenIdRepository[dropdown.SelectedItem], quantity.Value));

                }
            };
            quantity.LeftMouseButtonPressed += delegate
            {
                if (!TokenQuantityRepository.ContainsKey(dropdown.SelectedItem))
                {
                    TokenQuantityRepository.Add(dropdown.SelectedItem, quantity.Value);
                }
                else
                {
                    TokenQuantityRepository[dropdown.SelectedItem] = quantity.Value;
                }
                TokenQuantity.Value = DictionaryExtension.MergeLeft(TokenQuantity.Value, TokenQuantityRepository);
            };

            var fadeIn = AnimationService.Animation.Tweener.Tween(bgPanel, new { Opacity = 1.0f }, 0.2f);
            bgPanel.Disposed += delegate
            {
                var fadeOut = AnimationService.Animation.Tweener.Tween(bgPanel, new { Opacity = 0.0f }, 0.2f);
            };

            return bgPanel;
        }
    }

}
