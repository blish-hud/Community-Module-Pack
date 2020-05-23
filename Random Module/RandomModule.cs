using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Controls.Extern;
using Blish_HUD.Controls.Intern;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Glide;
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

        private SettingEntry<bool> ShowDice;

        internal List<Texture2D> _diceTextures = new List<Texture2D>();
        internal List<Texture2D> _coinTextures = new List<Texture2D>();

        private Image Dice;

        [ImportingConstructor]
        public RandomModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { ModuleInstance = this; }

        protected override void DefineSettings(SettingCollection settings)
        {
            ShowDice = settings.DefineSetting("ShowDice", true, "ShowDice", "Shows a dice");
        }

        protected override void Initialize() {
            _diceTextures.Add(ContentsManager.GetTexture("dice/side1.png"));
            _diceTextures.Add(ContentsManager.GetTexture("dice/side2.png"));
            _diceTextures.Add(ContentsManager.GetTexture("dice/side3.png"));
            _diceTextures.Add(ContentsManager.GetTexture("dice/side4.png"));
            _diceTextures.Add(ContentsManager.GetTexture("dice/side5.png"));
            _diceTextures.Add(ContentsManager.GetTexture("dice/side6.png"));

            /*_coinTextures.Add(ContentsManager.GetTexture("coin/heads.png"));
            _coinTextures.Add(ContentsManager.GetTexture("coin/tails.png"));*/
        }

        protected override async Task LoadAsync() {

        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            Dice = ShowDice.Value ? CreateDice() : null;

            // Base handler must be called
            base.OnModuleLoaded(e);
        }
        private void SendToChat(string message) {
            ClipboardUtil.WindowsClipboardService.SetTextAsync(message)
                .ContinueWith((clipboardResult) => {
                    if (clipboardResult.IsFaulted)
                        Logger.Warn(clipboardResult.Exception, "Failed to set clipboard text to {message}!",
                            message);
                    else
                        Task.Run(() => {
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
                });
        }
        private Image CreateDice()
        {
            var rolling = false;
            var _dice = new Image()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Texture = _diceTextures[RandomUtil.GetRandom(0,5)],
                Size = new Point(64, 64),
                Location = new Point(0, 0),
                Opacity = 0.4f,
                Visible = false
            };
            _dice.MouseEntered += delegate(object sender, MouseEventArgs e)
            {
                var fadeIn = GameService.Animation.Tweener.Tween(_dice, new { Opacity = 1.0f }, 0.45f);
            };
            _dice.MouseLeft += delegate(object sender, MouseEventArgs e)
            {
                var fadeOut = GameService.Animation.Tweener.Tween(_dice, new { Opacity = 0.4f }, 0.45f);
            };
            _dice.Disposed += delegate {
                var fadeOut = GameService.Animation.Tweener.Tween(_dice, new { Opacity = 0.0f }, 0.2f);
            };
            _dice.LeftMouseButtonPressed += delegate(object sender, MouseEventArgs e)
            {
                if (rolling) return;
                rolling = true;

                var index = 0;
                for (var i = 0; i < 20; i++)
                {
                    index = RandomUtil.GetRandom(0, 5);
                    _dice.Texture = _diceTextures[index];
                }

                SendToChat($"/me rolled {index + 1}.");
                ScreenNotification.ShowNotification($"{(GameService.Gw2Mumble.IsAvailable ? GameService.Gw2Mumble.PlayerCharacter.Name : "You")} rolled a {index + 1}.");
                rolling = false;
            };
            return _dice;
        }
        protected override void Update(GameTime gameTime)
        {
            if (Dice != null) {
                Dice.Visible = GameService.GameIntegration.IsInGame;
                Dice.Location = new Point((GameService.Graphics.SpriteScreen.Width - 480),
                    (GameService.Graphics.SpriteScreen.Height - Dice.Height) - 25);
            }
        }

        /// <inheritdoc />
        protected override void Unload() {
            // Unload
            Dice?.Dispose();
            // All static members must be manually unset
            ModuleInstance = null;
        }

    }

}
