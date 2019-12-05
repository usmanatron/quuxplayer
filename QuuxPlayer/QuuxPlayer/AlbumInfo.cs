/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxPlayer
{
    internal sealed class AlbumInfo
    {
        public enum LoadStatus { Loading, Loaded }

        public string Album { get; set; }
        public string Artist { get; set; }
        public string Description { get; set; }
        public string URL { get; set; }
        public string Label { get; set; }
        public LoadStatus Status { get; set; }

        public DateTime ReleaseDate { get; set; }
        public decimal AvgRating { get; set; }
        public List<String> Tracks { get; set; }

        private static AlbumInfo empty;

        public AlbumInfo(string Artist, string Album)
        {
            this.Status = LoadStatus.Loading;

            this.Artist = Artist;
            this.Album = Album;
            Description = String.Empty;
            AvgRating = -1;
            Tracks = new List<string>();
            URL = String.Empty;
            ReleaseDate = DateTime.MaxValue;
            Label = String.Empty;
        }
        static AlbumInfo()
        {
            empty = new AlbumInfo(String.Empty, String.Empty);
        }
        public static AlbumInfo Empty
        {
            get { return empty; }
        }
        public string TrackList
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (string s in Tracks)
                    sb.AppendLine(s);

                return sb.ToString();
            }
        }
        public bool IsEmpty
        {
            get { return Artist.Length == 0 && Album.Length == 0; }
        }
    }
}
