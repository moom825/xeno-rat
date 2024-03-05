using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    class Mono8SampleChunkConverter : ISampleChunkConverter
    {
        private int offset;
        private byte[] sourceBuffer;
        private int sourceBytes;

        /// <summary>
        /// Checks if the specified wave format is supported for PCM encoding, 8 bits per sample, and single channel.
        /// </summary>
        /// <param name="waveFormat">The wave format to be checked for support.</param>
        /// <returns>True if the specified wave format is supported; otherwise, false.</returns>
        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.Pcm &&
                waveFormat.BitsPerSample == 8 &&
                waveFormat.Channels == 1;
        }

        /// <summary>
        /// Loads the next chunk of audio data from the provided wave source.
        /// </summary>
        /// <param name="source">The wave provider from which to load the audio data.</param>
        /// <param name="samplePairsRequired">The number of sample pairs required for the next chunk.</param>
        /// <remarks>
        /// This method ensures that the source buffer is large enough to accommodate the required number of sample pairs.
        /// It then reads the audio data from the source into the source buffer and resets the offset to 0 for subsequent processing.
        /// </remarks>
        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            int sourceBytesRequired = samplePairsRequired;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
            sourceBytes = source.Read(sourceBuffer, 0, sourceBytesRequired);
            offset = 0;
        }

        /// <summary>
        /// Retrieves the next sample from the source buffer and returns it as the left and right audio samples.
        /// </summary>
        /// <param name="sampleLeft">The variable to store the left audio sample.</param>
        /// <param name="sampleRight">The variable to store the right audio sample.</param>
        /// <returns>True if the next sample is successfully retrieved; otherwise, false.</returns>
        /// <remarks>
        /// This method retrieves the next sample from the source buffer and assigns it to the <paramref name="sampleLeft"/> and <paramref name="sampleRight"/> parameters.
        /// If the end of the source buffer is reached, the method returns false and assigns 0.0f to both <paramref name="sampleLeft"/> and <paramref name="sampleRight"/>.
        /// </remarks>
        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (offset < sourceBytes)
            {
                sampleLeft = sourceBuffer[offset] / 256f;
                offset++;
                sampleRight = sampleLeft;
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
