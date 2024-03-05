using System;
using System.IO;
using System.Text;

namespace NAudio.Midi 
{
    /// <summary>
    /// Represents a MIDI pitch wheel change event
    /// </summary>
    public class PitchWheelChangeEvent : MidiEvent 
    {
        private int pitch;
        
        /// <summary>
        /// Reads a pitch wheel change event from a MIDI stream
        /// </summary>
        /// <param name="br">The MIDI stream to read from</param>
        public PitchWheelChangeEvent(BinaryReader br) 
        {
            byte b1 = br.ReadByte();
            byte b2 = br.ReadByte();
            if((b1 & 0x80) != 0) 
            {
                // TODO: might be a follow-on				
                throw new FormatException("Invalid pitchwheelchange byte 1");
            }
            if((b2 & 0x80) != 0) 
            {
                throw new FormatException("Invalid pitchwheelchange byte 2");
            }
            
            pitch = b1 + (b2 << 7); // 0x2000 is normal
        }

        /// <summary>
        /// Creates a new pitch wheel change event
        /// </summary>
        /// <param name="absoluteTime">Absolute event time</param>
        /// <param name="channel">Channel</param>
        /// <param name="pitchWheel">Pitch wheel value</param>
        public PitchWheelChangeEvent(long absoluteTime, int channel, int pitchWheel)
            : base(absoluteTime, channel, MidiCommandCode.PitchWheelChange)
        {
            Pitch = pitchWheel;
        }

        /// <summary>
        /// Returns a string representation of the object.
        /// </summary>
        /// <returns>A string containing the object's base representation, pitch, and pitch value relative to 0x2000.</returns>
        /// <remarks>
        /// This method returns a formatted string that includes the base representation of the object, its pitch, and the pitch value relative to 0x2000.
        /// </remarks>
        public override string ToString() 
        {
            return String.Format("{0} Pitch {1} ({2})",
                base.ToString(),
                this.pitch,
                this.pitch - 0x2000);
        }

        /// <summary>
        /// Pitch Wheel Value 0 is minimum, 0x2000 (8192) is default, 0x3FFF (16383) is maximum
        /// </summary>
        public int Pitch
        {
            get
            {
                return pitch;
            }
            set
            {
                if (value < 0 || value >= 0x4000)
                {
                    throw new ArgumentOutOfRangeException("value", "Pitch value must be in the range 0 - 0x3FFF");
                }
                pitch = value;
            }
        }

        /// <summary>
        /// Gets the short message representation of the MIDI event.
        /// </summary>
        /// <returns>The short message representation of the MIDI event.</returns>
        public override int GetAsShortMessage()
        {
            return base.GetAsShortMessage() + ((pitch & 0x7f) << 8) + (((pitch >> 7) & 0x7f) << 16);
        }

        /// <summary>
        /// Exports the object's data to a binary writer, including the pitch value.
        /// </summary>
        /// <param name="absoluteTime">The absolute time value.</param>
        /// <param name="writer">The binary writer to which the data is exported.</param>
        /// <remarks>
        /// This method exports the object's data to the specified binary writer, including the pitch value.
        /// The pitch value is written as two bytes, with the least significant 7 bits written first followed by the most significant 7 bits.
        /// </remarks>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            writer.Write((byte)(pitch & 0x7f));
            writer.Write((byte)((pitch >> 7) & 0x7f));
        }
    }
}