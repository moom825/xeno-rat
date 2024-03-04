namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Converts an IWaveProvider containing 24 bit PCM to an
    /// ISampleProvider
    /// </summary>
    public class Pcm24BitToSampleProvider : SampleProviderConverterBase
    {
        /// <summary>
        /// Initialises a new instance of Pcm24BitToSampleProvider
        /// </summary>
        /// <param name="source">Source Wave Provider</param>
        public Pcm24BitToSampleProvider(IWaveProvider source)
            : base(source)
        {
            
        }

        /// <summary>
        /// Reads data from the source buffer and converts it to floating point numbers, storing the result in the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer to store the converted floating point numbers.</param>
        /// <param name="offset">The zero-based byte offset in the buffer at which to begin storing the data.</param>
        /// <param name="count">The number of floating point numbers to read from the source buffer.</param>
        /// <returns>The actual number of floating point numbers read and stored in the buffer.</returns>
        /// <remarks>
        /// This method reads data from the source buffer, converts it to floating point numbers, and stores the result in the specified buffer.
        /// It ensures that the source buffer has enough space to accommodate the required bytes for conversion.
        /// The conversion is performed by combining bytes from the source buffer and dividing the result by 8388608f.
        /// The method returns the actual number of floating point numbers read and stored in the buffer, which is calculated based on the number of bytes read from the source buffer.
        /// </remarks>
        public override int Read(float[] buffer, int offset, int count)
        {
            int sourceBytesRequired = count * 3;
            EnsureSourceBuffer(sourceBytesRequired);
            int bytesRead = source.Read(sourceBuffer, 0, sourceBytesRequired);
            int outIndex = offset;
            for (int n = 0; n < bytesRead; n += 3)
            {
                buffer[outIndex++] = (((sbyte)sourceBuffer[n + 2] << 16) | (sourceBuffer[n + 1] << 8) | sourceBuffer[n]) / 8388608f;
            }
            return bytesRead / 3;
        }
    }
}
