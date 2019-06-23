using System;
using System.Collections.Generic;
using System.Threading;
using Musician_Module.Domain.Values;
using Microsoft.Xna.Framework;
namespace Musician_Module.Controls.Instrument
{
    public enum InstrumentSkillType
    {
        None,
        LowNote,
        MiddleNote,
        HighNote,
        IncreaseOctaveToMiddle,
        IncreaseOctaveToHigh,
        DecreaseOctaveToLow,
        DecreaseOctaveToMiddle,
        StopPlaying
    }
    public abstract class Instrument
    {
        public bool IsInstrument(string instrument) {
            return string.Equals(this.GetType().Name, instrument, StringComparison.OrdinalIgnoreCase);
        }
        public abstract void PlayNote(Note note);
        public abstract void GoToOctave(Note note);
    }
}