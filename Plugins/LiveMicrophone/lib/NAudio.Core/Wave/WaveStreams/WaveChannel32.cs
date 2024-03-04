using System;
using NAudio.Wave.SampleProviders;

namespace NAudio.Wave
{
    /// <summary>
    /// Represents Channel for the WaveMixerStream
    /// 32 bit output and 16 bit input
    /// It's output is always stereo
    /// The input stream can be panned
    /// </summary>
    public class WaveChannel32 : WaveStream, ISampleNotifier
    {
        private WaveStream sourceStream;
        private readonly WaveFormat waveFormat;
        private readonly long length;
        private readonly int destBytesPerSample;
        private readonly int sourceBytesPerSample;
        private volatile float volume;
        private volatile float pan;
        private long position;
        private readonly ISampleChunkConverter sampleProvider;
        private readonly object lockObject = new object();

        /// <summary>
        /// Creates a new WaveChannel32
        /// </summary>
        /// <param name="sourceStream">the source stream</param>
        /// <param name="volume">stream volume (1 is 0dB)</param>
        /// <param name="pan">pan control (-1 to 1)</param>
        public WaveChannel32(WaveStream sourceStream, float volume, float pan)
        {
            PadWithZeroes = true;

            var providers = new ISampleChunkConverter[] 
            {
                new Mono8SampleChunkConverter(),
                new Stereo8SampleChunkConverter(),
                new Mono16SampleChunkConverter(),
                new Stereo16SampleChunkConverter(),
                new Mono24SampleChunkConverter(),
                new Stereo24SampleChunkConverter(),
                new MonoFloatSampleChunkConverter(),
                new StereoFloatSampleChunkConverter(),
            };
            foreach (var provider in providers)
            {
                if (provider.Supports(sourceStream.WaveFormat))
                {
                    this.sampleProvider = provider;
                    break;
                }
            }

            if (this.sampleProvider == null)
            {
                throw new ArgumentException("Unsupported sourceStream format");
            }
         
            // always outputs stereo 32 bit
            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sourceStream.WaveFormat.SampleRate, 2);
            destBytesPerSample = 8; // includes stereo factoring

            this.sourceStream = sourceStream;
            this.volume = volume;
            this.pan = pan;
            sourceBytesPerSample = sourceStream.WaveFormat.Channels * sourceStream.WaveFormat.BitsPerSample / 8;

            length = SourceToDest(sourceStream.Length);
            position = 0;
        }

        /// <summary>
        /// Converts the size of the source bytes to the size of the destination bytes based on the sample sizes and returns the result.
        /// </summary>
        /// <param name="sourceBytes">The size of the source bytes to be converted.</param>
        /// <returns>The size of the destination bytes calculated based on the sample sizes.</returns>
        private long SourceToDest(long sourceBytes)
        {
            return (sourceBytes / sourceBytesPerSample) * destBytesPerSample;
        }

        /// <summary>
        /// Converts the destination bytes to source bytes based on the sample sizes and returns the result.
        /// </summary>
        /// <param name="destBytes">The number of destination bytes to be converted.</param>
        /// <returns>The equivalent number of source bytes calculated based on the sample sizes.</returns>
        private long DestToSource(long destBytes)
        {
            return (destBytes / destBytesPerSample) * sourceBytesPerSample;
        }

        /// <summary>
        /// Creates a WaveChannel32 with default settings
        /// </summary>
        /// <param name="sourceStream">The source stream</param>
        public WaveChannel32(WaveStream sourceStream)
            :
            this(sourceStream, 1.0f, 0.0f)
        {
        }

        /// <summary>
        /// Gets the block alignment for this WaveStream
        /// </summary>
        public override int BlockAlign => (int)SourceToDest(sourceStream.BlockAlign);

        /// <summary>
        /// Returns the stream length
        /// </summary>
        public override long Length => length;

