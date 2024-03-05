using System;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace NAudio.Midi 
{
    /// <summary>
    /// Represents a MIDI out device
    /// </summary>
    public class MidiOut : IDisposable 
    {
        private IntPtr hMidiOut = IntPtr.Zero;
        private bool disposed = false;
        MidiInterop.MidiOutCallback callback;

        /// <summary>
        /// Gets the number of MIDI devices available in the system
        /// </summary>
        public static int NumberOfDevices 
        {
            get 
            {
                return MidiInterop.midiOutGetNumDevs();
            }
        }

        /// <summary>
        /// Retrieves the capabilities of the specified MIDI output device.
        /// </summary>
        /// <param name="midiOutDeviceNumber">The number of the MIDI output device for which to retrieve the capabilities.</param>
        /// <returns>The capabilities of the specified MIDI output device as a <see cref="MidiOutCapabilities"/> object.</returns>
        /// <exception cref="MmException">Thrown when an error occurs while retrieving the device capabilities using the function <c>midiOutGetDevCaps</c>.</exception>
        public static MidiOutCapabilities DeviceInfo(int midiOutDeviceNumber)
        {
            MidiOutCapabilities caps = new MidiOutCapabilities();
            int structSize = Marshal.SizeOf(caps);
            MmException.Try(MidiInterop.midiOutGetDevCaps((IntPtr)midiOutDeviceNumber, out caps, structSize), "midiOutGetDevCaps");
            return caps;
        }

        
        /// <summary>
        /// Opens a specified MIDI out device
        /// </summary>
        /// <param name="deviceNo">The device number</param>
        public MidiOut(int deviceNo) 
        {
            this.callback = new MidiInterop.MidiOutCallback(Callback);
            MmException.Try(MidiInterop.midiOutOpen(out hMidiOut, (IntPtr)deviceNo, callback, IntPtr.Zero, MidiInterop.CALLBACK_FUNCTION), "midiOutOpen");
        }

        /// <summary>
        /// Closes the current resource.
        /// </summary>
        /// <remarks>
        /// This method calls the Dispose method to release resources used by the current instance.
        /// </remarks>
        public void Close() 
        {
            Dispose();
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:YourClassName"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        /// <remarks>
        /// This method disposes of the unmanaged resources used by the <see cref="T:YourClassName"/>.
        /// If <paramref name="disposing"/> is true, it also disposes of the managed resources.
        /// This method should be called when the object is no longer needed, to release the resources it holds.
        /// </remarks>
        public void Dispose() 
        {
            GC.KeepAlive(callback);
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets or sets the volume for this MIDI out device
        /// </summary>
        public int Volume 
        {
            // TODO: Volume can be accessed by device ID
            get 
            {
                int volume = 0;
                MmException.Try(MidiInterop.midiOutGetVolume(hMidiOut,ref volume),"midiOutGetVolume");
                return volume;
            }
            set 
            {
                MmException.Try(MidiInterop.midiOutSetVolume(hMidiOut,value),"midiOutSetVolume");
            }
        }

        /// <summary>
        /// Resets the MIDI output device.
        /// </summary>
        /// <exception cref="MmException">Thrown when an error occurs while resetting the MIDI output device.</exception>
        /// <remarks>
        /// This method resets the MIDI output device represented by the handle <paramref name="hMidiOut"/>.
        /// </remarks>
        public void Reset() 
        {
            MmException.Try(MidiInterop.midiOutReset(hMidiOut),"midiOutReset");
        }

        /// <summary>
        /// Sends a driver message to the MIDI output device.
        /// </summary>
        /// <param name="message">The message to be sent to the MIDI output device.</param>
        /// <param name="param1">The first parameter for the driver message.</param>
        /// <param name="param2">The second parameter for the driver message.</param>
        /// <exception cref="MmException">Thrown when an error occurs while sending the driver message.</exception>
        public void SendDriverMessage(int message, int param1, int param2) 
        {
            MmException.Try(MidiInterop.midiOutMessage(hMidiOut,message,(IntPtr)param1,(IntPtr)param2),"midiOutMessage");
        }

        /// <summary>
        /// Sends a MIDI message using the specified handle.
        /// </summary>
        /// <param name="message">The MIDI message to be sent.</param>
        /// <exception cref="MmException">Thrown when an error occurs while sending the MIDI message.</exception>
        public void Send(int message) 
        {
            MmException.Try(MidiInterop.midiOutShortMsg(hMidiOut,message),"midiOutShortMsg");
        }
        
        /// <summary>
        /// Closes the MIDI out device
        /// </summary>
        /// <param name="disposing">True if called from Dispose</param>
        protected virtual void Dispose(bool disposing) 
        {
            if(!this.disposed) 
            {
                //if(disposing) Components.Dispose();
                MidiInterop.midiOutClose(hMidiOut);
            }
            disposed = true;
        }

        /// <summary>
        /// Callback function for MIDI input messages.
        /// </summary>
        /// <param name="midiInHandle">Handle to the MIDI input device.</param>
        /// <param name="message">The MIDI output message.</param>
        /// <param name="userData">User-defined data.</param>
        /// <param name="messageParameter1">First message parameter.</param>
        /// <param name="messageParameter2">Second message parameter.</param>
        private void Callback(IntPtr midiInHandle, MidiInterop.MidiOutMessage message, IntPtr userData, IntPtr messageParameter1, IntPtr messageParameter2)
        {
        }

        /// <summary>
        /// Sends the specified byte buffer to the MIDI output device.
        /// </summary>
        /// <param name="byteBuffer">The byte array to be sent to the MIDI output device.</param>
        /// <remarks>
        /// This method prepares the header for the byte buffer, copies the byte buffer to unmanaged memory, and sends it to the MIDI output device.
        /// If an error occurs during the sending process, the method unprepares the header and frees the allocated memory.
        /// </remarks>
        public void SendBuffer(byte[] byteBuffer)
        {
            var header = new MidiInterop.MIDIHDR();
            header.lpData = Marshal.AllocHGlobal(byteBuffer.Length);
            Marshal.Copy(byteBuffer, 0, header.lpData, byteBuffer.Length);

            header.dwBufferLength = byteBuffer.Length;
            header.dwBytesRecorded = byteBuffer.Length;
            int size = Marshal.SizeOf(header);
            MidiInterop.midiOutPrepareHeader(this.hMidiOut, ref header, size);
            var errcode = MidiInterop.midiOutLongMsg(this.hMidiOut, ref header, size);
            if (errcode != MmResult.NoError)
            {
                MidiInterop.midiOutUnprepareHeader(this.hMidiOut, ref header, size);
            }
            Marshal.FreeHGlobal(header.lpData);
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        ~MidiOut()
        {
            System.Diagnostics.Debug.Assert(false);
            Dispose(false);
        }
    }
}
