// created on 10/12/2002 at 21:11
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using NAudio.Utils;

namespace NAudio.Mixer
{
    /// <summary>
    /// Represents a mixer control
    /// </summary>
    public abstract class MixerControl
    {
        internal MixerInterop.MIXERCONTROL mixerControl;
        internal MixerInterop.MIXERCONTROLDETAILS mixerControlDetails;

        /// <summary>
        /// Mixer Handle
        /// </summary>
        protected IntPtr mixerHandle;

        /// <summary>
        /// Number of Channels
        /// </summary>
        protected int nChannels;

        /// <summary>
        /// Mixer Handle Type
        /// </summary>
        protected MixerFlags mixerHandleType;

        /// <summary>
        /// Retrieves the mixer controls associated with the specified mixer line.
        /// </summary>
        /// <param name="mixerHandle">The handle to the mixer device.</param>
        /// <param name="mixerLine">The mixer line for which controls are to be retrieved.</param>
        /// <param name="mixerHandleType">The type of handle to be retrieved.</param>
        /// <returns>A list of mixer controls associated with the specified mixer line.</returns>
        /// <exception cref="MmException">Thrown when an error occurs while retrieving the mixer controls.</exception>
        public static IList<MixerControl> GetMixerControls(IntPtr mixerHandle, MixerLine mixerLine,
                                                           MixerFlags mixerHandleType)
        {
            var controls = new List<MixerControl>();
            if (mixerLine.ControlsCount > 0)
            {
                int mixerControlSize = Marshal.SizeOf<MixerInterop.MIXERCONTROL>();
                var mlc = new MixerInterop.MIXERLINECONTROLS();
                IntPtr pmc = Marshal.AllocHGlobal(mixerControlSize * mixerLine.ControlsCount);
                mlc.cbStruct = Marshal.SizeOf(mlc);
                mlc.dwLineID = mixerLine.LineId;
                mlc.cControls = mixerLine.ControlsCount;
                mlc.pamxctrl = pmc;
                mlc.cbmxctrl = Marshal.SizeOf<MixerInterop.MIXERCONTROL>();
                try
                {
                    MmResult err = MixerInterop.mixerGetLineControls(mixerHandle, ref mlc, MixerFlags.All | mixerHandleType);
                    if (err != MmResult.NoError)
                    {
                        throw new MmException(err, "mixerGetLineControls");
                    }
                    for (int i = 0; i < mlc.cControls; i++)
                    {
                        Int64 address = pmc.ToInt64() + mixerControlSize * i;

                        var mc = 
                            Marshal.PtrToStructure<MixerInterop.MIXERCONTROL>((IntPtr)address);
                        var mixerControl = GetMixerControl(mixerHandle, mixerLine.LineId, mc.dwControlID, mixerLine.Channels,
                                                                                 mixerHandleType);

                        controls.Add(mixerControl);
                    }
                }
                finally 
                {
                    Marshal.FreeHGlobal(pmc);
                }

            }
            return controls;
        }

        /// <summary>
        /// Retrieves the specified mixer control.
        /// </summary>
        /// <param name="mixerHandle">The handle to the mixer device.</param>
        /// <param name="nLineId">The line identifier.</param>
        /// <param name="controlId">The control identifier.</param>
        /// <param name="nChannels">The number of channels.</param>
        /// <param name="mixerFlags">The mixer flags.</param>
        /// <returns>The retrieved mixer control based on the specified parameters.</returns>
        /// <exception cref="MmException">Thrown when an error occurs during the retrieval of the mixer control.</exception>
        public static MixerControl GetMixerControl(IntPtr mixerHandle, int nLineId, int controlId, int nChannels,
                                                   MixerFlags mixerFlags)
        {
            var mlc = new MixerInterop.MIXERLINECONTROLS();
            var mc = new MixerInterop.MIXERCONTROL();

            // set up the pointer to a structure
            IntPtr pMixerControl = Marshal.AllocCoTaskMem(Marshal.SizeOf(mc));
            //Marshal.StructureToPtr(mc, pMixerControl, false);      

            mlc.cbStruct = Marshal.SizeOf(mlc);
            mlc.cControls = 1;
            mlc.dwControlID = controlId;
            mlc.cbmxctrl = Marshal.SizeOf(mc);
            mlc.pamxctrl = pMixerControl;
            mlc.dwLineID = nLineId;
            MmResult err = MixerInterop.mixerGetLineControls(mixerHandle, ref mlc, MixerFlags.OneById | mixerFlags);
            if (err != MmResult.NoError)
            {
                Marshal.FreeCoTaskMem(pMixerControl);
                throw new MmException(err, "mixerGetLineControls");
            }

            // retrieve the structure from the pointer
            mc = Marshal.PtrToStructure<MixerInterop.MIXERCONTROL>(mlc.pamxctrl);
            Marshal.FreeCoTaskMem(pMixerControl);

            if (IsControlBoolean(mc.dwControlType))
            {
                return new BooleanMixerControl(mc, mixerHandle, mixerFlags, nChannels);
            }

            if (IsControlSigned(mc.dwControlType))
            {
                return new SignedMixerControl(mc, mixerHandle, mixerFlags, nChannels);
            }

            if (IsControlUnsigned(mc.dwControlType))
            {
                return new UnsignedMixerControl(mc, mixerHandle, mixerFlags, nChannels);
            }

            if (IsControlListText(mc.dwControlType))
            {
                return new ListTextMixerControl(mc, mixerHandle, mixerFlags, nChannels);
            }

            if (IsControlCustom(mc.dwControlType))
            {
                return new CustomMixerControl(mc, mixerHandle, mixerFlags, nChannels);
            }

            throw new InvalidOperationException($"Unknown mixer control type {mc.dwControlType}");
        }

