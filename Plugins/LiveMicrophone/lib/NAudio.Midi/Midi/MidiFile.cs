using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using NAudio.Utils;

namespace NAudio.Midi 
{
    /// <summary>
    /// Class able to read a MIDI file
    /// </summary>
    public class MidiFile 
    {
        private readonly MidiEventCollection events;
        private readonly ushort fileFormat;
        //private ushort tracks;
        private readonly ushort deltaTicksPerQuarterNote;
        private readonly bool strictChecking;

        /// <summary>
        /// Opens a MIDI file for reading
        /// </summary>
        /// <param name="filename">Name of MIDI file</param>
        public MidiFile(string filename)
            : this(filename,true)
        {
        }

        /// <summary>
        /// MIDI File format
        /// </summary>
        public int FileFormat => fileFormat;

        /// <summary>
        /// Opens a MIDI file for reading
        /// </summary>
        /// <param name="filename">Name of MIDI file</param>
        /// <param name="strictChecking">If true will error on non-paired note events</param>
        public MidiFile(string filename, bool strictChecking) :
            this(File.OpenRead(filename), strictChecking, true)
        {
        }

        /// <summary>
        /// Opens a MIDI file stream for reading
        /// </summary>
        /// <param name="inputStream">The input stream containing a MIDI file</param>
        /// <param name="strictChecking">If true will error on non-paired note events</param>
        public MidiFile(Stream inputStream, bool strictChecking) :
            this(inputStream, strictChecking, false)
        {
        }

        private MidiFile(Stream inputStream, bool strictChecking, bool ownInputStream)
        {
            this.strictChecking = strictChecking;
            
            var br = new BinaryReader(inputStream);
            try 
            {
                string chunkHeader = Encoding.UTF8.GetString(br.ReadBytes(4));
                if(chunkHeader != "MThd") 
                {
                    throw new FormatException("Not a MIDI file - header chunk missing");
                }
                uint chunkSize = SwapUInt32(br.ReadUInt32());
                
                if(chunkSize != 6) 
                {
                    throw new FormatException("Unexpected header chunk length");
                }
                // 0 = single track, 1 = multi-track synchronous, 2 = multi-track asynchronous
                fileFormat = SwapUInt16(br.ReadUInt16());
                int tracks = SwapUInt16(br.ReadUInt16());
                deltaTicksPerQuarterNote = SwapUInt16(br.ReadUInt16());

                events = new MidiEventCollection((fileFormat == 0) ? 0 : 1, deltaTicksPerQuarterNote);
                for (int n = 0; n < tracks; n++)
                {
                    events.AddTrack();
                }
                
                long absoluteTime = 0;
                
                for(int track = 0; track < tracks; track++) 
                {
                    if(fileFormat == 1) 
                    {
                        absoluteTime = 0;
                    }
                    chunkHeader = Encoding.UTF8.GetString(br.ReadBytes(4));
                    if(chunkHeader != "MTrk") 
                    {
                        throw new FormatException("Invalid chunk header");
                    }
                    chunkSize = SwapUInt32(br.ReadUInt32());

                    long startPos = br.BaseStream.Position;
                    MidiEvent me = null;
                    var outstandingNoteOns = new List<NoteOnEvent>();
                    while(br.BaseStream.Position < startPos + chunkSize) 
                    {
                        try
                        {
                            me = MidiEvent.ReadNextEvent(br, me);
                        }
                        catch (InvalidDataException)
                        {
                            if (strictChecking) throw;
                            continue;
                        }
                        catch (FormatException)
                        {
                            if (strictChecking) throw;
                            continue;
                        }

                        absoluteTime += me.DeltaTime;
                        me.AbsoluteTime = absoluteTime;
                        events[track].Add(me);
                        if (me.CommandCode == MidiCommandCode.NoteOn) 
                        {
                            var ne = (NoteEvent) me;
                            if(ne.Velocity > 0) 
                            {
                                outstandingNoteOns.Add((NoteOnEvent) ne);
                            }
                            else 
                            {
                                // don't remove the note offs, even though
                                // they are annoying
                                // events[track].Remove(me);
                                FindNoteOn(ne,outstandingNoteOns);
                            }
                        }
                        else if(me.CommandCode == MidiCommandCode.NoteOff) 
                        {
                            FindNoteOn((NoteEvent) me,outstandingNoteOns);
                        }
                        else if(me.CommandCode == MidiCommandCode.MetaEvent) 
                        {
                            MetaEvent metaEvent = (MetaEvent) me;
                            if(metaEvent.MetaEventType == MetaEventType.EndTrack) 
                            {
                                //break;
                                // some dodgy MIDI files have an event after end track
                                if (strictChecking)
                                {
                                    if (br.BaseStream.Position < startPos + chunkSize)
                                    {
                                        throw new FormatException(
                                            $"End Track event was not the last MIDI event on track {track}");
                                    }
                                }
                            }
                        }
                    }
                    if(outstandingNoteOns.Count > 0) 
                    {
                        if (strictChecking)
                        {
                            throw new FormatException(
                                $"Note ons without note offs {outstandingNoteOns.Count} (file format {fileFormat})");
                        }
                    }
                    if(br.BaseStream.Position != startPos + chunkSize) 
                    {
                        throw new FormatException($"Read too far {chunkSize}+{startPos}!={br.BaseStream.Position}");
                    }
                }
            }
            finally
            {
                if (ownInputStream)
                {
                    br.Dispose();
                }
            }
        }

