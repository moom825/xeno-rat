using System;
using System.Collections.Generic;
using NAudio.Utils;

namespace NAudio.Midi
{
    /// <summary>
    /// A helper class to manage collection of MIDI events
    /// It has the ability to organise them in tracks
    /// </summary>
    public class MidiEventCollection : IEnumerable<IList<MidiEvent>>
    {
        private int midiFileType;
        private readonly List<IList<MidiEvent>> trackEvents;

        /// <summary>
        /// Creates a new Midi Event collection
        /// </summary>
        /// <param name="midiFileType">Initial file type</param>
        /// <param name="deltaTicksPerQuarterNote">Delta Ticks Per Quarter Note</param>
        public MidiEventCollection(int midiFileType, int deltaTicksPerQuarterNote)
        {
            this.midiFileType = midiFileType;
            DeltaTicksPerQuarterNote = deltaTicksPerQuarterNote;
            StartAbsoluteTime = 0;
            trackEvents = new List<IList<MidiEvent>>();
        }

        /// <summary>
        /// The number of tracks
        /// </summary>
        public int Tracks => trackEvents.Count;

        /// <summary>
        /// The absolute time that should be considered as time zero
        /// Not directly used here, but useful for timeshifting applications
        /// </summary>
        public long StartAbsoluteTime { get; set; }

        /// <summary>
        /// The number of ticks per quarter note
        /// </summary>
        public int DeltaTicksPerQuarterNote { get; }

        /// <summary>
        /// Retrieves the MIDI events for the specified track number.
        /// </summary>
        /// <param name="trackNumber">The number of the track for which MIDI events are to be retrieved.</param>
        /// <returns>The list of MIDI events associated with the specified track number.</returns>
        public IList<MidiEvent> GetTrackEvents(int trackNumber)
        {
            return trackEvents[trackNumber];
        }

        /// <summary>
        /// Gets events on a specific track
        /// </summary>
        /// <param name="trackNumber">Track number</param>
        /// <returns>The list of events</returns>
        public IList<MidiEvent> this[int trackNumber] => trackEvents[trackNumber];

        /// <summary>
        /// Adds a list of MIDI events to the track and returns the combined list of events.
        /// </summary>
        /// <param name="initialEvents">The initial list of MIDI events to be added to the track.</param>
        /// <returns>The combined list of MIDI events after adding the initial events to the track.</returns>
        public IList<MidiEvent> AddTrack()
        {
            return AddTrack(null);
        }

        /// <summary>
        /// Adds a new track
        /// </summary>
        /// <param name="initialEvents">Initial events to add to the new track</param>
        /// <returns>The new track event list</returns>
        public IList<MidiEvent> AddTrack(IList<MidiEvent> initialEvents)
        {
            List<MidiEvent> events = new List<MidiEvent>();
            if (initialEvents != null)
            {
                events.AddRange(initialEvents);
            }
            trackEvents.Add(events);
            return events;
        }

        /// <summary>
        /// Removes the track at the specified index from the track events collection.
        /// </summary>
        /// <param name="track">The index of the track to be removed.</param>
        /// <remarks>
        /// This method removes the track at the specified index from the track events collection.
        /// </remarks>
        public void RemoveTrack(int track)
        {
            trackEvents.RemoveAt(track);
        }

        /// <summary>
        /// Clears all the track events from the list.
        /// </summary>
        /// <remarks>
        /// This method removes all the track events from the list, effectively clearing it.
        /// </remarks>
        public void Clear()
        {
            trackEvents.Clear();
        }

        /// <summary>
        /// The MIDI file type
        /// </summary>
        public int MidiFileType
        {
            get => midiFileType;
            set
            {
                if (midiFileType != value)
                {
                    // set MIDI file type before calling flatten or explode functions
                    midiFileType = value;

                    if (value == 0)
                    {
                        FlattenToOneTrack();
                    }
                    else
                    {
                        ExplodeToManyTracks();
                    }
                }
            }
        }

        /// <summary>
        /// Adds a MIDI event to the specified track.
        /// </summary>
        /// <param name="midiEvent">The MIDI event to be added.</param>
        /// <param name="originalTrack">The original track number of the MIDI event.</param>
        /// <exception cref="InvalidOperationException">Thrown when the MIDI file type is not recognized.</exception>
        /// <remarks>
        /// This method adds the specified MIDI event to the appropriate track based on the MIDI file type and original track number.
        /// If the MIDI file type is 0, the event is added to the first track.
        /// If the MIDI file type is not 0, the event is added to a channel track based on its command code, or to the original track if specified.
        /// </remarks>
        public void AddEvent(MidiEvent midiEvent, int originalTrack)
        {
            if (midiFileType == 0)
            {
                EnsureTracks(1);
                trackEvents[0].Add(midiEvent);
            }
            else
            {
                if(originalTrack == 0)
                {
                    // if its a channel based event, lets move it off to
                    // a channel track of its own
                    switch (midiEvent.CommandCode)
                    {
                        case MidiCommandCode.NoteOff:
                        case MidiCommandCode.NoteOn:
                        case MidiCommandCode.KeyAfterTouch:
                        case MidiCommandCode.ControlChange:
                        case MidiCommandCode.PatchChange:
                        case MidiCommandCode.ChannelAfterTouch:
                        case MidiCommandCode.PitchWheelChange:
                            EnsureTracks(midiEvent.Channel + 1);
                            trackEvents[midiEvent.Channel].Add(midiEvent);
                            break;
                        default:
                            EnsureTracks(1);
                            trackEvents[0].Add(midiEvent);
                            break;
                    }

                }
                else
                {
                    // put it on the track it was originally on
                    EnsureTracks(originalTrack + 1);
                    trackEvents[originalTrack].Add(midiEvent);
                }
            }
        }

