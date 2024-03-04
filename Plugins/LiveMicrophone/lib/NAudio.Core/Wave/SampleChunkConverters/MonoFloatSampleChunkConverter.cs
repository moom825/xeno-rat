using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    class MonoFloatSampleChunkConverter : ISampleChunkConverter
    {
        private int sourceSample;
        private byte[] sourceBuffer;
        private WaveBuffer sourceWaveBuffer;
        private int sourceSamples;

        /// <summary>
        /// Checks if the specified wave format is supported for processing.
        /// </summary>
        /// <param name="waveFormat">The wave format to be checked for support.</param>
        /// <returns>True if the wave format is IEEE float encoded and has a single channel; otherwise, false.</returns>
        public bool Supports(WaveFormat waveFormat)
        {
            return waveFormat.Encoding == WaveFormatEncoding.IeeeFloat &&
                waveFormat.Channels == 1;
        }

        /// <summary>
        /// Loads the next chunk of audio data from the specified wave provider.
        /// </summary>
        /// <param name="source">The wave provider from which to load the audio data.</param>
        /// <param name="samplePairsRequired">The number of sample pairs required for the next chunk.</param>
        /// <remarks>
        /// This method loads the next chunk of audio data from the specified wave provider.
        /// It calculates the number of source bytes required based on the sample pairs required and ensures that the source buffer is large enough to accommodate the data.
        /// The method then reads the audio data into the source buffer, updates the source samples count, and resets the source sample index.
        /// </remarks>
        public void LoadNextChunk(IWaveProvider source, int samplePairsRequired)
        {
            int sourceBytesRequired = samplePairsRequired * 4;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
            sourceWaveBuffer = new WaveBuffer(sourceBuffer);
            sourceSamples = source.Read(sourceBuffer, 0, sourceBytesRequired) / 4;
            sourceSample = 0;
        }

        /// <summary>
        /// Retrieves the next sample from the wave buffer and assigns it to the specified output parameters.
        /// </summary>
        /// <param name="sampleLeft">The variable to store the retrieved left sample.</param>
        /// <param name="sampleRight">The variable to store the retrieved right sample.</param>
        /// <returns>True if there are more samples available in the buffer; otherwise, false.</returns>
        public bool GetNextSample(out float sampleLeft, out float sampleRight)
        {
            if (sourceSample < sourceSamples)
            {
                sampleLeft = sourceWaveBuffer.FloatBuffer[sourceSample++];
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
