using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Blish_HUD.Modules.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Kill_Proof_Module.Controls;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Flurl;
using Flurl.Http;
using System.Linq;

namespace Kill_Proof_Module
{
    [Export(typeof(Module))]
    public class KillProofModule : Module
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(KillProofModule));

        internal static KillProofModule ModuleInstance;

        // Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;

        private Texture2D ICON;
        private const int TOP_MARGIN = 0;
        private const int RIGHT_MARGIN = 5;
        private const int BOTTOM_MARGIN = 10;
        private const int LEFT_MARGIN = 8;
        private Point LABEL_SMALL = new Point(400, 30);
        private Point LABEL_BIG = new Point(400, 40);

        private const string DD_ALL = "KP to Titles";
        private const string DD_RAID = "Raid Titles";
        private const string DD_FRACTAL = "Fractal Titles";
        private const string DD_KILLPROOF = "Kill Proofs";

        private const string KILLPROOF_API_URL = "https://killproof.me/api/";

        private WindowTab KillProofTab;

        private Dictionary<string, Texture2D> TokenRenderRepository;
        private List<KillProof> CachedKillProofs;
        private Label CurrentAccountName;
        private Label CurrentAccountLastRefresh;
        private Label CurrentAccountKpId;
        private Label CurrentAccountProofUrl;
        private List<KillProofButton> DisplayedKillProofs;

        /// <summary>
        /// Ideally you should keep the constructor as is.
        /// Use <see cref="Initialize"/> to handle initializing the module.
        /// </summary>
        [ImportingConstructor]
        public KillProofModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
        {
            ModuleInstance = this;
        }

        /// <summary>
        /// Allows your module to perform any initialization it needs before starting to run.
        /// Please note that Initialize is NOT asynchronous and will block Blish HUD's update
        /// and render loop, so be sure to not do anything here that takes too long.
        /// </summary>
        protected override void Initialize()
        {
            ICON = ICON ?? ContentsManager.GetTexture("killproof_icon.png");
            TokenRenderRepository = new Dictionary<string, Texture2D>();
            DisplayedKillProofs = new List<KillProofButton>();
            CachedKillProofs = new List<KillProof>();
        }

        #region Settings

        /// <summary>
        /// Define the settings you would like to use in your module.  Settings are persistent
        /// between updates to both Blish HUD and your module.
        /// </summary>
        protected override void DefineSettings(SettingCollection settingsManager) { /** NOOP **/ }

        #endregion

        /// <summary>
        /// Load content and more here. This call is asynchronous, so it is a good time to
        /// run any long running steps for your module. Be careful when instancing
        /// <see cref="Blish_HUD.Entities.Entity"/> and <see cref="Blish_HUD.Controls.Control"/>.
        /// Setting their parent is not thread-safe and can cause the application to crash.
        /// You will want to queue them to add later while on the main thread or in a delegate queued
        /// with <see cref="Blish_HUD.DirectorService.QueueMainThreadUpdate(Action{GameTime})"/>.
        /// </summary>
        protected override async Task LoadAsync() {
            try
            {
                var rawJson = await (KILLPROOF_API_URL + "icons")
                .AllowAnyHttpStatus()
                .GetStringAsync();
                Dictionary<string, Url> tokenRenderUrlRepository = JsonConvert.DeserializeObject<Dictionary<string, Url>>(rawJson);

                foreach (KeyValuePair<string, Url> token in tokenRenderUrlRepository) {
                    Texture2D render = GameService.Content.GetRenderServiceTexture(token.Value);
                    TokenRenderRepository.Add(token.Key, render);
                }
            }
            catch (FlurlHttpException ex)
            {
                Logger.Warn(ex.Message);
            }
        }

        /// <summary>
        /// Allows you to perform an action once your module has finished loading (once
        /// <see cref="LoadAsync"/> has completed).  You must call "base.OnModuleLoaded(e)" at the
        /// end for the <see cref="ExternalModule.ModuleLoaded"/> event to fire and for
        /// <see cref="ExternalModule.Loaded" /> to update correctly.
        /// </summary>
        protected override void OnModuleLoaded(EventArgs e)
        {
            KillProofTab = GameService.Overlay.BlishHudWindow.AddTab("KillProof", ICON, BuildHomePanel(GameService.Overlay.BlishHudWindow), 0);
            base.OnModuleLoaded(e);
        }

        /// <summary>
        /// Allows your module to run logic such as updating UI elements,
        /// checking for conditions, playing audio, calculating changes, etc.
        /// This method will block the primary Blish HUD loop, so any long
        /// running tasks should be executed on a separate thread to prevent
        /// slowing down the overlay.
        /// </summary>
        protected override void Update(GameTime gameTime) { /** NOOP **/ }

        /// <summary>
        /// For a good module experience, your module should clean up ANY and ALL entities
        /// and controls that were created and added to either the World or SpriteScreen.
        /// Be sure to remove any tabs added to the Director window, CornerIcons, etc.
        /// </summary>
        protected override void Unload()
        {
            GameService.Overlay.BlishHudWindow.RemoveTab(KillProofTab);
            ModuleInstance = null;
        }

        private async Task<Texture2D> GetTokenRender(string key)
        {
            if (TokenRenderRepository.Any(x => x.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase)))
            {
                return TokenRenderRepository[key];
            }
            else
            {
                try
                {
                    var rawJson = await (KILLPROOF_API_URL + "icons")
                    .AllowAnyHttpStatus()
                    .GetStringAsync();
                    Dictionary<string, Url> tokenRenderUrlRepository = JsonConvert.DeserializeObject<Dictionary<string, Url>>(rawJson);

                    Url renderUrl = tokenRenderUrlRepository.First(x => x.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase)).Value;
                    Texture2D render = GameService.Content.GetRenderServiceTexture(renderUrl);
                    TokenRenderRepository.Add(key, render);

                    return render;
                }
                catch (FlurlHttpException ex)
                {
                    Logger.Warn(ex.Message);
                    return GameService.Content.GetTexture("deleted_item");
                }
            }
        }
        private async Task<string> GetKillProofContent(string _account)
        {
            try
            {
                var rawJson = await (KILLPROOF_API_URL + $"kp/{_account}")
                    .AllowAnyHttpStatus()
                    .GetStringAsync();
                return rawJson;
            }
            catch (FlurlHttpException ex)
            {
                Logger.Warn(ex.Message);
                return "{'error':'API connection failed.'}";
            }

        }
        /*######################################
          # PANEL RELATED STUFF BELOW.
          ######################################*/
        private Panel BuildHomePanel(WindowBase wndw)
        {
            var hPanel = new Panel()
            {
                CanScroll = false,
                Size = wndw.ContentRegion.Size
            };

            /// ###################
            ///       HEADER
            /// ###################
            var header = new Panel()
            {
                Parent = hPanel,
                Size = new Point(hPanel.Width, 200),
                Location = new Point(0, 0),
                CanScroll = false
            };
            var img_killproof = new Image(GameService.Content.GetTexture("killproof_logo"))
            {
                Parent = header,
                Size = new Point(128, 128),
                Location = new Point(KillProofModule.LEFT_MARGIN, -25)
            };
            var lab_account_name = new Label()
            {
                Parent = header,
                Size = new Point(200, 30),
                Location = new Point(header.Width / 2 - 100, KillProofModule.TOP_MARGIN),
                Text = "Account Name or KillProof.me-ID:"
            };
            CurrentAccountName = new Label()
            {
                Parent = header,
                Size = LABEL_BIG,
                Location = new Point(KillProofModule.LEFT_MARGIN, img_killproof.Bottom + KillProofModule.BOTTOM_MARGIN),
                ShowShadow = true,
                Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size36, ContentService.FontStyle.Regular),
                Text = ""
            };
            CurrentAccountLastRefresh = new Label()
            {
                Parent = header,
                Size = LABEL_SMALL,
                Location = new Point(KillProofModule.LEFT_MARGIN, CurrentAccountName.Bottom + KillProofModule.BOTTOM_MARGIN),
                Text = ""
            };
            var ddSortMethod = new Dropdown()
            {
                Parent = header,
                Visible = header.Visible,
                Width = 150,
                Location = new Point(header.Right - 150 - KillProofModule.RIGHT_MARGIN, CurrentAccountLastRefresh.Location.Y)
            };
            ddSortMethod.Items.Add(DD_ALL);
            ddSortMethod.Items.Add(DD_KILLPROOF);
            ddSortMethod.Items.Add(DD_RAID);
            ddSortMethod.Items.Add(DD_FRACTAL);
            ddSortMethod.SelectedItem = DD_ALL;
            ddSortMethod.ValueChanged += UpdateSort;

            /// ###################
            ///       FOOTER
            /// ###################
            var footer = new Panel()
            {
                Parent = hPanel,
                Size = new Point(hPanel.Width, 50),
                Location = new Point(0, hPanel.Height - 50),
                CanScroll = false
            };
            CurrentAccountKpId = new Label()
            {
                Parent = footer,
                Size = LABEL_SMALL,
                HorizontalAlignment = HorizontalAlignment.Left,
                Location = new Point(KillProofModule.LEFT_MARGIN, (footer.Height / 2) - (LABEL_SMALL.Y / 2)),
                Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size8, ContentService.FontStyle.Regular),
                Text = ""
            };
            CurrentAccountProofUrl = new Label()
            {
                Parent = footer,
                Size = LABEL_SMALL,
                HorizontalAlignment = HorizontalAlignment.Left,
                Location = new Point(KillProofModule.LEFT_MARGIN, (footer.Height / 2) - (LABEL_SMALL.Y / 2) + KillProofModule.BOTTOM_MARGIN),
                Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size8, ContentService.FontStyle.Regular),
                Text = ""
            };
            var creditLabel = new Label()
            {
                Parent = footer,
                Size = LABEL_SMALL,
                HorizontalAlignment = HorizontalAlignment.Center,
                Location = new Point((footer.Width / 2) - (LABEL_SMALL.X / 2), (footer.Height / 2) - (LABEL_SMALL.Y / 2)),
                Text = @"Powered by www.killproof.me"
            };

            var contentPanel = new Panel()
            {
                Parent = hPanel,
                Size = new Point(header.Size.X, hPanel.Height - header.Height - footer.Height),
                Location = new Point(0, header.Bottom),
                ShowBorder = true,
                CanScroll = true,
                ShowTint = true
            };
            LoadingSpinner PageLoading = new LoadingSpinner()
            {
                Parent = hPanel,
                Visible = false
            };
            PageLoading.Location = new Point(hPanel.Size.X / 2 - PageLoading.Size.X / 2, hPanel.Size.Y / 2 - PageLoading.Size.Y / 2);

            // Encapsule TextBox because not thread safe.
            GameService.Overlay.QueueMainThreadUpdate((gameTime) => {
                var tb_account_name = new TextBox()
                {
                    Parent = header,
                    Size = new Point(200, 30),
                    Location = new Point(header.Width / 2 - 100, lab_account_name.Bottom + KillProofModule.BOTTOM_MARGIN),
                    PlaceholderText = "Player.0000",

                };
                tb_account_name.EnterPressed += delegate {
                    if (!string.Equals(tb_account_name.Text, "") && !Regex.IsMatch(tb_account_name.Text, @"[^a-zA-Z0-9.\s]|^\.*$"))
                    {
                        PageLoading.Visible = true;
                        BuildContentPanelElements(contentPanel, tb_account_name.Text);
                        PageLoading.Visible = false;
                        UpdateSort(ddSortMethod, EventArgs.Empty);
                    }
                };
            });
            return hPanel;
        }
        private Panel BuildContentPanelElements(Panel contentPanel, string account)
        {

            KillProof currentAccount;
            if (!CachedKillProofs.Any(x => x.account_name.Equals(account, StringComparison.InvariantCultureIgnoreCase)))
            {
                var loader = Task.Run(() => GetKillProofContent(account));
                loader.Wait();
                currentAccount = JsonConvert.DeserializeObject<KillProof>(loader.Result);
                CachedKillProofs.Add(currentAccount);
            }
            else
            {
                currentAccount = CachedKillProofs.FirstOrDefault(x => x.account_name.Equals(account, StringComparison.InvariantCultureIgnoreCase));

            }

            foreach (KillProofButton e1 in DisplayedKillProofs) { e1.Dispose(); }
            DisplayedKillProofs.Clear();

            if (currentAccount.error == null)
            {
                CurrentAccountName.Text = currentAccount.account_name;
                CurrentAccountLastRefresh.Text = "Last Refresh: " + String.Format("{0:dddd, d. MMMM yyyy - HH:mm:ss}", currentAccount.last_refresh);
                CurrentAccountKpId.Text = "ID: " + currentAccount.kpid;
                CurrentAccountProofUrl.Text = currentAccount.proof_url;

                var killproofs = DictionaryExtension.MergeLeft(currentAccount.killproofs, currentAccount.tokens);

                foreach (KeyValuePair<string, int> token in killproofs)
                {
                    if (token.Value > 0)
                    {
                        var killProofButton = new KillProofButton()
                        {
                            Parent = contentPanel,
                            Icon = GetTokenRender(token.Key).Result,
                            Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size24, ContentService.FontStyle.Regular),
                            Text = token.Value.ToString(),
                            BottomText = token.Key
                        };
                        DisplayedKillProofs.Add(killProofButton);
                    }
                }

                foreach (KeyValuePair<string, string> token in currentAccount.titles)
                {
                    var titleButton = new KillProofButton()
                    {
                        Parent = contentPanel,
                        Icon = GameService.Content.GetTexture("icon_" + token.Value),
                        Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size16, ContentService.FontStyle.Regular),
                        Text = token.Key,
                        BottomText = token.Value,
                        IsTitleDisplay = true
                    };
                    DisplayedKillProofs.Add(titleButton);
                }

                RepositionKp();
            }
            else
            {
                currentAccount.account_name = account; // So we can compare error 'Account not found' API replies and cache them.
                CurrentAccountName.Text = currentAccount.error;
                CurrentAccountLastRefresh.Text = "Last Refresh: ";
                CurrentAccountKpId.Text = "ID: ";
                CurrentAccountProofUrl.Text = @"https://killproof.me/proof/";
            }
            return contentPanel;
        }
        private void UpdateSort(object sender, EventArgs e)
        {
            switch (((Dropdown)sender).SelectedItem)
            {
                case DD_ALL:
                    DisplayedKillProofs.Sort((e1, e2) =>
                    {
                        int result = e1.IsTitleDisplay.CompareTo(e2.IsTitleDisplay);
                        if (result != 0) return result;
                        else return e1.BottomText.CompareTo(e2.BottomText);
                    });
                    foreach (KillProofButton e1 in DisplayedKillProofs) { e1.Visible = true; }
                    break;
                case DD_KILLPROOF:
                    DisplayedKillProofs.Sort((e1, e2) => e1.BottomText.CompareTo(e2.BottomText));
                    foreach (KillProofButton e1 in DisplayedKillProofs) { e1.Visible = !e1.IsTitleDisplay; }
                    break;
                case DD_FRACTAL:
                    DisplayedKillProofs.Sort((e1, e2) => e1.Text.CompareTo(e2.Text));
                    foreach (KillProofButton e1 in DisplayedKillProofs) { e1.Visible = e1.BottomText.ToLower().Contains("fractal"); }
                    break;
                case DD_RAID:
                    DisplayedKillProofs.Sort((e1, e2) => e1.Text.CompareTo(e2.Text));
                    foreach (KillProofButton e1 in DisplayedKillProofs) { e1.Visible = e1.BottomText.ToLower().Contains("raid"); }
                    break;
                default:
                    throw new NotSupportedException();
            }
            RepositionKp();
        }
        private void RepositionKp()
        {
            int pos = 0;
            foreach (KillProofButton kp in DisplayedKillProofs)
            {
                int x = pos % 3;
                int y = pos / 3;
                kp.Location = new Point(x * 335, y * 108);

                ((Panel)kp.Parent).VerticalScrollOffset = 0;
                kp.Parent.Invalidate();
                if (kp.Visible) pos++;
            }
        }
    }
}
