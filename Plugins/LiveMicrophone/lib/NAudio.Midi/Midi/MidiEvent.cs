using System;
using System.IO;

namespace NAudio.Midi 
{
    /// <summary>
    /// Represents an individual MIDI event
    /// </summary>
    public class MidiEvent
        : ICloneable
    {
        /// <summary>The MIDI command code</summary>
        private MidiCommandCode commandCode;
        private int channel;
        private int deltaTime;
        private long absoluteTime;

        /// <summary>
        /// Converts a raw MIDI message to a MidiEvent object.
        /// </summary>
        /// <param name="rawMessage">The raw MIDI message to be converted.</param>
        /// <returns>A MidiEvent object representing the raw MIDI message.</returns>
        /// <remarks>
        /// This method parses the raw MIDI message and constructs a corresponding MidiEvent object.
        /// It extracts the command code, channel, and data bytes from the raw message and creates the appropriate MidiEvent based on the command code.
        /// If the command code is NoteOn, NoteOff, or KeyAfterTouch and the data2 value is greater than 0, it creates a NoteOnEvent; otherwise, it creates a NoteEvent.
        /// For ControlChange, PatchChange, ChannelAfterTouch, and PitchWheelChange command codes, it creates the corresponding events with the extracted data values.
        /// For other command codes, it throws a FormatException indicating an unsupported MIDI Command Code for the raw message.
        /// </remarks>
        /// <exception cref="FormatException">Thrown when the raw message contains an unsupported MIDI Command Code.</exception>
        public static MidiEvent FromRawMessage(int rawMessage)
        {
            long absoluteTime = 0;
            int b = rawMessage & 0xFF;
            int data1 = (rawMessage >> 8) & 0xFF;
            int data2 = (rawMessage >> 16) & 0xFF;
            MidiCommandCode commandCode;
            int channel = 1;

            if ((b & 0xF0) == 0xF0)
            {
                // both bytes are used for command code in this case
                commandCode = (MidiCommandCode)b;
            }
            else
            {
                commandCode = (MidiCommandCode)(b & 0xF0);
                channel = (b & 0x0F) + 1;
            }

            MidiEvent me;
            switch (commandCode)
            {
                case MidiCommandCode.NoteOn:
                case MidiCommandCode.NoteOff:
                case MidiCommandCode.KeyAfterTouch:
                    if (data2 > 0 && commandCode == MidiCommandCode.NoteOn)
                    {
                        me = new NoteOnEvent(absoluteTime, channel, data1, data2, 0);
                    }
                    else
                    {
                        me = new NoteEvent(absoluteTime, channel, commandCode, data1, data2);
                    }
                    break;
                case MidiCommandCode.ControlChange:
                    me = new ControlChangeEvent(absoluteTime,channel,(MidiController)data1,data2);
                    break;
                case MidiCommandCode.PatchChange:
                    me = new PatchChangeEvent(absoluteTime,channel,data1);
                    break;
                case MidiCommandCode.ChannelAfterTouch:
                    me = new ChannelAfterTouchEvent(absoluteTime,channel,data1);
                    break;
                case MidiCommandCode.PitchWheelChange:
                    me = new PitchWheelChangeEvent(absoluteTime, channel, data1 + (data2 << 7));
                    break;
                case MidiCommandCode.TimingClock:
                case MidiCommandCode.StartSequence:
                case MidiCommandCode.ContinueSequence:
                case MidiCommandCode.StopSequence:
                case MidiCommandCode.AutoSensing:
                    me = new MidiEvent(absoluteTime,channel,commandCode);
                    break;
                //case MidiCommandCode.MetaEvent:
                //case MidiCommandCode.Sysex:
                default:
                    throw new FormatException(String.Format("Unsupported MIDI Command Code for Raw Message {0}", commandCode));
            }
            return me;

        }

        /// <summary>
        /// Reads the next MIDI event from the BinaryReader and returns the corresponding MidiEvent.
        /// </summary>
        /// <param name="br">The BinaryReader used to read the MIDI data.</param>
        /// <param name="previous">The previous MidiEvent, used to determine running status.</param>
        /// <returns>The MidiEvent read from the BinaryReader.</returns>
        /// <remarks>
        /// This method reads the next MIDI event from the BinaryReader and returns the corresponding MidiEvent.
        /// It first reads the delta time using the ReadVarInt method, then determines the command code and channel based on the MIDI data.
        /// Depending on the command code, it creates a specific type of MidiEvent (e.g., NoteOnEvent, NoteOffEvent, ControlChangeEvent) using the BinaryReader.
        /// If the command code is not supported, it throws a FormatException with a message indicating the unsupported MIDI Command Code.
        /// The created MidiEvent is then populated with the channel, delta time, and command code before being returned.
        /// </remarks>
        /// <exception cref="FormatException">Thrown when the MIDI Command Code is not supported.</exception>
        public static MidiEvent ReadNextEvent(BinaryReader br, MidiEvent previous) 
        {
            int deltaTime = ReadVarInt(br);
            MidiCommandCode commandCode;
            int channel = 1;
            byte b = br.ReadByte();
            if((b & 0x80) == 0) 
            {
                // a running command - command & channel are same as previous
                commandCode = previous.CommandCode;
                channel = previous.Channel;
                br.BaseStream.Position--; // need to push this back
            }
            else 
            {
                if((b & 0xF0) == 0xF0) 
                {
                    // both bytes are used for command code in this case
                    commandCode = (MidiCommandCode) b;
                }
                else 
                {
                    commandCode = (MidiCommandCode) (b & 0xF0);
                    channel = (b & 0x0F) + 1;
                }
            }
            
            MidiEvent me;
            switch(commandCode) 
            {
            case MidiCommandCode.NoteOn:
                me = new NoteOnEvent(br);
                break;
            case MidiCommandCode.NoteOff:
            case MidiCommandCode.KeyAfterTouch:
                me = new NoteEvent(br);
                break;
            case MidiCommandCode.ControlChange:
                me = new ControlChangeEvent(br);
                break;
            case MidiCommandCode.PatchChange:
                me = new PatchChangeEvent(br);
                break;
            case MidiCommandCode.ChannelAfterTouch:
                me = new ChannelAfterTouchEvent(br);
                break;
            case MidiCommandCode.PitchWheelChange:
                me = new PitchWheelChangeEvent(br);
                break;
            case MidiCommandCode.TimingClock:
            case MidiCommandCode.StartSequence:
            case MidiCommandCode.ContinueSequence:
            case MidiCommandCode.StopSequence:
                me = new MidiEvent();
                break;
            case MidiCommandCode.Sysex:
                me = SysexEvent.ReadSysexEvent(br);
                break;
            case MidiCommandCode.MetaEvent:
                me = MetaEvent.ReadMetaEvent(br);
                break;
            default:
                throw new FormatException(String.Format("Unsupported MIDI Command Code {0:X2}",(byte) commandCode));
            }
            me.channel = channel;
            me.deltaTime = deltaTime;
            me.commandCode = commandCode;
            return me;
        }

        /// <summary>
        /// Returns the short message value calculated based on the channel and command code.
        /// </summary>
        /// <returns>The short message value calculated as (channel - 1) + (int)commandCode.</returns>
        public virtual int GetAsShortMessage()
        {
            return (channel - 1) + (int)commandCode;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        protected MidiEvent()
        {
        }

        /// <summary>
        /// Creates a MIDI event with specified parameters
        /// </summary>
        /// <param name="absoluteTime">Absolute time of this event</param>
        /// <param name="channel">MIDI channel number</param>
        /// <param name="commandCode">MIDI command code</param>
        public MidiEvent(long absoluteTime, int channel, MidiCommandCode commandCode)
        {
            this.absoluteTime = absoluteTime;
            Channel = channel;
            this.commandCode = commandCode;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public virtual MidiEvent Clone() => (MidiEvent)MemberwiseClone();

        object ICloneable.Clone() => Clone();

        /// <summary>
        /// The MIDI Channel Number for this event (1-16)
        /// </summary>
        public virtual int Channel 
        {
            get => channel;
            set
            {
                if ((value < 1) || (value > 16))
                {
                    throw new ArgumentOutOfRangeException("value", value,
                        String.Format("Channel must be 1-16 (Got {0})",value));
                }
                channel = value;
            }
        }
        
        /// <summary>
        /// The Delta time for this event
        /// </summary>
        public int DeltaTime 
        {
            get 
            {
                return deltaTime;
            }
        }
        
        /// <summary>
        /// The absolute time for this event
        /// </summary>
        public long AbsoluteTime 
        {
            get 
            {
                return absoluteTime;
            }
            set 
            {
                absoluteTime = value;
            }
        }
        
        /// <summary>
        /// The command code for this event
        /// </summary>
        public MidiCommandCode CommandCode 
        {
            get 
            {
                return commandCode;
            }
        }

        /// <summary>
        /// Checks if the given MIDI event represents a Note Off message.
        /// </summary>
        /// <param name="midiEvent">The MIDI event to be checked.</param>
        /// <returns>True if the MIDI event represents a Note Off message; otherwise, false.</returns>
        /// <remarks>
        /// This method checks if the provided <paramref name="midiEvent"/> is not null and if its command code is NoteOn.
        /// If the command code is NoteOn, it further checks if the velocity of the NoteEvent is 0 and returns true.
        /// If the command code is not NoteOn, it directly checks if the command code is NoteOff and returns the result.
        /// If the provided <paramref name="midiEvent"/> is null, it returns false.
        /// </remarks>
        public static bool IsNoteOff(MidiEvent midiEvent)
        {
            if (midiEvent != null)
            {
                if (midiEvent.CommandCode == MidiCommandCode.NoteOn)
                {
                    NoteEvent ne = (NoteEvent)midiEvent;
                    return (ne.Velocity == 0);
                }
                return (midiEvent.CommandCode == MidiCommandCode.NoteOff);
            }
            return false;
        }

        /// <summary>
        /// Checks if the provided MIDI event is a Note On event and returns true if the velocity is greater than 0.
        /// </summary>
        /// <param name="midiEvent">The MIDI event to be checked.</param>
        /// <returns>True if the provided <paramref name="midiEvent"/> is a Note On event with velocity greater than 0; otherwise, false.</returns>
        public static bool IsNoteOn(MidiEvent midiEvent)
        {
            if (midiEvent != null)
            {
                if (midiEvent.CommandCode == MidiCommandCode.NoteOn)
                {
                    NoteEvent ne = (NoteEvent)midiEvent;
                    return (ne.Velocity > 0);
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if the provided MIDI event is an end track event.
        /// </summary>
        /// <param name="midiEvent">The MIDI event to be checked.</param>
        /// <returns>True if the MIDI event is an end track event; otherwise, false.</returns>
        /// <remarks>
        /// This method checks if the provided MIDI event is a meta event and if so, it further checks if the meta event type is EndTrack.
        /// If the provided MIDI event is null or not a meta event, the method returns false.
        /// </remarks>
        public static bool IsEndTrack(MidiEvent midiEvent)
        {
            if (midiEvent != null)
            {
                MetaEvent me = midiEvent as MetaEvent;
                if (me != null)
                {
                    return me.MetaEventType == MetaEventType.EndTrack;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns a string representation of the current Midi event.
        /// </summary>
        /// <returns>
        /// A string representing the Midi event. If the <see cref="commandCode"/> is greater than or equal to <see cref="MidiCommandCode.Sysex"/>,
        /// the string contains the <see cref="absoluteTime"/> and the <see cref="commandCode"/>.
        /// Otherwise, the string contains the <see cref="absoluteTime"/>, the <see cref="commandCode"/>, and the <see cref="channel"/>.
        /// </returns>
        /// <remarks>
        /// This method returns a string representation of the current Midi event. If the <see cref="commandCode"/> is greater than or equal to <see cref="MidiCommandCode.Sysex"/>,
        /// the string contains the <see cref="absoluteTime"/> and the <see cref="commandCode"/>.
        /// Otherwise, the string contains the <see cref="absoluteTime"/>, the <see cref="commandCode"/>, and the <see cref="channel"/>.
        /// </remarks>
        public override string ToString() 
        {
            if(commandCode >= MidiCommandCode.Sysex)
                return String.Format("{0} {1}",absoluteTime,commandCode);
            else
                return String.Format("{0} {1} Ch: {2}", absoluteTime, commandCode, channel);
        }

        /// <summary>
        /// Reads a variable-length encoded integer from the provided BinaryReader and returns the result.
        /// </summary>
        /// <param name="br">The BinaryReader from which to read the variable-length encoded integer.</param>
        /// <returns>The variable-length encoded integer read from the BinaryReader.</returns>
        /// <remarks>
        /// This method reads a variable-length encoded integer from the provided BinaryReader by reading up to 4 bytes and decoding the value based on the MSB (Most Significant Bit) of each byte.
        /// The method shifts the value by 7 bits for each byte read and adds the lower 7 bits of the byte to the value until a byte with the MSB set to 0 is encountered, indicating the end of the encoded integer.
        /// If the method does not encounter a valid end byte after reading 4 bytes, it throws a FormatException with the message "Invalid Var Int".
        /// </remarks>
        /// <exception cref="FormatException">Thrown when the method encounters an invalid variable-length encoded integer format.</exception>
        public static int ReadVarInt(BinaryReader br) 
        {
            int value = 0;
            byte b;
            for(int n = 0; n < 4; n++) 
            {
                b = br.ReadByte();
                value <<= 7;
                value += (b & 0x7F);
                if((b & 0x80) == 0) 
                {
                    return value;
                }
            }
            throw new FormatException("Invalid Var Int");
        }

        /// <summary>
        /// Writes a variable-length encoded integer to the specified BinaryWriter.
        /// </summary>
        /// <param name="writer">The BinaryWriter to write the integer to.</param>
        /// <param name="value">The integer value to be written.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is negative or exceeds the maximum allowed value (0x0FFFFFFF).</exception>
        /// <remarks>
        /// This method writes a variable-length encoded integer to the specified BinaryWriter. The integer value is encoded using a variable-length format, where each byte contains 7 bits of the original value and a continuation bit. The process continues until all bits of the original value have been encoded.
        /// </remarks>
        public static void WriteVarInt(BinaryWriter writer, int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException("value", value, "Cannot write a negative Var Int");
            }
            if (value > 0x0FFFFFFF)
            {
                throw new ArgumentOutOfRangeException("value", value, "Maximum allowed Var Int is 0x0FFFFFFF");
            }

            int n = 0;
            byte[] buffer = new byte[4];
            do
            {
                buffer[n++] = (byte)(value & 0x7F);
                value >>= 7;
            } while (value > 0);
            
            while (n > 0)
            {
                n--;
                if(n > 0)
                    writer.Write((byte) (buffer[n] | 0x80));
                else 
                    writer.Write(buffer[n]);
            }
        }

        /// <summary>
        /// Exports the MIDI event and updates the absolute time.
        /// </summary>
        /// <param name="absoluteTime">The absolute time of the event.</param>
        /// <param name="writer">The BinaryWriter to write the event to.</param>
        /// <exception cref="FormatException">Thrown when the event is unsorted.</exception>
        /// <remarks>
        /// This method exports the MIDI event to the specified BinaryWriter and updates the absolute time.
        /// If the event's absolute time is less than the specified absolute time, a FormatException is thrown with the message "Can't export unsorted MIDI events".
        /// The method then writes the variable-length quantity representing the time difference to the writer, updates the absolute time, and writes the event data to the writer.
        /// </remarks>
        public virtual void Export(ref long absoluteTime, BinaryWriter writer)
        {
            if (this.absoluteTime < absoluteTime)
            {
                throw new FormatException("Can't export unsorted MIDI events");
            }
            WriteVarInt(writer,(int) (this.absoluteTime - absoluteTime));
            absoluteTime = this.absoluteTime;
            int output = (int) commandCode;
            if (commandCode != MidiCommandCode.MetaEvent)
            {
                output += (channel - 1);
            }
            writer.Write((byte)output);
        }
    }
}