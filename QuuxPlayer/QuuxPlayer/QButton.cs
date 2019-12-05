/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal sealed class QButton : Button
    {
        private const int COMPACT_MARGIN = 10;
        private const int NORMAL_MARGIN = 30;
        public delegate void ButtonDelegate(QButton Button);

        public event ButtonDelegate ButtonPressed;

        private bool mouseHovering = false;
        private Rectangle r;
        private const TextFormatFlags tffNormal = TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.NoClipping;
        private const TextFormatFlags tffShowPrefix = TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoClipping;
        private TextFormatFlags tff;
        private bool isToggle;
        private bool val;
        private bool compact;
        private bool pendingEnable;
        private Keys accelleratorKey = Keys.None;

        public QButton(string Text, bool IsToggle, bool Compact)
        {
            this.BackColor = Styles.Dark;
            this.Size = new System.Drawing.Size(100, 25);
            this.DoubleBuffered = true;
            this.compact = Compact;
            this.tff = tffNormal;

            // set Font before Text to make the size come out accurately
            this.Font = Styles.FontBold;
            this.Text = Text; 

            this.isToggle = IsToggle;
            this.val = false;
            this.Invalidate();
            this.EnabledChanged += new EventHandler(QButton_EnabledChanged);
        }
        private void QButton_EnabledChanged(object sender, EventArgs e)
        {
            this.Invalidate();
        }
        public bool Compact
        {
            get { return compact; }
            set
            {
                if (compact != value)
                {
                    compact = value;
                    this.Text = base.Text;
                }
            }
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
                this.Width = MeasureText() + (compact ? COMPACT_MARGIN : NORMAL_MARGIN);
            
            }
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            r = new Rectangle(Point.Empty, this.Size);
            this.Invalidate();
        }
        public bool Value
        {
            get { return val; }
            set
            {
                if (val != value)
                {
                    val = value;
                    this.Invalidate();
                }
            }
        }
        public void ShowAccellerator(Keys Key)
        {
            tff = tffShowPrefix;
            accelleratorKey = Key;
            //this.UseMnemonic = true;
            this.Invalidate();
        }
        public Keys AccelleratorKey
        {
            get { return accelleratorKey; }
        }
        public void DoAccelleratedAction()
        {
            InvokeButton();
        }
        public void InvokeButton()
        {
            invoke();
        }
        private void invoke()
        {
            if (this.isToggle)
            {
                val = !val;
            }

            this.Invalidate();

            if (ButtonPressed != null)
                ButtonPressed(this);
        }
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
        public int MeasureText()
        {
            return TextRenderer.MeasureText(this.Text,
                                            this.Font,
                                            Size.Empty,
                                            tff).Width;
        }
        public void SetEnabledThreadSafe(bool Enabled)
        {
            pendingEnable = Enabled;
            Clock.DoOnMainThread(setEnable, 30);
        }
        private void setEnable()
        {
            this.Enabled = pendingEnable;
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.X >= 0 &&
                e.Y >= 0 &&
                e.X < this.ClientRectangle.Width &&
                e.Y < this.ClientRectangle.Height)
            {
                invoke();
            }
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            mouseHovering = true;
            this.Invalidate();
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            mouseHovering = false;
            this.Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            bool enabled = this.Enabled;

            if (enabled && mouseHovering)
            {
                e.Graphics.DrawImageUnscaled(Styles.BitmapFilterOutlinebackground, 3, 0);
                e.Graphics.DrawImageUnscaled(Styles.filter_outline_left, Point.Empty);
                e.Graphics.DrawImageUnscaled(Styles.button_outline_right, this.Width - 3, 0);
            }
            else
            {
                e.Graphics.Clear(this.BackColor);
            }
            
            Color c = Styles.LightText;

            TextRenderer.DrawText(e.Graphics,
                                  this.Text,
                                  this.Font,
                                  r,
                                  enabled ? c : Styles.DisabledButtonText,
                                  tff);

            if (val)
                e.Graphics.DrawRectangle(Styles.FilterButtonHasValuePen, getBorderRectangle());
            else if (this.Focused)
                e.Graphics.DrawRectangle(Styles.ControlFocusedPen, getBorderRectangle());
        }
        private Rectangle getBorderRectangle()
        {
            return new Rectangle(Point.Empty, new Size(this.Width - 1, this.Height - 1));
        }
    }
}
