/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxControls
{
    internal class Document
    {
        private Avl<Line> lines;
        private int maxLength = 0;

        private int spacesInTab = 0;

        private static string[] tabSpaces;
        private UndoManager undoManager;
        private bool documentChanged;

        public Document(int SpacesInTab)
        {
            lines = new Avl<Line>(new Line(String.Empty));
            DocumentChanged = false;
            this.SpacesInTab = SpacesInTab;
        }
        public UndoManager UndoManager
        {
            set
            {
                undoManager = value;
            }
        }
        
        public Line this[int LineNum]
        {
            get
            {
                if (LineNum >= this.NumLines)
                    return new Line(String.Empty);
                else
                    return lines[LineNum];
            }
        }
        public void Load(List<string> Text)
        {
            Clear();

            lines.Delete(0);

            foreach (string s in Text)
                lines.Add(new Line(s));

            EventManager.DoEvent(EventManager.EventType.NumLinesChanged);

            if (lines.MaxLength != maxLength)
            {
                maxLength = lines.MaxLength;
                EventManager.DoEvent(EventManager.EventType.LongestLineChanged);
            }
            EventManager.DoEvent(EventManager.EventType.DocumentChanged);
        }

        public void ReplaceTabs(List<string> Text)
        {
            string text;

            for (int i = 0; i < Text.Count; i++)
            {
                text = Text[i];

                if (text.IndexOf('\t') >= 0)
                {
                    int j;
                    while ((j = text.IndexOf('\t')) >= 0)
                    {
                        text = text.Substring(0, j) + tabSpaces[j % spacesInTab] + text.Substring(j + 1);
                    }
                }
                Text[i] = text;
            }
        }

        public int NumLines { get { return lines.Count; } }
        public bool DocumentChanged
        {
            get { return documentChanged; }
            set
            {
                if (value)
                    EventManager.DoEvent(EventManager.EventType.DocumentChanged);

                if (documentChanged != value)
                {
                    documentChanged = value;
                    EventManager.DoEvent(EventManager.EventType.DocumentChangedChanged);
                }
            }
        }
        public int SpacesInTab
        {
            get { return spacesInTab; }
            set
            {
                spacesInTab = Math.Min(8, Math.Max(2, value));
                setTabSpaces();
            }
        }
        public int LongestLine
        {
            get { return maxLength; }
        }
        
        public DocumentLocation EOF
        {
            get
            {
                if (NumLines == 0)
                    return DocumentLocation.BOF;
                else
                    return new DocumentLocation(NumLines - 1, lines[NumLines - 1].Length);
            }
        }
        private void setTabSpaces()
        {
            tabSpaces = new string[spacesInTab];

            for (int i = 1; i < spacesInTab; i++)
                tabSpaces[i] = new String(' ', i);

            tabSpaces[0] = new string(' ', spacesInTab);
        }
        public DocumentLocation EnsureValid(DocumentLocation DL)
        {
            if (DL.LineNumber >= this.NumLines)
                return EOF;

            if (DL.LineNumber < 0)
                return DocumentLocation.BOF;

            if (DL.ColumnNumber > lines[DL.LineNumber].Length)
                return new DocumentLocation(DL.LineNumber, lines[DL.LineNumber].Length);

            return DL;
        }
        public DocumentRange EnsureValid(DocumentRange DR)
        {
            return new DocumentRange(EnsureValid(DR.Start), EnsureValid(DR.End));
        }
        public DocumentRange FindNext(string FindText, DocumentLocation Start)
        {
            int x;

            if ((x = this[Start.LineNumber].Text.IndexOf(FindText, Start.ColumnNumber, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                return new DocumentRange(Start.LineNumber, x, Start.LineNumber, x + FindText.Length);
            }
            int i = Start.LineNumber + 1;

            while (i < this.NumLines)
            {
                if ((x = this[i].Text.IndexOf(FindText, StringComparison.OrdinalIgnoreCase)) >= 0)
                    return new DocumentRange(i, x, i, x + FindText.Length);
                i++;
            }
            return DocumentRange.Empty;
        }
        public DocumentRange FindPrevious(string FindText, DocumentLocation Start)
        {
            int x;

            if ((x = this[Start.LineNumber].Text.LastIndexOf(FindText, Start.ColumnNumber, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                return new DocumentRange(Start.LineNumber, x, Start.LineNumber, x + FindText.Length);
            }

            int i = Start.LineNumber - 1;

            while (i >= 0)
            {
                if ((x = this[i].Text.LastIndexOf(FindText, StringComparison.OrdinalIgnoreCase)) >= 0)
                    return new DocumentRange(i, x, i, x + FindText.Length);
                i--;
            }
            return DocumentRange.Empty;
        }

        public DocumentLocation GetNextLocation(DocumentLocation DL)
        {
            if (DL.LineNumber >= this.NumLines)
                return EOF;

            if (DL.ColumnNumber >= lines[DL.LineNumber].Length)
            {
                if (DL.LineNumber >= this.NumLines - 1)
                    return EOF;
                else
                    return new DocumentLocation(DL.LineNumber + 1, 0);
            }
            else
            {
                return new DocumentLocation(DL.LineNumber, DL.ColumnNumber + 1);
            }
        }
        public DocumentLocation GetPreviousLocation(DocumentLocation DL)
        {
            if (DL.ColumnNumber > 0)
                return new DocumentLocation(DL.LineNumber, DL.ColumnNumber - 1);
            else if (DL.LineNumber == 0)
                return DocumentLocation.BOF;
            else
                return new DocumentLocation(DL.LineNumber - 1, lines[DL.LineNumber - 1].Length);
        }
        public DocumentLocation GetPreviousVisibleLocation(DocumentLocation DL)
        {
            Line l = this[DL.LineNumber];

            if (DL.ColumnNumber > 0)
            {
                return new DocumentLocation(DL.LineNumber, DL.ColumnNumber - 1);
            }

            int lineNum = DL.LineNumber - 1;

            l = l.Previous;

            if (l == null)
                return DocumentLocation.BOF;

            return new DocumentLocation(lineNum, this[lineNum].Length);
        }
        public DocumentLocation GetNextVisibleLocation(DocumentLocation DL)
        {
            Line l = this[DL.LineNumber];

            if (l.Length > DL.ColumnNumber)
            {
                return new DocumentLocation(DL.LineNumber, DL.ColumnNumber + 1);
            }

            int lineNum = DL.LineNumber + 1;

            l = l.Next;

            if (l == null)
                return DL;

            return new DocumentLocation(lineNum, 0);
        }

        public DocumentLocation GetNextWord(DocumentLocation DL)
        {
            Line l = this[DL.LineNumber];

            if (DL.ColumnNumber >= l.Length)
            {
                if (this.NumLines > DL.LineNumber + 1)
                    return new DocumentLocation(DL.LineNumber + 1, 0);
                else
                    return this.EOF;
            }

            int nw = l.GetNextWord(DL.ColumnNumber);

            if (nw < 0)
            {
                return new DocumentLocation(DL.LineNumber, l.Length);
            }
            else
            {
                return new DocumentLocation(DL.LineNumber, nw);
            }
        }
        public DocumentLocation GetPreviousWord(DocumentLocation DL)   
        {
            if (DL.ColumnNumber == 0)
            {
                if (DL.LineNumber == 0)
                    return DocumentLocation.BOF;
                else
                    return new DocumentLocation(DL.LineNumber - 1, this[DL.LineNumber - 1].Length);
            }
            
            Line l = this[DL.LineNumber];

            int pw = l.GetPrevWord(DL.ColumnNumber);

            if (pw < 0)
            {
                return new DocumentLocation(DL.LineNumber, 0);
            }
            else
            {
                return new DocumentLocation(DL.LineNumber, pw);
            }
        }
        public int VisibleRowCount(int Line1, int SubLine1, int Line2, int SubLine2)
        {
            int count;
            if (Line1 < Line2)
            {
                count = this[Line1].NumVisibleSubLines - SubLine1 + SubLine2;
                for (int i = Line1 + 1; i < Line2; i++)
                    count += this[i].NumVisibleSubLines;
            }
            else if (Line1 > Line2)
            {
                count = this[Line2].NumVisibleSubLines - SubLine2 + SubLine1;
                for (int i = Line2 + 1; i < Line1; i++)
                    count += this[i].NumVisibleSubLines;
            }
            else
            {
                count = SubLine2 - SubLine1;
            }
            return count;
        }
        public bool Offset(int LineCount, ref int LineNum, ref int SubLineNum)
        {
            // returns true if not limited by doc bounds

            if (LineCount > 0)
            {
                if (LineNum > this.NumLines - 1)
                    return false;

                LineCount += SubLineNum;

                while (LineCount > 0 && LineNum < this.NumLines)
                {
                    LineCount -= this[LineNum++].NumVisibleSubLines;
                }
                if (LineCount < 0)
                {
                    LineNum--;
                    SubLineNum = this[LineNum].NumVisibleSubLines + LineCount;
                }
                else if (LineCount > 0)
                {
                    LineNum = this.NumLines - 1;
                    SubLineNum = this[this.NumLines - 1].NumVisibleSubLines - 1;
                }
                else if (LineNum >= this.NumLines)
                {
                    LineNum = this.NumLines - 1;
                    SubLineNum = this[this.NumLines - 1].NumVisibleSubLines - 1;
                }
                else
                {
                    SubLineNum = 0;
                }
                return LineCount <= 0;
            }
            else if (LineCount < 0)
            {
                LineCount = -LineCount;

                LineCount -= SubLineNum;

                while (LineCount > 0 && LineNum > 0)
                {
                    LineCount -= this[--LineNum].NumVisibleSubLines;
                }
                if (LineCount < 0)
                {
                    SubLineNum = -LineCount;
                }
                else
                {
                    SubLineNum = 0;
                }
                return LineCount <= 0;
            }
            return true;
        }
        public int XCoord(DocumentLocation DL)
        {
            // Note: doesn't depend on DL being onscreen
            // Doesn't consider horizontal scroll posn

            Line l = this[DL.LineNumber];

            List<SubLine> lsl = l.GetSubLines(false);

            int sln = l.SubLineNum(DL.ColumnNumber);

            return DL.ColumnNumber - l.BeginningOfSubLine(sln);
        }
        public char GetCharBefore(DocumentLocation DL)
        {
            if (DL.ColumnNumber <= 0)
                return '\0';

            Line l = this[DL.LineNumber];

            if (DL.ColumnNumber > l.Length)
                return '\0';

            return l.Text[DL.ColumnNumber - 1];
        }
        public char GetCharBeforeIfNotCommented(DocumentLocation DL)
        {

            if (DL.ColumnNumber <= 0)
                return '\0';

            Line l = this[DL.LineNumber];

            if (DL.ColumnNumber > l.Length)
                return '\0';

            return l.TextWithBlankCommentsAndQuotes[DL.ColumnNumber - 1];
        }
        public char GetCharAt(DocumentLocation DL)
        {
            Line l = this[DL.LineNumber];

            if (DL.ColumnNumber >= l.Length)
                return '\0';

            return l.Text[DL.ColumnNumber];
        }
        public char GetCharAtIfNotCommentedOrQuoted(DocumentLocation DL)
        {
            Line l = this[DL.LineNumber];

            if (DL.ColumnNumber >= l.Length)
                return '\0';

            return l.TextWithBlankCommentsAndQuotes[DL.ColumnNumber];
        }

        public string GetText(DocumentRange DR)
        {
            if (DR.IsEmpty)
                return String.Empty;

            if (DR.Start.LineNumber == DR.End.LineNumber)
                return this[DR.Start.LineNumber].Text.Substring(DR.Start.ColumnNumber, DR.End.ColumnNumber - DR.Start.ColumnNumber);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(this[DR.Start.LineNumber].Text.Substring(DR.Start.ColumnNumber));

            for (int i = DR.Start.LineNumber + 1; i < DR.End.LineNumber; i++)
                sb.AppendLine(this[i].Text);

            sb.Append(this[DR.End.LineNumber].Text.Substring(0, DR.End.ColumnNumber));

            return sb.ToString();
        }
        public int CharNum(DocumentLocation DL)
        {
            return this[DL.LineNumber].StartIndex + DL.ColumnNumber;   
        }

        // EDITING METHODS

        public void Clear()
        {
            lines.Clear(new Line(String.Empty));
            undoManager.Clear();
            documentChanged = false;

            EventManager.DoEvent(EventManager.EventType.DocumentChanged);
        }
        public void SplitLine(DocumentLocation DL)
        {
            Line oldLine = this[DL.LineNumber];

            string oldText = oldLine.Text;
            string newText = oldText.Substring(0, Math.Min(oldText.Length, DL.ColumnNumber));

            this[DL.LineNumber].Text = newText;

            undoManager.AddElement(new UndoElementReplaceLine(DL.LineNumber, oldText, newText));

            newText = oldText.Substring(Math.Min(DL.ColumnNumber, oldText.Length));
            
            Line newLine = new Line(newText);
            lines.Insert(newLine, DL.LineNumber + 1);

            undoManager.AddElement(new UndoElementInsertLine(DL.LineNumber + 1, newText));

            EventManager.DoEvent(EventManager.EventType.NumLinesChanged);
            DocumentChanged = true;
        }
        public DocumentLocation Insert(char c, DocumentLocation DL)
        {
            this[DL.LineNumber].Insert(c, DL.ColumnNumber);

            undoManager.AddElement(new UndoElementInsertChar(DL, c));

            DocumentChanged = true;

            return GetNextLocation(DL);
        }
        public DocumentLocation Insert(string s, DocumentLocation DL)
        {
            string[] ss = s.Split(newLineChars, StringSplitOptions.None);

            if (ss.Length > 1)
            {
                Line l = this[DL.LineNumber];

                string txt = l.Text;

                l.Text = txt.Substring(0, DL.ColumnNumber) + ss[0];

                undoManager.AddElement(new UndoElementReplaceLine(DL.LineNumber, txt, l.Text));

                for (int i = 1; i < ss.Length - 1; i++)
                {
                    InsertLine(ss[i], DL.LineNumber + i);
                }

                InsertLine(ss[ss.Length - 1] + txt.Substring(DL.ColumnNumber), DL.LineNumber + ss.Length - 1);

                DocumentChanged = true;

                EventManager.DoEvent(EventManager.EventType.NumLinesChanged);

                return new DocumentLocation(DL.LineNumber + ss.Length - 1, ss[ss.Length - 1].Length);
            }
            else
            {
                Line l = this[DL.LineNumber];
                string old = l.Text;
                l.Text = old.Substring(0, DL.ColumnNumber) + s + l.Text.Substring(DL.ColumnNumber);

                undoManager.AddElement(new UndoElementReplaceLine(DL.LineNumber, old, l.Text));

                DocumentChanged = true;

                return new DocumentLocation(DL.LineNumber, DL.ColumnNumber + s.Length);
            }
        }
        public DocumentLocation Replace(char c, DocumentLocation DL)
        {
            Line l = this[DL.LineNumber];

            if (l.Length <= DL.ColumnNumber)
            {
                return Insert(c, DL);
            }

            undoManager.AddElement(new UndoElementReplaceChar(DL, l.Text[DL.ColumnNumber], c));

            l.Replace(c, DL.ColumnNumber);

            DocumentChanged = true;

            return GetNextLocation(DL);
        }
        public void Replace(string s, int LineNumber)
        {
            Line l = this[LineNumber];

            undoManager.AddElement(new UndoElementReplaceLine(LineNumber, l.Text, s));

            l.Text = s;

            DocumentChanged = true;
        }
        public void Delete(DocumentLocation DL)
        {
            Line l = this[DL.LineNumber];

            char c = '\0';
            
            if (DL.ColumnNumber < l.Length)
                c = l.Text[DL.ColumnNumber];

            if (!l.Delete(DL.ColumnNumber))
            {
                combineLines(DL.LineNumber);
                DocumentChanged = true;
                EventManager.DoEvent(EventManager.EventType.NumLinesChanged);
            }
            else
            {
                undoManager.AddElement(new UndoElementDeleteChar(DL, c));
                DocumentChanged = true;
            }
        }
        public void Delete(DocumentRange DR)
        {
            Line l1 = this[DR.Start.LineNumber];

            string oldText;

            if (!DR.MultiLine)
            {
                oldText = l1.Text;
                l1.Text = oldText.Substring(0, DR.Start.ColumnNumber) + l1.Text.Substring(DR.End.ColumnNumber);
                undoManager.AddElement(new UndoElementReplaceLine(DR.Start.LineNumber, oldText, l1.Text));
            }
            else
            {
                Line l2 = this[DR.End.LineNumber];

                oldText = l1.Text;

                l1.Text = oldText.Substring(0, DR.Start.ColumnNumber) + l2.Text.Substring(DR.End.ColumnNumber);

                undoManager.AddElement(new UndoElementReplaceLine(DR.Start.LineNumber, oldText, l1.Text));

                for (int i = DR.End.LineNumber; i > DR.Start.LineNumber; i--)
                {
                    Delete(i);
                }

                EventManager.DoEvent(EventManager.EventType.NumLinesChanged);
            }
            DocumentChanged = true;
        }
        public DocumentLocation Backspace(DocumentLocation DL)
        {
            DL = GetPreviousLocation(DL);

            if (!this[DL.LineNumber].Delete(DL.ColumnNumber))
            {
                combineLines(DL.LineNumber);
                DocumentChanged = true;
                EventManager.DoEvent(EventManager.EventType.NumLinesChanged);
            }
            else
            {
                DocumentChanged = true;
            }

            return DL;
        }
        public void Undo()
        {
            undoManager.Suspend();
            UndoPackage up = undoManager.GetUndoPackage();

            if (up != null)
            {
                for (int i = up.Elements.Count - 1; i >= 0; i--)
                {
                    up.Elements[i].Undo(this);
                }
                undoManager.SetOldView(up);
            }
            undoManager.Resume();
        }
        public void Redo()
        {
            undoManager.Suspend();
            UndoPackage rp = undoManager.GetRedoPackage();

            if (rp != null)
            {
                for (int i = 0; i < rp.Elements.Count; i++)
                {
                    rp.Elements[i].Redo(this);
                }
                undoManager.SetNewView(rp);
            }
            undoManager.Resume();
        }
        public void Delete(int LineNumber)
        {
            undoManager.AddElement(new UndoElementDeleteLine(LineNumber, lines[LineNumber].Text));

            lines.Delete(LineNumber);

            EventManager.DoEvent(EventManager.EventType.NumLinesChanged);
            DocumentChanged = true;
        }
        public void InsertLine(string s, int LineNum)
        {
            lines.Insert(new Line(s), LineNum);

            undoManager.AddElement(new UndoElementInsertLine(LineNum, s));

            EventManager.DoEvent(EventManager.EventType.NumLinesChanged);

            DocumentChanged = true;
        }
        public void SetInitialSpaces(int LineNum, int NumSpaces)
        {
            Line l = this[LineNum];
            
            int x = Math.Max(0, l.FirstNonWhiteSpaceChar);

            string old = l.Text;

            l.Text = new string(' ', NumSpaces) + old.Substring(Math.Max(0, x));

            undoManager.AddElement(new UndoElementReplaceLine(LineNum, old, l.Text));

            DocumentChanged = true;

            System.Diagnostics.Debug.Assert(this[LineNum].Text.StartsWith(new string(' ', NumSpaces)));
        }
        private void combineLines(int LineNumber)
        {
            if (this.NumLines > LineNumber + 1)
            {
                this[LineNumber].Text += this[LineNumber + 1].Text;
                Delete(LineNumber + 1);
            }
        }
        private static string[] newLineChars = new string[] { Environment.NewLine };
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < this.NumLines; i++)
                sb.AppendLine(this[i].Text);

            return sb.ToString();
        }
        public string ToString(DocumentLocation DL)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < DL.LineNumber; i++)
                sb.AppendLine(this[i].Text);

            string s = this[DL.LineNumber].Text;
            sb.AppendLine(s.Substring(0, Math.Min(DL.ColumnNumber, s.Length)));

            return sb.ToString();
        }
    }
}
