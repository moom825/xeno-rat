using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    class Mono16SampleChunkConverter : ISampleChunkConverter
    {
        private int sourceSample;
        private byte[] sourceBuffer;
        private WaveBuffer sourceWaveBuffer;
        private int sourceSamples;

        /// <summary>
        /// Checks if the given WaveFormat is supported.
        /// </summary>
        /// <param name="waveFormat">The WaveFormat to be checked for support.</param>
        /// <returns>True if the WaveFormat is PCM encoded with 16 bits per sample and a single channel; otherwise, false.</returns>
        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.Pcm &&
                waveFormat.BitsPerSample == 16 &&
                waveFormat.Channels == 1;
        }

        /// <summary>
        /// Loads the next chunk of audio data from the specified wave provider.
        /// </summary>
        /// <param name="source">The wave provider from which to load the audio data.</param>
        /// <param name="samplePairsRequired">The number of sample pairs required for the next chunk.</param>
        /// <remarks>
        /// This method calculates the number of bytes required from the number of sample pairs and initializes the necessary buffers.
        /// It then reads the audio data from the source wave provider into the source buffer and updates the source sample and source samples fields accordingly.
        /// </remarks>
        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            int sourceBytesRequired = samplePairsRequired * 2;
            sourceSample = 0;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
            sourceWaveBuffer = new WaveBuffer(sourceBuffer);
            sourceSamples = source.Read(sourceBuffer, 0, sourceBytesRequired) / 2;
        }

        /// <summary>
        /// Retrieves the next sample from the source wave buffer and returns it as floating-point values for the left and right channels.
        /// </summary>
        /// <param name="sampleLeft">When this method returns, contains the next sample value for the left channel.</param>
        /// <param name="sampleRight">When this method returns, contains the next sample value for the right channel.</param>
        /// <returns>True if the next sample was successfully retrieved; otherwise, false.</returns>
        /// <remarks>
        /// If the source sample index is less than the total number of source samples, this method retrieves the next sample from the source wave buffer and returns it as floating-point values for both the left and right channels.
        /// If the source sample index is equal to or greater than the total number of source samples, this method returns 0.0f for both the left and right channels and false to indicate that no more samples are available.
        /// </remarks>
        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (sourceSample < sourceSamples)
            {
                sampleLeft = sourceWaveBuffer.ShortBuffer[sourceSample++] / 32768.0f;
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
