using System;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Simply shifts the input stream in time, optionally
    /// clipping its start and end.
    /// (n.b. may include looping in the future)
    /// </summary>
    public class WaveOffsetStream : WaveStream
    {
        private WaveStream sourceStream;
        private long audioStartPosition;
        private long sourceOffsetBytes;
        private long sourceLengthBytes;
        private long length;
        private readonly int bytesPerSample; // includes all channels
        private long position;
        private TimeSpan startTime;
        private TimeSpan sourceOffset;
        private TimeSpan sourceLength;
        private readonly object lockObject = new object();

        /// <summary>
        /// Creates a new WaveOffsetStream
        /// </summary>
        /// <param name="sourceStream">the source stream</param>
        /// <param name="startTime">the time at which we should start reading from the source stream</param>
        /// <param name="sourceOffset">amount to trim off the front of the source stream</param>
        /// <param name="sourceLength">length of time to play from source stream</param>
        public WaveOffsetStream(WaveStream sourceStream, TimeSpan startTime, TimeSpan sourceOffset, TimeSpan sourceLength)
        {
            if (sourceStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
                throw new ArgumentException("Only PCM supported");
            // TODO: add support for IEEE float + perhaps some others -
            // anything with a fixed bytes per sample
            
            this.sourceStream = sourceStream;
            position = 0;
            bytesPerSample = (sourceStream.WaveFormat.BitsPerSample / 8) * sourceStream.WaveFormat.Channels;
            StartTime = startTime;
            SourceOffset = sourceOffset;
            SourceLength = sourceLength;
            
        }

        /// <summary>
        /// Creates a WaveOffsetStream with default settings (no offset or pre-delay,
        /// and whole length of source stream)
        /// </summary>
        /// <param name="sourceStream">The source stream</param>
        public WaveOffsetStream(WaveStream sourceStream)
            : this(sourceStream, TimeSpan.Zero, TimeSpan.Zero, sourceStream.TotalTime)
        {
        }

        /// <summary>
        /// The length of time before which no audio will be played
        /// </summary>
        public TimeSpan StartTime
        {
            get 
            { 
                return startTime; 
            }
            set 
            {
                lock (lockObject)
                {
                    startTime = value;
                    audioStartPosition = (long)(startTime.TotalSeconds * sourceStream.WaveFormat.SampleRate) * bytesPerSample;
                    // fix up our length and position
                    length = audioStartPosition + sourceLengthBytes;
                    Position = Position;
                }
            }
        }

        /// <summary>
        /// An offset into the source stream from which to start playing
        /// </summary>
        public TimeSpan SourceOffset
        {
            get
            {
                return sourceOffset;
            }
            set
            {
                lock (lockObject)
                {
                    sourceOffset = value;
                    sourceOffsetBytes = (long)(sourceOffset.TotalSeconds * sourceStream.WaveFormat.SampleRate) * bytesPerSample;
                    // fix up our position
                    Position = Position;
                }
            }
        }

        /// <summary>
        /// Length of time to read from the source stream
        /// </summary>
        public TimeSpan SourceLength
        {
            get
            {
                return sourceLength;
            }
            set
            {
                lock (lockObject)
                {
                    sourceLength = value;
                    sourceLengthBytes = (long)(sourceLength.TotalSeconds * sourceStream.WaveFormat.SampleRate) * bytesPerSample;
                    // fix up our length and position
                    length = audioStartPosition + sourceLengthBytes;
                    Position = Position;
                }
            }
    
        }

        /// <summary>
        /// Gets the block alignment for this WaveStream
        /// </summary>
        public override int BlockAlign => sourceStream.BlockAlign;

        /// <summary>
        /// Returns the stream length
        /// </summary>
        public override long Length => length;

        /// <summary>
        /// Gets or sets the current position in the stream
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
                    // make sure we don't get out of sync
                    value -= (value % BlockAlign);
                    if (value < audioStartPosition)
                        sourceStream.Position = sourceOffsetBytes;
                    else
                        sourceStream.Position = sourceOffsetBytes + (value - audioStartPosition);
                    position = value;
                }
            }
        }

        /// <summary>
        /// Reads audio data from the source stream into the destination buffer, filling with silence if necessary, and returns the number of bytes read.
        /// </summary>
        /// <param name="destBuffer">The destination buffer to write the audio data into.</param>
        /// <param name="offset">The offset in the destination buffer at which to start writing the audio data.</param>
        /// <param name="numBytes">The number of bytes to read from the source stream and write into the destination buffer.</param>
        /// <returns>The total number of bytes read and written into the destination buffer, which is equal to <paramref name="numBytes"/>.</returns>
        /// <remarks>
        /// This method fills the destination buffer with silence if the current position is before the audio start position.
        /// It then reads audio data from the source stream into the destination buffer, ensuring not to read beyond the source stream's length.
        /// Finally, it fills out any remaining space in the destination buffer with zeroes and updates the position accordingly.
        /// </remarks>
        public override int Read(byte[] destBuffer, int offset, int numBytes)
        {
            lock (lockObject)
            {
                int bytesWritten = 0;
                // 1. fill with silence
                if (position < audioStartPosition)
                {
                    bytesWritten = (int)Math.Min(numBytes, audioStartPosition - position);
                    for (int n = 0; n < bytesWritten; n++)
                        destBuffer[n + offset] = 0;
                }
                if (bytesWritten < numBytes)
                {
                    // don't read too far into source stream
                    int sourceBytesRequired = (int)Math.Min(
                        numBytes - bytesWritten,
                        sourceLengthBytes + sourceOffsetBytes - sourceStream.Position);
                    int read = sourceStream.Read(destBuffer, bytesWritten + offset, sourceBytesRequired);
                    bytesWritten += read;
                }
                // 3. Fill out with zeroes
                for (int n = bytesWritten; n < numBytes; n++)
                    destBuffer[offset + n] = 0;
                position += numBytes;
                return numBytes;
            }
        }

        /// <summary>
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override WaveFormat WaveFormat => sourceStream.WaveFormat;

        /// <summary>
        /// Checks if the source stream has data available.
        /// </summary>
        /// <param name="count">The number of bytes to check for availability.</param>
        /// <returns>True if the source stream has the specified amount of data available; otherwise, false.</returns>
        /// <remarks>
        /// This method checks if the source stream has the specified amount of data available by comparing the current position and count with the audio start position and length.
        /// If the position plus count is less than the audio start position, or if the position is greater than or equal to the length, false is returned.
        /// Otherwise, it delegates the check to the source stream's HasData method and returns its result.
        /// </remarks>
        public override bool HasData(int count)
        {
            if (position + count < audioStartPosition)
                return false;
            if (position >= length)
                return false;
            // Check whether the source stream has data.
            // source stream should be in the right poisition
            return sourceStream.HasData(count);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the WaveOffsetStream and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        /// <remarks>
        /// This method disposes the sourceStream if it is not null and sets it to null.
        /// If <paramref name="disposing"/> is false, it asserts that the WaveOffsetStream was not disposed.
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
                System.Diagnostics.Debug.Assert(false, "WaveOffsetStream was not Disposed");
            }
            base.Dispose(disposing);
        }
    }
}
