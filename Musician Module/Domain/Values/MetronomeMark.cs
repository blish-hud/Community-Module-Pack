using Blish_HUD;
using System;

namespace Musician_Module.Domain.Values
{
    public class MetronomeMark
    {
        public MetronomeMark(int metronome, Fraction beatsPerMeasure)
        {
            BeatsPerMeasure = beatsPerMeasure;
            Metronome = metronome;

            QuaterNoteLength = TimeSpan.FromMinutes(1)
                .Divide(metronome*16/beatsPerMeasure.Denominator);

            WholeNoteLength = TimeSpan.FromMinutes(1)
                .Divide(metronome*16/beatsPerMeasure.Denominator)
                .Multiply(4);
        }

        public int Metronome { get; }
        public Fraction BeatsPerMeasure { get; }
        public TimeSpan QuaterNoteLength { get; }
        public TimeSpan WholeNoteLength { get; }
    }
}