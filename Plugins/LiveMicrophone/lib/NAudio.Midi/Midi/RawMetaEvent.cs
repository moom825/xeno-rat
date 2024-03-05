using System.IO;
using System.Text;

namespace NAudio.Midi
{
    /// <summary>
    /// Represents a MIDI meta event with raw data
    /// </summary>
    public class RawMetaEvent : MetaEvent
    {
        /// <summary>
        /// Raw data contained in the meta event
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        ///  Creates a meta event with raw data
        /// </summary>
        public RawMetaEvent(MetaEventType metaEventType, long absoluteTime, byte[] data) : base(metaEventType, data?.Length ?? 0, absoluteTime)
        {
            Data = data;
        }

        /// <summary>
        /// Clones the current MidiEvent and returns a new instance of RawMetaEvent.
        /// </summary>
        /// <returns>A new instance of RawMetaEvent that is a clone of the current MidiEvent.</returns>
        public override MidiEvent Clone() => new RawMetaEvent(MetaEventType, AbsoluteTime, (byte[])Data?.Clone());

        /// <summary>
        /// Returns a string representation of the object, including the hexadecimal representation of the data.
        /// </summary>
        /// <returns>A string containing the hexadecimal representation of the data in the object.</returns>
        /// <remarks>
        /// This method overrides the base ToString method and appends the hexadecimal representation of the data in the object to the string representation.
        /// The hexadecimal representation is obtained by iterating through each byte in the Data array and formatting it as a two-digit hexadecimal number.
        /// The resulting string includes the base ToString representation followed by the hexadecimal data representation.
        /// </remarks>
        public override string ToString()
        {
            var sb = new StringBuilder().Append(base.ToString());
            foreach (var b in Data)
                sb.AppendFormat(" {0:X2}", b);
            return sb.ToString();
        }

        /// <summary>
        /// Exports the data to a binary writer, updating the <paramref name="absoluteTime"/> and writing the data if it is not null.
        /// </summary>
        /// <param name="absoluteTime">The absolute time reference to be updated.</param>
        /// <param name="writer">The binary writer to which the data will be exported.</param>
        /// <remarks>
        /// This method first calls the base class's Export method to update the <paramref name="absoluteTime"/>.
        /// If the <see cref="Data"/> is not null, it writes the data to the <paramref name="writer"/>.
        /// </remarks>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            if (Data == null) return;
            writer.Write(Data, 0, Data.Length);
        }
    }
}
