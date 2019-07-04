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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Universal_Search_Module.Controls;

namespace Universal_Search_Module {

    [Export(typeof(Module))]
    public class UniversalSearchModule : Module {

        internal static UniversalSearchModule ModuleInstance;

        // Service Managers
        internal SettingsManager    SettingsManager    => this.ModuleParameters.SettingsManager;
        internal ContentsManager    ContentsManager    => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager      Gw2ApiManager      => this.ModuleParameters.Gw2ApiManager;

        [ImportingConstructor]
        public UniversalSearchModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) {
            ModuleInstance = this;
        }

        internal SettingEntry<bool> _settingShowNotificationWhenLandmarkIsCopied;
        internal SettingEntry<bool> _settingHideWindowAfterSelection;
        internal SettingEntry<bool> _settingEnterSelectionIntoChatAutomatically;

        // Controls
        private SearchWindow _searchWindow;
        private CornerIcon   _searchIcon;

        protected override void DefineSettings(SettingCollection settingsManager) {
            _settingShowNotificationWhenLandmarkIsCopied = settingsManager.DefineSetting("ShowNotificationOnCopy", true, "Show Notification When Landmark is Copied", "If checked, a notification will be displayed in the center of the screen confirming the landmark was copied.");
            _settingHideWindowAfterSelection             = settingsManager.DefineSetting("HideWindowOnSelection",  true, "Hide Window After Selection",               "If checked, the landmark search window will automatically hide after a landmark is selected from the results.");
        }

        protected override void Initialize() {
            _searchWindow = new SearchWindow() {
                Location = GameService.Graphics.SpriteScreen.Size / new Point(2) - _searchWindow.Size / new Point(2),
                Parent   = GameService.Graphics.SpriteScreen
            };
        }

        protected override async Task LoadAsync() {
            _searchIcon = new CornerIcon() {
                Icon             = ContentsManager.GetTexture(@"textures\landmark-search"),
                HoverIcon        = ContentsManager.GetTexture(@"textures\landmark-search-hover"),
                BasicTooltipText = "Landmark Search",
                Priority         = 5
            };

            _searchIcon.Click += delegate { _searchWindow.ToggleWindow(); };

            var continents = await Gw2ApiManager.Gw2ApiClient.Continents.AllAsync();
        }

        protected override void OnModuleLoaded(EventArgs e) {
            

            base.OnModuleLoaded(e);
        }

        protected override void Update(GameTime gameTime) {
            
        }

        protected override void Unload() {
            _searchWindow.Dispose();
            _searchIcon.Dispose();

            ModuleInstance = null;
        }

    }

}
