using System;
using System.Collections.Generic;
using System.Threading;
using Musician_Module.Domain.Values;
using Blish_HUD.Controls.Intern;
using Blish_HUD;

namespace Musician_Module.Controls.Instrument
{
    public class Lute : Instrument
    {
        private static readonly TimeSpan NoteTimeout = TimeSpan.FromMilliseconds(5);
        private static readonly TimeSpan OctaveTimeout = TimeSpan.FromTicks(500);
        private static readonly Dictionary<LuteNote.Keys, GuildWarsControls> NoteMap = new Dictionary<LuteNote.Keys, GuildWarsControls>
        {
            {LuteNote.Keys.Note1, GuildWarsControls.WeaponSkill1},
            {LuteNote.Keys.Note2, GuildWarsControls.WeaponSkill2},
            {LuteNote.Keys.Note3, GuildWarsControls.WeaponSkill3},
            {LuteNote.Keys.Note4, GuildWarsControls.WeaponSkill4},
            {LuteNote.Keys.Note5, GuildWarsControls.WeaponSkill5},
            {LuteNote.Keys.Note6, GuildWarsControls.HealingSkill},
            {LuteNote.Keys.Note7, GuildWarsControls.UtilitySkill1},
            {LuteNote.Keys.Note8, GuildWarsControls.UtilitySkill2}
        };
        private LuteNote.Octaves CurrentOctave = LuteNote.Octaves.Low;
        public Lute() { this.Preview = new LutePreview(); }
        public override void PlayNote(Note note)
        {
            var luteNote = LuteNote.From(note);

            if (RequiresAction(luteNote))
            {
                if (luteNote.Key == LuteNote.Keys.None)
                {
                    PressNote(GuildWarsControls.EliteSkill);
                }
                else
                {
                    luteNote = OptimizeNote(luteNote);
                    PressNote(NoteMap[luteNote.Key]);
                }
            }
        }

        public override void GoToOctave(Note note)
        {
            var luteNote = LuteNote.From(note);

            if (RequiresAction(luteNote))
            {
                luteNote = OptimizeNote(luteNote);

                while (CurrentOctave != luteNote.Octave)
                {
                    if (CurrentOctave < luteNote.Octave)
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
        private static bool RequiresAction(LuteNote luteNote)
        {
            return luteNote.Key != LuteNote.Keys.None;
        }
        private LuteNote OptimizeNote(LuteNote note)
        {
            if (note.Equals(new LuteNote(LuteNote.Keys.Note1, LuteNote.Octaves.High)) && CurrentOctave == LuteNote.Octaves.Middle)
            {
                note = new LuteNote(LuteNote.Keys.Note8, LuteNote.Octaves.Middle);
            }
            else if (note.Equals(new LuteNote(LuteNote.Keys.Note8, LuteNote.Octaves.Middle)) && CurrentOctave == LuteNote.Octaves.High)
            {
                note = new LuteNote(LuteNote.Keys.Note1, LuteNote.Octaves.High);
            }
            else if (note.Equals(new LuteNote(LuteNote.Keys.Note1, LuteNote.Octaves.Middle)) && CurrentOctave == LuteNote.Octaves.Low)
            {
                note = new LuteNote(LuteNote.Keys.Note8, LuteNote.Octaves.Low);
            }
            else if (note.Equals(new LuteNote(LuteNote.Keys.Note8, LuteNote.Octaves.Low)) && CurrentOctave == LuteNote.Octaves.Middle)
            {
                note = new LuteNote(LuteNote.Keys.Note1, LuteNote.Octaves.Middle);
            }
            return note;
        }
        private void IncreaseOctave()
        {
            switch (CurrentOctave)
            {
                case LuteNote.Octaves.Low:
                    CurrentOctave = LuteNote.Octaves.Middle;
                    break;
                case LuteNote.Octaves.Middle:
                    CurrentOctave = LuteNote.Octaves.High;
                    break;
                case LuteNote.Octaves.High:
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
                case LuteNote.Octaves.Low:
                    break;
                case LuteNote.Octaves.Middle:
                    CurrentOctave = LuteNote.Octaves.Low;
                    break;
                case LuteNote.Octaves.High:
                    CurrentOctave = LuteNote.Octaves.Middle;
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