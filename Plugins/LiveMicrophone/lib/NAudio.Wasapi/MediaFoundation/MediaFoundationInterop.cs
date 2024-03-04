using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NAudio.Wave;
using System.Runtime.InteropServices.ComTypes;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Interop definitions for MediaFoundation
    /// thanks to Lucian Wischik for the initial work on many of these definitions (also various interfaces)
    /// n.b. the goal is to make as much of this internal as possible, and provide
    /// better .NET APIs using the MediaFoundationApi class instead
    /// </summary>
    public static class MediaFoundationInterop
    {

        /// <summary>
        /// Initializes the Media Foundation platform.
        /// </summary>
        /// <param name="version">The version of the Media Foundation platform to initialize.</param>
        /// <param name="dwFlags">Optional flags for initialization. Default is 0.</param>
        /// <exception cref="System.EntryPointNotFoundException">The mfplat.dll library is not found.</exception>
        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void MFStartup(int version, int dwFlags = 0);

        /// <summary>
        /// Shuts down the Media Foundation platform.
        /// </summary>
        /// <remarks>
        /// This method shuts down the Media Foundation platform. It is used to release all resources held by the platform and should be called when the platform is no longer needed.
        /// </remarks>
        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void MFShutdown();

        /// <summary>
        /// Creates a new media type object and returns it through the <paramref name="ppMFType"/> parameter.
        /// </summary>
        /// <param name="ppMFType">When this method returns, contains the new media type object.</param>
        /// <remarks>
        /// This method creates a new media type object and returns it through the <paramref name="ppMFType"/> parameter.
        /// The media type object represents a description of a stream of multimedia data, including the format of the data and optional metadata.
        /// </remarks>
        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        internal static extern void MFCreateMediaType(out IMFMediaType ppMFType);

        /// <summary>
        /// Initializes a media type from a WaveFormatEx structure.
        /// </summary>
        /// <param name="pMFType">The IMFMediaType interface pointer to initialize.</param>
        /// <param name="pWaveFormat">The WaveFormatEx structure to initialize the media type from.</param>
        /// <param name="cbBufSize">The size of the buffer.</param>
        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        internal static extern void MFInitMediaTypeFromWaveFormatEx([In] IMFMediaType pMFType, [In] WaveFormat pWaveFormat, [In] int cbBufSize);

        /// <summary>
        /// Creates a WaveFormatEx structure from an IMFMediaType object.
        /// </summary>
        /// <param name="pMFType">The IMFMediaType object from which to create the WaveFormatEx structure.</param>
        /// <param name="ppWF">A pointer to the created WaveFormatEx structure.</param>
        /// <param name="pcbSize">The size of the created WaveFormatEx structure.</param>
        /// <param name="flags">Optional flags for creating the WaveFormatEx structure.</param>
        /// <remarks>
        /// This method creates a WaveFormatEx structure from the specified IMFMediaType object. The created structure is returned through the <paramref name="ppWF"/> parameter, and its size is returned through the <paramref name="pcbSize"/> parameter.
        /// </remarks>
        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        internal static extern void MFCreateWaveFormatExFromMFMediaType(IMFMediaType pMFType, ref IntPtr ppWF, ref int pcbSize, int flags = 0);

        /// <summary>
        /// Creates a source reader from the specified URL.
        /// </summary>
        /// <param name="pwszURL">The URL of the media source.</param>
        /// <param name="pAttributes">A pointer to the IMFAttributes interface. Can be null.</param>
        /// <param name="ppSourceReader">Receives a pointer to the IMFSourceReader interface.</param>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when a call to the underlying native COM method fails.</exception>
        [DllImport("mfreadwrite.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void MFCreateSourceReaderFromURL([In, MarshalAs(UnmanagedType.LPWStr)] string pwszURL, [In] IMFAttributes pAttributes,
                                                                [Out, MarshalAs(UnmanagedType.Interface)] out IMFSourceReader ppSourceReader);

        /// <summary>
        /// Creates a source reader from a byte stream.
        /// </summary>
        /// <param name="pByteStream">The byte stream from which to create the source reader.</param>
        /// <param name="pAttributes">Additional attributes for creating the source reader.</param>
        /// <param name="ppSourceReader">When this method returns, contains the created source reader.</param>
        /// <remarks>
        /// This method creates a source reader from the specified byte stream and additional attributes.
        /// The source reader is used to read media data from a media source.
        /// </remarks>
        [DllImport("mfreadwrite.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void MFCreateSourceReaderFromByteStream([In] IMFByteStream pByteStream, [In] IMFAttributes pAttributes, [Out, MarshalAs(UnmanagedType.Interface)] out IMFSourceReader ppSourceReader);

        /// <summary>
        /// Creates a sink writer for a specified output URL.
        /// </summary>
        /// <param name="pwszOutputURL">The URL of the output file.</param>
        /// <param name="pByteStream">The byte stream to write to.</param>
        /// <param name="pAttributes">The attributes to configure the sink writer.</param>
        /// <param name="ppSinkWriter">When this method returns, contains the pointer to the sink writer interface.</param>
        [DllImport("mfreadwrite.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void MFCreateSinkWriterFromURL([In, MarshalAs(UnmanagedType.LPWStr)] string pwszOutputURL,
                                                           [In] IMFByteStream pByteStream, [In] IMFAttributes pAttributes, [Out] out IMFSinkWriter ppSinkWriter);

        /// <summary>
        /// Creates a byte stream object from the specified stream.
        /// </summary>
        /// <param name="punkStream">The stream from which to create the byte stream object.</param>
        /// <param name="ppByteStream">When this method returns, contains the created IMFByteStream object.</param>
        /// <exception cref="System.Runtime.InteropServices.COMException">
        /// Thrown when a COM error occurs during the creation of the IMFByteStream object.
        /// </exception>
        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void MFCreateMFByteStreamOnStreamEx([MarshalAs(UnmanagedType.IUnknown)] object punkStream, out IMFByteStream ppByteStream);

        /// <summary>
        /// Creates a byte stream object based on the provided stream object.
        /// </summary>
        /// <param name="punkStream">The input stream object.</param>
        /// <param name="ppByteStream">When this method returns, contains the created byte stream object.</param>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when the method fails to create the byte stream object.</exception>
        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void MFCreateMFByteStreamOnStream([In] IStream punkStream, out IMFByteStream ppByteStream);

        /// <summary>
        /// Enumerates Media Foundation transforms (MFTs) that match the specified search criteria.
        /// </summary>
        /// <param name="guidCategory">The category of MFTs to enumerate.</param>
        /// <param name="flags">Flags that control the enumeration.</param>
        /// <param name="pInputType">A pointer to an MFT_REGISTER_TYPE_INFO structure that specifies the input type to match.</param>
        /// <param name="pOutputType">A pointer to an MFT_REGISTER_TYPE_INFO structure that specifies the output type to match.</param>
        /// <param name="pppMFTActivate">Receives a pointer to an array of pointers to IMFActivate objects. The caller must release the objects when they are no longer needed by calling the IMFActivate::Release method.</param>
        /// <param name="pcMFTActivate">Receives the number of elements in the array pointed to by pppMFTActivate.</param>
        /// <remarks>
        /// This method enumerates MFTs that match the specified search criteria. The search criteria are specified by the guidCategory, flags, pInputType, and pOutputType parameters.
        /// The pppMFTActivate parameter receives a pointer to an array of pointers to IMFActivate objects. The caller must release the objects when they are no longer needed by calling the IMFActivate::Release method.
        /// The pcMFTActivate parameter receives the number of elements in the array pointed to by pppMFTActivate.
        /// </remarks>
        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void MFTEnumEx([In] Guid guidCategory, [In] _MFT_ENUM_FLAG flags, [In] MFT_REGISTER_TYPE_INFO pInputType, [In] MFT_REGISTER_TYPE_INFO pOutputType,
                                            [Out] out IntPtr pppMFTActivate, [Out] out int pcMFTActivate);

        /// <summary>
        /// Creates a new Media Foundation sample object.
        /// </summary>
        /// <param name="ppIMFSample">When this method returns, contains the new IMFSample object.</param>
        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        internal static extern void MFCreateSample([Out] out IMFSample ppIMFSample);

        /// <summary>
        /// Creates a memory buffer with the specified maximum length.
        /// </summary>
        /// <param name="cbMaxLength">The maximum length of the memory buffer to be created.</param>
        /// <param name="ppBuffer">When this method returns, contains the created IMFMediaBuffer.</param>
        /// <remarks>
        /// This method creates a memory buffer with the specified maximum length using the Media Foundation platform.
        /// The created memory buffer is returned through the <paramref name="ppBuffer"/> parameter.
        /// </remarks>
        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        internal static extern void MFCreateMemoryBuffer(
            int cbMaxLength, [Out] out IMFMediaBuffer ppBuffer);

        /// <summary>
        /// Creates a new instance of the IMFAttributes interface.
        /// </summary>
        /// <param name="ppMFAttributes">When this method returns, contains the new IMFAttributes interface. This parameter is passed uninitialized.</param>
        /// <param name="cInitialSize">The initial size of the attribute store. The value is a hint; it is not an absolute requirement. The value can be zero.</param>
        /// <exception cref="ExternalException">An error occurred while creating the IMFAttributes interface.</exception>
        [DllImport("mfplat.dll", ExactSpelling = true, PreserveSig = false)]
        internal static extern void MFCreateAttributes(
            [Out, MarshalAs(UnmanagedType.Interface)] out IMFAttributes ppMFAttributes,
            [In] int cInitialSize);

        /// <summary>
        /// Retrieves the available audio output types for transcoding using the specified subtype and configuration.
        /// </summary>
        /// <param name="guidSubType">The subtype of the media format.</param>
        /// <param name="dwMFTFlags">Flags that control the enumeration behavior.</param>
        /// <param name="pCodecConfig">An IMFAttributes interface that contains codec-specific configuration data.</param>
        /// <param name="ppAvailableTypes">When this method returns, contains the collection of available output types for transcoding.</param>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when a COM error occurs during the transcoding type retrieval.</exception>
        [DllImport("mf.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void MFTranscodeGetAudioOutputAvailableTypes(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidSubType,
            [In] _MFT_ENUM_FLAG dwMFTFlags,
            [In] IMFAttributes pCodecConfig,
            [Out, MarshalAs(UnmanagedType.Interface)] out IMFCollection ppAvailableTypes);

        /// <summary>
        /// All streams
        /// </summary>
        public const int MF_SOURCE_READER_ALL_STREAMS = unchecked((int)0xFFFFFFFE);
        /// <summary>
        /// First audio stream
        /// </summary>
        public const int MF_SOURCE_READER_FIRST_AUDIO_STREAM = unchecked((int)0xFFFFFFFD);
        /// <summary>
        /// First video stream
        /// </summary>
        public const int MF_SOURCE_READER_FIRST_VIDEO_STREAM = unchecked((int)0xFFFFFFFC);
        /// <summary>
        /// Media source
        /// </summary>
        public const int MF_SOURCE_READER_MEDIASOURCE = unchecked((int)0xFFFFFFFF);
        /// <summary>
        /// Media Foundation SDK Version
        /// </summary>
        public const int MF_SDK_VERSION = 0x2;
        /// <summary>
        /// Media Foundation API Version
        /// </summary>
        public const int MF_API_VERSION = 0x70;
        /// <summary>
        /// Media Foundation Version
        /// </summary>
        public const int MF_VERSION = (MF_SDK_VERSION << 16) | MF_API_VERSION;
        

    }
}
