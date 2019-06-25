using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using DiscordRPC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Discord_Rich_Presence_Module {

    [Export(typeof(Module))]
    public class Events_Module : Module {


        private const string DISCORD_APP_ID = "498585183792922677";

        private DiscordRpcClient rpcClient;

        private DateTime startTime;

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

        [ImportingConstructor]
        public Events_Module([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { /* NOOP */ }

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
            GameService.GameIntegration.OnGw2Started += delegate { InitRichPresence(); };

            // Clear presence when the game is closed
            GameService.GameIntegration.OnGw2Closed += delegate { CleanUpRichPresence(); };

            InitRichPresence();
        }

        protected override void OnModuleLoaded(EventArgs e) {


            base.OnModuleLoaded(e);
        }

        private void UpdateDetails() {
            if (GameService.Player.Map == null) return;

            Console.WriteLine($"Player changed maps to '{GameService.Player.Map.Name}' ({GameService.Player.Map.Id}).");

            // rpcClient *shouldn't* be null at this point unless a rare race condition occurs
            // In the event that this occurs, it'll be resolved by the next loop
            rpcClient?.SetPresence(new RichPresence() {
                // Truncate length (requirements: https://discordapp.com/developers/docs/rich-presence/how-to)
                // Identified in: [BLISHHUD-11]
                Details = DiscordUtil.TruncateLength(GameService.Player.CharacterName, 128),
                State = DiscordUtil.TruncateLength($"in {GameService.Player.Map.Name}", 128),
                Assets = new Assets() {
                    LargeImageKey  = DiscordUtil.TruncateLength(_mapOverrides.ContainsKey(GameService.Player.Map.Id.ToString()) ? _mapOverrides[GameService.Player.Map.Id.ToString()] : DiscordUtil.GetDiscordSafeString(GameService.Player.Map.Name), 32),
                    LargeImageText = DiscordUtil.TruncateLength(GameService.Player.Map.Name,                                        128),
                    SmallImageKey  = DiscordUtil.TruncateLength(((MapType)GameService.Player.MapType).ToString().ToLower(),         32),
                    SmallImageText = DiscordUtil.TruncateLength(((MapType)GameService.Player.MapType).ToString().Replace("_", " "), 128)
                },
                Timestamps = new Timestamps() {
                    Start = startTime
                }
            });

            rpcClient?.Invoke();
        }

        private void InitRichPresence() {
            try {
                startTime = GameService.GameIntegration.Gw2Process.StartTime.ToUniversalTime();
            } catch (Exception ex) {
                // TODO: Make a log entry here
                startTime = DateTime.UtcNow;
            }

            rpcClient = new DiscordRpcClient(DISCORD_APP_ID);
            rpcClient.Initialize();

            UpdateDetails();
        }

        private void CleanUpRichPresence() {
            // Disposing rpcClient also clears presence
            rpcClient?.Dispose();
            rpcClient = null;
        }

        protected override void Update(GameTime gameTime) {

        }

        protected override void Unload() {
            CleanUpRichPresence();
        }

    }

}
