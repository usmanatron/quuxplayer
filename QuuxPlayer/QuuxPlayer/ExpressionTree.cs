/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace QuuxPlayer
{
    internal sealed class ExpressionTree
    {
        public enum ValidationError { Valid, BadField, BadComparitor, PlaylistError, BadExpression, ShortExpression, UnmatchedParens, UnknownKeyword, BooleanValueError, NumericValueError, NumericComparatorError, BadSortByModifier, BadLimitToModifier, BadSelectByModifier, BadThenByModifier, ThenByWithoutSortByOrSelectBy, BadModifier, Unknown }
        public enum Field { Title, Artist, Album, AlbumArtist, Genre, TrackNum, DiskNum, Compilation, LengthInSeconds, LengthInMinutes, FileSizeInKB, FileSizeInMB, Grouping, Composer, Year, PlayCount, Rating, FilePath, BitRate, DaysSinceLastPlayed, FileAgeInDays, DaysSinceFileAdded, Encoder, FileType, Track, FileSize, Length, Random, Equalizer, SampleRate, Mono, None }
        public enum FieldType { String, Number, Boolean }
        public enum Comparator { Is, IsNot, LessThan, MoreThan, Contains, StartsWith, EndsWith, DoesNotStartWith, DoesNotEndWith, AtMost, AtLeast, ContainedIn, DoesNotContain, NotContainedIn }
        public enum ExpressionType { Atomic, And, Or, Nor, Trivial, None }

        private static readonly Dictionary<String, Field> fields = new Dictionary<string, Field>();
        private static readonly Dictionary<String, Comparator> comparators = new Dictionary<string, Comparator>();
        private static readonly Dictionary<String, ExpressionType> expressionTypes = new Dictionary<string, ExpressionType>();

        public static Dictionary<String, Comparator> Comparators
        {
            get { return comparators; }
        }
        public static Dictionary<String, Field> Fields
        {
            get { return fields; }
        }
        public static Dictionary<String, ExpressionType> ExpressionTypes
        {
            get { return expressionTypes; }
        }

        private List<string> words;
        private string text;
        private bool parsed = false;
        private bool valid = false;
        private static readonly char[] splitChars = new char[] { ' ' };

        private FilterExpression E;

        private ExpressionModifier em = null;

        private bool byalbum = false;

        public ExpressionTree(string Text)
        {
            text = Text;
        }

        private void parse()
        {
            if (parsed)
                return;

            em = new ExpressionModifier();

            text = text.Replace("\\\"", "{hardquote}");
            text = text.Replace("\"", " {quote} ").ToLowerInvariant();
            text = text.Replace("{hardquote}", "\""); 
            words = text.Split(splitChars, StringSplitOptions.RemoveEmptyEntries).ToList();

            bool quoting = false;
            int quoteStart = 0;
            for (int i = 0; i < words.Count; i++)
            {
                if (words[i] == "{quote}")
                {
                    quoting = !quoting;

                    if (quoting)
                        quoteStart = i;

                    words.RemoveAt(i--);

                }
                else if (quoting && i > quoteStart)
                {
                    words[i - 1] += (" " + words[i]);
                    words.RemoveAt(i--);
                }
                else if (!quoting)
                {
                    if (words[i].Contains('('))
                    {
                        i = isolateChar(words, i, '(');
                    }
                    else if (words[i].Contains(')'))
                    {
                        i = isolateChar(words, i, ')');
                    }
                }
            }
            parsed = true;
        }

        private int isolateChar(List<string> words, int i, char c)
        {
            int j;
            string s;
            while ((i < words.Count) && (j = words[i].IndexOf(c)) >= 0)
            {
                s = words[i];
                if (j == 0)
                {
                    words[i] = c.ToString();
                    if (s.Substring(1).Trim().Length > 0)
                        words.Insert(++i, s.Substring(1).Trim());
                    else
                        i++;
                }
                else
                {
                    words[i] = s.Substring(0, j);
                    words.Insert(++i, c.ToString());
                    if (s.Substring(j + 1).Trim().Length > 0)
                        words.Insert(++i, s.Substring(j + 1));
                    else
                        i++;
                }
            }
            return i;
        }
        public ValidationError Compile()
        {
            List<string> w;
            return Compile(out w);
        }
        public ValidationError Compile(out List<string> Words)
        {
            parse();

            Words = words.ToList();

            if (words.Count > 0)
            {
                switch (words[0])
                {
                    case "byalbum":
                        byalbum = true;
                        words.RemoveAt(0);
                        break;
                    case "bytrack":
                        words.RemoveAt(0);
                        break;
                }
            }

            em = ExpressionModifier.GetModifiers(words);

            ValidationError ve = em.Error;

            if (ve == ValidationError.Valid)
            {
                if (words.Count == 0)
                    E = FilterExpression.Trivial;
                else
                    E = FilterExpression.GetExpression(words, out ve);
            }
            
            if (ve == ValidationError.Valid)
                ve = E.Check();

            valid = (ve == ValidationError.Valid);

            return ve;
        }
        private static readonly string[] splitLineChars = new string[] { Environment.NewLine, "\t" };
        public static string CleanExpression(string Input)
        {
            List<string> exp = Input.Split(splitLineChars, StringSplitOptions.RemoveEmptyEntries).ToList();
            for (int i = 0; i < exp.Count; i++)
            {
                if (exp[i].Contains("//"))
                    exp[i] = exp[i].Substring(0, exp[i].IndexOf("//"));
                exp[i] = exp[i].Trim();
                if (exp[i].Length == 0)
                    exp.RemoveAt(i--);
            }
            return String.Join(" ", exp.ToArray());
        }
        public List<Track> Filter(List<Track> Tracks, out bool Sorted)
        {
            Sorted = false;
            bool limitSelectedBySort = false;

            if (valid)
            {
                try
                {
                    IEnumerable<Track> tracks = E.Evaluate(Tracks);

                    if (byalbum)
                    {
                        var v = (from t in tracks
                                 where t.Album.Length > 0
                                 select t.Album).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                        tracks = Tracks.FindAll(t => v.Contains(t.Album));

                        if (em.HasRandomClause)
                        {
                            Dictionary<string, int> keys = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
                            Random r = new Random();

                            foreach (string s in v)
                                keys.Add(s, r.Next());

                            foreach (Track ti in tracks)
                                ti.RandomKey = keys[ti.Album];
                        }
                    }
                    else if (em.HasRandomClause)
                    {
                        TrackQueue tq = tracks.ToList();
                        tq.Shuffle(null, true);
                    }

                    if (em.LimitTo != ExpressionModifier.LimitType.None)
                    {
                        if (em.SelectBy != null)
                        {
                            tracks = sortTracks(tracks, em.SelectBy, em.SelectThenBy1, em.SelectThenBy2, em.SelectThenBy3);
                            Sorted = true;
                        }
                        else if (em.SortBy != null)
                        {
                            tracks = sortTracks(tracks, em.SortBy, em.SortThenBy1, em.SortThenBy2, em.SortThenBy3);
                            limitSelectedBySort = true;
                            Sorted = true;
                        }
                        tracks = limitTracks(tracks);
                    }
                    
                    if (!limitSelectedBySort && em.SortBy != null)
                    {
                        tracks = sortTracks(tracks, em.SortBy, em.SortThenBy1, em.SortThenBy2, em.SortThenBy3);
                        Sorted = true;
                    }
                    else if (!Sorted && em.SelectBy != null)
                    {
                        tracks = sortTracks(tracks, em.SelectBy, em.SelectThenBy1, em.SelectThenBy2, em.SelectThenBy3);
                        Sorted = true;
                    }
                    return tracks.ToList();
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.ToString());
                    return new List<Track>();
                }
            }
            else
            {
                return new List<Track>();
            }
        }

        private IEnumerable<Track> limitTracks(IEnumerable<Track> tracks)
        {
            long sum = 0;
            long lim;
            switch (em.LimitTo)
            {
                case ExpressionModifier.LimitType.Tracks:
                    tracks = tracks.Take(em.LimitAmount);
                    break;
                case ExpressionModifier.LimitType.Gigabytes:
                    lim = (long)em.LimitAmount * 1024 * 1024 * 1024;
                    tracks = tracks.TakeWhile(t => (sum = (sum + t.FileSize)) < lim).ToList();
                    break;
                case ExpressionModifier.LimitType.Megabytes:
                    lim = (long)em.LimitAmount * 1024 * 1024;
                    tracks = tracks.TakeWhile(t => (sum = (sum + t.FileSize)) < lim).ToList();
                    break;
                case ExpressionModifier.LimitType.Kilobytes:
                    lim = (long)em.LimitAmount * 1024;
                    tracks = tracks.TakeWhile(t => (sum = (sum + t.FileSize)) < lim).ToList();
                    break;
                case ExpressionModifier.LimitType.Days:
                    lim = (long)em.LimitAmount * 24 * 60 * 60 * 1000;
                    tracks = tracks.TakeWhile(t => (sum = (sum + t.Duration)) < lim).ToList();
                    break;
                case ExpressionModifier.LimitType.Hours:
                    lim = (long)em.LimitAmount * 60 * 60 * 1000;
                    tracks = tracks.TakeWhile(t => (sum = (sum + t.Duration)) < lim).ToList();
                    break;
                case ExpressionModifier.LimitType.Minutes:
                    lim = (long)em.LimitAmount * 60 * 1000;
                    tracks = tracks.TakeWhile(t => (sum = (sum + t.Duration)) < lim).ToList();
                    break;
                case ExpressionModifier.LimitType.Seconds:
                    lim = (long)em.LimitAmount * 1000;
                    tracks = tracks.TakeWhile(t => (sum = (sum + t.Duration)) < lim).ToList();
                    break;
            }
            return tracks;
        }

        private static IEnumerable<Track> sortTracks(IEnumerable<Track> Tracks, Prioritization P1, Prioritization P2, Prioritization P3, Prioritization P4)
        {
            if (P1 == null)
                return Tracks;


            //if (P1.Field == Field.Random || P2.Field == Field.Random || P3.Field == Field.Random || P4.Field == Field.Random)
            //{
            //    TrackQueue tq = Tracks.ToList();
            //    tq.Shuffle(null, true);
            //}

            IEnumerable<Track> tracks = Tracks;

            // ONE PRIORITIZATION

            if (P2 == null)
            {
                if (P1.Order == Order.Ascending)
                    return tracks.OrderBy(ExpressionModifier.GetLambda(P1.Field));
                else
                    return tracks.OrderByDescending(ExpressionModifier.GetLambda(P1.Field));
            }

            // TWO PRIORITIZATIONS

            if (P3 == null)
            {
                if (P1.Order == Order.Ascending)
                {
                    if (P2.Order == Order.Ascending)
                        return tracks.OrderBy(ExpressionModifier.GetLambda(P1.Field)).
                            ThenBy(ExpressionModifier.GetLambda(P2.Field)).ToList();
                    else
                        return tracks.OrderBy(ExpressionModifier.GetLambda(P1.Field)).
                            ThenByDescending(ExpressionModifier.GetLambda(P2.Field));
                }
                else
                {
                    if (P2.Order == Order.Ascending)
                        return tracks.OrderByDescending(ExpressionModifier.GetLambda(P1.Field)).
                            ThenBy(ExpressionModifier.GetLambda(P2.Field));
                    else
                        return tracks.OrderByDescending(ExpressionModifier.GetLambda(P1.Field)).
                            ThenByDescending(ExpressionModifier.GetLambda(P2.Field));
                }
            }

            // THREE PRIORITIZATIONS

            if (P4 == null)
            {
                if (P1.Order == Order.Ascending)
                {
                    if (P2.Order == Order.Ascending)
                    {
                        if (P3.Order == Order.Ascending)
                        {
                            return tracks.OrderBy(ExpressionModifier.GetLambda(P1.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P2.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P3.Field));
                        }
                        else
                        {
                            return tracks.OrderBy(ExpressionModifier.GetLambda(P1.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P2.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P3.Field));
                        }
                    }
                    else
                    {
                        if (P3.Order == Order.Ascending)
                        {
                            return tracks.OrderBy(ExpressionModifier.GetLambda(P1.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P2.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P3.Field));
                        }
                        else
                        {
                            return tracks.OrderBy(ExpressionModifier.GetLambda(P1.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P2.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P3.Field));
                        }
                    }
                }
                else
                {
                    if (P2.Order == Order.Ascending)
                    {
                        if (P3.Order == Order.Ascending)
                        {
                            return tracks.OrderByDescending(ExpressionModifier.GetLambda(P1.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P2.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P3.Field));
                        }
                        else
                        {
                            return tracks.OrderByDescending(ExpressionModifier.GetLambda(P1.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P2.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P3.Field));
                        }
                    }
                    else
                    {
                        if (P3.Order == Order.Ascending)
                        {
                            return tracks.OrderByDescending(ExpressionModifier.GetLambda(P1.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P2.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P3.Field));
                        }
                        else
                        {
                            return tracks.OrderByDescending(ExpressionModifier.GetLambda(P1.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P2.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P3.Field));
                        }
                    }
                }
            }
            
            // FOUR PRIORITIZATIONS

            if (P1.Order == Order.Ascending)
            {
                if (P2.Order == Order.Ascending)
                {
                    if (P3.Order == Order.Ascending)
                    {
                        if (P4.Order == Order.Ascending)
                        {
                            return tracks.OrderBy(ExpressionModifier.GetLambda(P1.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P2.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P3.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P4.Field));
                        }
                        else
                        {
                            return tracks.OrderBy(ExpressionModifier.GetLambda(P1.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P2.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P3.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P4.Field));
                        }
                    }
                    else
                    {
                        if (P4.Order == Order.Ascending)
                        {
                            return tracks.OrderBy(ExpressionModifier.GetLambda(P1.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P2.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P3.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P4.Field));
                        }
                        else
                        {
                            return tracks.OrderBy(ExpressionModifier.GetLambda(P1.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P2.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P3.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P4.Field));
                        }
                    }
                }
                else
                {
                    if (P3.Order == Order.Ascending)
                    {
                        if (P4.Order == Order.Ascending)
                        {
                            return tracks.OrderBy(ExpressionModifier.GetLambda(P1.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P2.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P3.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P4.Field));
                        }
                        else
                        {
                            return tracks.OrderBy(ExpressionModifier.GetLambda(P1.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P2.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P3.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P4.Field));
                        }
                    }
                    else
                    {
                        if (P4.Order == Order.Ascending)
                        {
                            return tracks.OrderBy(ExpressionModifier.GetLambda(P1.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P2.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P3.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P4.Field));
                        }
                        else
                        {
                            return tracks.OrderBy(ExpressionModifier.GetLambda(P1.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P2.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P3.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P4.Field));
                        }
                    }
                }
            }
            else
            {
                if (P2.Order == Order.Ascending)
                {
                    if (P3.Order == Order.Ascending)
                    {
                        if (P4.Order == Order.Ascending)
                        {
                            return tracks.OrderByDescending(ExpressionModifier.GetLambda(P1.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P2.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P3.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P4.Field));
                        }
                        else
                        {
                            return tracks.OrderByDescending(ExpressionModifier.GetLambda(P1.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P2.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P3.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P4.Field));
                        }
                    }
                    else
                    {
                        if (P4.Order == Order.Ascending)
                        {
                            return tracks.OrderByDescending(ExpressionModifier.GetLambda(P1.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P2.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P3.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P4.Field));
                        }
                        else
                        {
                            return tracks.OrderByDescending(ExpressionModifier.GetLambda(P1.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P2.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P3.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P4.Field));
                        }
                    }
                }
                else
                {
                    if (P3.Order == Order.Ascending)
                    {
                        if (P4.Order == Order.Ascending)
                        {
                            return tracks.OrderByDescending(ExpressionModifier.GetLambda(P1.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P2.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P3.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P4.Field));
                        }
                        else
                        {
                            return tracks.OrderByDescending(ExpressionModifier.GetLambda(P1.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P2.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P3.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P4.Field));
                        }
                    }
                    else
                    {
                        if (P4.Order == Order.Ascending)
                        {
                            return tracks.OrderByDescending(ExpressionModifier.GetLambda(P1.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P2.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P3.Field)).
                                ThenBy(ExpressionModifier.GetLambda(P4.Field));
                        }
                        else
                        {
                            return tracks.OrderByDescending(ExpressionModifier.GetLambda(P1.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P2.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P3.Field)).
                                ThenByDescending(ExpressionModifier.GetLambda(P4.Field));
                        }
                    }
                }
            }
        }

        private enum Order { None, Ascending, Descending }
        private class Prioritization
        {
            public ExpressionTree.Field Field { get; set; }
            private Order order;
            public bool Inverse { get; set; }
            public Prioritization()
            {
                Field = Field.None;
                order = Order.None;
                Inverse = false;
            }

            public Order Order
            {
                set { order = value; }
                get
                {
                    if (Inverse)
                    {
                        return (order == Order.Ascending) ? Order.Descending : Order.Ascending;
                    }
                    else
                    {
                        return order;
                    }
                }
            }
        }
        private class ExpressionModifier
        {

            public enum LimitType { None, Tracks, Kilobytes, Megabytes, Gigabytes, Days, Hours, Minutes, Seconds }

            public Prioritization SortBy { get; set; }
            public Prioritization SelectBy { get; private set; }

            public LimitType LimitTo { get; private set; }
            public int LimitAmount { get; private set; }

            public ValidationError Error { get; private set; }

            public Prioritization SortThenBy1 { get; set; }
            public Prioritization SortThenBy2 { get; set; }
            public Prioritization SortThenBy3 { get; set; }
            public Prioritization SelectThenBy1 { get; set; }
            public Prioritization SelectThenBy2 { get; set; }
            public Prioritization SelectThenBy3 { get; set; }

            public bool HasRandomClause { get; private set;  }

            private static Dictionary<string, LimitType> limitTypes;

            private static Dictionary<ExpressionTree.Field, Func<Track, object>> sorters;

            public ExpressionModifier()
            {
                System.Diagnostics.Debug.WriteLine(sorters[Field.TrackNum].Method.ToString());

                SortBy = null;
                SelectBy = null;
                SortThenBy1 = null;
                SortThenBy2 = null;
                SortThenBy3 = null;
                SelectThenBy1 = null;
                SelectThenBy2 = null;
                SelectThenBy3 = null;

                LimitTo = LimitType.None;

                Error = ValidationError.Valid;
                LimitAmount = 0;
                HasRandomClause = false;

            }
            static ExpressionModifier()
            {
                limitTypes = new Dictionary<string, LimitType>();
                limitTypes.Add("tracks", LimitType.Tracks);
                limitTypes.Add("kilobytes", LimitType.Kilobytes);
                limitTypes.Add("megabytes", LimitType.Megabytes);
                limitTypes.Add("gigabytes", LimitType.Gigabytes);
                limitTypes.Add("days", LimitType.Days);
                limitTypes.Add("hours", LimitType.Hours);
                limitTypes.Add("minutes", LimitType.Minutes);
                limitTypes.Add("seconds", LimitType.Seconds);

                sorters = new Dictionary<Field, Func<Track, object>>();

                sorters.Add(Field.Album, t => t.Album);
                sorters.Add(Field.AlbumArtist, t => t.Album);
                sorters.Add(Field.Artist, t => t.Artist);
                sorters.Add(Field.BitRate, t => t.Bitrate);
                sorters.Add(Field.SampleRate, t => t.SampleRate);
                sorters.Add(Field.Composer, t => t.Composer);
                sorters.Add(Field.DaysSinceFileAdded, t => t.DaysSinceFileAddedDouble);
                sorters.Add(Field.DaysSinceLastPlayed, t => t.DaysSinceLastPlayedDouble);
                sorters.Add(Field.DiskNum, t => t.DiskNum);
                sorters.Add(Field.Encoder, t => t.Encoder);
                sorters.Add(Field.Equalizer, t => t.EqualizerString);
                sorters.Add(Field.FileAgeInDays, t => t.FileAgeInDaysDouble);
                sorters.Add(Field.FilePath, t => t.FilePath);
                sorters.Add(Field.FileSize, t => t.FileSize);
                sorters.Add(Field.FileType, t => t.Type);
                sorters.Add(Field.Genre, t => t.Genre);
                sorters.Add(Field.Grouping, t => t.Grouping);
                sorters.Add(Field.Length, t => t.Duration);
                sorters.Add(Field.PlayCount, t => t.PlayCount);
                sorters.Add(Field.Rating, t => t.Rating);
                sorters.Add(Field.Title, t => t.Title);
                sorters.Add(Field.TrackNum, t => t.TrackNum);
                sorters.Add(Field.Year, t => t.Year);
                sorters.Add(Field.Random, t => t.RandomKey);
            }
            public static Func<Track, object> GetLambda(ExpressionTree.Field Field)
            {
                return sorters[Field];
            }
            public static ExpressionModifier GetModifiers(List<string> Words)
            {
                ExpressionModifier em = new ExpressionModifier();

                int emFound = -1;
                bool latestWasSort = false;

                Prioritization p;
                for (int i = 0; i < Words.Count; )
                {
                    System.Diagnostics.Debug.Assert(Words[i] == Words[i].ToLowerInvariant());
                    switch (Words[i])
                    {
                        case "sortby":
                            if (emFound < 0)
                                emFound = i;
                            p = new Prioritization();
                            if (!getPrioritization(Words, ref i, p))
                            {
                                em.Error = ValidationError.BadSortByModifier;
                                return em;
                            }
                            em.SortBy = p;
                            em.HasRandomClause |= p.Field == Field.Random;
                            latestWasSort = true;
                            i += 2;
                            break;
                        case "limitto":
                            if (emFound < 0)
                                emFound = i;

                            if (Words.Count < i + 3)
                            {
                                em.Error = ValidationError.BadLimitToModifier;
                                return em;
                            }
                            int limVal = 0;
                            Int32.TryParse(Words[i + 1], out limVal);
                            if (limVal == 0)
                            {
                                em.Error = ValidationError.BadLimitToModifier;
                                return em;
                            }
                            if (!limitTypes.ContainsKey(Words[i + 2].ToLowerInvariant()))
                            {
                                em.Error = ValidationError.BadLimitToModifier;
                                return em;
                            }
                            em.LimitTo = limitTypes[Words[i + 2].ToLowerInvariant()];
                            em.LimitAmount = limVal;
                            i += 3;
                            break;
                        case "selectby":
                            if (emFound < 0)
                                emFound = i;
                            p = new Prioritization();
                            if (!getPrioritization(Words, ref i, p))
                            {
                                em.Error = ValidationError.BadSortByModifier;
                                return em;
                            }
                            em.SelectBy = p;
                            em.HasRandomClause |= p.Field == Field.Random;
                            latestWasSort = false;
                            i += 2;
                            break;
                        case "thenby":
                            if (em.SortBy == null && em.SelectBy == null)
                            {
                                em.Error = ValidationError.ThenByWithoutSortByOrSelectBy;
                                return em;
                            }


                            p = new Prioritization();
                            if (!getPrioritization(Words, ref i, p))
                            {
                                em.Error = ValidationError.BadThenByModifier;
                                return em;
                            }
                            em.HasRandomClause |= p.Field == Field.Random;
                            if (latestWasSort)
                            {
                                if (em.SortThenBy1 == null)
                                    em.SortThenBy1 = p;
                                else if (em.SortThenBy2 == null)
                                    em.SortThenBy2 = p;
                                else if (em.SortThenBy3 == null)
                                    em.SortThenBy3 = p;
                            }
                            else
                            {
                                if (em.SelectThenBy1 == null)
                                    em.SelectThenBy1 = p;
                                else if (em.SelectThenBy2 == null)
                                    em.SelectThenBy2 = p;
                                else if (em.SelectThenBy3 == null)
                                    em.SelectThenBy3 = p;
                            }
                            i += 2;
                            break;
                        default:
                            if (emFound >= 0)
                            {
                                em.Error = ValidationError.BadModifier;
                                return em;
                            }
                            i++;
                            break;
                    }
                }
                if (emFound >= 0)
                    Words.RemoveRange(emFound, Words.Count - emFound);

                return em;
            }

            private static bool getPrioritization(List<string> Words, ref int i, Prioritization p)
            {
                if ((Words.Count < i + 2) ||
                    (!fields.ContainsKey(Words[i + 1].ToLowerInvariant())))
                {
                    return false;
                }
                p.Field = fields[Words[i + 1].ToLowerInvariant()];
                if (p.Field == Field.Compilation || p.Field == Field.Track || p.Field == Field.Mono)
                {
                    return false;
                }
                p.Order = Order.Ascending;
                if (Words.Count > i + 2)
                {
                    switch (Words[i + 2].ToLowerInvariant())
                    {
                        case "descending":
                            p.Order = Order.Descending;
                            i++;
                            break;
                        case "ascending":
                            p.Order = Order.Ascending;
                            i++;
                            break;
                        default:
                            break;
                    }
                }
                
                return true;
            }
        }
        private class FilterExpression
        {
            private delegate bool Validate(Track T);

            private ExpressionType type;
            private FilterExpression a;
            private FilterExpression b;

            public static FilterExpression Trivial;

            private string fieldString;
            private string comparatorString;
            private string valueString;

            private Field field;
            private Comparator comparator;

            private int numberValue;
            private bool boolValue;

            private bool parsed = false;

            private Func<List<Track>, List<Track>> validator;

            private static Dictionary<Comparator, Func<string, string, bool>> stringComparitors = new Dictionary<Comparator, Func<string, string, bool>>();
            private static Dictionary<Comparator, Func<int, int, bool>> numberComparitors = new Dictionary<Comparator, Func<int, int, bool>>();
            private static Dictionary<Comparator, Func<bool, bool, bool>> boolComparitors = new Dictionary<Comparator, Func<bool, bool, bool>>();
            private static Dictionary<Comparator, Func<List<Track>, string, List<Track>>> playlistComparitors = new Dictionary<Comparator, Func<List<Track>, string, List<Track>>>();

            private static Controller controller;

            private FilterExpression()
            {
                this.type = ExpressionType.Trivial;
            }
            private FilterExpression(string Field, string Comparator, string Value)
            {
                this.fieldString = Field;
                this.comparatorString = Comparator;
                this.valueString = (Value == "blank") ? String.Empty : Value;
                this.type = ExpressionType.Atomic;
                this.a = null;
                this.b = null;
            }

            private FilterExpression(ExpressionType Type, FilterExpression A, FilterExpression B)
            {
                this.type = Type;
                this.a = A;
                this.b = B;
            }

            public static FilterExpression GetExpression(List<String> Words, out ValidationError VE)
            {
                FilterExpression E = null;
                FilterExpression e1;
                int i = 0;
                ExpressionType t = ExpressionType.None;

                if (Words.Count < 3)
                {
                    VE = ValidationError.ShortExpression;
                    return null;
                }

                while (Words.Count > i + 2)
                {
                    if (Words[i] == "(")
                    {
                        int j = getMatchingParen(Words, i);

                        if (j > 0)
                        {
                            ValidationError ve;
                            e1 = GetExpression(Words.GetRange(i + 1, j - i - 1), out ve);
                            if (ve != ValidationError.Valid)
                            {
                                VE = ve;
                                return null;
                            }

                            i = j + 1;
                        }
                        else
                        {
                            VE = ValidationError.UnmatchedParens;
                            return null;
                        }
                    }
                    else
                    {
                        e1 = new FilterExpression(Words[i], Words[i + 1], Words[i + 2]);
                        i += 3;
                    }

                    if (e1 == null)
                    {
                        VE = ValidationError.Unknown;
                        return null;
                    }

                    if (t == ExpressionType.None)
                        E = e1;
                    else
                        E = new FilterExpression(t, E, e1);

                    t = ExpressionType.None;

                    if (Words.Count > i)
                    {
                        if (expressionTypes.ContainsKey(Words[i]))
                        {
                            t = expressionTypes[Words[i++]];
                        }
                        else
                        {
                            VE = ValidationError.UnknownKeyword;
                            return null;
                        }
                    }
                }
                if (t != ExpressionType.None)
                {
                    VE = ValidationError.ShortExpression;
                    return null;
                }
                else
                {
                    VE = ValidationError.Valid;
                    return E;
                }
            }
            private static int getMatchingParen(List<String> words, int Start)
            {
                int stack = 1;

                do
                {
                    Start++;
                    if (words[Start] == "(")
                        stack++;
                    else if (words[Start] == ")")
                        stack--;
                }
                while ((words.Count > (Start + 1)) && stack > 0);

                if (Start >= words.Count)
                    return -1;
                else
                    return Start;
            }
            static FilterExpression()
            {
                controller = Controller.GetInstance();

                stringComparitors.Add(Comparator.Is, new Func<string, string, bool>((s1, s2) => s1 == s2));
                stringComparitors.Add(Comparator.IsNot, new Func<string, string, bool>((s1, s2) => s1 != s2));
                stringComparitors.Add(Comparator.LessThan, new Func<string, string, bool>((s1, s2) => s1.CompareTo(s2) < 0));
                stringComparitors.Add(Comparator.MoreThan, new Func<string, string, bool>((s1, s2) => s1.CompareTo(s2) > 0));
                stringComparitors.Add(Comparator.Contains, new Func<string, string, bool>((s1, s2) => s1.Contains(s2)));
                stringComparitors.Add(Comparator.StartsWith, new Func<string, string, bool>((s1, s2) => s1.StartsWith(s2, StringComparison.OrdinalIgnoreCase)));
                stringComparitors.Add(Comparator.EndsWith, new Func<string, string, bool>((s1, s2) => s1.EndsWith(s2, StringComparison.OrdinalIgnoreCase)));
                stringComparitors.Add(Comparator.DoesNotStartWith, new Func<string, string, bool>((s1, s2) => !s1.StartsWith(s2, StringComparison.OrdinalIgnoreCase)));
                stringComparitors.Add(Comparator.DoesNotEndWith, new Func<string, string, bool>((s1, s2) => !s1.EndsWith(s2, StringComparison.OrdinalIgnoreCase)));
                stringComparitors.Add(Comparator.AtMost, new Func<string, string, bool>((s1, s2) => s1.CompareTo(s2) <= 0));
                stringComparitors.Add(Comparator.AtLeast, new Func<string, string, bool>((s1, s2) => s1.CompareTo(s2) >= 0));
                stringComparitors.Add(Comparator.DoesNotContain, new Func<string, string, bool>((s1, s2) => !s1.Contains(s2)));
                stringComparitors.Add(Comparator.ContainedIn, new Func<string, string, bool>((s1, s2) => s2.Contains(s1)));
                stringComparitors.Add(Comparator.NotContainedIn, new Func<string, string, bool>((s1, s2) => !s2.Contains(s1)));

                numberComparitors.Add(Comparator.Is, new Func<int, int, bool>((n1, n2) => n1 == n2));
                numberComparitors.Add(Comparator.IsNot, new Func<int, int, bool>((n1, n2) => n1 != n2));
                numberComparitors.Add(Comparator.LessThan, new Func<int, int, bool>((n1, n2) => n1 < n2));
                numberComparitors.Add(Comparator.MoreThan, new Func<int, int, bool>((n1, n2) => n1 > n2));
                numberComparitors.Add(Comparator.Contains, new Func<int, int, bool>((n1, n2) => false));
                numberComparitors.Add(Comparator.StartsWith, new Func<int, int, bool>((n1, n2) => false));
                numberComparitors.Add(Comparator.EndsWith, new Func<int, int, bool>((n1, n2) => false));
                numberComparitors.Add(Comparator.DoesNotStartWith, new Func<int, int, bool>((n1, n2) => false));
                numberComparitors.Add(Comparator.DoesNotEndWith, new Func<int, int, bool>((n1, n2) => false));
                numberComparitors.Add(Comparator.AtMost, new Func<int, int, bool>((n1, n2) => n1 <= n2));
                numberComparitors.Add(Comparator.AtLeast, new Func<int, int, bool>((n1, n2) => n1 >= n2));
                numberComparitors.Add(Comparator.DoesNotContain, new Func<int, int, bool>((n1, n2) => false));
                numberComparitors.Add(Comparator.ContainedIn, new Func<int, int, bool>((n1, n2) => false));
                numberComparitors.Add(Comparator.NotContainedIn, new Func<int, int, bool>((n1, n2) => false));

                boolComparitors.Add(Comparator.Is, new Func<bool, bool, bool>((b1, b2) => b1 == b2));
                boolComparitors.Add(Comparator.IsNot, new Func<bool, bool, bool>((b1, b2) => b1 != b2));
                boolComparitors.Add(Comparator.LessThan, new Func<bool, bool, bool>((b1, b2) => false));
                boolComparitors.Add(Comparator.MoreThan, new Func<bool, bool, bool>((b1, b2) => false));
                boolComparitors.Add(Comparator.Contains, new Func<bool, bool, bool>((b1, b2) => false));
                boolComparitors.Add(Comparator.StartsWith, new Func<bool, bool, bool>((b1, b2) => false));
                boolComparitors.Add(Comparator.EndsWith, new Func<bool, bool, bool>((b1, b2) => false));
                boolComparitors.Add(Comparator.DoesNotStartWith, new Func<bool, bool, bool>((b1, b2) => false));
                boolComparitors.Add(Comparator.DoesNotEndWith, new Func<bool, bool, bool>((b1, b2) => false));
                boolComparitors.Add(Comparator.AtMost, new Func<bool, bool, bool>((b1, b2) => false));
                boolComparitors.Add(Comparator.AtLeast, new Func<bool, bool, bool>((b1, b2) => false));
                boolComparitors.Add(Comparator.DoesNotContain, new Func<bool, bool, bool>((b1, b2) => false));
                boolComparitors.Add(Comparator.ContainedIn, new Func<bool, bool, bool>((b1, b2) => false));
                boolComparitors.Add(Comparator.NotContainedIn, new Func<bool, bool, bool>((b1, b2) => false));

                playlistComparitors.Add(Comparator.Is, new Func<List<Track>, string, List<Track>>((lt, s) => new List<Track>()));
                playlistComparitors.Add(Comparator.IsNot, new Func<List<Track>, string, List<Track>>((lt, s) => new List<Track>()));
                playlistComparitors.Add(Comparator.LessThan, new Func<List<Track>, string, List<Track>>((lt, s) => new List<Track>()));
                playlistComparitors.Add(Comparator.MoreThan, new Func<List<Track>, string, List<Track>>((lt, s) => new List<Track>()));
                playlistComparitors.Add(Comparator.Contains, new Func<List<Track>, string, List<Track>>((lt, s) => new List<Track>()));
                playlistComparitors.Add(Comparator.StartsWith, new Func<List<Track>, string, List<Track>>((lt, s) => new List<Track>()));
                playlistComparitors.Add(Comparator.EndsWith, new Func<List<Track>, string, List<Track>>((lt, s) => new List<Track>()));
                playlistComparitors.Add(Comparator.DoesNotStartWith, new Func<List<Track>,string,List<Track>>((lt, s) => new List<Track>()));
                playlistComparitors.Add(Comparator.DoesNotEndWith, new Func<List<Track>,string,List<Track>>((lt, s) => new List<Track>()));
                playlistComparitors.Add(Comparator.AtMost, new Func<List<Track>, string, List<Track>>((lt, s) => new List<Track>()));
                playlistComparitors.Add(Comparator.AtLeast, new Func<List<Track>, string, List<Track>>((lt, s) => new List<Track>()));
                playlistComparitors.Add(Comparator.DoesNotContain, new Func<List<Track>, string, List<Track>>((lt, s) => new List<Track>()));
                playlistComparitors.Add(Comparator.ContainedIn, new Func<List<Track>, string, List<Track>>((lt, s) => Database.GetPlaylistTracks(s).Intersect(lt).ToList()));
                playlistComparitors.Add(Comparator.NotContainedIn, new Func<List<Track>, string, List<Track>>((lt, s) => lt.Except(Database.GetPlaylistTracks(s)).ToList()));

                Trivial = new FilterExpression();
            }

            public static Controller Controller
            {
                set { controller = value; }
            }

            public ValidationError Check()
            {
                switch (type)
                {
                    case ExpressionType.Atomic:

                        if (!fields.Keys.Contains(fieldString))
                            return ValidationError.BadField;

                        if (!comparators.Keys.Contains(comparatorString))
                            return ValidationError.BadComparitor;

                        if (fields[fieldString] == Field.Random)
                            return ValidationError.BadField;

                        if (fields[fieldString] == Field.None)
                            return ValidationError.BadField;

                        return Parse();
                    case ExpressionType.Trivial:
                        return ValidationError.Valid;
                    default:
                        ValidationError v = a.Check();
                        if (v != ValidationError.Valid)
                            return v;

                        return b.Check();
                }
            }
            private ValidationError Parse()
            {
                if (parsed)
                {
                    return ValidationError.Valid;
                }

                field = fields[fieldString];
                comparator = comparators[comparatorString];

                switch (field)
                {
                    case Field.Compilation:
                    case Field.Mono:
                        switch (valueString)
                        {
                            case "true":
                                boolValue = true;
                                break;
                            case "false":
                                boolValue = false;
                                break;
                            default:
                                return ValidationError.BooleanValueError;
                        }
                        break;
                    case Field.DiskNum:
                    case Field.FileSize:
                    case Field.FileSizeInKB:
                    case Field.FileSizeInMB:
                    case Field.Length:
                    case Field.LengthInMinutes:
                    case Field.LengthInSeconds:
                    case Field.TrackNum:
                    case Field.Year:
                    case Field.PlayCount:
                    case Field.Rating:
                    case Field.BitRate:
                    case Field.SampleRate:
                    case Field.DaysSinceLastPlayed:
                    case Field.FileAgeInDays:
                    case Field.DaysSinceFileAdded:
                        if (!Int32.TryParse(valueString, out numberValue))
                            return ValidationError.NumericValueError;
                        if (comparator != Comparator.Is && comparator != Comparator.IsNot && comparator != Comparator.LessThan && comparator != Comparator.MoreThan && comparator != Comparator.AtMost && comparator != Comparator.AtLeast)
                            return ValidationError.NumericComparatorError;
                        break;
                    case Field.Track:
                        if (comparator != Comparator.ContainedIn && comparator != Comparator.NotContainedIn)
                            return ValidationError.PlaylistError;
                        break;
                    case Field.Random:
                        return ValidationError.BadField;
                }

                switch (field)
                {
                    case Field.Album:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => stringComparitors[comparator](tt.Album.ToLowerInvariant(), valueString)));
                        break;
                    case Field.AlbumArtist:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => stringComparitors[comparator](tt.AlbumArtist.ToLowerInvariant(), valueString)));
                        break;
                    case Field.Artist:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => stringComparitors[comparator](tt.Artist.ToLowerInvariant(), valueString)));
                        break;
                    case Field.Genre:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => stringComparitors[comparator](tt.Genre.ToLowerInvariant(), valueString)));
                        break;
                    case Field.Compilation:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => boolComparitors[comparator](tt.Compilation, boolValue)));
                        break;
                    case Field.Mono:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => boolComparitors[comparator](tt.NumChannels == 1, boolValue)));
                        break;
                    case Field.DiskNum:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => numberComparitors[comparator](tt.DiskNum, numberValue)));
                        break;
                    case Field.FileSize:
                    case Field.FileSizeInKB:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => numberComparitors[comparator]((int)tt.FileSize, numberValue * 1024)));
                        break;
                    case Field.FileSizeInMB:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => numberComparitors[comparator]((int)tt.FileSize, numberValue * 1048576)));
                        break;
                    case Field.Grouping:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => stringComparitors[comparator](tt.Grouping.ToLowerInvariant(), valueString)));
                        break;
                    case Field.Composer:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => stringComparitors[comparator](tt.Composer.ToLowerInvariant(), valueString)));
                        break;
                    case Field.LengthInMinutes:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => numberComparitors[comparator](tt.Duration, numberValue * 60000)));
                        break;
                    case Field.Length:
                    case Field.LengthInSeconds:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => numberComparitors[comparator](tt.Duration, numberValue * 1000)));
                        break;
                    case Field.Title:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => stringComparitors[comparator](tt.Title.ToLowerInvariant(), valueString)));
                        break;
                    case Field.TrackNum:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => numberComparitors[comparator](tt.TrackNum, numberValue)));
                        break;
                    case Field.Year:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => numberComparitors[comparator](tt.Year, numberValue)));
                        break;
                    case Field.PlayCount:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => numberComparitors[comparator](tt.PlayCount, numberValue)));
                        break;
                    case Field.Rating:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => numberComparitors[comparator](tt.Rating, numberValue)));
                        break;
                    case Field.DaysSinceLastPlayed:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => numberComparitors[comparator](tt.DaysSinceLastPlayed, numberValue)));
                        break;
                    case Field.FileAgeInDays:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => numberComparitors[comparator](tt.FileAgeInDays, numberValue)));
                        break;
                    case Field.DaysSinceFileAdded:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => numberComparitors[comparator](tt.DaysSinceFileAdded, numberValue)));
                        break;
                    case Field.Equalizer:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => stringComparitors[comparator](tt.EqualizerString.ToLowerInvariant(), valueString)));
                        break;
                    case Field.BitRate:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => numberComparitors[comparator](tt.Bitrate, numberValue)));
                        break;
                    case Field.SampleRate:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => numberComparitors[comparator](tt.SampleRate, numberValue)));
                        break;
                    case Field.FilePath:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => stringComparitors[comparator](tt.FilePath.ToLowerInvariant(), valueString)));
                        break;
                    case Field.Encoder:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => stringComparitors[comparator](tt.Encoder.ToLowerInvariant(), valueString)));
                        break;
                    case Field.FileType:
                        validator = new Func<List<Track>, List<Track>>(t => t.FindAll(tt => stringComparitors[comparator](tt.TypeString.ToLowerInvariant(), valueString)));
                        break;
                    case Field.Track:
                        validator = new Func<List<Track>, List<Track>>(t => playlistComparitors[comparator](t, valueString));
                        break;
                    case Field.Random:
                        return ValidationError.BadField;
                }

                parsed = true;
                return ValidationError.Valid;
            }

            public IEnumerable<Track> Evaluate(List<Track> T)
            {
                IEnumerable<Track> tracks = null;
                switch (this.type)
                {
                    case ExpressionType.Atomic:
                        tracks = validator(T);
                        break;
                    case ExpressionType.And:
                        tracks = a.Evaluate(T).Intersect(b.Evaluate(T));
                        break;
                    case ExpressionType.Or:
                        tracks = a.Evaluate(T).Union(b.Evaluate(T));
                        break;
                    case ExpressionType.Nor:
                        tracks = T.Except(a.Evaluate(T).Union(b.Evaluate(T)));
                        break;
                    case ExpressionType.Trivial:
                        tracks = T;
                        break;
                }
                return tracks;
            }
        }
        static ExpressionTree()
        {
            fields.Add("album", Field.Album);
            fields.Add("albumartist", Field.AlbumArtist);
            fields.Add("artist", Field.Artist);
            fields.Add("compilation", Field.Compilation);
            fields.Add("mono", Field.Mono);
            fields.Add("composer", Field.Composer);
            fields.Add("disknum", Field.DiskNum);
            fields.Add("encoder", Field.Encoder);
            fields.Add("filepath", Field.FilePath);
            fields.Add("filetype", Field.FileType);
            fields.Add("genre", Field.Genre);
            fields.Add("filesizeinkb", Field.FileSizeInKB);
            fields.Add("filesizeinmb", Field.FileSizeInMB);
            fields.Add("grouping", Field.Grouping);
            fields.Add("lengthinminutes", Field.LengthInMinutes);
            fields.Add("lengthinseconds", Field.LengthInSeconds);
            fields.Add("dayssincelastplayed", Field.DaysSinceLastPlayed);
            fields.Add("fileageindays", Field.FileAgeInDays);
            fields.Add("dayssincefileadded", Field.DaysSinceFileAdded);
            fields.Add("equalizer", Field.Equalizer);
            fields.Add("title", Field.Title);
            fields.Add("tracknum", Field.TrackNum);
            fields.Add("year", Field.Year);
            fields.Add("rating", Field.Rating);
            fields.Add("playcount", Field.PlayCount);
            fields.Add("bitrate", Field.BitRate);
            fields.Add("samplerate", Field.SampleRate);
            fields.Add("track", Field.Track);
            fields.Add("length", Field.Length);
            fields.Add("random", Field.Random);

            comparators.Add("is", Comparator.Is);
            comparators.Add("isnot", Comparator.IsNot);
            comparators.Add("lessthan", Comparator.LessThan);
            comparators.Add("morethan", Comparator.MoreThan);
            comparators.Add("comesafter", Comparator.MoreThan);
            comparators.Add("comesbefore", Comparator.LessThan);
            comparators.Add("contains", Comparator.Contains);
            comparators.Add("startswith", Comparator.StartsWith);
            comparators.Add("endswith", Comparator.EndsWith);
            comparators.Add("doesnotstartwith", Comparator.DoesNotStartWith);
            comparators.Add("doesnotendwith", Comparator.DoesNotEndWith);
            comparators.Add("atmost", Comparator.AtMost);
            comparators.Add("atleast", Comparator.AtLeast);
            comparators.Add("containedin", Comparator.ContainedIn);
            comparators.Add("notcontainedin", Comparator.NotContainedIn);
            comparators.Add("doesnotcontain", Comparator.DoesNotContain);

            comparators.Add("==", Comparator.Is);
            comparators.Add("!=", Comparator.IsNot);
            comparators.Add("=", Comparator.Is);
            comparators.Add("<", Comparator.LessThan);
            comparators.Add(">", Comparator.MoreThan);
            comparators.Add("<=", Comparator.AtMost);
            comparators.Add(">=", Comparator.AtLeast);

            expressionTypes.Add("and", ExpressionType.And);
            expressionTypes.Add("or", ExpressionType.Or);
            expressionTypes.Add("nor", ExpressionType.Nor);

            expressionTypes.Add("&&", ExpressionType.And);
            expressionTypes.Add("||", ExpressionType.Or);
        }
    }
}