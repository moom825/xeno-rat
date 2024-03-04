using System;
using System.Runtime.InteropServices;
using NAudio.Mixer;
using System.Threading;
using NAudio.CoreAudioApi;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Recording using waveIn api with event callbacks.
    /// Use this for recording in non-gui applications
    /// Events are raised as recorded buffers are made available
    /// </summary>
    public class WaveInEvent : IWaveIn
    {
        private readonly AutoResetEvent callbackEvent;
        private readonly SynchronizationContext syncContext;
        private IntPtr waveInHandle;
        private volatile CaptureState captureState;
        private WaveInBuffer[] buffers;

        /// <summary>
        /// Indicates recorded data is available 
        /// </summary>
        public event EventHandler<WaveInEventArgs> DataAvailable;

        /// <summary>
        /// Indicates that all recorded data has now been received.
        /// </summary>
        public event EventHandler<StoppedEventArgs> RecordingStopped;

        /// <summary>
        /// Prepares a Wave input device for recording
        /// </summary>
        public WaveInEvent()
        {
            callbackEvent = new AutoResetEvent(false);
            syncContext = SynchronizationContext.Current;
            DeviceNumber = 0;
            WaveFormat = new WaveFormat(8000, 16, 1);
            BufferMilliseconds = 100;
            NumberOfBuffers = 3;
            captureState = CaptureState.Stopped;
        }

        /// <summary>
        /// Returns the number of Wave In devices available in the system
        /// </summary>
        public static int DeviceCount => WaveInterop.waveInGetNumDevs();

        /// <summary>
        /// Retrieves the capabilities of the specified audio input device.
        /// </summary>
        /// <param name="devNumber">The device number of the audio input device.</param>
        /// <returns>The capabilities of the audio input device specified by <paramref name="devNumber"/>.</returns>
        /// <exception cref="MmException">Thrown when an error occurs while retrieving the device capabilities.</exception>
        /// <remarks>
        /// This method retrieves the capabilities of the audio input device specified by <paramref name="devNumber"/> using the WaveInterop.waveInGetDevCaps method.
        /// It initializes a new instance of WaveInCapabilities and retrieves the device capabilities into it.
        /// The method then returns the retrieved capabilities.
        /// </remarks>
        public static WaveInCapabilities GetCapabilities(int devNumber)
        {
            WaveInCapabilities caps = new WaveInCapabilities();
            int structSize = Marshal.SizeOf(caps);
            MmException.Try(WaveInterop.waveInGetDevCaps((IntPtr)devNumber, out caps, structSize), "waveInGetDevCaps");
            return caps;
        }

        /// <summary>
        /// Milliseconds for the buffer. Recommended value is 100ms
        /// </summary>
        public int BufferMilliseconds { get; set; }

        /// <summary>
        /// Number of Buffers to use (usually 2 or 3)
        /// </summary>
        public int NumberOfBuffers { get; set; }

        /// <summary>
        /// The device number to use
        /// </summary>
        public int DeviceNumber { get; set; }

        /// <summary>
        /// Creates audio input buffers for capturing audio data.
        /// </summary>
        /// <remarks>
        /// This method creates audio input buffers for capturing audio data. It calculates the buffer size based on the specified <see cref="BufferMilliseconds"/> and <see cref="WaveFormat"/>.
        /// The method then initializes the buffers using the calculated size and the <see cref="waveInHandle"/>.
        /// </remarks>
        private void CreateBuffers()
        {
            // Default to three buffers of 100ms each
            int bufferSize = BufferMilliseconds * WaveFormat.AverageBytesPerSecond / 1000;
            if (bufferSize % WaveFormat.BlockAlign != 0)
            {
                bufferSize -= bufferSize % WaveFormat.BlockAlign;
            }

            buffers = new WaveInBuffer[NumberOfBuffers];
            for (int n = 0; n < buffers.Length; n++)
            {
                buffers[n] = new WaveInBuffer(waveInHandle, bufferSize);
            }
        }

        /// <summary>
        /// Opens the wave input device and creates necessary buffers.
        /// </summary>
        /// <remarks>
        /// This method closes any previously opened wave input device and then opens the specified wave input device using the provided wave format and callback event.
        /// It then creates necessary buffers for capturing audio data from the device.
        /// </remarks>
        /// <exception cref="MmException">Thrown when an error occurs during the wave input device opening process.</exception>
        private void OpenWaveInDevice()
        {
            CloseWaveInDevice();
            MmResult result = WaveInterop.waveInOpenWindow(out waveInHandle, (IntPtr)DeviceNumber, WaveFormat,
                callbackEvent.SafeWaitHandle.DangerousGetHandle(), 
                IntPtr.Zero, WaveInterop.WaveInOutOpenFlags.CallbackEvent);
            MmException.Try(result, "waveInOpen");
            CreateBuffers();
        }

        /// <summary>
        /// Starts recording audio.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the recording is already in progress.</exception>
        public void StartRecording()
        {
            if (captureState != CaptureState.Stopped)
                throw new InvalidOperationException("Already recording");
            OpenWaveInDevice();
            MmException.Try(WaveInterop.waveInStart(waveInHandle), "waveInStart");
            captureState = CaptureState.Starting;
            ThreadPool.QueueUserWorkItem((state) => RecordThread(), null);
        }

        /// <summary>
        /// Records the thread and raises the recording stopped event.
        /// </summary>
        /// <remarks>
        /// This method executes the DoRecording method to perform the recording process. If any exception occurs during the recording, it is captured and stored in the <paramref name="exception"/> variable.
        /// After the recording process is completed, the capture state is set to Stopped, and the RaiseRecordingStoppedEvent method is called to raise the recording stopped event, passing the captured exception as a parameter if it occurred.
        /// </remarks>
        /// <exception cref="Exception">Thrown if an error occurs during the recording process.</exception>
        private void RecordThread()
        {
            Exception exception = null;
            try
            {
                DoRecording();
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                captureState = CaptureState.Stopped;
                RaiseRecordingStoppedEvent(exception);
            }
        }

        /// <summary>
        /// Initiates the audio recording process and handles buffer reusability and data availability events.
        /// </summary>
        /// <remarks>
        /// This method sets the capture state to <see cref="CaptureState.Capturing"/> and reuses the buffers for capturing audio data.
        /// It continuously checks for data availability and invokes the <see cref="DataAvailable"/> event with the captured audio data if available.
        /// The method continues capturing audio until the capture state is changed.
        /// </remarks>
        private void DoRecording()
        {
            captureState = CaptureState.Capturing;
            foreach (var buffer in buffers)
            {
                if (!buffer.InQueue)
                {
                    buffer.Reuse();
                }
            }
            while (captureState == CaptureState.Capturing)
            {
                if (callbackEvent.WaitOne())
                {
                    // requeue any buffers returned to us
                    foreach (var buffer in buffers)
                    {
                        if (buffer.Done)
                        {
                            if (buffer.BytesRecorded > 0)
                            {
                                DataAvailable?.Invoke(this, new WaveInEventArgs(buffer.Data, buffer.BytesRecorded));
                            }

                            if (captureState == CaptureState.Capturing)
                            {
                                buffer.Reuse();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Raises the RecordingStopped event with the specified exception.
        /// </summary>
        /// <param name="e">The exception that caused the recording to stop.</param>
        /// <exception cref="ArgumentNullException">Thrown when the event handler is null.</exception>
        /// <remarks>
        /// This method raises the RecordingStopped event with the specified exception. If a synchronization context is available, the event is raised on the synchronization context; otherwise, it is raised on the current thread.
        /// </remarks>
        private void RaiseRecordingStoppedEvent(Exception e)
        {
            var handler = RecordingStopped;
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
        /// Stops the audio recording if it is currently in progress.
        /// </summary>
        /// <remarks>
        /// If the <paramref name="captureState"/> is not <see cref="CaptureState.Stopped"/>, this method changes the <paramref name="captureState"/> to <see cref="CaptureState.Stopping"/>,
        /// stops the audio recording using the <see cref="WaveInterop.waveInStop"/> method, resets the audio input device using the <see cref="WaveInterop.waveInReset"/> method, and signals the thread to exit by setting the <see cref="callbackEvent"/>.
        /// </remarks>
        /// <exception cref="MmException">Thrown when an error occurs during the waveInStop or waveInReset operation.</exception>
        public void StopRecording()
        {
            if (captureState != CaptureState.Stopped)
            {
                captureState = CaptureState.Stopping;
                MmException.Try(WaveInterop.waveInStop(waveInHandle), "waveInStop");

                //Reset, triggering the buffers to be returned
                MmException.Try(WaveInterop.waveInReset(waveInHandle), "waveInReset");

                callbackEvent.Set(); // signal the thread to exit
            }
        }

        /// <summary>
        /// Retrieves the current input device position in bytes.
        /// </summary>
        /// <returns>The current position in bytes.</returns>
        /// <exception cref="Exception">Thrown when the retrieved position type is not in bytes.</exception>
        public long GetPosition()
        {
            MmTime mmTime = new MmTime();
            mmTime.wType = MmTime.TIME_BYTES; // request results in bytes, TODO: perhaps make this a little more flexible and support the other types?
            MmException.Try(WaveInterop.waveInGetPosition(waveInHandle, out mmTime, Marshal.SizeOf(mmTime)), "waveInGetPosition");

            if (mmTime.wType != MmTime.TIME_BYTES)
                throw new Exception(string.Format("waveInGetPosition: wType -> Expected {0}, Received {1}", MmTime.TIME_BYTES, mmTime.wType));

            return mmTime.cb;
        }

        /// <summary>
        /// WaveFormat we are recording in
        /// </summary>
        public WaveFormat WaveFormat { get; set; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <remarks>
        /// This method releases the unmanaged resources used by the object and optionally releases the managed resources.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (captureState != CaptureState.Stopped)
                    StopRecording();

                CloseWaveInDevice();
            }
        }

        /// <summary>
        /// Closes the wave input device and releases associated resources.
        /// </summary>
        /// <remarks>
        /// Some drivers may require a reset to properly release buffers. This method resets the wave input device using WaveInterop.waveInReset.
        /// It then disposes of any allocated buffers and closes the wave input device using WaveInterop.waveInClose.
        /// </remarks>
        private void CloseWaveInDevice()
        {
            // Some drivers need the reset to properly release buffers
            WaveInterop.waveInReset(waveInHandle);
            if (buffers != null)
            {
                for (int n = 0; n < buffers.Length; n++)
                {
                    buffers[n].Dispose();
                }
                buffers = null;
            }
            WaveInterop.waveInClose(waveInHandle);
            waveInHandle = IntPtr.Zero;
        }

        /// <summary>
        /// Retrieves the mixer line associated with the current wave input handle or device number.
        /// </summary>
        /// <returns>The <see cref="MixerLine"/> object representing the mixer line associated with the wave input handle or device number.</returns>
        /// <remarks>
        /// This method retrieves the mixer line associated with the current wave input handle or device number.
        /// If the <see cref="waveInHandle"/> is not zero, it creates a new <see cref="MixerLine"/> object using the wave input handle and <see cref="MixerFlags.WaveInHandle"/>.
        /// Otherwise, it creates a new <see cref="MixerLine"/> object using the device number and <see cref="MixerFlags.WaveIn"/>.
        /// </remarks>
        public MixerLine GetMixerLine()
        {
            // TODO use mixerGetID instead to see if this helps with XP
            MixerLine mixerLine;
            if (waveInHandle != IntPtr.Zero)
            {
                mixerLine = new MixerLine(waveInHandle, 0, MixerFlags.WaveInHandle);
            }
            else
            {
                mixerLine = new MixerLine((IntPtr)DeviceNumber, 0, MixerFlags.WaveIn);
            }
            return mixerLine;
        }

        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

