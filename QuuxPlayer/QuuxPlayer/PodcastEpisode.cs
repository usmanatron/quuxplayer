/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Web;

namespace QuuxPlayer
{
    internal enum PodcastDownloadStatus { None, NotDownloaded, QueuedForDownload, DownloadInProgress, Unplayed, Error, Deleted, DownloadCanceled, Played };

    internal class PodcastEpisode : IListViewable
    {
        public enum Columns : int { Title, Description, Date, DownloadStatus, Duration, Download, Play, Remove, Count } // keep "Count" last

        public event QListView<IListViewable>.ListViewDataChanged DataChanged;
        public string Title { get; private set; }
        public string GUID { get; private set; }
        public string Description { get; set; }
        public string URL { get; private set; }
        public DateTime Date { get; private set; }
        public int Duration
        {
            get { return duration; }
            private set
            {
                if (duration != value)
                {
                    duration = value;
                    durationString = Lib.GetTimeString(duration);
                }
            }
        }
        private int duration = 0;
        private string durationString = String.Empty;
        public PodcastSubscription Subscription { get; private set; }
        private PodcastDownloadStatus downloadStatus = PodcastDownloadStatus.None;
        public DateTime DownloadDate { get; private set; }
        private static bool cancelAllDownloads;
        
        private static Comparison<PodcastEpisode>[] sortComparisons;
        private static Comparison<PodcastEpisode>[] revSortComparisons;

        public bool IsDeleted
        {
            get { return this.DownloadStatus == PodcastDownloadStatus.Deleted; }
        }
        private bool? fileExists = null;
        private bool FileExists
        {
            get
            {
                if (!fileExists.HasValue)
                    fileExists = this.Track != null && !this.Track.Deleted && this.Track.ConfirmExists;
                return fileExists.Value;
            }
        }
        public bool IsDownloadable
        {
            get { return !FileExists && !this.IsDeleted; }
        }
        public bool IsQueueable
        {
            get { return this.IsDownloadable && !this.IsQueued; }
        }
        public bool IsDownloading
        {
            get { return this.DownloadStatus == PodcastDownloadStatus.DownloadInProgress; }
        }
        public bool IsQueued
        {
            get { return this.DownloadStatus == PodcastDownloadStatus.QueuedForDownload; }
        }
        public bool IsQueuedOrDownloading
        {
            get
            {
                return this.DownloadStatus == PodcastDownloadStatus.QueuedForDownload ||
                       this.DownloadStatus == PodcastDownloadStatus.DownloadInProgress;
            }
        }
        public bool Playable
        {
            get { return this.FileExists && !this.IsDeleted; }
        }
        public bool IsSpecial
        {
            get { return Track != null && Track.IsPlaying; }
        }
        public void SetDownloadStatus(PodcastDownloadStatus Status)
        {
            if (this.DownloadStatus != Status)
            {
                if (Status == PodcastDownloadStatus.Played && this.Track != null && !this.Track.HasBeenPlayed)
                    this.Track.LastPlayedDate = DateTime.Now;
                else if (Status == PodcastDownloadStatus.Unplayed && this.Track != null && this.track.HasBeenPlayed)
                    this.Track.LastPlayedDate = DateTime.MinValue;
                this.DownloadStatus = Status;
                if (DataChanged != null)
                    DataChanged(this);
            }
        }
        public PodcastDownloadStatus DownloadStatus
        {
            get
            {
                if (downloadStatus == PodcastDownloadStatus.Deleted)
                {
                    //System.Diagnostics.Debug.Assert(Track == null);
                }
                else if (FileExists)
                {
                    if (Track.HasBeenPlayed)
                        downloadStatus = PodcastDownloadStatus.Played;
                    else
                        downloadStatus = PodcastDownloadStatus.Unplayed;
                }
                else if (downloadStatus == PodcastDownloadStatus.Played || downloadStatus == PodcastDownloadStatus.Unplayed)
                {
                    downloadStatus = PodcastDownloadStatus.NotDownloaded;
                }
                return downloadStatus;
            }
            private set
            {
                if (downloadStatus != value)
                {
                    downloadStatus = value;
                    
                    if (this.IsDeleted)
                        cancelThisDownload = true;

                    updateDisplayValues();
                }
            }
        }
        private Track track;
        public Track Track 
        {
            get { return track; }
            set
            {
                if (track != value)
                {
                    track = value;
                    fileExists = null;
                    if (track != null)
                    {
                        this.Duration = track.Duration;
                        track.TrackPlayed += (s) =>
                        {
                            this.DownloadStatus = PodcastDownloadStatus.Played;
                            updateDisplayValues();
                            if (DataChanged != null)
                                DataChanged(this);
                        };
                        track.TrackDeleted += (s) =>
                        {
                            switch (this.DownloadStatus)
                            {
                                case PodcastDownloadStatus.Played:
                                case PodcastDownloadStatus.Unplayed:
                                    this.DownloadStatus = PodcastDownloadStatus.NotDownloaded;
                                    break;
                            }
                            updateDisplayValues();
                            if (DataChanged != null)
                                DataChanged(this);
                        };
                    }
                }
            }
        }
        private string[] displayValues;

