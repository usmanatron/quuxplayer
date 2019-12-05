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
    internal abstract class QPanel : Control
    {
        protected int MARGIN = 8;
        protected int SPACING = 6;

        protected QButton btnOK;
        protected QButton btnCancel;

        protected List<QButton> buttons;

        private ulong resizeAlarm = Clock.NULL_ALARM;

        protected bool initialized = false;

        public QPanel()
        {
            this.SuspendLayout();

            this.buttons = new List<QButton>();
            this.BackColor = Styles.Dark;
            this.DoubleBuffered = true;
        }

        protected abstract void ok();
        protected abstract void cancel();

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.DrawRectangle(Styles.TrackDragLinePen,
                                     0,
                                     0,
                                     this.ClientRectangle.Width - 1,
                                     this.ClientRectangle.Height - 1);
        }
        protected void setWrapAroundTabControl(int TabIndex, Control c1, Control c2)
        {
            Button b = new Button();
            b.Location = new Point(-1000, -1000);
            b.TabIndex = TabIndex;
            b.GotFocus += (s, e) =>
            {
                if (c1.Enabled)
                    c1.Focus();
                else if (c2 != null)
                    c2.Focus();
            };
            this.Controls.Add(b);
        }

        protected void AddButton(QButton Button, Callback Target)
        {
            this.Controls.Add(Button);
            this.buttons.Add(Button);
            Button.ButtonPressed += (s) => { Target(); };
        }
        protected void PlaceButtonsVert()
        {
            int w = 0;
            foreach (QButton qb in buttons)
                w = Math.Max(2, qb.Width);

            foreach (QButton qb in buttons)
                qb.Width = w;

            btnOK.Location = new Point(this.ClientRectangle.Width - MARGIN - btnOK.Width,
                                       MARGIN);

            btnCancel.Location = new Point(btnOK.Left,
                                           btnOK.Bottom + SPACING);
        }
        protected void PlaceButtonsHoriz(int Y)
        {
            btnOK.Location = new Point(this.ClientRectangle.Width - btnOK.Width - MARGIN,
                                       Y - btnOK.Height);

            btnCancel.Location = new Point(btnOK.Left - btnCancel.Width - MARGIN,
                                           btnOK.Top);
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (initialized)
                Clock.Update(ref resizeAlarm, resize, 100, false);
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (Keyboard.Alt)
            {
                foreach (QButton b in buttons)
                    if (b.AccelleratorKey != Keys.None && Keyboard.GetKeyState(b.AccelleratorKey).IsPressed)
                    {
                        b.InvokeButton();
                        return true;
                    }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        protected virtual void resize()
        {
            resizeAlarm = Clock.NULL_ALARM;
        }
    }
}
