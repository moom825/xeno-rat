using System;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Silence producing wave provider
    /// Useful for playing silence when doing a WASAPI Loopback Capture
    /// </summary>
    public class SilenceProvider : IWaveProvider
    {
        /// <summary>
        /// Creates a new silence producing wave provider
        /// </summary>
        /// <param name="wf">Desired WaveFormat (should be PCM / IEE float</param>
        public SilenceProvider(WaveFormat wf) { WaveFormat = wf; }

        /// <summary>
        /// Clears the specified range of elements in the <paramref name="buffer"/> array and returns the number of elements cleared.
        /// </summary>
        /// <param name="buffer">The array containing the elements to be cleared.</param>
        /// <param name="offset">The zero-based starting index of the range of elements to clear.</param>
        /// <param name="count">The number of elements to clear.</param>
        /// <returns>The number of elements cleared, which is equal to <paramref name="count"/>.</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            Array.Clear(buffer, offset, count);
            return count;
        }

        /// <summary>
        /// WaveFormat of this silence producing wave provider
        /// </summary>
        public WaveFormat WaveFormat { get; private set; }
    }
}
