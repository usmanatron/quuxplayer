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
    internal interface IListViewable
    {
        string[] DisplayValues { get; }
        bool ActionEnabled(int Index);
        bool IsAction(int Index);
        bool IsColumnSortable(int ColumnNum);
        bool IsSpecial { get; }
        event QListView<IListViewable>.ListViewDataChanged DataChanged;
    }
    
    internal class QListView<T> : Control where T : class, IListViewable
    {
        private enum Align { Left, Center, Right }

        public delegate void ListViewDataChanged(T Item);
        public delegate void ClickDelegate(T SelectedItem);
        public delegate int ListViewSortDelegate(IListViewable A, IListViewable B, int Column, bool Fwd);
        public delegate void ContextMenuHookDelegate(ContextMenuStrip ContextMenu, T Item);

        public event ClickDelegate ClickCallback;
        public event ClickDelegate DoubleClickCallback;
        public event ContextMenuHookDelegate ContextMenuHook;

        private static int rowHeight;

        private const int SCROLL_BAR_WIDTH = 14;

        private object itemLock = new object();
        private bool isValid = false;

        private class RowCells : List<Rectangle>
        {
            public int ColumnAt(int X)
            {
                for (int i = 0; i < this.Count; i++)
                    if (this[i].Right > X)
                        return i;

                return -1;
            }
        }

        private List<T> items = new List<T>();

        private class ItemEventCallback
        {
            public string Name { get; private set; }
            public ClickDelegate Callback { get; private set; }
            public int ColNum { get; private set; }
            public ItemEventCallback(string Name, ClickDelegate Callback, int ColNum)
            {
                this.Name = Name;
                this.Callback = Callback;
                this.ColNum = ColNum;
            }
        }
        private List<ItemEventCallback> eventCallbacks = new List<ItemEventCallback>();
        private T selectedItem = null;
        private T hoverItem = null;
        private int _firstVisibleRow = 0;
        private QScrollBar scrollBar;
        string[] headings;
        private int numColumns;
        private int numFields;
        private int numActions;

        private List<Rectangle> rowRectangles = new List<Rectangle>();
        private List<RowCells> cellRectangles = new List<RowCells>();
        
        private int numVisibleRows = 0;
        private int hoverColumn = -1;

        private List<int> columnWidths = new List<int>();
        private List<float> columnWidthRatios = new List<float>();
        private int[] fixedColumnWidths;

        private const TextFormatFlags tffLeft = TextFormatFlags.NoPrefix | TextFormatFlags.WordEllipsis;
        private const TextFormatFlags tffCenter = tffLeft | TextFormatFlags.HorizontalCenter;
        private const TextFormatFlags tffRight = tffLeft | TextFormatFlags.Right;

        private TextFormatFlags[] tff;

        private int sortColumn = 0;
        private bool fwdSort = true;
        private ListViewSortDelegate sortDelegate;

        public QListView(List<string> Headings, List<ClickDelegate> EventCallbacks, string[] FixedColumnsText, ListViewSortDelegate SortDelegate)
        {
            this.DoubleBuffered = true;

            numColumns = Headings.Count;
            numFields = numColumns - EventCallbacks.Count;
            numActions = EventCallbacks.Count;

            this.sortDelegate = SortDelegate;

            headings = new string[numColumns];
            for (int i = 0; i < numColumns; i++)
                headings[i] = Headings[i];

            fixedColumnWidths = new int[numColumns];
            setFixedColumnWidths(FixedColumnsText);

            tff = new TextFormatFlags[numColumns];
            for (int i = 0; i < numColumns; i++)
                if (fixedColumnWidths[i] == -1)
                    tff[i] = tffLeft;
                else
                    tff[i] = tffCenter;

            scrollBar = new QScrollBar(false);
            scrollBar.UserScroll += new QScrollBar.ScrollDelegate(scrollBar_UserScroll);
            scrollBar.Max = 0;

            int callbackIndex = numFields;
            foreach (ClickDelegate cd in EventCallbacks)
            {
                eventCallbacks.Add(new QListView<T>.ItemEventCallback(Headings[callbackIndex], cd, callbackIndex));
                callbackIndex++;
            }

            this.Controls.Add(scrollBar);
        }

        private static Brush headerBrush;
        private static Brush selectedRowBrush;
        private static Brush selectedRowHoverBrush;
        private static Brush hoverBrush;
        
        static QListView()
        {
            rowHeight = Styles.TextHeight;

            headerBrush = Style.GetHeaderRowBrush(rowHeight, 0);
            selectedRowBrush = Style.GetSelectedRowBrush(rowHeight, 0);
            selectedRowHoverBrush = Style.GetSelectedHoverRowBrush(rowHeight, 0);
            hoverBrush = Style.GetHoverRowBrush(rowHeight, 0);
        }

        private bool suppressDoubleClick = false;

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            HoverItem = null;
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.Focus();
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Delta > 0)
                FirstVisibleRow -= 3;
            else
                FirstVisibleRow += 3;
        }
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (this.Enabled)
                if (!suppressDoubleClick)
                    if (this.SelectedItem != null)
                        if (this.DoubleClickCallback != null)
                            DoubleClickCallback(SelectedItem);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (this.Enabled)
            {
                suppressDoubleClick = false;
                if (e.Y < rowHeight)
                {
                    int col = cellRectangles[0].ColumnAt(e.X);
                    if (items.Count > 0 && items[0].IsColumnSortable(col))
                    {
                        if (col == sortColumn)
                            fwdSort = !fwdSort;
                        else
                            sortColumn = col;
                        Sort();
                        this.Invalidate();
                    }
                    suppressDoubleClick = true;
                }
                else
                {
                    T t = SelectedItem;
                    SelectedItem = getItemFromYPos(e.Y);
                    if (SelectedItem != null)
                    {
                        if (SelectedItem != t)
                            if (ClickCallback != null)
                                ClickCallback(SelectedItem);

                        if (e.Button == MouseButtons.Left) // only trigger an action on a left click
                        {
                            int x = 0;

                            if (numActions > 0)
                            {
                                int i = 0;
                                for (; i < numFields; i++)
                                    x += columnWidths[i];
                                int xx = e.X - x;
                                if (xx >= 0)
                                {
                                    while (i < columnWidths.Count && xx > columnWidths[i])
                                    {
                                        xx -= columnWidths[i++];
                                    }
                                    if (i < numColumns)
                                    {
                                        eventCallbacks[i - numFields].Callback(SelectedItem);
                                        suppressDoubleClick = true;
                                    }
                                }
                            }
                        }
                    }
                }
                if (e.Button == MouseButtons.Right && ContextMenuHook != null)
                {
                    ContextMenuStrip cms = new ContextMenuStrip();
                    cms.Renderer = new MenuItemRenderer();

                    ContextMenuHook(cms, SelectedItem);
                    if (cms.Items.Count > 0)
                    {
                        cms.Show(this, e.Location);
                    }
                }
            }
        }
        private int SelectedIndex
        {
            get
            {
                return items.FindIndex(i => i == this.SelectedItem);
            }
        }
        private void move(int NumToMove)
        {
            int idx = this.SelectedIndex;
            if (idx < 0)
            {
                if (items.Count > 0)
                    this.SelectedItem = items[0];
            }
            else
            {
                idx += NumToMove;
                if (idx < 0)
                    idx = 0;
                else if (idx >= items.Count)
                    idx = items.Count - 1;

                this.userSetSelectedItem(items[idx]);
            }
        }
        private void userSetSelectedItem(T Item)
        {
            if (Item != SelectedItem)
            {
                SelectedItem = Item;
                if (this.SelectedItem != null)
                    ensureVisible(SelectedItem);
                if (SelectedItem != null && ClickCallback != null)
                    ClickCallback(SelectedItem);
            }
        }
        public void EnsureSelectedItemVisible()
        {
            if (SelectedItem != null)
                ensureVisible(SelectedItem);
        }
        private void ensureVisible(T Item)
        {
            int idx = this.SelectedIndex;

            if (idx < FirstVisibleRow)
                FirstVisibleRow = idx;
            else if (idx > FirstVisibleRow + numVisibleRows - 1)
                FirstVisibleRow = idx - numVisibleRows + 1;
        }
        public void PageDown()
        {
            move(+numVisibleRows);  
        }
        public void PageUp()
        {
            move(-numVisibleRows);
        }
        public void MoveUp()
        {
            move(-1);
        }
        public void MoveDown()
        {
            move(+1);
        }
        public void Home()
        {
            if (this.items.Count > 0)
                userSetSelectedItem(items[0]);
        }
        public void End()
        {
            if (this.items.Count > 0)
                userSetSelectedItem(items.Last());
        }
        public bool HasItem(Predicate<T> Predicate)
        {
            lock (itemLock)
            {
                return items.Exists(Predicate);
            }
        }
        private void safeInvalidate()
        {
            if (isValid)
            {
                isValid = false;
                Clock.DoOnMainThread(this.Invalidate);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            T origHoverItem = HoverItem;
            
            HoverItem = getItemFromYPos(e.Y);

            if (HoverItem != null)
            {
                HoverColumn = cellRectangles[0].ColumnAt(e.X);
            }
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            scrollBar.Bounds = new Rectangle(this.ClientRectangle.Width - SCROLL_BAR_WIDTH,
                                             1,
                                             SCROLL_BAR_WIDTH,
                                             this.ClientRectangle.Height - 2);

            columnWidths.Clear();
            float availWidth = (float)(this.scrollBar.Left - fixedColumnWidths.Sum(w => Math.Max(0, w)));
            for (int i = 0; i < numColumns; i++)
            {
                if (fixedColumnWidths[i] > 0)
                    columnWidths.Add(fixedColumnWidths[i]);
                else
                    columnWidths.Add((int)(columnWidthRatios[i] * availWidth));
            }

            rowRectangles.Clear();
            cellRectangles.Clear();
            int height = this.ClientRectangle.Height - (rowHeight * 3 / 4);
            int width = this.ClientRectangle.Width;
            int y = 0;
            while (y < height)
            {
                rowRectangles.Add(new Rectangle(0, y, width, rowHeight));

                int x = 0;
                RowCells rc = new QListView<T>.RowCells();

                for (int i = 0; i < numColumns; i++)
                {
                    rc.Add(new Rectangle(x, y, columnWidths[i], rowHeight));
                    x += columnWidths[i];
                }
                y += rowHeight;
                cellRectangles.Add(rc);
            }
            numVisibleRows = rowRectangles.Count - 1;
            updateScrollBarMax();
            this.Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            this.isValid = true;

            int numRows = Math.Min(items.Count - FirstVisibleRow, numVisibleRows);

            for (int i = 1; i < numColumns; i++)
            {
                e.Graphics.DrawLine(Styles.DarkBorderPen,
                                    cellRectangles[0][i].Left,
                                    rowHeight,
                                    cellRectangles[0][i].Left,
                                    this.ClientRectangle.Height);
            }

            e.Graphics.DrawRectangle(Styles.DarkBorderPen,
                                     0,
                                     0,
                                     this.ClientRectangle.Width - 1,
                                     this.ClientRectangle.Height - 1);

            renderHeader(e.Graphics);
            for (int i = 0; i < numRows; i++)
            {
                renderItem(e.Graphics, items[FirstVisibleRow + i], i + 1);
            }
        }

        private int getIndexFromYPos(int Y)
        {
            return Y / rowHeight;
        }
        private int FirstVisibleRow
        {
            get { return _firstVisibleRow; }
            set
            {
                value = Math.Max(0, Math.Min(Items.Count - numVisibleRows, value));
                if (value != _firstVisibleRow)
                {
                    _firstVisibleRow = value;
                    scrollBar.Value = FirstVisibleRow;
                    this.Invalidate();
                }
            }
        }
        private T getItemFromYPos(int Y)
        {
            int row = FirstVisibleRow + Y / rowHeight - 1;

            if (row >= 0 && row < items.Count)
                return items[row];
            else
                return null;
        }
        
        private T HoverItem
        {
            get { return hoverItem; }
            set
            {
                if (hoverItem != value)
                {
                    hoverItem = value;
                    safeInvalidate();
                }
            }
        }
        private int HoverColumn
        {
            get { return hoverColumn; }
            set
            {
                if (hoverColumn != value)
                {
                    hoverColumn = value;
                    safeInvalidate();
                }
            }
        }
        private void scrollBar_UserScroll(QScrollBar Sender, int Value)
        {
            FirstVisibleRow = scrollBar.Value;
            safeInvalidate();
        }
        
        public void AddItem(T Item, bool Select, bool Sort)
        {
            bool contains;
            lock (itemLock)
            {
                contains = items.Contains(Item);
            }
            if (!contains)
            {
                System.Diagnostics.Debug.Assert(Item.DisplayValues.Length == numColumns);
                lock (itemLock)
                {
                    items.Add(Item);
                }
                if (Select)
                    SelectedItem = Item;
                if (Sort)
                    this.Sort();

                Clock.DoOnMainThread(updateScrollBarMax);
            }
        }
        public void Sort(int Column, bool Fwd)
        {
            sortColumn = Column;
            fwdSort = Fwd;
            Sort();
        }
        public void Sort()
        {
            lock (itemLock)
            {
                Items.Sort((a, b) => sortDelegate(a, b, sortColumn, fwdSort));
            }
            safeInvalidate();
        }
        public T SelectedItem
        {
            get
            {
                return selectedItem;
            }
            set
            {
                if (!object.Equals(selectedItem, value))
                {
                    selectedItem = value;
                    safeInvalidate();
                }
            }
        }
        public void SelectFirstItem()
        {
            T item = null;
            lock (itemLock)
            {
                if (items.Count > 0)
                    item = items[0];
            }
            if (item != null)
                SelectedItem = item;
        }
        public void Clear()
        {
            lock (itemLock)
            {
                items.Clear();
            }
            HoverItem = null;
            SelectedItem = null;
            updateScrollBarMax();
            safeInvalidate();
        }
        
        private void remove(T Item)
        {
            lock (itemLock)
            {
                items.Remove(Item);
            }
            safeInvalidate();
        }
        public void RemoveSelectedItem()
        {
            if (SelectedItem != null)
            {
                remove(SelectedItem);
                SelectedItem = null;
            }
        }
        public void RemoveItem(T Item)
        {
            bool contains;
            lock (itemLock)
            {
                contains = items.Contains(Item);
            }
            if (contains)
            {
                if (Item == SelectedItem)
                    SelectedItem = null;
                remove(Item);
                updateScrollBarMax();
            }
        }
        public List<T> Items
        {
            get { return items; }
            set
            {
                items = value;
                updateScrollBarMax();
                safeInvalidate();
            }
        }
        public List<T> CopyOfItems
        {
            get
            {
                lock (itemLock)
                {
                    return items.ToList();
                }
            }
        }
        public void InvalidateThreadSafe()
        {
            Clock.DoOnMainThread(this.Invalidate);
        }

        private void setFixedColumnWidths(string[] FixedColumnText)
        {
            float w = (float)(1.0 / (double)(numColumns - FixedColumnText.Count(fc => fc.Length > 0)));
            for (int i = 0; i < numColumns; i++)
            {
                columnWidthRatios.Add(w);
                if (FixedColumnText[i].Length == 0)
                    fixedColumnWidths[i] = -1;
                else
                    fixedColumnWidths[i] = TextRenderer.MeasureText(FixedColumnText[i], Styles.FontBold).Width + 8;
            }
        }

        private void renderHeader(Graphics g)
        {
            g.FillRectangle(headerBrush, rowRectangles[0]);

            for (int i = 0; i < numColumns; i++)
            {
                string text = headings[i];
                if (i == sortColumn)
                {
                    text += "    ";
                    Styles.DrawArrow(g, Styles.SortArrowPen, !fwdSort, cellRectangles[0][sortColumn].Right - 10, 6);
                }
                TextRenderer.DrawText(g,
                                      text,
                                      Styles.FontBold,
                                      cellRectangles[0][i],
                                      Styles.ColumnHeader,
                                      tff[i]);
            }
        }
        private void renderItem(Graphics g, T Item, int RowNumber)
        {
            Font norm;
            Font underline;
            if (Item.IsSpecial)
            {
                norm = Styles.FontItalic;
                underline = Styles.FontItalicUnderline;
            }
            else
            {
                norm = Styles.Font;
                underline = Styles.FontUnderline;
            }
            if (Item == SelectedItem)
            {
                if (Item == HoverItem)
                    g.FillRectangle(selectedRowHoverBrush, rowRectangles[RowNumber]);
                else
                    g.FillRectangle(selectedRowBrush, rowRectangles[RowNumber]);
            }
            else if (Item == HoverItem)
            {
                g.FillRectangle(hoverBrush, rowRectangles[RowNumber]);
            }

            for (int i = 0; i < numColumns; i++)
            {
                Color c =
                    Item.IsAction(i)
                            ?
                        (Item.ActionEnabled(i) ? Styles.VeryLight : Styles.DisabledText)
                            :
                        (Item.IsSpecial ? Styles.Playing : Styles.LightText);

                //= (Item.ActionEnabled(i) || !Item.IsAction(i)) ? (Item.IsSpecial ? Styles.Playing : Styles.LightText) : Styles.DisabledText;
                
                bool doUnderline = ((i == HoverColumn) && (Item == HoverItem) && Item.ActionEnabled(i));
                
                TextRenderer.DrawText(g,
                                      Item.DisplayValues[i],
                                      doUnderline ? underline : norm,
                                      cellRectangles[RowNumber][i],
                                      c,
                                      tff[i]);
            }
            if (Item == HoverItem && HoverColumn >= 0)
            {
                string ttText = Item.DisplayValues[HoverColumn];
                int w = TextRenderer.MeasureText(g, ttText, Styles.Font).Width;
                if (w > columnWidths[HoverColumn])
                {
                    Rectangle r1 = cellRectangles[RowNumber][HoverColumn];
                    Rectangle r = new Rectangle(r1.Left,
                                                r1.Top,
                                                w,
                                                r1.Height);
                    if (r.Right > scrollBar.Left)
                    {
                        r = new Rectangle(new Point(Math.Max(0, scrollBar.Left - w), r.Top),
                                          new Size(Math.Min(w, scrollBar.Left), r.Height));
                    }

                    g.FillRectangle(Styles.ToolTipBrush,
                                          r);

                    g.DrawRectangle(Styles.DarkBorderPen, r);

                    TextRenderer.DrawText(g,
                                          ttText,
                                          Styles.Font,
                                          r,
                                          Styles.ToolTipText,
                                          tffLeft);
                }
            }
        }
        private void updateScrollBarMax()
        {
            scrollBar.Max = items.Count - numVisibleRows;
            scrollBar.LargeChange = Math.Max(2, numVisibleRows);
        }
        
    }
}
