using System;
using System.Collections.Generic;

namespace NAudio.Wave
{
    /// <summary>
    /// WaveStream that can mix together multiple 32 bit input streams
    /// (Normally used with stereo input channels)
    /// All channels must have the same number of inputs
    /// </summary>
    public class WaveMixerStream32 : WaveStream
    {
        private readonly List<WaveStream> inputStreams;
        private readonly object inputsLock;
        private WaveFormat waveFormat;
        private long length;
        private long position;
        private readonly int bytesPerSample;

        /// <summary>
        /// Creates a new 32 bit WaveMixerStream
        /// </summary>
        public WaveMixerStream32()
        {
            AutoStop = true;
            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            bytesPerSample = 4;
            inputStreams = new List<WaveStream>();
            inputsLock = new object();
        }

        /// <summary>
        /// Creates a new 32 bit WaveMixerStream
        /// </summary>
        /// <param name="inputStreams">An Array of WaveStreams - must all have the same format.
        /// Use WaveChannel is designed for this purpose.</param>
        /// <param name="autoStop">Automatically stop when all inputs have been read</param>
        /// <exception cref="ArgumentException">Thrown if the input streams are not 32 bit floating point,
        /// or if they have different formats to each other</exception>
        public WaveMixerStream32(IEnumerable<WaveStream> inputStreams, bool autoStop)
            : this()
        {
            AutoStop = autoStop;

            foreach (var inputStream in inputStreams)
            {
                AddInputStream(inputStream);
            }
        }

        /// <summary>
        /// Adds an input audio stream to the mixer.
        /// </summary>
        /// <param name="waveStream">The WaveStream to be added to the mixer.</param>
        /// <exception cref="ArgumentException">Thrown when the input WaveStream does not meet the required format specifications.</exception>
        /// <remarks>
        /// This method adds the input audio stream <paramref name="waveStream"/> to the mixer.
        /// It performs format checks to ensure that the input stream meets the required specifications, such as being in IEEE floating point format and having 32-bit audio.
        /// If this is the first input stream being added, it sets the format for the mixer based on the input stream's properties.
        /// Subsequent input streams are checked to ensure that they match the format of the existing streams.
        /// The input stream is then added to the mixer, and its length is compared with the current length of the mixer, updating it if necessary.
        /// Finally, the position of the input stream is set to match the current position of the mixer.
        /// </remarks>
        public void AddInputStream(WaveStream waveStream)
        {
            if (waveStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                throw new ArgumentException("Must be IEEE floating point", "waveStream");
            if (waveStream.WaveFormat.BitsPerSample != 32)
                throw new ArgumentException("Only 32 bit audio currently supported", "waveStream");

            if (inputStreams.Count == 0)
            {
                // first one - set the format
                int sampleRate = waveStream.WaveFormat.SampleRate;
                int channels = waveStream.WaveFormat.Channels;
                waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            }
            else
            {
                if (!waveStream.WaveFormat.Equals(waveFormat))
                    throw new ArgumentException("All incoming channels must have the same format", "waveStream");
            }

            lock (inputsLock)
            {
                inputStreams.Add(waveStream);
                length = Math.Max(length, waveStream.Length);
                // get to the right point in this input file
                waveStream.Position = Position;
            }
        }

        /// <summary>
        /// Removes the specified WaveStream from the list of input streams and recalculates the total length of all input streams.
        /// </summary>
        /// <param name="waveStream">The WaveStream to be removed from the list of input streams.</param>
        /// <remarks>
        /// This method removes the specified <paramref name="waveStream"/> from the list of input streams and recalculates the total length of all input streams.
        /// If the specified <paramref name="waveStream"/> is successfully removed, the total length of all input streams is recalculated by finding the maximum length among all remaining input streams.
        /// The total length is then updated to the new calculated length.
        /// </remarks>
        public void RemoveInputStream(WaveStream waveStream)
        {
            lock (inputsLock)
            {
                if (inputStreams.Remove(waveStream))
                {
                    // recalculate the length
                    long newLength = 0;
                    foreach (var inputStream in inputStreams)
                    {
                        newLength = Math.Max(newLength, inputStream.Length);
                    }
                    length = newLength;
                }
            }
        }

        /// <summary>
        /// The number of inputs to this mixer
        /// </summary>
        public int InputCount => inputStreams.Count;

        /// <summary>
        /// Automatically stop when all inputs have been read
        /// </summary>
        public bool AutoStop { get; set; }

        /// <summary>
        /// Reads a specified number of bytes from the current stream into a byte array and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero if the end of the stream is reached before any data is read.</returns>
        /// <exception cref="ArgumentException">Thrown when count is not a whole number of samples.</exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (AutoStop)
            {
                if (position + count > length)
                    count = (int)(length - position);

                // was a bug here, should be fixed now
                System.Diagnostics.Debug.Assert(count >= 0, "length and position mismatch");
            }


            if (count % bytesPerSample != 0)
                throw new ArgumentException("Must read an whole number of samples", "count");

            // blank the buffer
            Array.Clear(buffer, offset, count);
            int bytesRead = 0;

            // sum the channels in
            var readBuffer = new byte[count];
            lock (inputsLock)
            {
                foreach (var inputStream in inputStreams)
                {
                    if (inputStream.HasData(count))
                    {
                        int readFromThisStream = inputStream.Read(readBuffer, 0, count);
                        // don't worry if input stream returns less than we requested - may indicate we have got to the end
                        bytesRead = Math.Max(bytesRead, readFromThisStream);
                        if (readFromThisStream > 0)
                            Sum32BitAudio(buffer, offset, readBuffer, readFromThisStream);
                    }
                    else
                    {
                        bytesRead = Math.Max(bytesRead, count);
                        inputStream.Position += count;
                    }
                }
            }
            position += count;
            return count;
        }

