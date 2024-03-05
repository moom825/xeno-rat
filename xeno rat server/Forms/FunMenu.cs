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
    public partial class FunMenu : Form
    {
        Node client;
        public FunMenu(Node _client)
        {
            client = _client;
            InitializeComponent();
            _=InitializeAsync();
        }

        /// <summary>
        /// Initializes the asynchronous task by sending a byte array to the client.
        /// </summary>
        /// <remarks>
        /// This method initializes an asynchronous task by sending the specified byte array to the client using the SendAsync method of the client object.
        /// </remarks>
        private async Task InitializeAsync() 
        {
            await client.SendAsync(new byte[] { 2 });
        }

        /// <summary>
        /// Loads the FunMenu form.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method is called when the FunMenu form is loaded. It can be used to initialize the form and its components.
        /// </remarks>
        private void FunMenu_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Sends a byte array asynchronously using the client and does not return anything.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method sends the specified byte array <paramref name="new byte[] { 1 }"/> asynchronously using the client.
        /// </remarks>
        private async void button1_Click(object sender, EventArgs e)
        {
            await client.SendAsync(new byte[] { 1 });
        }

        /// <summary>
        /// Sends a byte array to the client asynchronously.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method sends the specified byte array to the client asynchronously using the SendAsync method of the client object.
        /// </remarks>
        private async void button2_Click(object sender, EventArgs e)
        {
            await client.SendAsync(new byte[] { 2 });
        }

        /// <summary>
        /// Sends a byte array with a value of 3 using the client asynchronously.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method sends the byte array with a value of 3 using the client asynchronously.
        /// </remarks>
        private async void button3_Click(object sender, EventArgs e)
        {
            await client.SendAsync(new byte[] { 3 });
        }

        /// <summary>
        /// Sends a byte array with a value of 4 using the client asynchronously.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method sends the byte array with a value of 4 using the client asynchronously.
        /// </remarks>
        private async void button4_Click(object sender, EventArgs e)
        {
            await client.SendAsync(new byte[] { 4 });
        }

        /// <summary>
        /// Handles the scroll event of the trackBar1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        /// <remarks>
        /// This method sends the current value of the trackBar to the client asynchronously using the SendAsync method of the client object.
        /// </remarks>
        private async void trackBar1_Scroll(object sender, EventArgs e)
        {
            await client.SendAsync(new byte[] { 5 });
            await client.SendAsync(new byte[] { (byte)((TrackBar)sender).Value });
        }
    }
}
