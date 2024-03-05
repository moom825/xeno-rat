using System;
using System.Runtime.InteropServices;

namespace NAudio.Wave.Compression
{
    /// <summary>
    /// Interop definitions for Windows ACM (Audio Compression Manager) API
    /// </summary>
    class AcmInterop
    {
        // http://msdn.microsoft.com/en-us/library/dd742891%28VS.85%29.aspx
        public delegate bool AcmDriverEnumCallback(IntPtr hAcmDriverId, IntPtr instance, AcmDriverDetailsSupportFlags flags);

        public delegate bool AcmFormatEnumCallback(IntPtr hAcmDriverId, ref AcmFormatDetails formatDetails, IntPtr dwInstance, AcmDriverDetailsSupportFlags flags);

        public delegate bool AcmFormatTagEnumCallback(IntPtr hAcmDriverId, ref AcmFormatTagDetails formatTagDetails, IntPtr dwInstance, AcmDriverDetailsSupportFlags flags);

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/dd742910%28VS.85%29.aspx
        /// UINT ACMFORMATCHOOSEHOOKPROC acmFormatChooseHookProc(
        ///   HWND hwnd,     
        ///   UINT uMsg,     
        ///   WPARAM wParam, 
        ///   LPARAM lParam  
        /// </summary>        
        public delegate bool AcmFormatChooseHookProc(IntPtr windowHandle, int message, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Adds an Audio Compression Manager (ACM) driver to the system.
        /// </summary>
        /// <param name="driverHandle">When this method returns, contains a pointer to a handle for the added ACM driver.</param>
        /// <param name="driverModule">The handle to the module that contains the ACM driver function.</param>
        /// <param name="driverFunctionAddress">The address of the ACM driver function.</param>
        /// <param name="priority">The priority of the ACM driver.</param>
        /// <param name="flags">Flags that specify how the ACM driver should be added.</param>
        /// <returns>An MmResult value indicating the result of adding the ACM driver.</returns>
        [DllImport("msacm32.dll")]
        public static extern MmResult acmDriverAdd(out IntPtr driverHandle,
            IntPtr driverModule,
            IntPtr driverFunctionAddress,
            int priority,
            AcmDriverAddFlags flags);

        /// <summary>
        /// Removes an Audio Compression Manager (ACM) driver.
        /// </summary>
        /// <param name="driverHandle">The handle to the ACM driver to be removed.</param>
        /// <param name="removeFlags">Flags that specify how the driver should be removed.</param>
        /// <returns>A result code indicating the success or failure of the operation.</returns>
        [DllImport("msacm32.dll")]
        public static extern MmResult acmDriverRemove(IntPtr driverHandle,
            int removeFlags);

        /// <summary>
        /// Closes an Audio Compression Manager (ACM) driver.
        /// </summary>
        /// <param name="hAcmDriver">Handle to the ACM driver to be closed.</param>
        /// <param name="closeFlags">Flags that specify how the ACM driver should be closed.</param>
        /// <returns>An MmResult value indicating the result of the operation.</returns>
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmDriverClose(IntPtr hAcmDriver, int closeFlags);

        /// <summary>
        /// Enumerates the Audio Compression Manager (ACM) drivers installed on the system.
        /// </summary>
        /// <param name="fnCallback">A callback function to be called for each ACM driver found.</param>
        /// <param name="dwInstance">Application-defined parameter to be passed to the callback function.</param>
        /// <param name="flags">Flags that control the driver enumeration process.</param>
        /// <returns>A result code indicating the success or failure of the driver enumeration.</returns>
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmDriverEnum(AcmDriverEnumCallback fnCallback, IntPtr dwInstance, AcmDriverEnumFlags flags);

        /// <summary>
        /// Retrieves details about an Audio Compression Manager (ACM) driver.
        /// </summary>
        /// <param name="hAcmDriver">Handle to the ACM driver to retrieve details for.</param>
        /// <param name="driverDetails">Reference to an <see cref="AcmDriverDetails"/> structure that will receive the driver details.</param>
        /// <param name="reserved">Reserved parameter; must be set to 0.</param>
        /// <returns>An <see cref="MmResult"/> value indicating the result of the operation.</returns>
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmDriverDetails(IntPtr hAcmDriver, ref AcmDriverDetails driverDetails, int reserved);

        /// <summary>
        /// Opens an Audio Compression Manager (ACM) driver and returns a handle to the driver.
        /// </summary>
        /// <param name="pAcmDriver">When this method returns, contains a handle to the opened ACM driver.</param>
        /// <param name="hAcmDriverId">Handle to the ACM driver identifier. This parameter is not currently used and should be set to IntPtr.Zero.</param>
        /// <param name="openFlags">Flags that specify how the ACM driver should be opened.</param>
        /// <returns>An MmResult value that indicates the result of the operation.</returns>
        /// <remarks>
        /// This method opens an ACM driver specified by the <paramref name="hAcmDriverId"/> parameter and returns a handle to the driver in the <paramref name="pAcmDriver"/> parameter.
        /// The <paramref name="openFlags"/> parameter specifies how the ACM driver should be opened, such as for querying or for writing.
        /// </remarks>
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmDriverOpen(out IntPtr pAcmDriver, IntPtr hAcmDriverId, int openFlags);

        /// <summary>
        /// Displays the ACM Format Chooser common dialog box to enable a user to select a waveform-audio format.
        /// </summary>
        /// <param name="formatChoose">A reference to an <see cref="AcmFormatChoose"/> structure that contains information used to initialize the dialog box.</param>
        /// <returns>An <see cref="MmResult"/> value representing the result of the operation.</returns>
        [DllImport("Msacm32.dll", EntryPoint = "acmFormatChooseW")]
        public static extern MmResult acmFormatChoose(ref AcmFormatChoose formatChoose);

        /// <summary>
        /// Enumerates the available formats for an ACM driver.
        /// </summary>
        /// <param name="hAcmDriver">Handle to the ACM driver to query for available formats.</param>
        /// <param name="formatDetails">Reference to an AcmFormatDetails structure that receives details about the format.</param>
        /// <param name="callback">Callback function to be called for each format found.</param>
        /// <param name="instance">Application-defined instance handle.</param>
        /// <param name="flags">Flags that modify the behavior of the enumeration.</param>
        /// <returns>The result of the enumeration operation.</returns>
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmFormatEnum(IntPtr hAcmDriver, ref AcmFormatDetails formatDetails, AcmFormatEnumCallback callback, IntPtr instance, AcmFormatEnumFlags flags);

        /// <summary>
        /// Suggests a destination format for converting an audio format.
        /// </summary>
        /// <param name="hAcmDriver">Handle to the ACM driver to use for the format suggestion.</param>
        /// <param name="sourceFormatPointer">Pointer to the source format to be converted.</param>
        /// <param name="destFormatPointer">Pointer to the suggested destination format.</param>
        /// <param name="sizeDestFormat">Size of the destination format structure.</param>
        /// <param name="suggestFlags">Flags that specify the format suggestion options.</param>
        /// <returns>A result code indicating the success or failure of the format suggestion.</returns>
        [DllImport("Msacm32.dll",EntryPoint="acmFormatSuggest")]
        public static extern MmResult acmFormatSuggest2(
            IntPtr hAcmDriver,
            IntPtr sourceFormatPointer,
            IntPtr destFormatPointer,
            int sizeDestFormat,
            AcmFormatSuggestFlags suggestFlags);

        /// <summary>
        /// Enumerates the available format tags for audio compression manager (ACM) drivers.
        /// </summary>
        /// <param name="hAcmDriver">Handle to the ACM driver to query for format tags.</param>
        /// <param name="formatTagDetails">Reference to an <see cref="AcmFormatTagDetails"/> structure that receives the format tag details.</param>
        /// <param name="callback">Callback function to be called for each format tag found.</param>
        /// <param name="instance">Application-defined value to be passed to the callback function.</param>
        /// <param name="reserved">Reserved; must be set to zero.</param>
        /// <returns>The result of the enumeration operation, represented by an <see cref="MmResult"/> value.</returns>
        /// <remarks>
        /// This method enumerates the available format tags for the specified ACM driver, calling the provided callback function for each format tag found.
        /// The <paramref name="formatTagDetails"/> structure receives the details of each format tag, and the <paramref name="instance"/> parameter is passed to the callback function.
        /// </remarks>
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmFormatTagEnum(IntPtr hAcmDriver, ref AcmFormatTagDetails formatTagDetails, AcmFormatTagEnumCallback callback, IntPtr instance, int reserved);

        /// <summary>
        /// Retrieves information about an Audio Compression Manager (ACM) driver or converter.
        /// </summary>
        /// <param name="hAcmObject">Handle to the ACM driver or converter to query.</param>
        /// <param name="metric">The metric to query.</param>
        /// <param name="output">When this method returns, contains the requested information about the ACM driver or converter.</param>
        /// <returns>An MmResult value indicating the success or failure of the operation.</returns>
        /// <remarks>
        /// This method retrieves information about the specified ACM driver or converter, as specified by the <paramref name="hAcmObject"/> parameter.
        /// The <paramref name="metric"/> parameter specifies the type of information to retrieve.
        /// The retrieved information is stored in the <paramref name="output"/> parameter.
        /// </remarks>
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmMetrics(IntPtr hAcmObject, AcmMetrics metric, out int output);

        /// <summary>
        /// Opens a new conversion stream for audio data.
        /// </summary>
        /// <param name="hAcmStream">When this method returns, contains a handle to the new stream.</param>
        /// <param name="hAcmDriver">Handle to the ACM driver to be used for the conversion.</param>
        /// <param name="sourceFormatPointer">Pointer to the WAVEFORMATEX structure that specifies the format of the source data.</param>
        /// <param name="destFormatPointer">Pointer to the WAVEFORMATEX structure that specifies the format of the destination data.</param>
        /// <param name="waveFilter">A WaveFilter structure that specifies any filtering to be applied during the conversion process.</param>
        /// <param name="callback">Pointer to a callback function that is called during the conversion process.</param>
        /// <param name="instance">Handle to the application-defined instance that is passed to the callback function.</param>
        /// <param name="openFlags">Flags that specify the stream open options.</param>
        /// <returns>An MmResult value indicating the result of the operation.</returns>
        [DllImport("Msacm32.dll",EntryPoint="acmStreamOpen")]
        public static extern MmResult acmStreamOpen2(
            out IntPtr hAcmStream,
            IntPtr hAcmDriver,
            IntPtr sourceFormatPointer,
            IntPtr destFormatPointer,
            [In] WaveFilter waveFilter,
            IntPtr callback,
            IntPtr instance,
            AcmStreamOpenFlags openFlags);

        /// <summary>
        /// Closes an Audio Compression Manager (ACM) stream.
        /// </summary>
        /// <param name="hAcmStream">Handle to the ACM stream to be closed.</param>
        /// <param name="closeFlags">Flags that specify how the ACM stream should be closed.</param>
        /// <returns>An MmResult value indicating the result of the operation.</returns>
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmStreamClose(IntPtr hAcmStream, int closeFlags);

        /// <summary>
        /// Converts audio data from one format to another using the Audio Compression Manager (ACM).
        /// </summary>
        /// <param name="hAcmStream">Handle to the conversion stream.</param>
        /// <param name="streamHeader">The stream header containing information about the conversion process.</param>
        /// <param name="streamConvertFlags">Flags that control the conversion process.</param>
        /// <returns>The result of the conversion operation.</returns>
        /// <remarks>
        /// This method uses the Audio Compression Manager (ACM) to convert audio data from one format to another.
        /// It takes a handle to the conversion stream, a stream header containing information about the conversion process, and flags that control the conversion process.
        /// </remarks>
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmStreamConvert(IntPtr hAcmStream, [In, Out] AcmStreamHeaderStruct streamHeader, AcmStreamConvertFlags streamConvertFlags);

        /// <summary>
        /// Prepares the header for an Audio Compression Manager (ACM) stream.
        /// </summary>
        /// <param name="hAcmStream">Handle to the ACM stream.</param>
        /// <param name="streamHeader">The stream header to be prepared.</param>
        /// <param name="prepareFlags">Flags specifying the preparation options.</param>
        /// <returns>A result code indicating the success or failure of the operation.</returns>
        /// <remarks>
        /// This method prepares the specified stream header for use with the given ACM stream.
        /// The preparation flags can be used to specify additional options for the preparation process.
        /// </remarks>
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmStreamPrepareHeader(IntPtr hAcmStream, [In, Out] AcmStreamHeaderStruct streamHeader, int prepareFlags);

        /// <summary>
        /// Resets the conversion state of an Audio Compression Manager (ACM) stream.
        /// </summary>
        /// <param name="hAcmStream">Handle to the ACM stream to be reset.</param>
        /// <param name="resetFlags">Flags that specify how the stream should be reset.</param>
        /// <returns>An MmResult value indicating the result of the operation.</returns>
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmStreamReset(IntPtr hAcmStream, int resetFlags);

        /// <summary>
        /// Retrieves the size of the output buffer required for the specified input buffer size and conversion parameters.
        /// </summary>
        /// <param name="hAcmStream">Handle to the conversion stream.</param>
        /// <param name="inputBufferSize">Size of the input buffer.</param>
        /// <param name="outputBufferSize">When this method returns, contains the size of the output buffer required for the specified input buffer size and conversion parameters.</param>
        /// <param name="flags">Flags that specify the stream size operation.</param>
        /// <returns>An MmResult value indicating the result of the operation.</returns>
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmStreamSize(IntPtr hAcmStream, int inputBufferSize, out int outputBufferSize, AcmStreamSizeFlags flags);

        /// <summary>
        /// Unprepares the header for an Audio Compression Manager (ACM) stream.
        /// </summary>
        /// <param name="hAcmStream">Handle to the ACM stream.</param>
        /// <param name="streamHeader">The stream header to unprepare.</param>
        /// <param name="flags">Flags that specify the unprepare operation.</param>
        /// <returns>An MmResult value indicating the result of the unprepare operation.</returns>
        [DllImport("Msacm32.dll")]
        public static extern MmResult acmStreamUnprepareHeader(IntPtr hAcmStream, [In, Out] AcmStreamHeaderStruct streamHeader, int flags);
    }
}
