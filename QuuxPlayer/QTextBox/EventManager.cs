/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxControls
{
    internal static class EventManager
    {

        public enum EventType { NumLinesChanged, LongestLineChanged, DocumentChanged, DocumentChangedChanged, CaretLocationChanged, ViewChanged, SeletionChanged }

        public static EmptyDelegate NumLinesChanged;
        public static EmptyDelegate LongestLineChanged;
        public static EmptyDelegate DocumentChanged;
        public static EmptyDelegate DocumentChangedChanged;
        public static EmptyDelegate CaretLocationChanged;
        public static EmptyDelegate ViewChanged;
        public static EmptyDelegate SelectionChanged;

        private static bool numLinesChanged = false;
        private static bool longestLinechanged = false;
        private static bool documentChanged = false;
        private static bool documentChangedChanged = false;
        private static bool caretLocationChanged;
        private static bool viewChanged;
        private static bool selectionChanged;

        private static int count = 0;
        private static int holdLevel;

        static EventManager()
        {
            holdLevel = 0;
        }

        public static void HoldEvents()
        {
            holdLevel++;
        }
        public static void ReleaseEvents(bool Force)
        {
            count++;

            if (--holdLevel <= 0 || Force)
            {
                holdLevel = 0;
                
                if (numLinesChanged)
                {
                    NumLinesChanged.Invoke();
                    numLinesChanged = false;
                }
                if (longestLinechanged)
                {
                    LongestLineChanged.Invoke();
                    longestLinechanged = false;
                }
                

                if (documentChanged)
                {
                    DocumentChanged.Invoke();
                    documentChanged = false;
                }
                else if (caretLocationChanged)
                {
                    CaretLocationChanged.Invoke();
                }
                else if (viewChanged)
                {
                    ViewChanged.Invoke();
                }

                caretLocationChanged = false;
                viewChanged = false;

                if (selectionChanged)
                {
                    SelectionChanged.Invoke();
                    selectionChanged = false;
                }
                if (documentChangedChanged)
                {
                    DocumentChangedChanged.Invoke();
                    documentChangedChanged = false;
                }
            }
        }
        public static void DoEvent(EventType Type)
        {
            if (holdLevel > 0)
            {
                switch (Type)
                {
                    case EventType.NumLinesChanged:
                        numLinesChanged = true;
                        break;
                    case EventType.LongestLineChanged:
                        longestLinechanged = true;
                        break;
                    case EventType.DocumentChangedChanged:
                        documentChangedChanged = true;
                        break;
                    case EventType.DocumentChanged:
                        documentChanged = true;
                        break;
                    case EventType.CaretLocationChanged:
                        caretLocationChanged = true;
                        break;
                    case EventType.ViewChanged:
                        viewChanged = true;
                        break;
                    case EventType.SeletionChanged:
                        selectionChanged = true;
                        break;
                }
            }
            else
            {
                switch (Type)
                {
                    case EventType.NumLinesChanged:
                        NumLinesChanged.Invoke();
                        break;
                    case EventType.LongestLineChanged:
                        LongestLineChanged.Invoke();
                        break;
                    case EventType.DocumentChangedChanged:
                        DocumentChangedChanged.Invoke();
                        break;
                    case EventType.DocumentChanged:
                        DocumentChanged.Invoke();
                        break;
                    case EventType.CaretLocationChanged:
                        CaretLocationChanged.Invoke();
                        break;
                    case EventType.ViewChanged:
                        ViewChanged.Invoke();
                        break;
                    case EventType.SeletionChanged:
                        SelectionChanged.Invoke();
                        break;
                }
            }
        }
    }
}
