using System;
using System.IO;
using System.Text;

namespace NAudio.Midi 
{
    /// <summary>
    /// Represents a MIDI tempo event
    /// </summary>
    public class TempoEvent : MetaEvent 
    {
        private int microsecondsPerQuarterNote;
        
        /// <summary>
        /// Reads a new tempo event from a MIDI stream
        /// </summary>
        /// <param name="br">The MIDI stream</param>
        /// <param name="length">the data length</param>
        public TempoEvent(BinaryReader br,int length) 
        {
            if(length != 3) 
            {
                throw new FormatException("Invalid tempo length");
            }
            microsecondsPerQuarterNote = (br.ReadByte() << 16) + (br.ReadByte() << 8) + br.ReadByte();
        }

        /// <summary>
        /// Creates a new tempo event with specified settings
        /// </summary>
        /// <param name="microsecondsPerQuarterNote">Microseconds per quarter note</param>
        /// <param name="absoluteTime">Absolute time</param>
        public TempoEvent(int microsecondsPerQuarterNote, long absoluteTime)
            : base(MetaEventType.SetTempo,3,absoluteTime)
        {
            this.microsecondsPerQuarterNote = microsecondsPerQuarterNote;
        }

        /// <summary>
        /// Creates a new instance of the TempoEvent class that is a copy of the current instance.
        /// </summary>
        /// <returns>A new instance of the TempoEvent class that is a copy of the current instance.</returns>
        public override MidiEvent Clone() => (TempoEvent)MemberwiseClone();

        /// <summary>
        /// Returns a string representation of the object.
        /// </summary>
        /// <returns>A string containing the object's name, beats per minute, and calculated tempo.</returns>
        /// <remarks>
        /// This method returns a formatted string that includes the object's name, beats per minute, and calculated tempo based on the microseconds per quarter note.
        /// The tempo is calculated using the formula: 60000000 / microsecondsPerQuarterNote.
        /// </remarks>
        public override string ToString() 
        {
            return String.Format("{0} {2}bpm ({1})",
                base.ToString(),
                microsecondsPerQuarterNote,
                (60000000 / microsecondsPerQuarterNote));
        }

        /// <summary>
        /// Microseconds per quarter note
        /// </summary>
        public int MicrosecondsPerQuarterNote
        {
            get { return microsecondsPerQuarterNote; }
            set { microsecondsPerQuarterNote = value; }
        }

        /// <summary>
        /// Tempo
        /// </summary>
        public double Tempo
        {
            get { return (60000000.0/microsecondsPerQuarterNote); }
            set { microsecondsPerQuarterNote = (int) (60000000.0/value); }
        }

        /// <summary>
        /// Exports the current object to a binary writer, including the microseconds per quarter note.
        /// </summary>
        /// <param name="absoluteTime">The absolute time value.</param>
        /// <param name="writer">The binary writer to which the object is exported.</param>
        /// <remarks>
        /// This method exports the current object to the specified binary writer, including the microseconds per quarter note.
        /// It first calls the base class's Export method to export the base object, then writes the microseconds per quarter note to the writer in little-endian format using three bytes.
        /// </remarks>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            writer.Write((byte) ((microsecondsPerQuarterNote >> 16) & 0xFF));
            writer.Write((byte) ((microsecondsPerQuarterNote >> 8) & 0xFF));
            writer.Write((byte) (microsecondsPerQuarterNote & 0xFF));
        }
    }
}