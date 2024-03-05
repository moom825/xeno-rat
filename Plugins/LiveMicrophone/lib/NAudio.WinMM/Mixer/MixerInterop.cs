// created on 09/12/2002 at 21:03
using System;
using System.Runtime.InteropServices;
using NAudio.Wave;

// TODO: add function help from MSDN
// TODO: Create enums for flags parameters
namespace NAudio.Mixer
{
    class MixerInterop
    {
        public const UInt32 MIXERCONTROL_CONTROLF_UNIFORM = 0x00000001;
        public const UInt32 MIXERCONTROL_CONTROLF_MULTIPLE = 0x00000002;
        public const UInt32 MIXERCONTROL_CONTROLF_DISABLED = 0x80000000;

        public const Int32 MAXPNAMELEN = 32;
        public const Int32 MIXER_SHORT_NAME_CHARS = 16;
        public const Int32 MIXER_LONG_NAME_CHARS = 64;

        /// <summary>
        /// Retrieves the number of multimedia mixer devices present in the system.
        /// </summary>
        /// <returns>The number of multimedia mixer devices present in the system.</returns>
        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        public static extern Int32 mixerGetNumDevs();

        /// <summary>
        /// Opens a specified mixer device and returns a handle to the device.
        /// </summary>
        /// <param name="hMixer">When this method returns, contains a handle to the opened mixer device.</param>
        /// <param name="uMxId">The identifier of the mixer device to open.</param>
        /// <param name="dwCallback">Reserved; must be IntPtr.Zero.</param>
        /// <param name="dwInstance">Reserved; must be IntPtr.Zero.</param>
        /// <param name="dwOpenFlags">Flags that specify options for opening the mixer device.</param>
        /// <returns>An MmResult value that specifies the result of the operation.</returns>
        /// <exception cref="MmException">Thrown when an error occurs while opening the mixer device.</exception>
        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        public static extern MmResult mixerOpen(out IntPtr hMixer, int uMxId, IntPtr dwCallback, IntPtr dwInstance, MixerFlags dwOpenFlags);

        /// <summary>
        /// Closes the specified mixer device.
        /// </summary>
        /// <param name="hMixer">Handle to the mixer device to be closed.</param>
        /// <returns>An MmResult value indicating the result of the operation.</returns>
        /// <remarks>
        /// This method closes the specified mixer device identified by the handle <paramref name="hMixer"/>.
        /// </remarks>
        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        public static extern MmResult mixerClose(IntPtr hMixer);

        /// <summary>
        /// Retrieves details about a specified audio mixer control.
        /// </summary>
        /// <param name="hMixer">Handle to the audio mixer device.</param>
        /// <param name="mixerControlDetails">Reference to a <see cref="MIXERCONTROLDETAILS"/> structure that will receive the control details.</param>
        /// <param name="dwDetailsFlags">Flags specifying the details to retrieve.</param>
        /// <returns>An <see cref="MmResult"/> value indicating the success or failure of the operation.</returns>
        /// <remarks>
        /// This method retrieves details about a specified audio mixer control identified by the <paramref name="mixerControlDetails"/> parameter.
        /// The details retrieved are determined by the flags specified in the <paramref name="dwDetailsFlags"/> parameter.
        /// </remarks>
        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        public static extern MmResult mixerGetControlDetails(IntPtr hMixer, ref MIXERCONTROLDETAILS mixerControlDetails, MixerFlags dwDetailsFlags);

        /// <summary>
        /// Retrieves the capabilities of a specified mixer device.
        /// </summary>
        /// <param name="nMixerID">The handle to the mixer device to get the capabilities for.</param>
        /// <param name="mixerCaps">A reference to a <see cref="MIXERCAPS"/> structure that will receive the capabilities of the mixer device.</param>
        /// <param name="mixerCapsSize">The size of the <paramref name="mixerCaps"/> structure in bytes.</param>
        /// <returns>An <see cref="MmResult"/> value indicating the success or failure of the operation.</returns>
        /// <remarks>
        /// This method retrieves the capabilities of the specified mixer device identified by <paramref name="nMixerID"/>.
        /// The capabilities are stored in the <paramref name="mixerCaps"/> structure, and the size of the structure must be specified in <paramref name="mixerCapsSize"/>.
        /// If successful, the method returns <see cref="MmResult.MMSYSERR_NOERROR"/>; otherwise, it returns an error code indicating the cause of failure.
        /// </remarks>
        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        public static extern MmResult mixerGetDevCaps(IntPtr nMixerID, ref MIXERCAPS mixerCaps, Int32 mixerCapsSize);

