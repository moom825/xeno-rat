using System;
using NAudio.Wave.SampleProviders;

namespace NAudio.Wave
{
    /// <summary>
    /// Useful extension methods to make switching between WaveAndSampleProvider easier
    /// </summary>
    public static class WaveExtensionMethods
    {

        /// <summary>
        /// Converts the specified <paramref name="waveProvider"/> into a sample provider.
        /// </summary>
        /// <param name="waveProvider">The wave provider to be converted.</param>
        /// <returns>A sample provider representing the converted <paramref name="waveProvider"/>.</returns>
        public static ISampleProvider ToSampleProvider(this IWaveProvider waveProvider)
        {
            return SampleProviderConverters.ConvertWaveProviderIntoSampleProvider(waveProvider);
        }

        /// <summary>
        /// Initializes the specified wave player with the given sample provider and optional 16-bit conversion.
        /// </summary>
        /// <param name="wavePlayer">The wave player to be initialized.</param>
        /// <param name="sampleProvider">The sample provider to be used for audio playback.</param>
        /// <param name="convertTo16Bit">Optional parameter to indicate whether to convert the samples to 16-bit format. Default is false.</param>
        /// <remarks>
        /// This method initializes the specified <paramref name="wavePlayer"/> with the provided <paramref name="sampleProvider"/> for audio playback.
        /// If <paramref name="convertTo16Bit"/> is true, the sample provider is converted to 16-bit format using SampleToWaveProvider16; otherwise, it uses SampleToWaveProvider.
        /// </remarks>
        public static void Init(this IWavePlayer wavePlayer, ISampleProvider sampleProvider, bool convertTo16Bit = false)
        {
            IWaveProvider provider = convertTo16Bit ? (IWaveProvider)new SampleToWaveProvider16(sampleProvider) : new SampleToWaveProvider(sampleProvider);
            wavePlayer.Init(provider);
        }

        /// <summary>
        /// Converts the input WaveFormat to standard WaveFormat and returns the result.
        /// </summary>
        /// <param name="waveFormat">The WaveFormat to be converted.</param>
        /// <returns>The standard WaveFormat if the input is of type WaveFormatExtensible; otherwise, returns the input WaveFormat.</returns>
        /// <remarks>
        /// This method checks if the input <paramref name="waveFormat"/> is of type WaveFormatExtensible. If it is, it calls the ToStandardWaveFormat method to convert it to standard WaveFormat and returns the result. If the input is not of type WaveFormatExtensible, it returns the input WaveFormat as is.
        /// </remarks>
        public static WaveFormat AsStandardWaveFormat(this WaveFormat waveFormat)
        {
            var wfe = waveFormat as WaveFormatExtensible;
            return wfe != null ? wfe.ToStandardWaveFormat() : waveFormat;
        }

        /// <summary>
        /// Converts the given ISampleProvider to an IWaveProvider.
        /// </summary>
        /// <param name="sampleProvider">The ISampleProvider to be converted.</param>
        /// <returns>An IWaveProvider representing the converted sample provider.</returns>
        public static IWaveProvider ToWaveProvider(this ISampleProvider sampleProvider)
        {
            return new SampleToWaveProvider(sampleProvider);
        }

        /// <summary>
        /// Converts the given ISampleProvider to a 16-bit IWaveProvider.
        /// </summary>
        /// <param name="sampleProvider">The input ISampleProvider to be converted.</param>
        /// <returns>A 16-bit IWaveProvider representing the converted sampleProvider.</returns>
        public static IWaveProvider ToWaveProvider16(this ISampleProvider sampleProvider)
        {
            return new SampleToWaveProvider16(sampleProvider);
        }

        /// <summary>
        /// Appends a silence duration to the input sample provider, followed by another sample provider.
        /// </summary>
        /// <param name="sampleProvider">The input sample provider.</param>
        /// <param name="silenceDuration">The duration of silence to be appended.</param>
        /// <param name="next">The sample provider to be appended after the silence duration.</param>
        /// <returns>A new sample provider with the silence duration followed by the next sample provider.</returns>
        /// <remarks>
        /// This method appends a specified duration of silence to the input sample provider, followed by another sample provider.
        /// The resulting sample provider contains the input sample provider with the specified silence duration followed by the next sample provider.
        /// </remarks>
        public static ISampleProvider FollowedBy(this ISampleProvider sampleProvider, ISampleProvider next)
        {
            return new ConcatenatingSampleProvider(new[] { sampleProvider, next});
        }

