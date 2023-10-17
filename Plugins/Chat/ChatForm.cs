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
using xeno_rat_client;

namespace Chat
{
    public partial class ChatForm : Form
    {
        Node server;
        bool recived_heartbeat;
        public ChatForm(Node _server)
        {
            server = _server;
            InitializeComponent();
            textBox1.Enabled = false;
            InitializeAsync();
        }
        private async Task InitializeAsync() 
        {
            Heartbeat();
            await RecvThread();
        }

        public async Task Heartbeat() 
        {
            while (server.Connected()) 
            {
                await Task.Delay(5000);
                if (recived_heartbeat) 
                {
                    recived_heartbeat = false;
                    continue;
                }
                server.Disconnect();
                break;
            }
        }
        public async Task RecvThread()
        {
            while (server.Connected())
            {
                byte[] data = await server.ReceiveAsync();
                if (data == null)
                {
                    break;
                }
                if (data.Length == 1) 
                {
                    if (data[0] == 4) 
                    {
                        recived_heartbeat = true;
                        continue;
                    }
                }
                string message = Encoding.UTF8.GetString(data);
                textBox1.BeginInvoke((MethodInvoker)(() =>
                {
                    textBox1.Text += "Admin: " + message + Environment.NewLine;
                    textBox1.SelectionStart = textBox1.Text.Length;
                    textBox1.ScrollToCaret();
                }));
            }
            Console.WriteLine("end!");
            if (!this.IsDisposed)
            {
                this.BeginInvoke((MethodInvoker)(() =>
                {
                    this.Close();
                }));
            }
        }
        private async void button1_Click(object sender, EventArgs e)
        {
            string message = textBox2.Text;
            textBox2.Text = "";
            await server.SendAsync(Encoding.UTF8.GetBytes(message));
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

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
