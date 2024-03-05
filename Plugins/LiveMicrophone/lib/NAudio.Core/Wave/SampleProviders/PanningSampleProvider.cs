using System;
using NAudio.Utils;

namespace NAudio.Wave.SampleProviders
{
    /// <summary>
    /// Converts a mono sample provider to stereo, with a customisable pan strategy
    /// </summary>
    public class PanningSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private float pan;
        private float leftMultiplier;
        private float rightMultiplier;
        private readonly WaveFormat waveFormat;
        private float[] sourceBuffer;
        private IPanStrategy panStrategy;

        /// <summary>
        /// Initialises a new instance of the PanningSampleProvider
        /// </summary>
        /// <param name="source">Source sample provider, must be mono</param>
        public PanningSampleProvider(ISampleProvider source)
        {
            if (source.WaveFormat.Channels != 1)
            {
                throw new ArgumentException("Source sample provider must be mono");
            }
            this.source = source;
            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 2);
            panStrategy = new SinPanStrategy();
        }

        /// <summary>
        /// Pan value, must be between -1 (left) and 1 (right)
        /// </summary>
        public float Pan
        {
            get
            {
                return pan;
            }
            set
            {
                if (value < -1.0f || value > 1.0f)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Pan must be in the range -1 to 1");
                }
                pan = value;
                UpdateMultipliers();
            }
        }

        /// <summary>
        /// The pan strategy currently in use
        /// </summary>
        public IPanStrategy PanStrategy
        {
            get
            {
                return panStrategy;
            }
            set
            {
                panStrategy = value;
                UpdateMultipliers();
            }
        }

        /// <summary>
        /// Updates the multipliers for the strategy based on the current pan.
        /// </summary>
        /// <remarks>
        /// This method retrieves the multipliers for the current pan from the strategy and updates the left and right multipliers accordingly.
        /// </remarks>
        private void UpdateMultipliers()
        {
            var multipliers = panStrategy.GetMultipliers(Pan);
            leftMultiplier = multipliers.Left;
            rightMultiplier = multipliers.Right;
        }

        /// <summary>
        /// The WaveFormat of this sample provider
        /// </summary>
        public WaveFormat WaveFormat => waveFormat;

        /// <summary>
        /// Reads audio samples from the source buffer and writes them to the specified buffer.
        /// </summary>
        /// <param name="buffer">The destination buffer to write the audio samples to.</param>
        /// <param name="offset">The zero-based index in the destination buffer at which to begin writing.</param>
        /// <param name="count">The number of audio samples to read from the source buffer and write to the destination buffer.</param>
        /// <returns>The total number of audio samples written to the destination buffer.</returns>
        /// <remarks>
        /// This method reads audio samples from the source buffer, applies left and right multipliers, and writes them to the specified destination buffer.
        /// It ensures that the source buffer has enough capacity to hold the required number of samples, reads the required number of samples from the source buffer, and writes the processed samples to the destination buffer.
        /// </remarks>
        public int Read(float[] buffer, int offset, int count)
        {
            int sourceSamplesRequired = count / 2;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, sourceSamplesRequired);
            int sourceSamplesRead = source.Read(sourceBuffer, 0, sourceSamplesRequired);
            int outIndex = offset;
            for (int n = 0; n < sourceSamplesRead; n++)
            {
                buffer[outIndex++] = leftMultiplier * sourceBuffer[n];
                buffer[outIndex++] = rightMultiplier * sourceBuffer[n];
            }
            return sourceSamplesRead * 2;
        }
    }

    /// <summary>
    /// Pair of floating point values, representing samples or multipliers
    /// </summary>
    public struct StereoSamplePair
    {
        /// <summary>
        /// Left value
        /// </summary>
        public float Left { get; set; }
        /// <summary>
        /// Right value
        /// </summary>
        public float Right { get; set; }
    }

    /// <summary>
    /// Required Interface for a Panning Strategy
    /// </summary>
    public interface IPanStrategy
    {

        /// <summary>
        /// Returns the multipliers for left and right audio channels based on the input panning value.
        /// </summary>
        /// <param name="pan">The panning value in the range of -1 to 1.</param>
        /// <returns>A StereoSamplePair object containing the multipliers for the left and right audio channels.</returns>
        /// <remarks>
        /// This method calculates the multipliers for left and right audio channels based on the input panning value.
        /// The panning value is normalized to the range of 0 to 1, where -1 corresponds to 1 and 1 corresponds to 0.
        /// The left channel multiplier is equal to the normalized panning value, and the right channel multiplier is equal to 1 minus the normalized panning value.
        /// The method returns a StereoSamplePair object containing the calculated multipliers for the left and right channels.
        /// </remarks>
        StereoSamplePair GetMultipliers(float pan);
    }

    /// <summary>
    /// Simplistic "balance" control - treating the mono input as if it was stereo
    /// In the centre, both channels full volume. Opposite channel decays linearly 
    /// as balance is turned to to one side
    /// </summary>
    public class StereoBalanceStrategy : IPanStrategy
    {
        /// <summary>
        /// Gets the left and right channel multipliers for this pan value
        /// </summary>
        /// <param name="pan">Pan value, between -1 and 1</param>
        /// <returns>Left and right multipliers</returns>
        public StereoSamplePair GetMultipliers(float pan)
        {
            float leftChannel = (pan <= 0) ? 1.0f : ((1 - pan) / 2.0f);
            float rightChannel = (pan >= 0) ? 1.0f : ((pan + 1) / 2.0f);
            // Console.WriteLine(pan + ": " + leftChannel + "," + rightChannel);
            return new StereoSamplePair() { Left = leftChannel, Right = rightChannel };
        }
    }


    /// <summary>
    /// Square Root Pan, thanks to Yuval Naveh
    /// </summary>
    public class SquareRootPanStrategy : IPanStrategy
    {
        /// <summary>
        /// Gets the left and right channel multipliers for this pan value
        /// </summary>
        /// <param name="pan">Pan value, between -1 and 1</param>
        /// <returns>Left and right multipliers</returns>
        public StereoSamplePair GetMultipliers(float pan)
        {
            // -1..+1  -> 1..0
            float normPan = (-pan + 1) / 2;
            float leftChannel = (float)Math.Sqrt(normPan);
            float rightChannel = (float)Math.Sqrt(1 - normPan);
            // Console.WriteLine(pan + ": " + leftChannel + "," + rightChannel);
            return new StereoSamplePair() { Left = leftChannel, Right = rightChannel };
        }
    }

    /// <summary>
    /// Sinus Pan, thanks to Yuval Naveh
    /// </summary>
    public class SinPanStrategy : IPanStrategy
    {
        private const float HalfPi = (float)Math.PI / 2;

        /// <summary>
        /// Gets the left and right channel multipliers for this pan value
        /// </summary>
        /// <param name="pan">Pan value, between -1 and 1</param>
        /// <returns>Left and right multipliers</returns>
        public StereoSamplePair GetMultipliers(float pan)
        {
            // -1..+1  -> 1..0
            float normPan = (-pan + 1) / 2;
            float leftChannel = (float)Math.Sin(normPan * HalfPi);
            float rightChannel = (float)Math.Cos(normPan * HalfPi);
            // Console.WriteLine(pan + ": " + leftChannel + "," + rightChannel);
            return new StereoSamplePair() { Left = leftChannel, Right = rightChannel };
        }
    }

    /// <summary>
    /// Linear Pan
    /// </summary>
    public class LinearPanStrategy : IPanStrategy
    {
        /// <summary>
        /// Gets the left and right channel multipliers for this pan value
        /// </summary>
        /// <param name="pan">Pan value, between -1 and 1</param>
        /// <returns>Left and right multipliers</returns>
        public StereoSamplePair GetMultipliers(float pan)
        {
            // -1..+1  -> 1..0
            float normPan = (-pan + 1) / 2;
            float leftChannel = normPan;
            float rightChannel = 1 - normPan;
            return new StereoSamplePair() { Left = leftChannel, Right = rightChannel };
        }
    }
}
