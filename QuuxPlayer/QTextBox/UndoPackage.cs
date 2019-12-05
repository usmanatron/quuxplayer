/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxControls
{
    internal class UndoPackage
    {
        private List<UndoElement> elements;
        private DocumentLocation oldCaretPosn;
        private DocumentLocation newCaretPosn = null;
        private DocumentRange oldSelection;
        private DocumentRange newSelection = null;
        private bool oldDocumentChanged;
        private bool newDocumentChanged;

        public UndoPackage(DocumentLocation OldCaretPosn,
                           DocumentRange OldSelection,
                           bool OldDocumentChanged)
        {
            oldCaretPosn = OldCaretPosn;
            oldSelection = OldSelection;
            oldDocumentChanged = OldDocumentChanged;

            elements = new List<UndoElement>();
        }
        public void Add(UndoElement Element)
        {
            elements.Add(Element);
        }
        public List<UndoElement> Elements
        {
            get { return elements; }
        }
        public int Count
        {
            get { return elements.Count; }
        }
        public void Commit(DocumentLocation NewCaretPosn,
                           DocumentRange NewSelection,
                           bool NewDocumentChanged)
        {
            newCaretPosn = NewCaretPosn;
            newSelection = NewSelection;
            newDocumentChanged = NewDocumentChanged;
        }
        public DocumentLocation OldCaretPosn
        {
            get { return oldCaretPosn; }
        }
        public DocumentLocation NewCaretPosn
        {
            get { return newCaretPosn; }
        }
        public DocumentRange OldSelection
        {
            get { return oldSelection; }
        }
        public DocumentRange NewSelection
        {
            get { return newSelection; }
        }
        public bool OldDocumentChanged
        {
            get { return oldDocumentChanged; }
        }
        public bool NewDocumentChanged
        {
            get { return newDocumentChanged; }
        }
    }
}
