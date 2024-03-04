using System;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Takes a stereo input and turns it to mono
    /// </summary>
    public class StereoToMonoSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider sourceProvider;
        private float[] sourceBuffer;

        /// <summary>
        /// Creates a new mono ISampleProvider based on a stereo input
        /// </summary>
        /// <param name="sourceProvider">Stereo 16 bit PCM input</param>
        public StereoToMonoSampleProvider(ISampleProvider sourceProvider)
        {
            LeftVolume = 0.5f;
            RightVolume = 0.5f;
            if (sourceProvider.WaveFormat.Channels != 2)
            {
                throw new ArgumentException("Source must be stereo");
            }
            this.sourceProvider = sourceProvider;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sourceProvider.WaveFormat.SampleRate, 1);
        }

        /// <summary>
        /// 1.0 to mix the mono source entirely to the left channel
        /// </summary>
        public float LeftVolume { get; set; } 

        /// <summary>
        /// 1.0 to mix the mono source entirely to the right channel
        /// </summary>
        public float RightVolume { get; set; }

        /// <summary>
        /// Output Wave Format
        /// </summary>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// Reads audio samples from the source provider and processes them to fill the buffer with the mixed output.
        /// </summary>
        /// <param name="buffer">The buffer to be filled with the mixed audio samples.</param>
        /// <param name="offset">The zero-based index in the buffer at which to begin storing the mixed audio samples.</param>
        /// <param name="count">The number of mixed audio samples to read and process.</param>
        /// <returns>The number of mixed audio samples read and processed, which is half the number of source samples read.</returns>
        /// <remarks>
        /// This method reads audio samples from the source provider, mixes them based on the left and right volume settings, and stores the mixed output in the buffer starting at the specified offset.
        /// If the source buffer is null or smaller than required, a new buffer is created with the necessary size.
        /// The method iterates through the source samples, applies volume mixing, and stores the mixed output in the buffer.
        /// </remarks>
        public int Read(float[] buffer, int offset, int count)
        {
            var sourceSamplesRequired = count * 2;
            if (sourceBuffer == null || sourceBuffer.Length < sourceSamplesRequired) sourceBuffer = new float[sourceSamplesRequired];

            var sourceSamplesRead = sourceProvider.Read(sourceBuffer, 0, sourceSamplesRequired);
            var destOffset = offset;
            for (var sourceSample = 0; sourceSample < sourceSamplesRead; sourceSample += 2)
            {
                var left = sourceBuffer[sourceSample];
                var right = sourceBuffer[sourceSample + 1];
                var outSample = (left * LeftVolume) + (right * RightVolume);

                buffer[destOffset++] = outSample;
            }
            return sourceSamplesRead / 2;
        }
    }
}