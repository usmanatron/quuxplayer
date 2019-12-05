/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxControls
{
    public class BeforeShowIntellisenseEventArgs : EventArgs
    {
        private List<string> vals = null;
        private bool cancel = false;
        private bool valueNeedsQuoting = false;
        private string quoteException = String.Empty;

        public BeforeShowIntellisenseEventArgs()
            : base()
        {
        }
        public List<string> Values
        {
            get { return vals; }
            set { vals = value; }
        }
        public bool Cancel
        {
            get { return cancel; }
            set { cancel = value; }
        }
        public bool ValueNeedsQuoting
        {
            get { return valueNeedsQuoting; }
            set { valueNeedsQuoting = value; }
        }
        public string QuoteException
        {
            get { return quoteException; }
            set { quoteException = value; }
        }
    }
}
