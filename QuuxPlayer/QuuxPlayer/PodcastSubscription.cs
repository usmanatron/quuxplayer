/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml;
using System.Web;

namespace QuuxPlayer
{
    internal delegate void PodcastSubscriptionDelegate(PodcastSubscription PS);
    internal class PodcastSubscription : IListViewable
    {
        public enum Columns : int { Name, DefaultGenre, LastDownloadDate, DownloadCount, Status, CheckForNew, Download, Edit, Remove, Count }; // keep Count last

        private const string UNKNOWN_TITLE = "<Unknown>";

        private string defaultGenre;
        private string name;

        private static object episodeLock = new object();

        public event QListView<IListViewable>.ListViewDataChanged DataChanged;
        public string URL { get; set; }
        public string ReferenceURL { get; set; }
        private DateTime lastDownloadDate;
        public bool Loaded { get; private set; }
        private List<PodcastEpisode> episodes = new List<PodcastEpisode>();
        
        private static List<PodcastEpisode> downloadingEpisodes = new List<PodcastEpisode>();
        
        private string[] values;
        private static string[] dateParseFormats;

        private static Comparison<PodcastSubscription>[] sortComparisons;
        private static Comparison<PodcastSubscription>[] revSortComparisons;

        private static List<PodcastEpisode> syncQueue = new List<PodcastEpisode>();
        private static object syncQueueLock = new object();

        public List<PodcastEpisode> Episodes
        {
            get { return episodes; } 
            set
            {
                lock (episodeLock)
                {
                    episodes.Clear();
                    foreach (PodcastEpisode pe in value)
                        addEpisode(pe);
                }
                updateValues();
            }
        }

