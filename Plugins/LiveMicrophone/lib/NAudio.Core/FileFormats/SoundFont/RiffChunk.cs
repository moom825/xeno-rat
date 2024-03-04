using System;
using System.IO;
using NAudio.Utils;

namespace NAudio.SoundFont
{
    internal class RiffChunk
    {
        private string chunkID;
        private BinaryReader riffFile;

        /// <summary>
        /// Reads and returns the top-level RIFF chunk from the provided binary file.
        /// </summary>
        /// <param name="file">The binary file from which to read the RIFF chunk.</param>
        /// <returns>The top-level RIFF chunk read from the binary file.</returns>
        /// <remarks>
        /// This method reads the top-level RIFF chunk from the provided binary file using the RiffChunk class.
        /// It then proceeds to read the chunk using the ReadChunk method of the RiffChunk class.
        /// The resulting RiffChunk object representing the top-level chunk is returned.
        /// </remarks>
        public static RiffChunk GetTopLevelChunk(BinaryReader file)
        {
            RiffChunk r = new RiffChunk(file);
            r.ReadChunk();
            return r;
        }

        private RiffChunk(BinaryReader file)
        {
            riffFile = file;
            chunkID = "????";
            ChunkSize = 0;
            DataOffset = 0;
        }

        /// <summary>
        /// Reads the Chunk ID from the RIFF file and returns it as a string.
        /// </summary>
        /// <returns>The Chunk ID read from the RIFF file.</returns>
        /// <exception cref="InvalidDataException">Thrown when the Chunk ID could not be read from the RIFF file.</exception>
        /// <remarks>
        /// This method reads 4 bytes from the RIFF file and converts them to a string using the ByteEncoding class.
        /// If the length of the read bytes is not 4, an InvalidDataException is thrown with the message "Couldn't read Chunk ID".
        /// </remarks>
        public string ReadChunkID()
        {
            byte[] cid = riffFile.ReadBytes(4);
            if (cid.Length != 4)
            {
                throw new InvalidDataException("Couldn't read Chunk ID");
            }
            return ByteEncoding.Instance.GetString(cid, 0, cid.Length);
        }

        /// <summary>
        /// Reads the chunk ID, chunk size, and data offset from the RIFF file.
        /// </summary>
        /// <remarks>
        /// This method reads the chunk ID from the RIFF file and assigns it to the <see cref="chunkID"/> property.
        /// It then reads the chunk size from the RIFF file and assigns it to the <see cref="ChunkSize"/> property.
        /// Finally, it assigns the current position in the RIFF file to the <see cref="DataOffset"/> property.
        /// </remarks>
        private void ReadChunk()
        {
            this.chunkID = ReadChunkID();
            this.ChunkSize = riffFile.ReadUInt32(); //(uint) IPAddress.NetworkToHostOrder(riffFile.ReadUInt32());
            this.DataOffset = riffFile.BaseStream.Position;
        }

        /// <summary>
        /// Reads and returns the next sub-chunk from the RIFF file.
        /// </summary>
        /// <returns>The next sub-chunk as a <see cref="RiffChunk"/> object, or null if the end of the chunk is reached.</returns>
        /// <remarks>
        /// This method checks if the current position in the RIFF file plus 8 is less than the sum of the data offset and chunk size.
        /// If the condition is met, it creates a new <see cref="RiffChunk"/> object, reads the chunk, and returns it.
        /// If the condition is not met, it returns null, indicating that the end of the chunk is reached.
        /// </remarks>
        public RiffChunk GetNextSubChunk()
        {
            if (riffFile.BaseStream.Position + 8 < DataOffset + ChunkSize)
            {
                RiffChunk chunk = new RiffChunk(riffFile);
                chunk.ReadChunk();
                return chunk;
            }
            //Console.WriteLine("DEBUG Failed to GetNextSubChunk because Position is {0}, dataOffset{1}, chunkSize {2}",riffFile.BaseStream.Position,dataOffset,chunkSize);
            return null;
        }

