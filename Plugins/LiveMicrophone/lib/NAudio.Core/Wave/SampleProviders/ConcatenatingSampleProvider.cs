using System;
using System.Collections.Generic;
using System.Linq;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Sample Provider to concatenate multiple sample providers together
    /// </summary>
    public class ConcatenatingSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider[] providers;
        private int currentProviderIndex;

        /// <summary>
        /// Creates a new ConcatenatingSampleProvider
        /// </summary>
        /// <param name="providers">The source providers to play one after the other. Must all share the same sample rate and channel count</param>
        public ConcatenatingSampleProvider(IEnumerable<ISampleProvider> providers)
        {
            if (providers == null) throw new ArgumentNullException(nameof(providers));
            this.providers = providers.ToArray();
            if (this.providers.Length == 0) throw new ArgumentException("Must provide at least one input", nameof(providers));
            if (this.providers.Any(p => p.WaveFormat.Channels != WaveFormat.Channels)) throw new ArgumentException("All inputs must have the same channel count", nameof(providers));
            if (this.providers.Any(p => p.WaveFormat.SampleRate != WaveFormat.SampleRate)) throw new ArgumentException("All inputs must have the same sample rate", nameof(providers));
        }

        /// <summary>
        /// The WaveFormat of this Sample Provider
        /// </summary>
        public WaveFormat WaveFormat => providers[0].WaveFormat;

        /// <summary>
        /// Reads data from the providers into the buffer and returns the total number of bytes read.
        /// </summary>
        /// <param name="buffer">The buffer to read the data into.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current provider.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        /// <remarks>
        /// This method reads data from the providers into the buffer. It iterates through the providers, reading data into the buffer until the specified count is reached or all providers have been exhausted. If a provider returns 0 bytes read, it moves to the next provider. The method returns the total number of bytes read into the buffer.
        /// </remarks>
        public int Read(float[] buffer, int offset, int count)
        {
            var read = 0;
            while (read < count && currentProviderIndex < providers.Length)
            {
                var needed = count - read;
                var readThisTime = providers[currentProviderIndex].Read(buffer, offset + read, needed);
                read += readThisTime;
                if (readThisTime == 0) currentProviderIndex++;
            }
            return read;
        }
    }
}