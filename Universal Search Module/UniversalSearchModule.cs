using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Universal_Search_Module.Controls;

namespace Universal_Search_Module {

    [Export(typeof(Module))]
    public class UniversalSearchModule : Module {

        private static readonly Logger Logger = Logger.GetLogger(typeof(UniversalSearchModule));

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

        // API Loaded Info
        internal HashSet<ContinentFloorRegionMapPoi>            LoadedLandmarks;
        internal HashSet<ContinentFloorRegionMapSkillChallenge> HeroPoints;
        internal HashSet<ContinentFloorRegionMapMasteryPoint>   MasteryPoints;
        internal HashSet<ContinentFloorRegionMapTask>           HeroHearts;
        internal HashSet<ContinentFloorRegionMapSector>         Areas;

        protected override void DefineSettings(SettingCollection settingsManager) {
            _settingShowNotificationWhenLandmarkIsCopied = settingsManager.DefineSetting("ShowNotificationOnCopy", true, "Show Notification When Landmark is Copied", "If checked, a notification will be displayed in the center of the screen confirming the landmark was copied.");
            _settingHideWindowAfterSelection             = settingsManager.DefineSetting("HideWindowOnSelection",  true, "Hide Window After Selection",               "If checked, the landmark search window will automatically hide after a landmark is selected from the results.");
        }

        protected override void Initialize() {
            LoadedLandmarks = new HashSet<ContinentFloorRegionMapPoi>();
            HeroPoints      = new HashSet<ContinentFloorRegionMapSkillChallenge>();
            MasteryPoints   = new HashSet<ContinentFloorRegionMapMasteryPoint>();
            HeroHearts      = new HashSet<ContinentFloorRegionMapTask>();
            Areas           = new HashSet<ContinentFloorRegionMapSector>();

            _searchWindow = new SearchWindow() {
                Location      = GameService.Graphics.SpriteScreen.Size / new Point(2) - new Point(256, 178) / new Point(2),
                Parent        = GameService.Graphics.SpriteScreen,
                Id            = $"{nameof(SearchWindow)}_{nameof(UniversalSearchModule)}_090afc97-559c-4f1d-8196-0b77f5d0a9c9",
                SavesPosition = true
            };
        }

        protected override async Task LoadAsync() {
            _searchIcon = new CornerIcon() {
                IconName  = "Landmark Search",
                Icon      = ContentsManager.GetTexture(@"textures\landmark-search.png"),
                HoverIcon = ContentsManager.GetTexture(@"textures\landmark-search-hover.png"),
                Priority  = 5
            };
            
            // Continent 1 = Tyria
            // Continent 2 = Mists
            // Fetching a single floor will return all nested subresources as well, so fetch all floors
            var floors = await Gw2ApiManager.Gw2ApiClient.V2.Continents[1].Floors.IdsAsync();

            foreach (var floorId in floors) {
                _searchIcon.LoadingMessage = $"Loading floor {floorId}...";
                var floor = await Gw2ApiManager.Gw2ApiClient.V2.Continents[1].Floors[floorId].GetAsync();
                foreach (var regionPair in floor.Regions) {
                    foreach (var mapPair in regionPair.Value.Maps) {
                        LoadedLandmarks.UnionWith(mapPair.Value.PointsOfInterest.Values.Where(landmark => landmark.Name != null));
                        HeroPoints.UnionWith(mapPair.Value.SkillChallenges);
                        MasteryPoints.UnionWith(mapPair.Value.MasteryPoints);
                        HeroHearts.UnionWith(mapPair.Value.Tasks.Values);
                        Areas.UnionWith(mapPair.Value.Sectors.Values);
                    }
                }
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
