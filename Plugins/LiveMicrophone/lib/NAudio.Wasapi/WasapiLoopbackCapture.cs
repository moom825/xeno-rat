using System;
using NAudio.CoreAudioApi;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// WASAPI Loopback Capture
    /// based on a contribution from "Pygmy" - http://naudio.codeplex.com/discussions/203605
    /// </summary>
    public class WasapiLoopbackCapture : WasapiCapture
    {
        /// <summary>
        /// Initialises a new instance of the WASAPI capture class
        /// </summary>
        public WasapiLoopbackCapture() :
            this(GetDefaultLoopbackCaptureDevice())
        {
        }

        /// <summary>
        /// Initialises a new instance of the WASAPI capture class
        /// </summary>
        /// <param name="captureDevice">Capture device to use</param>
        public WasapiLoopbackCapture(MMDevice captureDevice) :
            base(captureDevice)
        {
        }

        /// <summary>
        /// Retrieves the default loopback capture device for audio rendering.
        /// </summary>
        /// <returns>The default loopback capture device for audio rendering.</returns>
        /// <remarks>
        /// This method retrieves the default loopback capture device using the MMDeviceEnumerator class and returns the default audio endpoint for rendering multimedia content.
        /// </remarks>
        public static MMDevice GetDefaultLoopbackCaptureDevice()
        {
            MMDeviceEnumerator devices = new MMDeviceEnumerator();
            return devices.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }

        /// <summary>
        /// Gets the audio client stream flags for loopback and base stream flags.
        /// </summary>
        /// <returns>The combined audio client stream flags for loopback and base stream flags.</returns>
        protected override AudioClientStreamFlags GetAudioClientStreamFlags()
        {
            return AudioClientStreamFlags.Loopback | base.GetAudioClientStreamFlags();
        }        
    }
}
