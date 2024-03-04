namespace NAudio.Utils
{
    /// <summary>
    /// Helper methods for working with audio buffers
    /// </summary>
    public static class BufferHelpers
    {

        /// <summary>
        /// Ensures that the input buffer has at least the specified number of samples and returns the buffer.
        /// </summary>
        /// <param name="buffer">The input buffer to be checked and potentially resized.</param>
        /// <param name="samplesRequired">The number of samples required in the buffer.</param>
        /// <returns>
        /// The input buffer if it has at least the specified number of samples; otherwise, a new buffer with the specified number of samples.
        /// </returns>
        public static byte[] Ensure(byte[] buffer, int bytesRequired)
        {
            if (buffer == null || buffer.Length < bytesRequired)
            {
                buffer = new byte[bytesRequired];
            }
            return buffer;
        }

        /// <summary>
        /// Ensures the buffer is big enough
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="samplesRequired"></param>
        /// <returns></returns>
        public static float[] Ensure(float[] buffer, int samplesRequired)
        {
            if (buffer == null || buffer.Length < samplesRequired)
            {
                buffer = new float[samplesRequired];
            }
            return buffer;
        }
    }
}
