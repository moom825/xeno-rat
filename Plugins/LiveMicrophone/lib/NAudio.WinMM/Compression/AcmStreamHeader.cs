using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave.Compression
{
    class AcmStreamHeader : IDisposable
    {
        private AcmStreamHeaderStruct streamHeader;
        private GCHandle hSourceBuffer;
        private GCHandle hDestBuffer;
        private IntPtr streamHandle;
        private bool firstTime;

        public AcmStreamHeader(IntPtr streamHandle, int sourceBufferLength, int destBufferLength)
        {
            streamHeader = new AcmStreamHeaderStruct();
            SourceBuffer = new byte[sourceBufferLength];
            hSourceBuffer = GCHandle.Alloc(SourceBuffer, GCHandleType.Pinned);

            DestBuffer = new byte[destBufferLength];
            hDestBuffer = GCHandle.Alloc(DestBuffer, GCHandleType.Pinned);

            this.streamHandle = streamHandle;
            firstTime = true;
            //Prepare();
        }

        /// <summary>
        /// Prepares the audio stream header for processing.
        /// </summary>
        /// <remarks>
        /// This method prepares the audio stream header for processing by setting the structure size and buffer pointers.
        /// It also calls the acmStreamPrepareHeader function from the AcmInterop library to prepare the audio stream header for processing.
        /// </remarks>
        /// <exception cref="MmException">Thrown when an error occurs during the preparation of the audio stream header.</exception>
        private void Prepare()
        {
            streamHeader.cbStruct = Marshal.SizeOf(streamHeader);
            streamHeader.sourceBufferLength = SourceBuffer.Length;
            streamHeader.sourceBufferPointer = hSourceBuffer.AddrOfPinnedObject();
            streamHeader.destBufferLength = DestBuffer.Length;
            streamHeader.destBufferPointer = hDestBuffer.AddrOfPinnedObject();
            MmException.Try(AcmInterop.acmStreamPrepareHeader(streamHandle, streamHeader, 0), "acmStreamPrepareHeader");
        }

        /// <summary>
        /// Unprepares the audio stream for conversion by updating the stream header and unpreparing the header using ACM interop.
        /// </summary>
        /// <remarks>
        /// This method updates the source and destination buffer lengths and pointers in the stream header, then unprepares the header using ACM interop.
        /// If an error occurs during unpreparing the header, a <see cref="MmException"/> is thrown with the corresponding error message.
        /// </remarks>
        /// <exception cref="MmException">Thrown when an error occurs during unpreparing the header.</exception>
        private void Unprepare()
        {
            streamHeader.sourceBufferLength = SourceBuffer.Length;
            streamHeader.sourceBufferPointer = hSourceBuffer.AddrOfPinnedObject();
            streamHeader.destBufferLength = DestBuffer.Length;
            streamHeader.destBufferPointer = hDestBuffer.AddrOfPinnedObject();

            MmResult result = AcmInterop.acmStreamUnprepareHeader(streamHandle, streamHeader, 0);
            if (result != MmResult.NoError)
            {
                //if (result == MmResult.AcmHeaderUnprepared)
                throw new MmException(result, "acmStreamUnprepareHeader");
            }
        }

        /// <summary>
        /// Sets the flag 'firstTime' to true, indicating that the repositioning is being performed for the first time.
        /// </summary>
        public void Reposition()
        {
            firstTime = true;
        }

        /// <summary>
        /// Converts the specified number of bytes using the Audio Compression Manager (ACM) and returns the number of bytes converted.
        /// </summary>
        /// <param name="bytesToConvert">The number of bytes to convert.</param>
        /// <param name="sourceBytesConverted">When this method returns, contains the actual number of bytes converted from the source buffer.</param>
        /// <returns>The number of bytes converted from the destination buffer.</returns>
        /// <remarks>
        /// This method prepares for the conversion, sets the source buffer length, and performs the ACM stream conversion using the specified flags.
        /// It then checks if the codec has changed the destination buffer length and updates the sourceBytesConverted parameter with the actual number of bytes converted.
        /// Finally, it unprepares for the conversion and returns the number of bytes converted from the destination buffer.
        /// </remarks>
        /// <exception cref="MmException">Thrown when an error occurs during the ACM stream conversion.</exception>
        public int Convert(int bytesToConvert, out int sourceBytesConverted)
        {
            Prepare();
            try
            {
                streamHeader.sourceBufferLength = bytesToConvert;
                streamHeader.sourceBufferLengthUsed = bytesToConvert;
                AcmStreamConvertFlags flags = firstTime ? (AcmStreamConvertFlags.Start | AcmStreamConvertFlags.BlockAlign) : AcmStreamConvertFlags.BlockAlign;
                MmException.Try(AcmInterop.acmStreamConvert(streamHandle, streamHeader, flags), "acmStreamConvert");
                firstTime = false;
                System.Diagnostics.Debug.Assert(streamHeader.destBufferLength == DestBuffer.Length, "Codecs should not change dest buffer length");
                sourceBytesConverted = streamHeader.sourceBufferLengthUsed;
            }
            finally
            {
                Unprepare();
            }

            return streamHeader.destBufferLengthUsed;
        }

        public byte[] SourceBuffer { get; private set; }

        public byte[] DestBuffer { get; private set; }

        #region IDisposable Members

        bool disposed = false;

        /// <summary>
        /// Disposes of the allocated resources.
        /// </summary>
        /// <param name="disposing">A boolean value indicating whether the method is being called from the <see langword="Dispose"/> method or the finalizer.</param>
        /// <remarks>
        /// This method releases the allocated resources if <paramref name="disposing"/> is <see langword="true"/>.
        /// The allocated resources include the source buffer, destination buffer, and their corresponding handles.
        /// Once the resources are disposed, the method sets the <see cref="disposed"/> flag to <see langword="true"/>.
        /// </remarks>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                //Unprepare();
                SourceBuffer = null;
                DestBuffer = null;
                hSourceBuffer.Free();
                hDestBuffer.Free();
            }
            disposed = true;
        }

        ~AcmStreamHeader()
        {
            System.Diagnostics.Debug.Assert(false, "AcmStreamHeader dispose was not called");
            Dispose(false);
        }
        #endregion
    }

}