        /// <summary>
        /// Sums 32-bit audio samples from the source buffer to the destination buffer.
        /// </summary>
        /// <param name="destBuffer">The destination buffer to which the audio samples will be added.</param>
        /// <param name="offset">The offset within the destination buffer where the addition will start.</param>
        /// <param name="sourceBuffer">The source buffer containing the audio samples to be added.</param>
        /// <param name="bytesRead">The number of bytes read from the source buffer.</param>
        /// <remarks>
        /// This method sums 32-bit audio samples from the source buffer to the destination buffer. It uses unsafe code to work with pointers for performance reasons.
        /// The method calculates the number of samples to be read based on the number of bytes read and performs the addition for each sample.
        /// </remarks>
        static unsafe void Sum32BitAudio(byte[] destBuffer, int offset, byte[] sourceBuffer, int bytesRead)
        {
            fixed (byte* pDestBuffer = &destBuffer[offset],
                      pSourceBuffer = &sourceBuffer[0])
            {
                float* pfDestBuffer = (float*)pDestBuffer;
                float* pfReadBuffer = (float*)pSourceBuffer;
                int samplesRead = bytesRead / 4;
                for (int n = 0; n < samplesRead; n++)
                {
                    pfDestBuffer[n] += pfReadBuffer[n];
                }
            }
        }

        /// <summary>
        /// <see cref="WaveStream.BlockAlign"/>
        /// </summary>
        public override int BlockAlign => waveFormat.BlockAlign;

        /// <summary>
        /// Length of this Wave Stream (in bytes)
        /// <see cref="System.IO.Stream.Length"/>
        /// </summary>
        public override long Length => length;

        /// <summary>
        /// Position within this Wave Stream (in bytes)
        /// <see cref="System.IO.Stream.Position"/>
        /// </summary>
        public override long Position
        {
            get
            {
                // all streams are at the same position
                return position;
            }
            set
            {
                lock (inputsLock)
                {
                    value = Math.Min(value, Length);
                    foreach (WaveStream inputStream in inputStreams)
                    {
                        inputStream.Position = Math.Min(value, inputStream.Length);
                    }
                    position = value;
                }
            }
        }

        /// <summary>
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Releases the unmanaged resources used by the WaveMixerStream32 and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        /// <remarks>
        /// This method disposes the input streams if <paramref name="disposing"/> is true, and asserts if <paramref name="disposing"/> is false.
        /// </remarks>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (inputsLock)
                {
                    foreach (WaveStream inputStream in inputStreams)
                    {
                        inputStream.Dispose();
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "WaveMixerStream32 was not disposed");
            }
            base.Dispose(disposing);
        }
    }
}
