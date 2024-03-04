using System;
using NAudio.Utils;

namespace NAudio.Wave
{
    /// <summary>
    /// Converts from mono to stereo, allowing freedom to route all, some, or none of the incoming signal to left or right channels
    /// </summary>
    public class MonoToStereoProvider16 : IWaveProvider
    {
        private readonly IWaveProvider sourceProvider;
        private byte[] sourceBuffer;

        /// <summary>
        /// Creates a new stereo waveprovider based on a mono input
        /// </summary>
        /// <param name="sourceProvider">Mono 16 bit PCM input</param>
        public MonoToStereoProvider16(IWaveProvider sourceProvider)
        {
            if (sourceProvider.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
            {
                throw new ArgumentException("Source must be PCM");
            }
            if (sourceProvider.WaveFormat.Channels != 1)
            {
                throw new ArgumentException("Source must be Mono");
            }
            if (sourceProvider.WaveFormat.BitsPerSample != 16)
            {
                throw new ArgumentException("Source must be 16 bit");
            }
            this.sourceProvider = sourceProvider;
            WaveFormat = new WaveFormat(sourceProvider.WaveFormat.SampleRate, 2);
            RightVolume = 1.0f;
            LeftVolume = 1.0f;
        }

        /// <summary>
        /// 1.0 to copy the mono stream to the left channel without adjusting volume
        /// </summary>
        public float LeftVolume { get; set; }

        /// <summary>
        /// 1.0 to copy the mono stream to the right channel without adjusting volume
        /// </summary>
        public float RightVolume { get; set; }

        /// <summary>
        /// Output Wave Format
        /// </summary>
        public WaveFormat WaveFormat { get; }

        /// <summary>
        /// Reads audio data from the source buffer and processes it to the destination buffer.
        /// </summary>
        /// <param name="buffer">The destination buffer to write the processed audio data to.</param>
        /// <param name="offset">The offset in the destination buffer to start writing the data.</param>
        /// <param name="count">The number of bytes to read from the source buffer and process.</param>
        /// <returns>The number of bytes written to the destination buffer after processing the audio data.</returns>
        /// <remarks>
        /// This method reads audio data from the source buffer, processes it by applying left and right volume adjustments, and writes the processed data to the destination buffer.
        /// It ensures that the source buffer has enough space to accommodate the required number of bytes, reads the source audio data, processes each sample by applying volume adjustments, and writes the processed data to the destination buffer.
        /// The method returns the total number of bytes written to the destination buffer after processing the audio data, which is calculated based on the number of samples read and the size of each sample.
        /// </remarks>
        public int Read(byte[] buffer, int offset, int count)
        {
            var sourceBytesRequired = count / 2;
            sourceBuffer = BufferHelpers.Ensure(this.sourceBuffer, sourceBytesRequired);
            var sourceWaveBuffer = new WaveBuffer(sourceBuffer);
            var destWaveBuffer = new WaveBuffer(buffer);

            var sourceBytesRead = sourceProvider.Read(sourceBuffer, 0, sourceBytesRequired);
            var samplesRead = sourceBytesRead / 2;
            var destOffset = offset / 2;
            for (var sample = 0; sample < samplesRead; sample++)
            {
                short sampleVal = sourceWaveBuffer.ShortBuffer[sample];
                destWaveBuffer.ShortBuffer[destOffset++] = (short)(LeftVolume * sampleVal);
                destWaveBuffer.ShortBuffer[destOffset++] = (short)(RightVolume * sampleVal);
            }
            return samplesRead * 4;
        }
    }
}
