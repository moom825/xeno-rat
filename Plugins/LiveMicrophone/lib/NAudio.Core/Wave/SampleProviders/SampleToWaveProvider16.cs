using System;
using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Converts a sample provider to 16 bit PCM, optionally clipping and adjusting volume along the way
    /// </summary>
    public class SampleToWaveProvider16 : IWaveProvider
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
        public SampleToWaveProvider16(ISampleProvider sourceProvider)
        {
            if (sourceProvider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                throw new ArgumentException("Input source provider must be IEEE float", nameof(sourceProvider));
            if (sourceProvider.WaveFormat.BitsPerSample != 32)
                throw new ArgumentException("Input source provider must be 32 bit", nameof(sourceProvider));

            waveFormat = new WaveFormat(sourceProvider.WaveFormat.SampleRate, 16, sourceProvider.WaveFormat.Channels);

            this.sourceProvider = sourceProvider;
            volume = 1.0f;
        }

        /// <summary>
        /// Reads audio data from the source provider and writes it to the destination buffer.
        /// </summary>
        /// <param name="destBuffer">The destination buffer to write the audio data to.</param>
        /// <param name="offset">The offset in the destination buffer to start writing the audio data.</param>
        /// <param name="numBytes">The number of bytes to read from the source provider and write to the destination buffer.</param>
        /// <returns>The number of bytes written to the destination buffer.</returns>
        /// <remarks>
        /// This method reads audio data from the source provider, adjusts the volume, clips the samples, and writes the processed data to the destination buffer.
        /// The source buffer is dynamically resized to accommodate the required number of samples.
        /// </remarks>
        public int Read(byte[] destBuffer, int offset, int numBytes)
        {
            int samplesRequired = numBytes / 2;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, samplesRequired);
            int sourceSamples = sourceProvider.Read(sourceBuffer, 0, samplesRequired);
            var destWaveBuffer = new WaveBuffer(destBuffer);

            int destOffset = offset / 2;
            for (int sample = 0; sample < sourceSamples; sample++)
            {
                // adjust volume
                float sample32 = sourceBuffer[sample] * volume;
                // clip
                if (sample32 > 1.0f)
                    sample32 = 1.0f;
                if (sample32 < -1.0f)
                    sample32 = -1.0f;
                destWaveBuffer.ShortBuffer[destOffset++] = (short)(sample32 * 32767);
            }

            return sourceSamples * 2;
        }

        /// <summary>
        /// <see cref="IWaveProvider.WaveFormat"/>
        /// </summary>
        public WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Volume of this channel. 1.0 = full scale
        /// </summary>
        public float Volume
        {
            get { return volume; }
            set { volume = value; }
        }
    }
}
