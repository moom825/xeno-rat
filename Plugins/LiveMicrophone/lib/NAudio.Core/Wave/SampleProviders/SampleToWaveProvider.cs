using System;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Helper class for when you need to convert back to an IWaveProvider from
    /// an ISampleProvider. Keeps it as IEEE float
    /// </summary>
    public class SampleToWaveProvider : IWaveProvider
    {
        private readonly ISampleProvider source;

        /// <summary>
        /// Initializes a new instance of the WaveProviderFloatToWaveProvider class
        /// </summary>
        /// <param name="source">Source wave provider</param>
        public SampleToWaveProvider(ISampleProvider source)
        {
            if (source.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                throw new ArgumentException("Must be already floating point");
            }
            this.source = source;
        }

        /// <summary>
        /// Reads audio samples from the buffer and returns the number of bytes read.
        /// </summary>
        /// <param name="buffer">The input buffer containing audio samples.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin reading.</param>
        /// <param name="count">The number of bytes to read from <paramref name="buffer"/>.</param>
        /// <returns>The number of bytes read from the buffer, which is a multiple of 4 due to the audio sample size.</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            int samplesNeeded = count / 4;
            var wb = new WaveBuffer(buffer);
            int samplesRead = source.Read(wb.FloatBuffer, offset / 4, samplesNeeded);
            return samplesRead * 4;
        }

        /// <summary>
        /// The waveformat of this WaveProvider (same as the source)
        /// </summary>
        public WaveFormat WaveFormat => source.WaveFormat;
    }
}