        /// <summary>
        /// Ensures that the number of tracks in the trackEvents list is at least <paramref name="count"/> by adding empty lists if necessary.
        /// </summary>
        /// <param name="count">The minimum number of tracks to be ensured in the trackEvents list.</param>
        /// <remarks>
        /// This method iterates through the trackEvents list and adds empty lists until the count of tracks reaches the specified <paramref name="count"/>.
        /// If the count of tracks in the trackEvents list is already greater than or equal to <paramref name="count"/>, no action is taken.
        /// </remarks>
        private void EnsureTracks(int count)
        {
            for (int n = trackEvents.Count; n < count; n++)
            {
                trackEvents.Add(new List<MidiEvent>());
            }
        }

        /// <summary>
        /// Explodes the events of the first track into multiple tracks and prepares for export.
        /// </summary>
        /// <remarks>
        /// This method retrieves the events from the first track, clears the existing tracks, and adds each event to a separate track.
        /// After adding the events to the tracks, it prepares the tracks for export.
        /// </remarks>
        private void ExplodeToManyTracks()
        {
            IList<MidiEvent> originalList = trackEvents[0];
            Clear();
            foreach (MidiEvent midiEvent in originalList)
            {
                AddEvent(midiEvent, 0);
            }
            PrepareForExport();
        }

        /// <summary>
        /// Flattens the multiple tracks into a single track.
        /// </summary>
        /// <remarks>
        /// This method iterates through each track in the <paramref name="trackEvents"/> and adds all non-end track MIDI events to the first track.
        /// After flattening, it removes all tracks except the first one. If any events are added during the process, it prepares the flattened track for export.
        /// </remarks>
        private void FlattenToOneTrack()
        {
            bool eventsAdded = false;
            for (int track = 1; track < trackEvents.Count; track++)
            {
                foreach (MidiEvent midiEvent in trackEvents[track])
                {
                    if (!MidiEvent.IsEndTrack(midiEvent))
                    {
                        trackEvents[0].Add(midiEvent);
                        eventsAdded = true;
                    }
                }
            }
            for (int track = trackEvents.Count - 1; track > 0; track--)
            {
                RemoveTrack(track);
            }
            if (eventsAdded)
            {
                PrepareForExport();
            }
        }

        /// <summary>
        /// Prepares the MIDI data for export by performing the following steps:
        /// 1. Sorts each track using the MergeSort algorithm with the provided comparer.
        /// 2. Removes all but one End track event at the very end of each track.
        /// 3. Removes empty tracks and adds missing End track events if necessary.
        /// </summary>
        /// <remarks>
        /// This method modifies the original MIDI data in place.
        /// </remarks>
        public void PrepareForExport()
        {
            var comparer = new MidiEventComparer();
            // 1. sort each track
            foreach (var list in trackEvents)
            {
                MergeSort.Sort(list, comparer);

                // 2. remove all End track events except one at the very end
                int index = 0;
                while (index < list.Count - 1)
                {
                    if(MidiEvent.IsEndTrack(list[index]))
                    {
                        list.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }
            }

            int track = 0;
            // 3. remove empty tracks and add missing
            while (track < trackEvents.Count)
            {
                var list = trackEvents[track];
                if (list.Count == 0)
                {
                    RemoveTrack(track);
                }
                else
                {
                    if(list.Count == 1 && MidiEvent.IsEndTrack(list[0]))
                    {
                        RemoveTrack(track);
                    }
                    else
                    {
                        if(!MidiEvent.IsEndTrack(list[list.Count-1]))
                        {
                            list.Add(new MetaEvent(MetaEventType.EndTrack, 0, list[list.Count - 1].AbsoluteTime));
                        }
                        track++;
                    }
                }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection of track events.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection of track events.</returns>
        public IEnumerator<IList<MidiEvent>> GetEnumerator()
        {
            return trackEvents.GetEnumerator();
            
        }

        /// <summary>
        /// Gets an enumerator for the lists of track events
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return trackEvents.GetEnumerator();
        }
    }
}
