using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using NAudio.Utils;

namespace NAudio.Wave
{
    /// <summary>
    /// Represents a Wave file format
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi, Pack=2)]
    public class WaveFormat
    {
        /// <summary>format type</summary>
        protected WaveFormatEncoding waveFormatTag;
        /// <summary>number of channels</summary>
        protected short channels;
        /// <summary>sample rate</summary>
        protected int sampleRate;
        /// <summary>for buffer estimation</summary>
        protected int averageBytesPerSecond;
        /// <summary>block size of data</summary>
        protected short blockAlign;
        /// <summary>number of bits per sample of mono data</summary>
        protected short bitsPerSample;
        /// <summary>number of following bytes</summary>
        protected short extraSize;

        /// <summary>
        /// Creates a new PCM 44.1Khz stereo 16 bit format
        /// </summary>
        public WaveFormat() : this(44100,16,2)
        {

        }
        
        /// <summary>
        /// Creates a new 16 bit wave format with the specified sample
        /// rate and channel count
        /// </summary>
        /// <param name="sampleRate">Sample Rate</param>
        /// <param name="channels">Number of channels</param>
        public WaveFormat(int sampleRate, int channels)
            : this(sampleRate, 16, channels)
        {
        }

        /// <summary>
        /// Converts the given latency in milliseconds to the corresponding byte size based on the average bytes per second.
        /// </summary>
        /// <param name="milliseconds">The latency in milliseconds to be converted.</param>
        /// <returns>The byte size calculated based on the average bytes per second and the given latency.</returns>
        /// <remarks>
        /// This method calculates the byte size by multiplying the average bytes per second by the given latency in milliseconds and then rounding it to the nearest block align if necessary.
        /// If the calculated byte size is not already aligned to the block size, it is adjusted to the next block-aligned value.
        /// </remarks>
        public int ConvertLatencyToByteSize(int milliseconds)
        {
            int bytes = (int) ((AverageBytesPerSecond/1000.0)*milliseconds);
            if ((bytes%BlockAlign) != 0)
            {
                // Return the upper BlockAligned
                bytes = bytes + BlockAlign - (bytes % BlockAlign);
            }
            return bytes;
        }

        /// <summary>
        /// Creates a custom WaveFormat with the specified parameters.
        /// </summary>
        /// <param name="tag">The encoding format of the WaveFormat.</param>
        /// <param name="sampleRate">The sample rate of the WaveFormat.</param>
        /// <param name="channels">The number of channels in the WaveFormat.</param>
        /// <param name="averageBytesPerSecond">The average bytes per second for the WaveFormat.</param>
        /// <param name="blockAlign">The block alignment for the WaveFormat.</param>
        /// <param name="bitsPerSample">The number of bits per sample in the WaveFormat.</param>
        /// <returns>A custom WaveFormat with the specified parameters.</returns>
        public static WaveFormat CreateCustomFormat(WaveFormatEncoding tag, int sampleRate, int channels, int averageBytesPerSecond, int blockAlign, int bitsPerSample)
        {
            WaveFormat waveFormat = new WaveFormat();
            waveFormat.waveFormatTag = tag;
            waveFormat.channels = (short)channels;
            waveFormat.sampleRate = sampleRate;
            waveFormat.averageBytesPerSecond = averageBytesPerSecond;
            waveFormat.blockAlign = (short)blockAlign;
            waveFormat.bitsPerSample = (short)bitsPerSample;
            waveFormat.extraSize = 0;
            return waveFormat;
        }

        /// <summary>
        /// Creates a new WaveFormat with A-Law encoding.
        /// </summary>
        /// <param name="sampleRate">The sample rate of the audio.</param>
        /// <param name="channels">The number of channels in the audio.</param>
        /// <returns>A new WaveFormat with A-Law encoding, sample rate, channels, and other parameters set accordingly.</returns>
        public static WaveFormat CreateALawFormat(int sampleRate, int channels)
        {
            return CreateCustomFormat(WaveFormatEncoding.ALaw, sampleRate, channels, sampleRate * channels, channels, 8);
        }

        /// <summary>
        /// Creates a new WaveFormat with MuLaw encoding.
        /// </summary>
        /// <param name="sampleRate">The sample rate of the audio.</param>
        /// <param name="channels">The number of channels in the audio.</param>
        /// <returns>A new WaveFormat with MuLaw encoding, based on the specified <paramref name="sampleRate"/> and <paramref name="channels"/>.</returns>
        public static WaveFormat CreateMuLawFormat(int sampleRate, int channels)
        {
            return CreateCustomFormat(WaveFormatEncoding.MuLaw, sampleRate, channels, sampleRate * channels, channels, 8);
        }

        /// <summary>
        /// Creates a new PCM format with the specified sample rate, bit depth and channels
        /// </summary>
        public WaveFormat(int rate, int bits, int channels)
        {
            if (channels < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(channels), "Channels must be 1 or greater");
            }
            // minimum 16 bytes, sometimes 18 for PCM
            waveFormatTag = WaveFormatEncoding.Pcm;
            this.channels = (short)channels;
            sampleRate = rate;
            bitsPerSample = (short)bits;
            extraSize = 0;

            blockAlign = (short)(channels * (bits / 8));
            averageBytesPerSecond = this.sampleRate * this.blockAlign;
        }

        /// <summary>
        /// Creates a new IEEE float wave format with the specified sample rate and number of channels.
        /// </summary>
        /// <param name="sampleRate">The sample rate of the wave format.</param>
        /// <param name="channels">The number of channels in the wave format.</param>
        /// <returns>A new WaveFormat object representing the IEEE float wave format with the specified parameters.</returns>
        public static WaveFormat CreateIeeeFloatWaveFormat(int sampleRate, int channels)
        {
            var wf = new WaveFormat();
            wf.waveFormatTag = WaveFormatEncoding.IeeeFloat;
            wf.channels = (short)channels;
            wf.bitsPerSample = 32;
            wf.sampleRate = sampleRate;
            wf.blockAlign = (short) (4*channels);
            wf.averageBytesPerSecond = sampleRate * wf.blockAlign;
            wf.extraSize = 0;
            return wf;
        }

        /// <summary>
        /// Marshals a WaveFormat structure from the specified pointer.
        /// </summary>
        /// <param name="pointer">A pointer to the WaveFormat structure.</param>
        /// <returns>The WaveFormat structure marshaled from the specified <paramref name="pointer"/>.</returns>
        /// <remarks>
        /// This method marshals a WaveFormat structure from the specified <paramref name="pointer"/>.
        /// It first marshals the WaveFormat structure using Marshal.PtrToStructure method.
        /// Then, based on the wave format encoding, it may further marshal the structure to a more specific type, such as WaveFormatExtensible, AdpcmWaveFormat, Gsm610WaveFormat, or WaveFormatExtraData.
        /// If the wave format encoding is PCM, the extra size is set to 0 to avoid reading corrupt data.
        /// If the wave format encoding is Extensible, Adpcm, or Gsm610, the structure is further marshaled to the specific type.
        /// If the wave format encoding is not recognized and the extra size is greater than 0, the structure is marshaled to WaveFormatExtraData.
        /// The marshaled WaveFormat structure is then returned.
        /// </remarks>
        public static WaveFormat MarshalFromPtr(IntPtr pointer)
        {
            var waveFormat = Marshal.PtrToStructure<WaveFormat>(pointer);
            switch (waveFormat.Encoding)
            {
                case WaveFormatEncoding.Pcm:
                    // can't rely on extra size even being there for PCM so blank it to avoid reading
                    // corrupt data
                    waveFormat.extraSize = 0;
                    break;
                case WaveFormatEncoding.Extensible:
                    waveFormat = Marshal.PtrToStructure<WaveFormatExtensible>(pointer);
                    break;
                case WaveFormatEncoding.Adpcm:
                    waveFormat = Marshal.PtrToStructure<AdpcmWaveFormat>(pointer);
                    break;
                case WaveFormatEncoding.Gsm610:
                    waveFormat = Marshal.PtrToStructure<Gsm610WaveFormat>(pointer);
                    break;
                default:
                    if (waveFormat.ExtraSize > 0)
                    {
                        waveFormat = Marshal.PtrToStructure<WaveFormatExtraData>(pointer);
                    }
                    break;
            }
            return waveFormat;
        }

        /// <summary>
        /// Marshals the WaveFormat structure to a pointer.
        /// </summary>
        /// <param name="format">The WaveFormat structure to be marshaled.</param>
        /// <returns>A pointer to the marshaled WaveFormat structure.</returns>
        /// <remarks>
        /// This method allocates memory for the WaveFormat structure, marshals the structure to the allocated memory, and returns a pointer to the marshaled structure.
        /// The allocated memory must be released using Marshal.FreeHGlobal when it is no longer needed to prevent memory leaks.
        /// </remarks>
        public static IntPtr MarshalToPtr(WaveFormat format)
        {
            int formatSize = Marshal.SizeOf(format);
            IntPtr formatPointer = Marshal.AllocHGlobal(formatSize);
            Marshal.StructureToPtr(format, formatPointer, false);
            return formatPointer;
        }

        /// <summary>
        /// Reads the wave format and extra data from the specified binary reader and returns the WaveFormatExtraData.
        /// </summary>
        /// <param name="br">The BinaryReader used to read the wave format and extra data.</param>
        /// <param name="formatChunkLength">The length of the format chunk.</param>
        /// <returns>The WaveFormatExtraData containing the wave format and extra data.</returns>
        /// <remarks>
        /// This method reads the wave format from the specified BinaryReader using the formatChunkLength to determine the length of the format chunk.
        /// It then reads the extra data from the same BinaryReader.
        /// The WaveFormatExtraData object containing the wave format and extra data is returned.
        /// </remarks>
        public static WaveFormat FromFormatChunk(BinaryReader br, int formatChunkLength)
        {
            var waveFormat = new WaveFormatExtraData();
            waveFormat.ReadWaveFormat(br, formatChunkLength);
            waveFormat.ReadExtraData(br);
            return waveFormat;
        }

        /// <summary>
        /// Reads the wave format information from the provided BinaryReader.
        /// </summary>
        /// <param name="br">The BinaryReader to read the wave format from.</param>
        /// <param name="formatChunkLength">The length of the format chunk.</param>
        /// <exception cref="InvalidDataException">Thrown when the formatChunkLength is less than 16, indicating an invalid WaveFormat structure.</exception>
        /// <remarks>
        /// This method reads the wave format information from the provided BinaryReader, including the wave format tag, number of channels, sample rate, average bytes per second, block align, and bits per sample.
        /// If the formatChunkLength is greater than 16, it also reads the extra size and checks for format chunk mismatch.
        /// </remarks>
        private void ReadWaveFormat(BinaryReader br, int formatChunkLength)
        {
            if (formatChunkLength < 16)
                throw new InvalidDataException("Invalid WaveFormat Structure");
            waveFormatTag = (WaveFormatEncoding)br.ReadUInt16();
            channels = br.ReadInt16();
            sampleRate = br.ReadInt32();
            averageBytesPerSecond = br.ReadInt32();
            blockAlign = br.ReadInt16();
            bitsPerSample = br.ReadInt16();
            if (formatChunkLength > 16)
            {
                extraSize = br.ReadInt16();
                if (extraSize != formatChunkLength - 18)
                {
                    Debug.WriteLine("Format chunk mismatch");
                    extraSize = (short)(formatChunkLength - 18);
                }
            }
        }

        /// <summary>
        /// Reads a new WaveFormat object from a stream
        /// </summary>
        /// <param name="br">A binary reader that wraps the stream</param>
        public WaveFormat(BinaryReader br)
        {
            int formatChunkLength = br.ReadInt32();
            ReadWaveFormat(br, formatChunkLength);
        }

        /// <summary>
        /// Returns a string representation of the WaveFormat object.
        /// </summary>
        /// <returns>
        /// A string representing the wave format, including the number of bits per sample, the sample rate in Hertz, and the number of channels.
        /// </returns>
        /// <remarks>
        /// This method returns a string representation of the WaveFormat object based on the wave format tag.
        /// If the wave format tag is PCM or Extensible, it returns a string with the number of bits per sample, the sample rate in Hertz, and the number of channels.
        /// If the wave format tag is IEEEFloat, it returns a string with the number of bits per sample, the sample rate in Hertz, and the number of channels.
        /// If the wave format tag is neither PCM, Extensible, nor IEEEFloat, it returns the wave format tag as a string.
        /// </remarks>
        public override string ToString()
        {
            switch (waveFormatTag)
            {
                case WaveFormatEncoding.Pcm:
                case WaveFormatEncoding.Extensible:
                    // extensible just has some extra bits after the PCM header
                    return $"{bitsPerSample} bit PCM: {sampleRate}Hz {channels} channels";
                case WaveFormatEncoding.IeeeFloat:
                    return $"{bitsPerSample} bit IEEFloat: {sampleRate}Hz {channels} channels";
                default:
                    return waveFormatTag.ToString();
            }
        }

        /// <summary>
        /// Determines whether the current WaveFormat object is equal to another object.
        /// </summary>
        /// <param name="obj">The object to compare with the current WaveFormat object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            var other = obj as WaveFormat;
            if(other != null)
            {
                return waveFormatTag == other.waveFormatTag &&
                    channels == other.channels &&
                    sampleRate == other.sampleRate &&
                    averageBytesPerSecond == other.averageBytesPerSecond &&
                    blockAlign == other.blockAlign &&
                    bitsPerSample == other.bitsPerSample;
            }
            return false;
        }

        /// <summary>
        /// Computes the hash code for the WaveFormat instance.
        /// </summary>
        /// <returns>The computed hash code.</returns>
        public override int GetHashCode()
        {
            return (int) waveFormatTag ^ 
                (int) channels ^ 
                sampleRate ^ 
                averageBytesPerSecond ^ 
                (int) blockAlign ^ 
                (int) bitsPerSample;
        }

        /// <summary>
        /// Returns the encoding type used
        /// </summary>
        public WaveFormatEncoding Encoding => waveFormatTag;

        /// <summary>
        /// Serializes the wave format data and writes it to the specified BinaryWriter.
        /// </summary>
        /// <param name="writer">The BinaryWriter to which the wave format data will be written.</param>
        /// <remarks>
        /// This method serializes the wave format data including encoding, channels, sample rate, average bytes per second, block align, bits per sample, and extra size, and writes it to the specified BinaryWriter.
        /// </remarks>
        public virtual void Serialize(BinaryWriter writer)
        {
            writer.Write((int)(18 + extraSize)); // wave format length
            writer.Write((short)Encoding);
            writer.Write((short)Channels);
            writer.Write((int)SampleRate);
            writer.Write((int)AverageBytesPerSecond);
            writer.Write((short)BlockAlign);
            writer.Write((short)BitsPerSample);
            writer.Write((short)extraSize);
        }

        /// <summary>
        /// Returns the number of channels (1=mono,2=stereo etc)
        /// </summary>
        public int Channels => channels;

        /// <summary>
        /// Returns the sample rate (samples per second)
        /// </summary>
        public int SampleRate => sampleRate;

        /// <summary>
        /// Returns the average number of bytes used per second
        /// </summary>
        public int AverageBytesPerSecond => averageBytesPerSecond;

        /// <summary>
        /// Returns the block alignment
        /// </summary>
        public virtual int BlockAlign => blockAlign;

        /// <summary>
        /// Returns the number of bits per sample (usually 16 or 32, sometimes 24 or 8)
        /// Can be 0 for some codecs
        /// </summary>
        public int BitsPerSample => bitsPerSample;

        /// <summary>
        /// Returns the number of extra bytes used by this waveformat. Often 0,
        /// except for compressed formats which store extra data after the WAVEFORMATEX header
        /// </summary>
        public int ExtraSize => extraSize;
    }
}
