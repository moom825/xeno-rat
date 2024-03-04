using System;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Utility class for converting to SampleProvider
    /// </summary>
    static class SampleProviderConverters
    {

        /// <summary>
        /// Converts the specified WaveProvider into a SampleProvider.
        /// </summary>
        /// <param name="waveProvider">The WaveProvider to be converted.</param>
        /// <returns>A SampleProvider representing the converted WaveProvider.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the bit depth of the WaveProvider is unsupported.</exception>
        /// <exception cref="ArgumentException">Thrown when the encoding of the WaveProvider is unsupported.</exception>
        /// <remarks>
        /// This method converts the input WaveProvider into a SampleProvider based on its encoding and bit depth.
        /// If the WaveProvider's encoding is PCM, it creates a corresponding PCM-to-SampleProvider converter based on the bit depth.
        /// If the WaveProvider's encoding is IEEE Float, it creates a corresponding Wave-to-SampleProvider converter.
        /// If the WaveProvider's encoding is not supported, an ArgumentException is thrown.
        /// If the bit depth of the WaveProvider is not supported, an InvalidOperationException is thrown.
        /// </remarks>
        public static ISampleProvider ConvertWaveProviderIntoSampleProvider(IWaveProvider waveProvider)
        {
            ISampleProvider sampleProvider;
            if (waveProvider.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                // go to float
                if (waveProvider.WaveFormat.BitsPerSample == 8)
                {
                    sampleProvider = new Pcm8BitToSampleProvider(waveProvider);
                }
                else if (waveProvider.WaveFormat.BitsPerSample == 16)
                {
                    sampleProvider = new Pcm16BitToSampleProvider(waveProvider);
                }
                else if (waveProvider.WaveFormat.BitsPerSample == 24)
                {
                    sampleProvider = new Pcm24BitToSampleProvider(waveProvider);
                }
                else if (waveProvider.WaveFormat.BitsPerSample == 32)
                {
                    sampleProvider = new Pcm32BitToSampleProvider(waveProvider);
                }
                else
                {
                    throw new InvalidOperationException("Unsupported bit depth");
                }
            }
            else if (waveProvider.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                if (waveProvider.WaveFormat.BitsPerSample == 64)
                    sampleProvider = new WaveToSampleProvider64(waveProvider);
                else
                    sampleProvider = new WaveToSampleProvider(waveProvider);
            }
            else
            {
                throw new ArgumentException("Unsupported source encoding");
            }
            return sampleProvider;
        }
    }
}
