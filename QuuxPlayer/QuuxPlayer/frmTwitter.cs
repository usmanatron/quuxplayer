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
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal sealed partial class frmTwitter : QFixedDialog
    {
        private QLabel lblInstructions;
        private QCheckBox chkEnable;

        private QLabel lblUserName;
        private QLabel lblPassword;
        private QLabel lblMode;

        private QTextBox txtUserName;
        private QTextBox txtPassword;

        private QComboBox cboMode;

        private QButton btnTest;
        private QButton btnViewOnWeb;

        public frmTwitter() : base(Localization.Get(UI_Key.Twitter_Title), ButtonCreateType.OKAndCancel)
        {
            this.ClientSize = new Size(420, 200);

            lblInstructions = new QLabel(Localization.Get(UI_Key.Twitter_Instructions, Application.ProductName));
            lblInstructions.Location = new Point(MARGIN, MARGIN);
            lblInstructions.SetWidth(this.ClientRectangle.Width - MARGIN - MARGIN);
            this.Controls.Add(lblInstructions);

            chkEnable = new QCheckBox(Localization.Get(UI_Key.Twitter_Enable), this.BackColor);
            chkEnable.Location = new Point(MARGIN, lblInstructions.Bottom + MARGIN + MARGIN);
            chkEnable.CheckedChanged += new EventHandler(enableCheckChanged);
            this.Controls.Add(chkEnable);

            lblUserName = new QLabel(Localization.Get(UI_Key.Twitter_User_Name));
            lblPassword = new QLabel(Localization.Get(UI_Key.Twitter_Password));

            txtUserName = new QTextBox();
            txtUserName.Text = Twitter.UserName;
            txtUserName.MaxLength = 64;
            txtUserName.TextChanged += new EventHandler(textChanged);

            txtPassword = new QTextBox();
            txtPassword.Text = Twitter.Password;
            txtPassword.MaxLength = 64;
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

            lblMode = new QLabel("Post A Tweet With Each:");
            this.Controls.Add(lblMode);
            lblMode.Location = new Point(4 * MARGIN, txtPassword.Bottom + MARGIN);

            cboMode = new QComboBox(false);
            cboMode.Items.Add(Localization.Get(UI_Key.Twitter_Mode_Song));
            cboMode.Items.Add(Localization.Get(UI_Key.Twitter_Mode_Album));

            this.Controls.Add(cboMode);

            cboMode.Location = new Point(lblMode.Right + MARGIN, lblMode.Top + (lblMode.Height - cboMode.Height) / 2);

            switch (Twitter.TwitterMode)
            {
                case Twitter.Mode.Album:
                    cboMode.SelectedIndex = cboMode.FindStringExact(Localization.Get(UI_Key.Twitter_Mode_Album));
                    break;
                default:
                    cboMode.SelectedIndex = cboMode.FindStringExact(Localization.Get(UI_Key.Twitter_Mode_Song));
                    break;
            }

            btnTest = new QButton(Localization.Get(UI_Key.Twitter_Test), false, false);
            AddButton(btnTest, test);

            btnViewOnWeb = new QButton(Localization.Get(UI_Key.Twitter_View_On_Web), false, false);
            AddButton(btnViewOnWeb, viewOnWeb);

            PlaceButtons(this.ClientRectangle.Width,
                         cboMode.Bottom + MARGIN + MARGIN,
                         btnCancel,
                         btnOK,
                         btnTest,
                         btnViewOnWeb);

            this.ClientSize = new Size(this.ClientRectangle.Width, btnCancel.Bottom + MARGIN);

            int tabIndex = 0;

            chkEnable.TabIndex = tabIndex++;
            txtUserName.TabIndex = tabIndex++;
            txtPassword.TabIndex = tabIndex++;

            btnViewOnWeb.TabIndex = tabIndex++;
            btnTest.TabIndex = tabIndex++;
            btnOK.TabIndex = tabIndex++;
            btnCancel.TabIndex = tabIndex++;

            chkEnable.Checked = Twitter.On;
            enableControls();
        }
        private void viewOnWeb()
        {
            Net.BrowseTo("http://twitter.com/" + txtUserName.Text);
        }
        private void enableCheckChanged(object sender, EventArgs e)
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
            lblMode.Enabled = enabled;
            cboMode.Enabled = enabled;

            btnViewOnWeb.Enabled = txtUserName.Text.Length > 0;
            btnTest.Enabled = enabled && btnViewOnWeb.Enabled && txtPassword.Text.Length > 0;
            
            btnOK.Enabled = !enabled || (txtPassword.Text.Length > 0 && txtUserName.Text.Length > 0);
        }

        protected override void ok()
        {
            Twitter.On = chkEnable.Checked;
            Twitter.UserName = txtUserName.Text;
            Twitter.Password = txtPassword.Text;

            if (cboMode.Text == Localization.Get(UI_Key.Twitter_Mode_Album))
                Twitter.TwitterMode = Twitter.Mode.Album;
            else
                Twitter.TwitterMode = Twitter.Mode.Track;
            
            this.Close();
        }
        private void test()
        {
            btnOK.Enabled = false;
            btnCancel.Enabled = false;
            btnTest.Enabled = false;
            btnViewOnWeb.Enabled = false;

            if (Twitter.SendTwitterMessage(Localization.Get(UI_Key.Twitter_Test_Message), txtUserName.Text, txtPassword.Text))
                QMessageBox.Show(this,
                                 Localization.Get(UI_Key.Twitter_Test_Message_Sent),
                                 Localization.Get(UI_Key.Twitter_Test_Message_Sent_Title),
                                 QMessageBoxIcon.Information);
            else
                QMessageBox.Show(this,
                                 Localization.Get(UI_Key.Twitter_Test_Message_Failed),
                                 Localization.Get(UI_Key.Twitter_Test_Message_Failed_Title),
                                 QMessageBoxIcon.Error);

            enableControls();
        }
        private void textChanged(object sender, EventArgs e)
        {
            enableControls();
        }
    }
}