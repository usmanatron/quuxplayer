/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal partial class frmTaskDialog : QFixedDialog
    {
        private const int DEFAULT_CELL_HEIGHT = 65;
        private const int CELL_SPACING = 10;
        private const int TOTAL_WIDTH = 450;
        private const int CELL_WIDTH = 400;
        private const int CELL_INDENT = (TOTAL_WIDTH - CELL_WIDTH) / 2;
        private const int OPTION_TEXT_LEFT = 40;
        private const int ARROW_INDENT = 36;

        private List<Option> options;
        private Rectangle titleRect;
        private string titleText;

        private static Font titleFont;
        private static Font normalFont;

        private const TextFormatFlags titleTff = TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak | TextFormatFlags.WordEllipsis;
        private const TextFormatFlags tff = titleTff | TextFormatFlags.VerticalCenter;

        private int resultIndex = -1;

        public class Option
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public int ResultIndex;
            private Rectangle bounds;
            public Rectangle FirstLine { get; private set; }
            public Rectangle SecondLine { get; private set; }
            private LinearGradientBrush brush = null;
            public Option(string Title, string Description, int ResultIndex)
            {
                this.Title = Title;
                this.Description = Description;
                this.ResultIndex = ResultIndex;
            }
            public Rectangle Bounds
            {
                get { return bounds; }
                set
                {
                    bounds = value;
                    FirstLine = new Rectangle(Bounds.Left + ARROW_INDENT, Bounds.Y, Bounds.Width - ARROW_INDENT, this.Bounds.Height * 4 / 10);

                    int height = this.bounds.Height * 6 / 10;

                    height = Math.Max(height, TextRenderer.MeasureText(this.Description, normalFont, value.Size, tff).Height);

                    SecondLine = new Rectangle(FirstLine.Left, FirstLine.Bottom, FirstLine.Width, height);
                    bounds.Height = SecondLine.Bottom - bounds.Top;

                    brush = new LinearGradientBrush(bounds, Styles.Medium, Styles.Dark, LinearGradientMode.Vertical);
                }
            }
            public void Render(Graphics g, bool Hover)
            {
                if (Hover)
                {
                    g.FillRectangle(brush, this.Bounds);
                    g.DrawRectangle(Styles.SortArrowPen, this.Bounds);
                }
                TextRenderer.DrawText(g, this.Title, titleFont, this.FirstLine, Styles.Playing, tff);
                TextRenderer.DrawText(g, this.Description, normalFont, this.SecondLine, Styles.LightText, tff);
                g.DrawImageUnscaled(Styles.BitmapTaskDialogArrow, this.Bounds.X + 5, this.bounds.Y + 5);
            }
            public override bool Equals(object obj)
            {
                return this.ToString() == obj.ToString();
            }
            public override int GetHashCode()
            {
                return this.ToString().GetHashCode();
            }
            public override string ToString()
            {
                return this.Title;
            }
        }
        
        public frmTaskDialog(string TitleBarText, string TitleText, List<Option> Options) : base(TitleBarText, ButtonCreateType.None)
        {
            this.titleText = TitleText;
            this.options = Options;

            MARGIN = 16;

            titleRect = new Rectangle(new Point(MARGIN, MARGIN), TextRenderer.MeasureText(TitleText, titleFont, new Size(TOTAL_WIDTH - MARGIN - MARGIN, 1000), TextFormatFlags.WordBreak));

            int y = titleRect.Bottom + SPACING + SPACING;
            foreach (Option o in options)
            {
                o.Bounds = new Rectangle(CELL_INDENT, y, CELL_WIDTH, DEFAULT_CELL_HEIGHT);
                y += o.Bounds.Height + CELL_SPACING;
            }

            this.ClientSize = new Size(TOTAL_WIDTH, options.Last().Bounds.Bottom + MARGIN);
        }
        static frmTaskDialog()
        {
            titleFont = new Font(Styles.Font.Name, 14.0f, FontStyle.Regular);
            normalFont = Styles.Font;
        }

        public int ResultIndex
        {
            get { return resultIndex; }
            private set { resultIndex = value; } 
        }

        private Option hoverOption = null;
        private Option HoverOption
        {
            get { return hoverOption; }
            set
            {
                if (hoverOption != value)
                {
                    hoverOption = value;
                    this.Invalidate();
                }
            }
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            TextRenderer.DrawText(e.Graphics, titleText, titleFont, titleRect, Styles.LightText, titleTff);
            foreach (Option o in options)
            {
                o.Render(e.Graphics, o == HoverOption);
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            foreach (Option o in options)
                if (o.Bounds.Contains(e.Location))
                {
                    HoverOption = o;
                    return;
                }
            this.Invalidate();
        }
        private void up()
        {
            if (HoverOption == null)
            {
                HoverOption = options[0];
                return;
            }
            int i = options.FindIndex(o => o == HoverOption);
            if (i > 0)
                HoverOption = options[i - 1];
        }
        private void down()
        {
            if (HoverOption == null)
            {
                HoverOption = options[0];
                return;
            }
            int i = options.FindIndex(o => o == HoverOption);
            if (i < options.Count - 1)
                HoverOption = options[i + 1];
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Up:
                    up();
                    return true;
                case Keys.Down:
                    down();
                    return true;
                case Keys.Enter:
                    if (HoverOption != null)
                    {
                        this.ResultIndex = HoverOption.ResultIndex;
                        this.Close();
                    }
                    return true;
                case Keys.Escape:
                    this.Close();
                    return true;
                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            foreach (Option o in options)
                if (o.Bounds.Contains(e.Location))
                {
                    this.ResultIndex = o.ResultIndex;
                    this.Close();
                    return;
                }
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            HoverOption = null;
        }
       
    }
}
