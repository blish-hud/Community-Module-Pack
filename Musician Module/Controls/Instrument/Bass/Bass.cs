using System;
using System.Collections.Generic;
using System.Threading;
using Musician_Module.Domain.Values;
using Blish_HUD.Controls.Intern;
using Blish_HUD;
using Microsoft.Xna.Framework;

namespace Musician_Module.Controls.Instrument
{
    public class Bass : Instrument
    {
        private static readonly TimeSpan NoteTimeout = TimeSpan.FromMilliseconds(5);
        private static readonly TimeSpan OctaveTimeout = TimeSpan.FromTicks(500);
        private static readonly Dictionary<BassNote.Keys, GuildWarsControls> NoteMap = new Dictionary<BassNote.Keys, GuildWarsControls>
        {
            {BassNote.Keys.Note1, GuildWarsControls.WeaponSkill1},
            {BassNote.Keys.Note2, GuildWarsControls.WeaponSkill2},
            {BassNote.Keys.Note3, GuildWarsControls.WeaponSkill3},
            {BassNote.Keys.Note4, GuildWarsControls.WeaponSkill4},
            {BassNote.Keys.Note5, GuildWarsControls.WeaponSkill5},
            {BassNote.Keys.Note6, GuildWarsControls.HealingSkill},
            {BassNote.Keys.Note7, GuildWarsControls.UtilitySkill1},
            {BassNote.Keys.Note8, GuildWarsControls.UtilitySkill2}
        };
        private BassNote.Octaves CurrentOctave = BassNote.Octaves.Low;
        public Bass() { this.Preview = new BassPreview(); }
        public override void PlayNote(Note note)
        {
            var bassNote = BassNote.From(note);

            if (RequiresAction(bassNote))
            {
                if (bassNote.Key == BassNote.Keys.None)
                {
                    PressNote(GuildWarsControls.EliteSkill);
                }
                else
                {
                    bassNote = OptimizeNote(bassNote);
                    PressNote(NoteMap[bassNote.Key]);
                }
            }
        }

        public override void GoToOctave(Note note)
        {
            var bassNote = BassNote.From(note);

            if (RequiresAction(bassNote))
            {
                bassNote = OptimizeNote(bassNote);

                while (CurrentOctave != bassNote.Octave)
                {
                    if (CurrentOctave < bassNote.Octave)
                    {
                        IncreaseOctave();
                    }
                    else
                    {
                        DecreaseOctave();
                    }
                }
            }
        }

        private static bool RequiresAction(BassNote bassNote)
        {
            return bassNote.Key != BassNote.Keys.None;
        }

        private BassNote OptimizeNote(BassNote note)
        {
            if (note.Equals(new BassNote(BassNote.Keys.Note1, BassNote.Octaves.High)) && CurrentOctave == BassNote.Octaves.Low)
            {
                note = new BassNote(BassNote.Keys.Note8, BassNote.Octaves.Low);
            }
            else if (note.Equals(new BassNote(BassNote.Keys.Note8, BassNote.Octaves.Low)) && CurrentOctave == BassNote.Octaves.High)
            {
                note = new BassNote(BassNote.Keys.Note1, BassNote.Octaves.High);
            }
            return note;
        }

        private void IncreaseOctave()
        {
            switch (CurrentOctave)
            {
                case BassNote.Octaves.Low:
                    CurrentOctave = BassNote.Octaves.High;
                    break;
                case BassNote.Octaves.High:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            PressKey(GuildWarsControls.EliteSkill, CurrentOctave.ToString());

            Thread.Sleep(OctaveTimeout);
        }

        private void DecreaseOctave()
        {
            switch (CurrentOctave)
            {
                case BassNote.Octaves.Low:
                    break;
                case BassNote.Octaves.High:
                    CurrentOctave = BassNote.Octaves.Low;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            PressKey(GuildWarsControls.UtilitySkill3, CurrentOctave.ToString());

            Thread.Sleep(OctaveTimeout);
        }

        private void PressNote(GuildWarsControls key)
        {
            PressKey(key, CurrentOctave.ToString());

            Thread.Sleep(NoteTimeout);
        }
    }
}