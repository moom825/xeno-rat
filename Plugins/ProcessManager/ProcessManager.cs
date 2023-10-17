using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xeno_rat_client;

namespace Plugin
{
    public class Main
    {

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        private Dictionary<string, string> windowsProcessPaths = new Dictionary<string, string>()
        {
            { "svchost.exe", @"C:\Windows\System32\svchost.exe" },
            { "explorer.exe", @"C:\Windows\explorer.exe" },
            { "taskhost.exe", @"C:\Windows\system32\taskhost.exe" },
            { "services.exe", @"C:\Windows\System32\services.exe" },
            { "lsass.exe", @"C:\Windows\System32\lsass.exe" },
            { "wininit.exe", @"C:\Windows\System32\wininit.exe" },
            { "csrss.exe", @"C:\Windows\System32\csrss.exe" },
            { "smss.exe", @"C:\Windows\System32\smss.exe" },
            { "spoolsv.exe", @"C:\Windows\System32\spoolsv.exe" },
            { "winlogon.exe", @"C:\Windows\System32\winlogon.exe" },
            { "dwm.exe", @"C:\Windows\System32\dwm.exe" },
            { "taskeng.exe", @"C:\Windows\System32\taskeng.exe" },
            { "logonui.exe", @"C:\Windows\System32\logonui.exe" },
            { "ctfmon.exe", @"C:\Windows\System32\ctfmon.exe" },
            { "cmd.exe", @"C:\Windows\System32\cmd.exe" },
            { "wmiprvse.exe", @"C:\Windows\System32\wbem\wmiprvse.exe" },
            { "iexplore.exe", @"C:\Program Files\Internet Explorer\iexplore.exe" },
            { "calc.exe", @"C:\Windows\System32\calc.exe" },
            { "notepad.exe", @"C:\Windows\System32\notepad.exe" },
            { "regedit.exe", @"C:\Windows\regedit.exe" },
            { "mspaint.exe", @"C:\Windows\System32\mspaint.exe" },
            { "mstsc.exe", @"C:\Windows\System32\mstsc.exe" }
        };

        private class ProcessNode
        {
            public Process Process { get; }
            public int PID { get; }
            public List<ProcessNode> Children { get; }
            public string FilePath { get; }
            public string FileDescription { get; }
            public string Name { get; }

            public ProcessNode(Process process, string filePath)
            {
                Process = process ?? throw new ArgumentNullException(nameof(process));
                PID = process.Id;
                Name = process.ProcessName;
                Children = new List<ProcessNode>();
                FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
                FileDescription = GetFileDescription(filePath);
            }

            private  string GetFileDescription(string filePath)
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return "Unknown";
                }

