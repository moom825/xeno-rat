using System;
using System.Runtime.InteropServices;
using NAudio.Dmo;
using NAudio.MediaFoundation;

namespace NAudio.Wave
{
    /// <summary>
    /// The Media Foundation Resampler Transform
    /// </summary>
    public class MediaFoundationResampler : MediaFoundationTransform
    {
        private int resamplerQuality;

        /// <summary>
        /// Checks if the given WaveFormat is PCM or IEEE Float and returns a boolean value.
        /// </summary>
        /// <param name="waveFormat">The WaveFormat to be checked.</param>
        /// <returns>True if the WaveFormat is PCM or IEEE Float, otherwise False.</returns>
        private static bool IsPcmOrIeeeFloat(WaveFormat waveFormat)
        {
            var wfe = waveFormat as WaveFormatExtensible;
            return waveFormat.Encoding == WaveFormatEncoding.Pcm ||
                   waveFormat.Encoding == WaveFormatEncoding.IeeeFloat ||
                   (wfe != null && (wfe.SubFormat == AudioSubtypes.MFAudioFormat_PCM
                                    || wfe.SubFormat == AudioSubtypes.MFAudioFormat_Float));
        }

        /// <summary>
        /// Creates the Media Foundation Resampler, allowing modifying of sample rate, bit depth and channel count
        /// </summary>
        /// <param name="sourceProvider">Source provider, must be PCM</param>
        /// <param name="outputFormat">Output format, must also be PCM</param>
        public MediaFoundationResampler(IWaveProvider sourceProvider, WaveFormat outputFormat)
            : base(sourceProvider, outputFormat)
        {
            if (!IsPcmOrIeeeFloat(sourceProvider.WaveFormat))
                throw new ArgumentException("Input must be PCM or IEEE float", "sourceProvider");
            if (!IsPcmOrIeeeFloat(outputFormat))
                throw new ArgumentException("Output must be PCM or IEEE float", "outputFormat");
            MediaFoundationApi.Startup();
            ResamplerQuality = 60; // maximum quality

            // n.b. we will create the resampler COM object on demand in the Read method, 
            // to avoid threading issues but just
            // so we can check it exists on the system we'll make one so it will throw an 
            // exception if not exists
            var comObject = CreateResamplerComObject();
            FreeComObject(comObject);
        }

        private static readonly Guid ResamplerClsid = new Guid("f447b69e-1884-4a7e-8055-346f74d6edb3");
        private static readonly Guid IMFTransformIid = new Guid("bf94c121-5b05-4e6f-8000-ba598961414d");
        private IMFActivate activate;

        /// <summary>
        /// Releases the specified COM object from memory.
        /// </summary>
        /// <param name="comObject">The COM object to be released from memory.</param>
        /// <exception cref="NullReferenceException">Thrown when the <paramref name="comObject"/> is null.</exception>
        /// <remarks>
        /// This method releases the specified COM object from memory by calling the <see cref="Marshal.ReleaseComObject"/> method.
        /// Additionally, if the <see cref="activate"/> object is not null, it calls the <see cref="activate.ShutdownObject"/> method to perform any necessary cleanup.
        /// </remarks>
        private void FreeComObject(object comObject)
        {
            if (activate != null) activate.ShutdownObject();
            Marshal.ReleaseComObject(comObject);
        }

        /// <summary>
        /// Creates and returns a resampler COM object based on the platform.
        /// </summary>
        /// <returns>
        /// A resampler COM object based on the platform. Returns an instance of <see cref="ResamplerMediaComObject"/> for non-UWP platforms and uses <see cref="CreateResamplerComObjectUsingActivator"/> for UWP platforms.
        /// </returns>
        private object CreateResamplerComObject()
        {
#if NETFX_CORE            
            return CreateResamplerComObjectUsingActivator();
#else
            return new ResamplerMediaComObject();
#endif
        }

