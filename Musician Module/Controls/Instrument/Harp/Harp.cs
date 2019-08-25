using System;
using System.Collections.Generic;
using System.Threading;
using Blish_HUD;
using Blish_HUD.Controls.Intern;
using Musician_Module.Domain.Values;
namespace Musician_Module.Controls.Instrument
{
    public class Harp : Instrument
    {
        private static readonly TimeSpan NoteTimeout = TimeSpan.FromMilliseconds(5);
        private static readonly TimeSpan OctaveTimeout = TimeSpan.FromTicks(500);
        private static readonly Dictionary<HarpNote.Keys, GuildWarsControls> NoteMap = new Dictionary<HarpNote.Keys, GuildWarsControls>
        {
            {HarpNote.Keys.Note1, GuildWarsControls.WeaponSkill1},
            {HarpNote.Keys.Note2, GuildWarsControls.WeaponSkill2},
            {HarpNote.Keys.Note3, GuildWarsControls.WeaponSkill3},
            {HarpNote.Keys.Note4, GuildWarsControls.WeaponSkill4},
            {HarpNote.Keys.Note5, GuildWarsControls.WeaponSkill5},
            {HarpNote.Keys.Note6, GuildWarsControls.HealingSkill},
            {HarpNote.Keys.Note7, GuildWarsControls.UtilitySkill1},
            {HarpNote.Keys.Note8, GuildWarsControls.UtilitySkill2}
        };
        private HarpNote.Octaves CurrentOctave = HarpNote.Octaves.Middle;
        public Harp() { this.Preview = new HarpPreview(); }
        public override void PlayNote(Note note)
        {
            var harpNote = HarpNote.From(note);

            if (RequiresAction(harpNote))
            {
                harpNote = OptimizeNote(harpNote);
                PressNote(NoteMap[harpNote.Key]);
            }
        }
        public override void GoToOctave(Note note)
        {
            var harpNote = HarpNote.From(note);

            if (RequiresAction(harpNote))
            {
                harpNote = OptimizeNote(harpNote);

                while (CurrentOctave != harpNote.Octave)
                {
                    if (CurrentOctave < harpNote.Octave)
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
        private static bool RequiresAction(HarpNote harpNote)
        {
            return harpNote.Key != HarpNote.Keys.None;
        }
        private HarpNote OptimizeNote(HarpNote note)
        {
            if (note.Equals(new HarpNote(HarpNote.Keys.Note1, HarpNote.Octaves.Middle)) && CurrentOctave == HarpNote.Octaves.Low)
            {
                note = new HarpNote(HarpNote.Keys.Note8, HarpNote.Octaves.Low);
            }
            else if (note.Equals(new HarpNote(HarpNote.Keys.Note1, HarpNote.Octaves.High)) && CurrentOctave == HarpNote.Octaves.Middle)
            {
                note = new HarpNote(HarpNote.Keys.Note8, HarpNote.Octaves.Middle);
            }
            return note;
        }
        private void IncreaseOctave()
        {
            switch (CurrentOctave)
            {
                case HarpNote.Octaves.Low:
                    CurrentOctave = HarpNote.Octaves.Middle;
                    break;
                case HarpNote.Octaves.Middle:
                    CurrentOctave = HarpNote.Octaves.High;
                    break;
                case HarpNote.Octaves.High:
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
                case HarpNote.Octaves.Low:
                    break;
                case HarpNote.Octaves.Middle:
                    CurrentOctave = HarpNote.Octaves.Low;
                    break;
                case HarpNote.Octaves.High:
                    CurrentOctave = HarpNote.Octaves.Middle;
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