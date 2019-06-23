using System.Text;
using Musician_Module.Domain.Values;

namespace Musician_Module.Notation.Serializer
{
    public class ChordOffsetSerializer
    {
        private readonly ChordSerializer _chordSerializer;

        public ChordOffsetSerializer(ChordSerializer chordSerializer)
        {
            _chordSerializer = chordSerializer;
        }

        public string Serialize(ChordOffset chordOffset)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(_chordSerializer.Serialize(chordOffset.Chord));

            return stringBuilder.ToString();
        }
    }
}