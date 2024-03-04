using NAudio.Wasapi.CoreAudioApi.Interfaces;
using System;
using System.Runtime.InteropServices;

namespace NAudio.Wasapi.CoreAudioApi
{
    static class NativeMethods
    {

        /// <summary>
        /// Activates an audio interface asynchronously.
        /// </summary>
        /// <param name="deviceInterfacePath">The path of the device interface to be activated.</param>
        /// <param name="riid">The interface identifier.</param>
        /// <param name="activationParams">A pointer to a PropVariant; typically null.</param>
        /// <param name="completionHandler">The completion handler for the activation operation.</param>
        /// <param name="activationOperation">The asynchronous activation operation.</param>
        /// <remarks>
        /// This method activates an audio interface asynchronously using the specified device interface path and interface identifier.
        /// The activation operation is completed using the provided completion handler.
        /// </remarks>
        [DllImport("Mmdevapi.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void ActivateAudioInterfaceAsync(
            [In, MarshalAs(UnmanagedType.LPWStr)] string deviceInterfacePath,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [In] IntPtr activationParams, // n.b. is actually a pointer to a PropVariant, but we never need to pass anything but null
            [In] IActivateAudioInterfaceCompletionHandler completionHandler,
            out IActivateAudioInterfaceAsyncOperation activationOperation);
    }
}
