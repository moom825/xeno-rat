using System;
using System.Runtime.InteropServices;
using NAudio.Utils;

namespace NAudio.Dmo
{
    /// <summary>
    /// Attempting to implement the COM IMediaBuffer interface as a .NET object
    /// Not sure what will happen when I pass this to an unmanaged object
    /// </summary>
    public class MediaBuffer : IMediaBuffer, IDisposable
    {
        private IntPtr buffer;
        private int length;
        private readonly int maxLength;
        
        /// <summary>
        /// Creates a new Media Buffer
        /// </summary>
        /// <param name="maxLength">Maximum length in bytes</param>
        public MediaBuffer(int maxLength)
        {
            buffer = Marshal.AllocCoTaskMem(maxLength);
            this.maxLength = maxLength;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the current instance of the <see cref="ClassName"/> class.
        /// </summary>
        /// <remarks>
        /// This method releases the unmanaged memory allocated for the buffer, if it is not already released.
        /// It also requests that the common language runtime not call the finalizer for the current instance of the class.
        /// </remarks>
        public void Dispose()
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(buffer);
                buffer = IntPtr.Zero;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~MediaBuffer()
        {
            Dispose();
        }

        /// <summary>
        /// Sets the length of the media buffer.
        /// </summary>
        /// <param name="length">The new length to be set for the media buffer.</param>
        /// <returns>
        ///   <see cref="HResult.E_INVALIDARG"/> if the specified length is greater than the maximum length;
        ///   otherwise, <see cref="HResult.S_OK"/>.
        /// </returns>
        /// <remarks>
        /// This method sets the length of the media buffer to the specified value. If the specified length is greater than the maximum length,
        /// it returns <see cref="HResult.E_INVALIDARG"/>; otherwise, it sets the length and returns <see cref="HResult.S_OK"/>.
        /// </remarks>
        int IMediaBuffer.SetLength(int length)
        {
            //System.Diagnostics.Debug.WriteLine(String.Format("Set Length {0}", length));
            if (length > maxLength)
            {
                return HResult.E_INVALIDARG;
            }
            this.length = length;
            return HResult.S_OK;
        }

        /// <summary>
        /// Retrieves the maximum length of the media buffer and returns the result.
        /// </summary>
        /// <param name="maxLength">When this method returns, contains the maximum length of the media buffer.</param>
        /// <returns>An HRESULT value indicating the success or failure of the method call.</returns>
        int IMediaBuffer.GetMaxLength(out int maxLength)
        {
            //System.Diagnostics.Debug.WriteLine("Get Max Length");
            maxLength = this.maxLength;
            return HResult.S_OK;
        }

        /// <summary>
        /// Retrieves the buffer and its length and writes them to the specified memory locations.
        /// </summary>
        /// <param name="bufferPointerPointer">A pointer to the memory location where the buffer pointer will be written.</param>
        /// <param name="validDataLengthPointer">A pointer to the memory location where the valid data length will be written.</param>
        /// <returns>The result of the operation. Returns HResult.S_OK if successful.</returns>
        /// <remarks>
        /// If <paramref name="bufferPointerPointer"/> is not IntPtr.Zero, the buffer pointer is written to the specified memory location.
        /// If <paramref name="validDataLengthPointer"/> is not IntPtr.Zero, the valid data length is written to the specified memory location.
        /// </remarks>
        int IMediaBuffer.GetBufferAndLength(IntPtr bufferPointerPointer, IntPtr validDataLengthPointer)
        {

            //System.Diagnostics.Debug.WriteLine(String.Format("Get Buffer and Length {0},{1}",
            //    bufferPointerPointer,validDataLengthPointer));
            if (bufferPointerPointer != IntPtr.Zero)
            {
                Marshal.WriteIntPtr(bufferPointerPointer, this.buffer);
            }
            if (validDataLengthPointer != IntPtr.Zero)
            {
                Marshal.WriteInt32(validDataLengthPointer, this.length);

            }
            //System.Diagnostics.Debug.WriteLine("Finished Getting Buffer and Length");
            return HResult.S_OK;

        }

        #endregion

        /// <summary>
        /// Length of data in the media buffer
        /// </summary>
        public int Length
        {
            get { return length; }
            set 
            {
                if (length > maxLength)
                {
                    throw new ArgumentException("Cannot be greater than maximum buffer size");
                }
                length = value; 
            }
        }

        /// <summary>
        /// Loads the specified data into the buffer.
        /// </summary>
        /// <param name="data">The byte array containing the data to be loaded.</param>
        /// <param name="bytes">The number of bytes to be loaded from the <paramref name="data"/> array.</param>
        /// <remarks>
        /// This method sets the length of the buffer to the specified number of bytes and copies the data from the input <paramref name="data"/> array into the buffer using the Marshal.Copy method.
        /// </remarks>
        public void LoadData(byte[] data, int bytes)
        {
            this.Length = bytes;
            Marshal.Copy(data, 0, buffer, bytes);
        }

        /// <summary>
        /// Copies the elements of the buffer to the specified array starting at the specified offset.
        /// </summary>
        /// <param name="data">The destination array where the data will be copied.</param>
        /// <param name="offset">The zero-based byte offset in the destination array at which to begin copying bytes.</param>
        /// <exception cref="ArgumentNullException">Thrown when the destination array is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the offset is less than zero or greater than the length of the destination array.</exception>
        public void RetrieveData(byte[] data, int offset)
        {
            Marshal.Copy(buffer, data, offset, Length);
        }
    }
}
