using System;
using System.Runtime.InteropServices;

namespace NAudio.Midi
{
    internal class MidiInterop
    {

        public enum MidiInMessage
        {
            /// <summary>
            /// MIM_OPEN
            /// </summary>
            Open = 0x3C1,
            /// <summary>
            /// MIM_CLOSE
            /// </summary>
            Close = 0x3C2,
            /// <summary>
            /// MIM_DATA
            /// </summary>
            Data = 0x3C3,
            /// <summary>
            /// MIM_LONGDATA
            /// </summary>
            LongData = 0x3C4,
            /// <summary>
            /// MIM_ERROR
            /// </summary>
            Error = 0x3C5,
            /// <summary>
            /// MIM_LONGERROR
            /// </summary>
            LongError = 0x3C6,
            /// <summary>
            /// MIM_MOREDATA
            /// </summary>
            MoreData = 0x3CC,
        }



        public enum MidiOutMessage
        {
            /// <summary>
            /// MOM_OPEN
            /// </summary>
            Open = 0x3C7,
            /// <summary>
            /// MOM_CLOSE
            /// </summary>
            Close = 0x3C8,
            /// <summary>
            /// MOM_DONE
            /// </summary>
            Done = 0x3C9
        }

        // http://msdn.microsoft.com/en-us/library/dd798460%28VS.85%29.aspx
        public delegate void MidiInCallback(IntPtr midiInHandle, MidiInMessage message, IntPtr userData, IntPtr messageParameter1, IntPtr messageParameter2);

        // http://msdn.microsoft.com/en-us/library/dd798478%28VS.85%29.aspx
        public delegate void MidiOutCallback(IntPtr midiInHandle, MidiOutMessage message, IntPtr userData, IntPtr messageParameter1, IntPtr messageParameter2);

        /// <summary>
        /// Establishes a MIDI connection between the specified MIDI input and output devices.
        /// </summary>
        /// <param name="hMidiIn">Handle to the MIDI input device.</param>
        /// <param name="hMidiOut">Handle to the MIDI output device.</param>
        /// <param name="pReserved">Reserved; must be set to IntPtr.Zero.</param>
        /// <returns>Returns an MmResult value indicating the success or failure of the MIDI connection.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiConnect(IntPtr hMidiIn, IntPtr hMidiOut, IntPtr pReserved);

        /// <summary>
        /// Disconnects a MIDI input device from a MIDI output device.
        /// </summary>
        /// <param name="hMidiIn">Handle to the MIDI input device.</param>
        /// <param name="hMidiOut">Handle to the MIDI output device.</param>
        /// <param name="pReserved">Reserved; must be set to IntPtr.Zero.</param>
        /// <returns>An MmResult value indicating the result of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiDisconnect(IntPtr hMidiIn, IntPtr hMidiOut, IntPtr pReserved);

        /// <summary>
        /// Adds a buffer to the specified MIDI input device.
        /// </summary>
        /// <param name="hMidiIn">Handle to the MIDI input device.</param>
        /// <param name="lpMidiInHdr">Pointer to a MIDIHDR structure that identifies the buffer to be added.</param>
        /// <param name="uSize">Size, in bytes, of the MIDIHDR structure.</param>
        /// <returns>Returns an MmResult value indicating the result of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiInAddBuffer(IntPtr hMidiIn, IntPtr lpMidiInHdr, int uSize);

        /// <summary>
        /// Closes the specified MIDI input device.
        /// </summary>
        /// <param name="hMidiIn">Handle to the MIDI input device.</param>
        /// <returns>Returns an MmResult value indicating the result of the operation.</returns>
        /// <remarks>
        /// This method closes the MIDI input device identified by the handle <paramref name="hMidiIn"/>.
        /// </remarks>
        [DllImport("winmm.dll")]
        public static extern MmResult midiInClose(IntPtr hMidiIn);

