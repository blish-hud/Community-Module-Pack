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
using Color = Microsoft.Xna.Framework.Color;
using Module = Blish_HUD.Modules.Module;
using Point = Microsoft.Xna.Framework.Point;
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


        private Texture2D _icon64;
        private Texture2D _icon128;
        private Texture2D _portaitModeIcon128;
        private Texture2D _portaitModeIcon512;
        private Texture2D _trashcanClosedIcon64;
        private Texture2D _trashcanOpenIcon64;
        private Texture2D _trashcanClosedIcon128;
        private Texture2D _trashcanOpenIcon128;
        private Texture2D _inspectIcon;
        private Texture2D _incompleteHeartIcon;
        private Texture2D _completeHeartIcon;

        private readonly string[] _imageFilters = { "*.bmp", "*.jpg", "*.png" };
        private const int WindowWidth = 1024;
        private const int WindowHeight = 780;
        private const int PanelMargin = 5;

        private CornerIcon moduleCornerIcon;
        private WindowTab moduleTab;
        private List<FileSystemWatcher> screensPathWatchers;

        private FlowPanel thumbnailFlowPanel;
        private Dictionary<string, Panel> displayedThumbnails;

        [ImportingConstructor]
        public ScreenshotManagerModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { ModuleInstance = this; }

        protected override void DefineSettings(SettingCollection settings) {

        }

        protected void LoadTextures()
        {
            _icon64 = ContentsManager.GetTexture("screenshots_icon_64x64.png");
            //_icon128 = ContentsManager.GetTexture("screenshots_icon_128x128.png");
            _inspectIcon = ContentsManager.GetTexture("inspect.png");
            _portaitModeIcon128 = ContentsManager.GetTexture("portaitMode_icon_128x128.png");
            //_portaitModeIcon512 = ContentsManager.GetTexture("portaitMode_icon_128x128.png");
            _trashcanClosedIcon64 = ContentsManager.GetTexture("trashcanClosed_icon_64x64.png");
            _trashcanOpenIcon64 = ContentsManager.GetTexture("trashcanOpen_icon_64x64.png");
            //_trashcanClosedIcon128 = ContentsManager.GetTexture("trashcanClosed_icon_128x128.png");
            //_trashcanOpenIcon128 = ContentsManager.GetTexture("trashcanOpen_icon_128x128.png");
            _incompleteHeartIcon = ContentsManager.GetTexture("incomplete_heart.png");
            _completeHeartIcon = ContentsManager.GetTexture("complete_heart.png");
        }

        protected override void Initialize()
        {
            LoadTextures();
            screensPathWatchers = new List<FileSystemWatcher>();
            displayedThumbnails = new Dictionary<string, Panel>();
            foreach (string f in _imageFilters) {
                var w = new FileSystemWatcher
                {
                    Path = DirectoryUtil.ScreensPath,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.LastWrite | NotifyFilters.FileName,
                    Filter = f,
                    EnableRaisingEvents = true
                };
                w.Changed += OnScreensFolderChanged;
                screensPathWatchers.Add(w);
            }
            var modulePanel = BuildModulePanel(GameService.Overlay.BlishHudWindow);
            moduleTab = GameService.Overlay.BlishHudWindow.AddTab(Name, _icon64, modulePanel, 0);
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
                //TODO: Select the correct tab.
            };

        }
        private void AddThumbnail(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var originalImage = System.Drawing.Image.FromFile(filePath);
            var originalSize = new Point(originalImage.Width, originalImage.Height);
            var thumbnailScale = PointExtensions.ResizeKeepAspect(originalSize, 300, 300);
            var thumbnail = new Panel() {
                Parent = thumbnailFlowPanel,
                Size = new Point(thumbnailScale.X + 6, thumbnailScale.Y + 6),
                BackgroundColor = Color.Black
            };
            Texture2D texture;
            using (var textureStream = new MemoryStream()) {
                originalImage.Save(textureStream, originalImage.RawFormat);
                    var buffer = new byte[textureStream.Length];
                    textureStream.Position = 0;
                    textureStream.Read(buffer, 0, buffer.Length);
                    texture = Texture2D.FromStream(GameService.Graphics.GraphicsDevice, textureStream);
                    originalImage.Dispose();
            }
            var tImage = new Blish_HUD.Controls.Image() {
                Parent = thumbnail,
                Location = new Point(3, 3),
                Size = thumbnailScale,
                Texture = texture,
                Opacity = 0.8f,
                BasicTooltipText = "Click To Zoom"
            };
            var fileNameTextBox = new TextBox() {
                Parent = thumbnail,
                Size = new Point(thumbnail.Width / 2 + 20, 30),
                Location = new Point(PanelMargin, thumbnail.Height - 30 - PanelMargin),
                PlaceholderText = Path.GetFileNameWithoutExtension(filePath),
                MaxLength = 255,
                BackgroundColor = Color.DarkBlue,
                Opacity = 0.8f
            };
            fileNameTextBox.MouseEntered += delegate { GameService.Animation.Tweener.Tween(fileNameTextBox, new { Opacity = 1.0f }, 0.2f); };
            fileNameTextBox.MouseLeft += delegate { if (!fileNameTextBox.Focused) GameService.Animation.Tweener.Tween(fileNameTextBox, new { Opacity = 0.8f }, 0.2f); };
            fileNameTextBox.EnterPressed += delegate
            {
                if (Path.GetInvalidFileNameChars().Any(x => fileNameTextBox.Text.Contains(x)) ||
                    Path.GetInvalidPathChars().Any(x => fileNameTextBox.Text.Contains(x))) {
                    ScreenNotification.ShowNotification("The file name contains invalid characters. Please enter a different file name.\n" +
                                                        "The following characters are not allowed: ! \" # $ % & ' ( ) * + , - . / : ; < = > ? @ [ \\ ] ^ ` { | } ~", ScreenNotification.NotificationType.Error, null, 10);
                    return;
                } else {
                    var newPath = Path.Combine(Directory.GetParent(Path.GetFullPath(filePath)).FullName, fileNameTextBox.Text + Path.GetExtension(filePath));
                    bool error = newPath.Equals(filePath);
                    if (File.Exists(newPath)) {
                        ScreenNotification.ShowNotification(
                            $"Unable to rename file:\n A duplicate file name was specified!",
                            ScreenNotification.NotificationType.Error);
                        error = true;
                    }
                    if (!File.Exists(filePath)) {
                        ScreenNotification.ShowNotification(
                            $"Unable to rename file:\n\"{filePath}\"\ndoesn't exist anymore!",
                            ScreenNotification.NotificationType.Error);
                        thumbnail?.Dispose();
                        error = true;
                    }
                    if (!error) {
                        try {
                            File.Move(filePath, newPath);
                            fileNameTextBox.PlaceholderText = Path.GetFileNameWithoutExtension(newPath);
                            filePath = newPath;
                        } catch (IOException e) {
                            Logger.Error(e.Message + " | StackTrace:" + e.StackTrace);
                            GC.Collect();
                        }
                    }
                }
                if (!fileNameTextBox.MouseOver) GameService.Animation.Tweener.Tween(fileNameTextBox, new { Opacity = 0.6f }, 0.2f);
                fileNameTextBox.Text = "";
            };
            var inspectButton = new Blish_HUD.Controls.Image() {
                Parent = thumbnail,
                Texture = _inspectIcon,
                Size = new Point(64, 64),
                Location = new Point((thumbnail.Width / 2) - 32, (thumbnail.Height / 2) - 32),
                Opacity = 0.0f,
                BasicTooltipText = "Click To Zoom"
            };
            tImage.MouseEntered += delegate
            {
                GameService.Animation.Tweener.Tween(inspectButton, new { Opacity = 1.0f }, 0.2f);
                GameService.Animation.Tweener.Tween(tImage, new { Opacity = 1.0f }, 0.45f);
            };
            tImage.MouseLeft += delegate
            {
                GameService.Animation.Tweener.Tween(inspectButton, new { Opacity = 0.0f }, 0.2f);
                GameService.Animation.Tweener.Tween(tImage, new { Opacity = 0.8f }, 0.45f);
            };
            Panel inspectPanel = null;
            tImage.Click += delegate {
                inspectPanel?.Dispose();
                var maxWidth = GameService.Graphics.Resolution.X - 100;
                var maxHeight = GameService.Graphics.Resolution.Y - 100;
                var inspectScale = PointExtensions.ResizeKeepAspect(GameService.Graphics.Resolution, maxWidth,
                    maxHeight);
                inspectPanel = new Panel() {
                    Parent = GameService.Graphics.SpriteScreen,
                    Size = new Point(inspectScale.X + 10, inspectScale.Y + 10),
                    Location = new Point((GameService.Graphics.SpriteScreen.Width / 2) - (inspectScale.X / 2), (GameService.Graphics.SpriteScreen.Height / 2) - (inspectScale.Y / 2)),
                    BackgroundColor = Color.Black,
                    ZIndex = 9999,
                    ShowBorder = true,
                    ShowTint = true,
                    Opacity = 0.0f
                };
                var inspImage = new Blish_HUD.Controls.Image() {
                    Parent = inspectPanel,
                    Location = new Point(5, 5),
                    Size = inspectScale,
                    Texture = texture
                };
                GameService.Animation.Tweener.Tween(inspectPanel, new { Opacity = 1.0f }, 0.35f);
                inspImage.Click += delegate { GameService.Animation.Tweener.Tween(inspectPanel, new { Opacity = 0.0f }, 0.15f).OnComplete(() => inspectPanel?.Dispose()); };
            };
            var deleteButton = new Image()
            {
                Parent = thumbnail,
                Texture = _trashcanClosedIcon64,
                Size = new Point(45,45),
                Location = new Point(thumbnail.Width - 45 - PanelMargin, thumbnail.Height - 45 - PanelMargin),
                Opacity = 0.5f,
                BasicTooltipText = "Delete File?"
            };
            deleteButton.MouseEntered += delegate {
                deleteButton.Texture = _trashcanOpenIcon64; GameService.Animation.Tweener.Tween(deleteButton, new { Opacity = 1.0f }, 0.2f);
            };
            deleteButton.MouseLeft += delegate {
                deleteButton.Texture = _trashcanClosedIcon64; GameService.Animation.Tweener.Tween(deleteButton, new { Opacity = 0.8f }, 0.2f);
            };
            deleteButton.LeftMouseButtonReleased += delegate
            {
                if (!File.Exists(filePath)) {
                    thumbnail?.Dispose();
                } else {
                    try {
                        File.Delete(filePath);
                        thumbnail?.Dispose();
                    } catch (IOException e) {
                        Logger.Error(e.Message + " | StackTrace:" + e.StackTrace);
                        GC.Collect();
                    }
                }
            };
            thumbnail.Disposed += delegate { if (displayedThumbnails.ContainsKey(filePath)) displayedThumbnails.Remove(filePath); };
            displayedThumbnails.Add(filePath, thumbnail);
        }
        private void OnScreensFolderChanged(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(e.FullPath)) {
                if (displayedThumbnails.ContainsKey(e.FullPath))
                    displayedThumbnails[e.FullPath]?.Dispose();
            } else if (!displayedThumbnails.ContainsKey(e.FullPath)) {
                AddThumbnail(e.FullPath);
            } else if (displayedThumbnails[e.FullPath] == null) {
                displayedThumbnails.Remove(e.FullPath);
                AddThumbnail(e.FullPath);
            }
        }
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
            thumbnailFlowPanel = new FlowPanel()
            {
                Parent = homePanel,
                Size = homePanel.ContentRegion.Size,
                Location = new Point(0,0),
                FlowDirection = ControlFlowDirection.LeftToRight,
                ControlPadding = new Vector2(5, 5),
                CanCollapse = false,
                CanScroll = true,
                Collapsed = false,
                ShowTint = true,
                ShowBorder = true
            };
            if (Directory.Exists(DirectoryUtil.ScreensPath))
            {
                GC.Collect();
                foreach (var fileName in Directory.EnumerateFiles(DirectoryUtil.ScreensPath).Where(s => s.EndsWith(".bmp") || s.EndsWith(".jpg") || s.EndsWith(".png")))
                {
                    AddThumbnail(Path.Combine(DirectoryUtil.ScreensPath, fileName));
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
            thumbnailFlowPanel?.Dispose();
            // avoiding resource leak
            for (var i=0; i<screensPathWatchers.Count; i++)
            {
                if (screensPathWatchers[i] == null) continue;
                screensPathWatchers[i].Changed -= OnScreensFolderChanged;
                screensPathWatchers[i].Dispose();
                screensPathWatchers[i] = null;
            }
            foreach (var p in displayedThumbnails)
                p.Value?.Dispose();
            displayedThumbnails.Clear();
            displayedThumbnails = null;
            // All static members must be manually unset
            ModuleInstance = null;
        }

    }

}
