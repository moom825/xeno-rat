using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using NAudio.MediaFoundation;
using NAudio.Utils;

namespace NAudio.Wave
{
    /// <summary>
    /// Media Foundation Encoder class allows you to use Media Foundation to encode an IWaveProvider
    /// to any supported encoding format
    /// </summary>
    public class MediaFoundationEncoder : IDisposable
    {

        /// <summary>
        /// Retrieves the array of encoded bitrates for the specified audio subtype, sample rate, and channel count.
        /// </summary>
        /// <param name="audioSubtype">The unique identifier for the audio subtype.</param>
        /// <param name="sampleRate">The sample rate of the audio.</param>
        /// <param name="channels">The number of audio channels.</param>
        /// <returns>An array containing the encoded bitrates for the specified audio configuration.</returns>
        public static int[] GetEncodeBitrates(Guid audioSubtype, int sampleRate, int channels)
        {
            return GetOutputMediaTypes(audioSubtype)
                .Where(mt => mt.SampleRate == sampleRate && mt.ChannelCount == channels)
                .Select(mt => mt.AverageBytesPerSecond*8)
                .Distinct()
                .OrderBy(br => br)
                .ToArray();
        }

        /// <summary>
        /// Retrieves the available output media types for the specified audio subtype.
        /// </summary>
        /// <param name="audioSubtype">The GUID of the audio subtype for which to retrieve the output media types.</param>
        /// <returns>An array of MediaType objects representing the available output media types for the specified audio subtype.</returns>
        /// <exception cref="COMException">Thrown when an error occurs while retrieving the available output media types.</exception>
        public static MediaType[] GetOutputMediaTypes(Guid audioSubtype)
        {
            IMFCollection availableTypes;
            try
            {
                MediaFoundationInterop.MFTranscodeGetAudioOutputAvailableTypes(
                    audioSubtype, _MFT_ENUM_FLAG.MFT_ENUM_FLAG_ALL, null, out availableTypes);
            }
            catch (COMException c)
            {
                if (c.GetHResult() == MediaFoundationErrors.MF_E_NOT_FOUND)
                {
                    // Don't worry if we didn't find any - just means no encoder available for this type
                    return new MediaType[0];
                }
                else
                {
                    throw;
                }
            }
            int count;
            availableTypes.GetElementCount(out count);
            var mediaTypes = new List<MediaType>(count);
            for (int n = 0; n < count; n++)
            {
                object mediaTypeObject;
                availableTypes.GetElement(n, out mediaTypeObject);
                var mediaType = (IMFMediaType)mediaTypeObject;
                mediaTypes.Add(new MediaType(mediaType));
            }
            Marshal.ReleaseComObject(availableTypes);
            return mediaTypes.ToArray();
        }

        /// <summary>
        /// Encodes the input audio data to WMA format and writes it to the output stream.
        /// </summary>
        /// <param name="inputProvider">The input audio data provider.</param>
        /// <param name="outputStream">The stream to which the encoded WMA data will be written.</param>
        /// <param name="desiredBitRate">The desired bit rate for the WMA encoding (default is 192000).</param>
        /// <exception cref="InvalidOperationException">Thrown when no suitable WMA encoders are available.</exception>
        /// <remarks>
        /// This method selects the appropriate media type for WMA encoding based on the input audio format and desired bit rate.
        /// It then uses the selected media type to create a MediaFoundationEncoder, which is used to encode the input audio data to WMA format.
        /// The encoded data is written to the output stream in ASF container format.
        /// </remarks>
        public static void EncodeToWma(IWaveProvider inputProvider, string outputFile, int desiredBitRate = 192000)
        {
            var mediaType = SelectMediaType(AudioSubtypes.MFAudioFormat_WMAudioV8, inputProvider.WaveFormat, desiredBitRate);
            if (mediaType == null) throw new InvalidOperationException("No suitable WMA encoders available");
            using (var encoder = new MediaFoundationEncoder(mediaType))
            {
                encoder.Encode(outputFile, inputProvider);
            }
        }

