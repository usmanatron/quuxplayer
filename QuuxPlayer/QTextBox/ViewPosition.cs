/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxControls
{
    internal class ViewPosition
    {
        private int line;
        private int subLine;
        private int column;

        public ViewPosition()
        {
            Reset();
        }

        public int Line
        {
            get { return line; }
            set
            {
                if (line != value)
                {
                    line = value;
                    EventManager.DoEvent(EventManager.EventType.ViewChanged);
                }
            }
        }
        public int SubLine
        {
            get { return subLine; }
            set
            {
                if (subLine != value)
                {
                    subLine = value;
                    EventManager.DoEvent(EventManager.EventType.ViewChanged);
                }
            }
        }
        public int Column
        {
            get { return column; }
            set
            {
                if (column != value)
                {
                    column = value;
                    EventManager.DoEvent(EventManager.EventType.ViewChanged);
                }
            }
        }
        public void Set(int Line, int SubLine, int Column)
        {
            if (line != Line || subLine != SubLine || column != Column)
            {
                line = Line;
                subLine = SubLine;
                column = Column;

                EventManager.DoEvent(EventManager.EventType.ViewChanged);
            }
        }
        public void Reset()
        {
            line = 0;
            subLine = 0;
            column = 0;
        }
    }
}
