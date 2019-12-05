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
    internal sealed class QCheckBox : CheckBox
    {
        private static Bitmap bmpOff;
        private static Bitmap bmpOn;
        private static Bitmap bmpDisabledOn;
        private static Bitmap bmpDisabledOff;
        private static Point textPoint;
        private Rectangle focusRectangle = Rectangle.Empty;
        
        private bool hovering = false;

        private const TextFormatFlags tffNormal = TextFormatFlags.NoPrefix;
        private const TextFormatFlags tffShowPrefix = TextFormatFlags.Default;
        private TextFormatFlags tff;
        //private Keys accelleratorKey = Keys.None;

        public QCheckBox(string Text, Color BackColor) : base()
        {
            this.Text = Text;
            this.Font = Styles.Font;
            this.ForeColor = Styles.LightText;
            this.FlatStyle = FlatStyle.Flat;
            this.BackColor = BackColor;
            this.GotFocus += new EventHandler(focusChanged);
            this.LostFocus += new EventHandler(focusChanged);
            if (Text.Length > 0)
            {
                this.AutoSize = true;
                this.Width = 18 + TextRenderer.MeasureText(this.Text, this.Font).Width + 10;
            }
            else
            {
                this.Size = new Size(18, 18);
            }
            setFocusRectangle();
        }

        private void focusChanged(object sender, EventArgs e)
        {
            this.Invalidate();
        }
        static QCheckBox()
        {
            bmpOff = Properties.Resources.checkbox_off;
            bmpOn = Properties.Resources.checkbox_on;
            bmpDisabledOn = Properties.Resources.checkbox_on_disabled;
            bmpDisabledOff = Properties.Resources.checkbox_off_disabled;
            textPoint = new Point(19, 2);
        }
        public void ShowAccellerator()
        {
            tff = tffShowPrefix;
            //accelleratorKey = Key;
            this.UseMnemonic = true;
            this.Invalidate();
        }
        //public Keys AccelleratorKey
        //{
        //    get { return accelleratorKey; }
        //}
        //public void DoAccelleratedAction()
        //{
        //    this.Checked = !this.Checked;
        //}
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            this.Invalidate();
        }
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            this.Invalidate();
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            setFocusRectangle();
            this.Invalidate();
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
                setFocusRectangle();
                this.Invalidate();
            }
        }
        public bool Hovering
        {
            get { return hovering; }
            set
            {
                if (hovering != value)
                {
                    this.hovering = value;
                    this.Invalidate();
                }
            }
        }
        protected override void OnMouseEnter(EventArgs eventargs)
        {
            base.OnMouseEnter(eventargs);
            Hovering = true;
        }
        protected override void OnMouseLeave(EventArgs eventargs)
        {
            base.OnMouseLeave(eventargs);
            Hovering = false;
        }
        private void setFocusRectangle()
        {
            if (this.Text.Length > 0)
                focusRectangle = new Rectangle(bmpOn.Width + 5, 1, this.Width - bmpOn.Width - 6, this.Height - 2);
            else
                focusRectangle = new Rectangle(3, 3, bmpOn.Width - 3, bmpOn.Height - 3);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(this.BackColor);

            Color c = this.Enabled ? (this.Hovering ? Styles.VeryLight : Styles.LightText) : Styles.DisabledText;
            Bitmap b = this.Enabled ? (this.Checked ? bmpOn : bmpOff) : (this.Checked ? bmpDisabledOn : bmpDisabledOff);

            TextRenderer.DrawText(e.Graphics, this.Text, this.Font, textPoint, c, tff);
            e.Graphics.DrawImageUnscaled(b, 2, 2);

            if (this.Focused)
                e.Graphics.DrawRectangle(Styles.ControlFocusedPen, focusRectangle);
        }
    }
}
