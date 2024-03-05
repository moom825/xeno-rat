namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Very simple sample provider supporting adjustable gain
    /// </summary>
    public class VolumeSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;

        /// <summary>
        /// Initializes a new instance of VolumeSampleProvider
        /// </summary>
        /// <param name="source">Source Sample Provider</param>
        public VolumeSampleProvider(ISampleProvider source)
        {
            this.source = source;
            Volume = 1.0f;
        }

        /// <summary>
        /// WaveFormat
        /// </summary>
        public WaveFormat WaveFormat => source.WaveFormat;

        /// <summary>
        /// Reads audio samples from the source and modifies the volume if necessary.
        /// </summary>
        /// <param name="buffer">The buffer to store the audio samples.</param>
        /// <param name="offset">The offset in the buffer at which to begin storing the samples.</param>
        /// <param name="sampleCount">The number of samples to read.</param>
        /// <returns>The number of samples actually read from the source.</returns>
        /// <remarks>
        /// This method reads audio samples from the source into the specified buffer starting at the given offset.
        /// If the volume is not equal to 1, it modifies the audio samples in the buffer by scaling them with the volume factor.
        /// </remarks>
        public int Read(float[] buffer, int offset, int sampleCount)
        {
            int samplesRead = source.Read(buffer, offset, sampleCount);
            if (Volume != 1f)
            {
                for (int n = 0; n < sampleCount; n++)
                {
                    buffer[offset + n] *= Volume;
                }
            }
            return samplesRead;
        }

        /// <summary>
        /// Allows adjusting the volume, 1.0f = full volume
        /// </summary>
        public float Volume { get; set; }
    }
}
