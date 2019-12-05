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
    internal sealed partial class frmAbout : QFixedDialog
    {
        private const string alfred = "alfred";

        private int alfredIndex;

        private QLabel lblRegInfo;
        private QLabel lblTitle;
        private QLabel lblVersion;
        private QLabel lblSmallPrint;

        private Controller controller;

        private Brush backgroundBrush;

        public frmAbout(Controller Controller) : base("About " + Application.ProductName, ButtonCreateType.OKAndCancelReverse)
        {
            this.controller = Controller;

            int ypos = MARGIN + MARGIN;

            this.ClientSize = new Size(450, 300);

            lblTitle = new QLabel(Application.ProductName, Styles.FontLarge);
            lblTitle.SetWidth(this.ClientRectangle.Width - MARGIN - MARGIN);
            lblTitle.Location = new Point(MARGIN, ypos);
            this.Controls.Add(lblTitle);
            ypos = lblTitle.Bottom + SPACING;

            lblVersion = new QLabel(Localization.Get(UI_Key.About_Version, Application.ProductVersion), Styles.FontBold);
            lblVersion.SetWidth(lblTitle.Width);
            lblVersion.Location = new Point(MARGIN, ypos);
            this.Controls.Add(lblVersion);
            ypos = lblVersion.Bottom + SPACING + SPACING + SPACING;

            lblRegInfo = new QLabel(Notices.ActivationInfo);
            lblRegInfo.Location = new Point(MARGIN, ypos);
            lblRegInfo.SetWidth(lblVersion.Width);
            this.Controls.Add(lblRegInfo);
            ypos = lblRegInfo.Bottom + SPACING;

            lblSmallPrint = new QLabel(Notices.CopyrightNotice, Styles.FontSmall);
            lblSmallPrint.Location = new Point(MARGIN, ypos);
            lblSmallPrint.SetWidth(this.lblRegInfo.Width);
            this.Controls.Add(lblSmallPrint);
            ypos = lblSmallPrint.Bottom + SPACING;

            btnOK.Text = Localization.Get(UI_Key.About_License_Agreement);

            PlaceButtons(this.ClientRectangle.Width, ypos);

            this.ClientSize = new Size(this.ClientRectangle.Width,
                                       btnOK.Bottom + SPACING);

            btnCancel.Focus();

            alfredIndex = 0;

            backgroundBrush = Style.GetDialogBackgroundBrush(this.ClientRectangle.Height);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            if (e.KeyChar.ToString().ToLowerInvariant()[0] == alfred[alfredIndex])
                alfredIndex++;

            if (alfredIndex >= alfred.Length)
            {
                alfredIndex = 0;

                frmAlfred a = new frmAlfred();
                a.ShowDialog(this);
            }
        }
        protected override void ok()
        {
            controller.RequestAction(QActionType.ReleaseFullScreenAuto);
            Lib.Run(Lib.ProgramPath("license.rtf"));
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            e.Graphics.FillRectangle(backgroundBrush, this.ClientRectangle);

            e.Graphics.DrawImageUnscaled(Properties.Resources.help_screen_graphic,
                                         new Point(this.ClientRectangle.Width - Properties.Resources.help_screen_graphic.Width,
                                                   5));
        }
    }
}
