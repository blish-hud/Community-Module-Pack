﻿using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Universal_Search_Module.Controls;
using Universal_Search_Module.Services.SearchHandler;

namespace Universal_Search_Module {

    [Export(typeof(Module))]
    public class UniversalSearchModule : Module {
        private static readonly Logger Logger = Logger.GetLogger(typeof(UniversalSearchModule));

        private readonly List<SearchHandler> _searchHandlers = new List<SearchHandler>();

        private SearchWindow _searchWindow;
        private CornerIcon _searchIcon;

        internal static UniversalSearchModule ModuleInstance;

        // Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;

        [ImportingConstructor]
        public UniversalSearchModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) {
            ModuleInstance = this;
        }

        internal SettingEntry<bool> SettingShowNotificationWhenLandmarkIsCopied;
        internal SettingEntry<bool> SettingHideWindowAfterSelection;
        internal SettingEntry<bool> SettingEnterSelectionIntoChatAutomatically;

        protected override void DefineSettings(SettingCollection settingsManager) {
            SettingShowNotificationWhenLandmarkIsCopied = settingsManager.DefineSetting("ShowNotificationOnCopy", true, () => Strings.Common.Settings_NotificationAfterCopy_Title, () => Strings.Common.Settings_NotificationAfterCopy_Text);
            SettingHideWindowAfterSelection = settingsManager.DefineSetting("HideWindowOnSelection", true, () => Strings.Common.Settings_HideWindowAfterSelection_Title, () => Strings.Common.Settings_HideWindowAfterSelection_Text);
            SettingEnterSelectionIntoChatAutomatically = settingsManager.DefineSetting("EnterSelectionIntoChat", false, () => Strings.Common.Settings_EnterSelectionIntoChat_Title, () => Strings.Common.Settings_EnterSelectionIntoChat_Text);
        }

        protected override void Initialize() {
            _searchHandlers.AddRange(new SearchHandler[] {
                new LandmarkSearchHandler(Gw2ApiManager),
                new SkillSearchHandler(Gw2ApiManager),
                new TraitSearchHandler(Gw2ApiManager),
            });

            _searchWindow = new SearchWindow(_searchHandlers) {
                Location = GameService.Graphics.SpriteScreen.Size / new Point(2) - new Point(256, 178) / new Point(2),
                Parent = GameService.Graphics.SpriteScreen,
                Id = $"{nameof(SearchWindow)}_{nameof(UniversalSearchModule)}_090afc97-559c-4f1d-8196-0b77f5d0a9c9",
                SavesPosition = true
            };
        }

        protected override async Task LoadAsync() {
            _searchIcon = new CornerIcon() {
                IconName = Strings.Common.Icon_Title,
                Icon = ContentsManager.GetTexture(@"textures\landmark-search.png"),
                HoverIcon = ContentsManager.GetTexture(@"textures\landmark-search-hover.png"),
                Priority = 5
            };

            foreach (var searchHandler in _searchHandlers) {
                await searchHandler.Initialize(progress => _searchIcon.LoadingMessage = progress);
            }
            _searchIcon.LoadingMessage = null;


            _searchIcon.Click += delegate { _searchWindow.ToggleWindow(); };
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
