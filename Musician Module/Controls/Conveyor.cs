using System;
using System.Collections.Generic;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Blish_HUD.Controls.Intern;
using Musician_Module.Controls.Instrument;
using Blish_HUD;

namespace Musician_Module.Controls {

    public class Conveyor:Container {
        private readonly Instrument.Instrument Instrument;
        private static Texture2D ConveyorTopSprite;
        private static Texture2D ConveyorBottomSprite;
        public Conveyor() {
            ConveyorTopSprite = ConveyorTopSprite ?? MusicianModule.ModuleInstance.ContentsManager.GetTexture("conveyor_top.png");
            ConveyorBottomSprite = ConveyorBottomSprite ?? MusicianModule.ModuleInstance.ContentsManager.GetTexture("conveyor_bottom.png");
            this.Size = new Point(744, Graphics.SpriteScreen.Height); // set static bounds.
            this.ZIndex = -1;
            UpdateLocation(null, null);
            Graphics.SpriteScreen.Resized += UpdateLocation;
        }

        private void UpdateLocation(object sender, EventArgs e) {
            this.Location = new Point((Graphics.SpriteScreen.Width / 2 - this.Width / 2), 0);
            this.Size = new Point(744, Graphics.SpriteScreen.Height);
        }

        protected override CaptureType CapturesInput() {
            return CaptureType.None;
        }
        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds) {
            var height = Graphics.SpriteScreen.Height;

            spriteBatch.DrawOnCtrl(this, ConveyorTopSprite, new Rectangle(0, 0, 744, height - 90), null, Color.White, 0f, Vector2.Zero, SpriteEffects.None);
            spriteBatch.DrawOnCtrl(this, ConveyorBottomSprite, new Rectangle(0, height - 93, 744, 75), null, Color.White, 0f, Vector2.Zero, SpriteEffects.None);
        }
        public void SpawnNoteBlock(IKeyboard keyboard, GuildWarsControls key, InstrumentSkillType noteType)
        {
            var note = new NoteBlock(keyboard, key, noteType) { Parent = this };
        }
    }
}
