/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Automation.Provider;
using System.Windows.Automation;
using System.Security.Permissions;

namespace QuuxPlayer
{
    internal enum ColumnID { Title, Album, Artist, AlbumArtist, Composer, TrackNum, DiskNum, Genre, Grouping, Length, BitRate, Size, Year, FileDate, DaysSinceLastPlayed, DaysSinceAdded, PlayCount, Rating, Encoder, FileType, NumChannels, SampleRate, FileName, Equalizer, ReplayGain }
    
    public sealed class TrackList : Control
    {
        public int SortColumn { get; set; }

        private Controller controller;
        private const int SCROLL_BAR_WIDTH = 14;
        private int numVisibleTracks;
        private int firstVisibleTrack = 0;
        private int scrollBuffer = 0;
        private int trackHeight;
        private bool active = false;
        private Font font;
        private Font playingFont;
        private Font headingFont;
        private List<ColInfo> columns;
        private List<ColInfo> normalColumns;
        private List<ColInfo> htpcColumns;
        private static List<ColInfo> columnDefinitions;
        private List<Rectangle> rowRectangles = new List<Rectangle>();
        private HTPCMode view = HTPCMode.Normal;

        private static TextFormatFlags tff =       TextFormatFlags.NoPrefix | TextFormatFlags.Left |             TextFormatFlags.EndEllipsis;
        private static TextFormatFlags tffRight =  TextFormatFlags.NoPrefix | TextFormatFlags.Right |            TextFormatFlags.EndEllipsis;
        private static TextFormatFlags tffCenter = TextFormatFlags.NoPrefix | TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis;

        private static TrackList instance;

        private TrackQueue queue;
        private Track playingTrack = null;
        private Track lastSelectedTrack = null;
        private int lastSelectedIndex = -1;
        private bool revSort = false;
        private Rectangle dragRect = Rectangle.Empty;
        
        private int dragIndex = -1;
        private int minDragIndex;
        private int maxDragIndex;

        private int keyboardIndex = -1;
        private int minKeyboardIndex;
        private int maxKeyboardIndex;
        
        private int dragCol = -1;
        private int dragHoverCol = -1;
        private int dragLocation = -1;
        private int hoverIndex = -1;
        private bool draggingColumns = false;
        private bool sizingColumns = false;
        private bool autoSizingColumns = false;
        private int sizingColumnXPosn = -1;
        private bool sizingColumnsConfirmed = false;
        private bool canDrag = false;
        private bool swiping = false;
        private bool doubleClicking = false;
        private int columnHeaderHover = -1;
        private Point mouseClickPoint = Point.Empty;

        private string toolTipText = String.Empty;
        private Point toolTipOriginalLocation = Point.Empty;
        private Font toolTipFont;
        private Rectangle toolTipRect = Rectangle.Empty;
        
        private bool initialized = false;
        private QScrollBar qScrollBar;

        private NormalView normal;

        internal TrackList(NormalView Normal)
        {
            this.normal = Normal;
            instance = this;

            this.DoubleBuffered = true;
            this.AllowDrop = true;

            this.SortColumn = 0;

            queue = TrackQueue.Empty;
            
            qScrollBar = new QScrollBar(true);
            qScrollBar.Top = 0;
            qScrollBar.Width = SCROLL_BAR_WIDTH;
            qScrollBar.UserScroll += new QScrollBar.ScrollDelegate(scroll);
            qScrollBar.ScrollSkip += new QScrollBar.ScrollSkipDelegate(qScrollBar_ScrollSkip);
            this.Controls.Add(qScrollBar);

            setupColumns();
            columns = normalColumns;

            this.HTPCMode = HTPCMode.Normal;
            toolTipFont = Styles.Font;

            initialized = true;
        }

        private void qScrollBar_ScrollSkip(bool Up)
        {
            if (SortColumn >= 0)
            {
                ColInfo ci = columns[SortColumn];
                lastSelectedIndex = -1;

                int fvi = FirstVisibleTrack;
                if (Up)
                    fvi--;
                if ((Up || queue.Count > fvi) && (!Up || fvi > 0))
                {
                    char s = Lib.FirstCharNoTheUpper(ci.Data(queue[fvi]));
                    char s2 = s;
                    Func<Track, char> getChar = (ci.SuppressThe ? (new Func<Track, char>(t => Lib.FirstCharNoTheUpper(ci.Data(t)))) : (new Func<Track, char>(t => Lib.FirstCharUpper(ci.Data(t)))));
                    if (Up)
                    {

                        do
                        {
                            fvi--;
                            s = s2;
                        }
                        while (fvi > 1 && s == (s2 = getChar(queue[fvi - 1])));

                        FirstVisibleTrack = Math.Min(fvi, FirstVisibleTrack - 1);
                    }
                    else
                    {
                        char min = 'A';
                        int max = queue.Count - numVisibleTracks + 1;
                        do
                        {
                            fvi++;
                            s = s2;
                        }
                        while (fvi < max && ((s == (s2 = Lib.FirstCharNoTheUpper(ci.Data(queue[fvi + 1])))) || (s2 < min)));

                        FirstVisibleTrack = Math.Max(fvi + 1, FirstVisibleTrack - 1);
                    }
                    normal.ShowLetter(Lib.FirstCharNoTheUpper(ci.Data(queue[FirstVisibleTrack])));
                }
            }
            else
            {
                Controller.ShowMessage(Localization.Get(UI_Key.Track_List_No_Sort));
            }
        }
        public bool Locked { get; set; }

        internal Controller Controller
        {
            set { controller = value; }
        }
        internal Track this[int Index]
        {
            get { return queue[Index]; }
        }
        internal Track PlayingTrack
        {
            set
            {
                if (playingTrack != value)
                {
                    playingTrack = value;

                    if (queue.Contains(value))
                    {
                        queue.CurrentIndex = queue.IndexOf(value);
                    }

                    this.Invalidate();
                }
            }
            private get { return playingTrack; }
        }
        internal List<Track> SelectedTracks
        {
            get
            {
                // other methods assume this is a fresh list, not cached:
                return queue.FindAll(t => t.Selected);
            }
        }
        internal Track CurrentTrack
        {
            get { return queue.CurrentTrack; }
        }
        internal int CurrentIndex
        {
            get { return queue.CurrentIndex; }
            set
            {
                queue.CurrentIndex = value;
                this.Invalidate();
            }
        }
        internal int ItemIndexFromPoint(Point P)
        {
            return getTrackIndex(P.Y, false);
        }
        internal void SelectTrack(int Index, bool MakeCurrent, bool ClearOthers, bool WithBuffer)
        {
            if (Index >= 0 && Index < queue.Count)
            {
                if (MakeCurrent)
                {
                    queue.CurrentIndex = Index;
                }

                if (ClearOthers)
                {
                    queue.ClearSelectedItems();                
                }

                lastSelectedTrack = queue[Index];
                lastSelectedIndex = Index;

                queue[Index].Selected = true;

                ensureVisible(Index, WithBuffer);

                this.Invalidate();
                controller.RequestAction(QActionType.UpdateTrackCount);
            }
        }
        internal Track FirstSelectedTrack
        {
            get
            {
                try
                {
                    return queue.FirstOrDefault(t => t.Selected);
                }
                catch
                {
                    return null;
                }
            }
        }
        internal Track FirstSelectedOrFirst
        {
            get
            {
                if (this.HasSelectedTracks)
                    return this.SelectedTracks[0];
                else if (this.Count > 0)
                    return this[0];
                else
                    return null;
            }
        }
        public int FirstSelectedIndex
        {
            get { return queue.IndexOf(FirstSelectedTrack); }
        }
        internal Track LastSelectedTrack
        {
            get { return queue.LastOrDefault(t => t.Selected); }
        }
        internal Rectangle GetBoundingRectangle(Track Track)
        {
            if (queue.Contains(Track))
            {
                int i = queue.IndexOf(Track);
                if (i < FirstVisibleTrack)
                    return Rectangle.Empty;
                else if (i > FirstVisibleTrack + numVisibleTracks - 1)
                    return Rectangle.Empty;
                else
                    return rowRectangles[i - firstVisibleTrack];
            }
            else
            {
                return Rectangle.Empty;
            }
        }
        public int LastSelectedIndex
        {
            get { return queue.IndexOf(LastSelectedTrack); }
        }
        public int Count
        {
            get { return queue.Count; }
        }
        public bool Active
        {
            get { return active; }
            set
            {
                if (active != value)
                {
                    active = value;
                    if (active && !Locked)
                        this.Focus();
                    this.Invalidate();
                }
            }
        }
        public bool HasTracks
        {
            get { return queue.Count > 0; }
        }
        public bool HasSelectedTracks
        {
            get { return queue.HasSelectedTracks; }
        }
        internal Track Peek(bool Loop)
        {
            return queue.Peek(Loop);
        }

        internal TrackQueue Queue
        {
            get { return queue; }
            set
            {
                if (queue.Reordered && queue.PlaylistBasis.Length > 0 && !Database.IsPlaylistDynamic(queue.PlaylistBasis))
                {
                    controller.RequestAction(QActionType.SavePlaylist);
                }

                queue = value;
                qScrollBar.Value = 0;
                qScrollBar.Max = Math.Max(0, queue.Count - numVisibleTracks);

                if (PlayingTrack != null && queue.Contains(PlayingTrack))
                {
                    queue.CurrentIndex = queue.IndexOf(PlayingTrack);
                }
                else
                {
                    queue.CurrentIndex = -1;
                }
                ClearHoverIndex();
                sort();
                firstVisibleTrack = 0;
                MakeAnInterestingTrackVisible(-1);
            }      
        }
        internal void RemoveTrack(Track Track)
        {
            if (queue.Contains(Track))
            {
                if (queue.CurrentIndex == queue.IndexOf(Track))
                    queue.CurrentIndex = -1;

                queue.Remove(Track);
                qScrollBar.Max = Math.Max(0, queue.Count - numVisibleTracks);
            }
        }
        public void SelectAll()
        {
            try
            {
                foreach (Track t in queue.ToList())
                    t.Selected = true;

                lastSelectedIndex = queue.Count - 1;
                lastSelectedTrack = queue[lastSelectedIndex];
            }
            catch { }
            this.Invalidate();
        }
        public void SelectNone()
        {
            try
            {
                foreach (Track t in queue.ToList())
                    t.Selected = false;

                lastSelectedIndex = -1;
                lastSelectedTrack = null;
            }
            catch { }
            this.Invalidate();
        }
        public void InvertSelection()
        {
            try
            {
                foreach (Track t in queue.ToList())
                    t.Selected = !t.Selected;

                lastSelectedIndex = -1;
                lastSelectedTrack = null;
            }
            catch { }
            this.Invalidate();
        }
        public void ClearHoverIndex()
        {
            hoverIndex = -1;
        }
        