        /// <summary>
        /// Retrieves details of the mixer control.
        /// </summary>
        /// <remarks>
        /// This method retrieves the details of the specified mixer control and populates the <paramref name="mixerControlDetails"/> structure with the retrieved information.
        /// The method first sets the size of the <paramref name="mixerControlDetails"/> structure and assigns the control ID from the <paramref name="mixerControl"/>.
        /// It then determines the number of channels for the control based on whether it is custom, uniform, or has a specific number of channels.
        /// If the control is a multiple item control, it sets the owner window handle to the value of <paramref name="mixerControl.cMultipleItems"/>.
        /// It then determines the size of the details based on the type of control (boolean, list text, signed, unsigned, or custom) and the number of channels.
        /// After allocating memory for the details, it retrieves the control details using <see cref="MixerInterop.mixerGetControlDetails"/> and populates the details in the allocated buffer.
        /// If successful, it calls the <see cref="GetDetails"/> method to process the retrieved details and then frees the allocated memory.
        /// If an error occurs during the retrieval process, it throws a <see cref="MmException"/> with the error message "mixerGetControlDetails".
        /// </remarks>
        /// <exception cref="MmException">Thrown when an error occurs during the retrieval of mixer control details.</exception>
        protected void GetControlDetails()
        {
            mixerControlDetails.cbStruct = Marshal.SizeOf(mixerControlDetails);
            mixerControlDetails.dwControlID = mixerControl.dwControlID;
            if (IsCustom)
            {
                mixerControlDetails.cChannels = 0;
            }
            else if ((mixerControl.fdwControl & MixerInterop.MIXERCONTROL_CONTROLF_UNIFORM) != 0)
            {
                mixerControlDetails.cChannels = 1;
            }
            else
            {
                mixerControlDetails.cChannels = nChannels;
            }


            if ((mixerControl.fdwControl & MixerInterop.MIXERCONTROL_CONTROLF_MULTIPLE) != 0)
            {
                mixerControlDetails.hwndOwner = (IntPtr) mixerControl.cMultipleItems;
            }
            else if (IsCustom)
            {
                mixerControlDetails.hwndOwner = IntPtr.Zero; // TODO: special cases
            }
            else
            {
                mixerControlDetails.hwndOwner = IntPtr.Zero;
            }

            if (IsBoolean)
            {
                mixerControlDetails.cbDetails = Marshal.SizeOf<MixerInterop.MIXERCONTROLDETAILS_BOOLEAN>();
            }
            else if (IsListText)
            {
                mixerControlDetails.cbDetails = Marshal.SizeOf<MixerInterop.MIXERCONTROLDETAILS_LISTTEXT>();
            }
            else if (IsSigned)
            {
                mixerControlDetails.cbDetails = Marshal.SizeOf<MixerInterop.MIXERCONTROLDETAILS_SIGNED>();
            }
            else if (IsUnsigned)
            {
                mixerControlDetails.cbDetails = Marshal.SizeOf<MixerInterop.MIXERCONTROLDETAILS_UNSIGNED>();
            }
            else
            {
                // must be custom
                mixerControlDetails.cbDetails = mixerControl.Metrics.customData;
            }
            var detailsSize = mixerControlDetails.cbDetails*mixerControlDetails.cChannels;
            if ((mixerControl.fdwControl & MixerInterop.MIXERCONTROL_CONTROLF_MULTIPLE) != 0)
            {
                // fixing issue 16390 - calculating size correctly for multiple items
                detailsSize *= (int) mixerControl.cMultipleItems;
            }
            IntPtr buffer = Marshal.AllocCoTaskMem(detailsSize);
            // To copy stuff in:
            // Marshal.StructureToPtr( theStruct, buffer, false );
            mixerControlDetails.paDetails = buffer;
            MmResult err = MixerInterop.mixerGetControlDetails(mixerHandle, ref mixerControlDetails,
                                                               MixerFlags.Value | mixerHandleType);
            // let the derived classes get the details before we free the handle			
            if (err == MmResult.NoError)
            {
                GetDetails(mixerControlDetails.paDetails);
            }
            Marshal.FreeCoTaskMem(buffer);
            if (err != MmResult.NoError)
            {
                throw new MmException(err, "mixerGetControlDetails");
            }
        }

