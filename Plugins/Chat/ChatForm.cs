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
using xeno_rat_client;

namespace Chat
{
    public partial class ChatForm : Form
    {
        Node server;
        bool recived_heartbeat;
        public ChatForm(Node _server)
        {
            server = _server;
            InitializeComponent();
            textBox1.Enabled = false;
            InitializeAsync();
        }

        /// <summary>
        /// Initializes the asynchronous operations by calling the Heartbeat method and awaiting the RecvThread method.
        /// </summary>
        /// <remarks>
        /// This method first calls the Heartbeat method to perform a heartbeat operation.
        /// Then, it awaits the RecvThread method to handle receiving data asynchronously.
        /// </remarks>
        private async Task InitializeAsync() 
        {
            Heartbeat();
            await RecvThread();
        }

        /// <summary>
        /// Waits for the heartbeat from the server and disconnects if not received within a specified time.
        /// </summary>
        /// <remarks>
        /// This method continuously checks for the heartbeat from the server while it is connected. If the heartbeat is not received within 5 seconds, the server is disconnected.
        /// </remarks>
        public async Task Heartbeat() 
        {
            while (server.Connected()) 
            {
                await Task.Delay(5000);
                if (recived_heartbeat) 
                {
                    recived_heartbeat = false;
                    continue;
                }
                server.Disconnect();
                break;
            }
        }

        /// <summary>
        /// Asynchronously receives data from the server and updates the UI with the received message.
        /// </summary>
        /// <remarks>
        /// This method continuously receives data from the server while the server is connected.
        /// If the received data is a single byte with value 4, it sets the <see cref="recived_heartbeat"/> flag to true and continues to receive more data.
        /// If the received data is not a heartbeat, it decodes the data to a UTF-8 string and appends it to the UI element <see cref="textBox1"/> with "Admin: " prefix.
        /// After updating the UI, it scrolls to the end of the text and continues to receive more data.
        /// Once the server connection is closed, it prints "end!" to the console and closes the UI if it is not already disposed.
        /// </remarks>
        public async Task RecvThread()
        {
            while (server.Connected())
            {
                byte[] data = await server.ReceiveAsync();
                if (data == null)
                {
                    break;
                }
                if (data.Length == 1) 
                {
                    if (data[0] == 4) 
                    {
                        recived_heartbeat = true;
                        continue;
                    }
                }
                string message = Encoding.UTF8.GetString(data);
                textBox1.BeginInvoke((MethodInvoker)(() =>
                {
                    textBox1.Text += "Admin: " + message + Environment.NewLine;
                    textBox1.SelectionStart = textBox1.Text.Length;
                    textBox1.ScrollToCaret();
                }));
            }
            Console.WriteLine("end!");
            if (!this.IsDisposed)
            {
                this.BeginInvoke((MethodInvoker)(() =>
                {
                    this.Close();
                }));
            }
        }

        /// <summary>
        /// Sends the input message to the server and updates the UI with the sent message.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method sends the input message to the server using asynchronous communication and updates the UI with the sent message.
        /// It clears the input text box after sending the message and scrolls the chat box to the latest message.
        /// </remarks>
        private async void button1_Click(object sender, EventArgs e)
        {
            string message = textBox2.Text;
            textBox2.Text = "";
            await server.SendAsync(Encoding.UTF8.GetBytes(message));
            textBox1.Text += "You: " + message + Environment.NewLine;
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.ScrollToCaret();
        }

        /// <summary>
        /// Handles the KeyDown event for textBox2 and performs a click on button1 when the Enter key is pressed.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A KeyEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method checks if the Enter key is pressed and then simulates a click on button1. It also handles the event by setting e.Handled and e.SuppressKeyPress to true to prevent further processing of the key event.
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
        /// Event handler for the TextChanged event of textBox2.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        /// This method is called when the text in textBox2 is changed. It can be used to perform actions based on the changed text.
        /// </remarks>
        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Activates the chat form when it is loaded.
        /// </summary>
        /// <remarks>
        /// This method is triggered when the chat form is loaded. It ensures that the chat form is activated and brought to the front.
        /// </remarks>
        private void ChatForm_Load(object sender, EventArgs e)
        {
            this.Activate();
        }
    }
}
