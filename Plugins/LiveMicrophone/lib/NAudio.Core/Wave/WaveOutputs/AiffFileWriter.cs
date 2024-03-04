using System;
using System.IO;
using NAudio.Utils;

namespace NAudio.Wave
{
    /// <summary>
    /// This class writes audio data to a .aif file on disk
    /// </summary>
    public class AiffFileWriter : Stream
    {
        private Stream outStream;
        private BinaryWriter writer;
        private long dataSizePos;
        private long commSampleCountPos;
        private int dataChunkSize = 8;
        private WaveFormat format;
        private string filename;

        /// <summary>
        /// Creates an AIFF audio file from the provided WaveStream.
        /// </summary>
        /// <param name="filename">The name of the AIFF file to be created.</param>
        /// <param name="sourceProvider">The WaveStream providing the audio data.</param>
        /// <remarks>
        /// This method reads audio data from the <paramref name="sourceProvider"/> and writes it to an AIFF file specified by <paramref name="filename"/>.
        /// The method uses a buffer to read and write the audio data in chunks, and it ensures that the entire audio data is written to the AIFF file.
        /// </remarks>
        public static void CreateAiffFile(string filename, WaveStream sourceProvider)
        {
            using (var writer = new AiffFileWriter(filename, sourceProvider.WaveFormat))
            {
                byte[] buffer = new byte[16384];

                while (sourceProvider.Position < sourceProvider.Length)
                {
                    int count = Math.Min((int)(sourceProvider.Length - sourceProvider.Position), buffer.Length);
                    int bytesRead = sourceProvider.Read(buffer, 0, count);

                    if (bytesRead == 0)
                    {
                        // end of source provider
                        break;
                    }

                    writer.Write(buffer, 0, bytesRead);
                }
            }
        }

        /// <summary>
        /// AiffFileWriter that actually writes to a stream
        /// </summary>
        /// <param name="outStream">Stream to be written to</param>
        /// <param name="format">Wave format to use</param>
        public AiffFileWriter(Stream outStream, WaveFormat format)
        {
            this.outStream = outStream;
            this.format = format;
            this.writer = new BinaryWriter(outStream, System.Text.Encoding.UTF8);
            this.writer.Write(System.Text.Encoding.UTF8.GetBytes("FORM"));
            this.writer.Write((int)0); // placeholder
            this.writer.Write(System.Text.Encoding.UTF8.GetBytes("AIFF"));

            CreateCommChunk();
            WriteSsndChunkHeader();
        }

        /// <summary>
        /// Creates a new AiffFileWriter
        /// </summary>
        /// <param name="filename">The filename to write to</param>
        /// <param name="format">The Wave Format of the output data</param>
        public AiffFileWriter(string filename, WaveFormat format)
            : this(new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read), format)
        {
            this.filename = filename;
        }

        /// <summary>
        /// Writes the SSND chunk header to the output stream.
        /// </summary>
        /// <remarks>
        /// This method writes the "SSND" identifier to the output stream, followed by placeholder values for data size and zero offset.
        /// It then writes the block align value after swapping its endianness.
        /// </remarks>
        private void WriteSsndChunkHeader()
        {
            this.writer.Write(System.Text.Encoding.UTF8.GetBytes("SSND"));
            dataSizePos = this.outStream.Position;
            this.writer.Write((int)0);  // placeholder
            this.writer.Write((int)0);  // zero offset
            this.writer.Write(SwapEndian((int)format.BlockAlign));
        }

        /// <summary>
        /// Swaps the endianness of the input integer and returns the result as a byte array.
        /// </summary>
        /// <param name="n">The integer value whose endianness needs to be swapped.</param>
        /// <returns>A byte array representing the input integer with swapped endianness.</returns>
        /// <remarks>
        /// This method swaps the endianness of the input integer by rearranging its bytes in reverse order to convert between little-endian and big-endian representations.
        /// The resulting byte array contains the bytes of the input integer in the opposite order.
        /// </remarks>
        private byte[] SwapEndian(short n)
        {
            return new byte[] { (byte)(n >> 8), (byte)(n & 0xff) };
        }

