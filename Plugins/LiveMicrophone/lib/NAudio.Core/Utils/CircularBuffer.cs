using System;
using System.Diagnostics;

namespace NAudio.Utils
{
    /// <summary>
    /// A very basic circular buffer implementation
    /// </summary>
    public class CircularBuffer
    {
        private readonly byte[] buffer;
        private readonly object lockObject;
        private int writePosition;
        private int readPosition;
        private int byteCount;

        /// <summary>
        /// Create a new circular buffer
        /// </summary>
        /// <param name="size">Max buffer size in bytes</param>
        public CircularBuffer(int size)
        {
            buffer = new byte[size];
            lockObject = new object();
        }

        /// <summary>
        /// Writes the specified number of bytes from the input <paramref name="data"/> array to the internal buffer, starting at the specified <paramref name="offset"/>.
        /// </summary>
        /// <param name="data">The input byte array from which data will be written to the internal buffer.</param>
        /// <param name="offset">The zero-based byte offset in the <paramref name="data"/> array at which to begin copying bytes to the internal buffer.</param>
        /// <param name="count">The number of bytes to write to the internal buffer.</param>
        /// <returns>The actual number of bytes written to the internal buffer, which may be less than the specified <paramref name="count"/> if the internal buffer does not have enough space.</returns>
        /// <remarks>
        /// This method locks the <paramref name="lockObject"/> to ensure thread safety while writing to the internal buffer.
        /// If the specified <paramref name="count"/> is greater than the available space in the internal buffer, only the available space is written.
        /// The method first writes to the end of the buffer, and if necessary, wraps around and writes to the start of the buffer to complete the write operation.
        /// The total number of bytes written is returned, and the internal byte count is updated accordingly.
        /// </remarks>
        public int Write(byte[] data, int offset, int count)
        {
            lock (lockObject)
            {
                var bytesWritten = 0;
                if (count > buffer.Length - byteCount)
                {
                    count = buffer.Length - byteCount;
                }
                // write to end
                int writeToEnd = Math.Min(buffer.Length - writePosition, count);
                Array.Copy(data, offset, buffer, writePosition, writeToEnd);
                writePosition += writeToEnd;
                writePosition %= buffer.Length;
                bytesWritten += writeToEnd;
                if (bytesWritten < count)
                {
                    Debug.Assert(writePosition == 0);
                    // must have wrapped round. Write to start
                    Array.Copy(data, offset + bytesWritten, buffer, writePosition, count - bytesWritten);
                    writePosition += (count - bytesWritten);
                    bytesWritten = count;
                }
                byteCount += bytesWritten;
                return bytesWritten;
            }
        }

        /// <summary>
        /// Reads data from the buffer into the specified byte array.
        /// </summary>
        /// <param name="data">The byte array to which the data will be read.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="data"/> at which to begin storing the data.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        /// <remarks>
        /// This method reads data from the internal buffer into the specified byte array. If the specified count is greater than the available bytes in the buffer, it only reads the available bytes.
        /// The method handles wrapping around the buffer if necessary and updates the internal state accordingly.
        /// </remarks>
        public int Read(byte[] data, int offset, int count)
        {
            lock (lockObject)
            {
                if (count > byteCount)
                {
                    count = byteCount;
                }
                int bytesRead = 0;
                int readToEnd = Math.Min(buffer.Length - readPosition, count);
                Array.Copy(buffer, readPosition, data, offset, readToEnd);
                bytesRead += readToEnd;
                readPosition += readToEnd;
                readPosition %= buffer.Length;

                if (bytesRead < count)
                {
                    // must have wrapped round. Read from start
                    Debug.Assert(readPosition == 0);
                    Array.Copy(buffer, readPosition, data, offset + bytesRead, count - bytesRead);
                    readPosition += (count - bytesRead);
                    bytesRead = count;
                }

                byteCount -= bytesRead;
                Debug.Assert(byteCount >= 0);
                return bytesRead;
            }
        }

        /// <summary>
        /// Maximum length of this circular buffer
        /// </summary>
        public int MaxLength => buffer.Length;

        /// <summary>
        /// Number of bytes currently stored in the circular buffer
        /// </summary>
        public int Count
        {
            get
            {
                lock (lockObject)
                {
                    return byteCount;
                }
            }
        }

        /// <summary>
        /// Resets the state of the object.
        /// </summary>
        /// <remarks>
        /// This method locks the <paramref name="lockObject"/> and then calls the <see cref="ResetInner"/> method to reset the state of the object.
        /// </remarks>
        public void Reset()
        {
            lock (lockObject)
            {
                ResetInner();
            }
        }

        /// <summary>
        /// Resets the internal state of the object.
        /// </summary>
        /// <remarks>
        /// This method resets the byte count, read position, and write position to their initial values, effectively clearing the internal state of the object.
        /// </remarks>
        private void ResetInner()
        {
            byteCount = 0;
            readPosition = 0;
            writePosition = 0;
        }

        /// <summary>
        /// Advances the read position in the buffer by the specified count.
        /// </summary>
        /// <param name="count">The number of positions to advance the read position by.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the count is negative.</exception>
        /// <remarks>
        /// This method advances the read position in the buffer by the specified count. If the count is greater than or equal to the total byte count, it resets the buffer.
        /// Otherwise, it decrements the byte count by the specified count, advances the read position, and ensures that the read position wraps around if it exceeds the maximum length.
        /// </remarks>
        public void Advance(int count)
        {
            lock (lockObject)
            {
                if (count >= byteCount)
                {
                    ResetInner();
                }
                else
                {
                    byteCount -= count;
                    readPosition += count;
                    readPosition %= MaxLength;
                }
            }
        }
    }
}
