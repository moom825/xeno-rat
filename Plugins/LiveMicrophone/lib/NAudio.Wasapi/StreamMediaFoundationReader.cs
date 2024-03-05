using System;
using System.IO;
using NAudio.MediaFoundation;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// MediaFoundationReader supporting reading from a stream
    /// </summary>
    public class StreamMediaFoundationReader : MediaFoundationReader
    {
        private readonly Stream stream;

        /// <summary>
        /// Constructs a new media foundation reader from a stream
        /// </summary>
        public StreamMediaFoundationReader(Stream stream, MediaFoundationReaderSettings settings = null)
        {
            this.stream = stream;
            Init(settings);
        }

        /// <summary>
        /// Creates a new instance of IMFSourceReader with the specified settings.
        /// </summary>
        /// <param name="settings">The settings for the new IMFSourceReader instance.</param>
        /// <returns>A new instance of IMFSourceReader configured based on the provided settings.</returns>
        /// <remarks>
        /// This method creates a new IMFSourceReader from the byte stream created using the provided stream.
        /// It then sets the stream selection for audio and video, and configures the current media type for audio based on the specified settings.
        /// </remarks>
        protected override IMFSourceReader CreateReader(MediaFoundationReaderSettings settings)
        {
            var ppSourceReader = MediaFoundationApi.CreateSourceReaderFromByteStream(MediaFoundationApi.CreateByteStream(new ComStream(stream)));

            ppSourceReader.SetStreamSelection(-2, false);
            ppSourceReader.SetStreamSelection(-3, true);
            ppSourceReader.SetCurrentMediaType(-3, IntPtr.Zero, new MediaType
            {
                MajorType = MediaTypes.MFMediaType_Audio,
                SubType = settings.RequestFloatOutput ? AudioSubtypes.MFAudioFormat_Float : AudioSubtypes.MFAudioFormat_PCM
            }.MediaFoundationObject);

            return ppSourceReader;
        }
    }
}
