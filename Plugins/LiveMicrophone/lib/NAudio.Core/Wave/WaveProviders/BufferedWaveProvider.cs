using System;
using NAudio.Utils;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Provides a buffered store of samples
    /// Read method will return queued samples or fill buffer with zeroes
    /// Now backed by a circular buffer
    /// </summary>
    public class BufferedWaveProvider : IWaveProvider
    {
        private CircularBuffer circularBuffer;
        private readonly WaveFormat waveFormat;

        /// <summary>
        /// Creates a new buffered WaveProvider
        /// </summary>
        /// <param name="waveFormat">WaveFormat</param>
        public BufferedWaveProvider(WaveFormat waveFormat)
        {
            this.waveFormat = waveFormat;
            BufferLength = waveFormat.AverageBytesPerSecond * 5;
            ReadFully = true;
        }

        /// <summary>
        /// If true, always read the amount of data requested, padding with zeroes if necessary
        /// By default is set to true
        /// </summary>
        public bool ReadFully { get; set; }

        /// <summary>
        /// Buffer length in bytes
        /// </summary>
        public int BufferLength { get; set; }

        /// <summary>
        /// Buffer duration
        /// </summary>
        public TimeSpan BufferDuration
        {
            get
            {
                return TimeSpan.FromSeconds((double)BufferLength / WaveFormat.AverageBytesPerSecond);
            }
            set
            {
                BufferLength = (int)(value.TotalSeconds * WaveFormat.AverageBytesPerSecond);
            }
        }

        /// <summary>
        /// If true, when the buffer is full, start throwing away data
        /// if false, AddSamples will throw an exception when buffer is full
        /// </summary>
        public bool DiscardOnBufferOverflow { get; set; }

        /// <summary>
        /// The number of buffered bytes
        /// </summary>
        public int BufferedBytes
        {
            get
            {
                return circularBuffer == null ? 0 : circularBuffer.Count;
            }
        }

        /// <summary>
        /// Buffered Duration
        /// </summary>
        public TimeSpan BufferedDuration
        {
            get { return TimeSpan.FromSeconds((double)BufferedBytes / WaveFormat.AverageBytesPerSecond); }
        }

        /// <summary>
        /// Gets the WaveFormat
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

        /// <summary>
        /// Adds the specified samples to the circular buffer.
        /// </summary>
        /// <param name="buffer">The buffer containing the samples to be added.</param>
        /// <param name="offset">The zero-based byte offset in the buffer at which to begin copying bytes to the circular buffer.</param>
        /// <param name="count">The number of samples to be added from the buffer to the circular buffer.</param>
        /// <exception cref="InvalidOperationException">Thrown when the circular buffer is full and <see cref="DiscardOnBufferOverflow"/> is set to false.</exception>
        public void AddSamples(byte[] buffer, int offset, int count)
        {
            // create buffer here to allow user to customise buffer length
            if (circularBuffer == null)
            { 
                circularBuffer = new CircularBuffer(BufferLength);
            }

            var written = circularBuffer.Write(buffer, offset, count);
            if (written < count && !DiscardOnBufferOverflow)
            {
                throw new InvalidOperationException("Buffer full");
            }
        }

        /// <summary>
        /// Reads data from the circular buffer into the specified byte array.
        /// </summary>
        /// <param name="buffer">The byte array to read the data into.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        /// <remarks>
        /// This method reads data from the circular buffer into the specified byte array starting at the specified offset and up to the specified count.
        /// If the circular buffer is not yet created, it returns 0.
        /// If <paramref name="ReadFully"/> is true and the total number of bytes read is less than the specified count, it zeros the end of the buffer and returns the total count.
        /// </remarks>
        public int Read(byte[] buffer, int offset, int count) 
        {
            int read = 0;
            if (circularBuffer != null) // not yet created
            { 
                read = circularBuffer.Read(buffer, offset, count);
            }
            if (ReadFully && read < count)
            {
                // zero the end of the buffer
                Array.Clear(buffer, offset + read, count - read);
                read = count;
            }
            return read;
        }

        /// <summary>
        /// Clears the circular buffer.
        /// </summary>
        /// <remarks>
        /// This method clears the circular buffer by resetting it to its initial state.
        /// </remarks>
        public void ClearBuffer()
        {
            if (circularBuffer != null)
            {
                circularBuffer.Reset();
            }
        }
    }
}
