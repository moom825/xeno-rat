using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NAudio.Utils;
using NAudio.Wave;
using System.Diagnostics;

namespace NAudio.FileFormats.Wav
{
    public class WaveFileChunkReader
    {
        private WaveFormat waveFormat;
        private long dataChunkPosition;
        private long dataChunkLength;
        private List<RiffChunk> riffChunks;
        private readonly bool strictMode;
        private bool isRf64;
        private readonly bool storeAllChunks;
        private long riffSize;

        public WaveFileChunkReader()
        {
            storeAllChunks = true;
            strictMode = false;
        }

        /// <summary>
        /// Reads the wave header from the provided stream and initializes the necessary fields.
        /// </summary>
        /// <param name="stream">The input stream containing the wave file data.</param>
        /// <exception cref="FormatException">Thrown when the input stream does not contain a WAVE header.</exception>
        /// <exception cref="InvalidDataException">Thrown when the format chunk length or riff chunk length is invalid.</exception>
        /// <remarks>
        /// This method reads the wave header from the provided stream and initializes the necessary fields such as dataChunkPosition, waveFormat, riffChunks, and dataChunkLength.
        /// It first reads the RIFF header, then the file size, and checks for the WAVE header. If the file is in RF64 format, it reads the DS64 chunk.
        /// It then iterates through the chunks in the stream, processing the data and format chunks accordingly.
        /// The method also handles word alignment for all chunks and throws exceptions if the format or data chunk is not found in the input stream.
        /// </remarks>
        public void ReadWaveHeader(Stream stream)
        {
            this.dataChunkPosition = -1;
            this.waveFormat = null;
            this.riffChunks = new List<RiffChunk>();
            this.dataChunkLength = 0;

            var br = new BinaryReader(stream);
            ReadRiffHeader(br);
            this.riffSize = br.ReadUInt32(); // read the file size (minus 8 bytes)

            if (br.ReadInt32() != ChunkIdentifier.ChunkIdentifierToInt32("WAVE"))
            {
                throw new FormatException("Not a WAVE file - no WAVE header");
            }

            if (isRf64)
            {
                ReadDs64Chunk(br);
            }

            int dataChunkId = ChunkIdentifier.ChunkIdentifierToInt32("data");
            int formatChunkId = ChunkIdentifier.ChunkIdentifierToInt32("fmt ");
            
            // sometimes a file has more data than is specified after the RIFF header
            long stopPosition = Math.Min(riffSize + 8, stream.Length);

            // this -8 is so we can be sure that there are at least 8 bytes for a chunk id and length
            while (stream.Position <= stopPosition - 8)
            {
                Int32 chunkIdentifier = br.ReadInt32();
                var chunkLength = br.ReadUInt32();
                if (chunkIdentifier == dataChunkId)
                {
                    dataChunkPosition = stream.Position;
                    if (!isRf64) // we already know the dataChunkLength if this is an RF64 file
                    {
                        dataChunkLength = chunkLength;
                    }
                    stream.Position += chunkLength;
                }
                else if (chunkIdentifier == formatChunkId)
                {
                    if (chunkLength > Int32.MaxValue)
                         throw new InvalidDataException(string.Format("Format chunk length must be between 0 and {0}.", Int32.MaxValue));
                    waveFormat = WaveFormat.FromFormatChunk(br, (int)chunkLength);
                }
                else
                {
                    // check for invalid chunk length
                    if (chunkLength > stream.Length - stream.Position)
                    {
                        if (strictMode)
                        {
                            Debug.Assert(false, String.Format("Invalid chunk length {0}, pos: {1}. length: {2}",
                                chunkLength, stream.Position, stream.Length));
                        }
                        // an exception will be thrown further down if we haven't got a format and data chunk yet,
                        // otherwise we will tolerate this file despite it having corrupt data at the end
                        break;
                    }
                    if (storeAllChunks)
                    {
                        if (chunkLength > Int32.MaxValue)
                            throw new InvalidDataException(string.Format("RiffChunk chunk length must be between 0 and {0}.", Int32.MaxValue));
                        riffChunks.Add(GetRiffChunk(stream, chunkIdentifier, (int)chunkLength));
                    }
                    stream.Position += chunkLength;
                }

                // All Chunks have to be word aligned.
                // https://www.tactilemedia.com/info/MCI_Control_Info.html
                // "If the chunk size is an odd number of bytes, a pad byte with value zero is
                //  written after ckData. Word aligning improves access speed (for chunks resident in memory)
                //  and maintains compatibility with EA IFF. The ckSize value does not include the pad byte."
                if (((chunkLength % 2) != 0) && (br.PeekChar() == 0))
                {
                    stream.Position++;
                }
            }

            if (waveFormat == null)
            {
                throw new FormatException("Invalid WAV file - No fmt chunk found");
            }
            if (dataChunkPosition == -1)
            {
                throw new FormatException("Invalid WAV file - No data chunk found");
            }
        }

