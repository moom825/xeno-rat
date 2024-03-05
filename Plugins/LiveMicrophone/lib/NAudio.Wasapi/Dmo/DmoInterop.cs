using System;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.Dmo
{
    static class DmoInterop
    {

        /// <summary>
        /// Enumerates the DMOs (DirectX Media Objects) that are registered in the system and returns an enumerator for the DMOs that match the specified criteria.
        /// </summary>
        /// <param name="guidCategory">The category of DMOs to enumerate.</param>
        /// <param name="flags">Flags that control the enumeration behavior.</param>
        /// <param name="inTypes">The number of input media types in the <paramref name="inTypesArray"/>.</param>
        /// <param name="inTypesArray">An array of input media types to match when enumerating DMOs.</param>
        /// <param name="outTypes">The number of output media types in the <paramref name="outTypesArray"/>.</param>
        /// <param name="outTypesArray">An array of output media types to match when enumerating DMOs.</param>
        /// <param name="enumDmo">When this method returns, contains an enumerator for the DMOs that match the specified criteria.</param>
        /// <exception cref="System.EntryPointNotFoundException">The specified function could not be found in the specified DLL.</exception>
        /// <exception cref="System.DllNotFoundException">The specified DLL was not found.</exception>
        [DllImport("msdmo.dll")]
        public static extern int DMOEnum(
            [In] ref Guid guidCategory,
            DmoEnumFlags flags,
            int inTypes,
            [In] DmoPartialMediaType[] inTypesArray,
            int outTypes,
            [In] DmoPartialMediaType[] outTypesArray,
            out IEnumDmo enumDmo);

        /// <summary>
        /// Frees the memory associated with the specified Direct Media Object (DMO) media type.
        /// </summary>
        /// <param name="mediaType">The DMO media type to be freed.</param>
        /// <returns>An integer value indicating the result of the operation.</returns>
        [DllImport("msdmo.dll")]
        public static extern int MoFreeMediaType(
            [In] ref DmoMediaType mediaType);

        /// <summary>
        /// Initializes a media type using the specified format block size.
        /// </summary>
        /// <param name="mediaType">The media type to be initialized.</param>
        /// <param name="formatBlockBytes">The size of the format block in bytes.</param>
        /// <returns>Returns an integer indicating the result of the initialization process.</returns>
        /// <exception cref="System.EntryPointNotFoundException">Thrown when the entry point for the specified function in the specified DLL is not found.</exception>
        [DllImport("msdmo.dll")]
        public static extern int MoInitMediaType(
            [In,Out] ref DmoMediaType mediaType, int formatBlockBytes);

        /// <summary>
        /// Retrieves the name of a DMO (DirectX Media Object) identified by the specified CLSID.
        /// </summary>
        /// <param name="clsidDMO">The CLSID (Class Identifier) of the DMO for which to retrieve the name.</param>
        /// <param name="name">A preallocated <see cref="StringBuilder"/> to store the name of the DMO.</param>
        /// <returns>An integer representing the result of the operation.</returns>
        [DllImport("msdmo.dll")]
        public static extern int DMOGetName([In] ref Guid clsidDMO,
            // preallocate 80 characters
            [Out] StringBuilder name);
    }
}
