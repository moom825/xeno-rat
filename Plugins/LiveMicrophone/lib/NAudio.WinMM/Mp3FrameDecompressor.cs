using System;
using NAudio.Wave.Compression;

namespace NAudio.Wave
{
    /// <summary>
    /// MP3 Frame Decompressor using ACM
    /// </summary>
    public class AcmMp3FrameDecompressor : IMp3FrameDecompressor
    {
        private readonly AcmStream conversionStream;
        private readonly WaveFormat pcmFormat;
        private bool disposed;

        /// <summary>
        /// Creates a new ACM frame decompressor
        /// </summary>
        /// <param name="sourceFormat">The MP3 source format</param>
        public AcmMp3FrameDecompressor(WaveFormat sourceFormat)
        {
            this.pcmFormat = AcmStream.SuggestPcmFormat(sourceFormat);
            try
            {
                conversionStream = new AcmStream(sourceFormat, pcmFormat);
            }
            catch (Exception)
            {
                disposed = true;
                GC.SuppressFinalize(this);
                throw;
            }
        }

        /// <summary>
        /// Output format (PCM)
        /// </summary>
        public WaveFormat OutputFormat { get { return pcmFormat; } }

        /// <summary>
        /// Decompresses the provided Mp3Frame and copies the decompressed data to the destination array starting at the specified offset.
        /// </summary>
        /// <param name="frame">The Mp3Frame to be decompressed.</param>
        /// <param name="dest">The destination array where the decompressed data will be copied.</param>
        /// <param name="destOffset">The offset in the destination array where the decompressed data will be copied.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided Mp3Frame is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the whole MP3 frame cannot be converted.</exception>
        /// <returns>The number of bytes copied to the destination array.</returns>
        /// <remarks>
        /// This method decompresses the provided Mp3Frame and copies the decompressed data to the destination array starting at the specified offset.
        /// It also handles exceptions if the provided Mp3Frame is null or if the whole MP3 frame cannot be converted.
        /// </remarks>
        public int DecompressFrame(Mp3Frame frame, byte[] dest, int destOffset)
        {
            if (frame == null)
            {
                throw new ArgumentNullException("frame", "You must provide a non-null Mp3Frame to decompress");
            }
            Array.Copy(frame.RawData, conversionStream.SourceBuffer, frame.FrameLength);
            int converted = conversionStream.Convert(frame.FrameLength, out int sourceBytesConverted);
            if (sourceBytesConverted != frame.FrameLength)
            {
                throw new InvalidOperationException(String.Format("Couldn't convert the whole MP3 frame (converted {0}/{1})",
                    sourceBytesConverted, frame.FrameLength));
            }
            Array.Copy(conversionStream.DestBuffer, 0, dest, destOffset, converted);
            return converted;
        }

        /// <summary>
        /// Resets the position of the conversion stream to the beginning.
        /// </summary>
        public void Reset()
        {
            conversionStream.Reposition();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <remarks>
        /// This method checks if the object has been disposed and disposes the conversionStream if it is not null.
        /// It then suppresses the finalization of the object by the garbage collector.
        /// </remarks>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
				if(conversionStream != null)
					conversionStream.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Finalizer ensuring that resources get released properly
        /// </summary>
        ~AcmMp3FrameDecompressor()
        {
            System.Diagnostics.Debug.Assert(false, "AcmMp3FrameDecompressor Dispose was not called");
            Dispose();
        }
    }
}
