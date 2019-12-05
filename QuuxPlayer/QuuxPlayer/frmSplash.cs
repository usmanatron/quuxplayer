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
    internal sealed partial class frmSplash : Form
    {
        private const int MARGIN = 11;
        private const int SPACING = 9;

        private int textWidth;

        private Size clientSize;
        private Brush backgroundBrush;

        private TextFormatFlags tff = TextFormatFlags.HidePrefix | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoClipping;

        private Rectangle titleRect;
        private Rectangle versionRect;
        private Rectangle infoRect;
        private Rectangle copyrightRect;

        public frmSplash()
        {
            clientSize = new Size(450, 260);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = false;

            textWidth = clientSize.Width - MARGIN - MARGIN;

            this.DoubleBuffered = true;

            InitializeComponent();

            this.Size = this.SizeFromClientSize(clientSize);

            this.Click += (s, e) => { this.Close(); };

            setHeight();

            backgroundBrush = Style.GetDialogBackgroundBrush(this.Height);
        }
        private void setHeight()
        {
            Size s = new Size(textWidth, 1000);

            titleRect =   new Rectangle(new Point(MARGIN, MARGIN),
                                        TextRenderer.MeasureText(Application.ProductName, Styles.FontHeading, s, tff));

            versionRect = new Rectangle(new Point(MARGIN, titleRect.Bottom + SPACING),
                                        TextRenderer.MeasureText("Version " + Application.ProductVersion, Styles.Font, s, tff));

            infoRect =    new Rectangle(new Point(MARGIN, versionRect.Bottom + SPACING),
                                        TextRenderer.MeasureText(Notices.ActivationInfo, Styles.Font, s, tff));

            copyrightRect = new Rectangle(new Point(MARGIN, infoRect.Bottom + 3 * SPACING),
                                        TextRenderer.MeasureText(Notices.CopyrightNotice, Styles.FontSmall, s, tff));

            this.ClientSize = new Size(this.clientSize.Width,
                                       copyrightRect.Bottom + MARGIN);

        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.FillRectangle(backgroundBrush, this.ClientRectangle);

            TextRenderer.DrawText(e.Graphics, Application.ProductName, Styles.FontHeading, titleRect, Styles.LightText, tff);

            TextRenderer.DrawText(e.Graphics, "Version " + Application.ProductVersion, Styles.Font, versionRect, Styles.LightText, tff);

            TextRenderer.DrawText(e.Graphics, Notices.ActivationInfo, Styles.Font, infoRect, Styles.LightText, tff);

            TextRenderer.DrawText(e.Graphics, Notices.CopyrightNotice, Styles.FontSmall, copyrightRect, Styles.LightText, tff);

            e.Graphics.DrawRectangle(Styles.SortArrowPen, 0, 0, this.ClientRectangle.Width - 1, this.ClientRectangle.Height - 1);
        }
    }
}
