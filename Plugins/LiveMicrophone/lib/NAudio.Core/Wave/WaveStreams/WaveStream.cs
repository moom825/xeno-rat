// created on 27/12/2002 at 20:20
using System;
using System.IO;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave 
{
    /// <summary>
    /// Base class for all WaveStream classes. Derives from stream.
    /// </summary>
    public abstract class WaveStream : Stream, IWaveProvider
    {
        /// <summary>
        /// Retrieves the WaveFormat for this stream
        /// </summary>
        public abstract WaveFormat WaveFormat { get; }

        // base class includes long Position get; set
        // base class includes long Length get
        // base class includes Read
        // base class includes Dispose

        /// <summary>
        /// We can read from this stream
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// We can seek within this stream
        /// </summary>
        public override bool CanSeek => true;

        /// <summary>
        /// We can't write to this stream
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// Flushes the buffer of the current stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush() { }

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
        /// <param name="origin">A value of type <see cref="System.IO.SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        /// <exception cref="System.IO.IOException">An I/O error occurs.</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
                Position = offset;
            else if (origin == SeekOrigin.Current)
                Position += offset;
            else
                Position = Length + offset;
            return Position;
        }

        /// <summary>
        /// Throws a NotSupportedException with the message "Can't set length of a WaveFormatString".
        /// </summary>
        /// <param name="length">The length to be set.</param>
        /// <exception cref="NotSupportedException">Thrown when attempting to set the length of a WaveFormatString.</exception>
        public override void SetLength(long length)
        {
            throw new NotSupportedException("Can't set length of a WaveFormatString");
        }

        /// <summary>
        /// Throws a NotSupportedException with the message "Can't write to a WaveFormatString".
        /// </summary>
        /// <param name="buffer">The buffer to write data from.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin writing bytes.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <exception cref="NotSupportedException">Thrown when attempting to write to a WaveFormatString.</exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Can't write to a WaveFormatString");
        }

        /// <summary>
        /// The block alignment for this wavestream. Do not modify the Position
        /// to anything that is not a whole multiple of this value
        /// </summary>
        public virtual int BlockAlign => WaveFormat.BlockAlign;

        /// <summary>
        /// Skips the playback to a new position in the audio file based on the specified number of seconds.
        /// </summary>
        /// <param name="seconds">The number of seconds to skip the playback by.</param>
        /// <remarks>
        /// This method calculates the new position in the audio file based on the current position and the specified number of seconds to skip.
        /// If the new position exceeds the length of the audio file, the playback position is set to the end of the file.
        /// If the new position is negative, the playback position is set to the beginning of the file.
        /// Otherwise, the playback position is set to the calculated new position.
        /// </remarks>
        public void Skip(int seconds)
        {
            long newPosition = Position + WaveFormat.AverageBytesPerSecond*seconds;
            if (newPosition > Length)
                Position = Length;
            else if (newPosition < 0)
                Position = 0;
            else
                Position = newPosition;
        }

        /// <summary>
        /// The current position in the stream in Time format
        /// </summary>
        public virtual TimeSpan CurrentTime
        {
            get
            {
                return TimeSpan.FromSeconds((double)Position / WaveFormat.AverageBytesPerSecond);                
            }
            set
            {
                Position = (long) (value.TotalSeconds * WaveFormat.AverageBytesPerSecond);
            }
        }

        /// <summary>
        /// Total length in real-time of the stream (may be an estimate for compressed files)
        /// </summary>
        public virtual TimeSpan TotalTime
        {
            get
            {
                return TimeSpan.FromSeconds((double) Length / WaveFormat.AverageBytesPerSecond);
            }
        }

        /// <summary>
        /// Checks if there is data available at the current position.
        /// </summary>
        /// <param name="count">The number of data items to check for.</param>
        /// <returns>True if there is data available within the specified count; otherwise, false.</returns>
        public virtual bool HasData(int count)
        {
            return Position < Length;
        }
    }
}
