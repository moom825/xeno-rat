using System;
using System.Runtime.InteropServices;
using System.Threading;
using NAudio.Mixer;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Allows recording using the Windows waveIn APIs
    /// Events are raised as recorded buffers are made available
    /// </summary>
    public class WaveIn : IWaveIn
    {
        private IntPtr waveInHandle;
        private volatile bool recording;
        private WaveInBuffer[] buffers;
        private readonly WaveInterop.WaveCallback callback;
        private WaveCallbackInfo callbackInfo;
        private readonly SynchronizationContext syncContext;
        private int lastReturnedBufferIndex;
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
        public WaveIn(): this(WaveCallbackInfo.NewWindow())
        {

        }

        /// <summary>
        /// Creates a WaveIn device using the specified window handle for callbacks
        /// </summary>
        /// <param name="windowHandle">A valid window handle</param>
        public WaveIn(IntPtr windowHandle)
            : this(WaveCallbackInfo.ExistingWindow(windowHandle))
        {

        }

        /// <summary>
        /// Prepares a Wave input device for recording
        /// </summary>
        public WaveIn(WaveCallbackInfo callbackInfo)
        {
            syncContext = SynchronizationContext.Current;
            if ((callbackInfo.Strategy == WaveCallbackStrategy.NewWindow || callbackInfo.Strategy == WaveCallbackStrategy.ExistingWindow) &&
                syncContext == null)
            {
                throw new InvalidOperationException("Use WaveInEvent to record on a background thread");
            }
            DeviceNumber = 0;
            WaveFormat = new WaveFormat(8000, 16, 1);
            BufferMilliseconds = 100;
            NumberOfBuffers = 3;
            callback = Callback;
            this.callbackInfo = callbackInfo;
            callbackInfo.Connect(callback);
        }

        /// <summary>
        /// Returns the number of Wave In devices available in the system
        /// </summary>
        public static int DeviceCount
        {
            get
            {
                return WaveInterop.waveInGetNumDevs();
            }
        }

        /// <summary>
        /// Retrieves the capabilities of the specified audio input device.
        /// </summary>
        /// <param name="devNumber">The device number of the audio input device.</param>
        /// <returns>The capabilities of the audio input device specified by <paramref name="devNumber"/>.</returns>
        /// <exception cref="MmException">Thrown when an error occurs while retrieving the device capabilities.</exception>
        public static WaveInCapabilities GetCapabilities(int devNumber)
        {
            var caps = new WaveInCapabilities();
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
        /// Creates audio buffers for capturing audio data.
        /// </summary>
        /// <remarks>
        /// This method creates audio buffers for capturing audio data. It calculates the buffer size based on the specified <see cref="BufferMilliseconds"/> and <see cref="WaveFormat"/>.
        /// The method then creates an array of <see cref="WaveInBuffer"/> objects and initializes each buffer with the calculated size.
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
        /// Callback method for handling wave input messages.
        /// </summary>
        /// <param name="waveInHandle">The handle to the input wave.</param>
        /// <param name="message">The wave message received.</param>
        /// <param name="userData">User data associated with the wave input.</param>
        /// <param name="waveHeader">The wave header associated with the input.</param>
        /// <param name="reserved">Reserved parameter.</param>
        /// <remarks>
        /// This method is called when a wave input message is received. If the message is WaveInData and recording is in progress, it processes the wave input buffer and raises the DataAvailable event.
        /// If an exception occurs while reusing the buffer, the recording is stopped and the RecordingStopped event is raised with the exception details.
        /// </remarks>
        private void Callback(IntPtr waveInHandle, WaveInterop.WaveMessage message, IntPtr userData, WaveHeader waveHeader, IntPtr reserved)
        {
            if (message == WaveInterop.WaveMessage.WaveInData)
            {
                if (recording)
                {
                    var hBuffer = (GCHandle)waveHeader.userData;
                    var buffer = (WaveInBuffer)hBuffer.Target;
                    if (buffer == null) return;

                    lastReturnedBufferIndex = Array.IndexOf(buffers, buffer);
                    RaiseDataAvailable(buffer);
                    try
                    {
                        buffer.Reuse();
                    }
                    catch (Exception e)
                    {
                        recording = false;
                        RaiseRecordingStopped(e);
                    }
                }

            }
        }

        /// <summary>
        /// Raises the DataAvailable event with the provided WaveInBuffer data.
        /// </summary>
        /// <param name="buffer">The WaveInBuffer containing the recorded data.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided buffer is null.</exception>
        /// <remarks>
        /// This method raises the DataAvailable event with the recorded data from the WaveInBuffer.
        /// The WaveInEventArgs object contains the recorded data and the number of bytes recorded.
        /// </remarks>
        private void RaiseDataAvailable(WaveInBuffer buffer)
        {
            DataAvailable?.Invoke(this, new WaveInEventArgs(buffer.Data, buffer.BytesRecorded));
        }

        /// <summary>
        /// Raises the RecordingStopped event with the specified exception.
        /// </summary>
        /// <param name="e">The exception that caused the recording to stop.</param>
        /// <exception cref="ArgumentNullException">Thrown when the exception <paramref name="e"/> is null.</exception>
        /// <remarks>
        /// This method raises the RecordingStopped event with the specified exception <paramref name="e"/>.
        /// If a synchronization context is available, the event is raised on the synchronization context; otherwise, it is raised on the current thread.
        /// </remarks>
        private void RaiseRecordingStopped(Exception e)
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
        /// Opens the wave input device and creates necessary buffers.
        /// </summary>
        /// <remarks>
        /// This method closes any previously opened wave input device, then attempts to open the specified wave input device using the provided <paramref name="DeviceNumber"/> and <paramref name="WaveFormat"/>.
        /// If successful, it creates necessary buffers for the opened wave input device.
        /// </remarks>
        /// <exception cref="MmException">Thrown when an error occurs during the wave input device opening process.</exception>
        private void OpenWaveInDevice()
        {
            CloseWaveInDevice();
            MmResult result = callbackInfo.WaveInOpen(out waveInHandle, DeviceNumber, WaveFormat, callback);
            MmException.Try(result, "waveInOpen");
            CreateBuffers();
        }

        /// <summary>
        /// Starts the audio recording process.
        /// </summary>
        /// <remarks>
        /// This method starts the audio recording process by opening the wave input device, enqueueing buffers, and invoking the waveInStart function from the WaveInterop class.
        /// If the recording is already in progress, an InvalidOperationException with the message "Already recording" is thrown.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown when the recording is already in progress.</exception>
        public void StartRecording()
        {
            if (recording)
            {
                throw new InvalidOperationException("Already recording");
            }
            OpenWaveInDevice();
            EnqueueBuffers();
            MmException.Try(WaveInterop.waveInStart(waveInHandle), "waveInStart");
            recording = true;
        }

        /// <summary>
        /// Enqueues the reusable buffers.
        /// </summary>
        /// <remarks>
        /// This method iterates through the list of buffers and enqueues the ones that are not already in the queue by calling their <see cref="Buffer.Reuse"/> method.
        /// </remarks>
        private void EnqueueBuffers()
        {
            foreach (var buffer in buffers)
            {
                if (!buffer.InQueue)
                {
                    buffer.Reuse();
                }
            }
        }

        /// <summary>
        /// Stops the recording if it is currently in progress.
        /// </summary>
        /// <exception cref="MmException">Thrown when an error occurs while stopping the recording.</exception>
        /// <remarks>
        /// If the recording is currently in progress, this method stops the recording and reports any remaining buffers in the correct order.
        /// It then raises the <see cref="DataAvailable"/> event for each completed buffer and the <see cref="RecordingStopped"/> event to indicate that the recording has stopped.
        /// If no recording is in progress, this method does nothing.
        /// </remarks>
        public void StopRecording()
        {
            if (recording)
            {
                recording = false;
                MmException.Try(WaveInterop.waveInStop(waveInHandle), "waveInStop");
                // report the last buffers, sometimes more than one, so taking care to report them in the right order
                for (int n = 0; n < buffers.Length; n++)
                {
                    int index = (n + lastReturnedBufferIndex + 1) % buffers.Length;
                    var buffer = buffers[index];
                    if (buffer.Done)
                    {
                        RaiseDataAvailable(buffer);
                    }
                }
                RaiseRecordingStopped(null);
            }
            //MmException.Try(WaveInterop.waveInReset(waveInHandle), "waveInReset");      
            // Don't actually close yet so we get the last buffer
        }

        /// <summary>
        /// Retrieves the current input position of the audio device in bytes.
        /// </summary>
        /// <returns>The current input position of the audio device in bytes.</returns>
        /// <exception cref="Exception">Thrown when the retrieved time type does not match the expected type.</exception>
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
        /// This method disposes the resources used by the current instance. It is called by the public <see cref="Dispose()"/> method and the <see cref="GC.SuppressFinalize(object)"/> method.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (recording)
                    StopRecording();
                CloseWaveInDevice();
                if (callbackInfo != null)
                {
                    callbackInfo.Disconnect();
                    callbackInfo = null;
                }
            }
        }

        /// <summary>
        /// Closes the wave input device and releases associated resources.
        /// </summary>
        /// <remarks>
        /// This method first checks if the <see cref="waveInHandle"/> is not equal to <see cref="IntPtr.Zero"/>, and if so, it returns without performing any further actions.
        /// If the <see cref="waveInHandle"/> is not zero, it resets the wave input device using <see cref="WaveInterop.waveInReset(IntPtr)"/> to properly release buffers.
        /// It then disposes of each buffer in the <see cref="buffers"/> array, if it is not null, and sets the array to null.
        /// Finally, it closes the wave input device using <see cref="WaveInterop.waveInClose(IntPtr)"/> and sets the <see cref="waveInHandle"/> to <see cref="IntPtr.Zero"/>.
        /// </remarks>
        private void CloseWaveInDevice()
        {
            if (waveInHandle == IntPtr.Zero) return;
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
        /// Gets the mixer line associated with the current wave input handle or device number.
        /// </summary>
        /// <returns>The <see cref="MixerLine"/> object representing the mixer line associated with the current wave input handle or device number.</returns>
        /// <remarks>
        /// This method retrieves the mixer line associated with the current wave input handle or device number. If the wave input handle is not IntPtr.Zero, it creates a new <see cref="MixerLine"/> object using the wave input handle and MixerFlags.WaveInHandle.
        /// If the wave input handle is IntPtr.Zero, it creates a new <see cref="MixerLine"/> object using the device number and MixerFlags.WaveIn.
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
