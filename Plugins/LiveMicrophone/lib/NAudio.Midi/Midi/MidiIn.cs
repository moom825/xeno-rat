using System;
using System.Runtime.InteropServices;

namespace NAudio.Midi
{
    /// <summary>
    /// Represents a MIDI in device
    /// </summary>
    public class MidiIn : IDisposable 
    {
        private IntPtr hMidiIn = IntPtr.Zero;
        private bool disposed = false;
        private MidiInterop.MidiInCallback callback;

        //  Buffer headers created and marshalled to recive incoming Sysex mesages
        private IntPtr[] SysexBufferHeaders = new IntPtr[0];

        /// <summary>
        /// Called when a MIDI message is received
        /// </summary>
        public event EventHandler<MidiInMessageEventArgs> MessageReceived;

        /// <summary>
        /// An invalid MIDI message
        /// </summary>
        public event EventHandler<MidiInMessageEventArgs> ErrorReceived;

        /// <summary>
        /// Called when a Sysex MIDI message is received
        /// </summary>
        public event EventHandler<MidiInSysexMessageEventArgs> SysexMessageReceived;

        /// <summary>
        /// Gets the number of MIDI input devices available in the system
        /// </summary>
        public static int NumberOfDevices 
        {
            get 
            {
                return MidiInterop.midiInGetNumDevs();
            }
        }
        
        /// <summary>
        /// Opens a specified MIDI in device
        /// </summary>
        /// <param name="deviceNo">The device number</param>
        public MidiIn(int deviceNo) 
        {
            this.callback = new MidiInterop.MidiInCallback(Callback);
            MmException.Try(MidiInterop.midiInOpen(out hMidiIn, (IntPtr) deviceNo,this.callback,IntPtr.Zero,MidiInterop.CALLBACK_FUNCTION),"midiInOpen");
        }

        /// <summary>
        /// Closes the current object by calling the Dispose method.
        /// </summary>
        /// <remarks>
        /// This method calls the Dispose method to release resources used by the current object.
        /// </remarks>
        public void Close() 
        {
            Dispose();
        }

