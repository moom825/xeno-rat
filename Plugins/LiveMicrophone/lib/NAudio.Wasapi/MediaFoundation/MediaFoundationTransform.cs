using System;
using System.Runtime.InteropServices;
using NAudio.Utils;
using NAudio.Wave;

namespace NAudio.MediaFoundation
{
    /// <summary>
    /// An abstract base class for simplifying working with Media Foundation Transforms
    /// You need to override the method that actually creates and configures the transform
    /// </summary>
    public abstract class MediaFoundationTransform : IWaveProvider, IDisposable
    {
        /// <summary>
        /// The Source Provider
        /// </summary>
        protected readonly IWaveProvider sourceProvider;
        /// <summary>
        /// The Output WaveFormat
        /// </summary>
        protected readonly WaveFormat outputWaveFormat;
        private readonly byte[] sourceBuffer;
        
        private byte[] outputBuffer;
        private int outputBufferOffset;
        private int outputBufferCount;

        private IMFTransform transform;
        private bool disposed;
        private long inputPosition; // in ref-time, so we can timestamp the input samples
        private long outputPosition; // also in ref-time
        private bool initializedForStreaming;

        /// <summary>
        /// Constructs a new MediaFoundationTransform wrapper
        /// Will read one second at a time
        /// </summary>
        /// <param name="sourceProvider">The source provider for input data to the transform</param>
        /// <param name="outputFormat">The desired output format</param>
        public MediaFoundationTransform(IWaveProvider sourceProvider, WaveFormat outputFormat)
        {
            this.outputWaveFormat = outputFormat;
            this.sourceProvider = sourceProvider;
            sourceBuffer = new byte[sourceProvider.WaveFormat.AverageBytesPerSecond];
            outputBuffer = new byte[outputWaveFormat.AverageBytesPerSecond + outputWaveFormat.BlockAlign]; // we will grow this buffer if needed, but try to make something big enough
        }

        /// <summary>
        /// Initializes the transform for streaming by processing specific messages and setting the <paramref name="initializedForStreaming"/> flag to true.
        /// </summary>
        /// <remarks>
        /// This method processes the MFT_MESSAGE_COMMAND_FLUSH, MFT_MESSAGE_NOTIFY_BEGIN_STREAMING, and MFT_MESSAGE_NOTIFY_START_OF_STREAM messages using the <paramref name="transform"/> object.
        /// After processing the messages, it sets the <paramref name="initializedForStreaming"/> flag to true, indicating that the transform is initialized for streaming.
        /// </remarks>
        private void InitializeTransformForStreaming()
        {
            transform.ProcessMessage(MFT_MESSAGE_TYPE.MFT_MESSAGE_COMMAND_FLUSH, IntPtr.Zero);
            transform.ProcessMessage(MFT_MESSAGE_TYPE.MFT_MESSAGE_NOTIFY_BEGIN_STREAMING, IntPtr.Zero);
            transform.ProcessMessage(MFT_MESSAGE_TYPE.MFT_MESSAGE_NOTIFY_START_OF_STREAM, IntPtr.Zero);
            initializedForStreaming = true;
        }

        /// <summary>
        /// Creates and returns a new IMFTransform object.
        /// </summary>
        /// <remarks>
        /// This method is an abstract method that must be implemented by derived classes.
        /// It is used to create and return a new IMFTransform object, which represents a Media Foundation transform (MFT).
        /// An MFT is a COM object that performs media processing operations, such as decoding, encoding, or processing audio and video data.
        /// </remarks>
        protected abstract IMFTransform CreateTransform();

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <remarks>
        /// This method is called by the public <see cref="Dispose"/> method and the <see cref="Finalize"/> method.
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly or indirectly by a user's code.
        /// Managed and unmanaged resources can be disposed.
        /// If disposing equals false, the method has been called by the runtime from inside the finalizer and you should not reference other objects.
        /// Only unmanaged resources can be disposed.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (transform != null)
            {
                Marshal.ReleaseComObject(transform);
            }
        }

        /// <summary>
        /// Disposes this Media Foundation Transform
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~MediaFoundationTransform()
        {
            Dispose(false);
        }

        /// <summary>
        /// The output WaveFormat of this Media Foundation Transform
        /// </summary>
        public WaveFormat WaveFormat { get { return outputWaveFormat; } }

