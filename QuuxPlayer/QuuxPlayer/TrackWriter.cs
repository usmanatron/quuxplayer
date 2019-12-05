/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using Un4seen.Bass;
using Un4seen.Bass.AddOn;
using Un4seen.Bass.AddOn.Tags;

namespace QuuxPlayer
{
    [Flags]
    internal enum ChangeType : int { None = 0x00, WriteTags = 0x01, EmbedImage = 0x02, Rename = 0x04, Move = 0x08, IgnoreContainment = 0x10 }

    internal static class TrackWriter
    {
        public enum RenameFormat : int { None, TK_AR_TI, TK_TI, TK_AR_AL_TI, TK_AL_AR_TI, AR_AL_TK_TI, AL_TK_TI, TI, Last }
        public enum DirectoryFormat : int { AR, AL, AR_AL, GE_AR_AL, GR_GE_AR_AL, GE_GR_AR_AL, GE_GR_AR, GR_AR, GR, GE, GR_AR_AL, GR_GE_AR, GE_AR, Last, None };

        private static readonly char[] trimChars = new char[] { ' ', '\0', '\t', '\r', '\n' };

        private static List<Track> unsavedTracks = new List<Track>();
        private static IEnumerable<Track> newDirtyTracks = null;
        private static List<string> itemsToDelete = null;

        private static bool cancel = false;
        private static bool lastChance = false;
        private static bool running = false;
        private static bool suspendWriting = false;

        private static object @lock = new object();
        private static object deleteLock = new object();

        private static ulong saveAlarm = Clock.NULL_ALARM;

        private static Dictionary<RenameFormat, string> renames;
        private static List<string> renameStrings;

        private static Dictionary<DirectoryFormat, string> dirFormats;
        private static List<string> dirFormatStrings;

        static TrackWriter()
        {
            TagLib.Id3v2.Tag.UseNumericGenres = false;

            renames = new Dictionary<RenameFormat, string>();
            renameStrings = new List<string>();

            renames.Add(RenameFormat.TK_AR_TI, Localization.Get(UI_Key.Edit_File_TK_AR_TI));
            renames.Add(RenameFormat.TK_TI, Localization.Get(UI_Key.Edit_File_TK_TI));
            renames.Add(RenameFormat.TK_AR_AL_TI, Localization.Get(UI_Key.Edit_File_TK_AR_AL_TI));
            renames.Add(RenameFormat.TK_AL_AR_TI, Localization.Get(UI_Key.Edit_File_TK_AL_AR_TI));
            renames.Add(RenameFormat.AR_AL_TK_TI, Localization.Get(UI_Key.Edit_File_AR_AL_TK_TI));
            renames.Add(RenameFormat.AL_TK_TI, Localization.Get(UI_Key.Edit_File_AL_TK_TI));
            renames.Add(RenameFormat.TI, Localization.Get(UI_Key.Edit_File_TI));

            renameStrings = renames.Values.ToList();
            renameStrings.Sort();

            renames.Add(RenameFormat.None, Localization.Get(UI_Key.Edit_Tags_Multiple_Values));

            renameStrings.Insert(0, renames[RenameFormat.None]);

            System.Diagnostics.Debug.Assert(renames.Count == (int)RenameFormat.Last);

            dirFormats = new Dictionary<DirectoryFormat, string>();
            dirFormatStrings = new List<string>();

            dirFormats.Add(DirectoryFormat.AL, Localization.Get(UI_Key.Organize_File_AL));
            dirFormats.Add(DirectoryFormat.AR, Localization.Get(UI_Key.Organize_File_AR));
            dirFormats.Add(DirectoryFormat.AR_AL, Localization.Get(UI_Key.Organize_File_AR_AL));
            dirFormats.Add(DirectoryFormat.GE_AR_AL, Localization.Get(UI_Key.Organize_File_GE_AR_AL));
            dirFormats.Add(DirectoryFormat.GE_GR_AR, Localization.Get(UI_Key.Organize_File_GE_GR_AR));
            dirFormats.Add(DirectoryFormat.GE_GR_AR_AL, Localization.Get(UI_Key.Organize_File_GE_GR_AR_AL));
            dirFormats.Add(DirectoryFormat.GR_GE_AR_AL, Localization.Get(UI_Key.Organize_File_GR_GE_AR_AL));
            dirFormats.Add(DirectoryFormat.GR_AR, Localization.Get(UI_Key.Organize_File_GR_AR));
            dirFormats.Add(DirectoryFormat.GE, Localization.Get(UI_Key.Organize_File_GE));
            dirFormats.Add(DirectoryFormat.GR, Localization.Get(UI_Key.Organize_File_GR));
            dirFormats.Add(DirectoryFormat.GR_AR_AL, Localization.Get(UI_Key.Organize_File_GR_AR_AL));
            dirFormats.Add(DirectoryFormat.GR_GE_AR, Localization.Get(UI_Key.Organize_File_GR_GE_AR));
            dirFormats.Add(DirectoryFormat.GE_AR, Localization.Get(UI_Key.Organize_File_GE_AR));

            dirFormatStrings = dirFormats.Values.ToList();
            dirFormatStrings.Sort();

            System.Diagnostics.Debug.Assert(dirFormats.Count == (int)DirectoryFormat.Last);

            AudioStreamFile.RegisterBASS();
        }

        private const int MAX_STRING_LEN = 60;
        private const int MAX_FILENAME_LEN = 120;

