/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxControls
{
    internal abstract class UndoElement
    {
        public enum UndoType { InsertChar, DeleteChar, ReplaceChar, InsertLine, DeleteLine, ReplaceLine }

        protected UndoType type;
        protected DocumentLocation dl;
        protected char c;
        protected char c2;
        protected string s = null;
        protected string s2 = null;

        public abstract void Undo(Document Document);
        public abstract void Redo(Document Document);
    }
    internal class UndoElementInsertChar : UndoElement
    {
        public UndoElementInsertChar(DocumentLocation DL, char c)
        {
            this.type = UndoType.InsertChar;
            this.dl = DL;
            this.c = c;
        }
        public override void Undo(Document Document)
        {
            Document.Delete(dl);
        }
        public override void Redo(Document Document)
        {
            Document.Insert(c, dl);
        }
    }
    internal class UndoElementDeleteChar : UndoElement
    {
        public UndoElementDeleteChar(DocumentLocation DL, char c)
        {
            this.type = UndoType.DeleteChar;
            this.dl = DL;
            this.c = c;
        }
        public override void Undo(Document Document)
        {
            Document.Insert(c, dl);
        }
        public override void Redo(Document Document)
        {
            Document.Delete(dl);
        }
    }
    internal class UndoElementReplaceChar : UndoElement
    {
        public UndoElementReplaceChar(DocumentLocation DL, char Old, char New)
        {
            this.type = UndoType.ReplaceChar;
            dl = DL;
            c = Old;
            c2 = New;
        }
        public override void Undo(Document Document)
        {
            Document.Replace(c2, dl);
        }
        public override void Redo(Document Document)
        {
            Document.Replace(c, dl);
        }
    }
    internal class UndoElementInsertLine : UndoElement
    {
        public UndoElementInsertLine(int LineNumber, string Text)
        {
            System.Diagnostics.Debug.Assert(!Text.Contains(Environment.NewLine));
            this.type = UndoType.InsertLine;
            this.dl = new DocumentLocation(LineNumber, 0);
            this.s = Text;
        }
        public override void Undo(Document Document)
        {
            Document.Delete(dl.LineNumber);
        }
        public override void Redo(Document Document)
        {
            Document.InsertLine(s, dl.LineNumber);
        }
    }
    internal class UndoElementDeleteLine : UndoElement
    {
        public UndoElementDeleteLine(int LineNumber, string Text)
        {
            System.Diagnostics.Debug.Assert(!Text.Contains(Environment.NewLine));
            this.type = UndoType.DeleteLine;
            this.dl = new DocumentLocation(LineNumber, 0);
            this.s = Text;
        }
        public override void Undo(Document Document)
        {
            Document.InsertLine(this.s, dl.LineNumber);
        }
        public override void Redo(Document Document)
        {
            Document.Delete(dl.LineNumber);
        }
    }
    internal class UndoElementReplaceLine : UndoElement
    {
        public UndoElementReplaceLine(int LineNumber, string Old, string New)
        {
            System.Diagnostics.Debug.Assert(!Old.Contains(Environment.NewLine));
            System.Diagnostics.Debug.Assert(!New.Contains(Environment.NewLine));

            this.type = UndoType.ReplaceLine;
            dl = new DocumentLocation(LineNumber, 0);
            s = Old;
            s2 = New;
        }
        public override void Undo(Document Document)
        {
            Document.Delete(dl.LineNumber);
            Document.InsertLine(s, dl.LineNumber);
        }
        public override void Redo(Document Document)
        {
            Document.Delete(dl.LineNumber);
            Document.InsertLine(s2, dl.LineNumber);
        }
    }
}
