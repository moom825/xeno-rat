using System;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Converts an IWaveProvider containing 16 bit PCM to an
    /// ISampleProvider
    /// </summary>
    public class Pcm16BitToSampleProvider : SampleProviderConverterBase
    {
        /// <summary>
        /// Initialises a new instance of Pcm16BitToSampleProvider
        /// </summary>
        /// <param name="source">Source wave provider</param>
        public Pcm16BitToSampleProvider(IWaveProvider source)
            : base(source)
        {
        }

        /// <summary>
        /// Reads a specified number of floating-point values from the source buffer and stores them in the provided buffer.
        /// </summary>
        /// <param name="buffer">The buffer to store the read floating-point values.</param>
        /// <param name="offset">The zero-based byte offset in the buffer at which to begin storing the data.</param>
        /// <param name="count">The number of floating-point values to read.</param>
        /// <returns>The actual number of floating-point values read and stored in the buffer.</returns>
        /// <remarks>
        /// This method reads a specified number of bytes from the source buffer and interprets them as 16-bit signed integers, which are then converted to floating-point values by dividing by 32768.
        /// The resulting floating-point values are stored in the provided buffer starting at the specified offset.
        /// </remarks>
        public override int Read(float[] buffer, int offset, int count)
        {
            int sourceBytesRequired = count * 2;
            EnsureSourceBuffer(sourceBytesRequired);
            int bytesRead = source.Read(sourceBuffer, 0, sourceBytesRequired);
            int outIndex = offset;
            for(int n = 0; n < bytesRead; n+=2)
            {
                buffer[outIndex++] = BitConverter.ToInt16(sourceBuffer, n) / 32768f;
            }
            return bytesRead / 2;
        }
    }
}
