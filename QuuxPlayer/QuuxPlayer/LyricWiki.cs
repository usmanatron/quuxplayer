/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace QuuxPlayer
{
    internal sealed class LyricWiki : Lyrics
    {
        private const string URL = "http://lyrics.wikia.com/";

        private static Dictionary<string, string> lyrics = new Dictionary<string, string>();
        
        public void GetLyrics(Track Track, LyricsCallback Callback)
        {
            getLyricsURL(Track.Artist, Track.Title, Callback);
        }
        private void getLyricsURL(string Artist, string Title, LyricsCallback Callback)
        {
            if (lyrics.ContainsKey(Artist + Title))
            {
                string s = lyrics[Artist + Title];
                if (s == Net.FAILED_TOKEN)
                {
                    if (StringUtil.HasParentheticalChars(Title))
                        getLyricsURL(Artist, StringUtil.RemoveParentheticalChars(Title), Callback);
                    else
                        Callback(Net.FAILED_TOKEN);
                }
                else
                {
                    Callback(s);
                }
            }
            else
            {
                getLyricsImpl(Artist, Title, Callback);
            }
        }
        private void getLyricsImpl(string Artist, string Title, LyricsCallback Callback)
        {
            if (Artist.Length > 0 && Title.Length > 0)
            {
                string url = LyricWiki.URL + String.Format("{0}:{1}", HttpUtility.UrlEncode(Artist.Replace(' ', '_')), HttpUtility.UrlEncode(Title.Replace(' ', '_')));

                string result = Net.Get(url).Trim();

                if (result.Length == 0)
                {
                    result = Net.FAILED_TOKEN;
                }
                else if (result.StartsWith("not found", StringComparison.InvariantCultureIgnoreCase))
                {
                    result = Net.FAILED_TOKEN;
                }

                if (result != Net.FAILED_TOKEN)
                    lyrics.Add(Artist + Title, result);

                if (result == Net.FAILED_TOKEN)
                {
                    if (StringUtil.HasParentheticalChars(Title))
                        getLyricsURL(Artist, StringUtil.RemoveParentheticalChars(Title), Callback);
                    else
                        Callback(Net.FAILED_TOKEN);
                }
                else
                {
                    string s = extractLyrics(result);
                    
                    if (s == String.Empty)
                        s = Net.FAILED_TOKEN;
                    Callback(s);
                }
            }
            else
            {
                Callback(Net.FAILED_TOKEN);
            }
        }
        private static string extractLyrics(string Input)
        {
            Input = removeRTMatcherStuff(Input);

            string Start = "<div class='lyricbox'";
            string End = "</div>";

            int i = Input.IndexOf(Start);

            if (i < 0)
                return string.Empty;

            Input = Input.Substring(i + Start.Length);

            if (Input.Contains(End))
                Input = Input.Substring(0, Input.IndexOf(End));

            if (Input.StartsWith(">"))
                Input = Input.Substring(1);

            return Net.ScrubHTMLNew(Input);
        }
        private static string removeRTMatcherStuff(string Input)
        {
            const string pattern = "<div class='rtMatcher'>"; 
            const string endPattern = "</div>";
            while (Input.Contains(pattern))
            {
                int i = Input.IndexOf(pattern);
                int j = Input.IndexOf(endPattern, i);
                if (j > i)
                {
                    Input = Input.Substring(0, i) + Input.Substring(j + endPattern.Length);
                }
                else
                {
                    break;
                }
            }
            return Input;
        }
    }
}
