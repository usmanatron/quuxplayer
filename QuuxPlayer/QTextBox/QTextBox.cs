/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxControls
{
    public delegate void DocumentLocationDelegate(DocumentLocation DL);
    public delegate void BeforeShowIntellisense(object sender, BeforeShowIntellisenseEventArgs e);

    public partial class QTextBox : UserControl
    {
        public event BeforeShowIntellisense BeforeShowIntellisense;
        public event DocumentLocationDelegate CaretLocationChanged;
        public EventHandler DocumentChangedChanged;
        public EventHandler SelectionChanged;
        public EventHandler DocumentChanged;
        public EventHandler NeedWordsBeforeCursorRefresh;

        private const int SCROLL_BAR_WIDTH = 14;
        
        private View currentView;
        private Document curDoc;
        private QVScrollBar vsb;
        private QHScrollBar hsb;

        private KeywordInfo ki;

        private frmIntellisense intellisense = null;

        private Font font = new Font("Courier New", 12f, FontStyle.Regular);

        private int numVisibleColumns = 0;
        private static readonly Point invalidXY = new Point(-1, -1);
        private Point mouseDownPoint = invalidXY;
        private bool mouseDrag = false;
        private Rectangle mouseDragRect = Rectangle.Empty;
        private bool doubleClicked = false;
        private StringComparer comparer;
        private List<string> wordsBeforeCursor = null;

        public QTextBox(Font Font, StringComparer Comparer)
        {
            InitializeComponent(); // NOP
            
            this.SuspendLayout();
            
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "QTextBox";
            
            this.DoubleBuffered = true;
            this.BackColor = Color.White;

            FontInfo.SetFont(Font);

            ki = new KeywordInfo(comparer = Comparer);
            SubLine.SetKeywordInfo(ki);
            Line.SetKeywordInfo(ki);

            currentView = new View(new Document(4), this);
            curDoc = currentView.Document;

            EventManager.ViewChanged += () =>
            {
                vsb.SetValue(currentView.FirstVisibleLine);
                this.Invalidate();
            };

            EventManager.DocumentChanged += () => { if (DocumentChanged != null) DocumentChanged.Invoke(this, EventArgs.Empty); };
            EventManager.CaretLocationChanged += () => { if (CaretLocationChanged != null) CaretLocationChanged.Invoke(currentView.CaretDocLoc); };
            EventManager.SelectionChanged += () => { if (SelectionChanged != null) SelectionChanged.Invoke(this, EventArgs.Empty); };
            EventManager.NumLinesChanged += updateMaxLinesChanged;
            EventManager.LongestLineChanged += updateForLongestLine;

            vsb = new QVScrollBar(SCROLL_BAR_WIDTH);
            this.Controls.Add(vsb);

            vsb.ValChanged += (e) =>
            {
                switch (e.Delta)
                {
                    case -1:
                        if (!currentView.ScrollView(-1))
                            e.Accept = false;
                        break;
                    case 1:
                        if (!currentView.ScrollView(1))
                            e.Accept = false;
                        break;
                    default:
                        currentView.FirstVisibleLine = vsb.Value;
                        break;
                }
            };

            hsb = new QHScrollBar(SCROLL_BAR_WIDTH);
            this.Controls.Add(hsb);

            hsb.ValueChanged += (s, e) =>
            {
                currentView.FirstVisibleColumn = hsb.Value;
                this.Invalidate();
            };

            hsb.Visible = false;

            this.ResumeLayout(false);

            ViewEnvironment.NumColumnsPerLineChanged += setMetrics;
            FontInfo.FontMetricsChanged += setMetrics;
            EventManager.DocumentChangedChanged += () => { if (this.DocumentChangedChanged != null) DocumentChangedChanged.Invoke(this, EventArgs.Empty); };

            this.Size = new System.Drawing.Size(492, 313);
        }
        ~QTextBox()
        {
            Clock.Close();
        }

        public string this[int Index]
        {
            get { return curDoc[Index].Text; }
            set
            {
                curDoc.Replace(value.Replace(Environment.NewLine, " "), Index);
            }
        }
        public string LineWithoutComments(int Index)
        {
            return curDoc[Index].TextWithoutComments;
        }

        private void updateForLongestLine()
        {
            int max = Math.Max(0, currentView.Document.LongestLine - numVisibleColumns);
            hsb.MaxValue = max;
        }

        public void LoadFile(string FilePath)
        {
            List<string> s = new List<string>();
            StreamReader sr = new StreamReader(FilePath, true);

            string ss;
            while ((ss = sr.ReadLine()) != null)
                s.Add(ss);

            if (s.Count == 0)
                s.Add(String.Empty);

            vsb.SetValue(0);
            currentView.Load(s);

            sr.Close();
            sr.Dispose();

            this.Invalidate();
        }
        public void SaveFile(string FilePath)
        {
            File.Delete(FilePath);
            StreamWriter sw = new StreamWriter(FilePath);
            for (int i = 0; i < curDoc.NumLines; i++)
                sw.WriteLine(curDoc[i].Text);
            sw.Close();
            sw.Dispose();

            curDoc.DocumentChanged = false;
        }
        public void Clear()
        {
            currentView.Clear();
            currentView.ClearUndo();
            //ki.Clear(comparer);
        }

        public void SetKeywordGroupStyle(int Group, FontStyle FontStyle, Color Color)
        {
            ki.SetFontInfo(Group, FontStyle, Color);
            this.Invalidate();
        }
        public void AddKeywords(int Group, string[] Keywords)
        {
            ki.Add(Group, Keywords);
            this.Invalidate();
        }
        public void ClearKeywords()
        {
            ki.Clear(comparer);
            this.Invalidate();
        }
        public void ClearUndo()
        {
            currentView.ClearUndo();
        }

        public void Insert(DocumentLocation DL, string Contents)
        {
            currentView.Insert(Contents, DL);
        }

        public override Font Font
        {
            get
            {
                return FontInfo.Default.Font;
            }
            set
            {
                FontInfo.SetFont(value);
            }
        }
        public float FontSize
        {
            get { return FontInfo.FontSize; }
            set
            {
                FontInfo.FontSize = value;
            }
        }

        public int NumLines { get { return curDoc.NumLines; } }
        public bool WordWrap { get { return ViewEnvironment.WordWrap; } set { ViewEnvironment.WordWrap = value; } }
        public bool LineNumbering { get { return currentView.LineNumbering; } set { currentView.LineNumbering = value; } }
        public DocumentLocation CaretLocation
        {
            get { return currentView.CaretDocLoc; }
            set { currentView.CaretDocLoc = value; }
        }
        public int CaretIndex { get { return currentView.Document.CharNum(CaretLocation); } }
        public int CaretIndexOneBased { get { return currentView.Document.CharNum(CaretLocation) + 1; } }
        public DocumentRange Selection
        {
            get
            {
                if (currentView.Selection == null)
                    return DocumentRange.Empty;
                else
                    return currentView.Selection;
            }
            set { currentView.Selection = value; }
        }
        public int SelectionLength
        {
            get
            {
                return currentView.Selection.Length(currentView.Document); 
            }
        }
        public DocumentRange EntireRange
        {
            get { return new DocumentRange(DocumentLocation.BOF, currentView.Document.EOF); }
        }
        public bool Changed
        {
            get { return curDoc.DocumentChanged; }
            set { curDoc.DocumentChanged = value; }
        }
        public string SelectedText
        {
            get { return currentView.Document.GetText(Selection); }
        }
        public DocumentLocation EOF
        {
            get { return curDoc.EOF; }
        }
        public List<string> WordsBeforeCursor
        {
            get
            {
                return wordsBeforeCursor;
            }
            set
            {
                wordsBeforeCursor = value;
            }
        }
        public bool IsCommentedOrQuoted(DocumentLocation DL)
        {
            return currentView.Document[DL.LineNumber].IsCommentedOrQuoted(DL.ColumnNumber);
        }

        public void SetInitialSpaces(int LineNum, int NumSpaces)
        {
            currentView.SetInitialSpaces(LineNum, NumSpaces);
        }
        public int GetInitialSpaces(int LineNum)
        {
            return currentView.GetInitialSpaces(LineNum);
        }

        public void InsertLine(string Text, int LineNumber)
        {
            currentView.InsertLine(Text, LineNumber);
        }
        public bool CaretIsAfterWhiteSpaceOrParen
        {
            get
            {
                DocumentLocation dl = CaretLocation;
                if (dl.ColumnNumber == 0)
                    return true;

                char c = currentView.Document[dl.LineNumber].Text[dl.ColumnNumber - 1];

                return c <= ' ' || c == '(' || c == ')';
            }
        }
        public bool CaretIsAfterWhiteSpace
        {
            get
            {
                DocumentLocation dl = CaretLocation;
                if (dl.ColumnNumber == 0)
                    return true;

                char c = currentView.Document[dl.LineNumber].Text[dl.ColumnNumber - 1];

                return c <= ' ';
            }
        }
        public string LastWord
        {
            get
            {
                if (wordsBeforeCursor.Count == 0)
                    return String.Empty;
                else
                    return wordsBeforeCursor.Last();
            }
        }
        public string WordBeforeCurrent
        {
            get
            {
                if (wordsBeforeCursor.Count == 0)
                {
                    return String.Empty;
                }
                else if (CaretIsAfterWhiteSpace)
                {
                    return wordsBeforeCursor.Last();
                }
                else if (wordsBeforeCursor.Count > 1)
                {
                    return wordsBeforeCursor[wordsBeforeCursor.Count - 2];
                }
                else
                {
                    return String.Empty;
                }
            }
        }
        public string WordBeforeWordBeforeCurrent
        {
            get
            {
                if (wordsBeforeCursor.Count == 0)
                {
                    return String.Empty;
                }
                else if (CaretIsAfterWhiteSpace && wordsBeforeCursor.Count > 1)
                {
                    return wordsBeforeCursor[wordsBeforeCursor.Count - 2];
                }
                else if (wordsBeforeCursor.Count > 2)
                {
                    return wordsBeforeCursor[wordsBeforeCursor.Count - 3];
                }
                else
                {
                    return String.Empty;
                }
            }
        }
        public void Undo()
        {
            currentView.Undo();
        }
        public void Redo()
        {
            currentView.Redo();
        }
        public void Bookmark()
        {
            currentView.Bookmark();
        }

        public void Cut()
        {
            currentView.Cut();
        }
        public void Copy()
        {
            currentView.Copy();
        }
        public void Paste()
        {
            currentView.Paste();
        }
        public DocumentRange FindNext(string FindText, DocumentLocation DL, bool SelectAndShow)
        {
            DocumentRange dr = currentView.Document.FindNext(FindText, DL);
            if (dr.IsEmpty)
            {
                return null;
            }
            else
            {
                if (SelectAndShow)
                {
                    currentView.Selection = dr;
                    currentView.MakeVisible(currentView.Selection);
                    currentView.CaretDocLoc = dr.End;
                }
                return dr;
            }
        }
        public DocumentRange FindPrevious(string FindText, DocumentLocation DL, bool SelectAndShow)
        {
            DocumentRange dr = currentView.Document.FindPrevious(FindText, DL);
            if (dr.IsEmpty)
            {
                return null;
            }
            else
            {
                if (SelectAndShow)
                {
                    currentView.Selection = dr;
                    currentView.MakeVisible(currentView.Selection);
                    currentView.CaretDocLoc = dr.End;
                }
                return dr;
            }
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.Size.Width > 50 && this.Size.Height > 50) // not minimized
                setMetrics();
        }
        private void setMetrics()
        {
            bool ww = ViewEnvironment.WordWrap;

            if (ww)
            {
                vsb.Bounds = new Rectangle(this.ClientRectangle.Width - SCROLL_BAR_WIDTH,
                                           0,
                                           SCROLL_BAR_WIDTH,
                                           this.ClientRectangle.Height);

                hsb.Visible = false;

                currentView.WorkingArea = new Rectangle(0, 0, vsb.Left, this.ClientRectangle.Height);
            }
            else
            {
                vsb.Bounds = new Rectangle(this.ClientRectangle.Width - vsb.Width,
                                           0,
                                           SCROLL_BAR_WIDTH,
                                           this.ClientRectangle.Height - SCROLL_BAR_WIDTH);

                hsb.Bounds = new Rectangle(0,
                                           this.ClientRectangle.Height - SCROLL_BAR_WIDTH,
                                           this.ClientRectangle.Width - SCROLL_BAR_WIDTH,
                                           SCROLL_BAR_WIDTH);

                hsb.Visible = true;

                currentView.WorkingArea = new Rectangle(0, 0, vsb.Left, this.ClientRectangle.Height - SCROLL_BAR_WIDTH);
            }
            
            vsb.NumVisibleLines = currentView.TextArea.Height / FontInfo.Height;

            numVisibleColumns = currentView.TextArea.Width / FontInfo.Width;

            hsb.NumVisibleColumns = numVisibleColumns;

            this.Invalidate();
        }
        private void processKey(Key Key)
        {
            switch (Key)
            {
                case Key.Up:
                    currentView.MoveUp(false);
                    break;
                case Key.Down:
                    currentView.MoveDown(false);
                    break;
                case Key.Left:
                    currentView.MoveLeft(false);
                    break;
                case Key.Right:
                    currentView.MoveRight(false);
                    break;
                case Key.UpShift:
                    currentView.MoveUp(true);
                    break;
                case Key.DownShift:
                    currentView.MoveDown(true);
                    break;
                case Key.LeftShift:
                    currentView.MoveLeft(true);
                    break;
                case Key.RightShift:
                    currentView.MoveRight(true);
                    break;
                case Key.NextWord:
                    currentView.MoveRightWord(false);
                    break;
                case Key.PrevWord:
                    currentView.MoveLeftWord(false);
                    break;
                case Key.NextWordShift:
                    currentView.MoveRightWord(true);
                    break;
                case Key.PrevWordShift:
                    currentView.MoveLeftWord(true);
                    break;
                case Key.PageUp:
                    currentView.PageUp(false);
                    break;
                case Key.PageDown:
                    currentView.PageDown(false);
                    break;
                case Key.PageUpShift:
                    currentView.PageUp(true);
                    break;
                case Key.PageDownShift:
                    currentView.PageDown(true);
                    break;
                case Key.Home:
                    currentView.Home(false);
                    break;
                case Key.End:
                    currentView.End(false);
                    break;
                case Key.HomeShift:
                    currentView.Home(true);
                    break;
                case Key.EndShift:
                    currentView.End(true);
                    break;
                case Key.ScrollUp:
                    currentView.ScrollView(-1);
                    break;
                case Key.ScrollDown:
                    currentView.ScrollView(1);
                    break;
                case Key.SelectAll:
                    currentView.Selection = EntireRange;
                    break;
                case Key.Delete:
                    currentView.Delete(false);
                    break;
                case Key.Backspace:
                    currentView.Backspace();
                    break;
                case Key.DeleteShift:
                    currentView.Delete(true);
                    break;
                case Key.Insert:
                    currentView.InsertMode = !currentView.InsertMode;
                    break;
                case Key.Cut:
                    currentView.Cut();
                    break;
                case Key.Copy:
                    currentView.Copy();
                    break;
                case Key.Paste:
                    currentView.Paste();
                    break;
                case Key.Undo:
                    currentView.Undo();
                    break;
                case Key.Redo:
                    currentView.Redo();
                    break;
                case Key.Tab:
                    currentView.Tab(false);
                    break;
                case Key.TabShift:
                    currentView.Tab(true);
                    break;
                case Key.Enter:
                    currentView.Enter();
                    break;
            }
        }
        private int intellisenseStart = 0;
        private bool valueNeedsQuoting = false;
        private string quoteException = String.Empty;

        private static readonly char[] anyOfForIntellisenseStart = new char[] { ' ', '(', '\"', ')' };
        public void ShowIntellisense()
        {
            BeforeShowIntellisenseEventArgs ea = new BeforeShowIntellisenseEventArgs();

            if (BeforeShowIntellisense != null)
                BeforeShowIntellisense(this, ea);

            if (!ea.Cancel && ea.Values != null && ea.Values.Count > 0)
            {
                string s = currentView.Document[CaretLocation.LineNumber].Text;

                if (this.IsCommentedOrQuoted(CaretLocation))
                    intellisenseStart = Math.Max(0, s.LastIndexOf('\"', Math.Min(s.Length - 1, CaretLocation.ColumnNumber) - 1));
                else if (this.CaretIsAfterWhiteSpace)
                    intellisenseStart = Math.Max(0, CaretLocation.ColumnNumber - 1);
                else
                    intellisenseStart = Math.Max(0, s.LastIndexOfAny(anyOfForIntellisenseStart,
                                                    Math.Min(s.Length - 1, CaretLocation.ColumnNumber)) + 1);

                valueNeedsQuoting = ea.ValueNeedsQuoting;
                quoteException = ea.QuoteException;

                if (intellisense == null)
                {
                    intellisense = new frmIntellisense();
                    intellisense.KeyPress += new KeyPressEventHandler(intellisense_KeyPress);
                    intellisense.MouseClick += new MouseEventHandler(intellisense_MouseClick);
                }
                else
                {
                    intellisense.Visible = false;
                }

                intellisense.Values = ea.Values;

                setIntellisenseLocation();
                if (wordsBeforeCursor.Count > 0)
                    intellisense.Find(wordsBeforeCursor.Last());
                this.Controls.Add(intellisense);
                intellisense.Visible = true;
            }
            this.Focus();
        }

        private void intellisense_MouseClick(object sender, MouseEventArgs e)
        {
            applyIntellisense(true);
        }
        private void processCmdKey(Keys KeyData)
        {
            Message m = new Message();
            m.HWnd = IntPtr.Zero;
            ProcessCmdKey(ref m, KeyData);
        }
        private void setIntellisenseLocation()
        {
            if (intellisense != null)
            {
                Point p = currentView.CaretPixelLoc;
                if (p.Y < this.ClientRectangle.Height - intellisense.Height)
                    p.Offset(0, FontInfo.Height + 3);
                else
                    p.Offset(0, -intellisense.Height);

                if (p.X > this.ClientRectangle.Width - intellisense.Width - SCROLL_BAR_WIDTH)
                    p.X = this.ClientRectangle.Width - intellisense.Width - SCROLL_BAR_WIDTH;

                intellisense.Location = p;
            }
        }

        private void intellisense_KeyPress(object sender, KeyPressEventArgs e)
        {
            OnKeyPress(e);
        }
        private bool applyIntellisense(bool Force)
        {
            bool ret = false;

            if (intellisense != null)
            {
                if (Force || (wordsBeforeCursor.Count > 0 && intellisense.Value.StartsWith(wordsBeforeCursor.Last(), StringComparison.InvariantCultureIgnoreCase)))
                {
                    DocumentLocation dl = currentView.CaretDocLoc;

                    string s = currentView.Document[dl.LineNumber].Text;

                    string s1 = s.Substring(dl.ColumnNumber);

                    string v = intellisense.Value;
                    v = (valueNeedsQuoting && (String.Compare(v, quoteException, StringComparison.InvariantCultureIgnoreCase) != 0)) ? ("\"" + v + "\"") : v;

                    if (intellisenseStart == 0)
                    {
                        currentView.Document.Replace(v + ' ' + (s1.TrimStart()), dl.LineNumber);
                        currentView.CaretDocLoc = new DocumentLocation(dl.LineNumber, v.Length + 1);
                    }
                    else
                    {
                        string s2 = s.Substring(0, intellisenseStart);
                        if (s2.Trim().Length > 0)
                            s2 = s2.TrimEnd();
                        else if (s2.EndsWith(" "))
                            s2 = s2.Substring(0, s2.Length - 1);
                        if (!s2.EndsWith("("))
                            s2 += ' ';
                        currentView.Document.Replace(s2 + v + ' ' + (s1.TrimStart()), dl.LineNumber);
                        currentView.CaretDocLoc = new DocumentLocation(dl.LineNumber, s2.Length + v.Length + 1);
                    }
                    EventManager.DoEvent(EventManager.EventType.DocumentChanged);
                    ret = true;
                    currentView.Bookmark();
                }

                CloseIntellisense();
                ShowIntellisense();
            }
            this.Focus();
            return ret;
        }
        public void CloseIntellisense()
        {
            if (intellisense != null)
            {
                intellisense.KeyPress -= intellisense_KeyPress;
                intellisense.MouseClick -= intellisense_MouseClick;
                this.Controls.Remove(intellisense);
                intellisense = null;
                this.Invalidate();
                this.Focus();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool done = true;
            switch (keyData)
            {
                case Keys.Up:
                    if (intellisense != null)
                        intellisense.Up();
                    else
                        processKey(Key.Up);
                    break;
                case Keys.Down:
                    if (intellisense != null)
                        intellisense.Down();
                    else
                        processKey(Key.Down);
                    break;
                case Keys.Up | Keys.Control:
                    CloseIntellisense();
                    processKey(Key.ScrollUp);
                    break;
                case Keys.Down | Keys.Control:
                    CloseIntellisense();
                    processKey(Key.ScrollDown);
                    break;
                case Keys.Left | Keys.Control:
                    CloseIntellisense();
                    processKey(Key.PrevWord);
                    break;
                case Keys.Right | Keys.Control:
                    CloseIntellisense();
                    processKey(Key.NextWord);
                    break;
                case Keys.Left:
                    CloseIntellisense();
                    processKey(Key.Left);
                    break;
                case Keys.Right:
                    CloseIntellisense();
                    processKey(Key.Right);
                    break;
                case Keys.Left | Keys.Shift:
                    CloseIntellisense();
                    processKey(Key.LeftShift);
                    break;
                case Keys.Right | Keys.Shift:
                    CloseIntellisense();
                    processKey(Key.RightShift);
                    break;
                case Keys.Left | Keys.Shift | Keys.Control:
                    CloseIntellisense();
                    processKey(Key.PrevWordShift);
                    break;
                case Keys.Right | Keys.Shift | Keys.Control:
                    CloseIntellisense();
                    processKey(Key.NextWordShift);
                    break;
                case Keys.Up | Keys.Shift:
                    CloseIntellisense();
                    processKey(Key.UpShift);
                    break;
                case Keys.Down | Keys.Shift:
                    CloseIntellisense();
                    processKey(Key.DownShift);
                    break;
                case Keys.PageUp:
                    if (intellisense != null)
                        intellisense.PageUp();
                    else
                        processKey(Key.PageUp);
                    break;
                case Keys.PageDown:
                    if (intellisense != null)
                        intellisense.PageDown();
                    else
                        processKey(Key.PageDown);
                    break;
                case Keys.PageUp | Keys.Shift:
                    CloseIntellisense();
                    processKey(Key.PageUpShift);
                    break;
                case Keys.PageDown | Keys.Shift:
                    CloseIntellisense();
                    processKey(Key.PageDownShift);
                    break;
                case Keys.Home:
                    CloseIntellisense();
                    processKey(Key.Home);
                    break;
                case Keys.End:
                    CloseIntellisense();
                    processKey(Key.End);
                    break;
                case Keys.Home | Keys.Shift:
                    CloseIntellisense();
                    processKey(Key.HomeShift);
                    break;
                case Keys.End | Keys.Shift:
                    CloseIntellisense();
                    processKey(Key.EndShift);
                    break;
                case Keys.A | Keys.Control:
                    CloseIntellisense();
                    processKey(Key.SelectAll);
                    break;
                case Keys.Delete:
                    processKey(Key.Delete);
                    break;
                case Keys.Back:

                    if (this.CaretLocation.ColumnNumber == 0)
                        CloseIntellisense();

                    processKey(Key.Backspace);
                    if (this.CaretLocation.ColumnNumber <= intellisenseStart)
                    {
                        CloseIntellisense();
                    }
                    else
                    {
                        if (intellisense != null)
                        {
                            NeedWordsBeforeCursorRefresh(this, EventArgs.Empty);
                            intellisense.Find(this.LastWord);
                        }
                    }
                    break;
                case Keys.Delete | Keys.Shift:
                    processKey(Key.DeleteShift);
                    break;
                case Keys.Insert:
                    processKey(Key.Insert);
                    break;
                case Keys.X | Keys.Control:
                    CloseIntellisense();
                    processKey(Key.Cut);
                    break;
                case Keys.C | Keys.Control:
                    processKey(Key.Copy);
                    break;
                case Keys.V | Keys.Control:
                    CloseIntellisense();
                    processKey(Key.Paste);
                    break;
                case Keys.Z | Keys.Control:
                    CloseIntellisense();
                    processKey(Key.Undo);
                    break;
                case Keys.Y | Keys.Control:
                    CloseIntellisense();
                    processKey(Key.Redo);
                    break;
                case Keys.Tab:
                    if (intellisense != null)
                    {
                        if (!applyIntellisense(true))
                            processKey(Key.Tab);
                    }
                    else
                    {
                        processKey(Key.Tab);
                    }
                    break;
                case Keys.Tab | Keys.Shift:
                    CloseIntellisense();
                    processKey(Key.TabShift);
                    break;
                case Keys.Enter:
                    if (intellisense != null)
                    {
                        if (!applyIntellisense(true))
                            processKey(Key.Enter);
                    }
                    else
                    {
                        processKey(Key.Enter);
                    }
                    break;
                case Keys.Escape:
                    CloseIntellisense();
                    break;
                default:
                    done = false;
                    break;
            }
            if (done)
                return true;
            else
                return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("OnKeyPress: " + e.KeyChar.ToString());

            if (e.KeyChar >= ' ')
            {
                currentView.Insert(e.KeyChar);
                e.Handled = true;
            }

            base.OnKeyPress(e);

            if ((e.KeyChar == ' ') && intellisense != null)
            {
                if (!this.IsCommentedOrQuoted(this.CaretLocation))
                    applyIntellisense(false);
                else
                    intellisense.Find(wordsBeforeCursor.Last());
            }
            else if (e.KeyChar == '(' && !this.IsCommentedOrQuoted(this.CaretLocation))
            {
                ShowIntellisense();
            }
            else if (e.KeyChar == ' ' || (intellisense == null && e.KeyChar != '/'))
            {
                ShowIntellisense();
            }
            else if (e.KeyChar == '/')
            {
                CloseIntellisense();
            }
            else if (wordsBeforeCursor.Count > 0)
            {
                intellisense.Find(wordsBeforeCursor.Last());
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(this.BackColor);

            currentView.Render(e.Graphics);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (!currentView.MouseDown(e.Location))
            {

                if (!currentView.Selection.IsEmpty && !currentView.Selection.Contains(currentView.GetDocumentLocation(e.Location)))
                    currentView.Selection = DocumentRange.Empty;

                if (e.Button == MouseButtons.Left)
                {
                    mouseDownPoint = e.Location;
                    mouseDragRect = new Rectangle(e.X - 5, e.Y - 5, 10, 10);
                }
                else
                {
                    mouseDownPoint = invalidXY;
                    mouseDrag = false;
                }
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            CloseIntellisense();
            if (e.Button == MouseButtons.Left)
            {
                if (doubleClicked)
                {
                    doubleClicked = false;
                }
                else
                {
                    currentView.MouseClick(e.Location);
                }
                mouseDownPoint = invalidXY;
                mouseDrag = false;
            }
            else if (e.Button == MouseButtons.Right)
            {
                ContextMenuStrip cms = new ContextMenuStrip();
                ToolStripMenuItem tsi;

                tsi = new ToolStripMenuItem("Cu&t");
                tsi.Enabled = !currentView.Selection.IsEmpty;
                tsi.Click += (s, ee) => { currentView.Cut(); };
                cms.Items.Add(tsi);

                tsi = new ToolStripMenuItem("Cop&y");
                tsi.Enabled = !currentView.Selection.IsEmpty;
                tsi.Click += (s, ee) => { currentView.Copy(); };
                cms.Items.Add(tsi);

                tsi = new ToolStripMenuItem("&Paste");
                tsi.Enabled = Clipboard.GetText().Length > 0;
                tsi.Click += (s, ee) => { currentView.Paste(); };
                cms.Items.Add(tsi);

                cms.Show(this, e.Location);
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.Button == MouseButtons.Left)
            {
                if (!mouseDrag && mouseDownPoint.X >= 0 && !mouseDragRect.Contains(e.Location))
                {
                    mouseDrag = true;
                }
                if (mouseDrag)
                {
                    currentView.MouseDrag(e.Location);
                }
            }
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            currentView.ScrollView(-e.Delta * SystemInformation.MouseWheelScrollLines / SystemInformation.MouseWheelScrollDelta);
        }
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            currentView.MouseDoubleClick(e.Location);
            doubleClicked = true;
        }

        private void updateMaxLinesChanged()
        {
            vsb.MaxVal = curDoc.NumLines - 1;
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            currentView.DisableCaret();
        }
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            currentView.EnableCaret();
        }

        public override string ToString()
        {
            return currentView.ToString();
        }
        public string ToString(DocumentLocation DL)
        {
            return currentView.ToString(DL);
        }
    }
}
