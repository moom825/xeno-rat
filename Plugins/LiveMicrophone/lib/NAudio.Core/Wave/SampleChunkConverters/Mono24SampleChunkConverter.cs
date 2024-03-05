using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    class Mono24SampleChunkConverter : ISampleChunkConverter
    {
        private int offset;
        private byte[] sourceBuffer;
        private int sourceBytes;

        /// <summary>
        /// Checks if the specified wave format is supported.
        /// </summary>
        /// <param name="waveFormat">The wave format to be checked for support.</param>
        /// <returns>True if the wave format is PCM encoded with 24 bits per sample and 1 channel; otherwise, false.</returns>
        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.Pcm &&
                waveFormat.BitsPerSample == 24 &&
                waveFormat.Channels == 1;
        }

        /// <summary>
        /// Loads the next chunk of audio data from the specified <paramref name="source"/> and prepares it for processing.
        /// </summary>
        /// <param name="source">The wave provider from which the audio data will be loaded.</param>
        /// <param name="samplePairsRequired">The number of sample pairs required for processing the next chunk of audio data.</param>
        /// <remarks>
        /// This method calculates the number of bytes required from the <paramref name="source"/> based on the <paramref name="samplePairsRequired"/>.
        /// It then ensures that the <paramref name="sourceBuffer"/> is of sufficient size to accommodate the required bytes using the <see cref="BufferHelpers.Ensure"/> method.
        /// The method reads the required bytes from the <paramref name="source"/> into the <paramref name="sourceBuffer"/> and sets the <paramref name="sourceBytes"/> to the number of bytes read.
        /// The <paramref name="offset"/> is reset to 0, indicating that the next chunk of audio data is ready for processing.
        /// </remarks>
        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            int sourceBytesRequired = samplePairsRequired * 3;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer,sourceBytesRequired);
            sourceBytes = source.Read(sourceBuffer, 0, sourceBytesRequired);
            offset = 0;
        }

        /// <summary>
        /// Retrieves the next audio sample from the source buffer.
        /// </summary>
        /// <param name="sampleLeft">The left channel audio sample.</param>
        /// <param name="sampleRight">The right channel audio sample.</param>
        /// <returns>True if the next sample was successfully retrieved; otherwise, false.</returns>
        /// <remarks>
        /// This method retrieves the next audio sample from the source buffer. It interprets the bytes in the source buffer as 24-bit signed PCM audio samples and converts them to floating-point values in the range [-1.0, 1.0].
        /// If there are remaining bytes in the source buffer, this method retrieves the next sample and advances the offset. If there are no remaining bytes, it returns false and sets both sampleLeft and sampleRight to 0.0f.
        /// </remarks>
        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (offset < sourceBytes)
            {
                sampleLeft = (((sbyte)sourceBuffer[offset + 2] << 16) | (sourceBuffer[offset + 1] << 8) | sourceBuffer[offset]) / 8388608f;
                offset += 3;
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