        /// <summary>
        /// Retrieves the capabilities of a specified MIDI input device.
        /// </summary>
        /// <param name="deviceId">The handle to the MIDI input device.</param>
        /// <param name="capabilities">When this method returns, contains the capabilities of the MIDI input device.</param>
        /// <param name="size">The size, in bytes, of the MIDIINCAPS structure.</param>
        /// <returns>An MmResult value indicating the result of the operation.</returns>
        /// <remarks>
        /// This method retrieves the capabilities of the specified MIDI input device and stores the result in the <paramref name="capabilities"/> parameter.
        /// The <paramref name="size"/> parameter specifies the size of the MIDIINCAPS structure.
        /// </remarks>
        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        public static extern MmResult midiInGetDevCaps(IntPtr deviceId, out MidiInCapabilities capabilities, int size);

        /// <summary>
        /// Retrieves the error message for a specified MIDI input device error code.
        /// </summary>
        /// <param name="err">The MIDI input device error code.</param>
        /// <param name="lpText">The buffer to receive the error message.</param>
        /// <param name="uSize">The size of the buffer in characters.</param>
        /// <returns>An MmResult value indicating the success or failure of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiInGetErrorText(int err, string lpText, int uSize);

        /// <summary>
        /// Retrieves the device identifier for the MIDI input device associated with the specified MIDI input handle.
        /// </summary>
        /// <param name="hMidiIn">The handle to the MIDI input device.</param>
        /// <param name="lpuDeviceId">When this method returns, contains the device identifier for the MIDI input device.</param>
        /// <returns>An MmResult value that specifies the result of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiInGetID(IntPtr hMidiIn, out int lpuDeviceId);

        /// <summary>
        /// Retrieves the number of MIDI input devices present in the system.
        /// </summary>
        /// <returns>The number of MIDI input devices present in the system.</returns>
        [DllImport("winmm.dll")]
        public static extern int midiInGetNumDevs();

        /// <summary>
        /// Sends a message to the specified MIDI input device.
        /// </summary>
        /// <param name="hMidiIn">Handle to the MIDI input device.</param>
        /// <param name="msg">The message to send.</param>
        /// <param name="dw1">Message parameter 1.</param>
        /// <param name="dw2">Message parameter 2.</param>
        /// <returns>The result of the message sent to the MIDI input device.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiInMessage(IntPtr hMidiIn, int msg, IntPtr dw1, IntPtr dw2);

        /// <summary>
        /// Opens a MIDI input device for receiving MIDI messages.
        /// </summary>
        /// <param name="hMidiIn">Receives a handle to the opened MIDI input device.</param>
        /// <param name="uDeviceID">The device ID of the MIDI input device to open.</param>
        /// <param name="callback">The callback function that will receive the MIDI messages.</param>
        /// <param name="dwInstance">User instance data passed to the callback function.</param>
        /// <param name="dwFlags">Flags for opening the MIDI input device.</param>
        /// <returns>An MmResult value indicating the success or failure of the operation.</returns>
        /// <remarks>
        /// This method opens a MIDI input device for receiving MIDI messages. The <paramref name="hMidiIn"/> parameter receives a handle to the opened MIDI input device.
        /// The <paramref name="uDeviceID"/> parameter specifies the device ID of the MIDI input device to open.
        /// The <paramref name="callback"/> parameter is the callback function that will receive the MIDI messages.
        /// The <paramref name="dwInstance"/> parameter is user instance data passed to the callback function.
        /// The <paramref name="dwFlags"/> parameter specifies flags for opening the MIDI input device.
        /// </remarks>
        [DllImport("winmm.dll", EntryPoint = "midiInOpen")]
        public static extern MmResult midiInOpen(out IntPtr hMidiIn, IntPtr uDeviceID, MidiInCallback callback, IntPtr dwInstance, int dwFlags);

        /// <summary>
        /// Opens a MIDI input device for system-exclusive (SysEx) message input.
        /// </summary>
        /// <param name="hMidiIn">The handle for the opened MIDI input device.</param>
        /// <param name="uDeviceID">The device ID of the MIDI input device to be opened.</param>
        /// <param name="callbackWindowHandle">The handle to the window that will receive the callback messages for the MIDI input device.</param>
        /// <param name="dwInstance">A pointer to the application-defined callback function that will process the messages for the MIDI input device.</param>
        /// <param name="dwFlags">Flags for opening the MIDI input device.</param>
        /// <returns>An MmResult value indicating the success or failure of opening the MIDI input device.</returns>
        [DllImport("winmm.dll", EntryPoint = "midiInOpen")]
        public static extern MmResult midiInOpenWindow(out IntPtr hMidiIn, IntPtr uDeviceID, IntPtr callbackWindowHandle, IntPtr dwInstance, int dwFlags);

