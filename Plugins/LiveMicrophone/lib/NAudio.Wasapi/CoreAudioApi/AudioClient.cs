using System;
using NAudio.CoreAudioApi.Interfaces;
using System.Runtime.InteropServices;
using NAudio.Wave;
using System.Threading.Tasks;
using NAudio.Wasapi.CoreAudioApi;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Windows CoreAudio AudioClient
    /// </summary>
    public class AudioClient : IDisposable
    {
        private IAudioClient audioClientInterface;
        private WaveFormat mixFormat;
        private AudioRenderClient audioRenderClient;
        private AudioCaptureClient audioCaptureClient;
        private AudioClockClient audioClockClient;
        private AudioStreamVolume audioStreamVolume;
        private AudioClientShareMode shareMode;

        /// <summary>
        /// Activates an audio client asynchronously and returns the activated AudioClient.
        /// </summary>
        /// <param name="deviceInterfacePath">The device interface path for the audio client.</param>
        /// <param name="audioClientProperties">Optional properties for the audio client.</param>
        /// <returns>An instance of the activated AudioClient.</returns>
        /// <remarks>
        /// This method activates an audio client asynchronously using the provided device interface path and optional audio client properties.
        /// If <paramref name="audioClientProperties"/> is not null, it sets the client properties using the provided values.
        /// The method then initializes the audio client and returns the activated AudioClient instance.
        /// </remarks>
        public static async Task<AudioClient> ActivateAsync(string deviceInterfacePath, AudioClientProperties? audioClientProperties)
        {
            var icbh = new ActivateAudioInterfaceCompletionHandler(
                ac2 =>
                {

                    if (audioClientProperties != null)
                    {
                        IntPtr p = Marshal.AllocHGlobal(Marshal.SizeOf(audioClientProperties.Value));
                        try
                        {
                            // TODO: consider whether we can marshal this without the need for AllocHGlobal
                            Marshal.StructureToPtr(audioClientProperties.Value, p, false);
                            ac2.SetClientProperties(p);
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(p);

                        }
                    }

                    /*var wfx = new WaveFormat(44100, 16, 2);
                int hr = ac2.Initialize(AudioClientShareMode.Shared,
                               AudioClientStreamFlags.EventCallback | AudioClientStreamFlags.NoPersist,
                               10000000, 0, wfx, IntPtr.Zero);*/
                });
            var IID_IAudioClient2 = new Guid("726778CD-F60A-4eda-82DE-E47610CD78AA");
            NativeMethods.ActivateAudioInterfaceAsync(deviceInterfacePath, IID_IAudioClient2, IntPtr.Zero, icbh, out var activationOperation);
            var audioClient2 = await icbh;
            return new AudioClient((IAudioClient)audioClient2);
        }

        public AudioClient(IAudioClient audioClientInterface)
        {
            this.audioClientInterface = audioClientInterface;
        }

        /// <summary>
        /// Retrieves the stream format that the audio engine uses for its internal processing of shared-mode streams.
        /// Can be called before initialize
        /// </summary>
        public WaveFormat MixFormat
        {
            get
            {
                if (mixFormat == null)
                {
                    Marshal.ThrowExceptionForHR(audioClientInterface.GetMixFormat(out var waveFormatPointer));
                    var waveFormat = WaveFormat.MarshalFromPtr(waveFormatPointer);
                    Marshal.FreeCoTaskMem(waveFormatPointer);
                    mixFormat = waveFormat;
                }
                return mixFormat;
            }
        }

        /// <summary>
        /// Initializes the audio client with the specified parameters.
        /// </summary>
        /// <param name="shareMode">The share mode for the audio client.</param>
        /// <param name="streamFlags">The stream flags for the audio client.</param>
        /// <param name="bufferDuration">The buffer duration in 100-nanosecond units.</param>
        /// <param name="periodicity">The periodicity of the audio client in 100-nanosecond units.</param>
        /// <param name="waveFormat">The wave format for the audio client.</param>
        /// <param name="audioSessionGuid">The GUID of the audio session.</param>
        /// <exception cref="MarshalDirectiveException">Thrown when an HRESULT is not S_OK.</exception>
        public void Initialize(AudioClientShareMode shareMode,
            AudioClientStreamFlags streamFlags,
            long bufferDuration,
            long periodicity,
            WaveFormat waveFormat,
            Guid audioSessionGuid)
        {
            this.shareMode = shareMode;
            int hresult = audioClientInterface.Initialize(shareMode, streamFlags, bufferDuration, periodicity, waveFormat, ref audioSessionGuid);
            Marshal.ThrowExceptionForHR(hresult);
            // may have changed the mix format so reset it
            mixFormat = null;
        }

        /// <summary>
        /// Retrieves the size (maximum capacity) of the audio buffer associated with the endpoint. (must initialize first)
        /// </summary>
        public int BufferSize
        {
            get
            {
                Marshal.ThrowExceptionForHR(audioClientInterface.GetBufferSize(out uint bufferSize));
                return (int) bufferSize;
            }
        }

        /// <summary>
        /// Retrieves the maximum latency for the current stream and can be called any time after the stream has been initialized.
        /// </summary>
        public long StreamLatency => audioClientInterface.GetStreamLatency();

        /// <summary>
        /// Retrieves the number of frames of padding in the endpoint buffer (must initialize first)
        /// </summary>
        public int CurrentPadding
        {
            get
            {
                Marshal.ThrowExceptionForHR(audioClientInterface.GetCurrentPadding(out var currentPadding));
                return currentPadding;
            }
        }

        /// <summary>
        /// Retrieves the length of the periodic interval separating successive processing passes by the audio engine on the data in the endpoint buffer.
        /// (can be called before initialize)
        /// </summary>
        public long DefaultDevicePeriod
        {
            get
            {
                Marshal.ThrowExceptionForHR(audioClientInterface.GetDevicePeriod(out var defaultDevicePeriod, out _));
                return defaultDevicePeriod;
            }
        }

        /// <summary>
        /// Gets the minimum device period 
        /// (can be called before initialize)
        /// </summary>
        public long MinimumDevicePeriod
        {
            get
            {
                Marshal.ThrowExceptionForHR(audioClientInterface.GetDevicePeriod(out _, out var minimumDevicePeriod));
                return minimumDevicePeriod;
            }
        }

        // TODO: GetService:
        // IID_IAudioSessionControl
        // IID_IChannelAudioVolume
        // IID_ISimpleAudioVolume

        /// <summary>
        /// Returns the AudioStreamVolume service for this AudioClient.
        /// </summary>
        /// <remarks>
        /// This returns the AudioStreamVolume object ONLY for shared audio streams.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// This is thrown when an exclusive audio stream is being used.
        /// </exception>
        public AudioStreamVolume AudioStreamVolume
        {
            get
            {
                if (shareMode == AudioClientShareMode.Exclusive)
                {
                    throw new InvalidOperationException("AudioStreamVolume is ONLY supported for shared audio streams.");
                }
                if (audioStreamVolume == null)
                {
                    var audioStreamVolumeGuid = new Guid("93014887-242D-4068-8A15-CF5E93B90FE3");
                    Marshal.ThrowExceptionForHR(audioClientInterface.GetService(audioStreamVolumeGuid, out var audioStreamVolumeInterface));
                    audioStreamVolume = new AudioStreamVolume((IAudioStreamVolume)audioStreamVolumeInterface);
                }
                return audioStreamVolume;
            }
        }

        /// <summary>
        /// Gets the AudioClockClient service
        /// </summary>
        public AudioClockClient AudioClockClient
        {
            get
            {
                if (audioClockClient == null)
                {
                    var audioClockClientGuid = new Guid("CD63314F-3FBA-4a1b-812C-EF96358728E7");
                    Marshal.ThrowExceptionForHR(audioClientInterface.GetService(audioClockClientGuid, out var audioClockClientInterface));
                    audioClockClient = new AudioClockClient((IAudioClock)audioClockClientInterface);
                }
                return audioClockClient;
            }
        }
        
        /// <summary>
        /// Gets the AudioRenderClient service
        /// </summary>
        public AudioRenderClient AudioRenderClient
        {
            get
            {
                if (audioRenderClient == null)
                {
                    var audioRenderClientGuid = new Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2");
                    Marshal.ThrowExceptionForHR(audioClientInterface.GetService(audioRenderClientGuid, out var audioRenderClientInterface));
                    audioRenderClient = new AudioRenderClient((IAudioRenderClient)audioRenderClientInterface);
                }
                return audioRenderClient;
            }
        }

        /// <summary>
        /// Gets the AudioCaptureClient service
        /// </summary>
        public AudioCaptureClient AudioCaptureClient
        {
            get
            {
                if (audioCaptureClient == null)
                {
                    var audioCaptureClientGuid = new Guid("c8adbd64-e71e-48a0-a4de-185c395cd317");
                    Marshal.ThrowExceptionForHR(audioClientInterface.GetService(audioCaptureClientGuid, out var audioCaptureClientInterface));
                    audioCaptureClient = new AudioCaptureClient((IAudioCaptureClient)audioCaptureClientInterface);
                }
                return audioCaptureClient;
            }
        }

        /// <summary>
        /// Checks if the specified audio format is supported for the given share mode and returns the closest match format if not directly supported.
        /// </summary>
        /// <param name="shareMode">The share mode for the audio client.</param>
        /// <param name="desiredFormat">The desired audio format to be checked for support.</param>
        /// <param name="closestMatchFormat">When this method returns, contains the closest match format if the desired format is not directly supported; otherwise, null.</param>
        /// <returns>
        ///   <c>true</c> if the specified audio format is directly supported for the given share mode; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="NotSupportedException">Thrown when the HRESULT is not recognized.</exception>
        public bool IsFormatSupported(AudioClientShareMode shareMode,
            WaveFormat desiredFormat)
        {
            return IsFormatSupported(shareMode, desiredFormat, out _);
        }

        /// <summary>
        /// Allocates memory and returns a pointer to a pointer to the allocated memory.
        /// </summary>
        /// <returns>A pointer to a pointer to the allocated memory.</returns>
        private IntPtr GetPointerToPointer()
        {
            return Marshal.AllocHGlobal(Marshal.SizeOf<IntPtr>());
        }

        /// <summary>
        /// Determines if the specified output format is supported in shared mode
        /// </summary>
        /// <param name="shareMode">Share Mode</param>
        /// <param name="desiredFormat">Desired Format</param>
        /// <param name="closestMatchFormat">Output The closest match format.</param>
        /// <returns>True if the format is supported</returns>
        public bool IsFormatSupported(AudioClientShareMode shareMode, WaveFormat desiredFormat, out WaveFormatExtensible closestMatchFormat)
        {
            IntPtr pointerToPtr = GetPointerToPointer(); // IntPtr.Zero; // Marshal.AllocHGlobal(Marshal.SizeOf<WaveFormatExtensible>());
            closestMatchFormat = null;
            int hresult = audioClientInterface.IsFormatSupported(shareMode, desiredFormat, pointerToPtr);

            var closestMatchPtr = Marshal.PtrToStructure<IntPtr>(pointerToPtr);

            if (closestMatchPtr != IntPtr.Zero)
            {
                closestMatchFormat = Marshal.PtrToStructure<WaveFormatExtensible>(closestMatchPtr);
                Marshal.FreeCoTaskMem(closestMatchPtr);
            }
            Marshal.FreeHGlobal(pointerToPtr);
            // S_OK is 0, S_FALSE = 1
            if (hresult == 0)
            {

                // directly supported
                return true;
            }
            if (hresult == 1)
            {
                return false;
            }
            if (hresult == (int)AudioClientErrors.UnsupportedFormat)
            {
                // documentation is confusing as to what this flag means
                // https://docs.microsoft.com/en-us/windows/desktop/api/audioclient/nf-audioclient-iaudioclient-isformatsupported
                // "Succeeded but the specified format is not supported in exclusive mode."
                return false; // shareMode != AudioClientShareMode.Exclusive;
            }
            Marshal.ThrowExceptionForHR(hresult);
            // shouldn't get here
            throw new NotSupportedException("Unknown hresult " + hresult);
        }

        /// <summary>
        /// Starts the audio client interface.
        /// </summary>
        /// <remarks>
        /// This method initiates the audio client interface to begin processing audio data.
        /// </remarks>
        public void Start()
        {
            audioClientInterface.Start();
        }

        /// <summary>
        /// Stops the audio playback.
        /// </summary>
        /// <remarks>
        /// This method stops the audio playback by calling the Stop method of the audioClientInterface.
        /// </remarks>
        public void Stop()
        {
            audioClientInterface.Stop();
        }

        /// <summary>
        /// Sets the event handle for the audio client interface.
        /// </summary>
        /// <param name="eventWaitHandle">The handle to the event to be set.</param>
        public void SetEventHandle(IntPtr eventWaitHandle)
        {
            audioClientInterface.SetEventHandle(eventWaitHandle);
        }

        /// <summary>
        /// Resets the audio client interface.
        /// </summary>
        public void Reset()
        {
            audioClientInterface.Reset();
        }

        /// <summary>
        /// Performs the disposal of resources used by the audio client.
        /// </summary>
        /// <remarks>
        /// This method disposes of the audio clock client, audio render client, audio capture client, audio stream volume, and the audio client interface if they are not null.
        /// Additionally, it suppresses the finalization of the current object by the garbage collector.
        /// </remarks>
        public void Dispose()
        {
            if (audioClientInterface != null)
            {
                if (audioClockClient != null)
                {
                    audioClockClient.Dispose();
                    audioClockClient = null;
                }
                if (audioRenderClient != null)
                {
                    audioRenderClient.Dispose();
                    audioRenderClient = null;
                }
                if (audioCaptureClient != null)
                {
                    audioCaptureClient.Dispose();
                    audioCaptureClient = null;
                }
                if (audioStreamVolume != null)
                {
                    audioStreamVolume.Dispose();
                    audioStreamVolume = null;
                }
                Marshal.ReleaseComObject(audioClientInterface);
                audioClientInterface = null;
                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }
}
