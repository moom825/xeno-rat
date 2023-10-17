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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace xeno_rat_server.Forms
{
    public partial class Chat : Form
    {
        Node client;
        public Chat(Node _client)//add a heartbeat...
        {
            client = _client;
            InitializeComponent();
            textBox1.Enabled = false;
            InitializeAsync();
        }
        private async Task InitializeAsync() 
        { 
            HeartBeat();
            await RecvThread();
        }
        public async Task HeartBeat() 
        {
            while (client.Connected()) 
            {
                await Task.Delay(2000);
                await client.SendAsync(new byte[] { 4 });
            }
        }
        public async Task RecvThread() 
        {
            while (client.Connected()) 
            {
                byte[] data=await client.ReceiveAsync();
                if (data == null) 
                {
                    break;
                }
                string message=Encoding.UTF8.GetString(data);
                textBox1.Invoke((MethodInvoker)(() =>
                {
                    textBox1.Text += "User: " + message + Environment.NewLine;
                    textBox1.SelectionStart = textBox1.Text.Length;
                    textBox1.ScrollToCaret();
                }));
            }
            if (!this.IsDisposed)
            {
                this.Invoke((MethodInvoker)(() =>
                {
                    this.Close();
                }));
            }
        }



        private async void button1_Click(object sender, EventArgs e)
        {
            string message = textBox2.Text;
            textBox2.Text = "";
            if (!await client.SendAsync(Encoding.UTF8.GetBytes(message))) 
            {
                this.Close();
            }
            textBox1.Text += "You: " + message + Environment.NewLine;
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.ScrollToCaret();
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                button1.PerformClick();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void Chat_Load(object sender, EventArgs e)
        {

        }
    }
}
