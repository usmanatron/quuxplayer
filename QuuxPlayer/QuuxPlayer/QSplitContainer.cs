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
    internal class QSplitContainer : SplitContainer
    {
        public const int MIN_SPLITTER_DISTANCE = 100;
        public static bool Initialized { get; set; }

        public QSplitContainer()
        {
            this.Width = 800;
            this.FixedPanel = FixedPanel.Panel1;
            this.Panel1MinSize = MIN_SPLITTER_DISTANCE;
            this.Panel2MinSize = 600;
            this.SplitterMoved += new SplitterEventHandler(QSplitContainer_SplitterMoved);
        }
        static QSplitContainer()
        {
            Initialized = false;
        }
        private void QSplitContainer_SplitterMoved(object sender, EventArgs e)
        {
            if (Initialized)
                Setting.SplitterDistance = this.SplitterDistance;
        }
    }
}