        /// <summary>
        /// Prepares a MIDI input header for input.
        /// </summary>
        /// <param name="hMidiIn">Handle to the MIDI input device.</param>
        /// <param name="lpMidiInHdr">Pointer to a MIDIHDR structure that identifies the buffer to be prepared.</param>
        /// <param name="uSize">Size, in bytes, of the MIDIHDR structure.</param>
        /// <returns>An MmResult value indicating the result of the operation.</returns>
        /// <exception cref="MmException">Thrown if an error occurs while preparing the MIDI input header.</exception>
        [DllImport("winmm.dll")]
        public static extern MmResult midiInPrepareHeader(IntPtr hMidiIn, IntPtr lpMidiInHdr, int uSize);

        /// <summary>
        /// Resets the specified MIDI input device to a known state.
        /// </summary>
        /// <param name="hMidiIn">Handle to the MIDI input device.</param>
        /// <returns>An MmResult value indicating the result of the reset operation.</returns>
        /// <remarks>
        /// This method resets the specified MIDI input device to a known state. After calling this method, the MIDI input device is ready to receive and process new MIDI messages.
        /// </remarks>
        [DllImport("winmm.dll")]
        public static extern MmResult midiInReset(IntPtr hMidiIn);

        /// <summary>
        /// Starts MIDI input on the specified MIDI input device.
        /// </summary>
        /// <param name="hMidiIn">Handle to the MIDI input device.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiInStart(IntPtr hMidiIn);

        /// <summary>
        /// Stops MIDI input on the specified MIDI input device.
        /// </summary>
        /// <param name="hMidiIn">Handle to the MIDI input device.</param>
        /// <returns>An MmResult value indicating the result of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiInStop(IntPtr hMidiIn);

        /// <summary>
        /// Frees the resources used by a specified MIDI input buffer.
        /// </summary>
        /// <param name="hMidiIn">Handle to the MIDI input device.</param>
        /// <param name="lpMidiInHdr">Pointer to a MIDIHDR structure that identifies the buffer to be cleaned up.</param>
        /// <param name="uSize">Size, in bytes, of the MIDIHDR structure.</param>
        /// <returns>An MmResult value indicating the result of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiInUnprepareHeader(IntPtr hMidiIn, IntPtr lpMidiInHdr, int uSize);

        /// <summary>
        /// Caches the drum patches for a MIDI output device.
        /// </summary>
        /// <param name="hMidiOut">Handle to the MIDI output device.</param>
        /// <param name="uPatch">Specifies the patch number to be cached.</param>
        /// <param name="lpKeyArray">Pointer to an array of key numbers that define the percussion notes.</param>
        /// <param name="uFlags">Reserved; must be set to zero.</param>
        /// <returns>Returns an MmResult value indicating the result of the operation.</returns>
        /// <remarks>
        /// This function caches the drum patches for a MIDI output device. It allows applications to cache percussion key maps for a device.
        /// The lpKeyArray parameter points to an array of key numbers that define the percussion notes. The uPatch parameter specifies the patch number to be cached.
        /// The uFlags parameter is reserved and must be set to zero.
        /// </remarks>
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutCacheDrumPatches(IntPtr hMidiOut, int uPatch, IntPtr lpKeyArray, int uFlags);

        /// <summary>
        /// Caches patches in a specified bank for a MIDI output device.
        /// </summary>
        /// <param name="hMidiOut">Handle to the MIDI output device.</param>
        /// <param name="uBank">Specifies the bank to be cached.</param>
        /// <param name="lpPatchArray">Pointer to an array of DWORD values that specifies the patches to be cached.</param>
        /// <param name="uFlags">Specifies caching options.</param>
        /// <returns>An MmResult value indicating the result of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutCachePatches(IntPtr hMidiOut, int uBank, IntPtr lpPatchArray, int uFlags);

