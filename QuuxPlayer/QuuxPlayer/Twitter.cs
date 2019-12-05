/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace QuuxPlayer
{
    internal static class Twitter
    {
        public enum Mode { Track, Album }

        private const string TWITTER_URL = "http://twitter.com/statuses/update.json";

        private static Mode mode = Mode.Track;
        private static string password = String.Empty;
        private static string userName = String.Empty;
        private static bool on = false;
        private static Track lastTrack = null;

        public static void SendTwitterMessage(Track Track)
        {
            if (Twitter.On && Track.Title.Length > 0)
            {
                switch (TwitterMode)
                {
                    case Mode.Track:
                        twitTrack(Track);
                        break;
                    case Mode.Album:
                        if (lastTrack == null || Track.Album != lastTrack.Album)
                        {
                            if (Track.Album.Length == 0)
                                twitTrack(Track);
                            else
                                twitAlbum(Track);
                        }
                        break;
                }
                lastTrack = Track;
            }
        }

        private static void twitAlbum(Track Track)
        {
            if (Track.Artist.Length > 0)
                Twitter.SendTwitterMessage("Listening to &quot;" +
                                                Track.Album +
                                                "&quot; by " +
                                                Track.Artist +
                                                " using QuuxPlayer",
                                           UserName,
                                           Password);
            else
                Twitter.SendTwitterMessage("Listening to &quot;" +
                                                Track.Title +
                                                "&quot; using QuuxPlayer",
                                           UserName,
                                           Password);
        }

        private static void twitTrack(Track Track)
        {
            if (Track.Artist.Length > 0)
                Twitter.SendTwitterMessage("Listening to &quot;" +
                                                Track.Title +
                                                "&quot; by " +
                                                Track.Artist +
                                                " using QuuxPlayer",
                                           UserName,
                                           Password);
            else
                Twitter.SendTwitterMessage("Listening to &quot;" +
                                                Track.Title +
                                                "&quot; using QuuxPlayer",
                                           UserName,
                                           Password);
        }
        public static bool SendTwitterMessage(string Message, string UserName, string Password)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(TWITTER_URL);

                string post = String.Empty;
                using (TextWriter writer = new StringWriter())
                {
                    writer.Write("status={0}&source=quuxplayer", HttpUtility.UrlEncode(Message));
                    post = writer.ToString();
                }
                setRequestParams(request);
                request.Credentials = new NetworkCredential(UserName, Password);
                using (Stream requestStream = request.GetRequestStream())
                {
                    using (StreamWriter writer = new StreamWriter(requestStream))
                    {
                        writer.Write(post);
                    }
                }
                WebResponse response = request.GetResponse();
                string result;
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        result = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return false;
            }
            return true;
        }
        public static bool On
        {
            get { return on; }
            set { on = value; }
        }
        public static Mode TwitterMode
        {
            get { return mode; }
            set { mode = value; }
        }
        public static string UserName
        {
            get { return userName; }
            set { userName = value; }
        }
        public static string Password
        {
            get { return password; }
            set { password = value; }
        }
        private static void setRequestParams(HttpWebRequest request)
        {
            request.Timeout = 10000;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Referer = "http://www.quuxplayer.com";
            request.UserAgent = "QuuxPlayer";
            request.ServicePoint.Expect100Continue = false; // Need this after twitter changed their api behavior
#if USE_PROXY
          request.Proxy = new WebProxy("http://localhost:8080", false);
#endif
        }
    }
}
