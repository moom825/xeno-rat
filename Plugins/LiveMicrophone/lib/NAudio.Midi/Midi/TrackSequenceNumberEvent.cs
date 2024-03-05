using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NAudio.Midi
{
    /// <summary>
    /// Represents a MIDI track sequence number event event
    /// </summary>
    public class TrackSequenceNumberEvent : MetaEvent
    {
        private ushort sequenceNumber;

        /// <summary>
        /// Creates a new track sequence number event
        /// </summary>
        public TrackSequenceNumberEvent(ushort sequenceNumber)
        {
            this.sequenceNumber = sequenceNumber;
        }

        /// <summary>
        /// Reads a new track sequence number event from a MIDI stream
        /// </summary>
        /// <param name="br">The MIDI stream</param>
        /// <param name="length">the data length</param>
        public TrackSequenceNumberEvent(BinaryReader br, int length)
        {
            // TODO: there is a form of the TrackSequenceNumberEvent that
            // has a length of zero
            if(length != 2) 
            {
                throw new FormatException("Invalid sequence number length");
            }
            sequenceNumber = (ushort) ((br.ReadByte() << 8) + br.ReadByte());
        }

        /// <summary>
        /// Creates a new instance of the TrackSequenceNumberEvent class that is a copy of the current instance.
        /// </summary>
        /// <returns>A new TrackSequenceNumberEvent that is a copy of this instance.</returns>
        public override MidiEvent Clone() => (TrackSequenceNumberEvent)MemberwiseClone();

        /// <summary>
        /// Returns a string that represents the current object, including the base string representation and the sequence number.
        /// </summary>
        /// <returns>A string that combines the base string representation and the sequence number.</returns>
        public override string ToString()
        {
            return String.Format("{0} {1}", base.ToString(), sequenceNumber);
        }

        /// <summary>
        /// Exports the data to a binary writer, including the sequence number split into two bytes.
        /// </summary>
        /// <param name="absoluteTime">The absolute time reference for the export operation.</param>
        /// <param name="writer">The binary writer to which the data is exported.</param>
        /// <remarks>
        /// This method first calls the base class Export method to handle the common export operations.
        /// It then writes the high byte and low byte of the sequence number to the binary writer.
        /// </remarks>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            writer.Write((byte)((sequenceNumber >> 8) & 0xFF));
            writer.Write((byte)(sequenceNumber & 0xFF));
        }
    }
}