        /// <summary>
        /// Gets or sets the current position in the stream
        /// </summary>
        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                lock (lockObject)
                {
                    // make sure we don't get out of sync
                    value -= (value % BlockAlign);
                    if (value < 0)
                    {
                        sourceStream.Position = 0;
                    }
                    else
                    {
                        sourceStream.Position = DestToSource(value);
                    }
                    // source stream may not have accepted the reposition we gave it
                    position = SourceToDest(sourceStream.Position);
                }
            }
        }

        /// <summary>
        /// Reads audio data from the source stream and writes it to the destination buffer.
        /// </summary>
        /// <param name="destBuffer">The destination buffer to write the audio data to.</param>
        /// <param name="offset">The offset in the destination buffer at which to start writing the audio data.</param>
        /// <param name="numBytes">The number of bytes of audio data to read and write to the destination buffer.</param>
        /// <returns>The number of bytes written to the destination buffer.</returns>
        /// <remarks>
        /// This method reads audio data from the source stream and writes it to the destination buffer.
        /// It first fills with silence if the position is less than 0, then loads the next chunk of audio data from the source stream and writes it to the destination buffer.
        /// It implements panning laws and adjusts the volume of the audio data.
        /// If PadWithZeroes is true, it fills out the remaining space in the destination buffer with zeroes.
        /// The position is updated by the number of bytes written to the destination buffer.
        /// </remarks>
        public override int Read(byte[] destBuffer, int offset, int numBytes)
        {
            lock (lockObject)
            {
                int bytesWritten = 0;
                WaveBuffer destWaveBuffer = new WaveBuffer(destBuffer);

                // 1. fill with silence
                if (position < 0)
                {
                    bytesWritten = (int) Math.Min(numBytes, 0 - position);
                    for (int n = 0; n < bytesWritten; n++)
                        destBuffer[n + offset] = 0;
                }
                if (bytesWritten < numBytes)
                {
                    sampleProvider.LoadNextChunk(sourceStream, (numBytes - bytesWritten)/8);
                    float left, right;

                    int outIndex = (offset/4) + bytesWritten/4;
                    while (this.sampleProvider.GetNextSample(out left, out right) && bytesWritten < numBytes)
                    {
                        // implement better panning laws. 
                        left = (pan <= 0) ? left : (left*(1 - pan)/2.0f);
                        right = (pan >= 0) ? right : (right*(pan + 1)/2.0f);
                        left *= volume;
                        right *= volume;
                        destWaveBuffer.FloatBuffer[outIndex++] = left;
                        destWaveBuffer.FloatBuffer[outIndex++] = right;
                        bytesWritten += 8;
                        if (Sample != null) RaiseSample(left, right);
                    }
                }
                // 3. Fill out with zeroes
                if (PadWithZeroes && bytesWritten < numBytes)
                {
                    Array.Clear(destBuffer, offset + bytesWritten, numBytes - bytesWritten);
                    bytesWritten = numBytes;
                }
                position += bytesWritten;
                return bytesWritten;
            }
        }

        /// <summary>
        /// If true, Read always returns the number of bytes requested
        /// </summary>
        public bool PadWithZeroes { get; set; }
      

        /// <summary>
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Volume of this channel. 1.0 = full scale
        /// </summary>
        public float Volume
        {
            get { return volume; }
            set { volume = value; }
        }

        /// <summary>
        /// Pan of this channel (from -1 to 1)
        /// </summary>
        public float Pan
        {
            get { return pan; }
            set { pan = value; }
        }

        /// <summary>
        /// Checks if the source stream has data available based on the specified count.
        /// </summary>
        /// <param name="count">The number of bytes to check for availability.</param>
        /// <returns>True if the source stream has data available for the specified count; otherwise, false.</returns>
        /// <remarks>
        /// This method checks whether the source stream has data available based on the specified count.
        /// It first checks if the source stream has data, and then verifies if the position plus count is not less than 0.
        /// If both conditions are met, it returns true if the position is less than the length and the volume is not equal to 0; otherwise, it returns false.
        /// </remarks>
        public override bool HasData(int count)
        {
            // Check whether the source stream has data.
            bool sourceHasData = sourceStream.HasData(count);

            if (sourceHasData)
            {
                if (position + count < 0)
                    return false;
                return (position < length) && (volume != 0);
            }
            return false;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the WaveChannel32 and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        /// <remarks>
        /// This method releases the unmanaged resources used by the WaveChannel32 and optionally releases the managed resources.
        /// If <paramref name="disposing"/> is true, this method disposes of the sourceStream if it is not null.
        /// If <paramref name="disposing"/> is false, this method asserts that the WaveChannel32 was not disposed.
        /// </remarks>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (sourceStream != null)
                {
                    sourceStream.Dispose();
                    sourceStream = null;
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "WaveChannel32 was not Disposed");
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Sample
        /// </summary>
        public event EventHandler<SampleEventArgs> Sample;

        // reuse the same object every time to avoid making lots of work for the garbage collector
        private SampleEventArgs sampleEventArgs = new SampleEventArgs(0,0);

        /// <summary>
        /// Raises the Sample event with the specified left and right values.
        /// </summary>
        /// <param name="left">The left value to be raised.</param>
        /// <param name="right">The right value to be raised.</param>
        /// <remarks>
        /// This method raises the Sample event with the specified left and right values by updating the <see cref="SampleEventArgs"/> instance and invoking the event.
        /// </remarks>
        private void RaiseSample(float left, float right)
        {
            sampleEventArgs.Left = left;
            sampleEventArgs.Right = right;
            Sample(this, sampleEventArgs);
        }
    }
}
