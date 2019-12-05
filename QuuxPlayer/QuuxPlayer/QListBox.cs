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
    internal sealed class QListBox : Control
    {
        private const TextFormatFlags tff = TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.Left;
        private const int SCROLLBAR_WIDTH = 14;

        private QScrollBar qScrollBar;
        private Rectangle borderRect;
        private List<string> items;
        private int topItem;
        private int visibleItems;
        private int hoverIndex;
        private int selectedIndex;
        private int itemHeight;
        private bool unique;

        public QListBox(bool Unique)
        {
            qScrollBar = new QScrollBar(false);
            qScrollBar.Max = 0;
            qScrollBar.Width = SCROLLBAR_WIDTH;
            qScrollBar.UserScroll += new QScrollBar.ScrollDelegate(scroll);

            this.Controls.Add(qScrollBar);

            items = new List<string>();
            itemHeight = Styles.TextHeight;
            topItem = 0;
            hoverIndex = -1;
            selectedIndex = -1;

            unique = Unique;

            this.DoubleBuffered = true;
        }
        
        public string this[int Index]
        {
            get { return items[Index]; }
            set
            {
                items[Index] = value;
                this.Invalidate();
            }
        }
        public int Count
        {
            get { return items.Count; }
        }
        public void RemoveAt(int Index)
        {
            items.RemoveAt(Index);
            
            if (selectedIndex == Index)
                selectedIndex = -1;
            else if (selectedIndex > Index)
                selectedIndex--;

            updateScrollBar();

            this.Invalidate();
        }
        public void RemoveSelected()
        {
            if (selectedIndex >= 0 && selectedIndex < items.Count)
            {
                items.RemoveAt(selectedIndex);
                selectedIndex = -1;
                updateScrollBar();
                this.Invalidate();
            }
        }
        public void Add(string Item)
        {
            if (!unique || !items.Contains(Item))
            {
                items.Add(Item);
                updateScrollBar();
                this.Invalidate();
            }
        }
        public void Clear()
        {
            items.Clear();
            selectedIndex = -1;
            updateScrollBar();
            this.Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            int hov = e.Y / itemHeight + topItem;
            if (hov >= items.Count)
                hov = -1;
            
            HoverIndex = hov;
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            
            HoverIndex = -1;
            
            this.Invalidate();
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            
            int sel = e.Y / itemHeight + topItem;
            if (sel >= items.Count)
                sel = -1;

            SelectedIndex = sel;
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            borderRect = new Rectangle(0, 0, this.ClientRectangle.Width - 1, this.ClientRectangle.Height - 1);
            qScrollBar.Bounds = new Rectangle(this.ClientRectangle.Width - SCROLLBAR_WIDTH, 0, SCROLLBAR_WIDTH, this.ClientRectangle.Height);
            visibleItems = this.ClientRectangle.Height / itemHeight;
            updateScrollBar();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            e.Graphics.Clear(Styles.ActiveBackground);

            e.Graphics.DrawRectangle(Styles.DarkBorderPen, borderRect);

            if (hoverIndex == selectedIndex)
            {
                if (hoverIndex >= 0)
                    e.Graphics.FillRectangle(Styles.LightBrush, rowRectangle(hoverIndex));
            }
            else
            {
                if (hoverIndex >= 0)
                    e.Graphics.FillRectangle(Styles.DarkBrush, rowRectangle(hoverIndex));
                if (selectedIndex >= 0)
                    e.Graphics.FillRectangle(Styles.MediumBrush, rowRectangle(selectedIndex));
            }

            int lastItem = Math.Min(topItem + visibleItems, items.Count) - 1;

            for (int i = topItem; i <= lastItem; i++)
            {
                TextRenderer.DrawText(e.Graphics, items[i], Styles.Font, new Rectangle(0, (i - topItem) * itemHeight, this.ClientRectangle.Width, itemHeight), Styles.LightText, tff);
            }
        }
        
        private int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                if (selectedIndex != value)
                {
                    selectedIndex = value;
                    this.Invalidate();
                }
            }
        }
        private int HoverIndex
        {
            get { return hoverIndex; }
            set
            {
                if (hoverIndex != value)
                {
                    hoverIndex = value;
                    this.Invalidate();
                }
            }
        }
        private Rectangle rowRectangle(int Index)
        {
            return new Rectangle(1, (Index - topItem) * itemHeight, borderRect.Width - 2, itemHeight);
        }
        private void updateScrollBar()
        {
            qScrollBar.Enabled = items.Count > visibleItems;
            if (qScrollBar.Enabled)
            {
                qScrollBar.Max = items.Count - visibleItems;
            }
            else
            {
                qScrollBar.Max = 0;
            }
        }
        private void scroll(QScrollBar Sender, int Value)
        {
            topItem = qScrollBar.Value;
            this.Invalidate();
        }
    }
}
