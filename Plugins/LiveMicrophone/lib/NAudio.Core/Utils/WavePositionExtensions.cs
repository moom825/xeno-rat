using System;
using NAudio.Wave;

namespace NAudio.Utils
{
    /// <summary>
    /// WavePosition extension methods
    /// </summary>
    public static class WavePositionExtensions
    {

        /// <summary>
        /// Gets the position in time as a TimeSpan based on the current wave position.
        /// </summary>
        /// <param name="@this">The wave position for which to get the time span.</param>
        /// <returns>The position in time as a TimeSpan.</returns>
        public static TimeSpan GetPositionTimeSpan(this IWavePosition @this)
        {
            var pos = @this.GetPosition() / (@this.OutputWaveFormat.Channels * @this.OutputWaveFormat.BitsPerSample / 8);
            return TimeSpan.FromMilliseconds(pos * 1000.0 / @this.OutputWaveFormat.SampleRate);
        }
    }
}
