using System;

namespace NAudio.Wave
{
    /// <summary>
    /// Represents a Xing VBR header
    /// </summary>
    public class XingHeader
    {
        [Flags]
        enum XingHeaderOptions
        {
            Frames = 1,
            Bytes = 2,
            Toc = 4,
            VbrScale = 8
        }

        private static int[] sr_table = { 44100, 48000, 32000, 99999 };
        private int vbrScale = -1;
        private int startOffset;
        private int endOffset;
        
        private int tocOffset = -1;
        private int framesOffset = -1;
        private int bytesOffset = -1;
        private Mp3Frame frame;

        /// <summary>
        /// Reads a 32-bit integer from the specified buffer in big-endian format.
        /// </summary>
        /// <param name="buffer">The input buffer containing the integer data.</param>
        /// <param name="offset">The offset at which the integer data starts in the buffer.</param>
        /// <returns>The 32-bit integer read from the buffer in big-endian format.</returns>
        private static int ReadBigEndian(byte[] buffer, int offset)
        {
            int x;
            // big endian extract
            x = buffer[offset+0];
            x <<= 8;
            x |= buffer[offset+1];
            x <<= 8;
            x |= buffer[offset+2];
            x <<= 8;
            x |= buffer[offset+3];

            return x;
        }

        /// <summary>
        /// Writes the integer value in big-endian format to the specified buffer at the given offset.
        /// </summary>
        /// <param name="buffer">The buffer to write the big-endian value to.</param>
        /// <param name="offset">The offset within the buffer at which to start writing the value.</param>
        /// <param name="value">The integer value to be written in big-endian format.</param>
        /// <remarks>
        /// This method converts the integer value into its little-endian byte representation using the BitConverter.GetBytes method.
        /// It then writes the bytes in reverse order to the buffer starting from the specified offset to ensure big-endian format.
        /// </remarks>
        private void WriteBigEndian(byte[] buffer, int offset, int value)
        {
            byte[] littleEndian = BitConverter.GetBytes(value);
            for (int n = 0; n < 4; n++)
            {
                buffer[offset + 3 - n] = littleEndian[n];
            }
        }

        /// <summary>
        /// Loads the Xing header from the provided Mp3Frame.
        /// </summary>
        /// <param name="frame">The Mp3Frame from which to load the Xing header.</param>
        /// <returns>The XingHeader loaded from the Mp3Frame, or null if the MPEG version is unsupported or the Xing header is not found.</returns>
        /// <remarks>
        /// This method loads the Xing header from the provided Mp3Frame. It calculates the offset based on the MPEG version and channel mode of the frame, and then checks for the presence of the Xing or Info header.
        /// If the Xing or Info header is found, it reads the XingHeaderOptions flags and sets the corresponding properties in the XingHeader object.
        /// The method returns the loaded XingHeader or null if the MPEG version is unsupported or the Xing header is not found.
        /// </remarks>
        /// <exception cref="FormatException">Thrown when the MPEG version is unsupported.</exception>
        public static XingHeader LoadXingHeader(Mp3Frame frame)
        {
            XingHeader xingHeader = new XingHeader();
            xingHeader.frame = frame;
            int offset = 0;

            if (frame.MpegVersion == MpegVersion.Version1)
            {
                if (frame.ChannelMode != ChannelMode.Mono)
                    offset = 32 + 4;
                else
                    offset = 17 + 4;
            }
            else if (frame.MpegVersion == MpegVersion.Version2)
            {
                if (frame.ChannelMode != ChannelMode.Mono)
                    offset = 17 + 4;
                else
                    offset = 9 + 4;
            }
            else
            {
                return null;
                // throw new FormatException("Unsupported MPEG Version");
            }

            if ((frame.RawData[offset + 0] == 'X') &&
                (frame.RawData[offset + 1] == 'i') &&
                (frame.RawData[offset + 2] == 'n') &&
                (frame.RawData[offset + 3] == 'g'))
            {
                xingHeader.startOffset = offset;
                offset += 4;
            }
            else if ((frame.RawData[offset + 0] == 'I') &&
                     (frame.RawData[offset + 1] == 'n') &&
                     (frame.RawData[offset + 2] == 'f') &&
                     (frame.RawData[offset + 3] == 'o'))
            {
                xingHeader.startOffset = offset;
                offset += 4;
            }
            else
            {
                return null;
            }

            XingHeaderOptions flags = (XingHeaderOptions)ReadBigEndian(frame.RawData, offset);
            offset += 4;

            if ((flags & XingHeaderOptions.Frames) != 0)
            {
                xingHeader.framesOffset = offset;
                offset += 4;
            }
            if ((flags & XingHeaderOptions.Bytes) != 0)
            {
                xingHeader.bytesOffset = offset;
                offset += 4;
            }
            if ((flags & XingHeaderOptions.Toc) != 0)
            {
                xingHeader.tocOffset = offset;
                offset += 100;
            }
            if ((flags & XingHeaderOptions.VbrScale) != 0)
            {
                xingHeader.vbrScale = ReadBigEndian(frame.RawData, offset);
                offset += 4;
            }
            xingHeader.endOffset = offset;
            return xingHeader;
        }

        /// <summary>
        /// Sees if a frame contains a Xing header
        /// </summary>
        private XingHeader()
        {
        }

        /// <summary>
        /// Number of frames
        /// </summary>
        public int Frames
        {
            get 
            { 
                if(framesOffset == -1) 
                    return -1;
                return ReadBigEndian(frame.RawData, framesOffset); 
            }
            set
            {
                if (framesOffset == -1)
                    throw new InvalidOperationException("Frames flag is not set");
                WriteBigEndian(frame.RawData, framesOffset, value);
            }
        }

        /// <summary>
        /// Number of bytes
        /// </summary>
        public int Bytes
        {
            get 
            { 
                if(bytesOffset == -1) 
                    return -1;
                return ReadBigEndian(frame.RawData, bytesOffset); 
            }
            set
            {
                if (framesOffset == -1)
                    throw new InvalidOperationException("Bytes flag is not set");
                WriteBigEndian(frame.RawData, bytesOffset, value);
            }
        }

        /// <summary>
        /// VBR Scale property
        /// </summary>
        public int VbrScale
        {
            get { return vbrScale; }
        }

        /// <summary>
        /// The MP3 frame
        /// </summary>
        public Mp3Frame Mp3Frame
        {
            get { return frame; }
        }

    }
}
