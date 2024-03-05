using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    class StereoFloatSampleChunkConverter : ISampleChunkConverter
    {
        private int sourceSample;
        private byte[] sourceBuffer;
        private WaveBuffer sourceWaveBuffer;
        private int sourceSamples;

        /// <summary>
        /// Checks if the specified wave format is supported for processing.
        /// </summary>
        /// <param name="waveFormat">The wave format to be checked for support.</param>
        /// <returns>True if the wave format is IEEE float encoded and has 2 channels; otherwise, false.</returns>
        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.IeeeFloat &&
                waveFormat.Channels == 2;
        }

        /// <summary>
        /// Loads the next chunk of audio data from the specified <paramref name="source"/> and prepares it for processing.
        /// </summary>
        /// <param name="source">The audio source from which the data will be loaded.</param>
        /// <param name="samplePairsRequired">The number of sample pairs required for processing the next chunk.</param>
        /// <remarks>
        /// This method calculates the number of bytes required from the <paramref name="samplePairsRequired"/> and ensures that the <paramref name="sourceBuffer"/> is of sufficient size to accommodate the data.
        /// It then reads the required data from the <paramref name="source"/> into the <paramref name="sourceBuffer"/> and initializes the <paramref name="sourceWaveBuffer"/> for processing.
        /// The number of samples read from the source is calculated and stored in <paramref name="sourceSamples"/>, and the <paramref name="sourceSample"/> index is reset to 0 for processing.
        /// </remarks>
        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            int sourceBytesRequired = samplePairsRequired * 8;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
            sourceWaveBuffer = new WaveBuffer(sourceBuffer);
            sourceSamples = source.Read(sourceBuffer, 0, sourceBytesRequired) / 4;
            sourceSample = 0;
        }

        /// <summary>
        /// Retrieves the next pair of samples from the wave buffer and returns them as out parameters.
        /// </summary>
        /// <param name="sampleLeft">The variable to store the left sample.</param>
        /// <param name="sampleRight">The variable to store the right sample.</param>
        /// <returns>True if the next samples are successfully retrieved; otherwise, false.</returns>
        /// <remarks>
        /// This method retrieves the next pair of samples from the wave buffer and stores them in the out parameters <paramref name="sampleLeft"/> and <paramref name="sampleRight"/>.
        /// If there are no more samples to retrieve, the method returns false and sets both <paramref name="sampleLeft"/> and <paramref name="sampleRight"/> to 0.0f.
        /// </remarks>
        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (sourceSample < sourceSamples)
            {
                sampleLeft = sourceWaveBuffer.FloatBuffer[sourceSample++];
                sampleRight = sourceWaveBuffer.FloatBuffer[sourceSample++];
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
