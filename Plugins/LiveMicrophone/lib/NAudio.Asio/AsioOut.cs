using System;
using NAudio.Wave.Asio;
using System.Threading;

namespace NAudio.Wave
{
    /// <summary>
    /// ASIO Out Player. New implementation using an internal C# binding.
    /// 
    /// This implementation is only supporting Short16Bit and Float32Bit formats and is optimized 
    /// for 2 outputs channels .
    /// SampleRate is supported only if AsioDriver is supporting it
    ///     
    /// This implementation is probably the first AsioDriver binding fully implemented in C#!
    /// 
    /// Original Contributor: Mark Heath 
    /// New Contributor to C# binding : Alexandre Mutel - email: alexandre_mutel at yahoo.fr
    /// </summary>
    public class AsioOut : IWavePlayer
    {
        private AsioDriverExt driver;
        private IWaveProvider sourceStream;
        private PlaybackState playbackState;
        private int nbSamples;
        private byte[] waveBuffer;
        private AsioSampleConvertor.SampleConvertor convertor;
        private string driverName;

        private readonly SynchronizationContext syncContext;
        private bool isInitialized;

        /// <summary>
        /// Playback Stopped
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        /// <summary>
        /// When recording, fires whenever recorded audio is available
        /// </summary>
        public event EventHandler<AsioAudioAvailableEventArgs> AudioAvailable;

