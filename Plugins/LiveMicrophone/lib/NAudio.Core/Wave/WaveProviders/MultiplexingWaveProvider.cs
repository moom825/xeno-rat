using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Utils;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Allows any number of inputs to be patched to outputs
    /// Uses could include swapping left and right channels, turning mono into stereo,
    /// feeding different input sources to different soundcard outputs etc
    /// </summary>
    public class MultiplexingWaveProvider : IWaveProvider
    {
        private readonly IList<IWaveProvider> inputs;
        private readonly int outputChannelCount;
        private readonly int inputChannelCount;
        private readonly List<int> mappings;
        private readonly int bytesPerSample;

        /// <summary>
        /// Creates a multiplexing wave provider, allowing re-patching of input channels to different
        /// output channels. Number of outputs is equal to total number of channels in inputs
        /// </summary>
        /// <param name="inputs">Input wave providers. Must all be of the same format, but can have any number of channels</param>
        public MultiplexingWaveProvider(IEnumerable<IWaveProvider> inputs) : this(inputs, -1)
        {
            
        }

        /// <summary>
        /// Creates a multiplexing wave provider, allowing re-patching of input channels to different
        /// output channels
        /// </summary>
        /// <param name="inputs">Input wave providers. Must all be of the same format, but can have any number of channels</param>
        /// <param name="numberOfOutputChannels">Desired number of output channels. (-1 means use total number of input channels)</param>
        public MultiplexingWaveProvider(IEnumerable<IWaveProvider> inputs, int numberOfOutputChannels)
        {
            this.inputs = new List<IWaveProvider>(inputs);
            
            outputChannelCount = numberOfOutputChannels == -1 ? this.inputs.Sum(i => i.WaveFormat.Channels)  : numberOfOutputChannels;

            if (this.inputs.Count == 0)
            {
                throw new ArgumentException("You must provide at least one input");
            }
            if (outputChannelCount < 1)
            {
                throw new ArgumentException("You must provide at least one output");
            }
            foreach (var input in this.inputs)
            {
                if (WaveFormat == null)
                {
                    if (input.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
                    {
                        WaveFormat = new WaveFormat(input.WaveFormat.SampleRate, input.WaveFormat.BitsPerSample, outputChannelCount);
                    }
                    else if (input.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                    {
                        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(input.WaveFormat.SampleRate, outputChannelCount);
                    }
                    else
                    {
                        throw new ArgumentException("Only PCM and 32 bit float are supported");
                    }
                }
                else
                {
                    if (input.WaveFormat.BitsPerSample != WaveFormat.BitsPerSample)
                    {
                        throw new ArgumentException("All inputs must have the same bit depth");
                    }
                    if (input.WaveFormat.SampleRate != WaveFormat.SampleRate)
                    {
                        throw new ArgumentException("All inputs must have the same sample rate");
                    }
                }
                inputChannelCount += input.WaveFormat.Channels;
            }
            bytesPerSample = WaveFormat.BitsPerSample / 8;

            mappings = new List<int>();
            for (int n = 0; n < outputChannelCount; n++)
            {
                mappings.Add(n % inputChannelCount);
            }
        }

        /// <summary>
        /// persistent temporary buffer to prevent creating work for garbage collector
        /// </summary>
        private byte[] inputBuffer;

        /// <summary>
        /// Reads audio data from the input buffer and writes it to the output buffer.
        /// </summary>
        /// <param name="buffer">The output buffer to write the audio data to.</param>
        /// <param name="offset">The offset in the output buffer to start writing the data.</param>
        /// <param name="count">The number of bytes to read from the input buffer and write to the output buffer.</param>
        /// <returns>The number of sample frames read and written to the output buffer.</returns>
        /// <remarks>
        /// This method reads audio data from all input sources, even if the data is not needed, to keep them in sync.
        /// It then processes the input data and writes it to the output buffer based on the specified offset and count.
        /// The method modifies the output buffer in place.
        /// </remarks>
        public int Read(byte[] buffer, int offset, int count)
        {
            int outputBytesPerFrame = bytesPerSample * outputChannelCount;
            int sampleFramesRequested = count / outputBytesPerFrame;
            int inputOffset = 0;
            int sampleFramesRead = 0;
            // now we must read from all inputs, even if we don't need their data, so they stay in sync
            foreach (var input in inputs)
            {
                int inputBytesPerFrame = bytesPerSample * input.WaveFormat.Channels;
                int bytesRequired = sampleFramesRequested * inputBytesPerFrame;
                inputBuffer = BufferHelpers.Ensure(inputBuffer, bytesRequired);
                int bytesRead = input.Read(inputBuffer, 0, bytesRequired);
                sampleFramesRead = Math.Max(sampleFramesRead, bytesRead / inputBytesPerFrame);

                for (int n = 0; n < input.WaveFormat.Channels; n++)
                {
                    int inputIndex = inputOffset + n;
                    for (int outputIndex = 0; outputIndex < outputChannelCount; outputIndex++)
                    {
                        if (mappings[outputIndex] == inputIndex)
                        {
                            int inputBufferOffset = n * bytesPerSample;
                            int outputBufferOffset = offset + outputIndex * bytesPerSample;
                            int sample = 0;
                            while (sample < sampleFramesRequested && inputBufferOffset < bytesRead)
                            {
                                Array.Copy(inputBuffer, inputBufferOffset, buffer, outputBufferOffset, bytesPerSample);
                                outputBufferOffset += outputBytesPerFrame;
                                inputBufferOffset += inputBytesPerFrame;
                                sample++;
                            }
                            // clear the end
                            while (sample < sampleFramesRequested)
                            {
                                Array.Clear(buffer, outputBufferOffset, bytesPerSample);
                                outputBufferOffset += outputBytesPerFrame;
                                sample++;
                            }
                        }
                    }
                }
                inputOffset += input.WaveFormat.Channels;
            }

            return sampleFramesRead * outputBytesPerFrame;
        }

        /// <summary>
        /// The WaveFormat of this WaveProvider
        /// </summary>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// Connects the input channel to the output channel.
        /// </summary>
        /// <param name="inputChannel">The input channel to be connected.</param>
        /// <param name="outputChannel">The output channel to be connected.</param>
        /// <exception cref="ArgumentException">Thrown when the input channel or output channel is invalid.</exception>
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
