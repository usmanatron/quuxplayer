/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using System.Web;

namespace QuuxPlayer
{
    internal sealed class LastFM
    {
        private class InfoPacket
        {
            public bool IsLoading { get; set; }
            public string Info { get; set; }
            public DateTime Date { get; set; }
            public string URL { get; set; }
            public string ImageURL { get; set; }

            public InfoPacket()
            {
                Info = String.Empty;
                Date = DateTime.MinValue;
                URL = String.Empty;
                ImageURL = String.Empty;
                IsLoading = false;
            }
        }

        internal delegate void AlbumCallback(string Info, DateTime Date, string URL);
        internal delegate void ArtistCallback(string Info, string URL);

        private const int MAX_SCROBBLE_TRACKS = 10;
        private const string CLIENT_ID = "qux";
        private const string CLIENT_VER = "1.0";
        private const string CLIENT_INFO = "c=" + CLIENT_ID + "&v=" + CLIENT_VER + "&";
        private const string API_KEY = "03ab02be3ca5559ba33f3fc74474cdad";

        // SCROBBLING

        private const string SESSION_URL = "http://post.audioscrobbler.com/?hs=true&p=1.2.1&{0}u={1}&t={2}&a={3}";
        private const string NOW_PLAYING_VALS = "s={0}&a={1}&t={2}&b={3}&l={4}&n={5}&m=";
        private const string SUBMISSION_VALS = "a[{0}]={1}&t[{0}]={2}&i[{0}]={3}&o[{0}]=P&r[{0}]=&l[{0}]={4}&b[{0}]={5}&n[{0}]={6}&m[{0}]=";
        
        private static string sessionID = String.Empty;
        private static string nowPlayingURL = String.Empty;
        private static string submissionURL = String.Empty;
        private static string nowPlayingVals = String.Empty;
        private static string submissionVals = String.Empty;
        private static string username = String.Empty;
        private static string password = String.Empty;
        private static List<Track> backlog = new List<Track>();
        private static List<Track> backlogSafe = new List<Track>();

        private static Dictionary<string, InfoPacket> artistInfo = new Dictionary<string, InfoPacket>();
        private static Dictionary<string, InfoPacket> albumInfo = new Dictionary<string, InfoPacket>();

        public static void Scrobble(Track Track, string UserName, string Password, bool NowPlaying)
        {
            try
            {
                if (!NowPlaying)
                    backlog.Add(Track);

                if (sessionID.Length == 0)
                {
                    if (!getScrobbleSession(UserName, Password))
                        return;
                }

                if (NowPlaying)
                {
                    string np = String.Format(NOW_PLAYING_VALS,
                                              sessionID,
                                              HttpUtility.UrlEncode(Track.Artist),
                                              HttpUtility.UrlEncode(Track.Title),
                                              HttpUtility.UrlEncode(Track.Album),
                                              (Track.Duration / 1000).ToString(),
                                              Track.TrackNumString);
                    Net.Post(nowPlayingURL, np);
                }
                else
                {
                    while (backlog.Count > 0)
                    {
                        bool res = false;
                        string sub = getScrobbleString();
                        if (sub.Length > 0)
                        {
                            res = Net.Post(submissionURL, sub).ToUpperInvariant().StartsWith("OK");
                        }
                        if (!res)
                        {
                            sessionID = String.Empty;
                            backlog.AddRange(backlogSafe);
                            backlogSafe.Clear();
                            return;
                        }
                        backlogSafe.Clear();
                    }
                }
            }
            catch
            {
            }
        }
        public static List<Track> Backlog { get { return backlog; } set { backlog = value; } }
        
