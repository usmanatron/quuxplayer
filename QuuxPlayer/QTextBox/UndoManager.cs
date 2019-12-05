/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxControls
{
    internal class UndoManager
    {
        private View view;
        private Document doc;
        private Stack<UndoPackage> undo;
        private Stack<UndoPackage> redo;
        private UndoPackage currentPackage;
        private bool suspend = false;

        public UndoManager(View View, Document Document )
        {
            view = View;
            doc = Document;
            undo = new Stack<UndoPackage>();
            redo = new Stack<UndoPackage>();
            currentPackage = null;
        }
        public void AddElement(UndoElement Element)
        {
            if (!suspend)
            {
                if (currentPackage == null)
                    currentPackage = new UndoPackage(view.CaretDocLoc, view.Selection, doc.DocumentChanged);
                currentPackage.Add(Element);
                redo.Clear();
            }
        }
        public void ClosePackage()
        {
            if (currentPackage != null)
            {
                if (currentPackage.Count > 0)
                {
                    currentPackage.Commit(view.CaretDocLoc, view.Selection, doc.DocumentChanged);
                    undo.Push(currentPackage);
                }
                currentPackage = null;
            }
        }
        public void Clear()
        {
            undo.Clear();
            redo.Clear();
            currentPackage = null;
        }
        public UndoPackage GetUndoPackage()
        {
            ClosePackage();
            if (undo.Count > 0)
            {
                UndoPackage up = undo.Pop();
                redo.Push(up);
                return up;
            }
            else
            {
                return null;
            }
        }
        public UndoPackage GetRedoPackage()
        {
            ClosePackage();
            if (redo.Count > 0)
            {
                UndoPackage rp = redo.Pop();
                undo.Push(rp);
                return rp;
            }
            else
            {
                return null;
            }
        }
        public void Suspend()
        {
            suspend = true;
        }
        public void Resume()
        {
            suspend = false;
        }
        public void SetOldView(UndoPackage UP)
        {
            view.CaretDocLoc = UP.OldCaretPosn;
            view.Selection = UP.OldSelection;
            doc.DocumentChanged = UP.OldDocumentChanged;
        }
        public void SetNewView(UndoPackage UP)
        {
            view.CaretDocLoc = UP.NewCaretPosn;
            view.Selection = UP.NewSelection;
            doc.DocumentChanged = UP.NewDocumentChanged;
        }
    }
}
