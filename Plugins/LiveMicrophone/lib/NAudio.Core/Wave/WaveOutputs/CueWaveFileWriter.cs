using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NAudio.Wave
{
    /// <summary>
    /// A wave file writer that adds cue support
    /// </summary>
    public class CueWaveFileWriter : WaveFileWriter
    {
        private CueList cues = null;

        /// <summary>
        /// Writes a wave file, including a cues chunk
        /// </summary>
        public CueWaveFileWriter(string fileName, WaveFormat waveFormat)
            : base (fileName, waveFormat)
        {
        }

        /// <summary>
        /// Adds a cue at the specified position with the given label.
        /// </summary>
        /// <param name="position">The position at which the cue should be added.</param>
        /// <param name="label">The label for the cue.</param>
        /// <remarks>
        /// If the cues list is null, a new CueList is created.
        /// The method then adds a new cue with the specified position and label to the cues list.
        /// </remarks>
        public void AddCue(int position, string label)
        {
            if (cues == null)
            {
                cues = new CueList();
            }
            cues.Add(new Cue(position, label));
        }

        /// <summary>
        /// Writes the cue chunks to the end of the stream.
        /// </summary>
        /// <param name="w">The BinaryWriter used to write the cue chunks.</param>
        /// <remarks>
        /// This method writes the cue chunks to the end of the stream. If the cues are not null, it gets the RIFF chunks and their size, then writes them to the end of the stream using the BinaryWriter.
        /// It also ensures that the stream is aligned by checking if the length is odd and adding a byte if necessary. After writing the cue chunks, it updates the size information at the beginning of the stream.
        /// </remarks>
        private void WriteCues(BinaryWriter w)
        {
            // write the cue chunks to the end of the stream
            if (cues != null)
            {
                byte[] cueChunks = cues.GetRiffChunks();
                int cueChunksSize = cueChunks.Length;
                w.Seek(0, SeekOrigin.End);
                
                if (w.BaseStream.Length % 2 == 1)
                {
                    w.Write((Byte)0x00);
                }
                
                w.Write(cues.GetRiffChunks(), 0, cueChunksSize);
                w.Seek(4, SeekOrigin.Begin);
                w.Write((int)(w.BaseStream.Length - 8));
            }
        }

        /// <summary>
        /// Updates the header and writes cues using the provided BinaryWriter.
        /// </summary>
        /// <param name="writer">The BinaryWriter used to write the cues.</param>
        /// <remarks>
        /// This method updates the header by calling the base class's UpdateHeader method and then writes cues using the provided BinaryWriter.
        /// </remarks>
        protected override void UpdateHeader(BinaryWriter writer)
        {
            base.UpdateHeader(writer);
            WriteCues(writer);
        }
    }
}

