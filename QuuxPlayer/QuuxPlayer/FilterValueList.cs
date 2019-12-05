/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal sealed class FilterValueList : QSelectPanel
    {
        public delegate void FilterValueSelect(FilterType Type, string Value);
        public delegate void DragDropDelegate(string FilterValue, DragEventArgs Args);
        public event FilterValueSelect FilterValueSelected = null;

        private FilterType filterType = FilterType.None;

        private Dictionary<FilterType, String> filterTypes;
        
        public FilterValueList()
        {
            filterTypes = new Dictionary<FilterType, string>();
            filterTypes.Add(FilterType.Playlist, Localization.Get(UI_Key.Filter_Playlist));
            filterTypes.Add(FilterType.Genre, Localization.Get(UI_Key.Filter_Genre));
            filterTypes.Add(FilterType.Artist, Localization.Get(UI_Key.Filter_Artist));
            filterTypes.Add(FilterType.Album, Localization.Get(UI_Key.Filter_Album));
            filterTypes.Add(FilterType.Year, Localization.Get(UI_Key.Filter_Year));
            filterTypes.Add(FilterType.Grouping, Localization.Get(UI_Key.Filter_Grouping));

            this.AllowDrop = true;
        }

        protected override Func<string, char> GetFilterChar()
        {
            return (this.filterType == FilterType.Artist || this.filterType == FilterType.Album) ?
                                              (new Func<string, char>(s => Lib.FirstCharNoTheUpper(s))) :
                                              (new Func<string, char>(s => Lib.FirstCharUpper(s)));
        }
        protected override void OnSelectedIndexChanged(int Value)
        {
            if (!loading && FilterValueSelected != null)
            {
                FilterValueSelected.Invoke(filterType, Values[Value]);
            }
        }
        
        public char CurrentLetterIndex
        {
            get
            {
                return Lib.FirstCharNoTheUpper(this.Value);
            }
        }
        
        public void LoadFilterValues(FilterType Type, List<string> Values, string Value, string Hint)
        {
            base.LoadValues(Values, Value, Hint, Localization.Get(UI_Key.Filter_Value_List_Any, filterTypes[Type]));
            filterType = Type;
            HeaderText = filterTypes[Type] + " (" + Values.Count().ToString() + ")";
            this.Invalidate();
        }
        
        public void SaveViewState(ViewState VS)
        {
            VS.FirstVisibleFilterItem = FirstVisibleItem;
        }
        public void RestoreViewState(ViewState VS)
        {
            FirstVisibleItem = VS.FirstVisibleFilterItem;
        }
       
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (!Locked)
            {
                if (e.Button == MouseButtons.Right)
                {
                    ContextMenuStrip cms = new ContextMenuStrip();
                    cms.Renderer = new MenuItemRenderer();
                    ToolStripMenuItem tsi;
                    ToolStripSeparator tss;

                    tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Filter_Value_List_Play));
                    tsi.Click += (s, ee) => { controller.RequestAction(QActionType.PlayFirstTrack); };
                    cms.Items.Add(tsi);

                    if (filterType == FilterType.Playlist)
                    {
                        tss = new ToolStripSeparator();
                        cms.Items.Add(tss);

                        tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Filter_Value_List_Add_Playlist));
                        tsi.Click += (s, ee) =>
                        {
                            string[] vals = Values.ToArray();
                            controller.RequestAction(QActionType.CreateNewPlaylist);
                        };
                        cms.Items.Add(tsi);

                        bool editable = Editable;

                        tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Filter_Value_List_Rename_Playlist));
                        tsi.Click += (s, ee) => { controller.RequestAction(QActionType.RenameSelectedPlaylist); };
                        tsi.Enabled = editable;
                        cms.Items.Add(tsi);

                        tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Filter_Value_List_Remove_Playlist));
                        tsi.Click += (s, ee) =>
                        {
                            if (QMessageBox.Show(this,
                                                 Localization.Get(UI_Key.Filter_Value_List_Remove_Playlist_Dialog, Values[SelectedIndex]),
                                                 Localization.Get(UI_Key.Filter_Value_List_Remove_Playlist_Dialog_Title),
                                                 QMessageBoxButtons.OKCancel,
                                                 QMessageBoxIcon.Question,
                                                 QMessageBoxButton.NoCancel)
                                                    == DialogResult.OK)
                                controller.RequestAction(QActionType.RemoveSelectedPlaylist);
                        };
                        tsi.Enabled = editable;
                        cms.Items.Add(tsi);

                        PlaylistType pt = Database.GetPlaylistType(Values[SelectedIndex]);

                        if (editable)
                        {
                            tsi = new ToolStripMenuItem((pt == PlaylistType.Auto) ? Localization.Get(UI_Key.Filter_Value_List_Edit_Auto_Playlist) : Localization.Get(UI_Key.Filter_Value_List_Convert_To_Auto_Playlist));
                            tsi.Click += (s, ee) => { controller.RequestAction(QActionType.EditAutoPlaylist); };
                            cms.Items.Add(tsi);
                            if (pt == PlaylistType.Auto)
                            {
                                tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Filter_Value_List_Convert_To_Standard_Playlist));
                                tsi.Click += (s, ee) => { controller.RequestAction(QActionType.ConvertToStandardPlaylist); };
                                cms.Items.Add(tsi);
                            }
                        }

                        tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Filter_Value_List_Export_Playlist));
                        tsi.Click += (s, ee) =>
                        {
                            controller.RequestAction(QActionType.ExportPlaylist);
                        };
                        cms.Items.Add(tsi);

                        switch (pt)
                        {
                            case PlaylistType.Standard:
                            case PlaylistType.Auto:

                                tss = new ToolStripSeparator();
                                cms.Items.Add(tss);

                                tsi = new ToolStripMenuItem("Send to iTunes...");
                                tsi.Click += (s, ee) =>
                                {
                                    try
                                    {
                                        iTunes.CreatePlaylist(Values[SelectedIndex], Database.GetPlaylistTracks(Values[SelectedIndex]));
                                    }
                                    catch
                                    {
                                        iTunes.ShowError();
                                    }
                                };
                                tsi.Enabled = !iTunes.Busy;
                                cms.Items.Add(tsi);

                                if (iTunes.Busy)
                                {
                                    tsi = new ToolStripMenuItem("Stop sending to iTunes...");
                                    tsi.Click += (s, ee) =>
                                    {
                                        iTunes.Cancel = true;
                                    };
                                    cms.Items.Add(tsi);
                                }

                                break;
                        }
                    }
                    if (SelectedIndex > ANY_INDEX)
                    {
                        tss = new ToolStripSeparator();
                        cms.Items.Add(tss);

                        tsi = new ToolStripMenuItem(Localization.Get(UI_Key.Filter_Value_List_Release_Filter));
                        tsi.Click += (s, ee) => controller.RequestAction(QActionType.ReleaseCurrentFilter);

                        cms.Items.Add(tsi);
                    }

                    cms.Show(this, e.Location);
                }
            }
        }
        
        protected override void OnDoubleClick(EventArgs e)
        {
            if (!Locked)
                controller.RequestAction(QActionType.PlayFirstTrack);
        }
        
        protected override void OnDragOver(DragEventArgs drgevent)
        {
            base.OnDragOver(drgevent);

            UpdateHoverIndex(this.PointToClient(new Point(drgevent.X, drgevent.Y)).Y);

            if ((HoverIndex > ANY_INDEX) &&
                (drgevent.Data.GetDataPresent(DataFormats.FileDrop)) &&
                (this.filterType == FilterType.Playlist) &&
                (!Database.IsPlaylistDynamic(Values[HoverIndex])))
            {
                drgevent.Effect = DragDropEffects.Copy;
            }
            else
            {
                drgevent.Effect = DragDropEffects.None;
            }
        }
        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            base.OnDragDrop(drgevent);

            UpdateHoverIndex(this.PointToClient(new Point(drgevent.X, drgevent.Y)).Y);

            if (drgevent.Data.GetDataPresent("FileDrop"))
            {
                if ((this.filterType == FilterType.Playlist) && (HoverIndex > ANY_INDEX))
                {
                    string playlist = Values[HoverIndex];
                    if (!Database.IsPlaylistDynamic(playlist))
                    {
                        string[] s = (string[])drgevent.Data.GetData("FileDrop");
                        controller.AddToLibraryOrPlaylist(s, Values[HoverIndex]);
                    }
                }
            }
        }
        
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            this.Active = true;
        }
        
        protected override bool Editable
        {
            get
            {
                if (this.filterType == FilterType.Playlist)
                {
                    PlaylistType pt = Database.GetPlaylistType(Value);
                    return (pt == PlaylistType.Auto || pt == PlaylistType.Standard);
                    
                }
                else
                {
                    return false;
                }
            }
        }
        

        public override string Value
        {
            set
            {
                base.Value = value;
                if (SelectedIndex != ANY_INDEX)
                    Hint = String.Empty;
            }
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if (!this.Active)
                controller.RequestAction(QActionType.MoveLeft);
        }
    }
}
