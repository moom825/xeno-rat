using System;

namespace NAudio.Midi
{
    /// <summary>
    /// Represents a MIDI message
    /// </summary>
    public class MidiMessage
    {
        private int rawData;

        /// <summary>
        /// Creates a new MIDI message
        /// </summary>
        /// <param name="status">Status</param>
        /// <param name="data1">Data parameter 1</param>
        /// <param name="data2">Data parameter 2</param>
        public MidiMessage(int status, int data1, int data2)
        {
            rawData = status + (data1 << 8) + (data2 << 16);
        }

        /// <summary>
        /// Creates a new MIDI message from a raw message
        /// </summary>
        /// <param name="rawData">A packed MIDI message from an MMIO function</param>
        public MidiMessage(int rawData)
        {
            this.rawData = rawData;
        }

        /// <summary>
        /// Starts a note with the specified parameters and returns the corresponding MIDI message.
        /// </summary>
        /// <param name="note">The MIDI note number to be played.</param>
        /// <param name="volume">The volume of the note to be played.</param>
        /// <param name="channel">The MIDI channel on which the note should be played.</param>
        /// <exception cref="ArgumentException">Thrown when the note, volume, or channel parameters are invalid.</exception>
        /// <returns>A MIDI message representing the start of the specified note with the given volume on the specified channel.</returns>
        /// <remarks>
        /// This method validates the input parameters using the ValidateNoteParameters method to ensure that the note, volume, and channel are within valid ranges.
        /// It then creates and returns a new MIDI message using the specified parameters, where the command code is NoteOn for the specified channel, followed by the note number and volume.
        /// </remarks>
        public static MidiMessage StartNote(int note, int volume, int channel)
        {
            ValidateNoteParameters(note, volume, channel);
            return new MidiMessage((int)MidiCommandCode.NoteOn + channel - 1, note, volume);
        }

        /// <summary>
        /// Validates the note, volume, and channel parameters for MIDI messages.
        /// </summary>
        /// <param name="note">The MIDI note number to be validated.</param>
        /// <param name="volume">The velocity value to be validated.</param>
        /// <param name="channel">The MIDI channel number to be validated.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the note number is not in the range 0-127 or when the volume is not in the range 0-127.</exception>
        /// <remarks>
        /// This method validates the input parameters for MIDI messages. It checks if the note number is within the range 0-127 and if the volume value is within the range 0-127.
        /// If any of the parameters are invalid, an <see cref="ArgumentOutOfRangeException"/> is thrown with a descriptive message indicating the specific parameter that caused the exception.
        /// </remarks>
        private static void ValidateNoteParameters(int note, int volume, int channel)
        {
            ValidateChannel(channel);
            if (note < 0 || note > 127)
            {
                throw new ArgumentOutOfRangeException("note", "Note number must be in the range 0-127");
            }
            if (volume < 0 || volume > 127)
            {
                throw new ArgumentOutOfRangeException("volume", "Velocity must be in the range 0-127");
            }
        }

        /// <summary>
        /// Validates the channel number to ensure it falls within the range of 1 to 16.
        /// </summary>
        /// <param name="channel">The channel number to be validated.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the channel number is not within the range of 1 to 16.</exception>
        /// <remarks>
        /// This method validates the input channel number to ensure it falls within the acceptable range of 1 to 16.
        /// If the channel number is not within the specified range, an <see cref="ArgumentOutOfRangeException"/> is thrown with a descriptive message indicating the invalid channel number.
        /// </remarks>
        private static void ValidateChannel(int channel)
        {
            if ((channel < 1) || (channel > 16))
            {
                throw new ArgumentOutOfRangeException("channel", channel,
                    String.Format("Channel must be 1-16 (Got {0})", channel));
            }
        }

        /// <summary>
        /// Stops the specified note on the given channel with the specified volume.
        /// </summary>
        /// <param name="note">The MIDI note number to stop.</param>
        /// <param name="volume">The volume at which to stop the note.</param>
        /// <param name="channel">The MIDI channel on which to stop the note.</param>
        /// <exception cref="ArgumentException">Thrown when the note, volume, or channel is not within the valid range.</exception>
        /// <returns>A MIDI message to stop the specified note on the given channel with the specified volume.</returns>
        /// <remarks>
        /// This method validates the input parameters for note, volume, and channel using the ValidateNoteParameters method.
        /// It then creates and returns a MIDI message to stop the specified note on the given channel with the specified volume.
        /// </remarks>
        public static MidiMessage StopNote(int note, int volume, int channel)
        {
            ValidateNoteParameters(note, volume, channel);
            return new MidiMessage((int)MidiCommandCode.NoteOff + channel - 1, note, volume);
        }

        /// <summary>
        /// Changes the patch for the specified MIDI channel and returns the corresponding MIDI message.
        /// </summary>
        /// <param name="patch">The patch number to be changed.</param>
        /// <param name="channel">The MIDI channel on which the patch change should occur.</param>
        /// <exception cref="ArgumentException">Thrown when the provided MIDI channel is not valid.</exception>
        /// <returns>A MIDI message representing the patch change on the specified channel.</returns>
        /// <remarks>
        /// This method validates the provided MIDI channel using the ValidateChannel method to ensure it is within the valid range.
        /// It then creates and returns a new MIDI message with the appropriate command code, channel, and patch number.
        /// </remarks>
        public static MidiMessage ChangePatch(int patch, int channel)
        {
            ValidateChannel(channel);
            return new MidiMessage((int)MidiCommandCode.PatchChange + channel - 1, patch, 0);
        }

        /// <summary>
        /// Changes the control value for a specific MIDI controller on the specified channel and returns the corresponding MIDI message.
        /// </summary>
        /// <param name="controller">The MIDI controller number to be changed.</param>
        /// <param name="value">The new value for the MIDI controller.</param>
        /// <param name="channel">The MIDI channel on which the control change should occur.</param>
        /// <exception cref="ArgumentException">Thrown when the specified MIDI channel is invalid.</exception>
        /// <returns>A MIDI message representing the control change with the specified parameters.</returns>
        /// <remarks>
        /// This method validates the specified MIDI channel and creates a MIDI message for the control change using the provided controller number and value.
        /// The MIDI message is then returned for further processing or transmission.
        /// </remarks>
        public static MidiMessage ChangeControl(int controller, int value, int channel)
        {
            ValidateChannel(channel);
            return new MidiMessage((int)MidiCommandCode.ControlChange + channel - 1, controller, value);
        }

        /// <summary>
        /// Returns the raw MIDI message data
        /// </summary>
        public int RawData
        {
            get
            {
                return rawData;
            }
        }
    }
}
