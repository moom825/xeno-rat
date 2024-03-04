using System;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Helper class turning an already 32 bit floating point IWaveProvider
    /// into an ISampleProvider - hopefully not needed for most applications
    /// </summary>
    public class WaveToSampleProvider : SampleProviderConverterBase
    {
        /// <summary>
        /// Initializes a new instance of the WaveToSampleProvider class
        /// </summary>
        /// <param name="source">Source wave provider, must be IEEE float</param>
        public WaveToSampleProvider(IWaveProvider source)
            : base(source)
        {
            if (source.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                throw new ArgumentException("Must be already floating point");
            }
        }

        /// <summary>
        /// Reads floating-point numbers from the source buffer into the specified destination buffer.
        /// </summary>
        /// <param name="buffer">The destination buffer to store the read floating-point numbers.</param>
        /// <param name="offset">The zero-based byte offset in the destination buffer at which to begin storing the data.</param>
        /// <param name="count">The number of floating-point numbers to read from the source buffer.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the source buffer is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown when the count is less than zero or the sum of offset and count is greater than the length of the source buffer.</exception>
        /// <returns>The number of floating-point numbers read into the destination buffer.</returns>
        /// <remarks>
        /// This method reads floating-point numbers from the source buffer into the specified destination buffer. It ensures that the source buffer has enough space to accommodate the required bytes and then reads the bytes into the source buffer. It then converts the bytes to floating-point numbers and stores them in the destination buffer starting from the specified offset. The method returns the total number of floating-point numbers read into the destination buffer.
        /// </remarks>
        public override int Read(float[] buffer, int offset, int count)
        {
            int bytesNeeded = count * 4;
            EnsureSourceBuffer(bytesNeeded);
            int bytesRead = source.Read(sourceBuffer, 0, bytesNeeded);
            int samplesRead = bytesRead / 4;
            int outputIndex = offset;
            unsafe
            {
                fixed(byte* pBytes = &sourceBuffer[0])
                {
                    float* pFloat = (float*)pBytes;
                    for (int n = 0, i = 0; n < bytesRead; n += 4, i++)
                    {
                        buffer[outputIndex++] = *(pFloat + i);
                    }
                }
            }
            return samplesRead;
        }
    }
}