        /// <summary>
        /// Retrieves the identifier of a mixer device associated with the specified mixer handle.
        /// </summary>
        /// <param name="hMixer">The handle to the mixer device.</param>
        /// <param name="mixerID">When this method returns, contains the identifier of the mixer device.</param>
        /// <param name="dwMixerIDFlags">Flags specifying how the mixer identifier should be retrieved.</param>
        /// <returns>An MmResult value indicating the success or failure of the operation.</returns>
        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        public static extern MmResult mixerGetID(IntPtr hMixer, out Int32 mixerID, MixerFlags dwMixerIDFlags);

        /// <summary>
        /// Retrieves the controls for a specified audio mixer line.
        /// </summary>
        /// <param name="hMixer">Handle to the mixer device.</param>
        /// <param name="mixerLineControls">Reference to a <see cref="MIXERLINECONTROLS"/> structure that specifies the controls to retrieve.</param>
        /// <param name="dwControlFlags">Flags that specify the control types to retrieve.</param>
        /// <returns>An <see cref="MmResult"/> value indicating the result of the operation.</returns>
        /// <remarks>
        /// This method retrieves the controls for the specified audio mixer line using the specified handle to the mixer device.
        /// The controls to retrieve are specified by the <paramref name="mixerLineControls"/> parameter, and the types of controls to retrieve are specified by the <paramref name="dwControlFlags"/> parameter.
        /// </remarks>
        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        public static extern MmResult mixerGetLineControls(IntPtr hMixer, ref MIXERLINECONTROLS mixerLineControls, MixerFlags dwControlFlags);

        /// <summary>
        /// Retrieves information about a specified audio mixer line.
        /// </summary>
        /// <param name="hMixer">Handle to the mixer device.</param>
        /// <param name="mixerLine">Reference to a MIXERLINE structure that will receive the line information.</param>
        /// <param name="dwInfoFlags">Flags specifying the information to retrieve.</param>
        /// <returns>Returns an MmResult value indicating the success or failure of the operation.</returns>
        /// <remarks>
        /// This method retrieves information about a specified audio mixer line identified by the <paramref name="mixerLine"/> parameter.
        /// The information retrieved is determined by the <paramref name="dwInfoFlags"/> parameter, which specifies the type of information to retrieve.
        /// The retrieved information is stored in the <paramref name="mixerLine"/> structure.
        /// </remarks>
        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        public static extern MmResult mixerGetLineInfo(IntPtr hMixer, ref MIXERLINE mixerLine, MixerFlags dwInfoFlags);

        /// <summary>
        /// Sends a message to a specified mixer device.
        /// </summary>
        /// <param name="hMixer">Handle to the mixer device.</param>
        /// <param name="nMessage">The message to send to the mixer device.</param>
        /// <param name="dwParam1">Message-specific parameter.</param>
        /// <param name="dwParam2">Message-specific parameter.</param>
        /// <returns>The result of the message sent to the mixer device.</returns>
        /// <remarks>
        /// This method sends a message to the specified mixer device using the parameters provided.
        /// </remarks>
        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        public static extern MmResult mixerMessage(IntPtr hMixer, UInt32 nMessage, IntPtr dwParam1, IntPtr dwParam2);

        /// <summary>
        /// Sets the details of a specified audio mixer control.
        /// </summary>
        /// <param name="hMixer">Handle to the audio mixer device.</param>
        /// <param name="mixerControlDetails">Reference to a MIXERCONTROLDETAILS structure that specifies the details to set.</param>
        /// <param name="dwDetailsFlags">Flags that define the details to set.</param>
        /// <returns>The result of the operation as an MmResult value.</returns>
        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        public static extern MmResult mixerSetControlDetails(IntPtr hMixer, ref MIXERCONTROLDETAILS mixerControlDetails, MixerFlags dwDetailsFlags);

        // http://msdn.microsoft.com/en-us/library/dd757294%28VS.85%29.aspx
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
        public struct MIXERCONTROLDETAILS
        {
            public Int32 cbStruct; // size of the MIXERCONTROLDETAILS structure
            public Int32 dwControlID;
            public Int32 cChannels; // Number of channels on which to get or set control properties
            public IntPtr hwndOwner; // Union with DWORD cMultipleItems
            public Int32 cbDetails; // Size of the paDetails Member
            public IntPtr paDetails; // LPVOID
        }

