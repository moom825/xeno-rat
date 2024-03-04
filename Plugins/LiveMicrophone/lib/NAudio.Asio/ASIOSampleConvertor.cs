using System;

namespace NAudio.Wave.Asio
{
    /// <summary>
    /// This class stores convertors for different interleaved WaveFormat to ASIOSampleType separate channel
    /// format.
    /// </summary>
    internal class AsioSampleConvertor
    {
        public delegate void SampleConvertor(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples);

        /// <summary>
        /// Selects the appropriate sample convertor based on the provided WaveFormat and AsioSampleType.
        /// </summary>
        /// <param name="waveFormat">The WaveFormat of the audio data.</param>
        /// <param name="asioType">The AsioSampleType of the audio data.</param>
        /// <returns>The selected SampleConvertor based on the provided WaveFormat and AsioSampleType.</returns>
        /// <remarks>
        /// This method selects the appropriate SampleConvertor based on the provided WaveFormat and AsioSampleType.
        /// It checks the combination of waveFormat and asioType to determine the appropriate convertor and returns it.
        /// If the combination is not supported, it throws an ArgumentException with a descriptive message.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when the combination of waveFormat and asioType is not supported.</exception>
        public static SampleConvertor SelectSampleConvertor(WaveFormat waveFormat, AsioSampleType asioType)
        {
            SampleConvertor convertor = null;
            bool is2Channels = waveFormat.Channels == 2;

            // TODO : IMPLEMENTS OTHER CONVERTOR TYPES
            switch (asioType)
            {
                case AsioSampleType.Int32LSB:
                    switch (waveFormat.BitsPerSample)
                    {
                        case 16:
                            convertor = (is2Channels) ? (SampleConvertor)ConvertorShortToInt2Channels : (SampleConvertor)ConvertorShortToIntGeneric;
                            break;
                        case 32:
                            if (waveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                                convertor = (is2Channels) ? (SampleConvertor)ConvertorFloatToInt2Channels : (SampleConvertor)ConvertorFloatToIntGeneric;
                            else
                                convertor = (is2Channels) ? (SampleConvertor)ConvertorIntToInt2Channels : (SampleConvertor)ConvertorIntToIntGeneric;
                            break;
                    }
                    break;
                case AsioSampleType.Int16LSB:
                    switch (waveFormat.BitsPerSample)
                    {
                        case 16:
                            convertor = (is2Channels) ? (SampleConvertor)ConvertorShortToShort2Channels : (SampleConvertor)ConvertorShortToShortGeneric;
                            break;
                        case 32:
                            if (waveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                                convertor = (is2Channels) ? (SampleConvertor)ConvertorFloatToShort2Channels : (SampleConvertor)ConvertorFloatToShortGeneric;
                            else
                                convertor = (is2Channels) ? (SampleConvertor)ConvertorIntToShort2Channels : (SampleConvertor)ConvertorIntToShortGeneric;
                            break;
                    }
                    break;
                case AsioSampleType.Int24LSB:
                    switch (waveFormat.BitsPerSample)
                    {
                        case 16:
                            throw new ArgumentException("Not a supported conversion");
                        case 32:
                            if (waveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                                convertor = ConverterFloatTo24LSBGeneric;
                            else
                                throw new ArgumentException("Not a supported conversion");
                            break;
                    }
                    break;
                case AsioSampleType.Float32LSB:
                    switch (waveFormat.BitsPerSample)
                    {
                        case 16:
                            throw new ArgumentException("Not a supported conversion");
                        case 32:
                            if (waveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                                convertor = ConverterFloatToFloatGeneric;
                            else
                                convertor = ConvertorIntToFloatGeneric;
                            break;
                    }
                    break;

                default:
                    throw new ArgumentException(
                        String.Format("ASIO Buffer Type {0} is not yet supported.",
                                      Enum.GetName(typeof(AsioSampleType), asioType)));
            }
            return convertor;
        }

        /// <summary>
        /// Converts an interleaved buffer of 16-bit samples to two separate channels of 16-bit samples.
        /// </summary>
        /// <param name="inputInterleavedBuffer">The pointer to the input interleaved buffer.</param>
        /// <param name="asioOutputBuffers">An array of pointers to the ASIO output buffers for each channel.</param>
        /// <param name="nbChannels">The number of channels.</param>
        /// <param name="nbSamples">The number of samples per channel.</param>
        /// <remarks>
        /// This method converts an interleaved buffer of 16-bit samples to two separate channels of 16-bit samples.
        /// It uses a pointer trick to avoid conversion from 16-bit to 32-bit.
        /// The input interleaved buffer is assumed to contain samples for left and right channels interleaved.
        /// The method then separates and stores the samples for the left and right channels in the respective ASIO output buffers.
        /// </remarks>
        public static void ConvertorShortToInt2Channels(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                short* inputSamples = (short*)inputInterleavedBuffer;
                // Use a trick (short instead of int to avoid any convertion from 16Bit to 32Bit)
                short* leftSamples = (short*)asioOutputBuffers[0];
                short* rightSamples = (short*)asioOutputBuffers[1];

                // Point to upper 16 bits of the 32Bits.
                leftSamples++;
                rightSamples++;
                for (int i = 0; i < nbSamples; i++)
                {
                    *leftSamples = inputSamples[0];
                    *rightSamples = inputSamples[1];
                    // Go to next sample
                    inputSamples += 2;
                    // Add 4 Bytes
                    leftSamples += 2;
                    rightSamples += 2;
                }
            }
        }

        /// <summary>
        /// Converts interleaved short input buffer to multiple short output buffers.
        /// </summary>
        /// <param name="inputInterleavedBuffer">The pointer to the interleaved input buffer.</param>
        /// <param name="asioOutputBuffers">The array of pointers to the output buffers.</param>
        /// <param name="nbChannels">The number of channels.</param>
        /// <param name="nbSamples">The number of samples.</param>
        /// <remarks>
        /// This method converts the interleaved short input buffer to multiple short output buffers.
        /// It uses a trick (short instead of int to avoid any conversion from 16Bit to 32Bit).
        /// The input samples are pointed to by the pointer <paramref name="inputInterleavedBuffer"/>.
        /// The output samples are pointed to by the array of pointers <paramref name="asioOutputBuffers"/>.
        /// The number of channels is specified by <paramref name="nbChannels"/>.
        /// The number of samples is specified by <paramref name="nbSamples"/>.
        /// </remarks>
        public static void ConvertorShortToIntGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                short* inputSamples = (short*)inputInterleavedBuffer;
                // Use a trick (short instead of int to avoid any convertion from 16Bit to 32Bit)
                short*[] samples = new short*[nbChannels];
                for (int i = 0; i < nbChannels; i++)
                {
                    samples[i] = (short*)asioOutputBuffers[i];
                    // Point to upper 16 bits of the 32Bits.
                    samples[i]++;
                }

                for (int i = 0; i < nbSamples; i++)
                {
                    for (int j = 0; j < nbChannels; j++)
                    {
                        *samples[j] = *inputSamples++;
                        samples[j] += 2;
                    }
                }
            }
        }

        /// <summary>
        /// Converts interleaved float input buffer to two separate int output channels.
        /// </summary>
        /// <param name="inputInterleavedBuffer">The pointer to the input interleaved buffer.</param>
        /// <param name="asioOutputBuffers">An array of pointers to the ASIO output buffers.</param>
        /// <param name="nbChannels">The number of channels.</param>
        /// <param name="nbSamples">The number of samples.</param>
        /// <remarks>
        /// This method converts the interleaved float input buffer to two separate int output channels.
        /// It iterates through the input buffer, converting and storing the samples in the left and right output channels.
        /// The conversion is performed using the clampToInt method to ensure that the float values are within the valid range for int.
        /// </remarks>
        public static void ConvertorFloatToInt2Channels(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                float* inputSamples = (float*)inputInterleavedBuffer;
                int* leftSamples = (int*)asioOutputBuffers[0];
                int* rightSamples = (int*)asioOutputBuffers[1];

                for (int i = 0; i < nbSamples; i++)
                {
                    *leftSamples++ = clampToInt(inputSamples[0]);
                    *rightSamples++ = clampToInt(inputSamples[1]);
                    inputSamples += 2;
                }
            }
        }

