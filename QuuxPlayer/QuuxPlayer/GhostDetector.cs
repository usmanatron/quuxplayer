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
    internal static class GhostDetector
    {
        private static Callback callback = null;
        private static bool running = false;
        private static bool cancel = false;
        private static DateTime lastCallback = DateTime.MinValue;

        public static void DetectGhosts(Callback Callback)
        {
            callback = Callback;
            start();
        }
        private static void start()
        {
            Thread t = new Thread(detectGhosts);
            t.Name = "Detect Ghosts";
            t.Priority = ThreadPriority.BelowNormal;
            t.IsBackground = true;
            t.Start();
        }
        private static void detectGhosts()
        {
            try
            {
                if (running)
                    return;

                running = true;
                cancel = false;

                List<Track> tracks = Database.LibrarySnapshot;

                for (int i = 0; i < tracks.Count && !cancel; i++)
                {
                    tracks[i].SetConfirmExists();
                    if ((i % 50) == 0)
                        if ((DateTime.Now - lastCallback) > TimeSpan.FromSeconds(2.0))
                            tryCallback();
                }
                tryCallback();

                running = false;
                cancel = false;
                Database.IncrementDatabaseVersion(false);
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
                lastCallback = DateTime.Now;
            }
            catch
            {
            }
        }
    }
}
