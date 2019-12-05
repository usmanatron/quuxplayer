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
    internal sealed class QInputBox : QFixedDialog
    {
        private QTextBox txtMain;
        private QLabel lblMain;
        private QCheckBox chkCheckbox;
        private int minLength = 0;
        public QInputBox(IWin32Window Owner,
                         string Text,
                         string Caption,
                         string DefaultValue,
                         string CheckboxText,
                         bool CheckboxVal,
                         int MaxLength,
                         int MinLength) : base(Caption, ButtonCreateType.OKAndCancel)
        {
            this.SPACING = 8;

            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;

            this.ClientSize = new Size(275, 300);

            lblMain = new QLabel(Text);
            lblMain.ShowAccellerator();
            this.Controls.Add(lblMain);
            lblMain.Location = new Point(SPACING, 2 * SPACING);
            lblMain.SetWidth(this.ClientRectangle.Width - SPACING - SPACING);

            txtMain = new QTextBox();
            txtMain.Text = DefaultValue;
            txtMain.KeyDown += (s, e) =>
            {
                /* if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    ok();
                }
                else */ if (e.KeyCode == Keys.Escape)
                {
                    e.Handled = true;
                    cancel();
                }
            };
            txtMain.MaxLength = MaxLength;
            minLength = MinLength;
            txtMain.Bounds = new System.Drawing.Rectangle(SPACING, lblMain.Bottom + 2 * SPACING, this.ClientRectangle.Width - 2 * SPACING, txtMain.Height);
            this.Controls.Add(txtMain);

            if (CheckboxText.Length > 0)
            {
                chkCheckbox = new QCheckBox(CheckboxText, this.BackColor);
                chkCheckbox.Checked = CheckboxVal;
                chkCheckbox.Location = new Point(SPACING, txtMain.Bottom + SPACING);
                chkCheckbox.Enabled = false;
                this.Controls.Add(chkCheckbox);
            }

            PlaceButtons(this.ClientRectangle.Width, ((chkCheckbox != null) ? chkCheckbox.Bottom : txtMain.Bottom) + 2 * SPACING);
            
            btnOK.Enabled = false;

            this.ClientSize = new Size(this.ClientSize.Width, btnOK.Bottom + SPACING);

            int tabIndex = 0;

            lblMain.TabIndex = tabIndex++;
            txtMain.TabIndex = tabIndex++;
            
            if (chkCheckbox != null)
                chkCheckbox.TabIndex = tabIndex++;

            btnOK.TabIndex = tabIndex++;
            btnCancel.TabIndex = tabIndex++;

            txtMain.TextChanged += (s, e) =>
            {
                btnOK.Enabled = txtMain.Text.Length >= minLength;
                if (chkCheckbox != null)
                    chkCheckbox.Enabled = btnOK.Enabled;
            };

            this.ShowDialog(Owner);
        }

        protected override void cancel()
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        protected override void ok()
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Enter:
                    if (btnCancel.Focused)
                        cancel();
                    else
                        ok();
                    return true;
                case Keys.Escape:
                    cancel();
                    return true;
                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }
        }
        public QInputBox(IWin32Window Owner, string Text, string Caption, string DefaultValue, int MaxLength, int MinLength)
            : this(Owner, Text, Caption, DefaultValue, String.Empty, false, MaxLength, MinLength)
        {
        }
        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            txtMain.Focus();
        }

        public string Value
        {
            get
            {
                return txtMain.Text;
            }
            set
            {
                txtMain.Text = value;
            }
        }
        public bool CheckboxChecked
        {
            get { return chkCheckbox != null && chkCheckbox.Checked; }
        }
    }
}
