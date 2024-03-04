/*
  LICENSE
  -------
  Copyright (C) 2007 Ray Molenkamp

  This source code is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this source code or the software it produces.

  Permission is granted to anyone to use this source code for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this source code must not be misrepresented; you must not
     claim that you wrote the original source code.  If you use this source code
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original source code.
  3. This notice may not be removed or altered from any source distribution.
*/
// this version modified for NAudio from Ray Molenkamp's original
using System;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Property Store class, only supports reading properties at the moment.
    /// </summary>
    public class PropertyStore
    {
        private readonly IPropertyStore storeInterface;

        /// <summary>
        /// Property Count
        /// </summary>
        public int Count
        {
            get
            {
                Marshal.ThrowExceptionForHR(storeInterface.GetCount(out var result));
                return result;
            }
        }

        /// <summary>
        /// Gets property by index
        /// </summary>
        /// <param name="index">Property index</param>
        /// <returns>The property</returns>
        public PropertyStoreProperty this[int index]
        {
            get
            {
                PropertyKey key = Get(index);
                Marshal.ThrowExceptionForHR(storeInterface.GetValue(ref key, out var result));
                return new PropertyStoreProperty(key, result);
            }
        }

        /// <summary>
        /// Checks if the collection contains the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="PropertyKey"/> to be checked for existence in the collection.</param>
        /// <returns>True if the collection contains the specified <paramref name="key"/>, otherwise false.</returns>
        /// <remarks>
        /// This method iterates through the collection and compares each <see cref="PropertyKey"/> with the specified <paramref name="key"/>.
        /// If a matching <see cref="PropertyKey"/> is found, the method returns true; otherwise, it returns false.
        /// </remarks>
        public bool Contains(PropertyKey key)
        {
            for (int i = 0; i < Count; i++)
            {
                PropertyKey ikey = Get(i);
                if ((ikey.formatId == key.formatId) && (ikey.propertyId == key.propertyId))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Indexer by guid
        /// </summary>
        /// <param name="key">Property Key</param>
        /// <returns>Property or null if not found</returns>
        public PropertyStoreProperty this[PropertyKey key]
        {
            get
            {
                for (int i = 0; i < Count; i++)
                {
                    PropertyKey ikey = Get(i);
                    if ((ikey.formatId == key.formatId) && (ikey.propertyId == key.propertyId))
                    {
                        Marshal.ThrowExceptionForHR(storeInterface.GetValue(ref ikey, out var result));
                        return new PropertyStoreProperty(ikey, result);
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Retrieves the PropertyKey at the specified index.
        /// </summary>
        /// <param name="index">The index of the PropertyKey to retrieve.</param>
        /// <returns>The PropertyKey at the specified <paramref name="index"/>.</returns>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when a COM error occurs during the retrieval process.</exception>
        public PropertyKey Get(int index)
        {
            Marshal.ThrowExceptionForHR(storeInterface.GetAt(index, out var key));
            return key;
        }

        /// <summary>
        /// Retrieves the value at the specified index and returns it as a PropVariant.
        /// </summary>
        /// <param name="index">The index of the value to retrieve.</param>
        /// <returns>The value at the specified <paramref name="index"/> as a PropVariant.</returns>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when an error is encountered during the retrieval of the value.</exception>
        public PropVariant GetValue(int index)
        {
            PropertyKey key = Get(index);
            Marshal.ThrowExceptionForHR(storeInterface.GetValue(ref key, out var result));
            return result;
        }

        /// <summary>
        /// Sets the value of a specified property key in the property store.
        /// </summary>
        /// <param name="key">The property key to set the value for.</param>
        /// <param name="value">The value to be set for the specified property key.</param>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when an error occurs while setting the value in the property store.</exception>
        public void SetValue(PropertyKey key, PropVariant value)
        {
            Marshal.ThrowExceptionForHR(storeInterface.SetValue(ref key, ref value));
        }

        /// <summary>
        /// Commits the changes made to the data store.
        /// </summary>
        /// <exception cref="System.Runtime.InteropServices.COMException">
        /// Thrown when an error occurs during the commit operation.
        /// </exception>
        public void Commit()
        {
            Marshal.ThrowExceptionForHR(storeInterface.Commit());
        }

        /// <summary>
        /// Creates a new property store
        /// </summary>
        /// <param name="store">IPropertyStore COM interface</param>
        internal PropertyStore(IPropertyStore store)
        {
            storeInterface = store;
        }
    }
}

