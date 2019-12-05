/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal class RadioGenreSelectPanel : QSelectPanel
    {
        protected override void OnMouseDown(System.Windows.Forms.MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!Locked)
            {
                if (this.Value.Length > 0)
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        ContextMenuStrip cms = new ContextMenuStrip();
                        cms.Renderer = new MenuItemRenderer();
                        ToolStripMenuItem tsi;
                        ToolStripSeparator tss;

                        tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Radio_Rename_Genre));
                        tsi.Click += (s, ee) => { this.StartItemEdit(); };
                        cms.Items.Add(tsi);

                        tss = new ToolStripSeparator();
                        cms.Items.Add(tss);

                        tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Radio_Remove_Genre, this.Value));
                        tsi.Click += (s, ee) =>
                            {
                                Radio.RadioStations = Radio.RadioStations.FindAll(rs => rs.Genre != this.Value).ToList();
                            };
                        cms.Items.Add(tsi);

                        cms.Show(this, e.Location);
                    }
                }
            }
        }
    }
}
