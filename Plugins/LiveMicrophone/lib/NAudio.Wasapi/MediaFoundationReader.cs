using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.MediaFoundation;
using NAudio.Utils;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Class for reading any file that Media Foundation can play
    /// Will only work in Windows Vista and above
    /// Automatically converts to PCM
    /// If it is a video file with multiple audio streams, it will pick out the first audio stream
    /// </summary>
    public class MediaFoundationReader : WaveStream
    {
        private WaveFormat waveFormat;
        private long length;
        private MediaFoundationReaderSettings settings;
        private readonly string file;
        private IMFSourceReader pReader;

        private long position;

        /// <summary>
        /// Allows customisation of this reader class
        /// </summary>
        public class MediaFoundationReaderSettings
        {
            /// <summary>
            /// Sets up the default settings for MediaFoundationReader
            /// </summary>
            public MediaFoundationReaderSettings()
            {
                RepositionInRead = true;
            }

            /// <summary>
            /// Allows us to request IEEE float output (n.b. no guarantee this will be accepted)
            /// </summary>
            public bool RequestFloatOutput { get; set; }
            /// <summary>
            /// If true, the reader object created in the constructor is used in Read
            /// Should only be set to true if you are working entirely on an STA thread, or 
            /// entirely with MTA threads.
            /// </summary>
            public bool SingleReaderObject { get; set; }
            /// <summary>
            /// If true, the reposition does not happen immediately, but waits until the
            /// next call to read to be processed.
            /// </summary>
            public bool RepositionInRead { get; set; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        protected MediaFoundationReader()
        {
        }
        
        /// <summary>
        /// Creates a new MediaFoundationReader based on the supplied file
        /// </summary>
        /// <param name="file">Filename (can also be a URL  e.g. http:// mms:// file://)</param>
        public MediaFoundationReader(string file)
            : this(file, null)
        {
        }


        /// <summary>
        /// Creates a new MediaFoundationReader based on the supplied file
        /// </summary>
        /// <param name="file">Filename</param>
        /// <param name="settings">Advanced settings</param>
        public MediaFoundationReader(string file, MediaFoundationReaderSettings settings)
        {
            this.file = file;
            Init(settings);
        }

        /// <summary>
        /// Initializes the MediaFoundationReader with the specified settings.
        /// </summary>
        /// <param name="initialSettings">The initial settings for the MediaFoundationReader.</param>
        /// <remarks>
        /// This method initializes the MediaFoundation API, sets the specified initial settings, creates a reader based on the settings, retrieves the wave format, sets the stream selection for audio, and gets the length of the reader.
        /// If the <paramref name="initialSettings"/> is null, default settings are used.
        /// If the <paramref name="SingleReaderObject"/> property is set to true in the settings, the reader object is retained; otherwise, it is released.
        /// </remarks>
        protected void Init(MediaFoundationReaderSettings initialSettings)
        {
            MediaFoundationApi.Startup();
            settings = initialSettings ?? new MediaFoundationReaderSettings();
            var reader = CreateReader(settings);

            waveFormat = GetCurrentWaveFormat(reader);

            reader.SetStreamSelection(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, true);
            length = GetLength(reader);

            if (settings.SingleReaderObject)
            {
                pReader = reader;
            }
            else
            {
                Marshal.ReleaseComObject(reader);
            }
        }

        /// <summary>
        /// Retrieves the current wave format from the specified IMFSourceReader.
        /// </summary>
        /// <param name="reader">The IMFSourceReader from which to retrieve the wave format.</param>
        /// <returns>The WaveFormat corresponding to the current audio stream's media type.</returns>
        /// <exception cref="InvalidDataException">Thrown when the audio sub type is not supported.</exception>
        /// <remarks>
        /// This method retrieves the current media type of the first audio stream from the specified IMFSourceReader and extracts relevant information such as the audio sub type, number of channels, bits per sample, and sample rate.
        /// It then determines the appropriate WaveFormat based on the audio sub type and returns it.
        /// If the audio sub type is not supported, an InvalidDataException is thrown with a descriptive message indicating the unsupported sub type.
        /// </remarks>
        private WaveFormat GetCurrentWaveFormat(IMFSourceReader reader)
        {
            reader.GetCurrentMediaType(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, out IMFMediaType uncompressedMediaType);

            // Two ways to query it, first is to ask for properties (second is to convert into WaveFormatEx using MFCreateWaveFormatExFromMFMediaType)
            var outputMediaType = new MediaType(uncompressedMediaType);
            Guid actualMajorType = outputMediaType.MajorType;
            Debug.Assert(actualMajorType == MediaTypes.MFMediaType_Audio);
            Guid audioSubType = outputMediaType.SubType;
            int channels = outputMediaType.ChannelCount;
            int bits = outputMediaType.BitsPerSample;
            int sampleRate = outputMediaType.SampleRate;

            if (audioSubType == AudioSubtypes.MFAudioFormat_PCM)
                return new WaveFormat(sampleRate, bits, channels);
            if (audioSubType == AudioSubtypes.MFAudioFormat_Float)
                return WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            var subTypeDescription = FieldDescriptionHelper.Describe(typeof (AudioSubtypes), audioSubType);
            throw new InvalidDataException($"Unsupported audio sub Type {subTypeDescription}");
        }

        /// <summary>
        /// Retrieves the current media type of the specified IMFSourceReader.
        /// </summary>
        /// <param name="reader">The IMFSourceReader from which to retrieve the current media type.</param>
        /// <returns>A new MediaType object representing the current media type of the IMFSourceReader.</returns>
        /// <remarks>
        /// This method retrieves the current media type of the specified IMFSourceReader for the first audio stream.
        /// It then creates a new MediaType object based on the retrieved media type and returns it.
        /// </remarks>
        private static MediaType GetCurrentMediaType(IMFSourceReader reader)
        {
            reader.GetCurrentMediaType(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, out IMFMediaType mediaType);
            return new MediaType(mediaType);
        }

        /// <summary>
        /// Creates a Media Foundation source reader with the specified settings.
        /// </summary>
        /// <param name="settings">The settings for the Media Foundation reader.</param>
        /// <returns>The Media Foundation source reader created with the specified settings.</returns>
        /// <remarks>
        /// This method creates a Media Foundation source reader from the specified file URL and sets the stream selection for audio.
        /// It then creates a partial media type indicating the desired audio format and sets it as the current media type for the reader.
        /// If the specified media type is not supported, it may adjust the sample rate and channel count to accommodate certain audio formats.
        /// </remarks>
        /// <exception cref="COMException">Thrown when the specified media type is not supported (MF_E_INVALIDMEDIATYPE).</exception>
        protected virtual IMFSourceReader CreateReader(MediaFoundationReaderSettings settings)
        {
            IMFSourceReader reader;
            MediaFoundationInterop.MFCreateSourceReaderFromURL(file, null, out reader);
            reader.SetStreamSelection(MediaFoundationInterop.MF_SOURCE_READER_ALL_STREAMS, false);
            reader.SetStreamSelection(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, true);

            // Create a partial media type indicating that we want uncompressed PCM audio

            var partialMediaType = new MediaType();
            partialMediaType.MajorType = MediaTypes.MFMediaType_Audio;
            partialMediaType.SubType = settings.RequestFloatOutput ? AudioSubtypes.MFAudioFormat_Float : AudioSubtypes.MFAudioFormat_PCM;

            var currentMediaType = GetCurrentMediaType(reader);

            // mono, low sample rate files can go wrong on Windows 10 unless we specify here
            partialMediaType.ChannelCount = currentMediaType.ChannelCount;
            partialMediaType.SampleRate = currentMediaType.SampleRate;

            try
            {
                // set the media type
                // can return MF_E_INVALIDMEDIATYPE if not supported
                reader.SetCurrentMediaType(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, IntPtr.Zero, partialMediaType.MediaFoundationObject);
            }
            catch (COMException ex) when (ex.GetHResult() == MediaFoundationErrors.MF_E_INVALIDMEDIATYPE)
            {               
                // HE-AAC (and v2) seems to halve the samplerate
                if (currentMediaType.SubType == AudioSubtypes.MFAudioFormat_AAC && currentMediaType.ChannelCount == 1)
                {
                    partialMediaType.SampleRate = currentMediaType.SampleRate *= 2;
                    partialMediaType.ChannelCount = currentMediaType.ChannelCount *= 2;
                    reader.SetCurrentMediaType(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, IntPtr.Zero, partialMediaType.MediaFoundationObject);
                }
                else { throw; }
            }

            Marshal.ReleaseComObject(currentMediaType.MediaFoundationObject);
            return reader;
        }

        /// <summary>
        /// Gets the length of the media source in bytes.
        /// </summary>
        /// <param name="reader">The IMFSourceReader used to read the media source.</param>
        /// <returns>The length of the media source in bytes.</returns>
        /// <remarks>
        /// This method retrieves the duration of the media source using the GetPresentationAttribute method of the IMFSourceReader interface.
        /// If the duration attribute is not found, it returns 0, indicating that the media source does not support providing its duration (e.g., streaming media).
        /// If an error occurs during the retrieval of the duration attribute, an exception is thrown using Marshal.ThrowExceptionForHR.
        /// The method then calculates the length in bytes based on the retrieved duration and the average bytes per second of the wave format.
        /// </remarks>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when an error occurs during the retrieval of the duration attribute.</exception>
        private long GetLength(IMFSourceReader reader)
        {
            var variantPtr = Marshal.AllocHGlobal(Marshal.SizeOf<PropVariant>());
            try
            {

                // http://msdn.microsoft.com/en-gb/library/windows/desktop/dd389281%28v=vs.85%29.aspx#getting_file_duration
                int hResult = reader.GetPresentationAttribute(MediaFoundationInterop.MF_SOURCE_READER_MEDIASOURCE,
                    MediaFoundationAttributes.MF_PD_DURATION, variantPtr);
                if (hResult == MediaFoundationErrors.MF_E_ATTRIBUTENOTFOUND)
                {
                    // this doesn't support telling us its duration (might be streaming)
                    return 0;
                }
                if (hResult != 0)
                {
                    Marshal.ThrowExceptionForHR(hResult);
                }
                var variant = Marshal.PtrToStructure<PropVariant>(variantPtr);

                var lengthInBytes = (((long)variant.Value) * waveFormat.AverageBytesPerSecond) / 10000000L;
                return lengthInBytes;
            }
            finally 
            {
                PropVariant.Clear(variantPtr);
                Marshal.FreeHGlobal(variantPtr);
            }
        }

        private byte[] decoderOutputBuffer;
        private int decoderOutputOffset;
        private int decoderOutputCount;

        /// <summary>
        /// Ensures that the decoder output buffer has the required capacity.
        /// </summary>
        /// <param name="bytesRequired">The number of bytes required for the buffer.</param>
        /// <remarks>
        /// This method checks if the decoder output buffer is null or has insufficient capacity to hold the required number of bytes.
        /// If the buffer is null or smaller than the required size, a new buffer with the required capacity is created.
        /// </remarks>
        private void EnsureBuffer(int bytesRequired)
        {
            if (decoderOutputBuffer == null || decoderOutputBuffer.Length < bytesRequired)
            {
                decoderOutputBuffer = new byte[bytesRequired];
            }
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes written into buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero if the end of the stream is reached before any bytes are read.</returns>
        /// <remarks>
        /// This method reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read. If the end of the stream is reached before any bytes are read, it returns zero. The method will continue to read until count bytes have been read or the end of the stream is reached. This method may block until at least one byte is available or end of the stream is reached.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown when there is a Media Foundation read error.</exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (pReader == null)
            {
                pReader = CreateReader(settings);
            }
            if (repositionTo != -1)
            {
                Reposition(repositionTo);
            }

            int bytesWritten = 0;
            // read in any leftovers from last time
            if (decoderOutputCount > 0)
            {
                bytesWritten += ReadFromDecoderBuffer(buffer, offset, count - bytesWritten);
            }

            while (bytesWritten < count)
            {
                pReader.ReadSample(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, 0, 
                    out int actualStreamIndex, out MF_SOURCE_READER_FLAG dwFlags, out ulong timestamp, out IMFSample pSample);
                if ((dwFlags & MF_SOURCE_READER_FLAG.MF_SOURCE_READERF_ENDOFSTREAM) != 0)
                {
                    // reached the end of the stream
                    break;
                }
                else if ((dwFlags & MF_SOURCE_READER_FLAG.MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED) != 0)
                {
                    waveFormat = GetCurrentWaveFormat(pReader);
                    OnWaveFormatChanged();
                    // carry on, but user must handle the change of format
                }
                else if (dwFlags != 0)
                {
                    throw new InvalidOperationException($"MediaFoundationReadError {dwFlags}");
                }

                pSample.ConvertToContiguousBuffer(out IMFMediaBuffer pBuffer);
                pBuffer.Lock(out IntPtr pAudioData, out int pcbMaxLength, out int cbBuffer);
                EnsureBuffer(cbBuffer);
                Marshal.Copy(pAudioData, decoderOutputBuffer, 0, cbBuffer);
                decoderOutputOffset = 0;
                decoderOutputCount = cbBuffer;

                bytesWritten += ReadFromDecoderBuffer(buffer, offset + bytesWritten, count - bytesWritten);

                pBuffer.Unlock();
                Marshal.ReleaseComObject(pBuffer);
                Marshal.ReleaseComObject(pSample);
            }
            position += bytesWritten;
            return bytesWritten;
        }

        /// <summary>
        /// Reads bytes from the decoder output buffer into the provided buffer.
        /// </summary>
        /// <param name="buffer">The destination buffer to copy the bytes into.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes.</param>
        /// <param name="needed">The number of bytes needed to be read from the decoder output buffer.</param>
        /// <returns>The actual number of bytes read from the decoder output buffer, which is the minimum of <paramref name="needed"/> and the available bytes in the decoder output buffer.</returns>
        private int ReadFromDecoderBuffer(byte[] buffer, int offset, int needed)
        {
            int bytesFromDecoderOutput = Math.Min(needed, decoderOutputCount);
            Array.Copy(decoderOutputBuffer, decoderOutputOffset, buffer, offset, bytesFromDecoderOutput);
            decoderOutputOffset += bytesFromDecoderOutput;
            decoderOutputCount -= bytesFromDecoderOutput;
            if (decoderOutputCount == 0)
            {
                decoderOutputOffset = 0;
            }
            return bytesFromDecoderOutput;
        }

        /// <summary>
        /// WaveFormat of this stream (n.b. this is after converting to PCM)
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

        /// <summary>
        /// The bytesRequired of this stream in bytes (n.b may not be accurate)
        /// </summary>
        public override long Length
        {
            get
            {
                return length;
            }
        }

        /// <summary>
        /// Current position within this stream
        /// </summary>
        public override long Position
        {
            get { return position; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", "Position cannot be less than 0");
                if (settings.RepositionInRead)
                {
                    repositionTo = value;
                    position = value; // for gui apps, make it look like we have alread processed the reposition
                }
                else
                {
                    Reposition(value);
                }
            }
        }

        private long repositionTo = -1;

        /// <summary>
        /// Repositions the media player to the desired position in time.
        /// </summary>
        /// <param name="desiredPosition">The desired position in time to reposition the media player to.</param>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when there is a failure in setting the current position in the media player.</exception>
        /// <remarks>
        /// This method calculates the position in 100 nanosecond units based on the desired position and the average bytes per second of the wave format.
        /// It then creates a PropVariant from the calculated position and sets the current position in the media player using the PropVariant.
        /// After repositioning, it resets the decoder output count and offset, updates the position, and clears the reposition flag.
        /// </remarks>
        private void Reposition(long desiredPosition)
        {
            long nsPosition = (10000000L * repositionTo) / waveFormat.AverageBytesPerSecond;
            var pv = PropVariant.FromLong(nsPosition);
            var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(pv));
            try
            {
                Marshal.StructureToPtr(pv, ptr, false);

                // should pass in a variant of type VT_I8 which is a long containing time in 100nanosecond units
                pReader.SetCurrentPosition(Guid.Empty, ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            decoderOutputCount = 0;
            decoderOutputOffset = 0;
            position = desiredPosition;
            repositionTo = -1;// clear the flag
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="ClassName"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        /// <remarks>
        /// This method releases the unmanaged resources used by the <see cref="ClassName"/>. If the <paramref name="disposing"/> parameter is true, this method also releases all managed resources that this <see cref="ClassName"/> holds.
        /// </remarks>
        protected override void Dispose(bool disposing)
        {
            if (pReader != null)
            {
                Marshal.ReleaseComObject(pReader);
                pReader = null;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// WaveFormat has changed
        /// </summary>
        public event EventHandler WaveFormatChanged;

        /// <summary>
        /// Raises the WaveFormatChanged event.
        /// </summary>
        /// <remarks>
        /// This method raises the WaveFormatChanged event, indicating that the wave format has been changed.
        /// </remarks>
        private void OnWaveFormatChanged()
        {
            var handler = WaveFormatChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
