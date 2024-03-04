using System;
using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Converts a sample provider to 24 bit PCM, optionally clipping and adjusting volume along the way
    /// </summary>
    public class SampleToWaveProvider24 : IWaveProvider
    {
        private readonly ISampleProvider sourceProvider;
        private readonly WaveFormat waveFormat;
        private volatile float volume;
        private float[] sourceBuffer;

        /// <summary>
        /// Converts from an ISampleProvider (IEEE float) to a 16 bit PCM IWaveProvider.
        /// Number of channels and sample rate remain unchanged.
        /// </summary>
        /// <param name="sourceProvider">The input source provider</param>
        public SampleToWaveProvider24(ISampleProvider sourceProvider)
        {
            if (sourceProvider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                throw new ArgumentException("Input source provider must be IEEE float", "sourceProvider");
            if (sourceProvider.WaveFormat.BitsPerSample != 32)
                throw new ArgumentException("Input source provider must be 32 bit", "sourceProvider");

            waveFormat = new WaveFormat(sourceProvider.WaveFormat.SampleRate, 24, sourceProvider.WaveFormat.Channels);

            this.sourceProvider = sourceProvider;
            volume = 1.0f;
        }

        /// <summary>
        /// Reads audio samples from the source provider and writes them to the destination buffer.
        /// </summary>
        /// <param name="destBuffer">The destination buffer to write the audio samples to.</param>
        /// <param name="offset">The offset in the destination buffer at which to start writing.</param>
        /// <param name="numBytes">The number of bytes to read from the source provider and write to the destination buffer.</param>
        /// <returns>The actual number of bytes written to the destination buffer.</returns>
        /// <remarks>
        /// This method reads audio samples from the source provider, adjusts their volume, clips them if necessary, converts them to 24-bit samples, and writes them to the destination buffer.
        /// If the number of bytes requested is not a multiple of 3, the remaining bytes in the destination buffer will not be written to.
        /// </remarks>
        public int Read(byte[] destBuffer, int offset, int numBytes)
        {
            var samplesRequired = numBytes / 3;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, samplesRequired);
            var sourceSamples = sourceProvider.Read(sourceBuffer, 0, samplesRequired);

            int destOffset = offset;
            for (var sample = 0; sample < sourceSamples; sample++)
            {
                // adjust volume
                var sample32 = sourceBuffer[sample] * volume;
                // clip
                if (sample32 > 1.0f)
                    sample32 = 1.0f;
                if (sample32 < -1.0f)
                    sample32 = -1.0f;

                var sample24 = (int) (sample32*8388607.0);
                destBuffer[destOffset++] = (byte)(sample24);
                destBuffer[destOffset++] = (byte)(sample24 >> 8);
                destBuffer[destOffset++] = (byte)(sample24 >> 16);
            }

            return sourceSamples * 3;
        }

        /// <summary>
        /// The Format of this IWaveProvider
        /// <see cref="IWaveProvider.WaveFormat"/>
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

        /// <summary>
        /// Volume of this channel. 1.0 = full scale, 0.0 to mute
        /// </summary>
        public float Volume
        {
            get { return volume; }
            set { volume = value; }
        }
    }
}