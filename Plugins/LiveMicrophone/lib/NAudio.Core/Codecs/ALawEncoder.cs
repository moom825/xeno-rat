namespace NAudio.Codecs
{
    /// <summary>
    /// A-law encoder
    /// </summary>
    public static class ALawEncoder
    {
        private const int cBias = 0x84;
        private const int cClip = 32635;
        private static readonly byte[] ALawCompressTable = new byte[128]
        {
             1,1,2,2,3,3,3,3,
             4,4,4,4,4,4,4,4,
             5,5,5,5,5,5,5,5,
             5,5,5,5,5,5,5,5,
             6,6,6,6,6,6,6,6,
             6,6,6,6,6,6,6,6,
             6,6,6,6,6,6,6,6,
             6,6,6,6,6,6,6,6,
             7,7,7,7,7,7,7,7,
             7,7,7,7,7,7,7,7,
             7,7,7,7,7,7,7,7,
             7,7,7,7,7,7,7,7,
             7,7,7,7,7,7,7,7,
             7,7,7,7,7,7,7,7,
             7,7,7,7,7,7,7,7,
             7,7,7,7,7,7,7,7
        };

        /// <summary>
        /// Converts a linear PCM sample to A-law format.
        /// </summary>
        /// <param name="sample">The linear PCM sample to be converted.</param>
        /// <returns>The A-law compressed byte representation of the input <paramref name="sample"/>.</returns>
        /// <remarks>
        /// This method converts a linear PCM sample to A-law format as per the ITU-T G.711 specification.
        /// It first determines the sign, exponent, and mantissa of the input sample and then compresses it to A-law format.
        /// The resulting A-law compressed byte is returned as the output.
        /// </remarks>
        public static byte LinearToALawSample(short sample)
        {
            int sign;
            int exponent;
            int mantissa;
            byte compressedByte;

            sign = ((~sample) >> 8) & 0x80;
            if (sign == 0)
                sample = (short)-sample;
            if (sample > cClip)
                sample = cClip;
            if (sample >= 256)
            {
                exponent = (int)ALawCompressTable[(sample >> 8) & 0x7F];
                mantissa = (sample >> (exponent + 3)) & 0x0F;
                compressedByte = (byte)((exponent << 4) | mantissa);
            }
            else
            {
                compressedByte = (byte)(sample >> 4);
            }
            compressedByte ^= (byte)(sign ^ 0x55);
            return compressedByte;
        }
    }
}
