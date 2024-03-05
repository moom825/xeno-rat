using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Alternative WaveOut class, making use of the Event callback
    /// </summary>
    public class WaveOutEvent : IWavePlayer, IWavePosition
    {
        private readonly object waveOutLock;
        private readonly SynchronizationContext syncContext;
        private IntPtr hWaveOut; // WaveOut handle
        private WaveOutBuffer[] buffers;
        private IWaveProvider waveStream;
        private volatile PlaybackState playbackState;
        private AutoResetEvent callbackEvent;

        /// <summary>
        /// Indicates playback has stopped automatically
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

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
        /// Opens a WaveOut device
        /// </summary>
        public WaveOutEvent()
        {
            syncContext = SynchronizationContext.Current;
            if (syncContext != null &&
                ((syncContext.GetType().Name == "LegacyAspNetSynchronizationContext") ||
                (syncContext.GetType().Name == "AspNetSynchronizationContext")))
            {
                syncContext = null;
            }

            // set default values up
            DesiredLatency = 300;
            NumberOfBuffers = 2;

            waveOutLock = new object();
        }

        /// <summary>
        /// Initializes the WaveOut device with the specified wave provider.
        /// </summary>
        /// <param name="waveProvider">The wave provider to be initialized.</param>
        /// <exception cref="InvalidOperationException">Thrown when attempting to re-initialize during playback.</exception>
        /// <remarks>
        /// This method initializes the WaveOut device with the specified wave provider, allocating buffers and setting up the necessary event handling.
        /// If the playback state is not stopped, an <see cref="InvalidOperationException"/> is thrown, indicating that re-initialization during playback is not allowed.
        /// If the WaveOut device is already initialized, it is cleaned up and re-initialized with the new wave provider.
        /// </remarks>
        public void Init(IWaveProvider waveProvider)
        {
            if (playbackState != PlaybackState.Stopped)
            {
                throw new InvalidOperationException("Can't re-initialize during playback");
            }
            if (hWaveOut != IntPtr.Zero)
            {
                // normally we don't allow calling Init twice, but as experiment, see if we can clean up and go again
                // try to allow reuse of this waveOut device
                // n.b. risky if Playback thread has not exited
                DisposeBuffers();
                CloseWaveOut();
            }

            callbackEvent = new AutoResetEvent(false);

            waveStream = waveProvider;
            int bufferSize = waveProvider.WaveFormat.ConvertLatencyToByteSize((DesiredLatency + NumberOfBuffers - 1) / NumberOfBuffers);

            MmResult result;
            lock (waveOutLock)
            {
                result = WaveInterop.waveOutOpenWindow(out hWaveOut, (IntPtr)DeviceNumber, waveStream.WaveFormat, callbackEvent.SafeWaitHandle.DangerousGetHandle(), IntPtr.Zero, WaveInterop.WaveInOutOpenFlags.CallbackEvent);
            }
            MmException.Try(result, "waveOutOpen");

            buffers = new WaveOutBuffer[NumberOfBuffers];
            playbackState = PlaybackState.Stopped;
            for (var n = 0; n < NumberOfBuffers; n++)
            {
                buffers[n] = new WaveOutBuffer(hWaveOut, bufferSize, waveStream, waveOutLock);
            }
        }

        /// <summary>
        /// Plays the audio.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when <paramref name="buffers"/> or <paramref name="waveStream"/> is null. Must call Init first.</exception>
        /// <remarks>
        /// If the <paramref name="playbackState"/> is stopped, it sets the state to Playing and starts the playback thread using ThreadPool.QueueUserWorkItem.
        /// If the <paramref name="playbackState"/> is paused, it resumes the playback and starts the playback thread using ThreadPool.QueueUserWorkItem.
        /// </remarks>
        public void Play()
        {
            if (buffers == null || waveStream == null)
            {
                throw new InvalidOperationException("Must call Init first");
            }
            if (playbackState == PlaybackState.Stopped)
            {
                playbackState = PlaybackState.Playing;
                callbackEvent.Set(); // give the thread a kick
                ThreadPool.QueueUserWorkItem(state => PlaybackThread(), null);
            }
            else if (playbackState == PlaybackState.Paused)
            {
                Resume();
                callbackEvent.Set(); // give the thread a kick
            }
        }

        /// <summary>
        /// Executes the DoPlayback method in a separate thread and raises the PlaybackStopped event upon completion or in case of an exception.
        /// </summary>
        /// <remarks>
        /// This method encapsulates the logic for executing the DoPlayback method in a separate thread. Upon completion or in case of an exception, the method updates the playbackState to Stopped and raises the PlaybackStopped event, passing any caught exception as an argument.
        /// </remarks>
        private void PlaybackThread()
        {
            Exception exception = null;
            try
            {
                DoPlayback();
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                playbackState = PlaybackState.Stopped;
                // we're exiting our background thread
                RaisePlaybackStoppedEvent(exception);
            }
        }

        /// <summary>
        /// Performs the audio playback operation.
        /// </summary>
        /// <remarks>
        /// This method continuously checks the playback state and waits for the callback event with the desired latency.
        /// If the playback state is playing and the callback event times out, a warning message is logged.
        /// It also requeues any buffers that are returned and checks if all buffers have been queued, indicating the end of playback.
        /// If all buffers have been queued, the playback state is set to stopped and the callback event is set.
        /// </remarks>
        private void DoPlayback()
        {
            while (playbackState != PlaybackState.Stopped)
            {
                if (!callbackEvent.WaitOne(DesiredLatency))
                {
                    if (playbackState == PlaybackState.Playing)
                    {
                        Debug.WriteLine("WARNING: WaveOutEvent callback event timeout");
                    }
                }
                    
                
                // requeue any buffers returned to us
                if (playbackState == PlaybackState.Playing)
                {
                    int queued = 0;
                    foreach (var buffer in buffers)
                    {
                        if (buffer.InQueue || buffer.OnDone())
                        {
                            queued++;
                        }
                    }
                    if (queued == 0)
                    {
                        // we got to the end
                        playbackState = PlaybackState.Stopped;
                        callbackEvent.Set();
                    }
                }
            }
        }

        /// <summary>
        /// Pauses the audio playback if the current state is playing.
        /// </summary>
        /// <exception cref="MmException">Thrown when an error occurs while pausing the audio playback.</exception>
        /// <remarks>
        /// If the current playback state is playing, this method pauses the audio playback.
        /// It sets the playback state to paused to avoid a deadlock problem with some drivers and then pauses the audio output using the WaveInterop.waveOutPause method.
        /// If an error occurs during the pause operation, a MmException is thrown with details about the error.
        /// </remarks>
        public void Pause()
        {
            if (playbackState == PlaybackState.Playing)
            {
                MmResult result;
                playbackState = PlaybackState.Paused; // set this here to avoid a deadlock problem with some drivers
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
        /// Resumes playback if the current state is paused.
        /// </summary>
        /// <exception cref="MmException">Thrown when an error occurs while trying to resume playback.</exception>
        /// <remarks>
        /// If the current playback state is paused, this method resumes the playback by calling the waveOutRestart function from WaveInterop.
        /// The method locks the waveOutLock object to ensure thread safety while calling the waveOutRestart function.
        /// If an error occurs during the waveOutRestart call, a MmException is thrown with the corresponding error message.
        /// After successful resumption of playback, the playbackState is set to Playing.
        /// </remarks>
        private void Resume()
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
        /// Stops the playback.
        /// </summary>
        /// <exception cref="MmException">Thrown when an error occurs during the waveOutReset operation.</exception>
        /// <remarks>
        /// If the current playback state is not stopped, this method sets the playback state to stopped and resets the waveOut device.
        /// This method also signals the callback event to ensure that the thread exits.
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
                callbackEvent.Set(); // give the thread a kick, make sure we exit
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
            get => WaveOutUtils.GetWaveOutVolume(hWaveOut, waveOutLock);
            set => WaveOutUtils.SetWaveOutVolume(value, hWaveOut, waveOutLock);
        }

        /// <summary>
        /// Disposes the resources used by the WaveOut device.
        /// </summary>
        /// <param name="disposing">A boolean value indicating whether the method is being called from the <c>Dispose</c> method.</param>
        /// <remarks>
        /// This method stops the WaveOut device and disposes the buffers used by the device. If <paramref name="disposing"/> is <c>true</c>, it also closes the WaveOut device.
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
                DisposeBuffers();
            }

            CloseWaveOut();
        }

        /// <summary>
        /// Closes the wave output device and releases associated resources.
        /// </summary>
        /// <remarks>
        /// This method closes the wave output device and releases any associated resources.
        /// If the <see cref="callbackEvent"/> is not null, it is closed and set to null.
        /// The method then locks the <see cref="waveOutLock"/> and checks if the <see cref="hWaveOut"/> is not equal to <see cref="IntPtr.Zero"/>.
        /// If it is not, the <see cref="WaveInterop.waveOutClose"/> method is called to close the wave output device, and <see cref="hWaveOut"/> is set to <see cref="IntPtr.Zero"/>.
        /// </remarks>
        private void CloseWaveOut()
        {
            if (callbackEvent != null)
            {
                callbackEvent.Close();
                callbackEvent = null;
            }
            lock (waveOutLock)
            {
                if (hWaveOut != IntPtr.Zero)
                {
                    WaveInterop.waveOutClose(hWaveOut);
                    hWaveOut= IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Disposes all the buffers and sets them to null.
        /// </summary>
        /// <remarks>
        /// This method disposes all the buffers in the collection and sets the collection to null.
        /// </remarks>
        private void DisposeBuffers()
        {
            if (buffers != null)
            {
                foreach (var buffer in buffers)
                {
                    buffer.Dispose();
                }
                buffers = null;
            }
        }

        /// <summary>
        /// Finalizer. Only called when user forgets to call <see>Dispose</see>
        /// </summary>
        ~WaveOutEvent()
        {
            Dispose(false);
            Debug.Assert(false, "WaveOutEvent device was not closed");
        }

        /// <summary>
        /// Raises the PlaybackStopped event with the specified exception.
        /// </summary>
        /// <param name="e">The exception that caused the playback to stop.</param>
        /// <exception cref="ArgumentNullException">Thrown when the exception is null.</exception>
        /// <remarks>
        /// This method raises the PlaybackStopped event with the specified exception. If a synchronization context is available, the event is raised on the synchronization context; otherwise, it is raised on the current thread.
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
