namespace NAudio.Wave
{
    /// <summary>
    /// Buffered WaveProvider taking source data from WaveIn
    /// </summary>
    public class WaveInProvider : IWaveProvider
    {
        private readonly IWaveIn waveIn;
        private readonly BufferedWaveProvider bufferedWaveProvider;

        /// <summary>
        /// Creates a new WaveInProvider
        /// n.b. Should make sure the WaveFormat is set correctly on IWaveIn before calling
        /// </summary>
        /// <param name="waveIn">The source of wave data</param>
        public WaveInProvider(IWaveIn waveIn)
        {
            this.waveIn = waveIn;
            waveIn.DataAvailable += OnDataAvailable;
            bufferedWaveProvider = new BufferedWaveProvider(WaveFormat);
        }

        /// <summary>
        /// Adds the available data to the buffered wave provider.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">An object that contains the event data.</param>
        /// <remarks>
        /// This method adds the available data from the <paramref name="e"/> to the <paramref name="bufferedWaveProvider"/>.
        /// </remarks>
        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }

        /// <summary>
        /// Reads a sequence of bytes from the current <see cref="BufferedWaveProvider"/> and advances the position within the buffer by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current source.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The total number of bytes read into the buffer. This might be less than the number of bytes requested if that many bytes are not currently available, or zero if the end of the stream is reached.</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            return bufferedWaveProvider.Read(buffer, offset, count);
        }

        /// <summary>
        /// The WaveFormat
        /// </summary>
        public WaveFormat WaveFormat => waveIn.WaveFormat;
    }
}
