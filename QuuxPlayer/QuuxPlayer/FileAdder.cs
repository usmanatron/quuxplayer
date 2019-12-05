/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal static class FileAdder
    {
        private static int addCount = 0;
        private static object addItemLock = new object();
        private static List<ItemToAdd> itemsToAdd = new List<ItemToAdd>();
        private static bool cancel = false;
        private static bool running = false;
        private static Callback callback = null;

        private class ItemToAdd
        {
            public string FilePath { get; set; }
            public string PlaylistTarget { get; set; }
            public bool AllowDuplicates { get; set; }
            public ItemToAdd(string FilePath, string PlaylistTarget, bool AllowDuplicates)
            {
                this.FilePath = FilePath;
                this.PlaylistTarget = PlaylistTarget;
                this.AllowDuplicates = AllowDuplicates;
            }
        }

        public static void AddItemsToLibrary(IEnumerable<string> FilePaths, string PlaylistTarget, bool AllowDuplicates, Callback Callback)
        {
            callback = Callback;

            lock (addItemLock)
            {
                foreach (string s in FilePaths)
                    itemsToAdd.Add(new ItemToAdd(s, PlaylistTarget, AllowDuplicates));
            }
         
            if (!running)
            {
                Controller.ShowMessage(Localization.Get(UI_Key.Background_Cataloging_Tracks));
                addCount = 0;

                Thread t = new Thread(addItems);
                t.Name = "Add Items";
                t.Priority = ThreadPriority.Normal;
                t.IsBackground = true;
                t.Start();
            }
        }
        private static void addItems()
        {
            try
            {
                running = true;
                cancel = false;
                int itemsLeft;
                lock (addItemLock)
                {
                    itemsLeft = itemsToAdd.Count;
                }

                while (!cancel && itemsLeft > 0)
                {
                    ItemToAdd ita;
                    lock (addItemLock)
                    {
                        ita = itemsToAdd[0];
                        itemsToAdd.RemoveAt(0);
                    }
                    if (Directory.Exists(ita.FilePath))
                    {
                        List<ItemToAdd> newItems = new List<ItemToAdd>();
                        DirectoryInfo di = new DirectoryInfo(ita.FilePath);
                        foreach (FileInfo fi in di.GetFiles())
                            newItems.Add(new ItemToAdd(fi.FullName, ita.PlaylistTarget, ita.AllowDuplicates));
                        foreach (DirectoryInfo ddi in di.GetDirectories())
                            newItems.Add(new ItemToAdd(ddi.FullName, ita.PlaylistTarget, ita.AllowDuplicates));

                        lock (addItemLock)
                        {
                            itemsToAdd = itemsToAdd.Union(newItems).ToList();
                        }
                    }
                    else
                    {
                        if (Track.IsValidExtension(Path.GetExtension(ita.FilePath)))
                        {
                            Track tt = Track.Load(ita.FilePath);
                            if (tt != null)
                            {
                                Database.AddLibraryResult alr = Database.AddToLibrary(tt, ita.AllowDuplicates, true);
                                
                                TrackWriter.AddToUnsavedTracks(tt);
                                
                                if (ita.PlaylistTarget.Length > 0)
                                    Database.AddToPlaylist(ita.PlaylistTarget, tt);
                        
                                switch (alr)
                                {
                                    case Database.AddLibraryResult.OK:
                                        Controller.ShowMessage("Loading: " + (++addCount).ToString() + " - " + tt.ToString());
                                        break;
                                    case Database.AddLibraryResult.UpdateOnly:
                                        Controller.ShowMessage("Updating: " + (++addCount).ToString() + " - " + tt.ToString());
                                        break;
                                }
                                if (((addCount < 200) && ((addCount % 10) == 0)) || (addCount % 200 == 0))
                                {
                                    tryCallback();
                                }
                            }
                        }
                    }

                    lock (addItemLock)
                    {
                        itemsLeft = itemsToAdd.Count;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            running = false;
            lock (addItemLock)
            {
                if (cancel)
                    itemsToAdd.Clear();
            }
            tryCallback();
            TrackWriter.Start();
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

        public static void Cancel()
        {
            cancel = true;
        }
        
        public static void ExpandPaths(List<string> Paths)
        {
            for (int i = 0; i < Paths.Count; i++)
            {
                if (Directory.Exists(Paths[i]))
                {
                    string dir = Paths[i];
                    Paths.RemoveAt(i--);

                    try
                    {
                        foreach (string s in Directory.GetDirectories(dir))
                        {
                            Paths.Add(s);
                        }
                        foreach (string s in Directory.GetFiles(dir))
                        {
                            Paths.Add(s);
                        }
                    }
                    catch { }
                }
            }
        }
        // File Adding

        public static void AddFolder(Callback Callback)
        {
            string DirectoryPath = Lib.GetUserSelectedFolder(Application.ProductName + " will add all the music files at or below this folder:", String.Empty, false);

            if (String.IsNullOrEmpty(DirectoryPath) || !Directory.Exists(DirectoryPath))
                return;

            FileAdder.AddItemsToLibrary(new string[] { DirectoryPath }, String.Empty, true, Callback);
        }
    }
}