        /// <summary>
        /// Converts the input interleaved buffer of float samples to int samples and writes the result to the specified ASIO output buffers.
        /// </summary>
        /// <param name="inputInterleavedBuffer">The pointer to the input interleaved buffer of float samples.</param>
        /// <param name="asioOutputBuffers">An array of pointers to ASIO output buffers where the converted int samples will be written.</param>
        /// <param name="nbChannels">The number of channels in the input and output buffers.</param>
        /// <param name="nbSamples">The number of samples to be converted and written to the output buffers.</param>
        /// <remarks>
        /// This method converts the float samples in the input interleaved buffer to int samples and writes them to the specified ASIO output buffers.
        /// It uses pointer manipulation to achieve the conversion and writing process efficiently.
        /// The conversion is performed for each channel and for the specified number of samples.
        /// </remarks>
        public static void ConvertorFloatToIntGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                float* inputSamples = (float*)inputInterleavedBuffer;
                int*[] samples = new int*[nbChannels];
                for (int i = 0; i < nbChannels; i++)
                {
                    samples[i] = (int*)asioOutputBuffers[i];
                }

                for (int i = 0; i < nbSamples; i++)
                {
                    for (int j = 0; j < nbChannels; j++)
                    {
                        *samples[j]++ = clampToInt(*inputSamples++);
                    }
                }
            }
        }

        /// <summary>
        /// Converts an interleaved buffer of input samples to two separate channels and stores the result in the specified output buffers.
        /// </summary>
        /// <param name="inputInterleavedBuffer">A pointer to the interleaved input buffer containing the samples to be converted.</param>
        /// <param name="asioOutputBuffers">An array of pointers to the output buffers where the converted samples will be stored. The first element represents the left channel, and the second element represents the right channel.</param>
        /// <param name="nbChannels">The number of channels in the input buffer.</param>
        /// <param name="nbSamples">The number of samples to be converted.</param>
        /// <remarks>
        /// This method iterates through the input interleaved buffer, extracts the samples for the left and right channels, and stores them in the respective output buffers.
        /// The input buffer is assumed to contain interleaved samples, with each pair of consecutive samples representing the left and right channels, respectively.
        /// The method uses unsafe code to work with pointers for improved performance.
        /// </remarks>
        public static void ConvertorIntToInt2Channels(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                int* inputSamples = (int*)inputInterleavedBuffer;
                int* leftSamples = (int*)asioOutputBuffers[0];
                int* rightSamples = (int*)asioOutputBuffers[1];

                for (int i = 0; i < nbSamples; i++)
                {
                    *leftSamples++ = inputSamples[0];
                    *rightSamples++ = inputSamples[1];
                    inputSamples += 2;
                }
            }
        }

        /// <summary>
        /// Converts an interleaved buffer of integers to individual output buffers for each channel.
        /// </summary>
        /// <param name="inputInterleavedBuffer">The input buffer containing interleaved integer samples.</param>
        /// <param name="asioOutputBuffers">An array of output buffers for each channel.</param>
        /// <param name="nbChannels">The number of channels.</param>
        /// <param name="nbSamples">The number of samples per channel.</param>
        /// <remarks>
        /// This method converts the input interleaved buffer of integer samples to individual output buffers for each channel.
        /// It iterates through the input buffer and distributes the samples to the respective output buffers based on the number of channels.
        /// </remarks>
        public static void ConvertorIntToIntGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                int* inputSamples = (int*)inputInterleavedBuffer;
                int*[] samples = new int*[nbChannels];
                for (int i = 0; i < nbChannels; i++)
                {
                    samples[i] = (int*)asioOutputBuffers[i];
                }

                for (int i = 0; i < nbSamples; i++)
                {
                    for (int j = 0; j < nbChannels; j++)
                    {
                        *samples[j]++ = *inputSamples++;
                    }
                }
            }
        }

        /// <summary>
        /// Converts interleaved int buffer to two separate short channels.
        /// </summary>
        /// <param name="inputInterleavedBuffer">The input interleaved buffer containing int samples.</param>
        /// <param name="asioOutputBuffers">The array of IntPtrs representing the output buffers for each channel.</param>
        /// <param name="nbChannels">The number of channels in the output buffers.</param>
        /// <param name="nbSamples">The number of samples to be converted.</param>
        /// <remarks>
        /// This method converts the interleaved int buffer to two separate short channels by dividing each int sample by (1 << 16) and storing the result in the left and right output buffers.
        /// </remarks>
        public static void ConvertorIntToShort2Channels(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                int* inputSamples = (int*)inputInterleavedBuffer;
                short* leftSamples = (short*)asioOutputBuffers[0];
                short* rightSamples = (short*)asioOutputBuffers[1];

                for (int i = 0; i < nbSamples; i++)
                {
                    *leftSamples++ = (short)(inputSamples[0] / (1 << 16));
                    *rightSamples++ = (short)(inputSamples[1] / (1 << 16));
                    inputSamples += 2;
                }
            }
        }

        /// <summary>
        /// Converts interleaved int samples to short samples and stores them in the specified ASIO output buffers.
        /// </summary>
        /// <param name="inputInterleavedBuffer">The pointer to the input interleaved buffer containing int samples.</param>
        /// <param name="asioOutputBuffers">An array of pointers to ASIO output buffers where the converted short samples will be stored.</param>
        /// <param name="nbChannels">The number of audio channels.</param>
        /// <param name="nbSamples">The number of samples per channel.</param>
        /// <remarks>
        /// This method converts interleaved int samples to short samples and stores them in the specified ASIO output buffers. It iterates through the input samples, converts them to short, and stores them in the ASIO output buffers for each channel. The conversion is performed by dividing the input sample by (1 << 16) to obtain the short sample.
        /// </remarks>
        public static void ConvertorIntToShortGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                int* inputSamples = (int*)inputInterleavedBuffer;
                int*[] samples = new int*[nbChannels];
                for (int i = 0; i < nbChannels; i++)
                {
                    samples[i] = (int*)asioOutputBuffers[i];
                }

                for (int i = 0; i < nbSamples; i++)
                {
                    for (int j = 0; j < nbChannels; j++)
                    {
                        *samples[j]++ = (short)(*inputSamples++ / (1 << 16));
                    }
                }
            }
        }

        /// <summary>
        /// Converts interleaved int samples to float samples and writes the result to the specified ASIO output buffers.
        /// </summary>
        /// <param name="inputInterleavedBuffer">The pointer to the input interleaved buffer containing int samples.</param>
        /// <param name="asioOutputBuffers">An array of pointers to ASIO output buffers where the float samples will be written.</param>
        /// <param name="nbChannels">The number of channels in the input and output buffers.</param>
        /// <param name="nbSamples">The number of samples to be converted and written to the output buffers.</param>
        /// <remarks>
        /// This method converts the interleaved int samples in the input buffer to float samples and writes the result to the specified ASIO output buffers.
        /// It uses unsafe code to perform pointer arithmetic for efficient conversion and writing.
        /// </remarks>
        public static void ConvertorIntToFloatGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                int* inputSamples = (int*)inputInterleavedBuffer;
                float*[] samples = new float*[nbChannels];
                for (int i = 0; i < nbChannels; i++)
                {
                    samples[i] = (float*)asioOutputBuffers[i];
                }

                for (int i = 0; i < nbSamples; i++)
                {
                    for (int j = 0; j < nbChannels; j++)
                    {
                        *samples[j]++ = *inputSamples++ / (1 << (32 - 1));
                    }
                }
            }
        }

        /// <summary>
        /// Converts a short interleaved buffer to two separate short channels and stores the result in the specified ASIO output buffers.
        /// </summary>
        /// <param name="inputInterleavedBuffer">A pointer to the input interleaved buffer containing the short samples.</param>
        /// <param name="asioOutputBuffers">An array of IntPtrs pointing to the ASIO output buffers where the converted samples will be stored.</param>
        /// <param name="nbChannels">The number of channels in the input interleaved buffer.</param>
        /// <param name="nbSamples">The number of samples to be converted.</param>
        /// <remarks>
        /// This method converts the short interleaved buffer to two separate short channels and stores the result in the specified ASIO output buffers.
        /// It uses unsafe code to perform pointer manipulation for efficient conversion.
        /// </remarks>
        public static void ConvertorShortToShort2Channels(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                short* inputSamples = (short*)inputInterleavedBuffer;
                // Use a trick (short instead of int to avoid any convertion from 16Bit to 32Bit)
                short* leftSamples = (short*)asioOutputBuffers[0];
                short* rightSamples = (short*)asioOutputBuffers[1];

                // Point to upper 16 bits of the 32Bits.
                for (int i = 0; i < nbSamples; i++)
                {
                    *leftSamples++ = inputSamples[0];
                    *rightSamples++ = inputSamples[1];
                    // Go to next sample
                    inputSamples += 2;
                }
            }
        }

        /// <summary>
        /// Converts a short interleaved buffer to an array of short pointers representing the output buffers for each channel.
        /// </summary>
        /// <param name="inputInterleavedBuffer">The pointer to the input interleaved buffer.</param>
        /// <param name="asioOutputBuffers">An array of pointers to the output buffers for each channel.</param>
        /// <param name="nbChannels">The number of channels.</param>
        /// <param name="nbSamples">The number of samples.</param>
        /// <remarks>
        /// This method converts the short interleaved buffer to an array of short pointers representing the output buffers for each channel.
        /// It uses unsafe code to perform pointer manipulation for efficient conversion.
        /// </remarks>
        public static void ConvertorShortToShortGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                short* inputSamples = (short*)inputInterleavedBuffer;
                // Use a trick (short instead of int to avoid any convertion from 16Bit to 32Bit)
                short*[] samples = new short*[nbChannels];
                for (int i = 0; i < nbChannels; i++)
                {
                    samples[i] = (short*)asioOutputBuffers[i];
                }

                for (int i = 0; i < nbSamples; i++)
                {
                    for (int j = 0; j < nbChannels; j++)
                    {
                        *(samples[j]++) = *inputSamples++;
                    }
                }
            }
        }

        /// <summary>
        /// Converts a buffer of interleaved float samples to two channels of short samples.
        /// </summary>
        /// <param name="inputInterleavedBuffer">The pointer to the buffer containing interleaved float samples.</param>
        /// <param name="asioOutputBuffers">An array of pointers to the output buffers for each channel.</param>
        /// <param name="nbChannels">The number of channels in the output buffers.</param>
        /// <param name="nbSamples">The number of samples to be converted.</param>
        /// <remarks>
        /// This method converts the interleaved float samples in the input buffer to two separate channels of short samples.
        /// It uses a trick to avoid conversion from 16-bit to 32-bit by using short instead of int.
        /// The conversion is done by iterating through the input samples, clamping each float sample to a short value, and storing the result in the corresponding channel buffer.
        /// </remarks>
        public static void ConvertorFloatToShort2Channels(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                float* inputSamples = (float*)inputInterleavedBuffer;
                // Use a trick (short instead of int to avoid any convertion from 16Bit to 32Bit)
                short* leftSamples = (short*)asioOutputBuffers[0];
                short* rightSamples = (short*)asioOutputBuffers[1];

                for (int i = 0; i < nbSamples; i++)
                {
                    *leftSamples++ = clampToShort(inputSamples[0]);
                    *rightSamples++ = clampToShort(inputSamples[1]);
                    inputSamples += 2;
                }
            }
        }

        /// <summary>
        /// Converts a buffer of interleaved float samples to a buffer of short samples for each channel.
        /// </summary>
        /// <param name="inputInterleavedBuffer">The pointer to the input interleaved buffer containing float samples.</param>
        /// <param name="asioOutputBuffers">An array of pointers to the output buffers for each channel.</param>
        /// <param name="nbChannels">The number of channels.</param>
        /// <param name="nbSamples">The number of samples per channel.</param>
        /// <remarks>
        /// This method converts the interleaved float samples in the input buffer to short samples for each channel using pointer manipulation.
        /// It modifies the output buffers in place.
        /// </remarks>
        public static void ConvertorFloatToShortGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                float* inputSamples = (float*)inputInterleavedBuffer;
                // Use a trick (short instead of int to avoid any convertion from 16Bit to 32Bit)
                short*[] samples = new short*[nbChannels];
                for (int i = 0; i < nbChannels; i++)
                {
                    samples[i] = (short*)asioOutputBuffers[i];
                }

                for (int i = 0; i < nbSamples; i++)
                {
                    for (int j = 0; j < nbChannels; j++)
                    {
                        *(samples[j]++) = clampToShort(*inputSamples++);
                    }
                }
            }
        }

        /// <summary>
        /// Converts a float buffer to 24-bit LSB format and writes the result to the specified ASIO output buffers.
        /// </summary>
        /// <param name="inputInterleavedBuffer">A pointer to the input interleaved buffer containing float samples.</param>
        /// <param name="asioOutputBuffers">An array of pointers to ASIO output buffers where the 24-bit LSB formatted samples will be written.</param>
        /// <param name="nbChannels">The number of audio channels.</param>
        /// <param name="nbSamples">The number of audio samples per channel.</param>
        /// <remarks>
        /// This method converts the float samples in the input interleaved buffer to 24-bit LSB format and writes the result to the specified ASIO output buffers.
        /// It iterates through each sample, converts it to 24-bit format, and writes the bytes to the corresponding ASIO output buffer for each channel.
        /// </remarks>
        public static void ConverterFloatTo24LSBGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                float* inputSamples = (float*)inputInterleavedBuffer;
                
                byte*[] samples = new byte*[nbChannels];
                for (int i = 0; i < nbChannels; i++)
                {
                    samples[i] = (byte*)asioOutputBuffers[i];
                }

                for (int i = 0; i < nbSamples; i++)
                {
                    for (int j = 0; j < nbChannels; j++)
                    {
                        int sample24 = clampTo24Bit(*inputSamples++);
                        *(samples[j]++) = (byte)(sample24);
                        *(samples[j]++) = (byte)(sample24 >> 8);
                        *(samples[j]++) = (byte)(sample24 >> 16);
                    }
                }
            }
        }

        /// <summary>
        /// Converts interleaved float input buffer to non-interleaved float output buffers for the specified number of channels and samples.
        /// </summary>
        /// <param name="inputInterleavedBuffer">The pointer to the input interleaved float buffer.</param>
        /// <param name="asioOutputBuffers">An array of pointers to non-interleaved float output buffers for each channel.</param>
        /// <param name="nbChannels">The number of channels.</param>
        /// <param name="nbSamples">The number of samples.</param>
        /// <remarks>
        /// This method converts the interleaved float input buffer to non-interleaved float output buffers for the specified number of channels and samples.
        /// It uses unsafe code to directly manipulate memory pointers for improved performance.
        /// </remarks>
        public static void ConverterFloatToFloatGeneric(IntPtr inputInterleavedBuffer, IntPtr[] asioOutputBuffers, int nbChannels, int nbSamples)
        {
            unsafe
            {
                float* inputSamples = (float*)inputInterleavedBuffer;
                float*[] samples = new float*[nbChannels];
                for (int i = 0; i < nbChannels; i++)
                {
                    samples[i] = (float*)asioOutputBuffers[i];
                }

                for (int i = 0; i < nbSamples; i++)
                {
                    for (int j = 0; j < nbChannels; j++)
                    {
                        *(samples[j]++) = *inputSamples++;
                    }
                }
            }
        }

        /// <summary>
        /// Clamps the input sample value to a 24-bit integer representation.
        /// </summary>
        /// <param name="sampleValue">The input sample value to be clamped.</param>
        /// <returns>A 24-bit integer representation of the clamped <paramref name="sampleValue"/>.</returns>
        /// <remarks>
        /// This method clamps the input <paramref name="sampleValue"/> to the range [-1.0, 1.0] and then converts it to a 24-bit integer representation.
        /// The clamping is performed by setting the value to -1.0 if it is less than -1.0, to 1.0 if it is greater than 1.0, or leaving it unchanged otherwise.
        /// The conversion to 24-bit integer is achieved by multiplying the clamped value by 8388607 and then casting it to an integer.
        /// </remarks>
        private static int clampTo24Bit(double sampleValue)
        {
            sampleValue = (sampleValue < -1.0) ? -1.0 : (sampleValue > 1.0) ? 1.0 : sampleValue;
            return (int)(sampleValue * 8388607.0);
        }

        /// <summary>
        /// Clamps the input double value to the range [-1.0, 1.0] and returns the corresponding integer value.
        /// </summary>
        /// <param name="sampleValue">The input double value to be clamped.</param>
        /// <returns>
        /// The integer value obtained by multiplying the clamped <paramref name="sampleValue"/> with the maximum integer value (2147483647).
        /// </returns>
        private static int clampToInt(double sampleValue)
        {
            sampleValue = (sampleValue < -1.0) ? -1.0 : (sampleValue > 1.0) ? 1.0 : sampleValue;
            return (int)(sampleValue * 2147483647.0);
        }

        /// <summary>
        /// Clamps the input sample value to the range of a short data type and returns the result.
        /// </summary>
        /// <param name="sampleValue">The sample value to be clamped.</param>
        /// <returns>The clamped value of <paramref name="sampleValue"/> as a short data type.</returns>
        private static short clampToShort(double sampleValue)
        {
            sampleValue = (sampleValue < -1.0) ? -1.0 : (sampleValue > 1.0) ? 1.0 : sampleValue;
            return (short)(sampleValue * 32767.0);
        }
    }
}
