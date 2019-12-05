/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxControls
{
    internal partial class frmIntellisense : Control
    {
        //internal delegate void CommandKeyPressed(Keys KeyData);
        //internal event CommandKeyPressed CommandKeyPressedEvent;

        private const int SCROLL_BAR_WIDTH = 16;
        private QVScrollBar vsb = null;
        private List<string> values;
        private int numVisibleItems = 1;
        private int selectedIndex = 0;

        private const TextFormatFlags tff = TextFormatFlags.NoPrefix | TextFormatFlags.NoClipping;

        public frmIntellisense()
        {
            this.DoubleBuffered = true;

            values = new List<string>();

            InitializeComponent();

            vsb = new QVScrollBar(SCROLL_BAR_WIDTH);
            vsb.Value = 0;
            
            this.Controls.Add(vsb);

            vsb.GotFocus += new EventHandler(vsb_GotFocus);
            vsb.Scroll += new ScrollEventHandler(vsb_Scroll);
            vsb.LargeChange = numVisibleItems;

            this.ClientSize = new Size(220, FontInfo.Height * 6 + 2);

            this.Font = new Font(FontInfo.FontName, 9f, FontStyle.Regular);
        }

        private void vsb_Scroll(object sender, ScrollEventArgs e)
        {
            this.Invalidate();
        }

        private void vsb_GotFocus(object sender, EventArgs e)
        {
            this.Focus();
        }
        public List<string> Values
        {
            get
            {
                return values;
            }
            set
            {
                values = value;
                vsb.MaxVal = Math.Max(0, values.Count - numVisibleItems);
                selectedIndex = 0;
                this.Invalidate();
            }
        }
        public string Value
        {
            get
            {
                return values[selectedIndex].Replace("\"", "\\\"");
            }
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            selectedIndex = vsb.Value + e.Y / FontInfo.Height;
            this.Invalidate();
            //CommandKeyPressedEvent(Keys.Enter);
        }
        public void Find(string Value)
        {
            if (Value.Length > 0)
            {
                int i = values.FindIndex(s => s.StartsWith(Value, StringComparison.InvariantCultureIgnoreCase));
                if (i >= 0)
                {
                    selectedIndex = i;
                    makeSelectedVisible();
                    this.Invalidate();
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            
            if (vsb != null)
                vsb.Bounds = new Rectangle(this.ClientRectangle.Width - SCROLL_BAR_WIDTH - 1,
                                           0,
                                           SCROLL_BAR_WIDTH,
                                           this.ClientRectangle.Height);

            numVisibleItems = this.ClientRectangle.Height / FontInfo.Height;    
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            e.Graphics.Clear(Color.White);

            int firstVisibleItem = vsb.Value;

            int y = 3;

            for (int i = 0; i + firstVisibleItem < values.Count && i < numVisibleItems; i++)
            {
                if (selectedIndex == i + firstVisibleItem)
                {
                    e.Graphics.FillRectangle(Brushes.DarkBlue, new Rectangle(1, y - 2, this.ClientRectangle.Width - 2, FontInfo.Height));
                    TextRenderer.DrawText(e.Graphics, values[i + firstVisibleItem], this.Font, new Point(2, y), Color.White, tff);
                }
                else
                {
                    TextRenderer.DrawText(e.Graphics, values[i + firstVisibleItem], this.Font, new Point(2, y), Color.Black, tff);
                }
                y += FontInfo.Height;
            }

            e.Graphics.DrawRectangle(Pens.DarkGray,
                                     0,
                                     0,
                                     this.ClientRectangle.Width - 1,
                                     this.ClientRectangle.Height - 1);
        }
        public void Up()
        {
            selectedIndex = Math.Max(0, selectedIndex - 1);
            makeSelectedVisible();
        }
        public void Down()
        {
            selectedIndex = Math.Min(selectedIndex + 1, values.Count - 1);
            makeSelectedVisible();
        }
        public void PageUp()
        {
            selectedIndex = Math.Max(0, selectedIndex - numVisibleItems + 1);
            makeSelectedVisible();
        }
        public void PageDown()
        {
            selectedIndex = Math.Min(selectedIndex + numVisibleItems - 1, values.Count - 1);
            makeSelectedVisible();
        }
        //protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        //{
        //    switch (keyData)
        //    {
        //        case Keys.Down:
        //            selectedIndex = Math.Min(selectedIndex + 1, values.Count - 1);
        //            makeSelectedVisible();
        //            return true;
        //        case Keys.Up:
        //            selectedIndex = Math.Max(0, selectedIndex - 1);
        //            makeSelectedVisible();
        //            return true;
        //        case Keys.PageDown:
        //            selectedIndex = Math.Min(selectedIndex + numVisibleItems - 1, values.Count - 1);
        //            makeSelectedVisible();
        //            return true;
        //        case Keys.PageUp:
        //            selectedIndex = Math.Max(0, selectedIndex - numVisibleItems + 1);
        //            makeSelectedVisible();
        //            return true;
        //        default:
        //            if (CommandKeyPressedEvent != null)
        //                CommandKeyPressedEvent(keyData);
        //            break;
        //    }
        //    return base.ProcessCmdKey(ref msg, keyData);
        //}
        private void makeSelectedVisible()
        {
            if (selectedIndex < vsb.Value)
                vsb.Value = selectedIndex;
            else if (selectedIndex > vsb.Value + numVisibleItems - 1)
                vsb.Value = Math.Max(0, selectedIndex - numVisibleItems + 1);
            
            this.Invalidate();
        }
    }
}
