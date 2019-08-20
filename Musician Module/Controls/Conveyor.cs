using System;
using System.Collections.Generic;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Blish_HUD.Controls.Intern;
using Musician_Module.Controls.Instrument;
using Blish_HUD;

namespace Musician_Module.Controls {

    public class Conveyor : Container {
        public static readonly Dictionary<GuildWarsControls, int> LaneCoordinatesX = new Dictionary<GuildWarsControls, int>
        {
            {GuildWarsControls.WeaponSkill1, 13},
            {GuildWarsControls.WeaponSkill2, 75},
            {GuildWarsControls.WeaponSkill3, 136},
            {GuildWarsControls.WeaponSkill4, 197},
            {GuildWarsControls.WeaponSkill5, 260},
            {GuildWarsControls.HealingSkill, 429},
            {GuildWarsControls.UtilitySkill1, 491},
            {GuildWarsControls.UtilitySkill2, 552},
            {GuildWarsControls.UtilitySkill3, 614},
            {GuildWarsControls.EliteSkill, 675}
        };
        private readonly Texture2D ConveyorTopSprite;
        private readonly Texture2D ConveyorBottomSprite;
        public Conveyor() {
            ConveyorTopSprite = ConveyorTopSprite ?? MusicianModule.ModuleInstance.ContentsManager.GetTexture("conveyor_top.png");
            ConveyorBottomSprite = ConveyorBottomSprite ?? MusicianModule.ModuleInstance.ContentsManager.GetTexture("conveyor_bottom.png");
            this.Size = new Point(744, Graphics.SpriteScreen.Height); // set static bounds.
            this.ZIndex = 0;
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
        public void SpawnNoteBlock(GuildWarsControls key, InstrumentSkillType noteType, Color spriteColor)
        {
            NoteBlock note = new NoteBlock(key, noteType, spriteColor) { Parent = this };
        }
    }
}
