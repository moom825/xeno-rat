using System;
using System.Linq;
using NAudio.Dsp;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Fully managed resampling sample provider, based on the WDL Resampler
    /// </summary>
    public class WdlResamplingSampleProvider : ISampleProvider
    {
        private readonly WdlResampler resampler;
        private readonly WaveFormat outFormat;
        private readonly ISampleProvider source;
        private readonly int channels;

        /// <summary>
        /// Constructs a new resampler
        /// </summary>
        /// <param name="source">Source to resample</param>
        /// <param name="newSampleRate">Desired output sample rate</param>
        public WdlResamplingSampleProvider(ISampleProvider source, int newSampleRate)
        {
            channels = source.WaveFormat.Channels;
            outFormat = WaveFormat.CreateIeeeFloatWaveFormat(newSampleRate, channels);
            this.source = source;

            resampler = new WdlResampler();
            resampler.SetMode(true, 2, false);
            resampler.SetFilterParms();
            resampler.SetFeedMode(false); // output driven
            resampler.SetRates(source.WaveFormat.SampleRate, newSampleRate);
        }

        /// <summary>
        /// Reads data from the source into the buffer and returns the number of frames read.
        /// </summary>
        /// <param name="buffer">The buffer to store the read data.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of frames to read.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the buffer is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when offset or count is less than 0.</exception>
        /// <returns>The number of frames read into the buffer.</returns>
        /// <remarks>
        /// This method reads data from the source into the buffer and performs resampling if necessary to match the requested number of frames.
        /// It returns the actual number of frames read into the buffer after resampling.
        /// </remarks>
        public int Read(float[] buffer, int offset, int count)
        {
            float[] inBuffer;
            int inBufferOffset;
            int framesRequested = count / channels;
            int inNeeded = resampler.ResamplePrepare(framesRequested, outFormat.Channels, out inBuffer, out inBufferOffset);
            int inAvailable = source.Read(inBuffer, inBufferOffset, inNeeded * channels) / channels;
            int outAvailable = resampler.ResampleOut(buffer, offset, inAvailable, framesRequested, channels);
            return outAvailable * channels;
        }

        /// <summary>
        /// Output WaveFormat
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return outFormat; }
        }
    }
}
