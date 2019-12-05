/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal sealed class TrackQueue : IEnumerable<Track>
    {
        private static TrackQueue empty = new TrackQueue();

        private List<Track> queue;
        private int cursor = -1;
        private bool reordered = false;
        private string playlistBasis = String.Empty;

        private TrackQueue()
        {
            queue = new List<Track>();
            cursor = -1;
            PreSorted = false;
        }
        public TrackQueue(List<Track> SongQueue)
        {
            queue = SongQueue;
            PreSorted = false;
        }

        public static TrackQueue Empty
        {
            get { return empty; }
        }
        public bool PreSorted { get; set; }

        public Track this[int Index]
        {
            get
            {
                return queue[Index];
            }
        }

        public int CurrentIndex
        {
            get { return cursor; }
            set
            {
                cursor = Math.Max(-1, Math.Min(queue.Count - 1, value));
            }
        }
        public int Count
        {
            get { return queue.Count; }
        }
        public long TotalTime
        {
            get
            {
                try
                {
                    return queue.Sum(t => (long)t.Duration);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    return 0;
                }
            }
        }
        public long TotalSize
        {
            get
            {
                try
                {
                    return queue.Sum(t => t.FileSize);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    return 0;
                }
            }
        }
        public int RemainingTracks
        {
            get { return queue.Count - cursor - 1; }
        }
        public bool HasPrevious
        {
            get { return (cursor > 0); }
        }
        public bool Reordered
        {
            get { return reordered; }
            set { reordered = value; }
        }
        public string PlaylistBasis
        {
            get { return playlistBasis; }
            set { playlistBasis = value; }
        }

        public void Append(Track Track)
        {
            queue.Add(Track);
        }
        public void Remove(Track Track)
        {
            if (this.Contains(Track))
            {
                queue.Remove(Track);
                cursor = -1;
            }
        }
        //public bool Contains(Track Track)
        //{
        //    if (Track != null)
        //    {
        //        if (Track.ID > 0)
        //        {
        //            return queue.Exists(ti => ti.ID == Track.ID);
        //        }
        //        if (Track.FilePath.Length > 0)
        //        {
        //            return queue.Exists(ti => ti.FilePath == Track.FilePath);
        //        }
        //    }
        //    return false;
        //}
        public int IndexOf(Track Track)
        {
            //if (this.Contains(Track))
                return queue.IndexOf(Track);
            //else
            //    return -1;
        }
        public bool HasSelectedTracks
        {
            get { return queue.Exists(ti => ti.Selected); }
        }

        public List<Track> FindAll(Predicate<Track> P)
        {
            return queue.FindAll(P);
        }
        public void Clear()
        {
            if (queue.Count > 0)
            {
                queue.Clear();
                cursor = -1;
            }
            playlistBasis = String.Empty;
            reordered = false;
        }
        public Track Peek(bool Loop)
        {
            if (queue.Count == 0)
                return null;

            int curs = cursor;

            if (curs >= queue.Count - 1)
            {
                if (Loop)
                    curs = -1;
                else
                    return null;
            }

            curs++;

            if (!queue[curs].ConfirmExists)
                return null;
         
            return queue[curs];
        }
        public Track Advance(bool Loop)
        {
            if (queue.Count == 0)
                return null;

            if (cursor >= queue.Count - 1)
            {
                if (Loop)
                    cursor = 0;
                else
                    return null;
            }
            else
            {
                cursor++;
            }

            while (!this[cursor].ConfirmExists)
            {
                cursor++;
                if (cursor >= this.Count)
                    return null;
            }

            return queue[cursor];
        }
        public void Insert(int Index, Track Track)
        {
            queue.Insert(Index, Track);
            if (cursor >= Index)
                cursor++;
        }

        public Track CurrentTrack
        {
            get
            {
                if (this.Count == 0)
                    return null;
                else
                    return queue[Math.Max(0, Math.Min(this.Count - 1, cursor))];
            }
        }
        public Track GetPrevious()
        {
            if (!HasPrevious)
                return null;

            Track ti = queue[--cursor];

            return ti;
        }
        public void ClearSelectedItems()
        {
            List<Track> selectedTracks = queue.FindAll(t => t.Selected);

            foreach (Track ti in selectedTracks)
                ti.Selected = false;
        }

        public void Sort(Comparison<Track> Comparison)
        {
            Track ti = CurrentTrack;
            queue.Sort(Comparison);
            if (ti != null && cursor >= 0)
            {
                cursor = queue.IndexOf(ti);
            }
            reordered = true;
        }
        public void Shuffle(Track ForceFirst, bool OnlySeedRandomKey)
        {
            Random r = new Random();

            foreach (Track ti in queue)
                ti.RandomKey = r.Next();
            
            if (ForceFirst != null)
                ForceFirst.RandomKey = Int32.MinValue;
            
            if (!OnlySeedRandomKey)
                this.Sort((a, b) => a.RandomKey.CompareTo(b.RandomKey));
        }
        public void Swap(int Index1, int Index2)
        {
            if (Index1 >= 0 && Index2 >= 0 && Index1 < queue.Count && Index2 < queue.Count && Index1 != Index2)
            {
                Track ti = queue[Index1];
                queue[Index1] = queue[Index2];
                queue[Index2] = ti;
            }
            reordered = true;
        }

        public IEnumerator<Track> GetEnumerator()
        {
            foreach (Track TI in queue)
            {
                yield return TI;
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static implicit operator TrackQueue(List<Track> Tracks)
        {
            TrackQueue q = new TrackQueue();
            q.queue = Tracks;

            return q;
        }
        public static implicit operator List<Track>(TrackQueue Tracks)
        {
            return Tracks.queue;
        }
    }
}
