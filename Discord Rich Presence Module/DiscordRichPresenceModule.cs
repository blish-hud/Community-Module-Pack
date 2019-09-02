using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using DiscordRPC;
using Microsoft.Xna.Framework;

namespace Discord_Rich_Presence_Module {

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
            settings.DefineSetting("HideInWvW", false, "Hide Detailed Location while in WvW", "Prevents people on Discord from being able to see closest landmark details while you're in WvW.");
        }

        protected override void Initialize() {

        }

        protected override async Task LoadAsync() {
            // Update character name
            GameService.Player.CharacterNameChanged += delegate { UpdateDetails(); };

            // Update map
            GameService.Player.MapChanged += delegate { UpdateDetails(); };

            // Initiate presence when the game is opened
            GameService.GameIntegration.Gw2Started += delegate { InitRichPresence(); };

            // Clear presence when the game is closed
            GameService.GameIntegration.Gw2Closed += delegate { CleanUpRichPresence(); };

            InitRichPresence();
        }

        private void UpdateDetails() {
            if (GameService.Player.Map == null) return;

            Logger.Debug($"Player changed maps to '{GameService.Player.Map.Name}' ({GameService.Player.Map.Id}).");

            // rpcClient *shouldn't* be null at this point unless a rare race condition occurs
            // In the event that this occurs, it'll be resolved by the next loop
            _rpcClient?.SetPresence(new RichPresence() {
                // Truncate length (requirements: https://discordapp.com/developers/docs/rich-presence/how-to)
                Details = DiscordUtil.TruncateLength(GameService.Player.CharacterName,    128),
                State   = DiscordUtil.TruncateLength($"in {GameService.Player.Map.Name}", 128),
                Assets = new Assets() {
                    LargeImageKey = DiscordUtil.TruncateLength(_mapOverrides.ContainsKey(GameService.Player.Map.Id.ToString())
                                                                   ? _mapOverrides[GameService.Player.Map.Id.ToString()]
                                                                   : DiscordUtil.GetDiscordSafeString(GameService.Player.Map.Name), 32),
                    LargeImageText = DiscordUtil.TruncateLength(GameService.Player.Map.Name,                                         128),
                    SmallImageKey  = DiscordUtil.TruncateLength(((MapType) GameService.Player.MapType).ToString().ToLowerInvariant(),         32),
                    SmallImageText = DiscordUtil.TruncateLength(((MapType) GameService.Player.MapType).ToString().Replace("_", " "), 128)
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

            UpdateDetails();
        }

        private void CleanUpRichPresence() {
            // Disposing _rpcClient also clears presence
            _rpcClient?.Dispose();
            _rpcClient = null;
        }

        protected override void Update(GameTime gameTime) {
            /* NOOP */
        }

        protected override void Unload() {
            ModuleInstance = null;

            CleanUpRichPresence();
        }

    }

}
