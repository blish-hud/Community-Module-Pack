using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using Blish_HUD;
using Blish_HUD.Controls;
namespace Kill_Proof_Module.Controls
{
    public class KillProofButton : DetailsButton
    {
        private const int DEFAULT_WIDTH = 327;
        private const int DEFAULT_HEIGHT = 100;
        private const int DEFAULT_BOTTOMSECTION_HEIGHT = 35;

        private readonly Texture2D ICON_TITLE;
        private readonly Texture2D PIXEL;

        private BitmapFont _font;
        public BitmapFont Font
        {
            get => _font;
            set
            {
                if (_font == value) return;

                _font = value;
                OnPropertyChanged();
            }
        }
        private string _bottomText = "z";
        public string BottomText
        {
            get => _bottomText;
            set
            {
                if (_bottomText == value) return;
                _bottomText = value;
            }
        }
        private bool _isTitleDisplay = false;
        public bool IsTitleDisplay
        {
            get => _isTitleDisplay;
            set {
                if (value == _isTitleDisplay) return;
                _isTitleDisplay = value;
            }
        }
        public KillProofButton()
        {
            this.ICON_TITLE = ICON_TITLE ?? KillProofModule.ModuleInstance.ContentsManager.GetTexture("icon_title.png");
            this.PIXEL = PIXEL ?? ContentService.Textures.Pixel;

            this.Size = new Point(DEFAULT_WIDTH, DEFAULT_HEIGHT);
        }
        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            // Draw background
            spriteBatch.DrawOnCtrl(this, PIXEL, bounds, Color.Black * 0.25f);

            // Draw bottom section
            spriteBatch.DrawOnCtrl(this, PIXEL, this.ContentRegion, Color.Black * 0.1f);

            int iconSize = this.IconSize == DetailsIconSize.Large ? DEFAULT_HEIGHT : DEFAULT_HEIGHT - DEFAULT_BOTTOMSECTION_HEIGHT;

            // Draw bottom text
            if (this.IsTitleDisplay)
            {
                spriteBatch.DrawOnCtrl(this, ICON_TITLE, new Rectangle(DEFAULT_WIDTH - 36, bounds.Height - DEFAULT_BOTTOMSECTION_HEIGHT + 1, 32, 32), Color.White);
            }
            else
            {
                spriteBatch.DrawStringOnCtrl(this, this.BottomText, Content.DefaultFont14, new Rectangle(iconSize + 20, iconSize - DEFAULT_BOTTOMSECTION_HEIGHT, DEFAULT_WIDTH - 40, DEFAULT_BOTTOMSECTION_HEIGHT), Color.White, false, true, 2);
            }
            if (this.Icon != null)
            {
                // Draw icon
                spriteBatch.DrawOnCtrl(this, this.Icon, new Rectangle(iconSize / 2 - 64 / 2 + (this.IconSize == DetailsIconSize.Small ? 10 : 0), iconSize / 2 - 64 / 2, 64, 64), Color.White);

                // Draw icon box
                if (this.IconSize == DetailsIconSize.Large)
                    spriteBatch.DrawOnCtrl(this, Content.GetTexture("605003"), new Rectangle(0, 0, iconSize, iconSize), Color.White);
            }

            // Draw bottom section seperator
            spriteBatch.DrawOnCtrl(this, Content.GetTexture("157218"), new Rectangle(this.ContentRegion.X, bounds.Height - 40, bounds.Width, 8), Color.White);

            // Wrap text
            string wrappedText = DrawUtil.WrapText(this.Font, this.Text, DEFAULT_WIDTH - 40 - iconSize - 20);

            // Draw name
            spriteBatch.DrawStringOnCtrl(this, wrappedText, this.Font, new Rectangle(iconSize + 20, 0, DEFAULT_WIDTH - 40, this.Height - DEFAULT_BOTTOMSECTION_HEIGHT), Color.White, false, true, 2);
        }
    }
}
