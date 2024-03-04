using System.IO;

namespace NAudio.SoundFont
{
    internal class GeneratorBuilder : StructureBuilder<Generator>
    {

        /// <summary>
        /// Reads and returns a Generator object from the provided BinaryReader.
        /// </summary>
        /// <param name="br">The BinaryReader used to read the Generator object.</param>
        /// <returns>The Generator object read from the BinaryReader.</returns>
        public override Generator Read(BinaryReader br)
        {
            Generator g = new Generator();
            g.GeneratorType = (GeneratorEnum)br.ReadUInt16();
            g.UInt16Amount = br.ReadUInt16();
            data.Add(g);
            return g;
        }

        /// <summary>
        /// Writes the data to the specified binary writer.
        /// </summary>
        /// <param name="bw">The binary writer to write the data to.</param>
        /// <param name="o">The generator object containing the data to be written.</param>
        /// <remarks>
        /// This method is responsible for writing the data from the generator object <paramref name="o"/> to the specified binary writer <paramref name="bw"/>.
        /// </remarks>
        public override void Write(BinaryWriter bw, Generator o)
        {
            //Zone z = (Zone) o;
            //bw.Write(p.---);
        }

        public override int Length => 4;

        public Generator[] Generators => data.ToArray();

        /// <summary>
        /// Loads the sample headers into the generators.
        /// </summary>
        /// <param name="sampleHeaders">An array of sample headers to be loaded.</param>
        /// <remarks>
        /// This method iterates through each generator and assigns the corresponding sample header from the input array based on the generator's type.
        /// If the generator type is 'SampleID', it assigns the sample header at the index specified by the generator's 'UInt16Amount' property.
        /// </remarks>
        public void Load(Instrument[] instruments)
        {
            foreach (Generator g in Generators)
            {
                if (g.GeneratorType == GeneratorEnum.Instrument)
                {
                    g.Instrument = instruments[g.UInt16Amount];
                }
            }
        }

        public void Load(SampleHeader[] sampleHeaders)
        {
            foreach (Generator g in Generators)
            {
                if (g.GeneratorType == GeneratorEnum.SampleID)
                {
                    g.SampleHeader = sampleHeaders[g.UInt16Amount];
                }
            }
        }
    }
}