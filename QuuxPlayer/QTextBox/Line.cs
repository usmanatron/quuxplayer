/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxControls
{
    internal class Line : AvlNode<Line>
    {
        private string text;
        private string textWithBlankCommentsAndQuotes = null;
        private string textWithoutComments = null;

        private List<SubLine> subLines = null;
        
        private int subLinesColBasis = -1;

        private static List<char> breakLeftChars = new List<char> { ' ', '(', ')', '.', ',', '"', '\t', ':', ';' };
        private static List<char> breakRightChars = new List<char> { ' ', '(', ')', '.', ',', '"', '\t', ':', ';' };
        private static int minChars = 4;
        private bool parsed = false;

        private static KeywordInfo ki;
        private uint parseVersionBasis = 0;

        private object @lock = new object();

        public Line(string Text)
        {
            this.text = Text;
            subLines = null;
        }
        
        public static void SetKeywordInfo(KeywordInfo KI)
        {
            ki = KI;
        }

        public List<SubLine> GetSubLines(bool PrepareToRender)
        {
            lock (@lock)
            {
                if (ViewEnvironment.NumColumnsPerLine != subLinesColBasis)
                {
                    if (ViewEnvironment.NumColumnsPerLine < this.Length || subLines == null || subLines.Count != 1)
                    {
                        if (subLines == null)
                            subLines = new List<SubLine>();
                        else
                            subLines.Clear();

                        if (Text.Length <= ViewEnvironment.NumColumnsPerLine)
                        {
                            subLines.Add(new SubLine(Text, 0));
                        }
                        else
                        {
                            string s = Text;
                            int start = 0;

                            while (s.Length > ViewEnvironment.NumColumnsPerLine)
                            {
                                int len = ViewEnvironment.NumColumnsPerLine;

                                while (len > 0 && breakLeftChars.IndexOf(s[len]) < 0)
                                    len--;

                                if (len < minChars)
                                {
                                    while (len < s.Length && len < ViewEnvironment.NumColumnsPerLine && s[len] != ' ')
                                        len++;
                                }

                                while (len < s.Length && s[len] == ' ')
                                    len++;

                                subLines.Add(new SubLine(s.Substring(0, len), start));
                                s = s.Substring(len);
                                start += len;
                            }
                            if (s.Length > 0)
                                subLines.Add(new SubLine(s, start));
                        }
                        parsed = false;
                    }
                    subLinesColBasis = ViewEnvironment.NumColumnsPerLine;
                }
                if (PrepareToRender && (!parsed || ki.ParseVersion > parseVersionBasis))
                {
                    bool quoting = false;
                    bool singleLineCommenting = false;
                    bool escaping = false;
                    foreach (SubLine sl in subLines)
                    {
                        sl.GetTextSegments(ref quoting, ref singleLineCommenting, ref escaping);
                    };

                    parsed = true;
                    parseVersionBasis = ki.ParseVersion;
                }
                return subLines;
            }
        }
        public int NumVisibleSubLines
        {
            get
            {
                if (subLines == null || subLinesColBasis != ViewEnvironment.NumColumnsPerLine)
                    GetSubLines(false);

                return subLines.Count;
            }
        }
        public List<SubLine> VisibleSubLines
        {
            get
            {
                List<SubLine> sl = GetSubLines(true);

                return sl;
            }
        }
        public string Text
        {
            get { return text; }
            set
            {
                this.text = value;
                this.reset();
                this.Fix();
            }
        }
        public string TextWithBlankCommentsAndQuotes
        {
            get
            {
                if (textWithBlankCommentsAndQuotes == null)
                {
                    parse();
                    StringBuilder sb = new StringBuilder(text);
                    int offset = 0;
                    for (int i = 0; i < subLines.Count; i++)
                    {
                        List<Word> lw = subLines[i].Words;
                        for (int j = 0; j < lw.Count; j++)
                        {
                            if (lw[j].SingleLineComment || lw[j].Quote)
                            {
                                for (int k = lw[j].Start + offset; k < lw[j].End + offset; k++)
                                    sb[k] = ' ';
                            }
                        }
                        offset += subLines[i].Length;
                    }
                    textWithBlankCommentsAndQuotes = sb.ToString();
                }
                return textWithBlankCommentsAndQuotes;
            }
        }
        public string TextWithoutComments
        {
            get
            {
                if (textWithoutComments == null)
                {
                    parse();
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < subLines.Count; i++)
                    {
                        List<Word> lw = subLines[i].Words;
                        for (int j = 0; j < lw.Count; j++)
                        {
                            if (!lw[j].SingleLineComment)
                            {
                                sb.Append(lw[j].Text);
                            }
                        }
                    }
                    textWithoutComments = sb.ToString().Trim();
                }
                return textWithoutComments;
            }
        }
        public int FirstNonWhiteSpaceChar
        {
            get
            {
                int x = 0;
                while (x < this.Length && isWS(this.Text[x]))
                {
                    x++;
                }
                return x;
                //if (x < this.Length && !isWS(this.text[x]))
                //    return x;
                //else
                //    return -1;
            }
        }
        public int SubLineStart(int SubLineNum)
        {
            return GetSubLines(false)[SubLineNum].Start;
        }
        public override int Length { get { return Text.Length; } }
        public int SubLineNum(int ColNum)
        {
            int slNum = 0;
            List<SubLine> subLines = this.GetSubLines(false);
            SubLine sl;
            while ((sl = subLines[slNum]).Length < ColNum || (!ViewEnvironment.BiasPreviousLine && sl.Length == ColNum))
            {
                ColNum -= sl.Length;
                slNum++;
                if (slNum >= subLines.Count)
                    return subLines.Count - 1;
            }
            return slNum;
        }
        public int BeginningOfSubLine(int SubLineNumber)
        {
            GetSubLines(false);
            int x = 0;
            for (int i = 0; i < SubLineNumber; i++)
                x += subLines[i].Length;

            return x;
        }
        public int EndOfSubLine(int SubLineNumber)
        {
            lock (@lock)
            {
                GetSubLines(false);
                int x = 0;
                for (int i = 0; i <= SubLineNumber; i++)
                    x += subLines[i].Length;

                return x;
            }
        }
        public DocumentRange GetWordRange(DocumentLocation DL)
        {
            lock (@lock)
            {
                if (this.Length == 0)
                    return DocumentRange.Empty;

                int pw = Math.Min(DL.ColumnNumber, this.Length - 1);

                while (pw > 0 && !isWS(text[pw]) && !isSep(text[pw]))
                    pw--;
                if ((pw < this.Length - 1) && (pw > 0 || (isWS(text[pw]) || isSep(text[pw]))))
                    pw++;

                int nw = DL.ColumnNumber;

                while (nw < text.Length && !isWS(text[nw]) && !isSep(text[nw]))
                    nw++;

                return new DocumentRange(DL.LineNumber, pw, DL.LineNumber, nw);
            }
        }
        public int NumInitialSpaces
        {
            get { return text.Length - text.TrimStart().Length; }
        }

        public void Insert(char c, int ColNum)
        {
            if (ColNum > this.Text.Length)
                this.Text += c;
            else
                this.Text = this.Text.Substring(0, ColNum) + c + this.Text.Substring(ColNum);
        }
        public void Replace(char c, int ColNum)
        {
            if (ColNum > this.Text.Length - 1)
                this.Text = this.Text.Substring(0, ColNum) + c;
            else
                this.Text = this.Text.Substring(0, ColNum) + c + this.Text.Substring(ColNum + 1);
        }
        public bool Delete(int ColNum)
        {
            if (this.text.Length > ColNum)
            {
                this.Text = this.Text.Substring(0, ColNum) + this.Text.Substring(ColNum + 1);
                return true;
            }
            else
            {
                return false;
            }
        }

        public int GetNextWord(int x)
        {
            bool foundWS = false;

            while (x < text.Length)
            {
                if (isWS(text[x]))
                    foundWS = true;
                else if (foundWS)
                    return x;
                if (isSep(text[x]))
                    return x + 1;
                x++;
            }
            return -1;
        }
        public int GetPrevWord(int x)
        {
            bool foundNWS = false;

            int origX = x--;

            while (x >= 0)
            {
                if (!isWS(text[x]))
                    foundNWS = true;
                else if (foundNWS)
                    return x + 1;
                if (isSep(text[x]))
                    return x;
                x--;
            }
            return -1;
        }

        public bool IsCommentedOrQuoted(int ColNum)
        {
            List<SubLine> sl = GetSubLines(true);
            for (int i = 0; i < sl.Count; i++)
            {
                if (sl[i].Start > ColNum)
                    return false;

                foreach (Word w in sl[i].Words)
                {
                    if (w.SingleLineComment)
                    {
                        return w.Start + sl[i].Start < ColNum;
                    }
                    else if (w.Quote && (w.Start + sl[i].Start) <= ColNum && (w.End + sl[i].Start) >= ColNum)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool isWS(char c)
        {
            return c <= ' ' || c == ',';
        }
        private static bool isSep(char c)
        {
            return sepChars.IndexOf(c) >= 0;
        }
        private static List<char> sepChars = new List<char> { ',', '(', ')', '[', ']', '{', '}', '+', '/', '\\', '<', '>', '.', '!', ':', ';' };

        private void reset()
        {
            this.textWithBlankCommentsAndQuotes = null;
            this.textWithoutComments = null;
            
            if (subLines != null)
                subLines.Clear();

            subLinesColBasis = -1;
            parsed = false;
        }
        private void parse()
        {
            GetSubLines(true);
        }
        public override string ToString()
        {
            return this.Text + " (" + this.Text.Length + " chars)";
        }
    }
}