                try
                {
                    FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(filePath);
                    if (fileVersionInfo.FileDescription == null) 
                    {
                        return "Unknown";
                    }
                    return fileVersionInfo.FileDescription;
                }
                catch
                {
                    return "Unknown";
                }
            }
        }
        private async Task<Dictionary<int, string>> GetAllProcessFilePathsAsync()
        {
            return await Task.Run(() =>
            {
                var processFilePaths = new Dictionary<int, string>();
                ManagementObjectSearcher searcher = null;
                ManagementObjectCollection objects = null;
                try
                {
                    searcher = new ManagementObjectSearcher("SELECT Description, ProcessId, ExecutablePath, CommandLine FROM Win32_Process");
                    objects = searcher.Get();
                    foreach (ManagementObject obj in objects)
                    {
                        int processId = Convert.ToInt32(obj["ProcessId"]);
                        string filename = obj["Description"].ToString();
                        string filePath = obj["ExecutablePath"]?.ToString() ?? string.Empty;

                        if (string.IsNullOrEmpty(filePath))
                        {
                            // If ExecutablePath is null, try to retrieve the path from the CommandLine
                            string commandLine = obj["CommandLine"]?.ToString() ?? string.Empty;
                            filePath = ExtractFilePathFromCommandLine(commandLine);
                        }
                        if (string.IsNullOrEmpty(filePath))
                        {
                            if (windowsProcessPaths.TryGetValue(filename, out string path))
                            {
                                filePath = path;
                            }
                        }
                        processFilePaths[processId] = filePath;
                        obj.Dispose();
                    }

                }
                catch (ManagementException)
                {
                    // Handle exceptions if WMI query fails
                }
                finally
                {
                    searcher?.Dispose();
                    objects?.Dispose();
                }
                return processFilePaths;
            });
        }


        private Dictionary<int, string> GetAllProcessFilePaths()
        {
            var processFilePaths = new Dictionary<int, string>();
            ManagementObjectSearcher searcher=null;
            ManagementObjectCollection objects=null;
            try
            {
                searcher = new ManagementObjectSearcher("SELECT Description, ProcessId, ExecutablePath, CommandLine FROM Win32_Process");
                objects = searcher.Get();
                foreach (ManagementObject obj in objects)
                {
                    int processId = Convert.ToInt32(obj["ProcessId"]);
                    string filename = obj["Description"].ToString();
                    string filePath = obj["ExecutablePath"]?.ToString() ?? string.Empty;
                
                    if (string.IsNullOrEmpty(filePath))
                    {
                        // If ExecutablePath is null, try to retrieve the path from the CommandLine
                        string commandLine = obj["CommandLine"]?.ToString() ?? string.Empty;
                        filePath = ExtractFilePathFromCommandLine(commandLine);
                    }
                    if (string.IsNullOrEmpty(filePath))
                    {
                        if (windowsProcessPaths.TryGetValue(filename, out string path))
                        {
                            filePath = path;
                        }
                    }
                    processFilePaths[processId] = filePath;
                    obj.Dispose();
                }
               
            }
            catch (ManagementException)
            {
                // Handle exceptions if WMI query fails
            }
            if (searcher != null) 
            {
                searcher.Dispose();  
            }
            if (objects != null)
            {
                objects.Dispose();
            }
            return processFilePaths;
        }

        private  string ExtractFilePathFromCommandLine(string commandLine)
        {
            // Extract the file path from the command line using custom logic
            // Modify this logic based on your requirements and assumptions about the command line format

            string filePath = string.Empty;

            if (!string.IsNullOrEmpty(commandLine))
            {
                // Split the command line by whitespace
                string[] tokens = commandLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                // Look for the first token that ends with .        // Look for the first token that ends with .exe or .dll to extract the file path
                foreach (string token in tokens)
                {
                    if (token.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || token.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        filePath = token;
                        break;
                    }
                }
            }

            return filePath;
        }

        private async Task<Dictionary<int, ProcessNode>> BuildProcessTree(Process[] processes, Dictionary<int, string> processFilePaths)
        {
            var processMap = new Dictionary<int, ProcessNode>();

            foreach (var process in processes)
            {
                string filePath = processFilePaths.ContainsKey(process.Id) ? processFilePaths[process.Id] : string.Empty;
                ProcessNode node = new ProcessNode(process, filePath);
                processMap[process.Id] = node;
            }

            foreach (var process in processes)
            {
                if (processMap.TryGetValue(process.Id, out var childNode))
                {
                    int parentId = await GetParentProcessIdAsync(process);
                    if (processMap.TryGetValue(parentId, out var parentNode))
                    {
                        parentNode.Children.Add(childNode);
                    }
                }
            }

            return processMap;
        }
        private async Task<int> GetParentProcessIdAsync(Process process)
        {
            return await Task.Run(() =>
            {
                IntPtr snapshotHandle = CreateToolhelp32Snapshot(2 /* TH32CS_SNAPPROCESS */, 0);

                if (snapshotHandle.ToInt64() == -1)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                PROCESSENTRY32 processEntry = new PROCESSENTRY32();
                processEntry.dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32));

                if (!Process32First(snapshotHandle, ref processEntry))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                do
                {
                    if (processEntry.th32ProcessID == (uint)process.Id)
                    {
                        return (int)processEntry.th32ParentProcessID;
                    }
                }
                while (Process32Next(snapshotHandle, ref processEntry));

                return -1; // Default value if parent process ID is not found
            });
        }

        private int GetParentProcessId(Process process)
        {
            IntPtr snapshotHandle = CreateToolhelp32Snapshot(2 /* TH32CS_SNAPPROCESS */, 0);

            if (snapshotHandle.ToInt64() == -1)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            PROCESSENTRY32 processEntry = new PROCESSENTRY32();
            processEntry.dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32));

            if (!Process32First(snapshotHandle, ref processEntry))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            do
            {
                if (processEntry.th32ProcessID == (uint)process.Id)
                {
                    return (int)processEntry.th32ParentProcessID;
                }
            }
            while (Process32Next(snapshotHandle, ref processEntry));

            return -1; // Default value if parent process ID is not found
        }

        private  List<ProcessNode> GetRootProcesses(Dictionary<int, ProcessNode> processMap)
        {
            var rootProcesses = new List<ProcessNode>();

            foreach (var processNode in processMap.Values)
            {
                if (!processMap.ContainsKey(GetParentProcessId(processNode.Process)))
                {
                    rootProcesses.Add(processNode);
                }
            }

            return rootProcesses;
        }

        private  byte[] SerializeProcessList(List<ProcessNode> processList)
        {
            byte[] done;
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(memoryStream))
            {
                // Serialize the number of processes
                writer.Write(processList.Count);

                // Serialize each process node
                foreach (var processNode in processList)
                {
                    SerializeProcessNode(processNode, writer);
                }

                done=memoryStream.ToArray();
            }
            return done;
        }

        private void SerializeProcessNode(ProcessNode node, BinaryWriter writer)
        {
            writer.Write(node.PID);
            writer.Write(node.Children.Count);
            writer.Write(node.FilePath);
            writer.Write(node.FileDescription);
            writer.Write(node.Name);

            foreach (var child in node.Children)
            {
                SerializeProcessNode(child, writer);
            }
        }
        private void disposeAllProcess(Process[] processes) 
        {
            foreach (Process i in processes) 
            {
                i.Dispose();
            }
        }

        private bool paused = false;

        public async Task RecvThread(Node node) 
        {
            while (node.Connected())
            {
                byte[] data = await node.ReceiveAsync();
                if (data == null) 
                {
                    return;
                }
                if (data.Length == 1)
                {
                    if (data[0] == (byte)1)
                    {
                        paused = true;
                    }
                    else
                    {
                        paused = false;
                    }
                }
                else if (data.Length == 4) 
                {
                    Process process=null;
                    try
                    {
                        int pid = node.sock.BytesToInt(data);
                        process = Process.GetProcessById(pid);
                        process.Kill();
                    }
                    catch 
                    { 
                    
                    }
                    if (process != null) 
                    {
                        process.Dispose();
                    }
                    
                }
            }
        }
        public async Task Run(Node node)
        {
            await node.SendAsync(new byte[] { 3 });//indicate that it has connected
            RecvThread(node);
            while (node.Connected())
            {
                if (!paused)
                {
                    Process[] processes = Process.GetProcesses();
                    Dictionary<int, string> processFilePaths = await GetAllProcessFilePathsAsync();
                    Dictionary<int, ProcessNode> processMap = await BuildProcessTree(processes, processFilePaths);
                    List<ProcessNode> rootProcesses = GetRootProcesses(processMap);
                    disposeAllProcess(processes);
                    byte[] searlized = SerializeProcessList(rootProcesses);
                    await node.SendAsync(searlized);
                }
                else 
                {
                    await Task.Delay(500);
                }
            }
        }
    }
}
