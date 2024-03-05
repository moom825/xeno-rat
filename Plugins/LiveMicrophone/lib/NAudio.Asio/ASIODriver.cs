using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace NAudio.Wave.Asio
{
    /// <summary>
    /// Main AsioDriver Class. To use this class, you need to query first the GetAsioDriverNames() and
    /// then use the GetAsioDriverByName to instantiate the correct AsioDriver.
    /// This is the first AsioDriver binding fully implemented in C#!
    /// 
    /// Contributor: Alexandre Mutel - email: alexandre_mutel at yahoo.fr
    /// </summary>
    public class AsioDriver
    {
        private IntPtr pAsioComObject;
        private IntPtr pinnedcallbacks;
        private AsioDriverVTable asioDriverVTable;

        private AsioDriver()
        {
        }

        /// <summary>
        /// Retrieves the names of ASIO drivers installed on the system.
        /// </summary>
        /// <returns>An array of strings containing the names of the ASIO drivers installed on the system.</returns>
        /// <remarks>
        /// This method retrieves the names of ASIO drivers by accessing the registry key "SOFTWARE\\ASIO" under the Local Machine hive.
        /// It initializes an empty string array and populates it with the names of the subkeys under the "SOFTWARE\\ASIO" registry key.
        /// If the registry key does not exist or is inaccessible, an empty array is returned.
        /// </remarks>
        public static string[] GetAsioDriverNames()
        {
            var regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\ASIO");
            var names = new string[0];
            if (regKey != null)
            {
                names = regKey.GetSubKeyNames();
                regKey.Close();
            }
            return names;
        }

        /// <summary>
        /// Retrieves the ASIO driver with the specified name.
        /// </summary>
        /// <param name="name">The name of the ASIO driver to retrieve.</param>
        /// <returns>The ASIO driver with the specified <paramref name="name"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when the specified <paramref name="name"/> does not exist in the registry.</exception>
        public static AsioDriver GetAsioDriverByName(String name)
        {
            var regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\ASIO\\" + name);
            if (regKey == null)
            {
                throw new ArgumentException($"Driver Name {name} doesn't exist");
            }
            var guid = regKey.GetValue("CLSID").ToString();
            return GetAsioDriverByGuid(new Guid(guid));
        }

        /// <summary>
        /// Retrieves an ASIO driver based on the specified GUID.
        /// </summary>
        /// <param name="guid">The GUID of the ASIO driver to retrieve.</param>
        /// <returns>An instance of the <see cref="AsioDriver"/> class initialized with the specified GUID.</returns>
        public static AsioDriver GetAsioDriverByGuid(Guid guid)
        {
            var driver = new AsioDriver();
            driver.InitFromGuid(guid);
            return driver;
        }

        /// <summary>
        /// Initializes the ASIO driver with the specified system handle.
        /// </summary>
        /// <param name="sysHandle">A pointer to the system handle.</param>
        /// <returns>True if the initialization is successful; otherwise, false.</returns>
        /// <remarks>
        /// This method initializes the ASIO driver using the provided system handle.
        /// It calls the 'init' method of the ASIO driver VTable and returns true if the return value is 1, indicating successful initialization.
        /// </remarks>
        public bool Init(IntPtr sysHandle)
        {
            int ret = asioDriverVTable.init(pAsioComObject, sysHandle);
            return ret == 1;
        }

        /// <summary>
        /// Retrieves the name of the ASIO driver.
        /// </summary>
        /// <returns>The name of the ASIO driver.</returns>
        public string GetDriverName() 
        {
            var name = new StringBuilder(256);
            asioDriverVTable.getDriverName(pAsioComObject, name);
            return name.ToString();
        }

        /// <summary>
        /// Gets the version of the driver.
        /// </summary>
        /// <returns>The version of the driver.</returns>
        public int GetDriverVersion() {
            return asioDriverVTable.getDriverVersion(pAsioComObject);
        }

        /// <summary>
        /// Retrieves the error message from the ASIO driver and returns it as a string.
        /// </summary>
        /// <returns>The error message retrieved from the ASIO driver.</returns>
        public string GetErrorMessage()
        {
            var errorMessage = new StringBuilder(256);
            asioDriverVTable.getErrorMessage(pAsioComObject, errorMessage);
            return errorMessage.ToString();
        }

        /// <summary>
        /// Starts the ASIO driver.
        /// </summary>
        /// <remarks>
        /// This method calls the start function of the ASIO driver's VTable to initiate the driver.
        /// </remarks>
        /// <exception cref="Exception">
        /// Throws an exception if there is an error while starting the ASIO driver.
        /// </exception>
        public void Start()
        {
            HandleException(asioDriverVTable.start(pAsioComObject),"start");
        }

        /// <summary>
        /// Stops the ASIO driver and returns any error that occurred during the operation.
        /// </summary>
        /// <returns>An AsioError object representing any error that occurred during the stop operation.</returns>
        public AsioError Stop()
        {
            return asioDriverVTable.stop(pAsioComObject);
        }

        /// <summary>
        /// Retrieves the number of input and output channels available.
        /// </summary>
        /// <param name="numInputChannels">The number of input channels available.</param>
        /// <param name="numOutputChannels">The number of output channels available.</param>
        /// <exception cref="Exception">Thrown if there is an error retrieving the channels.</exception>
        public void GetChannels(out int numInputChannels, out int numOutputChannels)
        {
            HandleException(asioDriverVTable.getChannels(pAsioComObject, out numInputChannels, out numOutputChannels), "getChannels");
        }

        /// <summary>
        /// Retrieves the input and output latencies of the ASIO driver.
        /// </summary>
        /// <param name="inputLatency">The variable to store the input latency.</param>
        /// <param name="outputLatency">The variable to store the output latency.</param>
        /// <returns>An <see cref="AsioError"/> representing the result of the operation.</returns>
        /// <remarks>
        /// This method retrieves the input and output latencies of the ASIO driver by calling the <c>getLatencies</c> method of the ASIO driver's VTable.
        /// The input and output latencies are stored in the <paramref name="inputLatency"/> and <paramref name="outputLatency"/> variables, respectively.
        /// </remarks>
        public AsioError GetLatencies(out int inputLatency, out int outputLatency)
        {
            return asioDriverVTable.getLatencies(pAsioComObject, out inputLatency, out outputLatency);
        }

        /// <summary>
        /// Retrieves the buffer size information from the ASIO driver.
        /// </summary>
        /// <param name="minSize">The minimum buffer size supported by the ASIO driver.</param>
        /// <param name="maxSize">The maximum buffer size supported by the ASIO driver.</param>
        /// <param name="preferredSize">The preferred buffer size suggested by the ASIO driver.</param>
        /// <param name="granularity">The granularity of buffer size adjustments supported by the ASIO driver.</param>
        /// <remarks>
        /// This method retrieves the buffer size information from the ASIO driver using the ASIO driver's <paramref name="pAsioComObject"/>.
        /// It handles exceptions using the <see cref="HandleException"/> method and passes the retrieved buffer size information to the out parameters.
        /// </remarks>
        /// <exception cref="Exception">
        /// Throws an exception if there is an error while retrieving the buffer size information from the ASIO driver.
        /// </exception>
        public void GetBufferSize(out int minSize, out int maxSize, out int preferredSize, out int granularity)
        {
            HandleException(asioDriverVTable.getBufferSize(pAsioComObject, out minSize, out maxSize, out preferredSize, out granularity), "getBufferSize");
        }

        /// <summary>
        /// Checks if the specified sample rate is supported by the ASIO driver.
        /// </summary>
        /// <param name="sampleRate">The sample rate to be checked.</param>
        /// <returns>True if the sample rate is supported; otherwise, false.</returns>
        /// <exception cref="AsioException">Thrown when an error occurs while checking the sample rate.</exception>
        public bool CanSampleRate(double sampleRate)
        {
            var error = asioDriverVTable.canSampleRate(pAsioComObject, sampleRate);
            if (error == AsioError.ASE_NoClock)
            {
                return false;
            } 
            if ( error == AsioError.ASE_OK )
            {
                return true;
            }
            HandleException(error, "canSampleRate");
            return false;
        }

        /// <summary>
        /// Retrieves the sample rate from the ASIO driver.
        /// </summary>
        /// <returns>The sample rate retrieved from the ASIO driver.</returns>
        /// <remarks>
        /// This method retrieves the sample rate from the ASIO driver using the ASIO driver's <paramref name="getSampleRate"/> function.
        /// It handles any exceptions that may occur during the retrieval process and returns the retrieved sample rate.
        /// </remarks>
        /// <exception cref="Exception">
        /// Thrown if there is an error while retrieving the sample rate from the ASIO driver.
        /// </exception>
        public double GetSampleRate()
        {
            double sampleRate;
            HandleException(asioDriverVTable.getSampleRate(pAsioComObject, out sampleRate), "getSampleRate");
            return sampleRate;
        }

        /// <summary>
        /// Sets the sample rate for the ASIO driver.
        /// </summary>
        /// <param name="sampleRate">The sample rate to be set.</param>
        /// <remarks>
        /// This method sets the sample rate for the ASIO driver using the provided <paramref name="sampleRate"/>.
        /// </remarks>
        /// <exception cref="AsioException">Thrown if there is an error setting the sample rate.</exception>
        public void SetSampleRate(double sampleRate)
        {
            HandleException(asioDriverVTable.setSampleRate(pAsioComObject, sampleRate), "setSampleRate");
        }

        /// <summary>
        /// Retrieves the clock sources and outputs the result in the <paramref name="clocks"/> parameter.
        /// </summary>
        /// <param name="clocks">The output parameter that will contain the retrieved clock sources.</param>
        /// <param name="numSources">The number of clock sources to retrieve.</param>
        /// <remarks>
        /// This method retrieves the clock sources from the ASIO driver and stores the result in the <paramref name="clocks"/> parameter.
        /// </remarks>
        /// <exception cref="Exception">
        /// Throws an exception if there is an error while retrieving the clock sources from the ASIO driver.
        /// </exception>
        public void GetClockSources(out long clocks, int numSources)
        {
            HandleException(asioDriverVTable.getClockSources(pAsioComObject, out clocks,numSources), "getClockSources");
        }

        /// <summary>
        /// Sets the clock source for the ASIO driver.
        /// </summary>
        /// <param name="reference">The reference value for setting the clock source.</param>
        /// <exception cref="Exception">Thrown when an error occurs while setting the clock source.</exception>
        /// <remarks>
        /// This method sets the clock source for the ASIO driver using the provided <paramref name="reference"/> value.
        /// </remarks>
        public void SetClockSource(int reference)
        {
            HandleException(asioDriverVTable.setClockSource(pAsioComObject, reference), "setClockSources");
        }

        /// <summary>
        /// Retrieves the current sample position and timestamp.
        /// </summary>
        /// <param name="samplePos">The current sample position.</param>
        /// <param name="timeStamp">The timestamp associated with the sample position.</param>
        /// <exception cref="Exception">Thrown if there is an error retrieving the sample position and timestamp.</exception>
        public void GetSamplePosition(out long samplePos, ref Asio64Bit timeStamp)
        {
            HandleException(asioDriverVTable.getSamplePosition(pAsioComObject, out samplePos, ref timeStamp), "getSamplePosition");
        }

        /// <summary>
        /// Retrieves the information about the specified channel.
        /// </summary>
        /// <param name="channelNumber">The number of the channel for which information is to be retrieved.</param>
        /// <param name="trueForInputInfo">A boolean value indicating whether to retrieve input information (true) or output information (false).</param>
        /// <returns>The <see cref="AsioChannelInfo"/> object containing the information about the specified channel.</returns>
        /// <exception cref="AsioException">Thrown if there is an error while retrieving the channel information.</exception>
        public AsioChannelInfo GetChannelInfo(int channelNumber, bool trueForInputInfo)
        {
            var info = new AsioChannelInfo {channel = channelNumber, isInput = trueForInputInfo};
            HandleException(asioDriverVTable.getChannelInfo(pAsioComObject, ref info), "getChannelInfo");
            return info;
        }

        /// <summary>
        /// Creates buffers for audio input and output.
        /// </summary>
        /// <param name="bufferInfos">Pointer to buffer information.</param>
        /// <param name="numChannels">The number of audio channels.</param>
        /// <param name="bufferSize">The size of the buffer.</param>
        /// <param name="callbacks">Reference to the AsioCallbacks structure.</param>
        /// <exception cref="AsioException">Thrown if there is an error creating the buffers.</exception>
        /// <remarks>
        /// This method creates buffers for audio input and output using the provided buffer information, number of channels, buffer size, and AsioCallbacks structure.
        /// It allocates memory for the callbacks, marshals the structure, and handles any exceptions that may occur during the buffer creation process.
        /// </remarks>
        public void CreateBuffers(IntPtr bufferInfos, int numChannels, int bufferSize, ref AsioCallbacks callbacks)
        {
            // next two lines suggested by droidi on codeplex issue tracker
            pinnedcallbacks = Marshal.AllocHGlobal(Marshal.SizeOf(callbacks));
            Marshal.StructureToPtr(callbacks, pinnedcallbacks, false);
            HandleException(asioDriverVTable.createBuffers(pAsioComObject, bufferInfos, numChannels, bufferSize, pinnedcallbacks), "createBuffers");
        }

        /// <summary>
        /// Disposes the ASIO buffers and frees the allocated memory.
        /// </summary>
        /// <returns>The result of disposing the ASIO buffers.</returns>
        /// <remarks>
        /// This method disposes the ASIO buffers and frees the allocated memory.
        /// </remarks>
        public AsioError DisposeBuffers()
        {
            AsioError result = asioDriverVTable.disposeBuffers(pAsioComObject);
            Marshal.FreeHGlobal(pinnedcallbacks);
            return result;
        }

        /// <summary>
        /// Opens the control panel for the ASIO driver.
        /// </summary>
        /// <remarks>
        /// This method opens the control panel for the ASIO driver using the ASIO driver VTable.
        /// </remarks>
        /// <exception cref="Exception">
        /// Throws an exception if there is an error while opening the control panel.
        /// </exception>
        public void ControlPanel()
        {
            HandleException(asioDriverVTable.controlPanel(pAsioComObject), "controlPanel");
        }

        /// <summary>
        /// Calls the ASIO driver's future function with the specified selector and options.
        /// </summary>
        /// <param name="selector">The selector for the future function.</param>
        /// <param name="opt">The options to be passed to the future function.</param>
        /// <exception cref="AsioException">Thrown if there is an error calling the ASIO driver's future function.</exception>
        public void Future(int selector, IntPtr opt)
        {
            HandleException(asioDriverVTable.future(pAsioComObject, selector, opt), "future");
        }

        /// <summary>
        /// Checks if the output is ready and returns an AsioError.
        /// </summary>
        /// <returns>An AsioError indicating the status of the output readiness.</returns>
        public AsioError OutputReady()
        {
            return asioDriverVTable.outputReady(pAsioComObject);
        }

        /// <summary>
        /// Releases the COM object associated with the ASIO driver.
        /// </summary>
        /// <remarks>
        /// This method releases the COM object <paramref name="pAsioComObject"/> associated with the ASIO driver.
        /// </remarks>
        public void ReleaseComAsioDriver()
        {
            Marshal.Release(pAsioComObject);
        }

        /// <summary>
        /// Handles the exception for the ASIO method.
        /// </summary>
        /// <param name="error">The ASIO error code.</param>
        /// <param name="methodName">The name of the ASIO method being called.</param>
        /// <exception cref="AsioException">Thrown when the ASIO error code is not ASE_OK or ASE_SUCCESS. The exception message contains the error code, method name, and error message.</exception>
        /// <remarks>
        /// This method handles the exception for the ASIO method by checking the error code. If the error code is not ASE_OK or ASE_SUCCESS, it creates and throws an AsioException with the error code, method name, and error message.
        /// </remarks>
        private void HandleException(AsioError error, string methodName)
        {
            if (error != AsioError.ASE_OK && error != AsioError.ASE_SUCCESS)
            {
                var asioException = new AsioException(
                    $"Error code [{AsioException.getErrorName(error)}] while calling ASIO method <{methodName}>, {this.GetErrorMessage()}");
                asioException.Error = error;
                throw asioException;
            }
        }

        /// <summary>
        /// Initializes the ASIO driver from the given GUID and sets up the virtual table for method calls.
        /// </summary>
        /// <param name="asioGuid">The GUID of the ASIO driver to be initialized.</param>
        /// <exception cref="COMException">Thrown when unable to instantiate ASIO. Check if STAThread is set.</exception>
        /// <remarks>
        /// This method initializes the ASIO driver by querying the virtual table at index 3 and setting up the virtual table for method calls.
        /// It uses CoCreateInstance instead of built-in COM-Class instantiation, as the AsioDriver expects the ASIOGuid used for both COM Object and COM interface.
        /// The CoCreateInstance works only in STAThread mode.
        /// The method modifies the original ASIO Com Object in place by attaching internal delegates to call the methods on the COM Object.
        /// </remarks>
        private void InitFromGuid(Guid asioGuid)
        {
            const uint CLSCTX_INPROC_SERVER = 1;
            // Start to query the virtual table a index 3 (init method of AsioDriver)
            const int INDEX_VTABLE_FIRST_METHOD = 3;

            // Pointer to the ASIO object
            // USE CoCreateInstance instead of builtin COM-Class instantiation,
            // because the AsioDriver expect to have the ASIOGuid used for both COM Object and COM interface
            // The CoCreateInstance is working only in STAThread mode.
            int hresult = CoCreateInstance(ref asioGuid, IntPtr.Zero, CLSCTX_INPROC_SERVER, ref asioGuid, out pAsioComObject);
            if ( hresult != 0 )
            {
                throw new COMException("Unable to instantiate ASIO. Check if STAThread is set",hresult);
            }

            // The first pointer at the adress of the ASIO Com Object is a pointer to the
            // C++ Virtual table of the object.
            // Gets a pointer to VTable.
            IntPtr pVtable = Marshal.ReadIntPtr(pAsioComObject);

            // Instantiate our Virtual table mapping
            asioDriverVTable = new AsioDriverVTable();

            // This loop is going to retrieve the pointer from the C++ VirtualTable
            // and attach an internal delegate in order to call the method on the COM Object.
            FieldInfo[] fieldInfos =  typeof (AsioDriverVTable).GetFields();
            for (int i = 0; i < fieldInfos.Length; i++)
            {
                FieldInfo fieldInfo = fieldInfos[i];
                // Read the method pointer from the VTable
                IntPtr pPointerToMethodInVTable = Marshal.ReadIntPtr(pVtable, (i + INDEX_VTABLE_FIRST_METHOD) * IntPtr.Size);
                // Instantiate a delegate
                object methodDelegate = Marshal.GetDelegateForFunctionPointer(pPointerToMethodInVTable, fieldInfo.FieldType);
                // Store the delegate in our C# VTable
                fieldInfo.SetValue(asioDriverVTable, methodDelegate);
            }
        }

        /// <summary>
        /// Internal VTable structure to store all the delegates to the C++ COM method.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        private class AsioDriverVTable
        {
            //3  virtual ASIOBool init(void *sysHandle) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate int ASIOInit(IntPtr _pUnknown, IntPtr sysHandle);
            public ASIOInit init = null;
            //4  virtual void getDriverName(char *name) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate void ASIOgetDriverName(IntPtr _pUnknown, StringBuilder name);
            public ASIOgetDriverName getDriverName = null;
            //5  virtual long getDriverVersion() = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate int ASIOgetDriverVersion(IntPtr _pUnknown);
            public ASIOgetDriverVersion getDriverVersion = null;
            //6  virtual void getErrorMessage(char *string) = 0;	
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate void ASIOgetErrorMessage(IntPtr _pUnknown, StringBuilder errorMessage);
            public ASIOgetErrorMessage getErrorMessage = null;
            //7  virtual ASIOError start() = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOstart(IntPtr _pUnknown);
            public ASIOstart start = null;
            //8  virtual ASIOError stop() = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOstop(IntPtr _pUnknown);
            public ASIOstop stop = null;
            //9  virtual ASIOError getChannels(long *numInputChannels, long *numOutputChannels) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOgetChannels(IntPtr _pUnknown, out int numInputChannels, out int numOutputChannels);
            public ASIOgetChannels getChannels = null;
            //10  virtual ASIOError getLatencies(long *inputLatency, long *outputLatency) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOgetLatencies(IntPtr _pUnknown, out int inputLatency, out int outputLatency);
            public ASIOgetLatencies getLatencies = null;
            //11 virtual ASIOError getBufferSize(long *minSize, long *maxSize, long *preferredSize, long *granularity) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOgetBufferSize(IntPtr _pUnknown, out int minSize, out int maxSize, out int preferredSize, out int granularity);
            public ASIOgetBufferSize getBufferSize = null;
            //12 virtual ASIOError canSampleRate(ASIOSampleRate sampleRate) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOcanSampleRate(IntPtr _pUnknown, double sampleRate);
            public ASIOcanSampleRate canSampleRate = null;
            //13 virtual ASIOError getSampleRate(ASIOSampleRate *sampleRate) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOgetSampleRate(IntPtr _pUnknown, out double sampleRate);
            public ASIOgetSampleRate getSampleRate = null;
            //14 virtual ASIOError setSampleRate(ASIOSampleRate sampleRate) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOsetSampleRate(IntPtr _pUnknown, double sampleRate);
            public ASIOsetSampleRate setSampleRate = null;
            //15 virtual ASIOError getClockSources(ASIOClockSource *clocks, long *numSources) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOgetClockSources(IntPtr _pUnknown, out long clocks, int numSources);
            public ASIOgetClockSources getClockSources = null;
            //16 virtual ASIOError setClockSource(long reference) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOsetClockSource(IntPtr _pUnknown, int reference);
            public ASIOsetClockSource setClockSource = null;
            //17 virtual ASIOError getSamplePosition(ASIOSamples *sPos, ASIOTimeStamp *tStamp) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOgetSamplePosition(IntPtr _pUnknown, out long samplePos, ref Asio64Bit timeStamp);
            public ASIOgetSamplePosition getSamplePosition = null;
            //18 virtual ASIOError getChannelInfo(ASIOChannelInfo *info) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOgetChannelInfo(IntPtr _pUnknown, ref AsioChannelInfo info);
            public ASIOgetChannelInfo getChannelInfo = null;
            //19 virtual ASIOError createBuffers(ASIOBufferInfo *bufferInfos, long numChannels, long bufferSize, ASIOCallbacks *callbacks) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            //            public delegate ASIOError ASIOcreateBuffers(IntPtr _pUnknown, ref ASIOBufferInfo[] bufferInfos, int numChannels, int bufferSize, ref ASIOCallbacks callbacks);
            public delegate AsioError ASIOcreateBuffers(IntPtr _pUnknown, IntPtr bufferInfos, int numChannels, int bufferSize, IntPtr callbacks);
            public ASIOcreateBuffers createBuffers = null;
            //20 virtual ASIOError disposeBuffers() = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOdisposeBuffers(IntPtr _pUnknown);
            public ASIOdisposeBuffers disposeBuffers = null;
            //21 virtual ASIOError controlPanel() = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOcontrolPanel(IntPtr _pUnknown);
            public ASIOcontrolPanel controlPanel = null;
            //22 virtual ASIOError future(long selector,void *opt) = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOfuture(IntPtr _pUnknown, int selector, IntPtr opt);
            public ASIOfuture future = null;
            //23 virtual ASIOError outputReady() = 0;
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
            public delegate AsioError ASIOoutputReady(IntPtr _pUnknown);
            public ASIOoutputReady outputReady = null;
        }

        /// <summary>
        /// Creates a new instance of a COM object with the specified class identifier (CLSID).
        /// </summary>
        /// <param name="clsid">The CLSID of the object to be created.</param>
        /// <param name="inner">A pointer to the controlling IUnknown interface if the object is being created as part of an aggregate, or IntPtr.Zero otherwise.</param>
        /// <param name="context">The execution context in which the code is to run.</param>
        /// <param name="uuid">The IID of the interface on the object that the caller wants to communicate with.</param>
        /// <param name="rReturnedComObject">When this method returns, contains a reference to the created object.</param>
        /// <returns>An HRESULT value indicating success or failure.</returns>
        [DllImport("ole32.Dll")]
        private static extern int CoCreateInstance(ref Guid clsid,
           IntPtr inner,
           uint context,
           ref Guid uuid,
           out IntPtr rReturnedComObject);
    }
}
