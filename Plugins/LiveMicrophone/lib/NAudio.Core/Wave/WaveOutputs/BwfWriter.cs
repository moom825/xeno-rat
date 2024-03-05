using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NAudio.Wave
{
    /// <summary>
    /// Broadcast WAVE File Writer
    /// </summary>
    public class BwfWriter : IDisposable
    {
        private readonly WaveFormat format;
        private readonly BinaryWriter writer;
        private readonly long dataChunkSizePosition;
        private long dataLength;
        private bool isDisposed;

        /// <summary>
        /// Createa a new BwfWriter
        /// </summary>
        /// <param name="filename">Rarget filename</param>
        /// <param name="format">WaveFormat</param>
        /// <param name="bextChunkInfo">Chunk information</param>
        public BwfWriter(string filename, WaveFormat format, BextChunkInfo bextChunkInfo)
        {
            this.format = format;
            writer = new BinaryWriter(File.OpenWrite(filename));
            writer.Write(Encoding.UTF8.GetBytes("RIFF")); // will be updated to RF64 if large 
            writer.Write(0); // placeholder
            writer.Write(Encoding.UTF8.GetBytes("WAVE"));

            writer.Write(Encoding.UTF8.GetBytes("JUNK")); // ds64
            writer.Write(28); // ds64 size
            writer.Write(0L); // RIFF size
            writer.Write(0L); // data size
            writer.Write(0L); // sampleCount size
            writer.Write(0); // table length
            // TABLE appears here - to store the sizes of other huge chunks other than

            // write the broadcast audio extension
            writer.Write(Encoding.UTF8.GetBytes("bext"));
            var codingHistory = Encoding.ASCII.GetBytes(bextChunkInfo.CodingHistory ?? "");
            var bextLength = 602 + codingHistory.Length;
            if (bextLength % 2 != 0)
                bextLength++;
            writer.Write(bextLength); // bext size
            var bextStart = writer.BaseStream.Position;
            writer.Write(GetAsBytes(bextChunkInfo.Description, 256));
            writer.Write(GetAsBytes(bextChunkInfo.Originator, 32));
            writer.Write(GetAsBytes(bextChunkInfo.OriginatorReference, 32));
            writer.Write(GetAsBytes(bextChunkInfo.OriginationDate, 10));
            writer.Write(GetAsBytes(bextChunkInfo.OriginationTime, 8));
            writer.Write(bextChunkInfo.TimeReference); // 8 bytes long
            writer.Write(bextChunkInfo.Version); // 2 bytes long
            writer.Write(GetAsBytes(bextChunkInfo.UniqueMaterialIdentifier, 64));
            writer.Write(bextChunkInfo.Reserved); // for version 1 this is 190 bytes
            writer.Write(codingHistory);
            if (codingHistory.Length % 2 != 0)
                writer.Write((byte)0);
            Debug.Assert(writer.BaseStream.Position == bextStart + bextLength, "Invalid bext chunk size");

            // write the format chunk
            writer.Write(Encoding.UTF8.GetBytes("fmt "));
            format.Serialize(writer);

            writer.Write(Encoding.UTF8.GetBytes("data"));
            dataChunkSizePosition = writer.BaseStream.Position;
            writer.Write(-1); // will be overwritten unless this is RF64
            // now finally the data chunk
        }

        /// <summary>
        /// Writes a specified number of bytes from a byte array to the current stream at the specified position.
        /// </summary>
        /// <param name="buffer">The byte array containing the data to write.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        /// <exception cref="ObjectDisposedException">Thrown if the BWF Writer has already been disposed.</exception>
        /// <remarks>
        /// This method writes the specified number of bytes from the given byte array to the current stream at the specified position.
        /// It also updates the data length by adding the count of bytes written to the current stream.
        /// </remarks>
        public void Write(byte[] buffer, int offset, int count)
        {
            if (isDisposed) throw new ObjectDisposedException("This BWF Writer already disposed");
            writer.Write(buffer, offset, count);
            dataLength += count;
        }

        /// <summary>
        /// Flushes the underlying writer and ensures the WAV file is always playable after the flush operation.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the BWF Writer has already been disposed.</exception>
        public void Flush()
        {
            if (isDisposed) throw new ObjectDisposedException("This BWF Writer already disposed");
            writer.Flush();
            FixUpChunkSizes(true); // here to ensure WAV file created is always playable after Flush
        }

        /// <summary>
        /// Fixes up the chunk sizes in the WAV file.
        /// </summary>
        /// <param name="restorePosition">A boolean value indicating whether to restore the position of the writer after fixing up the chunk sizes.</param>
        /// <remarks>
        /// This method adjusts the chunk sizes in the WAV file based on the data length and format. If the data length exceeds Int32.MaxValue, it updates the RIFF and data chunk sizes for RF64 format. Otherwise, it updates the RIFF and data chunk sizes for standard WAV format. The method also handles restoring the position of the writer if specified.
        /// </remarks>
        private void FixUpChunkSizes(bool restorePosition)
        {
            var pos = writer.BaseStream.Position;
            var isLarge = dataLength > Int32.MaxValue;
            var riffSize = writer.BaseStream.Length - 8;
            if (isLarge)
            {
                var bytesPerSample = (format.BitsPerSample / 8) * format.Channels;
                writer.BaseStream.Position = 0;
                writer.Write(Encoding.UTF8.GetBytes("RF64"));
                writer.Write(-1);
                writer.BaseStream.Position += 4; // skip over WAVE
                writer.Write(Encoding.UTF8.GetBytes("ds64"));
                writer.BaseStream.Position += 4; // skip over ds64 chunk size
                writer.Write(riffSize);
                writer.Write(dataLength);
                writer.Write(dataLength / bytesPerSample);

                // data chunk size can stay as -1
            }
            else
            {
                // fix up the RIFF size
                writer.BaseStream.Position = 4;
                writer.Write((uint)riffSize);
                // fix up the data chunk size
                writer.BaseStream.Position = dataChunkSizePosition;
                writer.Write((uint)dataLength);
            }
            if (restorePosition)
            {
                writer.BaseStream.Position = pos;
            }

        }

        /// <summary>
        /// Disposes of the current object and releases any resources it is using.
        /// </summary>
        /// <remarks>
        /// This method checks if the object has already been disposed. If not, it calls the <see cref="FixUpChunkSizes"/> method with the parameter <c>false</c> to perform any necessary cleanup.
        /// It then disposes of the internal writer and sets the <c>isDisposed</c> flag to <c>true</c>.
        /// </remarks>
        public void Dispose()
        {
            if (!isDisposed)
            {
                FixUpChunkSizes(false);
                writer.Dispose();
                isDisposed = true;
            }
        }

        /// <summary>
        /// Converts the input string to a byte array of the specified size and returns the result.
        /// </summary>
        /// <param name="message">The input string to be converted to a byte array.</param>
        /// <param name="byteSize">The size of the byte array to be generated.</param>
        /// <returns>A byte array representing the input string, with a length of <paramref name="byteSize"/>. If the input string is shorter than <paramref name="byteSize"/>, the remaining bytes are filled with zeros.</returns>
        /// <remarks>
        /// This method creates a new byte array of size <paramref name="byteSize"/> and then encodes the input string using ASCII encoding. The encoded bytes are then copied to the output buffer, and if the length of the encoded bytes is less than <paramref name="byteSize"/>, the remaining bytes in the output buffer are filled with zeros.
        /// </remarks>
        private static byte[] GetAsBytes(string message, int byteSize)
        {
            var outputBuffer = new byte[byteSize];
            var encoded = Encoding.ASCII.GetBytes(message ?? "");
            Array.Copy(encoded, outputBuffer, Math.Min(encoded.Length, byteSize));
            return outputBuffer;
        }
    }
}