using System;
using Musician_Module.Domain.Values;
using Blish_HUD;
using Blish_HUD.Controls.Intern;
using System.Collections.Generic;
using Blish_HUD.Controls.Extern;

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
        protected static readonly Dictionary<GuildWarsControls, VirtualKeyShort> VirtualKeyShorts = new Dictionary<GuildWarsControls, VirtualKeyShort>
        {
            {GuildWarsControls.WeaponSkill1, VirtualKeyShort.KEY_1},
            {GuildWarsControls.WeaponSkill2, VirtualKeyShort.KEY_2},
            {GuildWarsControls.WeaponSkill3, VirtualKeyShort.KEY_3},
            {GuildWarsControls.WeaponSkill4, VirtualKeyShort.KEY_4},
            {GuildWarsControls.WeaponSkill5, VirtualKeyShort.KEY_5},
            {GuildWarsControls.HealingSkill, VirtualKeyShort.KEY_6},
            {GuildWarsControls.UtilitySkill1, VirtualKeyShort.KEY_7},
            {GuildWarsControls.UtilitySkill2, VirtualKeyShort.KEY_8},
            {GuildWarsControls.UtilitySkill3, VirtualKeyShort.KEY_9},
            {GuildWarsControls.EliteSkill, VirtualKeyShort.KEY_0}
        };

        protected IInstrumentPreview Preview;
        public InstrumentMode Mode { get; set; }

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

                Keyboard.Press(VirtualKeyShorts[key]);
                Keyboard.Release(VirtualKeyShorts[key]);

            } else if (Mode == InstrumentMode.Preview) {

                Preview.PlaySoundByKey(key);

            }
        }
        public abstract void PlayNote(Note note);
        public abstract void GoToOctave(Note note);
    }
}