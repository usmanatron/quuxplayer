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
    internal sealed class QCheckedMessageBox : QMessageBox
    {
        private QCheckBox chkCheckbox;

        public QCheckedMessageBox(IWin32Window Owner, string Text, string Title, QMessageBoxButtons Buttons, QMessageBoxIcon Icon, QMessageBoxButton DefaultButton, string CheckboxText, bool Checked)
            : base(Text, Title, Buttons, Icon, DefaultButton)
        {

            chkCheckbox = new QCheckBox(CheckboxText, this.BackColor);
            chkCheckbox.Location = new Point(MARGIN, btnOK.Bottom + SPACING);
            chkCheckbox.Checked = Checked;
            this.Controls.Add(chkCheckbox);
            this.ClientSize = new Size(Math.Max(this.ClientRectangle.Width, chkCheckbox.Right + MARGIN), chkCheckbox.Bottom + MARGIN);

            this.ShowDialog(Owner);
        }
        public bool Checked
        {
            get { return chkCheckbox.Checked; }
        }
    }
}
