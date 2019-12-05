/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxControls
{

    internal class QVScrollBar : VScrollBar
    {
        public delegate void BeforeValChangedDelegate(QScrollEventArgs Args);

        public BeforeValChangedDelegate ValChanged;
        private int val = -1;
        private int max = -1;
        
        private int numVisibleLines;

        public QVScrollBar(int Width) : base()
        {
            this.Width = Width;
            this.val = this.Value;
            this.SmallChange = 1;
        }

        private bool allowValueChangedEvent = false;
        protected override void OnValueChanged(EventArgs e)
        {
            base.OnValueChanged(e);

            if (allowValueChangedEvent)
            {
                QScrollEventArgs ee = new QScrollEventArgs(this.Value - val);

                ValChanged.Invoke(ee);

                if (ee.Accept)
                    val = Value;
                else
                    Value = val;
            }
            else
            {
                val = Value;
            }
        }
        public void SetValue(int NewValue)
        {
            this.MaxVal = Math.Max(NewValue, this.MaxVal);
            allowValueChangedEvent = false;
            this.Value = NewValue;
            allowValueChangedEvent = true;
        }
        public int NumVisibleLines
        {
            get { return numVisibleLines; }
            set
            {
                if (this.numVisibleLines != value)
                {
                    numVisibleLines = value;
                    this.LargeChange = numVisibleLines;

                    int m = this.max;
                    this.max = -1;
                    this.MaxVal = m;
                    
                    this.SmallChange = 1;
                }
            }
        }
        public int MaxVal
        {
            get { return max; }
            set
            {
                try
                {
                    if (max != value)
                    {
                        max = value;
                        base.Maximum = max + LargeChange - 1;
                    }
                }
                catch { }
            }
        }
    }
    internal class QHScrollBar : HScrollBar
    {
        public delegate void ValChangedDelegate(QScrollEventArgs Args);

        private int val;

        private int max;

        private int numVisibleColumns;

        public QHScrollBar(int Height)
            : base()
        {
            this.Height = Width;
            this.val = this.Value;
            this.SmallChange = 1;
            this.MaxValue = base.Maximum;
        }
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            this.Value = 0;
        }
        public int MaxValue
        {
            get { return max; }
            set
            {
                if (max != value)
                {
                    max = value;
                    base.Maximum = max + LargeChange - 1;
                }
            }
        }
        public int NumVisibleColumns
        {
            get { return numVisibleColumns; }
            set
            {
                if (this.numVisibleColumns != value)
                {
                    numVisibleColumns = value;
                    this.LargeChange = Math.Max(10, numVisibleColumns);
                    this.SmallChange = 1;
                }
            }
        }
    }
    internal class QScrollEventArgs : EventArgs
    {
        public int Delta { get; private set; }
        public bool Accept { get; set; }
        public QScrollEventArgs(int Delta)
        {
            this.Delta = Delta;
            this.Accept = true;
        }
    }
}
