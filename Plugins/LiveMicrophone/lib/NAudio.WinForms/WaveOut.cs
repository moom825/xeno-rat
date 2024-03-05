using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

namespace NAudio.Wave 
{
    /// <summary>
    /// Represents a wave out device
    /// </summary>
    public class WaveOut : IWavePlayer, IWavePosition
    {
        private IntPtr hWaveOut;
        private WaveOutBuffer[] buffers;
        private IWaveProvider waveStream;
        private volatile PlaybackState playbackState;
        private readonly WaveInterop.WaveCallback callback;
        private readonly WaveCallbackInfo callbackInfo;
        private readonly object waveOutLock;
        private int queuedBuffers;
        private readonly SynchronizationContext syncContext;

        /// <summary>
        /// Indicates playback has stopped automatically
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        /// <summary>
        /// Retrieves the capabilities of the specified audio output device.
        /// </summary>
        /// <param name="devNumber">The device number for which to retrieve the capabilities.</param>
        /// <returns>The capabilities of the audio output device specified by <paramref name="devNumber"/>.</returns>
        /// <exception cref="MmException">Thrown when an error occurs while retrieving the device capabilities.</exception>
        /// <remarks>
        /// This method retrieves the capabilities of the specified audio output device using the WaveOut API.
        /// It initializes a new instance of <see cref="WaveOutCapabilities"/> and populates it with the capabilities of the specified device.
        /// The method then returns the retrieved capabilities.
        /// </remarks>
        public static WaveOutCapabilities GetCapabilities(int devNumber)
        {
            var caps = new WaveOutCapabilities();
            var structSize = Marshal.SizeOf(caps);
            MmException.Try(WaveInterop.waveOutGetDevCaps((IntPtr)devNumber, out caps, structSize), "waveOutGetDevCaps");
            return caps;
        }

        /// <summary>
        /// Returns the number of Wave Out devices available in the system
        /// </summary>
        public static Int32 DeviceCount => WaveInterop.waveOutGetNumDevs();

        /// <summary>
        /// Gets or sets the desired latency in milliseconds
        /// Should be set before a call to Init
        /// </summary>
        public int DesiredLatency { get; set; }

        /// <summary>
        /// Gets or sets the number of buffers used
        /// Should be set before a call to Init
        /// </summary>
        public int NumberOfBuffers { get; set; }

        /// <summary>
        /// Gets or sets the device number
        /// Should be set before a call to Init
        /// This must be between -1 and <see>DeviceCount</see> - 1.
        /// -1 means stick to default device even default device is changed
        /// </summary>
        public int DeviceNumber { get; set; } = -1;


        /// <summary>
        /// Creates a default WaveOut device
        /// Will use window callbacks if called from a GUI thread, otherwise function
        /// callbacks
        /// </summary>
        public WaveOut()
            : this(SynchronizationContext.Current == null ? WaveCallbackInfo.FunctionCallback() : WaveCallbackInfo.NewWindow())
        {
        }

        /// <summary>
        /// Creates a WaveOut device using the specified window handle for callbacks
        /// </summary>
        /// <param name="windowHandle">A valid window handle</param>
        public WaveOut(IntPtr windowHandle)
            : this(WaveCallbackInfo.ExistingWindow(windowHandle))
        {

        }

        /// <summary>
        /// Opens a WaveOut device
        /// </summary>
        public WaveOut(WaveCallbackInfo callbackInfo)
        {
            syncContext = SynchronizationContext.Current;
            // set default values up
            DesiredLatency = 300;
            NumberOfBuffers = 2;

            callback = Callback;
            waveOutLock = new object();
            this.callbackInfo = callbackInfo;
            callbackInfo.Connect(callback);
        }

        /// <summary>
        /// Initializes the audio output with the specified wave provider.
        /// </summary>
        /// <param name="waveProvider">The wave provider to be used for audio output.</param>
        /// <exception cref="MmException">Thrown when an error occurs during the initialization process.</exception>
        /// <remarks>
        /// This method initializes the audio output with the provided wave provider and sets up the necessary buffers for playback.
        /// It calculates the buffer size based on the desired latency and number of buffers, and then opens the wave output device.
        /// After successful initialization, the audio output is ready for playback.
        /// </remarks>
        public void Init(IWaveProvider waveProvider)
        {
            waveStream = waveProvider;
            int bufferSize = waveProvider.WaveFormat.ConvertLatencyToByteSize((DesiredLatency + NumberOfBuffers - 1) / NumberOfBuffers);            

            MmResult result;
            lock (waveOutLock)
            {
                result = callbackInfo.WaveOutOpen(out hWaveOut, DeviceNumber, waveStream.WaveFormat, callback);
            }
            MmException.Try(result, "waveOutOpen");

            buffers = new WaveOutBuffer[NumberOfBuffers];
            playbackState = PlaybackState.Stopped;
            for (int n = 0; n < NumberOfBuffers; n++)
            {
                buffers[n] = new WaveOutBuffer(hWaveOut, bufferSize, waveStream, waveOutLock);
            }
        }

