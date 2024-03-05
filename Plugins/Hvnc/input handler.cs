using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Hidden_handler
{
    public class input_handler
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

        /// <summary>
        /// Opens the specified desktop object.
        /// </summary>
        /// <param name="lpszDesktop">The name of the desktop to be opened.</param>
        /// <param name="dwFlags">Reserved; set to 0.</param>
        /// <param name="fInherit">If this value is TRUE, processes created by this process will inherit the handle. Otherwise, the processes do not inherit this handle.</param>
        /// <param name="dwDesiredAccess">The access to the desktop. For a list of access rights, see Desktop Security and Access Rights.</param>
        /// <returns>If the function succeeds, the return value is an open handle to the specified desktop.</returns>
        /// <exception cref="System.ComponentModel.Win32Exception">Thrown when the function fails. Use the Marshal.GetLastWin32Error method to get the error code.</exception>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr OpenDesktop(string lpszDesktop, int dwFlags, bool fInherit, uint dwDesiredAccess);

        /// <summary>
        /// Creates a new desktop with the specified name and settings.
        /// </summary>
        /// <param name="lpszDesktop">The name of the new desktop.</param>
        /// <param name="lpszDevice">A handle to the device to use when creating the desktop.</param>
        /// <param name="pDevmode">A pointer to a DEVMODE structure that specifies the mode to use for the new desktop.</param>
        /// <param name="dwFlags">The desktop creation flags. For a list of possible values, see https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-createdesktopw.</param>
        /// <param name="dwDesiredAccess">The access rights for the desktop. For a list of possible values, see https://docs.microsoft.com/en-us/windows/win32/secauthz/desktop-security-and-access-rights.</param>
        /// <param name="lpsa">A pointer to a SECURITY_ATTRIBUTES structure that determines whether the returned handle can be inherited by child processes. If lpsa is NULL, the handle cannot be inherited.</param>
        /// <returns>
        /// If the function succeeds, the return value is a handle to the newly created desktop. If the function fails, the return value is NULL.
        /// </returns>
        /// <exception cref="System.ComponentModel.Win32Exception">Thrown when an error occurs during the creation of the desktop. The exception contains the error code.</exception>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateDesktop(string lpszDesktop, IntPtr lpszDevice,
            IntPtr pDevmode, int dwFlags, uint dwDesiredAccess, IntPtr lpsa);

        /// <summary>
        /// Closes the specified desktop.
        /// </summary>
        /// <param name="hDesktop">A handle to the desktop to be closed.</param>
        /// <returns>True if the desktop is successfully closed; otherwise, false.</returns>
        /// <remarks>
        /// This method closes the desktop identified by the handle <paramref name="hDesktop"/>.
        /// If the function succeeds, the return value is true. If the function fails, the return value is false.
        /// To get extended error information, call GetLastError.
        /// </remarks>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseDesktop(IntPtr hDesktop);

        /// <summary>
        /// Sets the desktop for the current thread to the specified desktop.
        /// </summary>
        /// <param name="hDesktop">A handle to the desktop. This parameter can be NULL, to indicate the default desktop for the window station specified by the lpDesktop parameter of the OpenWindowStation function used to open the window station.</param>
        /// <returns>True if the function succeeds; otherwise, false. To get extended error information, call GetLastError.</returns>
        /// <exception cref="System.ComponentModel.Win32Exception">Thrown when the function fails. Use GetLastError to obtain the error code.</exception>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetThreadDesktop(IntPtr hDesktop);

        /// <summary>
        /// Retrieves a handle to the window that contains the specified point.
        /// </summary>
        /// <param name="point">A POINT structure that defines the point to be checked.</param>
        /// <returns>A handle to the window that contains the specified point, or NULL if no window is found.</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT point);

        /// <summary>
        /// Sends the specified message to a window or windows. The SendMessage function calls the window procedure for the specified window and does not return until the window procedure has processed the message.
        /// </summary>
        /// <param name="hWnd">A handle to the window whose window procedure will receive the message.</param>
        /// <param name="msg">The message to be sent.</param>
        /// <param name="wParam">Additional message-specific information.</param>
        /// <param name="lParam">Additional message-specific information.</param>
        /// <returns>The result of the message processing; it depends on the message sent.</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Sends the specified message to a window or windows. The function calls the window procedure for the specified window and does not return until the window procedure has processed the message.
        /// </summary>
        /// <param name="hWnd">A handle to the window whose window procedure will receive the message.</param>
        /// <param name="msg">The message to be posted.</param>
        /// <param name="wParam">Additional message-specific information.</param>
        /// <param name="lParam">Additional message-specific information.</param>
        /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero.</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Retrieves the dimensions of the bounding rectangle of the specified window.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="lpRect">A pointer to a RECT structure that receives the screen coordinates of the upper-left and lower-right corners of the window.</param>
        /// <returns>true if the function succeeds, otherwise false.</returns>
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>
        /// Transforms screen coordinates into client-area coordinates.
        /// </summary>
        /// <param name="hWnd">A handle to the window whose client area will be used for the transformation.</param>
        /// <param name="lpPoint">A reference to a POINT structure that contains the screen coordinates to be transformed. Upon successful completion, this structure contains the client-area coordinates.</param>
        /// <returns>True if the function succeeds, otherwise false.</returns>
        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        /// <summary>
        /// Retrieves a handle to the child window at the specified point.
        /// </summary>
        /// <param name="hWnd">A handle to the parent window.</param>
        /// <param name="point">The client coordinates of the point to be checked.</param>
        /// <returns>A handle to the child window that contains the specified point, or IntPtr.Zero if the point is not within any child window.</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr ChildWindowFromPoint(IntPtr hWnd, POINT point);

        /// <summary>
        /// Retrieves the text of the specified window's title bar, if it has one.
        /// </summary>
        /// <param name="hWnd">A handle to the window or control containing the text.</param>
        /// <param name="lpString">The buffer that will receive the text.</param>
        /// <param name="nMaxCount">The maximum number of characters to copy to the buffer, including the null-terminating character.</param>
        /// <returns>
        /// If the function succeeds, the return value is the length, in characters, of the copied string, not including the terminating null character.
        /// If the window has no title bar or text, if the title bar is empty, or if the window or control handle is invalid, the return value is zero.
        /// To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        /// <summary>
        /// Determines whether the specified point is within the specified rectangle.
        /// </summary>
        /// <param name="lprc">A reference to a RECT structure that contains the coordinates of the rectangle.</param>
        /// <param name="pt">A POINT structure that contains the coordinates of the point to be tested.</param>
        /// <returns>True if the point is within the rectangle; otherwise, false.</returns>
        [DllImport("user32.dll")]
        public static extern bool PtInRect(ref RECT lprc, POINT pt);

        /// <summary>
        /// Changes an attribute of the specified window. The function also sets a new value for the attribute, if needed.
        /// </summary>
        /// <param name="hWnd">A handle to the window and, indirectly, the class to which the window belongs.</param>
        /// <param name="nIndex">The zero-based offset to the value to be set.</param>
        /// <param name="dwNewLong">The replacement value.</param>
        /// <returns>
        /// If the function succeeds, the return value is the previous value of the specified 32-bit integer.
        /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("user32.dll")]
        public static extern bool SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        /// <summary>
        /// Retrieves information about the specified window. The function also retrieves the value at a specified offset into the extra window memory.
        /// </summary>
        /// <param name="hWnd">A handle to the window and, indirectly, the class to which the window belongs.</param>
        /// <param name="nIndex">The zero-based offset to the value to be retrieved.</param>
        /// <returns>The requested 32-bit value at the specified offset.</returns>
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        /// <summary>
        /// Retrieves the show state and the restored, minimized, and maximized positions of the specified window.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="lpwndpl">A reference to a WINDOWPLACEMENT structure that receives the show state and position information.</param>
        /// <returns>True if the function succeeds, otherwise false.</returns>
        [DllImport("user32.dll")]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        /// <summary>
        /// Finds a window with the specified class name and window name.
        /// </summary>
        /// <param name="lpClassName">The class name of the window to find.</param>
        /// <param name="lpWindowName">The window name (title) of the window to find.</param>
        /// <returns>The handle to the window if it is found; otherwise, IntPtr.Zero.</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        /// <summary>
        /// Retrieves the menu item that is at the specified location.
        /// </summary>
        /// <param name="hWnd">A handle to the window that contains the menu.</param>
        /// <param name="hMenu">A handle to the menu.</param>
        /// <param name="pt">A POINT structure that defines the location of the mouse cursor, in screen coordinates.</param>
        /// <returns>The identifier of the menu item at the specified location. If no such menu item exists, the return value is -1.</returns>
        [DllImport("user32.dll")]
        public static extern int MenuItemFromPoint(IntPtr hWnd, IntPtr hMenu, POINT pt);

        /// <summary>
        /// Retrieves the menu item identifier of a menu item located at the specified position in a menu.
        /// </summary>
        /// <param name="hMenu">A handle to the menu that contains the item.</param>
        /// <param name="nPos">The zero-based relative position of the menu item.</param>
        /// <returns>The identifier of the specified menu item.</returns>
        [DllImport("user32.dll")]
        public static extern int GetMenuItemID(IntPtr hMenu, int nPos);

        /// <summary>
        /// Retrieves a handle to the drop-down menu activated by the specified menu item.
        /// </summary>
        /// <param name="hMenu">A handle to the menu that contains the item for which the drop-down menu handle is to be retrieved.</param>
        /// <param name="nPos">The zero-based relative position of the menu item. This parameter can be either a menu handle or a menu-item identifier.</param>
        /// <returns>
        /// If the function succeeds, the return value is a handle to the drop-down menu.
        /// If the function fails, the return value is NULL.
        /// </returns>
        [DllImport("user32.dll")]
        public static extern IntPtr GetSubMenu(IntPtr hMenu, int nPos);

        /// <summary>
        /// Moves and resizes the specified window.
        /// </summary>
        /// <param name="hWnd">A handle to the window to be moved and resized.</param>
        /// <param name="x">The new position of the left side of the window.</param>
        /// <param name="y">The new position of the top of the window.</param>
        /// <param name="width">The new width of the window.</param>
        /// <param name="height">The new height of the window.</param>
        /// <param name="repaint">true to repaint the window after it is moved and sized; otherwise, false.</param>
        [DllImport("user32.dll")]
        public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

        /// <summary>
        /// Retrieves the name of the window class to which the specified window belongs.
        /// </summary>
        /// <param name="hwnd">A handle to the window and, indirectly, the class to which the window belongs.</param>
        /// <param name="pszType">A pointer to the buffer that will receive the class name string.</param>
        /// <param name="cchType">The length of the buffer pointed to by the <paramref name="pszType"/> parameter.</param>
        /// <returns>
        /// If the function succeeds, the return value is the number of characters copied to the buffer, not including the terminating null character.
        /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int RealGetWindowClass(IntPtr hwnd, [Out] StringBuilder pszType, int cchType);


        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }


        private const int GWL_STYLE = -16;
        private const int WS_DISABLED = 0x8000000;

        private const int WM_CHAR = 0x0102;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_CLOSE = 0x0010;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MINIMIZE = 0xF020;
        private const int SC_RESTORE = 0xF120;
        private const int SC_MAXIMIZE = 0xF030;
        private const int HTCAPTION = 2;
        private const int HTTOP = 12;
        private const int HTBOTTOM = 15;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int HTCLOSE = 20;
        private const int HTMINBUTTON = 8;
        private const int HTMAXBUTTON = 9;
        private const int HTTRANSPARENT = -1;
        private const int VK_RETURN = 0x0D;
        private const int MN_GETHMENU = 0x01E1;
        private const int BM_CLICK = 0x00F5;

        private const int MAX_PATH = 260;
        private const int WM_NCHITTEST = 0x0084;
        private const int SW_SHOWMAXIMIZED = 3;
        private POINT lastPoint = new POINT() {x=0,y=0};
        private IntPtr hResMoveWindow = IntPtr.Zero;
        private IntPtr resMoveType = IntPtr.Zero;
        private bool lmouseDown = false;

        private static object lockObject = new object();

        string DesktopName = null;
        public IntPtr Desktop = IntPtr.Zero;
        public input_handler(string DesktopName)
        {
            this.DesktopName = DesktopName;
            IntPtr Desk = OpenDesktop(DesktopName, 0, true, (uint)DESKTOP_ACCESS.GENERIC_ALL);
            if (Desk == IntPtr.Zero)
            {
                Desk = CreateDesktop(DesktopName, IntPtr.Zero, IntPtr.Zero, 0, (uint)DESKTOP_ACCESS.GENERIC_ALL, IntPtr.Zero);
            }
            Desktop = Desk;
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
        /// Extracts the x-coordinate from the specified <paramref name="lParam"/>.
        /// </summary>
        /// <param name="lParam">The pointer to the message's lParam parameter.</param>
        /// <returns>The x-coordinate value extracted from <paramref name="lParam"/>.</returns>
        public static int GET_X_LPARAM(IntPtr lParam)
        {
            return (short)(lParam.ToInt32() & 0xFFFF);
        }

        /// <summary>
        /// Extracts the Y coordinate from the given Windows message <paramref name="lParam"/>.
        /// </summary>
        /// <param name="lParam">The Windows message parameter containing the coordinates.</param>
        /// <returns>The Y coordinate extracted from the <paramref name="lParam"/>.</returns>
        public static int GET_Y_LPARAM(IntPtr lParam)
        {
            return (short)((lParam.ToInt32() >> 16) & 0xFFFF);
        }

        /// <summary>
        /// Combines the specified low and high words into a single IntPtr value.
        /// </summary>
        /// <param name="lowWord">The low-order word.</param>
        /// <param name="highWord">The high-order word.</param>
        /// <returns>An IntPtr value representing the combined low and high words.</returns>
        public static IntPtr MAKELPARAM(int lowWord, int highWord)
        {
            int lParam = (highWord << 16) | (lowWord & 0xFFFF);
            return new IntPtr(lParam);
        }

        /// <summary>
        /// Handles input messages and performs corresponding actions based on the message type.
        /// </summary>
        /// <param name="msg">The input message to be handled.</param>
        /// <param name="wParam">Additional message information.</param>
        /// <param name="lParam">Additional message information.</param>
        /// <remarks>
        /// This method handles input messages and performs corresponding actions based on the message type. It locks the <see cref="lockObject"/> to ensure thread safety. It sets the thread desktop to the specified <see cref="Desktop"/>. It then processes the input message by identifying its type and performing the appropriate actions. If the message is a keyboard-related message (e.g., WM_CHAR, WM_KEYDOWN, WM_KEYUP), it retrieves the window handle based on the last known point and performs the necessary actions. If the message is a mouse-related message, it processes the mouse coordinates and performs actions such as clicking buttons, handling menu items, or moving windows based on the message type. The method also ensures that the original array is modified in place.
        /// </remarks>
        public void Input(uint msg, IntPtr wParam, IntPtr lParam)
        {
            lock (lockObject) 
            { 
                SetThreadDesktop(Desktop);
                IntPtr hWnd = IntPtr.Zero;
                POINT point;
                POINT lastPointCopy;
                bool mouseMsg = false;
                switch (msg)
                {
                    case WM_CHAR:
                    case WM_KEYDOWN:
                    case WM_KEYUP:
                        {
                            point = lastPoint;
                            hWnd = WindowFromPoint(point);
                            break;
                        }
                    default:
                        {
                            mouseMsg = true;
                            point.x = GET_X_LPARAM(lParam);
                            point.y = GET_Y_LPARAM(lParam);
                            lastPointCopy = lastPoint;
                            lastPoint = point;
                            hWnd = WindowFromPoint(point);
                            if (msg == WM_LBUTTONUP)
                            {
                                lmouseDown = false;
                                IntPtr lResult = SendMessage(hWnd, WM_NCHITTEST, IntPtr.Zero, lParam);

                                switch (lResult.ToInt32())
                                {
                                    case HTTRANSPARENT:
                                        {
                                            SetWindowLong(hWnd, GWL_STYLE, GetWindowLong(hWnd, GWL_STYLE) | WS_DISABLED);
                                            lResult = SendMessage(hWnd, WM_NCHITTEST, IntPtr.Zero, lParam);
                                            break;
                                        }
                                    case HTCLOSE:
                                        {
                                            PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                                            break;
                                        }
                                    case HTMINBUTTON:
                                        {
                                            PostMessage(hWnd, WM_SYSCOMMAND, new IntPtr(SC_MINIMIZE), IntPtr.Zero);
                                            break;
                                        }
                                    case HTMAXBUTTON:
                                        {
                                            WINDOWPLACEMENT windowPlacement = new WINDOWPLACEMENT();
                                            windowPlacement.length = Marshal.SizeOf(windowPlacement);
                                            GetWindowPlacement(hWnd, ref windowPlacement);
                                            if ((windowPlacement.flags & SW_SHOWMAXIMIZED) != 0)
                                                PostMessage(hWnd, WM_SYSCOMMAND, new IntPtr(SC_RESTORE), IntPtr.Zero);
                                            else
                                                PostMessage(hWnd, WM_SYSCOMMAND, new IntPtr(SC_MAXIMIZE), IntPtr.Zero);
                                            break;
                                        }
                                }
                                break;
                            }
                            else if (msg == WM_LBUTTONDOWN)
                            {
                                lmouseDown = true;
                                hResMoveWindow = IntPtr.Zero;

                                RECT startButtonRect;
                                IntPtr hStartButton = FindWindow("Button", null);
                                GetWindowRect(hStartButton, out startButtonRect);
                                if (PtInRect(ref startButtonRect, point))
                                {
                                    PostMessage(hStartButton, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                                    return;
                                }
                                else
                                {
                                    StringBuilder windowClass = new StringBuilder(MAX_PATH);
                                    RealGetWindowClass(hWnd, windowClass, MAX_PATH);

                                    if (windowClass.ToString() == "#32768")
                                    {
                                        IntPtr hMenu = GetSubMenu(hWnd, 0);
                                        int itemPos = MenuItemFromPoint(IntPtr.Zero, hMenu, point);
                                        int itemId = GetMenuItemID(hMenu, itemPos);
                                        PostMessage(hWnd, 0x1E5, new IntPtr(itemPos), IntPtr.Zero);
                                        PostMessage(hWnd, WM_KEYDOWN, new IntPtr(VK_RETURN), IntPtr.Zero);
                                        return;
                                    }
                                }
                            }
                            else if (msg == WM_MOUSEMOVE)
                            {
                                if (!lmouseDown)
                                    break;

                                if (hResMoveWindow == IntPtr.Zero)
                                    resMoveType = SendMessage(hWnd, WM_NCHITTEST, IntPtr.Zero, lParam);
                                else 
                                {
                                    hWnd = hResMoveWindow;
                                }
                                
                                int moveX = lastPointCopy.x - point.x;
                                int moveY = lastPointCopy.y - point.y;

                                RECT rect;
                                GetWindowRect(hWnd, out rect);

                                int x = rect.left;
                                int y = rect.top;
                                int width = rect.right - rect.left;
                                int height = rect.bottom - rect.top;
                                switch (resMoveType.ToInt32())
                                {
                                    case HTCAPTION:
                                        {
                                            x -= moveX;
                                            y -= moveY;
                                            break;
                                        }
                                    case HTTOP:
                                        {
                                            y -= moveY;
                                            height += moveY;
                                            break;
                                        }
                                    case HTBOTTOM:
                                        {
                                            height -= moveY;
                                            break;
                                        }
                                    case HTLEFT:
                                        {
                                            x -= moveX;
                                            width += moveX;
                                            break;
                                        }
                                    case HTRIGHT:
                                        {
                                            width -= moveX;
                                            break;
                                        }
                                    case HTTOPLEFT:
                                        {
                                            y -= moveY;
                                            height += moveY;
                                            x -= moveX;
                                            width += moveX;
                                            break;
                                        }
                                    case HTTOPRIGHT:
                                        {
                                            y -= moveY;
                                            height += moveY;
                                            width -= moveX;
                                            break;
                                        }
                                    case HTBOTTOMLEFT:
                                        {
                                            height -= moveY;
                                            x -= moveX;
                                            width += moveX;
                                            break;
                                        }
                                    case HTBOTTOMRIGHT:
                                        {
                                            height -= moveY;
                                            width -= moveX;
                                            break;
                                        }
                                    default:
                                        return;
                                }
                                MoveWindow(hWnd, x, y, width, height, false);
                                hResMoveWindow = hWnd;
                                return;
                            }
                            break;
                        }
                }

                for (IntPtr currHwnd = hWnd; ;)
                {
                    hWnd = currHwnd;
                    ScreenToClient(hWnd, ref point);
                    currHwnd = ChildWindowFromPoint(hWnd, point);
                    if (currHwnd == IntPtr.Zero || currHwnd == hWnd)
                        break;
                }

                if (mouseMsg)
                {
                    lParam = MAKELPARAM(point.x, point.y);
                }
                PostMessage(hWnd, msg, wParam, lParam);
            }
        }
    }
}
