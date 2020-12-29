using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Screenshot_Manager_Module.Controls
{
    public class ScreenshotNotification : Panel
    {
        private const int HEADING_HEIGHT = 20;

        private const int PanelMargin = 10;

        private static int _visibleNotifications;
        private readonly Texture2D _inspectIcon;

        private readonly AsyncTexture2D _thumbnail;
        private readonly string _filePath;

        private readonly Point _thumbnailSize;
        private Rectangle _layoutInspectIconBounds;

        private Rectangle _layoutThumbnailBounds;

        private ScreenshotNotification(string filePath, string message)
        {
            _filePath = filePath;
            _thumbnail = ScreenshotManagerModule.ModuleInstance.GetThumbnail(_filePath);
            _thumbnailSize = ScreenshotManagerModule.ModuleInstance._thumbnailSize;
            _inspectIcon = ScreenshotManagerModule.ModuleInstance._inspectIcon;

            Opacity = 0f;

            Size = new Point(_thumbnailSize.X + PanelMargin, _thumbnailSize.Y + HEADING_HEIGHT + PanelMargin);

            Location = new Point(60, 60 + (Size.Y + 15) * _visibleNotifications);

            ShowBorder = true;
            ShowTint = true;

            var borderPanel = new Panel
            {
                Parent = this,
                Size = new Point(Size.X, _thumbnailSize.Y + PanelMargin),
                Location = new Point(0, HEADING_HEIGHT),
                BackgroundColor = Color.Black,
                ShowTint = true,
                ShowBorder = true
            };
            var messageLbl = new Label
            {
                Parent = this,
                Location = new Point(0, 2),
                Size = Size,
                Font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size14,
                    ContentService.FontStyle.Regular),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
                Text = message
            };
            _visibleNotifications++;
            Click += delegate
            {
                ScreenshotManagerModule.ModuleInstance.CreateInspectionPanel(_filePath);
                GameService.Overlay.BlishHudWindow.Show();
                GameService.Overlay.BlishHudWindow.Navigate(ScreenshotManagerModule.ModuleInstance.modulePanel);
                Dispose();
            };
        }

        protected override CaptureType CapturesInput()
        {
            return CaptureType.Mouse;
        }

        /// <inheritdoc />
        public override void RecalculateLayout()
        {
            _layoutThumbnailBounds = new Rectangle(PanelMargin / 2, HEADING_HEIGHT + PanelMargin / 2, _thumbnailSize.X,
                _thumbnailSize.Y);
            _layoutInspectIconBounds = new Rectangle(Size.X / 2 - 32, Size.Y / 2 - 32 + HEADING_HEIGHT, 64, 64);
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            spriteBatch.DrawOnCtrl(this,
                ScreenshotManagerModule.ModuleInstance._notificationBackroundTexture,
                bounds,
                Color.White * 0.85f);
        }

        public override void PaintAfterChildren(SpriteBatch spriteBatch, Rectangle bound)
        {
            spriteBatch.DrawOnCtrl(this,
                _thumbnail,
                _layoutThumbnailBounds);

            spriteBatch.DrawOnCtrl(this,
                _inspectIcon,
                _layoutInspectIconBounds);
        }

        private void Show(float duration)
        {
            Content.PlaySoundEffectByName(@"audio/color-change");

            Animation.Tweener
                .Tween(this, new {Opacity = 1f}, 0.2f)
                .Repeat(1)
                .RepeatDelay(duration)
                .Reflect()
                .OnComplete(Dispose);
        }

        public static void ShowNotification(string filePath, string message, float duration)
        {
            var notif = new ScreenshotNotification(filePath, message)
            {
                Parent = Graphics.SpriteScreen
            };

            notif.Show(duration);
        }

        protected override void DisposeControl()
        {
            _visibleNotifications--;

            base.DisposeControl();
        }
    }
}