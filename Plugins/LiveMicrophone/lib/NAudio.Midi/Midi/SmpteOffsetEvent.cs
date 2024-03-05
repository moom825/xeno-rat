using System;
using System.IO;

namespace NAudio.Midi
{
    /// <summary>
    /// SMPTE Offset Event
    /// </summary>
    public class SmpteOffsetEvent : MetaEvent
    {
        private readonly byte hours;
        private readonly byte minutes;
        private readonly byte seconds;
        private readonly byte frames;
        private readonly byte subFrames; // 100ths of a frame

        /// <summary>
        /// Creates a new time signature event
        /// </summary>
        public SmpteOffsetEvent(byte hours, byte minutes, byte seconds, byte frames, byte subFrames)
        {
            this.hours = hours;
            this.minutes = minutes;
            this.seconds = seconds;
            this.frames = frames;
            this.subFrames = subFrames;
        }

        /// <summary>
        /// Reads a new time signature event from a MIDI stream
        /// </summary>
        /// <param name="br">The MIDI stream</param>
        /// <param name="length">The data length</param>
        public SmpteOffsetEvent(BinaryReader br,int length) 
        {
            if(length != 5) 
            {
                throw new FormatException(String.Format("Invalid SMPTE Offset length: Got {0}, expected 5",length));
            }
            hours = br.ReadByte();
            minutes = br.ReadByte();
            seconds = br.ReadByte();
            frames = br.ReadByte();
            subFrames = br.ReadByte();
        }

        /// <summary>
        /// Creates a new instance of the SmpteOffsetEvent with the same property values as the current instance.
        /// </summary>
        /// <returns>A new instance of SmpteOffsetEvent with the same property values as the current instance.</returns>
        public override MidiEvent Clone() => (SmpteOffsetEvent)MemberwiseClone();

        /// <summary>
        /// Hours
        /// </summary>
        public int Hours => hours;

        /// <summary>
        /// Minutes
        /// </summary>
        public int Minutes => minutes;

        /// <summary>
        /// Seconds
        /// </summary>
        public int Seconds => seconds;

        /// <summary>
        /// Frames
        /// </summary>
        public int Frames => frames;

        /// <summary>
        /// SubFrames
        /// </summary>
        public int SubFrames => subFrames;

        /// <summary>
        /// Returns a formatted string representing the time.
        /// </summary>
        /// <returns>A string formatted as "{base.ToString()} {hours}:{minutes}:{seconds}:{frames}:{subFrames}".</returns>
        public override string ToString() 
        {
            return String.Format("{0} {1}:{2}:{3}:{4}:{5}",
                base.ToString(),hours,minutes,seconds,frames,subFrames);
        }

        /// <summary>
        /// Exports the time data to a binary writer.
        /// </summary>
        /// <param name="absoluteTime">The absolute time value.</param>
        /// <param name="writer">The binary writer to which the time data is exported.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="writer"/> is null.</exception>
        /// <remarks>
        /// This method exports the time data, including hours, minutes, seconds, frames, and subframes, to the specified binary writer.
        /// </remarks>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            writer.Write(hours);
            writer.Write(minutes);
            writer.Write(seconds);
            writer.Write(frames);
            writer.Write(subFrames);
        }
    }
}

