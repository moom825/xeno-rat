using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Helper base class for classes converting to ISampleProvider
    /// </summary>
    public abstract class SampleProviderConverterBase : ISampleProvider
    {
        /// <summary>
        /// Source Wave Provider
        /// </summary>
        protected IWaveProvider source;
        private readonly WaveFormat waveFormat;

        /// <summary>
        /// Source buffer (to avoid constantly creating small buffers during playback)
        /// </summary>
        protected byte[] sourceBuffer;

        /// <summary>
        /// Initialises a new instance of SampleProviderConverterBase
        /// </summary>
        /// <param name="source">Source Wave provider</param>
        public SampleProviderConverterBase(IWaveProvider source)
        {
            this.source = source;
            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, source.WaveFormat.Channels);
        }

        /// <summary>
        /// Wave format of this wave provider
        /// </summary>
        public WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Reads a sequence of elements from the specified buffer and advances the position within the buffer by the number of elements read.
        /// </summary>
        /// <param name="buffer">The buffer to read data from.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of elements to read.</param>
        /// <returns>The total number of elements read into the buffer.</returns>
        public abstract int Read(float[] buffer, int offset, int count);

        /// <summary>
        /// Ensures that the source buffer has enough capacity to accommodate the specified number of bytes required.
        /// </summary>
        /// <param name="sourceBytesRequired">The number of bytes required for the source buffer.</param>
        /// <remarks>
        /// This method ensures that the source buffer has enough capacity to accommodate the specified number of bytes required.
        /// If the current capacity of the source buffer is less than the specified number of bytes required, it reallocates the buffer with the new capacity.
        /// The source buffer is modified in place to ensure sufficient capacity.
        /// </remarks>
        protected void EnsureSourceBuffer(int sourceBytesRequired)
        {
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceBytesRequired);
        }
    }
}
