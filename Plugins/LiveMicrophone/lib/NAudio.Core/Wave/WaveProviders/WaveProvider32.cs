using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Wave
{
    /// <summary>
    /// Base class for creating a 32 bit floating point wave provider
    /// Can also be used as a base class for an ISampleProvider that can 
    /// be plugged straight into anything requiring an IWaveProvider
    /// </summary>
    public abstract class WaveProvider32 : IWaveProvider, ISampleProvider
    {
        private WaveFormat waveFormat;

        /// <summary>
        /// Initializes a new instance of the WaveProvider32 class 
        /// defaulting to 44.1kHz mono
        /// </summary>
        public WaveProvider32()
            : this(44100, 1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the WaveProvider32 class with the specified
        /// sample rate and number of channels
        /// </summary>
        public WaveProvider32(int sampleRate, int channels)
        {
            SetWaveFormat(sampleRate, channels);
        }

        /// <summary>
        /// Sets the wave format using the specified sample rate and number of channels.
        /// </summary>
        /// <param name="sampleRate">The sample rate for the wave format.</param>
        /// <param name="channels">The number of channels for the wave format.</param>
        public void SetWaveFormat(int sampleRate, int channels)
        {
            this.waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        }

        /// <summary>
        /// Reads a specified number of samples from the buffer starting at the given offset and returns the number of samples read.
        /// </summary>
        /// <param name="buffer">The buffer containing the samples to be read.</param>
        /// <param name="offset">The offset within the buffer at which to start reading.</param>
        /// <param name="sampleCount">The number of samples to read from the buffer.</param>
        /// <returns>The number of samples read from the buffer.</returns>
        /// <remarks>
        /// This method reads a specified number of samples from the buffer starting at the given offset.
        /// It returns the actual number of samples read, which may be less than the requested sample count if the end of the buffer is reached.
        /// </remarks>
        public int Read(byte[] buffer, int offset, int count)
        {
            WaveBuffer waveBuffer = new WaveBuffer(buffer);
            int samplesRequired = count / 4;
            int samplesRead = Read(waveBuffer.FloatBuffer, offset / 4, samplesRequired);
            return samplesRead * 4;
        }

        /// <summary>
        /// Method to override in derived classes
        /// Supply the requested number of samples into the buffer
        /// </summary>
        public abstract int Read(float[] buffer, int offset, int sampleCount);

        /// <summary>
        /// The Wave Format
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }
    }
}
