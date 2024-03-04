using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NAudio.Dmo
{
    /// <summary>
    /// DirectX Media Object Enumerator
    /// </summary>
    public class DmoEnumerator
    {

        /// <summary>
        /// Retrieves the names of audio effects available and returns them as an enumerable collection of DmoDescriptors.
        /// </summary>
        /// <returns>An enumerable collection of DmoDescriptors representing the names of available audio effects.</returns>
        /// <remarks>
        /// This method retrieves the audio effects by querying the DMO category for audio effects using the DmoGuids.DMOCATEGORY_AUDIO_EFFECT constant.
        /// It returns the names of available audio effects as DmoDescriptors, which contain information about the effects.
        /// </remarks>
        public static IEnumerable<DmoDescriptor> GetAudioEffectNames()
        {
            return GetDmos(DmoGuids.DMOCATEGORY_AUDIO_EFFECT);
        }

        /// <summary>
        /// Retrieves the names of available audio encoders.
        /// </summary>
        /// <returns>An IEnumerable of DmoDescriptor objects representing the available audio encoders.</returns>
        /// <remarks>
        /// This method retrieves the names of available audio encoders by querying the system for DMOs (DirectX Media Objects) belonging to the category of audio encoders.
        /// It returns an IEnumerable of DmoDescriptor objects, each representing an available audio encoder.
        /// </remarks>
        public static IEnumerable<DmoDescriptor> GetAudioEncoderNames()
        {
            return GetDmos(DmoGuids.DMOCATEGORY_AUDIO_ENCODER);
        }

        /// <summary>
        /// Retrieves the names of available audio decoders.
        /// </summary>
        /// <returns>An IEnumerable of DmoDescriptor objects representing the available audio decoders.</returns>
        /// <remarks>
        /// This method retrieves the names of available audio decoders by calling the GetDmos method with the DMO category DmoGuids.DMOCATEGORY_AUDIO_DECODER.
        /// </remarks>
        public static IEnumerable<DmoDescriptor> GetAudioDecoderNames()
        {
            return GetDmos(DmoGuids.DMOCATEGORY_AUDIO_DECODER);
        }

        /// <summary>
        /// Retrieves a collection of DMO descriptors for a specified category.
        /// </summary>
        /// <param name="category">The GUID of the category for which DMO descriptors are to be retrieved.</param>
        /// <returns>A collection of DMO descriptors for the specified category.</returns>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when a COM error occurs while enumerating the DMOs.</exception>
        private static IEnumerable<DmoDescriptor> GetDmos(Guid category)
        {
            IEnumDmo enumDmo;
            var hresult = DmoInterop.DMOEnum(ref category, DmoEnumFlags.None, 0, null, 0, null, out enumDmo);
            Marshal.ThrowExceptionForHR(hresult);
            int itemsFetched;
            do
            {
                Guid guid;
                IntPtr namePointer;
                enumDmo.Next(1, out guid, out namePointer, out itemsFetched);

                if (itemsFetched == 1)
                {
                    string name = Marshal.PtrToStringUni(namePointer);
                    Marshal.FreeCoTaskMem(namePointer);
                    yield return new DmoDescriptor(name, guid);
                }
            } while (itemsFetched > 0);
        }
    }
}
