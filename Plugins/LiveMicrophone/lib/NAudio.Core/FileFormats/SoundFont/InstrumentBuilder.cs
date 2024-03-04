using System;
using System.IO;
using System.Text;

namespace NAudio.SoundFont
{
    /// <summary>
    /// Instrument Builder
    /// </summary>
    internal class InstrumentBuilder : StructureBuilder<Instrument>
    {
        private Instrument lastInstrument = null;

        /// <summary>
        /// Reads an instrument from the binary reader and returns the instrument.
        /// </summary>
        /// <param name="br">The binary reader to read from.</param>
        /// <returns>The instrument read from the binary reader.</returns>
        /// <remarks>
        /// This method reads an instrument from the binary reader by reading the name and startInstrumentZoneIndex, and then updates the lastInstrument and adds the read instrument to the data collection.
        /// </remarks>
        public override Instrument Read(BinaryReader br)
        {
            Instrument i = new Instrument();
            string s = Encoding.UTF8.GetString(br.ReadBytes(20), 0, 20);
            if (s.IndexOf('\0') >= 0)
            {
                s = s.Substring(0, s.IndexOf('\0'));
            }
            i.Name = s;
            i.startInstrumentZoneIndex = br.ReadUInt16();
            if (lastInstrument != null)
            {
                lastInstrument.endInstrumentZoneIndex = (ushort)(i.startInstrumentZoneIndex - 1);
            }
            data.Add(i);
            lastInstrument = i;
            return i;
        }

        /// <summary>
        /// Writes the instrument data to a binary writer.
        /// </summary>
        /// <param name="bw">The binary writer to write the data to.</param>
        /// <param name="instrument">The instrument data to be written.</param>
        /// <remarks>
        /// This method writes the instrument data to the specified binary writer.
        /// </remarks>
        public override void Write(BinaryWriter bw, Instrument instrument)
        {
        }

        public override int Length => 22;

        /// <summary>
        /// Loads the provided zones into the instruments, excluding the last preset (EOP).
        /// </summary>
        /// <param name="zones">An array of Zone objects to be loaded into the instruments.</param>
        /// <remarks>
        /// This method iterates through the instruments and assigns the corresponding zones based on the start and end indices.
        /// It then removes the last preset (EOP) from the data.
        /// </remarks>
        public void LoadZones(Zone[] zones)
        {
            // don't do the last preset, which is simply EOP
            for (int instrument = 0; instrument < data.Count - 1; instrument++)
            {
                Instrument i = data[instrument];
                i.Zones = new Zone[i.endInstrumentZoneIndex - i.startInstrumentZoneIndex + 1];
                Array.Copy(zones, i.startInstrumentZoneIndex, i.Zones, 0, i.Zones.Length);
            }
            // we can get rid of the EOP record now
            data.RemoveAt(data.Count - 1);
        }

        public Instrument[] Instruments => data.ToArray();
    }
}