        private static string concatFile(params string[] Input)
        {
            List<string> input = new List<string>(Input);

            input.RemoveAll(s => s.Length == 0);

            if (input.Count == 0)
                return String.Empty;

            for (int i = 0; i < input.Count; i++)
                if (input[i].Length > MAX_STRING_LEN)
                    input[i] = input[i].Substring(0, MAX_STRING_LEN);

            StringBuilder sb = new StringBuilder(input[0]);
            for (int i = 1; i < input.Count; i++)
            {
                sb.Append(" - ");
                sb.Append(input[i]);
            }
            return validateFileName(sb.ToString(), false);
        }

        private static string concatDir(params string[] Input)
        {
            List<string> input = new List<string>(Input);

            input.RemoveAll(ss => ss.Length == 0);

            for (int i = 0; i < input.Count; i++)
            {
                if (input[i].Contains(Path.DirectorySeparatorChar))
                    input[i] = input[i].Replace(Path.DirectorySeparatorChar, '_');
                if (input[i].Contains(Path.AltDirectorySeparatorChar))
                    input[i] = input[i].Replace(Path.AltDirectorySeparatorChar, '_');
            }

            if (input.Count == 0)
                return String.Empty;

            for (int i = 0; i < input.Count; i++)
                if (input[i].Length > MAX_STRING_LEN)
                    input[i] = input[i].Substring(0, MAX_STRING_LEN);

            StringBuilder sb = new StringBuilder(stripBadEndingChars(input[0]));
            for (int i = 1; i < input.Count; i++)
            {
                sb.Append(Path.DirectorySeparatorChar);
                sb.Append(stripBadEndingChars(input[i]));
            }
            
            string s = sb.ToString();
            if (s.Length > MAX_FILENAME_LEN)
                s = s.Substring(0, MAX_FILENAME_LEN);

            return validateFileName(s, true);
        }
        public static IEnumerable<string> GetRenames(Track Track)
        {
            List<string> ss = new List<string>();

            for (RenameFormat rf = RenameFormat.None; rf < RenameFormat.Last; rf++)
            {
                string s = GetRename(Track, rf);
                if (s.Length > 0)
                    ss.Add(s);
            }

            return ss.Distinct();
        }
        public static IEnumerable<string> GetRenames()
        {
            return renameStrings;
        }
        public static IEnumerable<string> GetDirFormats()
        {
            return dirFormatStrings;
        }
        public static string GetDirFormat(DirectoryFormat Format)
        {
            return dirFormats[Format];
        }
        public static DirectoryFormat GetDirFormat(string Sample)
        {
            foreach (KeyValuePair<DirectoryFormat, string> kvp in dirFormats)
            {
                if (kvp.Value == Sample)
                    return kvp.Key;
            }
            return DirectoryFormat.None;
        }
        public static string GetDirFormat(Track Track, DirectoryFormat Format)
        {
            switch (Format)
            {
                case DirectoryFormat.AL:
                    return concatDir(Track.Album);
                case DirectoryFormat.AR:
                    return concatDir(Track.MainGroup);
                case DirectoryFormat.AR_AL:
                    return concatDir(Track.MainGroup, Track.Album);
                case DirectoryFormat.GE_AR_AL:
                    return concatDir(Track.Genre, Track.MainGroup, Track.Album);
                case DirectoryFormat.GE_GR_AR:
                    return concatDir(Track.Genre, Track.Grouping, Track.MainGroup);
                case DirectoryFormat.GR_GE_AR:
                    return concatDir(Track.Grouping, Track.Genre, Track.MainGroup);
                case DirectoryFormat.GR_AR:
                    return concatDir(Track.Grouping, Track.MainGroup);
                case DirectoryFormat.GE_AR:
                    return concatDir(Track.Genre, Track.MainGroup);
                case DirectoryFormat.GE_GR_AR_AL:
                    return concatDir(Track.Genre, Track.Grouping, Track.MainGroup, Track.Album);
                case DirectoryFormat.GR:
                    return concatDir(Track.Grouping);
                case DirectoryFormat.GE:
                    return concatDir(Track.Genre);
                case DirectoryFormat.GR_GE_AR_AL:
                    return concatDir(Track.Grouping, Track.Genre, Track.MainGroup, Track.Album);
                case DirectoryFormat.GR_AR_AL:
                    return concatDir(Track.Grouping, Track.MainGroup, Track.Album);
                default:
                    return String.Empty;
            }
        }
        public static string GetRename(Track Track, RenameFormat Format)
        {
            if (Format == RenameFormat.None)
                return String.Empty;

            string s = getRenameWithoutExtension(Track, Format).Trim();

            if (s.Length == 0)
                return String.Empty;
            else if (s.Length > 90)
                return s.Substring(0, 90).Trim() + Path.GetExtension(Track.FilePath);
            else
                return s + Path.GetExtension(Track.FilePath);
        }
        private static string getRenameWithoutExtension(Track Track, RenameFormat Format)
        {
            switch (Format)
            {
                case RenameFormat.AL_TK_TI:
                    return concatFile(Track.Album, Track.TrackNumStringLeadingZero, Track.Title);

                case RenameFormat.AR_AL_TK_TI:
                    return concatFile(Track.MainGroup, Track.Album, Track.TrackNumStringLeadingZero, Track.Title);

                case RenameFormat.TK_AR_AL_TI:
                    return concatFile(Track.TrackNumStringLeadingZero, Track.MainGroup, Track.Album, Track.Title);

                case RenameFormat.TK_AL_AR_TI:
                    return concatFile(Track.TrackNumStringLeadingZero, Track.Album, Track.MainGroup, Track.Title);

                case RenameFormat.TK_AR_TI:
                    return concatFile(Track.TrackNumStringLeadingZero, Track.MainGroup, Track.Title);

                case RenameFormat.TK_TI:
                    return concatFile(Track.TrackNumStringLeadingZero, Track.Title);

                case RenameFormat.TI:
                    return concatFile(Track.Title);

                default:
                    return Path.GetFileNameWithoutExtension(Track.FilePath);
            }
        }