        /// <summary>
        /// Helper function to simplify encoding Window Media Audio
        /// Should be supported on Vista and above (not tested)
        /// </summary>
        /// <param name="inputProvider">Input provider, must be PCM</param>
        /// <param name="outputStream">Output stream</param>
        /// <param name="desiredBitRate">Desired bitrate. Use GetEncodeBitrates to find the possibilities for your input type</param>
        public static void EncodeToWma(IWaveProvider inputProvider, Stream outputStream, int desiredBitRate = 192000) {
            var mediaType = SelectMediaType(AudioSubtypes.MFAudioFormat_WMAudioV8, inputProvider.WaveFormat, desiredBitRate);
            if (mediaType == null) throw new InvalidOperationException("No suitable WMA encoders available");
            using (var encoder = new MediaFoundationEncoder(mediaType)) {
                encoder.Encode(outputStream, inputProvider, TranscodeContainerTypes.MFTranscodeContainerType_ASF);
            }
        }

        /// <summary>
        /// Encodes the input audio data to MP3 format and writes it to the specified output stream using the specified bit rate.
        /// </summary>
        /// <param name="inputProvider">The input audio data provider.</param>
        /// <param name="outputStream">The output stream to write the encoded MP3 data to.</param>
        /// <param name="desiredBitRate">The desired bit rate for the MP3 encoding, defaults to 192000 bps if not specified.</param>
        /// <exception cref="InvalidOperationException">Thrown when no suitable MP3 encoders are available.</exception>
        /// <remarks>
        /// This method selects the appropriate media type for MP3 encoding based on the input audio format and desired bit rate.
        /// It then uses the selected encoder to encode the input audio data and writes the encoded MP3 data to the output stream.
        /// </remarks>
        public static void EncodeToMp3(IWaveProvider inputProvider, string outputFile, int desiredBitRate = 192000)
        {
            var mediaType = SelectMediaType(AudioSubtypes.MFAudioFormat_MP3, inputProvider.WaveFormat, desiredBitRate);
            if (mediaType == null) throw new InvalidOperationException("No suitable MP3 encoders available");
            using (var encoder = new MediaFoundationEncoder(mediaType))
            {
                encoder.Encode(outputFile, inputProvider);
            }
        }

        /// <summary>
        /// Helper function to simplify encoding to MP3
        /// By default, will only be available on Windows 8 and above
        /// </summary>
        /// <param name="inputProvider">Input provider, must be PCM</param>
        /// <param name="outputStream">Output stream</param>
        /// <param name="desiredBitRate">Desired bitrate. Use GetEncodeBitrates to find the possibilities for your input type</param>
        public static void EncodeToMp3(IWaveProvider inputProvider, Stream outputStream, int desiredBitRate = 192000) {
            var mediaType = SelectMediaType(AudioSubtypes.MFAudioFormat_MP3, inputProvider.WaveFormat, desiredBitRate);
            if (mediaType == null) throw new InvalidOperationException("No suitable MP3 encoders available");
            using (var encoder = new MediaFoundationEncoder(mediaType)) {
                encoder.Encode(outputStream, inputProvider, TranscodeContainerTypes.MFTranscodeContainerType_MP3);
            }
        }

        /// <summary>
        /// Encodes the input audio data to AAC format and writes the result to the specified output stream.
        /// </summary>
        /// <param name="inputProvider">The input audio data provider.</param>
        /// <param name="outputStream">The stream to which the encoded AAC data will be written.</param>
        /// <param name="desiredBitRate">The desired bit rate for the AAC encoding (default is 192000).</param>
        /// <exception cref="InvalidOperationException">Thrown when no suitable AAC encoders are available.</exception>
        /// <remarks>
        /// This method selects the appropriate media type for AAC encoding based on the input audio format and desired bit rate.
        /// It then uses the selected media type to create a MediaFoundationEncoder, which is used to encode the input audio data to AAC format.
        /// The encoded AAC data is written to the specified output stream.
        /// </remarks>
        public static void EncodeToAac(IWaveProvider inputProvider, string outputFile, int desiredBitRate = 192000)
        {
            // Information on configuring an AAC media type can be found here:
            // http://msdn.microsoft.com/en-gb/library/windows/desktop/dd742785%28v=vs.85%29.aspx
            var mediaType = SelectMediaType(AudioSubtypes.MFAudioFormat_AAC, inputProvider.WaveFormat, desiredBitRate);
            if (mediaType == null) throw new InvalidOperationException("No suitable AAC encoders available");
            using (var encoder = new MediaFoundationEncoder(mediaType))
            {
                // should AAC container have ADTS, or is that just for ADTS?
                // http://www.hydrogenaudio.org/forums/index.php?showtopic=97442
                encoder.Encode(outputFile, inputProvider);
            }
        }