        /// <summary>
        /// Retrieves the details using the provided pointer to details.
        /// </summary>
        /// <param name="pDetails">A pointer to the details.</param>
        /// <remarks>
        /// This method retrieves the details using the provided pointer to details and is intended to be implemented in derived classes.
        /// </remarks>
        protected abstract void GetDetails(IntPtr pDetails);

        /// <summary>
        /// Mixer control name
        /// </summary>
        public String Name => mixerControl.szName;

        /// <summary>
        /// Mixer control type
        /// </summary>
        public MixerControlType ControlType => mixerControl.dwControlType;

        /// <summary>
        /// Checks if the given MixerControlType is a boolean control type.
        /// </summary>
        /// <param name="controlType">The MixerControlType to be checked.</param>
        /// <returns>True if the <paramref name="controlType"/> is a boolean control type; otherwise, false.</returns>
        private static bool IsControlBoolean(MixerControlType controlType)
        {
            switch (controlType)
            {
                case MixerControlType.BooleanMeter:
                case MixerControlType.Boolean:
                case MixerControlType.Button:
                case MixerControlType.Loudness:
                case MixerControlType.Mono:
                case MixerControlType.Mute:
                case MixerControlType.OnOff:
                case MixerControlType.StereoEnhance:
                case MixerControlType.Mixer:
                case MixerControlType.MultipleSelect:
                case MixerControlType.Mux:
                case MixerControlType.SingleSelect:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Is this a boolean control
        /// </summary>
        public bool IsBoolean => IsControlBoolean(mixerControl.dwControlType);

        /// <summary>
        /// Checks if the given MixerControlType is a type of control list text.
        /// </summary>
        /// <param name="controlType">The MixerControlType to be checked.</param>
        /// <returns>True if the control type is Equalizer, Mixer, MultipleSelect, Mux, or SingleSelect; otherwise, false.</returns>
        private static bool IsControlListText(MixerControlType controlType)
        {
            switch (controlType)
            {
                case MixerControlType.Equalizer:
                case MixerControlType.Mixer:
                case MixerControlType.MultipleSelect:
                case MixerControlType.Mux:
                case MixerControlType.SingleSelect:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// True if this is a list text control
        /// </summary>
        public bool IsListText => IsControlListText(mixerControl.dwControlType);

        /// <summary>
        /// Checks if the given MixerControlType is signed.
        /// </summary>
        /// <param name="controlType">The type of mixer control to be checked.</param>
        /// <returns>True if the <paramref name="controlType"/> is a signed control; otherwise, false.</returns>
        /// <remarks>
        /// This method checks if the specified <paramref name="controlType"/> is one of the signed mixer control types, such as PeakMeter, SignedMeter, Signed, Decibels, Pan, QSoundPan, or Slider.
        /// If the <paramref name="controlType"/> matches any of the signed types, it returns true; otherwise, it returns false.
        /// </remarks>
        private static bool IsControlSigned(MixerControlType controlType)
        {
            switch (controlType)
            {
                case MixerControlType.PeakMeter:
                case MixerControlType.SignedMeter:
                case MixerControlType.Signed:
                case MixerControlType.Decibels:
                case MixerControlType.Pan:
                case MixerControlType.QSoundPan:
                case MixerControlType.Slider:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// True if this is a signed control
        /// </summary>
        public bool IsSigned => IsControlSigned(mixerControl.dwControlType);

        /// <summary>
        /// Determines if the given MixerControlType is of an unsigned type.
        /// </summary>
        /// <param name="controlType">The MixerControlType to be checked.</param>
        /// <returns>True if the <paramref name="controlType"/> is of an unsigned type; otherwise, false.</returns>
        private static bool IsControlUnsigned(MixerControlType controlType)
        {
            switch (controlType)
            {
                case MixerControlType.UnsignedMeter:
                case MixerControlType.Unsigned:
                case MixerControlType.Bass:
                case MixerControlType.Equalizer:
                case MixerControlType.Fader:
                case MixerControlType.Treble:
                case MixerControlType.Volume:
                case MixerControlType.MicroTime:
                case MixerControlType.MilliTime:
                case MixerControlType.Percent:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// True if this is an unsigned control
        /// </summary>
        public bool IsUnsigned => IsControlUnsigned(mixerControl.dwControlType);

        /// <summary>
        /// Checks if the given MixerControlType is custom.
        /// </summary>
        /// <param name="controlType">The MixerControlType to be checked.</param>
        /// <returns>True if the <paramref name="controlType"/> is custom; otherwise, false.</returns>
        private static bool IsControlCustom(MixerControlType controlType)
        {
            return controlType == MixerControlType.Custom;
        }

        /// <summary>
        /// True if this is a custom control
        /// </summary>
        public bool IsCustom => IsControlCustom(mixerControl.dwControlType);

        /// <summary>
        /// Returns a string representation of the object, including the Name and ControlType properties.
        /// </summary>
        /// <returns>A string containing the Name and ControlType properties of the object.</returns>
        public override string ToString() => $"{Name} {ControlType}";
    }
}