        /// <summary>
        /// Reads the ds64 chunk from the binary reader and updates the relevant properties.
        /// </summary>
        /// <param name="reader">The BinaryReader to read from.</param>
        /// <exception cref="FormatException">Thrown when the ds64 chunk is not found, indicating an invalid RF64 WAV file.</exception>
        /// <remarks>
        /// This method reads the ds64 chunk from the provided BinaryReader and updates the <see cref="riffSize"/>, <see cref="dataChunkLength"/>, and <see cref="sampleCount"/> properties accordingly.
        /// It also advances the reader to the end of the ds64 chunk.
        /// </remarks>
        private void ReadDs64Chunk(BinaryReader reader)
        {
            int ds64ChunkId = ChunkIdentifier.ChunkIdentifierToInt32("ds64");
            int chunkId = reader.ReadInt32();
            if (chunkId != ds64ChunkId)
            {
                throw new FormatException("Invalid RF64 WAV file - No ds64 chunk found");
            }
            int chunkSize = reader.ReadInt32();
            this.riffSize = reader.ReadInt64();
            this.dataChunkLength = reader.ReadInt64();
            long sampleCount = reader.ReadInt64(); // replaces the value in the fact chunk
            reader.ReadBytes(chunkSize - 24); // get to the end of this chunk (should parse extra stuff later)
        }

        /// <summary>
        /// Creates a new RIFF chunk with the specified identifier, length, and position in the stream.
        /// </summary>
        /// <param name="stream">The input stream from which the RIFF chunk is being read.</param>
        /// <param name="chunkIdentifier">The identifier of the RIFF chunk.</param>
        /// <param name="chunkLength">The length of the RIFF chunk.</param>
        /// <returns>A new <see cref="RiffChunk"/> object with the specified identifier, length, and position in the stream.</returns>
        private static RiffChunk GetRiffChunk(Stream stream, Int32 chunkIdentifier, Int32 chunkLength)
        {
            return new RiffChunk(chunkIdentifier, chunkLength, stream.Position);
        }

        /// <summary>
        /// Reads the RIFF header from the provided BinaryReader and sets the isRf64 flag if the header is "RF64".
        /// Throws a FormatException if the header is not "RIFF".
        /// </summary>
        /// <param name="br">The BinaryReader used to read the RIFF header.</param>
        /// <exception cref="FormatException">Thrown when the header is not "RIFF".</exception>
        private void ReadRiffHeader(BinaryReader br)
        {
            int header = br.ReadInt32();
            if (header == ChunkIdentifier.ChunkIdentifierToInt32("RF64"))
            {
                this.isRf64 = true;
            }
            else if (header != ChunkIdentifier.ChunkIdentifierToInt32("RIFF"))
            {
                throw new FormatException("Not a WAVE file - no RIFF header");
            }
        }

        /// <summary>
        /// WaveFormat
        /// </summary>
        public WaveFormat WaveFormat { get { return this.waveFormat; } }

        /// <summary>
        /// Data Chunk Position
        /// </summary>
        public long DataChunkPosition { get { return this.dataChunkPosition; } }

        /// <summary>
        /// Data Chunk Length
        /// </summary>
        public long DataChunkLength { get { return this.dataChunkLength; } }

        /// <summary>
        /// Riff Chunks
        /// </summary>
        public List<RiffChunk> RiffChunks { get { return this.riffChunks; } }
    }
}
