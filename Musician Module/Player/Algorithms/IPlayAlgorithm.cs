using Musician_Module.Controls.Instrument;
using Musician_Module.Domain.Values;
using Musician_Module.Controls;
namespace Musician_Module.Player.Algorithms
{
    public interface IPlayAlgorithm
    {
        void Play(Instrument instrument, MetronomeMark metronomeMark, ChordOffset[] melody);
        void Dispose();
    }
}