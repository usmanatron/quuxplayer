/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxControls
{
    internal class TextSegment
    {
        public int Start { get; private set; }
        public int End { get; set; }
        public FontInfo FontInfo { get; private set; }
        public bool Selected { get; private set; }

        public TextSegment(int Start, int End, FontInfo FontInfo)
        {
            this.Start = Start;
            this.End = End;
            this.FontInfo = FontInfo;
        }
        public TextSegment(int Start, int End, FontInfo FontInfo, bool Selected)
        {
            this.Start = Start;
            this.End = End;
            this.FontInfo = FontInfo;
            this.Selected = Selected;
        }
        public override string ToString()
        {
            return "Start: " + Start.ToString() + " End: " + End.ToString();
        }
        public string ToString(string Input)
        {
            return Input.Substring(Start, End - Start);
        }
    }
}
