using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Manages the AudioStreamVolume for the <see cref="AudioClient"/>.
    /// </summary>
    public class AudioStreamVolume : IDisposable
    {
        IAudioStreamVolume audioStreamVolumeInterface;

        internal AudioStreamVolume(IAudioStreamVolume audioStreamVolumeInterface)
        {
            this.audioStreamVolumeInterface = audioStreamVolumeInterface;
        }

        /// <summary>
        /// Checks if the provided channel index is within the valid range.
        /// </summary>
        /// <param name="channelIndex">The index of the channel to be checked.</param>
        /// <param name="parameter">The name of the parameter being checked.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the provided channel index is greater than or equal to the current count of channels.</exception>
        /// <remarks>
        /// This method checks if the provided channel index is within the valid range based on the current count of channels.
        /// If the channel index is greater than or equal to the current count of channels, an <see cref="ArgumentOutOfRangeException"/> is thrown with a message indicating that a valid channel index must be supplied.
        /// </remarks>
        private void CheckChannelIndex(int channelIndex, string parameter)
        {
            int channelCount = ChannelCount;
            if (channelIndex >= channelCount)
            {
                throw new ArgumentOutOfRangeException(parameter, "You must supply a valid channel index < current count of channels: " + channelCount.ToString());
            }
        }

        /// <summary>
        /// Retrieves the volume levels for all audio channels and returns an array of volume levels.
        /// </summary>
        /// <returns>An array containing the volume levels for all audio channels.</returns>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when an error occurs while retrieving the volume levels.</exception>
        /// <remarks>
        /// This method retrieves the number of audio channels using the <see cref="audioStreamVolumeInterface.GetChannelCount"/> method and then retrieves the volume levels for all channels using the <see cref="audioStreamVolumeInterface.GetAllVolumes"/> method.
        /// The retrieved volume levels are returned as an array of floats, where each element represents the volume level for a specific audio channel.
        /// </remarks>
        public float[] GetAllVolumes()
        {
            Marshal.ThrowExceptionForHR(audioStreamVolumeInterface.GetChannelCount(out var channels));
            var levels = new float[channels];
            Marshal.ThrowExceptionForHR(audioStreamVolumeInterface.GetAllVolumes(channels, levels));
            return levels;
        }

        /// <summary>
        /// Returns the current number of channels in this audio stream.
        /// </summary>
        public int ChannelCount
        {
            get
            {
                Marshal.ThrowExceptionForHR(audioStreamVolumeInterface.GetChannelCount(out var channels));
                unchecked
                {
                    return (int)channels;
                }
            }
        }

        /// <summary>
        /// Retrieves the volume level of the specified audio channel.
        /// </summary>
        /// <param name="channelIndex">The index of the audio channel for which to retrieve the volume level.</param>
        /// <returns>The volume level of the specified audio <paramref name="channelIndex"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="channelIndex"/> is invalid.</exception>
        /// <remarks>
        /// This method retrieves the volume level of the specified audio channel using the <c>audioStreamVolumeInterface</c>.
        /// It first checks the validity of the <paramref name="channelIndex"/> using the <c>CheckChannelIndex</c> method.
        /// The volume level is then retrieved using the <c>GetChannelVolume</c> method and returned.
        /// </remarks>
        public float GetChannelVolume(int channelIndex)
        {
            CheckChannelIndex(channelIndex, "channelIndex");

            uint index;
            unchecked 
            {
                index = (uint)channelIndex;
            }
            Marshal.ThrowExceptionForHR(audioStreamVolumeInterface.GetChannelVolume(index, out var level));
            return level;
        }

        /// <summary>
        /// Sets the volume levels for all channels in the audio stream.
        /// </summary>
        /// <param name="levels">An array of volume levels for each channel. Must contain a volume level for each channel in the audio stream.</param>
        /// <exception cref="ArgumentNullException">Thrown when the input array <paramref name="levels"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the length of the input array <paramref name="levels"/> does not match the number of channels in the audio stream, or when any volume level is outside the valid range of 0.0 to 1.0.</exception>
        /// <remarks>
        /// This method sets the volume levels for all channels in the audio stream. It throws exceptions for common problems such as null input array, mismatch in the number of channels, or invalid volume levels.
        /// </remarks>
        public void SetAllVolumes(float[] levels)
        {
            // Make friendly Net exceptions for common problems:
            int channelCount = ChannelCount;
            if (levels == null)
            {
                throw new ArgumentNullException(nameof(levels));
            }
            if (levels.Length != channelCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(levels),
                    String.Format(CultureInfo.InvariantCulture, "SetAllVolumes MUST be supplied with a volume level for ALL channels. The AudioStream has {0} channels and you supplied {1} channels.",
                                  channelCount, levels.Length));
            }
            for (int i = 0; i < levels.Length; i++)
            {
                float level = levels[i];
                if (level < 0.0f) throw new ArgumentOutOfRangeException(nameof(levels), "All volumes must be between 0.0 and 1.0. Invalid volume at index: " + i.ToString());
                if (level > 1.0f) throw new ArgumentOutOfRangeException(nameof(levels), "All volumes must be between 0.0 and 1.0. Invalid volume at index: " + i.ToString());
            }
            unchecked
            {
                Marshal.ThrowExceptionForHR(audioStreamVolumeInterface.SetAllVoumes((uint)channelCount, levels));
            }
        }

        /// <summary>
        /// Sets the volume level for the specified audio channel.
        /// </summary>
        /// <param name="index">The index of the audio channel for which the volume level is to be set.</param>
        /// <param name="level">The volume level to be set for the specified audio channel. Must be between 0.0 and 1.0.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the volume level is not within the range of 0.0 to 1.0.</exception>
        /// <remarks>
        /// This method sets the volume level for the specified audio channel using the <paramref name="index"/> and <paramref name="level"/> parameters.
        /// It first checks if the <paramref name="index"/> is valid by calling the <see cref="CheckChannelIndex"/> method.
        /// If the <paramref name="level"/> is not within the range of 0.0 to 1.0, an <see cref="ArgumentOutOfRangeException"/> is thrown with an appropriate message.
        /// The volume level is then set using the <see cref="audioStreamVolumeInterface.SetChannelVolume"/> method.
        /// </remarks>
        public void SetChannelVolume(int index, float level)
        {
            CheckChannelIndex(index, "index");

            if (level < 0.0f) throw new ArgumentOutOfRangeException(nameof(level), "Volume must be between 0.0 and 1.0");
            if (level > 1.0f) throw new ArgumentOutOfRangeException(nameof(level), "Volume must be between 0.0 and 1.0");
            unchecked
            {
                Marshal.ThrowExceptionForHR(audioStreamVolumeInterface.SetChannelVolume((uint)index, level));
            }
        }

        /// <summary>
        /// Releases the unmanaged resources used by the AudioStreamVolume object.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        /// <remarks>
        /// This method releases the unmanaged resources used by the AudioStreamVolume object if <paramref name="disposing"/> is true.
        /// It releases the audioStreamVolumeInterface by calling Marshal.ReleaseComObject if it is not null.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Release/cleanup objects during Dispose/finalization.
        /// </summary>
        /// <param name="disposing">True if disposing and false if being finalized.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (audioStreamVolumeInterface != null)
                {
                    // although GC would do this for us, we want it done now
                    Marshal.ReleaseComObject(audioStreamVolumeInterface);
                    audioStreamVolumeInterface = null;
                }
            }
        }

        #endregion
    }
}
