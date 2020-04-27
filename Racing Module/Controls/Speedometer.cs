using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Bh.Racing.Controls {
    public class Speedometer : Control {

        #region Load Static

        private static readonly Texture2D _textureSpeedFill;
        private static readonly Texture2D _textureBar;

        static Speedometer() {
            _textureSpeedFill = Module.ModuleInstance.ContentsManager.GetTexture("speed-fill.png");
            _textureBar       = Module.ModuleInstance.ContentsManager.GetTexture("1060345-2.png");
        }

        #endregion

        public int   MinSpeed = 0;
        public float MaxSpeed = 50;
        public float Speed { get; set; } = 0;

        public bool ShowSpeedValue { get; set; } = false;

        public Speedometer() {
            this.ClipsBounds = true;
            this.Size        = new Point(128, 128);

            UpdateLocation(null, null);

            Graphics.SpriteScreen.Resized += UpdateLocation;
        }

        private void UpdateLocation(object sender, EventArgs e) {
            this.Location = new Point(Graphics.SpriteScreen.Width / 2 - 64, Graphics.SpriteScreen.Height - 218);
        }

        protected override CaptureType CapturesInput() => CaptureType.ForceNone;

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds) {
            float ang = (float)((4 + this.Speed / MaxSpeed * 2));

            spriteBatch.DrawOnCtrl(this,
                                   _textureSpeedFill,
                                   new Rectangle(_size.X / 2, _size.Y, 150, 203),
                                   null,
                                   Color.GreenYellow,
                                   ang,
                                   new Vector2(_textureSpeedFill.Bounds.Width / 2, 141));

            spriteBatch.DrawOnCtrl(this,
                                   _textureBar,
                                   _size.InBounds(bounds),
                                   null,
                                   Color.White,
                                   0f,
                                   Vector2.Zero);

            if (this.ShowSpeedValue) {
                spriteBatch.DrawStringOnCtrl(this,
                                             Math.Round(this.Speed).ToString(),
                                             Content.DefaultFont14,
                                             new Rectangle(0, 0, _size.X, 50),
                                             Color.White,
                                             false);
            }
        }

    }
}
