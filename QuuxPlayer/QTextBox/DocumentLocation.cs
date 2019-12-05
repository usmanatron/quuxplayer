/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxControls
{
    public class DocumentLocation
    {
        public static DocumentLocation BOF { get; private set; }

        private readonly int lineNum;
        private readonly int colNum;

        public DocumentLocation(int LineNum, int ColNum)
        {
            this.lineNum = LineNum;
            this.colNum = ColNum;
        }
        internal DocumentLocation(Document Document, int LineNum, int SubLineNum, int X)
        {
            lineNum = LineNum;

            int xx = X;

            if (SubLineNum > 0)
            {
                int i = 0;
                List<SubLine> sl = Document[LineNum].GetSubLines(false);
                while (i < SubLineNum)
                    xx += sl[i++].Length;

                X = Math.Min(xx, Document[LineNum].EndOfSubLine(SubLineNum));
            }
            else
            {
                X = Math.Min(X, Document[LineNum].Length);
            }
            colNum = X;
        }
        static DocumentLocation()
        {
            BOF = new DocumentLocation(0, 0);
        }

        public int LineNumber { get { return lineNum; } }
        public int ColumnNumber { get { return colNum; } }

        public override string ToString()
        {
            return "Line " + lineNum.ToString() + " Col " + colNum.ToString();
        }
        public string ToStringOneBased()
        {
            return "Line " + (lineNum + 1).ToString() + " Col " + (colNum + 1).ToString();
        }

        public static bool operator ==(DocumentLocation DL1, DocumentLocation DL2)
        {
            if (((object)DL1) == null)
            {
                return ((object)DL2) == null;
            }
            if (((object)DL2) == null)
            {
                return false;
            }
            return DL1.lineNum == DL2.lineNum && DL1.colNum == DL2.colNum;
        }
        public override bool Equals(object obj)
        {
            return (this == (obj as DocumentLocation));
        }
        public override int GetHashCode()
        {
            return lineNum.GetHashCode() ^ colNum.GetHashCode();
        }
        public static bool operator !=(DocumentLocation DL1, DocumentLocation DL2)
        {
            if (((object)DL1) == null)
            {
                return ((object)DL2) != null;
            }
            if (((object)DL2) == null)
            {
                return true;
            }
            return DL1.lineNum != DL2.lineNum || DL1.colNum != DL2.colNum;
        }
        public static bool operator >(DocumentLocation DL1, DocumentLocation DL2)
        {
            return ((DL1.lineNum > DL2.lineNum) ||
                    ((DL1.lineNum == DL2.lineNum) && (DL1.colNum > DL2.colNum)));
        }
        public static bool operator <(DocumentLocation DL1, DocumentLocation DL2)
        {
            return ((DL1.lineNum < DL2.lineNum) ||
                    ((DL1.lineNum == DL2.lineNum) && (DL1.colNum < DL2.colNum)));
        }
        public static bool operator >=(DocumentLocation DL1, DocumentLocation DL2)
        {
            return ((DL1.lineNum > DL2.lineNum) ||
                    ((DL1.lineNum == DL2.lineNum) && (DL1.colNum >= DL2.colNum)));
        }
        public static bool operator <=(DocumentLocation DL1, DocumentLocation DL2)
        {
            return ((DL1.lineNum < DL2.lineNum) ||
                    ((DL1.lineNum == DL2.lineNum) && (DL1.colNum <= DL2.colNum)));
        }
    }
}