        /// <summary>
        /// Helper function to simplify encoding to AAC
        /// By default, will only be available on Windows 7 and above
        /// </summary>
        /// <param name="inputProvider">Input provider, must be PCM</param>
        /// <param name="outputStream">Output stream</param>
        /// <param name="desiredBitRate">Desired bitrate. Use GetEncodeBitrates to find the possibilities for your input type</param>
        public static void EncodeToAac(IWaveProvider inputProvider, Stream outputStream, int desiredBitRate = 192000) {
            // Information on configuring an AAC media type can be found here:
            // http://msdn.microsoft.com/en-gb/library/windows/desktop/dd742785%28v=vs.85%29.aspx
            var mediaType = SelectMediaType(AudioSubtypes.MFAudioFormat_AAC, inputProvider.WaveFormat, desiredBitRate);
            if (mediaType == null) throw new InvalidOperationException("No suitable AAC encoders available");
            using (var encoder = new MediaFoundationEncoder(mediaType)) {
                // should AAC container have ADTS, or is that just for ADTS?
                // http://www.hydrogenaudio.org/forums/index.php?showtopic=97442
                encoder.Encode(outputStream, inputProvider, TranscodeContainerTypes.MFTranscodeContainerType_MPEG4);
            }
        }

        /// <summary>
        /// Selects the media type that best matches the input audio subtype, format, and desired bit rate.
        /// </summary>
        /// <param name="audioSubtype">The audio subtype to be matched.</param>
        /// <param name="inputFormat">The input wave format.</param>
        /// <param name="desiredBitRate">The desired bit rate in bits per second.</param>
        /// <returns>The media type that best matches the input criteria, or null if no match is found.</returns>
        /// <remarks>
        /// This method selects the media type that best matches the input audio subtype, wave format, and desired bit rate from the available output media types.
        /// It filters the output media types based on sample rate and channel count, calculates the delta between the desired bit rate and the average bytes per second of each media type,
        /// orders the filtered media types by delta, and returns the first matching media type or null if no match is found.
        /// </remarks>
        public static MediaType SelectMediaType(Guid audioSubtype, WaveFormat inputFormat, int desiredBitRate)
        {
            return GetOutputMediaTypes(audioSubtype)
                .Where(mt => mt.SampleRate == inputFormat.SampleRate && mt.ChannelCount == inputFormat.Channels)
                .Select(mt => new { MediaType = mt, Delta = Math.Abs(desiredBitRate - mt.AverageBytesPerSecond * 8) } )
                .OrderBy(mt => mt.Delta)
                .Select(mt => mt.MediaType)
                .FirstOrDefault();
        }

        private readonly MediaType outputMediaType;
        private bool disposed;

        /// <summary>
        /// Creates a new encoder that encodes to the specified output media type
        /// </summary>
        /// <param name="outputMediaType">Desired output media type</param>
        public MediaFoundationEncoder(MediaType outputMediaType)
        {
            if (outputMediaType == null) throw new ArgumentNullException("outputMediaType");
            this.outputMediaType = outputMediaType;
        }

        /// <summary>
        /// Encodes the input audio data from <paramref name="inputProvider"/> into the specified <paramref name="transcodeContainerType"/> and writes the result to the <paramref name="outputStream"/>.
        /// </summary>
        /// <param name="outputStream">The stream to which the encoded audio data will be written.</param>
        /// <param name="inputProvider">The input audio data provider.</param>
        /// <param name="transcodeContainerType">The GUID specifying the container type for the transcoded audio data.</param>
        /// <exception cref="ArgumentException">Thrown when the input format of <paramref name="inputProvider"/> is not PCM or IEEE float.</exception>
        /// <remarks>
        /// This method encodes the input audio data using the specified <paramref name="transcodeContainerType"/> and writes the result to the <paramref name="outputStream"/>.
        /// It first checks if the input format is PCM or IEEE float, and then creates a media type based on the input format.
        /// It then creates a sink writer and adds a stream with the output media type, sets the input media type, and performs the encoding.
        /// Finally, it releases the allocated COM objects.
        /// </remarks>
        public void Encode(string outputFile, IWaveProvider inputProvider)
        {
            if (inputProvider.WaveFormat.Encoding != WaveFormatEncoding.Pcm && inputProvider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                throw new ArgumentException("Encode input format must be PCM or IEEE float");
            }

            var inputMediaType = new MediaType(inputProvider.WaveFormat);

            var writer = CreateSinkWriter(outputFile);
            try
            {
                int streamIndex;
                writer.AddStream(outputMediaType.MediaFoundationObject, out streamIndex);

                // n.b. can get 0xC00D36B4 - MF_E_INVALIDMEDIATYPE here
                writer.SetInputMediaType(streamIndex, inputMediaType.MediaFoundationObject, null);

                PerformEncode(writer, streamIndex, inputProvider);
            }
            finally
            {
                Marshal.ReleaseComObject(writer);
                Marshal.ReleaseComObject(inputMediaType.MediaFoundationObject);
            }
        }

