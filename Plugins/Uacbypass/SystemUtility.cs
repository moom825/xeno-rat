using System;
using System.Runtime.InteropServices;

namespace Plugin
{
    class SystemUtility//thanks for https://github.com/vrnobody/V2RayGCon/blob/8169e9a4622dcc5a5640bf65a186aab1a2f3b7a8/3rd/ZipExtractor/SystemUtility.cs, saved me a lot of time
    {

        /// <summary>
        /// Executes a process without elevation.
        /// </summary>
        /// <param name="process">The path to the process to be executed.</param>
        /// <param name="args">The arguments to be passed to the process.</param>
        /// <param name="currentDirectory">The current directory for the process.</param>
        /// <remarks>
        /// This method executes the specified <paramref name="process"/> with the given <paramref name="args"/> and <paramref name="currentDirectory"/> without requiring elevation.
        /// It uses the ShellExecute function to start the process in a non-elevated context.
        /// </remarks>
        public static void ExecuteProcessUnElevated(
            string process,
            string args,
            string currentDirectory
        )
        {
            var shellWindows = (IShellWindows)new CShellWindows();

            object loc = CSIDL_Desktop;
            object unused = new object();
            int hwnd;
            var serviceProvider = (IServiceProvider)
                shellWindows.FindWindowSW(
                    ref loc,
                    ref unused,
                    SWC_DESKTOP,
                    out hwnd,
                    SWFO_NEEDDISPATCH
                );

            var serviceGuid = SID_STopLevelBrowser;
            var interfaceGuid = typeof(IShellBrowser).GUID;
            var shellBrowser = (IShellBrowser)
                serviceProvider.QueryService(ref serviceGuid, ref interfaceGuid);

            var dispatch = typeof(IDispatch).GUID;
            var folderView = (IShellFolderViewDual)
                shellBrowser.QueryActiveShellView().GetItemObject(SVGIO_BACKGROUND, ref dispatch);
            var shellDispatch = (IShellDispatch2)folderView.Application;

            shellDispatch.ShellExecute(
                process,
                args,
                currentDirectory,
                string.Empty,
                SW_SHOWNORMAL
            );
        }

        private const int CSIDL_Desktop = 0;
        private const int SWC_DESKTOP = 8;
        private const int SWFO_NEEDDISPATCH = 1;
        private const int SW_SHOWNORMAL = 1;
        private const int SVGIO_BACKGROUND = 0;
        private static readonly Guid SID_STopLevelBrowser = new Guid(
            "4C96BE40-915C-11CF-99D3-00AA004AE837"
        );

        [ComImport]
        [Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39")]
        [ClassInterface(ClassInterfaceType.None)]
        private class CShellWindows { }

        [ComImport]
        [Guid("85CB6900-4D95-11CF-960C-0080C7F4EE85")]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        private interface IShellWindows
        {
            [return: MarshalAs(UnmanagedType.IDispatch)]
            object FindWindowSW(
                [MarshalAs(UnmanagedType.Struct)] ref object pvarloc,
                [MarshalAs(UnmanagedType.Struct)] ref object pvarlocRoot,
                int swClass,
                out int pHWND,
                int swfwOptions
            );
        }

        [ComImport]
        [Guid("6d5140c1-7436-11ce-8034-00aa006009fa")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IServiceProvider
        {
            [return: MarshalAs(UnmanagedType.Interface)]
            object QueryService(ref Guid guidService, ref Guid riid);
        }

        [ComImport]
        [Guid("000214E2-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellBrowser
        {
            void VTableGap01(); // GetWindow
            void VTableGap02(); // ContextSensitiveHelp
            void VTableGap03(); // InsertMenusSB
            void VTableGap04(); // SetMenuSB
            void VTableGap05(); // RemoveMenusSB
            void VTableGap06(); // SetStatusTextSB
            void VTableGap07(); // EnableModelessSB
            void VTableGap08(); // TranslateAcceleratorSB
            void VTableGap09(); // BrowseObject
            void VTableGap10(); // GetViewStateStream
            void VTableGap11(); // GetControlWindow
            void VTableGap12(); // SendControlMsg
            IShellView QueryActiveShellView();
        }

        [ComImport]
        [Guid("000214E3-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellView
        {
            void VTableGap01(); // GetWindow
            void VTableGap02(); // ContextSensitiveHelp
            void VTableGap03(); // TranslateAcceleratorA
            void VTableGap04(); // EnableModeless
            void VTableGap05(); // UIActivate
            void VTableGap06(); // Refresh
            void VTableGap07(); // CreateViewWindow
            void VTableGap08(); // DestroyViewWindow
            void VTableGap09(); // GetCurrentInfo
            void VTableGap10(); // AddPropertySheetPages
            void VTableGap11(); // SaveViewState
            void VTableGap12(); // SelectItem

            [return: MarshalAs(UnmanagedType.Interface)]
            object GetItemObject(UInt32 aspectOfView, ref Guid riid);
        }

        [ComImport]
        [Guid("00020400-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        private interface IDispatch { }

        [ComImport]
        [Guid("E7A1AF80-4D96-11CF-960C-0080C7F4EE85")]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        private interface IShellFolderViewDual
        {
            object Application
            {
                [return: MarshalAs(UnmanagedType.IDispatch)]
                get;
            }
        }

        [ComImport]
        [Guid("A4C6892C-3BA9-11D2-9DEA-00C04FB16162")]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        public interface IShellDispatch2
        {
            void ShellExecute(
                [MarshalAs(UnmanagedType.BStr)] string File,
                [MarshalAs(UnmanagedType.Struct)] object vArgs,
                [MarshalAs(UnmanagedType.Struct)] object vDir,
                [MarshalAs(UnmanagedType.Struct)] object vOperation,
                [MarshalAs(UnmanagedType.Struct)] object vShow
            );
        }
    }
}