        public void MakeAnInterestingTrackVisible(int InterestingIndex)
        {
            if (InterestingIndex < 0)
            {
                if ((lastSelectedTrack != null) && queue.Contains(lastSelectedTrack))
                {
                    InterestingIndex = queue.IndexOf(lastSelectedTrack);
                }
                else
                {
                    TrackQueue tq = this.SelectedTracks;
                    if (tq.Count == 0)
                    {
                        if (queue.CurrentIndex >= 0)
                            InterestingIndex = queue.CurrentIndex;
                        else
                            InterestingIndex = 0;
                    }
                    else
                    {
                        InterestingIndex = queue.IndexOf(tq[0]);
                    }
                }
            }

            FirstVisibleTrack = InterestingIndex - (numVisibleTracks / 2);

            this.Invalidate();
        }
        public bool PageDown()
        {
            return this.MoveDown(numVisibleTracks, Keyboard.Shift);
        }
        public bool PageUp()
        {
            return this.MoveUp(numVisibleTracks, Keyboard.Shift);
        }
        public bool Home()
        {
            if (queue.Count > 0)
            {
                SelectTrack(0, false, false, false);
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool End()
        {
            if (queue.Count > 0)
            {
                int index = queue.Count - 1;
                SelectTrack(index, false, false, false);
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool MoveDown(int Num, bool Shift)
        {
            try
            {
                if (HasSelectedTracks)
                {
                    int i = (lastSelectedIndex >= 0 ? lastSelectedIndex : LastSelectedIndex);
                    int index = Math.Min(queue.Count - 1, i + Num);
                    lastSelectedIndex = index;
                    SelectTrack(index, false, !Shift, true);

                    if (Shift && keyboardIndex >= 0)
                    {
                        for (int j = keyboardIndex; j <= index; j++)
                            queue[j].Selected = true;

                        if (index <= keyboardIndex)
                        {
                            for (int j = minKeyboardIndex; j < index; j++)
                                queue[j].Selected = false;
                        }
                        if (index == keyboardIndex)
                            setKeyboardIndex(index);
                        else
                            maxKeyboardIndex = Math.Max(index, maxKeyboardIndex);

                        this.Invalidate();
                    }
                    else
                    {
                        setKeyboardIndex(index);
                    }

                    return true;
                }
                else if (queue.Contains(PlayingTrack))
                {
                    int i = queue.IndexOf(PlayingTrack);
                    SelectTrack(i, false, false, false);
                    setKeyboardIndex(i);
                    return true;
                }
                else if (hoverIndex >= 0)
                {
                    SelectTrack(hoverIndex, true, false, false);
                    setKeyboardIndex(hoverIndex);
                    return true;
                }
                else if (queue.Count > 0)
                {
                    SelectTrack(0, false, false, false);
                    setKeyboardIndex(0);
                    return true;
                }
                else
                {
                    setKeyboardIndex(-1);
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
        public bool MoveUp(int Num, bool Shift)
        {
            try
            {
                if (HasSelectedTracks)
                {
                    int i = (lastSelectedIndex >= 0 ? lastSelectedIndex : LastSelectedIndex);
                    int index = Math.Max(0, i - Num);
                    lastSelectedIndex = index;
                    SelectTrack(index, false, !Shift, true);

                    if (Shift && keyboardIndex >= 0)
                    {
                        for (int j = index; j <= keyboardIndex; j++)
                            queue[j].Selected = true;

                        if (index >= keyboardIndex)
                        {
                            for (int ii = index + 1; ii <= maxKeyboardIndex; ii++)
                                queue[ii].Selected = false;
                        }
                        if (index == keyboardIndex)
                            setKeyboardIndex(index);
                        else
                            minKeyboardIndex = Math.Min(index, minKeyboardIndex);

                        this.Invalidate();
                    }
                    else
                    {
                        setKeyboardIndex(index);
                    }

                    return true;
                }
                else
                {
                    if (queue.Contains(PlayingTrack))
                    {
                        int i = queue.IndexOf(PlayingTrack);
                        SelectTrack(i, false, false, false);
                        setKeyboardIndex(i);
                        return true;
                    }
                    else if (hoverIndex >= 0)
                    {
                        SelectTrack(hoverIndex, true, false, false);
                        setKeyboardIndex(hoverIndex);
                        return true;
                    }
                    else if (queue.Count > 0)
                    {
                        SelectTrack(queue.Count - 1, false, false, false);
                        setKeyboardIndex(queue.Count - 1);
                        return true;
                    }
                    else
                    {
                        setKeyboardIndex(-1);
                        return false;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
        private void setKeyboardIndex(int Index)
        {
            keyboardIndex = Index;
            minKeyboardIndex = Index;
            maxKeyboardIndex = Index;
        }
        public void MoveTracksUp(int Num)
        {
            List<int> selIndexes = new List<int>();

            for (int i = 0; i < queue.Count; i++)
            {
                if (queue[i].Selected)
                    selIndexes.Add(i);
            }
            if (selIndexes.Count > 0)
            {
                int firstSwapIndex = -1;

                int bestAvail = 0;

                while (queue[bestAvail].Selected && bestAvail < queue.Count - 1)
                    bestAvail++;

                for (int i = 0; i <= selIndexes.Count - 1 && bestAvail < queue.Count - 1; i++)
                {
                    if (selIndexes[i] - Num <= bestAvail)
                    {
                        if (selIndexes[i] > bestAvail)
                        {
                            if (firstSwapIndex < 0)
                                firstSwapIndex = bestAvail;

                            queue.Swap(selIndexes[i], bestAvail++);

                            while (queue[bestAvail].Selected && bestAvail < queue.Count - 1)
                                bestAvail++;
                        }
                    }
                    else
                    {
                        if (firstSwapIndex < 0)
                            firstSwapIndex = selIndexes[i] - Num;

                        queue.Swap(selIndexes[i], selIndexes[i] - Num);
                    }
                }
                if (firstSwapIndex > 0)
                    ensureVisible(firstSwapIndex, false);
            }
            this.Invalidate();
        }
        public void MoveTracksDown(int Num)
        {
            List<int> selIndexes = new List<int>();

            for (int i = 0; i < queue.Count; i++)
            {
                if (queue[i].Selected)
                    selIndexes.Add(i);
            }
            if (selIndexes.Count > 0)
            {
                int firstSwapIndex = -1;

                int bestAvail = queue.Count - 1;
                while (queue[bestAvail].Selected && bestAvail > 0)
                    bestAvail--;

                for (int i = selIndexes.Count - 1; i >= 0 && bestAvail > 0; i--)
                {
                    if (selIndexes[i] + Num >= bestAvail)
                    {
                        if (selIndexes[i] < bestAvail)
                        {
                            if (firstSwapIndex < 0)
                                firstSwapIndex = bestAvail;

                            queue.Swap(selIndexes[i], bestAvail--);

                            while (queue[bestAvail].Selected && bestAvail > 0)
                                bestAvail--;
                        }
                    }
                    else
                    {
                        if (firstSwapIndex < 0)
                            firstSwapIndex = selIndexes[i] + Num;

                        queue.Swap(selIndexes[i], selIndexes[i] + Num);
                    }
                }
                if (firstSwapIndex > 0)
                    ensureVisible(firstSwapIndex, false);
            }
            this.Invalidate();
        }
        public void Shuffle()
        {
            queue.Shuffle(PlayingTrack, false);

            if (queue.Contains(PlayingTrack))
                queue.CurrentIndex = 0;
            else
                queue.CurrentIndex = -1;

            FirstVisibleTrack = 0;
            SortColumn = -1;
            queue.ClearSelectedItems();
            this.Invalidate();
        }

        internal Track Advance(bool Loop)
        {
            return queue.Advance(Loop);
        }
        internal void MakeVisible(Track TI, bool Select)
        {
            int i = queue.IndexOf(TI);
            if (i >= 0)
            {
                if (Select)
                {
                    queue.ClearSelectedItems();
                    SelectTrack(i, false, true, true);
                }
                MakeAnInterestingTrackVisible(i);
            }
        }
        public void NoSort()
        {
            SortColumn = -1;
        }
        public void SortByArtist()
        {
            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i].ID == ColumnID.Artist || columns[i].ID == ColumnID.AlbumArtist)
                {
                    SortColumn = i;
                    sort();
                    break;
                }
            }
        }
        public void AdvanceSortColumn()
        {
            if (SortColumn < 0)
            {
                SortColumn = 0;
            }
            else
            {
                SortColumn++;
            }
            if (SortColumn >= columns.Count)
                SortColumn = 0;

            revSort = false;

            sort();

            MakeAnInterestingTrackVisible(-1);

            this.Invalidate();
        }
        public void ResetColumns(bool WidthOnly)
        {
            ColInfo.ResetColumnWidths(columnDefinitions);

            if (!WidthOnly)
            {
                setupColumns();
                columns = (view == HTPCMode.Normal) ? normalColumns : htpcColumns;
            }

            setupCells();

            this.Invalidate();
        }
        private void setupColumns()
        {
            normalColumns = columnDefinitions.Where(ci => ci.ID == ColumnID.Artist || ci.ID == ColumnID.Album || ci.ID == ColumnID.Title || ci.ID == ColumnID.Genre || ci.ID == ColumnID.TrackNum || ci.ID == ColumnID.Length || ci.ID == ColumnID.Rating).ToList();
            htpcColumns = columnDefinitions.Where(ci => ci.ID == ColumnID.Artist || ci.ID == ColumnID.Album || ci.ID == ColumnID.Title || ci.ID == ColumnID.TrackNum).ToList();
        }
        public void EnsurePlayingTrackVisible()
        {
            if (playingTrack != null)
            {
                if (queue.Contains(playingTrack))
                {
                    ensureVisible(queue.IndexOf(playingTrack), true);
                }
            }
        }
        internal void EnsureVisible(Track Track)
        {
            int index = queue.IndexOf(Track);
            if (index >= 0)
                ensureVisible(index, false);
        }
        public char CurrentLetterIndex
        {
            get
            {
                if (SortColumn < 0)
                    return '\0';

                Track t = this.FirstSelectedTrack;

                if (t == null)
                    return '\0';

                string ret;

                switch (columns[SortColumn].ID)
                {
                    case ColumnID.Album:
                        ret = t.Album;
                        break;
                    case ColumnID.Artist:
                        ret = t.ArtistNoThe;
                        break;
                    case ColumnID.AlbumArtist:
                        ret = t.AlbumArtistNoThe;
                        break;
                    case ColumnID.Title:
                        ret = t.Title;
                        break;
                    case ColumnID.Grouping:
                        ret = t.Grouping;
                        break;
                    case ColumnID.Genre:
                        ret = t.Genre;
                        break;
                    case ColumnID.Composer:
                        ret = t.Composer;
                        break;
                    default:
                        return '\0';
                }
                return Lib.FirstCharNoTheUpper(ret);
            }
        }

        internal HTPCMode HTPCMode
        {
            get { return view; }
            set
            {
                if (!initialized || value != view)
                {
                    ColInfo ci;
                    
                    if (SortColumn < 0)
                        ci = null;
                    else
                        ci = columns[SortColumn];

                    view = value;
                    if (value == HTPCMode.Normal)
                    {
                        columns = normalColumns;
                        trackHeight = Styles.TextHeight;
                        font = Styles.Font;
                        playingFont = Styles.FontItalic;
                        headingFont = Styles.FontBold;
                    }
                    else
                    {
                        columns = htpcColumns;
                        trackHeight = Styles.TextHeightHTPC;
                        font = Styles.FontHTPC;
                        playingFont = Styles.FontItalicHTPC;
                        headingFont = Styles.FontBoldHTPC;
                    }
                    if (ci != null)
                    {
                        if (columns.Exists(c => c.ID == ci.ID))
                            SortColumn = columns.FindIndex(c => c.ID == ci.ID);
                        else
                            SortColumn = 0;
                    }
                    setNumVisibleTracks();
                    setupCells();
        
                    headerBrush = Style.GetHeaderRowBrush(trackHeight, 0);
                    selectedRowBrush = Style.GetSelectedRowBrush(trackHeight, 0);
                    selectedRowHoverBrush = Style.GetSelectedHoverRowBrush(trackHeight, 0);
                    hoverBrush = Style.GetHoverRowBrush(trackHeight, 0);
        
                    this.Invalidate();
                }
            }
        }
        internal List<ColumnStatusItem> GetColumnStatus()
        {
            List<ColumnStatusItem> csi = new List<ColumnStatusItem>();
            foreach (ColInfo ci in columnDefinitions)
            {
                csi.Add(new ColumnStatusItem(ci.ID, ci.LongName, columns.Contains(ci)));
            }
            return csi;
        }
        internal string SerializeColumnStatus(HTPCMode Mode)
        {
            StringBuilder sb = new StringBuilder();
            List<ColInfo> cols = (Mode == HTPCMode.Normal) ? normalColumns : htpcColumns;
            foreach (ColInfo ci in cols)
            {
                sb.Append(ci.Name + ";" + ci.RelativeWidth.ToString() + ";");
            }
            return sb.ToString();
        }
        internal void SetColumnStatus(string Serialization, HTPCMode Mode)
        {
            List<ColInfo> cols = (Mode == HTPCMode.Normal) ? normalColumns : htpcColumns;
            if (Serialization.Trim().Length == 0)
            {
                ResetColumns(false);
            }
            else
            {
                string[] status = Serialization.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                cols.Clear();

                for (int i = 0; i < status.Length; i += 2)
                {
                    ColInfo ci = columnDefinitions.FirstOrDefault(c => c.Name == status[i]);
                    if (ci != null)
                    {
                        ci.RelativeWidth = Int32.Parse(status[i + 1]);
                        cols.Add(ci);
                    }
                }

                if (cols.Count == 0)
                {
                    ResetColumns(false);
                }
                else if (view == Mode)
                {
                    setupCells();
                    this.Invalidate();
                }
            }
        }
        internal void ToggleColumnVisibility(ColumnStatusItem CSI)
        {
            ColInfo ci = columnDefinitions.First(c => c.ID == CSI.ID);

            if (columns.Contains(ci))
            {
                columns.Remove(ci);
            }
            else
            {
                int i = 0;
                while (i < columns.Count && columns[i].HeaderEdgeLeft < mouseClickPoint.X)
                    i++;

                if (i >= columns.Count)
                    columns.Add(ci);
                else
                    columns.Insert(i, ci);
            }

            if (columns.Count == 0)
                columns.Add(columnDefinitions[0]);

            ResetColumns(true);
        }
        internal void SaveViewState(ViewState VS)
        {
            VS.FirstVisibleTrack = FirstVisibleTrack;
            VS.SortColumn = SortColumn;
        }
        internal void RestoreViewState(ViewState VS)
        {
            FirstVisibleTrack = VS.FirstVisibleTrack;
            SortColumn = VS.SortColumn;
            sort();
            this.Invalidate();
        }
        public override string ToString()
        {
            return queue.ToString();
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (!Locked)
            {
                doubleClicking = true;

                if (e.Y < trackHeight)  // header
                {
                    int col = -1;

                    for (int i = 0; i < columns.Count; i++)
                    {
                        if ((columns[i].HeaderEdgeLeft <= e.X) && (e.X <= columns[i].HeaderEdgeRight))
                        {
                            col = i;
                            break;
                        }
                    }

                    ColInfo.AutoSizeColumn(columns, col, queue, firstVisibleTrack, firstVisibleTrack + numVisibleTracks, headingFont, font);
                    sizingColumns = false;
                    autoSizingColumns = true;
                }
                else
                {
                    int clickIndex = getTrackIndex(e.Location.Y, false);

                    if (clickIndex >= 0)
                    {
                        SelectTrack(clickIndex, true, true, false);
                        controller.RequestAction(QActionType.PlaySelectedTracks);
                    }
                }
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            columnHeaderHover = -1;
            if (sizingColumns)
            {
                sizingColumnXPosn = Math.Max(columns[dragCol].Left + 10, Math.Min(this.ClientRectangle.Width, e.X));
                sizingColumnsConfirmed = true;
                this.Invalidate();
            }
            else if (draggingColumns)
            {
                ColInfo ci = columns.Find(c => (c.Left < e.X && c.Right > e.X));
                if (ci != null)
                {
                    int col = columns.IndexOf(ci);
                    if (dragHoverCol != col)
                    {
                        dragHoverCol = columns.IndexOf(ci);
                        this.Invalidate();
                    }
                }
            }
            else if (swiping)
            {
                int onTrackIndex = getTrackIndex(e.Y + trackHeight / 2, true);
                if (onTrackIndex > dragIndex)
                {
                    for (int i = minDragIndex; i <= Math.Min(queue.Count - 1, maxDragIndex); i++)
                        queue[i].Selected = (i <= onTrackIndex && i >= dragIndex);
                }
                else if (onTrackIndex < dragIndex)
                {
                    for (int i = minDragIndex; i <= Math.Min(queue.Count - 1, maxDragIndex); i++)
                        queue[i].Selected = (i >= onTrackIndex && i <= dragIndex);
                }
                else
                {
                    for (int i = minDragIndex; i <= Math.Min(queue.Count - 1, maxDragIndex); i++)
                        queue[i].Selected = (i == dragIndex);

                    minDragIndex = dragIndex;
                    maxDragIndex = dragIndex;
                }
                controller.RequestAction(QActionType.UpdateTrackCount);
                this.Invalidate();
                maxDragIndex = Math.Max(onTrackIndex, maxDragIndex);
                minDragIndex = Math.Min(onTrackIndex, minDragIndex);
            }
            else if (dragCol >= 0 && !dragRect.Contains(e.Location))
            {
                draggingColumns = true;
                OnMouseMove(e);
                return;
            }
            else if (dragIndex >= 0)
            {
                if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
                {
                    if (!dragRect.Contains(e.Location))
                    {
                        if (canDrag)
                        {
                            var v = from t in this.SelectedTracks
                                    select t.FilePath;

                            DoDragDrop(new DataObject(DataFormats.FileDrop, v.ToArray()), DragDropEffects.Copy);
                        }
                        else
                        {
                            queue.ClearSelectedItems();
                            swiping = true;
                            minDragIndex = dragIndex;
                            maxDragIndex = dragIndex;
                        }
                    }
                }
            }
            else if (e.Y < trackHeight) // on header
            {
                bool set = false;
                if (!Locked)
                {
                    for (int i = 0; i < columns.Count; i++)
                    {
                        if ((columns[i].HeaderEdgeLeft <= e.X) && (e.X <= columns[i].HeaderEdgeRight))
                        {
                            this.Cursor = Cursors.VSplit;
                            set = true;
                            break;
                        }
                        else if (columnHeaderHover < 0 && columns[i].HeaderEdgeRight > e.X)
                        {
                            columnHeaderHover = i;
                            this.Invalidate();
                        }
                    }
                }
                if (!set)
                {
                    this.Cursor = Locked ? Cursors.Default : Cursors.Hand;
                    setToolTip();
                }
                setHoverIndex(-1, false);
            }
            else
            {
                this.Cursor = Cursors.Default;
                if (!Locked)
                    setHoverIndex(e.Y, false);
                setToolTip();
            }
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            mouseClickPoint = e.Location;

            if (!Locked)
            {
                if (e.Y < trackHeight) // column header
                {
                    ColInfo ci = columns.Find(c => ((c.HeaderEdgeLeft <= e.X) && (e.X <= c.HeaderEdgeRight)));

                    if (ci != null)
                    {
                        dragCol = columns.IndexOf(ci);
                        sizingColumns = true;
                        sizingColumnsConfirmed = false;
                        this.Cursor = Cursors.VSplit;
                        sizingColumnXPosn = ci.Right;
                    }
                    else
                    {
                        Size dragSize = SystemInformation.DragSize;

                        dragRect = new Rectangle(new Point(e.X - (dragSize.Width / 2),
                                                           e.Y - (dragSize.Height / 2)),
                                                 dragSize);

                        ci = columns.Find(c => (c.Left < e.X && c.Right > e.X));
                        if (ci != null)
                        {
                            dragCol = columns.IndexOf(ci);
                            this.Cursor = Cursors.Hand;
                        }
                    }
                }
                else
                {
                    int trackIndex = getTrackIndex(e.Y, true);

                    if (trackIndex >= 0 && trackIndex < queue.Count)
                    {
                        if (e.Button == MouseButtons.Left)
                        {
                            Size dragSize = SystemInformation.DragSize;

                            dragRect = new Rectangle(new Point(e.X - (dragSize.Width / 2),
                                                               e.Y - (dragSize.Height / 2)),
                                                     dragSize);
                            dragIndex = trackIndex;

                            if (dragIndex >= 0)
                                canDrag = queue[Math.Min(dragIndex, queue.Count - 1)].Selected;
                            else
                                canDrag = false;
                        }
                        else
                        {
                            dragIndex = -1;
                            SelectTrack(trackIndex, true, !queue[trackIndex].Selected, false);
                        }
                    }
                }
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (!Locked)
            {
                if (e.Button == MouseButtons.Right)
                {
                    ContextMenuStrip cms = new ContextMenuStrip();
                    cms.Renderer = new MenuItemRenderer();
                    ToolStripMenuItem tsi;    
                    if (e.Y < trackHeight) // header
                    {
                        PopulateColumnSelections(cms.Items);
                    }
                    else
                    {                        
                        ToolStripSeparator tss;

                        Track refTrack = null;

                        bool hasOneSelectedTrack = SelectedTracks.Count == 1;
                        if (hasOneSelectedTrack)
                            refTrack = SelectedTracks[0];

                        bool hasAlbum = refTrack != null && refTrack.Album.Length > 0;

                        tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_Play));
                        tsi.ShortcutKeyDisplayString = "P";
                        tsi.Click += (s, ee) => { controller.RequestAction(QActionType.PlaySelectedTracks); };
                        cms.Items.Add(tsi);

                        if (hasAlbum)
                        {
                            tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_Play_This_Album) + ":  " + refTrack.Album.Replace("&", "&&"));
                            tsi.Click += (s, ee) => { controller.RequestAction(QActionType.PlayThisAlbum); };
                        }
                        else
                        {
                            tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_Play_This_Album));
                            tsi.Enabled = false;
                        }
                        tsi.ShortcutKeyDisplayString = "Z";
                        cms.Items.Add(tsi);

                        tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_Play_Next));
                        tsi.ShortcutKeyDisplayString = "F10";
                        tsi.Click += (s, ee) =>
                        {
                            controller.RequestAction(QActionType.PlaySelectedTrackNext);
                        };
                        tsi.Enabled = hasOneSelectedTrack;
                        cms.Items.Add(tsi);

                        if (controller.TargetPlaylistName.Length > 0)
                        {
                            tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_Add_To_Playlist, controller.TargetPlaylistName));
                            tsi.ShortcutKeyDisplayString = "Ctrl+F7";
                            tsi.Click += (s, ee) => { controller.RequestAction(QActionType.AddToTargetPlaylist); };
                            cms.Items.Add(tsi);
                        }

                        tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_Add_To_Now_Playing));
                        tsi.ShortcutKeyDisplayString = "F7";
                        tsi.Click += (s, ee) => { controller.RequestAction(QActionType.AddToNowPlaying); };
                        cms.Items.Add(tsi);

                        tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_Add_Album_To_Now_Playing));
                        tsi.ShortcutKeyDisplayString = "Shift+Z";
                        tsi.Click += (s, ee) => controller.RequestAction(QActionType.AddAlbumToNowPlaying);
                        cms.Items.Add(tsi);

