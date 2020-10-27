using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Screenshot_Manager_Module.Controls;
using Screenshot_Manager_Module.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Color = Microsoft.Xna.Framework.Color;
using Image = System.Drawing.Image;
using Point = Microsoft.Xna.Framework.Point;

namespace Screenshot_Manager_Module
{
    [Export(typeof(Module))]
    public class ScreenshotManagerModule : Module
    {
        private const int WindowWidth = 1024;
        private const int WindowHeight = 780;
        private const int PanelMargin = 5;
        private const int FileTimeOutMilliseconds = 10000;
        private const int NewFileNotificationDelay = 300;
        private const int MaxFileNameLength = 50;

        private static readonly Logger Logger = Logger.GetLogger(typeof(ScreenshotManagerModule));

        internal static ScreenshotManagerModule ModuleInstance;

        private readonly string[] _imageFilters = {"*.bmp", "*.jpg", "*.png"};
        private readonly IEnumerable<char> _invalidFileNameCharacters;
        private readonly Point _thumbnailSize = new Point(306, 175);
        private Texture2D _completeHeartIcon;
        private Texture2D _deleteSearchBoxContentIcon;

        private Texture2D _icon64;

        private Texture2D _incompleteHeartIcon;

        //private Texture2D _trashcanClosedIcon128;
        //private Texture2D _trashcanOpenIcon128;
        internal Texture2D _inspectIcon;

        internal Texture2D _notificationBackroundTexture;

        //private Texture2D _icon128;
        //private Texture2D _portaitModeIcon128;
        // private Texture2D _portaitModeIcon512;
        private Texture2D _trashcanClosedIcon64;
        private Texture2D _trashcanOpenIcon64;
        private Dictionary<string, Panel> displayedThumbnails;

        #region Settings

        private SettingEntry<List<string>> favorites;

        #endregion

        private CornerIcon moduleCornerIcon;
        internal Panel modulePanel;
        private WindowTab moduleTab;
        private KeyBinding printScreenKey;
        private List<FileSystemWatcher> screensPathWatchers;
        private bool isLoadingThumbnails;
        private bool isDisposingThumbnails;

        private FlowPanel thumbnailFlowPanel;

        [ImportingConstructor]
        public ScreenshotManagerModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(
            moduleParameters)
        {
            ModuleInstance = this;
            _invalidFileNameCharacters = Path.GetInvalidFileNameChars().Union(Path.GetInvalidPathChars());
        }

        protected override void DefineSettings(SettingCollection settings)
        {
            var selfManagedSettings = settings.AddSubCollection("ManagedSettings", false, false);
            favorites = selfManagedSettings.DefineSetting("favorites", new List<string>());
        }

        protected void LoadTextures()
        {
            _icon64 = ContentsManager.GetTexture("screenshots_icon_64x64.png");
            //_icon128 = ContentsManager.GetTexture("screenshots_icon_128x128.png");
            _inspectIcon = ContentsManager.GetTexture("inspect.png");
            //_portaitModeIcon128 = ContentsManager.GetTexture("portaitMode_icon_128x128.png");
            //_portaitModeIcon512 = ContentsManager.GetTexture("portaitMode_icon_128x128.png");
            _trashcanClosedIcon64 = ContentsManager.GetTexture("trashcanClosed_icon_64x64.png");
            _trashcanOpenIcon64 = ContentsManager.GetTexture("trashcanOpen_icon_64x64.png");
            //_trashcanClosedIcon128 = ContentsManager.GetTexture("trashcanClosed_icon_128x128.png");
            //_trashcanOpenIcon128 = ContentsManager.GetTexture("trashcanOpen_icon_128x128.png");
            _incompleteHeartIcon = ContentsManager.GetTexture("incomplete_heart.png");
            _completeHeartIcon = ContentsManager.GetTexture("complete_heart.png");
            _deleteSearchBoxContentIcon = ContentsManager.GetTexture("784262.png");
            _notificationBackroundTexture = ContentsManager.GetTexture("ns-button.png");
        }

        private async void ScreenshotNotify(object sender, EventArgs e)
        {
            // Delaying so created file handle is closed (write completed) before we look at the directory for its newest file.
            await Task.Delay(NewFileNotificationDelay).ContinueWith(delegate
            {
                FileInfo screenshot = null;
                var completed = false;
                var timeout = DateTime.Now.AddMilliseconds(FileTimeOutMilliseconds);
                while (!completed)
                    try
                    {
                        var directory = new DirectoryInfo(DirectoryUtil.ScreensPath);
                        screenshot = directory.GetFiles()
                            .OrderByDescending(f => f.LastWriteTime)
                            .First();
                        completed = true;
                    }
                    catch (InvalidOperationException ex)
                    {
                        if (DateTime.Now < timeout) continue;
                        Logger.Error(ex.Message + ex.StackTrace);
                        return;
                    }

                ScreenshotNotification.ShowNotification(screenshot.FullName,ScreenshotCreated, 5.0f);
            });
        }

