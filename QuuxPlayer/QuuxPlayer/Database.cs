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
    public enum PlaylistType { None, Standard, Auto, NowPlaying, Duplicates, Ghosts }

    internal static class Database
    {
        private const int SCHEMA_VERSION = 10;

        private const int NO_EQUALIZER = -1;
        private const int EQUALIZER_OFF = -2;

        public static object LibraryLock = new object();

        private sealed class Playlist
        {
            private static readonly string[] splitChars = new string[] { Environment.NewLine, "\t" };

            private PlaylistType type = PlaylistType.Standard;

            private uint databaseVersionBasis = 0;
            private bool dirty;
            private List<PlaylistItem> items;
            private string expression = String.Empty;
            private string expressionRaw = String.Empty;

            public Playlist(string Name, PlaylistType Type)
            {
                System.Diagnostics.Debug.Assert(Type != PlaylistType.Auto);

                this.Name = Name;
                this.PreSorted = false;
                this.items = new List<PlaylistItem>();
                ExpressionTree = null;
                expression = String.Empty;
                this.type = Type;
                this.dirty = false;
            }
            public Playlist(string Name, string Expression, PlaylistType Type)
            {
                System.Diagnostics.Debug.Assert(Type == PlaylistType.Auto);

                this.Name = Name;
                this.PreSorted = false;
                this.items = new List<PlaylistItem>();
                this.Expression = Expression;
                this.type = Type;
                this.dirty = true;
            }

            public static List<Track> Library
            { get; set; }

            public bool PreSorted
            { get; private set; }
            public string Name
            { get; set; }
            public ExpressionTree ExpressionTree
            { get; set; }

            public bool IsDynamic
            {
                get
                {
                    return this.PlaylistType == PlaylistType.Auto || this.PlaylistType == PlaylistType.Duplicates || this.PlaylistType == PlaylistType.Ghosts;
                }
            }
            public PlaylistType PlaylistType
            {
                get { return type; }
                set
                {
                    if (type != value)
                    {
                        type = value;
                        dirty = this.IsDynamic;
                        PreSorted = false;
                    }
                }
            }
            public string Expression
            {
                get
                {
                    return expressionRaw;
                }
                set
                {
                    if (expressionRaw != value)
                    {
                        expressionRaw = value;
                        expression = ExpressionTree.CleanExpression(expressionRaw);
                        dirty = true;
                    }
                }
            }
            public List<PlaylistItem> Items
            {
                get
                {
                    if (IsDynamic)
                        Update();

                    return items;
                }
                set
                {
                    items = value;
                    dirty = true;
                }
            }
            public string SortName
            {
                get
                {
                    switch (this.type)
                    {
                        case PlaylistType.NowPlaying:
                            return "!";
                        case PlaylistType.Duplicates:
                            return "!!";
                        case PlaylistType.Ghosts:
                            return "!!!";
                        default:
                            return Name;
                    }
                }
            }

            public void Update()
            {
                System.Diagnostics.Debug.Assert(IsDynamic);

                if (dirty || (databaseVersionBasis < Database.Version))
                {
                    List<Track> l = null;
                    items.Clear();

                    switch (PlaylistType)
                    {
                        case PlaylistType.Auto:

                            ExpressionTree = new ExpressionTree(expression);

                            ExpressionTree.Compile();

                            bool sorted;
                            
                            l = ExpressionTree.Filter(Library, out sorted);
                            
                            PreSorted = sorted;

                            break;
                        case PlaylistType.Duplicates:

                            var l1 = (from t in LibrarySnapshot
                                      group t by t.SearchString
                                          into g
                                          where g.Count() > 1
                                          select g.Key).ToList();

                            l = Database.FindAllTracks(t => l1.Contains(t.SearchString));
                            l.Sort((a, b) => (String.Compare(a.SearchString, b.SearchString, StringComparison.Ordinal)));
                            this.PreSorted = true;

                            break;
                        case PlaylistType.Ghosts:
                            GhostDetector.DetectGhosts(Controller.GetInstance().RefreshIfGhosts);
                            l = Database.FindAllTracks(t => t.Exists.HasValue && !t.Exists.Value);
                            l.Sort((a, b) => (String.Compare(a.MainGroup, b.MainGroup, StringComparison.OrdinalIgnoreCase)));

                            break;
                    }
                    if (l != null)
                        foreach (Track t in l)
                            items.Add(new PlaylistItem(this, t));

                    databaseVersionBasis = Database.Version;
                }
            }
        }
        private sealed class PlaylistItem
        {
            public Playlist Playlist;
            public Track Track;

            public PlaylistItem(Playlist Playlist, Track Track)
            {
                this.Playlist = Playlist;
                this.Track = Track;
            }
        }

        private static List<Track> library;
        private static List<Playlist> playlists;
        private static Dictionary<string, Playlist> playlistDictionary;
        private static List<string> crawlDirs;
        private static Dictionary<SettingType, string> settings;
        private static Playlist nowPlayingPlaylist;
        private static bool? isLargeLibrary = null;

        public static bool IsLargeLibrary
        {
            get
            {
                if (!isLargeLibrary.HasValue)
                {
                    isLargeLibrary = (library.Count * playlists.Count > 200000);
                }
                return isLargeLibrary.Value;
            }
        }

        public static void Start()
        {
            Version = 1; // not zero; force refreshes

            library = new List<Track>();
            Playlist.Library = library;

            playlists = new List<Playlist>();
            playlistDictionary = new Dictionary<string, Playlist>(StringComparer.OrdinalIgnoreCase);
            settings = new Dictionary<SettingType, string>();
            crawlDirs = new List<string>();
            nowPlayingPlaylist = new Playlist(Localization.NOW_PLAYING, PlaylistType.NowPlaying);

            Database.Open();
        }
        public static List<string> CrawlDirs
        {
            get { return crawlDirs; }
            set { crawlDirs = value; }
        }
        
        public static void IncrementDatabaseVersion(bool IsAddOrRemove)
        {
            Version++;
            if (IsAddOrRemove)
                LatestLibraryAddOrRemove = Version;
        }
        public static void Open()
        {
            string path = getDatabaseFilePath();

            if (!File.Exists(path))
            {
                if (!File.Exists(getBackupFilePath()))
                {
                    if (File.Exists(getOldDatabaseFilePath()))
                    {
                        path = getOldDatabaseFilePath();
                    }
                    else
                    {
                        CreateNewDatabase(true);
                        return;
                    }
                }
                else
                {
                    File.Copy(getBackupFilePath(), path);
                }
            }
            open(path);
        }
        public static void Close(Form MainForm)
        {
            string tempPath = Path.GetTempFileName();

            BinaryWriter bw = null;

            try
            {
                bw = new BinaryWriter(File.OpenWrite(tempPath));

                bw.Write(SCHEMA_VERSION);
                bw.Write(false); // was a flag to indicate whether written with Pro version

                List<EqualizerSetting> eq = Equalizer.GetEqualizerSettings();

                saveEqualizers(bw, eq);

                Dictionary<EqualizerSetting, int> eqIndexes = new Dictionary<EqualizerSetting, int>();
                for (int i = 0; i < eq.Count; i++)
                    eqIndexes.Add(eq[i], i);
                eqIndexes.Add(EqualizerSetting.Off, EQUALIZER_OFF);

                saveTracks(bw, eqIndexes);
                savePlaylists(bw);
                saveSettings(bw);
                saveAutoUpdateFolders(bw);
                saveLastFMData(bw);
                saveRadioStations(bw);
                savePodcasts(bw);

                bw.Close();
                bw = null;

                string path = getDatabaseFilePath();
                if (File.Exists(path))
                {
                    File.Copy(path, getBackupFilePath(), true);
                }
                File.Copy(tempPath, path, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                QMessageBox.Show(MainForm, "Error saving library file.", "Error saving file", QMessageBoxIcon.Error);                
            }
            finally
            {
                if (bw != null)
                    bw.Close();
                
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        private static void saveTracks(BinaryWriter bw, Dictionary<EqualizerSetting, int> eqIndexes)
        {
            bw.Write(library.Count);

            for (int i = 0; i < library.Count; i++)
            {
                Track t = library[i];
                t.ID = i;
                bw.Write(i);
                bw.Write(t.FilePath);
                bw.Write((Int32)t.Type);
                bw.Write(t.Title);
                bw.Write(t.Album);
                bw.Write(t.Artist);
                bw.Write(t.AlbumArtist);
                bw.Write(t.Composer);
                bw.Write(t.Grouping);
                bw.Write(t.Genre);
                bw.Write(t.Duration);
                bw.Write(t.TrackNum);
                bw.Write(t.DiskNum);
                bw.Write(t.Year);
                bw.Write(t.PlayCount);
                bw.Write(t.InternalRating);
                bw.Write(t.Bitrate);
                bw.Write(t.FileSize);
                bw.Write(t.Compilation);
                bw.Write(t.FileDate.Ticks);
                bw.Write(t.LastPlayedDate.Ticks);
                bw.Write(t.AddDate.Ticks);
                bw.Write(t.Encoder);
                bw.Write(t.NumChannels);
                bw.Write(t.SampleRate);
                bw.Write((int)t.ChangeType);// & ~ChangeType.FileName));

                if (t.Equalizer == null)
                    bw.Write(NO_EQUALIZER);
                else
                    bw.Write(eqIndexes[t.Equalizer]);

                bw.Write(t.ReplayGainAlbumInternal);
                bw.Write(t.ReplayGainTrackInternal);
            }
        }

        private static void saveLastFMData(BinaryWriter bw)
        {
            if (GetSetting(SettingType.LastFMOn, false))
            {
                bw.Write(LastFM.Backlog.Count);

                foreach (Track t in LastFM.Backlog)
                {
                    if (t != null)
                        bw.Write(t.ID);
                    else
                        bw.Write((int)-1);
                }
            }
            else
            {
                bw.Write((int)0);
            }
        }
        private static void loadPodcasts(BinaryReader br)
        {
            int numPodcastSubscriptions = br.ReadInt32();
            List<PodcastSubscription> subscriptions = new List<PodcastSubscription>();
            for (int i = 0; i < numPodcastSubscriptions; i++)
            {
                PodcastSubscription ps = new PodcastSubscription(br.ReadString(), // Name
                                                                 br.ReadString(), // URL
                                                                 br.ReadString(), // Default Genre
                                                                 br.ReadString(), // Reference URL
                                                                 DateTime.FromBinary(br.ReadInt64()));

                int numEpisodes = br.ReadInt32();
                List<PodcastEpisode> episodes = new List<PodcastEpisode>();
                for (int j = 0; j < numEpisodes; j++)
                {
                    int id;
                    
                    PodcastEpisode pe = new PodcastEpisode(br.ReadString(),                       // Title
                                                           br.ReadString(),                       // GUID
                                                           br.ReadString(),                       // Description
                                                           br.ReadString(),                       // URL
                                                           DateTime.FromBinary(br.ReadInt64()),   // Date
                                                           br.ReadInt32(),                        // Duration
                                                           DateTime.FromBinary(br.ReadInt64()),   // Download date
                                                           (PodcastDownloadStatus)br.ReadInt32(), // Download status
                                                           
                                                           ((((id = br.ReadInt32()) >= 0) && (id < Library.Count)) ? Library[id] : null), // Track
                                                           
                                                           ps);
                    episodes.Add(pe);
                }
                ps.Episodes = episodes;
                subscriptions.Add(ps);
            }
            PodcastManager.Subscriptions = subscriptions;
        }
        private static void savePodcasts(BinaryWriter bw)
        {
            List<PodcastSubscription> subscriptions = PodcastManager.Subscriptions;

            bw.Write(subscriptions.Count);
            foreach (PodcastSubscription ps in subscriptions)
            {
                bw.Write(ps.Name);
                bw.Write(ps.URL);
                bw.Write(ps.DefaultGenre);
                bw.Write(ps.ReferenceURL);
                bw.Write(ps.LastDownloadDate.Ticks);
                List<PodcastEpisode> episodes = ps.Episodes;
                bw.Write(episodes.Count);
                foreach (PodcastEpisode pe in episodes)
                {
                    System.Diagnostics.Debug.Assert(pe.Track != null || (pe.DownloadStatus != PodcastDownloadStatus.Played && pe.DownloadStatus != PodcastDownloadStatus.Unplayed));
                    if (pe.Track != null)
                    {
                        System.Diagnostics.Debug.Assert(pe.Track.Deleted || (pe.Track.ID >= 0 && pe.Track.ID < library.Count));
                    }
                    
                    switch (pe.DownloadStatus)
                    {
                        case PodcastDownloadStatus.Error:
                        case PodcastDownloadStatus.DownloadCanceled:
                        case PodcastDownloadStatus.DownloadInProgress:
                        case PodcastDownloadStatus.QueuedForDownload:
                        pe.SetDownloadStatus(PodcastDownloadStatus.NotDownloaded);
                            break;
                    }

                    bw.Write(pe.Title);
                    bw.Write(pe.GUID);
                    bw.Write(pe.Description);
                    bw.Write(pe.URL);
                    bw.Write(pe.Date.Ticks);
                    bw.Write(pe.Duration);
                    bw.Write(pe.DownloadDate.Ticks);
                    
                    bw.Write((int)pe.DownloadStatus);

                    if (pe.Track == null)
                        bw.Write(Int32.MinValue);
                    else
                        bw.Write(pe.Track.ID);
                }
            }
        }
        private static void saveRadioStations(BinaryWriter bw)
        {
            List<RadioStation> stations = Radio.RadioStations;

            bw.Write(stations.Count);

            foreach (RadioStation rs in Radio.RadioStations)
            {
                bw.Write(rs.Name);
                bw.Write(rs.URL);
                bw.Write(rs.Genre);
                bw.Write(rs.BitRate);
                bw.Write((int)rs.StreamType);
            }
        }
        private static void saveAutoUpdateFolders(BinaryWriter bw)
        {
            bw.Write(crawlDirs.Count);
            foreach (string s in crawlDirs)
                bw.Write(s);

        }

        private static void saveEqualizers(BinaryWriter bw, List<EqualizerSetting> eq)
        {
            bw.Write(eq.Count);
            foreach (EqualizerSetting es in eq)
            {
                System.Diagnostics.Debug.Assert(!es.IsOff);
                bw.Write(es.Name);
                bw.Write(es.Locked);
                System.Diagnostics.Debug.WriteLine(es.Name);
                for (int i = 0; i < Equalizer.MAX_NUM_BANDS; i++)
                {
                    bw.Write(es.Values[i]);
                }
            }
        }

        private static void saveSettings(BinaryWriter bw)
        {
            bw.Write(settings.Count);

            foreach (KeyValuePair<SettingType, string> kvp in settings)
            {
                bw.Write((int)kvp.Key);
                bw.Write(kvp.Value);
            }
        }

        private static void savePlaylists(BinaryWriter bw)
        {
            List<Playlist> pl;
            
            if (Setting.SaveNowPlayingOnExit)
                pl = playlists.FindAll(p => p.PlaylistType == PlaylistType.Standard || p.PlaylistType == PlaylistType.NowPlaying);
            else
                pl = playlists.FindAll(p => p.PlaylistType == PlaylistType.Standard);

            bw.Write(pl.Count);

            for (int i = 0; i < pl.Count; i++)
            {
                Playlist p = pl[i];

                bw.Write(p.Name);
                bw.Write(p.Expression);
                bw.Write(p.Items.Count);
                for (int j = 0; j < p.Items.Count; j++)
                    bw.Write(p.Items[j].Track.ID);
            }

            pl = playlists.FindAll(p => p.PlaylistType == PlaylistType.Auto);

            bw.Write(pl.Count);

            for (int i = 0; i < pl.Count; i++)
            {
                Playlist p = pl[i];
                bw.Write(p.Name);
                bw.Write(p.Expression);
            }
        }
        public static void CreateNewDatabase(bool NewEqualizers)
        {
            library.Clear();
            playlists.Clear();
            playlistDictionary.Clear();
            Radio.RadioStations.Clear();
            TrackWriter.Clear();
            Setting.KeepOrganized = false;
            Setting.MoveNewFilesIntoMain = false;

            Radio.RadioStations = RadioStation.DefaultList;

            Playlist p;

            p = new Playlist(Localization.NOW_PLAYING, PlaylistType.NowPlaying);
            playlists.Add(p);
            playlistDictionary.Add(Localization.NOW_PLAYING, p);
            nowPlayingPlaylist = p;

            p = new Playlist(Localization.DUPLICATES, PlaylistType.Duplicates);
            playlists.Add(p);
            playlistDictionary.Add(Localization.DUPLICATES, p);

            p = new Playlist(Localization.GHOSTS, PlaylistType.Ghosts);
            playlists.Add(p);
            playlistDictionary.Add(Localization.GHOSTS, p);

            p = new Playlist(Localization.MY_PLAYLIST, PlaylistType.Standard);
            p.PlaylistType = PlaylistType.Standard;
            playlists.Add(p);
            playlistDictionary.Add(Localization.MY_PLAYLIST, p);

            p = new Playlist("Recently Played", "DaysSinceLastPlayed < 7" + Environment.NewLine + "\tSortBy DaysSinceLastPlayed Ascending", PlaylistType.Auto);
            playlists.Add(p);
            playlistDictionary.Add(p.Name, p);

            p = new Playlist("Recently Added to Library", "DaysSinceFileAdded < 7" + Environment.NewLine + "\tSortBy DaysSinceFileAdded Ascending", PlaylistType.Auto);
            playlists.Add(p);
            playlistDictionary.Add(p.Name, p);

            p = new Playlist("80's Rock", "Genre = \"Rock\" and (Year >= 1980 and Year <= 1989)" + Environment.NewLine + "\tSortBy Artist", PlaylistType.Auto);
            playlists.Add(p);
            playlistDictionary.Add(p.Name, p);

            p = new Playlist("Never Played", "PlayCount = 0", PlaylistType.Auto);
            playlists.Add(p);
            playlistDictionary.Add(p.Name, p);

            p = new Playlist("10GB for iPod", "ByAlbum LimitTo 10 Gigabytes" +
                                              Environment.NewLine +
                                              "\tSelectBy Rating" +
                                              Environment.NewLine +
                                              "\t\tThenBy Random" +
                                              Environment.NewLine +
                                              "\tSortBy Artist" +
                                              Environment.NewLine +
                                              "\t\tThenBy Album" +
                                              Environment.NewLine +
                                              "\t\tThenBy TrackNum", PlaylistType.Auto);
            playlists.Add(p);
            playlistDictionary.Add(p.Name, p);

            p = new Playlist("Highly Rated", "Rating >= 4" + Environment.NewLine + "\tSortBy Rating Descending", PlaylistType.Auto);
            playlists.Add(p);
            playlistDictionary.Add(p.Name, p);

            p = new Playlist("100 Good Songs", "LimitTo 100 Tracks" + Environment.NewLine + "\tSelectBy Rating Descending" + Environment.NewLine + "\tSortBy Random", PlaylistType.Auto);
            playlists.Add(p);
            playlistDictionary.Add(p.Name, p);

            p = new Playlist("Most Played", "PlayCount > 0" + Environment.NewLine + "\tSortBy PlayCount Descending", PlaylistType.Auto);
            playlists.Add(p);
            playlistDictionary.Add(p.Name, p);

            p = new Playlist("Oldies", "(Year > 1900 And Year < 1960) Or Genre Is Oldies", PlaylistType.Auto);
            playlists.Add(p);
            playlistDictionary.Add(p.Name, p);

            if (NewEqualizers)
            {
                Equalizer.GetInstance().EqualizerSettings = Equalizer.DefaultEqualizerSettings;
            }
            isLargeLibrary = null;
            IncrementDatabaseVersion(true);
        }

        public static List<string> GetArtists()
        {
            lock (LibraryLock)
            {
                return GetArtists(library);
            }
        }
        public static List<string> GetArtists(List<Track> Tracks)
        {
            return (from t in Tracks
                    group t by t.Artist
                        into g
                        where g.Key.Length > 0
                        orderby g.Key
                        select g.Key).ToList();
        }
        public static List<string> GetAlbumArtists()
        {
            lock (LibraryLock)
            {
                return GetAlbumArtists(library);
            }
        }
        public static List<string> GetAlbumArtists(List<Track> Tracks)
        {
            return (from t in Tracks
                    group t by t.AlbumArtist
                        into g
                        where g.Key.Length > 0
                        orderby g.Key
                        select g.Key).ToList();
        }
        public static List<string> GetAlbums()
        {
            lock (LibraryLock)
            {
                return GetAlbums(library);
            }
        }
        public static List<string> GetAlbums(List<Track> Tracks)
        {
            var aa = from t in Tracks
                     group t by t.Album
                         into g
                         where g.Key.Length > 0
                         select g.Key;

            List<string> albums = aa.ToList();
            albums.Sort((a, b) => Track.NoTheComparer(a, b));
            return albums;
        }
        public static List<string> GetMainGroups(List<Track> Tracks)
        {
            var aa = from t in Tracks
                     group t by t.MainGroup
                         into g
                         where g.Key.Length > 0
                         select g.Key;

            List<string> artists = aa.ToList();
            artists.Sort((a, b) => Track.NoTheComparer(a, b));
            return artists;
        }
        public static List<string> GetGenres()
        {
            lock (LibraryLock)
            {
                return GetGenres(library);
            }
        }
        public static List<string> GetGenres(List<Track> Tracks)
        {
            return (from t in Tracks
                     group t by t.Genre
                         into g
                         where g.Key.Length > 0
                         orderby g.Key
                         select g.Key).ToList();
        }
        public static List<string> GetYears()
        {
            lock (LibraryLock)
            {
                return GetYears(library);
            }
        }
        public static List<string> GetYears(List<Track> Tracks)
        {
            return (from t in Tracks
                          group t by t.YearString
                              into g
                              where g.Key.Length > 0
                              orderby g.Key
                              select g.Key).ToList();
        }
        public static List<string> GetGroupings()
        {
            lock (LibraryLock)
            {
                return GetGroupings(library);
            }
        }
        public static List<string> GetGroupings(List<Track> Tracks)
        {
            return (from t in Tracks
                    group t by t.Grouping
                        into g
                        where g.Key.Length > 0
                        orderby g.Key
                        select g.Key).ToList();
        }
        public static List<string> GetEncoders()
        {
            lock (LibraryLock)
            {
                return (from t in library
                        group t by t.Encoder
                            into g
                            where g.Key.Length > 0
                            orderby g.Key
                            select g.Key).ToList();
            }
        }
        public static List<string> GetTitles()
        {
            lock (LibraryLock)
            {
                return (from t in library
                        group t by t.Title
                            into g
                            where g.Key.Length > 0
                            orderby g.Key
                            select g.Key).ToList();
            }
        }
        public static List<string> GetComposers()
        {
            lock (LibraryLock)
            {
                return GetComposers(library);
            }
        }
        public static List<string> GetComposers(List<Track> Tracks)
        {
            return (from t in Tracks
                    group t by t.Composer
                        into g
                        where g.Key.Length > 0
                        orderby g.Key
                        select g.Key).ToList();
        }
        public static List<string> GetFileTypes()
        {
            lock (LibraryLock)
            {
                return (from t in library
                        group t by t.TypeString
                            into g
                            where g.Key.Length > 0
                            orderby g.Key
                            select g.Key).ToList();
            }
        }

        public static List<Track> Library
        {
            get { return library; }
        }
        public static List<Track> LibrarySnapshot
        {
            get
            {
                lock (LibraryLock)
                {
                    return library.ToList();
                }
            }
        }

        public static Track GetTrackWithFilePath(string FilePath)
        {
            lock (LibraryLock)
            {
                return library.FirstOrDefault(t => String.Compare(t.FilePath, FilePath, StringComparison.OrdinalIgnoreCase) == 0);
            }
        }
        public static bool TrackIsInLibrary(Track Track)
        {
            lock (LibraryLock)
            {
                return library.Contains(Track);
            }
        }
        public static bool HasTracks
        {
            get { return library.Count > 0; }
        }
        public static Track GetMatchingTrack(Func<Track, bool> Predicate)
        {
            lock (LibraryLock)
            {
                return library.FirstOrDefault(Predicate);
            }
        }
        public static bool TrackExists(Predicate<Track> Predicate)
        {
            lock (LibraryLock)
            {
                return library.Exists(Predicate);
            }
        }
        public static List<Track> Validate(List<Track> Tracks)
        {
            lock (LibraryLock)
            {
                return Tracks.FindAll(t => library.Contains(t));
            }
        }
        public static List<Track> FindAllTracks(Predicate<Track> Predicate)
        {
            lock (LibraryLock)
            {
                return library.FindAll(Predicate);
            }
        }
        public static List<String> GetPlaylists()
        {
            var p = from pl in playlists
                    orderby pl.SortName
                    select pl.Name;

            return p.ToList();

        }
        public static bool PlaylistExists(string PlaylistName)
        {
            return playlistDictionary.ContainsKey(PlaylistName);
        }
        public static List<string> GetPlaylists(List<Track> Tracks)
        {
            if (Tracks.Count == library.Count || Database.IsLargeLibrary)
                return GetPlaylists();

            List<string> pls = new List<string>();

            foreach (Playlist p in playlists)
            {
                if (p.Items.Exists(pi => Tracks.Contains(pi.Track)))
                    pls.Add(p.Name);
            }
            return pls;
        }
        public static Track GetFirstTrackFromPlaylist(string PlaylistName)
        {
            if (playlistDictionary.ContainsKey(PlaylistName))
            {
                Playlist p = playlistDictionary[PlaylistName];

                if (p.Items.Count > 0)
                    return p.Items[0].Track;
                else
                    return null;
            }
            else
            {
                return null;
            }
        }

        public static List<Track> GetPlaylistTracks(char StartChar)
        {
            IEnumerable<Track> tt = new List<Track>();

            foreach (Playlist pl in playlists)
                if (Char.ToUpperInvariant(pl.Name[0]) == StartChar)
                    tt = tt.Union(GetPlaylistTracks(pl.Name));

            return tt.ToList();
        }
        public static TrackQueue GetPlaylistTracks(string PlaylistName)
        {
            if (playlistDictionary.ContainsKey(PlaylistName))
            {
                Playlist p = playlistDictionary[PlaylistName];

                var g = from y in p.Items
                        select y.Track;

                TrackQueue tq = g.ToList();
                tq.PreSorted = p.PreSorted;
                return tq;
            }
            else
            {
                return TrackQueue.Empty;
            }
        }
        public static bool PlaylistIsPreSorted(string PlaylistName)
        {
            return playlistDictionary[PlaylistName].PreSorted;
        }
        public static bool AddPlaylist(string PlaylistName)
        {
            if (!playlistDictionary.ContainsKey(PlaylistName))
            {
                Playlist p = new Playlist(PlaylistName, PlaylistType.Standard);
                playlists.Add(p);
                playlistDictionary.Add(PlaylistName, p);
                playlists.Sort((a, b) => a.SortName.CompareTo(b.SortName));
                isLargeLibrary = null;
                IncrementDatabaseVersion(false);
                return true;
            }
            else
            {
                return false;
            }
        }
        public static void SaveStandardPlaylist(TrackQueue Queue)
        {
            if (playlistDictionary.ContainsKey(Queue.PlaylistBasis))
            {
                Playlist p = playlistDictionary[Queue.PlaylistBasis];

                // Queue may be filtered: need to retain ordering in Queue while not losing anything in p.Items
                // p.items must retain ordering also

                if (p.Items.Count <= Queue.Count)
                {
                    p.Items.Clear();
                    foreach (Track t in Queue)
                        p.Items.Add(new PlaylistItem(p, t));
                }
                else
                {
                    for (int i = 0; i < p.Items.Count; i++)
                        p.Items[i].Track.Index = i;

                    int qc = Queue.Count;

                    List<int> indexes = new List<int>();

                    for (int i = 0; i < qc; i++)
                    {
                        indexes.Add(Queue[i].Index);
                    }

                    indexes.Sort();

                    for (int i = 0; i < qc; i++)
                    {
                        p.Items[indexes[i]] = new PlaylistItem(p, Queue[i]);
                    }
                }
                IncrementDatabaseVersion(false);
            }
        }
        public static bool ChangePlaylistName(string OldName, string NewName)
        {
            if (PlaylistExists(OldName) &&
               (OldName != NewName) &&
               ((!PlaylistExists(NewName)) || (OldName.ToLowerInvariant() == NewName.ToLowerInvariant()))
               && (NewName.Length > 0))
            {
                Playlist p = playlistDictionary[OldName];
                p.Name = NewName;
                playlists.Sort((a, b) => a.SortName.CompareTo(b.SortName));
                playlistDictionary.Remove(OldName);
                playlistDictionary.Add(NewName, p);
                IncrementDatabaseVersion(false);
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool DeletePlaylist(string PlaylistName)
        {
            if (PlaylistExists(PlaylistName) && (PlaylistName != Localization.NOW_PLAYING))
            {
                Playlist p = playlistDictionary[PlaylistName];
                playlists.Remove(p);
                playlistDictionary.Remove(PlaylistName);
                IncrementDatabaseVersion(false);
                return true;
            }
            else
            {
                return false;
            }
        }
        public static void InsertInNowPlaying(Track Track, int Index)
        {
            Playlist p = playlistDictionary[Localization.NOW_PLAYING];

            p.Items.RemoveAll(pi => pi.Track == Track);
            p.Items.Insert(Math.Min(Index, p.Items.Count), new PlaylistItem(p, Track));
            IncrementDatabaseVersion(false);
        }
        public static void InsertAtBeginningOfNowPlaying(List<Track> Tracks)
        {
            List<PlaylistItem> pi = new List<PlaylistItem>();

            foreach (Track t in Tracks)
            {
                pi.Add(new PlaylistItem(nowPlayingPlaylist, t));
            }

            nowPlayingPlaylist.Items.RemoveAll(playitem => Tracks.Contains(playitem.Track));
            nowPlayingPlaylist.Items.InsertRange(0, pi);

            IncrementDatabaseVersion(false);
        }
        public static void AddToPlaylist(string PlaylistName, List<Track> Queue)
        {
            if (Queue != null)
            {
                Playlist p = playlistDictionary[PlaylistName];

                foreach (Track t in Queue)
                {
                    if (!p.Items.Exists(pi => pi.Track.Equals(t)))
                    {
                        PlaylistItem pi = new PlaylistItem(p, t);
                        p.Items.Add(pi);
                    }
                }
                IncrementDatabaseVersion(false);
            }
        }
        public static void AddToPlaylist(string PlaylistName, Track Track)
        {
            if (Track != null)
            {
                Playlist p = playlistDictionary[PlaylistName];

                if (!p.Items.Exists(pi => pi.Track.Equals(Track)))
                {
                    PlaylistItem pi = new PlaylistItem(p, Track);
                    p.Items.Add(pi);
                }
                IncrementDatabaseVersion(false);
            }
        }
        public static void ClearPlaylist(string PlaylistName, Track ExcludeTrack)
        {
            if (playlistDictionary.ContainsKey(PlaylistName))
            {
                Playlist p = playlistDictionary[PlaylistName];
                if (ExcludeTrack == null)
                {
                    p.Items.Clear();
                }
                else
                {
                    p.Items.RemoveAll(pi => pi.Track != ExcludeTrack);
                }
            }
            IncrementDatabaseVersion(false);
        }
        public static bool IsPlaylistDynamic(string PlaylistName)
        {
            Playlist pl;

            bool success = playlistDictionary.TryGetValue(PlaylistName, out pl);

            if (success)
                return pl.IsDynamic;
            else
                return false;
        }
        public static PlaylistType GetPlaylistType(string PlaylistName)
        {
            Playlist pl;

            bool success = playlistDictionary.TryGetValue(PlaylistName, out pl);

            if (success)
                return pl.PlaylistType;
            else
                return PlaylistType.None;
        }
        public static void ConvertPlaylistToStandard(string PlaylistName)
        {
            playlistDictionary[PlaylistName].Update();
            playlistDictionary[PlaylistName].PlaylistType = PlaylistType.Standard;
            IncrementDatabaseVersion(false);
        }
        public static void SetPlaylistExpression(string PlaylistName, string Expression, bool MakeAuto)
        {
            Playlist p = playlistDictionary[PlaylistName];
            p.Expression = Expression;

            if (MakeAuto && p.Expression.Length > 0)
                p.PlaylistType = PlaylistType.Auto;
            else
                p.PlaylistType = PlaylistType.Standard;
        }
        public static string GetPlaylistExpression(string PlaylistName)
        {
            return playlistDictionary[PlaylistName/*.ToLowerInvariant()*/].Expression;
        }
        public static void RemoveFirstTrackFromPlaylist(string PlaylistName)
        {
            Playlist p = playlistDictionary[PlaylistName];
            if (p.Items.Count > 0)
                p.Items.RemoveAt(0);
            IncrementDatabaseVersion(false);
        }
        public static void RemoveFromPlaylist(string PlaylistName, TrackQueue Tracks)
        {
            if (PlaylistExists(PlaylistName))
            {
                playlistDictionary[PlaylistName].Items.RemoveAll(pi => Tracks.Contains(pi.Track));
                IncrementDatabaseVersion(false);
            }
        }
        public static void RemoveFromNowPlaying(Track Track)
        {
            nowPlayingPlaylist.Items.RemoveAll(pi => pi.Track == Track);
        }
        public static void RemoveFromLibrary(List<Track> Tracks)
        {
            foreach (Track t in Tracks)
                t.Deleted = true;

            lock (LibraryLock)
            {
                library.RemoveAll(t => t.Deleted);
            }

            foreach (Playlist p in playlists)
                if (!p.IsDynamic)
                    p.Items.RemoveAll(pi => pi.Track.Deleted);

            isLargeLibrary = null;
            IncrementDatabaseVersion(true);
        }
        
        public enum AddLibraryResult { None, OK, ShortDurationLimit, Duplicate, UpdateOnly }

        public static AddLibraryResult AddToLibrary(Track Track, bool AllowDuplicates, bool RespectLibrarySizeLimit)
        {
            if (Track.Duration < Setting.ShortTrackCutoff * 1000)
                return AddLibraryResult.ShortDurationLimit;

            if (!AllowDuplicates && Database.TrackExists(t => t.IsStrongMatch(Track)))
                return AddLibraryResult.Duplicate;

            IncrementDatabaseVersion(true);

            Track existingTrack = Database.GetTrackWithFilePath(Track.FilePath);

            if (existingTrack != null)
            {
                existingTrack.Update(Track);
                return AddLibraryResult.UpdateOnly;
            }
            lock (LibraryLock)
            {
                library.Add(Track);
            }

            resetTrackIDs();

            return AddLibraryResult.OK;
        }

        public static void AddToLibrary(List<Track> Tracks, int ShortTrackCutoffMsec, bool AllowDuplicates)
        {
            IEqualityComparer<Track> ct = new CompareByFilePath();
            List<Track> newTracks = new List<Track>();

            foreach (Track t in Tracks)
            {
                if (t.Duration > ShortTrackCutoffMsec)
                {
                    if (AllowDuplicates || !Database.TrackExists(tt => tt.IsStrongMatch(t)))
                    {
                        Track tt = Database.GetTrackWithFilePath(t.FilePath);

                        if (tt == null)
                            newTracks.Add(t);
                        else
                            tt.Update(t);
                    }
                }
            }

            newTracks = newTracks.Distinct(ct).ToList();

            lock (LibraryLock)
            {
                library.AddRange(newTracks);
                System.Diagnostics.Debug.Assert(library.Count == (library.Distinct(ct).Count()));
            }

            resetTrackIDs();

            IncrementDatabaseVersion(true);
        }

        public static string GetSetting(SettingType Setting, string Default)
        {
            if (settings.ContainsKey(Setting))
                return settings[Setting];
            else
                return Default;
        }
        public static float GetSetting(SettingType Setting, float Default)
        {
            if (settings.ContainsKey(Setting))
                return float.Parse(settings[Setting]);
            else
                return Default;
        }
        public static bool GetSetting(SettingType Setting, bool Default)
        {
            if (settings.ContainsKey(Setting))
                return bool.Parse(settings[Setting]);
            else
                return Default;
        }
        public static int GetSetting(SettingType Setting, int Default)
        {
            if (settings.ContainsKey(Setting))
                return (int)float.Parse(settings[Setting]);
            else
                return Default;
        }
        public static void SaveSetting(SettingType Setting, string Value)
        {
            if (settings.ContainsKey(Setting))
                settings[Setting] = Value;
            else
                settings.Add(Setting, Value);
        }
        public static void SaveSetting(SettingType Setting, float Value)
        {
            SaveSetting(Setting, Value.ToString());
        }
        public static void SaveSetting(SettingType Setting, bool Value)
        {
            SaveSetting(Setting, Value.ToString());
        }
        public static void SaveSetting(SettingType Setting, int Value)
        {
            SaveSetting(Setting, Value.ToString());
        }

        public static uint Version
        { get; private set; }

        public static uint LatestLibraryAddOrRemove
        {
            get;
            private set;
        }

        private static void open(string FilePath)
        {
            BinaryReader br = null;
            
            try
            {
                br = new BinaryReader(File.OpenRead(FilePath));

                int schemaVersion = br.ReadInt32();

                bool savedAsPro = false;

                if (schemaVersion > 3)
                {
                    savedAsPro = br.ReadBoolean();
                }

                if (schemaVersion < 3)
                {
                    loadSchemaVersion2OrLess(br, schemaVersion);
                }
                else if (schemaVersion < 6)
                {
                    loadSchemaVersion5OrLess(br, schemaVersion);
                }
                else
                {
                    load(br, schemaVersion);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());

                if (FilePath == getDatabaseFilePath())
                {
                    library.Clear();
                    playlists.Clear();
                    playlistDictionary.Clear();
                    open(getBackupFilePath());
                }
            }
            finally
            {
                if (br != null)
                    br.Close();
            }
        }
        private static void load(BinaryReader br, int SchemaVersion)
        {
            List<EqualizerSetting> eqs = loadEqualizers(br);

            if (SchemaVersion <= 9)
                loadTracksSchemaVersion9OrLess(br, eqs);
            else
                loadTracks(br, eqs);

            loadPlaylists(br);

            loadSettings(br);

            loadAutoUpdateFolders(br);

            if (SchemaVersion >= 5)
                loadLastFMData(br);

            if (SchemaVersion >= 7)
                loadRadioStations(br);
            else
                Radio.RadioStations = RadioStation.DefaultList;

            if (SchemaVersion >= 9)
                loadPodcasts(br);

#if PERF
            Track[] array = new Track[this.Library.Count];
            this.Library.CopyTo(array);

            for (int i = 0; i < Math.Max(0, (100000 - array.Length) / array.Length); i++)
            {
                foreach (Track ttt in array)
                {
                    Track t = new Track(ttt);
                    t.updateAlbumAndArtist(i.ToString() + " - " + t.Album, t.Artist + i.ToString());
                    library.Add(t);
                }
            }

#endif
        }
        private static void loadSchemaVersion5OrLess(BinaryReader br, int SchemaVersion)
        {
            loadTracksSchemaVersion5OrLess(br);

            loadPlaylists(br);

            loadSettings(br);

            loadEqualizers(br);

            loadAutoUpdateFolders(br);

            if (SchemaVersion >= 5)
                loadLastFMData(br);
        }

        private static void loadAutoUpdateFolders(BinaryReader br)
        {
            crawlDirs = new List<string>();

            int crawlDirCount = br.ReadInt32();
            for (int i = 0; i < crawlDirCount; i++)
            {
                string s = br.ReadString();
                crawlDirs.Add(s);
            }
        }

        private static void loadSettings(BinaryReader br)
        {
            int settingsCount = br.ReadInt32();
            for (int i = 0; i < settingsCount; i++)
            {
                settings.Add((SettingType)br.ReadInt32(), br.ReadString());
            }
        }

        private static void loadTracksSchemaVersion9OrLess(BinaryReader br, List<EqualizerSetting> equalizers)
        {
            int trackCount = br.ReadInt32();

            Dictionary<int, EqualizerSetting> eqs = new Dictionary<int, EqualizerSetting>();

            for (int i = 0; i < equalizers.Count; i++)
                eqs.Add(i, equalizers[i]);

            eqs.Add(NO_EQUALIZER, null);
            eqs.Add(EQUALIZER_OFF, EqualizerSetting.Off);

            for (int i = 0; i < trackCount; i++)
            {
                Track t = new Track(br.ReadInt32(),   // ID
                                     br.ReadString(), // FilePath
                                     (Track.FileType)br.ReadInt32(), // Type
                                     br.ReadString(), // Title
                                     br.ReadString(), // Album
                                     br.ReadString(), // Artist
                                     br.ReadString(), // AlbumArtist
                                     br.ReadString(), // Composer
                                     br.ReadString(), // Grouping
                                     br.ReadString(), // Genre
                                     br.ReadInt32(),  // Length
                                     br.ReadInt32(),  // TrackNum
                                     br.ReadInt32(),  // DiskNum
                                     br.ReadInt32(),  // Year
                                     br.ReadInt32(),  // Playcount
                                     br.ReadInt32(),  // Rating
                                     br.ReadInt32(),  // Bitrate
                                     br.ReadInt64(),  // File Size
                                     br.ReadBoolean(), // Compilation
                                     DateTime.FromBinary(br.ReadInt64()),  // file date
                                     DateTime.FromBinary(br.ReadInt64()),  // last played date
                                     DateTime.FromBinary(br.ReadInt64()),  // date added
                                     br.ReadString(),  // Encoder
                                     br.ReadInt32(),   // Num Channels
                                     br.ReadInt32(),   // Sample Rate
                                     (br.ReadSingle() == 0 ? (ChangeType)br.ReadInt32() : (ChangeType)br.ReadInt32()), // Dirty, also kill old replay gain thing
                                     eqs[br.ReadInt32()],   // Equalizer
                                     float.MinValue, // Replay Gain Album
                                     float.MinValue // Replay Gain Track
                                    );

                library.Add(t);
            }
        }
        private static void loadTracks(BinaryReader br, List<EqualizerSetting> equalizers)
        {
            int trackCount = br.ReadInt32();

            Dictionary<int, EqualizerSetting> eqs = new Dictionary<int, EqualizerSetting>();

            for (int i = 0; i < equalizers.Count; i++)
                eqs.Add(i, equalizers[i]);

            eqs.Add(NO_EQUALIZER, null);
            eqs.Add(EQUALIZER_OFF, EqualizerSetting.Off);

            for (int i = 0; i < trackCount; i++)
            {
                Track t = new Track(br.ReadInt32(),   // ID
                                     br.ReadString(), // FilePath
                                     (Track.FileType)br.ReadInt32(), // Type
                                     br.ReadString(), // Title
                                     br.ReadString(), // Album
                                     br.ReadString(), // Artist
                                     br.ReadString(), // AlbumArtist
                                     br.ReadString(), // Composer
                                     br.ReadString(), // Grouping
                                     br.ReadString(), // Genre
                                     br.ReadInt32(),  // Length
                                     br.ReadInt32(),  // TrackNum
                                     br.ReadInt32(),  // DiskNum
                                     br.ReadInt32(),  // Year
                                     br.ReadInt32(),  // Playcount
                                     br.ReadInt32(),  // Rating
                                     br.ReadInt32(),  // Bitrate
                                     br.ReadInt64(),  // File Size
                                     br.ReadBoolean(), // Compilation
                                     DateTime.FromBinary(br.ReadInt64()),  // file date
                                     DateTime.FromBinary(br.ReadInt64()),  // last played date
                                     DateTime.FromBinary(br.ReadInt64()),  // date added
                                     br.ReadString(),  // Encoder
                                     br.ReadInt32(),   // Num Channels
                                     br.ReadInt32(),   // Sample Rate
                                     (ChangeType)br.ReadInt32(), // Dirty
                                     eqs[br.ReadInt32()],   // Equalizer
                                     br.ReadSingle(), // Replay Gain Album
                                     br.ReadSingle() // Replay Gain Track
                                    );

                library.Add(t);
            }
        }
        private static void loadTracksSchemaVersion5OrLess(BinaryReader br)
        {
            int trackCount = br.ReadInt32();

            for (int i = 0; i < trackCount; i++)
            {
                Track t = new Track(br.ReadInt32(),   // ID
                                     br.ReadString(), // FilePath
                                     (Track.FileType)br.ReadInt32(), // Type
                                     br.ReadString(), // Title
                                     br.ReadString(), // Album
                                     br.ReadString(), // Artist
                                     br.ReadString(), // AlbumArtist
                                     br.ReadString(), // Composer
                                     br.ReadString(), // Grouping
                                     br.ReadString(), // Genre
                                     br.ReadInt32(),  // Length
                                     br.ReadInt32(),  // TrackNum
                                     br.ReadInt32(),  // DiskNum
                                     br.ReadInt32(),  // Year
                                     br.ReadInt32(),  // Playcount
                                     br.ReadInt32(),  // Rating
                                     br.ReadInt32(),  // Bitrate
                                     br.ReadInt64(),  // File Size
                                     br.ReadBoolean(), // Compilation
                                     DateTime.FromBinary(br.ReadInt64()),  // file date
                                     DateTime.FromBinary(br.ReadInt64()),  // last played date
                                     DateTime.FromBinary(br.ReadInt64()),  // date added
                                     br.ReadString(),  // Encoder
                                     br.ReadInt32(),   // Num Channels
                                     br.ReadInt32(),   // Sample Rate
                                     ChangeType.None, // ChangeType
                                     null, // Eq
                                     float.MinValue, // replay gain album
                                     float.MinValue // replay gain track
                                    );
                br.ReadInt32(); // ignore junk

                library.Add(t);
            }
        }
        private static void loadPlaylists(BinaryReader br)
        {
            int playlistCount = br.ReadInt32();
            int playlistItemCount;

            Playlist p;

            for (int i = 0; i < playlistCount; i++)
            {
                p = new Playlist(br.ReadString(), PlaylistType.Standard);
                p.Expression = br.ReadString();
                playlistItemCount = br.ReadInt32();
                for (int j = 0; j < playlistItemCount; j++)
                {
                    int index = br.ReadInt32();
                    if (index >= 0)
                        p.Items.Add(new PlaylistItem(p, library[index]));
                }
                playlists.Add(p);
                playlistDictionary.Add(p.Name, p);
            }

            playlistCount = br.ReadInt32();
            for (int i = 0; i < playlistCount; i++)
            {
                p = new Playlist(br.ReadString(), br.ReadString(), PlaylistType.Auto);
                playlists.Add(p);
                playlistDictionary.Add(p.Name, p);
            }

            if (!PlaylistExists(Localization.NOW_PLAYING))
            {
                p = new Playlist(Localization.NOW_PLAYING, PlaylistType.NowPlaying);
                playlists.Add(p);
                playlistDictionary.Add(Localization.NOW_PLAYING, p);
            }
            nowPlayingPlaylist = playlistDictionary[Localization.NOW_PLAYING];
            nowPlayingPlaylist.PlaylistType = PlaylistType.NowPlaying;

            System.Diagnostics.Debug.Assert(!PlaylistExists(Localization.DUPLICATES));
            p = new Playlist(Localization.DUPLICATES, PlaylistType.Duplicates);
            playlists.Add(p);
            playlistDictionary.Add(Localization.DUPLICATES, p);

            System.Diagnostics.Debug.Assert(!PlaylistExists(Localization.GHOSTS));
            p = new Playlist(Localization.GHOSTS, PlaylistType.Ghosts);
            playlists.Add(p);
            playlistDictionary.Add(Localization.GHOSTS, p);

            playlists.Sort((a, b) => a.SortName.CompareTo(b.SortName));
        }

        private static void loadRadioStations(BinaryReader br)
        {
            int numStations = br.ReadInt32();
            if (numStations > 0)
            {
                List<RadioStation> stations = new List<RadioStation>();
                for (int i = 0; i < numStations; i++)
                {
                    stations.Add(new RadioStation(br.ReadString(), // name
                                                  br.ReadString(), // url
                                                  br.ReadString(), // genre
                                                  br.ReadInt32(),  // bitrate
                                                  (StationStreamType)br.ReadInt32())); // stream type
                }

                Radio.RadioStations = stations;
            }
            else
            {
                Radio.RadioStations = RadioStation.DefaultList;
            }
        }
        private static void loadLastFMData(BinaryReader br)
        {
            int lastFMcount = br.ReadInt32();

            if (lastFMcount > 0)
            {
                List<Track> lt = new List<Track>();
                for (int i = 0; i < lastFMcount; i++)
                {
                    int id = br.ReadInt32();

                    // make sure there isn't a lock on libraryLock here

                    Track t = Database.GetMatchingTrack(tt => tt.ID == id);
                    if (t != null)
                        lt.Add(t);
                }
                LastFM.Backlog = lt;
            }
        }

        private static List<EqualizerSetting> loadEqualizers(BinaryReader br)
        {
            int equalizerCount = br.ReadInt32();
            List<EqualizerSetting> eqs = new List<EqualizerSetting>();

            for (int i = 0; i < equalizerCount; i++)
            {
                string name = br.ReadString();
                bool locked = br.ReadBoolean();

                float[] eq = new float[Equalizer.MAX_NUM_BANDS];
                for (int j = 0; j < Equalizer.MAX_NUM_BANDS; j++)
                {
                    eq[j] = br.ReadSingle();
                }
                eqs.Add(new EqualizerSetting(name, eq, locked));
            }
            Equalizer.GetInstance().EqualizerSettings = eqs;
            return eqs;
        }
        
        private static void loadSchemaVersion2OrLess(BinaryReader br, int schemaVersion)
        {
            int trackCount = br.ReadInt32();

            for (int i = 0; i < trackCount; i++)
            {
                Track t = new Track(br.ReadInt32(), // ID
                                     br.ReadString(), // FilePath
                                     (Track.FileType)br.ReadInt32(), // Type
                                     br.ReadString(), // Title
                                     br.ReadString(), // Album
                                     br.ReadString(), // Artist
                                     br.ReadString(), // AlbumArtist
                                     br.ReadString(), // Composer
                                     br.ReadString(), // Grouping
                                     br.ReadString(), // Genre
                                     br.ReadInt32(),  // Length
                                     br.ReadInt32(),  // TrackNum
                                     br.ReadInt32(),  // DiskNum
                                     br.ReadInt32(),  // Year
                                     br.ReadInt32(),  // Playcount
                                     br.ReadInt32(),  // Rating
                                     br.ReadInt32(),  // Bitrate
                                     br.ReadInt64(),  // File Size
                                     br.ReadBoolean(), // Compilation
                                     DateTime.FromBinary(br.ReadInt64()),  // file date
                                     DateTime.FromBinary(br.ReadInt64()),  // last played date
                                     DateTime.FromBinary(br.ReadInt64()),  // date added
                                     br.ReadString(),  // Encoder
                                     0, // Num Channels
                                     0, // Sample Rate
                                     ChangeType.None, // ChangeType
                                     null,   // Equalizer
                                     float.MinValue, // Replay Gain Album
                                     float.MinValue // Replay Gain Track
                                    );
                library.Add(t);
            }

            Playlist p;

            int playlistCount = br.ReadInt32();
            int playlistItemCount;

            for (int i = 0; i < playlistCount; i++)
            {
                p = new Playlist(br.ReadString(), PlaylistType.Standard);
                p.Expression = br.ReadString();
                playlistItemCount = br.ReadInt32();
                for (int j = 0; j < playlistItemCount; j++)
                {
                    int index = br.ReadInt32();
                    if (index >= 0)
                        p.Items.Add(new PlaylistItem(p, library[index]));
                }
                playlists.Add(p);
                playlistDictionary.Add(p.Name, p);
            }

            playlistCount = br.ReadInt32();
            for (int i = 0; i < playlistCount; i++)
            {
                p = new Playlist(br.ReadString(), br.ReadString(), PlaylistType.Auto);
                playlists.Add(p);
                playlistDictionary.Add(p.Name, p);
            }

            if (!PlaylistExists(Localization.NOW_PLAYING))
            {
                p = new Playlist(Localization.NOW_PLAYING, PlaylistType.NowPlaying);
                playlists.Add(p);
                playlistDictionary.Add(Localization.NOW_PLAYING, p);
            }

            nowPlayingPlaylist = playlistDictionary[Localization.NOW_PLAYING];
            nowPlayingPlaylist.PlaylistType = PlaylistType.NowPlaying;

            p = new Playlist(Localization.DUPLICATES, PlaylistType.Duplicates);
            playlists.Add(p);
            playlistDictionary.Add(Localization.DUPLICATES, p);

            playlists.Sort((a, b) => a.SortName.CompareTo(b.SortName));

            int settingsCount = br.ReadInt32();

            for (int i = 0; i < settingsCount; i++)
            {
                settings.Add((SettingType)br.ReadInt32(), br.ReadString());
            }

            int equalizerCount = br.ReadInt32();

            for (int i = 0; i < equalizerCount; i++)
            {
                string name = br.ReadString();
                bool locked = br.ReadBoolean();

                float[] eq = new float[Equalizer.MAX_NUM_BANDS];
                for (int j = 0; j < Equalizer.MAX_NUM_BANDS; j++)
                {
                    eq[j] = br.ReadSingle();
                }
                Equalizer.GetInstance().EqualizerSettings.Add(new EqualizerSetting(name, eq, locked));
            }

            crawlDirs = new List<string>();

            if (schemaVersion > 1)
            {
                int crawlDirCount = br.ReadInt32();
                for (int i = 0; i < crawlDirCount; i++)
                {
                    string s = br.ReadString();
                    crawlDirs.Add(s);
                }
            }
        }
        private static void resetTrackIDs()
        {
            for (int i = 0; i < library.Count; i++)
                library[i].ID = i;

            IncrementDatabaseVersion(false);
        }
        private static string getOldDatabaseFilePath()
        {
            return Lib.ProgramPath("db.qxp");
        }
        private static string getDatabaseFilePath()
        {
            return Lib.LocalPath("db.qxp");
        }
        private static string getBackupFilePath()
        {
            return Lib.LocalPath("db_backup.qxp");
        }
    }
}
