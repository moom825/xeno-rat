using System;

namespace NAudio.Wave
{
    /// <summary>
    /// Wave Callback Info
    /// </summary>
    public class WaveCallbackInfo
    {
        /// <summary>
        /// Callback Strategy
        /// </summary>
        public WaveCallbackStrategy Strategy { get; private set; }
        /// <summary>
        /// Window Handle (if applicable)
        /// </summary>
        public IntPtr Handle { get; private set; }

        private WaveWindow waveOutWindow;
        private WaveWindowNative waveOutWindowNative;

        /// <summary>
        /// Returns a new instance of WaveCallbackInfo with WaveCallbackStrategy set to FunctionCallback and IntPtr set to Zero.
        /// </summary>
        /// <returns>A new instance of WaveCallbackInfo with specified WaveCallbackStrategy and IntPtr.</returns>
        public static WaveCallbackInfo FunctionCallback()
        {
            return new WaveCallbackInfo(WaveCallbackStrategy.FunctionCallback, IntPtr.Zero);
        }

        /// <summary>
        /// Creates a new instance of WaveCallbackInfo with WaveCallbackStrategy set to NewWindow and IntPtr set to Zero.
        /// </summary>
        /// <returns>A new instance of WaveCallbackInfo with WaveCallbackStrategy set to NewWindow and IntPtr set to Zero.</returns>
        public static WaveCallbackInfo NewWindow()
        {
            return new WaveCallbackInfo(WaveCallbackStrategy.NewWindow, IntPtr.Zero);
        }

        /// <summary>
        /// Returns a new WaveCallbackInfo object with the specified WaveCallbackStrategy and handle.
        /// </summary>
        /// <param name="handle">The handle of the existing window.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="handle"/> is zero.</exception>
        /// <returns>A new WaveCallbackInfo object with the specified WaveCallbackStrategy and handle.</returns>
        public static WaveCallbackInfo ExistingWindow(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                throw new ArgumentException("Handle cannot be zero");
            }
            return new WaveCallbackInfo(WaveCallbackStrategy.ExistingWindow, handle);
        }

        private WaveCallbackInfo(WaveCallbackStrategy strategy, IntPtr handle)
        {
            this.Strategy = strategy;
            this.Handle = handle;
        }

        /// <summary>
        /// Connects the audio output to the specified wave callback.
        /// </summary>
        /// <param name="callback">The wave callback to connect to.</param>
        /// <exception cref="InvalidOperationException">Thrown when the strategy is not recognized.</exception>
        /// <remarks>
        /// This method connects the audio output to the specified wave callback based on the strategy provided.
        /// If the strategy is set to WaveCallbackStrategy.NewWindow, a new WaveWindow is created and the audio output is connected to it.
        /// If the strategy is set to WaveCallbackStrategy.ExistingWindow, the audio output is connected to the existing window handle.
        /// </remarks>
        internal void Connect(WaveInterop.WaveCallback callback)
        {
            if (Strategy == WaveCallbackStrategy.NewWindow)
            {
                waveOutWindow = new WaveWindow(callback);
                waveOutWindow.CreateControl();
                this.Handle = waveOutWindow.Handle;
            }
            else if (Strategy == WaveCallbackStrategy.ExistingWindow)
            {
                waveOutWindowNative = new WaveWindowNative(callback);
                waveOutWindowNative.AssignHandle(this.Handle);
            }
        }

        /// <summary>
        /// Opens a waveform-audio output device for playback.
        /// </summary>
        /// <param name="waveOutHandle">When this method returns, contains a handle to the opened waveform-audio output device.</param>
        /// <param name="deviceNumber">The device to be used for playback.</param>
        /// <param name="waveFormat">An instance of the WaveFormat class that specifies the format of the waveform-audio data.</param>
        /// <param name="callback">The callback method to be used for playback.</param>
        /// <returns>An MmResult value indicating the success or failure of the operation.</returns>
        /// <remarks>
        /// This method opens a waveform-audio output device for playback. The device is specified by <paramref name="deviceNumber"/>.
        /// If the <see cref="Strategy"/> property is set to WaveCallbackStrategy.FunctionCallback, the device is opened using the specified callback function.
        /// If the <see cref="Strategy"/> property is set to WaveCallbackStrategy.WindowCallback, the device is opened using the specified window handle for callback notifications.
        /// </remarks>
        internal MmResult WaveOutOpen(out IntPtr waveOutHandle, int deviceNumber, WaveFormat waveFormat, WaveInterop.WaveCallback callback)
        {
            MmResult result;
            if (Strategy == WaveCallbackStrategy.FunctionCallback)
            {
                result = WaveInterop.waveOutOpen(out waveOutHandle, (IntPtr)deviceNumber, waveFormat, callback, IntPtr.Zero, WaveInterop.WaveInOutOpenFlags.CallbackFunction);
            }
            else
            {
                result = WaveInterop.waveOutOpenWindow(out waveOutHandle, (IntPtr)deviceNumber, waveFormat, this.Handle, IntPtr.Zero, WaveInterop.WaveInOutOpenFlags.CallbackWindow);
            }
            return result;
        }

        /// <summary>
        /// Opens a waveform-audio input device for recording.
        /// </summary>
        /// <param name="waveInHandle">When this method returns, contains a handle identifying the open waveform-audio input device.</param>
        /// <param name="deviceNumber">The device to be used for waveform-audio input.</param>
        /// <param name="waveFormat">An instance of the WaveFormat class that specifies the format of the waveform-audio data.</param>
        /// <param name="callback">The address of a fixed callback function or a handle to a window that will receive callback information.</param>
        /// <returns>An MmResult value representing the result of the operation.</returns>
        /// <remarks>
        /// This method opens a waveform-audio input device for recording. The device is identified by its device number, which is a value between zero and one less than the number of devices present.
        /// The <paramref name="waveInHandle"/> parameter is used to identify the device in all subsequent calls to the waveform-audio input functions.
        /// The <paramref name="waveFormat"/> parameter specifies the format of the waveform-audio data to be recorded.
        /// The <paramref name="callback"/> parameter can be either the address of a fixed callback function or a handle to a window that will receive callback information.
        /// The <paramref name="callback"/> parameter is used only if the Strategy property is set to WaveCallbackStrategy.FunctionCallback; otherwise, it is ignored.
        /// </remarks>
        internal MmResult WaveInOpen(out IntPtr waveInHandle, int deviceNumber, WaveFormat waveFormat, WaveInterop.WaveCallback callback)
        {
            MmResult result;
            if (Strategy == WaveCallbackStrategy.FunctionCallback)
            {
                result = WaveInterop.waveInOpen(out waveInHandle, (IntPtr)deviceNumber, waveFormat, callback, IntPtr.Zero, WaveInterop.WaveInOutOpenFlags.CallbackFunction);
            }
            else
            {
                result = WaveInterop.waveInOpenWindow(out waveInHandle, (IntPtr)deviceNumber, waveFormat, this.Handle, IntPtr.Zero, WaveInterop.WaveInOutOpenFlags.CallbackWindow);
            }
            return result;
        }

        /// <summary>
        /// Disconnects the waveOutWindow and waveOutWindowNative resources.
        /// </summary>
        internal void Disconnect()
        {
            if (waveOutWindow != null)
            {
                waveOutWindow.Close();
                waveOutWindow = null;
            }
            if (waveOutWindowNative != null)
            {
                waveOutWindowNative.ReleaseHandle();
                waveOutWindowNative = null;
            }
        }
    }
}
