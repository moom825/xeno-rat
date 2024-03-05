using System;
using System.Collections.Generic;
using NAudio.Utils;
using System.Runtime.InteropServices;
using NAudio.Wave;
using System.Diagnostics;

namespace NAudio.Dmo
{
    /// <summary>
    /// Media Object
    /// </summary>
    public class MediaObject : IDisposable
    {
        private IMediaObject mediaObject;
        private readonly int inputStreams;
        private readonly int outputStreams;

        #region Construction

        /// <summary>
        /// Creates a new Media Object
        /// </summary>
        /// <param name="mediaObject">Media Object COM interface</param>
        internal MediaObject(IMediaObject mediaObject)
        {
            this.mediaObject = mediaObject;
            mediaObject.GetStreamCount(out inputStreams, out outputStreams);
        }

        #endregion

        #region Public Properties
        /// <summary>
        /// Number of input streams
        /// </summary>
        public int InputStreamCount
        {
            get { return inputStreams; }
        }

        /// <summary>
        /// Number of output streams
        /// </summary>
        public int OutputStreamCount
        {
            get { return outputStreams; }
        }

        /// <summary>
        /// Retrieves the input type for a specified input stream and input type index.
        /// </summary>
        /// <param name="inputStream">The index of the input stream.</param>
        /// <param name="inputTypeIndex">The index of the input type.</param>
        /// <returns>The input type for the specified input stream and input type index, or null if not found.</returns>
        /// <exception cref="COMException">Thrown when an error occurs while retrieving the input type.</exception>
        public DmoMediaType? GetInputType(int inputStream, int inputTypeIndex)
        {
            try
            {
                DmoMediaType mediaType;
                int hresult = mediaObject.GetInputType(inputStream, inputTypeIndex, out mediaType);
                if (hresult == HResult.S_OK)
                {
                    // this frees the format (if present)
                    // we should therefore come up with a way of marshaling the format
                    // into a completely managed structure
                    DmoInterop.MoFreeMediaType(ref mediaType);
                    return mediaType;
                }
            }
            catch (COMException e)
            {
                if (e.GetHResult() != (int)DmoHResults.DMO_E_NO_MORE_ITEMS)
                {
                    throw;
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieves the output type for a specified output stream and output type index.
        /// </summary>
        /// <param name="outputStream">The index of the output stream.</param>
        /// <param name="outputTypeIndex">The index of the output type.</param>
        /// <returns>The output type for the specified output stream and output type index, or null if no more items are available.</returns>
        /// <exception cref="COMException">Thrown when an error occurs during the retrieval of the output type, except when the error is due to no more items being available.</exception>
        public DmoMediaType? GetOutputType(int outputStream, int outputTypeIndex)
        {
            try
            {
                DmoMediaType mediaType;
                int hresult = mediaObject.GetOutputType(outputStream, outputTypeIndex, out mediaType);
                if (hresult == HResult.S_OK)
                {
                    // this frees the format (if present)
                    // we should therefore come up with a way of marshaling the format
                    // into a completely managed structure
                    DmoInterop.MoFreeMediaType(ref mediaType);
                    return mediaType;
                }
            }
            catch (COMException e)
            {
                if (e.GetHResult() != (int)DmoHResults.DMO_E_NO_MORE_ITEMS)
                {
                    throw;
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieves the current output media type for the specified output stream index.
        /// </summary>
        /// <param name="outputStreamIndex">The index of the output stream for which to retrieve the media type.</param>
        /// <returns>The current media type of the specified output stream.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the media type was not set.</exception>
        /// <exception cref="Exception">Thrown when an error occurs while retrieving the media type.</exception>
        /// <remarks>
        /// This method retrieves the current media type for the specified output stream index from the media object.
        /// If the operation is successful, it returns the current media type. If the media type was not set, it throws an InvalidOperationException.
        /// If an error occurs during the retrieval process, it throws an Exception with details of the error.
        /// </remarks>
        public DmoMediaType GetOutputCurrentType(int outputStreamIndex)
        {
            DmoMediaType mediaType;
            int hresult = mediaObject.GetOutputCurrentType(outputStreamIndex, out mediaType);
            if (hresult == HResult.S_OK)
            {
                // this frees the format (if present)
                // we should therefore come up with a way of marshaling the format
                // into a completely managed structure
                DmoInterop.MoFreeMediaType(ref mediaType);
                return mediaType;
            }
            else
            {
                if (hresult == (int)DmoHResults.DMO_E_TYPE_NOT_SET)
                {
                    throw new InvalidOperationException("Media type was not set.");
                }
                else
                {
                    throw Marshal.GetExceptionForHR(hresult);
                }
            }
        }

        /// <summary>
        /// Retrieves the input types for the specified input stream index.
        /// </summary>
        /// <param name="inputStreamIndex">The index of the input stream for which to retrieve the types.</param>
        /// <returns>An enumerable collection of input types for the specified input stream index.</returns>
        /// <remarks>
        /// This method retrieves the input types for the specified input stream index by iterating through the available types using a while loop.
        /// It calls the GetInputType method to retrieve each type and yields the result until no more types are available.
        /// </remarks>
        public IEnumerable<DmoMediaType> GetInputTypes(int inputStreamIndex)
        {
            int typeIndex = 0;
            DmoMediaType? mediaType;
            while ((mediaType = GetInputType(inputStreamIndex,typeIndex)) != null)
            {
                yield return mediaType.Value;
                typeIndex++;
            }
        }

        /// <summary>
        /// Retrieves the output types for the specified output stream index.
        /// </summary>
        /// <param name="outputStreamIndex">The index of the output stream for which to retrieve the types.</param>
        /// <returns>An enumerable collection of <see cref="DmoMediaType"/> representing the output types for the specified output stream index.</returns>
        /// <remarks>
        /// This method iterates through the output types of the specified output stream index using a while loop.
        /// It retrieves each output type using the <see cref="GetOutputType"/> method and yields the result.
        /// The method continues iterating until no more output types are found for the specified output stream index.
        /// </remarks>
        public IEnumerable<DmoMediaType> GetOutputTypes(int outputStreamIndex)
        {
            int typeIndex = 0;
            DmoMediaType? mediaType;
            while ((mediaType = GetOutputType(outputStreamIndex, typeIndex)) != null)
            {
                yield return mediaType.Value;
                typeIndex++;
            }
        }

        /// <summary>
        /// Checks if the specified input stream index supports the given media type.
        /// </summary>
        /// <param name="inputStreamIndex">The index of the input stream to be checked.</param>
        /// <param name="mediaType">The media type to be checked for support.</param>
        /// <returns>True if the input stream supports the specified media type; otherwise, false.</returns>
        public bool SupportsInputType(int inputStreamIndex, DmoMediaType mediaType)
        {
            return SetInputType(inputStreamIndex, mediaType, DmoSetTypeFlags.DMO_SET_TYPEF_TEST_ONLY);
        }

        /// <summary>
        /// Sets the input type for the specified input stream using the given media type.
        /// </summary>
        /// <param name="inputStreamIndex">The index of the input stream for which the media type is to be set.</param>
        /// <param name="mediaType">The media type to be set for the input stream.</param>
        /// <exception cref="ArgumentException">Thrown when the specified media type is not supported.</exception>
        private bool SetInputType(int inputStreamIndex, DmoMediaType mediaType, DmoSetTypeFlags flags)
        {
            int hResult = mediaObject.SetInputType(inputStreamIndex, ref mediaType, flags);
            if (hResult != HResult.S_OK)
            {
                if (hResult == (int)DmoHResults.DMO_E_INVALIDSTREAMINDEX)
                {
                    throw new ArgumentException("Invalid stream index");
                }
                if (hResult == (int)DmoHResults.DMO_E_TYPE_NOT_ACCEPTED)
                {
                    Debug.WriteLine("Media type was not accepted");
                }

                return false;
            }
            return true;
        }

        /// <summary>
        /// Sets the input type
        /// </summary>
        /// <param name="inputStreamIndex">Input stream index</param>
        /// <param name="mediaType">Media Type</param>
        public void SetInputType(int inputStreamIndex, DmoMediaType mediaType)
        {
            if(!SetInputType(inputStreamIndex,mediaType,DmoSetTypeFlags.None))
            {
                throw new ArgumentException("Media Type not supported");
            }
        }

        /// <summary>
        /// Sets the input wave format for the specified input stream.
        /// </summary>
        /// <param name="inputStreamIndex">The index of the input stream for which the wave format is to be set.</param>
        /// <param name="waveFormat">The WaveFormat to be set for the input stream.</param>
        /// <exception cref="ArgumentException">Thrown when the specified media type is not supported.</exception>
        /// <remarks>
        /// This method sets the input wave format for the specified input stream using the DMO (DirectX Media Object) media type.
        /// It creates a DMO media type based on the provided WaveFormat and sets it for the input stream using DmoInterop.SetInputType method.
        /// If the media type cannot be set, an ArgumentException is thrown with the message "Media Type not supported".
        /// </remarks>
        public void SetInputWaveFormat(int inputStreamIndex, WaveFormat waveFormat)
        {
            DmoMediaType mediaType = CreateDmoMediaTypeForWaveFormat(waveFormat);
            bool set = SetInputType(inputStreamIndex, mediaType, DmoSetTypeFlags.None);
            DmoInterop.MoFreeMediaType(ref mediaType);
            if (!set)
            {
                throw new ArgumentException("Media Type not supported");
            }
        }

        /// <summary>
        /// Checks if the specified input stream supports the given wave format.
        /// </summary>
        /// <param name="inputStreamIndex">The index of the input stream to be checked.</param>
        /// <param name="waveFormat">The wave format to be checked for support.</param>
        /// <returns>True if the input stream supports the specified wave format; otherwise, false.</returns>
        /// <remarks>
        /// This method creates a DMO media type for the provided wave format and then sets the input type for the specified input stream using the DMO_SET_TYPEF_TEST_ONLY flag to check for support.
        /// After the check, the allocated DMO media type is freed using MoFreeMediaType method.
        /// </remarks>
        public bool SupportsInputWaveFormat(int inputStreamIndex, WaveFormat waveFormat)
        {
            DmoMediaType mediaType = CreateDmoMediaTypeForWaveFormat(waveFormat);
            bool supported = SetInputType(inputStreamIndex, mediaType, DmoSetTypeFlags.DMO_SET_TYPEF_TEST_ONLY);
            DmoInterop.MoFreeMediaType(ref mediaType);
            return supported;
        }

        /// <summary>
        /// Creates a DMO media type for the specified WaveFormat.
        /// </summary>
        /// <param name="waveFormat">The WaveFormat for which the DMO media type is to be created.</param>
        /// <returns>The DMO media type created for the specified <paramref name="waveFormat"/>.</returns>
        /// <remarks>
        /// This method initializes a DMO media type, sets the wave format, and returns the created media type.
        /// </remarks>
        private DmoMediaType CreateDmoMediaTypeForWaveFormat(WaveFormat waveFormat)
        {
            DmoMediaType mediaType = new DmoMediaType();
            int waveFormatExSize = Marshal.SizeOf(waveFormat);  // 18 + waveFormat.ExtraSize;
            DmoInterop.MoInitMediaType(ref mediaType, waveFormatExSize);
            mediaType.SetWaveFormat(waveFormat);
            return mediaType;
        }

        /// <summary>
        /// Checks if the specified output stream supports the given media type.
        /// </summary>
        /// <param name="outputStreamIndex">The index of the output stream to be checked.</param>
        /// <param name="mediaType">The media type to be checked for support.</param>
        /// <returns>True if the specified output stream supports the given media type; otherwise, false.</returns>
        /// <remarks>
        /// This method internally calls the SetOutputType method with the DMO_SET_TYPEF_TEST_ONLY flag to check if the specified output stream supports the given media type without actually setting it.
        /// </remarks>
        public bool SupportsOutputType(int outputStreamIndex, DmoMediaType mediaType)
        {
            return SetOutputType(outputStreamIndex, mediaType, DmoSetTypeFlags.DMO_SET_TYPEF_TEST_ONLY);
        }

        /// <summary>
        /// Checks if the specified output stream supports the given wave format.
        /// </summary>
        /// <param name="outputStreamIndex">The index of the output stream to be checked.</param>
        /// <param name="waveFormat">The wave format to be checked for support.</param>
        /// <returns>True if the specified output stream supports the given wave format; otherwise, false.</returns>
        /// <remarks>
        /// This method creates a DMO media type for the provided wave format and sets the output type for the specified stream to test if it is supported.
        /// It then frees the allocated media type and returns the result indicating whether the wave format is supported by the output stream.
        /// </remarks>
        public bool SupportsOutputWaveFormat(int outputStreamIndex, WaveFormat waveFormat)
        {
            DmoMediaType mediaType = CreateDmoMediaTypeForWaveFormat(waveFormat);
            bool supported = SetOutputType(outputStreamIndex, mediaType, DmoSetTypeFlags.DMO_SET_TYPEF_TEST_ONLY);
            DmoInterop.MoFreeMediaType(ref mediaType);
            return supported;
        }

        /// <summary>
        /// Sets the output type for the specified output stream using the provided media type.
        /// </summary>
        /// <param name="outputStreamIndex">The index of the output stream for which the media type is to be set.</param>
        /// <param name="mediaType">The media type to be set for the output stream.</param>
        /// <exception cref="ArgumentException">Thrown when the specified media type is not supported.</exception>
        private bool SetOutputType(int outputStreamIndex, DmoMediaType mediaType, DmoSetTypeFlags flags)
        {
            int hresult = mediaObject.SetOutputType(outputStreamIndex, ref mediaType, flags);
            if (hresult == (int)DmoHResults.DMO_E_TYPE_NOT_ACCEPTED)
            {
                return false;
            }
            else if (hresult == HResult.S_OK)
            {
                return true;
            }
            else
            {
                throw Marshal.GetExceptionForHR(hresult);
            }
        }

        /// <summary>
        /// Sets the output type
        /// n.b. may need to set the input type first
        /// </summary>
        /// <param name="outputStreamIndex">Output stream index</param>
        /// <param name="mediaType">Media type to set</param>
        public void SetOutputType(int outputStreamIndex, DmoMediaType mediaType)
        {
            if (!SetOutputType(outputStreamIndex, mediaType, DmoSetTypeFlags.None))
            {
                throw new ArgumentException("Media Type not supported");
            }
        }

        /// <summary>
        /// Sets the output wave format for the specified output stream.
        /// </summary>
        /// <param name="outputStreamIndex">The index of the output stream for which the wave format is to be set.</param>
        /// <param name="waveFormat">The wave format to be set for the output stream.</param>
        /// <exception cref="ArgumentException">Thrown when the specified media type is not supported.</exception>
        public void SetOutputWaveFormat(int outputStreamIndex, WaveFormat waveFormat)
        {
            DmoMediaType mediaType = CreateDmoMediaTypeForWaveFormat(waveFormat);
            bool succeeded = SetOutputType(outputStreamIndex, mediaType, DmoSetTypeFlags.None);
            DmoInterop.MoFreeMediaType(ref mediaType);
            if (!succeeded)
            {
                throw new ArgumentException("Media Type not supported");
            }
        }

        /// <summary>
        /// Retrieves the size information for the input stream at the specified index.
        /// </summary>
        /// <param name="inputStreamIndex">The index of the input stream for which to retrieve size information.</param>
        /// <returns>An instance of <see cref="MediaObjectSizeInfo"/> containing the size, max lookahead, and alignment information for the input stream.</returns>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when an error occurs while retrieving the input size information from the media object.</exception>
        public MediaObjectSizeInfo GetInputSizeInfo(int inputStreamIndex)
        {
            int size;
            int maxLookahead;
            int alignment;
            Marshal.ThrowExceptionForHR(mediaObject.GetInputSizeInfo(inputStreamIndex, out size, out maxLookahead, out alignment));
            return new MediaObjectSizeInfo(size, maxLookahead, alignment);
        }

        /// <summary>
        /// Retrieves the size information for the specified output stream.
        /// </summary>
        /// <param name="outputStreamIndex">The index of the output stream for which to retrieve size information.</param>
        /// <returns>A <see cref="MediaObjectSizeInfo"/> object containing the size and alignment information for the specified output stream.</returns>
        /// <exception cref="MarshalDirectiveException">Thrown when an HRESULT indicates a failed COM method call.</exception>
        public MediaObjectSizeInfo GetOutputSizeInfo(int outputStreamIndex)
        {
            int size;
            int alignment;
            Marshal.ThrowExceptionForHR(mediaObject.GetOutputSizeInfo(outputStreamIndex, out size, out alignment));
            return new MediaObjectSizeInfo(size, 0, alignment);
        }

        /// <summary>
        /// Processes the input data using the specified media buffer and flags.
        /// </summary>
        /// <param name="inputStreamIndex">The index of the input stream.</param>
        /// <param name="mediaBuffer">The media buffer containing the input data.</param>
        /// <param name="flags">The flags indicating how the input data should be processed.</param>
        /// <param name="timestamp">The timestamp of the input data.</param>
        /// <param name="duration">The duration of the input data.</param>
        /// <exception cref="System.Runtime.InteropServices.COMException">
        /// Thrown when an error occurs during processing the input data using the media object.
        /// </exception>
        public void ProcessInput(int inputStreamIndex, IMediaBuffer mediaBuffer, DmoInputDataBufferFlags flags,
            long timestamp, long duration)
        {
            Marshal.ThrowExceptionForHR(mediaObject.ProcessInput(inputStreamIndex, mediaBuffer, flags, timestamp, duration));
        }

        /// <summary>
        /// Processes the output data buffers using the specified flags and output buffer count.
        /// </summary>
        /// <param name="flags">The flags that specify the processing behavior.</param>
        /// <param name="outputBufferCount">The number of output data buffers.</param>
        /// <param name="outputBuffers">An array of DmoOutputDataBuffer objects containing the output data.</param>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when an error occurs during the processing of output data.</exception>
        /// <remarks>
        /// This method processes the output data buffers using the specified flags and output buffer count. It throws a COMException if an error occurs during the processing.
        /// </remarks>
        public void ProcessOutput(DmoProcessOutputFlags flags, int outputBufferCount, DmoOutputDataBuffer[] outputBuffers)
        {
            int reserved;
            Marshal.ThrowExceptionForHR(mediaObject.ProcessOutput(flags, outputBufferCount, outputBuffers, out reserved));
        }

        /// <summary>
        /// Allocates streaming resources for the media object.
        /// </summary>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when an error occurs during the allocation of streaming resources.</exception>
        public void AllocateStreamingResources()
        {
            Marshal.ThrowExceptionForHR(mediaObject.AllocateStreamingResources());
        }

        /// <summary>
        /// Frees the streaming resources used by the media object.
        /// </summary>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when an error occurs while freeing the streaming resources.</exception>
        public void FreeStreamingResources()
        {
            Marshal.ThrowExceptionForHR(mediaObject.FreeStreamingResources());
        }

        /// <summary>
        /// Retrieves the maximum latency for the specified input stream.
        /// </summary>
        /// <param name="inputStreamIndex">The index of the input stream for which to retrieve the maximum latency.</param>
        /// <returns>The maximum latency for the specified input stream.</returns>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when an error occurs while retrieving the maximum latency from the media object.</exception>
        public long GetInputMaxLatency(int inputStreamIndex)
        {
            long maxLatency;
            Marshal.ThrowExceptionForHR(mediaObject.GetInputMaxLatency(inputStreamIndex, out maxLatency));
            return maxLatency;
        }

        /// <summary>
        /// Flushes the media object.
        /// </summary>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when an error is encountered while flushing the media object.</exception>
        public void Flush()
        {
            Marshal.ThrowExceptionForHR(mediaObject.Flush());
        }

        /// <summary>
        /// Notifies the media object that a discontinuity has occurred in the input stream at the specified index.
        /// </summary>
        /// <param name="inputStreamIndex">The index of the input stream where the discontinuity has occurred.</param>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when an error occurs while notifying the media object about the discontinuity.</exception>
        public void Discontinuity(int inputStreamIndex)
        {
            Marshal.ThrowExceptionForHR(mediaObject.Discontinuity(inputStreamIndex));
        }

        /// <summary>
        /// Checks if the specified input stream is accepting data.
        /// </summary>
        /// <param name="inputStreamIndex">The index of the input stream to be checked.</param>
        /// <returns>True if the input stream is accepting data; otherwise, false.</returns>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when an error occurs while getting the input status from the media object.</exception>
        public bool IsAcceptingData(int inputStreamIndex)
        {
            DmoInputStatusFlags flags;
            int hresult = mediaObject.GetInputStatus(inputStreamIndex, out flags);
            Marshal.ThrowExceptionForHR(hresult);
            return (flags & DmoInputStatusFlags.DMO_INPUT_STATUSF_ACCEPT_DATA) == DmoInputStatusFlags.DMO_INPUT_STATUSF_ACCEPT_DATA;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the media object.
        /// </summary>
        /// <remarks>
        /// This method releases the unmanaged resources used by the media object by calling the <see cref="Marshal.ReleaseComObject(object)"/> method.
        /// If the media object is not null, it releases the resources and sets the media object to null.
        /// </remarks>
        public void Dispose()
        {
            if (mediaObject != null)
            {
                Marshal.ReleaseComObject(mediaObject);
                mediaObject = null;
            }
        }

        #endregion
    }
}