        public void Close(Callback Callback)
        {
            lock (episodeLock)
            {
                downloadingEpisodes.RemoveAll(pe => pe.Subscription == this);
            }
            if (Callback != null)
                Callback();
        }
        public bool IsSpecial { get { return false; } }
        private void addEpisode(PodcastEpisode Episode)
        {
            lock (episodeLock)
            {
                System.Diagnostics.Debug.Assert(Episode != null && Episode.GUID.Length > 0);
                if (episodes.Contains(Episode))
                {
                    //PodcastEpisode pe = episodes.Find(e => Episode.Equals(e));
                    //if (pe != null)
                    //    pe.Description = Episode.Description;
                    return;
                }
                else
                {
                    episodes.Add(Episode);
                }
            }
            
            if (Episode.DownloadDate > this.LastDownloadDate)
                this.LastDownloadDate = Episode.DownloadDate;
            
            Episode.DataChanged += (s) =>
            {
                updateForEpisodeChange(Episode);
            };
        }
        private void updateForEpisodeChange(PodcastEpisode Episode)
        {
            if (Episode.DownloadDate > this.LastDownloadDate)
                this.LastDownloadDate = Episode.DownloadDate;

            lock (episodeLock)
            {
                if (!Episode.IsDownloading && downloadingEpisodes.Contains(Episode))
                    downloadingEpisodes.Remove(Episode);
            }
            updateValues();
            this.DataChanged(this);
        }
        public PodcastSubscription(string Name,
                                   string URL,
                                   string DefaultGenre,
                                   string ReferenceURL,
                                   DateTime LastDownloadDate)
        {
            this.Name = Name;
            this.URL = URL;
            this.DefaultGenre = DefaultGenre;
            this.ReferenceURL = ReferenceURL;
            this.LastDownloadDate = LastDownloadDate;

            updateValues();
        }
        public string Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value;
                    updateValues();
                    if (DataChanged != null)
                        DataChanged(this);
                }
            }
        }
        public string DefaultGenre
        {
            get { return defaultGenre; }
            set
            {
                if (defaultGenre != value)
                {
                    defaultGenre = value;
                    updateValues();
                    if (DataChanged != null)
                        DataChanged(this);
                }
            }
        }
        public bool HasDeletedEpisodes
        {
            get { return episodes.Exists(ep => ep.IsDeleted); }
        }
        public bool IsColumnSortable(int ColumnNum)
        {
            return ColumnNum < 3;
        }
        static PodcastSubscription()
        {
            dateParseFormats = new string[] { CultureInfo.InvariantCulture.DateTimeFormat.RFC1123Pattern,
                                              "ddd, dd MMM yyyy HH:mm:ss zzz"};

            sortComparisons = new Comparison<PodcastSubscription>[(int)Columns.Count];
            sortComparisons[(int)Columns.Name] = new Comparison<PodcastSubscription>((a, b) => a.Name.CompareTo(b.Name));
            sortComparisons[(int)Columns.DefaultGenre] = new Comparison<PodcastSubscription>((a, b) => a.DefaultGenre.CompareTo(b.DefaultGenre));
            sortComparisons[(int)Columns.LastDownloadDate] = new Comparison<PodcastSubscription>((a, b) => a.LastDownloadDate.CompareTo(b.LastDownloadDate));
            //sortComparisons[(int)Columns.Download] = new Comparison<PodcastSubscription>((a, b) => 0);
            //sortComparisons[(int)Columns.Edit] = new Comparison<PodcastSubscription>((a, b) => 0);
            //sortComparisons[(int)Columns.Remove] = new Comparison<PodcastSubscription>((a, b) => 0);

            revSortComparisons = new Comparison<PodcastSubscription>[(int)Columns.Count];
            revSortComparisons[(int)Columns.Name] = new Comparison<PodcastSubscription>((a, b) => b.Name.CompareTo(a.Name));
            revSortComparisons[(int)Columns.DefaultGenre] = new Comparison<PodcastSubscription>((a, b) => b.DefaultGenre.CompareTo(a.DefaultGenre));
            revSortComparisons[(int)Columns.LastDownloadDate] = new Comparison<PodcastSubscription>((a, b) => b.LastDownloadDate.CompareTo(a.LastDownloadDate));
            //revSortComparisons[(int)Columns.Download] = new Comparison<PodcastSubscription>((a, b) => 0);
            //revSortComparisons[(int)Columns.Edit] = new Comparison<PodcastSubscription>((a, b) => 0);
            //revSortComparisons[(int)Columns.Remove] = new Comparison<PodcastSubscription>((a, b) => 0);
        }
        public PodcastSubscription(string URL)
        {
            try
            {
                this.Name = UNKNOWN_TITLE;
                this.URL = URL;
                this.DefaultGenre = "Podcast";
                this.LastDownloadDate = DateTime.MinValue;

                episodes = new List<PodcastEpisode>();

                UpdateEpisodeInfo();

                Loaded = episodes.Count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                Loaded = false;
            }
            finally
            {
                updateValues();
            }
        }
        private static string clean(string Input)
        {
            Input = HttpUtility.HtmlDecode(Input.Substring(0, Math.Min(Input.Length, 200)));
            Input = Input.Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ').Trim();
            
            int i = Input.IndexOf('<');
            if (i > 0)
                return Input.Substring(0, i);
            else
                return Input;
        }
        public void UpdateEpisodeInfo()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(URL);

                XmlNode baseNode = doc.SelectSingleNode("rss").FirstChild;

                if (baseNode != null)
                {
                    if (this.Name == UNKNOWN_TITLE || Keyboard.Shift)
                        this.Name = getXMLField(baseNode, "title");

                    this.ReferenceURL = getXMLField(baseNode, "link");

                    CultureInfo ci = CultureInfo.InvariantCulture;

                    foreach (XmlNode xn in doc.SelectNodes("//item"))
                    {
                        try
                        {
                            string url = xn.SelectSingleNode("enclosure").Attributes["url"].Value;
                            string date = String.Empty;
                            string guid = String.Empty;
                            string title = String.Empty;
                            string description = String.Empty;
                            string durationString = String.Empty;
                            int duration = 0;

                            if (url.Length > 0 && Track.IsValidExtension(Path.GetExtension(url)))
                            {
                                foreach (XmlNode n in xn.ChildNodes)
                                {
                                    switch (n.Name.ToLowerInvariant())
                                    {
                                        case "pubdate":
                                            date = n.InnerText;
                                            break;
                                        case "guid":
                                            guid = n.InnerText;
                                            break;
                                        case "title":
                                            title = n.InnerText;
                                            break;
                                        case "description":
                                            if (description.Length == 0)
                                                description = n.InnerText;
                                            break;
                                        case "itunes:duration":
                                            durationString = n.InnerText;
                                            break;
                                        case "itunes:summary":
                                            description = n.InnerText;
                                            break;
                                        default:
                                            System.Diagnostics.Debug.WriteLine(n.Name.ToLowerInvariant());
                                            break;
                                    }
                                }
                                DateTime dt;
                                if (!DateTime.TryParse(date, out dt))
                                    if (!DateTime.TryParseExact(date, dateParseFormats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out dt))
                                        dt = extractDate(date);

                                if (guid.Length == 0)
                                    guid = url;

                                if (durationString.Length > 0)
                                    duration = durationFromString(durationString);

                                PodcastEpisode pe = new PodcastEpisode(title,
                                                                       guid,
                                                                       clean(description),
                                                                       url,
                                                                       dt,
                                                                       duration,
                                                                       DateTime.MinValue,
                                                                       PodcastDownloadStatus.NotDownloaded,
                                                                       null,
                                                                       this);
                                addEpisode(pe);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(ex.ToString());
                        }
                    }
                    updateValues();
                    if (DataChanged != null)
                        DataChanged(this);
                    Controller.ShowMessage("Refreshed " + this.Name);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                Controller.ShowMessage("Failed to find podcast updates.");
            }
        }
        public void PopulateAndSortWithNewEpisodes(QListView<PodcastEpisode> lvwEpisodes)
        {
            Refresh();
            List<PodcastEpisode> ppe;

            lock (episodeLock)
            {
                ppe = episodes.FindAll(e => !e.IsDeleted).ToList();
            }

            lvwEpisodes.Items = ppe;
            lvwEpisodes.Sort();

            if (ppe.Count > 0)
                lvwEpisodes.SelectedItem = lvwEpisodes.Items[0];
        }
        public void Refresh()
        {
            updateValues();
        }
        private int durationFromString(string Input)
        {
            Input = Input.Trim();

            for (int i = 0; i < Input.Length; i++)
                if (Input[i] != ':' && (Input[i] < '0' || Input[i] > '9'))
                    return 0;

            if (!Input.Contains(':'))
                return Int32.Parse(Input) * 1000;

            Input = Input + ":";
            int res = 0;
            do
            {
                int i = Input.IndexOf(':');
                string s = Input.Substring(0, i);
                if (s.Length > 0)
                {
                    res *= 60;
                    res += Int32.Parse(s);
                }
                Input = Input.Substring(i + 1);
            }
            while (Input.Contains(':'));
            return res * 1000;
        }
        private static string getXMLField(XmlNode Node, string SubNodeName)
        {
            XmlNode subNode = Node.SelectSingleNode(SubNodeName);

            if (subNode == null)
                return String.Empty;
            else
                return subNode.InnerText;
        }
        public static int Compare(IListViewable A, IListViewable B, int Column, bool Fwd)
        {
            System.Diagnostics.Debug.Assert(A is PodcastSubscription && B is PodcastSubscription);
            return Fwd ? sortComparisons[Column]((PodcastSubscription)A, (PodcastSubscription)B) : revSortComparisons[Column]((PodcastSubscription)A, (PodcastSubscription)B);
        }
        public bool ActionEnabled(int Index)
        {
            switch ((Columns)Index)
            {
                case Columns.Download:
                case Columns.Edit:
                case Columns.Remove:
                case Columns.CheckForNew:
                    return true;
                default:
                    return false;
            }
        }
        public bool IsAction(int Index)
        {
            switch ((Columns)Index)
            {
                case Columns.CheckForNew:
                case Columns.Download:
                case Columns.Edit:
                case Columns.Remove:
                    return true;
                default:
                    return false;
            }
        }
        private void episodeDataChanged(IListViewable PE)
        {
            if (this.DataChanged != null)
                DataChanged(this);
        }
        public void DeleteSubscriptionFiles(Callback Callback)
        {
            List<string> files = new List<string>();
            List<Track> tracks = new List<Track>();

            List<PodcastEpisode> ppe = this.Episodes.ToList();

            foreach (PodcastEpisode pe in ppe)
            {
                if (pe.Track != null)
                {
                    tracks.Add(pe.Track);
                    if (pe.Track.ConfirmExists)
                        files.Add(pe.Track.FilePath);
                }
            }

            Database.RemoveFromLibrary(tracks);

            if (files.Count > 0)
            {
                foreach (string s in files)
                    TrackWriter.AddToDeleteList(s);
            }
            TrackWriter.DeleteItems();
            this.Close(Callback);
        }
        public void DownloadThisSubscription()
        {
            List<PodcastEpisode> ppe;
            lock (episodeLock)
            {
                ppe = this.episodes.FindAll(e => !e.IsDeleted);
            }
            ppe.Sort((a, b) => (b.Date.CompareTo(a.Date)));
            
            foreach (PodcastEpisode pe in ppe)
                PodcastSubscription.Download(pe);
         
            if (DataChanged != null)
                DataChanged(this);
        }
        public void DownloadNewEpisodes()
        {
            List<PodcastEpisode> ppe;
            lock (episodeLock)
            {
                ppe = episodes.ToList();
            }
            if (ppe.Count > 0)
            {
                ppe.Sort((a, b) => b.Date.CompareTo(a.Date));

                ppe = ppe.TakeWhile(p => p.DownloadStatus == PodcastDownloadStatus.NotDownloaded).ToList();

                foreach (PodcastEpisode pe in ppe)
                    PodcastSubscription.Download(pe);

                if (DataChanged != null)
                    DataChanged(this);
            }
        }
        public PodcastEpisode LatestEpisode
        {
            get
            {
                List<PodcastEpisode> ppe;
                lock (episodeLock)
                {
                    ppe = episodes.ToList();
                }
                if (ppe.Count == 0)
                {
                    return null;
                }
                else
                {
                    ppe.Sort((a, b) => b.Date.CompareTo(a.Date));
                    return ppe[0];
                }
            }
        }
        public void DownloadLatestEpisode()
        {
            PodcastEpisode pe = this.LatestEpisode;
            if (pe != null && pe.IsQueueable)
            {
                PodcastSubscription.Download(pe);

                if (DataChanged != null)
                    DataChanged(this);
            }
        }
        public static void Download(PodcastEpisode PE)
        {
            cancelDownload = false;
            if (PE != null && PE.IsDownloadable)
            {
                PE.SetDownloadStatus(PodcastDownloadStatus.QueuedForDownload);
                lock (syncQueueLock)
                {
                    if (!syncQueue.Contains(PE))
                    {
                        syncQueue.Add(PE);
                        Clock.DoOnNewThread(startSync, 100);
                    }
                }
            }
        }
        public bool DownloadInProgress
        {
            get
            {
                lock (episodeLock)
                {
                    return episodes.Exists(e => e.IsDownloading);
                }
            }
        }
        public bool HasQueuedOrDownloadingEpisodes
        {
            get
            {
                lock (episodeLock)
                {
                    return episodes.Exists(e => e.IsQueuedOrDownloading);
                }
            }
        }
        public DateTime LastDownloadDate
        {
            get { return lastDownloadDate; }
            set
            {
                if (this.lastDownloadDate != value)
                {
                    this.lastDownloadDate = value;
                    values[(int)Columns.LastDownloadDate] = LastDownloadDateString;
                    if (DataChanged != null)
                        DataChanged(this);
                }
            }
        }
        public static void CancelDownload(PodcastEpisode PE)
        {
            PE.CancelDownload();
            lock (syncQueueLock)
            {
                syncQueue.Remove(PE);
            }
        }
        public static void StopDownloads()
        {
            cancelDownload = true;

            PodcastEpisode.CancelAllDownloads();
            
            lock (syncQueueLock)
            {
                foreach (PodcastEpisode pe in syncQueue)
                    pe.SetDownloadStatus(PodcastDownloadStatus.DownloadCanceled);

                syncQueue.Clear();
            }
        }
        private static bool syncing = false;
        private static bool cancelDownload = false;
        private static void startSync()
        {
            if (syncing)
                return;
            
            syncing = true;

            try
            {
                while (!cancelDownload)
                {
                    PodcastEpisode ep = null;

                    lock (syncQueueLock)
                    {
                        if (syncQueue.Count > 0)
                        {
                            ep = syncQueue[0];
                            syncQueue.RemoveAt(0);
                        }
                        else
                        {
                            cancelDownload = true;
                        }
                    }
                    if (ep != null)
                    {
                        lock (episodeLock)
                        {
                            downloadingEpisodes.Add(ep);
                        }
                        Clock.DoOnNewThread(ep.Download);
                        bool wait;
                        do
                        {
                            lock (episodeLock)
                            {
                                wait = downloadingEpisodes.Count >= Setting.PodcastMaxConcurrentDownloads;
                            }
                            System.Threading.Thread.Sleep(1000);
                        }
                        while (wait);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            finally
            {
                syncing = false;
            }
        }
        private DateTime extractDate(string Input)
        {
            System.Diagnostics.Debug.Assert(Input == Input.Trim());

            double offset = double.MinValue;

            if (Input.EndsWith(" GMT", StringComparison.OrdinalIgnoreCase)) { offset = +0; Input = Input.Substring(0, Input.LastIndexOf(" GMT")); }
            else if (Input.EndsWith(" WET", StringComparison.OrdinalIgnoreCase)) { offset = +0; Input = Input.Substring(0, Input.LastIndexOf(" WET")); }
            else if (Input.EndsWith(" BST", StringComparison.OrdinalIgnoreCase)) { offset = +1; Input = Input.Substring(0, Input.LastIndexOf(" BST")); }
            else if (Input.EndsWith(" IST", StringComparison.OrdinalIgnoreCase)) { offset = +1; Input = Input.Substring(0, Input.LastIndexOf(" IST")); }
            else if (Input.EndsWith(" MEZ", StringComparison.OrdinalIgnoreCase)) { offset = +1; Input = Input.Substring(0, Input.LastIndexOf(" MEZ")); }
            else if (Input.EndsWith(" WEDT", StringComparison.OrdinalIgnoreCase)) { offset = +1; Input = Input.Substring(0, Input.LastIndexOf(" WEDT")); }
            else if (Input.EndsWith(" WEST", StringComparison.OrdinalIgnoreCase)) { offset = +1; Input = Input.Substring(0, Input.LastIndexOf(" WEST")); }
            else if (Input.EndsWith(" CET", StringComparison.OrdinalIgnoreCase)) { offset = +1; Input = Input.Substring(0, Input.LastIndexOf(" CET")); }
            else if (Input.EndsWith(" CEST", StringComparison.OrdinalIgnoreCase)) { offset = +2; Input = Input.Substring(0, Input.LastIndexOf(" CEST")); }
            else if (Input.EndsWith(" CEDT", StringComparison.OrdinalIgnoreCase)) { offset = +2; Input = Input.Substring(0, Input.LastIndexOf(" CEDT")); }
            else if (Input.EndsWith(" MESZ", StringComparison.OrdinalIgnoreCase)) { offset = +2; Input = Input.Substring(0, Input.LastIndexOf(" MESZ")); }
            else if (Input.EndsWith(" EET", StringComparison.OrdinalIgnoreCase)) { offset = +2; Input = Input.Substring(0, Input.LastIndexOf(" EET")); }
            else if (Input.EndsWith(" EEST", StringComparison.OrdinalIgnoreCase)) { offset = +3; Input = Input.Substring(0, Input.LastIndexOf(" EEST")); }
            else if (Input.EndsWith(" EEDT", StringComparison.OrdinalIgnoreCase)) { offset = +3; Input = Input.Substring(0, Input.LastIndexOf(" EEDT")); }
            else if (Input.EndsWith(" MSK", StringComparison.OrdinalIgnoreCase)) { offset = +3; Input = Input.Substring(0, Input.LastIndexOf(" MSK")); }
            else if (Input.EndsWith(" MSD", StringComparison.OrdinalIgnoreCase)) { offset = +4; Input = Input.Substring(0, Input.LastIndexOf(" MSD")); }
            else if (Input.EndsWith(" HAT", StringComparison.OrdinalIgnoreCase)) { offset = -2.5; Input = Input.Substring(0, Input.LastIndexOf(" HAT")); }
            else if (Input.EndsWith(" HAA", StringComparison.OrdinalIgnoreCase)) { offset = -3; Input = Input.Substring(0, Input.LastIndexOf(" HAA")); }
            else if (Input.EndsWith(" HNT", StringComparison.OrdinalIgnoreCase)) { offset = -3.5; Input = Input.Substring(0, Input.LastIndexOf(" HNT")); }
            else if (Input.EndsWith(" HAE", StringComparison.OrdinalIgnoreCase)) { offset = -4; Input = Input.Substring(0, Input.LastIndexOf(" HAE")); }
            else if (Input.EndsWith(" HNA", StringComparison.OrdinalIgnoreCase)) { offset = -4; Input = Input.Substring(0, Input.LastIndexOf(" HNA")); }
            else if (Input.EndsWith(" HAC", StringComparison.OrdinalIgnoreCase)) { offset = -5; Input = Input.Substring(0, Input.LastIndexOf(" HAC")); }
            else if (Input.EndsWith(" HNE", StringComparison.OrdinalIgnoreCase)) { offset = -5; Input = Input.Substring(0, Input.LastIndexOf(" HNE")); }
            else if (Input.EndsWith(" HAR", StringComparison.OrdinalIgnoreCase)) { offset = -6; Input = Input.Substring(0, Input.LastIndexOf(" HAR")); }
            else if (Input.EndsWith(" HNC", StringComparison.OrdinalIgnoreCase)) { offset = -6; Input = Input.Substring(0, Input.LastIndexOf(" HNC")); }
            else if (Input.EndsWith(" HAP", StringComparison.OrdinalIgnoreCase)) { offset = -7; Input = Input.Substring(0, Input.LastIndexOf(" HAP")); }
            else if (Input.EndsWith(" HNR", StringComparison.OrdinalIgnoreCase)) { offset = -7; Input = Input.Substring(0, Input.LastIndexOf(" HNR")); }
            else if (Input.EndsWith(" HAY", StringComparison.OrdinalIgnoreCase)) { offset = -8; Input = Input.Substring(0, Input.LastIndexOf(" HAY")); }
            else if (Input.EndsWith(" HNP", StringComparison.OrdinalIgnoreCase)) { offset = -8; Input = Input.Substring(0, Input.LastIndexOf(" HNP")); }
            else if (Input.EndsWith(" HNY", StringComparison.OrdinalIgnoreCase)) { offset = -9; Input = Input.Substring(0, Input.LastIndexOf(" HNY")); }
            else if (Input.EndsWith(" NST", StringComparison.OrdinalIgnoreCase)) { offset = -3.5; Input = Input.Substring(0, Input.LastIndexOf(" NST")); }
            else if (Input.EndsWith(" NDT", StringComparison.OrdinalIgnoreCase)) { offset = -2.5; Input = Input.Substring(0, Input.LastIndexOf(" NDT")); }
            else if (Input.EndsWith(" EST", StringComparison.OrdinalIgnoreCase)) { offset = -5; Input = Input.Substring(0, Input.LastIndexOf(" EST")); }
            else if (Input.EndsWith(" EDT", StringComparison.OrdinalIgnoreCase)) { offset = -4; Input = Input.Substring(0, Input.LastIndexOf(" EDT")); }
            else if (Input.EndsWith(" AST", StringComparison.OrdinalIgnoreCase)) { offset = -4; Input = Input.Substring(0, Input.LastIndexOf(" AST")); }
            else if (Input.EndsWith(" ADT", StringComparison.OrdinalIgnoreCase)) { offset = -3; Input = Input.Substring(0, Input.LastIndexOf(" ADT")); }
            else if (Input.EndsWith(" CST", StringComparison.OrdinalIgnoreCase)) { offset = -6; Input = Input.Substring(0, Input.LastIndexOf(" CST")); }
            else if (Input.EndsWith(" CDT", StringComparison.OrdinalIgnoreCase)) { offset = -5; Input = Input.Substring(0, Input.LastIndexOf(" CDT")); }
            else if (Input.EndsWith(" MST", StringComparison.OrdinalIgnoreCase)) { offset = -7; Input = Input.Substring(0, Input.LastIndexOf(" MST")); }
            else if (Input.EndsWith(" MDT", StringComparison.OrdinalIgnoreCase)) { offset = -6; Input = Input.Substring(0, Input.LastIndexOf(" MDT")); }
            else if (Input.EndsWith(" PST", StringComparison.OrdinalIgnoreCase)) { offset = -8; Input = Input.Substring(0, Input.LastIndexOf(" PST")); }
            else if (Input.EndsWith(" PDT", StringComparison.OrdinalIgnoreCase)) { offset = -7; Input = Input.Substring(0, Input.LastIndexOf(" PDT")); }
            else if (Input.EndsWith(" HAST", StringComparison.OrdinalIgnoreCase)) { offset = -10; Input = Input.Substring(0, Input.LastIndexOf(" HAST")); }
            else if (Input.EndsWith(" HADT", StringComparison.OrdinalIgnoreCase)) { offset = -9; Input = Input.Substring(0, Input.LastIndexOf(" HADT")); }
            else if (Input.EndsWith(" WST", StringComparison.OrdinalIgnoreCase)) { offset = +8; Input = Input.Substring(0, Input.LastIndexOf(" WST")); }
            else if (Input.EndsWith(" WDT", StringComparison.OrdinalIgnoreCase)) { offset = +9; Input = Input.Substring(0, Input.LastIndexOf(" WDT")); }
            else if (Input.EndsWith(" ACST", StringComparison.OrdinalIgnoreCase)) { offset = +9.5; Input = Input.Substring(0, Input.LastIndexOf(" ACST")); }
            else if (Input.EndsWith(" ACDT", StringComparison.OrdinalIgnoreCase)) { offset = +10.5; Input = Input.Substring(0, Input.LastIndexOf(" ACDT")); }
            else if (Input.EndsWith(" AEST", StringComparison.OrdinalIgnoreCase)) { offset = +10; Input = Input.Substring(0, Input.LastIndexOf(" AEST")); }
            else if (Input.EndsWith(" AEDT", StringComparison.OrdinalIgnoreCase)) { offset = +11; Input = Input.Substring(0, Input.LastIndexOf(" AEDT")); }
            else if (Input.EndsWith(" AWST", StringComparison.OrdinalIgnoreCase)) { offset = +8; Input = Input.Substring(0, Input.LastIndexOf(" AWST")); }
            else if (Input.EndsWith(" AWDT", StringComparison.OrdinalIgnoreCase)) { offset = +9; Input = Input.Substring(0, Input.LastIndexOf(" AWDT")); }
            else if (Input.EndsWith(" NFT", StringComparison.OrdinalIgnoreCase)) { offset = +11.5; Input = Input.Substring(0, Input.LastIndexOf(" NFT")); }
            else if (Input.EndsWith(" CXT", StringComparison.OrdinalIgnoreCase)) { offset = +7; Input = Input.Substring(0, Input.LastIndexOf(" CXT")); }
            else if (Input.EndsWith(" AKST", StringComparison.OrdinalIgnoreCase)) { offset = -9; Input = Input.Substring(0, Input.LastIndexOf(" AKST")); }
            else if (Input.EndsWith(" AKDT", StringComparison.OrdinalIgnoreCase)) { offset = -8; Input = Input.Substring(0, Input.LastIndexOf(" AKDT")); }
            else if (Input.EndsWith(" A", StringComparison.OrdinalIgnoreCase)) { offset = +1; Input = Input.Substring(0, Input.LastIndexOf(" A")); }
            else if (Input.EndsWith(" B", StringComparison.OrdinalIgnoreCase)) { offset = +2; Input = Input.Substring(0, Input.LastIndexOf(" B")); }
            else if (Input.EndsWith(" C", StringComparison.OrdinalIgnoreCase)) { offset = +3; Input = Input.Substring(0, Input.LastIndexOf(" C")); }
            else if (Input.EndsWith(" D", StringComparison.OrdinalIgnoreCase)) { offset = +4; Input = Input.Substring(0, Input.LastIndexOf(" D")); }
            else if (Input.EndsWith(" E", StringComparison.OrdinalIgnoreCase)) { offset = +5; Input = Input.Substring(0, Input.LastIndexOf(" E")); }
            else if (Input.EndsWith(" F", StringComparison.OrdinalIgnoreCase)) { offset = +6; Input = Input.Substring(0, Input.LastIndexOf(" F")); }
            else if (Input.EndsWith(" G", StringComparison.OrdinalIgnoreCase)) { offset = +7; Input = Input.Substring(0, Input.LastIndexOf(" G")); }
            else if (Input.EndsWith(" H", StringComparison.OrdinalIgnoreCase)) { offset = +8; Input = Input.Substring(0, Input.LastIndexOf(" H")); }
            else if (Input.EndsWith(" I", StringComparison.OrdinalIgnoreCase)) { offset = +9; Input = Input.Substring(0, Input.LastIndexOf(" I")); }
            else if (Input.EndsWith(" K", StringComparison.OrdinalIgnoreCase)) { offset = +10; Input = Input.Substring(0, Input.LastIndexOf(" K")); }
            else if (Input.EndsWith(" L", StringComparison.OrdinalIgnoreCase)) { offset = +11; Input = Input.Substring(0, Input.LastIndexOf(" L")); }
            else if (Input.EndsWith(" M", StringComparison.OrdinalIgnoreCase)) { offset = +12; Input = Input.Substring(0, Input.LastIndexOf(" M")); }
            else if (Input.EndsWith(" N", StringComparison.OrdinalIgnoreCase)) { offset = -1; Input = Input.Substring(0, Input.LastIndexOf(" N")); }
            else if (Input.EndsWith(" O", StringComparison.OrdinalIgnoreCase)) { offset = -2; Input = Input.Substring(0, Input.LastIndexOf(" O")); }
            else if (Input.EndsWith(" P", StringComparison.OrdinalIgnoreCase)) { offset = -3; Input = Input.Substring(0, Input.LastIndexOf(" P")); }
            else if (Input.EndsWith(" Q", StringComparison.OrdinalIgnoreCase)) { offset = -4; Input = Input.Substring(0, Input.LastIndexOf(" Q")); }
            else if (Input.EndsWith(" R", StringComparison.OrdinalIgnoreCase)) { offset = -5; Input = Input.Substring(0, Input.LastIndexOf(" R")); }
            else if (Input.EndsWith(" S", StringComparison.OrdinalIgnoreCase)) { offset = -6; Input = Input.Substring(0, Input.LastIndexOf(" S")); }
            else if (Input.EndsWith(" T", StringComparison.OrdinalIgnoreCase)) { offset = -7; Input = Input.Substring(0, Input.LastIndexOf(" T")); }
            else if (Input.EndsWith(" U", StringComparison.OrdinalIgnoreCase)) { offset = -8; Input = Input.Substring(0, Input.LastIndexOf(" U")); }
            else if (Input.EndsWith(" V", StringComparison.OrdinalIgnoreCase)) { offset = -9; Input = Input.Substring(0, Input.LastIndexOf(" V")); }
            else if (Input.EndsWith(" W", StringComparison.OrdinalIgnoreCase)) { offset = -10; Input = Input.Substring(0, Input.LastIndexOf(" W")); }
            else if (Input.EndsWith(" X", StringComparison.OrdinalIgnoreCase)) { offset = -11; Input = Input.Substring(0, Input.LastIndexOf(" X")); }
            else if (Input.EndsWith(" Y", StringComparison.OrdinalIgnoreCase)) { offset = -12; Input = Input.Substring(0, Input.LastIndexOf(" Y")); }
            else if (Input.EndsWith(" Z", StringComparison.OrdinalIgnoreCase)) { offset = +0; Input = Input.Substring(0, Input.LastIndexOf(" Z")); }

            if (offset < -100.0)
                return DateTime.Now;

            DateTime dt;

            if (DateTime.TryParse(Input + " +0000", out dt))
                return dt.AddHours(-offset);

            return DateTime.Now;
        }
        public PodcastEpisode this[int Index]
        {
            get { return episodes[Index]; }
        }
        public string LastDownloadDaysAgo
        {
            get
            {
                if (this.LastDownloadDate.Date == DateTime.Today)
                    return "Today";
                else if (this.LastDownloadDate.Date == DateTime.Today.AddDays(-1).Date)
                    return "Yesterday";
                else if (this.LastDownloadDate.Year < 2005)
                    return "Never";
                else
                    return (DateTime.Today - this.LastDownloadDate.Date).Days.ToString() + " Days";
            }
        }
        private string StatusString
        {
            get
            {
                if (episodes == null)
                    return String.Empty;

                List<PodcastEpisode> ppe;
                lock (episodeLock)
                {
                    ppe = episodes.FindAll(e => !e.IsDeleted);
                }

                int i;
                if (episodes.Count == 0)
                    return "Empty";
                else if (ppe.Exists(e => e.IsDownloading))
                    return "In Progress";
                else if ((i = ppe.Count(e => e.IsQueued)) > 0)
                    return i.ToString() + " Queued";
                else if (ppe.Count(e => e.Playable) == ppe.Count)
                    return "Up to Date";
                else
                    return episodes.Count(ep => ep.IsDownloadable).ToString() + " Available";
            }
        }
        private string DownloadsCountString
        {
            get
            {
                if (episodes == null)
                    return String.Empty;

                lock (episodeLock)
                {
                    return String.Format("{0} / {1}",
                                         episodes.Count(ep => ep.Playable).ToString(),
                                         episodes.Count(ep => !ep.IsDeleted).ToString());
                }
            }
        }
        private void updateValues()
        {
            this.values = new string[] { Name,
                                         DefaultGenre,
                                         LastDownloadDateString,
                                         DownloadsCountString,
                                         StatusString,
                                         "Refresh",
                                         "Get All",
                                         "Edit",
                                         "Remove" };
        }
        private string LastDownloadDateString { get { return LastDownloadDate.Year > 2000 ? LastDownloadDate.ToShortDateString() : "Never"; } }
        public string[] DisplayValues
        {
            get
            {
                return values;
            }
        }

        public override bool Equals(object obj)
        {
            PodcastSubscription other = obj as PodcastSubscription;
            if (other == null)
                return false;
            else
                return String.Compare(this.URL, other.URL, StringComparison.OrdinalIgnoreCase) == 0;
        }
        public override int GetHashCode()
        {
            return this.URL.ToLowerInvariant().GetHashCode();
        }
    }
}
