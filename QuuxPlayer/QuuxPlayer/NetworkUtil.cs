/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace QuuxPlayer
{
    internal static class NetworkUtil
    {
        private static Dictionary<string, ImageItem> images;
        private static Dictionary<string, AlbumInfo> albumInfoCache;
        private static int newLineLength;
        private static string threeNewLines = Environment.NewLine + Environment.NewLine + Environment.NewLine;
        private static string twoNewLines = Environment.NewLine + Environment.NewLine;
        
        static NetworkUtil()
        {
            images = new Dictionary<string, ImageItem>();
            albumInfoCache = new Dictionary<string, AlbumInfo>();
            newLineLength = Environment.NewLine.Length;
        }

        public static void RemoveNullAlbumCache()
        {
            Dictionary<string, ImageItem> img = new Dictionary<string, ImageItem>();

            foreach (KeyValuePair<string, ImageItem> kvp in images)
            {
                if (kvp.Value != null)
                    img.Add(kvp.Key, kvp.Value);
            }
            images = img;
        }
        private static string stripTags(string Input)
        {
            Input = Input.Replace("<BR>", twoNewLines)
                         .Replace("<P>", twoNewLines)
                         .Replace("<br>", twoNewLines)
                         .Replace("<p>", twoNewLines)
                         .Replace("  ", " ");

            return System.Text.RegularExpressions.Regex.Replace(Input, "<[^>]*>", String.Empty);
        }
        private static bool suppress(string Input)
        {
            return (Input.ToLowerInvariant().Contains("no ") && Input.ToLowerInvariant().Contains("available")) ||
                    Input.Trim().ToLowerInvariant() == "\\n";
        }
    }
}
