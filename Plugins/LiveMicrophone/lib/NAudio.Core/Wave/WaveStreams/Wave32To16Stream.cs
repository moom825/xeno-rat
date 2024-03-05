using System;
using NAudio.Utils;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// WaveStream that converts 32 bit audio back down to 16 bit, clipping if necessary
    /// </summary>
    public class Wave32To16Stream : WaveStream
    {
        private WaveStream sourceStream;
        private readonly WaveFormat waveFormat;
        private readonly long length;
        private long position;
        private bool clip;
        private float volume;
        private readonly object lockObject = new object();

        /// <summary>
        /// The <see cref="Read"/> method reuses the same buffer to prevent
        /// unnecessary allocations.
        /// </summary>
        private byte[] sourceBuffer;

        /// <summary>
        /// Creates a new Wave32To16Stream
        /// </summary>
        /// <param name="sourceStream">the source stream</param>
        public Wave32To16Stream(WaveStream sourceStream)
        {
            if (sourceStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                throw new ArgumentException("Only 32 bit Floating point supported");
            if (sourceStream.WaveFormat.BitsPerSample != 32)
                throw new ArgumentException("Only 32 bit Floating point supported");

            waveFormat = new WaveFormat(sourceStream.WaveFormat.SampleRate, 16, sourceStream.WaveFormat.Channels);
            volume = 1.0f;
            this.sourceStream = sourceStream;
            length = sourceStream.Length / 2;
            position = sourceStream.Position / 2;
        }

        /// <summary>
        /// Sets the volume for this stream. 1.0f is full scale
        /// </summary>
        public float Volume
        {
            get
            {
                return volume;
            }
            set
            {
                volume = value;
            }
        }

        /// <summary>
        /// <see cref="WaveStream.BlockAlign"/>
        /// </summary>
        public override int BlockAlign => sourceStream.BlockAlign / 2;


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
                    sourceStream.Position = value * 2;
                    position = value;
                }
            }
        }

        /// <summary>
        /// Reads data from the source stream, converts it to 16-bit and writes it to the destination buffer.
        /// </summary>
        /// <param name="destBuffer">The destination buffer where the converted data will be written.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="destBuffer"/> at which to begin storing the data.</param>
        /// <param name="numBytes">The maximum number of bytes to read from the source stream.</param>
        /// <returns>The actual number of 16-bit words read into <paramref name="destBuffer"/>.</returns>
        /// <exception cref="System.IO.IOException">An I/O error occurs while reading from the source stream.</exception>
        public override int Read(byte[] destBuffer, int offset, int numBytes)
        {
            lock (lockObject)
            {
                int count = numBytes*2;
                sourceBuffer = BufferHelpers.Ensure(sourceBuffer, count);
                int bytesRead = sourceStream.Read(sourceBuffer, 0, count);
                Convert32To16(destBuffer, offset, sourceBuffer, bytesRead);
                position += (bytesRead/2);
                return bytesRead/2;
            }
        }

        /// <summary>
        /// Converts 32-bit audio samples to 16-bit audio samples and writes the result to the destination buffer.
        /// </summary>
        /// <param name="destBuffer">The destination buffer to write the 16-bit audio samples to.</param>
        /// <param name="offset">The offset in the destination buffer to start writing the samples.</param>
        /// <param name="source">The source buffer containing the 32-bit audio samples.</param>
        /// <param name="bytesRead">The number of bytes read from the source buffer.</param>
        /// <remarks>
        /// This method converts the 32-bit audio samples in the source buffer to 16-bit audio samples and writes the result to the destination buffer.
        /// It also applies volume scaling and handles clipping if the sample value exceeds the range of a 16-bit integer.
        /// The method uses unsafe code to work with pointers for better performance.
        /// </remarks>
        private unsafe void Convert32To16(byte[] destBuffer, int offset, byte[] source, int bytesRead)
        {
            fixed (byte* pDestBuffer = &destBuffer[offset],
                pSourceBuffer = &source[0])
            {
                short* psDestBuffer = (short*)pDestBuffer;
                float* pfSourceBuffer = (float*)pSourceBuffer;

                int samplesRead = bytesRead / 4;
                for (int n = 0; n < samplesRead; n++)
                {
                    float sampleVal = pfSourceBuffer[n] * volume;
                    if (sampleVal > 1.0f)
                    {
                        psDestBuffer[n] = short.MaxValue;
                        clip = true;
                    }
                    else if (sampleVal < -1.0f)
                    {
                        psDestBuffer[n] = short.MinValue;
                        clip = true;
                    }
                    else
                    {
                        psDestBuffer[n] = (short)(sampleVal * 32767);
                    }
                }
            }
        }

        /// <summary>
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Clip indicator. Can be reset.
        /// </summary>
        public bool Clip
        {
            get
            {
                return clip;
            }
            set
            {
                clip = value;
            }
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="ClassName"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        /// <remarks>
        /// This method releases the unmanaged resources used by the <see cref="ClassName"/> and optionally releases the managed resources.
        /// If <paramref name="disposing"/> is true, this method releases all resources held by any managed objects that this <see cref="ClassName"/> references.
        /// This method is called by the public <see cref="Dispose()"/> method and the <see cref="Finalize"/> method.
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
            base.Dispose(disposing);
        }
    }
}
