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
    internal sealed class frmSleep : QFixedDialog
    {   
        private QLabel lblInstructions;
        private QLabel lblAndThen;

        private QSpin txtTime;
        private QSpin txtFadeTime;

        private QCheckBox chkForce;
        private QComboBox cboAction;

        private Controller controller;

        private static readonly string SHUTDOWN_TEXT = Localization.Get(UI_Key.Sleep_Shutdown_Computer);
        private static readonly string EXIT_TEXT = Localization.Get(UI_Key.Sleep_Exit_App);
        private static readonly string STANDBY_TEXT = Localization.Get(UI_Key.Sleep_Computer_StandBy);
        private static readonly string HIBERNATE_TEXT = Localization.Get(UI_Key.Sleep_Computer_Hibernate);

        public frmSleep(Controller Controller) : base(Localization.Get(UI_Key.Sleep_Title), ButtonCreateType.OKAndCancel)
        {
            this.controller = Controller;

            this.SuspendLayout();

            lblInstructions = new QLabel(Localization.Get(UI_Key.Sleep_Instructions, Application.ProductName));
            lblInstructions.AutoSize = false;
            this.Controls.Add(lblInstructions);

            txtTime = new QSpin(true, true, Localization.Get(UI_Key.Sleep_Play_For_Another), Localization.Get(UI_Key.Sleep_Minutes), 1, 24 * 60, 60, 5, this.BackColor);
            txtTime.CheckedChanged += new EventHandler(txtTime_CheckedChanged);
            txtTime.ValueChanged += new EventHandler(txtTime_ValueChanged);
            this.Controls.Add(txtTime);

            lblAndThen = new QLabel(Localization.Get(UI_Key.Sleep_And_Then));
            this.Controls.Add(lblAndThen);

            cboAction = new QComboBox(false);
            cboAction.Items.Add(SHUTDOWN_TEXT);
            cboAction.Items.Add(STANDBY_TEXT);
            cboAction.Items.Add(HIBERNATE_TEXT);
            cboAction.Items.Add(EXIT_TEXT);
            
            cboAction.SelectedIndexChanged += new EventHandler(cboAction_SelectedIndexChanged);
            cboAction.Width = TextRenderer.MeasureText(SHUTDOWN_TEXT, this.Font).Width + 40;
            this.Controls.Add(cboAction);

            txtFadeTime = new QSpin(true, true, Localization.Get(UI_Key.Sleep_Gradually_Reduce_Volume_After), Localization.Get(UI_Key.Sleep_Minutes), 0, 24 * 60, 0, 5, this.BackColor);
            txtFadeTime.ValueChanged += new EventHandler(txtFadeTime_ValueChanged);
            this.Controls.Add(txtFadeTime);

            chkForce = new QCheckBox(Localization.Get(UI_Key.Sleep_Force_Shutdown), this.BackColor);
            chkForce.EnabledChanged += (s, e) => { this.Invalidate(); };
            this.Controls.Add(chkForce);

            Sleep = new Sleep(controller);

            txtFadeTime_ValueChanged(this, EventArgs.Empty);
            txtTime_ValueChanged(this, EventArgs.Empty);

            setupControls();

            this.ResumeLayout(false);
        }

        public Sleep Sleep
        {
            get
            {
                return new Sleep(controller,
                                 txtTime.Checked,
                                 txtTime.Value,
                                 txtFadeTime.Checked,
                                 txtFadeTime.Value,
                                 getAction(),
                                 chkForce.Checked);
            }
            set
            {
                if (value == null)
                {
                    value = new Sleep(controller);
                }

                txtTime.Checked = value.Active;

                double alarmMinutes = (value.Alarm - DateTime.Now).TotalMinutes;
                
                if (alarmMinutes < 1)
                    alarmMinutes = 1;
                if (alarmMinutes > 24 * 60)
                    alarmMinutes = 60;

                txtTime.Value = ((int)Math.Round(alarmMinutes, 0));

                txtFadeTime.Checked = value.FadeActive;

                double fadeMinutes = (value.Fade - DateTime.Now).TotalMinutes;

                if (fadeMinutes < 0)
                    fadeMinutes = 0;

                if (fadeMinutes > alarmMinutes)
                {
                    fadeMinutes = 0;
                    txtFadeTime.Checked = false;
                }

                txtFadeTime.Value = ((int)Math.Round(fadeMinutes, 0));

                chkForce.Checked = value.Force;

                switch (value.Action)
                {
                    case Sleep.ActionType.ExitApp:
                        cboAction.SelectedIndex = cboAction.FindStringExact(EXIT_TEXT);
                        break;
                    case Sleep.ActionType.ShutDown:
                        cboAction.SelectedIndex = cboAction.FindStringExact(SHUTDOWN_TEXT);
                        break;
                    case Sleep.ActionType.Hibernate:
                        cboAction.SelectedIndex = cboAction.FindStringExact(HIBERNATE_TEXT);
                        break;
                    case Sleep.ActionType.StandBy:
                        cboAction.SelectedIndex = cboAction.FindStringExact(STANDBY_TEXT);
                        break;
                }
            }
        }
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            txtTime.Focus();
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (chkForce.Enabled)
            {
                if ((e.X < (btnOK.Left - 10)) && (e.Y > chkForce.Top))
                    chkForce.Checked = !chkForce.Checked;
            }
        }
        private void setupControls()
        {
            lblInstructions.Location = new Point(SPACING, SPACING);

            lblInstructions.SetWidth(this.ClientRectangle.Width - SPACING - SPACING);

            txtTime.Location = new Point(2 * SPACING, lblInstructions.Bottom + SPACING + SPACING);

            lblAndThen.Location = new Point(2 * SPACING, txtTime.Bottom + SPACING);

            cboAction.Location = new Point(lblAndThen.Right + 5, lblAndThen.Top + lblAndThen.Height / 2 - cboAction.Height / 2);

            txtFadeTime.Location = new Point(4 * SPACING, cboAction.Bottom + SPACING);

            int buttonTop = txtFadeTime.Bottom + 2 * SPACING;

            chkForce.Location = new Point(2 * SPACING,
                                          buttonTop + btnOK.Height / 2 - chkForce.Height / 2);

            int x = 400;
            foreach (Control c in this.Controls)
                x = Math.Max(x, c.Right);

            this.ClientSize = new Size(x + SPACING, buttonTop + btnOK.Height + SPACING);

            PlaceButtons(this.ClientRectangle.Width, buttonTop);

            //btnCancel.Location = new Point(this.ClientRectangle.Width - btnCancel.Width - SPACING,
              //                             buttonTop);

            //btnOK.Location = new Point(btnCancel.Left - btnOK.Width - SPACING,
              //                         buttonTop);
        }
        private Sleep.ActionType getAction()
        {
            if (cboAction.Text == SHUTDOWN_TEXT)
                return Sleep.ActionType.ShutDown;
            else if (cboAction.Text == STANDBY_TEXT)
                return Sleep.ActionType.StandBy;
            else if (cboAction.Text == HIBERNATE_TEXT)
                return Sleep.ActionType.Hibernate;
            else
                return Sleep.ActionType.ExitApp;
        }

        private void cboAction_SelectedIndexChanged(object sender, EventArgs e)
        {
            chkForce.Enabled = cboAction.SelectedIndex != cboAction.FindStringExact(EXIT_TEXT);

            if (cboAction.SelectedIndex == cboAction.FindStringExact(EXIT_TEXT))
            {
                chkForce.Enabled = false;
                chkForce.Text = Localization.Get(UI_Key.Sleep_Force);
            }
            else if (cboAction.SelectedIndex == cboAction.FindStringExact(SHUTDOWN_TEXT))
            {
                chkForce.Enabled = txtTime.Checked;
                chkForce.Text = Localization.Get(UI_Key.Sleep_Force_Shutdown);
            }
            else if (cboAction.SelectedIndex == cboAction.FindStringExact(HIBERNATE_TEXT))
            {
                chkForce.Enabled = txtTime.Checked;
                chkForce.Text = Localization.Get(UI_Key.Sleep_Force_Hibernate);
            }
            else if (cboAction.SelectedIndex == cboAction.FindStringExact(STANDBY_TEXT))
            {
                chkForce.Enabled = txtTime.Checked;
                chkForce.Text = Localization.Get(UI_Key.Sleep_Force_StandBy);
            }
        }
        private void txtFadeTime_ValueChanged(object sender, EventArgs e)
        {
            txtFadeTime.PostfixText = Localization.Get(UI_Key.Sleep_Minutes) + " (" + (DateTime.Now + TimeSpan.FromMinutes(txtFadeTime.Value)).ToShortTimeString() + ")";
        }
        private void txtTime_ValueChanged(object sender, EventArgs e)
        {
            txtTime.PostfixText = Localization.Get(UI_Key.Sleep_Minutes) + " (" + (DateTime.Now + TimeSpan.FromMinutes(txtTime.Value)).ToShortTimeString() + ")";
            txtFadeTime.Maximum = txtTime.Value;
        }
        private void txtTime_CheckedChanged(object sender, EventArgs e)
        {
            txtFadeTime.Enabled = txtTime.Checked;
            cboAction.Enabled = txtTime.Checked;
            lblAndThen.Enabled = txtTime.Checked;

            cboAction_SelectedIndexChanged(this, EventArgs.Empty);
        }
    }
}
