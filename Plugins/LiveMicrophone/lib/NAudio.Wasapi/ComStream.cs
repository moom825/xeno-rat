using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace NAudio.Wave
{
    /// <summary>
    /// Implementation of Com IStream
    /// </summary>
    class ComStream : Stream, IStream
    {
        private Stream stream;

        public override bool CanRead => stream.CanRead;

        public override bool CanSeek => stream.CanSeek;

        public override bool CanWrite => stream.CanWrite;

        public override long Length => stream.Length;

        public override long Position
        {
            get { return stream.Position; }
            set { stream.Position = value; }
        }

        public ComStream(Stream stream)
            : this(stream, true)
        {
        }

        internal ComStream(Stream stream, bool synchronizeStream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (synchronizeStream)
                stream = Synchronized(stream);
            this.stream = stream;
        }

        /// <summary>
        /// Clones the current stream.
        /// </summary>
        /// <param name="ppstm">When this method returns, contains a reference to the cloned stream. This parameter is passed uninitialized.</param>
        /// <remarks>
        /// This method creates a new stream object that is a copy of the current stream.
        /// The cloned stream is independent of the original stream, and any changes to the original stream do not affect the cloned stream, and vice versa.
        /// </remarks>
        void IStream.Clone(out IStream ppstm)
        {
            ppstm = null;
        }

        /// <summary>
        /// Flushes the stream.
        /// </summary>
        /// <param name="grfCommitFlags">Flags that specify the nature of the commit operation.</param>
        /// <remarks>
        /// This method flushes the underlying stream, writing any buffered data to the underlying file or network stream.
        /// </remarks>
        void IStream.Commit(int grfCommitFlags)
        {
            stream.Flush();
        }

        /// <summary>
        /// Copies a specified number of bytes from the current stream to another stream.
        /// </summary>
        /// <param name="pstm">The stream to which the data is to be copied.</param>
        /// <param name="cb">The number of bytes to be copied from the current stream.</param>
        /// <param name="pcbRead">A pointer to a 64-bit value that receives the actual number of bytes read from the source stream.</param>
        /// <param name="pcbWritten">A pointer to a 64-bit value that receives the actual number of bytes written to the destination stream.</param>
        void IStream.CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
        {
        }

        /// <summary>
        /// Locks a region of the stream.
        /// </summary>
        /// <param name="libOffset">The byte offset of the beginning of the range to be locked.</param>
        /// <param name="cb">The length of the range to be locked, in bytes.</param>
        /// <param name="dwLockType">The type of access to be granted for the specified range.</param>
        void IStream.LockRegion(long libOffset, long cb, int dwLockType)
        {
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This might be less than the number of bytes requested if that many bytes are not currently available, or zero if the end of the stream is reached before any bytes are read.</returns>
        void IStream.Read(byte[] pv, int cb, IntPtr pcbRead)
        {
            if (!CanRead)
                throw new InvalidOperationException("Stream is not readable.");
            int val = Read(pv, 0, cb);
            if (pcbRead != IntPtr.Zero)
                Marshal.WriteInt32(pcbRead, val);
        }

        /// <summary>
        /// Reverts the stream to its previous state.
        /// </summary>
        void IStream.Revert()
        {
        }

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
        /// <param name="origin">A value of type <see cref="System.IO.SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        void IStream.Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition)
        {
            SeekOrigin origin = (SeekOrigin) dwOrigin;
            long val = Seek(dlibMove, origin);
            if (plibNewPosition != IntPtr.Zero)
                Marshal.WriteInt64(plibNewPosition, val);
        }

        /// <summary>
        /// Sets the size of the stream to the specified new size.
        /// </summary>
        /// <param name="libNewSize">The new size to set for the stream.</param>
        /// <remarks>
        /// This method sets the size of the stream to the specified new size.
        /// </remarks>
        void IStream.SetSize(long libNewSize)
        {
            SetLength(libNewSize);
        }

        /// <summary>
        /// Retrieves the statistics for this stream.
        /// </summary>
        /// <param name="pstatstg">When this method returns, contains a STATSTG structure that describes this stream object. This parameter is passed uninitialized.</param>
        /// <param name="grfStatFlag">Members of the STATFLAG enumeration that control the type of statistics returned in pstatstg.</param>
        /// <exception cref="ObjectDisposedException">Thrown when the stream has been disposed.</exception>
        void IStream.Stat(out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg, int grfStatFlag)
        {
            const int STGM_READ = 0x00000000;
            const int STGM_WRITE = 0x00000001;
            const int STGM_READWRITE = 0x00000002;

            var tmp = new System.Runtime.InteropServices.ComTypes.STATSTG { type = 2, cbSize = Length, grfMode = 0 };

            if (CanWrite && CanRead)
                tmp.grfMode |= STGM_READWRITE;
            else if (CanRead)
                tmp.grfMode |= STGM_READ;
            else if (CanWrite)
                tmp.grfMode |= STGM_WRITE;
            else
                throw new ObjectDisposedException("Stream");

            pstatstg = tmp;
        }

        /// <summary>
        /// Unlocks the specified region of the stream.
        /// </summary>
        /// <param name="libOffset">The byte offset of the beginning of the region to be unlocked.</param>
        /// <param name="cb">The length of the region to be unlocked in bytes.</param>
        /// <param name="dwLockType">The type of access to the region to be unlocked.</param>
        void IStream.UnlockRegion(long libOffset, long cb, int dwLockType)
        {
        }

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        /// <remarks>
        /// This method writes count bytes from buffer to the current stream at the current position.
        /// The current position within the stream is advanced by count.
        /// If the write operation is successful, the current position within the stream is advanced by count; otherwise, the current position within the stream is unchanged.
        /// </remarks>
        void IStream.Write(byte[] pv, int cb, IntPtr pcbWritten)
        {
            if (!CanWrite)
                throw new InvalidOperationException("Stream is not writeable.");
            Write(pv, 0, cb);
            if (pcbWritten != IntPtr.Zero)
                Marshal.WriteInt32(pcbWritten, cb);
        }

        /// <summary>
        /// Flushes the stream's buffer.
        /// </summary>
        /// <remarks>
        /// This method flushes the buffer of the underlying stream, writing any buffered data to the underlying device.
        /// </remarks>
        public override void Flush()
        {
            stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }

        /// <summary>
        /// Sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the stream in bytes.</param>
        /// <remarks>
        /// This method sets the length of the current stream to the specified value.
        /// If the specified value is less than the current length of the stream, the stream is truncated.
        /// If the specified value is greater than the current length of the stream, the stream is expanded.
        /// If the stream is expanded, the contents of the stream between the old and new lengths are undefined.
        /// </remarks>
        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
        }

        /// <summary>
        /// Disposes the stream if it is not null.
        /// </summary>
        /// <param name="disposing">A boolean value indicating whether the method is being called from the Dispose method and not from the finalizer.</param>
        /// <remarks>
        /// This method disposes the stream if it is not null and sets it to null.
        /// </remarks>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (stream == null)
                return;
            stream.Dispose();
            stream = null;
        }

        /// <summary>
        /// Closes the stream and releases any system resources associated with it.
        /// </summary>
        /// <remarks>
        /// This method closes the underlying stream if it is not null and releases any system resources associated with it.
        /// </remarks>
        public override void Close()
        {
            base.Close();
            if (stream == null)
                return;
            stream.Close();
            stream = null;
        }
    }
}
