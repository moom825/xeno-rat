using System;

namespace NAudio.Wave.Asio
{
    /// <summary>
    /// Callback used by the AsioDriverExt to get wave data
    /// </summary>
    public delegate void AsioFillBufferCallback(IntPtr[] inputChannels, IntPtr[] outputChannels);

    /// <summary>
    /// AsioDriverExt is a simplified version of the AsioDriver. It provides an easier
    /// way to access the capabilities of the Driver and implement the callbacks necessary 
    /// for feeding the driver.
    /// Implementation inspired from Rob Philpot's with a managed C++ ASIO wrapper BlueWave.Interop.Asio
    /// http://www.codeproject.com/KB/mcpp/Asio.Net.aspx
    /// 
    /// Contributor: Alexandre Mutel - email: alexandre_mutel at yahoo.fr
    /// </summary>
    public class AsioDriverExt
    {
        private readonly AsioDriver driver;
        private AsioCallbacks callbacks;
        private AsioDriverCapability capability;
        private AsioBufferInfo[] bufferInfos;
        private bool isOutputReadySupported;
        private IntPtr[] currentOutputBuffers;
        private IntPtr[] currentInputBuffers;
        private int numberOfOutputChannels;
        private int numberOfInputChannels;
        private AsioFillBufferCallback fillBufferCallback;
        private int bufferSize;
        private int outputChannelOffset;
        private int inputChannelOffset;
        public Action ResetRequestCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsioDriverExt"/> class based on an already
        /// instantiated AsioDriver instance.
        /// </summary>
        /// <param name="driver">A AsioDriver already instantiated.</param>
        public AsioDriverExt(AsioDriver driver)
        {
            this.driver = driver;

            if (!driver.Init(IntPtr.Zero))
            {
                throw new InvalidOperationException(driver.GetErrorMessage());
            }

            callbacks = new AsioCallbacks();
            callbacks.pasioMessage = AsioMessageCallBack;
            callbacks.pbufferSwitch = BufferSwitchCallBack;
            callbacks.pbufferSwitchTimeInfo = BufferSwitchTimeInfoCallBack;
            callbacks.psampleRateDidChange = SampleRateDidChangeCallBack;

            BuildCapabilities();
        }

        /// <summary>
        /// Sets the channel offsets for output and input channels.
        /// </summary>
        /// <param name="outputChannelOffset">The offset for the output channels.</param>
        /// <param name="inputChannelOffset">The offset for the input channels.</param>
        /// <exception cref="ArgumentException">Thrown when the sum of <paramref name="outputChannelOffset"/> and <paramref name="numberOfOutputChannels"/> is greater than the number of output channels in the capabilities.</exception>
        /// <exception cref="ArgumentException">Thrown when the sum of <paramref name="inputChannelOffset"/> and <paramref name="numberOfInputChannels"/> is greater than the number of input channels in the capabilities.</exception>
        public void SetChannelOffset(int outputChannelOffset, int inputChannelOffset)
        {
            if (outputChannelOffset + numberOfOutputChannels <= Capabilities.NbOutputChannels)
            {
                this.outputChannelOffset = outputChannelOffset;
            }
            else
            {
                throw new ArgumentException("Invalid channel offset");
            }
            if (inputChannelOffset + numberOfInputChannels <= Capabilities.NbInputChannels)
            {
                this.inputChannelOffset = inputChannelOffset;
            }
            else
            {
                throw new ArgumentException("Invalid channel offset");
            }

       }

        /// <summary>
        /// Gets the driver used.
        /// </summary>
        /// <value>The ASIOdriver.</value>
        public AsioDriver Driver => driver;

        /// <summary>
        /// Starts the driver.
        /// </summary>
        /// <remarks>
        /// This method starts the driver, allowing it to perform its designated tasks.
        /// </remarks>
        public void Start()
        {
            driver.Start();
        }

        /// <summary>
        /// Stops the driver.
        /// </summary>
        public void Stop()
        {
            driver.Stop();
        }

        /// <summary>
        /// Shows the control panel by invoking the driver's ControlPanel method.
        /// </summary>
        public void ShowControlPanel()
        {
            driver.ControlPanel();
        }

