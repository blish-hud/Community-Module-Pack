using System;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Glide;
using System.Collections.Generic;
using Blish_HUD.Controls.Intern;
using Musician_Module.Controls.Instrument;
using Blish_HUD;

namespace Musician_Module.Controls {
    public class NoteBlock : Control {

        private static readonly Dictionary<InstrumentSkillType, Texture2D> NoteTextures = new Dictionary<InstrumentSkillType, Texture2D>
        {
            {InstrumentSkillType.Note, MusicianModule.ModuleInstance.ContentsManager.GetTexture("note_block.png")},
            {InstrumentSkillType.IncreaseOctave, MusicianModule.ModuleInstance.ContentsManager.GetTexture("incr_octave.png")},
            {InstrumentSkillType.DecreaseOctave, MusicianModule.ModuleInstance.ContentsManager.GetTexture("decr_octave.png")},
            {InstrumentSkillType.StopPlaying, MusicianModule.ModuleInstance.ContentsManager.GetTexture("pause_block.png")}
        };
        private static readonly Dictionary<GuildWarsControls, int> ConveyorLanes = new Dictionary<GuildWarsControls, int>
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
        private readonly Texture2D NoteSprite;
        private readonly Color SpriteColor;
        private readonly GuildWarsControls NoteKey;
        private readonly int XOffset;

        private Glide.Tween NoteAnim = null;
        /// <summary>
        /// Creates a note block.
        /// </summary>
        /// <param name="_keyboard">The keyboard this note block reacts to.</param>
        /// <param name="_key">The key this noteblock reacts to.</param>
        /// <param name="_noteType">The type of the note skill used ingame.</param>
        public NoteBlock(GuildWarsControls _key, InstrumentSkillType _noteType, Color _spriteColor) {
            this.ZIndex = 1;
            this.Size = new Point(56, 20);
            this.NoteSprite = NoteTextures[_noteType];
            this.SpriteColor = _spriteColor;
            this.NoteKey = _key;
            this.XOffset = ConveyorLanes[_key];
            this.Location = new Point(XOffset, 0 - this.Width);

            NoteAnim = Animation.Tweener
                .Tween(this, new { Top = Graphics.SpriteScreen.Height - 100 }, 10)
                .OnComplete(() => {
                    NoteAnim = null;
                    this.Dispose();
                }
            );
            Graphics.SpriteScreen.Resized += UpdateLocation;
        }
        private void UpdateLocation(object sender, EventArgs e) {
            this.Size = new Point(56, 30);
            if (this.Location.Y >= Graphics.SpriteScreen.Height - (100 + this.Size.Y))
            {
                NoteAnim.Pause();
            }
        }

        protected override CaptureType CapturesInput() {
            return CaptureType.Keyboard;
        }
        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds) {
            spriteBatch.DrawOnCtrl(this, this.NoteSprite, new Rectangle(0, 0, 56, 20), null, this.SpriteColor, 0f, Vector2.Zero, SpriteEffects.None);
        }
    }
}
