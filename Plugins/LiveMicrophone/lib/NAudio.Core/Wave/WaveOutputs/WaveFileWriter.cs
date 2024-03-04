using System;
using System.IO;
using NAudio.Wave.SampleProviders;
using NAudio.Utils;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// This class writes WAV data to a .wav file on disk
    /// </summary>
    public class WaveFileWriter : Stream
    {
        private Stream outStream;
        private readonly BinaryWriter writer;
        private long dataSizePos;
        private long factSampleCountPos;
        private long dataChunkSize;
        private readonly WaveFormat format;
        private readonly string filename;

        /// <summary>
        /// Creates a 16-bit wave file from the provided <paramref name="sourceProvider"/> and saves it with the specified <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">The name of the wave file to be created.</param>
        /// <param name="sourceProvider">The sample provider used to generate the wave file.</param>
        /// <remarks>
        /// This method creates a 16-bit wave file using the provided <paramref name="sourceProvider"/> by converting the samples to 16-bit PCM format.
        /// The resulting wave file is saved with the specified <paramref name="filename"/>.
        /// </remarks>
        public static void CreateWaveFile16(string filename, ISampleProvider sourceProvider)
        {
            CreateWaveFile(filename, new SampleToWaveProvider16(sourceProvider));
        }

        /// <summary>
        /// Creates a wave file with the specified filename using the provided wave provider.
        /// </summary>
        /// <param name="filename">The name of the wave file to be created.</param>
        /// <param name="sourceProvider">The wave provider used as the source for the wave file.</param>
        /// <exception cref="Exception">Thrown when the WAV file becomes too large.</exception>
        /// <remarks>
        /// This method creates a wave file with the specified filename using the provided wave provider.
        /// It reads data from the source provider in chunks and writes it to the wave file until the end of the source provider is reached.
        /// If the WAV file becomes too large, an exception is thrown.
        /// </remarks>
        public static void CreateWaveFile(string filename, IWaveProvider sourceProvider)
        {
            using (var writer = new WaveFileWriter(filename, sourceProvider.WaveFormat))
            {
                var buffer = new byte[sourceProvider.WaveFormat.AverageBytesPerSecond * 4];
                while (true)
                {
                    int bytesRead = sourceProvider.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        // end of source provider
                        break;
                    }
                    // Write will throw exception if WAV file becomes too large
                    writer.Write(buffer, 0, bytesRead);
                }
            }
        }

        /// <summary>
        /// Writes the audio data from the specified <paramref name="sourceProvider"/> to the <paramref name="outStream"/> in WAV format.
        /// </summary>
        /// <param name="outStream">The output stream to write the WAV data to.</param>
        /// <param name="sourceProvider">The audio source provider containing the audio data to be written.</param>
        /// <exception cref="ArgumentNullException">Thrown when either <paramref name="outStream"/> or <paramref name="sourceProvider"/> is null.</exception>
        /// <remarks>
        /// This method writes the audio data from the <paramref name="sourceProvider"/> to the <paramref name="outStream"/> in WAV format.
        /// It uses a buffer to read data from the <paramref name="sourceProvider"/> and write it to the <paramref name="outStream"/>.
        /// The process continues until no more data is available from the <paramref name="sourceProvider"/>.
        /// </remarks>
        public static void WriteWavFileToStream(Stream outStream, IWaveProvider sourceProvider)
        {
            using (var writer = new WaveFileWriter(new IgnoreDisposeStream(outStream), sourceProvider.WaveFormat)) 
            {
                var buffer = new byte[sourceProvider.WaveFormat.AverageBytesPerSecond * 4];
                while(true) 
                {
                    var bytesRead = sourceProvider.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) 
                    {
                        // end of source provider
                        outStream.Flush();
                        break;
                    }

                    writer.Write(buffer, 0, bytesRead);
                }
            }
        }
        
        /// <summary>
        /// WaveFileWriter that actually writes to a stream
        /// </summary>
        /// <param name="outStream">Stream to be written to</param>
        /// <param name="format">Wave format to use</param>
        public WaveFileWriter(Stream outStream, WaveFormat format)
        {
            this.outStream = outStream;
            this.format = format;
            writer = new BinaryWriter(outStream, System.Text.Encoding.UTF8);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            writer.Write((int)0); // placeholder
            writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));

            writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            format.Serialize(writer);

            CreateFactChunk();
            WriteDataChunkHeader();
        }

        /// <summary>
        /// Creates a new WaveFileWriter
        /// </summary>
        /// <param name="filename">The filename to write to</param>
        /// <param name="format">The Wave Format of the output data</param>
        public WaveFileWriter(string filename, WaveFormat format)
            : this(new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read), format)
        {
            this.filename = filename;
        }

        /// <summary>
        /// Writes the data chunk header to the output stream.
        /// </summary>
        /// <remarks>
        /// This method writes the "data" header to the output stream using UTF-8 encoding and then writes a placeholder for the data size.
        /// The position of the data size placeholder is stored in <see cref="dataSizePos"/>.
        /// </remarks>
        private void WriteDataChunkHeader()
        {
            writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            dataSizePos = outStream.Position;
            writer.Write((int)0); // placeholder
        }

        /// <summary>
        /// Creates a fact chunk in the output stream if it does not already exist.
        /// </summary>
        /// <remarks>
        /// This method checks if a fact chunk already exists in the output stream. If not, it writes the fact chunk header and initializes the sample count to 0.
        /// </remarks>
        private void CreateFactChunk()
        {
            if (HasFactChunk())
            {
                writer.Write(System.Text.Encoding.UTF8.GetBytes("fact"));
                writer.Write((int)4);
                factSampleCountPos = outStream.Position;
                writer.Write((int)0); // number of samples
            }
        }

        /// <summary>
        /// Checks if the wave format has a fact chunk.
        /// </summary>
        /// <returns>True if the wave format has a fact chunk; otherwise, false.</returns>
        private bool HasFactChunk()
        {
            return format.Encoding != WaveFormatEncoding.Pcm && 
                format.BitsPerSample != 0;
        }

        /// <summary>
        /// The wave file name or null if not applicable
        /// </summary>
        public string Filename => filename;

        /// <summary>
        /// Number of bytes of audio in the data chunk
        /// </summary>
        public override long Length => dataChunkSize;

        /// <summary>
        /// Total time (calculated from Length and average bytes per second)
        /// </summary>
        public TimeSpan TotalTime => TimeSpan.FromSeconds((double)Length / WaveFormat.AverageBytesPerSecond);

        /// <summary>
        /// WaveFormat of this wave file
        /// </summary>
        public WaveFormat WaveFormat => format;

        /// <summary>
        /// Returns false: Cannot read from a WaveFileWriter
        /// </summary>
        public override bool CanRead => false;

        /// <summary>
        /// Returns true: Can write to a WaveFileWriter
        /// </summary>
        public override bool CanWrite => true;

        /// <summary>
        /// Returns false: Cannot seek within a WaveFileWriter
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// Throws an InvalidOperationException with the message "Cannot read from a WaveFileWriter".
        /// </summary>
        /// <param name="buffer">The buffer to read the data into.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <exception cref="InvalidOperationException">Thrown when an attempt is made to read from a WaveFileWriter.</exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Cannot read from a WaveFileWriter");
        }

        /// <summary>
        /// Throws an InvalidOperationException with the message "Cannot seek within a WaveFileWriter".
        /// </summary>
        /// <param name="offset">The new position within the stream.</param>
        /// <param name="origin">Specifies the beginning, the end, or the current position as a reference point for offset, using a value of type SeekOrigin.</param>
        /// <exception cref="InvalidOperationException">Thrown when seeking within a WaveFileWriter is not allowed.</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException("Cannot seek within a WaveFileWriter");
        }

        /// <summary>
        /// Throws an InvalidOperationException with the message "Cannot set length of a WaveFileWriter".
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when an attempt is made to set the length of a WaveFileWriter.</exception>
        public override void SetLength(long value)
        {
            throw new InvalidOperationException("Cannot set length of a WaveFileWriter");
        }

        /// <summary>
        /// Gets the Position in the WaveFile (i.e. number of bytes written so far)
        /// </summary>
        public override long Position
        {
            get => dataChunkSize;
            set => throw new InvalidOperationException("Repositioning a WaveFileWriter is not supported");
        }

        /// <summary>
        /// Writes the specified number of samples from the given array, starting at the specified offset.
        /// </summary>
        /// <param name="samples">The array containing the samples to be written.</param>
        /// <param name="offset">The zero-based index in the array at which to start writing samples.</param>
        /// <param name="count">The number of samples to write.</param>
        /// <exception cref="ObsoleteException">This method is obsolete. Use WriteSamples instead.</exception>
        [Obsolete("Use Write instead")]
        public void WriteData(byte[] data, int offset, int count)
        {
            Write(data, offset, count);
        }

        /// <summary>
        /// Writes the specified bytes to the WAV file.
        /// </summary>
        /// <param name="data">The array of bytes to be written.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="data"/> at which to begin copying bytes to the WAV file.</param>
        /// <param name="count">The number of bytes to be written.</param>
        /// <exception cref="ArgumentException">Thrown when the WAV file size exceeds the maximum allowed value.</exception>
        /// <remarks>
        /// This method writes the specified bytes from the input array <paramref name="data"/> to the WAV file. It also updates the data chunk size accordingly.
        /// </remarks>
        public override void Write(byte[] data, int offset, int count)
        {
            if (outStream.Length + count > UInt32.MaxValue)
                throw new ArgumentException("WAV file too large", nameof(count));
            outStream.Write(data, offset, count);
            dataChunkSize += count;
        }

        private readonly byte[] value24 = new byte[3]; // keep this around to save us creating it every time

        /// <summary>
        /// Writes the specified audio sample to the wave file.
        /// </summary>
        /// <param name="sample">The audio sample to be written.</param>
        /// <exception cref="InvalidOperationException">Thrown when the audio data format is not supported (i.e., not 16, 24, or 32 bit PCM or IEEE float).</exception>
        /// <remarks>
        /// This method writes the specified audio sample to the wave file based on the wave format's bits per sample and encoding.
        /// If the wave format is 16-bit, it writes the sample as a 16-bit integer and updates the data chunk size by 2 bytes.
        /// If the wave format is 24-bit, it converts the sample to a 24-bit integer and writes it as 3 bytes, updating the data chunk size accordingly.
        /// If the wave format is 32-bit with extensible encoding, it writes the sample as a 32-bit integer and updates the data chunk size by 4 bytes.
        /// If the wave format is IEEE float, it directly writes the sample as a float and updates the data chunk size by 4 bytes.
        /// If the wave format does not match any of the supported formats, an InvalidOperationException is thrown.
        /// </remarks>
        public void WriteSample(float sample)
        {
            if (WaveFormat.BitsPerSample == 16)
            {
                writer.Write((Int16)(Int16.MaxValue * sample));
                dataChunkSize += 2;
            }
            else if (WaveFormat.BitsPerSample == 24)
            {
                var value = BitConverter.GetBytes((Int32)(Int32.MaxValue * sample));
                value24[0] = value[1];
                value24[1] = value[2];
                value24[2] = value[3];
                writer.Write(value24);
                dataChunkSize += 3;
            }
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.Extensible)
            {
                writer.Write(UInt16.MaxValue * (Int32)sample);
                dataChunkSize += 4;
            }
            else if (WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                writer.Write(sample);
                dataChunkSize += 4;
            }
            else
            {
                throw new InvalidOperationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
            }
        }

        /// <summary>
        /// Writes audio samples to the output stream based on the specified format and data.
        /// </summary>
        /// <param name="samples">The array of audio samples to be written.</param>
        /// <param name="offset">The offset in the samples array from which to start writing.</param>
        /// <param name="count">The number of samples to write.</param>
        /// <exception cref="InvalidOperationException">Thrown when the audio data format is not supported (only 16, 24, or 32 bit PCM or IEEE float audio data are supported).</exception>
        /// <remarks>
        /// This method writes the audio samples to the output stream based on the specified format and data. It handles different bit depths and encodings, updating the data chunk size accordingly for each case.
        /// </remarks>
        public void WriteSamples(float[] samples, int offset, int count)
        {
            for (int n = 0; n < count; n++)
            {
                WriteSample(samples[offset + n]);
            }
        }

        /// <summary>
        /// Writes 16 bit samples to the Wave file
        /// </summary>
        /// <param name="samples">The buffer containing the 16 bit samples</param>
        /// <param name="offset">The offset from which to start writing</param>
        /// <param name="count">The number of 16 bit samples to write</param>
        [Obsolete("Use WriteSamples instead")]
        public void WriteData(short[] samples, int offset, int count)
        {
            WriteSamples(samples, offset, count);
        }


        /// <summary>
        /// Writes 16 bit samples to the Wave file
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
                    writer.Write(samples[sample + offset]);
                }
                dataChunkSize += (count * 2);
            }
            // 24 bit PCM data
            else if (WaveFormat.BitsPerSample == 24)
            {
                for (int sample = 0; sample < count; sample++)
                {
                    var value = BitConverter.GetBytes(UInt16.MaxValue * (Int32)samples[sample + offset]);
                    value24[0] = value[1];
                    value24[1] = value[2];
                    value24[2] = value[3];
                    writer.Write(value24);
                }
                dataChunkSize += (count * 3);
            }
            // 32 bit PCM data
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.Extensible)
            {
                for (int sample = 0; sample < count; sample++)
                {
                    writer.Write(UInt16.MaxValue * (Int32)samples[sample + offset]);
                }
                dataChunkSize += (count * 4);
            }
            // IEEE float data
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                for (int sample = 0; sample < count; sample++)
                {
                    writer.Write((float)samples[sample + offset] / (float)(Int16.MaxValue + 1));
                }
                dataChunkSize += (count * 4);
            }
            else
            {
                throw new InvalidOperationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
            }
        }

        /// <summary>
        /// Updates the header and flushes the underlying stream.
        /// </summary>
        /// <remarks>
        /// This method updates the header information and flushes the underlying stream to persist the changes.
        /// </remarks>
        public override void Flush()
        {
            var pos = writer.BaseStream.Position;
            UpdateHeader(writer);
            writer.BaseStream.Position = pos;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="ClassName"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <remarks>
        /// This method releases the unmanaged resources used by the <see cref="ClassName"/> and optionally releases the managed resources.
        /// If <paramref name="disposing"/> is true, this method also releases all managed resources that this object holds.
        /// This method is called by the public <see cref="Dispose"/> method and the <see cref="Finalize"/> method.
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
        /// Updates the header of the audio file by flushing the writer and updating the RIFF, FACT, and data chunks.
        /// </summary>
        /// <param name="writer">The BinaryWriter used to write to the audio file.</param>
        /// <remarks>
        /// This method flushes the writer to ensure that all buffered data is written to the file.
        /// It then updates the RIFF chunk, FACT chunk, and data chunk in the audio file.
        /// </remarks>
        protected virtual void UpdateHeader(BinaryWriter writer)
        {
            writer.Flush();
            UpdateRiffChunk(writer);
            UpdateFactChunk(writer);
            UpdateDataChunk(writer);
        }

        /// <summary>
        /// Updates the data chunk size in the binary writer at the specified position.
        /// </summary>
        /// <param name="writer">The binary writer to be updated.</param>
        /// <remarks>
        /// This method seeks to the specified position in the binary writer and writes the updated data chunk size as a 32-bit unsigned integer.
        /// </remarks>
        private void UpdateDataChunk(BinaryWriter writer)
        {
            writer.Seek((int)dataSizePos, SeekOrigin.Begin);
            writer.Write((UInt32)dataChunkSize);
        }

        /// <summary>
        /// Updates the RIFF chunk in the output stream with the correct size.
        /// </summary>
        /// <param name="writer">The BinaryWriter used to write data to the output stream.</param>
        /// <remarks>
        /// This method updates the RIFF chunk in the output stream by seeking to the 4th position from the beginning and writing the correct size, which is calculated as the length of the output stream minus 8 bytes.
        /// </remarks>
        private void UpdateRiffChunk(BinaryWriter writer)
        {
            writer.Seek(4, SeekOrigin.Begin);
            writer.Write((UInt32)(outStream.Length - 8));
        }

        /// <summary>
        /// Updates the 'fact' chunk in the WAV file with the sample count information.
        /// </summary>
        /// <param name="writer">The BinaryWriter used to write data to the WAV file.</param>
        /// <remarks>
        /// This method checks if the 'fact' chunk exists in the WAV file. If it does, it calculates the sample count based on the data chunk size, bits per sample, and number of channels, and updates the 'fact' chunk with the calculated sample count information.
        /// </remarks>
        private void UpdateFactChunk(BinaryWriter writer)
        {
            if (HasFactChunk())
            {
                int bitsPerSample = (format.BitsPerSample * format.Channels);
                if (bitsPerSample != 0)
                {
                    writer.Seek((int)factSampleCountPos, SeekOrigin.Begin);
                    
                    writer.Write((int)((dataChunkSize * 8) / bitsPerSample));
                }
            }
        }

        /// <summary>
        /// Finaliser - should only be called if the user forgot to close this WaveFileWriter
        /// </summary>
        ~WaveFileWriter()
        {
            System.Diagnostics.Debug.Assert(false, "WaveFileWriter was not disposed");
            Dispose(false);
        }

        #endregion
    }
}
