using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace NAudio.Midi 
{
    /// <summary>
    /// Represents a MIDI sysex message
    /// </summary>
    public class SysexEvent : MidiEvent 
    {
        private byte[] data;

        /// <summary>
        /// Reads a System Exclusive (Sysex) event from the provided BinaryReader and returns the SysexEvent object.
        /// </summary>
        /// <param name="br">The BinaryReader used to read the Sysex event data.</param>
        /// <returns>A SysexEvent object containing the data read from the BinaryReader.</returns>
        /// <remarks>
        /// This method reads a Sysex event from the provided BinaryReader by parsing the data until the termination byte 0xF7 is encountered.
        /// The parsed data is stored in a SysexEvent object and returned.
        /// </remarks>
        public static SysexEvent ReadSysexEvent(BinaryReader br) 
        {
            SysexEvent se = new SysexEvent();
            //se.length = ReadVarInt(br);
            //se.data = br.ReadBytes(se.length);

            List<byte> sysexData = new List<byte>();
            bool loop = true;
            while(loop) 
            {
                byte b = br.ReadByte();
                if(b == 0xF7) 
                {
                    loop = false;
                }
                else 
                {
                    sysexData.Add(b);
                }
            }
            
            se.data = sysexData.ToArray();

            return se;
        }

        /// <summary>
        /// Clones the SysexEvent and returns a new instance of MidiEvent.
        /// </summary>
        /// <returns>A new instance of MidiEvent with cloned SysexEvent data.</returns>
        public override MidiEvent Clone() => new SysexEvent { data = (byte[])data?.Clone() };

        /// <summary>
        /// Returns a string representation of the object.
        /// </summary>
        /// <returns>A string containing the hexadecimal representation of each byte in the data array, along with the absolute time and the length of the data array.</returns>
        /// <remarks>
        /// This method constructs a string by iterating through each byte in the data array and formatting it as a hexadecimal value.
        /// The resulting string includes the absolute time, the length of the data array, and the formatted hexadecimal representation of each byte.
        /// </remarks>
        public override string ToString() 
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in data)
            {
                sb.AppendFormat("{0:X2} ", b);
            }
            return String.Format("{0} Sysex: {1} bytes\r\n{2}",this.AbsoluteTime,data.Length,sb.ToString());
        }

        /// <summary>
        /// Exports the data to a binary writer after exporting the base class and writing the data and a specific byte.
        /// </summary>
        /// <param name="absoluteTime">The absolute time reference.</param>
        /// <param name="writer">The binary writer to which the data is exported.</param>
        /// <remarks>
        /// This method first exports the base class using the provided absolute time and writer, then writes the data to the writer and finally writes a specific byte (0xF7) to the writer.
        /// </remarks>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            //WriteVarInt(writer,length);
            //writer.Write(data, 0, data.Length);
            writer.Write(data, 0, data.Length);
            writer.Write((byte)0xF7);
        }
    }
}