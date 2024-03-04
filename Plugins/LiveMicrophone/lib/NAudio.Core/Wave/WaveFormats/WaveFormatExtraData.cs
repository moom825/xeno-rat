using System;
using System.Runtime.InteropServices;
using System.IO;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// This class used for marshalling from unmanaged code
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 2)]
    public class WaveFormatExtraData : WaveFormat
    {
        // try with 100 bytes for now, increase if necessary
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        private byte[] extraData = new byte[100];

        /// <summary>
        /// Allows the extra data to be read
        /// </summary>
        public byte[] ExtraData => extraData;

        /// <summary>
        /// parameterless constructor for marshalling
        /// </summary>
        internal WaveFormatExtraData()
        {
        }

        /// <summary>
        /// Reads this structure from a BinaryReader
        /// </summary>
        public WaveFormatExtraData(BinaryReader reader)
            : base(reader)
        {
            ReadExtraData(reader);
        }

        /// <summary>
        /// Reads extra data from the provided BinaryReader if the extra size is greater than 0.
        /// </summary>
        /// <param name="reader">The BinaryReader from which to read the extra data.</param>
        /// <remarks>
        /// This method reads extra data from the provided BinaryReader if the extra size is greater than 0.
        /// The extra data is read into the internal extraData array.
        /// </remarks>
        internal void ReadExtraData(BinaryReader reader)
        {
            if (this.extraSize > 0)
            {
                reader.Read(extraData, 0, extraSize);
            }
        }

        /// <summary>
        /// Serializes the object and writes the data to a binary writer.
        /// </summary>
        /// <param name="writer">The binary writer to which the data is written.</param>
        /// <exception cref="InvalidOperationException">Thrown when the object is not in a valid state for serialization.</exception>
        /// <remarks>
        /// This method first calls the base class's serialization method to write the base data to the binary writer.
        /// If the extra size is greater than 0, it writes the extra data to the binary writer.
        /// </remarks>
        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            if (extraSize > 0)
            {
                writer.Write(extraData, 0, extraSize);
            }
        }
    }
}
