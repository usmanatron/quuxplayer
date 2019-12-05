/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace QuuxPlayer
{
    internal static class FileRefresher
    {
        private static Callback callback = null;
        private static bool cancel = false;

        public static void RefreshTracks(Callback Callback)
        {
            callback = Callback;
            start();
        }
        public static void Cancel()
        {
            cancel = true;
        }
        private static void start()
        {
            cancel = false;

            Thread t = new Thread(refreshFiles);
            t.Name = "Refresh Files";
            t.Priority = ThreadPriority.BelowNormal;
            t.IsBackground = true;
            t.Start();
        }
        private static void tryCallback()
        {
            try
            {
                callback();
            }
            catch
            {
            }
        }
        private static void refreshFiles()
        {
            try
            {
                IEnumerable<Track> all = Database.LibrarySnapshot;
                foreach (Track t in all)
                {
                    if (t.ConfirmExists)
                    {
                        t.ForceLoad();
                        Controller.ShowMessage("Updating: " + t.ToString());
                    }
                    Database.IncrementDatabaseVersion(false);
                    if (cancel)
                        break;
                }
                tryCallback();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }
    }
}