        /// <summary>
        /// Releases the driver resources and COM ASIO driver.
        /// </summary>
        /// <remarks>
        /// This method releases the driver resources by disposing the buffers and releasing the COM ASIO driver.
        /// </remarks>
        /// <exception cref="Exception">Thrown if an error occurs while disposing the buffers.</exception>
        public void ReleaseDriver()
        {
            try
            {
                driver.DisposeBuffers();
            } catch (Exception ex)
            {
                Console.Out.WriteLine(ex.ToString());
            }
            driver.ReleaseComAsioDriver();
        }

        /// <summary>
        /// Checks if the specified sample rate is supported by the driver.
        /// </summary>
        /// <param name="sampleRate">The sample rate to be checked.</param>
        /// <returns>True if the sample rate is supported; otherwise, false.</returns>
        public bool IsSampleRateSupported(double sampleRate)
        {
            return driver.CanSampleRate(sampleRate);
        }

        /// <summary>
        /// Sets the sample rate for the driver and updates the capabilities.
        /// </summary>
        /// <param name="sampleRate">The new sample rate to be set.</param>
        /// <remarks>
        /// This method sets the sample rate for the driver to the specified value and then updates the capabilities by calling the BuildCapabilities method.
        /// </remarks>
        public void SetSampleRate(double sampleRate)
        {
            driver.SetSampleRate(sampleRate);
            // Update Capabilities
            BuildCapabilities();
        }

        /// <summary>
        /// Gets or sets the fill buffer callback.
        /// </summary>
        /// <value>The fill buffer callback.</value>
        public AsioFillBufferCallback FillBufferCallback
        {
            get { return fillBufferCallback; }
            set { fillBufferCallback = value; }
        }

        /// <summary>
        /// Gets the capabilities of the AsioDriver.
        /// </summary>
        /// <value>The capabilities.</value>
        public AsioDriverCapability Capabilities => capability;

        /// <summary>
        /// Creates buffers for the specified number of output and input channels and returns the buffer size used.
        /// </summary>
        /// <param name="numberOfOutputChannels">The number of output channels for which buffers need to be created.</param>
        /// <param name="numberOfInputChannels">The number of input channels for which buffers need to be created.</param>
        /// <param name="useMaxBufferSize">A boolean value indicating whether to use the maximum buffer size provided by the driver.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the number of output channels or input channels is invalid, i.e., not within the specified range.
        /// </exception>
        /// <returns>
        /// The size of the buffers created for the channels, based on whether the maximum buffer size provided by the driver is used or the preferred buffer size.
        /// </returns>
        /// <remarks>
        /// This method creates buffers for the specified number of output and input channels. It sets the number of output and input channels and initializes buffer information accordingly.
        /// If <paramref name="useMaxBufferSize"/> is true, the driver's maximum buffer size is used; otherwise, the preferred buffer size is used.
        /// The ASIO Buffers are created with the specified buffer information and callbacks, and the method checks if outputReady is supported by the driver.
        /// </remarks>
        public int CreateBuffers(int numberOfOutputChannels, int numberOfInputChannels, bool useMaxBufferSize)
        {
            if (numberOfOutputChannels < 0 || numberOfOutputChannels > capability.NbOutputChannels)
            {
                throw new ArgumentException(
                    $"Invalid number of channels {numberOfOutputChannels}, must be in the range [0,{capability.NbOutputChannels}]");
            }
            if (numberOfInputChannels < 0 || numberOfInputChannels > capability.NbInputChannels)
            {
                throw new ArgumentException("numberOfInputChannels",
                    $"Invalid number of input channels {numberOfInputChannels}, must be in the range [0,{capability.NbInputChannels}]");
            }

            // each channel needs a buffer info
            this.numberOfOutputChannels = numberOfOutputChannels;
            this.numberOfInputChannels = numberOfInputChannels;
            // Ask for maximum of output channels even if we use only the nbOutputChannelsArg
            int nbTotalChannels = capability.NbInputChannels + capability.NbOutputChannels;
            bufferInfos = new AsioBufferInfo[nbTotalChannels];
            currentOutputBuffers = new IntPtr[numberOfOutputChannels];
            currentInputBuffers = new IntPtr[numberOfInputChannels];

            // and do the same for output channels
            // ONLY work on output channels (just put isInput = true for InputChannel)
            int totalIndex = 0;
            for (int index = 0; index < capability.NbInputChannels; index++, totalIndex++)
            {
                bufferInfos[totalIndex].isInput = true;
                bufferInfos[totalIndex].channelNum = index;
                bufferInfos[totalIndex].pBuffer0 = IntPtr.Zero;
                bufferInfos[totalIndex].pBuffer1 = IntPtr.Zero;
            }

            for (int index = 0; index < capability.NbOutputChannels; index++, totalIndex++)
            {
                bufferInfos[totalIndex].isInput = false;
                bufferInfos[totalIndex].channelNum = index;
                bufferInfos[totalIndex].pBuffer0 = IntPtr.Zero;
                bufferInfos[totalIndex].pBuffer1 = IntPtr.Zero;
            }

            if (useMaxBufferSize)
            {
                // use the drivers maximum buffer size
                bufferSize = capability.BufferMaxSize;
            }
            else
            {
                // use the drivers preferred buffer size
                bufferSize = capability.BufferPreferredSize;
            }

            unsafe
            {
                fixed (AsioBufferInfo* infos = &bufferInfos[0])
                {
                    IntPtr pOutputBufferInfos = new IntPtr(infos);

                    // Create the ASIO Buffers with the callbacks
                    driver.CreateBuffers(pOutputBufferInfos, nbTotalChannels, bufferSize, ref callbacks);
                }
            }

            // Check if outputReady is supported
            isOutputReadySupported = (driver.OutputReady() == AsioError.ASE_OK);
            return bufferSize;
        }

