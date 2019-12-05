/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace QuuxControls
{
    internal class View
    {
        private enum MoveType { Left, Right, Up, Down, LeftWord, RightWord, Home, End, PageUp, PageDown }

        private const int OFF_SCREEN_DRAG_DELAY = 50;

        private Document doc;
        private ViewPosition viewPosn;
        private TextFormatFlags tffr = TextFormatFlags.Right | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
        private Rectangle workingArea;
        private Rectangle textArea;
        private Rectangle lineNumberArea;
        private List<Rectangle> rects;
        private List<Rectangle> lineNumberRects;
        private int lineNumberMargin;
        private Control parent;
        private int preferredX = 0;
        private bool lineNumbering = false;
        private static readonly Point invalidXY = new Point(-1, -1);
        private DocumentLocation caretDocLoc = DocumentLocation.BOF;
        private Caret caret;
        private Point caretXY = Point.Empty;

        private DocumentRange selection = DocumentRange.Empty;
        private DocumentLocation selectionAnchor = null;

        private DocumentLocation selectAnchor = null;
        private bool dragging = false;
        private bool dragDelayOK = false;
        private Point lastMouseDragPoint = Point.Empty;
        private ulong mouseDragTimer = uint.MaxValue;

        private UndoManager undoManager;

        private bool insertMode = false;

        public View(Document Document, Control Parent)
        {
            doc = Document;
            EventManager.NumLinesChanged += updateLineNumberMargin;
            //EventManager.DocumentChanged += docChanged;
            //EventManager.CaretLocationChanged += caretPosnChanged;
            //EventManager.ViewChanged += viewChanged;
            
            //EventManager.SelectionChanged += () => { parent.Invalidate(); };

            this.parent = Parent;

            rects = new List<Rectangle>();
            lineNumberRects = new List<Rectangle>();

            FontInfo.FontMetricsChanged += updateMetrics;

            viewPosn = new ViewPosition();
            
            CaretDocLoc = DocumentLocation.BOF;

            undoManager = new UndoManager(this, doc);
            doc.UndoManager = undoManager;
        }
        public Font Font
        {
            get { return FontInfo.Default.Font; }
            set
            {
                FontInfo.SetFont(value);
            }
        }
        public int FirstVisibleLine
        {
            get { return viewPosn.Line; }
            set
            {
                viewPosn.Line = value;
            }
        }
        public int FirstVisibleSubLine
        {
            get { return viewPosn.SubLine; }
        }
        public int FirstVisibleColumn
        {
            get { return viewPosn.Column; }
            set
            {
                viewPosn.Column = value;
            }
        }

        public int NumVisibleLines
        {
            get { return rects.Count; }
        }
        public int LeftMargin
        {
            get { return lineNumberMargin; }
        }
        public Document Document
        {
            get { return doc; }
        }
        public bool LineNumbering
        {
            get { return lineNumbering; }
            set
            {
                if (lineNumbering != value)
                {
                    lineNumbering = value;
                    updateMetrics();
                }
            }
        }
        public Rectangle WorkingArea
        {
            get { return workingArea; }
            set
            {
                if (workingArea != value)
                {
                    workingArea = value;
                    updateMetrics();
                }
            }
        }
        public Rectangle TextArea
        {
            get { return textArea; }
        }
        public Point CaretXY
        {
            get { return caretXY; }
            private set { caretXY = value; }
        }
        public Point CaretPixelLoc
        {
            get
            {
                return pixelLoc(CaretXY);
            }
        }
        public DocumentLocation CaretDocLoc
        {
            get { return caretDocLoc; }
            set
            {
                if (caretDocLoc != value)
                {
                    caretDocLoc = value;
                    setCaretLocation(false);
                    EventManager.DoEvent(EventManager.EventType.CaretLocationChanged);
                }
            }
        }
        public DocumentRange Selection
        {
            get { return selection; }
            set
            {
                if (selection != value)
                {
                    if (value == null)
                        selection = DocumentRange.Empty;
                    else
                        selection = value;

                    if (selection.IsEmpty)
                        selectionAnchor = null;
                    
                    EventManager.DoEvent(EventManager.EventType.SeletionChanged);

                    parent.Invalidate();
                }
            }
        }
        public bool InsertMode
        {
            get { return insertMode; }
            set
            {
                if (insertMode != value)
                {
                    insertMode = value;
                    setupCaret();
                }
            }
        }

        public void Load(List<string> Text)
        {
            EventManager.HoldEvents();

            viewPosn.Reset();
            CaretDocLoc = DocumentLocation.BOF;
            Document.ReplaceTabs(Text);
            doc.Load(Text);

            setCaretLocation(true);
            parent.Invalidate();
            
            EventManager.ReleaseEvents(true);
        }

        public void Render(Graphics g)
        {
            g.Clear(Color.White);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            if (lineNumbering)
            {
                g.FillRectangle(Brushes.AliceBlue, lineNumberArea);
                g.DrawLine(Pens.DodgerBlue, lineNumberArea.Right, lineNumberArea.Top, lineNumberArea.Right, lineNumberArea.Bottom);
            }

            int line = viewPosn.Line;
            int numLines = NumVisibleLines;
            List<SubLine> subLines;

            int extraSubLines = viewPosn.SubLine; ;

            int i = 0;
            Line l = null;
            while (i < numLines && line < doc.NumLines)
            {
                    l = doc[line];

                if (line >= doc.NumLines)
                    return;

                int subLineIndex = 0;

                if (lineNumbering && extraSubLines == 0)
                    TextRenderer.DrawText(g, (line + 1).ToString(), FontInfo.Default.Font, lineNumberRects[i], Color.DodgerBlue, tffr);

                subLines = l.VisibleSubLines;

                while (i < numLines && subLineIndex < subLines.Count)
                {
                    if (extraSubLines-- > 0)
                    {
                        subLineIndex++;
                    }
                    else
                    {
                        SubLine sl = subLines[subLineIndex++];
                        sl.Render(g, rects[i++], viewPosn.Column, getSelectionArea(line, sl));
                    }
                }
                extraSubLines = 0;
                line++;
            }
        }
        
        public void MoveLeft(bool Select)
        {
            ViewEnvironment.BiasPreviousLine = false;
            move(MoveType.Left, Select);
            PreferredX = CaretXY.X;
        }
        public void MoveRight(bool Select)
        {
            ViewEnvironment.BiasPreviousLine = false;
            move(MoveType.Right, Select);
            PreferredX = CaretXY.X;
        }
        public void MoveUp(bool Select)
        {
            ViewEnvironment.BiasPreviousLine = PreferredX > 0;
            move(MoveType.Up, Select);
        }
        public void MoveDown(bool Select)
        {
            ViewEnvironment.BiasPreviousLine = PreferredX > 0;
            move(MoveType.Down, Select);
        }
        public void MoveRightWord(bool Select)
        {
            ViewEnvironment.BiasPreviousLine = true;
            move(MoveType.RightWord, Select);
            PreferredX = CaretXY.X;
        }
        public void MoveLeftWord(bool Select)
        {
            ViewEnvironment.BiasPreviousLine = false;
            move(MoveType.LeftWord, Select);
            PreferredX = CaretXY.X;
        }
        public void Home(bool Select)
        {
            ViewEnvironment.BiasPreviousLine = false;
            move(MoveType.Home, Select);
            PreferredX = 0;
        }
        public void End(bool Select)
        {
            ViewEnvironment.BiasPreviousLine = true;
            move(MoveType.End, Select);
            PreferredX = 0;
        }
        public void PageUp(bool Select)
        {
            ViewEnvironment.BiasPreviousLine = PreferredX > 0;

            move(MoveType.PageUp, Select);
        }
        public void PageDown(bool Select)
        {
            ViewEnvironment.BiasPreviousLine = PreferredX > 0;

            move(MoveType.PageDown, Select);
        }

        public bool ScrollView(int NumRows)
        {
            int l = viewPosn.Line;

            offset(NumRows);
            
            setCaretLocation(false);
            parent.Invalidate();

            return l != viewPosn.Line;
        }
        public void MakeVisible(DocumentRange DR)
        {
            makeVisible(DR.End);
            makeVisible(DR.Start);
        }

        public bool MouseDown(Point P)
        {
            if (lineNumberArea.Contains(P))
            {
                int l;
                int sl;
                getDocLineFromScreenY(getScreenXYFromPixel(P).Y, out l, out sl);

                List<SubLine> lsl = doc[l].GetSubLines(false);

                this.Selection = new DocumentRange(new DocumentLocation(l, lsl[sl].Start), new DocumentLocation(l, lsl[sl].End));
            }
            else
            {
                return false;
            }
            parent.Invalidate();

            return true;
        }
        public void MouseClick(Point P)
        {
            EventManager.HoldEvents();

            undoManager.ClosePackage();

            Clock.RemoveAlarm(mouseDragTimer);
            mouseDragTimer = Clock.NULL_ALARM;
            dragDelayOK = true;

            DocumentLocation dl = getDocLocFromScreenXY(getScreenXYFromPixel(P));

            if (selectAnchor != null)
            {
                CaretDocLoc = dl;
                PreferredX = CaretXY.X;
                selectAnchor = null;
            }
            else if (dragging && !Selection.IsEmpty)
            {
                dragging = false;
                parent.Cursor = Cursors.Default;
                string s = doc.GetText(Selection);
                DocumentRangeLength drl = Selection.DRLength;
                if (Selection.Start >= dl)
                {
                    doc.Delete(Selection);
                    Selection = DocumentRange.Empty;
                    doc.Insert(s, dl);
                    CaretDocLoc = dl;
                }
                else if (Selection.End <= dl)
                {
                    doc.Insert(s, dl);
                    doc.Delete(Selection);
                    Selection = DocumentRange.Empty;
                }
                CaretDocLoc = dl;
                Selection = new DocumentRange(dl, drl, doc);
            }
            else if (textArea.Contains(P))
            {
                CaretDocLoc = dl;
                PreferredX = xCoord(dl);
                this.Selection = DocumentRange.Empty;
            }

            setCaretLocation(false);
            parent.Invalidate();

            EventManager.ReleaseEvents(true);

            undoManager.ClosePackage();
        }
        public void MouseDrag(Point P)
        {
            DocumentLocation dl;

            if (P.Y < 0 || P.Y > textArea.Bottom)
            {
                lastMouseDragPoint = P;
                
                if (!dragDelayOK)
                    return;

                dragDelayOK = false;

                DocumentLocation dl2 = (selectAnchor == Selection.Start) ? Selection.End : Selection.Start;

                int l = dl2.LineNumber;
                int sl = doc[l].SubLineNum(dl2.ColumnNumber);

                if (doc.Offset((P.Y < 0 ? -1 : 1), ref l, ref sl))
                {
                    dl = new DocumentLocation(l, doc[l].SubLineStart(sl) + Math.Min(doc[l].GetSubLines(false)[sl].Length, doc.XCoord(dl2)));

                }
                else
                {
                    return;
                }
                
                Clock.Update(ref mouseDragTimer, makeDragDelayOK, OFF_SCREEN_DRAG_DELAY, false);
            }
            else
            {
                if (mouseDragTimer != Clock.NULL_ALARM)
                    Clock.RemoveAlarm(mouseDragTimer);
                
                mouseDragTimer = Clock.NULL_ALARM;
                dragDelayOK = true;
                dl = getDocLocFromScreenXY(getScreenXYFromPixel(P));
            }
            
            if (selectAnchor == null)
            {
                if (dragging)
                {
                    CaretDocLoc = dl;
                    setCaretLocation(true);
                }
                else if (!Selection.IsEmpty && Selection.Contains(dl))
                {
                    dragging = true;
                    CaretDocLoc = dl;
                    setCaretLocation(true);
                    parent.Cursor = Cursors.Hand;
                    dragDelayOK = true;
                }
                else
                {
                    dragDelayOK = true;
                    selectAnchor = dl;
                }
            }
            else
            {
                Selection = new DocumentRange(selectAnchor, dl);
                if (!Selection.IsEmpty)
                {
                    CaretDocLoc = (Selection.Start == selectAnchor) ? Selection.End : Selection.Start;
                    setCaretLocation(true);
                }
            }
            setCaretLocation(true);
            parent.Invalidate();
        }
        public void MouseDoubleClick(Point P)
        {
            DocumentLocation DL = getDocLocFromScreenXY(getScreenXYFromPixel(P));
            Selection = doc[DL.LineNumber].GetWordRange(DL);
            if (!Selection.IsEmpty)
            {
                CaretDocLoc = Selection.End;
                setCaretLocation(false);
            }
            setCaretLocation(false);
            parent.Invalidate();
        }

        // EDIT COMMANDS

        public void InsertLine(string s, int LineNumber)
        {
            EventManager.HoldEvents();

            Line l = doc[LineNumber];

            doc.InsertLine(s, LineNumber);

            setCaretLocation(true);
            parent.Invalidate();

            EventManager.ReleaseEvents(true);
        }
        public void Insert(char c)
        {
            EventManager.HoldEvents();

            if (!Selection.IsEmpty)
            {
                undoManager.ClosePackage();
                CaretDocLoc = Selection.Start;
                doc.Delete(Selection);
                Selection = DocumentRange.Empty;
            }
            if (insertMode)
                CaretDocLoc = doc.Replace(c, CaretDocLoc);
            else
                CaretDocLoc = doc.Insert(c, CaretDocLoc);

            setCaretLocation(true);

            parent.Invalidate();

            EventManager.ReleaseEvents(true);
        }
        public void Delete(bool Shift)
        {
            EventManager.HoldEvents();

            if (Selection.IsEmpty)
            {
                if (Shift)
                {
                    undoManager.ClosePackage();
                    
                    doc.Delete(CaretDocLoc.LineNumber);
                    CaretDocLoc = doc.EnsureValid(CaretDocLoc);
                    
                    undoManager.ClosePackage();
                }
                else
                {
                    doc.Delete(CaretDocLoc);
                }
            }
            else
            {
                undoManager.ClosePackage();

                CaretDocLoc = Selection.Start;

                doc.Delete(Selection);
                Selection = DocumentRange.Empty;

                setCaretLocation(false);

                undoManager.ClosePackage();
            }
            setCaretLocation(true);
            parent.Invalidate();
            
            EventManager.ReleaseEvents(true);
        }
        public void Backspace()
        {
            if (Selection.IsEmpty)
            {
                if (CaretDocLoc > DocumentLocation.BOF)
                    CaretDocLoc = doc.GetPreviousLocation(CaretDocLoc);
                else
                    return;
            }

            Delete(false);
        }
        public void Enter()
        {
            undoManager.ClosePackage();
            EventManager.HoldEvents();
            
            if (!Selection.IsEmpty)
            {
                CaretDocLoc = Selection.Start;
                doc.Delete(Selection);
            }
            
            doc.SplitLine(CaretDocLoc);

            Line l = doc[CaretDocLoc.LineNumber];

            int x = l.FirstNonWhiteSpaceChar;
            
            if (x < 0)
                x = 0;

            if (l.Text.EndsWith("{"))
                x += doc.SpacesInTab;

            doc.SetInitialSpaces(CaretDocLoc.LineNumber + 1, x);
            CaretDocLoc = new DocumentLocation(CaretDocLoc.LineNumber + 1, x);

            PreferredX = x;

            setCaretLocation(true);
            parent.Invalidate();

            undoManager.ClosePackage();
            EventManager.ReleaseEvents(true);
        }
        public void Tab(bool Shift)
        {
            EventManager.HoldEvents();

            if (Shift)
            {
                int startLine;
                int endLine;
                if (Selection.IsEmpty)
                {
                    startLine = CaretDocLoc.LineNumber;
                    endLine = CaretDocLoc.LineNumber;
                }
                else
                {
                    startLine = Selection.Start.LineNumber;
                    endLine = Selection.End.LineNumber;
                }
                undoManager.ClosePackage();
                for (int i = startLine; i <= endLine; i++)
                {
                    Line l = doc[i];
                    int x = 0;
                    while (x < l.Text.Length && x < doc.SpacesInTab && l.Text[x] == ' ')
                        x++;
                    doc.Delete(new DocumentRange(i, 0, i, x));
                }
            }
            else
            {
                if (Selection.MultiLine)
                {
                    string s = String.Empty;
                    for (int i = 0; i < doc.SpacesInTab; i++)
                        s += ' ';

                    undoManager.ClosePackage();
                    for (int i = selection.Start.LineNumber; i <= selection.End.LineNumber; i++)
                    {
                        doc.Insert(s, new DocumentLocation(i, 0));
                    }
                    CaretDocLoc = doc.EnsureValid(new DocumentLocation(CaretDocLoc.LineNumber, CaretDocLoc.ColumnNumber + doc.SpacesInTab));
                }
                else
                {
                    string s = String.Empty;

                    for (int i = 0; i < doc.SpacesInTab - (CaretDocLoc.ColumnNumber % doc.SpacesInTab); i++)
                        s += ' ';

                    CaretDocLoc = doc.Insert(s, CaretDocLoc);
                }
            }
            
            parent.Invalidate();
            setCaretLocation(true);

            undoManager.ClosePackage();
            EventManager.ReleaseEvents(true);
        }
        public void Cut()
        {
            undoManager.ClosePackage();
            EventManager.HoldEvents();

            if (Selection != null && !Selection.IsEmpty)
            {
                Clipboard.SetText(doc.GetText(Selection));
                Delete(false);
            }
            setCaretLocation(true);
            parent.Invalidate();

            EventManager.ReleaseEvents(true);

            undoManager.ClosePackage();
        }
        public void Copy()
        {
            if (Selection != null && !Selection.IsEmpty)
                Clipboard.SetText(doc.GetText(Selection));
        }
        public void Paste()
        {
            undoManager.ClosePackage();
            EventManager.HoldEvents();

            if (!Selection.IsEmpty)
            {
                CaretDocLoc = Selection.Start;
                doc.Delete(Selection);
                Selection = DocumentRange.Empty;
            }

            CaretDocLoc = Insert(Clipboard.GetText(), CaretDocLoc);

            setCaretLocation(false);
            parent.Invalidate();
            EventManager.ReleaseEvents(true);
            undoManager.ClosePackage();
        }

        public DocumentLocation Insert(string Text, DocumentLocation dl)
        {
            List<string> ss = Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).ToList();
            Document.ReplaceTabs(ss);
            string s = String.Join(Environment.NewLine, ss.ToArray());

            if (s.Length > 0)
                dl = doc.Insert(s, dl);
            
            return dl;
        }
        
        public void Undo()
        {
            EventManager.HoldEvents();
            doc.Undo();

            if (this.Selection.IsEmpty)
            {
                setCaretLocation(true);
            }
            else
            {
                makeVisible(Selection.End);
                makeVisible(Selection.Start);
                setCaretLocation(false);
            }

            parent.Invalidate();
            EventManager.ReleaseEvents(true);
        }
        public void Redo()
        {
            EventManager.HoldEvents();
            doc.Redo();

            if (this.Selection.IsEmpty)
            {
                setCaretLocation(true);
            }
            else
            {
                makeVisible(Selection.End);
                makeVisible(Selection.Start);
                setCaretLocation(false);
            }

            parent.Invalidate();
            EventManager.ReleaseEvents(true);
        }
        public void Bookmark()
        {
            undoManager.ClosePackage();
        }

        public void Clear()
        {
            EventManager.HoldEvents();

            CaretDocLoc = DocumentLocation.BOF;
            this.Selection = DocumentRange.Empty;
            viewPosn.Reset();
            doc.Clear();

            EventManager.ReleaseEvents(true);

            setCaretLocation(true);
            parent.Invalidate();
        }
        public void ClearUndo()
        {
            undoManager.Clear();
        }
        public void SetInitialSpaces(int LineNum, int NumSpaces)
        {
            EventManager.HoldEvents();

            doc.SetInitialSpaces(LineNum, NumSpaces);
            
            setCaretLocation(true);
            
            parent.Invalidate();

            EventManager.ReleaseEvents(true);
        }
        public int GetInitialSpaces(int LineNum)
        {
            return doc[LineNum].NumInitialSpaces;
        }

        public void EnableCaret()
        {
            caret.Show();
        }
        public void DisableCaret()
        {
            caret.Hide();
        }
        public DocumentLocation GetDocumentLocation(Point Pixel)
        {
            return getDocLocFromScreenXY(getScreenXYFromPixel(Pixel));
        }

        private int PreferredX
        {
            get { return preferredX; }
            set
            {
                preferredX = Math.Max(0, value);
            }
        }
        private int LineNumberMargin
        {
            get { return lineNumberMargin; }
            set
            {
                if (lineNumberMargin != value)
                {
                    lineNumberMargin = value;
                    updateMetrics();
                }
            }
        }
        
        private void setupCaret()
        {
            caret = new Caret(parent.Handle);
            caret.Size = new Size(insertMode ? FontInfo.Width : 1, FontInfo.Height);

            setCaretLocation(false);
            caret.Show();
        }
        private int getNewLineNumMargin()
        {
            int margin;

            if (doc.NumLines < 10)
                margin = 5 + FontInfo.Width;
            else if (doc.NumLines < 100)
                margin = 5 + 2 * FontInfo.Width;
            else if (doc.NumLines < 1000)
                margin = 5 + 3 * FontInfo.Width;
            else if (doc.NumLines < 10000)
                margin = 5 + 4 * FontInfo.Width;
            else if (doc.NumLines < 100000)
                margin = 5 + 5 * FontInfo.Width;
            else if (doc.NumLines < 1000000)
                margin = 5 + 6 * FontInfo.Width;
            else
                margin = 5 + 7 * FontInfo.Width;

            return margin;
        }
        private void updateMetrics()
        {
            rects.Clear();
            lineNumberRects.Clear();

            int ypos = workingArea.Top;

            lineNumberMargin = lineNumbering ? getNewLineNumMargin() : 0;

            lineNumberArea = new Rectangle(0, 0, lineNumberMargin, workingArea.Height);

            textArea = new Rectangle(lineNumberMargin + 1, 0, workingArea.Width - lineNumberMargin, workingArea.Height);

            while (ypos < workingArea.Bottom - FontInfo.Height)
            {
                lineNumberRects.Add(new Rectangle(lineNumberArea.Left, ypos, lineNumberArea.Width - 1, FontInfo.Height));
                rects.Add(new Rectangle(textArea.Left, ypos, textArea.Width, FontInfo.Height));

                ypos += FontInfo.Height;
            }

            ViewEnvironment.NumVisibleColumns = textArea.Width / FontInfo.Width;
            ViewEnvironment.NumColumnsPerLine = ViewEnvironment.NumVisibleColumns;

            setupCaret();

            EventManager.DoEvent(EventManager.EventType.ViewChanged);
        }
        private void updateLineNumberMargin()
        {
            LineNumberMargin = getNewLineNumMargin();
        }
        private void makeVisible(DocumentLocation DL)
        {
            if (ViewEnvironment.WordWrap)
            {
                // CHECK VISIBLE Y

                if (DL.LineNumber < viewPosn.Line)
                {
                    viewPosn.Line = DL.LineNumber;
                    viewPosn.SubLine = doc[DL.LineNumber].SubLineNum(DL.ColumnNumber);
                }
                else if (DL.LineNumber == viewPosn.Line)
                {
                    int sl = doc[DL.LineNumber].SubLineNum(DL.ColumnNumber);
                    if (sl < viewPosn.SubLine)
                    {
                        viewPosn.SubLine = sl;
                    }
                }
                else
                {
                    int ll;
                    int lsl;
                    getLastVisibleLineAndSubline(out ll, out lsl);
                    if (DL.LineNumber > ll || (DL.LineNumber == ll && doc[DL.LineNumber].SubLineNum(DL.ColumnNumber) > lsl))
                    {
                        int l = DL.LineNumber;
                        int sl = doc[DL.LineNumber].SubLineNum(DL.ColumnNumber);

                        doc.Offset(-NumVisibleLines + 1, ref l, ref sl);

                        viewPosn.Set(l, sl, viewPosn.Column);
                    }
                }

                // CHECK VISIBLE X

                int x = xCoord(DL);

                if (x < viewPosn.Column)
                {
                    viewPosn.Column = x;
                }
                else if (x > viewPosn.Column + ViewEnvironment.NumVisibleColumns)
                {
                    viewPosn.Column = x - ViewEnvironment.NumVisibleColumns + 1;
                }
            }
            else // no word wrap
            {
                if (DL.LineNumber < viewPosn.Line)
                {
                    viewPosn.Line = DL.LineNumber;
                }
                else if (DL.LineNumber >= viewPosn.Line + NumVisibleLines)
                {
                    viewPosn.Line = DL.LineNumber - NumVisibleLines + 1;
                }
                if (DL.ColumnNumber < viewPosn.Column)
                {
                    viewPosn.Column = DL.ColumnNumber;
                }
                else if (DL.ColumnNumber >= viewPosn.Column + ViewEnvironment.NumVisibleColumns)
                {
                    viewPosn.Column = DL.ColumnNumber - ViewEnvironment.NumVisibleColumns + 1;
                }
            }
        }
        private DocumentLocation getDocLocFromScreenXY(Point P)
        {
            int line;
            int subLine;
            
            getDocLineFromScreenY(P.Y, out line, out subLine);

            return new DocumentLocation(doc, line, subLine, P.X);
        }
        private void getDocLineFromScreenY(int Y, out int Line, out int SubLine)
        {
            Line = viewPosn.Line;
            SubLine = viewPosn.SubLine;

            doc.Offset(Y, ref Line, ref SubLine);

            return;
        }
        private int xCoord(DocumentLocation DL)
        {
            return Math.Min(ViewEnvironment.NumVisibleColumns + viewPosn.Column, doc.XCoord(DL));
        }
        private Point getScreenXYFromDocLoc(DocumentLocation DL)
        {
            if ((DL == null) || DL.LineNumber < viewPosn.Line ||DL.LineNumber > getLastVisibleLine())
                return invalidXY;

            Line l = doc[DL.LineNumber];
            int sl = l.SubLineNum(DL.ColumnNumber);
           
            Point p = new Point(xCoord(DL) - FirstVisibleColumn,
                                doc.VisibleRowCount(viewPosn.Line,
                                          viewPosn.SubLine,
                                          DL.LineNumber,
                                          sl));

            if (p.Y < 0 || p.Y >= NumVisibleLines || p.X < 0 || p.X > ViewEnvironment.NumVisibleColumns)
                return invalidXY;

            return p;
        }
        private void setCaretLocation(bool EnsureVisible)
        {
            if (EnsureVisible)
                makeVisible(CaretDocLoc);

            CaretXY = getScreenXYFromDocLoc(CaretDocLoc);

            if (CaretXY.X < 0)
            {
                System.Diagnostics.Debug.Assert(!EnsureVisible);
                caret.Hide();
            }
            else
            {
                caret.Location = pixelLoc(CaretXY);
                caret.Show();
            }
        }

        private Point pixelLoc(Point XY)
        {
            return new Point(textArea.Left + XY.X * FontInfo.Width, textArea.Top + XY.Y * FontInfo.Height);
        }
        private Point getScreenXYFromPixel(Point P)
        {
            return new Point((Math.Max(0, P.X - TextArea.Left + FontInfo.Width / 2)) / FontInfo.Width,
                              Math.Max(0, (P.Y - TextArea.Top) / FontInfo.Height));
        }
        private bool offset(int LineCount)
        {
            // returns true if not limited by doc bounds
            int l = viewPosn.Line;
            int sl = viewPosn.SubLine;

            bool res = doc.Offset(LineCount, ref l, ref sl);

            viewPosn.Set(l, sl, viewPosn.Column);
            
            return res;
        }
        private void getLastVisibleLineAndSubline(out int Line, out int Subline)
        {
            Line = viewPosn.Line;
            Subline = viewPosn.SubLine;

            doc.Offset(NumVisibleLines - 1, ref Line, ref Subline);
        }
        private int getLastVisibleLine()
        {
            int l;
            int ll;
            getLastVisibleLineAndSubline(out l, out ll);
            return l;
        }
        private Rectangle getCharRectangle(Point XY)
        {
            if (XY.X < 0)
                return Rectangle.Empty;
            else
                return new Rectangle(textArea.Left + XY.X * FontInfo.WidthTimes100 / 100 + 1,
                                     textArea.Top + XY.Y * FontInfo.Height,
                                     FontInfo.Width - 2,
                                     FontInfo.Height);
        }
        private int charCount(string s, char c)
        {
            int count = 0;
            for (int i = 0; i < s.Length; i++)
                if (s[i] == c)
                    count++;
            return count;
        }
        private Point getSelectionArea(int LineNum, SubLine SubLine)
        {
            if (Selection == null || Selection.IsEmpty)
                return Point.Empty;

            if (Selection.End.LineNumber < LineNum)
                return Point.Empty;

            if (Selection.Start.LineNumber > LineNum)
                return Point.Empty;

            if (Selection.Start.LineNumber < LineNum && Selection.End.LineNumber > LineNum)
                return new Point(0, int.MaxValue / 2);

            if (Selection.Start.LineNumber < LineNum)
                return new Point(0, Selection.End.ColumnNumber - SubLine.Start);

            if (Selection.End.LineNumber > LineNum)
                return new Point(Math.Max(0, Selection.Start.ColumnNumber - SubLine.Start), int.MaxValue / 2);

            if (selection.End.ColumnNumber <= SubLine.Start)
                return Point.Empty;

            return new Point(Math.Max(0, Selection.Start.ColumnNumber - SubLine.Start), Selection.End.ColumnNumber - Math.Max(SubLine.Start, Selection.Start.ColumnNumber));
        }
        private void move(MoveType MoveType, bool Select)
        {
            DocumentLocation old = CaretDocLoc;

            if (!Select)
                Selection = DocumentRange.Empty;

            undoManager.ClosePackage();

            switch (MoveType)
            {
                case MoveType.Left:
                    CaretDocLoc = doc.GetPreviousVisibleLocation(CaretDocLoc);
                    break;
                case MoveType.Right:
                    CaretDocLoc = doc.GetNextVisibleLocation(CaretDocLoc);
                    break;
                case MoveType.Up:
                    if (CaretXY == invalidXY)
                        setCaretLocation(true);
                    CaretDocLoc = getDocLocFromScreenXY(new Point(PreferredX, CaretXY.Y - 1));
                    break;
                case MoveType.Down:
                    if (CaretXY == invalidXY)
                        setCaretLocation(true);
                    CaretDocLoc = getDocLocFromScreenXY(new Point(PreferredX, CaretXY.Y + 1));
                    break;
                case MoveType.RightWord:
                    CaretDocLoc = doc.GetNextWord(CaretDocLoc);
                    break;
                case MoveType.LeftWord:
                    CaretDocLoc = doc.GetPreviousWord(CaretDocLoc);
                    break;
                case MoveType.Home:
                    Line l = doc[CaretDocLoc.LineNumber];
                    int sln = l.SubLineNum(caretDocLoc.ColumnNumber);
                    int x = l.GetSubLines(false)[sln].FirstNonWSChar();

                    if (CaretDocLoc.ColumnNumber == 0 && x > 0)
                        CaretDocLoc = new DocumentLocation(CaretDocLoc.LineNumber, x);
                    else if (x > 0 && x < CaretDocLoc.ColumnNumber - l.BeginningOfSubLine(sln))
                        CaretDocLoc = new DocumentLocation(CaretDocLoc.LineNumber, x + l.BeginningOfSubLine(sln));
                    else
                        CaretDocLoc = new DocumentLocation(CaretDocLoc.LineNumber, l.BeginningOfSubLine(sln));
                    break;
                case MoveType.End:
                    CaretDocLoc = new DocumentLocation(CaretDocLoc.LineNumber, doc[CaretDocLoc.LineNumber].EndOfSubLine(doc[CaretDocLoc.LineNumber].SubLineNum(CaretDocLoc.ColumnNumber)));
                    break;
                case MoveType.PageUp:
                    if (CaretXY.X < 0)
                        makeVisible(CaretDocLoc);
                    {
                        Point p = CaretXY;
                        p.X = PreferredX;
                        if (!offset(-NumVisibleLines))
                            p.Y = 0;
                        CaretDocLoc = getDocLocFromScreenXY(p);
                    }
                    break;
                case MoveType.PageDown:
                    if (CaretXY.X < 0)
                        makeVisible(CaretDocLoc);
                    {
                        Point p = CaretXY;
                        p.X = PreferredX;

                        if (offset(NumVisibleLines))
                            CaretDocLoc = getDocLocFromScreenXY(p);
                        else
                            CaretDocLoc = new DocumentLocation(doc, doc.NumLines - 1, doc[doc.NumLines - 1].NumVisibleSubLines - 1, PreferredX);
                    }
                    break;
            }

            if (Select)
            {
                if (selectionAnchor == null)
                    selectionAnchor = old;
                Selection = new DocumentRange(selectionAnchor, CaretDocLoc);
            }
            setCaretLocation(true);
            parent.Invalidate();
        }
        private void makeDragDelayOK()
        {
            dragDelayOK = true;
            mouseDragTimer = Clock.NULL_ALARM;
            if (dragging || selectAnchor != null)
                MouseDrag(lastMouseDragPoint);
        }

        public override string ToString()
        {
            return doc.ToString();
        }
        public string ToString(DocumentLocation DL)
        {
            return doc.ToString(DL);
        }
    }
}