        protected override void Initialize()
        {
            LoadTextures();
            GameService.Overlay.UserLocaleChanged += ChangeLocalization;
            ChangeLocalization(null, null);
            printScreenKey = new KeyBinding(Keys.PrintScreen);
            printScreenKey.Activated += ScreenshotNotify;
            printScreenKey.Enabled = true;
            screensPathWatchers = new List<FileSystemWatcher>();
            displayedThumbnails = new Dictionary<string, Panel>();
            foreach (var f in _imageFilters)
            {
                var w = new FileSystemWatcher
                {
                    Path = DirectoryUtil.ScreensPath,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                    Filter = f,
                    EnableRaisingEvents = true
                };
                w.Created += OnScreenshotCreated;
                w.Deleted += OnScreenshotDeleted;
                w.Renamed += OnScreenshotRenamed;
                screensPathWatchers.Add(w);
            }
            moduleCornerIcon = new CornerIcon
            {
                IconName = Name,
                Icon = _icon64,
                Priority = Name.GetHashCode()
            };
            moduleCornerIcon.Click += delegate
            {
                GameService.Overlay.BlishHudWindow.Show();
                GameService.Overlay.BlishHudWindow.Navigate(modulePanel);
                //TODO: Select the correct tab.
            };
        }

        private void ToggleFileSystemWatchers(object sender, EventArgs e)
        {
            if (screensPathWatchers == null) return;
            foreach (var fsw in screensPathWatchers)
                fsw.EnableRaisingEvents = GameService.GameIntegration.Gw2HasFocus;
        }

        private Texture2D GetScreenshot(string filePath)
        {
            Texture2D texture = null;
            var completed = false;
            var timeout = DateTime.Now.AddMilliseconds(FileTimeOutMilliseconds);
            while (!completed) {
                if (!File.Exists(filePath)) return null;
                try {
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
                        using (var source = Image.FromStream(fs)) {
                            var maxWidth = GameService.Graphics.Resolution.X - 100;
                            var maxHeight = GameService.Graphics.Resolution.Y - 100;
                            var (width, height) = PointExtensions.ResizeKeepAspect(
                                new Point(source.Width, source.Height), maxWidth,
                                maxHeight);
                            using (var target = new Bitmap(source, width, height)) {
                                using (var graphic = Graphics.FromImage(target)) {
                                    graphic.CompositingQuality = CompositingQuality.HighSpeed;
                                    graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                    graphic.SmoothingMode = SmoothingMode.HighSpeed;
                                    graphic.DrawImage(target, 0, 0, width, height);
                                }
                                using (var textureStream = new MemoryStream()) {
                                    target.Save(textureStream, ImageFormat.Jpeg);
                                    var buffer = new byte[textureStream.Length];
                                    textureStream.Position = 0;
                                    textureStream.Read(buffer, 0, buffer.Length);
                                    texture = Texture2D.FromStream(GameService.Graphics.GraphicsDevice, textureStream);
                                }
                            }
                        }
                    }
                    completed = true;
                } catch (IOException e) {
                    if (DateTime.Now < timeout) continue;
                    Logger.Error(e.Message + e.StackTrace);
                    return null;
                }
            }

            return texture;
        }
        internal Texture2D GetThumbnail(string filePath) {
            Texture2D texture = null;
            var completed = false;
            var timeout = DateTime.Now.AddMilliseconds(FileTimeOutMilliseconds);
            while (!completed) {
                if (!File.Exists(filePath)) return null;
                try {
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
                        using (var source = Image.FromStream(fs)) {
                            using (var target = new Bitmap(source, _thumbnailSize.X, _thumbnailSize.Y)) {
                                using (var graphic = Graphics.FromImage(source)) {
                                    graphic.CompositingQuality = CompositingQuality.HighSpeed;
                                    graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                    graphic.SmoothingMode = SmoothingMode.HighSpeed;
                                    graphic.DrawImage(target, 0, 0, _thumbnailSize.X, _thumbnailSize.Y);
                                }
                                using (var textureStream = new MemoryStream()) {
                                    target.Save(textureStream, ImageFormat.Jpeg);
                                    var buffer = new byte[textureStream.Length];
                                    textureStream.Position = 0;
                                    textureStream.Read(buffer, 0, buffer.Length);
                                    texture = Texture2D.FromStream(GameService.Graphics.GraphicsDevice, textureStream);
                                }
                            }
                        }
                    }
                    completed = true;
                } catch (IOException e) {
                    if (DateTime.Now < timeout) continue;
                    Logger.Error(e.Message + e.StackTrace);
                    return null;
                }
            }

