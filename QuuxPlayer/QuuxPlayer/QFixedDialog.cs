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
    internal abstract class QFixedDialog : Form
    {
        internal enum ButtonCreateType { None, OKOnly, OKAndCancel, OKAndCancelReverse };

        protected QButton btnOK = null;
        protected QButton btnCancel = null;
        
        protected int SPACING = 6;
        protected int MARGIN = 6;
        
        private Dictionary<QButton, Callback> addedButtons = new Dictionary<QButton, Callback>();

        private Keys lastKey = Keys.None;

        internal QFixedDialog(string Title, ButtonCreateType CreateButtons)
        {
            this.StartPosition = FormStartPosition.CenterParent;
            this.DialogResult = DialogResult.Cancel;
            this.Text = Title;
            this.ShowIcon = false;
            this.SizeGripStyle = SizeGripStyle.Hide;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = Styles.Dark;
            this.ForeColor = Styles.LightText;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.DoubleBuffered = true;
            this.AutoScaleMode = AutoScaleMode.None;
            this.KeyPreview = true;
            switch (CreateButtons)
            {
                case ButtonCreateType.OKOnly:
                    btnOK = new QButton(Localization.OK, false, false);
                    AddButton(btnOK, ok);
                    break;
                case ButtonCreateType.OKAndCancel:
                    btnOK = new QButton(Localization.OK, false, false);
                    AddButton(btnOK, ok);
                    btnCancel = new QButton(Localization.CANCEL, false, false);
                    AddButton(btnCancel, cancel);
                    break;
                case ButtonCreateType.OKAndCancelReverse:
                    btnCancel = new QButton(Localization.OK, false, false);
                    AddButton(btnCancel, cancel);
                    btnOK = new QButton(String.Empty, false, false);
                    AddButton(btnOK, ok);
                    break;
            }
        }
        private QFixedDialog()
        {

        }
        protected void AddButton(QButton Button, Callback Function)
        {
            addedButtons.Add(Button, Function);
            this.Controls.Add(Button);
            Button.ButtonPressed += (s) => { Function(); };
        }
        protected void PlaceButtons(int X, int Y)
        {
            X -= MARGIN;

            if (btnCancel == null)
            {
                if (btnOK != null)
                    btnOK.Location = new Point(X - btnOK.Width, Y);
            }
            else if (btnOK != null)
            {
                btnCancel.Location = new Point(X - btnCancel.Width, Y);
                btnOK.Location = new Point(btnCancel.Left - MARGIN - btnOK.Width, Y);
            }
        }
        protected void PlaceButtons(int X, int Y, params QButton[] Buttons)
        {
            for (int i = 0; i < Buttons.Length; i++)
            {
                X -= (Buttons[i].Width + MARGIN);
                Buttons[i].Location = new Point(X, Y);
            }
        }
        protected virtual void ok()
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        protected virtual void cancel()
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            lastKey = e.KeyData;
        }
        protected override void OnKeyUp(System.Windows.Forms.KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyData == lastKey)
            {
                switch (e.KeyData)
                {
                    case System.Windows.Forms.Keys.Enter:
                    case System.Windows.Forms.Keys.Space:
                        if (btnOK != null && this.ActiveControl == btnOK)
                        {
                            e.Handled = true;
                            ok();
                        }
                        else if (btnCancel != null && this.ActiveControl == btnCancel)
                        {
                            e.Handled = true;
                            cancel();
                        }
                        else
                        {
                            foreach (KeyValuePair<QButton, Callback> kvp in addedButtons)
                            {
                                if (this.ActiveControl == kvp.Key)
                                {
                                    e.Handled = true;
                                    kvp.Value();
                                }
                            }
                        }
                        break;
                    case System.Windows.Forms.Keys.Escape:
                        e.Handled = true;
                        cancel();
                        break;
                }
            }
            else
            {
                lastKey = Keys.None;
            }
        }
    }
}
