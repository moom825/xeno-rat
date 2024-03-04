using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace xeno_rat_server
{
    class Compression
    {
        const ushort COMPRESSION_FORMAT_LZNT1 = 2;
        const ushort COMPRESSION_ENGINE_MAXIMUM = 0x100;

        /// <summary>
        /// Retrieves the size of the workspace required for compression.
        /// </summary>
        /// <param name="CompressionFormat">The compression format.</param>
        /// <param name="pNeededBufferSize">Receives the size of the workspace required for compression.</param>
        /// <param name="Unknown">Receives an unknown value.</param>
        /// <returns>The status of the operation.</returns>
        [DllImport("ntdll.dll")]
        private static extern uint RtlGetCompressionWorkSpaceSize(ushort CompressionFormat, out uint pNeededBufferSize, out uint Unknown);

        /// <summary>
        /// Decompresses a buffer using the specified compression format and returns the decompressed data.
        /// </summary>
        /// <param name="CompressionFormat">The compression format used for the input buffer.</param>
        /// <param name="UncompressedBuffer">The buffer containing the uncompressed data.</param>
        /// <param name="UncompressedBufferSize">The size of the uncompressed data buffer.</param>
        /// <param name="CompressedBuffer">The buffer containing the compressed data to be decompressed.</param>
        /// <param name="CompressedBufferSize">The size of the compressed data buffer.</param>
        /// <param name="FinalUncompressedSize">When this method returns, contains the size of the decompressed data.</param>
        /// <returns>The status of the decompression operation. Zero indicates success; otherwise, an error occurred.</returns>
        /// <remarks>
        /// This method decompresses the data in the input buffer <paramref name="CompressedBuffer"/> using the specified compression format <paramref name="CompressionFormat"/>.
        /// The decompressed data is stored in the output buffer <paramref name="UncompressedBuffer"/>.
        /// The size of the decompressed data is stored in the output parameter <paramref name="FinalUncompressedSize"/>.
        /// </remarks>
        [DllImport("ntdll.dll")]
        private static extern uint RtlDecompressBuffer(ushort CompressionFormat, byte[] UncompressedBuffer, int UncompressedBufferSize, byte[] CompressedBuffer,
            int CompressedBufferSize, out int FinalUncompressedSize);

        /// <summary>
        /// Compresses the input buffer using the specified compression format and returns the compressed data in the destination buffer.
        /// </summary>
        /// <param name="CompressionFormat">The compression format to be used.</param>
        /// <param name="SourceBuffer">The input buffer to be compressed.</param>
        /// <param name="SourceBufferLength">The length of the input buffer.</param>
        /// <param name="DestinationBuffer">The buffer to store the compressed data.</param>
        /// <param name="DestinationBufferLength">The length of the destination buffer.</param>
        /// <param name="Unknown">Unknown parameter.</param>
        /// <param name="pDestinationSize">The size of the compressed data in the destination buffer.</param>
        /// <param name="WorkspaceBuffer">Workspace buffer for compression.</param>
        /// <returns>The status of the compression operation.</returns>
        /// <remarks>
        /// This method compresses the input buffer using the specified compression format and stores the compressed data in the destination buffer.
        /// The compression process modifies the original data in place.
        /// </remarks>
        [DllImport("ntdll.dll")]
        private static extern uint RtlCompressBuffer(ushort CompressionFormat, byte[] SourceBuffer, int SourceBufferLength, byte[] DestinationBuffer,
            int DestinationBufferLength, uint Unknown, out int pDestinationSize, IntPtr WorkspaceBuffer);

        /// <summary>
        /// Allocates the specified number of bytes in the local heap and returns a pointer to the allocated memory.
        /// </summary>
        /// <param name="uFlags">The memory allocation attributes. This parameter specifies the allocation and access protection attributes of the memory block. </param>
        /// <param name="sizetdwBytes">The size of the memory block, in bytes. If this parameter is zero, the LocalAlloc function allocates a zero-length item and returns a valid pointer.</param>
        /// <returns>A pointer to the newly allocated memory, or <see cref="IntPtr.Zero"/> if the function fails.</returns>
        /// <exception cref="OutOfMemoryException">The LocalAlloc function has insufficient memory to satisfy the request.</exception>
        /// <exception cref="ArgumentException">The LocalAlloc function could not allocate the requested number of bytes.</exception>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LocalAlloc(int uFlags, IntPtr sizetdwBytes);

        /// <summary>
        /// Frees the memory block allocated by LocalAlloc and LocalReAlloc and invalidates the handle.
        /// </summary>
        /// <param name="hMem">A handle to the local memory object.</param>
        /// <returns>If the function succeeds, the return value is NULL. If the function fails, the return value is equal to a handle to the local memory object. To get extended error information, call GetLastError.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LocalFree(IntPtr hMem);

        /// <summary>
        /// Compresses the input buffer using the LZNT1 compression format and returns the compressed data.
        /// </summary>
        /// <param name="buffer">The input buffer to be compressed.</param>
        /// <returns>The compressed data in the LZNT1 format.</returns>
        /// <exception cref="System.Exception">Thrown when the compression process fails.</exception>
        public static byte[] Compress(byte[] buffer)
        {
            var outBuf = new byte[buffer.Length * 6];
            uint dwSize = 0, dwRet = 0;
            uint ret = RtlGetCompressionWorkSpaceSize(COMPRESSION_FORMAT_LZNT1 | COMPRESSION_ENGINE_MAXIMUM, out dwSize, out dwRet);
            if (ret != 0)
            {
                return null;
            }
            int dstSize = 0;
            IntPtr hWork = LocalAlloc(0, new IntPtr(dwSize));
            ret = RtlCompressBuffer(COMPRESSION_FORMAT_LZNT1 | COMPRESSION_ENGINE_MAXIMUM, buffer,
                buffer.Length, outBuf, outBuf.Length, 0, out dstSize, hWork);
            if (ret != 0)
            {
                return null;
            }
            LocalFree(hWork);
            Array.Resize(ref outBuf, dstSize);
            return outBuf;
        }

        /// <summary>
        /// Decompresses the input buffer using the LZNT1 compression format and returns the decompressed data.
        /// </summary>
        /// <param name="buffer">The compressed data to be decompressed.</param>
        /// <param name="original_size">The size of the original data before compression.</param>
        /// <returns>The decompressed data as a byte array.</returns>
        /// <remarks>
        /// This method decompresses the input buffer using the LZNT1 compression format and returns the decompressed data as a new byte array.
        /// The size of the original data before compression must be provided as the <paramref name="original_size"/> parameter.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown when the decompression operation fails or the input buffer is invalid.</exception>
        public static byte[] Decompress(byte[] buffer, int original_size)
        {
            int dwRet = 0;
            byte[] a = new byte[original_size];
            RtlDecompressBuffer(COMPRESSION_FORMAT_LZNT1, a, original_size, buffer, buffer.Length, out dwRet);
            return a;
        }
    }
}