        /// <summary>
        /// Encodes a file
        /// </summary>
        /// <param name="outputStream">Output stream</param>
        /// <param name="inputProvider">Input provider (should be PCM, some encoders will also allow IEEE float)</param>
        /// <param name="transcodeContainerType">One of <see cref="TranscodeContainerTypes"/></param>
        public void Encode(Stream outputStream, IWaveProvider inputProvider, Guid transcodeContainerType) {
            if (inputProvider.WaveFormat.Encoding != WaveFormatEncoding.Pcm && inputProvider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat) {
                throw new ArgumentException("Encode input format must be PCM or IEEE float");
            }

            var inputMediaType = new MediaType(inputProvider.WaveFormat);

            var writer = CreateSinkWriter(new ComStream(outputStream), transcodeContainerType);
            try {
				writer.AddStream(outputMediaType.MediaFoundationObject, out int streamIndex);

				// n.b. can get 0xC00D36B4 - MF_E_INVALIDMEDIATYPE here
				writer.SetInputMediaType(streamIndex, inputMediaType.MediaFoundationObject, null);

                PerformEncode(writer, streamIndex, inputProvider);
            } finally {
                Marshal.ReleaseComObject(writer);
                Marshal.ReleaseComObject(inputMediaType.MediaFoundationObject);
            }
        }

        /// <summary>
        /// Creates a sink writer for writing media data to the specified output stream with the given transcode container type.
        /// </summary>
        /// <param name="outputStream">The output stream to write the media data to.</param>
        /// <param name="TranscodeContainerType">The GUID representing the transcode container type.</param>
        /// <returns>The sink writer for writing media data to the specified output stream.</returns>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown if there is an error creating the sink writer.</exception>
        private static IMFSinkWriter CreateSinkWriter(string outputFile)
        {
            // n.b. could try specifying the container type using attributes, but I think
            // it does a decent job of working it out from the file extension 
            // n.b. AAC encode on Win 8 can have AAC extension, but use MP4 in win 7
            // http://msdn.microsoft.com/en-gb/library/windows/desktop/dd389284%28v=vs.85%29.aspx
            IMFSinkWriter writer;
            var attributes = MediaFoundationApi.CreateAttributes(1);
            attributes.SetUINT32(MediaFoundationAttributes.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, 1);
            try
            {
                MediaFoundationInterop.MFCreateSinkWriterFromURL(outputFile, null, attributes, out writer);
            }
            catch (COMException e)
            {
                if (e.GetHResult() == MediaFoundationErrors.MF_E_NOT_FOUND)
                {
                    throw new ArgumentException("Was not able to create a sink writer for this file extension");
                }
                throw;
            }
            finally
            {
                Marshal.ReleaseComObject(attributes);
            }
            return writer;
        }

        private static IMFSinkWriter CreateSinkWriter(IStream outputStream, Guid TranscodeContainerType) {
            // n.b. could try specifying the container type using attributes, but I think
            // it does a decent job of working it out from the file extension 
            // n.b. AAC encode on Win 8 can have AAC extension, but use MP4 in win 7
            // http://msdn.microsoft.com/en-gb/library/windows/desktop/dd389284%28v=vs.85%29.aspx
            IMFSinkWriter writer;
            var attributes = MediaFoundationApi.CreateAttributes(1);
            attributes.SetGUID(MediaFoundationAttributes.MF_TRANSCODE_CONTAINERTYPE, TranscodeContainerType);
            try {
                MediaFoundationInterop.MFCreateMFByteStreamOnStream(outputStream, out var ppByteStream);
                MediaFoundationInterop.MFCreateSinkWriterFromURL(null, ppByteStream, attributes, out writer);
            } finally {
                Marshal.ReleaseComObject(attributes);
            }
            return writer;
        }

