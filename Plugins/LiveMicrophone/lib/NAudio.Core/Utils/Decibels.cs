using System;

namespace NAudio.Utils
{
    /// <summary>
    /// A util class for conversions
    /// </summary>
    public class Decibels
    {
        // 20 / ln( 10 )
        private const double LOG_2_DB = 8.6858896380650365530225783783321;

        // ln( 10 ) / 20
        private const double DB_2_LOG = 0.11512925464970228420089957273422;

        /// <summary>
        /// Converts a linear value to decibels.
        /// </summary>
        /// <param name="lin">The linear value to be converted.</param>
        /// <returns>The value of <paramref name="lin"/> converted to decibels.</returns>
        /// <remarks>
        /// This method calculates the logarithm of the input linear value and multiplies it by the conversion factor to obtain the value in decibels.
        /// </remarks>
        public static double LinearToDecibels(double lin)
        {
            return Math.Log(lin) * LOG_2_DB;
        }

        /// <summary>
        /// Converts a decibel value to its linear equivalent.
        /// </summary>
        /// <param name="dB">The decibel value to be converted.</param>
        /// <returns>The linear equivalent of the input decibel value.</returns>
        /// <remarks>
        /// This method uses the formula: linear = exp(dB * 0.11512925464970229).
        /// </remarks>
        public static double DecibelsToLinear(double dB)
        {
            return Math.Exp(dB * DB_2_LOG);
        }

    }
}
