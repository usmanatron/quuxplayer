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
    internal class NormalView : Control, IMainView
    {
        private const int GAP_BETWEEN_FILTER_LIST_AND_ALB_COVER = 12;

        private enum ActiveListEnum { TrackList, FilterValueList }

        private ActiveListEnum activeList = ActiveListEnum.TrackList;

        private Controller controller;
        private TrackList trackList;
        private FilterValueList filterValueList;
        private FilterBar filterBar;
        private QSplitContainer splMain;
        private QPanel panel = null;
        private Artwork artwork;
        private frmMain mainForm;

        private static NormalView instance;

        public NormalView(frmMain MainForm)
        {
            this.mainForm = MainForm;
            instance = this;

            this.trackList = new TrackList(this);
            this.filterValueList = new FilterValueList();
            this.filterBar = new FilterBar();
            this.artwork = new Artwork();

            this.splMain = new QSplitContainer();

            this.splMain.Panel1.Controls.Add(filterValueList);
            this.splMain.Panel2.Controls.Add(trackList);
            this.Controls.Add(splMain);
            this.Controls.Add(filterBar);

            splMain.Resize += (s, e) => { arrangeSplitterControls(); };
            splMain.SplitterMoved += (s, e) => { arrangeSplitterControls(); };
            splMain.VisibleChanged += (s, e) =>
            {
                if (splMain.Visible)
                    splMain.SplitterDistance = Setting.SplitterDistance;
            };

            this.artwork = Artwork;

            splMain.Panel1.Controls.Add(artwork);

            filterBar.FilterReleased += filterBarFilterCleared;
            filterBar.FilterValueChanged += filterBarFilterValueChanged;
            
            filterValueList.FilterValueSelected += new FilterValueList.FilterValueSelect(filterValueChanged);
            filterValueList.ValueChanged += (oldVal, newVal) => { changePlaylistName(oldVal, newVal); };

            arrangeSplitterControls();
            splMain.Panel1.Paint += new PaintEventHandler(Panel1_Paint);
            artwork.Click += (s, e) => { controller.RequestAction(QActionType.FindPlayingTrack); };
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            filterBar.Width = this.ClientRectangle.Width;
            splMain.Bounds = new Rectangle(0, filterBar.Bottom, this.ClientRectangle.Width, this.ClientRectangle.Height - filterBar.Height);
            this.splMain.SplitterDistance = Setting.SplitterDistance;
        }
        public static NormalView GetInstance() { return instance; }

        public Controller Controller
        {
            set
            {
                controller = value;
                filterBar.Controller = controller;
                trackList.Controller = controller;
                filterValueList.Controller = controller;

                trackList.SetColumnStatus(Database.GetSetting(SettingType.ColumnStatus, String.Empty), HTPCMode.Normal);
                trackList.SetColumnStatus(Database.GetSetting(SettingType.ColumnStatusHTPC, String.Empty), HTPCMode.HTPC);
            }
        }
        public ViewType ViewType { get { return ViewType.Normal; } }
        public TrackList TrackList { get { return trackList; } }
        public Artwork Artwork { get { return artwork; } }
        private void arrangeSplitterControls()
        {
            if (Panel != null)
            {
                trackList.Size = new Size(splMain.Panel2.ClientSize.Width,
                                          splMain.Panel2.ClientSize.Height - Panel.Height);
                Panel.Top = trackList.Bottom;
                Panel.Width = trackList.Width;
            }
            else
            {
                trackList.Size = splMain.Panel2.ClientSize;
            }

            if (Setting.ShowAlbumArtOnMainScreen)
            {
                int artSize = Math.Min(splMain.Panel1.Height / 3 - GAP_BETWEEN_FILTER_LIST_AND_ALB_COVER,
                                       splMain.Panel1.Width - 4);

                artwork.Size = new Size(splMain.Panel1.Width,
                                         artSize);

                filterValueList.Size = new Size(splMain.Panel1.Width,
                                                splMain.Panel1.Height - artSize - GAP_BETWEEN_FILTER_LIST_AND_ALB_COVER * 3 / 2);
                artwork.Top = filterValueList.Bottom + GAP_BETWEEN_FILTER_LIST_AND_ALB_COVER;

                artwork.Visible = true;
                splMain.Panel1.Invalidate();
            }
            else
            {
                filterValueList.Size = new Size(splMain.Panel1.Width, splMain.Panel1.Height);
                artwork.Visible = false;
            }
        }
        public QPanel Panel
        {
            get { return panel; }
            set
            {
                if (!object.Equals(panel, value))
                {
                    splMain.Panel2.Controls.Remove(panel);
                    panel = value;
                    arrangeSplitterControls();
                    mainForm.UpdateForPanel();
                    if (panel != null)
                    {
                        splMain.Panel2.Controls.Add(panel);
                        panel.Focus();
                    }
                }
            }
        }
        public void ShowTagEditor(List<Track> tracks, TagEditor.TagEditComplete Callback)
        {
            ShowPanel(new TagEditor(trackList, tracks, Callback));
        }
        public void FindFilterValue(string Value)
        {
            filterValueList.FindValue(Value);
        }
        private void changePlaylistName(string OldName, string NewName)
        {
            if (CurrentFilterType == FilterType.Playlist)
            {
                if (Database.PlaylistExists(NewName) && (OldName.ToLowerInvariant() != NewName.ToLowerInvariant()))
                {
                    QMessageBox.Show(mainForm,
                                    Localization.Get(UI_Key.Dialog_Duplicate_Playlist, NewName.Trim()),
                                    Localization.Get(UI_Key.Dialog_Duplicate_Playlist_Title),
                                    QMessageBoxIcon.Warning);
                }
                else if (Database.ChangePlaylistName(OldName, NewName))
                {
                    if (Queue.PlaylistBasis == OldName)
                        Queue.PlaylistBasis = NewName;

                    if (controller.TargetPlaylistName == OldName)
                        controller.TargetPlaylistName = NewName;

                    CurrentFilterValue = NewName;

                    LoadFilterValues();
                }
            }
        }
        public void ShowIndexLetterFilterValue()
        {
            ShowLetter(filterValueList.CurrentLetterIndex);
        }
        public void ShowIndexLetterTrackList()
        {
            ShowLetter(trackList.CurrentLetterIndex);
        }
        public void RenameSelectedPlaylist()
        {
            if (CurrentFilterType == FilterType.Playlist && !NowPlayingVisible)
            {
                filterValueList.StartItemEdit();
            }
        }
        private void Panel1_Paint(object sender, PaintEventArgs e)
        {
            if (Setting.ShowAlbumArtOnMainScreen)
            {
                e.Graphics.DrawLine(Styles.DarkBorderPen,
                                    1,
                                    artwork.Top - GAP_BETWEEN_FILTER_LIST_AND_ALB_COVER / 2,
                                    artwork.Right - 1,
                                    artwork.Top - GAP_BETWEEN_FILTER_LIST_AND_ALB_COVER / 2);
            }
        }
        public bool ShowAlbumArtOnMainScreen
        {
            get { return Setting.ShowAlbumArtOnMainScreen; }
            set
            {
                if (Setting.ShowAlbumArtOnMainScreen != value)
                {
                    Setting.ShowAlbumArtOnMainScreen = value;
                    if (Setting.ShowAlbumArtOnMainScreen)
                    {
                        if (Controller.GetInstance().Playing)
                            artwork.CurrentTrack = Controller.GetInstance().PlayingTrack;
                    }
                     arrangeSplitterControls();
                }
            }
        }
        public bool NondynamicPlaylistBasedView
        {
            get
            {
                string s = filterBar.ActivePlaylist;
                if (s.Length == 0)
                    return false;

                return !Database.IsPlaylistDynamic(s);
            }
        }
        public void TempDisplayTrackInfo(Track t)
        {
            if (t != null)
            {
                if (Setting.ShowAlbumArtOnMainScreen)
                {
                    if (controller.Playing)
                        artwork.TemporaryTrack = t;
                    else
                        artwork.CurrentTrack = t;

                }
                updateFilterHint(t);
                Controller.ShowMessage(t.ToString());
            }
        }
        public void ClearHoverIndex() { trackList.ClearHoverIndex(); }
        public int TrackCount { get { return trackList.Count; } }
        public void InvalidateAll()
        {
            trackList.Invalidate();
            filterValueList.Invalidate();
        }
        private ActiveListEnum ActiveList
        {
            get { return activeList; }
            set
            {
                switch (value)
                {
                    case ActiveListEnum.FilterValueList:
                        filterValueList.Active = true;
                        trackList.Active = false;
                        break;
                    case ActiveListEnum.TrackList:
                        filterValueList.Active = false;
                        trackList.Active = true;
                        break;
                }
                activeList = value;
            }
        }
        public Track FirstSelectedOrFirst { get { return trackList.FirstSelectedOrFirst; } }
        public Track CurrentTrack
        {
            set
            {
                if (Setting.ShowAlbumArtOnMainScreen)
                {
                    artwork.CurrentTrack = value;
                    Clock.DoOnMainThread(artwork.Refresh);
                    //artwork.Refresh();
                }
            }
        }

        public void ShowLetter(char Letter) { artwork.ShowLetter(Letter); }

        public void MoveTracksUp()
        {
            if (NondynamicPlaylistBasedView)
                trackList.MoveTracksUp(1);
            controller.UpdateNextUpOrRadioBitrate();
        }
        public void MoveTracksDown()
        {
            if (NondynamicPlaylistBasedView)
                trackList.MoveTracksDown(1);
            controller.UpdateNextUpOrRadioBitrate();
        }
        private void updateFilterHint(Track t)
        {
            string hint = String.Empty;

            switch (filterBar.CurrentFilterType)
            {
                case FilterType.Artist:
                    hint = t.MainGroup;
                    break;
                case FilterType.Album:
                    hint = t.Album;
                    break;
                case FilterType.Genre:
                    hint = t.Genre;
                    break;
                case FilterType.Year:
                    hint = t.YearString;
                    break;
                case FilterType.Grouping:
                    hint = t.Grouping;
                    break;
            }
            if (hint.Length > 0)
                filterValueList.Hint = hint;
        }
        public void ShowAllByType(FilterType Type)
        {
            Track t = trackList.FirstSelectedOrFirst;
            if (t != null)
            {
                string val;
                string type;
                switch (Type)
                {
                    case FilterType.Artist:
                        val = t.MainGroup;
                        type = Localization.Get(UI_Key.Filter_Artist);
                        break;
                    case FilterType.Album:
                        val = t.Album;
                        type = Localization.Get(UI_Key.Filter_Album);
                        break;
                    case FilterType.Genre:
                        val = t.Genre;
                        type = Localization.Get(UI_Key.Filter_Genre);
                        break;
                    case FilterType.Year:
                        val = t.YearString;
                        type = Localization.Get(UI_Key.Filter_Year);
                        break;
                    case FilterType.Grouping:
                        val = t.Grouping;
                        type = Localization.Get(UI_Key.Filter_Grouping);
                        break;
                    default:
                        return;
                }
                if (val.Length > 0)
                {
                    Filter(Type, val);
                    updateFilterHint(t);
                }
            }
        }
        private void filterBarFilterCleared(FilterType Type)
        {
            RefreshAll(true);
            if (trackList.HasSelectedTracks)
                updateFilterHint(trackList.SelectedTracks[0]);
        }
        public void RefreshAll(bool ReSort)
        {
            LoadFilterValues();
            RefreshTrackList(ReSort);
            controller.UpdateNextUpOrRadioBitrate();
            controller.UpdateNowPlaying();
        }
        public void FocusPanel()
        {
            if (panel != null)
                panel.Focus();
        }
        public void LoadFilterValues()
        {
            try
            {
                List<string> filterValues = null;

                FilterType Type = filterBar.CurrentFilterType;

                Track t = controller.UsersMostInterestingTrack;

                string hint = String.Empty;

                switch (Type)
                {
                    case FilterType.Playlist:
                        filterValues = filterBar.GetPlaylistFilterValues();
                        break;
                    case FilterType.Album:
                        filterValues = Database.GetAlbums(filterBar.GetTracksWithoutFiltering(Type, true));
                        if (t != null)
                            hint = t.Album;
                        break;
                    case FilterType.Artist:
                        filterValues = Database.GetMainGroups(filterBar.GetTracksWithoutFiltering(Type, true));
                        if (t != null)
                            hint = t.MainGroup;
                        break;
                    case FilterType.Genre:
                        filterValues = Database.GetGenres(filterBar.GetTracksWithoutFiltering(Type, true));
                        if (t != null)
                            hint = t.Genre;
                        break;
                    case FilterType.Year:
                        filterValues = Database.GetYears(filterBar.GetTracksWithoutFiltering(Type, true));
                        if (t != null)
                            hint = t.YearString;
                        break;
                    case FilterType.Grouping:
                        filterValues = Database.GetGroupings(filterBar.GetTracksWithoutFiltering(Type, true));
                        if (t != null)
                            hint = t.Grouping;
                        break;
                }

                filterValueList.LoadFilterValues(Type, filterValues, filterBar.CurrentFilterIsActive ? filterBar.CurrentFilterValue : String.Empty, hint);
            }
            catch { }
        }
        public void RefreshTrackList(bool ReSort)
        {
            TrackQueue queue = filterBar.GetTracks();

            if (NondynamicPlaylistBasedView || (filterBar.IsFilterActive(FilterType.Playlist) && Database.PlaylistIsPreSorted(controller.CurrentPlaylist)))
                trackList.NoSort();
            else if (ReSort && trackList.SortColumn < 0)
                trackList.SortColumn = 0;

            trackList.Queue = queue;

            controller.UpdateTrackOrStationCount();

            if (trackList.HasTracks)
            {
                if (!controller.Playing)
                    artwork.CurrentTrack = trackList[0];
            }
            else if (!controller.Playing)
            {
                artwork.CurrentTrack = null;
            }

            controller.Preload();
        }
        private void filterValueChanged(FilterType Type, string Value)
        {
            if (filterValueList.HasValue)
                filterBar.SetFilterValue(Type, Value, false);
            else
                filterBar.ReleaseFilter(Type);
        }

        public bool HasTracks { get { return trackList.HasTracks; } }
        public bool HasSelectedTracks { get { return trackList.HasSelectedTracks; } }
        public List<Track> SelectedTracks { get { return trackList.SelectedTracks; } }
        public Track this[int Index] { get { return trackList[Index]; } }
        public void ResetColumns(bool WidthOnly) { trackList.ResetColumns(WidthOnly); }
        public void MoveUp()
        {
            if (activeList == ActiveListEnum.TrackList)
            {
                if (MoveUp(1, false))
                    TempDisplayTrackInfo(trackList.FirstSelectedTrack);
            }
            else
            {
                filterValueList.ChangeFilterIndex(-1);
            }
        }
        public bool MoveUp(int Num, bool Shift)
        {
            return trackList.MoveUp(Num, Shift);
        }
        public void MoveDown()
        {
            if (ActiveList == ActiveListEnum.TrackList)
            {
                if (MoveDown(1, false))
                    TempDisplayTrackInfo(trackList.FirstSelectedTrack);
            }
            else
            {
                filterValueList.ChangeFilterIndex(1);
            }
        }
        public bool MoveDown(int Num, bool Shift)
        { return trackList.MoveDown(Num, Shift); }
        public void PageUp()
        {
            if (activeList == ActiveListEnum.TrackList)
            {
                if (trackList.PageUp())
                {
                    TempDisplayTrackInfo(trackList.FirstSelectedTrack);
                    ShowLetter(trackList.CurrentLetterIndex);
                }
            }
            else
            {
                filterValueList.PageUp();
                ShowLetter(filterValueList.CurrentLetterIndex);
            }
        }
        public void PageDown()
        {

            if (activeList == ActiveListEnum.TrackList)
            {
                if (trackList.PageDown())
                {
                    TempDisplayTrackInfo(trackList.FirstSelectedTrack);
                    ShowLetter(trackList.CurrentLetterIndex);
                }
            }
            else
            {
                filterValueList.PageDown();
                ShowLetter(filterValueList.CurrentLetterIndex);
            }
        }
        public void ShowPanel(QPanel Panel)
        {
            controller.LockForPanel();

            this.Panel = Panel;

            Panel.Focus();
        }
        public void NoSort() { trackList.NoSort(); }
        public Track FirstSelectedTrack { get { return trackList.FirstSelectedTrack; } }
        public TrackQueue Queue { get { return trackList.Queue; } set { trackList.Queue = value; } }
        public Track PlayingTrack { set { trackList.PlayingTrack = value; } }
        public void EnsurePlayingTrackVisible() { trackList.EnsurePlayingTrackVisible(); }
        public void Shuffle() { trackList.Shuffle(); }
        public void SelectAll() { trackList.SelectAll(); }
        public void SelectNone() { trackList.SelectNone(); }
        public void InvertSelection() { trackList.InvertSelection(); }
        public Track Peek(bool Loop) { return trackList.Peek(Loop); }
        public Track Advance(bool Loop) { return trackList.Advance(Loop); }
        public void EnsureVisible(Track Track) { trackList.EnsureVisible(Track); }
        public void End()
        {
            if (activeList == ActiveListEnum.TrackList)
            {
                if (trackList.End())
                    TempDisplayTrackInfo(trackList.FirstSelectedTrack);
            }
            else
            {
                filterValueList.End();
            }
        }
        public void SetTrackListActive()
        {
            ActiveList = ActiveListEnum.TrackList;
        }
        public void SetFilterValueListActive()
        {
            ActiveList = ActiveListEnum.FilterValueList;
        }
        public void Home()
        {
            if (activeList == ActiveListEnum.TrackList)
            {
                if (trackList.Home())
                    TempDisplayTrackInfo(trackList.FirstSelectedTrack);
            }
            else
            {
                filterValueList.Home();
            } 
        }
        public string SerializeColumnStatus(HTPCMode HTPCMode) { return trackList.SerializeColumnStatus(HTPCMode); }
        public void RemoveTrack(Track Track) { trackList.RemoveTrack(Track); }
        public bool HasPanel { get { return panel != null; } }
        public bool NowPlayingVisible { get { return filterBar.NowPlayingVisible; } }
        public bool AllowEvents { get { return filterBar.AllowEvents; } set { filterBar.AllowEvents = value; } }
        public void RestoreViewState(ViewState VS)
        {
            filterBar.AllowEvents = false;
            filterBar.RestoreViewState(VS);
            trackList.RestoreViewState(VS);
            filterValueList.RestoreViewState(VS);
            filterBar.AllowEvents = true;
        }
        public void SaveViewState(ViewState VS)
        {
            filterBar.SaveViewState(VS);
            trackList.SaveViewState(VS);
            filterValueList.SaveViewState(VS); }

        public bool Locked
        {
            set
            {
                filterBar.Locked = value;
                trackList.Locked = value;
                filterValueList.Locked = value;
            }
        }
        public void SelectTracklistIndexZero()
        {
            if (trackList.Count > 0)
                trackList.SelectTrack(0, true, true, false);
        }
        public void FreshView()
        {
            trackList.Active = false;
            trackList.ClearHoverIndex();
            filterValueList.Active = false;
        }
        public void RemoveTracks()
        {
            List<Track> tq =SelectedTracks;
            if (tq.Count > 0)
            {
                int i = trackList.FirstSelectedIndex;

                if (NondynamicPlaylistBasedView)
                {
                    Database.RemoveFromPlaylist(ActivePlaylist, tq);
                    foreach (Track t in tq)
                        t.Selected = false;
                }
                else
                {
                    // TODO: Localize

                    frmTaskDialog td;
                    List<frmTaskDialog.Option> options = new List<frmTaskDialog.Option>();

                    // Singular

                    if (tq.Count == 1)
                    {
                        options.Add(new frmTaskDialog.Option("Remove from library only", "Remove the track from my QuuxPlayer library but leave the file where it is on my hard drive.", 0));
                        options.Add(new frmTaskDialog.Option("Remove and recycle", "Remove the track from QuuxPlayer and also send the file to the Windows recycle bin.", 1));
                        options.Add(new frmTaskDialog.Option("Cancel", "Don't remove the file.", 2));

                        td = new frmTaskDialog("Remove Track?", "Remove \"" + tq[0].ToShortString() + "\" from library?", options);
                    }
                    else
                    {
                        options.Add(new frmTaskDialog.Option("Remove from library only", "Remove the tracks from my QuuxPlayer library but leave the files where they are on my hard drive.", 0));
                        options.Add(new frmTaskDialog.Option("Remove and recycle", "Remove the tracks from QuuxPlayer and also send the files to the Windows recycle bin.", 1));
                        options.Add(new frmTaskDialog.Option("Cancel", "Don't remove the files.", 2));

                        td = new frmTaskDialog("Remove Tracks?", "Remove " + tq.Count.ToString() + " tracks from library?", options);
                    }

                    td.ShowDialog(this);

                    switch (td.ResultIndex)
                    {
                        case 0:
                            Database.RemoveFromLibrary(tq);
                            break;
                        case 1:
                            Database.RemoveFromLibrary(tq);
                            TrackWriter.RecycleFiles(tq);
                            break;
                        default:
                            return;
                    }

                    //cmb = new QCheckedMessageBox(mainForm, text, "Remove Track?", QMessageBoxButtons.YesNo, QMessageBoxIcon.Question, QMessageBoxButton.NoCancel, Localization.Get(UI_Key.General_Also_Recycle), false);

                    //if (cmb.DialogResult == DialogResult.OK)
                    //{
                    //    Database.RemoveFromLibrary(tq);
                    //    if (cmb.Checked)
                    //    {
                    //        TrackWriter.RecycleFiles(tq);
                    //    }
                    //}
                    //else
                    //{
                    //    return;
                    //}
                    //}
                    //else
                    //{
                    //    DialogResult dr;

                    //    if (tq.Count == 1)
                    //        dr = QMessageBox.Show(mainForm, "Remove \"" + tq[0].ToShortString() + "\" from library?", "Remove Track?", QMessageBoxButtons.YesNo, QMessageBoxIcon.Question, QMessageBoxButton.NoCancel);
                    //    else
                    //        dr = QMessageBox.Show(mainForm, "Remove " + tq.Count.ToString() + " tracks from library?", "Remove Tracks?", QMessageBoxButtons.YesNo, QMessageBoxIcon.Question, QMessageBoxButton.NoCancel);

                    //    if (dr == DialogResult.OK)
                    //        Database.RemoveFromLibrary(tq);
                    //    else
                    //        return;
                    //}
                }
                RefreshAll(false);

                if (i < TrackCount)
                    SelectTrack(i, false, true, true);
                else if (TrackCount > 0)
                    SelectTrack(TrackCount - 1, false, true, true);
            }
        }
        public void SelectTrack(int Index, bool MakeCurrent, bool ClearOthers, bool WithBuffer) { trackList.SelectTrack(Index, MakeCurrent, ClearOthers, WithBuffer); }
        public string ActivePlaylist { get { return filterBar.ActivePlaylist; } }
        public bool IsFilterActive(FilterType FilterType) { return filterBar.IsFilterActive(FilterType); }
        public string GetFilterValue(FilterType FilterType) { return filterBar.GetFilterValue(FilterType); }
        public void ShowFilterIndex() { filterBar.ShowFilterIndex(); }
        public void RemoveFilterIndex() { filterBar.RemoveFilterIndex(); }
        public void ReleaseAllFilters() { filterBar.ReleaseAllFilters(); }
        public void ReleaseCurrentFilter() { filterBar.ReleaseCurrentFilter(); }
        public void ReleaseFilter(FilterType FilterType) { filterBar.ReleaseFilter(FilterType); }
        public HTPCMode HTPCMode
        {
            set
            {
                filterBar.HTPCMode = value;
                trackList.HTPCMode = value;
                filterValueList.HTPCMode = value;
            }
        }
        public void AdvanceSortColumn() { trackList.AdvanceSortColumn(); }
        public void MakeVisible(Track Track, bool Select)
        {
            if (!trackList.Queue.Contains(Track))
                ReleaseAllFilters();
                        
            trackList.MakeVisible(Track, Select);
        }
        public void ChangeFilterIndex(int NumToMove) { filterValueList.ChangeFilterIndex(NumToMove); }
        public FilterType CurrentFilterType { get { return filterBar.CurrentFilterType; } set { filterBar.CurrentFilterType = value; } }
        public string CurrentFilterValue
        {
            get { return filterBar.CurrentFilterValue; }
            set
            {
                filterBar.CurrentFilterValue = value;
            }
        }
        public string CurrentFilterName
        {
            get
            {
                return filterBar.CurrentFilterName;
            }
            set
            {
                filterBar.CurrentFilterName = value;
                RefreshAll(true);
            }
        }
        public void SetFilterValue(FilterType Playlist, string PlaylistName, bool ReleaseAllOtherFilters)
        {
            filterBar.SetFilterValue(Playlist, PlaylistName, ReleaseAllOtherFilters);
        }
        public void AdvanceFilter(bool Forward)
        {
            filterBar.AdvanceFilter(Forward);
        }
        public void FocusSearchBox() { filterBar.FocusSearchBox(); }
        public bool KeyPreview { get { return filterBar.KeyPreview || filterValueList.KeyPreview; } }
        public int FirstSelectedIndex { get { return trackList.FirstSelectedIndex; } }

        public void Filter(FilterType Type, string val)
        {
            filterBar.AllowEvents = false;
            filterBar.SetFilterValue(Type, val, true);
            filterBar.AllowEvents = true;
            RefreshAll(true);
            trackList.SortByArtist();
        }
        public void FilterArtistAndAlbum(string Artist, string Album)
        {
            filterBar.AllowEvents = false;
            filterBar.SetFilterValue(FilterType.Artist, Artist, true);
            filterBar.SetFilterValue(FilterType.Album, Album, false);
            filterBar.AllowEvents = true;
            RefreshAll(true);
            trackList.SortByArtist();
        }

        private void filterBarFilterValueChanged(FilterType Type, string Value, bool WasOff, bool StartsWith)
        {
            bool sort = !WasOff;
            
            ViewState.PreviousViewState = null;

            switch (Type)
            {
                case FilterType.Text:
                    RefreshAll(sort);
                    break;
                case FilterType.Playlist:
                    if (StartsWith)
                    {
                        RefreshAll(false);
                    }
                    else
                    {
                        string s = ActivePlaylist;

                        PlaylistType pt = Database.GetPlaylistType(s);

                        switch (pt)
                        {
                            case PlaylistType.Auto:
                                Controller.ShowMessage(Localization.Get(UI_Key.Message_Is_Auto_Playlist, s));
                                break;
                            case PlaylistType.Standard:
                                Controller.ShowMessage(Localization.Get(UI_Key.Message_Is_Standard_Playlist, s));
                                break;
                            default:
                                Controller.ShowMessage(Localization.Get(UI_Key.Message_Is_System_Playlist, s));
                                break;
                        }

                        RefreshTrackList(sort);

                        if (pt == PlaylistType.Standard)
                        {
                            controller.TargetPlaylistName = s;
                        }

                        Queue.PlaylistBasis = s;

                        controller.UpdateNowPlaying();
                        controller.UpdateNextUpOrRadioBitrate();
                    }
                    break;
                default:
                    if (StartsWith)
                    {
                        RefreshAll(false);
                    }
                    else
                    {
                        RefreshTrackList(sort);
                        controller.UpdateNextUpOrRadioBitrate();
                    }
                    break;
            }
        }
    }
}
