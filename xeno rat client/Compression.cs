using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace xeno_rat_client
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
        /// This method decompresses the data in the input buffer using the specified compression format. The decompressed data is stored in the output buffer, and the size of the decompressed data is returned in the <paramref name="FinalUncompressedSize"/> parameter.
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
        /// <param name="WorkspaceBuffer">The workspace buffer used for compression.</param>
        /// <returns>The status of the compression operation.</returns>
        [DllImport("ntdll.dll")]
        private static extern uint RtlCompressBuffer(ushort CompressionFormat, byte[] SourceBuffer, int SourceBufferLength, byte[] DestinationBuffer,
            int DestinationBufferLength, uint Unknown, out int pDestinationSize, IntPtr WorkspaceBuffer);

        /// <summary>
        /// Allocates memory in the local heap.
        /// </summary>
        /// <param name="uFlags">The memory allocation attributes. This parameter specifies the allocation attributes. This parameter can be zero or any combination of the following values: LMEM_FIXED, LMEM_MOVEABLE, LMEM_ZEROINIT, and LMEM_NOCOMPACT.</param>
        /// <param name="sizetdwBytes">The size of the memory block, in bytes. If this parameter is zero, the LocalAlloc function allocates a zero-length item and returns a valid pointer to that item.</param>
        /// <returns>A pointer to the allocated memory block if the function succeeds; otherwise, it returns NULL.</returns>
        /// <remarks>
        /// This method allocates memory in the local heap of the calling process. The allocated memory is automatically initialized to zero.
        /// If the LocalAlloc function succeeds, the return value is a handle to the newly allocated memory object.
        /// If the LocalAlloc function fails, the return value is NULL.
        /// </remarks>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LocalAlloc(int uFlags, IntPtr sizetdwBytes);

        /// <summary>
        /// Frees the memory block allocated by the LocalAlloc or LocalReAlloc function.
        /// </summary>
        /// <param name="hMem">A handle to the local memory object. This handle is returned by either the LocalAlloc or LocalReAlloc function.</param>
        /// <returns>If the function succeeds, the return value is NULL. If the function fails, the return value is equal to a handle to the local memory object. To get extended error information, call GetLastError.</returns>
        /// <remarks>
        /// This method frees the memory block allocated by the LocalAlloc or LocalReAlloc function.
        /// It is important to note that this method is used for freeing memory allocated in unmanaged code, and it should be used with caution to avoid memory leaks and access violations.
        /// </remarks>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LocalFree(IntPtr hMem);

        /// <summary>
        /// Compresses the input buffer using the LZNT1 compression format and returns the compressed data.
        /// </summary>
        /// <param name="buffer">The input buffer to be compressed.</param>
        /// <returns>The compressed data in the LZNT1 format.</returns>
        /// <remarks>
        /// This method compresses the input buffer using the LZNT1 compression format and returns the compressed data.
        /// It first calculates the required workspace size for compression using the RtlGetCompressionWorkSpaceSize function.
        /// If the compression operation is successful, it returns the compressed data; otherwise, it returns null.
        /// </remarks>
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
            ret = RtlCompressBuffer(COMPRESSION_FORMAT_LZNT1 | COMPRESSION_ENGINE_MAXIMUM, buffer, buffer.Length, outBuf, outBuf.Length, 0, out dstSize, hWork);
            if (ret != 0)
            {
                LocalFree(hWork);
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
        /// The size of the original data before compression is required to properly decompress the input buffer.
        /// </remarks>
        public static byte[] Decompress(byte[] buffer, int original_size)
        {
            int dwRet = 0;
            byte[] a = new byte[original_size];
            RtlDecompressBuffer(COMPRESSION_FORMAT_LZNT1, a, original_size, buffer, buffer.Length, out dwRet);
            return a;
        }
    }
}
