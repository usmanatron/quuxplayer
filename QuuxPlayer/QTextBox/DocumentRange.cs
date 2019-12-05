/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxControls
{
    public class DocumentRange
    {
        private DocumentLocation start;
        private DocumentLocation end;

        public static DocumentRange Empty { get; private set; }

        public DocumentRange(DocumentLocation DL1, DocumentLocation DL2)
        {
            if (DL1 < DL2)
            {
                start = DL1;
                end = DL2;
            }
            else
            {
                start = DL2;
                end = DL1;
            }
        }
        public DocumentRange(int Line1, int Col1, int Line2, int Col2)
            : this(new DocumentLocation(Line1, Col1), new DocumentLocation(Line2, Col2))
        {
        }

        internal DocumentRange(DocumentLocation DL, DocumentRangeLength DRL, Document Document)
        {
            start = DL;
            end = Document.EnsureValid(new DocumentLocation(DL.LineNumber + DRL.Lines, DL.ColumnNumber + DRL.Columns));
        }
        static DocumentRange()
        {
            Empty = new DocumentRange(DocumentLocation.BOF, DocumentLocation.BOF);
        }
        public bool IsEmpty
        {
            get { return start == end; }
        }
        public DocumentLocation Start
        {
            get { return start; }
        }
        public DocumentLocation End
        {
            get { return end; }
        }
        public bool MultiLine
        {
            get { return this.end.LineNumber > this.start.LineNumber; }
        }
        internal int Length(Document Document)
        {
            if (start.LineNumber == end.LineNumber)
                return end.ColumnNumber - start.ColumnNumber;

            int l = Document[start.LineNumber].Length - start.ColumnNumber + 1;

            for (int i = start.LineNumber + 1; i < end.LineNumber; i++)
                l += (Document[i].Length + 1);

            l += end.ColumnNumber;

            return l;
        }
        internal DocumentRangeLength DRLength
        {
            get { return new DocumentRangeLength(end.LineNumber - start.LineNumber, end.ColumnNumber - start.ColumnNumber); }
        }
        public bool Contains(DocumentLocation DL)
        {
            return (start.LineNumber < DL.LineNumber && end.LineNumber > DL.LineNumber) ||
                   (start.LineNumber == DL.LineNumber && start.ColumnNumber <= DL.ColumnNumber && (end.LineNumber > DL.LineNumber || end.ColumnNumber >= DL.ColumnNumber)) ||
                   (start.LineNumber < DL.LineNumber && end.ColumnNumber >= DL.ColumnNumber);
        }
        public override string ToString()
        {
            if (IsEmpty)
                return "(empty)";
            else
                return start.ToString() + " : " + end.ToString();
        }
        public string ToStringOneBased()
        {
            if (IsEmpty)
                return "(empty)";
            else
                return start.ToStringOneBased() + " : " + end.ToStringOneBased();
        }
    }
}