        private byte[] SwapEndian(int n)
        {
            return new byte[] { (byte)((n >> 24) & 0xff), (byte)((n >> 16) & 0xff), (byte)((n >> 8) & 0xff), (byte)(n & 0xff), };
        }

        /// <summary>
        /// Creates a 'COMM' chunk in the WAV file.
        /// </summary>
        /// <remarks>
        /// This method writes the 'COMM' chunk to the WAV file. The 'COMM' chunk contains information about the audio format, such as the number of channels, bits per sample, and sample rate.
        /// It also includes a placeholder for the total number of samples, which is updated later when the actual audio data is written to the file.
        /// </remarks>
        private void CreateCommChunk()
        {
            this.writer.Write(System.Text.Encoding.UTF8.GetBytes("COMM"));
            this.writer.Write(SwapEndian((int)18));
            this.writer.Write(SwapEndian((short)format.Channels));
            commSampleCountPos = this.outStream.Position; ;
            this.writer.Write((int)0);  // placeholder for total number of samples
            this.writer.Write(SwapEndian((short)format.BitsPerSample));
            this.writer.Write(IEEE.ConvertToIeeeExtended(format.SampleRate));
        }

        /// <summary>
        /// The aiff file name or null if not applicable
        /// </summary>
        public string Filename
        {
            get { return filename; }
        }

        /// <summary>
        /// Number of bytes of audio in the data chunk
        /// </summary>
        public override long Length
        {
            get { return dataChunkSize; }
        }

