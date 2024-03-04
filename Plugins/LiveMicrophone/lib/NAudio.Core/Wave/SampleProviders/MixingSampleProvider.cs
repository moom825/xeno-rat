using System;
using System.Collections.Generic;
using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// A sample provider mixer, allowing inputs to be added and removed
    /// </summary>
    public class MixingSampleProvider : ISampleProvider
    {
        private readonly List<ISampleProvider> sources;
        private float[] sourceBuffer;
        private const int MaxInputs = 1024; // protect ourselves against doing something silly

        /// <summary>
        /// Creates a new MixingSampleProvider, with no inputs, but a specified WaveFormat
        /// </summary>
        /// <param name="waveFormat">The WaveFormat of this mixer. All inputs must be in this format</param>
        public MixingSampleProvider(WaveFormat waveFormat)
        {
            if (waveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                throw new ArgumentException("Mixer wave format must be IEEE float");
            }
            sources = new List<ISampleProvider>();
            WaveFormat = waveFormat;
        }

        /// <summary>
        /// Creates a new MixingSampleProvider, based on the given inputs
        /// </summary>
        /// <param name="sources">Mixer inputs - must all have the same waveformat, and must
        /// all be of the same WaveFormat. There must be at least one input</param>
        public MixingSampleProvider(IEnumerable<ISampleProvider> sources)
        {
            this.sources = new List<ISampleProvider>();
            foreach (var source in sources)
            {
                AddMixerInput(source);
            }
            if (this.sources.Count == 0)
            {
                throw new ArgumentException("Must provide at least one input in this constructor");
            }
        }

        /// <summary>
        /// Returns the mixer inputs (read-only - use AddMixerInput to add an input
        /// </summary>
        public IEnumerable<ISampleProvider> MixerInputs => sources;

        /// <summary>
        /// When set to true, the Read method always returns the number
        /// of samples requested, even if there are no inputs, or if the
        /// current inputs reach their end. Setting this to true effectively
        /// makes this a never-ending sample provider, so take care if you plan
        /// to write it out to a file.
        /// </summary>
        public bool ReadFully { get; set; }

        /// <summary>
        /// Adds a new input to the mixer.
        /// </summary>
        /// <param name="mixerInput">The input to be added to the mixer.</param>
        /// <exception cref="InvalidOperationException">Thrown when there are already too many mixer inputs.</exception>
        /// <exception cref="ArgumentException">Thrown when the input's WaveFormat does not match the mixer's WaveFormat.</exception>
        /// <remarks>
        /// This method adds a new input to the mixer. It checks if the maximum number of inputs has been reached and throws an InvalidOperationException if so.
        /// It also checks if the input's WaveFormat matches the mixer's WaveFormat and throws an ArgumentException if not.
        /// </remarks>
        public void AddMixerInput(IWaveProvider mixerInput)
        {
            AddMixerInput(SampleProviderConverters.ConvertWaveProviderIntoSampleProvider(mixerInput));
        }

        /// <summary>
        /// Adds a new mixer input
        /// </summary>
        /// <param name="mixerInput">Mixer input</param>
        public void AddMixerInput(ISampleProvider mixerInput)
        {
            // we'll just call the lock around add since we are protecting against an AddMixerInput at
            // the same time as a Read, rather than two AddMixerInput calls at the same time
            lock (sources)
            {
                if (sources.Count >= MaxInputs)
                {
                    throw new InvalidOperationException("Too many mixer inputs");
                }
                sources.Add(mixerInput);
            }
            if (WaveFormat == null)
            {
                WaveFormat = mixerInput.WaveFormat;
            }
            else
            {
                if (WaveFormat.SampleRate != mixerInput.WaveFormat.SampleRate ||
                    WaveFormat.Channels != mixerInput.WaveFormat.Channels)
                {
                    throw new ArgumentException("All mixer inputs must have the same WaveFormat");
                }
            }
        }

        /// <summary>
        /// Raised when a mixer input has been removed because it has ended
        /// </summary>
        public event EventHandler<SampleProviderEventArgs> MixerInputEnded;

        /// <summary>
        /// Removes the specified mixer input from the list of sources.
        /// </summary>
        /// <param name="mixerInput">The input to be removed from the list of sources.</param>
        public void RemoveMixerInput(ISampleProvider mixerInput)
        {
            lock (sources)
            {
                sources.Remove(mixerInput);
            }
        }

        /// <summary>
        /// Removes all mixer inputs.
        /// </summary>
        /// <remarks>
        /// This method removes all the inputs from the mixer by clearing the sources list after obtaining a lock on it.
        /// </remarks>
        public void RemoveAllMixerInputs()
        {
            lock (sources)
            {
                sources.Clear();
            }
        }

        /// <summary>
        /// The output WaveFormat of this sample provider
        /// </summary>
        public WaveFormat WaveFormat { get; private set; }

        /// <summary>
        /// Reads audio samples from the sources and mixes them into the provided buffer.
        /// </summary>
        /// <param name="buffer">The buffer to mix the audio samples into.</param>
        /// <param name="offset">The offset in the buffer at which to start mixing the samples.</param>
        /// <param name="count">The number of samples to mix into the buffer.</param>
        /// <returns>The number of output samples mixed into the buffer.</returns>
        /// <remarks>
        /// This method reads audio samples from the sources, mixes them into the provided buffer starting at the specified offset, and returns the number of output samples mixed into the buffer.
        /// If the ReadFully flag is set and the output samples are less than the count, the remaining space in the buffer is filled with zeros.
        /// </remarks>
        public int Read(float[] buffer, int offset, int count)
        {
            int outputSamples = 0;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, count);
            lock (sources)
            {
                int index = sources.Count - 1;
                while (index >= 0)
                {
                    var source = sources[index];
                    int samplesRead = source.Read(sourceBuffer, 0, count);
                    int outIndex = offset;
                    for (int n = 0; n < samplesRead; n++)
                    {
                        if (n >= outputSamples)
                        {
                            buffer[outIndex++] = sourceBuffer[n];
                        }
                        else
                        {
                            buffer[outIndex++] += sourceBuffer[n];
                        }
                    }
                    outputSamples = Math.Max(samplesRead, outputSamples);
                    if (samplesRead < count)
                    {
                        MixerInputEnded?.Invoke(this, new SampleProviderEventArgs(source));
                        sources.RemoveAt(index);
                    }
                    index--;
                }
            }
            // optionally ensure we return a full buffer
            if (ReadFully && outputSamples < count)
            {
                int outputIndex = offset + outputSamples;
                while (outputIndex < offset + count)
                {
                    buffer[outputIndex++] = 0;
                }
                outputSamples = count;
            }
            return outputSamples;
        }
    }

    /// <summary>
    /// SampleProvider event args
    /// </summary>
    public class SampleProviderEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs a new SampleProviderEventArgs
        /// </summary>
        public SampleProviderEventArgs(ISampleProvider sampleProvider)
        {
            SampleProvider = sampleProvider;
        }

        /// <summary>
        /// The Sample Provider
        /// </summary>
        public ISampleProvider SampleProvider { get; private set; }
    }
}