        private static string stripBadEndingChars(string Input)
        {
            while (Input.Length > 0 && Input.LastIndexOfAny(badEndingChars) == Input.Length - 1)
                Input = Input.Substring(0, Input.Length - 1);

            return Input;
        }

        private static char[] badEndingChars = { ' ', '.' };

        private static string validateFileName(string Input, bool IsDir)
        {
            int i = 0;

            do
            {
                i = Input.IndexOfAny(Path.GetInvalidFileNameChars(), i);
                if (i >= 0)
                {
                    if (!IsDir || (Input[i] != Path.DirectorySeparatorChar && Input[i] != Path.AltDirectorySeparatorChar))
                        Input = Input.Substring(0, i) + '_' + Input.Substring(i + 1);
                    i++;
                }
            } while (i >= 0);

            return stripBadEndingChars(Input);
        }
        public static RenameFormat GetRenameFormat(string Sample)
        {
            foreach (KeyValuePair<RenameFormat, string> kvp in renames)
            {
                if (kvp.Value == Sample)
                    return kvp.Key;
            }
            return RenameFormat.None;
        }
        public static RenameFormat GetRenameFormat(Track Track, string Sample)
        {
            for (RenameFormat rf = RenameFormat.None; rf < RenameFormat.Last; rf++)
                if (GetRename(Track, rf) == Sample)
                    return rf;

            return RenameFormat.None;
        }

        public static string GetRenameFormat(RenameFormat Format)
        {
            return renames[Format];
        }

