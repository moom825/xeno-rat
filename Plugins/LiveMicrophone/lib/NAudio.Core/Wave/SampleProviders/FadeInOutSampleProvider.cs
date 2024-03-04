namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Sample Provider to allow fading in and out
    /// </summary>
    public class FadeInOutSampleProvider : ISampleProvider
    {
        enum FadeState
        {
            Silence,
            FadingIn,
            FullVolume,
            FadingOut,
        }

        private readonly object lockObject = new object();
        private readonly ISampleProvider source;
        private int fadeSamplePosition;
        private int fadeSampleCount;
        private FadeState fadeState;

        /// <summary>
        /// Creates a new FadeInOutSampleProvider
        /// </summary>
        /// <param name="source">The source stream with the audio to be faded in or out</param>
        /// <param name="initiallySilent">If true, we start faded out</param>
        public FadeInOutSampleProvider(ISampleProvider source, bool initiallySilent = false)
        {
            this.source = source;
            fadeState = initiallySilent ? FadeState.Silence : FadeState.FullVolume;
        }

        /// <summary>
        /// Begins fading in the audio with the specified duration in milliseconds.
        /// </summary>
        /// <param name="fadeDurationInMilliseconds">The duration in milliseconds for the fade-in effect.</param>
        /// <remarks>
        /// This method initiates the fade-in effect for the audio by setting the initial sample position to 0 and calculating the total sample count based on the provided fade duration.
        /// The fade state is set to "FadingIn" to indicate that the audio is currently in the process of fading in.
        /// </remarks>
        public void BeginFadeIn(double fadeDurationInMilliseconds)
        {
            lock (lockObject)
            {
                fadeSamplePosition = 0;
                fadeSampleCount = (int)((fadeDurationInMilliseconds * source.WaveFormat.SampleRate) / 1000);
                fadeState = FadeState.FadingIn;
            }
        }

        /// <summary>
        /// Initiates a fade-out effect with the specified duration.
        /// </summary>
        /// <param name="fadeDurationInMilliseconds">The duration of the fade-out effect in milliseconds.</param>
        /// <remarks>
        /// This method locks the <paramref name="lockObject"/> to ensure thread safety and sets the <paramref name="fadeSamplePosition"/> to 0.
        /// It calculates the <paramref name="fadeSampleCount"/> based on the fade duration and the sample rate of the audio source.
        /// The method then sets the <paramref name="fadeState"/> to indicate that the audio is in the process of fading out.
        /// </remarks>
        public void BeginFadeOut(double fadeDurationInMilliseconds)
        {
            lock (lockObject)
            {
                fadeSamplePosition = 0;
                fadeSampleCount = (int)((fadeDurationInMilliseconds * source.WaveFormat.SampleRate) / 1000);
                fadeState = FadeState.FadingOut;
            }
        }

        /// <summary>
        /// Reads audio samples from the source into the buffer and applies any active fade effects.
        /// </summary>
        /// <param name="buffer">The buffer to read the audio samples into.</param>
        /// <param name="offset">The zero-based offset in the buffer at which to begin storing the data.</param>
        /// <param name="count">The maximum number of samples to read.</param>
        /// <returns>The actual number of samples read from the source and stored in the buffer.</returns>
        /// <remarks>
        /// This method reads audio samples from the source into the specified buffer starting at the given offset and up to the specified count.
        /// If any fade effect is active (fading in, fading out, or silence), it applies the corresponding effect to the samples in the buffer.
        /// The sourceSamplesRead variable holds the number of samples actually read from the source.
        /// The method then returns the number of samples read from the source and stored in the buffer.
        /// </remarks>
        public int Read(float[] buffer, int offset, int count)
        {
            int sourceSamplesRead = source.Read(buffer, offset, count);
            lock (lockObject)
            {
                if (fadeState == FadeState.FadingIn)
                {
                    FadeIn(buffer, offset, sourceSamplesRead);
                }
                else if (fadeState == FadeState.FadingOut)
                {
                    FadeOut(buffer, offset, sourceSamplesRead);
                }
                else if (fadeState == FadeState.Silence)
                {
                    ClearBuffer(buffer, offset, count);
                }
            }
            return sourceSamplesRead;
        }

        /// <summary>
        /// Clears a portion of the buffer by setting the specified range of elements to zero.
        /// </summary>
        /// <param name="buffer">The buffer to be cleared.</param>
        /// <param name="offset">The starting index of the range to be cleared.</param>
        /// <param name="count">The number of elements to be cleared.</param>
        /// <remarks>
        /// This method sets the elements in the specified range of the input buffer <paramref name="buffer"/> to zero.
        /// The range to be cleared starts at the index <paramref name="offset"/> and includes <paramref name="count"/> elements.
        /// </remarks>
        private static void ClearBuffer(float[] buffer, int offset, int count)
        {
            for (int n = 0; n < count; n++)
            {
                buffer[n + offset] = 0;
            }
        }

        /// <summary>
        /// Fades out the audio buffer by applying a multiplier to each sample based on the fade position and count.
        /// </summary>
        /// <param name="buffer">The audio buffer to be faded out.</param>
        /// <param name="offset">The offset within the buffer where fading should start.</param>
        /// <param name="sourceSamplesRead">The number of samples read from the source.</param>
        /// <remarks>
        /// This method iterates through the audio buffer and applies a multiplier to each sample based on the fade position and count.
        /// The multiplier is calculated as 1.0f minus the ratio of fadeSamplePosition to fadeSampleCount.
        /// For each channel in the audio buffer, the sample at the specified offset is multiplied by the calculated multiplier.
        /// The fadeSamplePosition is incremented after each iteration, and if it exceeds the fadeSampleCount, the fadeState is set to FadeState.Silence.
        /// Additionally, if the fadeSamplePosition exceeds the fadeSampleCount, the method clears out the remaining samples in the buffer by calling the ClearBuffer method.
        /// </remarks>
        private void FadeOut(float[] buffer, int offset, int sourceSamplesRead)
        {
            int sample = 0;
            while (sample < sourceSamplesRead)
            {
                float multiplier = 1.0f - (fadeSamplePosition / (float)fadeSampleCount);
                for (int ch = 0; ch < source.WaveFormat.Channels; ch++)
                {
                    buffer[offset + sample++] *= multiplier;
                }
                fadeSamplePosition++;
                if (fadeSamplePosition > fadeSampleCount)
                {
                    fadeState = FadeState.Silence;
                    // clear out the end
                    ClearBuffer(buffer, sample + offset, sourceSamplesRead - sample);
                    break;
                }
            }
        }

        /// <summary>
        /// Fades in the audio buffer by multiplying each sample with a multiplier that increases gradually from 0 to 1.
        /// </summary>
        /// <param name="buffer">The audio buffer to be faded in.</param>
        /// <param name="offset">The offset in the buffer where the fading should start.</param>
        /// <param name="sourceSamplesRead">The number of samples read from the source.</param>
        /// <remarks>
        /// This method modifies the input audio buffer in place by gradually increasing the amplitude of the audio samples from the specified offset.
        /// The fading is achieved by multiplying each sample with a multiplier that increases gradually from 0 to 1.
        /// The fading process stops when the fadeSamplePosition exceeds the fadeSampleCount, at which point the fadeState is set to FullVolume.
        /// </remarks>
        private void FadeIn(float[] buffer, int offset, int sourceSamplesRead)
        {
            int sample = 0;
            while (sample < sourceSamplesRead)
            {
                float multiplier = (fadeSamplePosition / (float)fadeSampleCount);
                for (int ch = 0; ch < source.WaveFormat.Channels; ch++)
                {
                    buffer[offset + sample++] *= multiplier;
                }
                fadeSamplePosition++;
                if (fadeSamplePosition > fadeSampleCount)
                {
                    fadeState = FadeState.FullVolume;
                    // no need to multiply any more
                    break;
                }
            }
        }

        /// <summary>
        /// WaveFormat of this SampleProvider
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return source.WaveFormat; }
        }
    }
}
