using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Controls.Intern;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Gw2Sharp.ChatLinks;
using Gw2Sharp.Mumble.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Inquest_Module
{
    [Export(typeof(Module))]
    public class InquestModule : Module
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(InquestModule));

        internal static InquestModule ModuleInstance;

        private static readonly Dictionary<string, int> TokenIdRepository = new Dictionary<string, int>
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
            {"W4 | Recreation Room Floor Fragment", 80269},
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

        private static readonly List<string> RaidWings = new List<string>
        {
            "W1",
            "W2",
            "W3",
            "W4",
            "W5",
            "W6",
            "W7"
        };

        private static Dictionary<string, Texture2D> EmoteRepository;

        private readonly Dictionary<GuildWarsControls, (int, int, int)> Abilities =
            new Dictionary<GuildWarsControls, (int, int, int)>
            {
                {GuildWarsControls.WeaponSkill1, (-328, 24, 0)},
                {GuildWarsControls.WeaponSkill2, (-267, 24, 0)},
                {GuildWarsControls.WeaponSkill3, (-206, 24, 0)},
                {GuildWarsControls.WeaponSkill4, (-145, 24, 0)},
                {GuildWarsControls.WeaponSkill5, (-84, 24, 0)},
                {GuildWarsControls.HealingSkill, (87, 24, 0)},
                {GuildWarsControls.UtilitySkill1, (148, 24, 0)},
                {GuildWarsControls.UtilitySkill2, (209, 24, 0)},
                {GuildWarsControls.UtilitySkill3, (270, 24, 0)},
                {GuildWarsControls.EliteSkill, (332, 24, 0)}
            };

        private List<Control> _moduleControls;

        private Panel DeceiverPanel;
        private Panel EmotePanel;
        private Image HealthpoolShadow;
        private CornerIcon InquestIcon;
        private ContextMenuStrip InquestIconMenu;
        private bool QueueAbort;
        private Panel QueuePanel;
        private Stopwatch QueueStopWatch;
        private Label QueueTimeLabel;
        private Thread QueueWorker;
        private Random Randomizer;

        private Dictionary<GuildWarsControls, Control> SkillFrames;
        private Dictionary<GuildWarsControls, Control> SkillGlassFrames;
        private Image SurrenderButton;
        private Dictionary<string, int> TokenQuantityRepository;

        [ImportingConstructor]
        public InquestModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
        {
            ModuleInstance = this;
        }

        protected override void DefineSettings(SettingCollection settings)
        {
            KillProofDeceiverEnabled = settings.DefineSetting("KillProofDeceiverEnabled", false,
                "Kill Proof Deceiver Panel.", "Deceive players by showing off with fake kill proofs.");
            TokenQuantity = settings.DefineSetting("TokenQuantity", new Dictionary<string, int>());
            SkillFramesEnabled = settings.DefineSetting("SkillFramesEnabled", false, "Customizable Skill Frames",
                "Shows customizable skill frames.");
            SkillFramesSettings = settings.DefineSetting("SkillFramesSettings",
                new Dictionary<GuildWarsControls, Tuple<bool, bool>>());
            HealthpoolShadowEnabled = settings.DefineSetting("HealthpoolShadowEnabled", false, "Health-pool shadow",
                "Shows an inner shadow on the health-pool.");
            EmotePanelEnabled = settings.DefineSetting("EmotePanelEnabled", false, "Emote Panel.",
                "Express a variety of emotes in a press of a button.");
            SurrenderButtonEnabled = settings.DefineSetting("SurrenderButtonEnabled", false, "Surrender Button.",
                "Send /gg by a press of the white surrender flag.");
        }

        protected override void Initialize()
        {
            Randomizer = new Random();
            _moduleControls = new List<Control>();
            TokenQuantityRepository = TokenQuantity.Value;
            SkillFrames = new Dictionary<GuildWarsControls, Control>();
            SkillGlassFrames = new Dictionary<GuildWarsControls, Control>();
            QueueStopWatch = new Stopwatch();
            EmoteRepository = new Dictionary<string, Texture2D>
            {
                //Grey (neutral)
                {"/beckon", ContentsManager.GetTexture("emotes/Beckon.png")},
                {"/bow", ContentsManager.GetTexture("emotes/Bow.png")},
                {"/kneel", ContentsManager.GetTexture("emotes/Kneel.png")},
                {"/point", ContentsManager.GetTexture("emotes/Point.png")},
                {"/ponder", ContentsManager.GetTexture("emotes/Think.png")},
                {"/salute", ContentsManager.GetTexture("emotes/Salute.png")},
                {"/sit", ContentsManager.GetTexture("emotes/Sit.png")},
                {"/sleep", ContentsManager.GetTexture("emotes/Doze.png")},
                {"/wave", ContentsManager.GetTexture("emotes/Wave.png")},
                {"/crossarms", ContentsManager.GetTexture("emotes/At_Ease.png")},
                {"/talk", ContentsManager.GetTexture("emotes/Converse.png")},
                //Blue (negative)
                {"/sad", ContentsManager.GetTexture("emotes/Disappointed.png")},
                {"/threaten", ContentsManager.GetTexture("emotes/Furious.png")},
                {"/surprised", ContentsManager.GetTexture("emotes/Surprised.png")},
                {"/shrug", ContentsManager.GetTexture("emotes/Shrug.png")},
                {"/no", ContentsManager.GetTexture("emotes/No.png")},
                {"/cry", ContentsManager.GetTexture("emotes/Cry.png")},
                {"/cower", ContentsManager.GetTexture("emotes/Tremble.png")},
                {"/upset", ContentsManager.GetTexture("emotes/Facepalm.png")},
                //Yellow (positive)
                {"/yes", ContentsManager.GetTexture("emotes/Yes.png")},
                {"/laugh", ContentsManager.GetTexture("emotes/Laugh.png")},
                {"/dance", ContentsManager.GetTexture("emotes/Dance.png")},
                {"/cheer", ContentsManager.GetTexture("emotes/Cheer.png")},
                {"/thanks", ContentsManager.GetTexture("emotes/Thumbs_Up.png")}
            };
            if (KillProofDeceiverEnabled.Value) DeceiverPanel = BuildDeceiverPanel();
            if (EmotePanelEnabled.Value) EmotePanel = BuildEmotePanel();
            if (SurrenderButtonEnabled.Value) SurrenderButton = BuildSurrenderButton();

            if (SkillFramesEnabled.Value)
            {
                foreach (var frameSetting in SkillFramesSettings.Value)
                {
                    if (frameSetting.Value.Item1) CreateSkillFrame(frameSetting.Key);
                    if (frameSetting.Value.Item2) CreateSkillFrame(frameSetting.Key, true);
                }

                if (HealthpoolShadowEnabled.Value)
                    HealthpoolShadow = new Image
                    {
                        Parent = GameService.Graphics.SpriteScreen,
                        Texture = ContentsManager.GetTexture("healthpool_shadow.png"),
                        Size = new Point(111, 111)
                    };
            }

            InquestIconMenu = new ContextMenuStrip();

            BuildCategoryMenus();

            InquestIcon = new CornerIcon
            {
                IconName = "Inquest Chipset",
                Icon = ContentsManager.GetTexture("assault_cube_icon.png"),
                Priority = "Inquest Chipset".GetHashCode()
            };
            InquestIcon.Click += delegate { InquestIconMenu.Show(InquestIcon); };
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
            if (DeceiverPanel != null) DeceiverPanel.Visible = GameService.GameIntegration.IsInGame;
            if (EmotePanel != null) EmotePanel.Visible = GameService.GameIntegration.IsInGame;
            if (SurrenderButton != null)
            {
                SurrenderButton.Visible = GameService.GameIntegration.IsInGame;
                SurrenderButton.Location =
                    new Point(GameService.Graphics.SpriteScreen.Width / 2 - SurrenderButton.Width / 2 + 431,
                        GameService.Graphics.SpriteScreen.Height - SurrenderButton.Height * 2 + 7);
            }

            if (!GameService.GameIntegration.IsInGame && !QueueAbort)
            {
                QueueAbort = true;
                if (QueuePanel != null)
                {
                    QueuePanel.Dispose();
                    QueuePanel = null;
                }
            }

            if (QueueTimeLabel != null && QueueTimeLabel.Visible && QueueStopWatch.IsRunning)
                QueueTimeLabel.Text = "In instance queue: " + string.Format($"{QueueStopWatch.Elapsed:mm\\:ss}");
            if (SkillFramesEnabled.Value)
            {
                if (HealthpoolShadow != null)
                {
                    HealthpoolShadow.Visible = GameService.GameIntegration.IsInGame;
                    HealthpoolShadow.Location =
                        new Point(GameService.Graphics.SpriteScreen.Width / 2 - HealthpoolShadow.Width / 2,
                            GameService.Graphics.SpriteScreen.Height - HealthpoolShadow.Height - 17);
                }

                foreach (var frame in SkillFrames)
                {
                    if (frame.Value == null) continue;
                    frame.Value.Visible = GameService.GameIntegration.IsInGame;
                    frame.Value.Location = new Point(
                        GameService.Graphics.SpriteScreen.Width / 2 - frame.Value.Width / 2 +
                        Abilities[frame.Key].Item1,
                        GameService.Graphics.SpriteScreen.Height - frame.Value.Height - Abilities[frame.Key].Item2);
                }

                foreach (var frame in SkillGlassFrames)
                {
                    if (frame.Value == null) continue;
                    frame.Value.Visible = GameService.GameIntegration.IsInGame;
                    frame.Value.Location = new Point(
                        GameService.Graphics.SpriteScreen.Width / 2 - frame.Value.Width / 2 +
                        Abilities[frame.Key].Item1,
                        GameService.Graphics.SpriteScreen.Height - frame.Value.Height - Abilities[frame.Key].Item2);
                }
            }
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            // Unload
            QueueAbort = true;
            foreach (var c in _moduleControls) c?.Dispose();
            DeceiverPanel?.Dispose();
            EmotePanel?.Dispose();
            SurrenderButton?.Dispose();
            HealthpoolShadow?.Dispose();
            QueuePanel?.Dispose();
            QueueWorker?.Abort();
            QueueWorker = null;
            InquestIcon?.Dispose();
            // All static members must be manually unset
            ModuleInstance = null;
        }

        private Panel BuildEmotePanel()
        {
            var emotePanel = new Panel
            {
                Parent = GameService.Graphics.SpriteScreen,
                Location = new Point(10, 180),
                Size = new Point(124, 708),
                ShowBorder = true,
                ShowTint = true,
                Opacity = 0.0f
            };

            var pos = new Point(0, 0);
            foreach (var emote in EmoteRepository)
            {
                var icon = new Image
                {
                    Parent = emotePanel,
                    Texture = emote.Value,
                    Size = new Point(56, 56),
                    Location = pos,
                    BasicTooltipText = emote.Key
                };

                pos.X += icon.Width + 2;
                if (pos.X + 56 > emotePanel.Width)
                {
                    pos.Y += icon.Height + 2;
                    pos.X = 0;
                }

                icon.LeftMouseButtonPressed += delegate
                {
                    icon.Size = new Point(icon.Size.X - 2, icon.Size.Y - 2);
                    icon.Location = new Point(icon.Location.X + 2, icon.Location.Y + 2);
                };
                icon.LeftMouseButtonReleased += delegate
                {
                    icon.Size = new Point(icon.Size.X + 2, icon.Size.Y + 2);
                    icon.Location = new Point(icon.Location.X - 2, icon.Location.Y - 2);

                    GameService.GameIntegration.Chat.Send(emote.Key + " @");
                };
            }

            GameService.Animation.Tweener.Tween(emotePanel, new {Opacity = 1.0f}, 0.35f);
            return emotePanel;
        }

        private Image BuildSurrenderButton()
        {
            var tooltip_texture = ContentsManager.GetTexture("surrender_tooltip.png");
            var tooltip_size = new Point(tooltip_texture.Width, tooltip_texture.Height);
            var surrenderButtonTooltip = new Tooltip
            {
                Size = tooltip_size
            };
            var surrenderButtonTooltipImage = new Image(tooltip_texture)
            {
                Parent = surrenderButtonTooltip,
                Location = new Point(0, 0),
                Visible = surrenderButtonTooltip.Visible
            };
            var surrenderButton = new Image
            {
                Parent = GameService.Graphics.SpriteScreen,
                Size = new Point(45, 45),
                Location = new Point(GameService.Graphics.SpriteScreen.Width / 2 - 22,
                    GameService.Graphics.SpriteScreen.Height - 45),
                Texture = ContentsManager.GetTexture("surrender_flag.png"),
                Visible = SurrenderButtonEnabled.Value,
                Tooltip = surrenderButtonTooltip,
                Opacity = 0.0f
            };
            surrenderButton.MouseEntered += delegate
            {
                surrenderButton.Texture = ContentsManager.GetTexture("surrender_flag_hover.png");
            };
            surrenderButton.MouseLeft += delegate
            {
                surrenderButton.Texture = ContentsManager.GetTexture("surrender_flag.png");
            };
            surrenderButton.LeftMouseButtonPressed += delegate
            {
                surrenderButton.Size = new Point(43, 43);
                surrenderButton.Texture = ContentsManager.GetTexture("surrender_flag_pressed.png");
            };
            surrenderButton.LeftMouseButtonReleased += delegate
            {
                surrenderButton.Size = new Point(45, 45);
                surrenderButton.Texture = ContentsManager.GetTexture("surrender_flag.png");
                GameService.GameIntegration.Chat.Send("/gg");
            };
            GameService.Animation.Tweener.Tween(surrenderButton, new {Opacity = 1.0}, 0.35f);
            return surrenderButton;
        }

        private void TryMapJoin(int x, int y)
        {
            var playerContextMenuItemJoinMapPos = new Point(x, y);
            var errorMapFullOkButton = new Point(0, 0);
            switch (GameService.Gw2Mumble.UI.UISize)
            {
                case UiSize.Small:
                    playerContextMenuItemJoinMapPos.X += 78;
                    playerContextMenuItemJoinMapPos.Y += 78;
                    errorMapFullOkButton.X = 1085;
                    errorMapFullOkButton.Y = 586;
                    break;
                case UiSize.Normal:
                    playerContextMenuItemJoinMapPos.X += 72;
                    playerContextMenuItemJoinMapPos.Y += 81;
                    errorMapFullOkButton.X = 1096;
                    errorMapFullOkButton.Y = 590;
                    break;
                case UiSize.Large:
                    playerContextMenuItemJoinMapPos.X += 71;
                    playerContextMenuItemJoinMapPos.Y += 97;
                    errorMapFullOkButton.X = 1107;
                    errorMapFullOkButton.Y = 597;
                    break;
                case UiSize.Larger:
                    playerContextMenuItemJoinMapPos.X += 81;
                    playerContextMenuItemJoinMapPos.Y += 108;
                    errorMapFullOkButton.X = 1126;
                    errorMapFullOkButton.Y = 603;
                    break;
            }

            var tries = 0;
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

                if (QueuePanel != null)
                {
                    tries++;
                    QueuePanel.BasicTooltipText = "Map join attempts: " + tries;
                }

                Thread.Sleep(Randomizer.Next(700, 1000));
            }

            QueueWorker = null;
        }

        private Panel BuildMapQueuePanel()
        {
            var mainPanel = new Panel
            {
                Parent = GameService.Graphics.SpriteScreen,
                Size = new Point(700, 260),
                Location = new Point(GameService.Graphics.SpriteScreen.Width / 2 - 350,
                    GameService.Graphics.SpriteScreen.Height / 2 - 130),
                ShowBorder = true,
                ShowTint = true,
                Title = "Map Instance Queue - Enqueue for another player's full instance.",
                Opacity = 0.0f
            };
            QueueTimeLabel = new Label
            {
                Parent = mainPanel,
                Size = mainPanel.ContentRegion.Size,
                Location = new Point(0, 20),
                Text =
                    "Left Click a player in your squad to get his slot coordinates.\nMake sure it's static when squad size changes.",
                Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size24,
                    ContentService.FontStyle.Regular),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                StrokeText = true,
                ShowShadow = true
            };
            mainPanel.Disposed += delegate { QueueStopWatch.Stop(); };
            var queuePosLabel = new Label
            {
                Parent = mainPanel,
                Size = mainPanel.ContentRegion.Size,
                Text = "",
                Location = new Point(0, 90),
                Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size24,
                    ContentService.FontStyle.Regular),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                StrokeText = true,
                ShowShadow = true
            };

            int x = 0, y = 0;

            var queueSpinner = new LoadingSpinner
            {
                Parent = mainPanel,
                Visible = false
            };
            queueSpinner.Location = new Point(mainPanel.ContentRegion.Width / 2 - queueSpinner.Width / 2,
                mainPanel.ContentRegion.Height / 2 - queueSpinner.Height / 2);

            var enqueueButton = new StandardButton
            {
                Parent = mainPanel,
                Size = new Point(100, 50),
                Location = new Point(mainPanel.ContentRegion.Width / 2 - 50, mainPanel.ContentRegion.Height - 60),
                Visible = false,
                Text = "Enqueue"
            };
            GameService.Input.Mouse.LeftMouseButtonPressed += delegate(object sender, MouseEventArgs e)
            {
                if (mainPanel == null) return;

                if (e.MouseState.X < mainPanel.AbsoluteBounds.X + mainPanel.AbsoluteBounds.Width &&
                    e.MouseState.X > mainPanel.AbsoluteBounds.X &&
                    e.MouseState.Y < mainPanel.AbsoluteBounds.Y + mainPanel.AbsoluteBounds.Height &&
                    e.MouseState.Y > mainPanel.AbsoluteBounds.Y) return;

                var pos = Mouse.GetPosition();
                x = pos.X;
                y = pos.Y;
                queuePosLabel.Text = "X: " + x + " Y: " + y;
                enqueueButton.Visible = true;
            };
            var closeButton = new Image
            {
                Parent = mainPanel,
                Texture = GameService.Content.GetTexture("button-exit"),
                Size = new Point(32, 32),
                Location = new Point(mainPanel.Width - 37, 0)
            };
            closeButton.LeftMouseButtonPressed += delegate
            {
                closeButton.Size = new Point(closeButton.Size.X - 2, closeButton.Size.Y - 2);
                closeButton.Location = new Point(closeButton.Location.X + 2, closeButton.Location.Y + 2);
                closeButton.Texture = GameService.Content.GetTexture("button-exit-active");
            };
            closeButton.LeftMouseButtonReleased += delegate
            {
                closeButton.Size = new Point(closeButton.Size.X + 2, closeButton.Size.Y + 2);
                closeButton.Location = new Point(closeButton.Location.X - 2, closeButton.Location.Y - 2);
                closeButton.Texture = GameService.Content.GetTexture("button-exit");

                GameService.Animation.Tweener.Tween(mainPanel, new {Opacity = 0.0f}, 0.2f).OnComplete(() =>
                {
                    mainPanel.Dispose();
                    QueuePanel = null;
                });
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
            GameService.Animation.Tweener.Tween(mainPanel, new {Opacity = 1.0f}, 0.35f);
            return mainPanel;
        }

        private void BuildCategoryMenus()
        {
            var queueItem = new ContextMenuStripItem
            {
                Text = "Map Queue",
                Parent = InquestIconMenu
            };
            queueItem.LeftMouseButtonPressed += delegate
            {
                if (GameService.GameIntegration.IsInGame && QueuePanel == null) QueuePanel = BuildMapQueuePanel();
            };
            _moduleControls.Add(queueItem);

            var deceiverItem = new ContextMenuStripItem
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
                    DeceiverPanel = BuildDeceiverPanel();
                else
                    GameService.Animation.Tweener.Tween(DeceiverPanel, new {Opacity = 0.0f}, 0.2f)
                        .OnComplete(() =>
                            {
                                DeceiverPanel.Dispose();
                                DeceiverPanel = null;
                            }
                        );
            };
            _moduleControls.Add(deceiverItem);

            var emoteItem = new ContextMenuStripItem
            {
                Text = "Emote Panel",
                CanCheck = true,
                Checked = EmotePanelEnabled.Value,
                Parent = InquestIconMenu
            };
            emoteItem.CheckedChanged += delegate(object sender, CheckChangedEvent e)
            {
                EmotePanelEnabled.Value = e.Checked;

                if (e.Checked)
                    EmotePanel = BuildEmotePanel();
                else
                    GameService.Animation.Tweener.Tween(EmotePanel, new {Opacity = 0.0f}, 0.2f)
                        .OnComplete(() =>
                            {
                                EmotePanel.Dispose();
                                EmotePanel = null;
                            }
                        );
            };
            _moduleControls.Add(emoteItem);

            var surrenderItem = new ContextMenuStripItem
            {
                Text = "Surrender Button",
                CanCheck = true,
                Checked = SurrenderButtonEnabled.Value,
                Parent = InquestIconMenu
            };
            surrenderItem.CheckedChanged += delegate(object sender, CheckChangedEvent e)
            {
                SurrenderButtonEnabled.Value = e.Checked;

                if (e.Checked)
                    SurrenderButton = BuildSurrenderButton();
                else
                    GameService.Animation.Tweener.Tween(SurrenderButton, new {Opacity = 0.0f}, 0.2f)
                        .OnComplete(() =>
                            {
                                SurrenderButton.Dispose();
                                SurrenderButton = null;
                            }
                        );
            };
            _moduleControls.Add(surrenderItem);

            BuildSkillFramesMenu();
        }

        private void CreateSkillFrame(GuildWarsControls control, bool glass = false)
        {
            if (!SkillFramesEnabled.Value) return;

            var texture = glass ? ContentsManager.GetTexture("skill_glass.png") :
                control == GuildWarsControls.EliteSkill ? ContentsManager.GetTexture("skill_elite_frame.png") :
                ContentsManager.GetTexture("skill_frame.png");

            var scale = Abilities[control].Item3 != 0 ? Abilities[control].Item3 : 58;

            var img = new Image
            {
                Parent = GameService.Graphics.SpriteScreen,
                Texture = texture,
                Size = new Point(scale, scale)
            };
            img.Location = new Point(
                GameService.Graphics.SpriteScreen.Width / 2 - img.Width / 2 + Abilities[control].Item1,
                GameService.Graphics.SpriteScreen.Height - img.Height - Abilities[control].Item2);

            _moduleControls.Add(img);

            if (glass)
                if (!SkillGlassFrames.ContainsKey(control))
                    SkillGlassFrames.Add(control, img);
                else
                    SkillGlassFrames[control] = img;
            else if (!SkillFrames.ContainsKey(control))
                SkillFrames.Add(control, img);
            else
                SkillFrames[control] = img;
        }

        private void BuildSkillFramesMenu()
        {
            var skillFramesRootMenu = new ContextMenuStrip();
            _moduleControls.Add(skillFramesRootMenu);
            var skillFramesItem = new ContextMenuStripItem
            {
                Text = "Skill Frames",
                Submenu = skillFramesRootMenu,
                CanCheck = true,
                Parent = InquestIconMenu,
                Checked = SkillFramesEnabled.Value
            };
            skillFramesItem.CheckedChanged += delegate
            {
                SkillFramesEnabled.Value = skillFramesItem.Checked;

                if (!skillFramesItem.Checked)
                {
                    var frames = SkillFrames.Select(x => x.Value).ToList();
                    frames.AddRange(SkillGlassFrames.Select(y => y.Value));
                    foreach (var frame in frames) frame?.Dispose();

                    SkillFrames.Clear();
                    SkillGlassFrames.Clear();
                    HealthpoolShadow?.Dispose();
                }
                else
                {
                    foreach (var frameSetting in SkillFramesSettings.Value)
                    {
                        if (frameSetting.Value.Item1) CreateSkillFrame(frameSetting.Key);
                        if (frameSetting.Value.Item2) CreateSkillFrame(frameSetting.Key, true);
                    }

                    if (HealthpoolShadowEnabled.Value)
                        HealthpoolShadow = new Image
                        {
                            Parent = GameService.Graphics.SpriteScreen,
                            Texture = ContentsManager.GetTexture("healthpool_shadow.png"),
                            Size = new Point(111, 111)
                        };
                }
            };

            _moduleControls.Add(skillFramesItem);
            var healthPoolItem = new ContextMenuStripItem
            {
                Text = "Healthpool Shadow",
                CanCheck = true,
                Parent = skillFramesRootMenu,
                Checked = HealthpoolShadowEnabled.Value
            };
            healthPoolItem.CheckedChanged += delegate(object sender, CheckChangedEvent e)
            {
                HealthpoolShadowEnabled.Value = e.Checked;
                if (e.Checked && SkillFramesEnabled.Value)
                    HealthpoolShadow = new Image
                    {
                        Parent = GameService.Graphics.SpriteScreen,
                        Texture = ContentsManager.GetTexture("healthpool_shadow.png"),
                        Size = new Point(111, 111)
                    };
                else
                    HealthpoolShadow?.Dispose();
            };
            foreach (var ability in Abilities)
            {
                var frameEnabled = false;
                var glassEnabled = false;
                if (SkillFramesSettings.Value.Any(x => x.Key == ability.Key))
                {
                    var settingsEntry = SkillFramesSettings.Value[ability.Key];
                    frameEnabled = settingsEntry.Item1;
                    glassEnabled = settingsEntry.Item2;
                }
                else
                {
                    SkillFramesSettings.Value.Add(ability.Key, new Tuple<bool, bool>(false, false));
                }

                var friendlyName = Regex.Replace(ability.Key.ToString(), "([A-Z]|[1-9])", " $1", RegexOptions.Compiled)
                    .Trim();
                var abilitySubmenu = new ContextMenuStrip();
                _moduleControls.Add(abilitySubmenu);
                var abilityItem = new ContextMenuStripItem
                {
                    Text = friendlyName,
                    Submenu = abilitySubmenu,
                    CanCheck = false,
                    Parent = skillFramesRootMenu
                };
                _moduleControls.Add(abilityItem);

                var glassItem = new ContextMenuStripItem
                {
                    Text = "Glass",
                    CanCheck = true,
                    Parent = abilitySubmenu,
                    Checked = glassEnabled
                };
                glassItem.CheckedChanged += delegate(object sender, CheckChangedEvent e)
                {
                    SkillFramesSettings.Value[ability.Key] =
                        new Tuple<bool, bool>(SkillFramesSettings.Value[ability.Key].Item1, e.Checked);
                    SkillFramesSettings.Value =
                        new Dictionary<GuildWarsControls, Tuple<bool, bool>>(SkillFramesSettings.Value);
                    if (e.Checked)
                        CreateSkillFrame(ability.Key, true);
                    else if (SkillGlassFrames.ContainsKey(ability.Key) && SkillGlassFrames[ability.Key] != null)
                        SkillGlassFrames[ability.Key].Dispose();
                };
                var frameItem = new ContextMenuStripItem
                {
                    Text = "Frame",
                    CanCheck = true,
                    Parent = abilitySubmenu,
                    Checked = frameEnabled
                };
                frameItem.CheckedChanged += delegate(object sender, CheckChangedEvent e)
                {
                    SkillFramesSettings.Value[ability.Key] =
                        new Tuple<bool, bool>(e.Checked, SkillFramesSettings.Value[ability.Key].Item2);
                    SkillFramesSettings.Value =
                        new Dictionary<GuildWarsControls, Tuple<bool, bool>>(SkillFramesSettings.Value);
                    if (e.Checked)
                        CreateSkillFrame(ability.Key);
                    else if (SkillFrames.ContainsKey(ability.Key) && SkillFrames[ability.Key] != null)
                        SkillFrames[ability.Key].Dispose();
                };
            }
        }

        private Panel BuildDeceiverPanel()
        {
            var bgPanel = new Panel
            {
                Parent = GameService.Graphics.SpriteScreen,
                Location = new Point(10, 38),
                Size = new Point(410, 40),
                Opacity = 0.0f,
                Visible = false,
                ShowBorder = true
            };
            bgPanel.Resized += delegate { bgPanel.Location = new Point(10, 38); };
            bgPanel.MouseEntered += delegate
            {
                GameService.Animation.Tweener.Tween(bgPanel, new {Opacity = 1.0f}, 0.45f);
            };
            var leftBracket = new Label
            {
                Parent = bgPanel,
                Size = bgPanel.Size,
                Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size20,
                    ContentService.FontStyle.Regular),
                Text = "[",
                Location = new Point(0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            var quantity = new CounterBox
            {
                Parent = bgPanel,
                Size = new Point(60, 30),
                Location = new Point(10, 4),
                MinValue = 1,
                MaxValue = 250,
                ValueWidth = 24,
                Numerator = 1
            };
            bgPanel.MouseLeft += delegate
            {
                //TODO: Check for when dropdown IsExpanded
                GameService.Animation.Tweener.Tween(bgPanel, new {Opacity = 0.4f}, 0.45f);
            };
            var dropdown = new Dropdown
            {
                Parent = bgPanel,
                Size = new Point(260, 20),
                Location = new Point(quantity.Right + 2, 3)
            };
            var rightBracket = new Label
            {
                Parent = bgPanel,
                Size = new Point(10, bgPanel.Height),
                Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size20,
                    ContentService.FontStyle.Regular),
                Text = "]",
                Location = new Point(dropdown.Right, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            foreach (var pair in TokenIdRepository) dropdown.Items.Add(pair.Key);
            dropdown.SelectedItem = "Legendary Insight";
            quantity.Value = TokenQuantityRepository.ContainsKey(dropdown.SelectedItem)
                ? TokenQuantityRepository[dropdown.SelectedItem]
                : 125;
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
            var sendButton = new Image
            {
                Parent = bgPanel,
                Size = new Point(24, 24),
                Location = new Point(rightBracket.Right + 1, 0),
                Texture = GameService.Content.GetTexture("784268"),
                SpriteEffects = SpriteEffects.FlipHorizontally,
                BasicTooltipText = "Send"
            };
            var randomizeButton = new StandardButton
            {
                Parent = bgPanel,
                Size = new Point(29, 24),
                Location = new Point(sendButton.Right + 7, 0),
                Text = "W1",
                BackgroundColor = Color.Gray,
                BasicTooltipText =
                    "Random token from selected wing when pressing send.\nLeft-Click: Toggle\nRight-Click: Iterate wings"
            };
            randomizeButton.LeftMouseButtonPressed += delegate
            {
                randomizeButton.Size = new Point(27, 22);
                randomizeButton.Location = new Point(sendButton.Right + 5, 2);
            };

            var tokens = new List<string>();
            foreach (var pair in TokenIdRepository)
                if (pair.Key.StartsWith(randomizeButton.Text))
                    tokens.Add(pair.Key);

            randomizeButton.LeftMouseButtonReleased += delegate
            {
                randomizeButton.Size = new Point(29, 24);
                randomizeButton.Location = new Point(sendButton.Right + 7, 0);
                randomizeButton.BackgroundColor =
                    randomizeButton.BackgroundColor == Color.Gray ? Color.LightGreen : Color.Gray;
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
                foreach (var pair in TokenIdRepository)
                    if (pair.Key.StartsWith(randomizeButton.Text))
                        tokens.Add(pair.Key);
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

                var chatLink = new ItemChatLink();

                if (randomizeButton.BackgroundColor == Color.LightGreen)
                {
                    var rand = Randomizer.Next(0, tokens.Count);
                    chatLink.ItemId = TokenIdRepository[tokens[rand]];
                    int amount;
                    try
                    {
                        amount = TokenQuantityRepository[tokens[rand]];
                    }
                    catch (KeyNotFoundException ex)
                    {
                        Logger.Warn(ex.Message);
                        amount = 125;
                    }

                    chatLink.Quantity = Convert.ToByte(amount);
                    GameService.GameIntegration.Chat.Send(chatLink.ToString());
                }
                else
                {
                    chatLink.ItemId = TokenIdRepository[dropdown.SelectedItem];
                    chatLink.Quantity = Convert.ToByte(quantity.Value);
                    GameService.GameIntegration.Chat.Send(chatLink.ToString());
                }
            };
            quantity.LeftMouseButtonPressed += delegate
            {
                if (!TokenQuantityRepository.ContainsKey(dropdown.SelectedItem))
                    TokenQuantityRepository.Add(dropdown.SelectedItem, quantity.Value);
                else
                    TokenQuantityRepository[dropdown.SelectedItem] = quantity.Value;
                TokenQuantity.Value = TokenQuantity.Value.MergeLeft(TokenQuantityRepository);
            };
            GameService.Animation.Tweener.Tween(bgPanel, new {Opacity = 0.4}, 0.35f);
            return bgPanel;
        }

        #region Service Managers

        internal SettingsManager SettingsManager => ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => ModuleParameters.Gw2ApiManager;

        #endregion

        #region Settings

        private SettingEntry<bool> KillProofDeceiverEnabled;
        private SettingEntry<Dictionary<string, int>> TokenQuantity;
        private SettingEntry<bool> SkillFramesEnabled;
        private SettingEntry<Dictionary<GuildWarsControls, Tuple<bool, bool>>> SkillFramesSettings;
        private SettingEntry<bool> EmotePanelEnabled;
        private SettingEntry<bool> SurrenderButtonEnabled;
        private SettingEntry<bool> HealthpoolShadowEnabled;

        #endregion
    }
}