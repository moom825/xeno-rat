using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xeno_rat_server
{
    static class Program
    {

        /// <summary>
        /// Runs the application with the specified main form.
        /// </summary>
        /// <remarks>
        /// This method initializes the application, enables visual styles, sets text rendering compatibility, and runs the specified main form.
        /// </remarks>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
