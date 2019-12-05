/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxControls
{
    internal class DocumentRangeLength
    {
        public int Lines { get; private set; }
        public int Columns { get; private set; }

        public DocumentRangeLength(int Lines, int Columns)
        {
            this.Lines = Lines;
            this.Columns = Columns;
        }
    }
}