                        tss = new ToolStripSeparator();
                        cms.Items.Add(tss);

                        ToolStripMenuItem tsiAllOf = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_Show_All_Of));
                        tsiAllOf.DropDownItems.Add(new ToolStripMenuItem(String.Empty));
                        tsiAllOf.DropDownOpening += (s, ee) =>
                        {
                            tsiAllOf.DropDownItems.Clear();
                            ToolStripMenuItem tsiArtist = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_This_Artist));
                            ToolStripMenuItem tsiAlbum = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_This_Album));
                            ToolStripMenuItem tsiGenre = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_This_Genre));
                            ToolStripMenuItem tsiYear = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_This_Year));
                            ToolStripMenuItem tsiGrouping = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_This_Grouping));

                            tsiArtist.ShortcutKeyDisplayString = "F4";
                            tsiAlbum.ShortcutKeyDisplayString = "Shift+F4";
                            tsiGenre.ShortcutKeyDisplayString = "Ctrl+F4";

                            if (hasOneSelectedTrack)
                            {
                                Track t = SelectedTracks[0];
                                if (t.Artist.Length > 0)
                                {
                                    tsiArtist.Text += ":  " + t.MainGroup.Replace("&", "&&");
                                    tsiArtist.Click += (ss, eee) => { controller.RequestAction(QActionType.FilterSelectedArtist); };
                                }
                                else
                                {
                                    tsiArtist.Enabled = false;
                                }
                                if (t.Album.Length > 0)
                                {
                                    tsiAlbum.Text += ":  " + t.Album.Replace("&", "&&");
                                    tsiAlbum.Click += (ss, eee) => { controller.RequestAction(QActionType.FilterSelectedAlbum); };
                                }
                                else
                                {
                                    tsiAlbum.Enabled = false;
                                }
                                if (t.Genre.Length > 0 && t.Genre.ToLowerInvariant() != Localization.NO_GENRE)
                                {
                                    tsiGenre.Text += ":  " + t.Genre.Replace("&", "&&");
                                    tsiGenre.Click += (ss, eee) => { controller.RequestAction(QActionType.FilterSelectedGenre); };
                                }
                                else
                                {
                                    tsiGenre.Enabled = false;
                                }
                                if (t.YearString.Length > 0)
                                {
                                    tsiYear.Text += ":  " + t.YearString.Replace("&", "&&");
                                    tsiYear.Click += (ss, eee) => { controller.RequestAction(QActionType.FilterSelectedYear); };
                                }
                                else
                                {
                                    tsiYear.Enabled = false;
                                }
                                if (t.Grouping.Length > 0)
                                {
                                    tsiGrouping.Text += ":  " + t.Grouping.Replace("&", "&&");
                                    tsiGrouping.Click += (ss, eee) => { controller.RequestAction(QActionType.FilterSelectedGrouping); };
                                }
                                else
                                {
                                    tsiGrouping.Enabled = false;
                                }
                            }

                            tsiAllOf.DropDownItems.Add(tsiArtist);
                            tsiAllOf.DropDownItems.Add(tsiAlbum);
                            tsiAllOf.DropDownItems.Add(tsiGenre);
                            tsiAllOf.DropDownItems.Add(tsiYear);
                            tsiAllOf.DropDownItems.Add(tsiGrouping);
                            tsiAllOf.DropDownItems.Add(new ToolStripSeparator());

                            ToolStripMenuItem tsiClearAll = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_Release_All_Filters));
                            tsiClearAll.Click += (ss, eee) => { controller.RequestAction(QActionType.ReleaseAllFilters); };
                            tsiClearAll.ShortcutKeyDisplayString = "I";
                            tsiAllOf.DropDownItems.Add(tsiClearAll);
                        };
                        tsiAllOf.Enabled = hasOneSelectedTrack;
                        cms.Items.Add(tsiAllOf);

                        tss = new ToolStripSeparator();
                        cms.Items.Add(tss);

                        tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_Show_Columns));
                        PopulateColumnSelections(tsi.DropDownItems);
                        cms.Items.Add(tsi);

                        tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_Reset_Columns));
                        tsi.Click += (s, ee) => { ResetColumns(false); };
                        cms.Items.Add(tsi);

                        tss = new ToolStripSeparator();
                        cms.Items.Add(tss);

                        tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_Show_In_Windows_Explorer));
                        tsi.Click += (s, ee) => { controller.RequestAction(QActionType.ShowInWindowsExplorer); };
                        cms.Items.Add(tsi);

                        tss = new ToolStripSeparator();
                        cms.Items.Add(tss);

                        if (normal.NondynamicPlaylistBasedView)
                            tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_Remove_From_Playlist));
                        else
                            tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_Remove_From_Library));

                        tsi.Click += (s, ee) => { controller.RequestAction(QActionType.Delete); };
                        cms.Items.Add(tsi);

                        tss = new ToolStripSeparator();
                        cms.Items.Add(tss);

                        tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_Set_Rating));
                        controller.PopulateRatingSelections(tsi);
                        cms.Items.Add(tsi);

                        tss = new ToolStripSeparator();
                        cms.Items.Add(tss);

                        tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_Edit_File_Info));
                        tsi.ShortcutKeyDisplayString = "Shift+Enter";
                        tsi.Click += (s, ee) => { controller.RequestAction(QActionType.EditTags); };
                        cms.Items.Add(tsi);

                        tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Track_List_Show_File_Details));
                        tsi.ShortcutKeyDisplayString = "F8";
                        tsi.Click += (s, ee) => { controller.RequestAction(QActionType.ShowFileDetails); };
                        cms.Items.Add(tsi);
                    }
                    cms.Show(this, e.Location);
                }
                else if (autoSizingColumns)
                {
                    this.Invalidate();
                }
                else if (sizingColumns)
                {
                    if (sizingColumnsConfirmed)
                    {
                        ColInfo.SizeColumn(columns, dragCol, columns[dragCol].Width + e.X - columns[dragCol].Right);
                        this.Invalidate();
                    }
                }
                else if (draggingColumns)
                {
                    ColInfo ci = columns.Find(c => (c.Left < e.X && c.Right > e.X));
                    if (ci != null)
                    {
                        int newCol = columns.IndexOf(ci);

                        if (SortColumn == newCol)
                        {
                            SortColumn++;
                        }
                        else if (SortColumn == dragCol)
                        {
                            SortColumn = newCol;
                        }

                        ColInfo.MoveColumn(columns, dragCol, newCol);

                    }
                    this.Invalidate();
                }
                else if (swiping)
                {
                    //swiping = false;
                }
                else
                {
                    int trackIndex = click(e.Location);

                    if (trackIndex >= 0)
                    {
                        Track ti = queue[trackIndex];
                        if (Keyboard.Shift && lastSelectedIndex >= 0)
                        {
                            int last = lastSelectedIndex; // because changes in the SelectTrack method
                            if (last > trackIndex)
                            {
                                for (int i = trackIndex; i < last; i++)
                                    queue[i].Selected = true;
                            }
                            else
                            {
                                for (int i = last + 1; i <= trackIndex; i++)
                                    queue[i].Selected = true;
                            }
                        }
                        else
                        {
                            if (!queue[trackIndex].Selected)
                                SelectTrack(trackIndex, false, !Keyboard.Control, false);
                            else if (Keyboard.Control)
                                queue[trackIndex].Selected = false;
                            else
                                SelectTrack(trackIndex, false, true, false);
                        }
                        if (e.Button == MouseButtons.Right)
                        {
                            // does this ever happen?
                            controller.RequestAction(QActionType.AddToTargetPlaylist);
                        }

                        if (!doubleClicking)
                            controller.RequestAction(new QAction(QActionType.ShowTrackDetails, ti));

                        controller.RequestAction(QActionType.UpdateTrackCount);
                    }
                    this.Invalidate();
                }

                sizingColumns = false;
                draggingColumns = false;
                autoSizingColumns = false;
                swiping = false;
                doubleClicking = false;

                this.Cursor = Cursors.Default;

                dragIndex = -1;
                dragCol = -1;
            }
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            int num = Keyboard.Shift ? Math.Max(5, queue.Count / 50) : 5;

            if (e.Delta > 0)
                scrollUp(num);
            else
                scrollDown(num);

            if (!Locked)
                setHoverIndex(e.Y, true);
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);

            if (!this.Active)
                controller.RequestAction(QActionType.MoveRight);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            ClearHoverIndex();
            this.Active = false;
            ToolTipText = String.Empty;

            base.OnMouseLeave(e);
        }
        protected override void OnDragEnter(DragEventArgs drgevent)
        {
            if (!Locked)
            {
                base.OnDragEnter(drgevent);
                if (normal.NondynamicPlaylistBasedView && drgevent.Data.GetDataPresent(typeof(TrackQueue)))
                {
                    drgevent.Effect = DragDropEffects.Move;
                }
                else if (drgevent.Data.GetDataPresent("FileDrop"))
                {
                    drgevent.Effect = DragDropEffects.Copy;
                }
                else
                {
                    drgevent.Effect = DragDropEffects.None;
                }
            }
        }
        protected override void OnDragOver(DragEventArgs drgevent)
        {
            if (!Locked)
            {
                base.OnDragOver(drgevent);

                dragLocation = getTrackIndex(this.PointToClient(new Point(drgevent.X, drgevent.Y)).Y + trackHeight / 2, true) - FirstVisibleTrack + 1;
                if (dragLocation < 2)
                {
                    FirstVisibleTrack--;
                    dragLocation = 1;
                }
                else if (dragLocation > numVisibleTracks - 1)
                {
                    FirstVisibleTrack += dragLocation - numVisibleTracks;
                    dragLocation = Math.Min(numVisibleTracks, queue.Count - FirstVisibleTrack);
                }

                this.Invalidate();
            }
        }
        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            if (!Locked)
            {
                base.OnDragDrop(drgevent);
                if (dragIndex >= 0)
                {
                    groupSelectedTracks(getTrackIndex(this.PointToClient(new Point(drgevent.X, drgevent.Y)).Y + trackHeight / 2, true));
                }
                else if (drgevent.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] s = (string[])drgevent.Data.GetData("FileDrop");
                    controller.AddToLibraryOrPlaylist(s, String.Empty);
                }
                NoSort();
                dragLocation = -1;
            }
        }
        protected override void OnDragLeave(EventArgs e)
        {
            base.OnDragLeave(e);
            dragLocation = -1;
        }
        protected override void OnGiveFeedback(GiveFeedbackEventArgs gfbevent)
        {
            base.OnGiveFeedback(gfbevent);
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            setNumVisibleTracks();
            qScrollBar.Left = this.ClientRectangle.Width - SCROLL_BAR_WIDTH;
            qScrollBar.Height = this.ClientRectangle.Height;

            if (initialized)
            {
                setupCells();
                qScrollBar.LargeChange = Math.Max(1, numVisibleTracks - 1);
                qScrollBar.Max = Math.Max(0, queue.Count - numVisibleTracks);
                ToolTipText = String.Empty;
                this.Invalidate();
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                if (this.Active)
                    e.Graphics.Clear(Styles.ActiveBackground);

                int yCursor = trackHeight;
                int trackNumOnScreen = 1;
                
                Rectangle clip = e.ClipRectangle;

                if (e.ClipRectangle.IntersectsWith(rowRectangles[0]))
                {
                    e.Graphics.FillRectangle(headerBrush, rowRectangles[0]);
                    for (int i = 0; i < columns.Count; i++)
                    {
                        TextRenderer.DrawText(e.Graphics,
                            (SortColumn == i) ? columns[i].NameWithRightPadding : columns[i].Name,
                                              headingFont,
                                              columns[i].Cells[0],
                                              (i == columnHeaderHover) ? Styles.ColumnHeaderHover : Styles.ColumnHeader,
                                              columns[i].TFF);
                    }
                }

                int maxI = Math.Min(queue.Count, FirstVisibleTrack + numVisibleTracks);
                for (int i = FirstVisibleTrack; i < maxI; i++)
                {
                    if (clip.IntersectsWith(rowRectangles[trackNumOnScreen]))
                    {
                        Font f = (queue[i] == PlayingTrack) ? playingFont : font;

                        if (queue[i].Selected)
                        {
                            if (i == hoverIndex)
                                e.Graphics.FillRectangle(selectedRowHoverBrush, rowRectangles[trackNumOnScreen]);
                            else
                                e.Graphics.FillRectangle(selectedRowBrush, rowRectangles[trackNumOnScreen]);
                        }
                        else if (i == hoverIndex)
                        {
                            e.Graphics.FillRectangle(hoverBrush, rowRectangles[trackNumOnScreen]);
                        }

                        paintRow(e.Graphics,
                                 trackNumOnScreen,
                                 queue[i],
                                 f,
                                 queue[i] == PlayingTrack ? Styles.Playing : ((queue[i].Exists == false) ? Styles.NonExistentTrack : Styles.LightText)
                                 );
                    }
                    yCursor += trackHeight;
                    trackNumOnScreen++;
                }


                for (int i = 0; i < columns.Count; i++)
                {
                    e.Graphics.DrawLine(Styles.DarkBorderPen,
                                        columns[i].Left,
                                        trackHeight,
                                        columns[i].Left,
                                        this.ClientRectangle.Height);
                }

                if (SortColumn >= 0)
                    Styles.DrawArrow(e.Graphics, Styles.SortArrowPen, revSort, columns[SortColumn].Right - 10, 6);

                if (draggingColumns)
                {
                    e.Graphics.FillRectangle(headerBrush, columns[dragHoverCol].Cells[0]);
                    e.Graphics.DrawRectangle(Styles.TrackDragLinePen, columns[dragHoverCol].Cells[0]);
                    TextRenderer.DrawText(e.Graphics,
                                          columns[dragCol].Name,
                                          headingFont,
                                          columns[dragHoverCol].Cells[0],
                                          Color.White,
                                          columns[dragHoverCol].TFF);
                }
                else if (sizingColumns)
                {
                    e.Graphics.DrawLine(Styles.TrackDragLinePen, sizingColumnXPosn, 0, sizingColumnXPosn, this.ClientRectangle.Height);
                }
                else if (dragLocation >= 0)
                {
                    e.Graphics.DrawLine(Styles.TrackDragLinePen, 0, columns[0].Cells[dragLocation].Top, this.ClientRectangle.Width, columns[0].Cells[dragLocation].Top);
                }
                if (ToolTipText.Length > 0)
                {
                    e.Graphics.FillRectangle(Styles.ToolTipBrush,
                                             ToolTipRect);
                    
                    e.Graphics.DrawRectangle(Styles.DarkBorderPen, ToolTipRect);
                   
                    TextRenderer.DrawText(e.Graphics,
                                          ToolTipText,
                                          toolTipFont,
                                          ToolTipRect,
                                          Styles.ToolTipText,
                                          tff);
                }
            }
            catch { }
        }
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            this.Active = true;
        }

        private void setHoverIndex(int yPos, bool InvalidateAll)
        {
            int i = getTrackIndex(yPos, false);
            if (i != hoverIndex)
            {
                if (InvalidateAll)
                {
                    this.Invalidate();
                }
                else
                {
                    if (i >= 0)
                        this.Invalidate(rowRectangles[i - FirstVisibleTrack + 1]);
                    if (hoverIndex >= 0 && hoverIndex < (rowRectangles.Count + FirstVisibleTrack - 1))
                        this.Invalidate(rowRectangles[hoverIndex - FirstVisibleTrack + 1]);
                }
                hoverIndex = i;
            }
        }
        private void setNumVisibleTracks()
        {
            numVisibleTracks = Math.Max(1, (this.ClientRectangle.Height + 2) / trackHeight - 1);
            scrollBuffer = numVisibleTracks / 10;
        }
        private int FirstVisibleTrack
        {
            get { return firstVisibleTrack; }
            set
            {
                int newVal = Math.Max(0, Math.Min(value, queue.Count - numVisibleTracks));
                if (firstVisibleTrack != newVal)
                {
                    hoverIndex = -1;
                    qScrollBar.Value = newVal;
                    firstVisibleTrack = newVal;
                    ToolTipText = String.Empty;
                    this.Invalidate();
                }
            }
        }
        private void scroll(QScrollBar Sender, int Value)
        {
            FirstVisibleTrack = Value;
            this.Invalidate();
        }
        private void setupCells()
        {
            int width = this.ClientRectangle.Width - qScrollBar.Width;

            ColInfo.SetupCells(columns, this.ClientRectangle.Width - qScrollBar.Width, numVisibleTracks + 2, trackHeight);
            
            rowRectangles.Clear();
            foreach (Rectangle r in columns[0].Cells)
                rowRectangles.Add(new Rectangle(r.Location, new Size(width, r.Height)));
        }
        private void paintRow(Graphics g, int TrackNumOnScreen, Track Track, Font Font, Color Color)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                TextRenderer.DrawText(g,
                                      columns[i].Data(Track),
                                      Font,
                                      columns[i].Cells[TrackNumOnScreen],
                                      Color, columns[i].TFF);
            }
        }
        
        private string ToolTipText
        {
            get { return toolTipText; }
            set
            {
                if (toolTipText != value)
                {
                    toolTipOriginalLocation = Point.Empty;
                    toolTipText = value;
                    this.Invalidate();
                }
            }
        }
        private Rectangle ToolTipRect
        {
            get { return toolTipRect; }
            set
            {
                if (toolTipRect != value)
                {
                    toolTipRect = value;
                    this.Invalidate();
                }
            }
        }
        private void setToolTip()
        {
            try
            {
                Point p = this.PointToClient(Cursor.Position);

                int rowNum = (int)(p.Y / trackHeight) - 1;
                ColInfo ci = columns.Find(c => (c.Left < p.X && c.Right > p.X));

                if (ci != null && (queue.Count > FirstVisibleTrack + rowNum || rowNum == -1) && rowNum < numVisibleTracks)
                {
                    string text;
                    if (rowNum == -1)
                    {
                        text = ci.Name;
                    }
                    else
                    {
                        text = ci.Data(queue[FirstVisibleTrack + rowNum]);
                    }

                    p = ci.Cells[rowNum + 1].Location;
                    if (p != toolTipOriginalLocation)
                    {
                        toolTipOriginalLocation = p;
                        if (rowNum == -1)
                            toolTipFont = headingFont;
                        else if (queue[FirstVisibleTrack + rowNum] == PlayingTrack)
                            toolTipFont = playingFont;
                        else
                            toolTipFont = font;
                        int width = TextRenderer.MeasureText(text, toolTipFont, ci.Cells[0].Size, TextFormatFlags.NoPrefix).Width;
                        if (width > ci.Width)
                        {
                            ToolTipRect = new Rectangle(toolTipOriginalLocation, new Size(width, trackHeight));
                            if (ToolTipRect.Right > this.Width - SCROLL_BAR_WIDTH)
                                ToolTipRect = new Rectangle(new Point(Math.Max(0, this.Right - width - SCROLL_BAR_WIDTH), ToolTipRect.Y),
                                                            ToolTipRect.Size);
                            ToolTipText = text;

                        }
                        else
                        {
                            ToolTipText = String.Empty;
                        }
                    }
                    return;
                }
            }
            catch { }
            ToolTipText = String.Empty;
        }

        private int click(Point Location)
        {
            if (Location.Y < trackHeight)
            {
                for (int i = 0; i < columns.Count; i++)
                {
                    if (Location.X < columns[i].Right)
                    {
                        if (SortColumn == i && !revSort)
                            revSort = true;
                        else
                            revSort = false;
                        SortColumn = i;
                        break;
                    }
                }
                sort();
                MakeAnInterestingTrackVisible(-1);
                return -1;
            }
            else
            {
                return getTrackIndex(Location.Y, false);
            }
        }
        private int getTrackIndex(int yPos, bool GoPastLast)
        {
            int index = firstVisibleTrack + yPos / trackHeight - 1;
            if (GoPastLast)
            {
                if (index >= queue.Count)
                    return queue.Count;
                else if (index < 0)
                    return 0;
                else
                    return index;
            }
            else
            {
                if (index < queue.Count && index >= firstVisibleTrack)
                    return index;
                else
                    return -1;
            }
        }
        private void sort()
        {
            if (SortColumn >= 0)
            {
                queue.Sort(columns[SortColumn].GetSorting(revSort));
                lastSelectedIndex = -1;
            }
        }
        
        private Brush headerBrush;
        private Brush selectedRowBrush;
        private Brush selectedRowHoverBrush;
        private Brush hoverBrush;
        
        static TrackList()
        {
            columnDefinitions = new List<ColInfo>();

            columnDefinitions.Add(new ColInfo(ColumnID.Artist,
                                  Localization.Get(UI_Key.Track_List_Column_Artist_Short),
                                  Localization.Get(UI_Key.Track_List_Column_Artist),
                                  new Func<Track, string>(t => t.Artist),
                                  new Comparison<Track>((a, b) => a.ArtistComparer(b)),
                                  new Comparison<Track>((a, b) => b.ArtistComparer(a)),
                                  600,
                                  true,
                                  tff));

            columnDefinitions.Add(new ColInfo(ColumnID.AlbumArtist,
                                  Localization.Get(UI_Key.Track_List_Column_Album_Artist_Short),
                                  Localization.Get(UI_Key.Track_List_Column_Album_Artist),
                                  new Func<Track, string>(t => t.AlbumArtist),
                                  new Comparison<Track>((a, b) => a.AlbumArtistComparer(b)),
                                  new Comparison<Track>((a, b) => b.AlbumArtistComparer(a)),
                                  500,
                                  true,
                                  tff));


            columnDefinitions.Add(new ColInfo(ColumnID.Album,
                                  Localization.Get(UI_Key.Track_List_Column_Album_Short),
                                  Localization.Get(UI_Key.Track_List_Column_Album),
                                  new Func<Track, string>(t => t.Album),
                                  new Comparison<Track>((a, b) => a.AlbumComparer(b)),
                                  new Comparison<Track>((a, b) => b.AlbumComparer(a)),
                                  600,
                                  false,
                                  tff));

            columnDefinitions.Add(new ColInfo(ColumnID.Title,
                                  Localization.Get(UI_Key.Track_List_Column_Title_Short),
                                  Localization.Get(UI_Key.Track_List_Column_Title),
                                  new Func<Track, string>(t => t.Title),
                                  new Comparison<Track>((a, b) => a.Title.CompareTo(b.Title)),
                                  new Comparison<Track>((a, b) => b.Title.CompareTo(a.Title)),
                                  1100,
                                  false,
                                  tff));

            columnDefinitions.Add(new ColInfo(ColumnID.Genre,
                                  Localization.Get(UI_Key.Track_List_Column_Genre_Short),
                                  Localization.Get(UI_Key.Track_List_Column_Genre),
                                  new Func<Track, string>(t => t.Genre),
                                  new Comparison<Track>((a, b) => a.Genre.CompareTo(b.Genre)),
                                  new Comparison<Track>((a, b) => b.Genre.CompareTo(a.Genre)),
                                  300,
                                  false,
                                  tff));

            columnDefinitions.Add(new ColInfo(ColumnID.Composer,
                                  Localization.Get(UI_Key.Track_List_Column_Composer_Short),
                                  Localization.Get(UI_Key.Track_List_Column_Composer),
                                  new Func<Track, string>(t => t.Composer),
                                  new Comparison<Track>((a, b) => a.Composer.CompareTo(b.Composer)),
                                  new Comparison<Track>((a, b) => b.Composer.CompareTo(a.Composer)),
                                  400,
                                  false,
                                  tff));

            columnDefinitions.Add(new ColInfo(ColumnID.Grouping,
                                  Localization.Get(UI_Key.Track_List_Column_Grouping_Short),
                                  Localization.Get(UI_Key.Track_List_Column_Grouping),
                                  new Func<Track, string>(t => t.Grouping),
                                  new Comparison<Track>((a, b) => a.Grouping.CompareTo(b.Grouping)),
                                  new Comparison<Track>((a, b) => b.Grouping.CompareTo(a.Grouping)),
                                  500,
                                  false,
                                  tff));

            columnDefinitions.Add(new ColInfo(ColumnID.TrackNum,
                                  Localization.Get(UI_Key.Track_List_Column_TrackNum_Short),
                                  Localization.Get(UI_Key.Track_List_Column_TrackNum),
                                  new Func<Track, string>(t => t.TrackNumString),
                                  new Comparison<Track>((a, b) => a.TrackNum.CompareTo(b.TrackNum)),
                                  new Comparison<Track>((a, b) => b.TrackNum.CompareTo(a.TrackNum)),
                                  120,
                                  false,
                                  tffRight));

            columnDefinitions.Add(new ColInfo(ColumnID.DiskNum,
                                  Localization.Get(UI_Key.Track_List_Column_DiskNum_Short),
                                  Localization.Get(UI_Key.Track_List_Column_DiskNum),
                                  new Func<Track, string>(t => t.DiskNumString),
                                  new Comparison<Track>((a, b) => a.DiskNumComparer(b)),
                                  new Comparison<Track>((a, b) => b.DiskNumComparer(a)),
                                  120,
                                  false,
                                  tffRight));

            columnDefinitions.Add(new ColInfo(ColumnID.Length,
                                  Localization.Get(UI_Key.Track_List_Column_Length_Short),
                                  Localization.Get(UI_Key.Track_List_Column_Length),
                                  new Func<Track, string>(t => t.DurationInfo),
                                  new Comparison<Track>((a, b) => a.Duration.CompareTo(b.Duration)),
                                  new Comparison<Track>((a, b) => b.Duration.CompareTo(a.Duration)),
                                  190,
                                  false,
                                  tffRight));

            columnDefinitions.Add(new ColInfo(ColumnID.Year,
                                  Localization.Get(UI_Key.Track_List_Column_Year_Short),
                                  Localization.Get(UI_Key.Track_List_Column_Year),
                                  new Func<Track, string>(t => t.YearString),
                                  new Comparison<Track>((a, b) => a.Year.CompareTo(b.Year)),
                                  new Comparison<Track>((a, b) => b.Year.CompareTo(a.Year)),
                                  170,
                                  false,
                                  tffRight));

            columnDefinitions.Add(new ColInfo(ColumnID.BitRate,
                                  Localization.Get(UI_Key.Track_List_Column_Bitrate_Short),
                                  Localization.Get(UI_Key.Track_List_Column_Bitrate),
                                  new Func<Track, string>(t => t.BitrateString),
                                  new Comparison<Track>((a, b) => a.Bitrate.CompareTo(b.Bitrate)),
                                  new Comparison<Track>((a, b) => b.Bitrate.CompareTo(a.Bitrate)),
                                  170,
                                  false,
                                  tffRight));

            columnDefinitions.Add(new ColInfo(ColumnID.Size,
                                  Localization.Get(UI_Key.Track_List_Column_Size_Short),
                                  Localization.Get(UI_Key.Track_List_Column_Size),
                                  new Func<Track, string>(t => t.FileSizeString),
                                  new Comparison<Track>((a, b) => a.FileSize.CompareTo(b.FileSize)),
                                  new Comparison<Track>((a, b) => b.FileSize.CompareTo(a.FileSize)),
                                  170,
                                  false,
                                  tffRight));

            columnDefinitions.Add(new ColInfo(ColumnID.FileDate,
                                  Localization.Get(UI_Key.Track_List_Column_File_Date_Short),
                                  Localization.Get(UI_Key.Track_List_Column_File_Date),
                                  new Func<Track, string>(t => t.FileDateString),
                                  new Comparison<Track>((a, b) => a.FileDate.CompareTo(b.FileDate)),
                                  new Comparison<Track>((a, b) => b.FileDate.CompareTo(a.FileDate)),
                                  220,
                                  false,
                                  tff));

            columnDefinitions.Add(new ColInfo(ColumnID.DaysSinceLastPlayed,
                                  Localization.Get(UI_Key.Track_List_Column_Days_Since_Last_Played_Short),
                                  Localization.Get(UI_Key.Track_List_Column_Days_Since_Last_Played),
                                  new Func<Track, string>(t => t.DaysSinceLastPlayedString),
                                  new Comparison<Track>((a, b) => b.LastPlayedDate.CompareTo(a.LastPlayedDate)),
                                  new Comparison<Track>((a, b) => a.LastPlayedDate.CompareTo(b.LastPlayedDate)),
                                  120,
                                  false,
                                  tffRight));

            columnDefinitions.Add(new ColInfo(ColumnID.DaysSinceAdded,
                                  Localization.Get(UI_Key.Track_List_Column_Days_Since_Added_Short),
                                  Localization.Get(UI_Key.Track_List_Column_Days_Since_Added),
                                  new Func<Track, string>(t => t.DaysSinceAddedString),
                                  new Comparison<Track>((a, b) => b.AddDate.CompareTo(a.AddDate)),
                                  new Comparison<Track>((a, b) => a.AddDate.CompareTo(b.AddDate)),
                                  120,
                                  false,
                                  tffRight));

            columnDefinitions.Add(new ColInfo(ColumnID.PlayCount,
                                  Localization.Get(UI_Key.Track_List_Column_Play_Count_Short),
                                  Localization.Get(UI_Key.Track_List_Column_Play_Count),
                                  new Func<Track, string>(t => t.PlayCountString),
                                  new Comparison<Track>((a, b) => a.PlayCount.CompareTo(b.PlayCount)),
                                  new Comparison<Track>((a, b) => b.PlayCount.CompareTo(a.PlayCount)),
                                  140,
                                  false,
                                  tffRight));

            columnDefinitions.Add(new ColInfo(ColumnID.Rating,
                                  Localization.Get(UI_Key.Track_List_Column_Rating_Short),
                                  Localization.Get(UI_Key.Track_List_Column_Rating),
                                  new Func<Track, string>(t => t.RatingString),
                                  new Comparison<Track>((a, b) => a.Rating.CompareTo(b.Rating)),
                                  new Comparison<Track>((a, b) => b.Rating.CompareTo(a.Rating)),
                                  140,
                                  false,
                                  tffCenter));

            columnDefinitions.Add(new ColInfo(ColumnID.Encoder,
                                  Localization.Get(UI_Key.Track_List_Column_Encoder_Short),
                                  Localization.Get(UI_Key.Track_List_Column_Encoder),
                                  new Func<Track, string>(t => t.Encoder),
                                  new Comparison<Track>((a, b) => a.Encoder.CompareTo(b.Encoder)),
                                  new Comparison<Track>((a, b) => b.Encoder.CompareTo(a.Encoder)),
                                  200,
                                  false,
                                  tff));

            columnDefinitions.Add(new ColInfo(ColumnID.FileType,
                                  Localization.Get(UI_Key.Track_List_Column_File_Type_Short),
                                  Localization.Get(UI_Key.Track_List_Column_File_Type),
                                  new Func<Track, string>(t => t.TypeString),
                                  new Comparison<Track>((a, b) => a.TypeString.CompareTo(b.TypeString)),
                                  new Comparison<Track>((a, b) => b.TypeString.CompareTo(b.TypeString)),
                                  200,
                                  false,
                                  tff));

            columnDefinitions.Add(new ColInfo(ColumnID.NumChannels,
                                  Localization.Get(UI_Key.Track_List_Column_Num_Channels_Short),
                                  Localization.Get(UI_Key.Track_List_Column_Num_Channels),
                                  new Func<Track, string>(t => t.NumChannelsString),
                                  new Comparison<Track>((a, b) => a.NumChannels.CompareTo(b.NumChannels)),
                                  new Comparison<Track>((a, b) => b.NumChannels.CompareTo(a.NumChannels)),
                                  200,
                                  false,
                                  tffCenter));

            columnDefinitions.Add(new ColInfo(ColumnID.SampleRate,
                                  Localization.Get(UI_Key.Track_List_Column_Sample_Freq_Short),
                                  Localization.Get(UI_Key.Track_List_Column_Sample_Freq),
                                  new Func<Track, string>(t => t.SampleRateString),
                                  new Comparison<Track>((a, b) => a.SampleRate.CompareTo(b.SampleRate)),
                                  new Comparison<Track>((a, b) => b.SampleRate.CompareTo(a.SampleRate)),
                                  200,
                                  false,
                                  tffRight));


            columnDefinitions.Add(new ColInfo(ColumnID.FileName,
                                  Localization.Get(UI_Key.Track_List_Column_File_Name_Short),
                                  Localization.Get(UI_Key.Track_List_Column_File_Name),
                                  new Func<Track, string>(t => t.FileName),
                                  new Comparison<Track>((a, b) => a.FileName.CompareTo(b.FileName)),
                                  new Comparison<Track>((a, b) => b.FileName.CompareTo(a.FileName)),
                                  600,
                                  false,
                                  tff));

            columnDefinitions.Add(new ColInfo(ColumnID.Equalizer,
                                  Localization.Get(UI_Key.Track_List_Column_Equalizer_Short),
                                  Localization.Get(UI_Key.Track_List_Column_Equalizer),
                                  new Func<Track, string>(t => t.EqualizerString),
                                  new Comparison<Track>((a, b) => a.EqualizerString.CompareTo(b.EqualizerString)),
                                  new Comparison<Track>((a, b) => b.EqualizerString.CompareTo(a.EqualizerString)),
                                  400,
                                  false,
                                  tff));

            columnDefinitions.Add(new ColInfo(ColumnID.ReplayGain,
                                 Localization.Get(UI_Key.Track_List_Column_Replay_Gain_Short),
                                 Localization.Get(UI_Key.Track_List_Column_Replay_Gain),
                                 new Func<Track, string>(t => t.ReplayGainString),
                                 new Comparison<Track>((a, b) => a.ReplayGain.CompareTo(b.ReplayGain)),
                                 new Comparison<Track>((a, b) => b.ReplayGain.CompareTo(a.ReplayGain)),
                                 300,
                                 false,
                                 TextFormatFlags.Right));
        }
        private void scrollDown(int NumTracks)
        {
            FirstVisibleTrack = FirstVisibleTrack + NumTracks;
            this.Invalidate();
        }
        private void scrollUp(int NumTracks)
        {
            FirstVisibleTrack = FirstVisibleTrack - NumTracks;
            this.Invalidate();
        }
        private void ensureVisible(int Index, bool WithBuffer)
        {
            if (WithBuffer)
            {
                if (Index < FirstVisibleTrack + scrollBuffer)
                    FirstVisibleTrack = Index - scrollBuffer;
                else if (Index >= FirstVisibleTrack + numVisibleTracks - scrollBuffer)
                    FirstVisibleTrack = Index - numVisibleTracks + scrollBuffer + 1;
            }
            else
            {
                if (Index < FirstVisibleTrack)
                    FirstVisibleTrack = Index;
                else if (Index >= FirstVisibleTrack + numVisibleTracks)
                    FirstVisibleTrack = Index - numVisibleTracks + 1;
            }
        }
        private void groupSelectedTracks(int TargetIndex)
        {
            bool done = false;
            while (!done)
            {
                done = true;
                for (int i = 0; i < queue.Count; i++)
                {
                    if (i < queue.Count - 1 && queue[i].Selected && !queue[i + 1].Selected && i < TargetIndex - 1)
                    {
                        done = false;
                        queue.Swap(i, i + 1);
                    }
                    if (i > 0 && queue[i].Selected && !queue[i - 1].Selected && i > TargetIndex)
                    {
                        done = false;
                        queue.Swap(i, i - 1);
                    }
                }
            }
            this.Invalidate();
        }
        public static void PopulateColumnSelections(ToolStripItemCollection tsic)
        {
            List<TrackList.ColumnStatusItem> cols = instance.GetColumnStatus();

            cols.Sort((a, b) => a.LongName.CompareTo(b.LongName));

            if (tsic.Count <= 1) // just the dummy
            {
                tsic.Clear();
                foreach (TrackList.ColumnStatusItem csi in cols)
                {
                    ToolStripItem tsi = tsic.Add(csi.LongName);
                    tsi.Tag = csi;
                    tsi.Click += (s, ee) =>
                    {
                        instance.ToggleColumnVisibility((TrackList.ColumnStatusItem)tsi.Tag);
                    };
                }
            }
            for (int i = 0; i < cols.Count; i++)
            {
                (tsic[i] as ToolStripMenuItem).Checked = cols[i].Visible;
            }
        }
        private bool indexIsVisible(int Index)
        {
            return Index >= firstVisibleTrack && Index < firstVisibleTrack + numVisibleTracks;
        }

        internal class ColumnStatusItem
        {
            public ColumnID ID { get; private set; }
            public string LongName { get; private set; }
            public bool Visible { get; set; }
            public ColumnStatusItem(ColumnID ID, string LongName, bool Visible)
            {
                this.ID = ID;
                this.LongName = LongName;
                this.Visible = Visible;
            }
        }
        private class ColInfo
        {
            public ColumnID ID { get; private set; }
            public string Name { get; private set; }
            public string LongName { get; private set; }
            public Func<Track, string> Data { get; private set; }
            private Comparison<Track> Sorting { get; set; }
            private Comparison<Track> ReverseSorting { get; set; }
            public List<Rectangle> Cells { get; private set; }
            public bool SuppressThe { get; private set; }
            public int RelativeWidth { get; set; }
            public TextFormatFlags TFF { get; private set; }

            private int defaultRelativeWidth;

            public ColInfo(ColumnID ID,
                           string Name,
                           string LongName,
                           Func<Track, string> Data,
                           Comparison<Track> Sorting,
                           Comparison<Track> ReverseSorting,
                           int RelativeWidth,
                           bool SuppressThe,
                           TextFormatFlags TFF)
            {
                this.ID = ID;
                this.Name = Name;
                this.LongName = LongName;
                this.Data = Data;
                this.Sorting = Sorting;
                this.ReverseSorting = ReverseSorting;
                this.RelativeWidth = this.defaultRelativeWidth = RelativeWidth;
                this.SuppressThe = SuppressThe;
                this.TFF = TFF;
                this.Cells = new List<Rectangle>();
            }

            public int Left
            {
                get { return Cells[0].Left; }
            }
            public int Right
            {
                get { return Cells[0].Right; }
            }
            public int Width
            {
                get { return this.Cells[0].Width; }
            }
            public string NameWithRightPadding
            {
                get
                {
                    if ((TFF & TextFormatFlags.Right) != TextFormatFlags.Right)
                        return Name;
                    else
                        return Name + "   ";
                }
            }
            public int HeaderEdgeLeft
            {
                get { return Cells[0].Right - 5; }
            }
            public int HeaderEdgeRight
            {
                get { return Cells[0].Right + 5; }
            }

            public Comparison<Track> GetSorting(bool Reverse)
            {
                return Reverse ? ReverseSorting : Sorting;
            }
            public void AddCell(Rectangle R)
            {
                Cells.Add(R);
            }
            public void ClearCells()
            {
                Cells.Clear();
            }
            public static void SetupCells(List<ColInfo> Columns, int pixWidth, int NumRows, int CellHeight)
            {
                int[] tabStops = new int[Columns.Count + 1];

                int totWidth = Columns.Sum(ci => ci.RelativeWidth);

                tabStops[0] = 0;
                for (int i = 1; i < tabStops.Length - 1; i++)
                {
                    tabStops[i] = (int)(pixWidth * Columns[i - 1].RelativeWidth / totWidth) + tabStops[i - 1];
                }
                tabStops[tabStops.Length - 1] = pixWidth;

                for (int i = 0; i < Columns.Count; i++)
                {
                    Columns[i].ClearCells();
                    for (int j = 0; j < NumRows; j++)
                    {
                        Columns[i].AddCell(new Rectangle(tabStops[i], j * CellHeight, tabStops[i + 1] - tabStops[i], CellHeight));
                    }
                }
            }
            public static void SizeColumn(List<ColInfo> Columns, int ColumnNum, int NewWidth)
            {
                int totalWidth = Columns.Sum(ci => ci.Cells[0].Width);

                NewWidth = Math.Max(10, NewWidth);
                NewWidth = Math.Min(NewWidth, totalWidth - 10);

                int totalRelativeWidthLessNewCol = 0;

                for (int i = 0; i < Columns.Count; i++)
                {
                    if (i != ColumnNum)
                    {
                        Columns[i].RelativeWidth = Columns[i].Width * 10;
                        totalRelativeWidthLessNewCol += Columns[i].RelativeWidth;
                    }
                }
                Columns[ColumnNum].RelativeWidth = totalRelativeWidthLessNewCol * NewWidth / (totalWidth - NewWidth);

                SetupCells(Columns, totalWidth, Columns[0].Cells.Count, Columns[0].Cells[0].Height);
            }
            public static void MoveColumn(List<ColInfo> Columns, int FromCol, int ToCol)
            {
                if (FromCol != ToCol)
                {
                    ColInfo col = Columns[FromCol];

                    if (FromCol < ToCol)
                    {
                        for (int i = FromCol; i < ToCol; i++)
                            Columns[i] = Columns[i + 1];
                        Columns[ToCol] = col;
                    }
                    else
                    {
                        for (int i = FromCol; i > ToCol; i--)
                            Columns[i] = Columns[i - 1];
                        Columns[ToCol] = col;
                    }

                    int left;
                    int width;
                    int top;
                    int height;

                    int low = Math.Min(FromCol, ToCol);
                    int high = Math.Max(FromCol, ToCol);

                    for (int i = low; i <= high; i++)
                    {

                        left = (i > 0) ? Columns[i - 1].Cells[0].Right : 0;
                        width = Columns[i].Cells[0].Width;
                        height = Columns[i].Cells[0].Height;

                        for (int j = 0; j < Columns[i].Cells.Count; j++)
                        {
                            top = Columns[i].Cells[j].Top;
                            Rectangle r = Columns[i].Cells[j];
                            Columns[i].Cells[j] = new Rectangle(left, top, width, height);
                        }
                    }
                }
            }
            public static void ResetColumnWidths(List<ColInfo> Columns)
            {
                foreach (ColInfo ci in Columns)
                    ci.RelativeWidth = ci.defaultRelativeWidth;
            }
            public static void AutoSizeColumn(List<ColInfo> Columns, int ColumnNum, List<Track> Tracks, int Start, int End, Font HeadingFont, Font Font)
            {
                if (ColumnNum >= 0)
                {
                    int width = TextRenderer.MeasureText(Columns[ColumnNum].Name, HeadingFont).Width;
                    for (int i = Start; i <= Math.Min(End, Tracks.Count - 1); i++)
                    {
                        width = Math.Max(width, TextRenderer.MeasureText(Columns[ColumnNum].Data(Tracks[i]), Font).Width);
                    }
                    width += 10;
                    SizeColumn(Columns, ColumnNum, width);
                }
            }
        }
        private QuuxPlayer.Automation.TrackListProvider automationProvider = null;
        public QuuxPlayer.Automation.TrackListProvider Provider
        {
            get { return automationProvider; }
        }

        [PermissionSetAttribute(SecurityAction.Demand, Unrestricted = true)]
        protected override void WndProc(ref Message winMessage)
        {
            const int WM_GETOBJECT = 0x003D;

            if (winMessage.Msg == WM_GETOBJECT)
            {
                if (automationProvider == null)
                {
                    automationProvider = new QuuxPlayer.Automation.TrackListProvider(this);
                }

                winMessage.Result = AutomationInteropProvider.ReturnRawElementProvider(Handle, winMessage.WParam, winMessage.LParam, automationProvider);
                return;
            }
            base.WndProc(ref winMessage);
        }
    }
}