        /// <summary>
        /// Builds the capabilities of the ASIO driver, including driver name, input/output channels, channel information, sample rate, latencies, and buffer size.
        /// </summary>
        /// <remarks>
        /// This method initializes the <paramref name="capability"/> object with the capabilities of the ASIO driver.
        /// It retrieves the driver name using <see cref="AsioDriver.GetDriverName"/> and the number of input/output channels using <see cref="AsioDriver.GetChannels"/>.
        /// It then retrieves the channel information for inputs and outputs using <see cref="AsioDriver.GetChannelInfo"/>.
        /// The sample rate is obtained using <see cref="AsioDriver.GetSampleRate"/>, and latencies are retrieved using <see cref="AsioDriver.GetLatencies"/>.
        /// If an error occurs during the retrieval of latencies, an <see cref="AsioException"/> is thrown with the specific error information.
        /// Finally, the buffer size is obtained using <see cref="AsioDriver.GetBufferSize"/>.
        /// </remarks>
        /// <exception cref="AsioException">Thrown when an error occurs during the retrieval of latencies.</exception>
        private void BuildCapabilities()
        {
            capability = new AsioDriverCapability();

            capability.DriverName = driver.GetDriverName();

            // Get nb Input/Output channels
            driver.GetChannels(out capability.NbInputChannels, out capability.NbOutputChannels);

            capability.InputChannelInfos = new AsioChannelInfo[capability.NbInputChannels];
            capability.OutputChannelInfos = new AsioChannelInfo[capability.NbOutputChannels];

            // Get ChannelInfo for Inputs
            for (int i = 0; i < capability.NbInputChannels; i++)
            {
                capability.InputChannelInfos[i] = driver.GetChannelInfo(i, true);
            }

            // Get ChannelInfo for Output
            for (int i = 0; i < capability.NbOutputChannels; i++)
            {
                capability.OutputChannelInfos[i] = driver.GetChannelInfo(i, false);
            }

            // Get the current SampleRate
            capability.SampleRate = driver.GetSampleRate();

            var error = driver.GetLatencies(out capability.InputLatency, out capability.OutputLatency);
            // focusrite scarlett 2i4 returns ASE_NotPresent here

            if (error != AsioError.ASE_OK && error != AsioError.ASE_NotPresent)
            {
                var ex = new AsioException("ASIOgetLatencies");
                ex.Error = error;
                throw ex;
            }

            // Get BufferSize
            driver.GetBufferSize(out capability.BufferMinSize, out capability.BufferMaxSize, out capability.BufferPreferredSize, out capability.BufferGranularity);
        }

