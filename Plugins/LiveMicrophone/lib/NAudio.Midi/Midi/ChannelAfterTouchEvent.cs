using System;
using System.IO;

namespace NAudio.Midi
{
    /// <summary>
    /// Represents a MIDI Channel AfterTouch Event.
    /// </summary>
    public class ChannelAfterTouchEvent : MidiEvent
    {
        private byte afterTouchPressure;

        /// <summary>
        /// Creates a new ChannelAfterTouchEvent from raw MIDI data
        /// </summary>
        /// <param name="br">A binary reader</param>
        public ChannelAfterTouchEvent(BinaryReader br)
        {
            afterTouchPressure = br.ReadByte();
            if ((afterTouchPressure & 0x80) != 0)
            {
                // TODO: might be a follow-on
                throw new FormatException("Invalid afterTouchPressure");
            }
        }

        /// <summary>
        /// Creates a new Channel After-Touch Event
        /// </summary>
        /// <param name="absoluteTime">Absolute time</param>
        /// <param name="channel">Channel</param>
        /// <param name="afterTouchPressure">After-touch pressure</param>
        public ChannelAfterTouchEvent(long absoluteTime, int channel, int afterTouchPressure)
            : base(absoluteTime, channel, MidiCommandCode.ChannelAfterTouch)
        {
            AfterTouchPressure = afterTouchPressure;
        }

        /// <summary>
        /// Exports the aftertouch pressure value to a binary writer.
        /// </summary>
        /// <param name="absoluteTime">The absolute time reference for the export operation.</param>
        /// <param name="writer">The binary writer to which the aftertouch pressure value is exported.</param>
        /// <remarks>
        /// This method exports the aftertouch pressure value to the specified binary writer, modifying the writer in place.
        /// </remarks>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            writer.Write(afterTouchPressure);
        }

        /// <summary>
        /// The aftertouch pressure value
        /// </summary>
        public int AfterTouchPressure
        {
            get { return afterTouchPressure; }
            set
            {
                if (value < 0 || value > 127)
                {
                    throw new ArgumentOutOfRangeException("value", "After touch pressure must be in the range 0-127");
                }
                afterTouchPressure = (byte) value;
            }
        }

        /// <summary>
        /// Returns the short message value with the addition of the after touch pressure shifted by 8 bits.
        /// </summary>
        /// <returns>The short message value with the addition of the after touch pressure shifted by 8 bits.</returns>
        public override int GetAsShortMessage()
        {
            return base.GetAsShortMessage() + (afterTouchPressure << 8);
        }

        /// <summary>
        /// Returns a string representation that includes the base class's string representation and the value of the 'afterTouchPressure' property.
        /// </summary>
        /// <returns>A string that includes the base class's string representation and the value of the 'afterTouchPressure' property.</returns>
        public override string ToString()
        {
            return $"{base.ToString()} {afterTouchPressure}";
        }
    }
}
