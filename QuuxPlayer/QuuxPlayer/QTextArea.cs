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
    internal sealed class QTextArea : Control
    {
        private string text;
        private QScrollBar scrollbar;
        private const TextFormatFlags tff = TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak;

        private const int SCROLL_BAR_WIDTH = 14;

        public QTextArea()
        {
            this.DoubleBuffered = true;

            this.text = String.Empty;
            
            scrollbar = new QScrollBar(false);
            scrollbar.Visible = false;
            this.Controls.Add(scrollbar);
            
            this.Font = Styles.Font;

            scrollbar.UserScroll += (s, v) => { this.Invalidate(); };
        }
        public override string Text
        {
            get { return text; }
            set
            {
                if (text != value)
                {
                    text = value;
                    scrollbar.Value = 0;
                    setMetrics();
                }
            }
        }
        private string pendingText;
        public void SetTextThreadSafe(string Text)
        {
            pendingText = Text;
            Clock.DoOnMainThread(setText, 10);
        }
        private void setText()
        {
            this.Text = pendingText;
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            scrollbar.Bounds = new Rectangle(this.ClientRectangle.Width - SCROLL_BAR_WIDTH,
                                             0,
                                             SCROLL_BAR_WIDTH,
                                             this.ClientRectangle.Height);

            setMetrics();
        }
        public int FirstVisibleYPixel
        {
            get { return scrollbar.Value; }
            set
            {
                if (scrollbar.Value != value)
                {
                    scrollbar.Value = value;
                    this.Invalidate();
                }
            }
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            scrollbar.Value += (e.Delta > 0) ? -50 : 50;
            this.Invalidate();
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.Focus();
        }
        public override Font Font
        {
            get
            {
                return base.Font;
            }
            set
            {
                base.Font = value;
                
                setMetrics();
                this.Invalidate();
            }
        }
        public void SetFontThreadSafe(Font Font)
        {
            base.Font = Font;
            Clock.DoOnMainThread(setMetrics, 30);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            TextRenderer.DrawText(e.Graphics, text, this.Font, new Rectangle(0,
                                                                        -scrollbar.Value,
                                                                        this.ClientRectangle.Width - (scrollbar.Visible ? SCROLL_BAR_WIDTH : 0),
                                                                        this.ClientRectangle.Height + scrollbar.Value), Styles.LightText, tff);
        }

        private void setMetrics()
        {
            int height = TextRenderer.MeasureText(text, this.Font, new Size(this.ClientRectangle.Width - SCROLL_BAR_WIDTH, this.ClientRectangle.Height), tff).Height;

            if (height > this.ClientRectangle.Height)
            {
                scrollbar.Visible = true;
                scrollbar.Max = height - this.ClientRectangle.Height;
                scrollbar.LargeChange = this.ClientRectangle.Height * 8 / 10;
            }
            else
            {
                scrollbar.Visible = false;
                scrollbar.Value = 0;
                scrollbar.Max = 0;
                scrollbar.LargeChange = 0;
            }
            this.Invalidate();
        }
    }
}
