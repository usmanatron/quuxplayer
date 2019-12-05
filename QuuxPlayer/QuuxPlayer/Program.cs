/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Windows.Forms;

namespace QuuxPlayer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.AboveNormal;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new frmMain());
            }
            catch (Exception ex)
            {
                Lib.ExceptionToClipboard(ex);
            }
        }
#if DEBUG
        public static void Run()
        {
            System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(Main));
            t.Name = "Test Start Thread";
            t.Priority = System.Threading.ThreadPriority.Normal;
            t.IsBackground = false;
            t.Start();

            while (!frmMain.Started)
                Application.DoEvents();
        }
        public static void Stop()
        {
            Controller.GetInstance().RequestAction(QActionType.Exit);
        }
#endif

    }
}