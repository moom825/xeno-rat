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
// updated for use in NAudio
using System;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi
{

    /// <summary>
    /// MM Device Enumerator
    /// </summary>
    public class MMDeviceEnumerator : IDisposable
    {
        private IMMDeviceEnumerator realEnumerator;

        /// <summary>
        /// Creates a new MM Device Enumerator
        /// </summary>
        public MMDeviceEnumerator()
        {
            if (System.Environment.OSVersion.Version.Major < 6)
            {
                throw new NotSupportedException("This functionality is only supported on Windows Vista or newer.");
            }
            realEnumerator = new MMDeviceEnumeratorComObject() as IMMDeviceEnumerator;
        }

        /// <summary>
        /// Enumerates all audio endpoint devices that meet the specified criteria.
        /// </summary>
        /// <param name="dataFlow">The data-flow direction for the endpoint devices.</param>
        /// <param name="dwStateMask">The state mask for the endpoint devices.</param>
        /// <returns>A collection of audio endpoint devices that meet the specified criteria.</returns>
        /// <exception cref="MarshalDirectiveException">Thrown when an HRESULT error code is returned from the underlying COM method call.</exception>
        public MMDeviceCollection EnumerateAudioEndPoints(DataFlow dataFlow, DeviceState dwStateMask)
        {
            Marshal.ThrowExceptionForHR(realEnumerator.EnumAudioEndpoints(dataFlow, dwStateMask, out var result));
            return new MMDeviceCollection(result);
        }

        /// <summary>
        /// Retrieves the default audio endpoint for the specified data flow and role.
        /// </summary>
        /// <param name="dataFlow">The data flow direction.</param>
        /// <param name="role">The role of the device.</param>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when a call to a COM component results in an error.</exception>
        /// <returns>The default audio endpoint for the specified data flow and role.</returns>
        public MMDevice GetDefaultAudioEndpoint(DataFlow dataFlow, Role role)
        {
            Marshal.ThrowExceptionForHR(((IMMDeviceEnumerator)realEnumerator).GetDefaultAudioEndpoint(dataFlow, role, out var device));
            return new MMDevice(device);
        }

        /// <summary>
        /// Checks if the default audio endpoint exists for the specified data flow and role.
        /// </summary>
        /// <param name="dataFlow">The data flow direction for the endpoint.</param>
        /// <param name="role">The role of the endpoint.</param>
        /// <returns>True if the default audio endpoint exists for the specified data flow and role; otherwise, false.</returns>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when a call to the underlying COM component fails.</exception>
        public bool HasDefaultAudioEndpoint(DataFlow dataFlow, Role role)
        {
            const int E_NOTFOUND = unchecked((int)0x80070490);
            int hresult = ((IMMDeviceEnumerator)realEnumerator).GetDefaultAudioEndpoint(dataFlow, role, out var device);
            if (hresult == 0x0)
            {
                Marshal.ReleaseComObject(device);
                return true;
            }
            if (hresult == E_NOTFOUND)
            {
                return false;
            }
            Marshal.ThrowExceptionForHR(hresult);
            return false;
        }

        /// <summary>
        /// Retrieves the audio endpoint device that is identified by the specified endpoint ID string.
        /// </summary>
        /// <param name="id">The endpoint ID string that identifies the audio endpoint device.</param>
        /// <returns>The <see cref="MMDevice"/> object representing the audio endpoint device.</returns>
        /// <exception cref="MarshalDirectiveException">Thrown when a call to a COM component method results in an HRESULT that indicates a failure.</exception>
        public MMDevice GetDevice(string id)
        {
            Marshal.ThrowExceptionForHR(((IMMDeviceEnumerator)realEnumerator).GetDevice(id, out var device));
            return new MMDevice(device);
        }

        /// <summary>
        /// Registers a notification callback for changes to the endpoint devices and returns the result.
        /// </summary>
        /// <param name="client">The notification client to be registered.</param>
        /// <returns>The result of registering the endpoint notification callback.</returns>
        public int RegisterEndpointNotificationCallback([In] [MarshalAs(UnmanagedType.Interface)] IMMNotificationClient client)
        {
            return realEnumerator.RegisterEndpointNotificationCallback(client);
        }

        /// <summary>
        /// Unregisters the endpoint notification callback for the specified client.
        /// </summary>
        /// <param name="client">The IMMNotificationClient interface representing the client for which the endpoint notification callback needs to be unregistered.</param>
        /// <returns>The result of unregistering the endpoint notification callback for the specified client.</returns>
        public int UnregisterEndpointNotificationCallback([In] [MarshalAs(UnmanagedType.Interface)] IMMNotificationClient client)
        {
            return realEnumerator.UnregisterEndpointNotificationCallback(client);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        /// <remarks>
        /// This method releases the unmanaged resources held by the <see cref="realEnumerator"/> if <paramref name="disposing"/> is true.
        /// It is important to release unmanaged resources in a timely manner to avoid memory leaks and potential resource exhaustion.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Called to dispose/finalize contained objects.
        /// </summary>
        /// <param name="disposing">True if disposing, false if called from a finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (realEnumerator != null)
                {
                    // although GC would do this for us, we want it done now
                    Marshal.ReleaseComObject(realEnumerator);
                    realEnumerator = null;
                }
            }
        }
    }
}
