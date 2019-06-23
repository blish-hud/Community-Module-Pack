using System.Linq;
using System.Threading;
using Musician_Module.Controls.Instrument;
using Musician_Module.Domain;
using Musician_Module.Player.Algorithms;
using Musician_Module.Controls;
namespace Musician_Module.Player
{
    public class MusicPlayer
    {
        public Thread Worker { get; private set; }
        public IPlayAlgorithm Algorithm { get; private set; }
        public void Dispose()
        {
            Algorithm.Dispose();
        }
        public MusicPlayer(MusicSheet musicSheet, Instrument instrument, IPlayAlgorithm algorithm)
        {
            Algorithm = algorithm;
            Worker = new Thread(() => algorithm.Play(instrument, musicSheet.MetronomeMark, musicSheet.Melody.ToArray()));
        }
    }
}