        /// <summary>
        /// Plays the audio if the playback state is stopped, or resumes playing if the state is paused.
        /// </summary>
        /// <remarks>
        /// If the <see cref="playbackState"/> is <see cref="PlaybackState.Stopped"/>, this method changes the state to <see cref="PlaybackState.Playing"/> and enqueues any available buffers for playback.
        /// If the <see cref="playbackState"/> is <see cref="PlaybackState.Paused"/>, this method enqueues any available buffers for playback, resumes playback, and changes the state to <see cref="PlaybackState.Playing"/>.
        /// </remarks>
        public void Play()
        {
            if (playbackState == PlaybackState.Stopped)
            {
                playbackState = PlaybackState.Playing;
                Debug.Assert(queuedBuffers == 0, "Buffers already queued on play");
                EnqueueBuffers();
            }
            else if (playbackState == PlaybackState.Paused)
            {
                EnqueueBuffers();
                Resume();
                playbackState = PlaybackState.Playing;
            }
        }

        /// <summary>
        /// Enqueues the available buffers for playback.
        /// </summary>
        /// <remarks>
        /// This method iterates through the available buffers and enqueues them for playback if they are not already in the queue.
        /// If a buffer is successfully enqueued, the count of queued buffers is incremented.
        /// If a buffer's 'OnDone' method returns false, the playback state is set to 'Stopped' and the iteration is terminated.
        /// </remarks>
        private void EnqueueBuffers()
        {
            for (int n = 0; n < NumberOfBuffers; n++)
            {
                if (!buffers[n].InQueue)
                {
                    if (buffers[n].OnDone())
                    {
                        Interlocked.Increment(ref queuedBuffers);
                    }
                    else
                    {
                        playbackState = PlaybackState.Stopped;
                        break;
                    }
                    //Debug.WriteLine(String.Format("Resume from Pause: Buffer [{0}] requeued", n));
                }
                else
                {
                    //Debug.WriteLine(String.Format("Resume from Pause: Buffer [{0}] already queued", n));
                }
            }
        }

        /// <summary>
        /// Pauses the playback if the current state is playing.
        /// </summary>
        /// <exception cref="MmException">Thrown when an error occurs during the pause operation.</exception>
        public void Pause()
        {
            if (playbackState == PlaybackState.Playing)
            {
                MmResult result;
                playbackState = PlaybackState.Paused; // set this here, to avoid a deadlock with some drivers
                lock (waveOutLock)
                {
                    result = WaveInterop.waveOutPause(hWaveOut);
                }
                if (result != MmResult.NoError)
                {
                    throw new MmException(result, "waveOutPause");
                }
            }
        }

        /// <summary>
        /// Resumes playback of the audio.
        /// </summary>
        /// <remarks>
        /// If the playback state is paused, this method resumes the audio playback using the waveOutRestart function from the WaveInterop class.
        /// If an error occurs during the resumption of playback, a MmException is thrown with the corresponding error message.
        /// </remarks>
        /// <exception cref="MmException">Thrown when an error occurs during the resumption of audio playback.</exception>
        public void Resume()
        {
            if (playbackState == PlaybackState.Paused)
            {
                MmResult result;
                lock (waveOutLock)
                {
                    result = WaveInterop.waveOutRestart(hWaveOut);
                }
                if (result != MmResult.NoError)
                {
                    throw new MmException(result, "waveOutRestart");
                }
                playbackState = PlaybackState.Playing;
            }
        }

        /// <summary>
        /// Stops the audio playback.
        /// </summary>
        /// <exception cref="MmException">Thrown when an error occurs during the audio playback stop operation.</exception>
        /// <remarks>
        /// If the current playback state is not already stopped, this method resets the audio output device and stops the playback.
        /// This method also raises the <see cref="PlaybackStopped"/> event if the audio playback was stopped successfully.
        /// </remarks>
        public void Stop()
        {
            if (playbackState != PlaybackState.Stopped)
            {

                // in the call to waveOutReset with function callbacks
                // some drivers will block here until OnDone is called
                // for every buffer
                playbackState = PlaybackState.Stopped; // set this here to avoid a problem with some drivers whereby 
                MmResult result;
                lock (waveOutLock)
                {
                    result = WaveInterop.waveOutReset(hWaveOut);
                }
                if (result != MmResult.NoError)
                {
                    throw new MmException(result, "waveOutReset");
                }

                // with function callbacks, waveOutReset will call OnDone,
                // and so PlaybackStopped must not be raised from the handler
                // we know playback has definitely stopped now, so raise callback
                if (callbackInfo.Strategy == WaveCallbackStrategy.FunctionCallback)
                {
                    RaisePlaybackStoppedEvent(null);
                }
            }
        }

        /// <summary>
        /// Gets the current position in bytes of the audio playback.
        /// </summary>
        /// <returns>The current position in bytes of the audio playback.</returns>
        public long GetPosition() => WaveOutUtils.GetPositionBytes(hWaveOut, waveOutLock);

