using System;
using System.Diagnostics;
using NAudio.Wave.Compression;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// WaveStream that passes through an ACM Codec
    /// </summary>
    public class WaveFormatConversionStream : WaveStream
    {
        private readonly WaveFormatConversionProvider conversionProvider;
        private readonly WaveFormat targetFormat;
        private readonly long length;
        private long position;
        private readonly WaveStream sourceStream;
        private bool isDisposed;

        /// <summary>
        /// Create a new WaveFormat conversion stream
        /// </summary>
        /// <param name="targetFormat">Desired output format</param>
        /// <param name="sourceStream">Source stream</param>
        public WaveFormatConversionStream(WaveFormat targetFormat, WaveStream sourceStream)
        {
            this.sourceStream = sourceStream;
            this.targetFormat = targetFormat;
            conversionProvider = new WaveFormatConversionProvider(targetFormat, sourceStream);
            length = EstimateSourceToDest((int)sourceStream.Length);
            position = 0;
        }

        /// <summary>
        /// Creates a PCM stream from the given source stream.
        /// </summary>
        /// <param name="sourceStream">The input wave stream.</param>
        /// <returns>A PCM stream converted from the <paramref name="sourceStream"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the suggested output format is invalid and no target format is explicitly provided.</exception>
        public static WaveStream CreatePcmStream(WaveStream sourceStream)
        {
            if (sourceStream.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                return sourceStream;
            }
            var pcmFormat = AcmStream.SuggestPcmFormat(sourceStream.WaveFormat);
            if (pcmFormat.SampleRate < 8000)
            {
                if (sourceStream.WaveFormat.Encoding == WaveFormatEncoding.G723)
                {
                    pcmFormat = new WaveFormat(8000, 16, 1);
                }
                else
                {
                    throw new InvalidOperationException("Invalid suggested output format, please explicitly provide a target format");
                }
            }
            return new WaveFormatConversionStream(pcmFormat, sourceStream);
        }

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
                // make sure we don't get out of sync
                value -= (value % BlockAlign);

                // this relies on conversionStream DestToSource and SourceToDest being reliable
                var desiredSourcePosition = EstimateDestToSource(value);  //conversionStream.DestToSource((int) value); 
                sourceStream.Position = desiredSourcePosition;
                position = EstimateSourceToDest(sourceStream.Position);  //conversionStream.SourceToDest((int)sourceStream.Position);
                conversionProvider.Reposition();
            }
        }

        /// <summary>
        /// Estimates the destination value from the given source value.
        /// </summary>
        /// <param name="source">The source value for estimation.</param>
        /// <returns>The estimated destination value based on the given <paramref name="source"/>.</returns>
        /// <remarks>
        /// This method is marked as obsolete and can be unreliable. It is not encouraged to use this method.
        /// </remarks>
        [Obsolete("can be unreliable, use of this method not encouraged")]
        public int SourceToDest(int source)
        {
            return (int)EstimateSourceToDest(source);
            //return conversionStream.SourceToDest(source);
        }

        /// <summary>
        /// Estimates the destination position based on the source position.
        /// </summary>
        /// <param name="source">The source position for estimation.</param>
        /// <returns>The estimated destination position based on the source position.</returns>
        private long EstimateSourceToDest(long source)
        {
            var dest = ((source * targetFormat.AverageBytesPerSecond) / sourceStream.WaveFormat.AverageBytesPerSecond);
            dest -= (dest % targetFormat.BlockAlign);
            return dest;
        }

        /// <summary>
        /// Estimates the source position corresponding to the given destination position.
        /// </summary>
        /// <param name="dest">The destination position for which the corresponding source position needs to be estimated.</param>
        /// <returns>The estimated source position corresponding to the given destination position.</returns>
        private long EstimateDestToSource(long dest)
        {
            var source = ((dest * sourceStream.WaveFormat.AverageBytesPerSecond) / targetFormat.AverageBytesPerSecond);
            source -= (source % sourceStream.WaveFormat.BlockAlign);
            return (int)source;
        }

        /// <summary>
        /// Converts the destination value to the source value using the EstimateDestToSource method and returns the result.
        /// </summary>
        /// <param name="dest">The destination value to be converted to the source value.</param>
        /// <returns>The converted source value from the given <paramref name="dest"/>.</returns>
        /// <exception cref="ObsoleteException">This method is obsolete and can be unreliable. Use of this method is not encouraged.</exception>
        [Obsolete("can be unreliable, use of this method not encouraged")]
        public int DestToSource(int dest)
        {
            return (int)EstimateDestToSource(dest);
            //return conversionStream.DestToSource(dest);
        }

        /// <summary>
        /// Returns the stream length
        /// </summary>
        public override long Length
        {
            get
            {
                return length;
            }
        }

        /// <summary>
        /// Gets the WaveFormat of this stream
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get
            {
                return targetFormat;
            }
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero if the end of the stream is reached before any bytes are read.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = conversionProvider.Read(buffer, offset, count);
            position += bytesRead;
            return bytesRead;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the WaveFormatConversionStream and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        /// <exception cref="ObjectDisposedException">Thrown if the WaveFormatConversionStream has already been disposed.</exception>
        /// <remarks>
        /// This method releases the unmanaged resources used by the WaveFormatConversionStream and optionally releases the managed resources.
        /// If <paramref name="disposing"/> is true, this method disposes the sourceStream and conversionProvider.
        /// If <paramref name="disposing"/> is false, this method checks if the WaveFormatConversionStream has already been disposed and asserts if not.
        /// </remarks>
        protected override void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
                if (disposing)
                {
                    sourceStream.Dispose();
                    conversionProvider.Dispose();
                }
                else
                {
                    // we've been called by the finalizer
                    Debug.Assert(false, "WaveFormatConversionStream was not disposed");
                }
            }
            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.
            base.Dispose(disposing);
        }
    }
}
