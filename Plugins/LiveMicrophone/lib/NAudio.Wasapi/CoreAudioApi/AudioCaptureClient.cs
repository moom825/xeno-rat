using System;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Audio Capture Client
    /// </summary>
    public class AudioCaptureClient : IDisposable
    {
        IAudioCaptureClient audioCaptureClientInterface;

        internal AudioCaptureClient(IAudioCaptureClient audioCaptureClientInterface)
        {
            this.audioCaptureClientInterface = audioCaptureClientInterface;
        }

        /// <summary>
        /// Retrieves a pointer to the buffer that is ready for the next capture.
        /// </summary>
        /// <param name="numFramesToRead">When this method returns, contains the number of frames available in the captured buffer.</param>
        /// <param name="bufferFlags">When this method returns, contains flags indicating the status of the captured buffer.</param>
        /// <returns>A pointer to the buffer that is ready for the next capture.</returns>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when an error is encountered while retrieving the buffer.</exception>
        public IntPtr GetBuffer(
            out int numFramesToRead,
            out AudioClientBufferFlags bufferFlags,
            out long devicePosition,
            out long qpcPosition)
        {
            Marshal.ThrowExceptionForHR(audioCaptureClientInterface.GetBuffer(out var bufferPointer, out numFramesToRead, out bufferFlags, out devicePosition, out qpcPosition));
            return bufferPointer;
        }

        /// <summary>
        /// Gets a pointer to the buffer
        /// </summary>
        /// <param name="numFramesToRead">Number of frames to read</param>
        /// <param name="bufferFlags">Buffer flags</param>
        /// <returns>Pointer to the buffer</returns>
        public IntPtr GetBuffer(
            out int numFramesToRead,
            out AudioClientBufferFlags bufferFlags)
        {
            Marshal.ThrowExceptionForHR(audioCaptureClientInterface.GetBuffer(out var bufferPointer, out numFramesToRead, out bufferFlags, out _, out _));
            return bufferPointer;
        }

        /// <summary>
        /// Retrieves the size of the next audio packet and returns the number of frames in the packet.
        /// </summary>
        /// <returns>The number of frames in the next audio packet.</returns>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when an error occurs while retrieving the next packet size.</exception>
        public int GetNextPacketSize()
        {
            Marshal.ThrowExceptionForHR(audioCaptureClientInterface.GetNextPacketSize(out var numFramesInNextPacket));
            return numFramesInNextPacket;
        }

        /// <summary>
        /// Releases the buffer containing audio data after writing a specified number of frames.
        /// </summary>
        /// <param name="numFramesWritten">The number of frames that have been written to the buffer.</param>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when an error occurs during the release of the buffer.</exception>
        public void ReleaseBuffer(int numFramesWritten)
        {
            Marshal.ThrowExceptionForHR(audioCaptureClientInterface.ReleaseBuffer(numFramesWritten));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <remarks>
        /// This method releases the unmanaged resources used by the <see cref="audioCaptureClientInterface"/> and suppresses the finalization of the current object.
        /// </remarks>
        public void Dispose()
        {
            if (audioCaptureClientInterface != null)
            {
                // although GC would do this for us, we want it done now
                // to let us reopen WASAPI
                Marshal.ReleaseComObject(audioCaptureClientInterface);
                audioCaptureClientInterface = null;
                GC.SuppressFinalize(this);
            }
        }
    }
}