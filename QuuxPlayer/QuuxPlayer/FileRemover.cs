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
    internal static class FileRemover
    {
        private static Callback callback = null;

        public static void RemoveGhosts(Callback Callback)
        {
            callback = Callback;
            start();
        }
        private static void start()
        {
            Thread t = new Thread(removeGhosts);
            t.Name = "Remove Ghosts";
            t.Priority = ThreadPriority.Normal;
            t.IsBackground = true;
            t.Start();
        }
        private static void removeGhosts()
        {
            try
            {
                List<Track> toRemove = Database.FindAllTracks(t => !t.ConfirmExists);
                Controller.ShowMessage("Removing " + toRemove.Count + " tracks from library...");
                Database.RemoveFromLibrary(toRemove);
                tryCallback();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
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
    }
}
