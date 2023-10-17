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
    public partial class FunMenu : Form
    {
        Node client;
        public FunMenu(Node _client)
        {
            client = _client;
            InitializeComponent();
            _=InitializeAsync();
        }
        private async Task InitializeAsync() 
        {
            await client.SendAsync(new byte[] { 2 });
        }
        private void FunMenu_Load(object sender, EventArgs e)
        {

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await client.SendAsync(new byte[] { 1 });
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            await client.SendAsync(new byte[] { 2 });
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            await client.SendAsync(new byte[] { 3 });
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            await client.SendAsync(new byte[] { 4 });
        }

        private async void trackBar1_Scroll(object sender, EventArgs e)
        {
            await client.SendAsync(new byte[] { 5 });
            await client.SendAsync(new byte[] { (byte)((TrackBar)sender).Value });
        }
    }
}
