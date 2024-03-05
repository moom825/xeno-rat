using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Wave
{
    /// <summary>
    /// WaveProvider that can mix together multiple 32 bit floating point input provider
    /// All channels must have the same number of inputs and same sample rate
    /// n.b. Work in Progress - not tested yet
    /// </summary>
    public class MixingWaveProvider32 : IWaveProvider
    {
        private List<IWaveProvider> inputs;
        private WaveFormat waveFormat;
        private int bytesPerSample;

        /// <summary>
        /// Creates a new MixingWaveProvider32
        /// </summary>
        public MixingWaveProvider32()
        {
            this.waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            this.bytesPerSample = 4;
            this.inputs = new List<IWaveProvider>();
        }

        /// <summary>
        /// Creates a new 32 bit MixingWaveProvider32
        /// </summary>
        /// <param name="inputs">inputs - must all have the same format.</param>
        /// <exception cref="ArgumentException">Thrown if the input streams are not 32 bit floating point,
        /// or if they have different formats to each other</exception>
        public MixingWaveProvider32(IEnumerable<IWaveProvider> inputs)
            : this()
        {
            foreach (var input in inputs)
            {
                AddInputStream(input);
            }
        }

        /// <summary>
        /// Adds an input audio stream to the mixer.
        /// </summary>
        /// <param name="waveProvider">The input audio stream to be added.</param>
        /// <exception cref="ArgumentException">Thrown when the input audio stream does not match the required format.</exception>
        /// <remarks>
        /// This method adds the input audio stream <paramref name="waveProvider"/> to the mixer.
        /// It checks if the format of the input stream matches the required format (IEEE floating point with 32 bits per sample).
        /// If it is the first input, it sets the format of the mixer to match the input stream.
        /// If it is not the first input, it checks if the format of the input stream matches the format of the other inputs already added to the mixer.
        /// </remarks>
        public void AddInputStream(IWaveProvider waveProvider)
        {
            if (waveProvider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                throw new ArgumentException("Must be IEEE floating point", "waveProvider.WaveFormat");
            if (waveProvider.WaveFormat.BitsPerSample != 32)
                throw new ArgumentException("Only 32 bit audio currently supported", "waveProvider.WaveFormat");

            if (inputs.Count == 0)
            {
                // first one - set the format
                int sampleRate = waveProvider.WaveFormat.SampleRate;
                int channels = waveProvider.WaveFormat.Channels;
                this.waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            }
            else
            {
                if (!waveProvider.WaveFormat.Equals(waveFormat))
                    throw new ArgumentException("All incoming channels must have the same format", "waveProvider.WaveFormat");
            }

            lock (inputs)
            {
                this.inputs.Add(waveProvider);
            }
        }

        /// <summary>
        /// Removes the specified input stream from the list of input streams.
        /// </summary>
        /// <param name="waveProvider">The input stream to be removed.</param>
        /// <remarks>
        /// This method removes the specified <paramref name="waveProvider"/> from the list of input streams. It locks the <see cref="inputs"/> list to ensure thread safety while removing the stream.
        /// </remarks>
        public void RemoveInputStream(IWaveProvider waveProvider)
        {
            lock (inputs)
            {
                this.inputs.Remove(waveProvider);
            }
        }

        /// <summary>
        /// The number of inputs to this mixer
        /// </summary>
        public int InputCount
        {
            get { return this.inputs.Count; }
        }

        /// <summary>
        /// Reads audio data from the input streams, sums the channels, and stores the result in the provided buffer.
        /// </summary>
        /// <param name="buffer">The buffer to store the summed audio data.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data.</param>
        /// <param name="count">The number of bytes to read from the input streams.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="count"/> is not a whole number of samples.</exception>
        /// <returns>The number of bytes read from the input streams, which may be less than the requested <paramref name="count"/>.</returns>
        /// <remarks>
        /// This method first checks if <paramref name="count"/> is a whole number of samples and throws an <see cref="ArgumentException"/> if not.
        /// It then clears the specified portion of the buffer, reads data from the input streams, sums the channels, and stores the result in the buffer.
        /// The method returns the actual number of bytes read from the input streams, which may be less than the requested count.
        /// </remarks>
        public int Read(byte[] buffer, int offset, int count)
        {
            if (count % bytesPerSample != 0)
                throw new ArgumentException("Must read an whole number of samples", "count");

            // blank the buffer
            Array.Clear(buffer, offset, count);
            int bytesRead = 0;

            // sum the channels in
            byte[] readBuffer = new byte[count];
            lock (inputs)
            {
                foreach (var input in inputs)
                {
                    int readFromThisStream = input.Read(readBuffer, 0, count);
                    // don't worry if input stream returns less than we requested - may indicate we have got to the end
                    bytesRead = Math.Max(bytesRead, readFromThisStream);
                    if (readFromThisStream > 0)
                    {
                        Sum32BitAudio(buffer, offset, readBuffer, readFromThisStream);
                    }
                }
            }
            return bytesRead;
        }

        /// <summary>
        /// Sums 32-bit audio samples from the source buffer to the destination buffer.
        /// </summary>
        /// <param name="destBuffer">The destination buffer to which the audio samples will be added.</param>
        /// <param name="offset">The offset within the destination buffer to start adding the samples.</param>
        /// <param name="sourceBuffer">The source buffer containing the audio samples to be added.</param>
        /// <param name="bytesRead">The number of bytes read from the source buffer.</param>
        /// <remarks>
        /// This method sums 32-bit audio samples from the source buffer to the destination buffer. It uses unsafe code to work with pointers and perform the addition operation directly on the memory.
        /// The method calculates the number of samples read based on the number of bytes read, assuming each sample is 4 bytes (32 bits). It then iterates through the samples and adds each sample from the source buffer to the corresponding sample in the destination buffer.
        /// </remarks>
        static unsafe void Sum32BitAudio(byte[] destBuffer, int offset, byte[] sourceBuffer, int bytesRead)
        {
            fixed (byte* pDestBuffer = &destBuffer[offset],
                      pSourceBuffer = &sourceBuffer[0])
            {
                float* pfDestBuffer = (float*)pDestBuffer;
                float* pfReadBuffer = (float*)pSourceBuffer;
                int samplesRead = bytesRead / 4;
                for (int n = 0; n < samplesRead; n++)
                {
                    pfDestBuffer[n] += pfReadBuffer[n];
                }
            }
        }

        /// <summary>
        /// <see cref="WaveStream.WaveFormat"/>
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return this.waveFormat; }
        }
    }
}