        /// <summary>
        /// Occurs when the driver settings are changed by the user, e.g. in the control panel.
        /// </summary>
        public event EventHandler DriverResetRequest;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsioOut"/> class with the first 
        /// available ASIO Driver.
        /// </summary>
        public AsioOut()
            : this(0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsioOut"/> class with the driver name.
        /// </summary>
        /// <param name="driverName">Name of the device.</param>
        public AsioOut(string driverName)
        {
            this.syncContext = SynchronizationContext.Current;
            InitFromName(driverName);
        }

        /// <summary>
        /// Opens an ASIO output device
        /// </summary>
        /// <param name="driverIndex">Device number (zero based)</param>
        public AsioOut(int driverIndex)
        {
            this.syncContext = SynchronizationContext.Current; 
            String[] names = GetDriverNames();
            if (names.Length == 0)
            {
                throw new ArgumentException("There is no ASIO Driver installed on your system");
            }
            if (driverIndex < 0 || driverIndex > names.Length)
            {
                throw new ArgumentException(String.Format("Invalid device number. Must be in the range [0,{0}]", names.Length));
            }
            InitFromName(names[driverIndex]);
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="AsioOut"/> is reclaimed by garbage collection.
        /// </summary>
        ~AsioOut()
        {
            Dispose();
        }

        /// <summary>
        /// Releases the resources used by the driver and sets the driver to null.
        /// </summary>
        /// <remarks>
        /// This method checks if the <paramref name="driver"/> is not null and if the <paramref name="playbackState"/> is not <see cref="PlaybackState.Stopped"/>.
        /// If the conditions are met, it stops the driver and sets the <paramref name="driver"/> to null after releasing the resources.
        /// The <paramref name="ResetRequestCallback"/> of the driver is set to null.
        /// </remarks>
        public void Dispose()
        {
            if (driver != null)
            {
                if (playbackState != PlaybackState.Stopped)
                {
                    driver.Stop();
                }
                driver.ResetRequestCallback = null;
                driver.ReleaseDriver();
                driver = null;
            }
        }

        /// <summary>
        /// Retrieves the names of the available ASIO drivers.
        /// </summary>
        /// <returns>An array of strings containing the names of the available ASIO drivers.</returns>
        public static string[] GetDriverNames()
        {
            return AsioDriver.GetAsioDriverNames();
        }

        /// <summary>
        /// Checks if the system supports the operation.
        /// </summary>
        /// <returns>True if the system supports the operation; otherwise, false.</returns>
        public static bool isSupported()
        {
            return GetDriverNames().Length > 0;
        }

        /// <summary>
        /// Checks if the specified sample rate is supported by the driver.
        /// </summary>
        /// <param name="sampleRate">The sample rate to be checked.</param>
        /// <returns>True if the sample rate is supported; otherwise, false.</returns>
        public bool IsSampleRateSupported(int sampleRate)
        {
            return driver.IsSampleRateSupported(sampleRate);
        }

        /// <summary>
        /// Initializes the object using the provided driver name and sets up the extended driver.
        /// </summary>
        /// <param name="driverName">The name of the driver to be initialized.</param>
        /// <remarks>
        /// This method initializes the object with the specified driver name and sets up the extended driver by instantiating it using the basic driver obtained from <see cref="AsioDriver.GetAsioDriverByName(string)"/>.
        /// If an exception occurs during the instantiation of the extended driver, the method releases the basic driver and rethrows the exception.
        /// The <see cref="driver.ResetRequestCallback"/> is set to <see cref="OnDriverResetRequest"/>.
        /// The <see cref="ChannelOffset"/> is set to 0.
        /// </remarks>
        private void InitFromName(string driverName)
        {
            this.driverName = driverName;

            // Get the basic driver
            AsioDriver basicDriver = AsioDriver.GetAsioDriverByName(driverName);

            try
            {
                // Instantiate the extended driver
                driver = new AsioDriverExt(basicDriver);
            }
            catch
            {
                ReleaseDriver(basicDriver);
                throw;
            }
            driver.ResetRequestCallback = OnDriverResetRequest;
            this.ChannelOffset = 0;
        }

        /// <summary>
        /// Raises the DriverResetRequest event.
        /// </summary>
        /// <remarks>
        /// This method raises the DriverResetRequest event, indicating that a reset request has been made for the driver.
        /// </remarks>
        private void OnDriverResetRequest()
        {
            DriverResetRequest?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Releases the resources associated with the provided AsioDriver.
        /// </summary>
        /// <param name="driver">The AsioDriver to be released.</param>
        /// <remarks>
        /// This method disposes the buffers associated with the provided <paramref name="driver"/> and releases the COM AsioDriver resources.
        /// </remarks>
        private void ReleaseDriver(AsioDriver driver)
        {
            driver.DisposeBuffers();
            driver.ReleaseComAsioDriver();
        }

        /// <summary>
        /// Shows the control panel.
        /// </summary>
        /// <remarks>
        /// This method calls the <see cref="driver.ShowControlPanel"/> method to display the control panel.
        /// </remarks>
        public void ShowControlPanel()
        {
            driver.ShowControlPanel();
        }

        /// <summary>
        /// Starts playing the media if it is not already playing.
        /// </summary>
        /// <remarks>
        /// This method checks the current playback state and starts playing the media if it is not already in the playing state.
        /// If the media is already playing, this method does nothing.
        /// </remarks>
        public void Play()
        {
            if (playbackState != PlaybackState.Playing)
            {
                playbackState = PlaybackState.Playing;
                HasReachedEnd = false;
                driver.Start();
            }
        }

        /// <summary>
        /// Stops the playback and resets the playback state to Stopped.
        /// </summary>
        public void Stop()
        {
            playbackState = PlaybackState.Stopped;
            driver.Stop();
            HasReachedEnd = false;
            RaisePlaybackStopped(null);
        }

        /// <summary>
        /// Pauses the playback.
        /// </summary>
        /// <remarks>
        /// This method changes the playback state to paused and stops the driver.
        /// </remarks>
        public void Pause()
        {
            playbackState = PlaybackState.Paused;
            driver.Stop();
        }

        /// <summary>
        /// Initializes the recording and playback with the specified <paramref name="waveProvider"/>.
        /// </summary>
        /// <param name="waveProvider">The wave provider to be initialized.</param>
        /// <remarks>
        /// This method initializes the recording and playback using the specified <paramref name="waveProvider"/>.
        /// The recording starts at position 0 and continues indefinitely until stopped.
        /// </remarks>
        public void Init(IWaveProvider waveProvider)
        {
            InitRecordAndPlayback(waveProvider, 0, -1);
        }

        /// <summary>
        /// Initializes the record and playback with the specified wave provider, record channels, and record only sample rate.
        /// </summary>
        /// <param name="waveProvider">The wave provider to be initialized.</param>
        /// <param name="recordChannels">The number of record channels.</param>
        /// <param name="recordOnlySampleRate">The record only sample rate.</param>
        /// <exception cref="InvalidOperationException">Thrown when attempting to initialize an already initialized instance of AsioOut.</exception>
        /// <exception cref="ArgumentException">Thrown when the sample rate is not supported.</exception>
        /// <remarks>
        /// This method initializes the record and playback with the specified wave provider, record channels, and record only sample rate.
        /// It sets the desired sample rate based on the wave provider or the record only sample rate.
        /// If the wave provider is not null, it sets the source stream, number of output channels, and selects the correct sample convertor based on the ASIO format.
        /// It then sets the output wave format based on the ASIO sample type.
        /// If the wave provider is null, it sets the number of output channels to 0.
        /// It checks if the desired sample rate is supported and sets it if necessary.
        /// The method also plugs the callback, fills the buffer, sets the number of input channels, creates buffers, and sets the channel offset.
        /// Finally, it creates a buffer big enough to read from the source stream to fill the ASIO buffers if the wave provider is not null.
        /// </remarks>
        public void InitRecordAndPlayback(IWaveProvider waveProvider, int recordChannels, int recordOnlySampleRate)
        {
            if (isInitialized)
            {
                throw new InvalidOperationException("Already initialised this instance of AsioOut - dispose and create a new one");
            }
            isInitialized = true;
            int desiredSampleRate = waveProvider != null ? waveProvider.WaveFormat.SampleRate : recordOnlySampleRate;

            if (waveProvider != null)
            {
                sourceStream = waveProvider;

                this.NumberOfOutputChannels = waveProvider.WaveFormat.Channels;

                // Select the correct sample convertor from WaveFormat -> ASIOFormat
                var asioSampleType = driver.Capabilities.OutputChannelInfos[0].type;
                convertor = AsioSampleConvertor.SelectSampleConvertor(waveProvider.WaveFormat, asioSampleType);

                switch (asioSampleType)
                {
                    case AsioSampleType.Float32LSB:
                        OutputWaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(waveProvider.WaveFormat.SampleRate, waveProvider.WaveFormat.Channels);
                        break;
                    case AsioSampleType.Int32LSB:
                        OutputWaveFormat = new WaveFormat(waveProvider.WaveFormat.SampleRate, 32, waveProvider.WaveFormat.Channels);
                        break;
                    case AsioSampleType.Int16LSB:
                        OutputWaveFormat = new WaveFormat(waveProvider.WaveFormat.SampleRate, 16, waveProvider.WaveFormat.Channels);
                        break;
                    case AsioSampleType.Int24LSB:
                        OutputWaveFormat = new WaveFormat(waveProvider.WaveFormat.SampleRate, 24, waveProvider.WaveFormat.Channels);
                        break;
                    default:
                        throw new NotSupportedException($"{asioSampleType} not currently supported");
                }
            }
            else
            {
                this.NumberOfOutputChannels = 0;
            }


            if (!driver.IsSampleRateSupported(desiredSampleRate))
            {
                throw new ArgumentException("SampleRate is not supported");
            }
            if (driver.Capabilities.SampleRate != desiredSampleRate)
            {
                driver.SetSampleRate(desiredSampleRate);
            }

            // Plug the callback
            driver.FillBufferCallback = driver_BufferUpdate;

            this.NumberOfInputChannels = recordChannels;
            // Used Prefered size of ASIO Buffer
            nbSamples = driver.CreateBuffers(NumberOfOutputChannels, NumberOfInputChannels, false);
            driver.SetChannelOffset(ChannelOffset, InputChannelOffset); // will throw an exception if channel offset is too high

            if (waveProvider != null)
            {
                // make a buffer big enough to read enough from the sourceStream to fill the ASIO buffers
                waveBuffer = new byte[nbSamples * NumberOfOutputChannels * waveProvider.WaveFormat.BitsPerSample / 8];
            }
        }

        /// <summary>
        /// Updates the buffer with input and output channels for audio processing.
        /// </summary>
        /// <param name="inputChannels">An array of pointers to the input channels.</param>
        /// <param name="outputChannels">An array of pointers to the output channels.</param>
        /// <remarks>
        /// This method updates the buffer with input and output channels for audio processing. It first checks if there are input channels available, and if so, raises the AudioAvailable event with the appropriate arguments. If the event handler writes to the output buffers, the method returns.
        /// If there are output channels available, it reads from the source stream into the wave buffer, calls the convertor to process the audio data, and handles end-of-data conditions.
        /// </remarks>
        void driver_BufferUpdate(IntPtr[] inputChannels, IntPtr[] outputChannels)
        {
            if (this.NumberOfInputChannels > 0)
            {
                var audioAvailable = AudioAvailable;
                if (audioAvailable != null)
                {
                    var args = new AsioAudioAvailableEventArgs(inputChannels, outputChannels, nbSamples,
                                                               driver.Capabilities.InputChannelInfos[0].type);
                    audioAvailable(this, args);
                    if (args.WrittenToOutputBuffers)
                        return;
                }
            }

            if (this.NumberOfOutputChannels > 0)
            {
                int read = sourceStream.Read(waveBuffer, 0, waveBuffer.Length);
                if (read < waveBuffer.Length)
                {
                    // we have reached the end of the input data - clear out the end
                    Array.Clear(waveBuffer, read, waveBuffer.Length - read);
                }

                // Call the convertor
                unsafe
                {
                    // TODO : check if it's better to lock the buffer at initialization?
                    fixed (void* pBuffer = &waveBuffer[0])
                    {
                        convertor(new IntPtr(pBuffer), outputChannels, NumberOfOutputChannels, nbSamples);
                    }
                }

                if (read == 0)
                {
                    if (AutoStop)
                        Stop(); // this can cause hanging issues
                    HasReachedEnd = true;
                }
            }
        }

        /// <summary>
        /// Gets the latency (in ms) of the playback driver
        /// </summary>
        public int PlaybackLatency
        {
            get
            {
                int latency, temp;
                driver.Driver.GetLatencies(out temp, out latency);
                return latency;
            }
        }

        /// <summary>
        /// Automatically stop when the end of the input stream is reached
        /// Disable this if auto-stop is causing hanging issues
        /// </summary>
        public bool AutoStop { get; set; } 

        /// <summary>
        /// A flag to let you know that we have reached the end of the input file
        /// Useful if AutoStop is set to false
        /// You can monitor this yourself and call Stop when it is true
        /// </summary>
        public bool HasReachedEnd { get; private set; }

        /// <summary>
        /// Playback State
        /// </summary>
        public PlaybackState PlaybackState => playbackState;

        /// <summary>
        /// Driver Name
        /// </summary>
        public string DriverName => this.driverName;

        /// <summary>
        /// The number of output channels we are currently using for playback
        /// (Must be less than or equal to DriverOutputChannelCount)
        /// </summary>
        public int NumberOfOutputChannels { get; private set; }

        /// <summary>
        /// The number of input channels we are currently recording from
        /// (Must be less than or equal to DriverInputChannelCount)
        /// </summary>
        public int NumberOfInputChannels { get; private set; }

        /// <summary>
        /// The maximum number of input channels this ASIO driver supports
        /// </summary>
        public int DriverInputChannelCount => driver.Capabilities.NbInputChannels;

        /// <summary>
        /// The maximum number of output channels this ASIO driver supports
        /// </summary>
        public int DriverOutputChannelCount => driver.Capabilities.NbOutputChannels;

        /// <summary>
        /// The number of samples per channel, per buffer.
        /// </summary>
        public int FramesPerBuffer
        {
            get
            {
                if (!isInitialized)
                    throw new Exception("Not initialized yet. Call this after calling Init");

                return nbSamples;
            }
        }

        /// <summary>
        /// By default the first channel on the input WaveProvider is sent to the first ASIO output.
        /// This option sends it to the specified channel number.
        /// Warning: make sure you don't set it higher than the number of available output channels - 
        /// the number of source channels.
        /// n.b. Future NAudio may modify this
        /// </summary>
        public int ChannelOffset { get; set; }

        /// <summary>
        /// Input channel offset (used when recording), allowing you to choose to record from just one
        /// specific input rather than them all
        /// </summary>
        public int InputChannelOffset { get; set; }

        /// <summary>
        /// Sets the volume (1.0 is unity gain)
        /// Not supported for ASIO Out. Set the volume on the input stream instead
        /// </summary>
        [Obsolete("this function will be removed in a future NAudio as ASIO does not support setting the volume on the device")]
        public float Volume
        {
            get
            {
                return 1.0f;
            }
            set
            {
                if (value != 1.0f)
                {
                    throw new InvalidOperationException("AsioOut does not support setting the device volume");
                }
            }
        }

        /// <inheritdoc/>
        public WaveFormat OutputWaveFormat { get; private set; }

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
        /// Returns the name of the input channel for the specified index.
        /// </summary>
        /// <param name="channel">The index of the input channel.</param>
        /// <returns>The name of the input channel at the specified index. Returns an empty string if the index is greater than the number of input channels supported by the driver.</returns>
        public string AsioInputChannelName(int channel)
        {
            return channel > DriverInputChannelCount ? "" : driver.Capabilities.InputChannelInfos[channel].name;
        }

        /// <summary>
        /// Returns the name of the output channel based on the provided channel number.
        /// </summary>
        /// <param name="channel">The channel number for which the name is to be retrieved.</param>
        /// <returns>The name of the output channel corresponding to the provided <paramref name="channel"/> number, or an empty string if the <paramref name="channel"/> number is greater than the DriverOutputChannelCount.</returns>
        public string AsioOutputChannelName(int channel)
        {
            return channel > DriverOutputChannelCount ? "" : driver.Capabilities.OutputChannelInfos[channel].name;
        }
    }
}
