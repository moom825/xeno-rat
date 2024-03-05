using System;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// No nonsense mono to stereo provider, no volume adjustment,
    /// just copies input to left and right. 
    /// </summary>
    public class MonoToStereoSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private float[] sourceBuffer;

        /// <summary>
        /// Initializes a new instance of MonoToStereoSampleProvider
        /// </summary>
        /// <param name="source">Source sample provider</param>
        public MonoToStereoSampleProvider(ISampleProvider source)
        {
            LeftVolume = 1.0f;
            RightVolume = 1.0f;
            if (source.WaveFormat.Channels != 1)
            {
                throw new ArgumentException("Source must be mono");
            }
            this.source = source;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 2);
        }

        /// <summary>
        /// WaveFormat of this provider
        /// </summary>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// Reads audio samples from the source and stores them in the buffer.
        /// </summary>
        /// <param name="buffer">The buffer to store the audio samples.</param>
        /// <param name="offset">The zero-based index in the buffer at which to begin storing the samples.</param>
        /// <param name="count">The number of samples to read.</param>
        /// <returns>The total number of samples read and stored in the buffer.</returns>
        /// <remarks>
        /// This method reads audio samples from the source, applies left and right volume adjustments, and stores them in the buffer.
        /// It ensures that the source buffer has enough space to accommodate the required number of samples.
        /// The method then reads the source samples, applies volume adjustments, and stores them in the buffer at the specified offset.
        /// The method returns the total number of samples read and stored in the buffer, which is twice the number of source samples read.
        /// </remarks>
        public int Read(float[] buffer, int offset, int count)
        {
            var sourceSamplesRequired = count / 2;
            var outIndex = offset;
            EnsureSourceBuffer(sourceSamplesRequired);
            var sourceSamplesRead = source.Read(sourceBuffer, 0, sourceSamplesRequired);
            for (var n = 0; n < sourceSamplesRead; n++)
            {
                buffer[outIndex++] = sourceBuffer[n] * LeftVolume;
                buffer[outIndex++] = sourceBuffer[n] * RightVolume;
            }
            return sourceSamplesRead * 2;
        }

        /// <summary>
        /// Multiplier for left channel (default is 1.0)
        /// </summary>
        public float LeftVolume { get; set; }

        /// <summary>
        /// Multiplier for right channel (default is 1.0)
        /// </summary>
        public float RightVolume { get; set; }

        /// <summary>
        /// Ensures that the source buffer has a minimum capacity of <paramref name="count"/>.
        /// If the current buffer is null or has a length less than <paramref name="count"/>, a new buffer of size <paramref name="count"/> is created.
        /// </summary>
        /// <param name="count">The minimum capacity required for the source buffer.</param>
        /// <remarks>
        /// This method is used to ensure that the source buffer has enough capacity to accommodate a specified number of elements.
        /// If the current buffer is null or has a length less than <paramref name="count"/>, a new buffer of size <paramref name="count"/> is created.
        /// </remarks>
        private void EnsureSourceBuffer(int count)
        {
            if (sourceBuffer == null || sourceBuffer.Length < count)
            {
                sourceBuffer = new float[count];
            }
        }
    }
}