        /// <summary>
        /// Callback function for switching buffers and invoking the fill buffer callback.
        /// </summary>
        /// <param name="doubleBufferIndex">The index of the double buffer.</param>
        /// <param name="directProcess">A boolean value indicating whether to process directly.</param>
        private void BufferSwitchCallBack(int doubleBufferIndex, bool directProcess)
        {
            for (int i = 0; i < numberOfInputChannels; i++)
            {
                currentInputBuffers[i] = bufferInfos[i + inputChannelOffset].Buffer(doubleBufferIndex);
            }

            for (int i = 0; i < numberOfOutputChannels; i++)
            {
                currentOutputBuffers[i] = bufferInfos[i + outputChannelOffset + capability.NbInputChannels].Buffer(doubleBufferIndex);
            }

            fillBufferCallback?.Invoke(currentInputBuffers, currentOutputBuffers);

            if (isOutputReadySupported)
            {
                driver.OutputReady();
            }
        }

        /// <summary>
        /// Updates the sample rate in the capability and modifies the original array in place.
        /// </summary>
        /// <param name="sRate">The new sample rate value.</param>
        /// <remarks>
        /// This method updates the sample rate in the capability object with the provided <paramref name="sRate"/> value.
        /// </remarks>
        private void SampleRateDidChangeCallBack(double sRate)
        {
            // Check when this is called?
            capability.SampleRate = sRate;
        }

        /// <summary>
        /// Handles ASIO messages and performs corresponding actions based on the message selector and value.
        /// </summary>
        /// <param name="selector">The ASIO message selector.</param>
        /// <param name="value">The value associated with the message selector.</param>
        /// <param name="message">A pointer to the message data.</param>
        /// <param name="opt">A pointer to optional data.</param>
        /// <returns>The result of the ASIO message handling.</returns>
        /// <remarks>
        /// This method handles ASIO messages and performs corresponding actions based on the message selector and value.
        /// It switches between different message selectors and executes the appropriate actions for each case.
        /// If the message selector is kAsioSelectorSupported, it further switches between sub-values to handle specific cases.
        /// The method also invokes the ResetRequestCallback when the message selector is kAsioResetRequest.
        /// </remarks>
        private int AsioMessageCallBack(AsioMessageSelector selector, int value, IntPtr message, IntPtr opt)
        {
            // Check when this is called?
            switch (selector)
            {
                case AsioMessageSelector.kAsioSelectorSupported:
                    AsioMessageSelector subValue = (AsioMessageSelector)Enum.ToObject(typeof(AsioMessageSelector), value);
                    switch (subValue)
                    {
                        case AsioMessageSelector.kAsioEngineVersion:
                            return 1;
                        case AsioMessageSelector.kAsioResetRequest:
                            ResetRequestCallback?.Invoke();
                            return 0;
                        case AsioMessageSelector.kAsioBufferSizeChange:
                            return 0;
                        case AsioMessageSelector.kAsioResyncRequest:
                            return 0;
                        case AsioMessageSelector.kAsioLatenciesChanged:
                            return 0;
                        case AsioMessageSelector.kAsioSupportsTimeInfo:
//                            return 1; DON'T SUPPORT FOR NOW. NEED MORE TESTING.
                            return 0;
                        case AsioMessageSelector.kAsioSupportsTimeCode:
//                            return 1; DON'T SUPPORT FOR NOW. NEED MORE TESTING.
                            return 0;
                    }
                    break;
                case AsioMessageSelector.kAsioEngineVersion:
                    return 2;
                case AsioMessageSelector.kAsioResetRequest:
                    ResetRequestCallback?.Invoke();
                    return 1;
                case AsioMessageSelector.kAsioBufferSizeChange:
                    return 0;
                case AsioMessageSelector.kAsioResyncRequest:
                    return 0;
                case AsioMessageSelector.kAsioLatenciesChanged:
                    return 0;
                case AsioMessageSelector.kAsioSupportsTimeInfo:
                    return 0;
                case AsioMessageSelector.kAsioSupportsTimeCode:
                    return 0;
            }
            return 0;
        }

        /// <summary>
        /// Callback function for switching buffer time information.
        /// </summary>
        /// <param name="asioTimeParam">The time parameter for the ASIO driver.</param>
        /// <param name="doubleBufferIndex">The index of the double buffer.</param>
        /// <param name="directProcess">A boolean value indicating whether the process is direct.</param>
        /// <returns>Returns a pointer to the buffer switch time information.</returns>
        private IntPtr BufferSwitchTimeInfoCallBack(IntPtr asioTimeParam, int doubleBufferIndex, bool directProcess)
        {
            // Check when this is called?
            return IntPtr.Zero;
        }
    }
}
