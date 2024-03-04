using System;
using System.Runtime.InteropServices;

namespace NAudio.Dmo
{
    /// <summary>
    /// Media Object InPlace
    /// </summary>
    public class MediaObjectInPlace : IDisposable
    {
        private IMediaObjectInPlace mediaObjectInPlace;

        /// <summary>
        /// Creates a new Media Object InPlace
        /// </summary>
        /// <param name="mediaObjectInPlace">Media Object InPlace COM Interface</param>
        internal MediaObjectInPlace(IMediaObjectInPlace mediaObjectInPlace)
        {
            this.mediaObjectInPlace = mediaObjectInPlace;
        }

        /// <summary>
        /// Processes the input data in place using the specified Direct Media Object (DMO) and returns the result.
        /// </summary>
        /// <param name="size">The size of the data to be processed.</param>
        /// <param name="offset">The offset within the <paramref name="data"/> array where the processing should start.</param>
        /// <param name="data">The input data array.</param>
        /// <param name="timeStart">The start time for the processing operation.</param>
        /// <param name="inPlaceFlag">The flags indicating the type of in-place processing to be performed.</param>
        /// <returns>The result of processing the input data using the specified DMO.</returns>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when an error occurs during the processing operation.</exception>
        public DmoInPlaceProcessReturn Process(int size, int offset, byte[] data, long timeStart, DmoInPlaceProcessFlags inPlaceFlag)
        {
            var pointer = Marshal.AllocHGlobal(size);
            Marshal.Copy(data, offset, pointer, size);

            var result = mediaObjectInPlace.Process(size, pointer, timeStart, inPlaceFlag);
            Marshal.ThrowExceptionForHR(result);

            Marshal.Copy(pointer, data, offset, size);
            Marshal.FreeHGlobal(pointer);

            return (DmoInPlaceProcessReturn) result;
        }

        /// <summary>
        /// Clones the current MediaObjectInPlace and returns the cloned object.
        /// </summary>
        /// <returns>A new MediaObjectInPlace that is a clone of the current object.</returns>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when an error is encountered during the cloning process.</exception>
        public MediaObjectInPlace Clone()
        {
            Marshal.ThrowExceptionForHR(this.mediaObjectInPlace.Clone(out var cloneObj));
            return new MediaObjectInPlace(cloneObj);
        }

        /// <summary>
        /// Retrieves the latency time of the media object in place.
        /// </summary>
        /// <returns>The latency time of the media object in place.</returns>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when an error is encountered while retrieving the latency time.</exception>
        public long GetLatency()
        {
            Marshal.ThrowExceptionForHR(this.mediaObjectInPlace.GetLatency(out var latencyTime));
            return latencyTime;
        }

        /// <summary>
        /// Returns a new instance of MediaObject based on the existing mediaObjectInPlace.
        /// </summary>
        /// <returns>A new MediaObject instance based on the existing mediaObjectInPlace.</returns>
        public MediaObject GetMediaObject()
        {
            return new MediaObject((IMediaObject) mediaObjectInPlace);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the MediaObjectInPlace and optionally releases the managed resources.
        /// </summary>
        /// <remarks>
        /// This method releases the unmanaged resources used by the MediaObjectInPlace and optionally releases the managed resources.
        /// It is a good practice to call this method when you have finished using the MediaObjectInPlace.
        /// This method ensures that the MediaObjectInPlace is properly disposed of and releases all resources associated with it.
        /// </remarks>
        public void Dispose()
        {
            if (mediaObjectInPlace != null)
            {
                Marshal.ReleaseComObject(mediaObjectInPlace);
                mediaObjectInPlace = null;
            }
        }
    }
}