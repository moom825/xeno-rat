using System;
using System.IO;
using System.Text;

namespace NAudio.Midi 
{
    /// <summary>
    /// Represents a MIDI text event
    /// </summary>
    public class TextEvent : MetaEvent 
    {
        private byte[] data;
        
        /// <summary>
        /// Reads a new text event from a MIDI stream
        /// </summary>
        /// <param name="br">The MIDI stream</param>
        /// <param name="length">The data length</param>
        public TextEvent(BinaryReader br,int length) 
        {
            data = br.ReadBytes(length);
        }

        /// <summary>
        /// Creates a new TextEvent
        /// </summary>
        /// <param name="text">The text in this type</param>
        /// <param name="metaEventType">MetaEvent type (must be one that is
        /// associated with text data)</param>
        /// <param name="absoluteTime">Absolute time of this event</param>
        public TextEvent(string text, MetaEventType metaEventType, long absoluteTime)
            : base(metaEventType, text.Length, absoluteTime)
        {
            Text = text;
        }

        /// <summary>
        /// Creates a new instance of the TextEvent class that is a copy of the current instance.
        /// </summary>
        /// <returns>A new TextEvent that is a copy of this instance.</returns>
        public override MidiEvent Clone() => (TextEvent)MemberwiseClone();

        /// <summary>
        /// The contents of this text event
        /// </summary>
        public string Text
        {
            get 
            { 
                Encoding byteEncoding = NAudio.Utils.ByteEncoding.Instance;
                return byteEncoding.GetString(data); 
            }
            set
            {
                Encoding byteEncoding = NAudio.Utils.ByteEncoding.Instance;
                data = byteEncoding.GetBytes(value);
                metaDataLength = data.Length;
            }
        }
        
        /// <summary>
        /// The raw contents of this text event
        /// </summary>
        public byte[] Data
        {
            get
            {
                return data;
            }
            set
            {
                data = value;
                metaDataLength = data.Length;
            }
        }

        /// <summary>
        /// Returns a string that represents the current object, including the base object's string representation and the value of the 'Text' property.
        /// </summary>
        /// <returns>A string that combines the string representation of the base object and the value of the 'Text' property.</returns>
        /// <remarks>
        /// This method overrides the default ToString method to provide a custom string representation of the current object.
        /// The returned string includes the string representation of the base object and the value of the 'Text' property.
        /// </remarks>
        public override string ToString() 
        {
            return String.Format("{0} {1}",base.ToString(),Text);
        }

        /// <summary>
        /// Exports the data to a BinaryWriter after performing the base export operation.
        /// </summary>
        /// <param name="absoluteTime">A reference to the absolute time.</param>
        /// <param name="writer">The BinaryWriter to which the data is exported.</param>
        /// <remarks>
        /// This method performs the base export operation by calling the base class's Export method with the provided <paramref name="absoluteTime"/> and <paramref name="writer"/>.
        /// It then writes the data to the <paramref name="writer"/>.
        /// </remarks>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            writer.Write(data);
        }
    }
}