        /// <summary>
        /// Performs encoding of the input audio data using the specified sink writer and wave provider.
        /// </summary>
        /// <param name="writer">The sink writer used for writing the encoded data.</param>
        /// <param name="streamIndex">The index of the stream to write the encoded data to.</param>
        /// <param name="inputProvider">The wave provider containing the input audio data to be encoded.</param>
        /// <remarks>
        /// This method performs encoding of the input audio data using the specified sink writer and wave provider. It iterates through the input data, converts and writes each buffer to the specified stream index, and finalizes the writing process once all data has been processed.
        /// </remarks>
        private void PerformEncode(IMFSinkWriter writer, int streamIndex, IWaveProvider inputProvider)
        {
            int maxLength = inputProvider.WaveFormat.AverageBytesPerSecond * 4;
            var managedBuffer = new byte[maxLength];

            writer.BeginWriting();

            long position = 0;
            long duration;
            do
            {
                duration = ConvertOneBuffer(writer, streamIndex, inputProvider, position, managedBuffer);
                position += duration;
            } while (duration > 0);

            writer.DoFinalize();
        }

        /// <summary>
        /// Converts the specified number of bytes to the corresponding position in nanoseconds based on the provided WaveFormat.
        /// </summary>
        /// <param name="bytes">The number of bytes to be converted.</param>
        /// <param name="waveFormat">The WaveFormat object containing information about the audio format.</param>
        /// <returns>The position in nanoseconds corresponding to the specified number of bytes based on the provided WaveFormat.</returns>
        private static long BytesToNsPosition(int bytes, WaveFormat waveFormat)
        {
            long nsPosition = (10000000L * bytes) / waveFormat.AverageBytesPerSecond;
            return nsPosition;
        }

        /// <summary>
        /// Converts data from the inputProvider to a memory buffer and writes it to the sink writer at the specified position.
        /// </summary>
        /// <param name="writer">The sink writer to which the data will be written.</param>
        /// <param name="streamIndex">The index of the stream in the sink writer.</param>
        /// <param name="inputProvider">The input wave provider from which data will be read.</param>
        /// <param name="position">The position in nanoseconds at which the data will be written.</param>
        /// <param name="managedBuffer">The managed buffer used for data conversion.</param>
        /// <returns>The duration of the converted data in nanoseconds.</returns>
        /// <remarks>
        /// This method converts data from the inputProvider to a memory buffer, sets the sample time and duration, and writes it to the sink writer at the specified position.
        /// If the read operation from the inputProvider is successful, the method returns the duration of the converted data in nanoseconds.
        /// If the read operation fails, no data is written to the sink writer, and the method returns 0.
        /// </remarks>
        private long ConvertOneBuffer(IMFSinkWriter writer, int streamIndex, IWaveProvider inputProvider, long position, byte[] managedBuffer)
        {
            long durationConverted = 0;
            int maxLength;
            IMFMediaBuffer buffer = MediaFoundationApi.CreateMemoryBuffer(managedBuffer.Length);
            buffer.GetMaxLength(out maxLength);

            IMFSample sample = MediaFoundationApi.CreateSample();
            sample.AddBuffer(buffer);

            IntPtr ptr;
            int currentLength;
            buffer.Lock(out ptr, out maxLength, out currentLength);
            int read = inputProvider.Read(managedBuffer, 0, maxLength);
            if (read > 0)
            {
                durationConverted = BytesToNsPosition(read, inputProvider.WaveFormat);
                Marshal.Copy(managedBuffer, 0, ptr, read);
                buffer.SetCurrentLength(read);
                buffer.Unlock();
                sample.SetSampleTime(position);
                sample.SetSampleDuration(durationConverted);
                writer.WriteSample(streamIndex, sample);
                //writer.Flush(streamIndex);
            }
            else
            {
                buffer.Unlock();
            }

            Marshal.ReleaseComObject(sample);
            Marshal.ReleaseComObject(buffer);
            return durationConverted;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <remarks>
        /// This method disposes of unmanaged resources and suppresses the finalization of the object.
        /// </remarks>
        protected void Dispose(bool disposing)
        {
            Marshal.ReleaseComObject(outputMediaType.MediaFoundationObject);
        }

        /// <summary>
        /// Disposes this instance
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                Dispose(true);
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~MediaFoundationEncoder()
        {
            Dispose(false);
        }
    }
}
