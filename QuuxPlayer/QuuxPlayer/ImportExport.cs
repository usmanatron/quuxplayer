/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal static class ImportExport
    {
        public static void ExportCSV(List<Track> Tracks, Form Parent)
        {
            SaveFileDialog sfd = new SaveFileDialog();

            sfd.AddExtension = true;
            sfd.CheckPathExists = true;
            sfd.DefaultExt = ".csv";
            sfd.FileName = Localization.Get(UI_Key.Export_CSV_Default_Filename);
            sfd.Filter = Localization.Get(UI_Key.Export_CSV_Filter);
            sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            sfd.RestoreDirectory = true;
            sfd.Title = Localization.Get(UI_Key.Export_CSV_Title);
            sfd.ValidateNames = true;
            sfd.AutoUpgradeEnabled = true;
            sfd.OverwritePrompt = true;

            if (sfd.ShowDialog(Parent) == DialogResult.OK)
            {
                System.IO.StreamWriter sw = new StreamWriter(sfd.FileName, false);

                sw.WriteLine(Track.CSVHeader);

                foreach (Track t in Tracks)
                    sw.WriteLine(t.CSV);

                sw.Close();
            }
        }
        public static void ExportPlaylist(string PlaylistName, List<Track> Tracks, Form Parent)
        {
            SaveFileDialog sfd = new SaveFileDialog();

            sfd.AddExtension = true;
            sfd.CheckPathExists = true;
            sfd.DefaultExt = ".m3u";
            sfd.FileName = Lib.ReplaceBadFilenameChars(PlaylistName) + ".m3u";
            sfd.Filter = Localization.Get(UI_Key.Export_Playlist_Filter);
            sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            sfd.RestoreDirectory = true;
            sfd.Title = Localization.Get(UI_Key.Export_Playlist_Title);
            sfd.ValidateNames = true;
            sfd.AutoUpgradeEnabled = true;
            sfd.OverwritePrompt = true;

            if (sfd.ShowDialog(Parent) == DialogResult.OK)
            {
                System.IO.StreamWriter sw = new StreamWriter(sfd.FileName, false);

                sw.WriteLine("#EXTM3U");

                foreach (Track t in Tracks)
                {
                    sw.WriteLine("#EXTINF:" + (int)t.Duration / 1000 + "," + t.Artist + " - " + t.Title);
                    sw.WriteLine(t.FilePath);
                }

                sw.Close();
            }
        }
        public static string ImportPlaylist(string FilePath, out List<Track> Tracks)
        {
            System.IO.StreamReader sr = new StreamReader(FilePath);

            string relativePath = Path.GetDirectoryName(FilePath);
            string playlistName = String.Empty;
            Tracks = new List<Track>();
            switch (Path.GetExtension(FilePath).ToLowerInvariant())
            {
                case ".m3u":
                    importM3U(Tracks, relativePath, sr);
                    break;
                case ".pls":
                    importPLS(Tracks, sr, true);
                    break;
                default:
                    return String.Empty;
            }

            if (Tracks.Count > 0)
            {
                playlistName = Path.GetFileNameWithoutExtension(FilePath);
            }
            sr.Close();
            return playlistName;
        }
        public static string ImportPlaylist(Form Parent, out List<Track> Tracks)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            ofd.AddExtension = true;
            ofd.CheckPathExists = true;
            ofd.DefaultExt = ".m3u";
            ofd.FileName = String.Empty;
            ofd.Filter = Localization.Get(UI_Key.Import_Playlist_Filter);
            ofd.FilterIndex = 3;
            ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            ofd.RestoreDirectory = true;
            ofd.Title = Localization.Get(UI_Key.Import_Playlist_Title);
            ofd.ValidateNames = true;
            ofd.AutoUpgradeEnabled = true;

            Tracks = new List<Track>();
            string playlistName = String.Empty;

            if (ofd.ShowDialog(Parent) == DialogResult.OK)
            {
                Lib.DoEvents();

                string relativePath = Path.GetDirectoryName(ofd.FileName);
                
                System.IO.StreamReader sr = new StreamReader(ofd.FileName);

                bool urlsFound = false;

                switch (Path.GetExtension(ofd.FileName).ToLowerInvariant())
                {
                    case ".m3u":
                        urlsFound = importM3U(Tracks, relativePath, sr);
                        break;
                    case ".pls":
                        urlsFound = importPLS(Tracks, sr, false);
                        break;
                }
                
                if (Tracks.Count > 0)
                {
                    playlistName = Path.GetFileNameWithoutExtension(ofd.FileName);
                }
                else if (!urlsFound)
                {
                    QMessageBox.Show(Parent,
                                     Localization.Get(UI_Key.Dialog_Import_Playlist_No_Tracks_Found),
                                     Localization.Get(UI_Key.Dialog_Import_Playlist_No_Tracks_Found_Title),
                                     QMessageBoxIcon.Warning);
                }
                sr.Close();
            }
            return playlistName;
        }

        private static bool importM3U(List<Track> Tracks, string relativePath, System.IO.StreamReader sr)
        {
            while (!sr.EndOfStream)
            {
                string s = sr.ReadLine();

                if (s.Contains("#"))
                    s = s.Substring(0, s.IndexOf('#'));

                if (File.Exists(s) && Track.IsValidExtension(Path.GetExtension(s)))
                {
                    if (!Path.IsPathRooted(s))
                        s = Path.Combine(relativePath, s);

                    if (File.Exists(s))
                    {
                        Track t = Track.Load(s);
                        if (t != null)
                            Tracks.Add(t);
                    }
                }
            }
            return false;
        }
        private static bool importPLS(List<Track> Tracks, System.IO.StreamReader sr, bool MaxOneURL)
        {
            bool urlsFound = false;

            List<string> lines = new List<string>();
            while (!sr.EndOfStream)
            {
                lines.Add(sr.ReadLine());
            }

            string noe = lines.FirstOrDefault(l =>l.StartsWith("numberofentries=", StringComparison.OrdinalIgnoreCase));

            if (noe != null)
            {
                int numLines;
                if (Int32.TryParse(noe.Substring("numberofentries=".Length), out numLines))
                {
                    for (int i = 1; i <= numLines; i++)
                    {
                        string prefix = "file" + i.ToString() + "=";
                        string line = lines.FirstOrDefault(l => l.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                        if (line == null)
                            return urlsFound;

                        line = line.Substring(prefix.Length).Trim();

                        if (line.StartsWith("http://"))
                        {
                            if (!urlsFound || !MaxOneURL)
                            {
                                string prefix2 = "title" + i.ToString() + "=";
                                string line2 = lines.FirstOrDefault(l => l.StartsWith(prefix2, StringComparison.OrdinalIgnoreCase));
                                if (line2 != null)
                                {
                                    Radio.AddStation(line, line2.Substring(prefix2.Length), MaxOneURL);
                                }
                                else
                                {
                                    Radio.AddStation(line, String.Empty, MaxOneURL);
                                }
                                urlsFound = true;
                            }
                        }
                        else if (File.Exists(line) && Track.IsValidExtension(Path.GetExtension(line)))
                        {
                            Track t = Track.Load(line);
                            if (t != null)
                                Tracks.Add(t);
                        }
                    }
                }
            }
            return urlsFound;
        }
    }
}