        /// <summary>
        /// Closes the specified MIDI output device.
        /// </summary>
        /// <param name="hMidiOut">Handle to the MIDI output device. This handle is returned by the midiOutOpen function.</param>
        /// <returns>An MmResult value that specifies the result of the operation.</returns>
        /// <remarks>
        /// This function closes the specified MIDI output device. All pending system-exclusive output buffers are marked as done and returned to the application.
        /// If there are any pending system-exclusive output buffers, the device driver will send a system-exclusive message to the MIDI output port with the MEVT_F_CALLBACK flag set in the dwEvent member of the MIDI_EVENT structure.
        /// </remarks>
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutClose(IntPtr hMidiOut);

        /// <summary>
        /// Retrieves the capabilities of a specified MIDI output device.
        /// </summary>
        /// <param name="deviceNumber">The device number of the MIDI output device.</param>
        /// <param name="caps">When this method returns, contains the capabilities of the MIDI output device specified by <paramref name="deviceNumber"/>.</param>
        /// <param name="uSize">The size, in bytes, of the <paramref name="caps"/> structure.</param>
        /// <returns>An MmResult value that specifies the result of the operation.</returns>
        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        public static extern MmResult midiOutGetDevCaps(IntPtr deviceNumber, out MidiOutCapabilities caps, int uSize);

        /// <summary>
        /// Retrieves the error message for a given MIDI output error code.
        /// </summary>
        /// <param name="err">The error code to retrieve the message for.</param>
        /// <param name="lpText">The buffer to receive the error message.</param>
        /// <param name="uSize">The size of the buffer in characters.</param>
        /// <returns>An MmResult value indicating the success or failure of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutGetErrorText(IntPtr err, string lpText, int uSize);

        /// <summary>
        /// Retrieves the device identifier for the MIDI output device associated with the specified MIDI output handle.
        /// </summary>
        /// <param name="hMidiOut">The handle to the MIDI output device.</param>
        /// <param name="lpuDeviceID">When this method returns, contains the device identifier for the MIDI output device.</param>
        /// <returns>An MmResult value indicating the success or failure of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutGetID(IntPtr hMidiOut, out int lpuDeviceID);

        /// <summary>
        /// Retrieves the number of MIDI output devices present in the system.
        /// </summary>
        /// <returns>The number of MIDI output devices present in the system.</returns>
        [DllImport("winmm.dll")]
        public static extern int midiOutGetNumDevs();

        /// <summary>
        /// Retrieves the volume level of the specified MIDI output device.
        /// </summary>
        /// <param name="uDeviceID">The identifier of the MIDI output device.</param>
        /// <param name="lpdwVolume">A reference to an integer that will receive the volume level.</param>
        /// <returns>An MmResult value indicating the success or failure of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutGetVolume(IntPtr uDeviceID, ref int lpdwVolume);

        /// <summary>
        /// Sends a system-exclusive MIDI message to the specified MIDI output device.
        /// </summary>
        /// <param name="hMidiOut">Handle to the MIDI output device.</param>
        /// <param name="lpMidiOutHdr">Reference to the MIDIHDR structure containing the system-exclusive MIDI message.</param>
        /// <param name="uSize">Size of the MIDIHDR structure.</param>
        /// <returns>An MmResult value indicating the success or failure of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutLongMsg(IntPtr hMidiOut, ref MIDIHDR lpMidiOutHdr, int uSize);

        /// <summary>
        /// Sends a message to the specified MIDI output device.
        /// </summary>
        /// <param name="hMidiOut">Handle to the MIDI output device.</param>
        /// <param name="msg">The message to send.</param>
        /// <param name="dw1">Additional parameter for the message.</param>
        /// <param name="dw2">Additional parameter for the message.</param>
        /// <returns>The result of the message sending operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutMessage(IntPtr hMidiOut, int msg, IntPtr dw1, IntPtr dw2);

