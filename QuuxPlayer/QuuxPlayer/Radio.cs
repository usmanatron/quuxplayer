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
    internal class Radio : QSplitContainer, IActionHandler, IMainView
    {
        public enum StationEditAction { OK, Cancel }
        public delegate void StationEditComplete(StationEditAction SEA);

        private const int MARGIN = 6;
        private const int SCROLL_BAR_WIDTH = 14;
        private static int stationCellHeight = Styles.TextHeight * 2 + 6;
        private StationPanel stationPanel;

        private static List<RadioStation> stations = new List<RadioStation>();
        private static List<RadioStation> filteredStations = new List<RadioStation>();
        private List<Cell> cells;
        private RadioStation selectedStation;
        private static RadioStation playingStation = null;
        private QTextBoxFocusOnClick txtURL;
        private QTextBox txtFilter;
        private QButton btnGo;
        private QButton btnClear;
        private static Radio instance;
        private Controller controller;
        private Point mouseDownPoint = new Point(-1, -1);

        private HTPCMode htpcMode = HTPCMode.Normal;

        private static object @lock = new object();

        private QScrollBar scrollBar;
        private RadioGenreSelectPanel genrePanel;

        private const TextFormatFlags tff = TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;
        private const TextFormatFlags tffr = tff | TextFormatFlags.Right;

        private Cell hoverCell = null;
        private int firstVisibleStation = 0;

        private bool removeHover = false;

        public Radio()
        {
            this.Visible = false;
            this.DoubleBuffered = true;

            stationPanel = new StationPanel();
            this.Panel2.Controls.Add(stationPanel);
            stationPanel.AllowDrop = true;

            instance = this;
            scrollBar = new QScrollBar(false);
            scrollBar.Width = SCROLL_BAR_WIDTH;
            scrollBar.UserScroll += new QScrollBar.ScrollDelegate(scroll);
            stationPanel.Controls.Add(scrollBar);

            txtURL = new QTextBoxFocusOnClick();
            stationPanel.Controls.Add(txtURL);
            txtURL.EnableWatermark(stationPanel, Localization.Get(UI_Key.Radio_URL_Watermark), String.Empty);
            txtURL.MaxLength = 2048;
            txtURL.Enter += (s, e) => { keyPreviewChange(); };
            txtURL.Leave += (s, e) => { keyPreviewChange(); };
            txtURL.KeyPress += (s, e) =>
                {
                    switch (e.KeyChar)
                    {
                        case '\r':
                            if (txtURL.Text.Length > 0)
                                go(btnGo);
                            e.Handled = true;
                            break;
                    }
                };

            txtFilter = new QTextBox();
            stationPanel.Controls.Add(txtFilter);
            txtFilter.EnableWatermark(stationPanel, Localization.Get(UI_Key.Radio_Filter_Watermark), String.Empty);
            txtFilter.Enter += (s, e) => { keyPreviewChange(); };
            txtFilter.Leave += (s, e) => { keyPreviewChange(); };
            txtFilter.TextChanged += (s, e) => { populateStations(); };

            btnClear = new QButton("X", false, true);
            btnClear.ButtonPressed += new QButton.ButtonDelegate(clear);
            btnClear.BackColor = stationPanel.BackColor;
            stationPanel.Controls.Add(btnClear);

            btnGo = new QButton(Localization.Get(UI_Key.Radio_Add_And_Play), false, true);
            btnGo.ButtonPressed += new QButton.ButtonDelegate(go);
            btnGo.BackColor = stationPanel.BackColor;
            stationPanel.Controls.Add(btnGo);

            genrePanel = new RadioGenreSelectPanel();
            genrePanel.Location = Point.Empty;
            genrePanel.HeaderText = Localization.Get(UI_Key.Radio_Genres);
            this.Panel1.Controls.Add(genrePanel);

            cells = new List<Cell>();
            setupCells();

            this.Panel1.Resize += (s, e) => { arrangeSelectPanel(); };
            this.Panel2.Resize += (s, e) => { arrangeStationPanel(); };

            stationPanel.MouseMove += new MouseEventHandler(stationPanelMouseMove);
            stationPanel.MouseDown += new MouseEventHandler(stationPanelMouseDown);
            stationPanel.MouseUp += new MouseEventHandler(stationPanelMouseUp);
            stationPanel.Paint += new PaintEventHandler(stationPanelPaint);
            stationPanel.MouseDoubleClick += new MouseEventHandler(stationPanelDoubleClick);
            stationPanel.MouseWheel += new MouseEventHandler(stationPanelMouseWheel);
            stationPanel.MouseEnter += (s, e) => { if (!txtURL.Focused && !txtFilter.Focused) stationPanel.Focus(); };
            stationPanel.MouseLeave += (s, e) => { hoverCell = null; };
            stationPanel.Resize += (s, e) => { setupCells(); };
            stationPanel.GotFocus += (s, e) => { stationPanel.Active = true; keyPreviewChange(); };
            stationPanel.LostFocus += (s, e) => { stationPanel.Active = false; };
            stationPanel.DragEnter += (s, e) => { onDragEnter(e); };
            stationPanel.DragDrop += (s, e) => { onDragDrop(e, true); };

            txtURL.DragEnter += (s, e) => { onDragEnter(e); };
            txtURL.DragDrop += (s, e) => { onDragDrop(e, false); };
            txtURL.AllowDrop = true;

            txtURL.Watermark.DragEnter += (s, e) => { onDragEnter(e); };
            txtURL.Watermark.DragDrop += (s, e) => { onDragDrop(e, false); };
            txtURL.Watermark.AllowDrop = true;

            genrePanel.AllowDrop = true;
            genrePanel.SelectedIndexChanged += () => { populateStations(); };
            genrePanel.ValueChanged += new QSelectPanel.ValueEditDelegate(selectPanel_ValueChanged);
            genrePanel.DragEnter += (s, e) =>
            {
                onDragEnter(e);
            };
            genrePanel.DragDrop += (s, e) => { onDragDropGenre(e); };
            this.genrePanel.MouseEnter += (s, e) => { genrePanel.Focus(); };
            this.genrePanel.GotFocus += (s, e) => { genrePanel.Active = true; };
            this.genrePanel.LostFocus += (s, e) => { genrePanel.Active = false; };
            this.genrePanel.DoubleClick += (s, e) => { genreDoubleClick(); };

            int tabIndex = 0;
            txtFilter.TabIndex = tabIndex++;
            btnClear.TabIndex = tabIndex++;
            txtURL.TabIndex = tabIndex++;
            btnGo.TabIndex = tabIndex++;
        }

        private static Brush selectedRowBrush;
        private static Brush selectedRowHoverBrush;
        private static Brush hoverBrush;
        private void setupBrushes(int Baseline)
        {
            selectedRowBrush = Style.GetSelectedRowBrush(stationCellHeight, Baseline);
            selectedRowHoverBrush = Style.GetSelectedHoverRowBrush(stationCellHeight, Baseline);
            hoverBrush = Style.GetHoverRowBrush(stationCellHeight, Baseline);
        }

        public ViewType ViewType { get { return ViewType.Radio; } }

        public bool Locked { get; set; }
        public bool AllowTagEditing
        {
            get { return SelectedStation != null; }
        }
        private void keyPreviewChange()
        {
            controller.RequestAction(QActionType.KeyPreviewChange);
        }
        public HTPCMode HTPCMode
        {
            get { return htpcMode; }
            set
            {
                if (htpcMode != value)
                {
                    htpcMode = value;
                    genrePanel.HTPCMode = value;
                    stationPanel.Invalidate();
                }
            }

        }
        public void PageUp()
        {
            moveUp(Int32.MaxValue);
        }
        public void MoveUp()
        {
            moveUp(1);
        }
        private void moveUp(int Num)
        {
            if (genrePanel.Focused)
            {
                if (Num == Int32.MaxValue)
                    genrePanel.PageUp();
                else
                    genrePanel.ChangeFilterIndex(-1);
            }
            else
            {
                if (SelectedStation == null || !filteredStations.Contains(SelectedStation))
                {
                    SelectedStation = filteredStations.LastOrDefault();
                }
                else
                {
                    if (Num == Int32.MaxValue)
                        Num = cells.Count;

                    int i = filteredStations.IndexOf(SelectedStation) - Num;
                    if (i > 0)
                    {
                        SelectedStation = filteredStations[i - 1];
                    }
                    else
                    {
                        SelectedStation = filteredStations.FirstOrDefault();
                    }
                }
                ensureSelectedStationVisible();
            }
        }
        public ActionHandlerType Type { get { return ActionHandlerType.Radio; } }
        public void RequestAction(QActionType Type)
        {
            switch (Type)
            {
                case QActionType.Play:
                case QActionType.PlaySelectedTracks:
                case QActionType.PlayThisAlbum:
                    InvokeSelectedStation();
                    break;
                case QActionType.MoveTracksDown:
                case QActionType.PageDown:
                    PageDown();
                    break;
                case QActionType.MoveDown:
                case QActionType.SelectNextItemGamePadRight:
                    MoveDown();
                    break;
                case QActionType.MoveTracksUp:
                case QActionType.PageUp:
                    PageUp();
                    break;
                case QActionType.MoveUp:
                case QActionType.SelectPreviousItemGamePadRight:
                    MoveUp();
                    break;
                case QActionType.Home:
                    home();
                    break;
                case QActionType.End:
                    end();
                    break;
                case QActionType.MoveLeft:
                    genrePanel.Focus();
                    break;
                case QActionType.MoveRight:
                    stationPanel.Focus();
                    break;
                case QActionType.FocusSearchBox:
                    txtFilter.Focus();
                    break;
                case QActionType.ExportPlaylist:
                    break;
                case QActionType.FindPlayingTrack:
                    SelectedStation = playingStation;
                    ensureSelectedStationVisible();
                    break;
                case QActionType.EditTags:
                    editStationDetails();
                    break;
                case QActionType.SelectNextItemGamePadLeft:
                    genrePanel.ChangeFilterIndex(1);
                    break;
                case QActionType.SelectPreviousItemGamePadLeft:
                    genrePanel.ChangeFilterIndex(-1);
                    break;
                case QActionType.ReleaseAllFilters:
                case QActionType.ReleaseCurrentFilter:
                    txtFilter.Text = String.Empty;
                    genrePanel.Value = String.Empty;
                    populateStations();
                    ensureSelectedStationVisible();
                    break;
                case QActionType.NextFilter:
                case QActionType.PreviousFilter:
                case QActionType.LoadFilterValues:
                case QActionType.Next:
                case QActionType.Previous:
                case QActionType.PlaySelectedTrackNext:
                case QActionType.PlayRandomAlbum:
                case QActionType.ScanBack:
                case QActionType.ScanFwd:
                case QActionType.SelectAll:
                case QActionType.SelectNone:
                case QActionType.InvertSelection:
                case QActionType.Shuffle:
                case QActionType.AdvanceSortColumn:
                case QActionType.RepeatToggle:
                case QActionType.ExportCSV:
                case QActionType.ExportCurrentView:
                case QActionType.PlayFirstTrack:
                case QActionType.RequestNextTrack:
                case QActionType.ShowFileDetails:
                case QActionType.ShowFilterIndex:
                    // suppress
                    break;
                default:
                    controller.RequestActionNoRedirect(Type);
                    break;
            }
        }
        public void RequestAction(QAction Action)
        {
            RequestAction(Action.Type);
        }
        private RadioEditPanel rep = null;
        private void editStationDetails()
        {
            if (SelectedStation != null)
            {
                List<string> genres = (from s in stations
                                       select s.Genre).Distinct().ToList();
                
                genres.Sort(StringComparer.OrdinalIgnoreCase);
                
                rep = new RadioEditPanel(SelectedStation, genres.ToArray(), doneEditingStation);
                keyPreviewChange();
                arrangeStationPanel();
                controller.LockForPanel();
                this.Panel2.Controls.Add(rep);
                rep.Focus();
            }
        }
        private void doneEditingStation(Radio.StationEditAction SEA)
        {
            if (SEA == StationEditAction.OK)
            {
                sort();
                populateStations();
                populateGenres();
                invalidateAll();
                Clock.DoOnMainThread(instance.ensureSelectedStationVisible, 30);
            }
            this.Panel2.Controls.Remove(rep);
            rep = null;
            keyPreviewChange();
            arrangeStationPanel();
            controller.UnlockForPanel();
        }
        public void PageDown()
        {
            moveDown(Int32.MaxValue);
        }
        public void MoveDown()
        {
            moveDown(1);
        }
        private void home()
        {
            if (genrePanel.Focused)
            {
                genrePanel.Home();
            }
            else
            {
                if (filteredStations.Count > 0)
                {
                    SelectedStation = filteredStations[0];
                    ensureSelectedStationVisible();
                }
            }
        }
        private void end()
        {
            if (genrePanel.Focused)
            {
                genrePanel.End();
            }
            else
            {
                if (filteredStations.Count > 0)
                {
                    SelectedStation = filteredStations.Last();
                    ensureSelectedStationVisible();
                }
            }
        }
        private void moveDown(int Num)
        {
            if (genrePanel.Focused)
            {
                if (Num == Int32.MaxValue)
                    genrePanel.PageDown();
                else
                    genrePanel.ChangeFilterIndex(Num);
            }
            else
            {
                if (SelectedStation == null || !filteredStations.Contains(SelectedStation))
                {
                    SelectedStation = filteredStations.FirstOrDefault();
                }
                else
                {
                    if (Num == Int32.MaxValue)
                        Num = cells.Count;
                    int i = filteredStations.IndexOf(SelectedStation) + Num;
                    if (i < filteredStations.Count)
                    {
                        SelectedStation = filteredStations[i];
                    }
                    else
                    {
                        SelectedStation = filteredStations.LastOrDefault();
                    }
                }
                ensureSelectedStationVisible();
            }
        }
        private void selectPanel_ValueChanged(string OldValue, string NewValue)
        {
            lock (@lock)
            {
                bool select = genrePanel.Value == OldValue;
                foreach (RadioStation rs in stations)
                {
                    if (String.Compare(rs.Genre, OldValue, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        rs.Genre = NewValue;
                    }
                }
                populateGenres();
                if (select)
                {
                    genrePanel.Value = NewValue;
                    populateStations();
                }
            }
            Clock.DoOnMainThread(invalidateAll);
        }
        public Controller Controller
        {
            set
            {
                controller = value;
                genrePanel.Controller = value;
            }
        }
        private void arrangeSelectPanel()
        {
            genrePanel.Size = this.Panel1.ClientSize;
        }
        private void arrangeStationPanel()
        {
            if (rep == null)
            {
                stationPanel.Size = this.Panel2.ClientSize;
            }
            else
            {
                stationPanel.Size = new Size(this.Panel2.ClientRectangle.Width, this.Panel2.ClientRectangle.Height - rep.Height);
                rep.Bounds = new Rectangle(stationPanel.Left, stationPanel.Bottom, stationPanel.Width, this.Panel2.ClientRectangle.Height - stationPanel.Height);
                rep.Focus();
            }
            setupCells();
        }
        private void scroll(QScrollBar Sender, int Value)
        {
            FirstVisibleStation = scrollBar.Value;
        }

        public void RenameSelectedGenre()
        {
            genrePanel.StartItemEdit();
            Clock.DoOnMainThread(genrePanel.FocusRenameBox, 30);
        }
        public void InvokeSelectedStation()
        {
            if (SelectedStation != null)
            {
                Controller.GetInstance().RequestAction(QActionType.UpdateRadioStation);
                txtURL.Text = SelectedStation.URL;
             
                ensureSelectedStationVisible();
            }
        }
        public void ReloadStations()
        {
            List<frmTaskDialog.Option> options = new List<frmTaskDialog.Option>();
            options.Add(new frmTaskDialog.Option("Add back default stations", "Leave any additions or changes I have made in place.", 0));
            options.Add(new frmTaskDialog.Option("Replace all existing stations with defaults", "The station list will revert to the original list.", 1));
            options.Add(new frmTaskDialog.Option("Cancel", "Don't change my station list.", 2));

            frmTaskDialog td = new frmTaskDialog(Localization.Get(UI_Key.Radio_Restore_Default_Stations_Title),
                Localization.Get(UI_Key.Radio_Restore_Default_Stations),
                options);

            td.ShowDialog(this);

            switch (td.ResultIndex)
            {
                case 0:
                    RadioStations = RadioStation.DefaultList.Union(RadioStations).ToList();
                    break;
                case 1:
                    RadioStations = RadioStation.DefaultList;
                    break;
                default:
                    return;
            }

            //QCheckedMessageBox mb = new QCheckedMessageBox(this,
            //                                               Localization.Get(UI_Key.Radio_Restore_Default_Stations),
            //                                               Localization.Get(UI_Key.Radio_Restore_Default_Stations_Title),
            //                                               QMessageBoxButtons.OKCancel,
            //                                               QMessageBoxIcon.Question,
            //                                               QMessageBoxButton.YesOK,
            //                                               Localization.Get(UI_Key.Radio_Restore_Default_Stations_Checkbox),
            //                                               false);
            //if (mb.DialogResult == DialogResult.OK)
            //{
            //    if (mb.Checked)
            //    {
            //        RadioStations = RadioStation.DefaultList;
            //    }
            //    else
            //    {
            //        RadioStations = RadioStation.DefaultList.Union(RadioStations).ToList();
            //    }

                SelectedStation = null;

                txtFilter.Text = String.Empty;
                genrePanel.Value = String.Empty;

                sort();
                populateStations();
                populateGenres();
                invalidateAll();
            //}
        }
        private RadioStation SelectedStation
        {
            get { return selectedStation; }
            set
            {
                if (!object.Equals(selectedStation, value))
                {
                    selectedStation = value;

                    if (value != null)
                        txtURL.Text = value.URL;
                    
                    stationPanel.Invalidate();

                    if (selectedStation != null)
                    {
                        genrePanel.Hint = selectedStation.Genre;
                        Controller.ShowMessage(selectedStation.Name);
                    }
                    else
                    {
                        genrePanel.Hint = String.Empty;
                    }
                }
            }
        }
        private void ensureSelectedStationVisible()
        {
            if (SelectedStation != null)
            {
                int i = filteredStations.IndexOf(SelectedStation);

                if (i < FirstVisibleStation)
                    FirstVisibleStation = i;
                else if (i > FirstVisibleStation + cells.Count - 1)
                    FirstVisibleStation = i - cells.Count + 1;
            }
        }
        public void genreDoubleClick()
        {
            if (!Locked)
            {
                if (selectedStation == null)
                {
                    if (filteredStations.Count == 0)
                        return;

                    SelectedStation = filteredStations[0];
                }
                InvokeSelectedStation();
            }
        }
        private bool RemoveHover
        {
            get { return removeHover; }
            set
            {
                if (removeHover != value)
                {
                    removeHover = value;
                    this.Invalidate();
                }
            }
        }
        private void removeStation(RadioStation RS)
        {
            if (RS != null)
            {
                lock (@lock)
                {
                    stations.Remove(RS);
                    filteredStations.Remove(RS);
                }
                if (RS.Equals(SelectedStation))
                    SelectedStation = null;

                populateStations();
                populateGenres();
            }
        }
        private void removeStation(Cell C)
        {
            if (C != null && C.Station != null)
                removeStation(C.Station);
        }
        private static void sort()
        {
            lock (@lock)
            {
                stations.Sort((a, b) => (String.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)));
            }
        }
        public static void AddStation(string URL, string Name, bool Play)
        {
            RadioStation rs;

            lock (@lock)
            {
                rs = stations.FirstOrDefault(s => String.Compare(s.URL, URL, StringComparison.OrdinalIgnoreCase) == 0);
            }
            if (rs == null)
            {
                rs = new RadioStation(URL, Name);
                lock (@lock)
                {
                    stations.Add(rs);
                }
            }
            else if (rs.Name.Length == 0)
            {
                rs.Name = Name;
            }

            instance.SelectedStation = rs;
            
            sort();
            
            instance.populateStations();

            if (!filteredStations.Contains(rs))
            {
                instance.genrePanel.Value = String.Empty;
                instance.txtFilter.Text = String.Empty;
                instance.populateStations();
            }
            instance.populateGenres();

            instance.SelectedStation = rs;
            
            if (Play)
            {
                instance.InvokeSelectedStation();
            }
            instance.ensureSelectedStationVisible();
        }
        public static void StationNameChanged()
        {
            sort();
            instance.populateStations();
            InvalidateInstance();
            Clock.DoOnMainThread(instance.ensureSelectedStationVisible, 30);
        }
        public static void StationGenreChanged()
        {
            instance.populateGenres();
            InvalidateInstance();
        }
        public static RadioStation PlayingStation
        {
            get { return playingStation; }
            set
            {
                if (!object.Equals(value, playingStation))
                {
                    playingStation = value;
                    InvalidateInstance();
                }
            }
        }
        public static void InvalidateInstance()
        {
            Clock.DoOnMainThread(instance.invalidateAll);
        }
        public static List<RadioStation> RadioStations
        {
            get { return stations; }
            set
            {
                instance.setStations(value);
            }
        }
        public int NumStationsDisplayed
        {
            get { return filteredStations.Count; }
        }
        private void invalidateAll()
        {
            this.Invalidate(true);
            controller.RequestAction(QActionType.DisplayRadioInfo);
        }
        private void setStations(List<RadioStation> Stations)
        {
            stations = Stations;
            sort();

            populateGenres();
            populateStations();
        }

        private void populateGenres()
        {
            FirstVisibleStation = 0;
            List<string> genres;
            lock (@lock)
            {
                genres = (from s in stations
                          group s by s.Genre
                              into g
                              orderby g.Key
                              select g.Key).ToList();
            }
            genrePanel.LoadValues(genres, String.Empty, String.Empty, Localization.Get(UI_Key.Filter_Value_List_Any, "Genre"));
        }
        private void populateStations()
        {
            lock (@lock)
            {
                string genre = genrePanel.Value;

                string filter = txtFilter.Text.Trim();

                if (filter.Length > 0)
                {
                    if (genre.Length > 0)
                        filteredStations = stations.FindAll(ss => ss.Genre == genre && ss.Matches(filter));
                    else
                        filteredStations = stations.FindAll(ss => ss.Matches(filter));

                }
                else
                {
                    if (genre.Length > 0)
                        filteredStations = stations.FindAll(ss => ss.Genre == genre);
                    else
                        filteredStations = stations;
                }
            }

            firstVisibleStation = Math.Max(0, Math.Min(FirstVisibleStation, filteredStations.Count - cells.Count));

            int i = FirstVisibleStation;
            int j = 0;

            while (j < cells.Count)
            {
                if (filteredStations.Count > i)
                    cells[j++].Station = filteredStations[i++];
                else
                    cells[j++].Station = null;
            }

            if (SelectedStation != null && !filteredStations.Contains(SelectedStation))
                SelectedStation = null;

            scrollBar.Value = FirstVisibleStation;
            scrollBar.Max = Math.Max(0, filteredStations.Count - cells.Count);
            scrollBar.LargeChange = cells.Count - 1;
            
            Clock.DoOnMainThread(invalidateAll);
        }
        private void go(QButton Button)
        {
            AddStation(txtURL.Text.Trim(), String.Empty, true);
        }
        private void clear(QButton Button)
        {
            genrePanel.Value = String.Empty;
            if (txtFilter.Text.Length > 0)
                txtFilter.Text = String.Empty;
            else
                populateStations();
        }
        public RadioStation Station
        {
            get
            {
                return SelectedStation;
            }
        }
        public bool KeyPreview
        {
            get { return this.Visible && (txtURL.Focused || txtFilter.Focused || rep != null || genrePanel.KeyPreview); }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            if (this.Visible)
                this.SplitterDistance = Setting.SplitterDistance;

            base.OnVisibleChanged(e);
            Controller c = Controller.GetInstance();
            if (c != null)
                c.RequestAction(QActionType.KeyPreviewChange);

            if (this.Visible)
            {
                stationPanel.Focus();
            }
        }
        
        private void stationPanelPaint(object sender, PaintEventArgs e)
        {
            for (int i = 0; i < cells.Count && cells[i].Station != null; i++)
            {
                if (cells[i].Station.Equals(SelectedStation))
                {
                    if (cells[i].Equals(hoverCell))
                    {
                        e.Graphics.FillRectangle(selectedRowHoverBrush, cells[i].MainRectangle);
                    }
                    else
                    {
                        e.Graphics.FillRectangle(selectedRowBrush, cells[i].MainRectangle);
                    }
                }
                else if (cells[i].Equals(hoverCell))
                {
                    e.Graphics.FillRectangle(hoverBrush, hoverCell.MainRectangle);
                }
                renderStation(e.Graphics, cells[i], cells[i].Station);
            }
        }
        private int distSqr(Point P1, Point P2)
        {
            return (P1.X - P2.X) * (P1.Y - P2.Y);
        }
        private void stationPanelMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (mouseDownPoint.X >= 0 && distSqr(e.Location, mouseDownPoint) > 125)
                {
                    mouseDownPoint = new Point(-1, -1);
                    stationPanel.DoDragDrop(selectedStation.URL, DragDropEffects.Copy);
                }
            }
            else
            {
                Cell c = getCellAtPoint(e.Location);

                bool rh = hoverCell != null && hoverCell.RemoveRectangle.Contains(e.Location);

                if (!object.Equals(c, hoverCell) || rh != removeHover)
                {
                    hoverCell = c;
                    removeHover = rh;
                    stationPanel.Invalidate();
                }
                stationPanel.Cursor = rh ? Cursors.Hand : Cursors.Default;
            }
        }
        private void stationPanelMouseUp(object sender, MouseEventArgs e)
        {
            mouseDownPoint = new Point(-1, -1);
        }
        private void stationPanelMouseDown(object sender, MouseEventArgs e)
        {
            if (!Locked)
            {
                mouseDownPoint = e.Location;
                Cell c = getCellAtPoint(e.Location);
                if (c != null && c.Station != null)
                {
                    SelectedStation = c.Station;
                    if (e.Button == MouseButtons.Left && c.RemoveRectangle.Contains(e.Location))
                        removeStation(c);
                }
                if (e.Button == MouseButtons.Right)
                {
                    if (!Locked)
                    {
                        if (e.Button == MouseButtons.Right)
                        {
                            ContextMenuStrip cms = new ContextMenuStrip();
                            cms.Renderer = new MenuItemRenderer();
                            ToolStripMenuItem tsi;
                            ToolStripSeparator tss;

                            tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Radio_Play));
                            tsi.ShortcutKeyDisplayString = "P";
                            tsi.Click += (s, ee) => { controller.RequestAction(QActionType.Play); };
                            cms.Items.Add(tsi);

                            tss = new ToolStripSeparator();
                            cms.Items.Add(tss);

                            tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Radio_Edit_Station));
                            tsi.ShortcutKeyDisplayString = "Shift+Enter";
                            tsi.Click += (s, ee) => { editStationDetails(); };
                            cms.Items.Add(tsi);

                            tss = new ToolStripSeparator();
                            cms.Items.Add(tss);

                            tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Radio_Remove_Station));
                            tsi.Click += (s, ee) => { removeStation(SelectedStation); };
                            cms.Items.Add(tsi);

                            cms.Show(stationPanel, e.Location);
                        }
                    }
                }
                stationPanel.Focus();
            }
        }
        private void stationPanelDoubleClick(object sender, MouseEventArgs e)
        {
            if (!Locked)
                InvokeSelectedStation();
        }
        private void stationPanelMouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
                FirstVisibleStation -= 3;
            else
                FirstVisibleStation += 3;
        }

        private Cell getCellAtPoint(Point P)
        {
            return cells.FirstOrDefault(r => r.Contains(P));
        }
        
        private int FirstVisibleStation
        {
            get { return firstVisibleStation; }
            set
            {
                int val = Math.Max(0, Math.Min(filteredStations.Count - cells.Count + 1, value));

                if (firstVisibleStation != val)
                {
                    firstVisibleStation = val;
                    scrollBar.Value = val;
                    hoverCell = null;
                    populateStations();
                }
            }
        }
        private void setupCells()
        {
            Size sz = stationPanel.ClientSize;
            scrollBar.Bounds = new Rectangle(sz.Width - SCROLL_BAR_WIDTH - 1,
                                             0,
                                             SCROLL_BAR_WIDTH,
                                             sz.Height);

            txtFilter.Bounds = new Rectangle(MARGIN, MARGIN, sz.Width / 5, txtFilter.Height);
            btnClear.Location = new Point(txtFilter.Right + MARGIN, MARGIN);
            btnGo.Location = new Point(scrollBar.Left - btnGo.Width - MARGIN, MARGIN);
            
            txtURL.Bounds = new Rectangle(btnClear.Right + MARGIN, MARGIN, btnGo.Left - MARGIN - MARGIN - btnClear.Right, txtURL.Height);

            cells.Clear();

            int y = txtURL.Bottom + MARGIN;

            setupBrushes(y);

            int w = scrollBar.Left - MARGIN - MARGIN;

            int totalHeight = sz.Height - stationCellHeight;

            while (y < totalHeight)
            {
                Rectangle r = new Rectangle(MARGIN, y, w, stationCellHeight);

                cells.Add(new Cell(r));
                y = r.Bottom;
            }
            
            populateStations();
        }
        private void renderStation(Graphics g, Cell Cell, RadioStation Station)
        {
            if (Station != null)
            {
                string name = Station.Name;

                if (name.Length == 0)
                    name = Localization.Get(UI_Key.Radio_Blank_Name);

                Color c = (Station.Equals(playingStation)) ? Styles.Playing : Styles.LightText;

                if (htpcMode == HTPCMode.HTPC)
                {
                    TextRenderer.DrawText(g, name, Styles.FontHTPC, Cell.MainRectangle, c, tff);
                }
                else
                {
                    TextRenderer.DrawText(g, name, Styles.FontBold, Cell.FirstRow, c, tff);
                }
                
                if (this.HTPCMode == HTPCMode.Normal)
                {
                    TextRenderer.DrawText(g, Station.Genre, Styles.Font, Cell.FirstRow, Styles.LightText, tffr);

                    if (Cell.Equals(hoverCell))
                    {
                        TextRenderer.DrawText(g, Station.URL, Styles.FontItalic, Cell.SecondRow, Styles.LightText, tff);
                        if (object.Equals(Cell.Station, SelectedStation))
                            if (removeHover)
                                TextRenderer.DrawText(g, "Remove", Styles.FontUnderline, Cell.SecondRow, Styles.LightText, tffr);
                            else
                                TextRenderer.DrawText(g, "Remove", Styles.Font, Cell.SecondRow, Styles.LightText, tffr);
                        else
                            if (removeHover)
                                TextRenderer.DrawText(g, "Remove", Styles.FontUnderline, Cell.SecondRow, Styles.VeryLight, tffr);
                            else
                                TextRenderer.DrawText(g, "Remove", Styles.Font, Cell.SecondRow, Styles.Light, tffr);
                    }
                    else
                    {
                        TextRenderer.DrawText(g, Station.URL, Styles.FontItalic, Cell.SecondRow, Styles.Light, tff);
                        if (Station.BitRate > 0)
                        {
                            if (Cell.Equals(hoverCell))
                                TextRenderer.DrawText(g, Station.BitRateString, Styles.Font, Cell.SecondRow, Styles.Medium, tffr);
                            else
                                TextRenderer.DrawText(g, Station.BitRateString, Styles.Font, Cell.SecondRow, Styles.LightText, tffr);
                        }
                    }
                }
            }

        }
        private void onDragEnter(DragEventArgs drgevent)
        {
            base.OnDragEnter(drgevent);

            if (!Locked &&
                drgevent.Data.GetDataPresent(DataFormats.Text) &&
                drgevent.Data.GetData(DataFormats.Text).ToString().StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                drgevent.Effect = DragDropEffects.Copy | DragDropEffects.Link;
            }
            else
            {
                drgevent.Effect = DragDropEffects.None;
            }
        }
        private void onDragDropGenre(DragEventArgs drgevent)
        {
            if (!Locked)
            {
                if (onDragDrop(drgevent, false))
                {
                    string genre = genrePanel.GetValueAt(genrePanel.PointToClient(new Point(drgevent.X, drgevent.Y)).Y);
                    if (genre.Length > 0)
                    {
                        string url = drgevent.Data.GetData(DataFormats.Text).ToString();
                        RadioStation rs = stations.FirstOrDefault(s => String.Compare(s.URL, url, StringComparison.OrdinalIgnoreCase) == 0);
                        if (rs != null)
                        {
                            rs.Genre = genre;
                            populateGenres();
                        }
                    }
                }
            }
        }
        private bool onDragDrop(DragEventArgs drgevent, bool Play)
        {
            base.OnDragDrop(drgevent);
            string url;
            if (
            (drgevent.Data.GetDataPresent(DataFormats.Text) &&
             (url = drgevent.Data.GetData(DataFormats.Text).ToString()).StartsWith("http", StringComparison.OrdinalIgnoreCase)))
            {
                Radio.AddStation(url, String.Empty, Play);
                return true;
            }
            else
            {
                return false;
            }
        }
        private class Cell
        {
            public Rectangle MainRectangle = Rectangle.Empty;
            public Rectangle FirstRow = Rectangle.Empty;
            public Rectangle SecondRow = Rectangle.Empty;
            public RadioStation Station = null;

            private static int removeWidth = 50;

            public Cell(Rectangle Rectangle, RadioStation Station) : this(Rectangle)
            {
                this.Station = Station;
            }
            public Cell(Rectangle Rectangle)
            {
                MainRectangle = Rectangle;
                FirstRow = new Rectangle(MainRectangle.X, MainRectangle.Y + 3, MainRectangle.Width, Styles.TextHeight);
                SecondRow = new Rectangle(MainRectangle.Left, FirstRow.Bottom, MainRectangle.Width, Styles.TextHeight);
            }
            static Cell()
            {
                removeWidth = TextRenderer.MeasureText("Remove", Styles.FontUnderline).Width;
            }
            public bool Contains(Point P)
            {
                return MainRectangle.Contains(P);
            }
            public Rectangle RemoveRectangle
            {
                get
                {
                    return new Rectangle(this.SecondRow.Right - removeWidth,
                                         this.SecondRow.Top,
                                         removeWidth,
                                         this.SecondRow.Height);
                }
            }
        }
        private class StationPanel : QControl
        {
            private bool active = false;
            public bool Active
            {
                get { return active; }
                set
                {
                    if (active != value)
                    {
                        this.active = value;
                        this.Invalidate();
                    }
                }
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                if (active)
                    e.Graphics.Clear(Styles.ActiveBackground);

                base.OnPaint(e);
               
                e.Graphics.DrawRectangle(Styles.DarkBorderPen, new Rectangle(0, 0, this.ClientRectangle.Width - SCROLL_BAR_WIDTH - 1, this.ClientRectangle.Height - 1));
            }
            
        }
    }
}
