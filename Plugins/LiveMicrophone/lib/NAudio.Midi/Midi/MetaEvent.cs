using System;
using System.IO;
using System.Text;

namespace NAudio.Midi 
{
    /// <summary>
    /// Represents a MIDI meta event
    /// </summary>
    public class MetaEvent : MidiEvent 
    {
        private MetaEventType metaEvent;
        internal int metaDataLength;

        /// <summary>
        /// Gets the type of this meta event
        /// </summary>
        public MetaEventType MetaEventType
        {
            get
            {
                return metaEvent;
            }
        }

        /// <summary>
        /// Empty constructor
        /// </summary>
        protected MetaEvent()
        {
        }

        /// <summary>
        /// Custom constructor for use by derived types, who will manage the data themselves
        /// </summary>
        /// <param name="metaEventType">Meta event type</param>
        /// <param name="metaDataLength">Meta data length</param>
        /// <param name="absoluteTime">Absolute time</param>
        public MetaEvent(MetaEventType metaEventType, int metaDataLength, long absoluteTime)
            : base(absoluteTime,1,MidiCommandCode.MetaEvent)
        {
            this.metaEvent = metaEventType;
            this.metaDataLength = metaDataLength;
        }

        /// <summary>
        /// Clones the current MidiEvent and returns the cloned instance.
        /// </summary>
        /// <returns>A new instance of MidiEvent that is a clone of the current instance.</returns>
        public override MidiEvent Clone() => new MetaEvent(metaEvent, metaDataLength, AbsoluteTime);

        /// <summary>
        /// Reads a meta event from the provided BinaryReader and returns the corresponding MetaEvent object.
        /// </summary>
        /// <param name="br">The BinaryReader used to read the meta event.</param>
        /// <returns>The MetaEvent object representing the meta event read from the BinaryReader.</returns>
        /// <remarks>
        /// This method reads a meta event from the provided BinaryReader and returns the corresponding MetaEvent object.
        /// It first reads the MetaEventType from the BinaryReader, then reads the length of the meta event using the ReadVarInt method.
        /// Based on the type of meta event, it creates and returns the corresponding MetaEvent object by instantiating the appropriate class.
        /// If the meta event is of type EndTrack, it checks if the length is 0 and throws a FormatException if it's not.
        /// If the meta event is of any other type, it reads the data for the meta event and returns a RawMetaEvent object with the meta event type, default long value, and the read data.
        /// </remarks>
        /// <exception cref="FormatException">Thrown when the end track length is not 0 or when failed to read metaevent's data fully.</exception>
        public static MetaEvent ReadMetaEvent(BinaryReader br) 
        {
            MetaEventType metaEvent = (MetaEventType) br.ReadByte();
            int length = ReadVarInt(br);
            
            MetaEvent me = new MetaEvent();
            switch(metaEvent) 
            {
            case MetaEventType.TrackSequenceNumber: // Sets the track's sequence number.
                me = new TrackSequenceNumberEvent(br,length);
                break;
            case MetaEventType.TextEvent: // Text event
            case MetaEventType.Copyright: // Copyright
            case MetaEventType.SequenceTrackName: // Sequence / Track Name
            case MetaEventType.TrackInstrumentName: // Track instrument name
            case MetaEventType.Lyric: // lyric
            case MetaEventType.Marker: // marker
            case MetaEventType.CuePoint: // cue point
            case MetaEventType.ProgramName:
            case MetaEventType.DeviceName:
                me = new TextEvent(br,length);
                break;
            case MetaEventType.EndTrack: // This event must come at the end of each track
                if(length != 0) 
                {
                    throw new FormatException("End track length");
                }
                break;
            case MetaEventType.SetTempo: // Set tempo
                me = new TempoEvent(br,length);
                break;
            case MetaEventType.TimeSignature: // Time signature
                me = new TimeSignatureEvent(br,length);
                break;
            case MetaEventType.KeySignature: // Key signature
                me = new KeySignatureEvent(br, length);
                break;
            case MetaEventType.SequencerSpecific: // Sequencer specific information
                me = new SequencerSpecificEvent(br, length);
                break;
            case MetaEventType.SmpteOffset:
                me = new SmpteOffsetEvent(br, length);
                break;
            default:
//System.Windows.Forms.MessageBox.Show(String.Format("Unsupported MetaEvent {0} length {1} pos {2}",metaEvent,length,br.BaseStream.Position));
                var data = br.ReadBytes(length);
                if (data.Length != length)
                {
                    throw new FormatException("Failed to read metaevent's data fully");
                }
                return new RawMetaEvent(metaEvent, default(long), data);
            }
            me.metaEvent = metaEvent;
            me.metaDataLength = length;
            
            return me;
        }

        /// <summary>
        /// Returns a string representation of the object, combining the AbsoluteTime and metaEvent properties.
        /// </summary>
        /// <returns>A string containing the AbsoluteTime and metaEvent properties.</returns>
        public override string ToString() 
        {
            return $"{AbsoluteTime} {metaEvent}";
        }

        /// <summary>
        /// Exports the meta event and its associated data to a binary writer.
        /// </summary>
        /// <param name="absoluteTime">The absolute time of the event.</param>
        /// <param name="writer">The binary writer to which the event and its data will be exported.</param>
        /// <remarks>
        /// This method exports the meta event and its associated data to the specified binary writer.
        /// It first calls the base class's Export method to export the absolute time, then writes the meta event as a byte, followed by writing the length of the meta data using variable-length encoding.
        /// </remarks>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            writer.Write((byte)metaEvent);
            WriteVarInt(writer, metaDataLength);
        }
    }
}