        /// <summary>
        /// Reads data from the input buffer and processes it using the transform, returning the number of bytes written to the output buffer.
        /// </summary>
        /// <param name="buffer">The input buffer containing the data to be processed.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin reading.</param>
        /// <param name="count">The maximum number of bytes to read from <paramref name="buffer"/>.</param>
        /// <returns>The total number of bytes written to the output buffer.</returns>
        /// <remarks>
        /// This method reads data from the input buffer and processes it using the transform. It ensures that the transform is initialized for streaming and then continuously reads data from the source, processes it using the transform, and writes the processed data to the output buffer until the specified count is reached. If there are any leftover bytes from the previous read, they are first written to the output buffer. If the end of the input is reached, the method ends the stream, drains any remaining data from the transform, and clears the output buffer. The method then returns the total number of bytes written to the output buffer.
        /// </remarks>
        public int Read(byte[] buffer, int offset, int count)
        {
            if (transform == null)
            {
                transform = CreateTransform();
                InitializeTransformForStreaming();
            }

            // strategy will be to always read 1 second from the source, and give it to the resampler
            int bytesWritten = 0;
            
            // read in any leftovers from last time
            if (outputBufferCount > 0)
            {
                bytesWritten += ReadFromOutputBuffer(buffer, offset, count - bytesWritten);
            }

            while (bytesWritten < count)
            {
                var sample = ReadFromSource();
                if (sample == null) // reached the end of our input
                {
                    // be good citizens and send some end messages:
                    EndStreamAndDrain();
                    // resampler might have given us a little bit more to return
                    bytesWritten += ReadFromOutputBuffer(buffer, offset + bytesWritten, count - bytesWritten);
                    ClearOutputBuffer();
                    break;
                }

                // might need to resurrect the stream if the user has read all the way to the end,
                // and then repositioned the input backwards
                if (!initializedForStreaming)
                {
                    InitializeTransformForStreaming();
                }

                // give the input to the resampler
                // can get MF_E_NOTACCEPTING if we didn't drain the buffer properly
                transform.ProcessInput(0, sample, 0);

                Marshal.ReleaseComObject(sample);

                int readFromTransform;
                // n.b. in theory we ought to loop here, although we'd need to be careful as the next time into ReadFromTransform there could
                // still be some leftover bytes in outputBuffer, which would get overwritten. Only introduce this if we find a transform that 
                // needs it. For most transforms, alternating read/write should be OK
                //do
                //{
                // keep reading from transform
                readFromTransform = ReadFromTransform();
                bytesWritten += ReadFromOutputBuffer(buffer, offset + bytesWritten, count - bytesWritten);
                //} while (readFromTransform > 0);
            }

            return bytesWritten;
        }

        /// <summary>
        /// Ends the stream and drains the transform.
        /// </summary>
        /// <remarks>
        /// This method sends a message to the transform to notify the end of the stream and then drains the transform by sending a command message.
        /// It then reads from the transform until no more data is available, resetting the input and output positions as well as notifying the end of streaming.
        /// </remarks>
        private void EndStreamAndDrain()
        {
            transform.ProcessMessage(MFT_MESSAGE_TYPE.MFT_MESSAGE_NOTIFY_END_OF_STREAM, IntPtr.Zero);
            transform.ProcessMessage(MFT_MESSAGE_TYPE.MFT_MESSAGE_COMMAND_DRAIN, IntPtr.Zero);
            int read;
            do
            {
                read = ReadFromTransform();
            } while (read > 0);
            inputPosition = 0;
            outputPosition = 0;
            transform.ProcessMessage(MFT_MESSAGE_TYPE.MFT_MESSAGE_NOTIFY_END_STREAMING, IntPtr.Zero);
            initializedForStreaming = false;
        }

        /// <summary>
        /// Clears the output buffer by resetting the count and offset to zero.
        /// </summary>
        private void ClearOutputBuffer()
        {
            outputBufferCount = 0;
            outputBufferOffset = 0;
        }

        /// <summary>
        /// Reads data from the transform and returns the length of the output buffer.
        /// </summary>
        /// <returns>The length of the output buffer.</returns>
        /// <remarks>
        /// This method reads data from the transform and returns the length of the output buffer. It creates a sample and memory buffer using MediaFoundationApi, processes the output, and handles exceptions accordingly.
        /// </remarks>
        private int ReadFromTransform()
        {
            var outputDataBuffer = new MFT_OUTPUT_DATA_BUFFER[1];
            // we have to create our own for
            var sample = MediaFoundationApi.CreateSample();
            var pBuffer = MediaFoundationApi.CreateMemoryBuffer(outputBuffer.Length);
            sample.AddBuffer(pBuffer);
            sample.SetSampleTime(outputPosition); // hopefully this is not needed
            outputDataBuffer[0].pSample = sample;
            
            _MFT_PROCESS_OUTPUT_STATUS status;
            var hr = transform.ProcessOutput(_MFT_PROCESS_OUTPUT_FLAGS.None, 
                                             1, outputDataBuffer, out status);
            if (hr == MediaFoundationErrors.MF_E_TRANSFORM_NEED_MORE_INPUT)
            {
                Marshal.ReleaseComObject(pBuffer);
                Marshal.ReleaseComObject(sample);
                // nothing to read
                return 0;
            }
            else if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            IMFMediaBuffer outputMediaBuffer;
            outputDataBuffer[0].pSample.ConvertToContiguousBuffer(out outputMediaBuffer);
            IntPtr pOutputBuffer;
            int outputBufferLength;
            int maxSize;
            outputMediaBuffer.Lock(out pOutputBuffer, out maxSize, out outputBufferLength);
            outputBuffer = BufferHelpers.Ensure(outputBuffer, outputBufferLength);
            Marshal.Copy(pOutputBuffer, outputBuffer, 0, outputBufferLength);
            outputBufferOffset = 0;
            outputBufferCount = outputBufferLength;
            outputMediaBuffer.Unlock();
            outputPosition += BytesToNsPosition(outputBufferCount, WaveFormat); // hopefully not needed
            Marshal.ReleaseComObject(pBuffer);
            sample.RemoveAllBuffers(); // needed to fix memory leak in some cases
            Marshal.ReleaseComObject(sample);
            Marshal.ReleaseComObject(outputMediaBuffer);
            return outputBufferLength;
        }