        // http://msdn.microsoft.com/en-us/library/dd757291%28VS.85%29.aspx
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct MIXERCAPS
        {
            public UInt16 wMid;
            public UInt16 wPid;
            public UInt32 vDriverVersion; // MMVERSION - major high byte, minor low byte
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAXPNAMELEN)]
            public String szPname;
            public UInt32 fdwSupport;
            public UInt32 cDestinations;
        }

        // http://msdn.microsoft.com/en-us/library/dd757306%28VS.85%29.aspx
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct MIXERLINECONTROLS
        {
            public Int32 cbStruct; // size of the MIXERLINECONTROLS structure
            public Int32 dwLineID; // Line identifier for which controls are being queried
            public Int32 dwControlID; // union with UInt32 dwControlType
            public Int32 cControls;
            public Int32 cbmxctrl;
            public IntPtr pamxctrl; // see MSDN "Structs Sample"
        }

        /// <summary>
        /// Mixer Line Flags
        /// </summary>
        [Flags]
        public enum MIXERLINE_LINEF
        {
            /// <summary>
            /// Audio line is active. An active line indicates that a signal is probably passing 
            /// through the line.
            /// </summary>
            MIXERLINE_LINEF_ACTIVE = 1,

            /// <summary>
            /// Audio line is disconnected. A disconnected line's associated controls can still be 
            /// modified, but the changes have no effect until the line is connected.
            /// </summary>
            MIXERLINE_LINEF_DISCONNECTED = 0x8000,

            /// <summary>
            /// Audio line is an audio source line associated with a single audio destination line. 
            /// If this flag is not set, this line is an audio destination line associated with zero 
            /// or more audio source lines.
            /// </summary>
            MIXERLINE_LINEF_SOURCE = (unchecked((int)0x80000000))
        }

        // http://msdn.microsoft.com/en-us/library/dd757305%28VS.85%29.aspx
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct MIXERLINE
        {
            public Int32 cbStruct;
            public Int32 dwDestination;
            public Int32 dwSource;
            public Int32 dwLineID;
            public MIXERLINE_LINEF fdwLine;
            public IntPtr dwUser;
            public MixerLineComponentType dwComponentType;
            public Int32 cChannels;
            public Int32 cConnections;
            public Int32 cControls;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MIXER_SHORT_NAME_CHARS)]
            public String szShortName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MIXER_LONG_NAME_CHARS)]
            public String szName;
            // start of target struct 'Target'
            public UInt32 dwType;
            public UInt32 dwDeviceID;
            public UInt16 wMid;
            public UInt16 wPid;
            public UInt32 vDriverVersion; // MMVERSION - major high byte, minor low byte
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAXPNAMELEN)]
            public String szPname;
            // end of target struct
        }

        /// <summary>
        /// BOUNDS structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct Bounds
        {
            /// <summary>
            /// dwMinimum / lMinimum / reserved 0
            /// </summary>
            public int minimum;
            /// <summary>
            /// dwMaximum / lMaximum / reserved 1
            /// </summary>
            public int maximum;
            /// <summary>
            /// reserved 2
            /// </summary>
            public int reserved2;
            /// <summary>
            /// reserved 3
            /// </summary>
            public int reserved3;
            /// <summary>
            /// reserved 4
            /// </summary>
            public int reserved4;
            /// <summary>
            /// reserved 5
            /// </summary>
            public int reserved5;
        }

        /// <summary>
        /// METRICS structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct Metrics
        {
            /// <summary>
            /// cSteps / reserved[0]
            /// </summary>
            public int step;
            /// <summary>
            /// cbCustomData / reserved[1], number of bytes for control details
            /// </summary>
            public int customData;
            /// <summary>
            /// reserved 2
            /// </summary>
            public int reserved2;
            /// <summary>
            /// reserved 3
            /// </summary>
            public int reserved3;
            /// <summary>
            /// reserved 4
            /// </summary>
            public int reserved4;
            /// <summary>
            /// reserved 5
            /// </summary>
            public int reserved5;
        }

        /// <summary>
        /// MIXERCONTROL struct
        /// http://msdn.microsoft.com/en-us/library/dd757293%28VS.85%29.aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct MIXERCONTROL
        {
            public UInt32 cbStruct;
            public Int32 dwControlID;
            public MixerControlType dwControlType;
            public UInt32 fdwControl;
            public UInt32 cMultipleItems;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MIXER_SHORT_NAME_CHARS)]
            public String szShortName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MIXER_LONG_NAME_CHARS)]
            public String szName;
            public Bounds Bounds;
            public Metrics Metrics;
        }

        // http://msdn.microsoft.com/en-us/library/dd757295%28VS.85%29.aspx
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct MIXERCONTROLDETAILS_BOOLEAN
        {
            public Int32 fValue;
        }

        // http://msdn.microsoft.com/en-us/library/dd757297%28VS.85%29.aspx
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct MIXERCONTROLDETAILS_SIGNED
        {
            public Int32 lValue;
        }

        // http://msdn.microsoft.com/en-us/library/dd757296%28VS.85%29.aspx
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct MIXERCONTROLDETAILS_LISTTEXT
        {
            public UInt32 dwParam1;
            public UInt32 dwParam2;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MIXER_LONG_NAME_CHARS)]
            public String szName;
        }

        // http://msdn.microsoft.com/en-us/library/dd757298%28VS.85%29.aspx
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct MIXERCONTROLDETAILS_UNSIGNED
        {
            public UInt32 dwValue;
        }
    }
}
