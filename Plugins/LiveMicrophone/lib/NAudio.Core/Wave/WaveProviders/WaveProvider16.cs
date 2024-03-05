using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Wave
{
    /// <summary>
    /// Base class for creating a 16 bit wave provider
    /// </summary>
    public abstract class WaveProvider16 : IWaveProvider
    {
        private WaveFormat waveFormat;

        /// <summary>
        /// Initializes a new instance of the WaveProvider16 class 
        /// defaulting to 44.1kHz mono
        /// </summary>
        public WaveProvider16()
            : this(44100, 1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the WaveProvider16 class with the specified
        /// sample rate and number of channels
        /// </summary>
        public WaveProvider16(int sampleRate, int channels)
        {
            SetWaveFormat(sampleRate, channels);
        }

        /// <summary>
        /// Sets the wave format using the specified sample rate and number of channels.
        /// </summary>
        /// <param name="sampleRate">The sample rate for the wave format.</param>
        /// <param name="channels">The number of channels for the wave format.</param>
        /// <remarks>
        /// This method sets the wave format using the provided sample rate and number of channels.
        /// </remarks>
        public void SetWaveFormat(int sampleRate, int channels)
        {
            this.waveFormat = new WaveFormat(sampleRate, 16, channels);
        }

        /// <summary>
        /// Reads a specified number of samples from the buffer starting at the specified offset.
        /// </summary>
        /// <param name="buffer">The buffer containing the samples to be read.</param>
        /// <param name="offset">The zero-based offset in the buffer at which to begin reading samples.</param>
        /// <param name="sampleCount">The number of samples to read from the buffer.</param>
        /// <returns>The number of samples read from the buffer.</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            WaveBuffer waveBuffer = new WaveBuffer(buffer);
            int samplesRequired = count / 2;
            int samplesRead = Read(waveBuffer.ShortBuffer, offset / 2, samplesRequired);
            return samplesRead * 2;
        }

        /// <summary>
        /// Method to override in derived classes
        /// Supply the requested number of samples into the buffer
        /// </summary>
        public abstract int Read(short[] buffer, int offset, int sampleCount);

        /// <summary>
        /// The Wave Format
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }
    }
}
