/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;
using System.Web;

namespace QuuxPlayer
{
    internal static class Net
    {
        internal const string FAILED_TOKEN = "{{fail}}";
        
        public static void BrowseTo(string URL)
        {
            try
            {
                Controller.GetInstance().RequestAction(QActionType.ReleaseFullScreenAuto);
                Controller.GetInstance().RequestAction(QActionType.UnlockControls);
                System.Diagnostics.Process.Start(URL);
            }
            catch { }
        }
        public static string Get(string URL)
        {
            string result = String.Empty;

            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URL);
                request.ServicePoint.Expect100Continue = false;
                WebResponse response = request.GetResponse();

                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        result = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
            return result.Trim();
        }
        public static string Post(string URL, string PostVals)
        {
            string result = String.Empty;
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URL);
                string post;
                using (TextWriter writer = new StringWriter())
                {
                    writer.Write(PostVals, HttpUtility.UrlEncode(PostVals));
                    post = writer.ToString();
                }
                Net.setRequestParams(request);
                using (Stream requestStream = request.GetRequestStream())
                {
                    using (StreamWriter writer = new StreamWriter(requestStream))
                    {
                        writer.Write(post);
                    }
                }
                WebResponse response = request.GetResponse();

                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        return reader.ReadToEnd().Trim();
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
            return String.Empty;
        }
        public static string GetUNIXTimestamp(DateTime Time)
        {
            TimeSpan ts = (Time - new DateTime(1970, 1, 1).ToLocalTime());
            string timeStamp = ((int)ts.TotalSeconds).ToString();
            return timeStamp;
        }
        public static string GetContentBetweenTags(string Input, string Start, string End)
        {
            if (!Input.Contains(Start))
                return string.Empty;

            int i = Input.IndexOf(Start) + Start.Length;

            string s;

            if ((s = Input.Substring(i)).Contains(End))
                s = s.Substring(0, s.IndexOf(End));

            return s.Trim();
        }
        public static string StripComments(string Input)
        {
            while (Input.Contains("<!--"))
            {
                int i = Input.IndexOf("<!--");
                int ii = Input.IndexOf("-->", i);
                if (ii > 0)
                    Input = Input.Substring(0, i) + Input.Substring(ii + 3);
                else
                    Input = Input.Substring(0, i);
            }
            return Input;
        }
        public static string ScrubHTMLNew(string Input)
        {
            Input = Input.Replace("<![CDATA[", String.Empty)
                         .Replace("]]>", String.Empty)
                         .Replace("\r ", Environment.NewLine)
                         .Replace("<br />", Environment.NewLine)
                         .Replace("<br>", Environment.NewLine);

            Input = HttpUtility.HtmlDecode(Input);

            Input = StripComments(Input);
            Input = Regex.Replace(Input, @"<[^>]*>", String.Empty).Trim();
            
            return Input;
        }
        public static string ScrubHTML(string Input)
        {
            string s = removeLinks(Input.Replace("<![CDATA[", String.Empty)
                        .Replace("]]>", String.Empty)
                        .Replace("&quot;", "\"")
                        .Replace("<em>", String.Empty)
                        .Replace("</em>", String.Empty)
                        .Replace("<strong>", String.Empty)
                        .Replace("</strong>", String.Empty)
                        .Replace("&ndash;", "-")
                        .Replace("&mdash;", "-")
                        .Replace("&amp;", "&")
                        .Replace("\r ", Environment.NewLine)
                        .Replace("<br />", Environment.NewLine)
                        .Replace("<br>", Environment.NewLine)
                        .Replace(" " + Environment.NewLine, Environment.NewLine)).Trim();

            while (s.Contains("<!--"))
            {
                int i = s.IndexOf("<!--");
                int ii;

                if ((ii = s.IndexOf("-->", i + 3)) > 0)
                    s = s.Substring(0, i) + s.Substring(ii + 3);
                else
                    break;
            }
            return s;
        }

        private static string removeLinks(string Input)
        {
            Input = removeLinks(Input, "a");
            Input = removeLinks(Input, "span");

            return Input;
        }
        private static string removeLinks(string Input, string Tag)
        {
            Input = Input.Replace("</" + Tag + ">", String.Empty);

            while (Input.Contains("<" + Tag))
            {
                int i = Input.IndexOf("<" + Tag);
                int j = Input.IndexOf(">", i + 1);

                if (j > 0)
                {
                    Input = Input.Substring(0, i) + Input.Substring(j + 1);
                }
                else
                {
                    Input = Input.Substring(0, i);
                }
            }
            return Input;
        }
        private static void setRequestParams(HttpWebRequest request)
        {
            request.Timeout = 10000;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Referer = "http://www.quuxplayer.com";
            request.UserAgent = "QuuxPlayer";
            request.ServicePoint.Expect100Continue = false;
#if USE_PROXY
          request.Proxy = new WebProxy("http://localhost:8080", false);
#endif
        }
    }
}
