using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;

namespace Compass_Module {

    [Export(typeof(Module))]
    public class CompassModule : Module {

        internal static CompassModule ModuleInstance;

        // Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;

        private SettingEntry<float> _settingCompassSize;
        private SettingEntry<float> _settingCompassRadius;
        private SettingEntry<float> _settingVerticalOffset;
        private SettingEntry<bool>  _settingFadeForwardDirection;

        private CompassBillboard _northBb;
        private CompassBillboard _eastBb;
        private CompassBillboard _southBb;
        private CompassBillboard _westBb;

        [ImportingConstructor]
        public CompassModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) {
            ModuleInstance = this;
        }

        private const float VERTICALOFFSET_MIDDLE = 2.5f;

        protected override void DefineSettings(SettingCollection settings) {
            _settingCompassSize          = settings.DefineSetting("CompassSize",          0.5f,                  () => "Compass Size",           () => "Size of the compass elements.");
            _settingCompassRadius        = settings.DefineSetting("CompassRadius",        0f,                    () => "Compass Radius",         () => "Radius of the compass.");
            _settingVerticalOffset       = settings.DefineSetting("VerticalOffset",       VERTICALOFFSET_MIDDLE, () => "Vertical Offset",        () => "How high to offset the compass off the ground.");
            _settingFadeForwardDirection = settings.DefineSetting("FadeForwardDirection", true,                  () => "Fade Forward Direction", () => "If enabled, the direction in front of the character is faded out.");

            _settingCompassSize.SetRange(0.1f, 2f);
            _settingCompassRadius.SetRange(0f, 4f);
            _settingVerticalOffset.SetRange(0f, VERTICALOFFSET_MIDDLE * 2);
        }

        protected override Task LoadAsync() {
            return Task.CompletedTask;
        }

        protected override void OnModuleLoaded(EventArgs e) {
            _northBb = new CompassBillboard(ContentsManager.GetTexture("north.png"));
            _eastBb = new CompassBillboard(ContentsManager.GetTexture("east.png"));
            _southBb = new CompassBillboard(ContentsManager.GetTexture("south.png"));
            _westBb = new CompassBillboard(ContentsManager.GetTexture("west.png"));

            GameService.Graphics.World.AddEntity(_northBb);
            GameService.Graphics.World.AddEntity(_eastBb);
            GameService.Graphics.World.AddEntity(_southBb);
            GameService.Graphics.World.AddEntity(_westBb);

            base.OnModuleLoaded(e);
        }

        protected override void Update(GameTime gameTime) {
            UpdateBillboardSize();
            UpdateBillboardOffset();
            UpdateBillboardOpacity();
        }

        private void UpdateBillboardSize() {
            _northBb.Scale = _settingCompassSize.Value;
            _eastBb.Scale  = _settingCompassSize.Value;
            _southBb.Scale = _settingCompassSize.Value;
            _westBb.Scale  = _settingCompassSize.Value;
        }

        private void UpdateBillboardOffset() {
            _northBb.Offset = new Vector3(0, 1 + _settingCompassRadius.Value, _settingVerticalOffset.Value - VERTICALOFFSET_MIDDLE);
            _eastBb.Offset = new Vector3(1 + _settingCompassRadius.Value, 0, _settingVerticalOffset.Value - VERTICALOFFSET_MIDDLE);
            _southBb.Offset = new Vector3(0, -1 - _settingCompassRadius.Value, _settingVerticalOffset.Value - VERTICALOFFSET_MIDDLE);
            _westBb.Offset = new Vector3(-1 - _settingCompassRadius.Value, 0, _settingVerticalOffset.Value - VERTICALOFFSET_MIDDLE);
        }

        private void UpdateBillboardOpacity() {
            if (_settingFadeForwardDirection.Value) {
                _northBb.Opacity = Math.Min(1 - GameService.Gw2Mumble.PlayerCamera.Forward.Y, 1f);
                _eastBb.Opacity  = Math.Min(1 - GameService.Gw2Mumble.PlayerCamera.Forward.X, 1f);
                _southBb.Opacity = Math.Min(1 + GameService.Gw2Mumble.PlayerCamera.Forward.Y, 1f);
                _westBb.Opacity  = Math.Min(1 + GameService.Gw2Mumble.PlayerCamera.Forward.X, 1f);
            } else {
                _northBb.Opacity = 1f;
                _eastBb.Opacity  = 1f;
                _southBb.Opacity = 1f;
                _westBb.Opacity  = 1f;
            }
        }

        /// <inheritdoc />
        protected override void Unload() {
            ModuleInstance = null;

            GameService.Graphics.World.RemoveEntity(_northBb);
            GameService.Graphics.World.RemoveEntity(_eastBb);
            GameService.Graphics.World.RemoveEntity(_southBb);
            GameService.Graphics.World.RemoveEntity(_westBb);
        }

    }

}
