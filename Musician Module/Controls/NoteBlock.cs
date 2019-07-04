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


    /// <summary>
    /// TODO: Add _octave as conditional for conveyor lane, colour and texture.
    /// </summary>
    public class NoteBlock : Control {

        private static readonly Dictionary<InstrumentSkillType, Color> NoteColor = new Dictionary<InstrumentSkillType, Color>
        {
            {InstrumentSkillType.LowNote, Color.Blue},
            {InstrumentSkillType.MiddleNote, Color.Green},
            {InstrumentSkillType.HighNote, Color.Red},
            {InstrumentSkillType.IncreaseOctaveToMiddle, Color.Green},
            {InstrumentSkillType.IncreaseOctaveToHigh, Color.Red},
            {InstrumentSkillType.DecreaseOctaveToLow, Color.Blue},
            {InstrumentSkillType.DecreaseOctaveToMiddle, Color.Green},
            {InstrumentSkillType.StopPlaying, Color.Black}
        };
        private static readonly Dictionary<InstrumentSkillType, Texture2D> NoteTexture = new Dictionary<InstrumentSkillType, Texture2D>
        {
            {InstrumentSkillType.LowNote, MusicianModule.ModuleInstance.ContentsManager.GetTexture("note_block.png")},
            {InstrumentSkillType.MiddleNote, MusicianModule.ModuleInstance.ContentsManager.GetTexture("note_block.png")},
            {InstrumentSkillType.HighNote, MusicianModule.ModuleInstance.ContentsManager.GetTexture("note_block.png")},
            {InstrumentSkillType.IncreaseOctaveToMiddle, MusicianModule.ModuleInstance.ContentsManager.GetTexture("incr_octave.png")},
            {InstrumentSkillType.IncreaseOctaveToHigh, MusicianModule.ModuleInstance.ContentsManager.GetTexture("incr_octave.png")},
            {InstrumentSkillType.DecreaseOctaveToLow, MusicianModule.ModuleInstance.ContentsManager.GetTexture("decr_octave.png")},
            {InstrumentSkillType.DecreaseOctaveToMiddle, MusicianModule.ModuleInstance.ContentsManager.GetTexture("decr_octave.png")},
            {InstrumentSkillType.StopPlaying, MusicianModule.ModuleInstance.ContentsManager.GetTexture("pause_block.png")}
        };
        public Glide.Tween NoteAnim = null;
        public int Lane;
        private Color SpriteColor;
        private IKeyboard fKeyboard;
        private GuildWarsControls fKey;
        private Texture2D NoteSprite;
        private IKeyboard keyboard;
        private GuildWarsControls key;
        private InstrumentSkillType noteType;

        public NoteBlock(IKeyboard _keyboard, GuildWarsControls _key, InstrumentSkillType _noteType) {
            this.ZIndex = 0;
            switch (_key)
            {
                case GuildWarsControls.WeaponSkill1:
                    this.Lane = 13;
                    break;
                case GuildWarsControls.WeaponSkill2:
                    this.Lane = 75;
                    break;
                case GuildWarsControls.WeaponSkill3:
                    this.Lane = 136;
                    break;
                case GuildWarsControls.WeaponSkill4:
                    this.Lane = 197;
                    break;
                case GuildWarsControls.WeaponSkill5:
                    this.Lane = 260;
                    break;
                case GuildWarsControls.HealingSkill:
                    this.Lane = 429;
                    break;
                case GuildWarsControls.UtilitySkill1:
                    this.Lane = 491;
                    break;
                case GuildWarsControls.UtilitySkill2:
                    this.Lane = 552;
                    break;
                case GuildWarsControls.UtilitySkill3:
                    this.Lane = 614;
                    break;
                case GuildWarsControls.EliteSkill:
                    this.Lane = 675;
                    break;
            }
            this.fKeyboard = _keyboard;
            this.fKey = _key;
            this.NoteSprite = NoteTexture[_noteType];
            this.SpriteColor = NoteColor[_noteType];
            this.Size = new Point(56, 20); // set static bounds.
            this.Location = new Point(Lane, 0 - this.Width);
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
        }

        protected override CaptureType CapturesInput() {
            return CaptureType.None;
        }
        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds) {
            spriteBatch.DrawOnCtrl(this, this.NoteSprite, new Rectangle(0, 0, 56, 20), null, this.SpriteColor, 0f, Vector2.Zero, SpriteEffects.None);
        }
    }
}
