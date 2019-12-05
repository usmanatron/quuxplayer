/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;

namespace QuuxPlayer
{
    internal static class FileSoftRefresher
    {
        private static int count = 0;
        public static void RefreshTracks()
        {
            count = 0;

            Thread t = new Thread(refreshFiles);
            t.Name = "Soft Refresh Files";
            t.Priority = ThreadPriority.Lowest;
            t.IsBackground = true;
            t.Start();
        }
        private static void refreshFiles()
        {
            try
            {
                IEnumerable<Track> all = Database.LibrarySnapshot;
                foreach (Track t in all)
                {
                    if (t.ConfirmExists && File.GetLastWriteTime(t.FilePath) > t.FileDate)
                    {
                        t.ForceLoad();
                    }
                    if (++count == 200)
                    {
                        Controller.GetInstance().Invalidate();
                    }
                }
                Controller.GetInstance().Invalidate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }
    }
}