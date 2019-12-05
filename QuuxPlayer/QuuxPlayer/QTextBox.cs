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
    internal class QTextBox : TextBox, IWatermarkable, IEditControl
    {
        private QWatermark watermark = null;
        private string nullText;

        public QTextBox(Font Font)
            : base()
        {
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Font = Font;
            this.NumericOnly = false;
        }
        public QTextBox()
            : this(Styles.Font)
        {
        }
        public QWatermark Watermark
        {
            get { return watermark; }
        }
        private void focus()
        {
            if (this.Enabled)
                this.Focus();
        }
        public bool Highlighted
        {
            get { return this.BackColor == Styles.HighlightEditControl; }
            set
            {
                bool h = this.Highlighted;

                if (value != h)
                {
                    this.BackColor = (value ? Styles.HighlightEditControl : Color.White);
                }
            }
        }
        public bool WatermarkEnabled
        {
            get { return watermark != null && !watermarkDisabled; }
            set { watermarkDisabled = !value; updateWatermark(); }
        }
        private bool watermarkDisabled = false;
        public void EnableWatermark(Control Parent, string WatermarkText, string NullText)
        {
            watermark = new QWatermark(Parent, this, new Font(this.Font, FontStyle.Italic), focus, WatermarkText);
            this.nullText = NullText;
            updateWatermark();
        }
        public bool NumericOnly
        {
            get;
            set;
        }
        public void SetOriginalText()
        {
            this.OriginalText = this.Text;
        }
        public string OriginalText { get; private set; }
        public bool Changed
        {
            get { return this.Text != this.OriginalText; }
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            updateWatermark();
        }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if (NumericOnly)
            {
                if (e.KeyChar >= ' ' && (e.KeyChar < '0' || e.KeyChar > '9'))
                {
                    e.Handled = true;
                }
            }
        }
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            updateWatermark();
        }
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            updateWatermark();
        }
        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            updateWatermark();
        }
        private void updateWatermark()
        {
            setWatermarkLabel(WatermarkEnabled &&
                              this.Text == nullText &&
                              !this.Focused);
        }
        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            updateWatermark();
        }
        private void setWatermarkLabel(bool On)
        {
            if (watermark != null)
            {
                watermark.Visible = On;
                if (On)
                {
                    watermark.Bounds = new Rectangle(this.Location.X + 2,
                                                     this.Location.Y + 2,
                                                     this.ClientRectangle.Width - 5,
                                                     this.ClientRectangle.Height - 5);
                    watermark.BringToFront();
                }
            }
        }
    }
    internal class QTextBoxFocusOnClick : QTextBox
    {
        bool alreadyFocused;

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            alreadyFocused = false;
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            // Select all text only if the mouse isn't down.
            // This makes tabbing to the textbox give focus.
            if (MouseButtons == MouseButtons.None)
            {
                this.SelectAll();
            }
        }
        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            // Web browsers like Google Chrome select the text on mouse up.
            // They only do it if the textbox isn't already focused,
            // and if the user hasn't selected all text.
            if (!alreadyFocused && this.SelectionLength == 0)
            {
                alreadyFocused = true;
                this.SelectAll();
            }
        }
    }
}