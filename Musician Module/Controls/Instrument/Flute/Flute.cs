using System;
using System.Collections.Generic;
using System.Threading;
using Musician_Module.Domain.Values;
using Blish_HUD.Controls.Intern;
using Blish_HUD;

namespace Musician_Module.Controls.Instrument
{
    public class Flute : Instrument
    {
        private static readonly TimeSpan NoteTimeout = TimeSpan.FromMilliseconds(5);
        private static readonly TimeSpan OctaveTimeout = TimeSpan.FromTicks(500);

        private static readonly Dictionary<FluteNote.Keys, GuildWarsControls> NoteMap = new Dictionary<FluteNote.Keys, GuildWarsControls>
        {
            {FluteNote.Keys.Note1, GuildWarsControls.WeaponSkill1},
            {FluteNote.Keys.Note2, GuildWarsControls.WeaponSkill2},
            {FluteNote.Keys.Note3, GuildWarsControls.WeaponSkill3},
            {FluteNote.Keys.Note4, GuildWarsControls.WeaponSkill4},
            {FluteNote.Keys.Note5, GuildWarsControls.WeaponSkill5},
            {FluteNote.Keys.Note6, GuildWarsControls.HealingSkill},
            {FluteNote.Keys.Note7, GuildWarsControls.UtilitySkill1},
            {FluteNote.Keys.Note8, GuildWarsControls.UtilitySkill2}
        };
        private FluteNote.Octaves CurrentOctave = FluteNote.Octaves.Low;
        public Flute() { this.Preview = new FlutePreview(); }
        public override void PlayNote(Note note)
        {
            var fluteNote = FluteNote.From(note);

            if (RequiresAction(fluteNote))
            {
                if (fluteNote.Key == FluteNote.Keys.None)
                {
                    PressNote(GuildWarsControls.EliteSkill);
                }
                else
                {
                    fluteNote = OptimizeNote(fluteNote);
                    PressNote(NoteMap[fluteNote.Key]);
                }
            }
        }
        public override void GoToOctave(Note note)
        {
            var fluteNote = FluteNote.From(note);

            if (RequiresAction(fluteNote))
            {
                fluteNote = OptimizeNote(fluteNote);

                while (CurrentOctave != fluteNote.Octave)
                {
                    if (CurrentOctave < fluteNote.Octave)
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
        private static bool RequiresAction(FluteNote fluteNote)
        {
            return fluteNote.Key != FluteNote.Keys.None;
        }
        private FluteNote OptimizeNote(FluteNote note)
        {
            if (note.Equals(new FluteNote(FluteNote.Keys.Note1, FluteNote.Octaves.High)) && CurrentOctave == FluteNote.Octaves.Low)
            {
                note = new FluteNote(FluteNote.Keys.Note8, FluteNote.Octaves.Low);
            }
            else if (note.Equals(new FluteNote(FluteNote.Keys.Note8, FluteNote.Octaves.Low)) && CurrentOctave == FluteNote.Octaves.High)
            {
                note = new FluteNote(FluteNote.Keys.Note1, FluteNote.Octaves.High);
            }
            return note;
        }
        private void IncreaseOctave()
        {
            switch (CurrentOctave)
            {
                case FluteNote.Octaves.Low:
                    CurrentOctave = FluteNote.Octaves.High;
                    break;
                case FluteNote.Octaves.High:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            PressKey(GuildWarsControls.UtilitySkill3, CurrentOctave.ToString());

            Thread.Sleep(OctaveTimeout);
        }
        private void DecreaseOctave()
        {
            switch (CurrentOctave)
            {
                case FluteNote.Octaves.Low:
                    break;
                case FluteNote.Octaves.High:
                    CurrentOctave = FluteNote.Octaves.Low;
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
        protected override void PressKey(GuildWarsControls key, string octave)
        {
            if (Mode == InstrumentMode.Practice)
            {
                InstrumentSkillType noteType;
                switch (key)
                {
                    case GuildWarsControls.EliteSkill:
                        noteType = InstrumentSkillType.StopPlaying;
                        break;
                    case GuildWarsControls.UtilitySkill3:
                        noteType =
                            CurrentOctave == FluteNote.Octaves.Low ?
                            InstrumentSkillType.IncreaseOctave :
                            InstrumentSkillType.DecreaseOctave;
                        break;
                    default:
                        noteType = InstrumentSkillType.Note;
                        break;
                }
                MusicianModule.ModuleInstance.Conveyor.SpawnNoteBlock(key, noteType, Note.OctaveColors[octave]);

            } else if (Mode == InstrumentMode.Emulate) {

                Keyboard.Press(VirtualKeyShorts[key]);
                Keyboard.Release(VirtualKeyShorts[key]);

            } else if (Mode == InstrumentMode.Preview) {

                Preview.PlaySoundByKey(key);

            }
        }
    }
}