/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxPlayer
{
    internal sealed class ViewState
    {
        private sealed class FilterState
        {
            public FilterValueType ValueType { get; set; }
            public char StartChar { get; set; }
            public string Value { get; set; }
        }

        public static ViewState PreviousViewState { get; set; }

        public int SortColumn { get; set; }
        public int FirstVisibleTrack { get; set; }
        public int FirstVisibleFilterItem { get; set; }
        private Dictionary<FilterType, FilterState> filters { get; set; }
        public FilterType CurrentFilter { get; set; }
        public string TextFilter { get; set; }
        public ViewType ViewType { get; set; }

        public ViewState()
        {
            SortColumn = -1;
            FirstVisibleTrack = -1;
            FirstVisibleFilterItem = -1;
            CurrentFilter = FilterType.None;
            TextFilter = String.Empty;
            filters = new Dictionary<FilterType, FilterState>();
        }
        public void AddFilterInfo(FilterType FilterType, FilterButton Button)
        {
            FilterState fs = new FilterState();
            fs.Value = Button.FilterValue;
            fs.ValueType = Button.ValueType;
            fs.StartChar = Button.StartChar;
            filters.Add(FilterType, fs);
        }
        public void RestoreFilterButton(FilterButton Button)
        {
            FilterState fs = filters[Button.FilterType];
            Button.FilterValue = fs.Value;
            Button.ValueType = fs.ValueType;
            Button.StartChar = fs.StartChar;
        }
    }
}