        public PodcastEpisode(string Title,
                              string GUID,
                              string Description,
                              string URL,
                              DateTime Date,
                              int Duration,
                              DateTime DownloadDate,
                              PodcastDownloadStatus DownloadStatus,
                              Track Track,
                              PodcastSubscription Subscription)
        {
            this.Title = Title;
            this.GUID = GUID;
            this.Description = Description;
            this.Date = Date;
            this.Duration = Duration;
            this.URL = URL;
            this.DownloadStatus = DownloadStatus;
            this.Subscription = Subscription;
            this.DownloadDate = DownloadDate;
            this.Track = Track;
            
            updateDisplayValues();
        }

        static PodcastEpisode()
        {
            sortComparisons = new Comparison<PodcastEpisode>[(int)Columns.Count];
            sortComparisons[(int)Columns.Title] = new Comparison<PodcastEpisode>((a, b) => a.Title.CompareTo(b.Title));
            sortComparisons[(int)Columns.Description] = new Comparison<PodcastEpisode>((a, b) => a.Description.CompareTo(b.Description));
            sortComparisons[(int)Columns.Date] = new Comparison<PodcastEpisode>((a, b) => a.Date.CompareTo(b.Date));
            sortComparisons[(int)Columns.DownloadStatus] = new Comparison<PodcastEpisode>((a, b) => a.DownloadStatusString.CompareTo(b.DownloadStatusString));
            sortComparisons[(int)Columns.Duration] = new Comparison<PodcastEpisode>((a, b) => a.Track == null ? ((b.Track == null) ? 0 : -1) : (b.Track == null ? 1 : (a.Track.Duration.CompareTo(b.Track.Duration))));

            revSortComparisons = new Comparison<PodcastEpisode>[(int)Columns.Count];
            revSortComparisons[(int)Columns.Title] = new Comparison<PodcastEpisode>((a, b) => b.Title.CompareTo(a.Title));
            revSortComparisons[(int)Columns.Description] = new Comparison<PodcastEpisode>((a, b) => b.Description.CompareTo(a.Description));
            revSortComparisons[(int)Columns.Date] = new Comparison<PodcastEpisode>((a, b) => b.Date.CompareTo(a.Date));
            revSortComparisons[(int)Columns.DownloadStatus] = new Comparison<PodcastEpisode>((a, b) => b.DownloadStatusString.CompareTo(a.DownloadStatusString));
            revSortComparisons[(int)Columns.Duration] = new Comparison<PodcastEpisode>((a, b) => b.Track == null ? ((a.Track == null) ? 0 : -1) : (a.Track == null ? 1 : (b.Track.Duration.CompareTo(a.Track.Duration))));
        }
        public bool IsColumnSortable(int ColumnNum)
        {
            return ColumnNum < 5;
        }
        public static int Compare(IListViewable A, IListViewable B, int Column, bool Fwd)
        {
            //System.Diagnostics.Debug.Assert(A is PodcastEpisode && B is PodcastEpisode);
            if (A == null)
                if (B == null)
                    return 0;
                else
                    return -1;
            else if (B == null)
                return 1;

            return Fwd ? sortComparisons[Column]((PodcastEpisode)A, (PodcastEpisode)B) : revSortComparisons[Column]((PodcastEpisode)A, (PodcastEpisode)B);
        }
        public bool ActionEnabled(int Index)
        {
            switch ((Columns)Index)
            {
                case Columns.Download:
                    return !this.Playable;
                case Columns.Play:
                    return this.Playable;
                    //return this.DownloadStatus == PodcastDownloadStatus.Unplayed || this.DownloadStatus == PodcastDownloadStatus.Played;
                case Columns.Remove:
                    return true;
                default:
                    return false;
            }
        }
        public bool IsAction(int Index)
        {
            switch ((Columns)Index)
            {
                case Columns.Download:
                case Columns.Play:
                case Columns.Remove:
                    return true;
                default:
                    return false;
            }
        }
        public void CancelDownload()
        {
            this.DownloadStatus = PodcastDownloadStatus.DownloadCanceled;
            this.cancelThisDownload = true;
        }
        private bool cancelThisDownload = false;
        private void updateDisplayValues()
        {
            string downloadAction;
            switch (this.DownloadStatus)
            {
                case PodcastDownloadStatus.DownloadInProgress:
                    downloadAction = "Stop";
                    break;
                case PodcastDownloadStatus.QueuedForDownload:
                    downloadAction = "Unqueue";
                    break;
                case PodcastDownloadStatus.Unplayed:
                case PodcastDownloadStatus.Played:
                    downloadAction = "Done";
                    break;
                default:
                    downloadAction = "Download";
                    break;
            }

            displayValues = new string[] { this.Title,
                                           this.Description,
                                           this.Date.ToShortDateString(),
                                           this.DownloadStatusString,
                                           this.DurationString,
                                           downloadAction,
                                           "Play",
                                           "Remove" };
            if (DataChanged != null)
                DataChanged(this);
        }
        private void updateDownloadStatusString(string Text)
        {
            displayValues[(int)Columns.DownloadStatus] = Text;
            if (DataChanged != null)
                DataChanged(this);
        }
        private void updateDownloadStatusString()
        {
            displayValues[(int)Columns.DownloadStatus] = DownloadStatusString;
            if (DataChanged != null)
                DataChanged(this);
        }
        public string[] DisplayValues
        {
            get { return displayValues; }
        }
        
