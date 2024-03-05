using System;
using System.IO;
using System.Text;

namespace NAudio.Midi
{
    /// <summary>
    /// Represents a MIDI patch change event
    /// </summary>
    public class PatchChangeEvent : MidiEvent
    {
        private byte patch;

        /// <summary>
        /// Retrieves the patch name corresponding to the given patch number.
        /// </summary>
        /// <param name="patchNumber">The number representing the patch.</param>
        /// <returns>The name of the patch corresponding to the given <paramref name="patchNumber"/>.</returns>
        public static string GetPatchName(int patchNumber)
        {
            return patchNames[patchNumber];
        }

        // TODO: localize
        private static readonly string[] patchNames = new string[] 
        {
            "Acoustic Grand","Bright Acoustic","Electric Grand","Honky-Tonk","Electric Piano 1","Electric Piano 2","Harpsichord","Clav",
            "Celesta","Glockenspiel","Music Box","Vibraphone","Marimba","Xylophone","Tubular Bells","Dulcimer",
            "Drawbar Organ","Percussive Organ","Rock Organ","Church Organ","Reed Organ","Accoridan","Harmonica","Tango Accordian",
            "Acoustic Guitar(nylon)","Acoustic Guitar(steel)","Electric Guitar(jazz)","Electric Guitar(clean)","Electric Guitar(muted)","Overdriven Guitar","Distortion Guitar","Guitar Harmonics",
            "Acoustic Bass","Electric Bass(finger)","Electric Bass(pick)","Fretless Bass","Slap Bass 1","Slap Bass 2","Synth Bass 1","Synth Bass 2",
            "Violin","Viola","Cello","Contrabass","Tremolo Strings","Pizzicato Strings","Orchestral Strings","Timpani",
            "String Ensemble 1","String Ensemble 2","SynthStrings 1","SynthStrings 2","Choir Aahs","Voice Oohs","Synth Voice","Orchestra Hit",
            "Trumpet","Trombone","Tuba","Muted Trumpet","French Horn","Brass Section","SynthBrass 1","SynthBrass 2",
            "Soprano Sax","Alto Sax","Tenor Sax","Baritone Sax","Oboe","English Horn","Bassoon","Clarinet",
            "Piccolo","Flute","Recorder","Pan Flute","Blown Bottle","Skakuhachi","Whistle","Ocarina",
            "Lead 1 (square)","Lead 2 (sawtooth)","Lead 3 (calliope)","Lead 4 (chiff)","Lead 5 (charang)","Lead 6 (voice)","Lead 7 (fifths)","Lead 8 (bass+lead)",
            "Pad 1 (new age)","Pad 2 (warm)","Pad 3 (polysynth)","Pad 4 (choir)","Pad 5 (bowed)","Pad 6 (metallic)","Pad 7 (halo)","Pad 8 (sweep)",
            "FX 1 (rain)","FX 2 (soundtrack)","FX 3 (crystal)","FX 4 (atmosphere)","FX 5 (brightness)","FX 6 (goblins)","FX 7 (echoes)","FX 8 (sci-fi)",
            "Sitar","Banjo","Shamisen","Koto","Kalimba","Bagpipe","Fiddle","Shanai",
            "Tinkle Bell","Agogo","Steel Drums","Woodblock","Taiko Drum","Melodic Tom","Synth Drum","Reverse Cymbal",
            "Guitar Fret Noise","Breath Noise","Seashore","Bird Tweet","Telephone Ring","Helicopter","Applause","Gunshot" 
        };

        /// <summary>
        /// Reads a new patch change event from a MIDI stream
        /// </summary>
        /// <param name="br">Binary reader for the MIDI stream</param>
        public PatchChangeEvent(BinaryReader br)
        {
            patch = br.ReadByte();
            if ((patch & 0x80) != 0)
            {
                // TODO: might be a follow-on
                throw new FormatException("Invalid patch");
            }
        }

        /// <summary>
        /// Creates a new patch change event
        /// </summary>
        /// <param name="absoluteTime">Time of the event</param>
        /// <param name="channel">Channel number</param>
        /// <param name="patchNumber">Patch number</param>
        public PatchChangeEvent(long absoluteTime, int channel, int patchNumber)
            : base(absoluteTime, channel, MidiCommandCode.PatchChange)
        {
            this.Patch = patchNumber;
        }

        /// <summary>
        /// The Patch Number
        /// </summary>
        public int Patch
        {
            get
            {
                return patch;
            }
            set
            {
                if (value < 0 || value > 127)
                {
                    throw new ArgumentOutOfRangeException("value", "Patch number must be in the range 0-127");
                }
                patch = (byte)value;
            }
        }

        /// <summary>
        /// Returns a string that represents the current object, including the patch name.
        /// </summary>
        /// <returns>A string that combines the base object's string representation with the patch name.</returns>
        public override string ToString()
        {
            return String.Format("{0} {1}",
                base.ToString(),
                GetPatchName(this.patch));
        }

        /// <summary>
        /// Adds the patch value shifted by 8 bits to the result of the base class's short message.
        /// </summary>
        /// <returns>The sum of the base class's short message and the patch value shifted by 8 bits.</returns>
        public override int GetAsShortMessage()
        {
            return base.GetAsShortMessage() + (this.patch << 8);
        }

        /// <summary>
        /// Exports the object's data to a binary writer, including the patch value.
        /// </summary>
        /// <param name="absoluteTime">The absolute time reference for the export operation.</param>
        /// <param name="writer">The binary writer to which the data is exported.</param>
        /// <remarks>
        /// This method exports the object's data to the specified binary writer, including the patch value. It first calls the base class's Export method to export any base class data, and then writes the patch value to the binary writer.
        /// </remarks>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            writer.Write(patch);
        }
    }
}