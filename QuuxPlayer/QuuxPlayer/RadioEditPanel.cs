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
    internal class RadioEditPanel : QPanel
    {
        private RadioStation station;
        private Radio.StationEditComplete callback;

        private QLabel lblName;
        private QLabel lblGenre;
        private QLabel lblBitRate;
        private QLabel lblStreamType;
        private QLabel lblURL;

        private QTextBox txtName;
        private QComboBox cboGenre;
        private QTextBox txtBitrate;
        private QComboBox cboStreamType;
        private QTextBox txtURL;

        public RadioEditPanel(RadioStation Station, string[] GenreList, Radio.StationEditComplete Callback) : base()
        {
            SPACING = 8;

            station = Station;
            callback = Callback;

            lblName = new QLabel("&Name");
            lblName.ShowAccellerator();
            this.Controls.Add(lblName);

            lblGenre = new QLabel("&Genre");
            lblGenre.ShowAccellerator();
            this.Controls.Add(lblGenre);

            lblBitRate = new QLabel("&Bit Rate");
            lblBitRate.ShowAccellerator();
            this.Controls.Add(lblBitRate);

            lblStreamType = new QLabel("&Stream Type");
            lblStreamType.ShowAccellerator();
            this.Controls.Add(lblStreamType);

            lblURL = new QLabel("&URL");
            lblURL.ShowAccellerator();
            this.Controls.Add(lblURL);

            txtName = new QTextBox();
            txtName.Width = 1000;
            txtName.MaxLength = 140;
            txtName.GotFocus += (s, e) => txtName.SelectAll();
            this.Controls.Add(txtName);

            cboGenre = new QComboBox(true);
            cboGenre.MaxLength = 30;
            cboGenre.Items.AddRange(GenreList);
            this.Controls.Add(cboGenre);

            txtBitrate = new QTextBox();
            txtBitrate.NumericOnly = true;
            txtBitrate.MaxLength = 3;
            this.Controls.Add(txtBitrate);

            cboStreamType = new QComboBox(false);
            cboStreamType.Items.AddRange(RadioStation.StreamTypeArray);
            this.Controls.Add(cboStreamType);

            txtURL = new QTextBox();
            txtURL.MaxLength = 2048;
            this.Controls.Add(txtURL);

            txtName.Text = station.Name;
            cboGenre.Text = station.Genre;
            if (station.BitRate > 0)
                txtBitrate.Text = station.BitRate.ToString();
            else
                txtBitrate.Text = String.Empty;

            cboStreamType.SelectedIndex = (int)station.StreamType;
            txtURL.Text = station.URL;

            btnOK = new QButton(Localization.OK, false, false);
            AddButton(btnOK, ok);
            btnCancel = new QButton(Localization.CANCEL, false, false);
            AddButton(btnCancel, cancel);

            resize();

            this.ClientSize = new Size(this.ClientRectangle.Width, btnOK.Bottom + MARGIN);

            int tabIndex = 0;

            lblName.TabIndex = tabIndex++;
            txtName.TabIndex = tabIndex++;
            lblGenre.TabIndex = tabIndex++;
            cboGenre.TabIndex = tabIndex++;
            lblURL.TabIndex = tabIndex++;
            txtURL.TabIndex = tabIndex++;
            lblBitRate.TabIndex = tabIndex++;
            txtBitrate.TabIndex = tabIndex++;
            lblStreamType.TabIndex = tabIndex++;
            cboStreamType.TabIndex = tabIndex++;
            btnOK.TabIndex = tabIndex++;
            btnCancel.TabIndex = tabIndex++;

            setWrapAroundTabControl(tabIndex, txtName, null);

            initialized = true;
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Enter:
                    ok();
                    return true;
                case Keys.Escape:
                    cancel();
                    return true;
                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }
        }
        protected override void resize()
        {
            base.resize();

            this.SuspendLayout();

            txtBitrate.Width = this.ClientRectangle.Width / 5;
            cboStreamType.Width = txtBitrate.Width;

            lblName.Location = new Point(MARGIN, MARGIN);
            lblGenre.Location = new Point(MARGIN, lblName.Bottom + SPACING);
            lblURL.Location = new Point(MARGIN, lblGenre.Bottom + SPACING);

            txtName.Location = new Point(Math.Max(lblName.Right, lblGenre.Right) + SPACING, lblName.Top + (lblName.Height - txtName.Height) / 2);
            cboGenre.Location = new Point(txtName.Left, lblGenre.Top + (lblGenre.Height - cboGenre.Height) / 2);
            txtURL.Location = new Point(txtName.Left, lblURL.Top + (lblURL.Height - txtURL.Height) / 2);

            txtBitrate.Location = new Point(this.ClientRectangle.Width - MARGIN - txtBitrate.Width, txtName.Top);
            cboStreamType.Location = new Point(txtBitrate.Left, cboGenre.Top);

            lblBitRate.Location = new Point(txtBitrate.Left - Math.Max(lblBitRate.Width, lblStreamType.Width) - SPACING, lblName.Top);
            lblStreamType.Location = new Point(lblBitRate.Left, lblGenre.Top);

            if (this.Width > 300)
            {
                txtName.Width = lblBitRate.Left - txtName.Left - SPACING;
                cboGenre.Width = txtName.Width;
                txtURL.Width = txtName.Width;
            }

            btnCancel.Location = new Point(this.ClientRectangle.Width - btnCancel.Width - MARGIN, cboGenre.Bottom + SPACING);
            btnOK.Location = new Point(btnCancel.Left - btnOK.Width - SPACING, btnCancel.Top);

            // weird bug where i can't prevent cboGenre from text being selected
            if (!cboGenre.Focused)
                cboGenre.SelectionLength = 0;

            this.ResumeLayout();
        }
        protected override void ok()
        {
            station.Name = txtName.Text.Trim();
            station.StreamType = (StationStreamType)cboStreamType.SelectedIndex;
            station.Genre = cboGenre.Text;

            int br;
            if (Int32.TryParse(txtBitrate.Text, out br))
            {
                if (br >= 0 && br <= 512)
                    station.BitRate = br;
            }

            station.URL = txtURL.Text.Trim();

            callback(Radio.StationEditAction.OK);
        }
        protected override void cancel()
        {
            callback(Radio.StationEditAction.Cancel);
        }
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            txtName.Focus();
        }
    }
}