        /// <summary>
        /// Converts the given number of bytes to the corresponding position in nanoseconds based on the provided WaveFormat.
        /// </summary>
        /// <param name="bytes">The number of bytes to be converted.</param>
        /// <param name="waveFormat">The WaveFormat used to calculate the position.</param>
        /// <returns>The position in nanoseconds corresponding to the given number of bytes based on the provided WaveFormat.</returns>
        private static long BytesToNsPosition(int bytes, WaveFormat waveFormat)
        {
            long nsPosition = (10000000L * bytes) / waveFormat.AverageBytesPerSecond;
            return nsPosition;
        }

        /// <summary>
        /// Reads data from the source provider and creates an IMFSample object.
        /// </summary>
        /// <returns>An IMFSample object containing the data read from the source provider.</returns>
        /// <remarks>
        /// This method reads a full second of data from the source provider and creates an IMFSample object.
        /// It locks the media buffer, copies the source data into it, unlocks the media buffer, and sets its current length.
        /// Then it creates a sample, adds the media buffer to it, sets the sample time and duration, and returns the sample.
        /// </remarks>
        private IMFSample ReadFromSource()
        {
            // we always read a full second
            int bytesRead = sourceProvider.Read(sourceBuffer, 0, sourceBuffer.Length);
            if (bytesRead == 0) return null;

            var mediaBuffer = MediaFoundationApi.CreateMemoryBuffer(bytesRead);
            IntPtr pBuffer;
            int maxLength, currentLength;
            mediaBuffer.Lock(out pBuffer, out maxLength, out currentLength);
            Marshal.Copy(sourceBuffer, 0, pBuffer, bytesRead);
            mediaBuffer.Unlock();
            mediaBuffer.SetCurrentLength(bytesRead);

            var sample = MediaFoundationApi.CreateSample();
            sample.AddBuffer(mediaBuffer);
            // we'll set the time, I don't think it is needed for Resampler, but other MFTs might need it
            sample.SetSampleTime(inputPosition);
            long duration = BytesToNsPosition(bytesRead, sourceProvider.WaveFormat);
            sample.SetSampleDuration(duration);
            inputPosition += duration;
            Marshal.ReleaseComObject(mediaBuffer);
            return sample;
        }

        /// <summary>
        /// Reads bytes from the output buffer into the provided buffer and returns the number of bytes read.
        /// </summary>
        /// <param name="buffer">The buffer to which the bytes will be copied.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes.</param>
        /// <param name="needed">The maximum number of bytes to read from the output buffer.</param>
        /// <returns>The actual number of bytes read from the output buffer and copied into <paramref name="buffer"/>.</returns>
        /// <remarks>
        /// This method reads a maximum of <paramref name="needed"/> bytes from the output buffer into the provided <paramref name="buffer"/>.
        /// If the output buffer contains fewer bytes than needed, it reads all available bytes.
        /// The method then updates the output buffer offset and count accordingly.
        /// </remarks>
        private int ReadFromOutputBuffer(byte[] buffer, int offset, int needed)
        {
            int bytesFromOutputBuffer = Math.Min(needed, outputBufferCount);
            Array.Copy(outputBuffer, outputBufferOffset, buffer, offset, bytesFromOutputBuffer);
            outputBufferOffset += bytesFromOutputBuffer;
            outputBufferCount -= bytesFromOutputBuffer;
            if (outputBufferCount == 0)
            {
                outputBufferOffset = 0;
            }
            return bytesFromOutputBuffer;
        }

        /// <summary>
        /// Repositions the object for streaming if it is initialized for streaming, by ending the stream and draining, clearing the output buffer, and reinitializing the transform for streaming.
        /// </summary>
        /// <remarks>
        /// This method checks if the object is initialized for streaming. If it is, it ends the stream and drains any remaining data, clears the output buffer, and reinitializes the transform for streaming.
        /// </remarks>
        public void Reposition()
        {
            if (initializedForStreaming)
            {
                EndStreamAndDrain();
                ClearOutputBuffer();
                InitializeTransformForStreaming();
            }
        }
    }
}