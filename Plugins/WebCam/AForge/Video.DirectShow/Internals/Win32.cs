// AForge Video for Windows Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2007-2011
// contacts@aforgenet.com
//

namespace AForge.Video.DirectShow.Internals
{
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;

    /// <summary>
    /// Some Win32 API used internally.
    /// </summary>
    /// 
    internal static class Win32
    {

        /// <summary>
        /// Creates a new instance of the bind context object.
        /// </summary>
        /// <param name="reserved">Reserved for future use. Must be 0.</param>
        /// <param name="ppbc">When this method returns, contains a reference to the new bind context object. This parameter is passed uninitialized.</param>
        /// <returns>An integer value indicating the result of the operation.</returns>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when an error occurs while creating the bind context object.</exception>
        [DllImport( "ole32.dll" )]
        public static extern
        int CreateBindCtx( int reserved, out IBindCtx ppbc );

        /// <summary>
        /// Parses a display name to create a moniker and returns the result.
        /// </summary>
        /// <param name="pbc">A reference to the bind context to use in this binding operation. </param>
        /// <param name="szUserName">The display name to be parsed.</param>
        /// <param name="pchEaten">On entry, contains the number of characters of szUserName to parse. On exit, contains the number of characters of szUserName that were successfully parsed.</param>
        /// <param name="ppmk">When this method returns, contains the interface pointer to the moniker that was created based on <paramref name="szUserName"/>.</param>
        /// <returns>An integer value indicating the result of the operation.</returns>
        [DllImport( "ole32.dll", CharSet = CharSet.Unicode )]
        public static extern
        int MkParseDisplayName( IBindCtx pbc, string szUserName,
            ref int pchEaten, out IMoniker ppmk );

        /// <summary>
        /// Copies a block of memory from a source address to a destination address.
        /// </summary>
        /// <param name="dst">The destination address where the memory will be copied.</param>
        /// <param name="src">The source address from where the memory will be copied.</param>
        /// <param name="count">The number of bytes to be copied.</param>
        /// <returns>The destination address after the memory has been copied.</returns>
        /// <remarks>
        /// This method copies a block of memory from the source address to the destination address. It is important to ensure that the destination address has enough space to accommodate the copied memory, and that the count parameter specifies the correct number of bytes to be copied.
        /// </remarks>
        [DllImport( "ntdll.dll", CallingConvention = CallingConvention.Cdecl )]
        public static unsafe extern int memcpy(
            byte* dst,
            byte* src,
            int count );

        /// <summary>
        /// Displays a property frame for the specified objects.
        /// </summary>
        /// <param name="hwndOwner">A handle to the window that owns the property frame.</param>
        /// <param name="x">The horizontal position of the upper-left corner of the property frame relative to the screen.</param>
        /// <param name="y">The vertical position of the upper-left corner of the property frame relative to the screen.</param>
        /// <param name="caption">The caption for the property frame.</param>
        /// <param name="cObjects">The number of objects in the <paramref name="ppUnk"/> array.</param>
        /// <param name="ppUnk">An array of pointers to the objects for which the property frame is displayed.</param>
        /// <param name="cPages">The number of property pages in the <paramref name="lpPageClsID"/> array.</param>
        /// <param name="lpPageClsID">An array of CLSIDs for the property pages that are displayed in the frame.</param>
        /// <param name="lcid">The locale identifier for the property frame and property pages.</param>
        /// <param name="dwReserved">Reserved for future use; must be 0.</param>
        /// <param name="lpvReserved">Reserved for future use; must be IntPtr.Zero.</param>
        /// <returns>Returns zero if successful; otherwise, returns a non-zero HRESULT value.</returns>
        [DllImport( "oleaut32.dll" )]
        public static extern int OleCreatePropertyFrame(
            IntPtr hwndOwner,
            int x,
            int y,
            [MarshalAs( UnmanagedType.LPWStr )] string caption,
            int cObjects,
            [MarshalAs( UnmanagedType.Interface, ArraySubType = UnmanagedType.IUnknown )] 
            ref object ppUnk,
            int cPages,
            IntPtr lpPageClsID,
            int lcid,
            int dwReserved,
            IntPtr lpvReserved );
    }
}
