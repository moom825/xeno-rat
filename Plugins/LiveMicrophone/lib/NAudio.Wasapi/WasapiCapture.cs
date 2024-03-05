using System;
using System.Threading;
using System.Runtime.InteropServices;
using NAudio.Wave;

// for consistency this should be in NAudio.Wave namespace, but left as it is for backwards compatibility
// ReSharper disable once CheckNamespace
namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Audio Capture using Wasapi
    /// See http://msdn.microsoft.com/en-us/library/dd370800%28VS.85%29.aspx
    /// </summary>
    public class WasapiCapture : IWaveIn
    {
        private const long ReftimesPerSec = 10000000;
        private const long ReftimesPerMillisec = 10000;
        private volatile CaptureState captureState;
        private byte[] recordBuffer;
        private Thread captureThread;
        private AudioClient audioClient;
        private int bytesPerFrame;
        private WaveFormat waveFormat;
        private bool initialized;
        private readonly SynchronizationContext syncContext;
        private readonly bool isUsingEventSync;
        private EventWaitHandle frameEventWaitHandle;
        private readonly int audioBufferMillisecondsLength;

        /// <summary>
        /// Indicates recorded data is available 
        /// </summary>
        public event EventHandler<WaveInEventArgs> DataAvailable;

        /// <summary>
        /// Indicates that all recorded data has now been received.
        /// </summary>
        public event EventHandler<StoppedEventArgs> RecordingStopped;

        /// <summary>
        /// Initialises a new instance of the WASAPI capture class
        /// </summary>
        public WasapiCapture() : 
            this(GetDefaultCaptureDevice())
        {
        }

        /// <summary>
        /// Initialises a new instance of the WASAPI capture class
        /// </summary>
        /// <param name="captureDevice">Capture device to use</param>
        public WasapiCapture(MMDevice captureDevice)
            : this(captureDevice, false)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WasapiCapture"/> class.
        /// </summary>
        /// <param name="captureDevice">The capture device.</param>
        /// <param name="useEventSync">true if sync is done with event. false use sleep.</param>
        public WasapiCapture(MMDevice captureDevice, bool useEventSync) 
            : this(captureDevice, useEventSync, 100)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WasapiCapture" /> class.
        /// </summary>
        /// <param name="captureDevice">The capture device.</param>
        /// <param name="useEventSync">true if sync is done with event. false use sleep.</param>
        /// <param name="audioBufferMillisecondsLength">Length of the audio buffer in milliseconds. A lower value means lower latency but increased CPU usage.</param>
        public WasapiCapture(MMDevice captureDevice, bool useEventSync, int audioBufferMillisecondsLength)
        {
            syncContext = SynchronizationContext.Current;
            audioClient = captureDevice.AudioClient;
            ShareMode = AudioClientShareMode.Shared;
            isUsingEventSync = useEventSync;
            this.audioBufferMillisecondsLength = audioBufferMillisecondsLength;

            waveFormat = audioClient.MixFormat;

        }

        /// <summary>
        /// Share Mode - set before calling StartRecording
        /// </summary>
        public AudioClientShareMode ShareMode { get; set; }

        /// <summary>
        /// Current Capturing State
        /// </summary>
        public CaptureState CaptureState {  get { return captureState; } }

        /// <summary>
        /// Capturing wave format
        /// </summary>
        public virtual WaveFormat WaveFormat 
        {
            get
            {
                // for convenience, return a WAVEFORMATEX, instead of the real
                // WAVEFORMATEXTENSIBLE being used
                return waveFormat.AsStandardWaveFormat();
            }
            set { waveFormat = value; }
        }

        /// <summary>
        /// Retrieves the default audio capture device.
        /// </summary>
        /// <returns>The default audio capture device.</returns>
        /// <remarks>
        /// This method retrieves the default audio capture device using the MMDeviceEnumerator class.
        /// It then returns the default audio endpoint for capture with the specified data flow and role.
        /// </remarks>
        public static MMDevice GetDefaultCaptureDevice()
        {
            var devices = new MMDeviceEnumerator();
            return devices.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
        }

        /// <summary>
        /// Initializes the capture device for audio recording.
        /// </summary>
        /// <remarks>
        /// This method initializes the capture device for audio recording. It sets up the audio client with the specified parameters such as share mode, stream flags, requested duration, wave format, and event handle. If using EventSync, the setup is specific with share mode and latencies. The method also initializes the record buffer for storing the recorded audio data.
        /// </remarks>
        private void InitializeCaptureDevice()
        {
            if (initialized)
                return;

            long requestedDuration = ReftimesPerMillisec * audioBufferMillisecondsLength;

            var streamFlags = GetAudioClientStreamFlags();

            // If using EventSync, setup is specific with shareMode
            if (isUsingEventSync)
            {
                // Init Shared or Exclusive
                if (ShareMode == AudioClientShareMode.Shared)
                {
                    // With EventCallBack and Shared, both latencies must be set to 0
                    audioClient.Initialize(ShareMode, AudioClientStreamFlags.EventCallback | streamFlags, requestedDuration, 0,
                        waveFormat, Guid.Empty);
                }
                else
                {
                    // With EventCallBack and Exclusive, both latencies must equals
                    audioClient.Initialize(ShareMode, AudioClientStreamFlags.EventCallback | streamFlags, requestedDuration, requestedDuration,
                                        waveFormat, Guid.Empty);
                }

                // Create the Wait Event Handle
                frameEventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
                audioClient.SetEventHandle(frameEventWaitHandle.SafeWaitHandle.DangerousGetHandle());
            }
            else
            {
                // Normal setup for both sharedMode
                audioClient.Initialize(ShareMode,
                streamFlags,
                requestedDuration,
                0,
                waveFormat,
                Guid.Empty);
            }

            int bufferFrameCount = audioClient.BufferSize;
            bytesPerFrame = waveFormat.Channels * waveFormat.BitsPerSample / 8;
            recordBuffer = new byte[bufferFrameCount * bytesPerFrame];
            
            //Debug.WriteLine(string.Format("record buffer size = {0}", this.recordBuffer.Length));

            initialized = true;
        }

        /// <summary>
        /// Gets the audio client stream flags for the audio client stream.
        /// </summary>
        /// <returns>The audio client stream flags, including the flag to enable auto-conversion of PCM and the default quality flag for the sample rate converter.</returns>
        protected virtual AudioClientStreamFlags GetAudioClientStreamFlags()
        {
            // enable auto-convert PCM
            return AudioClientStreamFlags.AutoConvertPcm | AudioClientStreamFlags.SrcDefaultQuality;
        }

        /// <summary>
        /// Starts the recording process.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when a previous recording is still in progress.</exception>
        /// <remarks>
        /// This method initiates the recording process by setting the capture state to <see cref="CaptureState.Starting"/>.
        /// It then initializes the capture device and starts a new thread to handle the recording using the <see cref="CaptureThread"/> method.
        /// </remarks>
        public void StartRecording()
        {
            if (captureState != CaptureState.Stopped)
            {
                throw new InvalidOperationException("Previous recording still in progress");
            }
            captureState = CaptureState.Starting;
            InitializeCaptureDevice();
            captureThread = new Thread(() => CaptureThread(audioClient));
            captureThread.Start();
        }

        /// <summary>
        /// Stops the recording if it is currently in progress.
        /// </summary>
        /// <remarks>
        /// This method changes the capture state to 'Stopping' if the current capture state is not already 'Stopped'.
        /// </remarks>
        public void StopRecording()
        {
            if (captureState != CaptureState.Stopped)
                captureState = CaptureState.Stopping;
        }

        /// <summary>
        /// Captures audio using the provided AudioClient and raises an event when recording is stopped.
        /// </summary>
        /// <param name="client">The AudioClient used for capturing audio.</param>
        /// <remarks>
        /// This method captures audio using the provided <paramref name="client"/>. It handles any exceptions that may occur during the recording process and raises a recording stopped event at the end.
        /// </remarks>
        /// <exception cref="Exception">Thrown if an exception occurs during the recording process.</exception>
        private void CaptureThread(AudioClient client)
        {
            Exception exception = null;
            try
            {
                DoRecording(client);
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                client.Stop();
                // don't dispose - the AudioClient only gets disposed when WasapiCapture is disposed
            }
            captureThread = null;
            captureState = CaptureState.Stopped;
            RaiseRecordingStopped(exception);
        }

        /// <summary>
        /// Records audio using the provided <paramref name="client"/> and manages the capture state.
        /// </summary>
        /// <param name="client">The audio client used for recording.</param>
        /// <remarks>
        /// This method calculates the duration of the allocated buffer, sets sleep and wait times, and starts capturing audio using the provided <paramref name="client"/>.
        /// It then enters a loop to continuously capture audio packets while the capture state is set to Capturing.
        /// The method handles different synchronization methods based on the value of <paramref name="isUsingEventSync"/>.
        /// </remarks>
        private void DoRecording(AudioClient client)
        {
            //Debug.WriteLine(String.Format("Client buffer frame count: {0}", client.BufferSize));
            int bufferFrameCount = client.BufferSize;

            // Calculate the actual duration of the allocated buffer.
            long actualDuration = (long)((double)ReftimesPerSec *
                             bufferFrameCount / waveFormat.SampleRate);
            int sleepMilliseconds = (int)(actualDuration / ReftimesPerMillisec / 2);
            int waitMilliseconds = (int)(3 * actualDuration / ReftimesPerMillisec);

            var capture = client.AudioCaptureClient;
            client.Start();
            // avoid race condition where we stop immediately after starting
            if (captureState == CaptureState.Starting)
            {
                captureState = CaptureState.Capturing;
            }
            while (captureState == CaptureState.Capturing)
            {
                if (isUsingEventSync)
                {
                    frameEventWaitHandle.WaitOne(waitMilliseconds, false);
                }
                else
                {
                    Thread.Sleep(sleepMilliseconds);
                }
                if (captureState != CaptureState.Capturing)
                    break;

                // If still recording
                ReadNextPacket(capture);
            }
        }

        /// <summary>
        /// Raises the RecordingStopped event with the specified exception.
        /// </summary>
        /// <param name="e">The exception that caused the recording to stop.</param>
        /// <remarks>
        /// This method raises the RecordingStopped event with the specified exception. If a synchronization context is available, the event is raised on the synchronization context; otherwise, it is raised on the current thread.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="e"/> is null.</exception>
        private void RaiseRecordingStopped(Exception e)
        {
            var handler = RecordingStopped;
            if (handler == null) return;
            if (syncContext == null)
            {
                handler(this, new StoppedEventArgs(e));
            }
            else
            {
                syncContext.Post(state => handler(this, new StoppedEventArgs(e)), null);
            }
        }

        /// <summary>
        /// Reads the next packet from the AudioCaptureClient and invokes the DataAvailable event with the captured audio data.
        /// </summary>
        /// <param name="capture">The AudioCaptureClient instance to read the packet from.</param>
        /// <remarks>
        /// This method reads the next packet from the specified AudioCaptureClient. It retrieves the packet size and iteratively processes the packet until no more data is available.
        /// The method then invokes the DataAvailable event with the captured audio data in a WaveInEventArgs object.
        /// </remarks>
        private void ReadNextPacket(AudioCaptureClient capture)
        {
            int packetSize = capture.GetNextPacketSize();
            int recordBufferOffset = 0;
            //Debug.WriteLine(string.Format("packet size: {0} samples", packetSize / 4));

            while (packetSize != 0)
            {
                IntPtr buffer = capture.GetBuffer(out int framesAvailable, out AudioClientBufferFlags flags);

                int bytesAvailable = framesAvailable * bytesPerFrame;

                // apparently it is sometimes possible to read more frames than we were expecting?
                // fix suggested by Michael Feld:
                int spaceRemaining = Math.Max(0, recordBuffer.Length - recordBufferOffset);
                if (spaceRemaining < bytesAvailable && recordBufferOffset > 0)
                {
                    DataAvailable?.Invoke(this, new WaveInEventArgs(recordBuffer, recordBufferOffset));
                    recordBufferOffset = 0;
                }

                // if not silence...
                if ((flags & AudioClientBufferFlags.Silent) != AudioClientBufferFlags.Silent)
                {
                    Marshal.Copy(buffer, recordBuffer, recordBufferOffset, bytesAvailable);
                }
                else
                {
                    Array.Clear(recordBuffer, recordBufferOffset, bytesAvailable);
                }
                recordBufferOffset += bytesAvailable;
                capture.ReleaseBuffer(framesAvailable);
                packetSize = capture.GetNextPacketSize();
            }
            DataAvailable?.Invoke(this, new WaveInEventArgs(recordBuffer, recordBufferOffset));
        }

        /// <summary>
        /// Disposes of the resources used for audio recording.
        /// </summary>
        /// <remarks>
        /// This method stops the audio recording, releases the capture thread, and disposes of the audio client used for recording.
        /// </remarks>
        public void Dispose()
        {
            StopRecording();
            if (captureThread != null)
            {
                captureThread.Join();
                captureThread = null;
            }
            if (audioClient != null)
            {
                audioClient.Dispose();
                audioClient = null;
            }
        }
    }
}
