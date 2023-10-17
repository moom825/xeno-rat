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
    public partial class Shell : Form
    {
        Node client;
        public Shell(Node _client)
        {
            client = _client;
            InitializeComponent();
            RecvThread();
        }
        public async Task RecvThread() 
        {
            while (client.Connected())
            {
                byte[] data = await client.ReceiveAsync();
                if (data == null)
                {
                    break;
                }
                textBox1.BeginInvoke((Action)(() =>
                {
                    textBox1.Text += Encoding.UTF8.GetString(data) + System.Environment.NewLine;
                }));
            }
        }

        private void Shell_Load(object sender, EventArgs e)
        {

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
            await client.SendAsync(new byte[] { 1 });
            //cmd
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
            await client.SendAsync(new byte[] { 2 });
            //powershell
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            await client.SendAsync(new byte[] { 0 });
            await client.SendAsync(Encoding.UTF8.GetBytes(textBox2.Text));
            textBox2.Clear();
            //enter
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                button3.PerformClick();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void textBox1_VisibleChanged(object sender, EventArgs e)
        {
            if (textBox1.Visible)
            {
                textBox1.SelectionStart = textBox1.TextLength;
                textBox1.ScrollToCaret();
            }
        }
    }
}
