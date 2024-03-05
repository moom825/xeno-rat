using System.IO;

namespace NAudio.SoundFont
{
    /// <summary>
    /// Builds a SoundFont version
    /// </summary>
    class SFVersionBuilder : StructureBuilder<SFVersion>
    {

        /// <summary>
        /// Reads a version from the binary reader and adds it to the data collection.
        /// </summary>
        /// <param name="br">The binary reader from which to read the version.</param>
        /// <returns>The version read from the binary reader.</returns>
        /// <remarks>
        /// This method reads the major and minor version numbers from the binary reader and creates a new SFVersion object with these values.
        /// The SFVersion object is then added to the data collection.
        /// </remarks>
        public override SFVersion Read(BinaryReader br)
        {
            SFVersion v = new SFVersion();
            v.Major = br.ReadInt16();
            v.Minor = br.ReadInt16();
            data.Add(v);
            return v;
        }

        /// <summary>
        /// Writes the major and minor version numbers to the specified binary writer.
        /// </summary>
        /// <param name="bw">The binary writer to write the version numbers to.</param>
        /// <param name="v">The SFVersion object containing the version numbers to be written.</param>
        public override void Write(BinaryWriter bw, SFVersion v)
        {
            bw.Write(v.Major);
            bw.Write(v.Minor);
        }

        /// <summary>
        /// Gets the length of this structure
        /// </summary>
        public override int Length => 4;
    }
}