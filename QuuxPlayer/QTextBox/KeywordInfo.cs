/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;

namespace QuuxControls
{
    internal class KeywordInfo
    {
        private StringComparer comparer;

        private Dictionary<int, List<string>> keywordsByIndex;
        private Dictionary<string, int> indecesByKeyword;
        private Dictionary<int, FontInfo> fontInfoByIndex;

        public uint ParseVersion { get; private set; }

        public KeywordInfo(StringComparer Comparer)
        {
            Clear(Comparer);
            ParseVersion = 0;
        }
        public void Add(int Index, string Keyword)
        {
            if (!indecesByKeyword.ContainsKey(Keyword))
            {
                indecesByKeyword.Add(Keyword, Index);
                if (!keywordsByIndex.ContainsKey(Index))
                {
                    keywordsByIndex.Add(Index, new List<string>());
                    if (!fontInfoByIndex.ContainsKey(Index))
                        fontInfoByIndex.Add(Index, new FontInfo(FontStyle.Regular, Color.Black));
                }
                keywordsByIndex[Index].Add(Keyword);
            }

            ParseVersion++;
        }
        public void Add(int Index, string[] Keywords)
        {
            foreach (string s in Keywords)
                Add(Index, s);

            ParseVersion++;
        }
        public FontInfo GetFontInfo(Word Word)
        {
            int i;

            if (Word.Quote)
                return FontInfo.Quote;
            else if (Word.SingleLineComment)
                return FontInfo.SingleLineComment;
            else if (indecesByKeyword.TryGetValue(Word.Text, out i))
                return fontInfoByIndex[i];
            else
                return FontInfo.Default;
        }
        public void SetFontInfo(int Index, FontStyle FontStyle, Color Color)
        {
            if (fontInfoByIndex.ContainsKey(Index))
                fontInfoByIndex.Remove(Index);

            fontInfoByIndex.Add(Index, new FontInfo(FontStyle, Color));

            ParseVersion++;
        }
        public void Clear(StringComparer Comparer)
        {
            comparer = Comparer;

            keywordsByIndex = new Dictionary<int, List<string>>();
            indecesByKeyword = new Dictionary<string, int>(comparer);
            fontInfoByIndex = new Dictionary<int, FontInfo>();

            ParseVersion++;
        }
        public bool Contains(string Keyword)
        {
            return indecesByKeyword.ContainsKey(Keyword);
        }
    }
}
