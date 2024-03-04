using System;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Simple class that raises an event on every sample
    /// </summary>
    public class NotifyingSampleProvider : ISampleProvider, ISampleNotifier
    {
        private readonly ISampleProvider source;
        // try not to give the garbage collector anything to deal with when playing live audio
        private readonly SampleEventArgs sampleArgs = new SampleEventArgs(0, 0);
        private readonly int channels;

        /// <summary>
        /// Initializes a new instance of NotifyingSampleProvider
        /// </summary>
        /// <param name="source">Source Sample Provider</param>
        public NotifyingSampleProvider(ISampleProvider source)
        {
            this.source = source;
            channels = WaveFormat.Channels;
        }

        /// <summary>
        /// WaveFormat
        /// </summary>
        public WaveFormat WaveFormat => source.WaveFormat;

        /// <summary>
        /// Reads audio samples into a buffer and raises the Sample event for each sample read.
        /// </summary>
        /// <param name="buffer">The buffer to read the samples into.</param>
        /// <param name="offset">The zero-based offset in the buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="sampleCount">The maximum number of samples to read.</param>
        /// <returns>The total number of samples read into the buffer.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when the buffer is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the offset or sampleCount is less than zero.</exception>
        public int Read(float[] buffer, int offset, int sampleCount)
        {
            int samplesRead = source.Read(buffer, offset, sampleCount);
            if (Sample != null)
            {
                for (int n = 0; n < samplesRead; n += channels)
                {
                    sampleArgs.Left = buffer[offset + n];
                    sampleArgs.Right = channels > 1 ? buffer[offset + n + 1] : sampleArgs.Left;
                    Sample(this, sampleArgs);
                }
            }
            return samplesRead;
        }

        /// <summary>
        /// Sample notifier
        /// </summary>
        public event EventHandler<SampleEventArgs> Sample;
    }
}
