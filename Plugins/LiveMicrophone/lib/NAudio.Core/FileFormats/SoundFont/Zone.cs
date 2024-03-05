using System;

namespace NAudio.SoundFont
{
    /// <summary>
    /// A SoundFont zone
    /// </summary>
    public class Zone
    {
        internal ushort generatorIndex;
        internal ushort modulatorIndex;
        internal ushort generatorCount;
        internal ushort modulatorCount;

        /// <summary>
        /// Returns a formatted string representing the zone, generator count and index, modulator count and index.
        /// </summary>
        /// <returns>A string containing the zone, generator count and index, modulator count and index.</returns>
        public override string ToString()
        {
            return String.Format("Zone {0} Gens:{1} {2} Mods:{3}", generatorCount, generatorIndex,
                modulatorCount, modulatorIndex);
        }

        /// <summary>
        /// Modulators for this Zone
        /// </summary>
        public Modulator[] Modulators { get; set; }

        /// <summary>
        /// Generators for this Zone
        /// </summary>
        public Generator[] Generators { get; set; }

    }
}