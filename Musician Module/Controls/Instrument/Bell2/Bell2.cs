using System;
using System.Collections.Generic;
using System.Threading;
using Musician_Module.Domain.Values;
using Blish_HUD.Controls.Intern;
using Blish_HUD;

namespace Musician_Module.Controls.Instrument
{
    public class Bell2 : Instrument
    {
        private static readonly TimeSpan NoteTimeout = TimeSpan.FromMilliseconds(5);
        private static readonly TimeSpan OctaveTimeout = TimeSpan.FromTicks(500);
        private static readonly Dictionary<Bell2Note.Keys, GuildWarsControls> NoteMap = new Dictionary<Bell2Note.Keys, GuildWarsControls>
        {
            {Bell2Note.Keys.Note1, GuildWarsControls.WeaponSkill1},
            {Bell2Note.Keys.Note2, GuildWarsControls.WeaponSkill2},
            {Bell2Note.Keys.Note3, GuildWarsControls.WeaponSkill3},
            {Bell2Note.Keys.Note4, GuildWarsControls.WeaponSkill4},
            {Bell2Note.Keys.Note5, GuildWarsControls.WeaponSkill5},
            {Bell2Note.Keys.Note6, GuildWarsControls.HealingSkill},
            {Bell2Note.Keys.Note7, GuildWarsControls.UtilitySkill1},
            {Bell2Note.Keys.Note8, GuildWarsControls.UtilitySkill2}
        };
        private Bell2Note.Octaves CurrentOctave = Bell2Note.Octaves.Low;
        public Bell2(){ this.Preview = new Bell2Preview(); }
        public override void PlayNote(Note note)
        {
            var bell2Note = Bell2Note.From(note);

            if (RequiresAction(bell2Note))
            {
                if (bell2Note.Key == Bell2Note.Keys.None)
                {
                    PressNote(GuildWarsControls.EliteSkill);
                }
                else
                {
                    bell2Note = OptimizeNote(bell2Note);
                    PressNote(NoteMap[bell2Note.Key]);
                }
            }
        }
        public override void GoToOctave(Note note)
        {
            var bell2Note = Bell2Note.From(note);

            if (RequiresAction(bell2Note))
            {
                bell2Note = OptimizeNote(bell2Note);

                while (CurrentOctave != bell2Note.Octave)
                {
                    if (CurrentOctave < bell2Note.Octave)
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
        private static bool RequiresAction(Bell2Note bell2Note)
        {
            return bell2Note.Key != Bell2Note.Keys.None;
        }
        private Bell2Note OptimizeNote(Bell2Note note)
        {
            if (note.Equals(new Bell2Note(Bell2Note.Keys.Note1, Bell2Note.Octaves.High)) && CurrentOctave == Bell2Note.Octaves.Low)
            {
                note = new Bell2Note(Bell2Note.Keys.Note8, Bell2Note.Octaves.Low);
            }
            else if (note.Equals(new Bell2Note(Bell2Note.Keys.Note8, Bell2Note.Octaves.Low)) && CurrentOctave == Bell2Note.Octaves.High)
            {
                note = new Bell2Note(Bell2Note.Keys.Note1, Bell2Note.Octaves.High);
            }
            return note;
        }
        private void IncreaseOctave()
        {
            switch (CurrentOctave)
            {
                case Bell2Note.Octaves.Low:
                    CurrentOctave = Bell2Note.Octaves.High;
                    break;
                case Bell2Note.Octaves.High:
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
                case Bell2Note.Octaves.Low:
                    break;
                case Bell2Note.Octaves.High:
                    CurrentOctave = Bell2Note.Octaves.Low;
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