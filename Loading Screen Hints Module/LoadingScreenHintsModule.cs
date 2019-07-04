using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Loading_Screen_Hints_Module.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Loading_Screen_Hints_Module {

    [Export(typeof(Module))]
    public class LoadingScreenHintsModule : Module {

        internal static LoadingScreenHintsModule ModuleInstance;

        // Service Managers
        internal SettingsManager    SettingsManager    => this.ModuleParameters.SettingsManager;
        internal ContentsManager    ContentsManager    => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager      Gw2ApiManager      => this.ModuleParameters.Gw2ApiManager;
        
        // Controls
        private LoadScreenPanel LoadScreenPanel;

        [ImportingConstructor]
        public LoadingScreenHintsModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) {
            ModuleInstance = this;
        }

        protected override void DefineSettings(SettingCollection settings) {
            
        }

        protected override void Initialize() { /* NOOP */ }

        protected override async Task LoadAsync() {
            LoadScreenPanel = new LoadScreenPanel() {Opacity = 0f};
        }

        protected override void OnModuleLoaded(EventArgs e) {
            LoadScreenPanel.Parent = GameService.Graphics.SpriteScreen;

            base.OnModuleLoaded(e);
        }

        protected override void Update(GameTime gameTime) {
            if (!GameService.GameIntegration.IsInGame) {
                if (LoadScreenPanel.Fade != null) {
                    LoadScreenPanel.Fade.Cancel();
                    LoadScreenPanel.Fade = null;
                    LoadScreenPanel.NextHint();
                }

                LoadScreenPanel.Opacity = 1.0f;
                LoadScreenPanel.Visible = true;

                return;
            }

            if (LoadScreenPanel.Fade == null) { LoadScreenPanel.FadeOut(); }
        }

        protected override void Unload() {
            ModuleInstance = null;

            LoadScreenPanel.Dispose();
        }

    }

}
