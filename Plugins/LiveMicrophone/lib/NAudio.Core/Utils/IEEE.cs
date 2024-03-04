using System;


namespace NAudio.Utils
{
    /// <summary>
    /// Methods for converting between IEEE 80-bit extended double precision
    /// and standard C# double precision.
    /// </summary>
    public static class IEEE
    {

        /// <summary>
        /// Converts the unsigned 64-bit integer to a floating-point number.
        /// </summary>
        /// <param name="u">The unsigned 64-bit integer to be converted.</param>
        /// <returns>The floating-point representation of the input unsigned 64-bit integer.</returns>
        /// <remarks>
        /// This method converts the input unsigned 64-bit integer <paramref name="u"/> to a floating-point number by subtracting 2147483647 and 1 from it, casting the result to a long, then casting it to a double, and finally adding 2147483648.0 to the result.
        /// </remarks>
        private static double UnsignedToFloat(ulong u)
        {
            return (((double)((long)(u - 2147483647L - 1))) + 2147483648.0);
        }

        /// <summary>
        /// Returns x multiplied by 2 raised to the power of exp.
        /// </summary>
        /// <param name="x">The number to be multiplied.</param>
        /// <param name="exp">The exponent to raise 2 to.</param>
        /// <returns>The result of multiplying <paramref name="x"/> by 2 raised to the power of <paramref name="exp"/>.</returns>
        private static double ldexp(double x, int exp)
        {
            return x * Math.Pow(2, exp);
        }

        /// <summary>
        /// Splits a double-precision floating-point value into a normalized fraction and an integral power of 2.
        /// </summary>
        /// <param name="x">The value to be split.</param>
        /// <param name="exp">When this method returns, contains the exponent of the value.</param>
        /// <returns>The normalized fraction of the value, and sets <paramref name="exp"/> to the exponent.</returns>
        /// <remarks>
        /// This method calculates the exponent of the input value <paramref name="x"/> and sets it in the out parameter <paramref name="exp"/>.
        /// It then computes the normalized fraction by subtracting the power of 2 raised to the exponent from the input value and dividing it by the same power of 2.
        /// </remarks>
        private static double frexp(double x, out int exp)
        {
            exp = (int)Math.Floor(Math.Log(x) / Math.Log(2)) + 1;
            return 1 - (Math.Pow(2, exp) - x) / Math.Pow(2, exp);
        }

        /// <summary>
        /// Converts a floating-point number to an unsigned long integer.
        /// </summary>
        /// <param name="f">The floating-point number to be converted.</param>
        /// <returns>The unsigned long integer equivalent of the input floating-point number <paramref name="f"/>.</returns>
        private static ulong FloatToUnsigned(double f)
        {
            return ((ulong)(((long)(f - 2147483648.0)) + 2147483647L) + 1);
        }

        /// <summary>
        /// Converts a double-precision floating-point number to its IEEE 754 extended precision representation.
        /// </summary>
        /// <param name="num">The number to be converted.</param>
        /// <returns>The IEEE 754 extended precision representation of the input <paramref name="num"/>.</returns>
        /// <remarks>
        /// This method first determines the sign of the input number and then calculates the exponent and mantissa based on the input number.
        /// If the input number is 0, the resulting IEEE 754 extended precision representation will have all bits set to 0.
        /// If the input number is Infinity or NaN, the resulting IEEE 754 extended precision representation will be set accordingly.
        /// For finite numbers, the method calculates the exponent and mantissa, and then constructs the 10-byte representation of the IEEE 754 extended precision format.
        /// The resulting byte array contains the sign bit, exponent, and mantissa in the IEEE 754 extended precision format.
        /// </remarks>
        public static byte[] ConvertToIeeeExtended(double num)
        {
            int sign;
            int expon;
            double fMant, fsMant;
            ulong hiMant, loMant;

            if (num < 0)
            {
                sign = 0x8000;
                num *= -1;
            }
            else
            {
                sign = 0;
            }

            if (num == 0)
            {
                expon = 0; hiMant = 0; loMant = 0;
            }
            else
            {
                fMant = frexp(num, out expon);
                if ((expon > 16384) || !(fMant < 1))
                {   //  Infinity or NaN 
                    expon = sign | 0x7FFF; hiMant = 0; loMant = 0; // infinity 
                }
                else
                {    // Finite 
                    expon += 16382;
                    if (expon < 0)
                    {    // denormalized
                        fMant = ldexp(fMant, expon);
                        expon = 0;
                    }
                    expon |= sign;
                    fMant = ldexp(fMant, 32);
                    fsMant = Math.Floor(fMant);
                    hiMant = FloatToUnsigned(fsMant);
                    fMant = ldexp(fMant - fsMant, 32);
                    fsMant = Math.Floor(fMant);
                    loMant = FloatToUnsigned(fsMant);
                }
            }

            byte[] bytes = new byte[10];

            bytes[0] = (byte)(expon >> 8);
            bytes[1] = (byte)(expon);
            bytes[2] = (byte)(hiMant >> 24);
            bytes[3] = (byte)(hiMant >> 16);
            bytes[4] = (byte)(hiMant >> 8);
            bytes[5] = (byte)(hiMant);
            bytes[6] = (byte)(loMant >> 24);
            bytes[7] = (byte)(loMant >> 16);
            bytes[8] = (byte)(loMant >> 8);
            bytes[9] = (byte)(loMant);

            return bytes;
        }

        /// <summary>
        /// Converts the given IEEE extended byte array to a double value.
        /// </summary>
        /// <param name="bytes">The IEEE extended byte array to be converted.</param>
        /// <exception cref="Exception">Thrown when the length of the input byte array is not 10.</exception>
        /// <returns>The double value converted from the IEEE extended byte array.</returns>
        /// <remarks>
        /// This method converts the given IEEE extended byte array to a double value according to the IEEE 754 standard.
        /// It first extracts the sign, exponent, and mantissa from the byte array and then performs the necessary calculations to obtain the double value.
        /// If the input represents Infinity or NaN, the method returns double.NaN.
        /// </remarks>
        public static double ConvertFromIeeeExtended(byte[] bytes)
        {
            if (bytes.Length != 10) throw new Exception("Incorrect length for IEEE extended.");
            double f;
            int expon;
            uint hiMant, loMant;

            expon = ((bytes[0] & 0x7F) << 8) | bytes[1];
            hiMant = (uint)((bytes[2] << 24) | (bytes[3] << 16) | (bytes[4] << 8) | bytes[5]);
            loMant = (uint)((bytes[6] << 24) | (bytes[7] << 16) | (bytes[8] << 8) | bytes[9]);

            if (expon == 0 && hiMant == 0 && loMant == 0)
            {
                f = 0;
            }
            else
            {
                if (expon == 0x7FFF)    /* Infinity or NaN */
                {
                    f = double.NaN;
                }
                else
                {
                    expon -= 16383;
                    f = ldexp(UnsignedToFloat(hiMant), expon -= 31);
                    f += ldexp(UnsignedToFloat(loMant), expon -= 32);
                }
            }

            if ((bytes[0] & 0x80) == 0x80) return -f;
            else return f;
        }
        #endregion
    }
}
