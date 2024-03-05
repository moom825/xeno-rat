using System;
using System.IO;
using System.Text;

namespace NAudio.SoundFont
{
    class PresetBuilder : StructureBuilder<Preset>
    {
        private Preset lastPreset = null;

        /// <summary>
        /// Reads and returns a Preset object from the provided BinaryReader.
        /// </summary>
        /// <param name="br">The BinaryReader used to read the Preset object.</param>
        /// <returns>The Preset object read from the BinaryReader.</returns>
        /// <remarks>
        /// This method reads a Preset object from the provided BinaryReader by reading various properties such as Name, PatchNumber, Bank, startPresetZoneIndex, library, genre, and morphology.
        /// If the lastPreset is not null, it updates the endPresetZoneIndex of the lastPreset.
        /// The read Preset object is added to the data collection and becomes the lastPreset.
        /// </remarks>
        public override Preset Read(BinaryReader br)
        {
            Preset p = new Preset();
            string s = Encoding.UTF8.GetString(br.ReadBytes(20), 0, 20);
            if (s.IndexOf('\0') >= 0)
            {
                s = s.Substring(0, s.IndexOf('\0'));
            }
            p.Name = s;
            p.PatchNumber = br.ReadUInt16();
            p.Bank = br.ReadUInt16();
            p.startPresetZoneIndex = br.ReadUInt16();
            p.library = br.ReadUInt32();
            p.genre = br.ReadUInt32();
            p.morphology = br.ReadUInt32();
            if (lastPreset != null)
                lastPreset.endPresetZoneIndex = (ushort)(p.startPresetZoneIndex - 1);
            data.Add(p);
            lastPreset = p;
            return p;
        }

        /// <summary>
        /// Writes the preset data to the specified binary writer.
        /// </summary>
        /// <param name="bw">The binary writer to write the data to.</param>
        /// <param name="preset">The preset data to be written.</param>
        public override void Write(BinaryWriter bw, Preset preset)
        {
        }

        public override int Length => 38;

        /// <summary>
        /// Loads the preset zones into the data and removes the last preset zone, which is simply EOP.
        /// </summary>
        /// <param name="presetZones">An array of preset zones to be loaded.</param>
        /// <remarks>
        /// This method loads the preset zones into the data, excluding the last preset zone, which is simply EOP.
        /// It iterates through the preset zones and assigns them to the corresponding presets in the data.
        /// The method then removes the EOP record from the data.
        /// </remarks>
        public void LoadZones(Zone[] presetZones)
        {
            // don't do the last preset, which is simply EOP
            for (int preset = 0; preset < data.Count - 1; preset++)
            {
                Preset p = data[preset];
                p.Zones = new Zone[p.endPresetZoneIndex - p.startPresetZoneIndex + 1];
                Array.Copy(presetZones, p.startPresetZoneIndex, p.Zones, 0, p.Zones.Length);
            }
            // we can get rid of the EOP record now
            data.RemoveAt(data.Count - 1);
        }

        public Preset[] Presets => data.ToArray();
    }
}