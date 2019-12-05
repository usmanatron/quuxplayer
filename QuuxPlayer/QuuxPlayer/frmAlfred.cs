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
    internal sealed class frmAlfred : QFixedDialog
    {
        Bitmap alfred;

        public frmAlfred() : base("Alfred!", ButtonCreateType.None)
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            
            alfred = Properties.Resources.alfred_e_neuman;
#if DEBUG
            //QMessageBox.Show(this, "Key: " + Activation.MakeKey(), "New Key", QMessageBoxIcon.Information);
            List<RadioStation> rr = Radio.RadioStations;
            foreach (RadioStation r in rr)
            {
                Console.WriteLine("rr.Add(new RadioStation(");
                Console.WriteLine("\t\"" + r.Name.Replace("\"", "\\\"") + "\", ");
                Console.WriteLine("\t\"" + r.URL.Replace("\"", "\\\"") + "\", ");
                Console.WriteLine("\t\"" + r.Genre.Replace("\"", "\\\"") + "\", ");
                Console.WriteLine("\t" + r.BitRate.ToString()+ ", ");
                Console.WriteLine("\t(StationStreamType)" + ((int)r.StreamType).ToString() + "));");
            }
#endif
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.Size = this.SizeFromClientSize(alfred.Size);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.DrawImage(alfred, new Rectangle(Point.Empty, alfred.Size));
        }
        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            this.Close();
        }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            this.Close();
        }
    }
}
