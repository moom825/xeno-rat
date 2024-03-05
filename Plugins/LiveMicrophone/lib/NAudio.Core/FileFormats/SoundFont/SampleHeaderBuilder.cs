using System.IO;
using NAudio.Utils;

namespace NAudio.SoundFont
{
    class SampleHeaderBuilder : StructureBuilder<SampleHeader>
    {

        /// <summary>
        /// Reads and returns a SampleHeader from the provided BinaryReader.
        /// </summary>
        /// <param name="br">The BinaryReader used to read the SampleHeader.</param>
        /// <returns>The SampleHeader read from the BinaryReader.</returns>
        /// <remarks>
        /// This method reads data from the provided BinaryReader to populate a SampleHeader object with the following properties:
        /// - SampleName: A string representing the sample name.
        /// - Start: A 32-bit unsigned integer representing the start position.
        /// - End: A 32-bit unsigned integer representing the end position.
        /// - StartLoop: A 32-bit unsigned integer representing the start loop position.
        /// - EndLoop: A 32-bit unsigned integer representing the end loop position.
        /// - SampleRate: A 32-bit unsigned integer representing the sample rate.
        /// - OriginalPitch: An 8-bit unsigned integer representing the original pitch.
        /// - PitchCorrection: An 8-bit signed integer representing the pitch correction.
        /// - SampleLink: A 16-bit unsigned integer representing the sample link.
        /// - SFSampleLink: An enumeration representing the SoundFont sample link.
        /// The method then adds the populated SampleHeader to a collection and returns it.
        /// </remarks>
        public override SampleHeader Read(BinaryReader br)
        {
            SampleHeader sh = new SampleHeader();
            var s = br.ReadBytes(20);

            sh.SampleName = ByteEncoding.Instance.GetString(s, 0, s.Length);
            sh.Start = br.ReadUInt32();
            sh.End = br.ReadUInt32();
            sh.StartLoop = br.ReadUInt32();
            sh.EndLoop = br.ReadUInt32();
            sh.SampleRate = br.ReadUInt32();
            sh.OriginalPitch = br.ReadByte();
            sh.PitchCorrection = br.ReadSByte();
            sh.SampleLink = br.ReadUInt16();
            sh.SFSampleLink = (SFSampleLink)br.ReadUInt16();
            data.Add(sh);
            return sh;
        }

        /// <summary>
        /// Writes the sample header to the specified binary writer.
        /// </summary>
        /// <param name="bw">The binary writer to write to.</param>
        /// <param name="sampleHeader">The sample header to be written.</param>
        public override void Write(BinaryWriter bw, SampleHeader sampleHeader)
        {
        }

        public override int Length => 46;

        /// <summary>
        /// Removes the last element from the list.
        /// </summary>
        /// <remarks>
        /// This method removes the last element from the list <paramref name="data"/>.
        /// </remarks>
        internal void RemoveEOS()
        {
            data.RemoveAt(data.Count - 1);
        }

        public SampleHeader[] SampleHeaders => data.ToArray();
    }
}