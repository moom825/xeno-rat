using System.IO;

namespace NAudio.SoundFont
{
    class ModulatorBuilder : StructureBuilder<Modulator>
    {

        /// <summary>
        /// Reads the modulator data from the binary reader and returns the modulator object.
        /// </summary>
        /// <param name="br">The binary reader used to read the modulator data.</param>
        /// <returns>The modulator object read from the binary reader.</returns>
        public override Modulator Read(BinaryReader br)
        {
            Modulator m = new Modulator();
            m.SourceModulationData = new ModulatorType(br.ReadUInt16());
            m.DestinationGenerator = (GeneratorEnum)br.ReadUInt16();
            m.Amount = br.ReadInt16();
            m.SourceModulationAmount = new ModulatorType(br.ReadUInt16());
            m.SourceTransform = (TransformEnum)br.ReadUInt16();
            data.Add(m);
            return m;
        }

        /// <summary>
        /// Writes the data of the Modulator object to a binary writer.
        /// </summary>
        /// <param name="bw">The binary writer to write the data to.</param>
        /// <param name="o">The Modulator object containing the data to be written.</param>
        /// <remarks>
        /// This method is intended to write the data of the Modulator object to a binary writer.
        /// However, the implementation is currently commented out, and it is unclear what specific data is being written.
        /// </remarks>
        public override void Write(BinaryWriter bw, Modulator o)
        {
            //Zone z = (Zone) o;
            //bw.Write(p.---);
        }

        public override int Length => 10;

        public Modulator[] Modulators => data.ToArray();
    }
}