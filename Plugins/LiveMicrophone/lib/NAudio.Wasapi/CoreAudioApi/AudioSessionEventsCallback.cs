// -----------------------------------------
// milligan22963 - implemented to work with nAudio
// 12/2014
// -----------------------------------------

using System;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// AudioSessionEvents callback implementation
    /// </summary>
    public class AudioSessionEventsCallback : IAudioSessionEvents
    {
        private readonly IAudioSessionEventsHandler audioSessionEventsHandler;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="handler"></param>
        public AudioSessionEventsCallback(IAudioSessionEventsHandler handler)
        {
            audioSessionEventsHandler = handler;
        }

        /// <summary>
        /// Notifies the handler that the display name for the audio session has changed.
        /// </summary>
        /// <param name="displayName">The new display name for the audio session.</param>
        /// <param name="eventContext">A unique identifier for the context of the event.</param>
        /// <returns>Zero if the method succeeds; otherwise, an error code.</returns>
        public int OnDisplayNameChanged(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string displayName,
            [In] ref Guid eventContext)
        {
            audioSessionEventsHandler.OnDisplayNameChanged(displayName);

            return 0;
        }

        /// <summary>
        /// Notifies that the icon path has changed and triggers the associated event handler.
        /// </summary>
        /// <param name="iconPath">The new icon path.</param>
        /// <param name="eventContext">A reference to the event context.</param>
        /// <returns>Zero if successful.</returns>
        public int OnIconPathChanged(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string iconPath,
            [In] ref Guid eventContext)
        {
            audioSessionEventsHandler.OnIconPathChanged(iconPath);

            return 0;
        }

        /// <summary>
        /// Notifies the handler when the volume of the audio session changes and returns 0.
        /// </summary>
        /// <param name="volume">The new volume level of the audio session.</param>
        /// <param name="isMuted">Indicates whether the audio session is muted.</param>
        /// <param name="eventContext">A reference to the event context.</param>
        /// <returns>0 indicating successful notification.</returns>
        public int OnSimpleVolumeChanged(
            [In] [MarshalAs(UnmanagedType.R4)] float volume,
            [In] [MarshalAs(UnmanagedType.Bool)] bool isMuted,
            [In] ref Guid eventContext)
        {
            audioSessionEventsHandler.OnVolumeChanged(volume, isMuted);

            return 0;
        }

        /// <summary>
        /// Notifies the handler that the volume level of a channel in the audio session has changed.
        /// </summary>
        /// <param name="channelCount">The number of channels in the audio session.</param>
        /// <param name="newVolumes">A pointer to an array of floats representing the new volume levels for each channel.</param>
        /// <param name="channelIndex">The index of the channel whose volume has changed.</param>
        /// <param name="eventContext">A reference to a unique identifier for the volume change event.</param>
        /// <returns>Zero if successful.</returns>
        public int OnChannelVolumeChanged(
            [In] [MarshalAs(UnmanagedType.U4)] UInt32 channelCount,
            [In] [MarshalAs(UnmanagedType.SysInt)] IntPtr newVolumes, // Pointer to float array
            [In] [MarshalAs(UnmanagedType.U4)] UInt32 channelIndex,
            [In] ref Guid eventContext)
        {
            audioSessionEventsHandler.OnChannelVolumeChanged(channelCount, newVolumes, channelIndex);

            return 0;
        }

        /// <summary>
        /// Notifies the audio session events handler that the grouping parameter has changed.
        /// </summary>
        /// <param name="groupingId">The unique identifier of the grouping parameter that has changed.</param>
        /// <param name="eventContext">The unique identifier of the event context.</param>
        /// <returns>Zero if the operation is successful.</returns>
        /// <remarks>
        /// This method notifies the audio session events handler that the grouping parameter identified by <paramref name="groupingId"/> has changed.
        /// The <paramref name="eventContext"/> parameter specifies the event context.
        /// </remarks>
        public int OnGroupingParamChanged(
            [In] ref Guid groupingId,
            [In] ref Guid eventContext)
        {
            audioSessionEventsHandler.OnGroupingParamChanged(ref groupingId);

            return 0;
        }

        /// <summary>
        /// Notifies the handler when the audio session state changes.
        /// </summary>
        /// <param name="state">The new state of the audio session.</param>
        /// <returns>Always returns 0.</returns>
        public int OnStateChanged(
            [In] AudioSessionState state)
        {
            audioSessionEventsHandler.OnStateChanged(state);

            return 0;
        }

        /// <summary>
        /// Notifies the client that the audio session has been disconnected.
        /// </summary>
        /// <param name="disconnectReason">The reason for the audio session disconnection.</param>
        /// <returns>Always returns 0.</returns>
        /// <remarks>
        /// This method notifies the client that the audio session has been disconnected and triggers the corresponding event in the audio session events handler.
        /// </remarks>
        public int OnSessionDisconnected(
            [In] AudioSessionDisconnectReason disconnectReason)
        {
            audioSessionEventsHandler.OnSessionDisconnected(disconnectReason);

            return 0;
        }
    }
}
