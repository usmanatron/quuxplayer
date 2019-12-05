/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal sealed class FilterBar : Control
    {
        public delegate void FilterSelectDelegate(FilterType Type);
        public delegate void FilterValueChangedDelegate(FilterType Type, string Value, bool WasOff, bool IsStartsWith);

        public event FilterSelectDelegate FilterReleased;
        public event FilterValueChangedDelegate FilterValueChanged;
        
        private const int MAX_SEARCH_STRINGS = 5;

        private FilterButton currentFilter = null;
        private Controller controller;
        private Rectangle releaseAllRectangle;
        private Rectangle clearTextFilterRectangle;
        private string previousText = String.Empty;
        private string[] searchStrings;
        private char[] splitArray;
        private bool txtFilterFocus = false;

        private QTextBox txtFilter;
        private FilterButton fltPlaylist;
        private FilterButton fltGenre;
        private FilterButton fltArtist;
        private FilterButton fltAlbum;
        private FilterButton fltGrouping;
        private FilterButton fltYear;

        private Dictionary<FilterType, FilterButton> filterButtons;

        private Func<TrackQueue, TrackQueue> ply;
        private Func<TrackQueue, TrackQueue> tex;
        private Func<TrackQueue, TrackQueue> gen;
        private Func<TrackQueue, TrackQueue> art;
        private Func<TrackQueue, TrackQueue> alb;
        private Func<TrackQueue, TrackQueue> yr;
        private Func<TrackQueue, TrackQueue> gpg;

        private Func<TrackQueue, TrackQueue>[] textArray;

        private ulong textTimer = Clock.NULL_ALARM;
        private bool locked;

        public bool AllowEvents { get; set; }

        private bool releaseAllHover = false;
        private bool clearTextHover = false;
        private HTPCMode viewMode = HTPCMode.Normal;

        private QToolTip ttFilterTextBox;
        private QToolTip ttClearFilter;

        private frmIndex indexForm = null;

        public FilterBar()
        {
            this.AllowEvents = false;
            this.DoubleBuffered = true;
            this.Height = 31;
            this.locked = false;

            this.splitArray = new char[] { ' ' };
            searchStrings = new string[MAX_SEARCH_STRINGS + 1];

            for (int i = 0; i < MAX_SEARCH_STRINGS + 1; i++)
                searchStrings[i] = String.Empty;

            setupControls();
            
            textArray = new Func<TrackQueue, TrackQueue>[MAX_SEARCH_STRINGS + 1];

            textArray[0] = t => t;
            
            textArray[1] = t => t.FindAll(ti => ti.FilterBy(searchStrings[0]));

            textArray[2] = t => t.FindAll(ti => ti.FilterBy(searchStrings[0]) &&
                                               ti.FilterBy(searchStrings[1]));

            textArray[3] = t => t.FindAll(ti => ti.FilterBy(searchStrings[0]) &&
                                               ti.FilterBy(searchStrings[1]) &&
                                               ti.FilterBy(searchStrings[2]));

            textArray[4] = t => t.FindAll(ti => ti.FilterBy(searchStrings[0]) &&
                                               ti.FilterBy(searchStrings[1]) &&
                                               ti.FilterBy(searchStrings[2]) &&
                                               ti.FilterBy(searchStrings[3]));

            textArray[5] = t => t.FindAll(ti => ti.FilterBy(searchStrings[0]) &&
                                               ti.FilterBy(searchStrings[1]) &&
                                               ti.FilterBy(searchStrings[2]) &&
                                               ti.FilterBy(searchStrings[3]) &&
                                               ti.FilterBy(searchStrings[4]));

            tex = textArray[0];

            ply = t => t;
            gen = t => t;
            art = t => t;
            alb = t => t;
            yr = t => t;
            gpg = t => t;

            ttFilterTextBox = new QToolTip(txtFilter, Localization.Get(UI_Key.ToolTip_Filter_Textbox));
            ttClearFilter = new QToolTip(this, String.Empty);

            fltPlaylist.Select();
        }
        public Controller Controller
        {
            set
            {
                controller = value;
                this.AllowEvents = true;
            }
        }

        public bool KeyPreview
        {
            get { return txtFilterFocus; }
        }
        public bool Locked
        {
            get { return locked; }
            set
            {
                if (this.locked != value)
                {
                    this.locked = value;
                    fltPlaylist.Locked = value;
                    fltArtist.Locked = value;
                    fltAlbum.Locked = value;
                    fltGenre.Locked = value;
                    fltYear.Locked = value;
                    fltGrouping.Locked = value;
                    txtFilter.Enabled = !value;
                }
            }
        }
        public FilterType CurrentFilterType
        {
            get
            {
                return currentFilter.FilterType;
            }
            set
            {
                filterButtons[value].Select();
            }
        }
        public string CurrentFilterName
        {
            get { return currentFilter.FilterName; }
            set
            {
                if (value == Localization.Get(UI_Key.Filter_Playlist))
                    CurrentFilterType = FilterType.Playlist;
                else if (value == Localization.Get(UI_Key.Filter_Genre))
                    CurrentFilterType = FilterType.Genre;
                else if (value == Localization.Get(UI_Key.Filter_Artist))
                    CurrentFilterType = FilterType.Artist;
                else if (value == Localization.Get(UI_Key.Filter_Album))
                    CurrentFilterType = FilterType.Album;
                else if (value == Localization.Get(UI_Key.Filter_Year))
                    CurrentFilterType = FilterType.Year;
                else if (value == Localization.Get(UI_Key.Filter_Grouping))
                    CurrentFilterType = FilterType.Grouping;
            }
        }
        public string CurrentFilterValue
        {
            get { return currentFilter.FilterValue; }
            set { currentFilter.FilterValue = value; }
        }
        public bool CurrentFilterIsActive
        {
            get { return currentFilter.ValueType == FilterValueType.SpecificValue; }
        }
        public string ActivePlaylist
        {
            get
            {
                if (fltPlaylist.ValueType == FilterValueType.SpecificValue)
                {
                    return fltPlaylist.FilterValue;
                }
                else
                {
                    return String.Empty;
                }
            }
        }
        public bool NowPlayingVisible
        {
            get
            {
                return fltPlaylist.ValueType == FilterValueType.SpecificValue &&
                       fltPlaylist.FilterValue == Localization.NOW_PLAYING;
            }
        }

        public TrackQueue GetTracks()
        {
            TrackQueue q;
            lock (Database.LibraryLock)
            {
                q = tex(gen(yr(art(alb(gpg(ply(Database.Library))))))); // ply must be innermost
            }
            if (fltPlaylist.ValueType == FilterValueType.SpecificValue)
                q.PlaylistBasis = fltPlaylist.FilterValue;
            
            return q;
        }
        public List<string> GetPlaylistFilterValues()
        {
            if (fltPlaylist.ValueType == FilterValueType.StartChar)
            {
                return (from pl in Database.GetPlaylists(GetTracksWithoutFiltering(FilterType.Playlist, false))
                   where pl.ToUpperInvariant()[0] == fltPlaylist.StartChar
                           select pl).ToList();
            }
            else
            {
                return Database.GetPlaylists(GetTracksWithoutFiltering(FilterType.Playlist, true));

            }}
        public TrackQueue GetTracksWithoutFiltering(FilterType Type, bool FilterStartsWith)
        {
            lock (Database.LibraryLock)
            {
                if (FilterStartsWith && filterButtons[Type].ValueType == FilterValueType.StartChar)
                    return tex(gen(yr(art(alb(gpg(ply(Database.Library)))))));

                switch (Type)
                {
                    case FilterType.Playlist:
                        return tex(gen(yr(art(alb(gpg(Database.Library))))));
                    case FilterType.Album:
                        return tex(ply(gen(yr(art(gpg(Database.Library))))));
                    case FilterType.Artist:
                        return tex(ply(gen(yr(alb(gpg(Database.Library))))));
                    case FilterType.Genre:
                        return tex(ply(yr(art(alb(gpg(Database.Library))))));
                    case FilterType.Year:
                        return tex(ply(gen(art(alb(gpg(Database.Library))))));
                    case FilterType.Grouping:
                        return tex(ply(gen(yr(art(alb(Database.Library))))));
                    case FilterType.Text:
                        return ply(gen(yr(art(alb(gpg(Database.Library))))));
                    default:
                        return Database.Library;
                }
            }
        }

        public void SetFilterValue(FilterType Type, string Value, bool ReleaseAllOtherFilters)
        {
            if (ReleaseAllOtherFilters)
                ReleaseAllFilters();

            filterButtons[Type].FilterValue = Value;
        }
        public bool IsFilterActive(FilterType Type)
        {
            return filterButtons[Type].ValueType == FilterValueType.SpecificValue;
        }
        public string GetFilterValue(FilterType Type)
        {
            return filterButtons[Type].FilterValue;
        }
        public void AdvanceFilter(bool Forward)
        {
            FilterButton toSelect = null;

            switch (currentFilter.FilterType)
            {
                case FilterType.Playlist:
                    toSelect = Forward ? fltGenre : fltGrouping;
                    break;
                case FilterType.Genre:
                    toSelect = Forward ? fltArtist : fltPlaylist;
                    break;
                case FilterType.Artist:
                    toSelect = Forward ? fltAlbum : fltGenre;
                    break;
                case FilterType.Album:
                    toSelect = Forward ? fltYear : fltArtist;
                    break;
                case FilterType.Year:
                    toSelect = Forward ? fltGrouping : fltAlbum;
                    break;
                default: // grouping
                    toSelect = Forward ? fltPlaylist : fltYear;
                    break;
            }
            toSelect.Select();
        }
        public void ReleaseCurrentFilter()
        {
            currentFilter.ReleaseFilter();
        }
        public void ReleaseFilter(FilterType Type)
        {
            filterButtons[Type].ReleaseFilter();
        }
        public void ReleaseAllFilters()
        {
            txtFilter.Text = String.Empty;

            foreach (KeyValuePair<FilterType, FilterButton> kvp in filterButtons)
                kvp.Value.ReleaseFilter();
        }
        public void FocusSearchBox()
        {
            txtFilter.Focus();
        }
        public HTPCMode HTPCMode
        {
            set
            {
                if (viewMode != value)
                {
                    viewMode = value;
                    Font f = (viewMode == HTPCMode.HTPC) ? Styles.FontButtonHTPC : Styles.FontBold;
                    foreach (KeyValuePair<FilterType, FilterButton> kvp in filterButtons)
                        kvp.Value.Font = f;
                    arrangeControls();
                }
            }
        }
        public void SaveViewState(ViewState VS)
        {
            foreach (KeyValuePair<FilterType, FilterButton> kvp in filterButtons)
                VS.AddFilterInfo(kvp.Key, kvp.Value);
            
            VS.TextFilter = txtFilter.Text;
            VS.CurrentFilter = this.CurrentFilterType;
        }
        public void RestoreViewState(ViewState VS)
        {
            foreach (KeyValuePair<FilterType, FilterButton> kvp in filterButtons)
                VS.RestoreFilterButton(kvp.Value);
            
            txtFilter.Text = VS.TextFilter;
            CurrentFilterType = VS.CurrentFilter;
        }
        public void ShowFilterIndex()
        {
            if (indexForm == null)
                showFilterIndex(currentFilter);
            else
                RemoveFilterIndex();
        }
        public void RemoveFilterIndex()
        {
            if (indexForm != null)
            {
                indexForm.Close();
                indexForm = null;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (Locked)
            {
                releaseAllHover = false;
                ClearTextHover = false;
            }
            else
            {
                ReleaseAllHover = releaseAllRectangle.Contains(e.Location);
                ClearTextHover = clearTextFilterRectangle.Contains(e.Location);
            }
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            ReleaseAllHover = false;
            ClearTextHover = false;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.DrawImageUnscaled(Styles.BitmapFilterBarBackground, Point.Empty);

            if (ReleaseAllHover)
                e.Graphics.DrawImageUnscaled(Styles.BitmapFilterBarXHighlighted, releaseAllRectangle.X - 1, 4);
            else
                e.Graphics.DrawImageUnscaled(Styles.BitmapFilterBarX, releaseAllRectangle.X - 1, 4);
                
            if (ClearTextHover)
                e.Graphics.DrawImageUnscaled(Styles.BitmapFilterBarXHighlighted, clearTextFilterRectangle.X - 1, 4);
            else
                e.Graphics.DrawImageUnscaled(Styles.BitmapFilterBarX, clearTextFilterRectangle.X - 1, 4);
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            RemoveFilterIndex();

            if (releaseAllRectangle.Contains(e.Location))
            {
                ReleaseAllFilters();
            }
            else if (clearTextFilterRectangle.Contains(e.Location))
            {
                txtFilter.Text = String.Empty;
                processTextChange();
            }
        }
       
        private bool ReleaseAllHover
        {
            get { return releaseAllHover; }
            set
            {
                if (releaseAllHover != value)
                {
                    releaseAllHover = value;
                    this.Invalidate();
                    if (value)
                    {
                        ttClearFilter.SetToolTip(this, Localization.Get(UI_Key.ToolTip_ReleaseFilters));
                        ttClearFilter.Active = true;
                    }
                    else
                    {
                        ttClearFilter.Active = false;
                    }
                }
            }
        }
        private bool ClearTextHover
        {
            get { return clearTextHover; }
            set
            {
                if (clearTextHover != value)
                {
                    clearTextHover = value;
                    this.Invalidate();
                    if (value)
                    {
                        ttClearFilter.SetToolTip(this, Localization.Get(UI_Key.ToolTip_ReleaseTextFilter));
                        ttClearFilter.Active = true;
                    }
                    else
                    {
                        ttClearFilter.Active = false;
                    }
                }
            }
        }

        private void filterSelected(FilterButton FilterButton)
        {
            RemoveFilterIndex();

            if (!FilterButton.Equals(currentFilter))
            {
                currentFilter = FilterButton;

                foreach (KeyValuePair<FilterType, FilterButton> kvp in filterButtons)
                    if (kvp.Key != FilterButton.FilterType)
                        kvp.Value.Selected = false;
                
                if (AllowEvents)
                    controller.RequestAction(QActionType.LoadFilterValues);

                arrangeControls();
            }
        }
        private void filterChanged(FilterButton FilterButton, bool WasOff)
        {
            RemoveFilterIndex();
           
            switch (FilterButton.ValueType)
            {
                case FilterValueType.StartChar:
                    FilterButton.ValueType = FilterValueType.StartChar;
                    switch (FilterButton.FilterType)
                    {
                        case FilterType.Playlist:
                            ply = t => Database.GetPlaylistTracks(FilterButton.StartChar);
                            break;
                        case FilterType.Genre:
                            gen = t => t.FindAll(ti => ti.Genre.Length > 0 && Char.ToUpperInvariant(ti.Genre[0]) == fltGenre.StartChar);
                            break;
                        case FilterType.Artist:
                            art = t => t.FindAll(ti => ti.MainGroupNoThe.Length > 0 && Char.ToUpperInvariant(ti.MainGroupNoThe[0]) == fltArtist.StartChar);
                            break;
                        case FilterType.Album:
                            alb = t => t.FindAll(ti => ti.Album.Length > 0 && Char.ToUpperInvariant(ti.Album[0]) == fltAlbum.StartChar);
                            break;
                        case FilterType.Year:
                            yr = t => t.FindAll(ti => ti.DecadeChar == fltYear.StartChar);
                            break;
                        case FilterType.Grouping:
                            gpg = t => t.FindAll(ti => ti.Grouping.Length > 0 && Char.ToUpperInvariant(ti.Grouping[0]) == fltGrouping.StartChar);
                            break;
                    }
                    if (AllowEvents)
                        FilterValueChanged.Invoke(FilterButton.FilterType, FilterButton.FilterValue, WasOff, true);
                    break;
                case FilterValueType.SpecificValue:
                    switch (FilterButton.FilterType)
                    {
                        case FilterType.Playlist:
                            ply = t => Database.GetPlaylistTracks(fltPlaylist.FilterValue);
                            break;
                        case FilterType.Genre:
                            gen = t => t.FindAll(ti => ti.Genre == fltGenre.FilterValue);
                            break;
                        case FilterType.Artist:
                            art = t => t.FindAll(ti => ti.MainGroup == fltArtist.FilterValue);
                            break;
                        case FilterType.Album:
                            alb = t => t.FindAll(ti => ti.Album == fltAlbum.FilterValue);
                            break;
                        case FilterType.Year:
                            yr = t => t.FindAll(ti => ti.YearString == fltYear.FilterValue);
                            break;
                        case FilterType.Grouping:
                            gpg = t => t.FindAll(ti => ti.Grouping == fltGrouping.FilterValue);
                            break;
                    }

                    if (AllowEvents)
                        FilterValueChanged.Invoke(FilterButton.FilterType, FilterButton.FilterValue, WasOff, false);
                    
                    break;
                case FilterValueType.None:
                    switch (FilterButton.FilterType)
                    {
                        case FilterType.Playlist:
                            ply = t => t;
                            break;
                        case FilterType.Genre:
                            gen = t => t;
                            break;
                        case FilterType.Artist:
                            art = t => t;
                            break;
                        case FilterType.Album:
                            alb = t => t;
                            break;
                        case FilterType.Year:
                            yr = t => t;
                            break;
                        case FilterType.Grouping:
                            gpg = t => t;
                            break;
                    }
                    
                    if (AllowEvents)
                        FilterReleased.Invoke(FilterButton.FilterType);
                    
                    break;
            }
            arrangeControls();
        }
        private void setupControls()
        {
            txtFilter = new QTextBox();
            txtFilter.EnableWatermark(this, Localization.Get(UI_Key.Filter_Search), String.Empty);

            fltPlaylist = new FilterButton(FilterType.Playlist);
            fltGenre = new FilterButton(FilterType.Genre);
            fltArtist = new FilterButton(FilterType.Artist);
            fltAlbum = new FilterButton(FilterType.Album);
            fltYear = new FilterButton(FilterType.Year);
            fltGrouping = new FilterButton(FilterType.Grouping);

            filterButtons = new Dictionary<FilterType, FilterButton>();

            filterButtons.Add(fltPlaylist.FilterType, fltPlaylist);
            filterButtons.Add(fltGenre.FilterType, fltGenre);
            filterButtons.Add(fltArtist.FilterType, fltArtist);
            filterButtons.Add(fltAlbum.FilterType, fltAlbum);
            filterButtons.Add(fltYear.FilterType, fltYear);
            filterButtons.Add(fltGrouping.FilterType, fltGrouping);

            txtFilter.Bounds = new Rectangle(8, 5, 135, 12);

            clearTextFilterRectangle = new Rectangle(150, 0, 15, this.ClientRectangle.Height);

            fltPlaylist.Left = 175;

            fltPlaylist.FilterName = Localization.Get(UI_Key.Filter_Playlist);
            fltGenre.FilterName = Localization.Get(UI_Key.Filter_Genre);
            fltArtist.FilterName = Localization.Get(UI_Key.Filter_Artist);
            fltAlbum.FilterName = Localization.Get(UI_Key.Filter_Album);
            fltYear.FilterName = Localization.Get(UI_Key.Filter_Year);
            fltGrouping.FilterName = Localization.Get(UI_Key.Filter_Grouping);

            foreach (KeyValuePair<FilterType, FilterButton> kvp in filterButtons)
            {
                kvp.Value.SelectedEvent += filterSelected;
                kvp.Value.FilterValueChanged += filterChanged;
                kvp.Value.IndexSelected += showFilterIndex;
                kvp.Value.ClickedWithoutSelected += (s) => { RemoveFilterIndex(); };
                kvp.Value.ReleaseFilter();
            }

            txtFilter.Enter += (s, e) => { txtFilterFocus = true; controller.RequestAction(QActionType.KeyPreviewChange); };
            txtFilter.Leave += (s, e) => { txtFilterFocus = false; controller.RequestAction(QActionType.KeyPreviewChange); };
            txtFilter.TextChanged += new EventHandler(txtFilter_TextChanged);

            this.Controls.AddRange(new Control[] { txtFilter, fltPlaylist, fltGenre, fltArtist, fltAlbum, fltYear, fltGrouping });
        }
        private void showFilterIndex(FilterButton Button)
        {
            bool show = indexForm == null || indexForm.FilterTypeBasis != Button.FilterType;

            if (indexForm != null)
                RemoveFilterIndex();

            if (show)
            {
                CurrentFilterType = Button.FilterType;

                indexForm = new frmIndex(this,
                                         new Point(Button.Left + Button.Width / 2, Button.Bottom),
                                         showFilterIndexCallback,
                                         Button);
            }
        }
        private void showFilterIndexCallback(frmIndex fi)
        {
            if (fi.NoData)
            {
                Controller.ShowMessage(Localization.Get(UI_Key.Message_No_Index_Items_Available));
            }
            else
            {
                if (fi.Char != '\0')
                {
                    bool wasOff = fi.Button.ValueType == FilterValueType.None;
                    if (fi.Char == frmIndex.CLEAR_CHAR)
                    {
                        fi.Button.ValueType = FilterValueType.None;
                    }
                    else
                    {
                        fi.Button.StartChar = fi.Char;
                        fi.Button.ValueType = FilterValueType.StartChar;
                    }
                    filterChanged(fi.Button, wasOff);
                    fi.Button.Invalidate();
                }
                RemoveFilterIndex();
            }
        }
        private void arrangeControls()
        {
            foreach (KeyValuePair<FilterType, FilterButton> kvp in filterButtons)
                kvp.Value.SetWidth();

            fltGenre.Left = fltPlaylist.Right + 20;
            fltArtist.Left = fltGenre.Right + 20;
            fltAlbum.Left = fltArtist.Right + 20;
            fltYear.Left = fltAlbum.Right + 20;
            fltGrouping.Left = fltYear.Right + 20;

            releaseAllRectangle = new Rectangle(fltGrouping.Right + 5, 0, 15, this.ClientRectangle.Height);

            this.Invalidate();
        }
        private void txtFilter_TextChanged(object sender, EventArgs e)
        {
            Clock.Update(ref textTimer, processTextChange, 140, false);
        }
        private void processTextChange()
        {
            textTimer = Clock.NULL_ALARM;

            bool wasOff = txtFilter.Text.Length == 0;

            string[] ss = txtFilter.Text.ToLowerInvariant().Split(splitArray, MAX_SEARCH_STRINGS, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < ss.Length; i++)
                searchStrings[i] = ss[i];

            tex = textArray[ss.Length];

            if (searchStrings.Length > 0)
            {
                if (AllowEvents)
                    FilterValueChanged.Invoke(FilterType.Text, txtFilter.Text, wasOff, false);
            }
            else
            {
                if (AllowEvents)
                    FilterReleased.Invoke(FilterType.Text);
            }
        }
    }
}
