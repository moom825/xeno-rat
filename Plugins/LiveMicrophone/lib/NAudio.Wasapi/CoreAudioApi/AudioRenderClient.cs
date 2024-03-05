using System;
using NAudio.CoreAudioApi.Interfaces;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Audio Render Client
    /// </summary>
    public class AudioRenderClient : IDisposable
    {
        IAudioRenderClient audioRenderClientInterface;

        internal AudioRenderClient(IAudioRenderClient audioRenderClientInterface)
        {
            this.audioRenderClientInterface = audioRenderClientInterface;
        }

        /// <summary>
        /// Requests a buffer of audio data for rendering and returns a pointer to the buffer.
        /// </summary>
        /// <param name="numFramesRequested">The number of audio frames requested for the buffer.</param>
        /// <returns>A pointer to the buffer containing the requested audio data.</returns>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when the audioRenderClientInterface.GetBuffer method returns an error HRESULT.</exception>
        public IntPtr GetBuffer(int numFramesRequested)
        {
            Marshal.ThrowExceptionForHR(audioRenderClientInterface.GetBuffer(numFramesRequested, out var bufferPointer));
            return bufferPointer;
        }

        /// <summary>
        /// Releases the buffer and signals the audio device that the buffer is ready for playback or capture.
        /// </summary>
        /// <param name="numFramesWritten">The number of frames written to the buffer.</param>
        /// <param name="bufferFlags">Flags indicating the state of the buffer.</param>
        /// <exception cref="MarshalDirectiveException">Thrown when an error is encountered during the release of the buffer.</exception>
        public void ReleaseBuffer(int numFramesWritten,AudioClientBufferFlags bufferFlags)
        {
            Marshal.ThrowExceptionForHR(audioRenderClientInterface.ReleaseBuffer(numFramesWritten, bufferFlags));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <remarks>
        /// This method releases the unmanaged resources used by the <see cref="audioRenderClientInterface"/> and suppresses the finalization of the current object.
        /// </remarks>
        public void Dispose()
        {
            if (audioRenderClientInterface != null)
            {
                // althugh GC would do this for us, we want it done now
                // to let us reopen WASAPI
                Marshal.ReleaseComObject(audioRenderClientInterface);
                audioRenderClientInterface = null;
                GC.SuppressFinalize(this);
            }
        }
    }
}
