using System;
using System.IO;

namespace NAudio.SoundFont
{
    class ZoneBuilder : StructureBuilder<Zone>
    {
        private Zone lastZone = null;

        /// <summary>
        /// Reads a Zone from the provided BinaryReader and returns the read Zone.
        /// </summary>
        /// <param name="br">The BinaryReader to read the Zone from.</param>
        /// <returns>The Zone read from the BinaryReader.</returns>
        /// <remarks>
        /// This method reads a Zone from the provided BinaryReader. It reads the generatorIndex and modulatorIndex from the BinaryReader and populates the lastZone's generatorCount and modulatorCount if lastZone is not null. It then adds the read Zone to the data list and sets lastZone to the read Zone before returning it.
        /// </remarks>
        public override Zone Read(BinaryReader br)
        {
            Zone z = new Zone();
            z.generatorIndex = br.ReadUInt16();
            z.modulatorIndex = br.ReadUInt16();
            if (lastZone != null)
            {
                lastZone.generatorCount = (ushort)(z.generatorIndex - lastZone.generatorIndex);
                lastZone.modulatorCount = (ushort)(z.modulatorIndex - lastZone.modulatorIndex);
            }
            data.Add(z);
            lastZone = z;
            return z;
        }

        /// <summary>
        /// Writes the data to the specified binary writer for the given zone.
        /// </summary>
        /// <param name="bw">The binary writer to write the data to.</param>
        /// <param name="zone">The zone for which the data is being written.</param>
        /// <remarks>
        /// This method is responsible for writing the data to the specified binary writer for the given zone.
        /// The specific data to be written and the format of writing may vary based on the implementation of this method in derived classes.
        /// </remarks>
        public override void Write(BinaryWriter bw, Zone zone)
        {
            //bw.Write(p.---);
        }

        /// <summary>
        /// Loads the modulators and generators into the specified zones.
        /// </summary>
        /// <param name="modulators">An array of modulators to be loaded.</param>
        /// <param name="generators">An array of generators to be loaded.</param>
        /// <exception cref="IndexOutOfRangeException">Thrown when the zone index is out of range.</exception>
        /// <remarks>
        /// This method loads the specified modulators and generators into each zone, excluding the last zone, which is simply End of Zone (EOZ).
        /// It iterates through each zone, copies the corresponding generators and modulators into the zone, and then removes the End of Program (EOP) record.
        /// </remarks>
        public void Load(Modulator[] modulators, Generator[] generators)
        {
            // don't do the last zone, which is simply EOZ
            for (int zone = 0; zone < data.Count - 1; zone++)
            {
                Zone z = (Zone)data[zone];
                z.Generators = new Generator[z.generatorCount];
                Array.Copy(generators, z.generatorIndex, z.Generators, 0, z.generatorCount);
                z.Modulators = new Modulator[z.modulatorCount];
                Array.Copy(modulators, z.modulatorIndex, z.Modulators, 0, z.modulatorCount);
            }
            // we can get rid of the EOP record now
            data.RemoveAt(data.Count - 1);
        }

        public Zone[] Zones => data.ToArray();

        public override int Length => 4;
    }
}