        private const int NUM_DOWNLOAD_UPDATES = 100;
        public void QueueForDownload()
        {
            PodcastSubscription.Download(this);
        }
        public void Download()
        {
            cancelAllDownloads = false;
            cancelThisDownload = false;
            if (this.FileExists)
            {
                if (this.DownloadStatus != PodcastDownloadStatus.Played)
                    this.DownloadStatus = PodcastDownloadStatus.Unplayed;
            }
            else
            {
                using (WebClient client = new WebClient())
                {
                    this.DownloadStatus = PodcastDownloadStatus.DownloadInProgress;
                    updateDownloadStatusString();

                    Stream localStream = null;
                    Stream resposeStream = null;
                    HttpWebRequest webRequest = null;
                    HttpWebResponse webResponse = null;

                    try
                    {
                        long totalSize = 1;

                        string localTempFile = Path.GetTempFileName();

                        webRequest = (HttpWebRequest)WebRequest.Create(this.URL);
                        webRequest.Credentials = CredentialCache.DefaultCredentials;
                        webRequest.ServicePoint.Expect100Continue = false;
                        webResponse = (HttpWebResponse)webRequest.GetResponse();
                        totalSize = webResponse.ContentLength;
                        resposeStream = client.OpenRead(this.URL);
                        localStream = new FileStream(localTempFile,
                                                  FileMode.Create,
                                                  FileAccess.Write,
                                                  FileShare.None);

                        bool showProgress = totalSize > 100000;
                        int chunkSize = 0;
                        long bytesRecd = 0;
                        long nextThreshold = 0;
                        byte[] downBuffer = new byte[2048];

                        while ((chunkSize = resposeStream.Read(downBuffer, 0, downBuffer.Length)) > 0)
                        {
                            if (cancelAllDownloads || cancelThisDownload)
                            {
                                this.DownloadStatus = PodcastDownloadStatus.DownloadCanceled;
                                return;
                            }
                            if (showProgress)
                            {
                                bytesRecd += chunkSize;
                                if (bytesRecd > nextThreshold)
                                {
                                    nextThreshold += totalSize / NUM_DOWNLOAD_UPDATES;
                                    updateDownloadStatusString("Downloading: " + (bytesRecd * 100L / totalSize).ToString() + "%");
                                }
                            }
                            localStream.Write(downBuffer, 0, chunkSize);
                        }

                        localStream.Close();
                        localStream = null;

                        if (!Directory.Exists(Setting.PodcastDownloadDirectory))
                        {
                            try
                            {
                                Directory.CreateDirectory(Setting.PodcastDownloadDirectory);
                            }
                            catch
                            {
                            }
                            if (!Directory.Exists(Setting.PodcastDownloadDirectory))
                            {
                                Setting.PodcastDownloadDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                                if (!Directory.Exists(Setting.PodcastDownloadDirectory))
                                {
                                    Directory.CreateDirectory(Setting.PodcastDownloadDirectory);
                                }
                            }
                        }
                        if (!Directory.Exists(Setting.PodcastDownloadDirectory))
                        {
                            throw new Exception("failed2");
                        }
                        string fileName = Lib.ReplaceBadFilenameChars(this.Subscription.Name + " - " + this.Title);
                        string newPathRoot = Path.Combine(Setting.PodcastDownloadDirectory, fileName);
                        string ext = Path.GetExtension(this.URL);
                        
                        string newPath = newPathRoot + ext;
                        if (File.Exists(newPath))
                        {
                            int i = 1;
                            do
                            {
                                newPath = newPathRoot + (i++).ToString() + ext;
                            }
                            while (File.Exists(newPath));
                        }

                        File.Copy(localTempFile, newPath);
                        TrackWriter.AddToDeleteList(localTempFile);
                        TrackWriter.DeleteItems();
                        
                        AddToLibrary(newPath);

                        fileExists = null;

                        this.DownloadStatus = PodcastDownloadStatus.Unplayed;
                        this.DownloadDate = DateTime.Now;
                        if (this.DownloadDate > this.Subscription.LastDownloadDate)
                            this.Subscription.LastDownloadDate = this.DownloadDate;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                        this.DownloadStatus = PodcastDownloadStatus.Error;
                        Controller.ShowMessage("Download of " + this.Title + " failed.");
                    }
                    finally
                    {
                        if (localStream != null)
                            localStream.Close();
                        if (resposeStream != null)
                            resposeStream.Close();
                        if (webResponse != null)
                            webResponse.Close();
                        
                        updateDownloadStatusString();
                    }
                }
            }
            System.Diagnostics.Debug.Assert(!this.IsDownloading);
            if (DataChanged != null)
                DataChanged(this);
        }
        public static void CancelAllDownloads()
        {
            cancelAllDownloads = true;
        }
        public string DurationString
        {
            get
            {
                return durationString;
            }
        }
        public void AddToLibrary(String FilePath)
        {
            if (Track == null)
            {
                Track = Track.Load(FilePath);
            }
            if (Track == null)
            {
                this.DownloadStatus = PodcastDownloadStatus.Error;
                throw new Exception("failed");
            }
            Track.Title = this.Title;
            Track.Album = Subscription.Name;
            Track.Genre = this.Subscription.DefaultGenre;
            Track.RenameFormat = TrackWriter.RenameFormat.AR_AL_TK_TI;
            TrackWriter.AddToUnsavedTracks(Track);

            Database.AddToLibrary(Track, true, false);

            TrackWriter.Start();
        }
        public override bool Equals(object obj)
        {
            return this.GUID == obj.ToString();
        }
        public override int GetHashCode()
        {
            return GUID.GetHashCode();
        }
        public override string ToString()
        {
            return this.GUID;
        }
        public string DownloadStatusString
        {
            get
            {
                switch (this.DownloadStatus)
                {
                    case PodcastDownloadStatus.NotDownloaded:
                        return String.Empty;
                    case PodcastDownloadStatus.DownloadInProgress:
                        return "Downloading...";
                    case PodcastDownloadStatus.Unplayed:
                        return "Unplayed";
                    case PodcastDownloadStatus.Played:
                        return "Played";
                    case PodcastDownloadStatus.Error:
                        return "Error";
                    case PodcastDownloadStatus.Deleted:
                        return "Deleted";
                    case PodcastDownloadStatus.QueuedForDownload:
                        return "Queued";
                    case PodcastDownloadStatus.DownloadCanceled:
                        return "Canceled";
                    case PodcastDownloadStatus.None:
                        return "None";
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}