        public static void AddToUnsavedTracks(Track Track)
        {
            lock (@lock)
            {
                unsavedTracks.Add(Track);
            }
        }
        public static void AddToUnsavedTracks(IEnumerable<Track> UnsavedTracks)
        {
            if (UnsavedTracks.Any())
            {
                cancel = true;

                newDirtyTracks = UnsavedTracks;

                if (!SuspendWriting)
                    Start();
            }
        }
        public static void Start()
        {
            Thread t = new Thread(doFileChanges);
            t.Name = "WriteTags";
            t.Priority = ThreadPriority.Lowest;
            t.IsBackground = true;
            t.Start();
        }
        public static void Stop()
        {
            cancel = true;
        }
        public static void Clear()
        {
            Stop();
            lock (deleteLock)
            {
                if (itemsToDelete != null)
                    itemsToDelete.Clear();
            }
            lock (@lock)
            {
                if (unsavedTracks != null)
                    unsavedTracks.Clear();
            }
        }
        public static bool HasUnsavedTracks
        {
            get { return unsavedTracks.Count > 0; }
        }
        public static void AddToDeleteList(string ItemToDelete)
        {
            if (itemsToDelete == null)
                itemsToDelete = new List<string>();

            lock (deleteLock)
            {
                if (!itemsToDelete.Contains(ItemToDelete, StringComparer.OrdinalIgnoreCase))
                    itemsToDelete.Add(ItemToDelete);
            }
        }
        private static void delay()
        {
            saveAlarm = Clock.NULL_ALARM;
            if (!running)
                doFileChanges();
        }
        private static void doFileChanges()
        {
            try
            {
                cancel = true;

                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();

                while (running)
                    System.Threading.Thread.Sleep(50);

                lock (@lock)
                {
                    if (newDirtyTracks != null)
                    {
                        unsavedTracks = unsavedTracks.Union(newDirtyTracks).ToList();
                        newDirtyTracks = null;
                    }
                }

                running = true;
                cancel = false;

                List<Track> failedTracks = new List<Track>();

                while (!cancel && unsavedTracks.Count > 0)
                {
                    Track t = null;

                    lock (@lock)
                    {
                        if (unsavedTracks.Count > 0)
                            t = unsavedTracks[0];
                    }

                    if (t != null)
                    {
                        if (t.Deleted)
                        {
                            t.ChangeType = ChangeType.None;
                        }
                        else if (SuspendWriting)
                        {
                            lock (@lock)
                            {
                                if (!failedTracks.Contains(t))
                                    failedTracks.Add(t);
                            }
                        }
                        else
                        {
                            if (t.ConfirmExists)
                            {
                                TrackWriter.write(t);
                                if (readOnlyDisposition == ReadOnlyDisposition.Cancel)
                                {
                                    List<Track> tt = Database.LibrarySnapshot.FindAll(ttt => ttt.ChangeType != ChangeType.None);
                                    foreach (Track ttt in tt)
                                    {
                                        ttt.ChangeType = ChangeType.None;
                                    }
                                    lock (@lock)
                                    {
                                        unsavedTracks.Clear();
                                        Controller.ShowMessage("File updates canceled.");
                                        cancel = true;
                                        readOnlyDisposition = ReadOnlyDisposition.Null;
                                        return;
                                    }
                                }
                            }
                            lock (@lock)
                            {
                                unsavedTracks.Remove(t);
                                if (t.ChangeType != ChangeType.None && t.ConfirmExists)
                                {
                                    failedTracks.Add(t);
                                }
                            }
                        }
                    }
                    System.Threading.Thread.Sleep(10);
                }
                running = false;

                System.Threading.Thread.Sleep(50); // give any blocked thread a chance to run

                lock (@lock)
                {
                    if (!running && !cancel && (unsavedTracks.Count > 0 || failedTracks.Count > 0)) // likely couldn't write because the track was being played
                    {
                        unsavedTracks = unsavedTracks.Union(failedTracks).ToList();
                        if (saveAlarm == Clock.NULL_ALARM)
                            saveAlarm = Clock.DoOnNewThread(delay, 30000);
                    }
                }

                DeleteItems();

                if (!running && !cancel)
                {
                    if (failedTracks.Count > 0)
                    {
                        //Controller.ShowMessage("Pausing file updates; " + failedTracks.Count.ToString() + " files left to update.");
                    }
                    else
                    {
                        readOnlyDisposition = ReadOnlyDisposition.Null;
                        Controller.ShowMessage("File updates complete.");
                    }
                }
                else
                {
                    readOnlyDisposition = ReadOnlyDisposition.Null;
                }
            }
            catch (Exception ex)
            {
                Lib.ExceptionToClipboard(ex);
            }
        }
        public static string GetPath(Track Track, DirectoryFormat DF, RenameFormat RF)
        {
            if (RF == RenameFormat.None)
                return Path.Combine(GetDirFormat(Track, DF), Path.GetFileName(Track.FileName));
            else
                return Path.Combine(GetDirFormat(Track, DF), GetRename(Track, RF));
        }
        public static void DeleteItemsLastChance()
        {
            lock (deleteLock)
            {
                if (itemsToDelete == null || itemsToDelete.Count == 0)
                    return;
            }
            lastChance = true;
            DeleteItems();
        }
        public static void DeleteItems()
        {
            if (itemsToDelete != null)
            {
                int tries = lastChance ? 5 : 1;

                for (int i = 0; i < tries; i++)
                {
                    List<string> items;
                    
                    lock (deleteLock)
                    {
                        items = itemsToDelete.ToList();
                    }
                    for (int j = 0; j < items.Count; j++)
                    {
                        bool success = true;

                        if (cancel && !lastChance)
                            return;

                        string s = items[j];
                        
                        try
                        {
                            if (File.Exists(s))
                            {
                                FileInfo fi = new FileInfo(s);
                                DirectoryInfo di = fi.Directory;
                                success = recycle(s);
                                if (success)
                                {
                                    cull(di);
                                }
                            }
                            else
                            {
                                DirectoryInfo di = new DirectoryInfo(s);
                                if (di.Exists)
                                {
                                    cull(di);
                                    if (di.Parent != null)
                                        cull(di.Parent);
                                }
                            }
                            lock (deleteLock)
                            {
                                if (success)
                                {
                                    itemsToDelete.Remove(s);
                                }
                                if (itemsToDelete.Count == 0)
                                    return;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(ex.ToString());
                        }
                    } // next i
                    if (lastChance)
                    {
                        Thread.Sleep(200);
                    }
                }
            }
        }
        private static void cull(DirectoryInfo di)
        {
            try
            {
                if (cancel && !lastChance)
                    return;

                if (!di.Exists)
                    return;

                if (di.GetFiles().Length > 0)
                    return;

                foreach (DirectoryInfo ddi in di.GetDirectories())
                        cull(ddi);

                if (isEmpty(di))
                    recycle(di.FullName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }


        public static void RecycleFiles(List<Track> Tracks)
        {
            List<string> paths = new List<string>();
            foreach (Track t in Tracks)
                paths.Add(t.FilePath);

            lock (deleteLock)
            {
                if (itemsToDelete == null)
                    itemsToDelete = new List<string>();

                lock (deleteLock)
                {
                    itemsToDelete = itemsToDelete.Union(paths, StringComparer.OrdinalIgnoreCase).ToList();
                }
            }
            Clock.DoOnNewThread(DeleteItems);
        }
        
        [Flags]
        public enum FileOperationFlags : ushort
        {
            FOF_SILENT = 0x0004,
            FOF_NOCONFIRMATION = 0x0010,
            FOF_ALLOWUNDO = 0x0040,
            FOF_SIMPLEPROGRESS = 0x0100,
            FOF_NOERRORUI = 0x0400,
            FOF_WANTNUKEWARNING = 0x4000,
        }

        public enum FileOperationType : uint
        {
            FO_MOVE = 0x0001,
            FO_COPY = 0x0002,
            FO_DELETE = 0x0003,
            FO_RENAME = 0x0004,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
        private struct SHFILEOPSTRUCT
        {

            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.U4)]
            public FileOperationType wFunc;
            public string pFrom;
            public string pTo;
            public FileOperationFlags fFlags;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);
        
        private static bool recycle(string path)
        {
            try
            {
                SHFILEOPSTRUCT fs = new SHFILEOPSTRUCT();
                fs.wFunc = FileOperationType.FO_DELETE;

                // important to double-terminate the string.
                fs.pFrom = path + '\0' + '\0';
                fs.fFlags = FileOperationFlags.FOF_ALLOWUNDO | FileOperationFlags.FOF_NOCONFIRMATION | FileOperationFlags.FOF_NOERRORUI | FileOperationFlags.FOF_SILENT;
                if (SHFileOperation(ref fs) == 0)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return false;
            }
        }
        private static bool isEmpty(DirectoryInfo Directory)
        {
            if (Directory.GetFiles().Length > 0)
                return false;
            foreach (DirectoryInfo di in Directory.GetDirectories())
                if (!isEmpty(di))
                    return false;

            return true;
        }
        public static void GetTags(Track Track)
        {
            if ((Track.ChangeType & ChangeType.WriteTags) == 0x00)
            {
                Dictionary<AudioStreamFile.TagType, object> tags = new Dictionary<AudioStreamFile.TagType, object>();

                TAG_INFO tagInfo = BassTags.BASS_TAG_GetFromFile(Track.FilePath, false, false);

                if (tagInfo != null)
                {
                    try
                    {
                        Track.Artist = trim(tagInfo.artist);
                        Track.Album = trim(tagInfo.album);
                        Track.AlbumArtist = trim(tagInfo.albumartist);
                        Track.Composer = trim(tagInfo.composer);
                        Track.Title = trim(tagInfo.title);
                        Track.Grouping = trim(tagInfo.NativeTag("TIT1"));
                        Track.Genre = trim(tagInfo.genre);
                        Track.Encoder = trim(tagInfo.encodedby);
                        Track.Bitrate = tagInfo.bitrate;
                        Track.Duration = (int)(tagInfo.duration * 1000.0 + 0.5);
                        Track.Compilation = (trim(tagInfo.NativeTag("TCMP")) == "1");

                        int trk;
                        string trkVal = trim(tagInfo.track);
                        if (Int32.TryParse(trkVal, out trk))
                            Track.TrackNum = trk;
                        else if ((trkVal.IndexOf('/') > 0) && Int32.TryParse(trkVal.Substring(0, trkVal.IndexOf('/')), out trk))
                            Track.TrackNum = trk;
                        else
                            Track.TrackNum = 0;

                        int yr;
                        Track.Year = 0;
                        if (Int32.TryParse(trim(tagInfo.year), out yr))
                        {
                            Track.Year = yr;
                        }
                        else if (tagInfo.year.IndexOf('\0') > 0)
                        {
                            if (Int32.TryParse(trim(tagInfo.year.Substring(0, tagInfo.year.IndexOf('\0'))), out yr))
                                Track.Year = yr;
                        }

                        int dsk;
                        string dskVal = trim(tagInfo.NativeTag("TPOS"));
                        
                        if (Int32.TryParse(dskVal, out dsk))
                            Track.DiskNum = dsk;
                        else if ((dskVal.IndexOf('/') > 0) && Int32.TryParse(dskVal.Substring(0, dskVal.IndexOf('/')), out dsk))
                            Track.DiskNum = dsk;
                        else
                            Track.DiskNum = 0;

                        if (!Track.HasReplayGainInfoAlbum || !Track.HasReplayGainInfoTrack || Setting.WriteReplayGainTags)
                            LoadReplayGain(Track, tagInfo);

                        UpdateTrackInfo(Track, tagInfo.channelinfo);
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.ToString());
                    }
                }
                if (Track.Title.Length == 0 ||
                    Track.Artist.Length == 0 ||
                    Track.Album.Length == 0 ||
                    Track.TrackNum == 0)
                {
                    tryAltTagging(Track);

                    if (Track.Title.Length == 0)
                        Track.Title = Path.GetFileNameWithoutExtension(Track.FilePath);
                }
            }
        }
        private static readonly char[] gainChars = new char[] { ':', '=', '+' };

        public static void LoadReplayGain(Track Track, TAG_INFO tagInfo)
        {
            foreach (string s in tagInfo.NativeTags)
            {
                if (s.IndexOf("album_gain", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    float replayGainDB;
                    int i = s.LastIndexOfAny(gainChars);
                    int j = s.IndexOf(' ', i) - i;

                    if (i > 0)
                    {
                        if (j > 0)
                        {
                            if (float.TryParse(s.Substring(i + 1).Substring(0, j), out replayGainDB))
                                Track.ReplayGainAlbum = Math.Max(-20f, Math.Min(20f, replayGainDB));
                        }
                        else
                        {
                            if (float.TryParse(s.Substring(i + 1), out replayGainDB))
                                Track.ReplayGainAlbum = Math.Max(-20f, Math.Min(20f, replayGainDB));
                        }
                    }
                }
                else if (s.IndexOf("track_gain", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    float replayGainDB;
                    int i = s.LastIndexOfAny(gainChars);
                    int j = s.IndexOf(' ', i) - i;

                    if (i > 0)
                    {
                        if (j > 0)
                        {
                            if (float.TryParse(s.Substring(i + 1).Substring(0, j), out replayGainDB))
                                Track.ReplayGainTrack = Math.Max(-20f, Math.Min(20f, replayGainDB));
                        }
                        else
                        {
                            if (float.TryParse(s.Substring(i + 1), out replayGainDB))
                                Track.ReplayGainTrack = Math.Max(-20f, Math.Min(20f, replayGainDB));
                        }
                    }
                }
            }
        }

        public static void UpdateTrackInfo(Track Track, BASS_CHANNELINFO Info)
        {
            switch (Info.ctype)
            {
                case BASSChannelType.BASS_CTYPE_STREAM_AAC:
                    Track.Type = Track.FileType.AAC;
                    break;
                case BASSChannelType.BASS_CTYPE_STREAM_AC3:
                    Track.Type = Track.FileType.AC3;
                    break;
                case BASSChannelType.BASS_CTYPE_STREAM_AIFF:
                    Track.Type = Track.FileType.AIFF;
                    break;
                case BASSChannelType.BASS_CTYPE_STREAM_ALAC:
                    Track.Type = Track.FileType.ALAC;
                    break;
                case BASSChannelType.BASS_CTYPE_STREAM_APE:
                    Track.Type = Track.FileType.APE;
                    break;
                case BASSChannelType.BASS_CTYPE_STREAM_FLAC:
                    Track.Type = Track.FileType.FLAC;
                    break;
                case BASSChannelType.BASS_CTYPE_STREAM_MP1:
                    Track.Type = Track.FileType.MP1;
                    break;
                case BASSChannelType.BASS_CTYPE_STREAM_MP2:
                    Track.Type = Track.FileType.MP2;
                    break;
                case BASSChannelType.BASS_CTYPE_STREAM_MPC:
                    Track.Type = Track.FileType.MPC;
                    break;
                case BASSChannelType.BASS_CTYPE_STREAM_OGG:
                    Track.Type = Track.FileType.OGG;
                    break;
                case BASSChannelType.BASS_CTYPE_STREAM_WAV:
                    Track.Type = Track.FileType.WAV;
                    break;
                case BASSChannelType.BASS_CTYPE_STREAM_WMA:
                    Track.Type = Track.FileType.WMA;
                    break;
                case BASSChannelType.BASS_CTYPE_STREAM_WV:
                    Track.Type = Track.FileType.WV;
                    break;
            }

            Track.SampleRate = Info.freq;
            Track.NumChannels = Info.chans;
        }

        private enum ReadOnlyDisposition { Null, Ignore, Skip, Cancel };

        private static ReadOnlyDisposition readOnlyDisposition;
        private static bool waitingForUserReadOnlyResponse;
        private static void getUserReadonlyPref()
        {
            waitingForUserReadOnlyResponse = true;
            Clock.DoOnMainThread(getUserReadonlyPrefMainThread);
            while (waitingForUserReadOnlyResponse)
            {
                Thread.Sleep(100);
            }
        }
        private static void getUserReadonlyPrefMainThread()
        {
            List<frmTaskDialog.Option> options = new List<frmTaskDialog.Option>();
            options.Add(new frmTaskDialog.Option("Yes, continue.", "The read only marker will be cleared and the files will be changed.", 0));
            options.Add(new frmTaskDialog.Option("No, skip read only files", "Other files will still be changed", 1));
            options.Add(new frmTaskDialog.Option("No, cancel.", "QuuxPlayer will stop changing files.", 2));
            frmTaskDialog td = new frmTaskDialog("Read Only Files Found", "Some of the sound files that QuuxPlayer has been directed to move or rename are marked as 'Read Only' in Windows. Do you want to change them anyway?", options);

            td.ShowDialog(frmMain.GetInstance());

            switch (td.ResultIndex)
            {
                case 0:
                    readOnlyDisposition = ReadOnlyDisposition.Ignore;
                    break;
                case 1:
                    readOnlyDisposition = ReadOnlyDisposition.Skip;
                    break;
                case 2:
                    readOnlyDisposition = ReadOnlyDisposition.Cancel;
                    break;
            }
            waitingForUserReadOnlyResponse = false;
        }
        private static void write(Track Track)
        {
            if (File.Exists(Track.FilePath) && Track.ChangeType != ChangeType.None && Database.TrackIsInLibrary(Track))
            {
                bool readOnly = Track.ReadOnly;
                if (readOnly && readOnlyDisposition == ReadOnlyDisposition.Null)
                {
                    getUserReadonlyPref();
                }
                if (readOnly)
                {
                    switch (readOnlyDisposition)
                    {
                        case ReadOnlyDisposition.Cancel:
                            return;
                        case ReadOnlyDisposition.Skip:
                            if (readOnly)
                            {
                                Track.ChangeType = ChangeType.None;
                                return;
                            }
                            break;
                        case ReadOnlyDisposition.Ignore:
                            File.SetAttributes(Track.FilePath, File.GetAttributes(Track.FilePath) & ~FileAttributes.ReadOnly);
                            break;
                    }
                }
                try
                {
                    bool rename = (Track.ChangeType & ChangeType.Rename) == ChangeType.Rename;
                    bool move = (Track.ChangeType & ChangeType.Move) == ChangeType.Move;
                    bool ignoreContainment = (Track.ChangeType & ChangeType.IgnoreContainment) == ChangeType.IgnoreContainment;
                    bool tags = (Track.ChangeType & ChangeType.WriteTags) == ChangeType.WriteTags;
                    bool image = (Track.ChangeType & ChangeType.EmbedImage) == ChangeType.EmbedImage;

                    if (rename || move || ignoreContainment)
                        organize(Track, rename, move, ignoreContainment);

                    if (tags || image)
                        writeTags(Track, tags, image);

                    Track.FileDate = File.GetLastWriteTime(Track.FilePath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
                Database.IncrementDatabaseVersion(true);
            }
        }

        private static void organize(Track Track, bool Rename, bool Move, bool IgnoreContainment)
        {
            try
            {
                string oldDir = Path.GetDirectoryName(Track.FilePath);
                string oldName = Path.GetFileName(Track.FilePath);

                string newName = String.Empty;
                
                if (Rename)
                    newName = TrackWriter.GetRename(Track, Track.RenameFormat); // this may return empty string

                if (newName.Length == 0)
                    newName = Path.GetFileName(Track.FilePath);

                string newDir;

                if (!Move)
                {
                    newDir = oldDir;
                }
                else
                {
                    string newSubDir = TrackWriter.GetDirFormat(Track, Setting.DefaultDirectoryFormat);

                    if (newSubDir.Length > 0)
                        newDir = Path.Combine(Setting.TopLevelDirectory, newSubDir);
                    else
                        newDir = Setting.TopLevelDirectory;
                }

                string newPath = Path.Combine(newDir, newName);

                if (pathIsDifferent(newDir, oldDir) ||
                        (String.Compare(newName, oldName, StringComparison.OrdinalIgnoreCase) != 0) && ((!similar(newName, oldName) || !File.Exists(newPath))))
                {
                    if (String.Compare(Track.FilePath, newPath, StringComparison.OrdinalIgnoreCase) != 0) // this can be the same b/c of subtle differences in how the directory parsing works in earlier checks
                    {
                        if (!Directory.Exists(newDir))
                            Directory.CreateDirectory(newDir);

                        bool contained = (Setting.TopLevelDirectory.Length > 0 && Track.FilePath.StartsWith(Setting.TopLevelDirectory, StringComparison.OrdinalIgnoreCase));

                        if (contained || IgnoreContainment)
                        {
                            bool copy = !Track.FilePath.StartsWith(Path.GetPathRoot(newPath), StringComparison.OrdinalIgnoreCase);

                            bool goodTarget = false;

                            if (File.Exists(newPath))
                            {
                                for (int i = 1; i < 10; i++)
                                    if (!File.Exists((newPath = Path.Combine(newDir, Path.GetFileNameWithoutExtension(newName) + i.ToString() + Path.GetExtension(newName)))))
                                    {
                                        goodTarget = true;
                                        break;
                                    }
                            }
                            else
                            {
                                goodTarget = true;
                            }

                            if (goodTarget)
                            {
                                System.Diagnostics.Debug.Assert(!File.Exists(newPath));
                                try
                                {
                                    if (copy)
                                    {
                                        Controller.ShowMessage("Copying " + Track.ToShortString() + "...");
                                        File.Copy(Track.FilePath, newPath);
                                        Track.FilePath = newPath;
                                    }
                                    else
                                    {
                                        Controller.ShowMessage("Moving " + Track.ToShortString() + "...");
                                        try
                                        {
                                            File.Move(Track.FilePath, newPath);
                                            Track.FilePath = newPath;
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine(ex.ToString());
                                            File.Copy(Track.FilePath, newPath);
                                            TrackWriter.AddToDeleteList(Track.FilePath);
                                            Track.FilePath = newPath;
                                        }
                                        if (String.Compare(oldDir, newDir, StringComparison.OrdinalIgnoreCase) != 0)
                                            TrackWriter.AddToDeleteList(oldDir);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                                }
                            }
                        }
                    }
                }
                Track.ChangeType &= ~(ChangeType.Rename | ChangeType.Move | ChangeType.IgnoreContainment);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }
        private static bool pathIsDifferent(string Path1, string Path2)
        {
            if (Path1.Contains(Path.AltDirectorySeparatorChar))
                Path1 = Path1.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            if (Path2.Contains(Path.AltDirectorySeparatorChar))
                Path2 = Path2.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            if (Path1.Length > 0 && Path1[Path1.Length - 1] == Path.DirectorySeparatorChar)
            {
                if (Path2.Length == 0 || Path2[Path2.Length - 1] != Path.DirectorySeparatorChar)
                    Path2 = Path2 + Path.DirectorySeparatorChar;
            }
            else if (Path2.Length > 0 && Path2[Path2.Length - 1] == Path.DirectorySeparatorChar)
            {
                Path1 = Path1 + Path.DirectorySeparatorChar;
            }

            return String.Compare(Path1, Path2, StringComparison.OrdinalIgnoreCase) != 0;
        }
        private static bool similar(string New, string Old)
        {
            // detect if we are trying to strip off a number added for uniqueness
            if (Old.Length == New.Length + 1)
            {
                string old = Path.GetFileNameWithoutExtension(Old);
                string @new = Path.GetFileNameWithoutExtension(New);
                if (old.StartsWith(@new, StringComparison.OrdinalIgnoreCase))
                {
                    char c = old[old.Length - 1];
                    if (c >= '1' && c <= '9')
                        return true;
                }
            }
            return false;
        }
        private static void writeTags(Track Track, bool Tags, bool Image)
        {
            TagLib.File file = TagLib.File.Create(Track.FilePath);
            file.Mode = TagLib.File.AccessMode.Write;
            TagLib.Tag tagInfo = file.Tag;

            if (Tags)
            {
                tagInfo.Title = Track.Title;
                tagInfo.Performers = new string[] { Track.Artist };

                tagInfo.Album = Track.Album;
                tagInfo.AlbumArtists = new string[] { Track.AlbumArtist };

                tagInfo.Composers = new string[] { Track.Composer };

                tagInfo.Grouping = Track.Grouping;

                tagInfo.Genres = new string[] { Track.GenreAllowBlank };

                uint tn = (uint)Track.TrackNum;
                if (tn > 0)
                {
                    tagInfo.Track = tn;
                }
                tagInfo.TrackCount = 0;

                uint dn = (uint)Track.DiskNum;
                if (dn > 0)
                    tagInfo.Disc = dn;
                tagInfo.DiscCount = 0;

                uint year = (uint)Track.Year;
                if (year > 1900 && year < 2100)
                    tagInfo.Year = year;

                TagLibTagSet tagSet = GetTagLibTagSet(file);

                setTextField(tagSet, "TCMP", "TCMP", "TCMP", "TCMP", (Track.Compilation ? "1" : "0"));

                if (Setting.WriteReplayGainTags)
                    writeReplayGainTags(tagSet.ID3Tag, Track);
            }
            if (Image)
            {
                embedImage(Track, tagInfo);
            }
            file.Save();
            Track.ChangeType &= ~(ChangeType.WriteTags | ChangeType.EmbedImage);
        }
        public static void EmbedImage(Track Track)
        {
            TagLib.File file = TagLib.File.Create(Track.FilePath);
            file.Mode = TagLib.File.AccessMode.Write;
            TagLib.Tag tagInfo = file.Tag;
            embedImage(Track, tagInfo);
            file.Save();
        }
        private static void embedImage(Track Track, TagLib.Tag tagInfo)
        {
            if (Track.Cover == null)
            {
                tagInfo.Pictures = new TagLib.IPicture[0];
            }
            else
            {
                TagLib.Picture p = new TagLib.Picture();
                p.Type = TagLib.PictureType.FrontCover;
                p.Data = new TagLib.ByteVector(Track.Cover.ImageBytesForEmbed);
                tagInfo.Pictures = new TagLib.IPicture[] { p };
            }
            ImageItem.RegisterAsEmbedded(Track);
        }
        private class TagLibTagSet
        {
            public TagLib.Mpeg4.AppleTag AppleTag;
            public TagLib.Id3v2.Tag ID3Tag;
            public TagLib.Ogg.XiphComment XiphTag;
            public TagLib.Ape.Tag ApeTag;
        }
        private static TagLibTagSet GetTagLibTagSet(TagLib.File File)
        {
            TagLibTagSet ret = new TagLibTagSet();
            
            ret.AppleTag = (TagLib.Mpeg4.AppleTag)File.GetTag(TagLib.TagTypes.Apple, true);
            ret.ID3Tag = (TagLib.Id3v2.Tag)File.GetTag(TagLib.TagTypes.Id3v2, true);
            ret.XiphTag = (TagLib.Ogg.XiphComment)File.GetTag(TagLib.TagTypes.Xiph, true);
            ret.ApeTag = (TagLib.Ape.Tag)File.GetTag(TagLib.TagTypes.Ape, true);

            return ret;
        }
        private static void writeReplayGainTags(TagLib.Id3v2.Tag Tags, Track Track)
        {
            if (Tags != null)
            {
                Tags.RemoveFrames("TXXX");
                
                var f = new TagLib.Id3v2.UserTextInformationFrame("replaygain_track_gain");
                f.Text = new string[] { String.Format("{0:0.00} dB", Track.ReplayGainTrack) };
                Tags.AddFrame(f);


                f = new TagLib.Id3v2.UserTextInformationFrame("replaygain_album_gain");
                f.Text = new string[] { String.Format("{0:0.00} dB", Track.ReplayGainTrack) };
                Tags.AddFrame(f);
            }
        }
        private static void setTextField(TagLibTagSet Tags,
                                         TagLib.ByteVector AppleName,
                                         TagLib.ByteVector ID3Name,
                                         string XiphName,
                                         string ApeName,
                                         string Value)
        {
            try
            {
                if (Tags.AppleTag != null)
                    Tags.AppleTag.SetText(AppleName, Value);

                if (Tags.ID3Tag != null)
                    Tags.ID3Tag.SetTextFrame(ID3Name, Value);

                if (Tags.XiphTag != null)
                    Tags.XiphTag.SetField(XiphName, Value);

                if (Tags.ApeTag != null)
                    Tags.ApeTag.AddValue(ApeName, Value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        private static string[] insertIntoArray(string[] Array, string Item)
        {
            if (Array.Length > 1)
            {
                List<string> ss = new List<string>(Array);

                ss.RemoveAt(0);
                ss.Insert(0, Item);

                return ss.ToArray();
            }
            else
            {
                return new string[] { Item };
            }
        }

        private static void tryAltTagging(Track Track)
        {
            try
            {
                TagLib.File file = TagLib.File.Create(Track.FilePath);
                TagLib.Tag tagInfo = file.Tag;

                if (Track.Artist.Length == 0)
                    Track.Artist = trim(tagInfo.FirstPerformer);

                if (Track.Album.Length == 0)
                    Track.Album = trim(tagInfo.Album);

                if (Track.Composer.Length == 0)
                    Track.Composer = trim(tagInfo.FirstComposer);

                if (Track.Grouping.Length == 0)
                    Track.Grouping = trim(tagInfo.Grouping);

                if (Track.Title.Length == 0)
                    Track.Title = trim(tagInfo.Title);

                if (Track.Genre.Length == 0 || Track.Genre == Localization.NO_GENRE)
                    Track.Genre = trim(tagInfo.FirstGenre);

                if (Track.AlbumArtist.Length == 0)
                    Track.AlbumArtist = trim(tagInfo.FirstAlbumArtist);

                if (Track.TrackNum == 0)
                    Track.TrackNum = (int)tagInfo.Track;

                if (Track.Year == 0)
                    Track.Year = (int)tagInfo.Year;

                if (Track.DiskNum == 0)
                    Track.DiskNum = (int)tagInfo.Disc;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }
        private static string trim(string Input)
        {
            if (Input == null)
                return String.Empty;

            return Input.Trim(trimChars);
        }
        public static bool SuspendWriting
        {
            get { return suspendWriting; }
            set
            {
                if (suspendWriting != value)
                {
                    suspendWriting = value;
                    if (!suspendWriting && !running)
                        Start();
                }
            }
        }
    }
}
