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
    internal class QControl : Control
    {
        public QControl()
        {
            this.DoubleBuffered = true;
            this.BackColor = Color.Black;
        }
    }
    internal interface IWatermarkable
    {
        void EnableWatermark(Control Parent, string WatermarkText, string NullText);
        bool WatermarkEnabled { set; }
        Control.ControlCollection Controls { get; }
    }
    internal interface IEditControl
    {
        object Tag { get; set; }
        string Text { get; set; }
        AutoCompleteMode AutoCompleteMode { get; set; }
        AutoCompleteSource AutoCompleteSource { get; set; }
        AutoCompleteStringCollection AutoCompleteCustomSource { get; set; }
        void SetOriginalText();
        string OriginalText { get; }
        bool Changed { get; }
        Color ForeColor { get; set; }
        Color BackColor { get; set; }
        int TabIndex { get; set; }
        int Left { get; set; }
        int Right { get; }
        int Top { get; set; }
        int Bottom { get; }
        int Width { get; set; }
        int Height { get; set; }
        Rectangle Bounds { get; set; }
        bool Highlighted { get; set; }

        event EventHandler TextChanged;
    }
}
