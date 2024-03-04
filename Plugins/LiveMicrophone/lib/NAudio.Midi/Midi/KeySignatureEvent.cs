using System;
using System.IO;

namespace NAudio.Midi
{
    /// <summary>
    /// Represents a MIDI key signature event event
    /// </summary>
    public class KeySignatureEvent : MetaEvent
    {
        private readonly byte sharpsFlats;
        private readonly byte majorMinor;

        /// <summary>
        /// Reads a new track sequence number event from a MIDI stream
        /// </summary>
        /// <param name="br">The MIDI stream</param>
        /// <param name="length">the data length</param>
        public KeySignatureEvent(BinaryReader br, int length)
        {
            if (length != 2)
            {
                throw new FormatException("Invalid key signature length");
            }
            sharpsFlats = br.ReadByte(); // sf=sharps/flats (-7=7 flats, 0=key of C,7=7 sharps)
            majorMinor = br.ReadByte(); // mi=major/minor (0=major, 1=minor)
        }

        /// <summary>
        /// Creates a new Key signature event with the specified data
        /// </summary>
        public KeySignatureEvent(int sharpsFlats, int majorMinor, long absoluteTime)
            : base(MetaEventType.KeySignature, 2, absoluteTime)
        {
            this.sharpsFlats = (byte) sharpsFlats;
            this.majorMinor = (byte) majorMinor;
        }

        /// <summary>
        /// Creates a new instance of the KeySignatureEvent with the same property values as the current instance.
        /// </summary>
        /// <returns>A new KeySignatureEvent that is a copy of the current instance.</returns>
        public override MidiEvent Clone() => (KeySignatureEvent)MemberwiseClone();

        /// <summary>
        /// Number of sharps or flats
        /// </summary>
        public int SharpsFlats => (sbyte)sharpsFlats;

        /// <summary>
        /// Major or Minor key
        /// </summary>
        public int MajorMinor => majorMinor;

        /// <summary>
        /// Returns a formatted string representation of the object.
        /// </summary>
        /// <returns>A string that represents the object, including the base string representation, sharps/flats, and major/minor information.</returns>
        public override string ToString()
        {
            return String.Format("{0} {1} {2}", base.ToString(), SharpsFlats, majorMinor);
        }

        /// <summary>
        /// Exports the object's data to a binary writer.
        /// </summary>
        /// <param name="absoluteTime">The absolute time value.</param>
        /// <param name="writer">The binary writer to which the data is exported.</param>
        /// <exception cref="ArgumentNullException">Thrown when the binary writer is null.</exception>
        /// <remarks>
        /// This method exports the sharpsFlats and majorMinor properties to the specified binary writer.
        /// </remarks>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            writer.Write(sharpsFlats);
            writer.Write(majorMinor);
        }
    }
}