        /// <summary>
        /// Opens a MIDI output device for sending MIDI data.
        /// </summary>
        /// <param name="lphMidiOut">Receives a handle identifying the opened MIDI output device.</param>
        /// <param name="uDeviceID">The device identifier of the MIDI output device to open.</param>
        /// <param name="dwCallback">A callback function for handling MIDI messages.</param>
        /// <param name="dwInstance">A user-defined instance value passed to the callback function.</param>
        /// <param name="dwFlags">Flags for opening the MIDI output device.</param>
        /// <returns>An MmResult value indicating the success or failure of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutOpen(out IntPtr lphMidiOut, IntPtr uDeviceID, MidiOutCallback dwCallback, IntPtr dwInstance, int dwFlags);

        /// <summary>
        /// Prepares a MIDI output header for playback.
        /// </summary>
        /// <param name="hMidiOut">Handle to the MIDI output device.</param>
        /// <param name="lpMidiOutHdr">Reference to the MIDI output header to be prepared.</param>
        /// <param name="uSize">The size, in bytes, of the MIDIHDR structure.</param>
        /// <returns>An MmResult value indicating the success or failure of the operation.</returns>
        /// <remarks>
        /// This function prepares a MIDI output header for playback on the specified MIDI output device.
        /// The lpMidiOutHdr parameter points to a MIDIHDR structure that specifies the MIDI data to be played.
        /// The uSize parameter specifies the size, in bytes, of the MIDIHDR structure.
        /// </remarks>
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutPrepareHeader(IntPtr hMidiOut, ref MIDIHDR lpMidiOutHdr, int uSize);

        /// <summary>
        /// Resets all MIDI output devices.
        /// </summary>
        /// <param name="hMidiOut">Handle to the MIDI output device.</param>
        /// <returns>Returns an MmResult value indicating the result of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutReset(IntPtr hMidiOut);

        /// <summary>
        /// Sets the volume level for a MIDI output device.
        /// </summary>
        /// <param name="hMidiOut">Handle to the MIDI output device.</param>
        /// <param name="dwVolume">The new volume level for the MIDI output device.</param>
        /// <returns>An MmResult value indicating the success or failure of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutSetVolume(IntPtr hMidiOut, int dwVolume);

        /// <summary>
        /// Sends a short MIDI message to the specified MIDI output device.
        /// </summary>
        /// <param name="hMidiOut">Handle to the MIDI output device.</param>
        /// <param name="dwMsg">The MIDI message to be sent.</param>
        /// <returns>The result of sending the MIDI message.</returns>
        /// <remarks>
        /// This method sends a short MIDI message to the specified MIDI output device using the winmm.dll library.
        /// The dwMsg parameter contains the MIDI message to be sent.
        /// The hMidiOut parameter is the handle to the MIDI output device.
        /// </remarks>
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutShortMsg(IntPtr hMidiOut, int dwMsg);

        /// <summary>
        /// Frees the specified MIDI output buffer.
        /// </summary>
        /// <param name="hMidiOut">Handle to the MIDI output device.</param>
        /// <param name="lpMidiOutHdr">Reference to the MIDIHDR structure that identifies the buffer to be freed.</param>
        /// <param name="uSize">Size, in bytes, of the MIDIHDR structure.</param>
        /// <returns>An MmResult value indicating the result of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiOutUnprepareHeader(IntPtr hMidiOut, ref MIDIHDR lpMidiOutHdr, int uSize);

        /// <summary>
        /// Closes the MIDI stream.
        /// </summary>
        /// <param name="hMidiStream">Handle to the MIDI stream to be closed.</param>
        /// <returns>An MmResult value indicating the result of the operation.</returns>
        /// <remarks>
        /// This method closes the MIDI stream identified by the handle <paramref name="hMidiStream"/>.
        /// </remarks>
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamClose(IntPtr hMidiStream);

        /// <summary>
        /// Opens a MIDI stream for output.
        /// </summary>
        /// <param name="hMidiStream">The handle to the opened MIDI stream.</param>
        /// <param name="puDeviceID">The device identifier.</param>
        /// <param name="cMidi">The number of MIDI messages in the buffer.</param>
        /// <param name="dwCallback">The callback function for the stream.</param>
        /// <param name="dwInstance">The user instance data passed to the callback function.</param>
        /// <param name="fdwOpen">Flags for opening the MIDI stream.</param>
        /// <returns>The result of opening the MIDI stream.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamOpen(out IntPtr hMidiStream, IntPtr puDeviceID, int cMidi, IntPtr dwCallback, IntPtr dwInstance, int fdwOpen);