        /// <summary>
        /// Creates a resampler COM object using the Activator.
        /// </summary>
        /// <returns>
        /// The resampler COM object created using the Activator, or null if no matching object is found.
        /// </returns>
        /// <remarks>
        /// This method enumerates the audio effect transforms using MediaFoundationApi.EnumerateTransforms and searches for the resampler using the ResamplerClsid.
        /// If a matching resampler is found, it activates the object using the activator and returns the created COM object.
        /// </remarks>
        private object CreateResamplerComObjectUsingActivator()
        {
            var transformActivators = MediaFoundationApi.EnumerateTransforms(MediaFoundationTransformCategories.AudioEffect);
            foreach (var activator in transformActivators)
            {
                Guid clsid;
                activator.GetGUID(MediaFoundationAttributes.MFT_TRANSFORM_CLSID_Attribute, out clsid);
                if (clsid.Equals(ResamplerClsid))
                {
                    object comObject;
                    activator.ActivateObject(IMFTransformIid, out comObject);
                    activate = activator;
                    return comObject;
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a resampler with a specified target output sample rate
        /// </summary>
        /// <param name="sourceProvider">Source provider</param>
        /// <param name="outputSampleRate">Output sample rate</param>
        public MediaFoundationResampler(IWaveProvider sourceProvider, int outputSampleRate)
            : this(sourceProvider, CreateOutputFormat(sourceProvider.WaveFormat, outputSampleRate))
        {

        }

        /// <summary>
        /// Creates and configures a resampler transform for media format conversion.
        /// </summary>
        /// <returns>The configured resampler transform for media format conversion.</returns>
        /// <remarks>
        /// This method creates a resampler transform for media format conversion. It sets the input and output media formats, configures quality settings, and returns the configured resampler transform.
        /// </remarks>
        protected override IMFTransform CreateTransform()
        {
            var comObject = CreateResamplerComObject();// new ResamplerMediaComObject();
            var resamplerTransform = (IMFTransform)comObject;

            var inputMediaFormat = MediaFoundationApi.CreateMediaTypeFromWaveFormat(sourceProvider.WaveFormat);
            resamplerTransform.SetInputType(0, inputMediaFormat, 0);
            Marshal.ReleaseComObject(inputMediaFormat);

            var outputMediaFormat = MediaFoundationApi.CreateMediaTypeFromWaveFormat(outputWaveFormat);
            resamplerTransform.SetOutputType(0, outputMediaFormat, 0);
            Marshal.ReleaseComObject(outputMediaFormat);

            //MFT_OUTPUT_STREAM_INFO pStreamInfo;
            //resamplerTransform.GetOutputStreamInfo(0, out pStreamInfo);
            // if pStreamInfo.dwFlags is 0, then it means we have to provide samples

            // setup quality
            var resamplerProps = (IWMResamplerProps)comObject;
            // 60 is the best quality, 1 is linear interpolation
            resamplerProps.SetHalfFilterLength(ResamplerQuality);
            // may also be able to set this using MFPKEY_WMRESAMP_CHANNELMTX on the
            // IPropertyStore interface.
            // looks like we can also adjust the LPF with MFPKEY_WMRESAMP_LOWPASS_BANDWIDTH
            return resamplerTransform;
        }

        /// <summary>
        /// Gets or sets the Resampler quality. n.b. set the quality before starting to resample.
        /// 1 is lowest quality (linear interpolation) and 60 is best quality
        /// </summary>
        public int ResamplerQuality
        {
            get { return resamplerQuality; }
            set 
            { 
                if (value < 1 || value > 60)
                    throw new ArgumentOutOfRangeException("Resampler Quality must be between 1 and 60");
                resamplerQuality = value; 
            }
        }

        /// <summary>
        /// Creates a new WaveFormat for the output based on the input WaveFormat and the specified output sample rate.
        /// </summary>
        /// <param name="inputFormat">The input WaveFormat to be used as a basis for creating the output format.</param>
        /// <param name="outputSampleRate">The sample rate of the output WaveFormat.</param>
        /// <returns>A new WaveFormat for the output based on the input WaveFormat and the specified output sample rate.</returns>
        /// <exception cref="ArgumentException">Thrown when the inputFormat.Encoding is neither WaveFormatEncoding.Pcm nor WaveFormatEncoding.IeeeFloat.</exception>
        /// <remarks>
        /// This method creates a new WaveFormat for the output based on the input WaveFormat and the specified output sample rate.
        /// If the inputFormat.Encoding is WaveFormatEncoding.Pcm, a new WaveFormat is created with the specified output sample rate, inputFormat.BitsPerSample, and inputFormat.Channels.
        /// If the inputFormat.Encoding is WaveFormatEncoding.IeeeFloat, a new WaveFormat is created with the specified output sample rate and inputFormat.Channels using WaveFormat.CreateIeeeFloatWaveFormat method.
        /// If the inputFormat.Encoding is neither WaveFormatEncoding.Pcm nor WaveFormatEncoding.IeeeFloat, an ArgumentException is thrown with the message "Can only resample PCM or IEEE float".
        /// </remarks>
        private static WaveFormat CreateOutputFormat(WaveFormat inputFormat, int outputSampleRate)
        {
            WaveFormat outputFormat;
            if (inputFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                outputFormat = new WaveFormat(outputSampleRate,
                    inputFormat.BitsPerSample,
                    inputFormat.Channels);
            }
            else if (inputFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(outputSampleRate,
                    inputFormat.Channels);
            }
            else
            {
                throw new ArgumentException("Can only resample PCM or IEEE float");
            }
            return outputFormat;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the component and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        /// <remarks>
        /// This method releases the unmanaged resources used by the component and optionally releases the managed resources.
        /// If <paramref name="disposing"/> is true, this method invokes the <see cref="ShutdownObject"/> method on the <see cref="activate"/> object to release its resources and sets the <see cref="activate"/> object to null.
        /// </remarks>
        protected override void Dispose(bool disposing)
        {
            if (activate != null)
            {
                activate.ShutdownObject();
                activate = null;
            }

            base.Dispose(disposing);
        }

    }
}