        /// <summary>
        /// Gets a <see cref="Wave.WaveFormat"/> instance indicating the format the hardware is using.
        /// </summary>
        public WaveFormat OutputWaveFormat => waveStream.WaveFormat;

        /// <summary>
        /// Playback State
        /// </summary>
        public PlaybackState PlaybackState => playbackState;

        /// <summary>
        /// Volume for this device 1.0 is full scale
        /// </summary>
        public float Volume
        {
            get
            {
                return WaveOutUtils.GetWaveOutVolume(hWaveOut, waveOutLock);
            }
            set
            {
                WaveOutUtils.SetWaveOutVolume(value, hWaveOut, waveOutLock);
            }
        }

        /// <summary>
        /// Releases the unmanaged resources used by the WaveOut device and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        /// <remarks>
        /// This method stops the playback of audio using the WaveOut device and releases the unmanaged resources associated with it.
        /// If <paramref name="disposing"/> is true, it also releases the managed resources, such as audio buffers.
        /// </remarks>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// Closes the WaveOut device and disposes of buffers
        /// </summary>
        /// <param name="disposing">True if called from <see>Dispose</see></param>
        protected void Dispose(bool disposing)
        {
            Stop();

            if (disposing)
            {
                if (buffers != null)
                {
                    for (int n = 0; n < buffers.Length; n++)
                    {
                        if (buffers[n] != null)
                        {
                            buffers[n].Dispose();
                        }
                    }
                    buffers = null;
                }
            }

            lock (waveOutLock)
            {
                WaveInterop.waveOutClose(hWaveOut);
            }
            if (disposing)
            {
                callbackInfo.Disconnect();
            }
        }

        /// <summary>
        /// Finalizer. Only called when user forgets to call <see>Dispose</see>
        /// </summary>
        ~WaveOut()
        {
            System.Diagnostics.Debug.Assert(false, "WaveOut device was not closed");
            Dispose(false);
        }

        /// <summary>
        /// Callback method for the wave out function.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio output device associated with the callback.</param>
        /// <param name="uMsg">The message sent to the callback function.</param>
        /// <param name="dwInstance">User instance data specified with waveOutOpen.</param>
        /// <param name="wavhdr">Pointer to a WaveHeader structure that identifies the header of the completed waveform-audio data block.</param>
        /// <param name="dwReserved">Not used.</param>
        /// <remarks>
        /// This method is called when a waveform-audio output buffer is done.
        /// It checks if the playback state is playing and then processes the buffer.
        /// If an exception occurs during processing, it is caught and stored in the 'exception' variable.
        /// After processing, it checks if all buffers have been played and raises the playback stopped event if so.
        /// </remarks>
        private void Callback(IntPtr hWaveOut, WaveInterop.WaveMessage uMsg, IntPtr dwInstance, WaveHeader wavhdr, IntPtr dwReserved)
        {
            if (uMsg == WaveInterop.WaveMessage.WaveOutDone)
            {
                GCHandle hBuffer = (GCHandle)wavhdr.userData;
                WaveOutBuffer buffer = (WaveOutBuffer)hBuffer.Target;
                Interlocked.Decrement(ref queuedBuffers);
                Exception exception = null;
                // check that we're not here through pressing stop
                if (PlaybackState == PlaybackState.Playing)
                {
                    // to avoid deadlocks in Function callback mode,
                    // we lock round this whole thing, which will include the
                    // reading from the stream.
                    // this protects us from calling waveOutReset on another 
                    // thread while a WaveOutWrite is in progress
                    lock (waveOutLock) 
                    {
                        try
                        {
                            if (buffer.OnDone())
                            {
                                Interlocked.Increment(ref queuedBuffers);
                            }
                        }
                        catch (Exception e)
                        {
                            // one likely cause is soundcard being unplugged
                            exception = e;
                        }
                    }
                }
                if (queuedBuffers == 0)
                {
                    if (callbackInfo.Strategy == WaveCallbackStrategy.FunctionCallback && playbackState == Wave.PlaybackState.Stopped)
                    {
                        // the user has pressed stop
                        // DO NOT raise the playback stopped event from here
                        // since on the main thread we are still in the waveOutReset function
                        // Playback stopped will be raised elsewhere
                    }
                    else
                    {
                        playbackState = PlaybackState.Stopped; // set explicitly for when we reach the end of the audio
                        RaisePlaybackStoppedEvent(exception);
                    }
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="PlaybackStopped"/> event with the specified exception.
        /// </summary>
        /// <param name="e">The exception that caused the playback to stop.</param>
        /// <remarks>
        /// This method raises the <see cref="PlaybackStopped"/> event with the specified exception. If a synchronization context is available, the event is raised on the synchronization context; otherwise, it is raised on the current thread.
        /// </remarks>
        private void RaisePlaybackStoppedEvent(Exception e)
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
    }
}
