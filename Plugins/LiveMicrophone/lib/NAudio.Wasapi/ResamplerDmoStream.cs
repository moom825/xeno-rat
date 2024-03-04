using System;
using NAudio.Dmo;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    /// <summary>
    /// Wave Stream for converting between sample rates
    /// </summary>
    public class ResamplerDmoStream : WaveStream
    {
        private readonly IWaveProvider inputProvider;
        private readonly WaveStream inputStream;
        private readonly WaveFormat outputFormat;
        private DmoOutputDataBuffer outputBuffer;
        private DmoResampler dmoResampler;
        private MediaBuffer inputMediaBuffer;
        private long position;

        /// <summary>
        /// WaveStream to resample using the DMO Resampler
        /// </summary>
        /// <param name="inputProvider">Input Stream</param>
        /// <param name="outputFormat">Desired Output Format</param>
        public ResamplerDmoStream(IWaveProvider inputProvider, WaveFormat outputFormat)
        {
            this.inputProvider = inputProvider;
            inputStream = inputProvider as WaveStream;
            this.outputFormat = outputFormat;
            dmoResampler = new DmoResampler();
            if (!dmoResampler.MediaObject.SupportsInputWaveFormat(0, inputProvider.WaveFormat))
            {
                throw new ArgumentException("Unsupported Input Stream format", nameof(inputProvider));
            }

            dmoResampler.MediaObject.SetInputWaveFormat(0, inputProvider.WaveFormat);
            if (!dmoResampler.MediaObject.SupportsOutputWaveFormat(0, outputFormat))
            {
                throw new ArgumentException("Unsupported Output Stream format", nameof(outputFormat));
            }
         
            dmoResampler.MediaObject.SetOutputWaveFormat(0, outputFormat);
            if (inputStream != null)
            {
                position = InputToOutputPosition(inputStream.Position);
            }
            inputMediaBuffer = new MediaBuffer(inputProvider.WaveFormat.AverageBytesPerSecond);
            outputBuffer = new DmoOutputDataBuffer(outputFormat.AverageBytesPerSecond);
        }

        /// <summary>
        /// Stream Wave Format
        /// </summary>
        public override WaveFormat WaveFormat => outputFormat;

        /// <summary>
        /// Converts the input position to the corresponding output position based on the ratio of average bytes per second between input and output formats.
        /// </summary>
        /// <param name="inputPosition">The input position to be converted.</param>
        /// <returns>The corresponding output position calculated based on the ratio of average bytes per second between input and output formats.</returns>
        /// <remarks>
        /// This method calculates the output position by multiplying the input position with the ratio of average bytes per second between the output format and the input provider's wave format.
        /// If the calculated output position is not aligned with the output format's block align, it is adjusted to the nearest aligned position.
        /// </remarks>
        private long InputToOutputPosition(long inputPosition)
        {
            double ratio = (double)outputFormat.AverageBytesPerSecond
                / inputProvider.WaveFormat.AverageBytesPerSecond;
            long outputPosition = (long)(inputPosition * ratio);
            if (outputPosition % outputFormat.BlockAlign != 0)
            {
                outputPosition -= outputPosition % outputFormat.BlockAlign;
            }
            return outputPosition;
        }

        /// <summary>
        /// Converts the output position to the corresponding input position based on the audio format ratios.
        /// </summary>
        /// <param name="outputPosition">The output position for which the corresponding input position needs to be calculated.</param>
        /// <returns>The input position corresponding to the given <paramref name="outputPosition"/> based on the audio format ratios.</returns>
        /// <remarks>
        /// This method calculates the input position based on the ratio of average bytes per second between the output and input audio formats.
        /// It then adjusts the input position to align with the block size of the input audio format if necessary.
        /// </remarks>
        private long OutputToInputPosition(long outputPosition)
        {
            double ratio = (double)outputFormat.AverageBytesPerSecond
                / inputProvider.WaveFormat.AverageBytesPerSecond;
            long inputPosition = (long)(outputPosition / ratio);
            if (inputPosition % inputProvider.WaveFormat.BlockAlign != 0)
            {
                inputPosition -= inputPosition % inputProvider.WaveFormat.BlockAlign;
            }
            return inputPosition;
        }

        /// <summary>
        /// Stream length in bytes
        /// </summary>
        public override long Length
        {
            get 
            {
                if (inputStream == null)
                {
                    throw new InvalidOperationException("Cannot report length if the input was an IWaveProvider");
                }
                return InputToOutputPosition(inputStream.Length); 
            }
        }

        /// <summary>
        /// Stream position in bytes
        /// </summary>
        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                if (inputStream == null)
                {
                    throw new InvalidOperationException("Cannot set position if the input was not a WaveStream");
                }
                inputStream.Position = OutputToInputPosition(value);
                position = InputToOutputPosition(inputStream.Position);
                dmoResampler.MediaObject.Discontinuity(0);
            }
        }

        /// <summary>
        /// Reads a specified number of bytes from the input stream and writes them to the buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write the data to.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The total number of bytes read into the buffer. This might be less than the number of bytes requested if that number of bytes are not currently available, or zero if the end of the stream is reached.</returns>
        /// <remarks>
        /// This method reads data from the input stream and writes it to the buffer provided. It uses a DMO (DirectX Media Object) resampler to process the input data and provide output data. The method loops until the specified number of bytes have been read or until the end of the stream is reached. It modifies the position of the stream accordingly and returns the total number of bytes read into the buffer.
        /// </remarks>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int outputBytesProvided = 0;

            while (outputBytesProvided < count)
            {
                if (dmoResampler.MediaObject.IsAcceptingData(0))
                {
                    // 1. Read from the input stream 
                    int inputBytesRequired = (int)OutputToInputPosition(count - outputBytesProvided);
                    byte[] inputByteArray = new byte[inputBytesRequired];
                    int inputBytesRead = inputProvider.Read(inputByteArray, 0, inputBytesRequired);
                    if (inputBytesRead == 0)
                    {
                        //Debug.WriteLine("ResamplerDmoStream.Read: No input data available");
                        break;
                    }
                    // 2. copy into our DMO's input buffer
                    inputMediaBuffer.LoadData(inputByteArray, inputBytesRead);

                    // 3. Give the input buffer to the DMO to process
                    dmoResampler.MediaObject.ProcessInput(0, inputMediaBuffer, DmoInputDataBufferFlags.None, 0, 0);

                    outputBuffer.MediaBuffer.SetLength(0);
                    outputBuffer.StatusFlags = DmoOutputDataBufferFlags.None;

                    // 4. Now ask the DMO for some output data
                    dmoResampler.MediaObject.ProcessOutput(DmoProcessOutputFlags.None, 1, new[] { outputBuffer });

                    if (outputBuffer.Length == 0)
                    {
                        Debug.WriteLine("ResamplerDmoStream.Read: No output data available");
                        break;
                    }

                    // 5. Now get the data out of the output buffer
                    outputBuffer.RetrieveData(buffer, offset + outputBytesProvided);
                    outputBytesProvided += outputBuffer.Length;

                    Debug.Assert(!outputBuffer.MoreDataAvailable, "have not implemented more data available yet");
                }
                else
                {
                    Debug.Assert(false, "have not implemented not accepting logic yet");
                }
            }
            
            position += outputBytesProvided;
            return outputBytesProvided;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.Media.DmoResampler"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        /// <remarks>
        /// This method disposes the input media buffer if it is not null and sets it to null.
        /// It disposes the output buffer.
        /// If the DMO resampler is not null, it sets it to null.
        /// </remarks>
        protected override void Dispose(bool disposing)
        {
            if (inputMediaBuffer != null)
            {
                inputMediaBuffer.Dispose();
                inputMediaBuffer = null;
            }
            outputBuffer.Dispose();
            if (dmoResampler != null)
            {
                //resampler.Dispose(); s
                dmoResampler = null;
            }
            base.Dispose(disposing);
        }
    }
}
