using System;
using System.Collections.Generic;
using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Allows any number of inputs to be patched to outputs
    /// Uses could include swapping left and right channels, turning mono into stereo,
    /// feeding different input sources to different soundcard outputs etc
    /// </summary>
    public class MultiplexingSampleProvider : ISampleProvider
    {
        private readonly IList<ISampleProvider> inputs;
        private readonly WaveFormat waveFormat;
        private readonly int outputChannelCount;
        private readonly int inputChannelCount;
        private readonly List<int> mappings;

        /// <summary>
        /// Creates a multiplexing sample provider, allowing re-patching of input channels to different
        /// output channels
        /// </summary>
        /// <param name="inputs">Input sample providers. Must all be of the same sample rate, but can have any number of channels</param>
        /// <param name="numberOfOutputChannels">Desired number of output channels.</param>
        public MultiplexingSampleProvider(IEnumerable<ISampleProvider> inputs, int numberOfOutputChannels)
        {
            this.inputs = new List<ISampleProvider>(inputs);
            outputChannelCount = numberOfOutputChannels;

            if (this.inputs.Count == 0)
            {
                throw new ArgumentException("You must provide at least one input");
            }
            if (numberOfOutputChannels < 1)
            {
                throw new ArgumentException("You must provide at least one output");
            }
            foreach (var input in this.inputs)
            {
                if (waveFormat == null)
                {
                    if (input.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                    {
                        throw new ArgumentException("Only 32 bit float is supported");
                    }
                    waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(input.WaveFormat.SampleRate, numberOfOutputChannels);
                }
                else
                {
                    if (input.WaveFormat.BitsPerSample != waveFormat.BitsPerSample)
                    {
                        throw new ArgumentException("All inputs must have the same bit depth");
                    }
                    if (input.WaveFormat.SampleRate != waveFormat.SampleRate)
                    {
                        throw new ArgumentException("All inputs must have the same sample rate");
                    }
                }
                inputChannelCount += input.WaveFormat.Channels;
            }

            mappings = new List<int>();
            for (int n = 0; n < outputChannelCount; n++)
            {
                mappings.Add(n % inputChannelCount);
            }
        }

        /// <summary>
        /// persistent temporary buffer to prevent creating work for garbage collector
        /// </summary>
        private float[] inputBuffer;

        /// <summary>
        /// Reads audio samples from the input channels and stores them in the buffer.
        /// </summary>
        /// <param name="buffer">The buffer to store the audio samples.</param>
        /// <param name="offset">The offset in the buffer to start storing the samples.</param>
        /// <param name="count">The number of samples to read from the input channels.</param>
        /// <returns>The total number of samples read from the input channels and stored in the buffer.</returns>
        /// <remarks>
        /// This method reads audio samples from all input channels, ensuring that they stay in sync, and stores them in the buffer.
        /// It modifies the original buffer in place.
        /// </remarks>
        public int Read(float[] buffer, int offset, int count)
        {
            int sampleFramesRequested = count / outputChannelCount;
            int inputOffset = 0;
            int sampleFramesRead = 0;
            // now we must read from all inputs, even if we don't need their data, so they stay in sync
            foreach (var input in inputs)
            {
                int samplesRequired = sampleFramesRequested * input.WaveFormat.Channels;
                inputBuffer = BufferHelpers.Ensure(inputBuffer, samplesRequired);
                int samplesRead = input.Read(inputBuffer, 0, samplesRequired);
                sampleFramesRead = Math.Max(sampleFramesRead, samplesRead / input.WaveFormat.Channels);

                for (int n = 0; n < input.WaveFormat.Channels; n++)
                {
                    int inputIndex = inputOffset + n;
                    for (int outputIndex = 0; outputIndex < outputChannelCount; outputIndex++)
                    {
                        if (mappings[outputIndex] == inputIndex)
                        {
                            int inputBufferOffset = n;
                            int outputBufferOffset = offset + outputIndex;
                            int sample = 0;
                            while (sample < sampleFramesRequested && inputBufferOffset < samplesRead)
                            {
                                buffer[outputBufferOffset] = inputBuffer[inputBufferOffset];
                                outputBufferOffset += outputChannelCount;
                                inputBufferOffset += input.WaveFormat.Channels;
                                sample++;
                            }
                            // clear the end
                            while (sample < sampleFramesRequested)
                            {
                                buffer[outputBufferOffset] = 0;
                                outputBufferOffset += outputChannelCount;
                                sample++;
                            }
                        }
                    }
                }
                inputOffset += input.WaveFormat.Channels;
            }

            return sampleFramesRead * outputChannelCount;
        }

        /// <summary>
        /// The output WaveFormat for this SampleProvider
        /// </summary>
        public WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Connects the input channel to the output channel.
        /// </summary>
        /// <param name="inputChannel">The input channel to be connected.</param>
        /// <param name="outputChannel">The output channel to be connected.</param>
        /// <exception cref="ArgumentException">Thrown when the input channel or output channel is invalid.</exception>
        /// <remarks>
        /// This method connects the specified input channel to the specified output channel by updating the mappings array.
        /// It performs validation to ensure that the input and output channels are within the valid range.
        /// </remarks>
        public void ConnectInputToOutput(int inputChannel, int outputChannel)
        {
            if (inputChannel < 0 || inputChannel >= InputChannelCount)
            {
                throw new ArgumentException("Invalid input channel");
            }
            if (outputChannel < 0 || outputChannel >= OutputChannelCount)
            {
                throw new ArgumentException("Invalid output channel");
            }
            mappings[outputChannel] = inputChannel;
        }

        /// <summary>
        /// The number of input channels. Note that this is not the same as the number of input wave providers. If you pass in
        /// one stereo and one mono input provider, the number of input channels is three.
        /// </summary>
        public int InputChannelCount => inputChannelCount;

        /// <summary>
        /// The number of output channels, as specified in the constructor.
        /// </summary>
        public int OutputChannelCount => outputChannelCount;
    }
}
