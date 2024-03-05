using System;
using System.Text;

namespace NAudio.Utils
{
    /// <summary>
    /// An encoding for use with file types that have one byte per character
    /// </summary>
    public class ByteEncoding : Encoding
    {
        private ByteEncoding() 
        { 
        }

        /// <summary>
        /// The one and only instance of this class
        /// </summary>
        public static readonly ByteEncoding Instance = new ByteEncoding();

        /// <summary>
        /// Gets the number of bytes required to encode a set of characters from the specified character array.
        /// </summary>
        /// <param name="chars">The character array containing the set of characters to encode.</param>
        /// <param name="index">The index of the first character in the character array to encode.</param>
        /// <param name="count">The number of characters to encode.</param>
        /// <returns>The number of bytes required to encode the specified set of characters.</returns>
        public override int GetByteCount(char[] chars, int index, int count)
        {
            return count;
        }

        /// <summary>
        /// Encodes a set of characters from the specified character array into the specified byte array.
        /// </summary>
        /// <param name="chars">The character array containing the characters to encode.</param>
        /// <param name="charIndex">The index of the first character to encode.</param>
        /// <param name="charCount">The number of characters to encode.</param>
        /// <param name="bytes">The byte array to contain the resulting sequence of bytes.</param>
        /// <param name="byteIndex">The index at which to start writing the resulting sequence of bytes.</param>
        /// <returns>The total number of bytes written into the byte array.</returns>
        /// <remarks>
        /// This method encodes a set of characters from the specified character array into the specified byte array.
        /// It iterates through the characters in the input character array and casts each character to a byte, then stores it in the output byte array.
        /// The method returns the total number of bytes written into the byte array, which is equal to the number of characters encoded.
        /// </remarks>
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            for (int n = 0; n < charCount; n++)
            {
                bytes[byteIndex + n] = (byte)chars[charIndex + n];
            }
            return charCount;
        }

        /// <summary>
        /// Returns the number of characters produced by decoding a sequence of bytes from the specified byte array.
        /// </summary>
        /// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
        /// <param name="index">The index of the first byte to decode.</param>
        /// <param name="count">The number of bytes to decode.</param>
        /// <returns>The number of characters produced by decoding the specified sequence of bytes.</returns>
        /// <remarks>
        /// This method iterates through the specified byte array starting from the given index and counts the number of characters until it encounters a null byte (0).
        /// If no null byte is encountered within the specified count, it returns the total count.
        /// </remarks>
        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            for (int n = 0; n < count; n++)
            {
                if (bytes[index + n] == 0)
                    return n;
            }
            return count;
        }

        /// <summary>
        /// Decodes a sequence of bytes from the specified byte array into the specified character array.
        /// </summary>
        /// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
        /// <param name="byteIndex">The index of the first byte to decode.</param>
        /// <param name="byteCount">The number of bytes to decode.</param>
        /// <param name="chars">The character array to contain the resulting decoded characters.</param>
        /// <param name="charIndex">The index at which to start writing the resulting decoded characters.</param>
        /// <returns>The actual number of characters written into <paramref name="chars"/>.</returns>
        /// <remarks>
        /// This method decodes a sequence of bytes from the specified byte array into the specified character array using the default character encoding.
        /// It starts at the specified byte index and decodes the specified number of bytes.
        /// If a null byte is encountered during decoding, the method returns the number of characters decoded up to that point.
        /// </remarks>
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            for (int n = 0; n < byteCount; n++)
            {
                var b = bytes[byteIndex + n];
                if (b == 0)
                {
                    return n;
                }
                chars[charIndex + n] = (char)b;
            }
            return byteCount;
        }

        /// <summary>
        /// Returns the maximum number of characters produced by decoding the specified number of bytes.
        /// </summary>
        /// <param name="byteCount">The number of bytes to decode.</param>
        /// <returns>The maximum number of characters produced by decoding the specified number of bytes.</returns>
        public override int GetMaxCharCount(int byteCount)
        {
            return byteCount;
        }

        /// <summary>
        /// Returns the maximum number of bytes required to encode the specified number of characters.
        /// </summary>
        /// <param name="charCount">The number of characters to encode.</param>
        /// <returns>The maximum number of bytes required to encode the specified number of characters.</returns>
        public override int GetMaxByteCount(int charCount)
        {
            return charCount;
        }
    }
}
