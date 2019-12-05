/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxPlayer
{
    internal class frmNumberTracks : QFixedDialog
    {
        private QLabel lblHeading;
        private QSpin spnFirst;
        private QLabel lblLast;
        private int range;

        public frmNumberTracks(int First, int Range)
            : base(Localization.Get(UI_Key.Number_Tracks_Title), ButtonCreateType.OKAndCancel)
        {
            range = Range;

            this.ClientSize = new System.Drawing.Size(350, 100);
            lblHeading = new QLabel(Localization.Get(UI_Key.Number_Tracks_Heading));
            lblHeading.Location = new System.Drawing.Point(MARGIN, MARGIN);
            this.Controls.Add(lblHeading);
            lblHeading.SetWidth(this.ClientRectangle.Width - MARGIN - MARGIN);
            
            int ypos = lblHeading.Bottom + SPACING;
            
            spnFirst = new QSpin(false, false, Localization.Get(UI_Key.Number_Tracks_First), String.Empty, 1, 200, 1, 1, this.BackColor);
            spnFirst.Value = First;
            spnFirst.Location = new System.Drawing.Point(MARGIN + MARGIN, ypos);
            spnFirst.ValueChanged += valueChanged;
            this.Controls.Add(spnFirst);

            ypos = spnFirst.Bottom + SPACING;

            lblLast = new QLabel(Localization.Get(UI_Key.Number_Tracks_Last, (First + range - 1).ToString()));
            lblLast.Location = new System.Drawing.Point(MARGIN + MARGIN, ypos);
            lblLast.SetWidth(this.ClientRectangle.Width - MARGIN - MARGIN - MARGIN);
            this.Controls.Add(lblLast);

            ypos = lblLast.Bottom + SPACING;

            PlaceButtons(this.ClientRectangle.Right, ypos);

            this.ClientSize = new System.Drawing.Size(this.ClientSize.Width, btnOK.Bottom + MARGIN);

            spnFirst.Focus();
        }
        public void valueChanged(object sender, EventArgs e)
        {
            lblLast.Text = Localization.Get(UI_Key.Number_Tracks_Last, (spnFirst.Value + range - 1).ToString());
        }
        public int First
        {
            get { return spnFirst.Value; }
        }
    }
}
