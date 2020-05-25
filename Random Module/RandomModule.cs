﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.Remoting.Channels;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Controls.Extern;
using Blish_HUD.Controls.Intern;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Pathing.Behaviors;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Random_Module
{

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class RandomModule : Blish_HUD.Modules.Module
    {

        private static readonly Logger Logger = Logger.GetLogger(typeof(RandomModule));

        internal static RandomModule ModuleInstance;

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        private SettingEntry<bool> ShowDie;
        private SettingEntry<int> DieSides;

        internal List<Texture2D> _dieTextures = new List<Texture2D>();
        internal List<Texture2D> _coinTextures = new List<Texture2D>();

        private Panel Die;

        [ImportingConstructor]
        public RandomModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { ModuleInstance = this; }

        protected override void DefineSettings(SettingCollection settings)
        {
            ShowDie = settings.DefineSetting("ShowDie", true, "Show Die", "Shows a die");
            DieSides = settings.DefineSetting("DieSides", 6, "Die Sides", "Indicates the amount of sides the die has.");
        }

        protected override void Initialize() {
            for (var i = 0; i < 7; i++) _dieTextures.Add(ContentsManager.GetTexture($"dice/side{i}.png"));

            /*_coinTextures.Add(ContentsManager.GetTexture("coin/heads.png"));
            _coinTextures.Add(ContentsManager.GetTexture("coin/tails.png"));*/
        }

        protected override async Task LoadAsync() {

        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            DieSides.Value = DieSides.Value > 100 || DieSides.Value < 2 ? 6 : DieSides.Value;
            Die = ShowDie.Value ? CreateDie() : null;

            // Base handler must be called
            base.OnModuleLoaded(e);
        }
        private async void SendToChat(string message)
        {
            var prevClipboardContent = await ClipboardUtil.WindowsClipboardService.GetTextAsync();
            await ClipboardUtil.WindowsClipboardService.SetTextAsync(message)
                .ContinueWith((clipboardResult) => {
                    if (clipboardResult.IsFaulted)
                        Logger.Warn(clipboardResult.Exception, $"Failed to set clipboard text to \"{message}\"!",
                            message);
                    else
                        Task.Run(() => {
                            GameService.GameIntegration.FocusGw2();
                            Mouse.Click(MouseButton.LEFT, GameService.Graphics.GraphicsDevice.Viewport.Width / 2, 0);
                            Keyboard.Stroke(VirtualKeyShort.RETURN);
                            Keyboard.Press(VirtualKeyShort.LCONTROL, true);
                            Keyboard.Stroke(VirtualKeyShort.KEY_V, true);
                            Thread.Sleep(50);
                            Keyboard.Release(VirtualKeyShort.LCONTROL, true);
                            Keyboard.Stroke(VirtualKeyShort.RETURN);
                        }).ContinueWith((result) => {
                            if (prevClipboardContent != null)
                                ClipboardUtil.WindowsClipboardService.SetTextAsync(prevClipboardContent);
                        });
                });
        }
        private Panel CreateDie()
        {
            var rolling = false;
            var _die = new Panel()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Size = new Point(64, 64),
                Location = new Point(0, 0),
                Opacity = 0.4f,
                Visible = false
            };
            var dieImage = new Image()
            {
                Parent = _die,
                Texture = _dieTextures[0],
                Size = new Point(64, 64),
                Location = new Point(0, 0)
            };
            var dieLabel = new Label()
            {
                Parent = _die,
                Size = _die.Size,
                Location = new Point(0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle,
                Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size22, ContentService.FontStyle.Regular),
                ShowShadow = true,
                TextColor = Color.Black,
                ShadowColor = Color.Black,
                StrokeText = false,
                Text = ""
            };

            int ApplyDieValue(bool reset = false)
            {
                var value = reset ? DieSides.Value : RandomUtil.GetRandom(1, DieSides.Value);
                if (value < 7) {
                    dieLabel.Text = "";
                    dieImage.Texture = _dieTextures[value];
                } else {
                    dieImage.Texture = _dieTextures[0];
                    dieLabel.Text = $"{value}";
                }
                return value;
            }
            ApplyDieValue();

            var dieSettingsOpen = false;
            _die.RightMouseButtonPressed += delegate(object sender, MouseEventArgs e)
            {
                if (rolling || dieSettingsOpen) return;
                dieSettingsOpen = true;
                var sidesTotalPanel = new Panel()
                {
                    Parent = GameService.Graphics.SpriteScreen,
                    Size = new Point(200, 120),
                    Location = new Point(GameService.Graphics.SpriteScreen.Width / 2 - 100, GameService.Graphics.SpriteScreen.Height / 2 - 60),
                    Opacity = 0.0f,
                    BackgroundTexture = GameService.Content.GetTexture("502049"),
                    ShowBorder = true,
                    Title = "Die Sides",
                };
                var counter = new CounterBox()
                {
                    Parent = sidesTotalPanel,
                    Size = new Point(100,100),
                    ValueWidth = 60,
                    Location = new Point(sidesTotalPanel.ContentRegion.Width / 2 - 50, sidesTotalPanel.Height / 2 - 50),
                    MinValue = 2,
                    MaxValue = 100,
                    Value = DieSides.Value,
                    Numerator = 1,
                    Suffix = " sides"
                };
                var applyButton = new StandardButton()
                {
                    Parent = sidesTotalPanel,
                    Size = new Point(50, 30),
                    Location = new Point(sidesTotalPanel.ContentRegion.Width / 2 - 25, sidesTotalPanel.ContentRegion.Height - 35),
                    Text = "Apply"
                };
                applyButton.LeftMouseButtonPressed += delegate
                {
                    DieSides.Value = counter.Value;
                    dieSettingsOpen = false;
                    ApplyDieValue(true);
                    var fadeOut = GameService.Animation.Tweener.Tween(sidesTotalPanel, new { Opacity = 0.0f }, 0.2f)
                                                                    .OnComplete(() =>{sidesTotalPanel.Dispose();});
                };
                var fadeIn = GameService.Animation.Tweener.Tween(sidesTotalPanel, new { Opacity = 1.0f }, 0.2f);
            };
            _die.MouseEntered += delegate(object sender, MouseEventArgs e)
            {
                var fadeIn = GameService.Animation.Tweener.Tween(_die, new { Opacity = 1.0f }, 0.45f);
            };
            _die.MouseLeft += delegate(object sender, MouseEventArgs e)
            {
                var fadeOut = GameService.Animation.Tweener.Tween(_die, new { Opacity = 0.4f }, 0.45f);
            };
            _die.Disposed += delegate {
                var fadeOut = GameService.Animation.Tweener.Tween(_die, new { Opacity = 0.0f }, 0.2f);
            };
            _die.LeftMouseButtonPressed += delegate(object sender, MouseEventArgs e)
            {
                if (rolling || dieSettingsOpen) return;
                rolling = true;

                var duration = new Stopwatch();
                var worker = new BackgroundWorker();
                var interval = new System.Timers.Timer(70);
                interval.Elapsed += delegate {
                    if (!worker.IsBusy)
                        worker.RunWorkerAsync();
                };
                worker.DoWork += delegate
                {
                    var value = ApplyDieValue();

                    if (duration.Elapsed > TimeSpan.FromMilliseconds(1200))
                    {
                        interval?.Stop();
                        interval?.Dispose();
                        duration?.Stop();
                        duration = null;
                        SendToChat($"/me rolls {value} on a {DieSides.Value} sided die.");
                        ScreenNotification.ShowNotification(
                            $"{(GameService.Gw2Mumble.IsAvailable ? GameService.Gw2Mumble.PlayerCharacter.Name : "You")} rolls {value} on a {DieSides.Value} sided die.");

                        rolling = false;
                        worker.Dispose();
                    }
                };
                interval.Start();
                duration.Start();
            };
            return _die;
        }
        protected override void Update(GameTime gameTime)
        {
            if (Die != null) {
                Die.Visible = GameService.GameIntegration.IsInGame;
                Die.Location = new Point((GameService.Graphics.SpriteScreen.Width - 480),(GameService.Graphics.SpriteScreen.Height - Die.Height) - 25);
            }
        }

        /// <inheritdoc />
        protected override void Unload() {
            // Unload
            Die?.Dispose();
            // All static members must be manually unset
            ModuleInstance = null;
        }

    }

}
