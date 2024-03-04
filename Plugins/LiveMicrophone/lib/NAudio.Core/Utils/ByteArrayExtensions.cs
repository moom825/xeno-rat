using System;
using System.Text;

namespace NAudio.Utils
{    
    /// <summary>
    /// these will become extension methods once we move to .NET 3.5
    /// </summary>
    public static class ByteArrayExtensions
    {

        /// <summary>
        /// Checks if the entire byte array is filled with null values.
        /// </summary>
        /// <param name="buffer">The byte array to be checked.</param>
        /// <returns>True if the entire byte array is filled with null values; otherwise, false.</returns>
        public static bool IsEntirelyNull(byte[] buffer)
        {
            foreach (byte b in buffer)
                if (b != 0)
                    return false;
            return true;
        }

        /// <summary>
        /// Converts the input byte array to a hexadecimal string with the specified separator and bytes per line.
        /// </summary>
        /// <param name="buffer">The byte array to be converted to hexadecimal string.</param>
        /// <param name="separator">The separator to be used between hexadecimal values.</param>
        /// <param name="bytesPerLine">The number of bytes to be displayed per line in the output.</param>
        /// <returns>A string representing the hexadecimal values of the input byte array with the specified separator and line formatting.</returns>
        public static string DescribeAsHex(byte[] buffer, string separator, int bytesPerLine)
        {
            StringBuilder sb = new StringBuilder();
            int n = 0;
            foreach (byte b in buffer)
            {
                sb.AppendFormat("{0:X2}{1}", b, separator);
                if (++n % bytesPerLine == 0)
                    sb.Append("\r\n");
            }
            sb.Append("\r\n");
            return sb.ToString();
        }

        /// <summary>
        /// Decodes a portion of a byte array into a string using the specified encoding and returns the result.
        /// </summary>
        /// <param name="buffer">The input byte array.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin decoding.</param>
        /// <param name="length">The number of bytes to decode.</param>
        /// <param name="encoding">The character encoding to use.</param>
        /// <returns>A string that contains the results of decoding the specified sequence of bytes.</returns>
        /// <remarks>
        /// This method decodes a portion of the input byte array <paramref name="buffer"/> into a string using the specified character encoding <paramref name="encoding"/>.
        /// It starts at the specified byte <paramref name="offset"/> and decodes the specified <paramref name="length"/> number of bytes.
        /// If a null byte is encountered within the specified length, the decoding stops at that position.
        /// </remarks>
        public static string DecodeAsString(byte[] buffer, int offset, int length, Encoding encoding)
        {
            for (int n = 0; n < length; n++)
            {
                if (buffer[offset + n] == 0)
                    length = n;
            }
            return encoding.GetString(buffer, offset, length);
        }

        /// <summary>
        /// Concatenates multiple byte arrays into a single byte array and returns the result.
        /// </summary>
        /// <param name="byteArrays">The byte arrays to be concatenated.</param>
        /// <returns>A byte array containing the concatenated elements of <paramref name="byteArrays"/>.</returns>
        /// <remarks>
        /// This method calculates the total size required for the concatenated byte array by summing the lengths of all input byte arrays.
        /// It then creates a new byte array of the calculated size and copies the elements of each input byte array into the new array.
        /// The resulting byte array is returned as the concatenation of all input byte arrays.
        /// If the total size is less than or equal to 0, an empty byte array is returned.
        /// </remarks>
        public static byte[] Concat(params byte[][] byteArrays)
        {
            int size = 0;
            foreach (byte[] btArray in byteArrays)
            {
                size += btArray.Length;
            }

            if (size <= 0)
            {
                return new byte[0];
            }

            byte[] result = new byte[size];
            int idx = 0;
            foreach (byte[] btArray in byteArrays)
            {
                Array.Copy(btArray, 0, result, idx, btArray.Length);
                idx += btArray.Length;
            }

            return result;
        }
    }
}
