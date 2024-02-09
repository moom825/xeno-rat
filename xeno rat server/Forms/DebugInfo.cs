using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xeno_rat_server.Forms
{
    public partial class DebugInfo : Form
    {
        Node client;
        public DebugInfo(Node _client)
        {
            InitializeComponent();
            client = _client;
            AsyncInit();
        }

        public async Task AsyncInit() 
        {
            await ResetAndPopulateListView();
        }

        public async Task<string[]> GetDlls() 
        {
            await client.SendAsync(new byte[] { 4, 0 });
            byte[] dll_info_bytes = await client.ReceiveAsync();
            string dll_info = Encoding.UTF8.GetString(dll_info_bytes);
            return dll_info.Split('\n');
        }

        public async Task<bool> UnLoadDll(string dllname) 
        {
            byte[] payload = new byte[] { 4, 1 };
            payload = client.sock.Concat(payload, Encoding.UTF8.GetBytes(dllname));
            await client.SendAsync(payload);
            bool worked = (await client.ReceiveAsync())[0] == 1;
            return worked;
        }

        public async Task<string> getConsoleOutput() 
        {
            await client.SendAsync(new byte[] { 4, 2 });
            byte[] console_output_bytes = await client.ReceiveAsync();
            string console_output = Encoding.UTF8.GetString(console_output_bytes);
            return console_output;
        }

        public async Task<string> GetConsoleOutput() 
        {
            await client.SendAsync(new byte[] { 4, 2 });
            byte[] console_output_bytes = await client.ReceiveAsync();
            return Encoding.UTF8.GetString(console_output_bytes);
        }

        public async Task ResetAndPopulateListView() 
        {
            listView1.Items.Clear();
            foreach (string i in await GetDlls()) 
            {
                listView1.Items.Add(i);
            }
        }

        public async Task RemoveClick(string dllname) 
        {
            if (await UnLoadDll(dllname))
            {
                await Task.Run(() => MessageBox.Show("dll unloaded"));
            }
            else 
            {
                await Task.Run(() => MessageBox.Show("there was an issue unloading the dll"));
            }
            await ResetAndPopulateListView();
        }

        public async Task PopulateConsoleOutput() 
        {
            string console_output = await getConsoleOutput();
            richTextBox1.Text = console_output;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await ResetAndPopulateListView();
        }



        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripMenuItem menuItem = new ToolStripMenuItem("Unload");
            menuItem.Click += new EventHandler(async (_, __) => await RemoveClick(listView1.SelectedItems[0].Text));
            contextMenu.Items.Add(menuItem);
            contextMenu.Show(Cursor.Position);
        }

        private void listView1_ItemActivate(object sender, EventArgs e)
        {
            
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            await PopulateConsoleOutput();
        }
    }
}
