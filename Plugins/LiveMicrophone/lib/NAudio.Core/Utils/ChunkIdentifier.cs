using System;
using System.Linq;
using System.Text;

namespace NAudio.Utils
{
    /// <summary>
    /// Chunk Identifier helpers
    /// </summary>
    public class ChunkIdentifier
    {

        /// <summary>
        /// Converts a four-character string to a 32-bit signed integer.
        /// </summary>
        /// <param name="s">The input string, must be exactly four characters long.</param>
        /// <returns>The 32-bit signed integer representation of the input string.</returns>
        /// <exception cref="ArgumentException">Thrown when the input string is not exactly four characters long or when it does not encode to exactly four bytes.</exception>
        /// <remarks>
        /// This method converts the input string <paramref name="s"/> to a byte array using UTF-8 encoding, and then converts the byte array to a 32-bit signed integer using the BitConverter.ToInt32 method.
        /// If the input string is not exactly four characters long or does not encode to exactly four bytes, an ArgumentException is thrown.
        /// </remarks>
        public static int ChunkIdentifierToInt32(string s)
        {
            if (s.Length != 4) throw new ArgumentException("Must be a four character string");
            var bytes = Encoding.UTF8.GetBytes(s);
            if (bytes.Length != 4) throw new ArgumentException("Must encode to exactly four bytes");
            return BitConverter.ToInt32(bytes, 0);
        }
    }
}
