using System;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;

namespace NAudio.Dmo
{
    /// <summary>
    /// From wmcodecsdp.h
    /// Implements:
    /// - IMediaObject 
    /// - IMFTransform (Media foundation - we will leave this for now as there is loads of MF stuff)
    /// - IPropertyStore 
    /// - IWMResamplerProps 
    /// Can resample PCM or IEEE
    /// </summary>
    [ComImport, Guid("f447b69e-1884-4a7e-8055-346f74d6edb3")]
    class ResamplerMediaComObject
    {
    }

    /// <summary>
    /// DMO Resampler
    /// </summary>
    public class DmoResampler : IDisposable
    {
        MediaObject mediaObject;
        IPropertyStore propertyStoreInterface;
        IWMResamplerProps resamplerPropsInterface;
        ResamplerMediaComObject mediaComObject;

        /// <summary>
        /// Creates a new Resampler based on the DMO Resampler
        /// </summary>
        public DmoResampler()
        {
            mediaComObject = new ResamplerMediaComObject();
            mediaObject = new MediaObject((IMediaObject)mediaComObject);
            propertyStoreInterface = (IPropertyStore)mediaComObject;
            resamplerPropsInterface = (IWMResamplerProps)mediaComObject;
        }

        /// <summary>
        /// Media Object
        /// </summary>
        public MediaObject MediaObject => mediaObject;

        /// <summary>
        /// Releases the unmanaged resources used by the current instance.
        /// </summary>
        /// <remarks>
        /// This method releases the unmanaged resources used by the current instance, such as COM objects, and sets the reference to null to indicate that the resources have been released.
        /// </remarks>
        public void Dispose()
        {
            if(propertyStoreInterface != null)
            {
                Marshal.ReleaseComObject(propertyStoreInterface);
                propertyStoreInterface = null;
            }
            if(resamplerPropsInterface != null)
            {
                Marshal.ReleaseComObject(resamplerPropsInterface);
                resamplerPropsInterface = null;
            }
            if (mediaObject != null)
            {
                mediaObject.Dispose();
                mediaObject = null;
            }
            if (mediaComObject != null)
            {
                Marshal.ReleaseComObject(mediaComObject);
                mediaComObject = null;
            }
        }

        #endregion
    }
}
