using System;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Simple SampleProvider that passes through audio unchanged and raises
    /// an event every n samples with the maximum sample value from the period
    /// for metering purposes
    /// </summary>
    public class MeteringSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;

        private readonly float[] maxSamples;
        private int sampleCount;
        private readonly int channels;
        private readonly StreamVolumeEventArgs args;

        /// <summary>
        /// Number of Samples per notification
        /// </summary>
        public int SamplesPerNotification { get; set; }

        /// <summary>
        /// Raised periodically to inform the user of the max volume
        /// </summary>
        public event EventHandler<StreamVolumeEventArgs> StreamVolume;

        /// <summary>
        /// Initialises a new instance of MeteringSampleProvider that raises 10 stream volume
        /// events per second
        /// </summary>
        /// <param name="source">Source sample provider</param>
        public MeteringSampleProvider(ISampleProvider source) :
            this(source, source.WaveFormat.SampleRate / 10)
        {
        }

        /// <summary>
        /// Initialises a new instance of MeteringSampleProvider 
        /// </summary>
        /// <param name="source">source sampler provider</param>
        /// <param name="samplesPerNotification">Number of samples between notifications</param>
        public MeteringSampleProvider(ISampleProvider source, int samplesPerNotification)
        {
            this.source = source;
            channels = source.WaveFormat.Channels;
            maxSamples = new float[channels];
            SamplesPerNotification = samplesPerNotification;
            args = new StreamVolumeEventArgs() { MaxSampleValues = maxSamples }; // create objects up front giving GC little to do
        }

        /// <summary>
        /// The WaveFormat of this sample provider
        /// </summary>
        public WaveFormat WaveFormat => source.WaveFormat;

        /// <summary>
        /// Reads audio samples from the source into the buffer and updates the stream volume.
        /// </summary>
        /// <param name="buffer">The buffer to store the audio samples.</param>
        /// <param name="offset">The zero-based byte offset in the buffer at which to begin storing the data.</param>
        /// <param name="count">The maximum number of samples to read.</param>
        /// <returns>The total number of samples read into the buffer.</returns>
        /// <remarks>
        /// This method reads audio samples from the source into the buffer, updates the stream volume, and returns the total number of samples read.
        /// If there is an event listener for stream volume, it updates the volume and triggers the event when the sample count reaches the specified threshold (SamplesPerNotification).
        /// </remarks>
        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = source.Read(buffer, offset, count);
            // only bother if there is an event listener
            if (StreamVolume != null)
            {
                for (int index = 0; index < samplesRead; index += channels)
                {
                    for (int channel = 0; channel < channels; channel++)
                    {
                        float sampleValue = Math.Abs(buffer[offset + index + channel]);
                        maxSamples[channel] = Math.Max(maxSamples[channel], sampleValue);
                    }
                    sampleCount++;
                    if (sampleCount >= SamplesPerNotification)
                    {
                        StreamVolume(this, args);
                        sampleCount = 0;
                        // n.b. we avoid creating new instances of anything here
                        Array.Clear(maxSamples, 0, channels);
                    }
                }
            }
            return samplesRead;
        }
    }

    /// <summary>
    /// Event args for aggregated stream volume
    /// </summary>
    public class StreamVolumeEventArgs : EventArgs
    {
        /// <summary>
        /// Max sample values array (one for each channel)
        /// </summary>
        public float[] MaxSampleValues { get; set; }
    }
}
