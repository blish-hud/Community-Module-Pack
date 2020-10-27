using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Quick_Surrender_Module
{

    [Export(typeof(Module))]
    public class QuickSurrenderModule : Module
    {
        //private static readonly Logger Logger = Logger.GetLogger(typeof(QuickSurrenderModule));

        internal static QuickSurrenderModule ModuleInstance;

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        [ImportingConstructor]
        public QuickSurrenderModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { ModuleInstance = this; }


        protected override void DefineSettings(SettingCollection settings) {
            SurrenderButtonEnabled = settings.DefineSetting("SurrenderButtonEnabled", true, "Show Surrender Skill",
                "Shows a skill with a white flag to the right of your skill bar.\nClicking it defeats you. (Sends \"/gg\" into chat.)");
            SurrenderBinding = settings.DefineSetting("SurrenderButtonKey", new KeyBinding(Keys.None),
                "Surrender", "Defeats you.\n(Sends \"/gg\" into chat.)");
        }

        #region Controls
        private Image _surrenderButton;
        #endregion

        #region Textures
        private Texture2D _surrenderTooltip_texture;
        private Texture2D _surrenderFlag_hover;
        private Texture2D _surrenderFlag;
        private Texture2D _surrenderFlag_pressed;
        #endregion

        #region Settings
        private SettingEntry<bool> SurrenderButtonEnabled;
        private SettingEntry<KeyBinding> SurrenderBinding;
        #endregion

        private DateTime _lastSurrenderTime;
        private int _cooldown; //in milliseconds

        protected override void Initialize() {
            LoadTextures();

            _lastSurrenderTime = DateTime.Now;
            _cooldown = 2000;

            _surrenderButton = SurrenderButtonEnabled.Value ? BuildSurrenderButton() : null;
        }


        private void LoadTextures() {
            _surrenderTooltip_texture = ContentsManager.GetTexture("surrender_tooltip.png");
            _surrenderFlag = ContentsManager.GetTexture("surrender_flag.png");
            _surrenderFlag_hover = ContentsManager.GetTexture("surrender_flag_hover.png");
            _surrenderFlag_pressed = ContentsManager.GetTexture("surrender_flag_pressed.png");
        }


        protected override void OnModuleLoaded(EventArgs e) {
            SurrenderBinding.Value.Enabled = true;
            SurrenderBinding.Value.Activated += OnSurrenderBindingActivated;
            SurrenderButtonEnabled.SettingChanged += OnSurrenderButtonEnabledSettingChanged;
            GameService.Gw2Mumble.UI.IsMapOpenChanged += OnIsMapOpenChanged;
            GameService.Gw2Mumble.IsAvailableChanged += OnIsAvailableChanged;
            // Base handler must be called
            base.OnModuleLoaded(e);
        }


        protected override void Update(GameTime gameTime) {
            if (_surrenderButton != null)
            {
                _surrenderButton.Visible = GameService.GameIntegration.IsInGame;
                _surrenderButton.Location =
                    new Point(GameService.Graphics.SpriteScreen.Width / 2 - _surrenderButton.Width / 2 + 431,
                        GameService.Graphics.SpriteScreen.Height - _surrenderButton.Height * 2 + 7);
            }
        }


        /// <inheritdoc />
        protected override void Unload() {
            SurrenderBinding.Value.Activated -= OnSurrenderBindingActivated;
            SurrenderButtonEnabled.SettingChanged -= OnSurrenderButtonEnabledSettingChanged;
            GameService.Gw2Mumble.UI.IsMapOpenChanged -= OnIsMapOpenChanged;
            GameService.Gw2Mumble.IsAvailableChanged -= OnIsAvailableChanged;
            _surrenderButton?.Dispose();
            // All static members must be manually unset
            ModuleInstance = null;
        }


        private void DoSurrender() {
            if (DateTimeOffset.Now.Subtract(_lastSurrenderTime).TotalMilliseconds < _cooldown) {
                ScreenNotification.ShowNotification("Skill recharging.", ScreenNotification.NotificationType.Error);
                return;
            }
            GameService.GameIntegration.Chat.Send("/gg");
            _lastSurrenderTime = DateTime.Now;
        }


        private void OnIsAvailableChanged(object o, ValueEventArgs<bool> e) {
            _surrenderButton.Visible = e.Value;
        }


        private void OnIsMapOpenChanged(object o, ValueEventArgs<bool> e) {
            _surrenderButton.Visible = e.Value;
        }


        private void OnSurrenderBindingActivated(object o, EventArgs e) {
            DoSurrender();
        }


        private void OnSurrenderButtonEnabledSettingChanged(object o, ValueChangedEventArgs<bool> e) {
            if (e.NewValue) {
                _surrenderButton?.Dispose();
                _surrenderButton = BuildSurrenderButton();
            } else
                GameService.Animation.Tweener.Tween(_surrenderButton, new {Opacity = 0.0f}, 0.2f).OnComplete(() => _surrenderButton?.Dispose());
        }


        private Image BuildSurrenderButton()
        {
            var tooltip_size = new Point(_surrenderTooltip_texture.Width, _surrenderTooltip_texture.Height);
            var surrenderButtonTooltip = new Tooltip
            {
                Size = tooltip_size
            };
            var surrenderButtonTooltipImage = new Image(_surrenderTooltip_texture)
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
                Texture = _surrenderFlag,
                Visible = SurrenderButtonEnabled.Value,
                Tooltip = surrenderButtonTooltip
            };
            surrenderButton.MouseEntered += delegate { surrenderButton.Texture = _surrenderFlag_hover; };
            surrenderButton.MouseLeft += delegate { surrenderButton.Texture = _surrenderFlag; };

            surrenderButton.LeftMouseButtonPressed += delegate
            {
                surrenderButton.Size = new Point(43, 43);
                surrenderButton.Texture = _surrenderFlag_pressed;
            };

            surrenderButton.LeftMouseButtonReleased += delegate
            {
                surrenderButton.Size = new Point(45, 45);
                surrenderButton.Texture = _surrenderFlag;
            };

            surrenderButton.Click += OnSurrenderBindingActivated;

            GameService.Animation.Tweener.Tween(surrenderButton, new {Opacity = 1.0f}, 0.35f);
            return surrenderButton;
        }
    }

}
