using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using NAudio.Wave;
using System.Runtime.InteropServices.ComTypes;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// Main interface for using Media Foundation with NAudio
    /// </summary>
    public static class MediaFoundationApi
    {
        private static bool initialized;

        /// <summary>
        /// Initializes the Media Foundation platform if not already initialized.
        /// </summary>
        /// <remarks>
        /// This method checks if the Media Foundation platform is already initialized. If not, it initializes the platform by calling <see cref="MediaFoundationInterop.MFStartup"/>.
        /// The method first checks the Media Foundation SDK version and the operating system version to determine the appropriate startup parameters.
        /// </remarks>
        public static void Startup()
        {
            if (!initialized)
            {
                var sdkVersion = MediaFoundationInterop.MF_SDK_VERSION;
                // Windows Vista check
                var os = Environment.OSVersion;
                if (os.Version.Major == 6 && os.Version.Minor == 0)
                    sdkVersion = 1;
                MediaFoundationInterop.MFStartup((sdkVersion << 16) | MediaFoundationInterop.MF_API_VERSION, 0);
                initialized = true;
            }
        }

        /// <summary>
        /// Enumerates and returns a collection of IMFActivate interfaces for the specified category.
        /// </summary>
        /// <param name="category">The category for which to enumerate the IMFActivate interfaces.</param>
        /// <returns>A collection of IMFActivate interfaces for the specified category.</returns>
        /// <remarks>
        /// This method uses MediaFoundationInterop.MFTEnumEx to enumerate the IMFActivate interfaces for the specified category.
        /// It then iterates through the interfaces, marshals them, and yields each interface in the collection.
        /// Finally, it frees the memory allocated for the interfacesPointer using Marshal.FreeCoTaskMem.
        /// </remarks>
        public static IEnumerable<IMFActivate> EnumerateTransforms(Guid category)
        {
            MediaFoundationInterop.MFTEnumEx(category, _MFT_ENUM_FLAG.MFT_ENUM_FLAG_ALL,
                null, null, out var interfacesPointer, out var interfaceCount);
            var interfaces = new IMFActivate[interfaceCount];
            for (int n = 0; n < interfaceCount; n++)
            {
                var ptr =
                    Marshal.ReadIntPtr(new IntPtr(interfacesPointer.ToInt64() + n*Marshal.SizeOf(interfacesPointer)));
                interfaces[n] = (IMFActivate) Marshal.GetObjectForIUnknown(ptr);
            }

            foreach (var i in interfaces)
            {
                yield return i;
            }
            Marshal.FreeCoTaskMem(interfacesPointer);
        }

        /// <summary>
        /// Shuts down the Media Foundation platform if it has been initialized.
        /// </summary>
        /// <remarks>
        /// This method checks if the Media Foundation platform has been initialized. If it has been initialized, it shuts down the platform using the MFShutdown method and sets the initialized flag to false.
        /// </remarks>
        public static void Shutdown()
        {
            if (initialized)
            {
                MediaFoundationInterop.MFShutdown();
                initialized = false;
            }
        }

        /// <summary>
        /// Creates a new instance of IMFMediaType.
        /// </summary>
        /// <returns>A new instance of IMFMediaType.</returns>
        /// <remarks>
        /// This method creates a new instance of IMFMediaType using the Media Foundation API MFCreateMediaType function.
        /// </remarks>
        public static IMFMediaType CreateMediaType()
        {
            MediaFoundationInterop.MFCreateMediaType(out IMFMediaType mediaType);
            return mediaType;
        }

        /// <summary>
        /// Creates a new IMFMediaType object from the provided WaveFormat.
        /// </summary>
        /// <param name="waveFormat">The WaveFormat to initialize the IMFMediaType with.</param>
        /// <returns>The IMFMediaType object initialized with the provided WaveFormat.</returns>
        /// <remarks>
        /// This method initializes a new IMFMediaType object using the provided WaveFormat.
        /// It first creates a new IMFMediaType object, then initializes it with the provided WaveFormat using MediaFoundationInterop.MFInitMediaTypeFromWaveFormatEx method.
        /// If an exception occurs during the initialization process, the created IMFMediaType object is released before rethrowing the exception.
        /// </remarks>
        public static IMFMediaType CreateMediaTypeFromWaveFormat(WaveFormat waveFormat)
        {
            var mediaType = CreateMediaType();
            try
            {
                MediaFoundationInterop.MFInitMediaTypeFromWaveFormatEx(mediaType, waveFormat, Marshal.SizeOf(waveFormat));
            }
            catch (Exception)
            {
                Marshal.ReleaseComObject(mediaType);
                throw;
            }
            return mediaType;
        }

        /// <summary>
        /// Creates a memory buffer of the specified size and returns it.
        /// </summary>
        /// <param name="bufferSize">The size of the memory buffer to be created.</param>
        /// <returns>The memory buffer of size <paramref name="bufferSize"/>.</returns>
        /// <remarks>
        /// This method creates a memory buffer of the specified size using Media Foundation's MFCreateMemoryBuffer function.
        /// The created memory buffer is then returned for further use.
        /// </remarks>
        public static IMFMediaBuffer CreateMemoryBuffer(int bufferSize)
        {
            MediaFoundationInterop.MFCreateMemoryBuffer(bufferSize, out IMFMediaBuffer buffer);
            return buffer;
        }

        /// <summary>
        /// Creates a new Media Foundation sample.
        /// </summary>
        /// <returns>A new instance of <see cref="IMFSample"/> representing the Media Foundation sample.</returns>
        public static IMFSample CreateSample()
        {
            MediaFoundationInterop.MFCreateSample(out IMFSample sample);
            return sample;
        }

        /// <summary>
        /// Creates a new instance of IMFAttributes with the specified initial size.
        /// </summary>
        /// <param name="initialSize">The initial size of the attributes.</param>
        /// <returns>A new instance of IMFAttributes with the specified initial size.</returns>
        /// <remarks>
        /// This method creates a new instance of IMFAttributes with the specified initial size using the MFCreateAttributes method from the Media Foundation Interop library.
        /// </remarks>
        public static IMFAttributes CreateAttributes(int initialSize)
        {
            MediaFoundationInterop.MFCreateAttributes(out IMFAttributes attributes, initialSize);
            return attributes;
        }

        /// <summary>
        /// Creates an IMFByteStream from the given stream object.
        /// </summary>
        /// <param name="stream">The stream object to create the IMFByteStream from.</param>
        /// <returns>The IMFByteStream created from the input <paramref name="stream"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when the input <paramref name="stream"/> is not of type IStream in desktop apps.</exception>
        public static IMFByteStream CreateByteStream(object stream)
        {
            // n.b. UWP apps should use MediaFoundationInterop.MFCreateMFByteStreamOnStreamEx(stream, out byteStream);
            IMFByteStream byteStream;
            
            if (stream is IStream)
            {
                MediaFoundationInterop.MFCreateMFByteStreamOnStream(stream as IStream, out byteStream);
            }
            else
            {
                throw new ArgumentException("Stream must be IStream in desktop apps");
            }
            return byteStream;
        }

        /// <summary>
        /// Creates a source reader from the specified byte stream.
        /// </summary>
        /// <param name="byteStream">The byte stream from which to create the source reader.</param>
        /// <returns>The source reader created from the specified byte stream.</returns>
        /// <remarks>
        /// This method creates a source reader from the provided byte stream using Media Foundation interop.
        /// </remarks>
        public static IMFSourceReader CreateSourceReaderFromByteStream(IMFByteStream byteStream)
        {
            MediaFoundationInterop.MFCreateSourceReaderFromByteStream(byteStream, null, out IMFSourceReader reader);
            return reader;
        }
    }
}
