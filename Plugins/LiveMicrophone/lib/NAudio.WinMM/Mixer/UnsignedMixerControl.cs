// created on 13/12/2002 at 22:04
using System;
using System.Runtime.InteropServices;
using NAudio.Utils;
using NAudio.Wave;

namespace NAudio.Mixer
{
	/// <summary>
	/// Represents an unsigned mixer control
	/// </summary>
	public class UnsignedMixerControl : MixerControl 
	{
		private MixerInterop.MIXERCONTROLDETAILS_UNSIGNED[] unsignedDetails;
		
		internal UnsignedMixerControl(MixerInterop.MIXERCONTROL mixerControl,IntPtr mixerHandle, MixerFlags mixerHandleType, int nChannels) 
		{
			this.mixerControl = mixerControl;
            this.mixerHandle = mixerHandle;
            this.mixerHandleType = mixerHandleType;
			this.nChannels = nChannels;
			this.mixerControlDetails = new MixerInterop.MIXERCONTROLDETAILS();
			GetControlDetails();
		}

        /// <summary>
        /// Retrieves details of the mixer control and populates the unsignedDetails array.
        /// </summary>
        /// <param name="pDetails">A pointer to the details of the mixer control.</param>
        /// <exception cref="ArgumentNullException">Thrown when the pointer to details is null.</exception>
        /// <remarks>
        /// This method retrieves the details of the mixer control using the provided pointer <paramref name="pDetails"/>.
        /// It populates the unsignedDetails array with the retrieved details for each channel.
        /// </remarks>
        protected override void GetDetails(IntPtr pDetails)
        {
            unsignedDetails = new MixerInterop.MIXERCONTROLDETAILS_UNSIGNED[nChannels];
            for (int channel = 0; channel < nChannels; channel++)
            {
                unsignedDetails[channel] = Marshal.PtrToStructure<MixerInterop.MIXERCONTROLDETAILS_UNSIGNED>(mixerControlDetails.paDetails);
            }
        }

		/// <summary>
		/// The control value
		/// </summary>
		public uint Value 
		{
			get 
			{
				GetControlDetails();
				return unsignedDetails[0].dwValue;
			}
			set 
			{
                int structSize = Marshal.SizeOf(unsignedDetails[0]);

                mixerControlDetails.paDetails = Marshal.AllocHGlobal(structSize * nChannels);
                for (int channel = 0; channel < nChannels; channel++)
                {
                    unsignedDetails[channel].dwValue = value;
                    long pointer = mixerControlDetails.paDetails.ToInt64() + (structSize * channel);                    
                    Marshal.StructureToPtr(unsignedDetails[channel], (IntPtr)pointer, false);
                }
				MmException.Try(MixerInterop.mixerSetControlDetails(mixerHandle, ref mixerControlDetails, MixerFlags.Value | mixerHandleType), "mixerSetControlDetails");
                Marshal.FreeHGlobal(mixerControlDetails.paDetails);
			}
		}
		
		/// <summary>
		/// The control's minimum value
		/// </summary>
		public UInt32 MinValue 
		{
			get 
			{
				return (uint) mixerControl.Bounds.minimum;
			}
		}

		/// <summary>
		/// The control's maximum value
		/// </summary>
		public UInt32 MaxValue 
		{
			get 
			{
				return (uint) mixerControl.Bounds.maximum;
			}
		}

        /// <summary>
        /// Value of the control represented as a percentage
        /// </summary>
        public double Percent
        {
            get
            {
                return 100.0 * (Value - MinValue) / (double)(MaxValue - MinValue);
            }
            set
            {
                Value = (uint)(MinValue + (value / 100.0) * (MaxValue - MinValue));
            }
        }

        /// <summary>
        /// Returns a formatted string representation of the object, including the percentage value.
        /// </summary>
        /// <returns>A string containing the base string representation followed by the percentage value.</returns>
        public override string ToString()
        {
            return String.Format("{0} {1}%", base.ToString(), Percent);
        }
	}
}
