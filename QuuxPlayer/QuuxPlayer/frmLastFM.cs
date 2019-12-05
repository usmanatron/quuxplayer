/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Web;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal sealed partial class frmLastFM : QFixedDialog
    {
        private QLabel lblInstructions;
        private QCheckBox chkEnable;
        private QLabel lblUserName;
        private QLabel lblPassword;
        private QTextBox txtUserName;
        private QTextBox txtPassword;
        private QButton btnGoToAcccount;

        public frmLastFM(bool Enabled, string UserName, string Password) : base(Localization.Get(UI_Key.LastFM_Title), ButtonCreateType.OKAndCancel)
        {
            this.ClientSize = new Size(370, 200);

            lblInstructions = new QLabel(Localization.Get(UI_Key.LastFM_Instructions, Application.ProductName));
            lblInstructions.Location = new Point(MARGIN, MARGIN);
            lblInstructions.SetWidth(this.ClientRectangle.Width - MARGIN - MARGIN);
            this.Controls.Add(lblInstructions);

            chkEnable = new QCheckBox(Localization.Get(UI_Key.LastFM_Enable), this.BackColor);
            chkEnable.Checked = Enabled;
            chkEnable.Location = new Point(MARGIN, lblInstructions.Bottom + MARGIN + MARGIN);
            chkEnable.CheckedChanged += new EventHandler(chkEnableChanged);
            this.Controls.Add(chkEnable);

            lblUserName = new QLabel(Localization.Get(UI_Key.LastFM_User_Name));
            lblPassword = new QLabel(Localization.Get(UI_Key.LastFM_Password));

            lblUserName.Enabled = Enabled;
            lblPassword.Enabled = Enabled;

            txtUserName = new QTextBox();
            txtUserName.Text = UserName;
            txtUserName.MaxLength = 64;
            txtUserName.Enabled = Enabled;
            txtUserName.TextChanged += new EventHandler(textChanged);

            txtPassword = new QTextBox();
            txtPassword.Text = Password;
            txtPassword.MaxLength = 64;
            txtPassword.Enabled = Enabled;
            txtPassword.TextChanged += new EventHandler(textChanged);
            txtPassword.PasswordChar = '*';

            lblUserName.Location = new Point(2 * MARGIN, chkEnable.Bottom + MARGIN + MARGIN + (txtUserName.Height - lblUserName.Height) / 2);
            lblPassword.Location = new Point(2 * MARGIN, lblUserName.Top + MARGIN + txtUserName.Height);

            this.Controls.Add(lblUserName);
            this.Controls.Add(lblPassword);

            int x = Math.Max(lblUserName.Right, lblPassword.Right) + MARGIN;

            txtUserName.Location = new Point(x, lblUserName.Top + (lblUserName.Height - txtUserName.Height) / 2);
            txtPassword.Location = new Point(x, lblPassword.Top + (lblPassword.Height - txtPassword.Height) / 2);

            this.Controls.Add(txtUserName);
            this.Controls.Add(txtPassword);

            //btnCancel.Location = new Point(this.ClientRectangle.Width - MARGIN - btnCancel.Width, txtPassword.Bottom + MARGIN + MARGIN);

            //btnOK.Location = new Point(btnCancel.Left - btnOK.Width - MARGIN, btnCancel.Top);

            btnGoToAcccount = new QButton(Localization.Get(UI_Key.LastFM_Go_To_Account), false, false);
            AddButton(btnGoToAcccount, gotoAccount);

            //btnGoToAcccount.Location = new Point(btnOK.Left - btnGoToAcccount.Width - MARGIN, btnCancel.Top);

            PlaceButtons(this.ClientRectangle.Width,
                         txtPassword.Bottom + MARGIN + MARGIN,
                         btnCancel,
                         btnOK,
                         btnGoToAcccount);

            this.ClientSize = new Size(this.ClientRectangle.Width, btnCancel.Bottom + MARGIN);

            int tabIndex = 0;

            chkEnable.TabIndex = tabIndex++;
            txtUserName.TabIndex = tabIndex++;
            txtPassword.TabIndex = tabIndex++;
            btnGoToAcccount.TabIndex = tabIndex++;
            btnOK.TabIndex = tabIndex++;
            btnCancel.TabIndex = tabIndex++;

            enableControls();
        }

        private void gotoAccount()
        {
            if (txtUserName.Text.Length > 0)
            {
                Net.BrowseTo("http://www.last.fm/user/" + HttpUtility.UrlEncode(txtUserName.Text));
            }
        }
        public bool On
        {
            get { return chkEnable.Checked; }
        }
        public string UserName
        {
            get { return txtUserName.Text; }
        }
        public string Password
        {
            get { return txtPassword.Text; }
        }

        protected override void OnKeyUp(System.Windows.Forms.KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (!e.Handled)
            {
                switch (e.KeyData)
                {
                    case System.Windows.Forms.Keys.Enter:
                    case System.Windows.Forms.Keys.Space:
                        if (this.ActiveControl == btnGoToAcccount)
                        {
                            gotoAccount();
                        }
                        break;
                }
            }
        }
        private void textChanged(object sender, EventArgs e)
        {
            enableControls();
        }
        private void chkEnableChanged(object sender, EventArgs e)
        {
            enableControls();
        }

        private void enableControls()
        {
            bool enabled = chkEnable.Checked;

            lblUserName.Enabled = enabled;
            txtUserName.Enabled = enabled;
            lblPassword.Enabled = enabled;
            txtPassword.Enabled = enabled;

            btnOK.Enabled = !enabled || (txtPassword.Text.Length > 0 && txtUserName.Text.Length > 0);
            btnGoToAcccount.Enabled = txtUserName.Text.Length > 0;
        }
    }
}
