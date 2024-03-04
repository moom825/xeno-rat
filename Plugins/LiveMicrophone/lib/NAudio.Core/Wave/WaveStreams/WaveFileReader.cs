using System;
using System.IO;
using System.Collections.Generic;
using NAudio.FileFormats.Wav;

namespace NAudio.Wave 
{
    /// <summary>This class supports the reading of WAV files,
    /// providing a repositionable WaveStream that returns the raw data
    /// contained in the WAV file
    /// </summary>
    public class WaveFileReader : WaveStream
    {
        private readonly WaveFormat waveFormat;
        private readonly bool ownInput;
        private readonly long dataPosition;
        private readonly long dataChunkLength;
        private readonly object lockObject = new object();
        private Stream waveStream;

        /// <summary>Supports opening a WAV file</summary>
        /// <remarks>The WAV file format is a real mess, but we will only
        /// support the basic WAV file format which actually covers the vast
        /// majority of WAV files out there. For more WAV file format information
        /// visit www.wotsit.org. If you have a WAV file that can't be read by
        /// this class, email it to the NAudio project and we will probably
        /// fix this reader to support it
        /// </remarks>
        public WaveFileReader(String waveFile) :
            this(File.OpenRead(waveFile), true)
        {            
        }

        /// <summary>
        /// Creates a Wave File Reader based on an input stream
        /// </summary>
        /// <param name="inputStream">The input stream containing a WAV file including header</param>
        public WaveFileReader(Stream inputStream) :
           this(inputStream, false)
        {
        }

        private WaveFileReader(Stream inputStream, bool ownInput)
        {
            this.waveStream = inputStream;
            var chunkReader = new WaveFileChunkReader();
            try
            {
                chunkReader.ReadWaveHeader(inputStream);
                waveFormat = chunkReader.WaveFormat;
                dataPosition = chunkReader.DataChunkPosition;
                dataChunkLength = chunkReader.DataChunkLength;
                ExtraChunks = chunkReader.RiffChunks;
            }
            catch
            {
                if (ownInput)
                {
                    inputStream.Dispose();
                }

                throw;
            }

            Position = 0;
            this.ownInput = ownInput;
        }

        /// <summary>
        /// Gets a list of the additional chunks found in this file
        /// </summary>
        public List<RiffChunk> ExtraChunks { get; }

        /// <summary>
        /// Retrieves the data of a specific RIFF chunk from the wave stream.
        /// </summary>
        /// <param name="chunk">The RIFF chunk for which the data needs to be retrieved.</param>
        /// <returns>The byte array containing the data of the specified <paramref name="chunk"/>.</returns>
        /// <remarks>
        /// This method temporarily changes the position of the wave stream to read the data of the specified <paramref name="chunk"/>.
        /// It then restores the original position of the wave stream after reading the data.
        /// </remarks>
        public byte[] GetChunkData(RiffChunk chunk)
        {
            long oldPosition = waveStream.Position;
            waveStream.Position = chunk.StreamPosition;
            byte[] data = new byte[chunk.Length];
            waveStream.Read(data, 0, data.Length);
            waveStream.Position = oldPosition;
            return data;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the WaveFileReader and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        /// <remarks>
        /// This method releases the unmanaged resources used by the WaveFileReader and optionally releases the managed resources.
        /// If <paramref name="disposing"/> is true, this method releases all resources held by any managed objects that this WaveFileReader references.
        /// This method also calls the Dispose method of the base class with the <paramref name="disposing"/> parameter set to true.
        /// If <paramref name="disposing"/> is false, this method indicates that the WaveFileReader was not disposed.
        /// </remarks>
        /// <exception cref="System.Diagnostics.Debug.AssertException">Thrown when WaveFileReader was not disposed.</exception>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Release managed resources.
                if (waveStream != null)
                {
                    // only dispose our source if we created it
                    if (ownInput)
                    {
                        waveStream.Dispose();
                    }
                    waveStream = null;
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "WaveFileReader was not disposed");
            }
            // Release unmanaged resources.
            // Set large fields to null.
            // Call Dispose on your base class.
            base.Dispose(disposing);
        }

        /// <summary>
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// This is the length of audio data contained in this WAV file, in bytes
        /// (i.e. the byte length of the data chunk, not the length of the WAV file itself)
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public override long Length => dataChunkLength;

