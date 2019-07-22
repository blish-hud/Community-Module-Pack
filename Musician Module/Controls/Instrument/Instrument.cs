﻿using System;
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
    public abstract class Instrument
    {
        public readonly IKeyboard PreviewKeyboard;
        public IKeyboard Keyboard { get; set; }
        public Instrument(IKeyboard previewkeyboard)
        {
            PreviewKeyboard = previewkeyboard;
        }
        public bool IsInstrument(string instrument) {
            return string.Equals(this.GetType().Name, instrument, StringComparison.OrdinalIgnoreCase);
        }
        protected virtual void PressKey(GuildWarsControls key, string octave)
        {
            if (Keyboard is KeyboardPractice)
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
            }
            Keyboard.Press(key);
            Keyboard.Release(key);
        }
        public abstract void PlayNote(Note note);
        public abstract void GoToOctave(Note note);
    }
}