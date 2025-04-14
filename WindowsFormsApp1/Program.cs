using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog;

namespace WindowsFormsApp1
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            var Logger = LogManager.GetCurrentClassLogger();
            try
            {
                Logger.Info("Application starting...");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
                Logger.Info("Application exited normally");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Application crashed");
                throw;
            }
        }
    }
}
