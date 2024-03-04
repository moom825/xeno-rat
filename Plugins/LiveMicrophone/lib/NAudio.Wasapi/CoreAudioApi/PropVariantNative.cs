using System;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi.Interfaces
{
    class PropVariantNative
    {

        /// <summary>
        /// Clears the memory allocated for a PROPVARIANT structure.
        /// </summary>
        /// <param name="pvar">A pointer to a PROPVARIANT structure.</param>
        /// <returns>An HRESULT value indicating success or failure.</returns>
        /// <remarks>
        /// This method releases the memory allocated for the specified PROPVARIANT structure.
        /// It should be called to free the memory when the PROPVARIANT is no longer needed.
        /// </remarks>
        [DllImport("ole32.dll")]
#endif
        internal static extern int PropVariantClear(ref PropVariant pvar);

#if WINDOWS_UWP
        [DllImport("api-ms-win-core-com-l1-1-1.dll")]
#else
        [DllImport("ole32.dll")]
#endif
        internal static extern int PropVariantClear(IntPtr pvar);
    }
}
