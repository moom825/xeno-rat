using System;
using System.Diagnostics;
using NAudio.Wave.Compression;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// IWaveProvider that passes through an ACM Codec
    /// </summary>
    public class WaveFormatConversionProvider : IWaveProvider, IDisposable
    {
        private readonly AcmStream conversionStream;
        private readonly IWaveProvider sourceProvider;
        private readonly int preferredSourceReadSize;
        private int leftoverDestBytes;
        private int leftoverDestOffset;
        private int leftoverSourceBytes;
        private bool isDisposed;

        /// <summary>
        /// Create a new WaveFormat conversion stream
        /// </summary>
        /// <param name="targetFormat">Desired output format</param>
        /// <param name="sourceProvider">Source Provider</param>
        public WaveFormatConversionProvider(WaveFormat targetFormat, IWaveProvider sourceProvider)
        {
            this.sourceProvider = sourceProvider;
            WaveFormat = targetFormat;

            conversionStream = new AcmStream(sourceProvider.WaveFormat, targetFormat);

            preferredSourceReadSize = Math.Min(sourceProvider.WaveFormat.AverageBytesPerSecond, conversionStream.SourceBuffer.Length);
            preferredSourceReadSize -= (preferredSourceReadSize% sourceProvider.WaveFormat.BlockAlign);
        }

        /// <summary>
        /// Gets the WaveFormat of this stream
        /// </summary>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// Repositions the conversion stream and resets leftover bytes and offsets.
        /// </summary>
        public void Reposition()
        {
            leftoverDestBytes = 0;
            leftoverDestOffset = 0;
            leftoverSourceBytes = 0;
            conversionStream.Reposition();
        }

        /// <summary>
        /// Reads data from the input buffer and returns the number of bytes read.
        /// </summary>
        /// <param name="buffer">The input buffer to read data from.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin reading.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        /// <remarks>
        /// This method reads data from the input buffer and returns the total number of bytes read into the buffer. It ensures that the count is a multiple of the block align, and if not, it adjusts the count to read complete blocks.
        /// The method then proceeds to copy any leftover destination bytes, followed by converting one full source buffer and saving any leftover bytes for the next call to Read.
        /// The method returns the total number of bytes read into the buffer.
        /// </remarks>
        public int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;
            if (count % WaveFormat.BlockAlign != 0)
            {
                //throw new ArgumentException("Must read complete blocks");
                count -= (count % WaveFormat.BlockAlign);
            }

            while (bytesRead < count)
            {
                // first copy in any leftover destination bytes
                int readFromLeftoverDest = Math.Min(count - bytesRead, leftoverDestBytes);
                if (readFromLeftoverDest > 0)
                {
                    Array.Copy(conversionStream.DestBuffer, leftoverDestOffset, buffer, offset+bytesRead, readFromLeftoverDest);
                    leftoverDestOffset += readFromLeftoverDest;
                    leftoverDestBytes -= readFromLeftoverDest;
                    bytesRead += readFromLeftoverDest;
                }
                if (bytesRead >= count)
                {
                    // we've fulfilled the request from the leftovers alone
                    break;
                }

                // now we'll convert one full source buffer
                var sourceReadSize = Math.Min(preferredSourceReadSize,
                    conversionStream.SourceBuffer.Length - leftoverSourceBytes);

                // always read our preferred size, we can always keep leftovers for the next call to Read if we get
                // too much
                int sourceBytesRead = sourceProvider.Read(conversionStream.SourceBuffer, leftoverSourceBytes, sourceReadSize);
                int sourceBytesAvailable = sourceBytesRead + leftoverSourceBytes;
                if (sourceBytesAvailable == 0)
                {
                    // we've reached the end of the input
                    break;
                }

                int sourceBytesConverted;
                int destBytesConverted = conversionStream.Convert(sourceBytesAvailable, out sourceBytesConverted);
                if (sourceBytesConverted == 0)
                {
                    Debug.WriteLine($"Warning: couldn't convert anything from {sourceBytesAvailable}");
                    // no point backing up in this case as we're not going to manage to finish playing this
                    break;
                }
                leftoverSourceBytes = sourceBytesAvailable - sourceBytesConverted;

                if (leftoverSourceBytes > 0)
                {
                    // buffer.blockcopy is safe for overlapping copies
                    Buffer.BlockCopy(conversionStream.SourceBuffer, sourceBytesConverted, conversionStream.SourceBuffer,
                        0, leftoverSourceBytes);
                }

                if (destBytesConverted > 0)
                {
                    int bytesRequired = count - bytesRead;
                    int toCopy = Math.Min(destBytesConverted, bytesRequired);
                    
                    // save leftovers
                    if (toCopy < destBytesConverted)
                    {
                        leftoverDestBytes = destBytesConverted - toCopy;
                        leftoverDestOffset = toCopy;
                    }
                    Array.Copy(conversionStream.DestBuffer, 0, buffer, bytesRead + offset, toCopy);
                    bytesRead += toCopy;
                }
                else
                {
                    // possible error here
                    Debug.WriteLine(
                        $"sourceBytesRead: {sourceBytesRead}, sourceBytesConverted {sourceBytesConverted}, destBytesConverted {destBytesConverted}");
                    //Debug.Assert(false, "conversion stream returned nothing at all");
                    break;
                }
            }
            return bytesRead;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <remarks>
        /// This method calls the Dispose(Boolean) method, passing in 'true', and suppresses the finalization of the object.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
                conversionStream?.Dispose();
            }
        }

        /// <summary>
        /// Disposes this resource
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~WaveFormatConversionProvider()
        {
            Dispose(false);
        }
    }
}