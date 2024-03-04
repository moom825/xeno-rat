using System;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace NAudio.Wave
{
    public static class WaveOutUtils
    {

        /// <summary>
        /// Gets the volume level of the specified wave output device.
        /// </summary>
        /// <param name="hWaveOut">The handle to the wave output device.</param>
        /// <param name="lockObject">An object used for synchronization.</param>
        /// <returns>The volume level of the wave output device as a floating-point number between 0.0 and 1.0.</returns>
        /// <exception cref="MmException">Thrown when an error occurs while retrieving the volume level.</exception>
        public static float GetWaveOutVolume(IntPtr hWaveOut, object lockObject)
        {
            int stereoVolume;
            MmResult result;
            lock (lockObject)
            {
                result = WaveInterop.waveOutGetVolume(hWaveOut, out stereoVolume);
            }
            MmException.Try(result, "waveOutGetVolume");
            return (stereoVolume & 0xFFFF) / (float)0xFFFF;
        }

        /// <summary>
        /// Sets the volume for the specified wave output device.
        /// </summary>
        /// <param name="value">The volume level to be set, ranging from 0.0 to 1.0.</param>
        /// <param name="hWaveOut">The handle to the wave output device.</param>
        /// <param name="lockObject">An object used for synchronization.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="value"/> is not within the range of 0.0 to 1.0.</exception>
        /// <remarks>
        /// This method sets the volume level for the specified wave output device using the <paramref name="hWaveOut"/> handle.
        /// The volume level is determined by the <paramref name="value"/> parameter, which should be within the range of 0.0 to 1.0.
        /// The method calculates the left and right channel volumes based on the provided value and then sets the stereo volume using the <see cref="WaveInterop.waveOutSetVolume"/> method.
        /// The operation is synchronized using the <paramref name="lockObject"/> for thread safety.
        /// </remarks>
        public static void SetWaveOutVolume(float value, IntPtr hWaveOut, object lockObject)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Volume must be between 0.0 and 1.0");
            if (value > 1) throw new ArgumentOutOfRangeException(nameof(value), "Volume must be between 0.0 and 1.0");
            float left = value;
            float right = value;

            int stereoVolume = (int)(left * 0xFFFF) + ((int)(right * 0xFFFF) << 16);
            MmResult result;
            lock (lockObject)
            {
                result = WaveInterop.waveOutSetVolume(hWaveOut, stereoVolume);
            }
            MmException.Try(result, "waveOutSetVolume");
        }

        /// <summary>
        /// Retrieves the current playback position in bytes from the specified wave output device.
        /// </summary>
        /// <param name="hWaveOut">A handle to the wave output device.</param>
        /// <param name="lockObject">An object used for synchronization to prevent concurrent access to the wave output device.</param>
        /// <exception cref="Exception">Thrown when the wave output device fails to retrieve the position in bytes.</exception>
        /// <returns>The current playback position in bytes.</returns>
        /// <remarks>
        /// This method retrieves the current playback position in bytes from the specified wave output device.
        /// It uses the MmTime structure to request the position in bytes and then checks if the retrieved type matches the requested type.
        /// If the retrieved type does not match, an exception is thrown with details about the mismatch.
        /// </remarks>
        public static long GetPositionBytes(IntPtr hWaveOut, object lockObject)
        {
            lock (lockObject)
            {
                var mmTime = new MmTime();
                mmTime.wType = MmTime.TIME_BYTES; // request results in bytes, TODO: perhaps make this a little more flexible and support the other types?
                MmException.Try(WaveInterop.waveOutGetPosition(hWaveOut, ref mmTime, Marshal.SizeOf(mmTime)), "waveOutGetPosition");

                if (mmTime.wType != MmTime.TIME_BYTES)
                    throw new Exception(string.Format("waveOutGetPosition: wType -> Expected {0}, Received {1}", MmTime.TIME_BYTES, mmTime.wType));

                return mmTime.cb;
            }
        }
    }
}
