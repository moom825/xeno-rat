using System;
using System.IO;
using System.Text;

namespace NAudio.Midi
{
    /// <summary>
    /// Represents a Sequencer Specific event
    /// </summary>
    public class SequencerSpecificEvent : MetaEvent
    {
        private byte[] data;

        /// <summary>
        /// Reads a new sequencer specific event from a MIDI stream
        /// </summary>
        /// <param name="br">The MIDI stream</param>
        /// <param name="length">The data length</param>
        public SequencerSpecificEvent(BinaryReader br, int length)
        {
            this.data = br.ReadBytes(length);
        }

        /// <summary>
        /// Creates a new Sequencer Specific event
        /// </summary>
        /// <param name="data">The sequencer specific data</param>
        /// <param name="absoluteTime">Absolute time of this event</param>
        public SequencerSpecificEvent(byte[] data, long absoluteTime)
            : base(MetaEventType.SequencerSpecific, data.Length, absoluteTime)
        {
            this.data = data;
        }

        /// <summary>
        /// Clones the current MidiEvent and returns a new instance of SequencerSpecificEvent with the same data and absolute time.
        /// </summary>
        /// <returns>A new instance of SequencerSpecificEvent with the same data and absolute time as the current MidiEvent.</returns>
        public override MidiEvent Clone() => new SequencerSpecificEvent((byte[])data.Clone(), AbsoluteTime);

        /// <summary>
        /// The contents of this sequencer specific
        /// </summary>
        public byte[] Data
        {
            get
            {
                return this.data;
            }
            set
            {
                this.data = value;
                this.metaDataLength = this.data.Length;
            }
        }

        /// <summary>
        /// Returns a string representation of the object.
        /// </summary>
        /// <returns>A string containing the hexadecimal representation of the data array elements concatenated with a space.</returns>
        /// <remarks>
        /// This method overrides the base ToString method and appends the hexadecimal representation of each element in the data array to the string builder.
        /// The resulting string is then returned after removing the trailing space.
        /// </remarks>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(base.ToString());
            sb.Append(" ");
            foreach (var b in data)
            {
                sb.AppendFormat("{0:X2} ", b);
            }
            sb.Length--;
            return sb.ToString();
        }

        /// <summary>
        /// Exports the data to a binary writer, updating the <paramref name="absoluteTime"/> and writing the data.
        /// </summary>
        /// <param name="absoluteTime">The reference to the absolute time.</param>
        /// <param name="writer">The binary writer to which the data is exported.</param>
        /// <remarks>
        /// This method updates the <paramref name="absoluteTime"/> and writes the data to the specified binary writer.
        /// </remarks>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            writer.Write(data);
        }
    }
}