        /// <summary>
        /// Concatenates one Sample Provider on the end of another with silence inserted
        /// </summary>
        /// <param name="sampleProvider">The sample provider to play first</param>
        /// <param name="silenceDuration">Silence duration to insert between the two</param>
        /// <param name="next">The sample provider to play next</param>
        /// <returns>A single sample provider</returns>
        public static ISampleProvider FollowedBy(this ISampleProvider sampleProvider, TimeSpan silenceDuration, ISampleProvider next)
        {
            var silenceAppended = new OffsetSampleProvider(sampleProvider) {LeadOut = silenceDuration};
            return new ConcatenatingSampleProvider(new[] { silenceAppended, next });
        }

        /// <summary>
        /// Skips the specified duration of audio samples from the beginning of the input sample provider.
        /// </summary>
        /// <param name="sampleProvider">The input sample provider.</param>
        /// <param name="skipDuration">The duration of audio samples to skip.</param>
        /// <returns>A new sample provider with the specified duration of audio samples skipped from the beginning.</returns>
        /// <remarks>
        /// This method creates a new sample provider that skips the specified duration of audio samples from the beginning of the input sample provider.
        /// The original input sample provider remains unmodified.
        /// </remarks>
        public static ISampleProvider Skip(this ISampleProvider sampleProvider, TimeSpan skipDuration)
        {
            return new OffsetSampleProvider(sampleProvider) { SkipOver = skipDuration};            
        }

        /// <summary>
        /// Takes a specified duration of audio from the input sample provider.
        /// </summary>
        /// <param name="sampleProvider">The input sample provider.</param>
        /// <param name="takeDuration">The duration of audio to be taken from the input sample provider.</param>
        /// <returns>A new sample provider that provides audio for the specified duration from the input sample provider.</returns>
        public static ISampleProvider Take(this ISampleProvider sampleProvider, TimeSpan takeDuration)
        {
            return new OffsetSampleProvider(sampleProvider) { Take = takeDuration };
        }

        /// <summary>
        /// Converts a stereo audio sample provider to mono with specified left and right volume levels.
        /// </summary>
        /// <param name="sourceProvider">The stereo audio sample provider to be converted to mono.</param>
        /// <param name="leftVol">The volume level for the left channel (default is 0.5).</param>
        /// <param name="rightVol">The volume level for the right channel (default is 0.5).</param>
        /// <returns>A mono audio sample provider with the specified left and right volume levels.</returns>
        /// <remarks>
        /// This method checks if the input audio sample provider has stereo channels. If it does, it converts it to mono by creating a new instance of the StereoToMonoSampleProvider class with the specified left and right volume levels.
        /// If the input audio sample provider already has only one channel, it returns the original provider without any modifications.
        /// </remarks>
        public static ISampleProvider ToMono(this ISampleProvider sourceProvider, float leftVol = 0.5f, float rightVol = 0.5f)
        {
            if(sourceProvider.WaveFormat.Channels == 1) return sourceProvider;
            return new StereoToMonoSampleProvider(sourceProvider) {LeftVolume = leftVol, RightVolume = rightVol};
        }

        /// <summary>
        /// Converts a mono audio sample provider to a stereo audio sample provider with specified left and right volume levels.
        /// </summary>
        /// <param name="sourceProvider">The mono audio sample provider to be converted to stereo.</param>
        /// <param name="leftVol">The volume level for the left channel (default is 1.0).</param>
        /// <param name="rightVol">The volume level for the right channel (default is 1.0).</param>
        /// <returns>
        /// A stereo audio sample provider with the specified left and right volume levels.
        /// </returns>
        public static ISampleProvider ToStereo(this ISampleProvider sourceProvider, float leftVol = 1.0f, float rightVol = 1.0f)
        {
            if (sourceProvider.WaveFormat.Channels == 2) return sourceProvider;
            return new MonoToStereoSampleProvider(sourceProvider) { LeftVolume = leftVol, RightVolume = rightVol };
        }
    }
}
