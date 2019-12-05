/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace QuuxPlayer
{
    internal static class Crawler
    {
        private static List<string> directories;

        private static bool running;
        private static bool cancel;
        private static List<Track> newTracks;
        private static ulong addTracksAlarm;
        private static ulong restartAlarm;

        private static object @lock = new object();

        static Crawler()
        {
            directories = new List<string>();
            newTracks = new List<Track>();
            addTracksAlarm = Clock.NULL_ALARM;
            restartAlarm = Clock.NULL_ALARM;
            running = false;
            cancel = false;
        }
        public static List<string> Directories
        {
            set
            {
                bool wasRunning = running;

                if (wasRunning)
                {
                    cancel = true;
                    while (running)
                    {
                        Thread.Sleep(250);
                    };
                }

                directories = value;
                
                if (wasRunning)
                    Start();
            }
        }
        public static void Start()
        {
            resetRestartAlarm();

            if (!running && directories.Count > 0)
            {
                newTracks.Clear();

                Thread t = new Thread(start);
                t.Name = "Crawler";
                t.Priority = ThreadPriority.Lowest;
                t.IsBackground = true;
                t.Start();
            }
        }
        public static void Stop()
        {
            cancel = true;
        }

        private static void start()
        {
            running = true;
            cancel = false;

            try
            {
                if (directories.Count > 0)
                {
                    Random r = new Random();

                    IOrderedEnumerable<string> dirs = directories.OrderBy(d => r.Next());

                    Stack<string> paths = new Stack<string>(dirs);

                    while (paths.Count > 0 && paths.Peek() != null && !cancel)
                    {
                        string s = paths.Pop();
                        if (Directory.Exists(s))
                        {
                            foreach (string ss in (Directory.GetDirectories(s)).OrderBy(x => r.Next()))
                                paths.Push(ss);
                            foreach (string ss in (Directory.GetFiles(s).Reverse()))
                                paths.Push(ss);
                        }
                        else if (File.Exists(s))
                        {
                            Track t = Track.Load(s);
                            if (t != null)
                            {
                                lock (@lock)
                                {
                                    newTracks.Add(t);
                                }

                                Thread.Sleep(100);
                                if (addIfNeeded(30))
                                    Thread.Sleep(200);
                            }
                            Thread.Sleep(50);
                        }
                    }
                    while (!cancel && !addIfNeeded(0))
                    {
                        Thread.Sleep(200);
                    };
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            running = false;

            if (cancel)
            {
                newTracks.Clear();
            }
            else
            {
                cancel = true;
                Clock.DoOnMainThread(sendTracks, 50);

                // get back to UI thread
                Clock.DoOnMainThread(Controller.GetInstance().RefreshAll, 500);
            }

            resetRestartAlarm();
        }
        private static void resetRestartAlarm()
        {
            if (directories.Count > 0)
                Clock.Update(ref restartAlarm, Start, 3600000, false);
            else
                Clock.RemoveAlarm(ref restartAlarm);
        }
        private static bool addIfNeeded(int Threshold)
        {
            lock (@lock)
            {
                if (newTracks.Count >= Threshold && addTracksAlarm == Clock.NULL_ALARM)
                {
                    addTracksAlarm = Clock.DoOnMainThread(sendTracks, 50);
                    return true;
                }
                else if (newTracks.Count == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        private static void sendTracks()
        {
            try
            {
                lock (@lock)
                {
                    int numTracks = newTracks.Count;

                    List<Track> tracks = newTracks.Take(numTracks).ToList();
                    newTracks.RemoveRange(0, numTracks);

                    Controller c = Controller.GetInstance();

                    c.AddToLibrarySilent(tracks, false);
                    c.RequestAction(QActionType.UpdateTrackCount);

                    addTracksAlarm = Clock.NULL_ALARM;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }
    }
}
