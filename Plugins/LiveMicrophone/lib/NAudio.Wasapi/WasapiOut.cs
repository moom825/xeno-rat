using System;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Threading;
using System.Runtime.InteropServices;
using NAudio.Utils;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Support for playback using Wasapi
    /// </summary>
    public class WasapiOut : IWavePlayer, IWavePosition
    {
        private AudioClient audioClient;
        private readonly MMDevice mmDevice;
        private readonly AudioClientShareMode shareMode;
        private AudioRenderClient renderClient;
        private IWaveProvider sourceProvider;
        private int latencyMilliseconds;
        private int bufferFrameCount;
        private int bytesPerFrame;
        private readonly bool isUsingEventSync;
        private EventWaitHandle frameEventWaitHandle;
        private byte[] readBuffer;
        private volatile PlaybackState playbackState;
        private Thread playThread;
        private readonly SynchronizationContext syncContext;
        private bool dmoResamplerNeeded;
        
        /// <summary>
        /// Playback Stopped
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        /// <summary>
        /// WASAPI Out shared mode, default
        /// </summary>
        public WasapiOut() :
            this(GetDefaultAudioEndpoint(), AudioClientShareMode.Shared, true, 200)
        {

        }

        /// <summary>
        /// WASAPI Out using default audio endpoint
        /// </summary>
        /// <param name="shareMode">ShareMode - shared or exclusive</param>
        /// <param name="latency">Desired latency in milliseconds</param>
        public WasapiOut(AudioClientShareMode shareMode, int latency) :
            this(GetDefaultAudioEndpoint(), shareMode, true, latency)
        {

        }

        /// <summary>
        /// WASAPI Out using default audio endpoint
        /// </summary>
        /// <param name="shareMode">ShareMode - shared or exclusive</param>
        /// <param name="useEventSync">true if sync is done with event. false use sleep.</param>
        /// <param name="latency">Desired latency in milliseconds</param>
        public WasapiOut(AudioClientShareMode shareMode, bool useEventSync, int latency) :
            this(GetDefaultAudioEndpoint(), shareMode, useEventSync, latency)
        {

        }

        /// <summary>
        /// Creates a new WASAPI Output
        /// </summary>
        /// <param name="device">Device to use</param>
        /// <param name="shareMode"></param>
        /// <param name="useEventSync">true if sync is done with event. false use sleep.</param>
        /// <param name="latency">Desired latency in milliseconds</param>
        public WasapiOut(MMDevice device, AudioClientShareMode shareMode, bool useEventSync, int latency)
        {
            audioClient = device.AudioClient;
            mmDevice = device;
            this.shareMode = shareMode;
            isUsingEventSync = useEventSync;
            latencyMilliseconds = latency;
            syncContext = SynchronizationContext.Current;
            OutputWaveFormat = audioClient.MixFormat; // allow the user to query the default format for shared mode streams
        }

        /// <summary>
        /// Retrieves the default audio endpoint for the specified data flow and role.
        /// </summary>
        /// <exception cref="NotSupportedException">Thrown when the operating system version is less than 6, indicating that WASAPI is supported only on Windows Vista and above.</exception>
        /// <returns>The default audio endpoint for the specified data flow and role.</returns>
        /// <remarks>
        /// This method retrieves the default audio endpoint for the specified data flow and role using the Windows Audio Session API (WASAPI).
        /// If the operating system version is less than 6, a <see cref="NotSupportedException"/> is thrown, indicating that WASAPI is supported only on Windows Vista and above.
        /// </remarks>
        static MMDevice GetDefaultAudioEndpoint()
        {
            if (Environment.OSVersion.Version.Major < 6)
            {
                throw new NotSupportedException("WASAPI supported only on Windows Vista and above");
            }
            var enumerator = new MMDeviceEnumerator();
            return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
        }

        /// <summary>
        /// Plays the audio using the specified source provider and handles resampling if needed.
        /// </summary>
        /// <remarks>
        /// This method plays the audio using the specified source provider. If resampling is needed, it handles the resampling using the DMO stream.
        /// It fills the buffer with audio data and starts the audio client. It then continuously checks for available buffer space and fills the buffer accordingly.
        /// If an exception occurs during the process, it is caught and handled, and the playback is stopped.
        /// </remarks>
        /// <exception cref="Exception">Thrown if an error occurs during the playback process.</exception>
        private void PlayThread()
        {
            ResamplerDmoStream resamplerDmoStream = null;
            IWaveProvider playbackProvider = sourceProvider;
            Exception exception = null;
            try
            {
                if (dmoResamplerNeeded)
                {
                    resamplerDmoStream = new ResamplerDmoStream(sourceProvider, OutputWaveFormat);
                    playbackProvider = resamplerDmoStream;
                }
                // fill a whole buffer
                bufferFrameCount = audioClient.BufferSize;
                bytesPerFrame = OutputWaveFormat.Channels * OutputWaveFormat.BitsPerSample / 8;
                readBuffer = BufferHelpers.Ensure(readBuffer, bufferFrameCount * bytesPerFrame);
                FillBuffer(playbackProvider, bufferFrameCount);

                // Create WaitHandle for sync
                var waitHandles = new WaitHandle[] { frameEventWaitHandle };

                audioClient.Start();

                while (playbackState != PlaybackState.Stopped)
                {
                    // If using Event Sync, Wait for notification from AudioClient or Sleep half latency
                    if (isUsingEventSync)
                    {
                        WaitHandle.WaitAny(waitHandles, 3 * latencyMilliseconds, false);
                    }
                    else
                    {
                        Thread.Sleep(latencyMilliseconds / 2);
                    }

                    // If still playing
                    if (playbackState == PlaybackState.Playing)
                    {
                        // See how much buffer space is available.
                        int numFramesPadding;
                        if (isUsingEventSync)
                        {
                            // In exclusive mode, always ask the max = bufferFrameCount = audioClient.BufferSize
                            numFramesPadding = (shareMode == AudioClientShareMode.Shared) ? audioClient.CurrentPadding : 0;
                        }
                        else
                        {
                            numFramesPadding = audioClient.CurrentPadding;
                        }
                        int numFramesAvailable = bufferFrameCount - numFramesPadding;
                        if (numFramesAvailable > 10) // see https://naudio.codeplex.com/workitem/16363
                        {
                            FillBuffer(playbackProvider, numFramesAvailable);
                        }
                    }
                }
                Thread.Sleep(latencyMilliseconds / 2);
                audioClient.Stop();
                if (playbackState == PlaybackState.Stopped)
                {
                    audioClient.Reset();
                }
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                if (resamplerDmoStream != null)
                {
                    resamplerDmoStream.Dispose();
                }
                RaisePlaybackStopped(exception);
            }
        }

        /// <summary>
        /// Raises the <see cref="PlaybackStopped"/> event with the specified exception.
        /// </summary>
        /// <param name="e">The exception that caused the playback to stop.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="e"/> is null.</exception>
        /// <remarks>
        /// This method raises the <see cref="PlaybackStopped"/> event with the specified exception. If a synchronization context is available, the event is raised on the synchronization context; otherwise, it is raised on the current thread.
        /// </remarks>
        private void RaisePlaybackStopped(Exception e)
        {
            var handler = PlaybackStopped;
            if (handler != null)
            {
                if (syncContext == null)
                {
                    handler(this, new StoppedEventArgs(e));
                }
                else
                {
                    syncContext.Post(state => handler(this, new StoppedEventArgs(e)), null);
                }
            }
        }

        /// <summary>
        /// Fills the buffer with audio data from the specified playback provider.
        /// </summary>
        /// <param name="playbackProvider">The IWaveProvider from which to read audio data.</param>
        /// <param name="frameCount">The number of audio frames to read and fill into the buffer.</param>
        /// <remarks>
        /// This method fills the buffer with audio data from the specified <paramref name="playbackProvider"/>.
        /// It then releases the buffer after filling it with the audio data.
        /// If the <paramref name="playbackProvider"/> returns 0 on read, the playback state is set to Stopped.
        /// </remarks>
        private void FillBuffer(IWaveProvider playbackProvider, int frameCount)
        {
            var buffer = renderClient.GetBuffer(frameCount);
            var readLength = frameCount * bytesPerFrame;
            int read = playbackProvider.Read(readBuffer, 0, readLength);
            if (read == 0)
            {
                playbackState = PlaybackState.Stopped;
            }
            Marshal.Copy(readBuffer, 0, buffer, read);
            if (this.isUsingEventSync && this.shareMode == AudioClientShareMode.Exclusive)
            {
                renderClient.ReleaseBuffer(frameCount, AudioClientBufferFlags.None);
            }
            else
            {
                int actualFrameCount = read / bytesPerFrame;
                /*if (actualFrameCount != frameCount)
                {
                    Debug.WriteLine(String.Format("WASAPI wanted {0} frames, supplied {1}", frameCount, actualFrameCount ));
                }*/
                renderClient.ReleaseBuffer(actualFrameCount, AudioClientBufferFlags.None);
            }
        }

        /// <summary>
        /// Gets the fallback WaveFormat to be used in case the provided format is not supported, based on the supported formats by the audio device.
        /// </summary>
        /// <returns>
        /// The WaveFormat to be used as a fallback, based on the supported formats by the audio device.
        /// </returns>
        /// <remarks>
        /// This method first tries the provided sample rate, then the device's sample rate, and finally 44.1kHz and 48kHz if not already included in the list of sample rates to try.
        /// It also considers the provided channel count, the device's channel count, and 2 channels if not already included in the list of channel counts to try.
        /// Additionally, it includes the provided bit depth, 32-bit depth, 24-bit depth, and 16-bit depth if not already included in the list of bit depths to try.
        /// The method iterates through the combinations of sample rates, channel counts, and bit depths to find a supported WaveFormat by the audio device, and returns it as a fallback.
        /// If no supported format is found, a NotSupportedException is thrown with the message "Can't find a supported format to use".
        /// </remarks>
        private WaveFormat GetFallbackFormat()
        {
            var deviceSampleRate = audioClient.MixFormat.SampleRate;
            var deviceChannels = audioClient.MixFormat.Channels; // almost certain to be stereo

            // we are in exclusive mode
            // First priority is to try the sample rate you provided.
            var sampleRatesToTry = new List<int>() { OutputWaveFormat.SampleRate };
            // Second priority is to use the sample rate the device wants
            if (!sampleRatesToTry.Contains(deviceSampleRate)) sampleRatesToTry.Add(deviceSampleRate);
            // And if we've not already got 44.1 and 48kHz in the list, let's try them too
            if (!sampleRatesToTry.Contains(44100)) sampleRatesToTry.Add(44100);
            if (!sampleRatesToTry.Contains(48000)) sampleRatesToTry.Add(48000);

            var channelCountsToTry = new List<int>() { OutputWaveFormat.Channels };
            if (!channelCountsToTry.Contains(deviceChannels)) channelCountsToTry.Add(deviceChannels);
            if (!channelCountsToTry.Contains(2)) channelCountsToTry.Add(2);

            var bitDepthsToTry = new List<int>() { OutputWaveFormat.BitsPerSample };
            if (!bitDepthsToTry.Contains(32)) bitDepthsToTry.Add(32);
            if (!bitDepthsToTry.Contains(24)) bitDepthsToTry.Add(24);
            if (!bitDepthsToTry.Contains(16)) bitDepthsToTry.Add(16);

            foreach (var sampleRate in sampleRatesToTry)
            {
                foreach (var channelCount in channelCountsToTry)
                {
                    foreach (var bitDepth in bitDepthsToTry)
                    {
                        var format = new WaveFormatExtensible(sampleRate, bitDepth, channelCount);
                        if (audioClient.IsFormatSupported(shareMode, format))
                            return format;
                    }
                }
            }
            throw new NotSupportedException("Can't find a supported format to use");
        }

        /// <summary>
        /// Gets the current playback position in bytes.
        /// </summary>
        /// <returns>
        /// The current playback position in bytes.
        /// </returns>
        /// <remarks>
        /// This method retrieves the current playback position in bytes based on the current state of the audio playback. If the playback state is stopped, the position is 0. If the playback state is playing, the position is obtained from the audio clock client's adjusted position. If the playback state is paused, the position is obtained using the audio clock client's GetPosition method. The position is then calculated in bytes based on the output wave format's average bytes per second and the audio clock client's frequency.
        /// </remarks>
        public long GetPosition()
        {
            ulong pos;
            switch (playbackState)
            {
                case PlaybackState.Stopped:
                    return 0;
                case PlaybackState.Playing:
                    pos = audioClient.AudioClockClient.AdjustedPosition;
                    break;
                default: // PlaybackState.Paused
                    audioClient.AudioClockClient.GetPosition(out pos, out _);
                    break;
            }
            return ((long)pos * OutputWaveFormat.AverageBytesPerSecond) / (long)audioClient.AudioClockClient.Frequency;
        }

        /// <summary>
        /// Gets a <see cref="Wave.WaveFormat"/> instance indicating the format the hardware is using.
        /// </summary>
        public WaveFormat OutputWaveFormat { get; private set; }

        /// <summary>
        /// Starts playing the audio if the current playback state is not already playing.
        /// If the playback state is stopped, it starts a new thread to play the audio and sets the playback state to playing.
        /// If the playback state is not stopped, it sets the playback state to playing.
        /// </summary>
        public void Play()
        {
            if (playbackState != PlaybackState.Playing)
            {
                if (playbackState == PlaybackState.Stopped)
                {
                    playThread = new Thread(PlayThread);
                    playbackState = PlaybackState.Playing;
                    playThread.Start();                    
                }
                else
                {
                    playbackState = PlaybackState.Playing;
                }                
            }
        }

        /// <summary>
        /// Stops the playback if it is currently running.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the playback state is not <see cref="PlaybackState.Stopped"/>.</exception>
        /// <remarks>
        /// This method stops the playback if it is currently running by setting the <see cref="playbackState"/> to <see cref="PlaybackState.Stopped"/>.
        /// It also waits for the play thread to join and sets it to null.
        /// </remarks>
        public void Stop()
        {
            if (playbackState != PlaybackState.Stopped)
            {
                playbackState = PlaybackState.Stopped;
                playThread.Join();
                playThread = null;
            }
        }

        /// <summary>
        /// Pauses the playback if it is currently playing.
        /// </summary>
        /// <remarks>
        /// This method pauses the playback if the current state is set to <see cref="PlaybackState.Playing"/>.
        /// If the playback is not in the playing state, this method does nothing.
        /// </remarks>
        public void Pause()
        {
            if (playbackState == PlaybackState.Playing)
            {
                playbackState = PlaybackState.Paused;
            }            
        }

        /// <summary>
        /// Initializes the audio output with the specified <paramref name="waveProvider"/>.
        /// </summary>
        /// <param name="waveProvider">The wave provider to be used for audio output.</param>
        /// <remarks>
        /// This method initializes the audio output with the provided <paramref name="waveProvider"/>. It sets the output wave format based on the wave provider's wave format.
        /// If the <paramref name="shareMode"/> is set to Exclusive, it checks if the format is supported and performs necessary fallbacks if required.
        /// If using EventSync, the setup is specific with shareMode and latency settings.
        /// </remarks>
        public void Init(IWaveProvider waveProvider)
        {
            long latencyRefTimes = latencyMilliseconds * 10000;
            OutputWaveFormat = waveProvider.WaveFormat;

            // allow auto sample rate conversion - works for shared mode
            var flags = AudioClientStreamFlags.AutoConvertPcm | AudioClientStreamFlags.SrcDefaultQuality;
            sourceProvider = waveProvider;

            if (shareMode == AudioClientShareMode.Exclusive)
            {
                flags = AudioClientStreamFlags.None;
                if (!audioClient.IsFormatSupported(shareMode, OutputWaveFormat, out WaveFormatExtensible closestSampleRateFormat))
                {
                    // Use closesSampleRateFormat (in sharedMode, it equals usualy to the audioClient.MixFormat)
                    // See documentation : http://msdn.microsoft.com/en-us/library/ms678737(VS.85).aspx 
                    // They say : "In shared mode, the audio engine always supports the mix format"
                    // The MixFormat is more likely to be a WaveFormatExtensible.
                    if (closestSampleRateFormat == null)
                    {

                        OutputWaveFormat = GetFallbackFormat();
                    }
                    else
                    {
                        OutputWaveFormat = closestSampleRateFormat;
                    }

                    try
                    {
                        // just check that we can make it.
                        using (new ResamplerDmoStream(waveProvider, OutputWaveFormat))
                        {
                        }
                    }
                    catch (Exception)
                    {
                        // On Windows 10 some poorly coded drivers return a bad format in to closestSampleRateFormat
                        // In that case, try and fallback as if it provided no closest (e.g. force trying the mix format)
                        OutputWaveFormat = GetFallbackFormat();
                        using (new ResamplerDmoStream(waveProvider, OutputWaveFormat))
                        {
                        }
                    }
                    dmoResamplerNeeded = true;
                }
                else
                {
                    dmoResamplerNeeded = false;
                }
            }

            // If using EventSync, setup is specific with shareMode
            if (isUsingEventSync)
            {
                // Init Shared or Exclusive
                if (shareMode == AudioClientShareMode.Shared)
                {
                    // With EventCallBack and Shared, both latencies must be set to 0 (update - not sure this is true anymore)
                    // 
                    audioClient.Initialize(shareMode, AudioClientStreamFlags.EventCallback | flags, latencyRefTimes, 0,
                        OutputWaveFormat, Guid.Empty);

                    // Windows 10 returns 0 from stream latency, resulting in maxing out CPU usage later
                    var streamLatency = audioClient.StreamLatency;
                    if (streamLatency != 0)
                    {
                        // Get back the effective latency from AudioClient
                        latencyMilliseconds = (int)(streamLatency / 10000);
                    }
                }
                else
                {
                    try
                    {
                        // With EventCallBack and Exclusive, both latencies must equals
                        audioClient.Initialize(shareMode, AudioClientStreamFlags.EventCallback | flags, latencyRefTimes, latencyRefTimes,
                                            OutputWaveFormat, Guid.Empty);
                    }
                    catch (COMException ex)
                    {
                        // Starting with Windows 7, Initialize can return AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED for a render device.
                        // We should to initialize again.
                        if (ex.ErrorCode != ErrorCodes.AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED)
                            throw;

                        // Calculate the new latency.
                        long newLatencyRefTimes = (long)(10000000.0 /
                            (double)this.OutputWaveFormat.SampleRate *
                            (double)this.audioClient.BufferSize + 0.5);

                        this.audioClient.Dispose();
                        this.audioClient = this.mmDevice.AudioClient;
                        this.audioClient.Initialize(this.shareMode, AudioClientStreamFlags.EventCallback | flags,
                                            newLatencyRefTimes, newLatencyRefTimes, this.OutputWaveFormat, Guid.Empty);
                    }
                }

                // Create the Wait Event Handle
                frameEventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
                audioClient.SetEventHandle(frameEventWaitHandle.SafeWaitHandle.DangerousGetHandle());
            }
            else
            {
                // Normal setup for both sharedMode
                audioClient.Initialize(shareMode, flags, latencyRefTimes, 0,
                                    OutputWaveFormat, Guid.Empty);
            }

            // Get the RenderClient
            renderClient = audioClient.AudioRenderClient;
        }

        /// <summary>
        /// Playback State
        /// </summary>
        public PlaybackState PlaybackState
        {
            get { return playbackState; }
        }

        /// <summary>
        /// Volume
        /// </summary>
        public float Volume
        {
            get
            {
                return mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar;                                
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value", "Volume must be between 0.0 and 1.0");
                if (value > 1) throw new ArgumentOutOfRangeException("value", "Volume must be between 0.0 and 1.0");
                mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar = value;
            }
        }

        /// <summary>
        /// Retrieve the AudioStreamVolume object for this audio stream
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
                return audioClient.AudioStreamVolume;  
            }
        }

        /// <summary>
        /// Disposes the audio client and stops the rendering if it is currently running.
        /// </summary>
        /// <remarks>
        /// This method disposes the audio client and stops the rendering if it is currently running. It also sets the audio client and render client to null after disposal.
        /// </remarks>
        public void Dispose()
        {
            if (audioClient != null)
            {
                Stop();

                audioClient.Dispose();
                audioClient = null;
                renderClient = null;
            }
        }

#endregion
    }
}