        /// <summary>
        /// The collection of events in this MIDI file
        /// </summary>
        public MidiEventCollection Events => events;

        /// <summary>
        /// Number of tracks in this MIDI file
        /// </summary>
        public int Tracks => events.Tracks;

        /// <summary>
        /// Delta Ticks Per Quarter Note
        /// </summary>
        public int DeltaTicksPerQuarterNote => deltaTicksPerQuarterNote;

        /// <summary>
        /// Finds and updates the corresponding NoteOnEvent in the list of outstandingNoteOns with the provided NoteOffEvent.
        /// </summary>
        /// <param name="offEvent">The NoteOffEvent to find a corresponding NoteOnEvent for.</param>
        /// <param name="outstandingNoteOns">The list of outstanding NoteOnEvents to search for a matching NoteOnEvent.</param>
        /// <exception cref="FormatException">Thrown when a NoteOffEvent is received without a corresponding NoteOnEvent, and strictChecking is enabled.</exception>
        /// <remarks>
        /// This method iterates through the list of outstandingNoteOns to find a matching NoteOnEvent based on the channel and note number.
        /// If a matching NoteOnEvent is found, it updates the OffEvent property of the NoteOnEvent with the provided offEvent, removes the NoteOnEvent from the list of outstandingNoteOns, and sets the found flag to true.
        /// If no matching NoteOnEvent is found and strictChecking is enabled, it throws a FormatException with a message indicating that an off without an on was received.
        /// </remarks>
        private void FindNoteOn(NoteEvent offEvent, List<NoteOnEvent> outstandingNoteOns)
        {
            bool found = false;
            foreach(NoteOnEvent noteOnEvent in outstandingNoteOns)
            {
                if ((noteOnEvent.Channel == offEvent.Channel) && (noteOnEvent.NoteNumber == offEvent.NoteNumber)) 
                {
                    noteOnEvent.OffEvent = offEvent;
                    outstandingNoteOns.Remove(noteOnEvent);
                    found = true;
                    break;
                }
            }
            if(!found) 
            {
                if (strictChecking)
                {
                    throw new FormatException($"Got an off without an on {offEvent}");
                }
            }
        }

        /// <summary>
        /// Swaps the bytes of a 32-bit unsigned integer and returns the result.
        /// </summary>
        /// <param name="i">The 32-bit unsigned integer to swap the bytes of.</param>
        /// <returns>The 32-bit unsigned integer with its bytes swapped.</returns>
        /// <remarks>
        /// This method swaps the bytes of the input 32-bit unsigned integer <paramref name="i"/> such that the most significant byte becomes the least significant byte and vice versa.
        /// </remarks>
        private static uint SwapUInt32(uint i) 
        {
            return ((i & 0xFF000000) >> 24) | ((i & 0x00FF0000) >> 8) | ((i & 0x0000FF00) << 8) | ((i & 0x000000FF) << 24);
        }

