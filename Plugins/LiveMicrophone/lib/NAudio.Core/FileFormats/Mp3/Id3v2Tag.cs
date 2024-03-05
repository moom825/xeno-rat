using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NAudio.Utils;

namespace NAudio.Wave
{
    /// <summary>
    /// An ID3v2 Tag
    /// </summary>
    public class Id3v2Tag
    {
        private long tagStartPosition;
        private long tagEndPosition;
        private byte[] rawData;

        /// <summary>
        /// Reads an ID3v2 tag from the input stream and returns the tag.
        /// </summary>
        /// <param name="input">The input stream containing the ID3v2 tag.</param>
        /// <returns>The ID3v2 tag read from the input stream. Returns null if the input stream does not contain a valid ID3v2 tag.</returns>
        /// <exception cref="FormatException">Thrown when the input stream does not contain a valid ID3v2 tag.</exception>
        public static Id3v2Tag ReadTag(Stream input)
        {
            try
            {
                return new Id3v2Tag(input);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        /// <summary>
        /// Creates an ID3v2 tag from the given collection of key-value pairs and returns the created tag.
        /// </summary>
        /// <param name="tags">The collection of key-value pairs representing the tags to be included in the ID3v2 tag.</param>
        /// <returns>The ID3v2 tag created from the specified key-value pairs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the input collection of tags is null.</exception>
        /// <remarks>
        /// This method creates an ID3v2 tag by reading the tag from the stream generated using the provided key-value pairs.
        /// The method uses the CreateId3v2TagStream method to generate the stream and then reads the tag using the ReadTag method.
        /// </remarks>
        public static Id3v2Tag Create(IEnumerable<KeyValuePair<string, string>> tags)
        {
            return Id3v2Tag.ReadTag(CreateId3v2TagStream(tags));
        }

        /// <summary>
        /// Converts the frame size to a byte array and returns the result.
        /// </summary>
        /// <param name="n">The frame size to be converted to a byte array.</param>
        /// <returns>The byte array representing the frame size <paramref name="n"/>.</returns>
        /// <remarks>
        /// This method converts the input frame size <paramref name="n"/> to a byte array using the BitConverter class.
        /// It then reverses the byte array using the Array.Reverse method to ensure little-endian encoding.
        /// The resulting byte array is returned as the output.
        /// </remarks>
        static byte[] FrameSizeToBytes(int n)
        {
            byte[] result = BitConverter.GetBytes(n);
            Array.Reverse(result);
            return result;
        }

        /// <summary>
        /// Creates an ID3v2 frame with the specified key and value.
        /// </summary>
        /// <param name="key">The key for the ID3v2 frame. Must be 4 characters long.</param>
        /// <param name="value">The value to be encoded in the ID3v2 frame.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> or <paramref name="value"/> is null or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="key"/> is not 4 characters long.</exception>
        /// <returns>The ID3v2 frame with the specified key and value encoded in it.</returns>
        /// <remarks>
        /// This method creates an ID3v2 frame with the specified key and value. It encodes the value using Unicode and modifies the original array in place.
        /// If the key is "COMM", it includes additional information such as language and short description in the frame.
        /// </remarks>
        static byte[] CreateId3v2Frame(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }

            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException("value");
            }

            if (key.Length != 4)
            {
                throw new ArgumentOutOfRangeException("key", "key " + key + " must be 4 characters long");
            }

            const byte UnicodeEncoding = 01; // encode text in Unicode
            byte[] UnicodeOrder = new byte[] { 0xff, 0xfe }; // Unicode byte order mark
            byte[] language = new byte[] { 0, 0, 0 }; // language is empty (only used in COMM -> comment)
            byte[] shortDescription = new byte[] { 0, 0 }; // short description is empty (only used in COMM -> comment)

            byte[] body;
            if (key == "COMM") // comment
            {
                body = ByteArrayExtensions.Concat(
                    new byte[] { UnicodeEncoding },
                    language,
                    shortDescription,
                    UnicodeOrder,
                    Encoding.Unicode.GetBytes(value));
            }
            else
            {
                body = ByteArrayExtensions.Concat(
                    new byte[] { UnicodeEncoding },
                    UnicodeOrder,
                    Encoding.Unicode.GetBytes(value));
            }

            return ByteArrayExtensions.Concat(
                // needs review - have converted to UTF8 as Win 8 has no Encoding.ASCII, 
                // need to check what the rules are for ID3v2 tag identifiers
                Encoding.UTF8.GetBytes(key),
                FrameSizeToBytes(body.Length),
                new byte[] { 0, 0 }, // flags
                body);
        }

