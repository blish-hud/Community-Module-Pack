using Blish_HUD;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using DiscordRPC;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Discord_Rich_Presence_Module.Utils;
namespace Discord_Rich_Presence_Module
{

    [Export(typeof(Module))]
    public class DiscordRichPresenceModule : Module {

        private static readonly Logger Logger = Logger.GetLogger(typeof(DiscordRichPresenceModule));

        internal static DiscordRichPresenceModule ModuleInstance;

        // Service Managers
        internal SettingsManager    SettingsManager    => this.ModuleParameters.SettingsManager;
        internal ContentsManager    ContentsManager    => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager      Gw2ApiManager      => this.ModuleParameters.Gw2ApiManager;

        private const string DISCORD_APP_ID = "498585183792922677";

        private enum MapType {
            PvP                   = 2,
            Instance              = 4,
            PvE                   = 5,
            Eternal_Battlegrounds = 9,
            WvW_Blue              = 10,
            WvW_Green             = 11,
            WvW_Red               = 12,
            Edge_of_The_Mists     = 15,
            Dry_Top               = 16,
            Armistice_Bastion     = 18
        }

        private readonly Dictionary<string, string> _mapOverrides = new Dictionary<string, string>() {
            { "1206", "fractals_of_the_mists" }, // Mistlock Sanctuary
            { "350",  "fractals_of_the_mists" }, // Heart of the Mists
            { "95",   "eternal_battlegrounds" }, // Alpine Borderlands
            { "96",   "eternal_battlegrounds" }, // Alpine Borderlands
        };

        private readonly Dictionary<int, string> _contextOverrides = new Dictionary<int, string>() {

        };

        private DiscordRpcClient _rpcClient;
        private DateTime         _startTime;

        [ImportingConstructor]
        public DiscordRichPresenceModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) {
            ModuleInstance = this;
        }

        protected override void DefineSettings(SettingCollection settings) {
            //settings.DefineSetting("HideInWvW", false, "Hide Detailed Location while in WvW", "Prevents people on Discord from being able to see closest landmark details while you're in WvW.");
        }

        protected override void Initialize() { /* NOOP */ }

        protected override async Task LoadAsync() {
            // Update character name
            GameService.Gw2Mumble.PlayerCharacter.NameChanged += delegate { CurrentMapOnMapChanged(null, new ValueEventArgs<int>(GameService.Gw2Mumble.CurrentMap.Id)); };

            // Update map
            GameService.Gw2Mumble.CurrentMap.MapChanged += CurrentMapOnMapChanged;

            // Initiate presence when the game is opened
            GameService.GameIntegration.Gw2Started += delegate { InitRichPresence(); };

            // Clear presence when the game is closed
            GameService.GameIntegration.Gw2Closed += delegate { CleanUpRichPresence(); };

            InitRichPresence();
        }

        private void CurrentMapOnMapChanged(object sender, ValueEventArgs<int> e) {
            // Stop-gap until we switch this over to the other Discord app where
            // map names are added to Discord via map ID instead of by name
            Gw2ApiManager.Gw2ApiClient.V2.Maps.GetAsync(e.Value)
                         .ContinueWith(mapTask =>
                         {
                             if (mapTask.IsFaulted || !mapTask.IsCompleted)
                                 return;
                             var map = mapTask.Result;
                             UpdateDetails(map);
                         });
        }

        private async Task<IEnumerable<ContinentFloorRegionMapSector>> RequestSectors(int continentId, int floor, int regionId, int mapId)
        {
            try
            {
                return await Gw2ApiManager.Gw2ApiClient.V2.Continents[continentId].Floors[floor].Regions[regionId].Maps[mapId].Sectors.AllAsync();
            }
            catch (Gw2Sharp.WebApi.Exceptions.BadRequestException e)
            {
                Logger.Debug("{0} | The map id {1} does not exist on floor {2}.", e.GetType().FullName, mapId, floor);
                return Enumerable.Empty<ContinentFloorRegionMapSector>();
            }
        }

        private async void UpdateDetails(Map map) {
            if (map.Id <= 0) return;

            var location = map.Name;

            //Some instanced maps consist of just a single sector and hide their display name in it.
            if (map.Name.Equals(map.RegionName, StringComparison.InvariantCultureIgnoreCase))
            {
                var defaultSector = (await RequestSectors(map.ContinentId, map.DefaultFloor, map.RegionId, map.Id)).FirstOrDefault();
                if (defaultSector != null && !string.IsNullOrEmpty(defaultSector.Name))
                    location = defaultSector.Name.Replace("<br>", " ");
            }

            // rpcClient *shouldn't* be null at this point unless a rare race condition occurs
            // In the event that this occurs, it'll be resolved by the next loop
            _rpcClient?.SetPresence(new RichPresence() {
                // Truncate length (requirements: https://discordapp.com/developers/docs/rich-presence/how-to)
                Details = DiscordUtil.TruncateLength(GameService.Gw2Mumble.PlayerCharacter.Name, 128),
                State   = DiscordUtil.TruncateLength($"in {location}", 128),
                Assets = new Assets() {
                    LargeImageKey = DiscordUtil.TruncateLength(_mapOverrides.ContainsKey(map.Id.ToString())
                                                                   ? _mapOverrides[map.Id.ToString()]
                                                                   : DiscordUtil.GetDiscordSafeString(location), 32),
                    LargeImageText = DiscordUtil.TruncateLength(location,                                                128),
                    SmallImageKey  = DiscordUtil.TruncateLength(((MapType)map.Type.Value).ToString().ToLowerInvariant(), 32),
                    SmallImageText = DiscordUtil.TruncateLength(((MapType)map.Type.Value).ToString().Replace("_", " "),  128)
                },
                Timestamps = new Timestamps() {
                    Start = _startTime
                }
            });

            _rpcClient?.Invoke();
        }

        private void InitRichPresence() {
            try {
                _startTime = GameService.GameIntegration.Gw2Process.StartTime.ToUniversalTime();
            } catch (Exception ex) {
                // TODO: Make a log entry here
                _startTime = DateTime.UtcNow;
            }

            _rpcClient = new DiscordRpcClient(DISCORD_APP_ID);
            _rpcClient.Initialize();

            CurrentMapOnMapChanged(null, new ValueEventArgs<int>(GameService.Gw2Mumble.CurrentMap.Id));
        }

        private void CleanUpRichPresence() {
            // Disposing _rpcClient also clears presence
            _rpcClient?.Dispose();
            _rpcClient = null;
        }

        protected override void Update(GameTime gameTime) { /* NOOP */ }

        protected override void Unload() {
            ModuleInstance = null;

            CleanUpRichPresence();
        }

    }

}
