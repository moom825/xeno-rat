using System;
using NAudio.Utils;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Helper stream that lets us read from compressed audio files with large block alignment
    /// as though we could read any amount and reposition anywhere
    /// </summary>
    public class BlockAlignReductionStream : WaveStream
    {
        private WaveStream sourceStream;
        private long position;
        private readonly CircularBuffer circularBuffer;
        private long bufferStartPosition;
        private byte[] sourceBuffer;
        private readonly object lockObject = new object();

        /// <summary>
        /// Creates a new BlockAlignReductionStream
        /// </summary>
        /// <param name="sourceStream">the input stream</param>
        public BlockAlignReductionStream(WaveStream sourceStream)
        {
            this.sourceStream = sourceStream;
            circularBuffer = new CircularBuffer(sourceStream.WaveFormat.AverageBytesPerSecond * 4);
        }

        /// <summary>
        /// Returns the source buffer of the specified size, creating a new buffer if necessary.
        /// </summary>
        /// <param name="size">The size of the buffer to be retrieved.</param>
        /// <returns>The source buffer of at least the specified <paramref name="size"/>.</returns>
        /// <remarks>
        /// If the existing source buffer is null or smaller than the specified size, a new buffer of double the size is created to accommodate the request.
        /// </remarks>
        private byte[] GetSourceBuffer(int size)
        {
            if (sourceBuffer == null || sourceBuffer.Length < size)
            {
                // let's give ourselves some leeway
                sourceBuffer = new byte[size * 2];
            }
            return sourceBuffer;
        }

        /// <summary>
        /// Block alignment of this stream
        /// </summary>
        public override int BlockAlign
        {
            get
            {
                // can position to sample level
                return (WaveFormat.BitsPerSample / 8) * WaveFormat.Channels;
            }
        }

        /// <summary>
        /// Wave Format
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get { return sourceStream.WaveFormat; }
        }

        /// <summary>
        /// Length of this Stream
        /// </summary>
        public override long Length
        {
            get { return sourceStream.Length; }
        }

        /// <summary>
        /// Current position within stream
        /// </summary>
        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                lock (lockObject)
                {
                    if (position != value)
                    {
                        if (position % BlockAlign != 0)
                            throw new ArgumentException("Position must be block aligned");
                        long sourcePosition = value - (value % sourceStream.BlockAlign);
                        if (sourceStream.Position != sourcePosition)
                        {
                            sourceStream.Position = sourcePosition;
                            circularBuffer.Reset();
                            bufferStartPosition = sourceStream.Position;
                        }
                        position = value;
                    }
                }
            }
        }

        private long BufferEndPosition
        {
            get
            {

                return bufferStartPosition + circularBuffer.Count;
            }
        }

        /// <summary>
        /// Releases the unmanaged resources used by the BlockAlignReductionStream and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        /// <remarks>
        /// This method releases the unmanaged resources used by the BlockAlignReductionStream and optionally releases the managed resources.
        /// If <paramref name="disposing"/> is true, this method disposes of the <see cref="sourceStream"/> if it is not null.
        /// If <paramref name="disposing"/> is false, it asserts that the BlockAlignReductionStream was not disposed.
        /// </remarks>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (sourceStream != null)
                {
                    sourceStream.Dispose();
                    sourceStream = null;
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "BlockAlignReductionStream was not Disposed");
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Reads a specified number of bytes from the circular buffer into the provided byte array, starting at the specified offset.
        /// </summary>
        /// <param name="buffer">The byte array to which the data will be read.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the circular buffer.</param>
        /// <param name="count">The maximum number of bytes to read from the circular buffer.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        /// <remarks>
        /// This method attempts to fill the circular buffer with enough data to meet the request. It then discards any unnecessary data from the start of the buffer and returns the specified number of bytes into the provided array.
        /// </remarks>
        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (lockObject)
            {
                // 1. attempt to fill the circular buffer with enough data to meet our request
                while (BufferEndPosition < position + count)
                {
                    int sourceReadCount = count;
                    if (sourceReadCount % sourceStream.BlockAlign != 0)
                    {
                        sourceReadCount = (count + sourceStream.BlockAlign) - (count % sourceStream.BlockAlign);
                    }

                    int sourceRead = sourceStream.Read(GetSourceBuffer(sourceReadCount), 0, sourceReadCount);
                    circularBuffer.Write(GetSourceBuffer(sourceReadCount), 0, sourceRead);
                    if (sourceRead == 0)
                    {
                        // assume we have run out of data
                        break;
                    }
                }

                // 2. discard any unnecessary stuff from the start
                if (bufferStartPosition < position)
                {
                    circularBuffer.Advance((int)(position - bufferStartPosition));
                    bufferStartPosition = position;
                }

                // 3. now whatever is in the buffer we can return
                int bytesRead = circularBuffer.Read(buffer, offset, count);
                position += bytesRead;
                // anything left in buffer is at start position
                bufferStartPosition = position;

                return bytesRead;
            }
        }
    }
}
