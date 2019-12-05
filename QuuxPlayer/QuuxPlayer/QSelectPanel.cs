/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal abstract class QSelectPanel : Control
    {
        public delegate void ValueEditDelegate(string OldValue, string NewValue);
        public event ValueEditDelegate ValueChanged = null;
        public event Callback SelectedIndexChanged = null;

        protected const int ANY_INDEX = 0;
        protected const int SCROLL_BAR_WIDTH = 14;

        protected QScrollBar qScrollBar;
        private TextBox txtEditValue = null;

        private List<string> values = new List<string>();
        protected int textHeight;
        protected bool loading = false;

        private string headerText;
        protected Controller controller;

        private Brush headerBrush;
        private Brush selectedRowBrush;
        private Brush selectedRowHoverBrush;
        private Brush hoverBrush;

        private int selectedIndex = 0;
        private int numVisibleItems;
        private int firstVisibleItem = 0;
        private int hoverIndex = -1;
        private bool active = false;
        private bool locked = false;

        private string hint;
        private int hintIndex;
        
        private TextFormatFlags tff = TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter;
        private List<Rectangle> cells = new List<Rectangle>();

        private HTPCMode viewMode = HTPCMode.Normal;
        
        private Font font;
        private Font fontItalic;
        private Font headingFont;

        public QSelectPanel()
        {
            this.DoubleBuffered = true;

            qScrollBar = new QScrollBar(true);
            qScrollBar.Top = 0;
            qScrollBar.Width = SCROLL_BAR_WIDTH;
            qScrollBar.UserScroll += new QScrollBar.ScrollDelegate(scroll);
            qScrollBar.ScrollSkip += new QScrollBar.ScrollSkipDelegate(qScrollBar_ScrollSkip);
            this.Controls.Add(qScrollBar);

            textHeight = Styles.TextHeight;
            viewMode = HTPCMode.Normal;
            updateForViewMode();
        }
        private void updateBrushes()
        {
            headerBrush = Style.GetHeaderRowBrush(textHeight, 0);
            selectedRowBrush = Style.GetSelectedRowBrush(textHeight, 0);
            selectedRowHoverBrush = Style.GetSelectedHoverRowBrush(textHeight, 0);
            hoverBrush = Style.GetHoverRowBrush(textHeight, 0);
        }
        public void LoadValues(List<string> Values, string Value, string Hint, string AnyValue)
        {
            loading = true;

            var vv = Values.Where(v => v.Length > 0);

            values.Clear();
            
            values.Add(AnyValue);

            values.AddRange(vv);

            qScrollBar.Max = Math.Max(0, values.Count - NumVisibleItems);

            if (Value.Length == 0)
            {
                SelectedIndex = ANY_INDEX;
                FirstVisibleItem = 0;
                this.Hint = Hint;
            }
            else
            {
                this.HintIndex = -1;
                Hint = String.Empty;
                if (Value.Length > 0)
                {
                    this.Value = Value;
                    FirstVisibleItem = Math.Max(0, SelectedIndex - NumVisibleItems / 2);
                }
                else
                {
                    ClearValue();
                    FirstVisibleItem = 0;
                }
            }
            loading = false;

            this.Invalidate();
        }
        public void FindValue(string Value)
        {
            int i = Values.IndexOf(Value);

            if (i >= 0)
                SelectedIndex = i;
        }
        public void ReplaceValue(string OldValue, string NewValue)
        {
            int index = values.IndexOf(OldValue);
            if (index >= 0)
                values[index] = NewValue;
        }
        public bool KeyPreview
        {
            get { return txtEditValue != null && txtEditValue.Focused; }
        }

        protected virtual Func<string, char> GetFilterChar()
        {
            return new Func<string, char>(s => Lib.FirstCharUpper(s));
        }

        private void qScrollBar_ScrollSkip(bool Up)
        {
            int fvi = Math.Max(1, FirstVisibleItem);

            if (Up)
                fvi--;

            if ((Up || Values.Count > fvi) && (!Up || fvi > 0))
            {
                char c = Lib.FirstCharNoTheUpper(Values[fvi]);
                char c2 = c;
                Func<string, char> getChar = GetFilterChar();

                if (c != ' ')
                {
                    if (Up)
                    {
                        do
                        {
                            fvi--;
                            c = c2;
                        }
                        while (fvi > 1 && c == (c2 = getChar(Values[fvi - 1])));

                        FirstVisibleItem = Math.Min(fvi, FirstVisibleItem - 1);
                    }
                    else
                    {
                        char min = 'A';
                        int max = Values.Count - NumVisibleItems + 1;
                        do
                        {
                            fvi++;
                            c = c2;
                        }
                        while (fvi < max && ((c == (c2 = getChar(Values[fvi + 1]))) || (c2 < min)));

                        FirstVisibleItem = Math.Max(fvi + 1, FirstVisibleItem - 1);
                    }
                }
                if (FirstVisibleItem > 0)
                    controller.ShowLetter(getChar(Values[FirstVisibleItem]));
            }
        }
        public Controller Controller
        {
            set { controller = value; }
        }
        public void StartItemEdit()
        {
            if (!Locked && Editable && SelectedIndex > 0)
            {
                if (txtEditValue == null)
                {
                    txtEditValue = new TextBox();
                    txtEditValue.KeyDown += (s, e) =>
                    {
                        if (e.KeyData == Keys.Enter || e.KeyData == Keys.Tab || e.KeyData == Keys.Escape)
                            { e.SuppressKeyPress = true; commitEdit(); }
                    };
                    txtEditValue.Font = this.font;
                    txtEditValue.Enter += (s, e) => { controller.RequestAction(QActionType.KeyPreviewChange); };
                    txtEditValue.Leave += (s, e) => { controller.RequestAction(QActionType.KeyPreviewChange); };
                }
                txtEditValue.Font = font;
                txtEditValue.Bounds = Cells[SelectedIndex - FirstVisibleItem + 1];
                txtEditValue.BorderStyle = BorderStyle.None;
                txtEditValue.Text = Values[SelectedIndex];
                this.Controls.Add(txtEditValue);
                txtEditValue.Focus();
            }
        }
        public void FocusRenameBox()
        {
            if (txtEditValue != null)
            {
                this.Focus();
                txtEditValue.Focus();
            }
        }
        private void commitEdit()
        {
            if (txtEditValue != null)
            {
                this.Controls.Remove(txtEditValue);
                if (Editable && Values[SelectedIndex] != txtEditValue.Text.Trim())
                {
                    if (ValueChanged != null)
                        ValueChanged(Values[SelectedIndex], txtEditValue.Text.Trim());
                }
                txtEditValue = null;
            }
        }
        protected virtual bool Editable
        {
            get { return this.SelectedIndex != ANY_INDEX; }
        }
        protected List<string> Values
        {
            get { return values; }
        }
        protected int HoverIndex
        {
            get { return hoverIndex; }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (!Locked)
            {
                commitEdit();

                int selIdx = SelectedIndex;

                SelectedIndex = GetItemIndex(e.Y);
            }
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.Height > 0)
            {
                qScrollBar.Left = this.ClientRectangle.Width - SCROLL_BAR_WIDTH;
                qScrollBar.Height = this.ClientRectangle.Height;
                NumVisibleItems = Math.Max(1, (this.ClientRectangle.Height + 2) / textHeight - 1);
                qScrollBar.Max = Math.Max(0, values.Count - NumVisibleItems);
                qScrollBar.LargeChange = Math.Max(1, NumVisibleItems - 1);
                setupCells();
                
                if (txtEditValue != null)
                    txtEditValue.Bounds = Cells[SelectedIndex - FirstVisibleItem + 1];
                
                this.Invalidate();
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!Locked)
                UpdateHoverIndex(e.Y);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (hoverIndex != -1)
            {
                hoverIndex = -1;
                this.Invalidate();
            }
            base.OnMouseLeave(e);
        }
        protected void UpdateHoverIndex(int yPos)
        {
            int ind = GetItemIndex(yPos);

            if (hoverIndex != ind)
            {
                hoverIndex = ind;
                this.Invalidate();
            }
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if (this.locked)
                hoverIndex = -1;
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Delta > 0)
                FirstVisibleItem -= 2;
            else
                FirstVisibleItem += 2;

            if (!Locked)
                UpdateHoverIndex(e.Y);
        }
        protected List<Rectangle> Cells
        {
            get { return cells; }
        }
        public void ClearValue()
        {
            if (SelectedIndex != ANY_INDEX)
            {
                SelectedIndex = ANY_INDEX;
                this.Invalidate();
            }
        }
        public void ChangeFilterIndex(int Num)
        {
            if (Num > 0 &&
                SelectedIndex == ANY_INDEX &&
                ((HintIndex - FirstVisibleItem) >= 0) &&
                ((HintIndex - FirstVisibleItem) < NumVisibleItems))
            {
                SelectedIndex = HintIndex;
            }
            else
            {
                SelectedIndex += Num;
            }
            EnsureVisible(SelectedIndex);
        }
        public void PageUp()
        {
            ChangeFilterIndex(-NumVisibleItems);
        }
        public void PageDown()
        {
            ChangeFilterIndex(NumVisibleItems);
        }
        public void Home()
        {
            SelectedIndex = 0;
            EnsureVisible(0);
        }
        public void End()
        {
            SelectedIndex = int.MaxValue;
            EnsureVisible(SelectedIndex);
        }
        public string HeaderText
        {
            get { return headerText; }
            set { headerText = value; }
        }
        protected int HintIndex
        {
            get { return hintIndex; }
            set { hintIndex = value; }
        }
        public string Hint
        {
            get { return hint; }
            set
            {
                if (hint != value || hintIndex < 0 || values.Count <= hintIndex || values[hintIndex] != value)
                {
                    if ((hintIndex = values.IndexOf(value)) >= 0)
                    {
                        hint = value;
                        FirstVisibleItem = Math.Min(values.Count - 1, hintIndex - NumVisibleItems / 2);
                    }
                    else
                    {
                        hint = String.Empty;
                    }
                    this.Invalidate();
                }
            }
        }
        protected int FirstVisibleItem
        {
            get { return firstVisibleItem; }
            set
            {
                int newVal = Math.Max(0, Math.Min(values.Count - numVisibleItems, value));
                if (firstVisibleItem != newVal)
                {
                    firstVisibleItem = newVal;
                    qScrollBar.Value = newVal;
                    this.Invalidate();
                }
            }
        }
        
        protected int NumVisibleItems
        {
            get { return numVisibleItems; }
            private set { numVisibleItems = value; }
        }
        public virtual string Value
        {
            get
            {
                if (HasValue && values.Count > 0)
                    return values[selectedIndex];
                else
                    return String.Empty;
            }
            set
            {
                if (values.Contains(value))
                {
                    //Hint = String.Empty;
                    selectedIndex = values.IndexOf(value);
                }
                else
                {
                    selectedIndex = ANY_INDEX;
                }
                this.Invalidate();
            }
        }
        public bool HasValue
        {
            get { return SelectedIndex > ANY_INDEX; }
        }
        protected void scroll(QScrollBar Sender, int Value)
        {
            FirstVisibleItem = qScrollBar.Value;
            this.Invalidate();
        }
        protected void EnsureVisible(int ItemNumber)
        {
            if (ItemNumber < firstVisibleItem)
            {
                FirstVisibleItem = ItemNumber;
            }
            else if (ItemNumber >= firstVisibleItem + numVisibleItems)
            {
                FirstVisibleItem = ItemNumber - numVisibleItems + 1;
            }
        }
        public bool Active
        {
            get { return active; }
            set
            {
                if (Active != value)
                {
                    active = value;
                    if (active && !Locked)
                        this.Focus();
                    this.Invalidate();
                }
            }
        }
        public bool Locked
        {
            get { return locked; }
            set
            {
                locked = value;
                if (locked)
                    hoverIndex = -1;
            }
        }
        public int SelectedIndex
        {
            get { return selectedIndex; }
            protected set
            {
                int newVal = Math.Max(0, Math.Min(values.Count - 1, value));
                if (selectedIndex != newVal)
                {
                    selectedIndex = newVal;
                    hintIndex = newVal;
                    hint = values[hintIndex];
                    
                    OnSelectedIndexChanged(newVal);
                    
                    this.Invalidate();
                }
            }
        }
        public HTPCMode HTPCMode
        {
            get { return viewMode; }
            set
            {
                if (viewMode != value)
                {
                    viewMode = value;
                    updateForViewMode();        
                }
            }
        }
        private void updateForViewMode()
        {
            if (viewMode == HTPCMode.Normal)
            {
                font = Styles.Font;
                fontItalic = Styles.FontItalic;
                headingFont = Styles.FontBold;
                textHeight = Styles.TextHeight;
            }
            else
            {
                font = Styles.FontHTPC;
                fontItalic = Styles.FontItalicHTPC;
                headingFont = Styles.FontBoldHTPC;
                textHeight = Styles.TextHeightHTPC;
            }
            
            if (txtEditValue != null)
                txtEditValue.Font = this.font;

            OnResize(EventArgs.Empty);
            updateBrushes();
            this.Invalidate();
        }
        protected virtual void OnSelectedIndexChanged(int Value)
        {
            if (SelectedIndexChanged != null)
                SelectedIndexChanged();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            try
            {
                if (Active)
                    e.Graphics.Clear(Styles.ActiveBackground);

                e.Graphics.FillRectangle(headerBrush, cells[0]);

                TextRenderer.DrawText(e.Graphics, headerText, headingFont, cells[0], Styles.ColumnHeader, tff);

                for (int i = FirstVisibleItem; i < FirstVisibleItem + NumVisibleItems && i < values.Count; i++)
                {
                    if (i == SelectedIndex)
                    {
                        if (i == hoverIndex)
                            e.Graphics.FillRectangle(selectedRowHoverBrush, cells[i - FirstVisibleItem + 1]);
                        else
                            e.Graphics.FillRectangle(selectedRowBrush, cells[i - FirstVisibleItem + 1]);
                    }
                    else if (i == hoverIndex)
                    {
                        e.Graphics.FillRectangle(hoverBrush, cells[i - FirstVisibleItem + 1]);
                    }

                    if (i == hintIndex || i == 0)
                        TextRenderer.DrawText(e.Graphics,
                                              values[i],
                                              fontItalic,
                                              cells[i - FirstVisibleItem + 1],
                                              i == SelectedIndex ? Styles.LightText : Styles.Highlight,
                                              tff);
                    else
                        TextRenderer.DrawText(e.Graphics,
                                              values[i],
                                              font,
                                              cells[i - FirstVisibleItem + 1],
                                              Styles.LightText,
                                              tff);
                }
            }
            catch
            {
            }
        }
        public string GetValueAt(int VertPos)
        {
            int i = GetItemIndex(VertPos);
            if (i >= 0 && i < values.Count)
                return values[i];
            else
                return String.Empty;
        }
        protected int GetItemIndex(int VertPos)
        {
            return Math.Min(FirstVisibleItem + (VertPos - textHeight) / textHeight, values.Count - 1);
        }
        private void setupCells()
        {
            for (int i = 0; i < numVisibleItems + 1; i++)
            {
                Rectangle r = new Rectangle(0, textHeight * i, this.ClientRectangle.Width - SCROLL_BAR_WIDTH, textHeight);
                if (cells.Count < i + 1)
                    cells.Add(r);
                else
                    cells[i] = r;
            }
        }
    }
}