        /// <summary>
        /// Swaps the high and low bytes of the input unsigned 16-bit integer and returns the result.
        /// </summary>
        /// <param name="i">The unsigned 16-bit integer to be swapped.</param>
        /// <returns>The result of swapping the high and low bytes of the input <paramref name="i"/>.</returns>
        /// <remarks>
        /// This method takes an unsigned 16-bit integer <paramref name="i"/> and swaps its high and low bytes.
        /// It does this by first masking the high and low bytes using bitwise AND operations, shifting the high byte to the low byte position, and shifting the low byte to the high byte position using bitwise OR operations.
        /// The result is then returned as the swapped unsigned 16-bit integer.
        /// </remarks>
        private static ushort SwapUInt16(ushort i) 
        {
            return (ushort) (((i & 0xFF00) >> 8) | ((i & 0x00FF) << 8));
        }

        /// <summary>
        /// Returns a string representation of the MIDI file.
        /// </summary>
        /// <returns>A string containing the format, number of tracks, and delta ticks per quarter note, followed by a list of MIDI events for each track.</returns>
        public override string ToString() 
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Format {0}, Tracks {1}, Delta Ticks Per Quarter Note {2}\r\n",
                fileFormat,Tracks,deltaTicksPerQuarterNote);
            for (var n = 0; n < Tracks; n++)
            {
                foreach (var midiEvent in events[n])
                {
                    sb.AppendFormat("{0}\r\n", midiEvent);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Exports the MIDI events to a file with the specified filename.
        /// </summary>
        /// <param name="filename">The name of the file to which the MIDI events will be exported.</param>
        /// <param name="events">The collection of MIDI events to be exported.</param>
        /// <exception cref="ArgumentException">Thrown when attempting to export more than one track to a type 0 file.</exception>
        /// <remarks>
        /// This method exports the MIDI events to a file with the specified filename.
        /// If the MIDI file type is 0 and there are more than one track, an <see cref="ArgumentException"/> is thrown.
        /// The method writes the MIDI events to the file in the specified format, including the header and track information.
        /// </remarks>
        public static void Export(string filename, MidiEventCollection events)
        {
            if (events.MidiFileType == 0 && events.Tracks > 1)
            {
                throw new ArgumentException("Can't export more than one track to a type 0 file");
            }
            using (var writer = new BinaryWriter(File.Create(filename)))
            {
                writer.Write(Encoding.UTF8.GetBytes("MThd"));
                writer.Write(SwapUInt32(6)); // chunk size
                writer.Write(SwapUInt16((ushort)events.MidiFileType));
                writer.Write(SwapUInt16((ushort)events.Tracks));
                writer.Write(SwapUInt16((ushort)events.DeltaTicksPerQuarterNote));

                for (int track = 0; track < events.Tracks; track++ )
                {
                    IList<MidiEvent> eventList = events[track];

                    writer.Write(Encoding.UTF8.GetBytes("MTrk"));
                    long trackSizePosition = writer.BaseStream.Position;
                    writer.Write(SwapUInt32(0));

                    long absoluteTime = events.StartAbsoluteTime;

                    // use a stable sort to preserve ordering of MIDI events whose 
                    // absolute times are the same
                    MergeSort.Sort(eventList, new MidiEventComparer());
                    if (eventList.Count > 0)
                    {
                        System.Diagnostics.Debug.Assert(MidiEvent.IsEndTrack(eventList[eventList.Count - 1]), "Exporting a track with a missing end track");
                    }
                    foreach (var midiEvent in eventList)
                    {
                        midiEvent.Export(ref absoluteTime, writer);
                    }

                    uint trackChunkLength = (uint)(writer.BaseStream.Position - trackSizePosition) - 4;
                    writer.BaseStream.Position = trackSizePosition;
                    writer.Write(SwapUInt32(trackChunkLength));
                    writer.BaseStream.Position += trackChunkLength;
                }
            }
        }
    }
}