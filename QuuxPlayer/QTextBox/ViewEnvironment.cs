/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxControls
{
    internal static class ViewEnvironment
    {
        //public static EmptyDelegate NumVisibleColumnsChanged;
        public static EmptyDelegate NumColumnsPerLineChanged;

        private static int numColumnsPerLine = int.MaxValue;
        private static int numColumnsPerLineAlt = 80;
        private static int numVisibleColumns = 80;
        private static bool biasPreviousLine = false;

        public static int NumColumnsPerLine
        {
            get { return numColumnsPerLine; }
            set
            {
                if (NumColumnsPerLine == int.MaxValue)
                {
                    numColumnsPerLineAlt = value;
                }
                else if (numColumnsPerLine != value)
                {
                    numColumnsPerLine = value;
                    NumColumnsPerLineChanged.Invoke();
                }
            }
        }
        public static int NumVisibleColumns
        {
            get { return numVisibleColumns; }
            set
            {
                if (numVisibleColumns != value)
                {
                    numVisibleColumns = value;
                    //NumVisibleColumnsChanged.Invoke();
                }
            }
        }
        public static bool WordWrap
        {
            get { return NumColumnsPerLine != int.MaxValue; }
            set
            {
                if (WordWrap != value)
                {
                    if (value)
                    {
                        numColumnsPerLine = -1;
                        NumColumnsPerLine = numColumnsPerLineAlt;
                    }
                    else
                    {
                        numColumnsPerLineAlt = numColumnsPerLine;
                        numColumnsPerLine = -1;
                        NumColumnsPerLine = int.MaxValue;
                    }
                }
            }
        }
        public static bool BiasPreviousLine
        {
            get { return biasPreviousLine; }
            set { biasPreviousLine = value; }
        }

    }
}
