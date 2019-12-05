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
    internal class QWatermark : Label
    {
        private Control associatedControl;
        private Callback callback;
        private const TextFormatFlags tff = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine;

        public QWatermark(Control Parent, Control AssociatedControl, Font Font, Callback Callback, string Text)
        {
            this.Visible = false;
            this.Font = Font;
            this.Text = Text;
            this.callback = Callback;
            this.BackColor = Color.White;
            this.ForeColor = Styles.Watermark;
            this.associatedControl = AssociatedControl;
            Parent.Controls.Add(this);
        }
        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);

            if (this.associatedControl.Enabled)
            {
                this.Visible = false;
                callback();
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            TextRenderer.DrawText(e.Graphics, this.Text, this.Font, new Rectangle(Point.Empty, this.ClientRectangle.Size), this.ForeColor, tff);
        }
    }
}
