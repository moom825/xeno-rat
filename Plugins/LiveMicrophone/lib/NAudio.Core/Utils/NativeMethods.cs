using System;
using System.Runtime.InteropServices;

namespace NAudio.Utils
{
    /// <summary>
    /// General purpose native methods for internal NAudio use
    /// </summary>
    public static class NativeMethods
    {

        /// <summary>
        /// Loads the specified dynamic-link library (DLL) into the address space of the calling process.
        /// </summary>
        /// <param name="dllToLoad">The name of the DLL to be loaded.</param>
        /// <returns>A handle to the loaded DLL. If the function fails, the return value is NULL.</returns>
        /// <remarks>
        /// The LoadLibrary function maps the specified DLL file into the address space of the calling process. If the specified file is a protected system file, the function fails.
        /// </remarks>
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllToLoad);

        /// <summary>
        /// Retrieves the address of an exported function or variable from the specified dynamic-link library (DLL).
        /// </summary>
        /// <param name="hModule">A handle to the DLL module that contains the function or variable.</param>
        /// <param name="procedureName">The function or variable name, or the function's ordinal value.</param>
        /// <returns>
        /// If the function succeeds, the return value is the address of the exported function or variable.
        /// If the function fails, the return value is NULL.
        /// </returns>
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        /// <summary>
        /// Frees the loaded dynamic-link library (DLL) module and returns a value indicating success.
        /// </summary>
        /// <param name="hModule">A handle to the loaded DLL module.</param>
        /// <returns><see langword="true"/> if the library is successfully freed; otherwise, <see langword="false"/>.</returns>
        [DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr hModule);
    }
}
