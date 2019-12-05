/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal sealed class QLabel : Label
    {
        private const TextFormatFlags tffNormal = TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak;
        private const TextFormatFlags tffShowPrefix = TextFormatFlags.WordBreak;
        private TextFormatFlags tff;
        public QLabel(string Text, Font Font)
        {
            this.Text = Text;
            this.Font = Font;
            this.ForeColor = Styles.LightText;
            this.BackColor = Color.Transparent;
            this.UseMnemonic = false;
            this.AutoSize = true;
            this.tff = tffNormal;
        }
        public QLabel(string Text) : this(Text, Styles.Font)
        {
        }
        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                base.Text = value;
                this.Invalidate();
            }
        }
        public void SetWidth(int Width)
        {
            this.AutoSize = false;
            this.Width = Width;
            this.Height = TextRenderer.MeasureText(this.Text, this.Font, new Size(this.Width, 300), TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak).Height;
        }
        public void ShowAccellerator()
        {
            this.tff = tffShowPrefix;
            this.UseMnemonic = true;
            this.Invalidate();
        }
        public void DoAccelleratedAction()
        {
            this.Focus();
        }
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);

            if (this.Enabled)
                this.ForeColor = Styles.LightText;
            else
                this.ForeColor = Styles.DisabledText;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            //e.Graphics.Clear(this.BackColor);
            TextRenderer.DrawText(e.Graphics,
                                  this.Text,
                                  this.Font,
                                  new Rectangle(Point.Empty, this.Size),
                                  this.ForeColor,
                                  tff);
        }
    }
}