            return texture;
        }
        internal Panel CreateInspectionPanel(string filePath)
        {
            var texture = GetScreenshot(filePath);
            if (texture == null) return null;
            var inspectPanel = new Panel
            {
                Parent = GameService.Graphics.SpriteScreen,
                Size = new Point(texture.Width + 10, texture.Height + 10),
                Location = new Point(GameService.Graphics.SpriteScreen.Width / 2 - texture.Width / 2,
                    GameService.Graphics.SpriteScreen.Height / 2 - texture.Height / 2),
                BackgroundColor = Color.Black,
                ZIndex = 9999,
                ShowTint = true,
                Opacity = 0.0f
            };
            var inspImage = new Blish_HUD.Controls.Image
            {
                Parent = inspectPanel,
                Location = new Point(5, 5),
                Size = new Point(texture.Width, texture.Height),
                Texture = texture
            };
            GameService.Animation.Tweener.Tween(inspectPanel, new {Opacity = 1.0f}, 0.35f);
            inspImage.Click += delegate
            {
                GameService.Animation.Tweener.Tween(inspectPanel, new {Opacity = 0.0f}, 0.15f)
                    .OnComplete(() => inspectPanel?.Dispose());
            };
            return inspectPanel;
        }

        private void AddThumbnail(string filePath)
        {
            if (modulePanel == null || displayedThumbnails.ContainsKey(filePath)) return;

            
            var texture = GetThumbnail(filePath);
            if (texture == null) return;

            var thumbnail = new Panel
            {
                Parent = thumbnailFlowPanel,
                Size = new Point(_thumbnailSize.X + 6, _thumbnailSize.Y + 6),
                BackgroundColor = Color.Black,
                BasicTooltipText = filePath
            };

            var tImage = new Blish_HUD.Controls.Image
            {
                Parent = thumbnail,
                Location = new Point(3, 3),
                Size = _thumbnailSize,
                Texture = texture,
                Opacity = 0.8f
            };
            var inspectButton = new Blish_HUD.Controls.Image
            {
                Parent = thumbnail,
                Texture = _inspectIcon,
                Size = new Point(64, 64),
                Location = new Point(thumbnail.Width / 2 - 32, thumbnail.Height / 2 - 32),
                Opacity = 0.0f
            };
            var deleteBackgroundTint = new Panel
            {
                Parent = thumbnail,
                Size = thumbnail.Size,
                BackgroundColor = Color.Black,
                Opacity = 0.0f
            };
            var deleteLabel = new Label
            {
                Parent = thumbnail,
                Size = thumbnail.Size,
                TextColor = Color.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle,
                Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size24,
                    ContentService.FontStyle.Regular),
                Text = FileDeletionPrompt,
                BasicTooltipText = ZoomInThumbnailTooltipText,
                StrokeText = true,
                ShowShadow = true,
                Opacity = 0.0f
            };
            var favoriteMarker = new Blish_HUD.Controls.Image
            {
                Parent = thumbnail,
                Location = new Point(thumbnail.Size.X - 30 - PanelMargin, PanelMargin),
                Size = new Point(30, 30),
                BasicTooltipText = favorites.Value.Contains(filePath) ? UnfavoriteMarkerTooltip : FavoriteMarkerTooltip,
                Texture = favorites.Value.Contains(filePath) ? _completeHeartIcon : _incompleteHeartIcon,
                Opacity = 0.5f
            };
            favoriteMarker.Click += delegate
            {
                var currentFilePath = thumbnail.BasicTooltipText;
                if (favorites.Value.Contains(currentFilePath))
                {
                    var copy = new List<string>(favorites.Value);
                    copy.Remove(currentFilePath);
                    favorites.Value = copy;
                    favoriteMarker.Texture = _incompleteHeartIcon;
                    favoriteMarker.BasicTooltipText = FavoriteMarkerTooltip;
                }
                else
                {
                    var copy = new List<string>(favorites.Value)
                        {currentFilePath};
                    favorites.Value = copy;
                    favoriteMarker.Texture = _completeHeartIcon;
                    favoriteMarker.BasicTooltipText = UnfavoriteMarkerTooltip;
                }

                SortThumbnails();
            };
            favoriteMarker.MouseEntered += delegate
            {
                GameService.Animation.Tweener.Tween(favoriteMarker, new {Opacity = 1.0f}, 0.2f);
            };
            favoriteMarker.MouseLeft += delegate
            {
                GameService.Animation.Tweener.Tween(favoriteMarker, new {Opacity = 0.5f}, 0.2f);
                favoriteMarker.Location = new Point(thumbnail.Size.X - 30 - PanelMargin, PanelMargin);
                favoriteMarker.Size = new Point(30, 30);
            };
            favoriteMarker.LeftMouseButtonPressed += delegate
            {
                favoriteMarker.Width -= 2;
                favoriteMarker.Height -= 2;
                favoriteMarker.Location = new Point(favoriteMarker.Location.X + 2, favoriteMarker.Location.Y + 2);
            };
            favoriteMarker.LeftMouseButtonReleased += delegate
            {
                favoriteMarker.Location = new Point(thumbnail.Size.X - 30 - PanelMargin, PanelMargin);
                favoriteMarker.Size = new Point(30, 30);
            };
            var fileNameTextBox = new TextBox
            {
                Parent = thumbnail,
                Size = new Point(thumbnail.Width / 2 + 20, 30),
                Location = new Point(PanelMargin, thumbnail.Height - 30 - PanelMargin),
                PlaceholderText = Path.GetFileNameWithoutExtension(filePath),
                MaxLength = MaxFileNameLength,
                BackgroundColor = Color.DarkBlue,
                BasicTooltipText = Path.GetFileNameWithoutExtension(filePath),
                Text = "",
                Opacity = 0.8f
            };
            var fileNameLengthLabel = new Label
            {
                Parent = thumbnail,
                Size = fileNameTextBox.Size,
                Location = new Point(fileNameTextBox.Location.X, fileNameTextBox.Location.Y - fileNameTextBox.Height),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Text = "0/" + MaxFileNameLength,
                Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size11,
                    ContentService.FontStyle.Regular),
                TextColor = Color.Yellow,
                StrokeText = true,
                Visible = false
            };
            fileNameTextBox.TextChanged += delegate
            {
                fileNameLengthLabel.Text = fileNameTextBox.Text.Length + "/" + MaxFileNameLength;
            };
            fileNameTextBox.MouseEntered += delegate
            {
                GameService.Animation.Tweener.Tween(fileNameTextBox, new {Opacity = 1.0f}, 0.2f);
            };
            fileNameTextBox.MouseLeft += delegate
            {
                if (!fileNameTextBox.Focused)
                    GameService.Animation.Tweener.Tween(fileNameTextBox, new {Opacity = 0.8f}, 0.2f);
            };
            var enterPressed = false;
            fileNameTextBox.EnterPressed += delegate
            {
                enterPressed = true;
                var oldFilePath = thumbnail.BasicTooltipText;
                var newFileName = fileNameTextBox.Text.Trim();
                if (newFileName.Equals(string.Empty))
                {
                    ScreenNotification.ShowNotification(ReasonEmptyFileName, ScreenNotification.NotificationType.Error);
                }
                else if (_invalidFileNameCharacters.Any(x => newFileName.Contains(x)))
                {
                    ScreenNotification.ShowNotification(ReasonInvalidFileName
                                                        + "\n" + PromptChangeFileName
                                                        + "\n" + InvalidFileNameCharactersHint + "\n"
                                                        + string.Join(" ", _invalidFileNameCharacters),
                        ScreenNotification.NotificationType.Error, null, 10);
                }
                else
                {
                    var newPath = Path.Combine(Directory.GetParent(Path.GetFullPath(oldFilePath)).FullName,
                        newFileName + Path.GetExtension(oldFilePath));
                    if (newPath.Equals(oldFilePath, StringComparison.InvariantCultureIgnoreCase))
                    {
                    }
                    else if (File.Exists(newPath))
                    {
                        ScreenNotification.ShowNotification(
                            FailedToRenameFileNotification + " " + ReasonDublicateFileName,
                            ScreenNotification.NotificationType.Error);
                    }
                    else if (!File.Exists(oldFilePath))
                    {
                        ScreenNotification.ShowNotification(
                            FailedToRenameFileNotification + " " + ReasonFileNotExisting,
                            ScreenNotification.NotificationType.Error);
                        thumbnail?.Dispose();
                    }
                    else
                    {
                        var renameCompleted = false;
                        var renameTimeout = DateTime.Now.AddMilliseconds(FileTimeOutMilliseconds);
                        while (!renameCompleted)
                            try
                            {
                                File.Move(oldFilePath, newPath);
                                renameCompleted = true;
                            }
                            catch (IOException e)
                            {
                                if (DateTime.Now < renameTimeout) continue;
                                Logger.Error(e.Message + e.StackTrace);
                            }
                    }
                }

                if (!fileNameTextBox.MouseOver)
                    GameService.Animation.Tweener.Tween(fileNameTextBox, new {Opacity = 0.6f}, 0.2f);
                fileNameTextBox.Text = "";
                enterPressed = false;
            };
            fileNameTextBox.InputFocusChanged += delegate
            {
                fileNameLengthLabel.Visible = fileNameTextBox.Focused;
                fileNameLengthLabel.Text = "0/" + MaxFileNameLength;
                fileNameTextBox.InputFocusChanged += delegate
                {
                    Task.Run(async delegate
                    {
                        //InputFocusChanged needs to wait to not interfere with EnterPressed.
                        await Task.Delay(1).ContinueWith(delegate
                        {
                            if (!enterPressed)
                                fileNameTextBox.Text = "";
                        });
                    });
                };
            };

            deleteLabel.MouseEntered += delegate
            {
                GameService.Animation.Tweener.Tween(inspectButton, new {Opacity = 1.0f}, 0.2f);
                GameService.Animation.Tweener.Tween(tImage, new {Opacity = 1.0f}, 0.45f);
            };
            deleteLabel.MouseLeft += delegate
            {
                GameService.Animation.Tweener.Tween(inspectButton, new {Opacity = 0.0f}, 0.2f);
                GameService.Animation.Tweener.Tween(tImage, new {Opacity = 0.8f}, 0.45f);
            };
            Panel inspectPanel = null;
            deleteLabel.Click += delegate
            {
                inspectPanel?.Dispose();
                inspectPanel = CreateInspectionPanel(filePath);
            };
            var deleteButton = new Blish_HUD.Controls.Image
            {
                Parent = thumbnail,
                Texture = _trashcanClosedIcon64,
                Size = new Point(45, 45),
                Location = new Point(thumbnail.Width - 45 - PanelMargin, thumbnail.Height - 45 - PanelMargin),
                Opacity = 0.5f
            };
            deleteButton.MouseEntered += delegate
            {
                deleteButton.Texture = _trashcanOpenIcon64;
                GameService.Animation.Tweener.Tween(deleteButton, new {Opacity = 1.0f}, 0.2f);
                GameService.Animation.Tweener.Tween(deleteLabel, new {Opacity = 1.0f}, 0.2f);
                GameService.Animation.Tweener.Tween(deleteBackgroundTint, new {Opacity = 0.6f}, 0.35f);
                GameService.Animation.Tweener.Tween(fileNameTextBox, new {Opacity = 0.0f}, 0.2f);
            };
            deleteButton.MouseLeft += delegate
            {
                deleteButton.Texture = _trashcanClosedIcon64;
                GameService.Animation.Tweener.Tween(deleteButton, new {Opacity = 0.8f}, 0.2f);
                GameService.Animation.Tweener.Tween(deleteLabel, new {Opacity = 0.0f}, 0.2f);
                GameService.Animation.Tweener.Tween(deleteBackgroundTint, new {Opacity = 0.0f}, 0.2f);
                GameService.Animation.Tweener.Tween(fileNameTextBox, new {Opacity = 0.8f}, 0.2f);
            };
            deleteButton.LeftMouseButtonReleased += delegate
            {
                var oldFilePath = thumbnail.BasicTooltipText;
                if (!File.Exists(oldFilePath))
                {
                    thumbnail?.Dispose();
                }
                else
                {
                    var deletionCompleted = false;
                    var deletionTimeout = DateTime.Now.AddMilliseconds(FileTimeOutMilliseconds);
                    while (!deletionCompleted)
                        try
                        {
                            fileNameTextBox.Text = "";
                            File.Delete(oldFilePath);
                            deletionCompleted = true;
                        }
                        catch (IOException e)
                        {
                            if (DateTime.Now < deletionTimeout) continue;
                            Logger.Error(e.Message + e.StackTrace);
                            ScreenNotification.ShowNotification(FailedToDeleteFileNotification + " " + ReasonFileInUse,
                                ScreenNotification.NotificationType.Error);
                        }
                }
            };
            thumbnail.Disposed += delegate
            {
                var oldFilePath = thumbnail.BasicTooltipText;
                if (displayedThumbnails.ContainsKey(oldFilePath))
                    displayedThumbnails.Remove(oldFilePath);

                if (!favorites.Value.Contains(oldFilePath)) return;
                var copy = new List<string>(favorites.Value);
                copy.Remove(oldFilePath);
                favorites.Value = copy;
            };
            try
            {
                displayedThumbnails.Add(filePath, thumbnail);
                SortThumbnails();
            }
            catch (ArgumentException e)
            {
                Logger.Error(e.Message + e.StackTrace);
                thumbnail.Dispose();
            }
        }

        private void SortThumbnails()
        {
            thumbnailFlowPanel.SortChildren(delegate(Panel x, Panel y)
            {
                var favMarkerX = (Blish_HUD.Controls.Image) x.Children.FirstOrDefault(m =>
                    m.BasicTooltipText != null && (m.BasicTooltipText.Equals(FavoriteMarkerTooltip) ||
                                                   m.BasicTooltipText.Equals(UnfavoriteMarkerTooltip)));
                var favMarkerY = (Blish_HUD.Controls.Image) y.Children.FirstOrDefault(m =>
                    m.BasicTooltipText != null && (m.BasicTooltipText.Equals(FavoriteMarkerTooltip) ||
                                                   m.BasicTooltipText.Equals(UnfavoriteMarkerTooltip)));
                if (favMarkerX != null && favMarkerY != null)
                {
                    var favorite = string.Compare(favMarkerY.BasicTooltipText, favMarkerX.BasicTooltipText,
                        StringComparison.InvariantCultureIgnoreCase);
                    if (favorite != 0)
                        return favorite;
                }

                return string.Compare(Path.GetFileNameWithoutExtension(x.BasicTooltipText),
                    Path.GetFileNameWithoutExtension(y.BasicTooltipText),
                    StringComparison.InvariantCultureIgnoreCase);
            });
        }

        private void OnScreenshotCreated(object sender, FileSystemEventArgs e)
        {
            if (!displayedThumbnails.ContainsKey(e.FullPath))
                AddThumbnail(e.FullPath);
        }

        private void OnScreenshotDeleted(object sender, FileSystemEventArgs e)
        {
            if (displayedThumbnails.ContainsKey(e.FullPath))
                displayedThumbnails[e.FullPath].Dispose();
        }

        private void OnScreenshotRenamed(object sender, RenamedEventArgs e)
        {
            if (!displayedThumbnails.ContainsKey(e.OldFullPath)) return;
            var thumbnail = displayedThumbnails.FirstOrDefault(x => x.Value.BasicTooltipText.Equals(e.OldFullPath))
                .Value;
            displayedThumbnails.Remove(e.OldFullPath);
            if (!displayedThumbnails.ContainsKey(e.FullPath))
                displayedThumbnails.Add(e.FullPath, thumbnail);

            if (thumbnail == null) return;
            var fileNameTextBox = (TextBox) thumbnail.Children.First(x => x.GetType() == typeof(TextBox));
            fileNameTextBox.PlaceholderText = Path.GetFileNameWithoutExtension(e.FullPath);
            fileNameTextBox.BasicTooltipText = Path.GetFileNameWithoutExtension(e.FullPath);
            thumbnail.BasicTooltipText = e.FullPath;
        }

        private Panel BuildModulePanel(WindowBase wnd)
        {
            var homePanel = new Panel
            {
                Parent = wnd,
                Size = new Point(WindowWidth, WindowHeight)
            };
            thumbnailFlowPanel = new FlowPanel
            {
                Parent = homePanel,
                Size = new Point(homePanel.ContentRegion.Size.X - 70, homePanel.ContentRegion.Size.Y - 130),
                Location = new Point(35, 50),
                FlowDirection = ControlFlowDirection.LeftToRight,
                ControlPadding = new Vector2(5, 5),
                CanCollapse = false,
                CanScroll = true,
                Collapsed = false,
                ShowTint = true,
                ShowBorder = true
            };
            thumbnailFlowPanel.PropertyChanged += delegate (object o, PropertyChangedEventArgs e) {
                if (!e.PropertyName.Equals(nameof(thumbnailFlowPanel.VerticalScrollOffset))) return;
                 //TODO: Load/Unload displayed thumbnails that are (not) in view while scrolling.

            };
            var searchBox = new TextBox
            {
                Parent = homePanel,
                Location = new Point(thumbnailFlowPanel.Location.X, thumbnailFlowPanel.Location.Y - 40),
                Size = new Point(200, 40),
                PlaceholderText = SearchBoxPlaceHolder
            };
            var deleteSearchBoxContentButton = new Blish_HUD.Controls.Image
            {
                Parent = homePanel,
                Location = new Point(searchBox.Right - 20 - PanelMargin, searchBox.Location.Y + PanelMargin),
                Size = new Point(20, 20),
                Texture = _deleteSearchBoxContentIcon,
                Visible = false
            };
            deleteSearchBoxContentButton.Click += delegate
            {
                searchBox.Text = "";
                deleteSearchBoxContentButton.Hide();
            };
            deleteSearchBoxContentButton.MouseEntered += delegate
            {
                if (deleteSearchBoxContentButton.Visible)
                    GameService.Animation.Tweener.Tween(deleteSearchBoxContentButton, new {Opacity = 1.0f}, 0.2f);
            };
            deleteSearchBoxContentButton.MouseLeft += delegate
            {
                if (deleteSearchBoxContentButton.Visible)
                    GameService.Animation.Tweener.Tween(deleteSearchBoxContentButton, new {Opacity = 0.8f}, 0.2f);

                deleteSearchBoxContentButton.Size = new Point(20, 20);
                deleteSearchBoxContentButton.Location = new Point(searchBox.Right - 20 - PanelMargin,
                    searchBox.Location.Y + PanelMargin);
            };
            deleteSearchBoxContentButton.LeftMouseButtonPressed += delegate
            {
                deleteSearchBoxContentButton.Width -= 2;
                deleteSearchBoxContentButton.Height -= 2;
                deleteSearchBoxContentButton.Location = new Point(deleteSearchBoxContentButton.Location.X + 2,
                    deleteSearchBoxContentButton.Location.Y + 2);
            };
            searchBox.InputFocusChanged += delegate { SortThumbnails(); };
            searchBox.TextChanged += delegate
            {
                deleteSearchBoxContentButton.Visible = !searchBox.Text.Equals(string.Empty);
                thumbnailFlowPanel.SortChildren(delegate(Panel x, Panel y)
                {
                    var fileNameX = Path.GetFileNameWithoutExtension(x.BasicTooltipText);
                    var fileNameY = Path.GetFileNameWithoutExtension(y.BasicTooltipText);
                    x.Visible = fileNameX.Contains(searchBox.Text);
                    y.Visible = fileNameY.Contains(searchBox.Text);
                    var favMarkerX = (Blish_HUD.Controls.Image) x.Children.FirstOrDefault(m =>
                        m.BasicTooltipText != null && (m.BasicTooltipText.Equals(FavoriteMarkerTooltip) ||
                                                       m.BasicTooltipText.Equals(UnfavoriteMarkerTooltip)));
                    var favMarkerY = (Blish_HUD.Controls.Image) y.Children.FirstOrDefault(m =>
                        m.BasicTooltipText != null && (m.BasicTooltipText.Equals(FavoriteMarkerTooltip) ||
                                                       m.BasicTooltipText.Equals(UnfavoriteMarkerTooltip)));
                    if (favMarkerX != null && favMarkerY != null)
                    {
                        var favorite = string.Compare(favMarkerY.BasicTooltipText, favMarkerX.BasicTooltipText,
                            StringComparison.InvariantCultureIgnoreCase);
                        if (favorite != 0)
                            return favorite;
                    }

                    return string.Compare(fileNameX, fileNameY, StringComparison.InvariantCultureIgnoreCase);
                });
            };
            homePanel.Hidden += ToggleFileSystemWatchers;
            homePanel.Hidden += ToggleFileSystemWatchers;
            homePanel.Shown += LoadImages;
            homePanel.Hidden += DisposeDisplayedThumbnails;
            return homePanel;
        }
        private async void DisposeDisplayedThumbnails(object sender, EventArgs e)
        {
            if (isLoadingThumbnails || displayedThumbnails == null || displayedThumbnails.Count == 0) return;
            isDisposingThumbnails = true;
            isDisposingThumbnails = await Task.Run(() =>
            {
                var filePaths = new List<string>(displayedThumbnails.Keys);
                foreach (var path in filePaths)
                {
                    if (isLoadingThumbnails) break;
                    displayedThumbnails[path]?.Dispose();
                    displayedThumbnails?.Remove(path);
                }
                filePaths.Clear();
                return false;
            });
        }
        private async void LoadImages(object sender, EventArgs e)
        {
            if (isLoadingThumbnails || !Directory.Exists(DirectoryUtil.ScreensPath)) return;
            isLoadingThumbnails = true;
            isLoadingThumbnails = await Task.Run(() =>
            {
                foreach (var fileName in Directory.EnumerateFiles(DirectoryUtil.ScreensPath)
                    .Where(s => Array.Exists(_imageFilters,
                        filter => filter.Equals('*' + Path.GetExtension(s),
                            StringComparison.InvariantCultureIgnoreCase))))
                {
                    if (!modulePanel.Visible) break;
                    AddThumbnail(Path.Combine(DirectoryUtil.ScreensPath, fileName));
                }
            }).ContinueWith(delegate
            {
                if (!modulePanel.Visible)
                    DisposeDisplayedThumbnails(null, null);
                return false;
            });
        }

        protected override async Task LoadAsync()
        {
            /* NOOP */
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        protected override void Update(GameTime gameTime)
        {
            /* NOOP */
        }

        private void CleanFavorites()
        {
            var copy = new List<string>(favorites.Value);
            foreach (var path in favorites.Value)
            {
                if (File.Exists(path)) continue;
                copy.Remove(path);
            }

            favorites.Value = copy;
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            // Unload
            CleanFavorites();
            GameService.Overlay.UserLocaleChanged -= ChangeLocalization;
            printScreenKey.Enabled = false;
            printScreenKey.Activated -= ScreenshotNotify;
            printScreenKey = null;
            modulePanel.Hidden -= ToggleFileSystemWatchers;
            modulePanel.Shown -= ToggleFileSystemWatchers;
            modulePanel.Shown -= LoadImages;
            GameService.Overlay.BlishHudWindow.RemoveTab(moduleTab);
            moduleTab = null;
            modulePanel?.Dispose();
            moduleCornerIcon?.Dispose();
            thumbnailFlowPanel?.Dispose();
            // avoiding resource leak
            for (var i = 0; i < screensPathWatchers.Count; i++)
            {
                if (screensPathWatchers[i] == null) continue;
                screensPathWatchers[i].Created -= OnScreenshotCreated;
                screensPathWatchers[i].Deleted -= OnScreenshotDeleted;
                screensPathWatchers[i].Renamed -= OnScreenshotRenamed;
                screensPathWatchers[i].Dispose();
                screensPathWatchers[i] = null;
            }

            displayedThumbnails.Clear();
            displayedThumbnails = null;
            // All static members must be manually unset
            ModuleInstance = null;
        }

        #region Service Managers

        internal SettingsManager SettingsManager => ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => ModuleParameters.Gw2ApiManager;

        #endregion

        #region Localization Strings

        private string FailedToDeleteFileNotification;
        private string FailedToRenameFileNotification;
        private string ReasonFileInUse;
        private string ReasonFileNotExisting;
        private string ReasonDublicateFileName;
        private string ReasonEmptyFileName;
        private string ReasonInvalidFileName;
        private string PromptChangeFileName;
        private string InvalidFileNameCharactersHint;
        private string FileDeletionPrompt;
        private string RenameFileTooltipText;
        private string ZoomInThumbnailTooltipText;
        private string SearchBoxPlaceHolder;
        private string FavoriteMarkerTooltip;
        private string UnfavoriteMarkerTooltip;
        private string ScreenshotCreated;

        private void ChangeLocalization(object sender, EventArgs e)
        {
            FailedToDeleteFileNotification = Resources.Failed_to_delete_image___0_;
            FailedToRenameFileNotification = Resources.Unable_to_rename_image___0_;
            ReasonFileInUse = Resources.The_image_file_is_in_use_by_another_process_;
            ReasonFileNotExisting = Resources.The_image_file_doesn_t_exist_anymore_;
            ReasonDublicateFileName = Resources.A_duplicate_image_name_was_specified_;
            ReasonEmptyFileName = Resources.Image_name_cannot_be_empty_;
            ReasonInvalidFileName = Resources.The_image_name_contains_invalid_characters_;
            PromptChangeFileName = Resources.Please_enter_a_different_image_name_;
            InvalidFileNameCharactersHint = Resources.The_following_characters_are_not_allowed___0_;
            FileDeletionPrompt = Resources.Delete_Image_;
            RenameFileTooltipText = Resources.Rename_Image;
            ZoomInThumbnailTooltipText = Resources.Click_To_Zoom;
            SearchBoxPlaceHolder = Resources.Search___;
            FavoriteMarkerTooltip = Resources.Favourite;
            UnfavoriteMarkerTooltip = Resources.Unfavourite;
            ScreenshotCreated = Resources.Screenshot_Created_;

            //TODO: Implement as View so panel reloads automatically.
            modulePanel?.Dispose();
            modulePanel = BuildModulePanel(GameService.Overlay.BlishHudWindow);

            if (moduleTab != null)
                GameService.Overlay.BlishHudWindow.RemoveTab(moduleTab);

            moduleTab = GameService.Overlay.BlishHudWindow.AddTab(Name, _icon64, modulePanel, 0);
        }

    #endregion
    }
}