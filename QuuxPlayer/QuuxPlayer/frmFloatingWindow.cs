/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal abstract class frmFloatingWindow : QFixedDialog
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        protected static extern IntPtr CreateRoundRectRgn(int nLeftRect,
                                                        int nTopRect,
                                                        int nRightRect,
                                                        int nBottomRect,
                                                        int nWidthEllipse,
                                                        int nHeightEllipse);
        public frmFloatingWindow() : base(String.Empty, ButtonCreateType.None)
        {
            this.MARGIN = 3;
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
        }
    }
}
