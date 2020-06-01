using System;
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
using Color = Microsoft.Xna.Framework.Color;
using Module = Blish_HUD.Modules.Module;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Screenshot_Manager_Module
{

    [Export(typeof(Module))]
    public class ScreenshotManagerModule : Module
    {

        private static readonly Logger Logger = Logger.GetLogger(typeof(ScreenshotManagerModule));

        internal static ScreenshotManagerModule ModuleInstance;

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion


        private Texture2D _icon;
        private Texture2D _inspectIcon;
        private Texture2D _portaitModeIcon;
        private Texture2D _incompleteHeartIcon;
        private Texture2D _completeHeartIcon;

        private const int WindowWidth = 1024;
        private const int WindowHeight = 780;
        private const int PanelMargin = 5;

        private CornerIcon moduleCornerIcon;
        private WindowTab moduleTab;

        [ImportingConstructor]
        public ScreenshotManagerModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { ModuleInstance = this; }

        protected override void DefineSettings(SettingCollection settings) {

        }

        protected void LoadTextures()
        {
            _icon = ContentsManager.GetTexture("screenshots_icon_64x64.png");
            _inspectIcon = ContentsManager.GetTexture("inspect.png");
            _portaitModeIcon = ContentsManager.GetTexture("portaitMode_icon_128x128.png");
            _incompleteHeartIcon = ContentsManager.GetTexture("incomplete_heart.png");
            _completeHeartIcon = ContentsManager.GetTexture("complete_heart.png");
        }
        protected override void Initialize() {
            LoadTextures();

            var modulePanel = BuildModulePanel(GameService.Overlay.BlishHudWindow);
            moduleTab = GameService.Overlay.BlishHudWindow.AddTab(Name, _icon, modulePanel, 0);
            moduleCornerIcon = new CornerIcon()
            {
                IconName = Name,
                Icon = ContentsManager.GetTexture("screenshots_icon_64x64.png"),
                Priority = Name.GetHashCode()
            };
            moduleCornerIcon.Click += delegate
            {
                GameService.Overlay.BlishHudWindow.Show();
                GameService.Overlay.BlishHudWindow.Navigate(modulePanel);
            };

        }
       /* Image image = Image.FromFile(fileName);
        Image thumb = image.GetThumbnailImage(120, 120, () => false, IntPtr.Zero); */
        private Panel BuildModulePanel(WindowBase wnd)
        {
            var homePanel = new Panel()
            {
                Parent = wnd,
                Size = new Point(WindowWidth, WindowHeight),
                Location = new Point(GameService.Graphics.SpriteScreen.Width / 2 - WindowWidth / 2, GameService.Graphics.SpriteScreen.Height / 2 - WindowHeight / 2),
            };
            homePanel.Hidden += delegate {
                homePanel.Dispose();
            };
            var thumbnailFlowpanel = new FlowPanel()
            {
                Parent = homePanel,
                Size = homePanel.ContentRegion.Size,
                Location = new Point(0,0),
                FlowDirection = ControlFlowDirection.TopToBottom,
                ControlPadding = new Vector2(2, 2),
                CanCollapse = false,
                CanScroll = true,
                Collapsed = false,
                ShowTint = true,
                ShowBorder = true
            };
            if (Directory.Exists(DirectoryUtil.ScreensPath))
            {
                Panel inspectPanel = null;
                foreach (var fileName in Directory.EnumerateFiles(DirectoryUtil.ScreensPath).Where(s => s.EndsWith(".bmp") || s.EndsWith(".jpg")))
                {
                    var filePath = Path.Combine(DirectoryUtil.ScreensPath, fileName);
                    var originalImage = System.Drawing.Image.FromFile(filePath);
                    var thumbnailScale = PointExtensions.ResizeKeepAspect(new Point(originalImage.Width, originalImage.Height), 300, 300);
                    var thumbnail = new Panel() {
                        Parent = thumbnailFlowpanel,
                        Size = new Point(thumbnailScale.X + 6, thumbnailScale.Y + 6),
                        BackgroundColor = Color.Black
                    };
                    Texture2D texture;
                    using (var textureStream = new MemoryStream())
                    {
                        originalImage.Save(textureStream, originalImage.RawFormat);
                        var buffer = new byte[textureStream.Length];
                        textureStream.Position = 0;
                        textureStream.Read(buffer, 0, buffer.Length);
                        texture = Texture2D.FromStream(GameService.Graphics.GraphicsDevice, textureStream);
                    }
                    var tImage = new Blish_HUD.Controls.Image()
                    {
                        Parent = thumbnail,
                        Location = new Point(3, 3),
                        Size = thumbnailScale,
                        Texture = texture,
                        Opacity = 0.8f
                    };
                    tImage.MouseEntered += delegate { GameService.Animation.Tweener.Tween(tImage, new {Opacity = 1.0f}, 0.45f); };
                    tImage.MouseLeft += delegate { GameService.Animation.Tweener.Tween(tImage, new {Opacity = 0.8f}, 0.45f); };
                    var fileNameTextBox = new TextBox()
                    {
                        Parent = thumbnail,
                        Size = new Point(thumbnail.Width / 2 + 20, 30),
                        Location = new Point( PanelMargin, thumbnail.Height - 30 - PanelMargin),
                        PlaceholderText = Path.GetFileNameWithoutExtension(filePath),
                        MaxLength = 255,
                        Opacity = 0.6f
                    };
                    fileNameTextBox.MouseEntered += delegate { GameService.Animation.Tweener.Tween(fileNameTextBox, new { Opacity = 1.0f }, 0.2f); };
                    fileNameTextBox.MouseLeft += delegate { if (!fileNameTextBox.Focused) GameService.Animation.Tweener.Tween(fileNameTextBox, new { Opacity = 0.6f }, 0.2f); };
                    fileNameTextBox.EnterPressed += delegate
                    {
                        if (Path.GetInvalidFileNameChars().Any(x => fileNameTextBox.Text.Contains(x)) ||
                            Path.GetInvalidPathChars().Any(x => fileNameTextBox.Text.Contains(x))) {
                            ScreenNotification.ShowNotification("The file name contains invalid characters. Please enter a different file name.\n" +
                                                                "The following characters are not allowed: ! \" # $ % & ' ( ) * + , - . / : ; < = > ? @ [ \\ ] ^ ` { | } ~", ScreenNotification.NotificationType.Error, null, 10);
                            return;
                        } else {
                            var newPath = Path.Combine(Directory.GetParent(Path.GetFullPath(filePath)).FullName,
                                Path.Combine(fileNameTextBox.Text, Path.GetExtension(filePath)));
                            if (!File.Exists(filePath))
                                ScreenNotification.ShowNotification($"Unable to rename file. File \"{filePath}\" doesn't exist anymore!", ScreenNotification.NotificationType.Error);
                            try
                            {
                                File.Move(filePath, newPath);
                                filePath = newPath;
                            } catch (IOException e) {
                                Logger.Error("Unable to rename file!", e.Message);
                                GC.Collect();
                            }
                        }
                        if (!fileNameTextBox.MouseOver) GameService.Animation.Tweener.Tween(fileNameTextBox, new { Opacity = 0.6f }, 0.2f);
                        fileNameTextBox.Text = "";
                    };
                    var inspectButton = new Blish_HUD.Controls.Image()
                    {
                        Parent = thumbnail,
                        Texture = _inspectIcon,
                        Size = new Point(64,64),
                        Location = new Point(thumbnail.Width - 64 - PanelMargin, thumbnail.Height - 64 - PanelMargin),
                        Opacity = 0.6f
                    };
                    inspectButton.MouseEntered += delegate { GameService.Animation.Tweener.Tween(inspectButton, new { Opacity = 1.0f }, 0.2f); };
                    inspectButton.MouseLeft += delegate { GameService.Animation.Tweener.Tween(inspectButton, new { Opacity = 0.6f }, 0.2f); };
                    inspectButton.Click += delegate
                    {
                        inspectPanel?.Dispose();
                        var maxWidth = GameService.Graphics.Resolution.X - 100;
                        var maxHeight = GameService.Graphics.Resolution.Y - 100;
                        var inspectScale = PointExtensions.ResizeKeepAspect(GameService.Graphics.Resolution, maxWidth,
                            maxHeight);
                        inspectPanel = new Panel()
                        {
                            Parent = GameService.Graphics.SpriteScreen,
                            Size = new Point(inspectScale.X + 10, inspectScale.Y + 10),
                            Location = new Point((GameService.Graphics.SpriteScreen.Width / 2) - (inspectScale.X / 2), (GameService.Graphics.SpriteScreen.Height / 2) - (inspectScale.Y / 2)),
                            BackgroundColor = Color.Black,
                            ZIndex = 9999,
                            Opacity = 0.0f
                        };
                        Texture2D tex;
                        using (var textureStream = new MemoryStream()) {
                            originalImage.Save(textureStream, originalImage.RawFormat);
                            var buffer = new byte[textureStream.Length];
                            textureStream.Position = 0;
                            textureStream.Read(buffer, 0, buffer.Length);
                            tex = Texture2D.FromStream(GameService.Graphics.GraphicsDevice, textureStream);
                        }
                        var inspImage = new Blish_HUD.Controls.Image() {
                            Parent = inspectPanel,
                            Location = new Point(5, 5),
                            Size = inspectScale,
                            Texture = tex
                        };
                        GameService.Animation.Tweener.Tween(inspectPanel, new {Opacity = 1.0f}, 0.45f);
                        inspImage.Click += delegate { inspectPanel.Dispose(); };
                    };
                }
            }
            return homePanel;
        }
        protected override async Task LoadAsync() {

        }

        protected override void OnModuleLoaded(EventArgs e) {

            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        protected override void Update(GameTime gameTime) {

        }

        /// <inheritdoc />
        protected override void Unload() {
            // Unload
            moduleCornerIcon?.Dispose();
            // All static members must be manually unset
            ModuleInstance = null;
        }

    }

}