        /// <summary>
        /// Number of Sample Frames  (if possible to calculate)
        /// This currently does not take into account number of channels
        /// Multiply number of channels if you want the total number of samples
        /// </summary>
        public long SampleCount
        {
            get
            {
                if (waveFormat.Encoding == WaveFormatEncoding.Pcm ||
                    waveFormat.Encoding == WaveFormatEncoding.Extensible ||
                    waveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                {
                    return dataChunkLength / BlockAlign;
                }
                // n.b. if there is a fact chunk, you can use that to get the number of samples
                throw new InvalidOperationException("Sample count is calculated only for the standard encodings");
            }
        }

        /// <summary>
        /// Position in the WAV data chunk.
        /// <see cref="Stream.Position"/>
        /// </summary>
        public override long Position
        {
            get
            {
                return waveStream.Position - dataPosition;
            }
            set
            {
                lock (lockObject)
                {
                    value = Math.Min(value, Length);
                    // make sure we don't get out of sync
                    value -= (value % waveFormat.BlockAlign);
                    waveStream.Position = value + dataPosition;
                }
            }
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="array">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in array at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <exception cref="ArgumentException">Thrown when count is not a multiple of waveFormat.BlockAlign.</exception>
        /// <returns>The total number of bytes read into the buffer. This might be less than the number of bytes requested if that many bytes are not currently available, or zero if the end of the stream is reached.</returns>
        /// <remarks>
        /// This method ensures that a complete block of bytes is read, as it throws an ArgumentException if count is not a multiple of waveFormat.BlockAlign.
        /// It then locks the current object and reads a sequence of bytes from the waveStream into the specified array, starting at the specified offset.
        /// If the position plus count exceeds the length of the data chunk, it adjusts the count to read only up to the end of the data chunk.
        /// </remarks>
        public override int Read(byte[] array, int offset, int count)
        {
            if (count % waveFormat.BlockAlign != 0)
            {
                throw new ArgumentException(
                    $"Must read complete blocks: requested {count}, block align is {WaveFormat.BlockAlign}");
            }
            lock (lockObject)
            {
                // sometimes there is more junk at the end of the file past the data chunk
                if (Position + count > dataChunkLength)
                {
                    count = (int) (dataChunkLength - Position);
                }
                return waveStream.Read(array, offset, count);
            }
        }

        /// <summary>
        /// Reads the next sample frame from the audio data and returns it as an array of floats.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the audio data encoding is not supported (only 16, 24, or 32 bit PCM or IEEE float audio data are supported).</exception>
        /// <exception cref="InvalidDataException">Thrown when an unexpected end of file is encountered while reading the audio data.</exception>
        /// <returns>An array of floats representing the next sample frame from the audio data.</returns>
        /// <remarks>
        /// This method reads the next sample frame from the audio data based on the specified wave format.
        /// It handles different bit depths and encodings to convert the raw byte data into floats.
        /// If the end of the file is reached, it returns null.
        /// </remarks>
        public float[] ReadNextSampleFrame()
        {
            switch (waveFormat.Encoding)
            {
                case WaveFormatEncoding.Pcm:
                case WaveFormatEncoding.IeeeFloat:
                case WaveFormatEncoding.Extensible: // n.b. not necessarily PCM, should probably write more code to handle this case
                    break;
                default:
                    throw new InvalidOperationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
            }
            var sampleFrame = new float[waveFormat.Channels];
            int bytesToRead = waveFormat.Channels*(waveFormat.BitsPerSample/8);
            byte[] raw = new byte[bytesToRead];
            int bytesRead = Read(raw, 0, bytesToRead);
            if (bytesRead == 0) return null; // end of file
            if (bytesRead < bytesToRead) throw new InvalidDataException("Unexpected end of file");
            int offset = 0;
            for (int channel = 0; channel < waveFormat.Channels; channel++)
            {
                if (waveFormat.BitsPerSample == 16)
                {
                    sampleFrame[channel] = BitConverter.ToInt16(raw, offset)/32768f;
                    offset += 2;
                }
                else if (waveFormat.BitsPerSample == 24)
                {
                    sampleFrame[channel] = (((sbyte)raw[offset + 2] << 16) | (raw[offset + 1] << 8) | raw[offset]) / 8388608f;
                    offset += 3;
                }
                else if (waveFormat.BitsPerSample == 32 && waveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                {
                    sampleFrame[channel] = BitConverter.ToSingle(raw, offset);
                    offset += 4;
                }
                else if (waveFormat.BitsPerSample == 32)
                {
                    sampleFrame[channel] = BitConverter.ToInt32(raw, offset) / (Int32.MaxValue + 1f);
                    offset += 4;
                }
                else
                {
                    throw new InvalidOperationException("Unsupported bit depth");
                }
            }
            return sampleFrame;
        }

        /// <summary>
        /// Tries to read the next sample frame and returns a float value if successful.
        /// </summary>
        /// <param name="sampleValue">When this method returns, contains the float value read from the sample frame, if the read operation succeeded, or 0 if it failed.</param>
        /// <returns>
        ///   <c>true</c> if a sample frame was successfully read and <paramref name="sampleValue"/> contains a valid float value; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method is marked as obsolete and it is recommended to use <see cref="ReadNextSampleFrame"/> instead, as this version does not support stereo properly.
        /// </remarks>
        [Obsolete("Use ReadNextSampleFrame instead (this version does not support stereo properly)")]
        public bool TryReadFloat(out float sampleValue)
        {
            var sf = ReadNextSampleFrame();
            sampleValue = sf != null ? sf[0] : 0;
            return sf != null;
        }
    }
}
