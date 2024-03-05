using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Wave
{
    /// <summary>
    /// Utility class to intercept audio from an IWaveProvider and
    /// save it to disk
    /// </summary>
    public class WaveRecorder : IWaveProvider, IDisposable
    {
        private WaveFileWriter writer;
        private IWaveProvider source;

        /// <summary>
        /// Constructs a new WaveRecorder
        /// </summary>
        /// <param name="destination">The location to write the WAV file to</param>
        /// <param name="source">The Source Wave Provider</param>
        public WaveRecorder(IWaveProvider source, string destination)
        {
            this.source = source;
            this.writer = new WaveFileWriter(destination, source.WaveFormat);
        }

        /// <summary>
        /// Reads a specified number of bytes from the source and writes them to the writer, returning the number of bytes read.
        /// </summary>
        /// <param name="buffer">The buffer to read the bytes into.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        /// <remarks>
        /// This method reads a maximum of <paramref name="count"/> bytes from the source into the <paramref name="buffer"/> at the specified <paramref name="offset"/>.
        /// It then writes the actual number of bytes read to the writer and returns this value.
        /// </remarks>
        public int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = source.Read(buffer, offset, count);
            writer.Write(buffer, offset, bytesRead);
            return bytesRead;
        }

        /// <summary>
        /// The WaveFormat
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return source.WaveFormat; }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <remarks>
        /// This method disposes the writer if it is not null and sets it to null.
        /// </remarks>
        public void Dispose()
        {
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }
        }
    }
}
