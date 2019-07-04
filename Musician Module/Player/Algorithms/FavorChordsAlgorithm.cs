using System;
using System.Diagnostics;
using System.Threading;
using Blish_HUD;
using Musician_Module.Controls.Instrument;
using Musician_Module.Domain.Values;

namespace Musician_Module.Player.Algorithms
{
    public class FavorChordsAlgorithm : IPlayAlgorithm
    {
        private bool Abort = false;
        public void Dispose() { this.Abort = true; }
        public void Play(Instrument instrument, MetronomeMark metronomeMark, ChordOffset[] melody)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            for (var strumIndex = 0; strumIndex < melody.Length;)
            {
                if (this.Abort) return;

                var strum = melody[strumIndex];

                if (stopwatch.ElapsedMilliseconds > metronomeMark.WholeNoteLength.Multiply(strum.Offest).TotalMilliseconds)
                {
                    var chord = strum.Chord;

                    foreach (var note in chord.Notes)
                    {
                        instrument.GoToOctave(note);
                        instrument.PlayNote(note);
                    }

                    strumIndex++;
                }
                else
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(1));
                }
            }

            stopwatch.Stop();
        }
    }
}