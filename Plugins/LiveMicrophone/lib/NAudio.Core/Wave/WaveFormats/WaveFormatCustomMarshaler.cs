using System;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Custom marshaller for WaveFormat structures
    /// </summary>
    public sealed class WaveFormatCustomMarshaler : ICustomMarshaler
    {
        private static WaveFormatCustomMarshaler marshaler = null;

        /// <summary>
        /// Returns an instance of the WaveFormatCustomMarshaler.
        /// </summary>
        /// <param name="cookie">The cookie parameter.</param>
        /// <returns>An instance of the WaveFormatCustomMarshaler.</returns>
        public static ICustomMarshaler GetInstance(string cookie)
        {
            if (marshaler == null)
            {
                marshaler = new WaveFormatCustomMarshaler();
            }
            return marshaler;
        }

        /// <summary>
        /// Cleans up managed data for the specified managed object.
        /// </summary>
        /// <param name="ManagedObj">The managed object for which the data needs to be cleaned up.</param>
        /// <remarks>
        /// This method performs cleanup operations for the managed data associated with the specified <paramref name="ManagedObj"/>.
        /// </remarks>
        public void CleanUpManagedData(object ManagedObj)
        {
            
        }

        /// <summary>
        /// Frees the memory allocated for the native data pointed to by the specified pointer.
        /// </summary>
        /// <param name="pNativeData">A pointer to the native data that needs to be deallocated.</param>
        /// <remarks>
        /// This method frees the memory allocated for the native data pointed to by the specified pointer using the <see cref="Marshal.FreeHGlobal(IntPtr)"/> method.
        /// </remarks>
        public void CleanUpNativeData(IntPtr pNativeData)
        {
            Marshal.FreeHGlobal(pNativeData);
        }

        /// <summary>
        /// Gets the size of the native data.
        /// </summary>
        /// <exception cref="NotImplementedException">Thrown when the method is not implemented.</exception>
        public int GetNativeDataSize()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Marshals a managed object to its native representation.
        /// </summary>
        /// <param name="ManagedObj">The managed object to be marshaled.</param>
        /// <returns>A pointer to the native representation of the managed object.</returns>
        public IntPtr MarshalManagedToNative(object ManagedObj)
        {
            return WaveFormat.MarshalToPtr((WaveFormat)ManagedObj);            
        }

        /// <summary>
        /// Marshals a native pointer to a managed WaveFormat object.
        /// </summary>
        /// <param name="pNativeData">A pointer to the native data to be marshaled.</param>
        /// <returns>A WaveFormat object representing the marshaled data.</returns>
        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            return WaveFormat.MarshalFromPtr(pNativeData);
        }
    }
}
