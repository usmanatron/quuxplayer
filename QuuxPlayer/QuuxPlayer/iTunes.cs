/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using iTunesLib;
using ITDETECTORLib;

namespace QuuxPlayer
{
    internal static class iTunes
    {
        private static string playlistName;
        private static IEnumerable<Track> tracks;
        public static bool Busy { get; set; }
        public static bool Cancel { get; set; }

        public static bool ITunesIsAvailable
        {
            get
            {
                ITDETECTORLib.iTunesDetector iDetect = new ITDETECTORLib.iTunesDetector();
                return iDetect.IsiTunesAvailable;
            }
        }
        public static void GetLibrary()
        {
            ITDETECTORLib.iTunesDetector iDetect = new ITDETECTORLib.iTunesDetector();
            if (iDetect.IsiTunesAvailable)
            {
                Controller.ShowMessage("Initializing iTunes...");

                Cancel = false;
                Busy = true;

                Clock.DoOnNewThread(getLibrary, 200);
            }
            else
            {
                ShowError();
            }
        }
        private static void getLibrary()
        {
            iTunesApp app = new iTunesApp();

            app.ForceToForegroundOnDialog = true;

            IITTrackCollection tc = app.LibraryPlaylist.Tracks;

            Controller.ShowMessage(tc.Count.ToString() + " tracks found...");

            List<string> tracks = new List<string>();

            foreach (IITTrack t in tc)
            {
                if (Cancel)
                    break;

                if (t.Kind == ITTrackKind.ITTrackKindFile)
                {
                    string l = (t as IITFileOrCDTrack).Location;
                    Controller.ShowMessage(t.Name);
                    tracks.Add(l);
                }
            }
            if (!Cancel)
                FileAdder.AddItemsToLibrary(tracks, String.Empty, true, Controller.GetInstance().RefreshAll);
        }
        public static void CreatePlaylist(string Name, List<Track> Tracks)
        {
            ITDETECTORLib.iTunesDetector iDetect = new ITDETECTORLib.iTunesDetector();
            if (iDetect.IsiTunesAvailable)
            {
                Controller.ShowMessage("Initializing iTunes...");

                Cancel = false;
                Busy = true;

                playlistName = Name;
                tracks = Tracks.ToList();
                Clock.DoOnNewThread(createPlaylist, 200);
            }
            else
            {
                ShowError();
            }
        }
        public static void ShowError()
        {
            QMessageBox.Show(frmMain.GetInstance(), "iTunes Error - Is iTunes installed?", "iTunes Error", QMessageBoxIcon.Error);
        }
        private static void createPlaylist()
        {
            try
            {
                iTunesApp app = new iTunesApp();

                app.ForceToForegroundOnDialog = true;

                //app.SetOptions(); would be nice to kill autoarrange

                IITPlaylistCollection pl = app.LibrarySource.Playlists;
                IITUserPlaylist playlist = null;
                
                playlist = findPlaylist(pl, playlist);

                if (playlist == null)
                {
                    playlist = (IITUserPlaylist)app.CreatePlaylist(playlistName);
                }
                else
                {
                    // remove tracks, how?
                    foreach (IITTrack t in playlist.Tracks)
                    {
                        //t.Delete(); <== ?
                    }
                }

                iTunesLib.IITLibraryPlaylist lp = app.LibraryPlaylist;

                IITTrackCollection itTracks = app.LibraryPlaylist.Tracks;

                Dictionary<string, int> libraryTrackDictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                List<string> libraryTrackFiles = new List<string>();

                foreach (IITTrack t in itTracks)
                {
                    if (Cancel)
                        break;

                    if (t.Kind == ITTrackKind.ITTrackKindFile)
                    {
                        string l = (t as IITFileOrCDTrack).Location;
                        if (l != null)
                        {
                            libraryTrackFiles.Add(l);
                            if (!libraryTrackDictionary.ContainsKey(l))
                                libraryTrackDictionary.Add(l, t.Index);
                        }
                    }
                }
                List<string> allTracks = new List<string>();
                foreach (Track t in tracks)
                    allTracks.Add(t.FilePath);

                object oo = (object)(allTracks.ToArray());
                playlist.AddFiles(ref oo);

                Controller.ShowMessage("Completed sending playlist to iTunes.");

                app.Quit();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                Clock.DoOnMainThread(ShowError);
            }
            Busy = false;
        }

        private static IITUserPlaylist findPlaylist(IITPlaylistCollection pl, IITUserPlaylist playlist)
        {
            playlist = null;

            int count = 0;

            while (playlist == null && ++count < 10)
            {
                foreach (IITPlaylist p in pl)
                {
                    if (p.Name == playlistName)
                    {
                        playlist = p as IITUserPlaylist;
                        if (playlist != null)
                        {
                            if (playlist.Kind != ITPlaylistKind.ITPlaylistKindUser ||
                                playlist.Smart ||
                                    (playlist.SpecialKind != ITUserPlaylistSpecialKind.ITUserPlaylistSpecialKindMusic &&
                                     playlist.SpecialKind != ITUserPlaylistSpecialKind.ITUserPlaylistSpecialKindNone))
                            {
                                playlistName = adjust(playlistName);
                                playlist = null;
                            }
                        }
                        break;
                    }
                }
            }
            return playlist;
        }
        private static string adjust(string Input)
        {
            if (Input.Length == 0)
                return "1";

            char c = Input[Input.Length - 1];
            if (c < '0' || c > '9')
                return Input + "1";

            if (c < '9')
            {
                return Input.Substring(0, Input.Length - 1) + ((char)(c + 1)).ToString();
            }

            System.Diagnostics.Debug.Assert(Input.EndsWith("9"));

            return adjust(Input.Substring(0, Input.Length - 1)) + "0";
        }
    }
}
