/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxControls
{
    internal class SubLine
    {
        private string text;
        private List<TextSegment> segments = null;
        private List<TextSegment> segmentsWithSelection = null;
        private int start;
        private int selStart = 0;
        private int selLength = 0;
        private List<Word> words;
        private const TextFormatFlags tff = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.NoClipping;

        private uint parseVersion = 0;

        private static KeywordInfo ki;

        public SubLine(string Text, int Start)
        {
            this.text = Text;
            this.start = Start;
        }
        public static void SetKeywordInfo(KeywordInfo KI)
        {
            ki = KI;
        }
        public string Text
        {
            get { return text; }
        }
        public int Start
        {
            get { return start; }
        }
        public int End
        {
            get { return start + Length; }
        }
        public List<Word> Words { get { return words; } }
        public int Length { get { return text.Length; } }
        public List<TextSegment> GetTextSegments(ref bool Quoting, ref bool SingleLineCommenting, ref bool Escaping)
        {
            if (segments == null || parseVersion < ki.ParseVersion)
            {
                segments = new List<TextSegment>();
                words = Word.GetWords(this.text, ref Quoting, ref SingleLineCommenting, ref Escaping);
                TextSegment last = null;
                foreach (Word w in words)
                {
                    if (!w.WhiteSpace)
                    {
                        FontInfo fi = ki.GetFontInfo(w);
                        if (last != null && last.FontInfo.Matches(fi))
                        {
                            last.End = w.End;
                        }
                        else
                        {
                            last = new TextSegment(w.Start, w.End, ki.GetFontInfo(w));
                            segments.Add(last);
                        }
                    }
                }
                parseVersion = ki.ParseVersion;
            }
            return segments;
        }
        public void Render(Graphics g, Rectangle r, int FirstCol, Point Selection)
        {
            // Selection: X == Start; Y == Length

            List<TextSegment> ts;

            if (Selection.Y == 0)
            {
                ts = segments;
            }
            else
            {
                g.FillRectangle(Brushes.DarkBlue, new Rectangle(r.Location.X + Selection.X * FontInfo.Width, r.Y, FontInfo.WidthTimes100 * Math.Min(Text.Length - Selection.X, Selection.Y) / 100, FontInfo.Height));

                if (selStart == Selection.X && selLength == Selection.Y)
                {
                    ts = segmentsWithSelection;
                }
                else
                {
                    selStart = Selection.X;
                    selLength = Selection.Y;
                    selectSegments();
                    ts = segmentsWithSelection;

                }
            }

            if (FirstCol == 0)
            {
                foreach (TextSegment t in ts)
                    drawText(g, t.ToString(this.text), t.FontInfo.Font, new Point(r.Left + t.Start * FontInfo.WidthTimes100 / 100, r.Top), t.FontInfo.Color, t.Selected);
            }
            else
            {
                foreach (TextSegment t in ts)
                {
                    if (t.Start > FirstCol)
                    {
                        drawText(g, t.ToString(this.text), t.FontInfo.Font, new Point(r.Left + (t.Start - FirstCol) * FontInfo.WidthTimes100 / 100, r.Top), t.FontInfo.Color, t.Selected);
                    }
                    else if (t.End > FirstCol)
                    {
                        drawText(g, t.ToString(this.text).Substring(FirstCol - t.Start), t.FontInfo.Font, new Point(r.Left, r.Top), t.FontInfo.Color, t.Selected);
                    }
                }
            }
        }
        private void drawText(Graphics g, string Text, Font F, Point P, Color C, bool Selected)
        {
            if (Selected)
            {
                TextRenderer.DrawText(g, Text, F, P, Color.White, tff);
            }
            else
            {
                TextRenderer.DrawText(g, Text, F, P, C, tff);
            }
        }
        private void selectSegments()
        {
            segmentsWithSelection = new List<TextSegment>();
            foreach (TextSegment ts in segments)
            {
                if (ts.End <= selStart || ts.Start >= (selStart + selLength))
                {
                    segmentsWithSelection.Add(ts);
                }
                else if (ts.Start >= selStart && ts.End <= (selStart + selLength))
                {
                    segmentsWithSelection.Add(new TextSegment(ts.Start, ts.End, ts.FontInfo, true));
                }
                else
                {
                    if (ts.Start < selStart)
                    {
                        segmentsWithSelection.Add(new TextSegment(ts.Start, selStart, ts.FontInfo, false));
                        if (ts.End <= (selStart + selLength))
                        {
                            segmentsWithSelection.Add(new TextSegment(selStart, ts.End, ts.FontInfo, true));
                        }
                        else
                        {
                            segmentsWithSelection.Add(new TextSegment(selStart, selStart + selLength, ts.FontInfo, true));
                            segmentsWithSelection.Add(new TextSegment(selStart + selLength, ts.End, ts.FontInfo, false));
                        }
                    }
                    else
                    {
                        segmentsWithSelection.Add(new TextSegment(ts.Start, selStart + selLength, ts.FontInfo, true));
                        segmentsWithSelection.Add(new TextSegment(selStart + selLength, ts.End, ts.FontInfo, false));
                    }
                }

            }
        }
        public int FirstNonWSChar()
        {
            int x = 0;
            while (x < text.Length)
            {
                if (text[x] > ' ')
                    return x;
                x++;
            }
            return -1;
        }
        public static implicit operator string(SubLine Input)
        {
            return Input.Text;
        }
    }
}
