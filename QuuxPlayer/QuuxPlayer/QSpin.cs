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
    internal sealed class QSpin : Control
    {
        public event EventHandler CheckedChanged;
        public event EventHandler ValueChanged;

        private QTextBox txtMain;
        private QCheckBox chkMain;
        private Point prefixTextLocation;
        private Point postfixTextLocation;
        private Rectangle upRect;
        private Rectangle downRect;
        private string oldText;
        private bool restoring;
        private int Min;
        private int Max;
        private string prefixText;
        private string postfixText;
        private bool upHover;
        private bool downHover;
        private bool hovering = false;
        private int value = -1;

        public QSpin(bool CheckBox, bool Checked, string PrefixText, string PostfixText, int Min, int Max, int Default, int Increment, Color BackColor)
        {
            restoring = true;

            this.Min = Min;
            this.Max = Max;

            this.BackColor = BackColor;

            upHover = false;
            downHover = false;

            this.Increment = Increment;

            if (CheckBox)
            {
                chkMain = new QCheckBox(PrefixText, this.BackColor);
                chkMain.AutoSize = false;
                chkMain.Checked = Checked;
                chkMain.CheckedChanged += new EventHandler(chkMain_CheckedChanged);
                chkMain.MouseEnter += (s, e) => { this.Hovering = true; this.Invalidate(); };
                chkMain.MouseLeave += (s, e) => { this.Hovering = false; this.Invalidate(); };
                this.Controls.Add(chkMain);
            }
            else
            {
                chkMain = null;
            }

            prefixText = PrefixText;
            postfixText = PostfixText;

            txtMain = new QTextBox();
            txtMain.Font = Styles.Font;
            txtMain.Width = 30;
            txtMain.TextAlign = HorizontalAlignment.Right;
            txtMain.BorderStyle = BorderStyle.None;
            txtMain.TextChanged += new EventHandler(txtMain_TextChanged);
            txtMain.Leave += (s, e) =>  { if (txtMain.Text.Length == 0) txtMain.Text = this.Min.ToString(); };
            txtMain.KeyUp += (s, e) =>
                {
                    switch (e.KeyCode)
                    {
                        case Keys.Up:
                            setValWithIncrement(this.Value + Increment);
                            e.Handled = true;
                            break;
                        case Keys.Down:
                            setValWithIncrement(this.value - Increment);
                            e.Handled = true;
                            break;
                    }
                };

            //this.Size = txtMain.Size;
            this.Controls.Add(txtMain);

            setValue(Math.Min(this.Max, Math.Max(this.Min, Default)));

            OffEquivalent = -1;

            updateControls();

            restoring = false;

            this.TabStop = false;
        }
        
        public int Value
        {
            get
            {
                int val;
                if (Int32.TryParse(txtMain.Text, out val))
                    return val;
                else
                    return 0;
            }
            set
            {
                int val = Math.Max(this.Min, Math.Min(this.Max, value));
                
                txtMain.Text = val.ToString();

                //if (value == OffEquivalent)
                //    this.Checked = false;
            }
        }
        public int Increment
        { get; set; }
        public bool Checked
        {
            get { return chkMain != null && chkMain.Checked; }
            set
            {
                if (chkMain != null)
                    chkMain.Checked = value;
            }
        }
        public string PostfixText
        {
            get { return postfixText; }
            set
            {
                postfixText = value;
                this.Width = Math.Max(this.Width, postfixTextLocation.X + TextRenderer.MeasureText(postfixText, Styles.Font).Width + 10);
                this.Invalidate();
            }
        }
        public int Maximum
        {
            get { return Max; }
            set
            {
                if (this.Max != value)
                {
                    this.Max = value;
                    if (this.value > Max)
                        setValue(Max);
                }
            }
        }
        public int OffEquivalent
        { get; set; }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            this.Invalidate();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            UpHover = upRect.Contains(e.Location);
            DownHover = downRect.Contains(e.Location);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            UpHover = false;
            DownHover = false;
            this.Hovering = false;
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.Hovering = true;
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (upRect.Contains(e.Location))
            {
                setValWithIncrement(this.Value + Increment);
            }
            else if (downRect.Contains(e.Location))
            {
                setValWithIncrement(this.Value - Increment);
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if ((e.X > upRect.Right || e.X < upRect.Left) && chkMain != null)
            {
                chkMain.Checked = !chkMain.Checked;
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Color c = this.Enabled ? (Hovering ? Styles.VeryLight : Styles.LightText) : Styles.DisabledText;

            Pen up = (this.Enabled) ? (UpHover ? Styles.SpinControlPenHover : Styles.SpinControlPen) : Styles.SpinControlPenDisabled;
            Pen down = (this.Enabled) ? (DownHover ? Styles.SpinControlPenHover : Styles.SpinControlPen) : Styles.SpinControlPenDisabled;

            Styles.DrawArrow(e.Graphics, up, true, upRect.Left + 3, upRect.Top + 3);
            Styles.DrawArrow(e.Graphics, down, false, downRect.Left + 3, downRect.Top + 3);

            if (chkMain == null && prefixText.Length > 0)
                TextRenderer.DrawText(e.Graphics, prefixText, Styles.Font, prefixTextLocation, c);

            if (postfixText.Length > 0)
            {
                TextRenderer.DrawText(e.Graphics, postfixText, Styles.Font, postfixTextLocation, c);
            }
        }
        
        private void setValWithIncrement(int Value)
        {
            if (Value % Increment == 0)
            {
                this.Value = Value;
            }
            else
            {
                if ((Value % Increment) > (Increment / 2))
                {
                    this.Value = Value - (Value % Increment) + Increment;
                }
                else
                {
                    this.Value = Value - (Value % Increment);
                }
            }
        }

        private bool Hovering
        {
            get { return hovering; }
            set
            {
                if (hovering != value)
                {
                    this.hovering = value;
                    if (chkMain != null)
                    {
                        chkMain.Hovering = value;
                    }
                    this.Invalidate();
                }
            }
        }
        private bool UpHover
        {
            get { return upHover; }
            set
            {
                if (value != upHover)
                {
                    upHover = value;
                    this.Invalidate();
                }
            }
        }
        private bool DownHover
        {
            get { return downHover; }
            set
            {
                if (value != downHover)
                {
                    downHover = value;
                    this.Invalidate();
                }
            }
        }
        private void chkMain_CheckedChanged(object sender, EventArgs e)
        {
            txtMain.Enabled = chkMain.Checked;

            if (CheckedChanged != null)
                CheckedChanged.Invoke(this, e);
        }
        private void updateControls()
        {
            int xCursor = 3;

            if (chkMain != null)
            {
                chkMain.Location = new Point(0, 0);
                xCursor = chkMain.Right;
            }
            else if (prefixText.Length > 0)
            {
                prefixTextLocation = new Point(0, 2);
                xCursor += TextRenderer.MeasureText(prefixText, Styles.Font).Width;
            }

            txtMain.Location = new Point(xCursor, 2);

            xCursor = txtMain.Right;
            
            upRect = new Rectangle(xCursor + 3, 1, 10,9);
            downRect = new Rectangle(upRect.Left, 10, 10, 9);

            xCursor = upRect.Right + 3;

            postfixTextLocation = new Point(xCursor, 2);

            if (chkMain == null)
                this.Size = new Size(xCursor + TextRenderer.MeasureText(postfixText, Styles.Font).Width + 14,
                                     txtMain.Height + 4);
            else
                this.Size = new Size(xCursor + TextRenderer.MeasureText(postfixText, Styles.Font).Width + 14,
                                     chkMain.Height + 4);

        }
        private void txtMain_TextChanged(object sender, EventArgs e)
        {
            if (!restoring && txtMain.Text.Length > 0)
            {
                for (int i = 0; i < txtMain.Text.Length; i++)
                {
                    if (txtMain.Text[i] < '0' || txtMain.Text[i] > '9')
                    {
                        restore();
                        return;
                    }
                }
                int val;
                if (!Int32.TryParse(txtMain.Text, out val))
                {
                    restore();
                    return;
                }
                if (val < Min)
                {
                    setValue(Min);
                    return;
                }
                if (val > Maximum)
                {
                    setValue(Max);
                    return;
                }
                setValue(val);
            }
            oldText = txtMain.Text;
            OnTextChanged(e);
        }
        private void setValue(int Value)
        {
            int val = Math.Max(Min, Math.Min(Max, Value));
            if (val != this.value)
            {
                restoring = true;

                value = val;
                oldText = value.ToString();
                txtMain.Text = oldText;

                restoring = false;

                if (ValueChanged != null)
                    ValueChanged.Invoke(this, EventArgs.Empty);
            }
        }
        private void restore()
        {
            restoring = true;
            txtMain.Text = oldText;
            txtMain.SelectAll();
            restoring = false;
        }
    }
}