        /// <summary>
        /// WaveFormat of this aiff file
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return format; }
        }

        /// <summary>
        /// Returns false: Cannot read from a AiffFileWriter
        /// </summary>
        public override bool CanRead
        {
            get { return false; }
        }

        /// <summary>
        /// Returns true: Can write to a AiffFileWriter
        /// </summary>
        public override bool CanWrite
        {
            get { return true; }
        }

        /// <summary>
        /// Returns false: Cannot seek within a AiffFileWriter
        /// </summary>
        public override bool CanSeek
        {
            get { return false; }
        }

        /// <summary>
        /// Throws an InvalidOperationException with a message indicating that reading from an AiffFileWriter is not allowed.
        /// </summary>
        /// <param name="buffer">The buffer to read data into.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <exception cref="InvalidOperationException">Thrown to indicate that reading from an AiffFileWriter is not allowed.</exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Cannot read from an AiffFileWriter");
        }

        /// <summary>
        /// Throws an InvalidOperationException with a message indicating that seeking within an AiffFileWriter is not allowed.
        /// </summary>
        /// <param name="offset">The new position within the stream.</param>
        /// <param name="origin">Specifies the beginning, the end, or the current position as a reference point for offset, using a value of type SeekOrigin.</param>
        /// <exception cref="InvalidOperationException">Thrown when seeking within an AiffFileWriter is attempted.</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException("Cannot seek within an AiffFileWriter");
        }

        /// <summary>
        /// Throws an InvalidOperationException with the message "Cannot set length of an AiffFileWriter".
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when attempting to set the length of an AiffFileWriter.</exception>
        public override void SetLength(long value)
        {
            throw new InvalidOperationException("Cannot set length of an AiffFileWriter");
        }

        /// <summary>
        /// Gets the Position in the AiffFile (i.e. number of bytes written so far)
        /// </summary>
        public override long Position
        {
            get { return dataChunkSize; }
            set { throw new InvalidOperationException("Repositioning an AiffFileWriter is not supported"); }
        }

        /// <summary>
        /// Writes the specified byte array to the output stream after swapping the bytes based on the format's BitsPerSample property.
        /// </summary>
        /// <param name="data">The byte array to be written to the output stream.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="data"/> at which to begin copying bytes to the output stream.</param>
        /// <param name="count">The number of bytes to be written to the output stream.</param>
        /// <remarks>
        /// This method swaps the bytes in the input byte array <paramref name="data"/> based on the format's BitsPerSample property.
        /// It then writes the swapped byte array to the output stream starting from the specified offset and writes the specified number of bytes.
        /// The dataChunkSize field is updated by adding the count of bytes written to the output stream.
        /// </remarks>
        public override void Write(byte[] data, int offset, int count)
        {
            byte[] swappedData = new byte[data.Length];

            int align = format.BitsPerSample / 8;

            for (int i = 0; i < data.Length; i++)
            {
                int pos = (int)Math.Floor((double)i / align) * align + (align - (i % align) - 1);
                swappedData[i] = data[pos];
            }

            outStream.Write(swappedData, offset, count);
            dataChunkSize += count;
        }

        private byte[] value24 = new byte[3]; // keep this around to save us creating it every time

        /// <summary>
        /// Writes a sample to the audio writer based on the wave format.
        /// </summary>
        /// <param name="sample">The sample to be written.</param>
        /// <exception cref="InvalidOperationException">Thrown when the wave format is not supported (only 16, 24, or 32 bit PCM or IEEE float audio data are supported).</exception>
        /// <remarks>
        /// This method writes the input sample to the audio writer based on the wave format. If the wave format is 16 bits per sample, it writes the sample as a 16-bit integer and updates the data chunk size by 2. If the wave format is 24 bits per sample, it writes the sample as a 24-bit integer and updates the data chunk size by 3. If the wave format is 32 bits per sample and the encoding is extensible, it writes the sample as a 32-bit unsigned integer and updates the data chunk size by 4.
        /// </remarks>
        public void WriteSample(float sample)
        {
            if (WaveFormat.BitsPerSample == 16)
            {
                writer.Write(SwapEndian((Int16)(Int16.MaxValue * sample)));
                dataChunkSize += 2;
            }
            else if (WaveFormat.BitsPerSample == 24)
            {
                var value = BitConverter.GetBytes((Int32)(Int32.MaxValue * sample));
                value24[2] = value[1];
                value24[1] = value[2];
                value24[0] = value[3];
                writer.Write(value24);
                dataChunkSize += 3;
            }
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == NAudio.Wave.WaveFormatEncoding.Extensible)
            {
                writer.Write(SwapEndian(UInt16.MaxValue * (Int32)sample));
                dataChunkSize += 4;
            }
            else
            {
                throw new InvalidOperationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
            }
        }

        /// <summary>
        /// Writes audio samples to the underlying stream based on the specified wave format.
        /// </summary>
        /// <param name="samples">The array of audio samples to be written.</param>
        /// <param name="offset">The offset in the samples array at which to begin writing.</param>
        /// <param name="count">The number of samples to write.</param>
        /// <exception cref="InvalidOperationException">Thrown when the wave format is not supported (only 16, 24, or 32 bit PCM audio data is supported).</exception>
        /// <remarks>
        /// This method writes the audio samples to the underlying stream based on the specified wave format.
        /// It handles 16, 24, and 32 bit PCM data by writing the samples in little-endian format and updating the data chunk size accordingly.
        /// </remarks>
        public void WriteSamples(float[] samples, int offset, int count)
        {
            for (int n = 0; n < count; n++)
            {
                WriteSample(samples[offset + n]);
            }
        }

        /// <summary>
        /// Writes 16 bit samples to the Aiff file
        /// </summary>
        /// <param name="samples">The buffer containing the 16 bit samples</param>
        /// <param name="offset">The offset from which to start writing</param>
        /// <param name="count">The number of 16 bit samples to write</param>
        public void WriteSamples(short[] samples, int offset, int count)
        {
            // 16 bit PCM data
            if (WaveFormat.BitsPerSample == 16)
            {
                for (int sample = 0; sample < count; sample++)
                {
                    writer.Write(SwapEndian(samples[sample + offset]));
                }
                dataChunkSize += (count * 2);
            }
            // 24 bit PCM data
            else if (WaveFormat.BitsPerSample == 24)
            {
                byte[] value;
                for (int sample = 0; sample < count; sample++)
                {
                    value = BitConverter.GetBytes(UInt16.MaxValue * (Int32)samples[sample + offset]);
                    value24[2] = value[1];
                    value24[1] = value[2];
                    value24[0] = value[3];
                    writer.Write(value24);
                }
                dataChunkSize += (count * 3);
            }
            // 32 bit PCM data
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.Extensible)
            {
                for (int sample = 0; sample < count; sample++)
                {
                    writer.Write(SwapEndian(UInt16.MaxValue * (Int32)samples[sample + offset]));
                }
                dataChunkSize += (count * 4);
            }
            else
            {
                throw new InvalidOperationException("Only 16, 24 or 32 bit PCM audio data supported");
            }
        }

        /// <summary>
        /// Flushes the buffer of the writer.
        /// </summary>
        /// <remarks>
        /// This method flushes the buffer of the writer, writing any buffered data to the underlying stream.
        /// </remarks>
        public override void Flush()
        {
            writer.Flush();
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="ClassName"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <remarks>
        /// This method disposes of the unmanaged resources used by the <see cref="ClassName"/>. If <paramref name="disposing"/> is true, it also disposes of the managed resources.
        /// The method first checks if <see cref="outStream"/> is not null, then attempts to update the header using the <see cref="UpdateHeader"/> method.
        /// If an <see cref="IOException"/> occurs during the update, the method ensures that the <see cref="outStream"/> is disposed in a finally block to prevent resource leaks.
        /// </remarks>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (outStream != null)
                {
                    try
                    {
                        UpdateHeader(writer);
                    }
                    finally
                    {
                        // in a finally block as we don't want the FileStream to run its disposer in
                        // the GC thread if the code above caused an IOException (e.g. due to disk full)
                        outStream.Dispose(); // will close the underlying base stream
                        outStream = null;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the header of the binary writer.
        /// </summary>
        /// <param name="writer">The binary writer to update the header for.</param>
        /// <remarks>
        /// This method flushes the current state, seeks to position 4 in the writer, and writes the updated length of the output stream in little-endian format.
        /// It then updates the communication chunk and sound chunk within the writer.
        /// </remarks>
        protected virtual void UpdateHeader(BinaryWriter writer)
        {
            this.Flush();
            writer.Seek(4, SeekOrigin.Begin);
            writer.Write(SwapEndian((int)(outStream.Length - 8)));
            UpdateCommChunk(writer);
            UpdateSsndChunk(writer);
        }

        /// <summary>
        /// Updates the communication chunk in the binary writer.
        /// </summary>
        /// <param name="writer">The binary writer to update.</param>
        /// <remarks>
        /// This method seeks to the position of the communication sample count in the writer and updates it with the calculated value.
        /// The calculated value is obtained by swapping the endianness of the result of the expression (dataChunkSize * 8 / format.BitsPerSample / format.Channels).
        /// </remarks>
        private void UpdateCommChunk(BinaryWriter writer)
        {
            writer.Seek((int)commSampleCountPos, SeekOrigin.Begin);
            writer.Write(SwapEndian((int)(dataChunkSize * 8 / format.BitsPerSample / format.Channels)));
        }

        /// <summary>
        /// Updates the SSND chunk in the WAV file by writing the data chunk size in little-endian format.
        /// </summary>
        /// <param name="writer">The BinaryWriter used to write data to the WAV file.</param>
        /// <remarks>
        /// This method seeks to the position of the data size in the WAV file and writes the data chunk size in little-endian format using the provided BinaryWriter.
        /// </remarks>
        private void UpdateSsndChunk(BinaryWriter writer)
        {
            writer.Seek((int)dataSizePos, SeekOrigin.Begin);
            writer.Write(SwapEndian((int)dataChunkSize));
        }

        /// <summary>
        /// Finaliser - should only be called if the user forgot to close this AiffFileWriter
        /// </summary>
        ~AiffFileWriter()
        {
            System.Diagnostics.Debug.Assert(false, "AiffFileWriter was not disposed");
            Dispose(false);
        }

        #endregion
    }
}
