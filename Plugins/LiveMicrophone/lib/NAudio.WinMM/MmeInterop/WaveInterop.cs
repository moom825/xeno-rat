using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave
{
    /// <summary>
    /// MME Wave function interop
    /// </summary>
    public class WaveInterop
    {
        [Flags]
        public enum WaveInOutOpenFlags
        {
            /// <summary>
            /// CALLBACK_NULL
            /// No callback
            /// </summary>
            CallbackNull = 0,
            /// <summary>
            /// CALLBACK_FUNCTION
            /// dwCallback is a FARPROC 
            /// </summary>
            CallbackFunction = 0x30000,
            /// <summary>
            /// CALLBACK_EVENT
            /// dwCallback is an EVENT handle 
            /// </summary>
            CallbackEvent = 0x50000,
            /// <summary>
            /// CALLBACK_WINDOW
            /// dwCallback is a HWND 
            /// </summary>
            CallbackWindow = 0x10000,
            /// <summary>
            /// CALLBACK_THREAD
            /// callback is a thread ID 
            /// </summary>
            CallbackThread = 0x20000,
            /*
            WAVE_FORMAT_QUERY = 1,
            WAVE_MAPPED = 4,
            WAVE_FORMAT_DIRECT = 8*/
        }

        //public const int TIME_MS = 0x0001;  // time in milliseconds 
        //public const int TIME_SAMPLES = 0x0002;  // number of wave samples 
        //public const int TIME_BYTES = 0x0004;  // current byte offset 

        public enum WaveMessage
        {
            /// <summary>
            /// WIM_OPEN
            /// </summary>
            WaveInOpen = 0x3BE,
            /// <summary>
            /// WIM_CLOSE
            /// </summary>
            WaveInClose = 0x3BF,
            /// <summary>
            /// WIM_DATA
            /// </summary>
            WaveInData = 0x3C0,

            /// <summary>
            /// WOM_CLOSE
            /// </summary>
            WaveOutClose = 0x3BC,
            /// <summary>
            /// WOM_DONE
            /// </summary>
            WaveOutDone = 0x3BD,
            /// <summary>
            /// WOM_OPEN
            /// </summary>
            WaveOutOpen = 0x3BB
        }

        // use the userdata as a reference
        // WaveOutProc http://msdn.microsoft.com/en-us/library/dd743869%28VS.85%29.aspx
        // WaveInProc http://msdn.microsoft.com/en-us/library/dd743849%28VS.85%29.aspx
        public delegate void WaveCallback(IntPtr hWaveOut, WaveMessage message, IntPtr dwInstance, WaveHeader wavhdr, IntPtr dwReserved);

        /// <summary>
        /// Converts a string to a FOURCC value.
        /// </summary>
        /// <param name="s">The string to be converted to a FOURCC value.</param>
        /// <param name="flags">Additional flags for the conversion.</param>
        /// <returns>The FOURCC value corresponding to the input string.</returns>
        /// <remarks>
        /// This method converts the input string <paramref name="s"/> to a FOURCC value using the specified flags.
        /// The FOURCC (Four Character Code) is a sequence of four bytes used to uniquely identify data formats.
        /// The conversion is performed using the winmm.dll library function mmioStringToFOURCC.
        /// </remarks>
        [DllImport("winmm.dll")]
        public static extern Int32 mmioStringToFOURCC([MarshalAs(UnmanagedType.LPStr)] String s, int flags);

        /// <summary>
        /// Retrieves the number of waveform-audio output devices present in the system.
        /// </summary>
        /// <returns>The number of waveform-audio output devices present in the system.</returns>
        [DllImport("winmm.dll")]
        public static extern Int32 waveOutGetNumDevs();

        /// <summary>
        /// Prepares the header for playback on a waveform-audio output device.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
        /// <param name="lpWaveOutHdr">Pointer to a WaveHeader structure that identifies the header to be prepared.</param>
        /// <param name="uSize">Size, in bytes, of the WaveHeader structure.</param>
        /// <returns>Returns an MmResult value representing the result of the operation.</returns>
        /// <remarks>
        /// This method prepares the specified header for playback on the specified waveform-audio output device.
        /// The header must be prepared with this method before it is used in playback operations.
        /// </remarks>
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutPrepareHeader(IntPtr hWaveOut, WaveHeader lpWaveOutHdr, int uSize);

        /// <summary>
        /// Notifies the audio device driver that it can return a buffer to the application.
        /// </summary>
        /// <param name="hWaveOut">Handle to the audio output device.</param>
        /// <param name="lpWaveOutHdr">Pointer to a WaveHeader structure that identifies the buffer to be returned.</param>
        /// <param name="uSize">Size, in bytes, of the WaveHeader structure.</param>
        /// <returns>Returns MmResult indicating success or failure.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutUnprepareHeader(IntPtr hWaveOut, WaveHeader lpWaveOutHdr, int uSize);

        /// <summary>
        /// Sends a data block to the specified waveform-audio output device.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
        /// <param name="lpWaveOutHdr">Pointer to a WaveHeader structure that identifies the data block to be sent.</param>
        /// <param name="uSize">Size, in bytes, of the WaveHeader structure.</param>
        /// <returns>Returns MMSYSERR_NOERROR if successful or an error otherwise.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutWrite(IntPtr hWaveOut, WaveHeader lpWaveOutHdr, int uSize);

        /// <summary>
        /// Opens a waveform output device for playback.
        /// </summary>
        /// <param name="hWaveOut">The handle to the opened waveform output device.</param>
        /// <param name="uDeviceID">The device identifier of the waveform-audio output device. It can be a device identifier or a handle of a MIDI output device.</param>
        /// <param name="lpFormat">A pointer to a WaveFormat structure that identifies the format for waveform-audio data.</param>
        /// <param name="dwCallback">The address of a callback function or a handle to an event that is called when playback has finished.</param>
        /// <param name="dwInstance">User-instance data passed to the callback function or event handler.</param>
        /// <param name="dwFlags">Flags for opening the waveform-audio output device.</param>
        /// <returns>Returns an MmResult value indicating the result of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutOpen(out IntPtr hWaveOut, IntPtr uDeviceID, WaveFormat lpFormat, WaveCallback dwCallback, IntPtr dwInstance, WaveInOutOpenFlags dwFlags);

        /// <summary>
        /// Opens a waveform-audio output device for playback.
        /// </summary>
        /// <param name="hWaveOut">The address of a handle to be used to identify the opened waveform-audio output device.</param>
        /// <param name="uDeviceID">The device identifier of the waveform-audio output device to open.</param>
        /// <param name="lpFormat">A pointer to a WaveFormat structure that identifies the format of the waveform-audio data to be sent to the output device.</param>
        /// <param name="callbackWindowHandle">A handle to a window that will receive callback information from the waveform-audio output device.</param>
        /// <param name="dwInstance">User-instance data passed to the callback function.</param>
        /// <param name="dwFlags">Flags for opening the waveform-audio output device.</param>
        /// <returns>An MmResult value that specifies the result of the operation.</returns>
        [DllImport("winmm.dll", EntryPoint = "waveOutOpen")]
        public static extern MmResult waveOutOpenWindow(out IntPtr hWaveOut, IntPtr uDeviceID, WaveFormat lpFormat, IntPtr callbackWindowHandle, IntPtr dwInstance, WaveInOutOpenFlags dwFlags);

        /// <summary>
        /// Resets the specified waveform-audio output device.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
        /// <returns>An MmResult value that represents the result of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutReset(IntPtr hWaveOut);

        /// <summary>
        /// Closes the specified waveform output device.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio output device. This parameter can also be a device identifier.</param>
        /// <returns>An MmResult value indicating the result of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutClose(IntPtr hWaveOut);

        /// <summary>
        /// Pauses playback on the specified waveform-audio output device.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
        /// <returns>An MmResult value indicating the success or failure of the operation.</returns>
        /// <remarks>
        /// This method pauses playback on the specified waveform-audio output device identified by the handle <paramref name="hWaveOut"/>.
        /// If successful, the method returns MmResult.NoError; otherwise, it returns an error code indicating the cause of failure.
        /// </remarks>
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutPause(IntPtr hWaveOut);

        /// <summary>
        /// Restarts playback on the specified waveform-audio output device.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
        /// <returns>An MmResult value representing the success or failure of the operation.</returns>
        /// <remarks>
        /// This method restarts playback on the specified waveform-audio output device that was previously paused or stopped.
        /// </remarks>
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutRestart(IntPtr hWaveOut);

        /// <summary>
        /// Retrieves the current playback position of the specified waveform output device.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform output device.</param>
        /// <param name="mmTime">Reference to an MmTime structure that receives the current playback position.</param>
        /// <param name="uSize">Size of the MmTime structure.</param>
        /// <returns>Returns an MmResult value indicating the success or failure of the operation.</returns>
        /// <remarks>
        /// This method retrieves the current playback position of the specified waveform output device and stores it in the provided MmTime structure.
        /// The MmTime structure contains timing information for various multimedia devices.
        /// </remarks>
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutGetPosition(IntPtr hWaveOut, ref MmTime mmTime, int uSize);

        /// <summary>
        /// Sets the volume level of the specified waveform-audio output device.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
        /// <param name="dwVolume">New volume setting for the audio device. The low-order word contains the left-channel volume setting, and the high-order word contains the right-channel setting.</param>
        /// <returns>Returns MMSYSERR_NOERROR if successful, otherwise an error code.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutSetVolume(IntPtr hWaveOut, int dwVolume);

        /// <summary>
        /// Retrieves the current volume level of the specified waveform-audio output device.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
        /// <param name="dwVolume">Receives the current volume setting of the audio device. The low-order word contains the left-channel volume setting, and the high-order word contains the right-channel setting.</param>
        /// <returns>An MmResult value indicating the success or failure of the function call.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutGetVolume(IntPtr hWaveOut, out int dwVolume);

        /// <summary>
        /// Retrieves the capabilities of a specified waveform-audio output device.
        /// </summary>
        /// <param name="deviceID">The identifier of the waveform-audio output device.</param>
        /// <param name="waveOutCaps">An output parameter that receives the capabilities of the specified waveform-audio output device.</param>
        /// <param name="waveOutCapsSize">The size, in bytes, of the <paramref name="waveOutCaps"/> parameter.</param>
        /// <returns>An MmResult value indicating the success or failure of the function call.</returns>
        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        public static extern MmResult waveOutGetDevCaps(IntPtr deviceID, out WaveOutCapabilities waveOutCaps, int waveOutCapsSize);

        /// <summary>
        /// Retrieves the number of waveform-audio input devices present in the system.
        /// </summary>
        /// <returns>The number of waveform-audio input devices present in the system.</returns>
        [DllImport("winmm.dll")]
        public static extern Int32 waveInGetNumDevs();

        /// <summary>
        /// Retrieves the capabilities of a specified input device.
        /// </summary>
        /// <param name="deviceID">The identifier of the input device.</param>
        /// <param name="waveInCaps">When this method returns, contains the capabilities of the input device.</param>
        /// <param name="waveInCapsSize">The size, in bytes, of the <paramref name="waveInCaps"/> structure.</param>
        /// <returns>An MmResult value indicating the success or failure of the operation.</returns>
        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        public static extern MmResult waveInGetDevCaps(IntPtr deviceID, out WaveInCapabilities waveInCaps, int waveInCapsSize);

        /// <summary>
        /// Adds a buffer to the specified waveform-audio input device.
        /// </summary>
        /// <param name="hWaveIn">Handle to the waveform-audio input device.</param>
        /// <param name="pwh">Pointer to a WaveHeader structure that identifies the buffer.</param>
        /// <param name="cbwh">Size, in bytes, of the WaveHeader structure.</param>
        /// <exception cref="MmException">Thrown when an error occurs while adding the buffer to the waveform-audio input device.</exception>
        [DllImport("winmm.dll")]
        public static extern MmResult waveInAddBuffer(IntPtr hWaveIn, WaveHeader pwh, int cbwh);

        /// <summary>
        /// Closes the specified waveform-audio input device.
        /// </summary>
        /// <param name="hWaveIn">Handle to the waveform-audio input device. This handle is returned by the waveInOpen function.</param>
        /// <returns>Returns MMSYSERR_NOERROR if successful or an error otherwise.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult waveInClose(IntPtr hWaveIn);

        /// <summary>
        /// Opens a waveform input device for recording.
        /// </summary>
        /// <param name="hWaveIn">The handle of the opened waveform input device.</param>
        /// <param name="uDeviceID">The device identifier of the waveform-audio input device to open.</param>
        /// <param name="lpFormat">An instance of the WaveFormat class that specifies the format of the waveform-audio data to be recorded.</param>
        /// <param name="dwCallback">The address of a fixed callback function, an event handle, a handle to a window, or the identifier of a thread to be called during waveform-audio recording to process messages related to the progress of recording.</param>
        /// <param name="dwInstance">User-instance data passed to the callback mechanism.</param>
        /// <param name="dwFlags">Flags for opening the waveform-audio input device.</param>
        /// <returns>The result of the operation as an MmResult value.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult waveInOpen(out IntPtr hWaveIn, IntPtr uDeviceID, WaveFormat lpFormat, WaveCallback dwCallback, IntPtr dwInstance, WaveInOutOpenFlags dwFlags);

        /// <summary>
        /// Opens a waveform-audio input device for recording.
        /// </summary>
        /// <param name="hWaveIn">The handle of the opened waveform-audio input device.</param>
        /// <param name="uDeviceID">The identifier of the waveform-audio input device to open.</param>
        /// <param name="lpFormat">A pointer to a WaveFormat structure that identifies the desired format for recording waveform-audio data.</param>
        /// <param name="callbackWindowHandle">A handle to the window that will receive callback information when waveform-audio data is recorded.</param>
        /// <param name="dwInstance">User-instance data passed to the callback function.</param>
        /// <param name="dwFlags">Flags for opening the waveform-audio input device.</param>
        /// <returns>An MmResult value indicating the success or failure of the operation.</returns>
        [DllImport("winmm.dll", EntryPoint = "waveInOpen")]
        public static extern MmResult waveInOpenWindow(out IntPtr hWaveIn, IntPtr uDeviceID, WaveFormat lpFormat, IntPtr callbackWindowHandle, IntPtr dwInstance, WaveInOutOpenFlags dwFlags);

        /// <summary>
        /// Prepares the specified waveform-audio input device for input.
        /// </summary>
        /// <param name="hWaveIn">Handle to the waveform-audio input device.</param>
        /// <param name="lpWaveInHdr">Pointer to a WaveHeader structure that identifies the waveform-audio data block to be prepared.</param>
        /// <param name="uSize">Size, in bytes, of the WaveHeader structure.</param>
        /// <returns>An MmResult value that indicates the result of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult waveInPrepareHeader(IntPtr hWaveIn, WaveHeader lpWaveInHdr, int uSize);

        /// <summary>
        /// Unprepares the header for wave input.
        /// </summary>
        /// <param name="hWaveIn">Handle to the waveform-audio input device.</param>
        /// <param name="lpWaveInHdr">Pointer to a WaveHeader structure that identifies the header to be unprepared.</param>
        /// <param name="uSize">Size, in bytes, of the WaveHeader structure.</param>
        /// <returns>Returns MmResult indicating the result of the operation.</returns>
        /// <remarks>
        /// This method unprepares the header for wave input on the specified waveform-audio input device.
        /// </remarks>
        [DllImport("winmm.dll")]
        public static extern MmResult waveInUnprepareHeader(IntPtr hWaveIn, WaveHeader lpWaveInHdr, int uSize);

        /// <summary>
        /// Resets the specified waveform-audio input device.
        /// </summary>
        /// <param name="hWaveIn">Handle to the waveform-audio input device.</param>
        /// <returns>An MmResult value that specifies the result of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult waveInReset(IntPtr hWaveIn);

        /// <summary>
        /// Starts input on the specified waveform-audio input device.
        /// </summary>
        /// <param name="hWaveIn">Handle to the waveform-audio input device.</param>
        /// <returns>Returns MMSYSERR_NOERROR if successful, otherwise an error code.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult waveInStart(IntPtr hWaveIn);

        /// <summary>
        /// Stops audio input on the specified waveform-audio input device.
        /// </summary>
        /// <param name="hWaveIn">Handle to the waveform-audio input device.</param>
        /// <returns>Returns an MmResult value indicating the result of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult waveInStop(IntPtr hWaveIn);

        /// <summary>
        /// Retrieves the current position in the audio stream being played or recorded.
        /// </summary>
        /// <param name="hWaveIn">Handle to the input device.</param>
        /// <param name="mmTime">A reference to a <see cref="MmTime"/> structure that receives the current position.</param>
        /// <param name="uSize">The size, in bytes, of the <paramref name="mmTime"/> structure.</param>
        /// <returns>An MmResult value indicating the success or failure of the operation.</returns>
        [DllImport("winmm.dll")]
        public static extern MmResult waveInGetPosition(IntPtr hWaveIn, out MmTime mmTime, int uSize);


    }
}
