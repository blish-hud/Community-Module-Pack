using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Events_Module {
    public class NotificationMover : Control {

        // This is all very kludgy.  I wouldn't use it as a reference for anything.

        private const int HANDLE_SIZE = 40;

        private readonly SpriteBatchParameters _clearDrawParameters;

        private readonly ScreenRegion[] _screenRegions;

        private ScreenRegion _activeScreenRegion = null;

        private Point _grabPosition = Point.Zero;

        private readonly Texture2D _handleTexture;

        public NotificationMover(params ScreenRegion[] screenPositions) : this(screenPositions.ToList()) { /* NOOP */ }

        public NotificationMover(IEnumerable<ScreenRegion> screenPositions) {
            this.ZIndex = int.MaxValue - 10;

            _clearDrawParameters = new SpriteBatchParameters(SpriteSortMode.Deferred, BlendState.Opaque);
            _screenRegions     = screenPositions.ToArray();

            _handleTexture = EventsModule.ModuleInstance.ContentsManager.GetTexture("textures/handle.png");
        }

        protected override void OnLeftMouseButtonPressed(MouseEventArgs e) {
            if (_activeScreenRegion == null) {
                // Only start drag if we were moused over one.
                return;
            }

            _grabPosition = this.RelativeMousePosition;
        }

        public override void DoUpdate(GameTime gameTime) {
            base.DoUpdate(gameTime);

            if (GameService.Input.Keyboard.KeysDown.Contains(Microsoft.Xna.Framework.Input.Keys.Escape)) {
                this.Dispose();
            }
        }

        protected override void OnLeftMouseButtonReleased(MouseEventArgs e) {
            _grabPosition = Point.Zero;
        }

        protected override void OnMouseMoved(MouseEventArgs e) {
            if (_grabPosition != Point.Zero && _activeScreenRegion != null) {
                var lastPos = _grabPosition;
                _grabPosition = this.RelativeMousePosition;

                _activeScreenRegion.Location += (_grabPosition - lastPos);
            } else {
                // Update which screen region the mouse is over.
                foreach (var region in _screenRegions) {
                    if (region.Bounds.Contains(this.RelativeMousePosition)) {
                        _activeScreenRegion = region;
                        return;
                    }
                }

                _activeScreenRegion = null;
            }
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds) {
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, bounds, Color.Black * 0.8f);
            spriteBatch.End();
            spriteBatch.Begin(_clearDrawParameters);

            foreach (var region in _screenRegions) {
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.TransparentPixel, region.Bounds, Color.Transparent);
            }

            spriteBatch.End();
            spriteBatch.Begin(this.SpriteBatchParameters);

            foreach (var region in _screenRegions) {
                if (region == _activeScreenRegion) {
                    spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(region.Location, region.Size), Color.White * 0.5f);
                }

                spriteBatch.DrawOnCtrl(this, _handleTexture, new Rectangle(region.Bounds.Left, region.Bounds.Top, HANDLE_SIZE, HANDLE_SIZE), _handleTexture.Bounds, Color.White * 0.6f);
                spriteBatch.DrawOnCtrl(this, _handleTexture, new Rectangle(region.Bounds.Right - HANDLE_SIZE / 2, region.Bounds.Top +  HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE), _handleTexture.Bounds, Color.White * 0.6f, MathHelper.PiOver2, new Vector2(HANDLE_SIZE / 2f, HANDLE_SIZE / 2f));
                spriteBatch.DrawOnCtrl(this, _handleTexture, new Rectangle(region.Bounds.Left + HANDLE_SIZE / 2, region.Bounds.Bottom - HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE), _handleTexture.Bounds, Color.White * 0.6f, MathHelper.PiOver2 * 3, new Vector2(HANDLE_SIZE / 2f, HANDLE_SIZE / 2f));
                spriteBatch.DrawOnCtrl(this, _handleTexture, new Rectangle(region.Bounds.Right - HANDLE_SIZE / 2, region.Bounds.Bottom - HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE), _handleTexture.Bounds, Color.White * 0.6f, MathHelper.Pi, new Vector2(HANDLE_SIZE / 2f, HANDLE_SIZE / 2f));

                //spriteBatch.DrawStringOnCtrl(this,
                //                             region.RegionName,
                //                             GameService.Content.DefaultFont32,
                //                             region.Bounds,
                //                             Color.Black,
                //                             false,
                //                             HorizontalAlignment.Center);
            }

            spriteBatch.DrawStringOnCtrl(this, "Press ESC to close.", GameService.Content.DefaultFont32, bounds, Color.White, false, HorizontalAlignment.Center);
        }

    }
}
