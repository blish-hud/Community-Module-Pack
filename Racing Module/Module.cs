using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Bh.Racing.Controls;
using Blish_HUD;
using Blish_HUD.Debug;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Gw2Sharp.WebApi.Http;
using Gw2Sharp.WebApi.V2;
using Microsoft.Xna.Framework;

namespace Bh.Racing {

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class Module : Blish_HUD.Modules.Module {

        private static readonly Logger Logger = Logger.GetLogger(typeof(Module));

        internal static Module ModuleInstance;

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        private SettingEntry<bool> _settingOnlyShowAtHighSpeeds;
        private SettingEntry<bool> _settingShowSpeedNumber;

        private Speedometer _speedometer;

        [ImportingConstructor]
        public Module([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { ModuleInstance = this; }

        protected override void DefineSettings(SettingCollection settings) {
            _settingOnlyShowAtHighSpeeds = settings.DefineSetting("OnlyShowAtHighSpeeds", false, "Only Show at High Speeds", "Only show the speedometer if you're going at least 1/4 the max speed.");
            _settingShowSpeedNumber      = settings.DefineSetting("ShowSpeedNumber",      false, "Show Speed Value",         "Shows the speed (in approx. inches per second) above the speedometer.");
        }

        protected override void Initialize() {
            
        }

        protected override async Task LoadAsync() { /* NOOP */ }

        protected override void OnModuleLoaded(EventArgs e) {
            _speedometer = new Speedometer() {
                Parent = GameService.Graphics.SpriteScreen,
                Speed = 0
            };

            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        private          Vector3       _lastPos      = Vector3.Zero;
        private          long          _lastUpdate   = 0;
        private          double        _leftOverTime = 0;
        private readonly Queue<double> _sampleBuffer = new Queue<double>();

        protected override void Update(GameTime gameTime) {
            // Unless we're in game running around, don't show the speedometer
            if (!GameService.GameIntegration.IsInGame) {
                _speedometer.Visible = false;
                _lastPos = Vector3.Zero;
                _sampleBuffer.Clear();
                return;
            }

            _leftOverTime += gameTime.ElapsedGameTime.TotalSeconds;

            // TODO: Ignore same tick for speed updates
            if (_lastPos != Vector3.Zero && _lastUpdate != GameService.Gw2Mumble.Tick) {
                double velocity = Vector3.Distance(GameService.Gw2Mumble.PlayerCharacter.Position, _lastPos) * 39.3700787f / _leftOverTime;
                _leftOverTime = 0;

                // TODO: Make the sample buffer a setting
                if (_sampleBuffer.Count > 50) {
                    double sped = _sampleBuffer.Average(i => i);

                    _speedometer.Speed = (float)Math.Round(sped, 1);

                    _speedometer.Visible = !_settingOnlyShowAtHighSpeeds.Value || _speedometer.Speed / _speedometer.MaxSpeed >= 0.25;
                    _speedometer.ShowSpeedValue = _settingShowSpeedNumber.Value;

                    _sampleBuffer.Dequeue();
                }

                _sampleBuffer.Enqueue(velocity);
            }

            _lastPos = GameService.Gw2Mumble.PlayerCharacter.Position;
            _lastUpdate = GameService.Gw2Mumble.Tick;
        }

        /// <inheritdoc />
        protected override async void Unload() {
            // Unload
            await GameService.Gw2WebApi.AnonymousConnection.Connection.CacheMethod.ClearAsync();

            // All static members must be manually unset
            ModuleInstance = null;
        }

    }

}