        /// <summary>
        /// Gets the ID3 tag header size as an array of bytes.
        /// </summary>
        /// <param name="size">The size of the ID3 tag header.</param>
        /// <returns>An array of bytes representing the ID3 tag header size.</returns>
        /// <remarks>
        /// This method calculates the ID3 tag header size as an array of bytes by dividing the input <paramref name="size"/> by 128 and storing the remainders in the result array.
        /// The result array is then returned.
        /// </remarks>
        static byte[] GetId3TagHeaderSize(int size)
        {
            byte[] result = new byte[4];
            for (int idx = result.Length - 1; idx >= 0; idx--)
            {
                result[idx] = (byte)(size % 128);
                size = size / 128;
            }

            return result;
        }

        /// <summary>
        /// Creates an ID3v2 tag header based on the provided frames and returns the resulting tag header.
        /// </summary>
        /// <param name="frames">The collection of frames to be included in the tag header.</param>
        /// <returns>The ID3v2 tag header created based on the provided frames.</returns>
        /// <remarks>
        /// This method calculates the size of the tag header based on the combined size of the input frames.
        /// It then constructs the tag header by concatenating the ID3 identifier, version, flags, and the calculated tag header size.
        /// The resulting tag header is returned as a byte array.
        /// </remarks>
        static byte[] CreateId3v2TagHeader(IEnumerable<byte[]> frames)
        {
            int size = 0;
            foreach (byte[] frame in frames)
            {
                size += frame.Length;
            }

            byte[] tagHeader = ByteArrayExtensions.Concat(
                Encoding.UTF8.GetBytes("ID3"),
                new byte[] { 3, 0 }, // version
                new byte[] { 0 }, // flags
                GetId3TagHeaderSize(size));
            return tagHeader;
        }

        /// <summary>
        /// Creates an ID3v2 tag stream based on the provided tags.
        /// </summary>
        /// <param name="tags">The collection of key-value pairs representing the tags to be included in the ID3v2 tag stream.</param>
        /// <returns>A memory stream containing the ID3v2 tag with the specified tags.</returns>
        /// <remarks>
        /// This method creates an ID3v2 tag stream by constructing individual frames for each tag in the input collection and then combining them with a tag header.
        /// The resulting memory stream contains the complete ID3v2 tag ready to be written to a file or used in memory.
        /// </remarks>
        static Stream CreateId3v2TagStream(IEnumerable<KeyValuePair<string, string>> tags)
        {
            List<byte[]> frames = new List<byte[]>();
            foreach (KeyValuePair<string, string> tag in tags)
            {
                frames.Add(CreateId3v2Frame(tag.Key, tag.Value));
            }

            byte[] header = CreateId3v2TagHeader(frames);

            MemoryStream ms = new MemoryStream();
            ms.Write(header, 0, header.Length);
            foreach (byte[] frame in frames)
            {
                ms.Write(frame, 0, frame.Length);
            }

            ms.Position = 0;
            return ms;
        }

        #endregion

        private Id3v2Tag(Stream input)
        {
            tagStartPosition = input.Position;
            var reader = new BinaryReader(input);
            byte[] headerBytes = reader.ReadBytes(10);
            if ((headerBytes.Length >= 3) &&
                (headerBytes[0] == (byte)'I') &&
                (headerBytes[1] == (byte)'D') &&
                (headerBytes[2] == '3'))
            {

                // http://www.id3.org/develop.html
                // OK found an ID3 tag
                // bytes 3 & 4 are ID3v2 version

                if ((headerBytes[5] & 0x40) == 0x40)
                {
                    // extended header present
                    byte[] extendedHeader = reader.ReadBytes(4);
                    int extendedHeaderLength = extendedHeader[0] * (1 << 21);
                    extendedHeaderLength += extendedHeader[1] * (1 << 14);
                    extendedHeaderLength += extendedHeader[2] * (1 << 7);
                    extendedHeaderLength += extendedHeader[3];
                }

                // synchsafe
                int dataLength = headerBytes[6] * (1 << 21);
                dataLength += headerBytes[7] * (1 << 14);
                dataLength += headerBytes[8] * (1 << 7);
                dataLength += headerBytes[9];
                byte[] tagData = reader.ReadBytes(dataLength);

                if ((headerBytes[5] & 0x10) == 0x10)
                {
                    // footer present
                    byte[] footer = reader.ReadBytes(10);
                }
            }
            else
            {
                input.Position = tagStartPosition;
                throw new FormatException("Not an ID3v2 tag");
            }
            tagEndPosition = input.Position;
            input.Position = tagStartPosition;
            rawData = reader.ReadBytes((int)(tagEndPosition - tagStartPosition));

        }

        /// <summary>
        /// Raw data from this tag
        /// </summary>
        public byte[] RawData
        {
            get
            {
                return rawData;
            }
        }
    }
}
