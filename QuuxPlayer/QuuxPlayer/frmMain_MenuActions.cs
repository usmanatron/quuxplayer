/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal partial class frmMain
    {
        private void mnuFile_DropDownOpening(object sender, EventArgs e)
        {
            normal.RemoveFilterIndex();

            bool enabled = normal.HasSelectedTracks || controller.Playing;

            mnuFileShowFileDetails.Enabled = enabled;
            mnuFileSleep.Checked = controller.Sleep != null && controller.Sleep.Active;
            mnuFileLockControls.Checked = this.locked;

            enabled = !this.locked;

            bool hasTracks = normal.HasTracks;

            mnuFileAddFileToLibrary.Enabled = enabled;
            mnuFileAddFolderToLibrary.Enabled = enabled;
            mnuFileAutoMonitorFolders.Enabled = enabled;
            mnuFileExit.Enabled = enabled;
            mnuFileExportCurrentViewAsPlaylist.Enabled = enabled && hasTracks && (controller.CurrentView == ViewType.Normal);
            mnuFileExportCurrentViewAsSpreadsheet.Enabled = enabled && hasTracks && (controller.CurrentView == ViewType.Normal);
            mnuFileImportPlaylist.Enabled = enabled;
            mnuFileLibraryMaintenance.Enabled = enabled;
            mnuFileRefreshTracks.Enabled = enabled && !TrackWriter.HasUnsavedTracks;
            mnuFileSleep.Enabled = enabled;
            mnuFileSetFileAssociations.Enabled = enabled;
            mnuFilePodcasts.Enabled = enabled;
            
            mnuFileShowFileDetails.Enabled &= (enabled && controller.CurrentView == ViewType.Normal);
            mnuFileOrganizeLibrary.Enabled = enabled && (controller.CurrentView == ViewType.Normal);
        }
        private void mnuFileExit_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.Exit);
        }
        private void mnuFileClearDatabase_Click(object sender, EventArgs e)
        {
            if (QMessageBox.Show(this,
                                 Localization.Get(UI_Key.Dialog_Clear_Database),
                                 Localization.Get(UI_Key.Dialog_Clear_Database_Title),
                                 QMessageBoxButtons.OKCancel,
                                 QMessageBoxIcon.Warning,
                                 QMessageBoxButton.NoCancel)
                                 == DialogResult.OK)
                controller.CreateNewDatabase();
        }
        private void mnuFileAddFolderToLibrary_Click(object sender, EventArgs e)
        {
            Lib.DoEvents();
            controller.RequestAction(QActionType.AddFolderToLibrary);
        }
        private void mnuFileRemoveGhostTracks_Click(object sender, EventArgs e)
        {
            if (QMessageBox.Show(this,
                                 Localization.Get(UI_Key.Dialog_Remove_Ghost_Tracks),
                                 Localization.Get(UI_Key.Dialog_Remove_Ghost_Tracks_Title),
                                 QMessageBoxButtons.OKCancel,
                                 QMessageBoxIcon.Question,
                                 QMessageBoxButton.NoCancel) 
                                    == DialogResult.OK)
                controller.RequestAction(QActionType.RemoveNonExistentTracks);
        }
        private void mnuFileSuggestDeleteDuplicatesKeepHighBitRate_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.SuggestDuplicatesBitrate);
        }
        private void mnuFileSuggestDeleteDuplicatesKeepOldest_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.SuggestDuplicatesOldest);
        }
        private void mnuFileSuggestDeleteDuplicatesKeepNewest_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.SuggestDuplicatesNewest);
        }
        private void mnuFileSuggestDeleteDuplicatesKeepHighestRated_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.SuggestDuplicatesHighestRated);
        }
        private void mnuFileRefreshTracks_DropDownOpening(object sender, EventArgs e)
        {
            mnuFileRefreshSelectedTracks.Enabled = normal.HasSelectedTracks;
        }
        private void mnuFileRefreshAllTracks_Click(object sender, EventArgs e)
        {
            if (QMessageBox.Show(this,
                                 Localization.Get(UI_Key.Dialog_Refresh_Tracks),
                                 Localization.Get(UI_Key.Dialog_Refresh_Tracks_Title),
                                 QMessageBoxButtons.OKCancel,
                                 QMessageBoxIcon.Question,
                                 QMessageBoxButton.YesOK)
                                    == DialogResult.OK)
                controller.RequestAction(QActionType.RefreshLibrary);
        }
        private void mnuFileRefreshSelectedTracks_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.RefreshSelectedTracks);
        }
        private void mnuFileImportPlaylist_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ImportPlaylist);
        }
        private void mnuFileSleep_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.SetSleepOptions);
        }
        private void mnuFileLockControls_Click(object sender, EventArgs e)
        {
            if (Locked)
                controller.RequestAction(QActionType.UnlockControls);
            else
                controller.RequestAction(QActionType.SetLockOptions);
        }
        
        private void mnuFileLibraryMaintenanceUpdateFilePaths_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.AskAboutMissingFiles);
        }
        private void mnuFileSetFileAssociations_Click(object sender, EventArgs e)
        {
            Lib.DoEvents();
            controller.RequestAction(QActionType.SetFileAssociations);
        }
        private void mnuFilePodcasts_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.TogglePodcastView);
        }
        private void mnuFileShowFileDetails_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ShowFileDetails);
        }
        private void mnuFileAutoMonitorFolders_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ShowCrawlDialog);
        }
        private void mnuFileExportCurrentViewAsPlaylist_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ExportCurrentView);
        }
        private void mnuFileExportCurrentViewAsSpreadsheet_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ExportCSV);
        }
        private void mnuFileAddFileToLibrary_Click(object sender, EventArgs e)
        {
            Lib.DoEvents();
            controller.RequestAction(QActionType.AddFileToLibrary);
        }

        private void mnuEdit_DropDownOpening(object sender, EventArgs e)
        {
            normal.RemoveFilterIndex();

            bool enabled = controller.CurrentView == ViewType.Normal;

            mnuEditSelectAll.Enabled = enabled;
            mnuEditSelectNone.Enabled = enabled;
            mnuEditInvertSelection.Enabled = enabled;

            mnuEditResetPlayHistory.Enabled =
                mnuEditSetRating.Enabled =
                mnuEditEqualizer.Enabled =
                mnuEditTags.Enabled =
                    enabled && normal.HasSelectedTracks;

            mnuEditTags.Enabled &= controller.AllowTagEditing;
        }
        private void mnuEditSelectAll_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.SelectAll);
        }
        private void mnuEditSelectNone_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.SelectNone);
        }
        private void mnuEditInvertSelection_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.InvertSelection);
        }
        private void mnuEditSetRating_DropDownOpening(object sender, EventArgs e)
        {
            controller.PopulateRatingSelections(mnuEditSetRating);
        }
        private void mnuEditEqualizer_DropDownOpening(object sender, EventArgs e)
        {
            controller.PopulateEqualizerSelections(mnuEditEqualizer);
        }
        private void mnuEditResetPlayHistory_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ResetPlayHistory);
        }
        private void mnuEditTags_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.EditTags);
        }
        private void mnuFileOrganizeLibrary_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.OrganizeLibrary);
        }
        private void mnuPlay_DropDownOpening(object sender, EventArgs e)
        {
            normal.RemoveFilterIndex();

            mnuPlayMuteVolume.Checked = Setting.Mute;

            bool radio = controller.RadioMode;
            bool playingNotRadio = !radio && controller.Playing;

            mnuPlayRepeat.Checked = controlPanel.Repeat;
            mnuPlayRepeat.Enabled = !radio;

            bool hst = normal.HasSelectedTracks;
            bool normView = controller.CurrentView == ViewType.Normal;

            mnuPlayPlaySelectedTrackNext.Enabled = hst && normView;
            mnuPlayAddTracksToNowPlaying.Enabled = hst && normView;

            mnuPlayPlayThisAlbum.Enabled = (hst && normView && normal.SelectedTracks[0].Album.Length > 0) || 
                                           (normal.IsFilterActive(FilterType.Album) && normal.HasTracks && normal[0].Album.Length > 0);
            
            mnuPlayAddThisAlbumToNowPlaying.Enabled = mnuPlayPlayThisAlbum.Enabled;
            
            mnuPlayPause.Enabled = controller.Playing;
            mnuPlayStop.Enabled = controller.Playing;
            mnuPlayStopAfterThisTrack.Enabled = playingNotRadio;
            mnuPlayStopAfterThisTrack.Checked = controller.StopAfterThisTrack;
            mnuPlayPlayNextTrack.Enabled = playingNotRadio;
            mnuPlayPlayPreviousTrack.Enabled = playingNotRadio;
            mnuPlayScanBackward.Enabled = playingNotRadio;
            mnuPlayScanForward.Enabled = playingNotRadio;

            mnuPlayPlayRandomAlbum.Enabled = !radio;
            mnuPlayShuffleTracks.Enabled = !radio;
        }
        private void mnuPlayPlay_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.Play);
        }
        private void mnuPlayStop_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.Stop);
        }
        private void mnuPlayStopAfterThisTrack_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.StopAfterThisTrack);
        }
        private void mnuPlayPause_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.Pause);
        }
        private void mnuPlayPlayNextTrack_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.Next);
        }
        private void mnuPlayPlayPreviousTrack_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.Previous);
        }
        private void mnuPlayShuffleTracks_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.Shuffle);
        }
        private void mnuPlayMuteVolume_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.Mute);
        }
        private void mnuPlayRepeat_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.RepeatToggle);
        }
        private void mnuPlayVolumeUp_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.VolumeUp);
        }        
        private void mnuPlayVolumeDown_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.VolumeDown);
        }
        private void mnuPlayScanForward_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ScanFwd);
        }
        private void mnuPlayScanBackward_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ScanBack);
        }
        private void mnuPlayPlayThisAlbum_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.PlayThisAlbum);
        }
        private void mnuPlayPlaySelectedTrackNext_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.PlaySelectedTrackNext);
        }

        private void mnuPlaylists_DropDownOpening(object sender, EventArgs e)
        {
            normal.RemoveFilterIndex();

            bool radio = controller.RadioMode;

            bool hasSel = normal.HasSelectedTracks;

            mnuPlaylistsAddPlaylist.Enabled = !radio;
            mnuPlaylistsDeletePlaylist.Enabled = !radio;
            mnuPlaylistsSwitchToNowPlaying.Enabled = !radio;

            mnuPlaylistsAddSelectedTracksTo.Enabled = hasSel && !radio;
            mnuPlaylistsAddTracksToTargetPlaylist.Enabled = hasSel && !radio && controller.PlaylistExists(controller.TargetPlaylistName);
            mnuPlayAddTracksToNowPlaying.Enabled = hasSel && !radio;

            if (controller.TargetPlaylistName.Length > 0)
                mnuPlaylistsAddTracksToTargetPlaylist.Text = Localization.Get(UI_Key.Menu_Playlist_Add_Tracks_To_Playlist, controller.TargetPlaylistName);
            else
                mnuPlaylistsAddTracksToTargetPlaylist.Text = Localization.Get(UI_Key.Menu_Playlist_Add_Tracks_To);

            mnuPlaylistsRemoveSelectedTracksFromPlaylist.Enabled = normal.NondynamicPlaylistBasedView;

            PlaylistType pt = PlaylistType.None;

            bool playlistEnabled = normal.IsFilterActive(FilterType.Playlist);

            if (playlistEnabled)
                pt = Database.GetPlaylistType(normal.ActivePlaylist);

            switch (pt)
            {
                case PlaylistType.Auto:
                    mnuPlaylistsEditAutoPlaylist.Enabled = true;
                    mnuPlaylistsConvertToStandardPlaylist.Enabled = true;
                    mnuPlaylistsEditAutoPlaylist.Text = Localization.Get(UI_Key.Menu_Playlist_Edit_Auto_Playlist);
                    break;
                case PlaylistType.Standard:
                    mnuPlaylistsEditAutoPlaylist.Enabled = true;
                    mnuPlaylistsConvertToStandardPlaylist.Enabled = false;
                    mnuPlaylistsEditAutoPlaylist.Text = Localization.Get(UI_Key.Menu_Playlist_Convert_To_Auto_Playlist);
                    break;
                default:
                    mnuPlaylistsEditAutoPlaylist.Enabled = false;
                    mnuPlaylistsConvertToStandardPlaylist.Enabled = false;
                    mnuPlaylistsRenameSelectedPlaylist.Enabled = false;
                    break;
            }

            mnuPlaylistsSwitchToNowPlaying.Checked = controlPanel.NowPlaying;
        }
        private void mnuPlaylistsDeletePlaylist_DropDownOpening(object sender, EventArgs e)
        {
            List<string> playlists = Database.GetPlaylists();
            mnuPlaylistsDeletePlaylist.DropDownItems.Clear();
            foreach (string s in playlists)
            {
                switch (Database.GetPlaylistType(s))
                {
                    case PlaylistType.Standard:
                    case PlaylistType.Auto:

                        ToolStripItem tsi = mnuPlaylistsDeletePlaylist.DropDownItems.Add(s);
                        tsi.Tag = s;
                        tsi.Click += delegate(object sender1, EventArgs e1)
                        {
                            string ss = tsi.Tag.ToString();
                            if (QMessageBox.Show(this,
                                                 Localization.Get(UI_Key.Dialog_Remove_Playlist, ss),
                                                 Localization.Get(UI_Key.Dialog_Remove_Playlist_Title),
                                                 QMessageBoxButtons.OKCancel,
                                                 QMessageBoxIcon.Question,
                                                 QMessageBoxButton.NoCancel)
                                                    == DialogResult.OK)
                                controller.DeletePlaylist(ss);
                        };
                        break;
                }
            }
        }
        private void mnuPlaylistsAddPlaylist_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.CreateNewPlaylist);
        }
        private void mnuPlaylistsAddSelectedTracksTo_DropDownOpening(object sender, EventArgs e)
        {
            List<string> playlists = Database.GetPlaylists().FindAll(s => !Database.IsPlaylistDynamic(s));

            mnuPlaylistsAddSelectedTracksTo.DropDownItems.Clear();
            foreach (string s in playlists)
            {
                ToolStripMenuItem tsi = (ToolStripMenuItem)mnuPlaylistsAddSelectedTracksTo.DropDownItems.Add(s);

                tsi.Click += (s1, e1) =>
                {
                    controller.AddToPlaylist(tsi.ToString(), normal.SelectedTracks);
                    controller.TargetPlaylistName = tsi.ToString();
                    controller.RequestAction(QActionType.RefreshAll);
                };
            }
        }
        private void mnuPlaylistsClearTargetPlaylist_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ClearTargetPlaylist);
        }
        private void mnuPlaylistsRemoveSelectedTracksFromPlaylist_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.Delete);
        }
        private void mnuPlayAddTracksToNowPlaying_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.AddToNowPlaying);
        }
        private void mnuPlaylistsAddTracksToTargetPlaylist_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.AddToTargetPlaylist);
        }
        private void mnuPlaylistsSwitchToNowPlaying_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ViewNowPlaying);
        }
        private void mnuPlaylistsEditAutoPlaylist_Click(object sender, EventArgs e)
        {
            Lib.DoEvents();
            controller.RequestAction(QActionType.EditAutoPlaylist);
        }
        private void mnuPlaylistsConvertToStandardPlaylist_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ConvertToStandardPlaylist);
        }
        private void mnuPlaylistsRenameSelectedPlaylist_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.RenameSelectedPlaylist);
        }

        private void mnuFilters_DropDownOpening(object sender, EventArgs e)
        {
            normal.RemoveFilterIndex();

            bool notRadio = !controller.RadioMode;

            mnuFiltersReleaseAllFilters.Enabled = notRadio;
            mnuFiltersReleaseSelectedFilter.Enabled = notRadio;
            mnuFiltersSelectFilter.Enabled = notRadio;
            mnuFiltersSelectNextFilter.Enabled = notRadio;
            mnuFiltersSelectPreviousFilter.Enabled = notRadio;
            mnuFiltersShowFilterIndex.Enabled = notRadio;
        }
        private void mnuFiltersSelectNextFilter_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.NextFilter);
        }
        private void mnuFiltersSelectPreviousFilter_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.PreviousFilter);
        }
        private void mnuFiltersReleaseSelectedFilter_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ReleaseCurrentFilter);
        }
        private void mnuFiltersReleaseAllFilters_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ReleaseAllFilters);
        }
        private void mnuFiltersSelectFilterPlaylists_Click(object sender, EventArgs e)
        {
            normal.CurrentFilterType = FilterType.Playlist;
        }
        private void mnuFiltersSelectFilterGenres_Click(object sender, EventArgs e)
        {
            normal.CurrentFilterType = FilterType.Genre;
        }
        private void mnuFiltersSelectFilterArtists_Click(object sender, EventArgs e)
        {
            normal.CurrentFilterType = FilterType.Artist;
        }
        private void mnuFiltersSelectFilterAlbums_Click(object sender, EventArgs e)
        {
            normal.CurrentFilterType = FilterType.Album;
        }
        private void mnuFiltersSelectFilterYear_Click(object sender, EventArgs e)
        {
            normal.CurrentFilterType = FilterType.Year;
        }
        private void mnuFiltersSelectFilterGroupings_Click(object sender, EventArgs e)
        {
            normal.CurrentFilterType = FilterType.Grouping;
        }
        private void mnuFiltersSelectFilter_DropDownOpening(object sender, EventArgs e)
        {
            FilterType ft = normal.CurrentFilterType;

            mnuFiltersSelectFilterPlaylists.Checked = ft == FilterType.Playlist;
            mnuFiltersSelectFilterGenres.Checked    = ft == FilterType.Genre;
            mnuFiltersSelectFilterArtists.Checked   = ft == FilterType.Artist;
            mnuFiltersSelectFilterAlbums.Checked    = ft == FilterType.Album;
            mnuFiltersSelectFilterYears.Checked     = ft == FilterType.Year;
            mnuFiltersSelectFilterGroupings.Checked = ft == FilterType.Grouping;
        }
        
        private void mnuView_DropDownOpening(object sender, EventArgs e)
        {
            normal.RemoveFilterIndex();

            mnuViewUseMiniPlayerWhenMinimized.Checked = Setting.UseMiniPlayer;

            mnuViewShowAlbumArtOnMainScreen.Checked = Setting.ShowAlbumArtOnMainScreen;
            mnuViewHTPCMode.Checked = (controller.HTPCMode == HTPCMode.HTPC);
            mnuViewFindCurrentlyPlayingTrack.Enabled = controller.Playing;
            mnuViewShowFullScreen.Checked = Lib.FullScreen;

            bool notRadio = !controller.RadioMode;

            mnuViewShowAllOf.Enabled = notRadio;

            bool normalView = controller.CurrentView == ViewType.Normal;

            mnuViewShowColumns.Enabled = normalView;
            mnuViewResetColumns.Enabled = normalView;
        }
        private void mnuViewShowColumns_DropDownOpening(object sender, EventArgs e)
        {
            TrackList.PopulateColumnSelections(mnuViewShowColumns.DropDownItems);
        }
        private void mnuViewResetColumns_Click(object sender, EventArgs e)
        {
            normal.ResetColumns(false);
        }
        private void mnuViewTagCloud_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ShowTagCloud);
        }
        private void mnuViewHTPCMode_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.HTPCMode);
        }
        private void mnuViewAdvanceView_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.AdvanceScreen);
        }
        private void mnuViewShowAllOfThisArtist_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.FilterSelectedArtist);
        }
        private void mnuViewShowAllOfThisAlbum_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.FilterSelectedAlbum);
        }
        private void mnuViewShowAllOfThisGenre_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.FilterSelectedGenre);
        }
        private void mnuViewShowAllOfThisYear_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.FilterSelectedYear);
        }
        private void mnuViewShowAllOfThisGrouping_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.FilterSelectedGrouping);
        }
        private void mnuViewShowAllOf_DropDownOpening(object sender, EventArgs e)
        {
            Track t = normal.FirstSelectedTrack;

            mnuViewShowAllOfThisArtist.Enabled = t != null && t.Artist.Length > 0;
            mnuViewShowAllOfThisAlbum.Enabled = t != null && t.Album.Length > 0;
            mnuViewShowAllOfThisGenre.Enabled = t != null && t.Genre.Length > 0 && (String.Compare(t.Genre, Localization.NO_GENRE, true) != 0);
            mnuViewShowAllOfThisYear.Enabled = t != null && t.YearString.Length > 0;
            mnuViewShowAllOfThisGrouping.Enabled = t != null && t.Grouping.Length > 0;

            mnuViewShowAllOfThisArtist.Text = Localization.Get(UI_Key.Menu_View_This_Artist) + (mnuViewShowAllOfThisArtist.Enabled ? (":  " + t.MainGroup.Replace("&", "&&")) : String.Empty);
            mnuViewShowAllOfThisAlbum.Text = Localization.Get(UI_Key.Menu_View_This_Album) + (mnuViewShowAllOfThisArtist.Enabled ? (":  " + t.Album.Replace("&", "&&")) : String.Empty);
            mnuViewShowAllOfThisGenre.Text = Localization.Get(UI_Key.Menu_View_This_Genre) + (mnuViewShowAllOfThisArtist.Enabled ? (":  " + t.Genre.Replace("&", "&&")) : String.Empty);
            mnuViewShowAllOfThisYear.Text = Localization.Get(UI_Key.Menu_View_This_Year) + (mnuViewShowAllOfThisArtist.Enabled ? (":  " + t.YearString.Replace("&", "&&")) : String.Empty);
            mnuViewShowAllOfThisGrouping.Text = Localization.Get(UI_Key.Menu_View_This_Grouping) + (mnuViewShowAllOfThisArtist.Enabled ? (":  " + t.Grouping.Replace("&", "&&")) : String.Empty);

        }
        private void mnuViewShowAlbumArtOnMainScreen_Click(object sender, EventArgs e)
        {
            normal.ShowAlbumArtOnMainScreen = !normal.ShowAlbumArtOnMainScreen;
        }
        private void mnuViewFindCurrentlyPlayingTrack_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.FindPlayingTrack);
        }
        private void mnuViewShowFullScreen_Click(object sender, EventArgs e)
        {
            controller.RequestAction(Lib.FullScreen ? QActionType.ReleaseFullScreen : QActionType.SetFullScreen);
        }
        
        private void mnuOptions_DropDownOpening(object sender, EventArgs e)
        {
            normal.RemoveFilterIndex();

            mnuOptionsHighResolutionSpectrum.Checked = (controller.SpectrumMode == SpectrumMode.Normal);
            mnuOptionsUseEqualizer.Checked = controller.EqualizerOn;
            mnuOptionsEqualizerSettings.Checked = (controller.CurrentView == ViewType.Equalizer);
            mnuOptionsAutoClippingControl.Checked = Setting.AutoClipControl;
        }
        private void mnuOptionsHighResolutionSpectrum_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ToggleSpectrumResolution);
            if (spectrumView.Visible)
                Clock.DoOnMainThread(spectrumView.Invalidate, 100);
        }
        private void mnuOptionsDecoderGain_DropDownOpening(object sender, EventArgs e)
        {
            int gainDBTimes100 = ((int)(controller.DecoderGainDB * 100));

            mnuOptionsDecoderGain.DropDownItems.Clear();

            foreach (float f in decoderGainSettingsInDB)
            {
                ToolStripMenuItem mi = new ToolStripMenuItem(f.ToString("+0.0;-0.0;0.0") + " dB");

                if (gainDBTimes100 == ((int)(f * 100f)))
                    mi.Checked = true;

                mi.Tag = f;

                mi.Click += (ss, ee) => { controller.DecoderGainDB = (float)((ToolStripMenuItem)mi).Tag; };
                mnuOptionsDecoderGain.DropDownItems.Add(mi);
            }
        }
        private void mnuOptionsDetectGamepad_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.InitializeGamepad);
            if (controller.GamepadEnabled)
                QMessageBox.Show(this,
                                 Localization.Get(UI_Key.Dialog_Gamepad_Enabled),
                                 Localization.Get(UI_Key.Dialog_Gamepad_Enabled_Title),
                                 QMessageBoxIcon.Information);
        }
        private void mnuViewUseMiniPlayerWhenMinimized_Click(object sender, EventArgs e)
        {
            Setting.UseMiniPlayer = !Setting.UseMiniPlayer;
        }
        private void mnuViewShowMiniPlayer_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ShowMiniPlayer);
        }
        private void mnuOptionsUseEqualizer_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ToggleEqualizer);
        }
        private void mnuOptionsEqualizerSettings_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ShowEqualizer);
        }
        private void mnuOptionsSpectrumViewGainUp_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.SpectrumViewGainUp);
        }
        private void mnuOptionsSpectrumViewGainDown_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.SpectrumViewGainDown);
        }
        
        private void mnuOptionsAutoClippingControl_Click(object sender, EventArgs e)
        {
            Setting.AutoClipControl = !Setting.AutoClipControl;
        }
        private void mnuInternetDownloadMissingCoverArt_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ToggleDownloadCoverArt);
        }
        private void mnuOptionsTwitterOptions_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.TwitterOptions);
        }
        private void mnuOptionsLastFMOptions_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.LastFMOptions);
        }
        private void mnuOptionsReplayGainAlbum_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ReplayGainAlbum);
        }
        private void mnuOptionsReplayGainTrack_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ReplayGainTrack);
        }
        private void mnuOptionsReplayGainOff_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ReplayGainOff);
        }
        private void mnuOptionsReplayGainAnalyzeSelectedTracks_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ReplayGainAnalyzeSelectedTracks);
        }
        private void mnuOptionsReplayGainAnalyzeCancel_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ReplayGainAnalyzeCancel);
        }
        private void mnuOptionsReplayGainWriteTags_Click(object sender, EventArgs e)
        {
            Setting.WriteReplayGainTags = !Setting.WriteReplayGainTags;
        }
        private void mnuOptionsUseReplayGain_DropDownOpening(object sender, EventArgs e)
        {
            mnuOptionsReplayGainAlbum.Checked = controller.ReplayGain == ReplayGainMode.Album;
            mnuOptionsReplayGainTrack.Checked = controller.ReplayGain == ReplayGainMode.Track;
            mnuOptionsReplayGainOff.Checked = controller.ReplayGain == ReplayGainMode.Off;

            mnuOptionsReplayGainAnalyzeSelectedTracks.Enabled = !controller.ReplayGainAnalysisInProgress && normal.HasSelectedTracks;
            mnuOptionsReplayGainAnalyzeCancel.Enabled = controller.ReplayGainAnalysisInProgress;

            mnuOptionsReplayGainWriteTags.Checked = Setting.WriteReplayGainTags;
        }
        private void mnuOptionsSelectNextEqualizer_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.SelectNextEqualizer);
        }
        private void mnuOptionsMoreOptions_Click(object sender, EventArgs e)
        {
            frmOptions fo = new frmOptions();

            fo.Icon = this.Icon;

            fo.ShowGridOnSpectrum = spectrumView.ShowGrid;
            fo.DownloadCovertArt = Track.DownloadCoverArt;
            fo.AutoClippingControl = Setting.AutoClipControl;
            fo.OutputDeviceName = controller.OutputDeviceName;
            fo.AutoCheckForUpdates = Setting.AutoCheckForUpdates;
            fo.UseGlobalHotKeys = controller.UseGlobalHotKeys;
            fo.IncludeTagCloud = Setting.IncludeTagCloud;
            fo.ShortTrackCutoff = Setting.ShortTrackCutoff;
            fo.ArtSaveOption = controller.ArtSaveOption;
            fo.DisableScreensaver = Setting.DisableScreensaver;
            fo.DisableScreensaverOnlyWhenPlaying = Setting.DisableScreensaverOnlyWhenPlaying;
            fo.LocalVolumeControl = controller.LocalVolumeControl;
            fo.StopClearsNowPlaying = Setting.StopClearsNowPlaying;
            fo.SaveNowPlayingOnExit = Setting.SaveNowPlayingOnExit;

            fo.ShowDialog(this);

            if (fo.DialogResult == DialogResult.OK)
            {
                fo.Visible = false;
                Lib.DoEvents();
                spectrumView.ShowGrid = fo.ShowGridOnSpectrum;
                Track.DownloadCoverArt = fo.DownloadCovertArt;
                Setting.AutoClipControl = fo.AutoClippingControl;
                Setting.AutoCheckForUpdates = fo.AutoCheckForUpdates;
                controller.OutputDeviceName = fo.OutputDeviceName;
                Setting.IncludeTagCloud = fo.IncludeTagCloud;
                controller.UseGlobalHotKeys = fo.UseGlobalHotKeys;
                Setting.ShortTrackCutoff = fo.ShortTrackCutoff;
                controller.ArtSaveOption = fo.ArtSaveOption;
                Setting.StopClearsNowPlaying = fo.StopClearsNowPlaying;
                Setting.SaveNowPlayingOnExit = fo.SaveNowPlayingOnExit;
                Setting.DisableScreensaverOnlyWhenPlaying = fo.DisableScreensaverOnlyWhenPlaying;
                Setting.DisableScreensaver = fo.DisableScreensaver;
                Lib.ScreenSaverIsActive = !Setting.DisableScreensaver;
                
                if (Setting.DisableScreensaver)
                    preventPowerEvent();

                controller.LocalVolumeControl = fo.LocalVolumeControl;
            }
        }

        private void mnuInternet_DropDownOpening(object sender, EventArgs e)
        {
            mnuInternetRadio.Checked = controller.RadioMode;
            mnuInternetDownloadMissingCoverArt.Checked = Track.DownloadCoverArt;

            Track t = controller.UsersMostInterestingTrack;

            bool unlock = !this.Locked;

            mnuInternetShowInternetInfo.Enabled = unlock && (t != null);
            mnuInternetShowArtist.Enabled = mnuInternetShowInternetInfo.Enabled;

            mnuInternetShowAlbum.Enabled = mnuInternetShowInternetInfo.Enabled && t.Album.Length > 0;

            mnuInternetRadioReload.Enabled = controller.CurrentView == ViewType.Radio;
        }
        private void mnuInternetRadio_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ToggleRadioMode);
        }
        private void mnuInternetRadioReload_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ReloadRadioStations);
        }
        private void mnuInternetShowAlbum_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.LastFMShowAlbum);
        }
        private void mnuInternetShowArtist_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.LastFMShowArtist);
        }
        private void mnuInternetShowInternetInfo_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ShowTrackAndAlbumDetails);
        }
        private void mnuHelp_DropDownOpening(object sender, EventArgs e)
        {
            normal.RemoveFilterIndex();
        }
        private void mnuHelpAbout_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ShowAboutScreen);
        }
        private void mnuHelpCheckForUpdate_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.CheckForUpdate);
        }
        private void mnuHelpShowGamePadHelp_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ToggleGamepadHelp);
        }
        private void mnuHelpOtherLanguages_Click(object sender, EventArgs e)
        {
            Net.BrowseTo(Lib.PRODUCT_URL + "/localization.php");
        }
        private void mnuHelpVisitWebSite_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.VisitWebSite);
        }
        private void mnuHelpOnlineHelp_Click(object sender, EventArgs e)
        {
            Net.BrowseTo(Lib.PRODUCT_URL + "/quuxplayerhelp.php");
        }
    }
}
