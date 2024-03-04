using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    class Stereo24SampleChunkConverter : ISampleChunkConverter
    {
        private int offset;
        private byte[] sourceBuffer;
        private int sourceBytes;

        /// <summary>
        /// Checks if the specified wave format is supported.
        /// </summary>
        /// <param name="waveFormat">The wave format to be checked for support.</param>
        /// <returns>True if the wave format is PCM encoded with 24 bits per sample and 2 channels; otherwise, false.</returns>
        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.Pcm &&
                waveFormat.BitsPerSample == 24 &&
                waveFormat.Channels == 2;
        }

        /// <summary>
        /// Loads the next chunk of audio data from the specified wave provider.
        /// </summary>
        /// <param name="source">The wave provider from which to load the audio data.</param>
        /// <param name="samplePairsRequired">The number of sample pairs required for the next chunk.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="source"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the source does not contain enough data to fulfill the required sample pairs.</exception>
        /// <remarks>
        /// This method loads the next chunk of audio data from the specified wave provider.
        /// It calculates the number of bytes required based on the sample pairs required and ensures that the source buffer is large enough to hold the data.
        /// It then reads the specified number of bytes from the wave provider into the source buffer and sets the offset to 0.
        /// </remarks>
        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            int sourceBytesRequired = samplePairsRequired * 6;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
            sourceBytes = source.Read(sourceBuffer, 0, sourceBytesRequired);
            offset = 0;
        }

        /// <summary>
        /// Retrieves the next audio sample from the source buffer and returns it as two floating-point values.
        /// </summary>
        /// <param name="sampleLeft">The variable to store the left channel audio sample.</param>
        /// <param name="sampleRight">The variable to store the right channel audio sample.</param>
        /// <returns>True if the next sample is successfully retrieved; otherwise, false.</returns>
        /// <remarks>
        /// This method retrieves the next audio sample from the source buffer and stores it in the variables <paramref name="sampleLeft"/> and <paramref name="sampleRight"/>.
        /// The method also advances the internal offset to the next set of samples in the source buffer.
        /// If the end of the source buffer is reached, the method returns false and sets both <paramref name="sampleLeft"/> and <paramref name="sampleRight"/> to 0.0f.
        /// </remarks>
        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (offset < sourceBytes)
            {
                sampleLeft = (((sbyte)sourceBuffer[offset + 2] << 16) | (sourceBuffer[offset + 1] << 8) | sourceBuffer[offset]) / 8388608f;
                offset += 3;
                sampleRight = (((sbyte)sourceBuffer[offset + 2] << 16) | (sourceBuffer[offset + 1] << 8) | sourceBuffer[offset]) / 8388608f;
                offset += 3;
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