        public static string GetLastFMArtistURL(Track Track)
        {
            return "http://www.last.fm/music/" +
                   HttpUtility.UrlEncode(Track.Artist.Replace(' ', '+'));
        }
        public static string GetLastFMAlbumURL(Track Track)
        {
            return "http://www.last.fm/music/" +
                   HttpUtility.UrlEncode(Track.Artist.Replace(' ', '+')) +
                   "/" +
                   HttpUtility.UrlEncode(Track.Album.Replace(' ', '+'));
        }
        public static string GetAlbumImageURL(string Artist, string Album)
        {
            InfoPacket ip = getInfoPacket(Artist, Album);
            return ip.ImageURL;
        }
        public static void UpdateAlbumInfo(Track Track, AlbumCallback Callback)
        {
            updateAlbumInfo(Track.Artist, Track.Album, Callback);
        }
        private static void updateAlbumInfo(string Artist, string Album, AlbumCallback Callback)
        {
            InfoPacket ip = getInfoPacket(Artist, Album);
            Callback(ip.Info, ip.Date, ip.URL);
        }
        private static InfoPacket getInfoPacket(string Artist, string Album)
        {
            InfoPacket ip = null;

            if (albumInfo.ContainsKey(Artist + Album))
            {
                ip = albumInfo[Artist + Album];
                
                while (ip.IsLoading)
                    System.Threading.Thread.Sleep(100);

                if (ip.Info == Net.FAILED_TOKEN && StringUtil.HasParentheticalChars(Album))
                {
                    return getInfoPacket(Artist, StringUtil.RemoveParentheticalChars(Album));
                }
                else
                {
                    return ip;
                }
            }

            ip = new InfoPacket();
            ip.IsLoading = true;
            albumInfo.Add(Artist + Album, ip);
            ip.Info = Net.FAILED_TOKEN;

            string date = String.Empty;

            if (Artist.Length > 0 && Album.Length > 0)
            {
                string url = String.Format("http://ws.audioscrobbler.com/2.0/?method=album.getinfo&api_key={0}&artist={1}&album={2}",
                                           API_KEY,
                                           HttpUtility.UrlEncode(Artist),
                                           HttpUtility.UrlEncode(Album));

                string ret = Net.Get(url);

                if (ret.Length > 0)
                {
                    populateInfoPacket(ip, ret);

                    if (ip.Info == Net.FAILED_TOKEN &&
                        StringUtil.HasParentheticalChars(Album))
                    {
                        ip.IsLoading = false;
                        return getInfoPacket(Artist, StringUtil.RemoveParentheticalChars(Album));
                    }
                }
            }
            if (ip.Info != Net.FAILED_TOKEN)
            {
                ip.Info += Environment.NewLine +
                           Environment.NewLine +
                           "Information provided courtesy of last.fm";
            }
            ip.IsLoading = false;
            return ip;
        }
        public static void UpdateArtistInfo(Track Track, ArtistCallback Callback)
        {
            InfoPacket ip;

            if (artistInfo.ContainsKey(Track.Artist))
            {
                ip = artistInfo[Track.Artist];
                Callback(ip.Info, ip.URL);
                return;
            }

            ip = new InfoPacket();
            artistInfo.Add(Track.Artist, ip);
            ip.Info = Net.FAILED_TOKEN;

            if (Track.Artist.Length > 0)
            {                
                string url = String.Format("http://ws.audioscrobbler.com/2.0/?method=artist.getinfo&api_key={0}&artist={1}",
                               API_KEY,
                               HttpUtility.UrlEncode(Track.Artist));

                string ret = Net.Get(url);

                if (ret.Length > 0)
                {
                    populateInfoPacket(ip, ret);

                    if (ip.Info != Net.FAILED_TOKEN)
                        ip.Info += Environment.NewLine + Environment.NewLine + "Information provided courtesy of last.fm";
                }
            }

            Callback(ip.Info, ip.URL);
            
            return;
        }

        private static void populateInfoPacket(InfoPacket IP, string Input)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(Input);

            // Images

            XmlNodeList images = xmlDoc.GetElementsByTagName("image");

            foreach (XmlNode xn in images)
            {
                if (xn.Attributes["size"].Value == "extralarge")
                {
                    IP.ImageURL = xn.InnerText;
                    break;
                }
            }

            // Content

            XmlNodeList content = xmlDoc.GetElementsByTagName("content");

            if (content.Count > 0)
            {
                IP.Info = Net.ScrubHTMLNew(content[0].InnerXml);
            }
            else
            {
                content = xmlDoc.GetElementsByTagName("summary");
                if (content.Count > 0)
                    IP.Info = Net.ScrubHTMLNew(content[0].InnerText);
            }

            // URL

            XmlNodeList url = xmlDoc.GetElementsByTagName("url");

            if (url.Count > 0)
            {
                IP.URL = url[0].InnerText;
            }

            // Date

            DateTime d = DateTime.MinValue;
            XmlNodeList date = xmlDoc.GetElementsByTagName("releasedate");
            if (date.Count > 0)
            {
                string dateString = date[0].InnerText;
                DateTime.TryParse(dateString, out d);
            }
            IP.Date = d;
        }
        private static string getScrobbleString()
        {
            StringBuilder sb = new StringBuilder();
            if (backlog.Count > 0)
            {
                Track t;
                int i = 0;

                sb.Append("s=");
                sb.Append(sessionID);
                while (i < MAX_SCROBBLE_TRACKS && ((t = getBacklogTrack()) != null))
                {
                    backlogSafe.Add(t);
                    sb.Append("&");
                    sb.Append(getScrobbleSubString(i, t));
                    i++;
                }
            }
            return sb.ToString();
        }
        private static string getScrobbleSubString(int Index, Track Track)
        {
            return String.Format(SUBMISSION_VALS,
                                 Index.ToString(),
                                 HttpUtility.UrlEncode(Track.Artist),
                                 HttpUtility.UrlEncode(Track.Title),
                                 Net.GetUNIXTimestamp(Track.LastPlayedDate),
                                 (Track.Duration / 1000).ToString(),
                                 HttpUtility.UrlEncode(Track.Album),
                                 Track.TrackNumString);
        }
        private static Track getBacklogTrack()
        {
            if (backlog.Count > 0)
            {
                Track t = backlog[0];
                backlog.RemoveAt(0);
                return t;
            }
            else
            {
                return null;
            }
        }
        private static bool getScrobbleSession(string UserName, string Password)
        {
            string timeStamp = Net.GetUNIXTimestamp(DateTime.Now);

            string auth = Notices.MD5Hash(Notices.MD5Hash(Password) + timeStamp);

            string s = String.Format(SESSION_URL, CLIENT_INFO, UserName, timeStamp, auth);
            string result = Net.Get(s);

            string[] vals = result.Split('\n');

            if (vals.Length < 4)
                return false;

            if (vals[0] != "OK")
                return false;

            sessionID = vals[1];
            nowPlayingURL = vals[2];
            submissionURL = vals[3];

            return true;
        }
    }
}
