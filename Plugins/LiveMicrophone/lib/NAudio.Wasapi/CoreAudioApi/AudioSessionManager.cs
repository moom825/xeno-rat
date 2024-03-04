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
    /// AudioSessionManager
    /// 
    /// Designed to manage audio sessions and in particuar the
    /// SimpleAudioVolume interface to adjust a session volume
    /// </summary>
    public class AudioSessionManager
    {
        private readonly IAudioSessionManager audioSessionInterface;
        private readonly IAudioSessionManager2 audioSessionInterface2;
        private AudioSessionNotification audioSessionNotification;
        private SessionCollection sessions;

        private SimpleAudioVolume simpleAudioVolume;
        private AudioSessionControl audioSessionControl;

        /// <summary>
        /// Session created delegate
        /// </summary>
        public delegate void SessionCreatedDelegate(object sender, IAudioSessionControl newSession);
        
        /// <summary>
        /// Occurs when audio session has been added (for example run another program that use audio playback).
        /// </summary>
        public event SessionCreatedDelegate OnSessionCreated;

        internal AudioSessionManager(IAudioSessionManager audioSessionManager)
        {
            audioSessionInterface = audioSessionManager;
            audioSessionInterface2 = audioSessionManager as IAudioSessionManager2;

            RefreshSessions();
        }

        /// <summary>
        /// SimpleAudioVolume object
        /// for adjusting the volume for the user session
        /// </summary>
        public SimpleAudioVolume SimpleAudioVolume
        {
            get
            {
                if (simpleAudioVolume == null)
                {
                    audioSessionInterface.GetSimpleAudioVolume(Guid.Empty, 0, out var simpleAudioInterface);

                    simpleAudioVolume = new SimpleAudioVolume(simpleAudioInterface);
                }
                return simpleAudioVolume;
            }
        }

        /// <summary>
        /// AudioSessionControl object
        /// for registring for callbacks and other session information
        /// </summary>
        public AudioSessionControl AudioSessionControl
        {
            get
            {
                if (audioSessionControl == null)
                {
                    audioSessionInterface.GetAudioSessionControl(Guid.Empty, 0, out var audioSessionControlInterface);

                    audioSessionControl = new AudioSessionControl(audioSessionControlInterface);
                }
                return audioSessionControl;
            }
        }

        /// <summary>
        /// Fires an event when a new audio session is created.
        /// </summary>
        /// <param name="newSession">The newly created audio session control.</param>
        internal void FireSessionCreated(IAudioSessionControl newSession)
        {
            OnSessionCreated?.Invoke(this, newSession);
        }

        /// <summary>
        /// Refreshes the audio sessions and registers for notifications.
        /// </summary>
        /// <remarks>
        /// This method unregisters any existing notifications, retrieves the session enumerator, and creates a new session collection.
        /// It then registers for session notifications and throws an exception if any of the underlying COM operations fail.
        /// </remarks>
        /// <exception cref="COMException">Thrown when any underlying COM operation fails.</exception>
        public void RefreshSessions()
        {
            UnregisterNotifications();

            if (audioSessionInterface2 != null)
            {
                Marshal.ThrowExceptionForHR(audioSessionInterface2.GetSessionEnumerator(out var sessionEnum));
                sessions = new SessionCollection(sessionEnum);

                audioSessionNotification = new AudioSessionNotification(this);
                Marshal.ThrowExceptionForHR(audioSessionInterface2.RegisterSessionNotification(audioSessionNotification));
            }
        }

        /// <summary>
        /// Returns list of sessions of current device.
        /// </summary>
        public SessionCollection Sessions => sessions;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <remarks>
        /// This method unregisters any active notifications and suppresses the finalization of the current object by the garbage collector.
        /// </remarks>
        public void Dispose()
        {
            UnregisterNotifications();

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Unregisters the notifications for audio sessions and sets the sessions to null.
        /// </summary>
        /// <remarks>
        /// This method sets the <paramref name="sessions"/> to null and unregisters the audio session notification if <paramref name="audioSessionNotification"/> and <paramref name="audioSessionInterface2"/> are not null.
        /// If <paramref name="audioSessionNotification"/> and <paramref name="audioSessionInterface2"/> are not null, it calls the <see cref="Marshal.ThrowExceptionForHR(int)"/> method with the HRESULT returned by <see cref="IAudioSessionControl2.UnregisterSessionNotification(IAudioSessionEvents)"/>.
        /// </remarks>
        private void UnregisterNotifications()
        {
            sessions = null;

            if (audioSessionNotification != null && audioSessionInterface2 != null)
            {
                Marshal.ThrowExceptionForHR(
                    audioSessionInterface2.UnregisterSessionNotification(audioSessionNotification));
                audioSessionNotification = null;
            }
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~AudioSessionManager()
        {
            Dispose();
        }
    }
}