        /// <summary>
        /// Reads and returns the data from the specified position in the RIFF file.
        /// </summary>
        /// <returns>The byte array containing the data read from the RIFF file.</returns>
        /// <exception cref="InvalidDataException">Thrown when the length of the read data does not match the expected chunk size.</exception>
        public byte[] GetData()
        {
            riffFile.BaseStream.Position = DataOffset;
            byte[] data = riffFile.ReadBytes((int)ChunkSize);
            if (data.Length != ChunkSize)
            {
                throw new InvalidDataException(String.Format("Couldn't read chunk's data Chunk: {0}, read {1} bytes", this, data.Length));
            }
            return data;
        }

        /// <summary>
        /// Gets the data as a string.
        /// </summary>
        /// <returns>The data as a string, or null if the data is null.</returns>
        /// <remarks>
        /// This method retrieves the data as a byte array using the GetData method.
        /// If the data is not null, it is converted to a string using the ByteEncoding class and returned.
        /// If the data is null, null is returned.
        /// </remarks>
        public string GetDataAsString()
        {
            byte[] data = GetData();
            if (data == null)
                return null;
            return ByteEncoding.Instance.GetString(data, 0, data.Length);
        }

        /// <summary>
        /// Reads and returns the data as a structure of type T.
        /// </summary>
        /// <typeparam name="T">The type of structure to be returned.</typeparam>
        /// <param name="s">The StructureBuilder used to read the data.</param>
        /// <exception cref="InvalidDataException">Thrown when the length of the structure <paramref name="s"/> does not match the chunk size.</exception>
        /// <returns>The data read as a structure of type T.</returns>
        /// <remarks>
        /// This method sets the position of the base stream to the data offset and then reads the data using the specified <paramref name="s"/>.
        /// If the length of <paramref name="s"/> does not match the chunk size, an InvalidDataException is thrown with a message indicating the mismatch.
        /// </remarks>
        public T GetDataAsStructure<T>(StructureBuilder<T> s)
        {
            riffFile.BaseStream.Position = DataOffset;
            if (s.Length != ChunkSize)
            {
                throw new InvalidDataException(String.Format("Chunk size is: {0} so can't read structure of: {1}", ChunkSize, s.Length));
            }
            return s.Read(riffFile);
        }

        /// <summary>
        /// Reads data from the riff file and returns it as an array of structures of type T.
        /// </summary>
        /// <typeparam name="T">The type of structure to be read from the riff file.</typeparam>
        /// <param name="s">The structure builder used to read the data.</param>
        /// <exception cref="InvalidDataException">Thrown when the chunk size is not a multiple of the structure size.</exception>
        /// <returns>An array of structures of type T read from the riff file.</returns>
        /// <remarks>
        /// This method reads data from the riff file starting at the data offset and constructs an array of structures of type T.
        /// It first checks if the chunk size is a multiple of the structure size, and if not, it throws an InvalidDataException.
        /// It then calculates the number of structures to read based on the chunk size and structure size, initializes an array of type T with that size, and reads each structure using the provided structure builder.
        /// The method returns the array of structures read from the riff file.
        /// </remarks>
        public T[] GetDataAsStructureArray<T>(StructureBuilder<T> s)
        {
            riffFile.BaseStream.Position = DataOffset;
            if (ChunkSize % s.Length != 0)
            {
                throw new InvalidDataException(String.Format("Chunk size is: {0} not a multiple of structure size: {1}", ChunkSize, s.Length));
            }
            int structuresToRead = (int)(ChunkSize / s.Length);
            T[] a = new T[structuresToRead];
            for (int n = 0; n < structuresToRead; n++)
            {
                a[n] = s.Read(riffFile);
            }
            return a;
        }

        public string ChunkID
        {
            get
            {
                return chunkID;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("ChunkID may not be null");
                }
                if (value.Length != 4)
                {
                    throw new ArgumentException("ChunkID must be four characters");
                }
                chunkID = value;
            }
        }

        public uint ChunkSize { get; private set; }

        public long DataOffset { get; private set; }

        /// <summary>
        /// Returns a string representation of the RiffChunk, including its ID, size, and data offset.
        /// </summary>
        /// <returns>A formatted string containing the RiffChunk's ID, size, and data offset.</returns>
        public override string ToString()
        {
            return String.Format("RiffChunk ID: {0} Size: {1} Data Offset: {2}", ChunkID, ChunkSize, DataOffset);
        }

    }

}
