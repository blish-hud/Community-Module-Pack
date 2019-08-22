using System;
using Musician_Module.Domain.Values;
using Blish_HUD;
using Blish_HUD.Controls.Intern;

namespace Musician_Module.Controls.Instrument
{
    public enum InstrumentSkillType
    {
        None,
        Note,
        IncreaseOctave,
        DecreaseOctave,
        StopPlaying
    }
    public enum InstrumentMode
    {
        None,
        Preview,
        Practice,
        Emulate
    }
    public abstract class Instrument
    {
        protected IKeyboard PreviewKeyboard;
        protected IKeyboard PracticeKeyboard;
        protected IKeyboard EmulatedKeyboard;
        public InstrumentMode Mode { get; set; }

        public Instrument(){
            PracticeKeyboard = new KeyboardPractice();
            EmulatedKeyboard = new Keyboard();
        }
        public bool IsInstrument(string instrument) {
            return string.Equals(this.GetType().Name, instrument, StringComparison.OrdinalIgnoreCase);
        }
        protected virtual void PressKey(GuildWarsControls key, string octave)
        {
            if (Mode == InstrumentMode.Practice)
            {
                InstrumentSkillType noteType;
                switch (key)
                {
                    case GuildWarsControls.EliteSkill:
                        noteType = InstrumentSkillType.IncreaseOctave;
                        break;
                    case GuildWarsControls.UtilitySkill3:
                        noteType = InstrumentSkillType.DecreaseOctave;
                        break;
                    default:
                        noteType = InstrumentSkillType.Note;
                        break;
                }
                MusicianModule.ModuleInstance.Conveyor.SpawnNoteBlock(key, noteType, Note.OctaveColors[octave]);

            } else if (Mode == InstrumentMode.Emulate) {

                EmulatedKeyboard.Press(key);
                EmulatedKeyboard.Release(key);

            } else if (Mode == InstrumentMode.Preview) {

                PreviewKeyboard.Press(key);
                PreviewKeyboard.Release(key);

            }
        }
        public abstract void PlayNote(Note note);
        public abstract void GoToOctave(Note note);
    }
}