using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    class Stereo16SampleChunkConverter : ISampleChunkConverter
    {
        private int sourceSample;
        private byte[] sourceBuffer;
        private WaveBuffer sourceWaveBuffer;
        private int sourceSamples;

        /// <summary>
        /// Checks if the specified wave format is supported for PCM encoding with 16 bits per sample and 2 channels.
        /// </summary>
        /// <param name="waveFormat">The wave format to be checked for support.</param>
        /// <returns>True if the specified wave format is supported; otherwise, false.</returns>
        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.Pcm &&
                waveFormat.BitsPerSample == 16 &&
                waveFormat.Channels == 2;
        }

        /// <summary>
        /// Loads the next chunk of audio data from the specified wave provider.
        /// </summary>
        /// <param name="source">The wave provider from which to load the audio data.</param>
        /// <param name="samplePairsRequired">The number of sample pairs required for the next chunk.</param>
        /// <remarks>
        /// This method calculates the number of bytes required from the wave provider based on the specified number of sample pairs required.
        /// It then ensures that the source buffer is large enough to accommodate the required number of bytes.
        /// The method reads the audio data into the source buffer, converts it to sample pairs, and updates the source sample and source samples fields accordingly.
        /// </remarks>
        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            int sourceBytesRequired = samplePairsRequired * 4;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
            sourceWaveBuffer = new WaveBuffer(sourceBuffer);
            sourceSamples = source.Read(sourceBuffer, 0, sourceBytesRequired) / 2;
            sourceSample = 0;
        }

        /// <summary>
        /// Retrieves the next pair of audio samples from the source buffer and returns them as floating-point values.
        /// </summary>
        /// <param name="sampleLeft">When this method returns, contains the next audio sample from the left channel as a floating-point value.</param>
        /// <param name="sampleRight">When this method returns, contains the next audio sample from the right channel as a floating-point value.</param>
        /// <returns>True if the next samples are successfully retrieved; otherwise, false.</returns>
        /// <remarks>
        /// This method retrieves the next pair of audio samples from the source buffer and converts them to floating-point values by dividing the sample values by 32768.0f.
        /// If there are no more samples available in the source buffer, this method returns false and sets both sampleLeft and sampleRight to 0.0f.
        /// </remarks>
        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (sourceSample < sourceSamples)
            {
                sampleLeft = sourceWaveBuffer.ShortBuffer[sourceSample++] / 32768.0f;
                sampleRight = sourceWaveBuffer.ShortBuffer[sourceSample++] / 32768.0f;
                return true;
            }
            else
            {
                sampleLeft = 0.0f;
                sampleRight = 0.0f;
                return false;
            }
        }
    }
}
