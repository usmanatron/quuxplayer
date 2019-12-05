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
    internal sealed class frmLock : QFixedDialog
    {
        private QLabel lblInstructions;
        private QCheckBox chkCode;
        private QCheckBox chkMain;
        private QCheckBox chkGamepadLock;
        private QTextBox txtCode;

        public frmLock(QLock Lock) : base(Localization.Get(UI_Key.Lock_Title), ButtonCreateType.OKAndCancel)
        {
            this.ClientSize = new Size(400, 300);

            lblInstructions = new QLabel(Localization.Get(UI_Key.Lock_Instructions, Application.ProductName));
            lblInstructions.Location = new Point(SPACING, SPACING);
            lblInstructions.SetWidth(this.ClientRectangle.Width - 2 * SPACING);
            this.Controls.Add(lblInstructions);

            chkMain = new QCheckBox(Localization.Get(UI_Key.Lock_Checkbox), this.BackColor);
            chkMain.Checked = Lock != null && Lock.Locked;
            chkMain.Location = new Point(2 * SPACING, lblInstructions.Bottom + SPACING);
            chkMain.CheckedChanged += new EventHandler(chkMainCheckedChanged);
            this.Controls.Add(chkMain);

            chkCode = new QCheckBox(Localization.Get(UI_Key.Lock_Code), this.BackColor);
            chkCode.Checked = (Lock != null) && (Lock.Code.Length > 0);
            chkCode.Location = new Point(3 * SPACING, chkMain.Bottom + SPACING);
            chkCode.CheckedChanged += (s, e) => { txtCode.Enabled = chkCode.Checked; };
            this.Controls.Add(chkCode);

            txtCode = new QTextBox();
            txtCode.Text = (Lock != null) ? Lock.Code : String.Empty;
            txtCode.MaxLength = QLock.MAX_CODE_LENGTH;
            txtCode.Location = new Point(chkCode.Right + SPACING, chkCode.Top + chkCode.Height / 2 - txtCode.Height / 2 + 2);
            txtCode.Enabled = chkCode.Checked;
            this.Controls.Add(txtCode);

            chkGamepadLock = new QCheckBox(Localization.Get(UI_Key.Lock_Gamepad), this.BackColor);
            chkGamepadLock.Checked = (Lock != null) && Lock.GamepadLock;
            chkGamepadLock.Location = new Point(3 * SPACING, chkCode.Bottom + SPACING);
            this.Controls.Add(chkGamepadLock);

            //btnCancel.Location = new Point(this.ClientRectangle.Width - btnCancel.Width - SPACING, chkGamepadLock.Bottom + SPACING);

            //btnOK.Location = new Point(btnCancel.Left - btnOK.Width - SPACING, btnCancel.Top);

            PlaceButtons(this.ClientRectangle.Width, chkGamepadLock.Bottom + SPACING);

            this.ClientSize = new Size(this.ClientRectangle.Width, btnOK.Bottom + SPACING);

            this.chkMainCheckedChanged(this, EventArgs.Empty);

            int tabIndex = 0;

            chkMain.TabIndex = tabIndex++;
            chkCode.TabIndex = tabIndex++;
            txtCode.TabIndex = tabIndex++;
            chkGamepadLock.TabIndex = tabIndex++;
            btnOK.TabIndex = tabIndex++;
            btnCancel.TabIndex = tabIndex++;
        }
        public QLock Lock
        {
            get
            {
                return new QLock(chkMain.Checked, chkCode.Checked ? txtCode.Text.Trim() : String.Empty, chkGamepadLock.Checked);
            }
        }

        private void chkMainCheckedChanged(object sender, EventArgs e)
        {
            chkCode.Enabled = chkMain.Checked;
            txtCode.Enabled = chkMain.Checked && chkCode.Checked;
            chkGamepadLock.Enabled = chkMain.Checked;
        }
    }
}
