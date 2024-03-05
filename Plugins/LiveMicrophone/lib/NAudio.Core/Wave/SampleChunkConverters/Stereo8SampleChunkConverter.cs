using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    class Stereo8SampleChunkConverter : ISampleChunkConverter
    {
        private int offset;
        private byte[] sourceBuffer;
        private int sourceBytes;

        /// <summary>
        /// Checks if the given WaveFormat is supported.
        /// </summary>
        /// <param name="waveFormat">The WaveFormat to be checked for support.</param>
        /// <returns>True if the WaveFormat is PCM encoded with 8 bits per sample and 2 channels; otherwise, false.</returns>
        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.Pcm &&
                waveFormat.BitsPerSample == 8 &&
                waveFormat.Channels == 2;
        }

        /// <summary>
        /// Loads the next chunk of audio data from the specified wave provider.
        /// </summary>
        /// <param name="source">The wave provider from which to load the audio data.</param>
        /// <param name="samplePairsRequired">The number of sample pairs required for the next chunk of audio data.</param>
        /// <remarks>
        /// This method loads the next chunk of audio data from the specified wave provider.
        /// It calculates the number of bytes required based on the sample pairs required and ensures that the source buffer is large enough to hold the data.
        /// The method then reads the specified number of bytes from the wave provider into the source buffer and resets the offset to 0.
        /// </remarks>
        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            int sourceBytesRequired = samplePairsRequired * 2;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
            sourceBytes = source.Read(sourceBuffer, 0, sourceBytesRequired);
            offset = 0;
        }

        /// <summary>
        /// Retrieves the next pair of samples from the source buffer and returns them as out parameters.
        /// </summary>
        /// <param name="sampleLeft">The left channel sample retrieved from the source buffer.</param>
        /// <param name="sampleRight">The right channel sample retrieved from the source buffer.</param>
        /// <returns>True if there are more samples available in the source buffer; otherwise, false.</returns>
        /// <remarks>
        /// This method retrieves the next pair of samples from the source buffer and returns them as out parameters.
        /// If there are more samples available in the source buffer, it returns true and updates the out parameters with the samples.
        /// If there are no more samples available, it returns false and updates the out parameters with 0.0f.
        /// </remarks>
        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (offset < sourceBytes)
            {
                sampleLeft = sourceBuffer[offset++] / 256f;
                sampleRight = sourceBuffer[offset++] / 256f;
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
