using NAudio.CoreAudioApi.Interfaces;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi
{
    internal class AudioSessionNotification : IAudioSessionNotification
    {
        private AudioSessionManager parent;

        internal AudioSessionNotification(AudioSessionManager parent)
        {
            this.parent = parent;
        }

        /// <summary>
        /// Fires an event when a new audio session is created and returns 0.
        /// </summary>
        /// <param name="newSession">The new audio session control object.</param>
        /// <returns>0 indicating successful execution.</returns>
        [PreserveSig]
        public int OnSessionCreated(IAudioSessionControl newSession)
        {
            parent.FireSessionCreated(newSession);
            return 0;
        }
    }
}
