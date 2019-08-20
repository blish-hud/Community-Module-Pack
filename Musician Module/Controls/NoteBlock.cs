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
        private readonly Texture2D NoteSprite;
        private readonly Color SpriteColor;
        private readonly GuildWarsControls NoteKey;
        private readonly int XOffset;

        private Glide.Tween NoteAnim = null;

        public NoteBlock(GuildWarsControls _key, InstrumentSkillType _noteType, Color _spriteColor) {
            this.ZIndex = 1;
            this.Size = new Point(56, 20);
            this.NoteSprite = NoteTextures[_noteType];
            this.SpriteColor = _spriteColor;
            this.NoteKey = _key;
            this.XOffset = Conveyor.LaneCoordinatesX[_key];
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
                foreach (NoteBlock note in MusicianModule.ModuleInstance.Conveyor.Children)
                {
                    note.NoteAnim.Pause();
                }
                MusicianModule.ModuleInstance.MusicPlayer.Worker.Join();
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
