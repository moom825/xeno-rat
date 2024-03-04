namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Converts an IWaveProvider containing 32 bit PCM to an
    /// ISampleProvider
    /// </summary>
    public class Pcm32BitToSampleProvider : SampleProviderConverterBase
    {
        /// <summary>
        /// Initialises a new instance of Pcm32BitToSampleProvider
        /// </summary>
        /// <param name="source">Source Wave Provider</param>
        public Pcm32BitToSampleProvider(IWaveProvider source)
            : base(source)
        {

        }

        /// <summary>
        /// Reads floating-point numbers from the source buffer and stores them in the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer to store the read floating-point numbers.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data.</param>
        /// <param name="count">The number of floating-point numbers to read from the source buffer.</param>
        /// <exception cref="System.IO.IOException">An I/O error occurs while reading from the source buffer.</exception>
        /// <returns>The actual number of floating-point numbers read and stored in the buffer.</returns>
        /// <remarks>
        /// This method reads floating-point numbers from the source buffer and stores them in the specified buffer.
        /// It ensures that the source buffer has enough space to read the required number of bytes before reading.
        /// The method then converts the bytes to floating-point numbers and stores them in the buffer starting from the specified offset.
        /// The method returns the actual number of floating-point numbers read and stored in the buffer, which may be less than the requested count if the end of the source buffer is reached.
        /// </remarks>
        public override int Read(float[] buffer, int offset, int count)
        {
            int sourceBytesRequired = count*4;
            EnsureSourceBuffer(sourceBytesRequired);
            int bytesRead = source.Read(sourceBuffer, 0, sourceBytesRequired);
            int outIndex = offset;
            for (int n = 0; n < bytesRead; n += 4)
            {
                buffer[outIndex++] = (((sbyte) sourceBuffer[n + 3] << 24 |
                                       sourceBuffer[n + 2] << 16) |
                                      (sourceBuffer[n + 1] << 8) |
                                      sourceBuffer[n])/2147483648f;
            }
            return bytesRead/4;
        }
    }
}
