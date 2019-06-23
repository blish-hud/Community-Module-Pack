using System.Collections.Generic;
using System.Linq;
using NAudio.Vorbis;
using NAudio.Wave;

namespace Musician_Module.Player.Sound
{
    public class CachedSound
    {
        public CachedSound(ISampleProvider vorbisWaveReader)
        {
            WaveFormat = vorbisWaveReader.WaveFormat;

            var wholeFile = new List<float>();
            var readBuffer = new float[vorbisWaveReader.WaveFormat.SampleRate*vorbisWaveReader.WaveFormat.Channels];

            int samplesRead;
            while ((samplesRead = vorbisWaveReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
            {
                wholeFile.AddRange(readBuffer.Take(samplesRead));
            }

            AudioData = wholeFile.ToArray();
        }

        public float[] AudioData { get; }

        public WaveFormat WaveFormat { get; }
    }
}