        /// <summary>
        /// Releases the unmanaged resources used by the MidiIn class and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        /// <remarks>
        /// This method releases the unmanaged resources used by the MidiIn class and optionally releases the managed resources.
        /// If disposing is true, this method disposes of all managed and unmanaged resources.
        /// If disposing is false, this method releases only the unmanaged resources.
        /// If the SysexBufferHeaders array has elements, it resets the MIDI input device, frees up all created and allocated buffers for incoming Sysex messages, and closes the MIDI input device handle.
        /// </remarks>
        public void Dispose() 
        {
            GC.KeepAlive(callback);
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Starts the MIDI input device.
        /// </summary>
        /// <exception cref="MmException">Thrown when an error occurs while starting the MIDI input device.</exception>
        public void Start()
        {
            MmException.Try(MidiInterop.midiInStart(hMidiIn), "midiInStart");
        }

        /// <summary>
        /// Stops the MIDI input device.
        /// </summary>
        /// <exception cref="MmException">Thrown when an error occurs while stopping the MIDI input device.</exception>
        public void Stop()
        {
            MmException.Try(MidiInterop.midiInStop(hMidiIn), "midiInStop");
        }

        /// <summary>
        /// Resets the MIDI input device to its default state.
        /// </summary>
        /// <exception cref="MmException">Thrown when an error occurs while resetting the MIDI input device.</exception>
        public void Reset()
        {
            MmException.Try(MidiInterop.midiInReset(hMidiIn), "midiInReset");
        }

        /// <summary>
        /// Creates sysex buffers for MIDI input.
        /// </summary>
        /// <param name="bufferSize">The size of each buffer.</param>
        /// <param name="numberOfBuffers">The number of buffers to create.</param>
        /// <remarks>
        /// This method creates sysex buffers for MIDI input. It allocates memory for each buffer, prepares the header, and adds the buffer to the MIDI input device.
        /// </remarks>
        /// <exception cref="MmException">Thrown when there is an error in preparing or adding the buffer to the MIDI input device.</exception>
        public void CreateSysexBuffers(int bufferSize, int numberOfBuffers)
        {
            SysexBufferHeaders = new IntPtr[numberOfBuffers];

            var hdrSize = Marshal.SizeOf(typeof(MidiInterop.MIDIHDR));
            for (var i = 0; i < numberOfBuffers; i++)
            {
                var hdr = new MidiInterop.MIDIHDR();

                hdr.dwBufferLength = bufferSize;
                hdr.dwBytesRecorded = 0;
                hdr.lpData = Marshal.AllocHGlobal(bufferSize);
                hdr.dwFlags = 0;

                var lpHeader = Marshal.AllocHGlobal(hdrSize);
                Marshal.StructureToPtr(hdr, lpHeader, false);

                MmException.Try(MidiInterop.midiInPrepareHeader(hMidiIn, lpHeader, Marshal.SizeOf(typeof(MidiInterop.MIDIHDR))), "midiInPrepareHeader");
                MmException.Try(MidiInterop.midiInAddBuffer(hMidiIn, lpHeader, Marshal.SizeOf(typeof(MidiInterop.MIDIHDR))), "midiInAddBuffer");
                SysexBufferHeaders[i] = lpHeader;
            }
        }

        /// <summary>
        /// Callback function for handling MIDI input messages.
        /// </summary>
        /// <param name="midiInHandle">Handle to the MIDI input device.</param>
        /// <param name="message">The type of MIDI input message received.</param>
        /// <param name="userData">User-defined data passed to the callback function.</param>
        /// <param name="messageParameter1">The first parameter associated with the MIDI input message.</param>
        /// <param name="messageParameter2">The second parameter associated with the MIDI input message.</param>
        /// <remarks>
        /// This callback function is used to handle different types of MIDI input messages received from the MIDI input device.
        /// It switches on the type of message received and performs specific actions based on the message type.
        /// If the message type is Data, it raises the MessageReceived event with the packed MIDI message and milliseconds since MidiInStart as parameters.
        /// If the message type is Error, it raises the ErrorReceived event with the invalid MIDI message as a parameter.
        /// If the message type is LongData, it processes the pointer to MIDI header and milliseconds since MidiInStart to raise the SysexMessageReceived event with the sysex message bytes and milliseconds as parameters.
        /// </remarks>
        private void Callback(IntPtr midiInHandle, MidiInterop.MidiInMessage message, IntPtr userData, IntPtr messageParameter1, IntPtr messageParameter2)
        {
            switch(message)
            {
                case MidiInterop.MidiInMessage.Open:
                    // message Parameter 1 & 2 are not used
                    break;
                case MidiInterop.MidiInMessage.Data:
                    // parameter 1 is packed MIDI message
                    // parameter 2 is milliseconds since MidiInStart
                    if (MessageReceived != null)
                    {
                        MessageReceived(this, new MidiInMessageEventArgs(messageParameter1.ToInt32(), messageParameter2.ToInt32()));
                    }
                    break;
                case MidiInterop.MidiInMessage.Error:
                    // parameter 1 is invalid MIDI message
                    if (ErrorReceived != null)
                    {
                        ErrorReceived(this, new MidiInMessageEventArgs(messageParameter1.ToInt32(), messageParameter2.ToInt32()));
                    } 
                    break;
                case MidiInterop.MidiInMessage.Close:
                    // message Parameter 1 & 2 are not used
                    break;
                case MidiInterop.MidiInMessage.LongData:
                    // parameter 1 is pointer to MIDI header
                    // parameter 2 is milliseconds since MidiInStart
                    if (SysexMessageReceived != null)
                    {
                        MidiInterop.MIDIHDR hdr = (MidiInterop.MIDIHDR)Marshal.PtrToStructure(messageParameter1, typeof(MidiInterop.MIDIHDR));

                        //  Copy the bytes received into an array so that the buffer is immediately available for re-use
                        var sysexBytes = new byte[hdr.dwBytesRecorded];
                        Marshal.Copy(hdr.lpData, sysexBytes, 0, hdr.dwBytesRecorded);

                        SysexMessageReceived(this, new MidiInSysexMessageEventArgs(sysexBytes, messageParameter2.ToInt32()));
                        //  Re-use the buffer - but not if we have no event handler registered as we are closing
                        MidiInterop.midiInAddBuffer(hMidiIn, messageParameter1, Marshal.SizeOf(typeof(MidiInterop.MIDIHDR)));
                    }
                    break;
                case MidiInterop.MidiInMessage.LongError:
                    // parameter 1 is pointer to MIDI header
                    // parameter 2 is milliseconds since MidiInStart
                    break;
                case MidiInterop.MidiInMessage.MoreData:
                    // parameter 1 is packed MIDI message
                    // parameter 2 is milliseconds since MidiInStart
                    break;
            }
        }

        /// <summary>
        /// Retrieves the capabilities of the specified MIDI input device.
        /// </summary>
        /// <param name="midiInDeviceNumber">The number of the MIDI input device for which to retrieve the capabilities.</param>
        /// <returns>The capabilities of the specified MIDI input device.</returns>
        /// <exception cref="MmException">Thrown when an error occurs while retrieving the device capabilities.</exception>
        public static MidiInCapabilities DeviceInfo(int midiInDeviceNumber)
        {
            MidiInCapabilities caps = new MidiInCapabilities();
            int structSize = Marshal.SizeOf(caps);
            MmException.Try(MidiInterop.midiInGetDevCaps((IntPtr)midiInDeviceNumber,out caps,structSize),"midiInGetDevCaps");
            return caps;
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

                if (SysexBufferHeaders.Length > 0)
                {
                    //  Reset in order to release any Sysex buffers
                    //  We can't Unprepare and free them until they are flushed out. Neither can we close the handle.
                    MmException.Try(MidiInterop.midiInReset(hMidiIn), "midiInReset");

                    //  Free up all created and allocated buffers for incoming Sysex messages
                    foreach (var lpHeader in SysexBufferHeaders)
                    {
                        MidiInterop.MIDIHDR hdr = (MidiInterop.MIDIHDR)Marshal.PtrToStructure(lpHeader, typeof(MidiInterop.MIDIHDR));
                        MmException.Try(MidiInterop.midiInUnprepareHeader(hMidiIn, lpHeader, Marshal.SizeOf(typeof(MidiInterop.MIDIHDR))), "midiInPrepareHeader");
                        Marshal.FreeHGlobal(hdr.lpData);
                        Marshal.FreeHGlobal(lpHeader);
                    }

                    //  Defensive protection against double disposal
                    SysexBufferHeaders = new IntPtr[0];
                }
                MidiInterop.midiInClose(hMidiIn);
            }
            disposed = true;
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        ~MidiIn()
        {
            System.Diagnostics.Debug.Assert(false,"MIDI In was not finalised");
            Dispose(false);
        }
    }
}