using System;
using System.Collections.Generic;
using System.IO;
namespace NAudio.SoundFont
{

    /// <summary>
    /// base class for structures that can read themselves
    /// </summary>
    internal abstract class StructureBuilder<T>
    {
        protected List<T> data;

        public StructureBuilder()
        {
            Reset();
        }

        /// <summary>
        /// Reads data from the specified BinaryReader and returns the result.
        /// </summary>
        /// <param name="br">The BinaryReader from which to read data.</param>
        /// <returns>The data read from the BinaryReader.</returns>
        public abstract T Read(BinaryReader br);

        /// <summary>
        /// Writes the specified object of type T to the binary writer.
        /// </summary>
        /// <param name="bw">The binary writer to write to.</param>
        /// <param name="o">The object of type T to be written.</param>
        public abstract void Write(BinaryWriter bw, T o);
        public abstract int Length { get; }

        /// <summary>
        /// Resets the data by creating a new empty list of type T.
        /// </summary>
        public void Reset()
        {
            data = new List<T>();
        }

        public T[] Data => data.ToArray();
    }

}