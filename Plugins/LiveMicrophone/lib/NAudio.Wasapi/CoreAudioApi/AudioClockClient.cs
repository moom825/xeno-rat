using System;
using NAudio.CoreAudioApi.Interfaces;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi
{
    /// <summary>
    /// Audio Clock Client
    /// </summary>
    public class AudioClockClient : IDisposable
    {
        IAudioClock audioClockClientInterface;

        internal AudioClockClient(IAudioClock audioClockClientInterface)
        {
            this.audioClockClientInterface = audioClockClientInterface;

            //Stopwatch.GetTimestamp();
            //Stopwatch.Frequency
        }

        /// <summary>
        /// Characteristics
        /// </summary>
        public int Characteristics
        {
            get
            {
                Marshal.ThrowExceptionForHR(audioClockClientInterface.GetCharacteristics(out var characteristics));
                return (int)characteristics;
            }
        }

        /// <summary>
        /// Frequency
        /// </summary>
        public ulong Frequency
        {
            get
            {
                Marshal.ThrowExceptionForHR(audioClockClientInterface.GetFrequency(out var freq));
                return freq;
            }
        }

        /// <summary>
        /// Retrieves the current position of the audio clock and the corresponding QPC (QueryPerformanceCounter) position.
        /// </summary>
        /// <param name="position">When this method returns, contains the current position of the audio clock.</param>
        /// <param name="qpcPosition">When this method returns, contains the corresponding QPC position.</param>
        /// <returns>True if the position was successfully retrieved; otherwise, False.</returns>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when an error occurs while retrieving the position.</exception>
        public bool GetPosition(out ulong position, out ulong qpcPosition)
        {
            var hr = audioClockClientInterface.GetPosition(out position, out qpcPosition);
            if (hr == -1) return false;
            Marshal.ThrowExceptionForHR(hr);
            return true;
        }

        /// <summary>
        /// Adjusted Position
        /// </summary>
        public ulong AdjustedPosition
        {
            get
            {
                ulong pos, qpos;
                int cnt = 0;
                while (!GetPosition(out pos, out qpos))
                {
                    if (++cnt == 5)
                    {
                        // we've tried too many times, so now we have to just run with what we have...
                        break;
                    }
                }

                if (Stopwatch.IsHighResolution)
                {
                    // cool, we can adjust our position appropriately

                    // get the current qpc count (in ticks)
                    var qposNow = (ulong)((Stopwatch.GetTimestamp() * 10000000M) / Stopwatch.Frequency);

                    // find out how many ticks have passed since the device reported the position
                    var qposDiff = qposNow - qpos;

                    // find out how many device position units (usually bytes) would have played in that time span
                    var posDiff = (qposDiff * Frequency) / TimeSpan.TicksPerSecond;

                    // add it to the position
                    pos += posDiff;
                }
                return pos;
            }
        }

        /// <summary>
        /// Can Adjust Position
        /// </summary>
        public bool CanAdjustPosition => Stopwatch.IsHighResolution;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <remarks>
        /// This method releases the unmanaged resources used by the <see cref="audioClockClientInterface"/> and suppresses the finalization of the current object.
        /// </remarks>
        public void Dispose()
        {
            if (audioClockClientInterface != null)
            {
                // althugh GC would do this for us, we want it done now
                // to let us reopen WASAPI
                Marshal.ReleaseComObject(audioClockClientInterface);
                audioClockClientInterface = null;
                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }
}