        /// <summary>
        /// Sends an output stream of MIDI data to a MIDI output device.
        /// </summary>
        /// <param name="hMidiStream">Handle to the MIDI stream to which the data is sent.</param>
        /// <param name="pmh">Reference to the MIDIHDR structure that contains the MIDI data to be sent.</param>
        /// <param name="cbmh">Size, in bytes, of the MIDIHDR structure.</param>
        /// <returns>An MmResult value indicating the success or failure of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamOut(IntPtr hMidiStream, ref MIDIHDR pmh, int cbmh);

        /// <summary>
        /// Pauses the specified MIDI stream.
        /// </summary>
        /// <param name="hMidiStream">Handle to the MIDI stream to be paused.</param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="MmException">Thrown when an error occurs while pausing the MIDI stream.</exception>
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamPause(IntPtr hMidiStream);

        /// <summary>
        /// Retrieves the current position in the MIDI stream.
        /// </summary>
        /// <param name="hMidiStream">Handle to the MIDI stream.</param>
        /// <param name="lpmmt">Reference to an MMTIME structure that receives the current position information.</param>
        /// <param name="cbmmt">Size, in bytes, of the MMTIME structure.</param>
        /// <returns>An MmResult value indicating the success or failure of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamPosition(IntPtr hMidiStream, ref MMTIME lpmmt, int cbmmt);

        /// <summary>
        /// Retrieves the current value of a property for a MIDI stream.
        /// </summary>
        /// <param name="hMidiStream">Handle to the MIDI stream.</param>
        /// <param name="lppropdata">Pointer to the property data structure.</param>
        /// <param name="dwProperty">Specifies the property to retrieve.</param>
        /// <returns>The result of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamProperty(IntPtr hMidiStream, IntPtr lppropdata, int dwProperty);

        /// <summary>
        /// Restarts a paused MIDI stream.
        /// </summary>
        /// <param name="hMidiStream">Handle to the MIDI stream to be restarted.</param>
        /// <returns>An MmResult value indicating the success or failure of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamRestart(IntPtr hMidiStream);

        /// <summary>
        /// Stops the MIDI stream.
        /// </summary>
        /// <param name="hMidiStream">Handle to the MIDI stream to be stopped.</param>
        /// <returns>The result of stopping the MIDI stream.</returns>
        /// <remarks>
        /// This method stops the specified MIDI stream identified by the handle <paramref name="hMidiStream"/>.
        /// </remarks>
        [DllImport("winmm.dll")]
        public static extern MmResult midiStreamStop(IntPtr hMidiStream);

        // TODO: this is general MM interop
        public const int CALLBACK_FUNCTION = 0x30000;
        public const int CALLBACK_NULL = 0;

        // http://msdn.microsoft.com/en-us/library/dd757347%28VS.85%29.aspx
        // TODO: not sure this is right
        [StructLayout(LayoutKind.Sequential)]
        public struct MMTIME
        {
            public int wType;
            public int u;
        }

        // TODO: check for ANSI strings in these structs
        // TODO: check for WORD params
        [StructLayout(LayoutKind.Sequential)]
        public struct MIDIEVENT
        {
            public int dwDeltaTime;
            public int dwStreamID;
            public int dwEvent;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public int dwParms;
        }

        // http://msdn.microsoft.com/en-us/library/dd798449%28VS.85%29.aspx
        [StructLayout(LayoutKind.Sequential)]
        public struct MIDIHDR
        {
            public IntPtr lpData; // LPSTR
            public int dwBufferLength; // DWORD
            public int dwBytesRecorded; // DWORD
            public IntPtr dwUser; // DWORD_PTR
            public int dwFlags; // DWORD
            public IntPtr lpNext; // struct mididhdr_tag *
            public IntPtr reserved; // DWORD_PTR
            public int dwOffset; // DWORD
            // n.b. MSDN documentation incorrect, see mmsystem.h
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] 
            public IntPtr[] dwReserved; // DWORD_PTR dwReserved[8]
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIDIPROPTEMPO
        {
            public int cbStruct;
            public int dwTempo;
        }
    }
}
