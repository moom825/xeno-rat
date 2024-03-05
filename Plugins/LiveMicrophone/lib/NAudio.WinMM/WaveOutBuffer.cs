using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave 
{
    /// <summary>
    /// A buffer of Wave samples for streaming to a Wave Output device
    /// </summary>
    public class WaveOutBuffer : IDisposable
    {
        private readonly WaveHeader header;
        private readonly Int32 bufferSize; // allocated bytes, may not be the same as bytes read
        private readonly byte[] buffer;
        private readonly IWaveProvider waveStream;
        private readonly object waveOutLock;
        private GCHandle hBuffer;
        private IntPtr hWaveOut;
        private GCHandle hHeader; // we need to pin the header structure
        private GCHandle hThis; // for the user callback

        /// <summary>
        /// creates a new wavebuffer
        /// </summary>
        /// <param name="hWaveOut">WaveOut device to write to</param>
        /// <param name="bufferSize">Buffer size in bytes</param>
        /// <param name="bufferFillStream">Stream to provide more data</param>
        /// <param name="waveOutLock">Lock to protect WaveOut API's from being called on >1 thread</param>
        public WaveOutBuffer(IntPtr hWaveOut, Int32 bufferSize, IWaveProvider bufferFillStream, object waveOutLock)
        {
            this.bufferSize = bufferSize;
            buffer = new byte[bufferSize];
            hBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            this.hWaveOut = hWaveOut;
            waveStream = bufferFillStream;
            this.waveOutLock = waveOutLock;

            header = new WaveHeader();
            hHeader = GCHandle.Alloc(header, GCHandleType.Pinned);
            header.dataBuffer = hBuffer.AddrOfPinnedObject();
            header.bufferLength = bufferSize;
            header.loops = 1;
            hThis = GCHandle.Alloc(this);
            header.userData = (IntPtr)hThis;
            lock (waveOutLock)
            {
                MmException.Try(WaveInterop.waveOutPrepareHeader(hWaveOut, header, Marshal.SizeOf(header)), "waveOutPrepareHeader");
            }
        }

        #region Dispose Pattern

        /// <summary>
        /// Finalizer for this wave buffer
        /// </summary>
        ~WaveOutBuffer()
        {
            Dispose(false);
            System.Diagnostics.Debug.Assert(true, "WaveBuffer was not disposed");
        }

        /// <summary>
        /// Disposes of the managed and unmanaged resources used by the WaveOut device.
        /// </summary>
        /// <param name="disposing">A boolean value indicating whether to dispose of managed resources.</param>
        /// <remarks>
        /// This method releases the managed resources if <paramref name="disposing"/> is true. It also releases the unmanaged resources including the allocated headers and buffers used by the WaveOut device.
        /// </remarks>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// Releases resources held by this WaveBuffer
        /// </summary>
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
            }
            // free unmanaged resources
            if (hHeader.IsAllocated)
                hHeader.Free();
            if (hBuffer.IsAllocated)
                hBuffer.Free();
            if (hThis.IsAllocated)
                hThis.Free();
            if (hWaveOut != IntPtr.Zero)
            {
                lock (waveOutLock)
                {
                    WaveInterop.waveOutUnprepareHeader(hWaveOut, header, Marshal.SizeOf(header));
                }
                hWaveOut = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Reads data from the wave stream, writes it to the output, and returns a boolean value indicating whether the operation is successful.
        /// </summary>
        /// <returns>True if data is successfully read and written to the output; otherwise, false.</returns>
        /// <remarks>
        /// This method locks the wave stream to read data into the buffer and then writes the data to the output.
        /// If no data is read from the wave stream, it returns false.
        /// The method also ensures that any remaining buffer space is filled with zeros before writing to the output.
        /// </remarks>
        public bool OnDone()
        {
            int bytes;
            lock (waveStream)
            {
                bytes = waveStream.Read(buffer, 0, buffer.Length);
            }
            if (bytes == 0)
            {
                return false;
            }
            for (int n = bytes; n < buffer.Length; n++)
            {
                buffer[n] = 0;
            }
            WriteToWaveOut();
            return true;
        }

        /// <summary>
        /// Whether the header's in queue flag is set
        /// </summary>
        public bool InQueue
        {
            get
            {
                return (header.flags & WaveHeaderFlags.InQueue) == WaveHeaderFlags.InQueue;
            }
        }

        /// <summary>
        /// The buffer size in bytes
        /// </summary>
        public int BufferSize => bufferSize;

        /// <summary>
        /// Writes the audio data to the WaveOut device for playback.
        /// </summary>
        /// <remarks>
        /// This method writes the audio data to the WaveOut device for playback. It locks the <paramref name="waveOutLock"/> to ensure thread safety and then calls the <see cref="WaveInterop.waveOutWrite"/> method to write the audio data to the device using the specified <paramref name="header"/>.
        /// If an error occurs during the write operation, a <see cref="MmException"/> is thrown with details about the specific error.
        /// After writing the data, this method ensures that the current object is kept alive by calling <see cref="GC.KeepAlive"/> to prevent premature garbage collection.
        /// </remarks>
        private void WriteToWaveOut()
        {
            MmResult result;

            lock (waveOutLock)
            {
                result = WaveInterop.waveOutWrite(hWaveOut, header, Marshal.SizeOf(header));
            }
            if (result != MmResult.NoError)
            {
                throw new MmException(result, "waveOutWrite");
            }

            GC.KeepAlive(this);
        }

    }
}
