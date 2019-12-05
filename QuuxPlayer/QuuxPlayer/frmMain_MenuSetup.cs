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
        private System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));

        private MenuStrip mnuMain = new System.Windows.Forms.MenuStrip();

        private ToolStripMenuItem mnuFile = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileAddFolderToLibrary = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileAddFileToLibrary = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileAutoMonitorFolders = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileLibraryMaintenance = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileRefreshTracks = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileRefreshSelectedTracks = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileRefreshAllTracks = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileRemoveGhostTracks = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileSetFileAssociations = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileClearDatabase = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileSuggestDeleteDuplicates = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileSuggestDeleteDuplicatesKeepHighBitRate = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileSuggestDeleteDuplicatesKeepOldest = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileSuggestDeleteDuplicatesKeepNewest = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileSuggestDeleteDuplicatesKeepHighestRated = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFilePodcasts = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileOrganizeLibrary = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileImportPlaylist = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileExportCurrentViewAsPlaylist = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileExportCurrentViewAsSpreadsheet = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileShowFileDetails = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileSleep = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileLockControls = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFileExit = new ToolStripMenuItem();

        private ToolStripMenuItem mnuEdit = new ToolStripMenuItem();
        private ToolStripMenuItem mnuEditSelectAll = new ToolStripMenuItem();
        private ToolStripMenuItem mnuEditSelectNone = new ToolStripMenuItem();
        private ToolStripMenuItem mnuEditInvertSelection = new ToolStripMenuItem();
        private ToolStripMenuItem mnuEditSetRating = new ToolStripMenuItem();
        private ToolStripMenuItem mnuEditSetRatingDummy = new ToolStripMenuItem();
        private ToolStripMenuItem mnuEditEqualizer = new ToolStripMenuItem();
        private ToolStripMenuItem mnuEditEqualizerDummy = new ToolStripMenuItem();
        private ToolStripMenuItem mnuEditResetPlayHistory = new ToolStripMenuItem();
        private ToolStripMenuItem mnuEditTags = new ToolStripMenuItem();
        
        private ToolStripMenuItem mnuPlay = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlayPlay = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlayStop = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlayStopAfterThisTrack = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlayPause = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlayPlayPreviousTrack = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlayPlayNextTrack = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlayMuteVolume = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlayRepeat = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlayVolumeUp = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlayVolumeDown = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlayScanForward = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlayScanBackward = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlayPlayThisAlbum = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlayAddThisAlbumToNowPlaying = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlayPlayRandomAlbum = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlayShuffleTracks = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlayPlaySelectedTrackNext = new ToolStripMenuItem();

        private ToolStripMenuItem mnuPlaylists = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlaylistsAddPlaylist = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlaylistsDeletePlaylist = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlaylistsDeletePlaylistDummy = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlaylistsAddSelectedTracksTo = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlaylistsAddSelectedTracksToDummy = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlaylistsRemoveSelectedTracksFromPlaylist = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlayAddTracksToNowPlaying = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlaylistsSwitchToNowPlaying = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlaylistsAddTracksToTargetPlaylist = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlaylistsEditAutoPlaylist = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlaylistsConvertToStandardPlaylist = new ToolStripMenuItem();
        private ToolStripMenuItem mnuPlaylistsRenameSelectedPlaylist = new ToolStripMenuItem();

        private ToolStripMenuItem mnuFilters = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFiltersSelectNextFilter = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFiltersSelectPreviousFilter = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFiltersReleaseSelectedFilter = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFiltersReleaseAllFilters = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFiltersShowFilterIndex = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFiltersSelectFilter = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFiltersSelectFilterPlaylists = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFiltersSelectFilterGenres = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFiltersSelectFilterArtists = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFiltersSelectFilterAlbums = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFiltersSelectFilterYears = new ToolStripMenuItem();
        private ToolStripMenuItem mnuFiltersSelectFilterGroupings = new ToolStripMenuItem();

        private ToolStripMenuItem mnuView = new ToolStripMenuItem();
        private ToolStripMenuItem mnuViewShowFullScreen = new ToolStripMenuItem();
        private ToolStripMenuItem mnuViewUseMiniPlayerWhenMinimized = new ToolStripMenuItem();
        private ToolStripMenuItem mnuViewShowMiniPlayer = new ToolStripMenuItem();
        private ToolStripMenuItem mnuViewFindCurrentlyPlayingTrack = new ToolStripMenuItem();
        private ToolStripMenuItem mnuViewShowAllOf = new ToolStripMenuItem();
        private ToolStripMenuItem mnuViewShowAllOfThisArtist = new ToolStripMenuItem();
        private ToolStripMenuItem mnuViewShowAllOfThisAlbum = new ToolStripMenuItem();
        private ToolStripMenuItem mnuViewShowAllOfThisGenre = new ToolStripMenuItem();
        private ToolStripMenuItem mnuViewShowAllOfThisYear = new ToolStripMenuItem();
        private ToolStripMenuItem mnuViewShowAllOfThisGrouping = new ToolStripMenuItem();
        private ToolStripMenuItem mnuViewShowColumns = new ToolStripMenuItem();
        private ToolStripMenuItem mnuViewShowColumnsDummy = new ToolStripMenuItem();
        private ToolStripMenuItem mnuViewResetColumns = new ToolStripMenuItem();
        private ToolStripMenuItem mnuViewHTPCMode = new ToolStripMenuItem();
        private ToolStripMenuItem mnuViewTagCloud = new ToolStripMenuItem();
        private ToolStripMenuItem mnuViewAdvanceView = new ToolStripMenuItem();
        private ToolStripMenuItem mnuViewShowAlbumArtOnMainScreen = new ToolStripMenuItem();

        private ToolStripMenuItem mnuOptions = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOptionsDetectGamepad = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOptionsDecoderGain = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOptionsDecoderGainDummy = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOptionsUseReplayGain = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOptionsReplayGainAlbum = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOptionsReplayGainTrack = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOptionsReplayGainOff = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOptionsReplayGainAnalyzeSelectedTracks = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOptionsReplayGainWriteTags = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOptionsReplayGainAnalyzeCancel = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOptionsAutoClippingControl = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOptionsUseEqualizer = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOptionsSelectNextEqualizer = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOptionsEqualizerSettings = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOptionsSpectrumViewGain = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOptionsSpectrumViewGainUp = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOptionsSpectrumViewGainDown = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOptionsHighResolutionSpectrum = new ToolStripMenuItem();
        private ToolStripMenuItem mnuInternetDownloadMissingCoverArt = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOptionsTwitterOptions = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOptionsLastFMOptions = new ToolStripMenuItem();
        private ToolStripMenuItem mnuOptionsMoreOptions = new ToolStripMenuItem();

        private ToolStripMenuItem mnuInternet = new ToolStripMenuItem();
        private ToolStripMenuItem mnuInternetRadio = new ToolStripMenuItem();
        private ToolStripMenuItem mnuInternetRadioReload = new ToolStripMenuItem();
        private ToolStripMenuItem mnuInternetShowAlbum = new ToolStripMenuItem();
        private ToolStripMenuItem mnuInternetShowArtist = new ToolStripMenuItem();
        private ToolStripMenuItem mnuInternetShowInternetInfo = new ToolStripMenuItem();

        private ToolStripMenuItem mnuHelp = new ToolStripMenuItem();
        private ToolStripMenuItem mnuHelpOnlineHelp = new ToolStripMenuItem();
        private ToolStripMenuItem mnuHelpShowGamepadHelp = new ToolStripMenuItem();
        private ToolStripMenuItem mnuHelpOtherLanguages = new ToolStripMenuItem();
        private ToolStripMenuItem mnuHelpCheckForUpdate = new ToolStripMenuItem();
        private ToolStripMenuItem mnuHelpVisitWebSite = new ToolStripMenuItem();
        private ToolStripMenuItem mnuHelpAbout = new ToolStripMenuItem();

        private void init()
        {
            this.mnuMain.SuspendLayout();
            this.SuspendLayout();

            this.mnuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuFile,
                this.mnuEdit,
                this.mnuPlay,
                this.mnuPlaylists,
                this.mnuFilters,
                this.mnuView,
                this.mnuOptions,
                this.mnuInternet,
                this.mnuHelp
            });

            this.mnuMain.Location = new System.Drawing.Point(0, 0);
            this.mnuMain.TabIndex = 1;

            this.mnuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuFileAddFolderToLibrary,
                this.mnuFileAddFileToLibrary,
                this.mnuFileAutoMonitorFolders,
                this.mnuFileOrganizeLibrary,
                this.mnuFileLibraryMaintenance,
                new ToolStripSeparator(),
                this.mnuFilePodcasts,
                new ToolStripSeparator(),
                this.mnuFileImportPlaylist,
                this.mnuFileExportCurrentViewAsPlaylist,
                this.mnuFileExportCurrentViewAsSpreadsheet,
                new ToolStripSeparator(),
                this.mnuFileShowFileDetails,
                new ToolStripSeparator(),
                this.mnuFileSleep,
                this.mnuFileLockControls,
                new ToolStripSeparator(),
                this.mnuFileSetFileAssociations,
                new ToolStripSeparator(),
                this.mnuFileExit});

            this.mnuFile.Text = Localization.Get(UI_Key.Menu_File);
            this.mnuFile.DropDownOpening += new System.EventHandler(this.mnuFile_DropDownOpening);

            this.mnuFileAddFolderToLibrary.Text = Localization.Get(UI_Key.Menu_File_Add_Folder);
            this.mnuFileAddFolderToLibrary.Click += new System.EventHandler(this.mnuFileAddFolderToLibrary_Click);

            this.mnuFileAddFileToLibrary.Text = Localization.Get(UI_Key.Menu_File_Add_File);
            this.mnuFileAddFileToLibrary.Click += new System.EventHandler(this.mnuFileAddFileToLibrary_Click);

            this.mnuFileAutoMonitorFolders.Size = new System.Drawing.Size(325, 22);
            this.mnuFileAutoMonitorFolders.Text = Localization.Get(UI_Key.Menu_File_Auto_Monitor);
            this.mnuFileAutoMonitorFolders.Click += new System.EventHandler(this.mnuFileAutoMonitorFolders_Click);

            this.mnuFileLibraryMaintenance.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuFileRefreshTracks,
            this.mnuFileRemoveGhostTracks,
            new ToolStripSeparator(),
            this.mnuFileSuggestDeleteDuplicates,
            new ToolStripSeparator(),
            this.mnuFileClearDatabase});

            this.mnuFileSuggestDeleteDuplicates.DropDownItems.AddRange(new ToolStripItem[] {
                this.mnuFileSuggestDeleteDuplicatesKeepHighBitRate,
                this.mnuFileSuggestDeleteDuplicatesKeepNewest,
                this.mnuFileSuggestDeleteDuplicatesKeepOldest,
                this.mnuFileSuggestDeleteDuplicatesKeepHighestRated
            });

            this.mnuFileLibraryMaintenance.Text = Localization.Get(UI_Key.Menu_File_Library_Maintenance);

            this.mnuFileRefreshTracks.Text = Localization.Get(UI_Key.Menu_File_Refresh_Tracks);
            this.mnuFileRefreshAllTracks.Text = Localization.Get(UI_Key.Menu_File_Refresh_All_Tracks);
            this.mnuFileRefreshSelectedTracks.Text = Localization.Get(UI_Key.Menu_File_Refresh_Selected_Tracks);

            this.mnuFileRefreshAllTracks.Click += new System.EventHandler(this.mnuFileRefreshAllTracks_Click);
            this.mnuFileRefreshSelectedTracks.Click += new EventHandler(this.mnuFileRefreshSelectedTracks_Click);
            this.mnuFileRefreshSelectedTracks.ShortcutKeyDisplayString = "Shift+F8";
            this.mnuFileRefreshTracks.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuFileRefreshAllTracks,
                this.mnuFileRefreshSelectedTracks});

            this.mnuFileRefreshTracks.DropDownOpening += new System.EventHandler(this.mnuFileRefreshTracks_DropDownOpening);

            this.mnuFileRemoveGhostTracks.Text = Localization.Get(UI_Key.Menu_File_Remove_Ghost_Tracks);
            this.mnuFileRemoveGhostTracks.Click += new System.EventHandler(this.mnuFileRemoveGhostTracks_Click);

            this.mnuFileOrganizeLibrary.Text = Localization.Get(UI_Key.Menu_File_Organize_Library);
            this.mnuFileOrganizeLibrary.Click += new System.EventHandler(this.mnuFileOrganizeLibrary_Click);

            this.mnuFileSuggestDeleteDuplicates.Text = Localization.Get(UI_Key.Menu_File_Suggest_Delete_Duplicates);
            
            this.mnuFileSuggestDeleteDuplicatesKeepHighBitRate.Text = Localization.Get(UI_Key.Menu_File_Suggest_Delete_Duplicates_Keep_High_BitRate);
            this.mnuFileSuggestDeleteDuplicatesKeepHighBitRate.Click += new EventHandler(mnuFileSuggestDeleteDuplicatesKeepHighBitRate_Click);

            this.mnuFileSuggestDeleteDuplicatesKeepOldest.Text = Localization.Get(UI_Key.Menu_File_Suggest_Delete_Duplicates_Keep_Oldest);
            this.mnuFileSuggestDeleteDuplicatesKeepOldest.Click += new EventHandler(mnuFileSuggestDeleteDuplicatesKeepOldest_Click);

            this.mnuFileSuggestDeleteDuplicatesKeepNewest.Text = Localization.Get(UI_Key.Menu_File_Suggest_Delete_Duplicates_Keep_Newest);
            this.mnuFileSuggestDeleteDuplicatesKeepNewest.Click += new EventHandler(mnuFileSuggestDeleteDuplicatesKeepNewest_Click);

            this.mnuFileSuggestDeleteDuplicatesKeepHighestRated.Text = Localization.Get(UI_Key.Menu_File_Suggest_Delete_Duplicates_Keep_Highest_Rated);
            this.mnuFileSuggestDeleteDuplicatesKeepHighestRated.Click += new EventHandler(mnuFileSuggestDeleteDuplicatesKeepHighestRated_Click);

            this.mnuFileClearDatabase.Text = Localization.Get(UI_Key.Menu_File_Clear_Database);
            this.mnuFileClearDatabase.Click += new System.EventHandler(this.mnuFileClearDatabase_Click);

            this.mnuFileSetFileAssociations.Text = Localization.Get(UI_Key.Menu_File_Set_File_Associations);
            this.mnuFileSetFileAssociations.Click += new EventHandler(mnuFileSetFileAssociations_Click);

            this.mnuFilePodcasts.Text = Localization.Get(UI_Key.Menu_File_Podcasts);
            this.mnuFilePodcasts.ShortcutKeyDisplayString = "O";
            this.mnuFilePodcasts.Click += new EventHandler(mnuFilePodcasts_Click);

            this.mnuFileImportPlaylist.Text = Localization.Get(UI_Key.Menu_File_Import_Playlist);
            this.mnuFileImportPlaylist.Click += new System.EventHandler(this.mnuFileImportPlaylist_Click);

            this.mnuFileExportCurrentViewAsPlaylist.Text = Localization.Get(UI_Key.Menu_File_Export_View_As_Playlist);
            this.mnuFileExportCurrentViewAsPlaylist.Click += new System.EventHandler(this.mnuFileExportCurrentViewAsPlaylist_Click);

            this.mnuFileExportCurrentViewAsSpreadsheet.Text = Localization.Get(UI_Key.Menu_File_Export_Current_View_As_Spreadsheet);
            this.mnuFileExportCurrentViewAsSpreadsheet.Click += new System.EventHandler(this.mnuFileExportCurrentViewAsSpreadsheet_Click);

            this.mnuFileShowFileDetails.ShortcutKeyDisplayString = "F8";
            this.mnuFileShowFileDetails.Text = Localization.Get(UI_Key.Menu_File_Show_File_Details);
            this.mnuFileShowFileDetails.Click += new System.EventHandler(this.mnuFileShowFileDetails_Click);

            this.mnuFileSleep.Text = Localization.Get(UI_Key.Menu_File_Sleep);
            this.mnuFileSleep.Click += new System.EventHandler(this.mnuFileSleep_Click);

            this.mnuFileLockControls.Text = Localization.Get(UI_Key.Menu_File_Lock_Controls);
            this.mnuFileLockControls.Click += new System.EventHandler(this.mnuFileLockControls_Click);

            this.mnuFileExit.ShortcutKeyDisplayString = "Shift+X";
            this.mnuFileExit.Text = Localization.Get(UI_Key.Menu_File_Exit);
            this.mnuFileExit.Click += new System.EventHandler(this.mnuFileExit_Click);

            this.mnuEdit.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuEditSelectAll,
                this.mnuEditSelectNone,
                this.mnuEditInvertSelection,
                new ToolStripSeparator(),
                this.mnuEditSetRating,
                this.mnuEditEqualizer,
                new ToolStripSeparator(),
                this.mnuEditResetPlayHistory,
                new ToolStripSeparator(),
                this.mnuEditTags});

            this.mnuEdit.Text = Localization.Get(UI_Key.Menu_Edit);
            this.mnuEdit.DropDownOpening += new System.EventHandler(this.mnuEdit_DropDownOpening);

            this.mnuEditSelectAll.ShortcutKeyDisplayString = "Ctrl+A";
            this.mnuEditSelectAll.Text = Localization.Get(UI_Key.Menu_Edit_Select_All);
            this.mnuEditSelectAll.Click += new System.EventHandler(this.mnuEditSelectAll_Click);

            this.mnuEditSelectNone.ShortcutKeyDisplayString = "Ctrl+Shift+A";
            this.mnuEditSelectNone.Text = Localization.Get(UI_Key.Menu_Edit_Select_None);
            this.mnuEditSelectNone.Click += new System.EventHandler(this.mnuEditSelectNone_Click);

            this.mnuEditInvertSelection.Text = Localization.Get(UI_Key.Menu_Edit_Invert_Selection);
            this.mnuEditInvertSelection.Click += new EventHandler(mnuEditInvertSelection_Click);

            this.mnuEditSetRating.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuEditSetRatingDummy});
            this.mnuEditSetRating.Text = Localization.Get(UI_Key.Menu_Edit_Set_Rating);
            this.mnuEditSetRating.DropDownOpening += new System.EventHandler(this.mnuEditSetRating_DropDownOpening);
            this.mnuEditSetRatingDummy.Text = String.Empty;

            this.mnuEditEqualizer.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuEditEqualizerDummy});
            this.mnuEditEqualizer.Text = Localization.Get(UI_Key.Menu_Edit_Equalizer);
            this.mnuEditEqualizer.DropDownOpening += new System.EventHandler(this.mnuEditEqualizer_DropDownOpening);
            this.mnuEditEqualizerDummy.Text = String.Empty;

            this.mnuEditResetPlayHistory.Text = Localization.Get(UI_Key.Menu_Edit_Reset_Play_History);
            this.mnuEditResetPlayHistory.Click += new System.EventHandler(this.mnuEditResetPlayHistory_Click);

            this.mnuEditTags.Text = Localization.Get(UI_Key.Menu_Edit_Tags);
            this.mnuEditTags.ShortcutKeyDisplayString = "Shift+Enter";
            this.mnuEditTags.Click += new System.EventHandler(this.mnuEditTags_Click);

            this.mnuPlay.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuPlayPlay,
                this.mnuPlayStop,
                this.mnuPlayStopAfterThisTrack,
                this.mnuPlayPause,
                this.mnuPlayPlayPreviousTrack,
                this.mnuPlayPlayNextTrack,
                new ToolStripSeparator(),
                this.mnuPlayMuteVolume,
                this.mnuPlayRepeat,
                new ToolStripSeparator(),
                this.mnuPlayVolumeUp,
                this.mnuPlayVolumeDown,
                new ToolStripSeparator(),
                this.mnuPlayScanForward,
                this.mnuPlayScanBackward,
                new ToolStripSeparator(),
                this.mnuPlayPlaySelectedTrackNext,
                this.mnuPlayAddTracksToNowPlaying,
                this.mnuPlayPlayThisAlbum,
                this.mnuPlayAddThisAlbumToNowPlaying,
                new ToolStripSeparator(),
                this.mnuPlayPlayRandomAlbum,
                this.mnuPlayShuffleTracks});

            this.mnuPlay.ShortcutKeyDisplayString = "T";
            this.mnuPlay.Text = Localization.Get(UI_Key.Menu_Play);
            this.mnuPlay.DropDownOpening += new System.EventHandler(this.mnuPlay_DropDownOpening);

            this.mnuPlayPlay.ShortcutKeyDisplayString = "P";
            this.mnuPlayPlay.Text = Localization.Get(UI_Key.Menu_Play_Play);
            this.mnuPlayPlay.Click += new System.EventHandler(this.mnuPlayPlay_Click);

            this.mnuPlayStop.ShortcutKeyDisplayString = "S";
            this.mnuPlayStop.Text = Localization.Get(UI_Key.Menu_Play_Stop);
            this.mnuPlayStop.Click += new System.EventHandler(this.mnuPlayStop_Click);

            this.mnuPlayStopAfterThisTrack.ShortcutKeyDisplayString = "Shift+S";
            this.mnuPlayStopAfterThisTrack.Text = Localization.Get(UI_Key.Menu_Play_Stop_After_This_Track);
            this.mnuPlayStopAfterThisTrack.Click += new EventHandler(this.mnuPlayStopAfterThisTrack_Click);

            this.mnuPlayPause.ShortcutKeyDisplayString = "U";
            this.mnuPlayPause.Text = Localization.Get(UI_Key.Menu_Play_Pause);
            this.mnuPlayPause.Click += new System.EventHandler(this.mnuPlayPause_Click);

            this.mnuPlayPlayPreviousTrack.ShortcutKeyDisplayString = "V";
            this.mnuPlayPlayPreviousTrack.Text = Localization.Get(UI_Key.Menu_Play_Previous_Track);
            this.mnuPlayPlayPreviousTrack.Click += new System.EventHandler(this.mnuPlayPlayPreviousTrack_Click);

            this.mnuPlayPlayNextTrack.ShortcutKeyDisplayString = "N";
            this.mnuPlayPlayNextTrack.Text = Localization.Get(UI_Key.Menu_Play_Next_Track);
            this.mnuPlayPlayNextTrack.Click += new System.EventHandler(this.mnuPlayPlayNextTrack_Click);

            this.mnuPlayMuteVolume.ShortcutKeyDisplayString = "M";
            this.mnuPlayMuteVolume.Text = Localization.Get(UI_Key.Menu_Play_Mute);
            this.mnuPlayMuteVolume.Click += new System.EventHandler(this.mnuPlayMuteVolume_Click);

            this.mnuPlayRepeat.ShortcutKeyDisplayString = "Shift+R";
            this.mnuPlayRepeat.Text = Localization.Get(UI_Key.Menu_Play_Repeat);
            this.mnuPlayRepeat.Click += new System.EventHandler(this.mnuPlayRepeat_Click);

            this.mnuPlayVolumeUp.ShortcutKeyDisplayString = ">";
            this.mnuPlayVolumeUp.Text = Localization.Get(UI_Key.Menu_Play_Volume_Up);
            this.mnuPlayVolumeUp.Click += new System.EventHandler(this.mnuPlayVolumeUp_Click);

            this.mnuPlayVolumeDown.ShortcutKeyDisplayString = "<";
            this.mnuPlayVolumeDown.Text = Localization.Get(UI_Key.Menu_Play_Volume_Down);
            this.mnuPlayVolumeDown.Click += new System.EventHandler(this.mnuPlayVolumeDown_Click);

            this.mnuPlayScanForward.ShortcutKeyDisplayString = "Y";
            this.mnuPlayScanForward.Text = Localization.Get(UI_Key.Menu_Play_Scan_Fwd);
            this.mnuPlayScanForward.Click += new System.EventHandler(this.mnuPlayScanForward_Click);

            this.mnuPlayScanBackward.ShortcutKeyDisplayString = "T";
            this.mnuPlayScanBackward.Text = Localization.Get(UI_Key.Menu_Play_Scan_Back);
            this.mnuPlayScanBackward.Click += new System.EventHandler(this.mnuPlayScanBackward_Click);

            this.mnuPlayPlayThisAlbum.ShortcutKeyDisplayString = "Z";
            this.mnuPlayPlayThisAlbum.Text = Localization.Get(UI_Key.Menu_Play_This_Album);
            this.mnuPlayPlayThisAlbum.Click += new System.EventHandler(this.mnuPlayPlayThisAlbum_Click);

            this.mnuPlayAddThisAlbumToNowPlaying.ShortcutKeyDisplayString = "Shift+Z";
            this.mnuPlayAddThisAlbumToNowPlaying.Text = Localization.Get(UI_Key.Menu_Play_Add_This_Album_To_Now_Playing);
            this.mnuPlayAddThisAlbumToNowPlaying.Click += new EventHandler(mnuPlayAddThisAlbumToNowPlaying_Click);

            this.mnuPlayPlayRandomAlbum.ShortcutKeyDisplayString = "Shift+F5";
            this.mnuPlayPlayRandomAlbum.Text = Localization.Get(UI_Key.Menu_Play_Random_Album);
            this.mnuPlayPlayRandomAlbum.Click += new System.EventHandler(this.playRandomAlbumToolStripMenuItem_Click);

            this.mnuPlayShuffleTracks.ShortcutKeyDisplayString = "F5";
            this.mnuPlayShuffleTracks.Text = Localization.Get(UI_Key.Menu_Play_Shuffle_Tracks);
            this.mnuPlayShuffleTracks.Click += new System.EventHandler(this.mnuPlayShuffleTracks_Click);

            this.mnuPlayPlaySelectedTrackNext.ShortcutKeyDisplayString = "F10";
            this.mnuPlayPlaySelectedTrackNext.Text = Localization.Get(UI_Key.Menu_Play_Play_Selected_Track_Next);
            this.mnuPlayPlaySelectedTrackNext.Click += new EventHandler(mnuPlayPlaySelectedTrackNext_Click);

            this.mnuPlaylists.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuPlaylistsAddPlaylist,
                this.mnuPlaylistsDeletePlaylist,
                new ToolStripSeparator(),
                this.mnuPlaylistsAddSelectedTracksTo,
                this.mnuPlaylistsRemoveSelectedTracksFromPlaylist,
                new ToolStripSeparator(),
                this.mnuPlaylistsSwitchToNowPlaying,
                new ToolStripSeparator(),
                this.mnuPlaylistsAddTracksToTargetPlaylist,
                new ToolStripSeparator(),
                this.mnuPlaylistsEditAutoPlaylist,
                this.mnuPlaylistsConvertToStandardPlaylist,
                this.mnuPlaylistsRenameSelectedPlaylist});

            this.mnuPlaylists.Text = Localization.Get(UI_Key.Menu_Playlists);
            this.mnuPlaylists.DropDownOpening += new System.EventHandler(this.mnuPlaylists_DropDownOpening);

            this.mnuPlaylistsAddPlaylist.Text = Localization.Get(UI_Key.Menu_Playlist_Add_Playlist);
            this.mnuPlaylistsAddPlaylist.Click += new System.EventHandler(this.mnuPlaylistsAddPlaylist_Click);

            this.mnuPlaylistsDeletePlaylist.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuPlaylistsDeletePlaylistDummy});
            this.mnuPlaylistsDeletePlaylist.Text = Localization.Get(UI_Key.Menu_Playlist_Remove_Playlist);
            this.mnuPlaylistsDeletePlaylist.DropDownOpening += new System.EventHandler(this.mnuPlaylistsDeletePlaylist_DropDownOpening);

            this.mnuPlaylistsDeletePlaylistDummy.Text = String.Empty;

            this.mnuPlaylistsAddSelectedTracksTo.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuPlaylistsAddSelectedTracksToDummy});
            this.mnuPlaylistsAddSelectedTracksTo.Text = Localization.Get(UI_Key.Menu_Playlist_Add_Selected_Tracks_To);
            this.mnuPlaylistsAddSelectedTracksTo.DropDownOpening += new System.EventHandler(this.mnuPlaylistsAddSelectedTracksTo_DropDownOpening);

            this.mnuPlaylistsAddSelectedTracksToDummy.Text = String.Empty;

            this.mnuPlaylistsRemoveSelectedTracksFromPlaylist.ShortcutKeyDisplayString = "Delete";
            this.mnuPlaylistsRemoveSelectedTracksFromPlaylist.Text = Localization.Get(UI_Key.Menu_Playlist_Remove_Selected_Tracks_From);
            this.mnuPlaylistsRemoveSelectedTracksFromPlaylist.Click += new System.EventHandler(this.mnuPlaylistsRemoveSelectedTracksFromPlaylist_Click);

            this.mnuPlayAddTracksToNowPlaying.ShortcutKeyDisplayString = "F7";
            this.mnuPlayAddTracksToNowPlaying.Text = Localization.Get(UI_Key.Menu_Play_Add_To_Now_Playing);
            this.mnuPlayAddTracksToNowPlaying.Click += new System.EventHandler(this.mnuPlayAddTracksToNowPlaying_Click);

            this.mnuPlaylistsSwitchToNowPlaying.ShortcutKeyDisplayString = "Shift+F7";
            this.mnuPlaylistsSwitchToNowPlaying.Text = Localization.Get(UI_Key.Menu_Playlist_Switch_To_Now_Playing);
            this.mnuPlaylistsSwitchToNowPlaying.Click += new System.EventHandler(this.mnuPlaylistsSwitchToNowPlaying_Click);

            this.mnuPlaylistsAddTracksToTargetPlaylist.ShortcutKeyDisplayString = "Ctrl+F7";
            this.mnuPlaylistsAddTracksToTargetPlaylist.Text = String.Empty;
            this.mnuPlaylistsAddTracksToTargetPlaylist.Click += new System.EventHandler(this.mnuPlaylistsAddTracksToTargetPlaylist_Click);

            this.mnuPlaylistsEditAutoPlaylist.ShortcutKeyDisplayString = "F6";
            this.mnuPlaylistsEditAutoPlaylist.Text = Localization.Get(UI_Key.Menu_Playlist_Edit_Auto_Playlist);
            this.mnuPlaylistsEditAutoPlaylist.Click += new System.EventHandler(this.mnuPlaylistsEditAutoPlaylist_Click);

            this.mnuPlaylistsConvertToStandardPlaylist.ShortcutKeyDisplayString = "Shift+F6";
            this.mnuPlaylistsConvertToStandardPlaylist.Text = Localization.Get(UI_Key.Menu_Playlist_Convert_To_Standard_Playlist);
            this.mnuPlaylistsConvertToStandardPlaylist.Click += new System.EventHandler(this.mnuPlaylistsConvertToStandardPlaylist_Click);

            this.mnuPlaylistsRenameSelectedPlaylist.ShortcutKeyDisplayString = "F2";
            this.mnuPlaylistsRenameSelectedPlaylist.Text = Localization.Get(UI_Key.Menu_Playlist_Rename_Selected_Playlist);
            this.mnuPlaylistsRenameSelectedPlaylist.Click += new System.EventHandler(this.mnuPlaylistsRenameSelectedPlaylist_Click);

            this.mnuFilters.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuFiltersSelectNextFilter,
                this.mnuFiltersSelectPreviousFilter,
                this.mnuFiltersReleaseSelectedFilter,
                this.mnuFiltersReleaseAllFilters,
                new ToolStripSeparator(),
                this.mnuFiltersShowFilterIndex,
                new ToolStripSeparator(),
                this.mnuFiltersSelectFilter});

            this.mnuFilters.Text = Localization.Get(UI_Key.Menu_Filters);
            this.mnuFilters.DropDownOpening += new EventHandler(this.mnuFilters_DropDownOpening);

            this.mnuFiltersSelectNextFilter.ShortcutKeyDisplayString = "L";
            this.mnuFiltersSelectNextFilter.Text = Localization.Get(UI_Key.Menu_Filters_Select_Next_Filter);
            this.mnuFiltersSelectNextFilter.Click += new System.EventHandler(this.mnuFiltersSelectNextFilter_Click);

            this.mnuFiltersSelectPreviousFilter.ShortcutKeyDisplayString = "J";
            this.mnuFiltersSelectPreviousFilter.Text = Localization.Get(UI_Key.Menu_Filters_Select_Previous_Filter);
            this.mnuFiltersSelectPreviousFilter.Click += new System.EventHandler(this.mnuFiltersSelectPreviousFilter_Click);

            this.mnuFiltersReleaseSelectedFilter.ShortcutKeyDisplayString = "K";
            this.mnuFiltersReleaseSelectedFilter.Text = Localization.Get(UI_Key.Menu_Filters_Release_Selected_Filter);
            this.mnuFiltersReleaseSelectedFilter.Click += new System.EventHandler(this.mnuFiltersReleaseSelectedFilter_Click);

            this.mnuFiltersReleaseAllFilters.ShortcutKeyDisplayString = "I";
            this.mnuFiltersReleaseAllFilters.Text = Localization.Get(UI_Key.Menu_Filters_Release_All_Filters);
            this.mnuFiltersReleaseAllFilters.Click += new System.EventHandler(this.mnuFiltersReleaseAllFilters_Click);

            this.mnuFiltersShowFilterIndex.ShortcutKeyDisplayString = "/";
            this.mnuFiltersShowFilterIndex.Text = Localization.Get(UI_Key.Menu_Filters_Show_Filter_Index);
            this.mnuFiltersShowFilterIndex.Click += new EventHandler(mnuFiltersShowFilterIndex_Click);

            this.mnuFiltersSelectFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuFiltersSelectFilterPlaylists,
                this.mnuFiltersSelectFilterGenres,
                this.mnuFiltersSelectFilterArtists,
                this.mnuFiltersSelectFilterAlbums,
                this.mnuFiltersSelectFilterYears,
                this.mnuFiltersSelectFilterGroupings});
            this.mnuFiltersSelectFilter.Text = Localization.Get(UI_Key.Menu_Filters_Select_Filter);
            this.mnuFiltersSelectFilter.DropDownOpening += new System.EventHandler(this.mnuFiltersSelectFilter_DropDownOpening);

            this.mnuFiltersSelectFilterPlaylists.Text = Localization.Get(UI_Key.Menu_Filters_Select_Filter_Playlists);
            this.mnuFiltersSelectFilterPlaylists.ShortcutKeyDisplayString = "1";
            this.mnuFiltersSelectFilterPlaylists.Click += new System.EventHandler(this.mnuFiltersSelectFilterPlaylists_Click);

            this.mnuFiltersSelectFilterGenres.Text = Localization.Get(UI_Key.Menu_Filters_Select_Filter_Genres);
            this.mnuFiltersSelectFilterGenres.ShortcutKeyDisplayString = "2";
            this.mnuFiltersSelectFilterGenres.Click += new System.EventHandler(this.mnuFiltersSelectFilterGenres_Click);

            this.mnuFiltersSelectFilterArtists.Text = Localization.Get(UI_Key.Menu_Filters_Select_Filter_Artists);
            this.mnuFiltersSelectFilterArtists.ShortcutKeyDisplayString = "3";
            this.mnuFiltersSelectFilterArtists.Click += new System.EventHandler(this.mnuFiltersSelectFilterArtists_Click);

            this.mnuFiltersSelectFilterAlbums.Text = Localization.Get(UI_Key.Menu_Filters_Select_Filter_Albums);
            this.mnuFiltersSelectFilterAlbums.ShortcutKeyDisplayString = "4";
            this.mnuFiltersSelectFilterAlbums.Click += new System.EventHandler(this.mnuFiltersSelectFilterAlbums_Click);

            this.mnuFiltersSelectFilterYears.Text = Localization.Get(UI_Key.Menu_Filters_Select_Filter_Years);
            this.mnuFiltersSelectFilterYears.ShortcutKeyDisplayString = "5";
            this.mnuFiltersSelectFilterYears.Click += new System.EventHandler(this.mnuFiltersSelectFilterYear_Click);

            this.mnuFiltersSelectFilterGroupings.Text = Localization.Get(UI_Key.Menu_Filters_Select_Filter_Groupings);
            this.mnuFiltersSelectFilterGroupings.ShortcutKeyDisplayString = "6";
            this.mnuFiltersSelectFilterGroupings.Click += new System.EventHandler(this.mnuFiltersSelectFilterGroupings_Click);

            this.mnuView.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuViewShowFullScreen,
                new ToolStripSeparator(),
                this.mnuViewShowMiniPlayer,
                this.mnuViewUseMiniPlayerWhenMinimized,
                new ToolStripSeparator(),
                this.mnuViewFindCurrentlyPlayingTrack,
                this.mnuViewShowAllOf,
                new ToolStripSeparator(),
                this.mnuViewShowColumns,
                this.mnuViewResetColumns,
                new ToolStripSeparator(),
                this.mnuViewHTPCMode,
                this.mnuViewTagCloud,
                this.mnuViewAdvanceView,
                new ToolStripSeparator(),
                this.mnuViewShowAlbumArtOnMainScreen});
            this.mnuView.Text = Localization.Get(UI_Key.Menu_View);
            this.mnuView.DropDownOpening += new System.EventHandler(this.mnuView_DropDownOpening);

            this.mnuViewShowFullScreen.ShortcutKeyDisplayString = "Alt+Enter";
            this.mnuViewShowFullScreen.Text = Localization.Get(UI_Key.Menu_View_Show_Full_Screen);
            this.mnuViewShowFullScreen.Click += new System.EventHandler(this.mnuViewShowFullScreen_Click);

            this.mnuViewFindCurrentlyPlayingTrack.ShortcutKeyDisplayString = "C";
            this.mnuViewFindCurrentlyPlayingTrack.Text = Localization.Get(UI_Key.Menu_View_Find_Currently_Playing_Track);
            this.mnuViewFindCurrentlyPlayingTrack.Click += new System.EventHandler(this.mnuViewFindCurrentlyPlayingTrack_Click);

            this.mnuViewShowAllOf.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuViewShowAllOfThisArtist,
                this.mnuViewShowAllOfThisAlbum,
                this.mnuViewShowAllOfThisGenre,
                this.mnuViewShowAllOfThisYear,
                this.mnuViewShowAllOfThisGrouping});
            this.mnuViewShowAllOf.Text = Localization.Get(UI_Key.Menu_View_Show_All_Of);
            this.mnuViewShowAllOf.DropDownOpening += new System.EventHandler(this.mnuViewShowAllOf_DropDownOpening);

            this.mnuViewShowAllOfThisArtist.ShortcutKeyDisplayString = "F4";
            this.mnuViewShowAllOfThisArtist.Text = Localization.Get(UI_Key.Menu_View_This_Artist);
            this.mnuViewShowAllOfThisArtist.Click += new System.EventHandler(this.mnuViewShowAllOfThisArtist_Click);

            this.mnuViewShowAllOfThisAlbum.ShortcutKeyDisplayString = "Shift+F4";
            this.mnuViewShowAllOfThisAlbum.Text = Localization.Get(UI_Key.Menu_View_This_Album);
            this.mnuViewShowAllOfThisAlbum.Click += new System.EventHandler(this.mnuViewShowAllOfThisAlbum_Click);

            this.mnuViewShowAllOfThisGenre.ShortcutKeyDisplayString = "Ctrl+F4";
            this.mnuViewShowAllOfThisGenre.Text = Localization.Get(UI_Key.Menu_View_This_Genre);
            this.mnuViewShowAllOfThisGenre.Click += new System.EventHandler(this.mnuViewShowAllOfThisGenre_Click);

            this.mnuViewShowAllOfThisYear.Text = Localization.Get(UI_Key.Menu_View_This_Year);
            this.mnuViewShowAllOfThisYear.Click += new System.EventHandler(this.mnuViewShowAllOfThisYear_Click);

            this.mnuViewShowAllOfThisGrouping.Text = Localization.Get(UI_Key.Menu_View_This_Grouping);
            this.mnuViewShowAllOfThisGrouping.Click += new System.EventHandler(this.mnuViewShowAllOfThisGrouping_Click);

            this.mnuViewShowColumns.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuViewShowColumnsDummy});
            this.mnuViewShowColumns.Text = Localization.Get(UI_Key.Menu_View_Show_Columns);
            this.mnuViewShowColumns.DropDownOpening += new System.EventHandler(this.mnuViewShowColumns_DropDownOpening);

            this.mnuViewShowColumnsDummy.Text = String.Empty;

            this.mnuViewResetColumns.Text = Localization.Get(UI_Key.Menu_View_Reset_Columns);
            this.mnuViewResetColumns.Click += new System.EventHandler(this.mnuViewResetColumns_Click);

            this.mnuViewTagCloud.ShortcutKeyDisplayString = "Shift+F11";
            this.mnuViewTagCloud.Text = Localization.Get(UI_Key.Menu_View_Tag_Cloud);
            this.mnuViewTagCloud.Click += new EventHandler(mnuViewTagCloud_Click);

            this.mnuViewHTPCMode.ShortcutKeyDisplayString = "F11";
            this.mnuViewHTPCMode.Text = Localization.Get(UI_Key.Menu_View_HTPC_Mode);
            this.mnuViewHTPCMode.Click += new System.EventHandler(this.mnuViewHTPCMode_Click);

            this.mnuViewAdvanceView.ShortcutKeyDisplayString = "Space";
            this.mnuViewAdvanceView.Text = Localization.Get(UI_Key.Menu_View_Advance_View);
            this.mnuViewAdvanceView.Click += new System.EventHandler(this.mnuViewAdvanceView_Click);

            this.mnuViewShowAlbumArtOnMainScreen.Text = Localization.Get(UI_Key.Menu_View_Album_Art_On_Main_Screen);
            this.mnuViewShowAlbumArtOnMainScreen.Click += new System.EventHandler(this.mnuViewShowAlbumArtOnMainScreen_Click);

            this.mnuOptions.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[]
            {
                this.mnuOptionsDetectGamepad,
                new ToolStripSeparator(),
                this.mnuOptionsDecoderGain,
                this.mnuOptionsUseReplayGain,
                this.mnuOptionsAutoClippingControl,
                this.mnuOptionsUseEqualizer,
                this.mnuOptionsSelectNextEqualizer,
                this.mnuOptionsEqualizerSettings,
                new ToolStripSeparator(),
                this.mnuOptionsSpectrumViewGain,
                this.mnuOptionsHighResolutionSpectrum,
                new ToolStripSeparator(),
                this.mnuOptionsMoreOptions});

            this.mnuOptions.Text = Localization.Get(UI_Key.Menu_Options);
            this.mnuOptions.DropDownOpening += new System.EventHandler(this.mnuOptions_DropDownOpening);

            this.mnuViewUseMiniPlayerWhenMinimized.Text = Localization.Get(UI_Key.Menu_View_Use_Mini_Player);
            this.mnuViewUseMiniPlayerWhenMinimized.Click += new System.EventHandler(this.mnuViewUseMiniPlayerWhenMinimized_Click);

            this.mnuViewShowMiniPlayer.Text = Localization.Get(UI_Key.Menu_View_Show_Mini_Player);
            this.mnuViewShowMiniPlayer.ShortcutKeyDisplayString = "\\";
            this.mnuViewShowMiniPlayer.Click += new System.EventHandler(this.mnuViewShowMiniPlayer_Click);

            this.mnuOptionsDetectGamepad.Text = Localization.Get(UI_Key.Menu_Options_Detect_Gamepad);
            this.mnuOptionsDetectGamepad.Click += new System.EventHandler(this.mnuOptionsDetectGamepad_Click);

            this.mnuOptionsDecoderGain.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuOptionsDecoderGainDummy});
            this.mnuOptionsDecoderGain.Text = Localization.Get(UI_Key.Menu_Options_Decoder_Gain);
            this.mnuOptionsDecoderGain.DropDownOpening += new System.EventHandler(this.mnuOptionsDecoderGain_DropDownOpening);

            this.mnuOptionsDecoderGainDummy.Text = String.Empty;

            this.mnuOptionsUseReplayGain.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuOptionsReplayGainAlbum,
                this.mnuOptionsReplayGainTrack,
                this.mnuOptionsReplayGainOff,
                new ToolStripSeparator(),
                this.mnuOptionsReplayGainAnalyzeSelectedTracks,
                this.mnuOptionsReplayGainAnalyzeCancel,
                new ToolStripSeparator(),
                this.mnuOptionsReplayGainWriteTags});

            this.mnuOptionsUseReplayGain.Text = Localization.Get(UI_Key.Menu_Options_Replay_Gain);
            this.mnuOptionsUseReplayGain.DropDownOpening += new System.EventHandler(this.mnuOptionsUseReplayGain_DropDownOpening);

            this.mnuOptionsReplayGainAlbum.Text = Localization.Get(UI_Key.Menu_Options_Replay_Gain_Album);
            this.mnuOptionsReplayGainAlbum.Click += new System.EventHandler(this.mnuOptionsReplayGainAlbum_Click);

            this.mnuOptionsReplayGainTrack.Text = Localization.Get(UI_Key.Menu_Options_Replay_Gain_Track);
            this.mnuOptionsReplayGainTrack.Click += new System.EventHandler(this.mnuOptionsReplayGainTrack_Click);

            this.mnuOptionsReplayGainOff.Text = Localization.Get(UI_Key.Menu_Options_Replay_Gain_Off);
            this.mnuOptionsReplayGainOff.Click += new System.EventHandler(this.mnuOptionsReplayGainOff_Click);

            this.mnuOptionsReplayGainAnalyzeSelectedTracks.Text = Localization.Get(UI_Key.Menu_Options_Replay_Gain_Analyze_Selected_Tracks);
            this.mnuOptionsReplayGainAnalyzeSelectedTracks.ShortcutKeyDisplayString = "Shift+G";
            this.mnuOptionsReplayGainAnalyzeSelectedTracks.Click += new EventHandler(mnuOptionsReplayGainAnalyzeSelectedTracks_Click);

            this.mnuOptionsReplayGainAnalyzeCancel.Text = Localization.Get(UI_Key.Menu_Options_Replay_Gain_Analyze_Cancel);
            this.mnuOptionsReplayGainAnalyzeCancel.Click += new EventHandler(mnuOptionsReplayGainAnalyzeCancel_Click);

            this.mnuOptionsReplayGainWriteTags.Text = Localization.Get(UI_Key.Menu_Options_Replay_Gain_Write_Tags);
            this.mnuOptionsReplayGainWriteTags.Click += new EventHandler(mnuOptionsReplayGainWriteTags_Click);

            this.mnuOptionsAutoClippingControl.Text = Localization.Get(UI_Key.Menu_Options_Auto_Clipping_Control);
            this.mnuOptionsAutoClippingControl.Click += new System.EventHandler(this.mnuOptionsAutoClippingControl_Click);

            this.mnuOptionsUseEqualizer.ShortcutKeyDisplayString = "F12";
            this.mnuOptionsUseEqualizer.Text = Localization.Get(UI_Key.Menu_Options_Use_Equalizer);
            this.mnuOptionsUseEqualizer.Click += new System.EventHandler(this.mnuOptionsUseEqualizer_Click);

            this.mnuOptionsSelectNextEqualizer.ShortcutKeyDisplayString = "Shift+F12";
            this.mnuOptionsSelectNextEqualizer.Text = Localization.Get(UI_Key.Menu_Options_Select_Next_Equalizer_Setting);
            this.mnuOptionsSelectNextEqualizer.Click += new System.EventHandler(this.mnuOptionsSelectNextEqualizer_Click);

            this.mnuOptionsEqualizerSettings.ShortcutKeyDisplayString = "Ctrl+F12";
            this.mnuOptionsEqualizerSettings.Text = Localization.Get(UI_Key.Menu_Options_Equalizer_Settings);
            this.mnuOptionsEqualizerSettings.Click += new System.EventHandler(this.mnuOptionsEqualizerSettings_Click);

            this.mnuOptionsSpectrumViewGain.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.mnuOptionsSpectrumViewGainUp,
                this.mnuOptionsSpectrumViewGainDown});
            this.mnuOptionsSpectrumViewGain.Text = Localization.Get(UI_Key.Menu_Options_Spectrum_View_Gain);

            this.mnuOptionsSpectrumViewGainUp.ShortcutKeyDisplayString = "[";
            this.mnuOptionsSpectrumViewGainUp.Text = Localization.Get(UI_Key.Menu_Options_Spectrum_View_Gain_Up);
            this.mnuOptionsSpectrumViewGainUp.Click += new System.EventHandler(this.mnuOptionsSpectrumViewGainUp_Click);

            this.mnuOptionsSpectrumViewGainDown.ShortcutKeyDisplayString = "]";
            this.mnuOptionsSpectrumViewGainDown.Text = Localization.Get(UI_Key.Menu_Options_Spectrum_View_Gain_Down);
            this.mnuOptionsSpectrumViewGainDown.Click += new System.EventHandler(this.mnuOptionsSpectrumViewGainDown_Click);

            this.mnuOptionsHighResolutionSpectrum.Text = Localization.Get(UI_Key.Menu_Options_High_Resolution_Spectrum_Analyzer);
            this.mnuOptionsHighResolutionSpectrum.Click += new System.EventHandler(this.mnuOptionsHighResolutionSpectrum_Click);

            this.mnuInternetDownloadMissingCoverArt.Text = Localization.Get(UI_Key.Menu_Internet_Download_Cover_Art);
            this.mnuInternetDownloadMissingCoverArt.Click += new System.EventHandler(this.mnuInternetDownloadMissingCoverArt_Click);

            this.mnuOptionsLastFMOptions.Text = Localization.Get(UI_Key.Menu_Options_Last_FM);
            this.mnuOptionsLastFMOptions.Click += new EventHandler(mnuOptionsLastFMOptions_Click);

            this.mnuOptionsTwitterOptions.Text = Localization.Get(UI_Key.Menu_Options_Twitter);
            this.mnuOptionsTwitterOptions.Click += new System.EventHandler(this.mnuOptionsTwitterOptions_Click);

            this.mnuOptionsMoreOptions.Text = Localization.Get(UI_Key.Menu_Options_More_Options);
            this.mnuOptionsMoreOptions.Click += new System.EventHandler(this.mnuOptionsMoreOptions_Click);

            this.mnuInternet.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[]
            {
                this.mnuInternetRadio,
                this.mnuInternetRadioReload,
                new ToolStripSeparator(),
                this.mnuInternetShowInternetInfo,
                new ToolStripSeparator(),
                this.mnuInternetDownloadMissingCoverArt,
                new ToolStripSeparator(),
                this.mnuOptionsTwitterOptions,
                new ToolStripSeparator(),
                this.mnuOptionsLastFMOptions,
                this.mnuInternetShowArtist,
                this.mnuInternetShowAlbum
            });

            this.mnuInternet.Text = Localization.Get(UI_Key.Menu_Internet);
            this.mnuInternet.DropDownOpening += new EventHandler(mnuInternet_DropDownOpening);

            this.mnuInternetRadio.Text = Localization.Get(UI_Key.Menu_Internet_Radio);
            this.mnuInternetRadio.ShortcutKeyDisplayString = "R";
            this.mnuInternetRadio.Click += new EventHandler(mnuInternetRadio_Click);

            this.mnuInternetRadioReload.Text = Localization.Get(UI_Key.Menu_Internet_Radio_Reload);
            this.mnuInternetRadioReload.Click += new EventHandler(mnuInternetRadioReload_Click);

            this.mnuInternetShowArtist.Click += new EventHandler(mnuInternetShowArtist_Click);
            this.mnuInternetShowArtist.ShortcutKeyDisplayString = "Shift+F9";
            this.mnuInternetShowArtist.Text = Localization.Get(UI_Key.Menu_Internet_Artist_Info);
            this.mnuInternetShowAlbum.Click += new EventHandler(mnuInternetShowAlbum_Click);
            this.mnuInternetShowAlbum.ShortcutKeyDisplayString = "Ctrl+F9";
            this.mnuInternetShowAlbum.Text = Localization.Get(UI_Key.Menu_Internet_Album_Info);

            this.mnuInternetShowInternetInfo.ShortcutKeyDisplayString = "F9";
            this.mnuInternetShowInternetInfo.Text = Localization.Get(UI_Key.Menu_Internet_Show_Track_And_Album_Details);
            this.mnuInternetShowInternetInfo.Click += new System.EventHandler(this.mnuInternetShowInternetInfo_Click);

            this.mnuHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[]
            {
                this.mnuHelpOnlineHelp,
                this.mnuHelpShowGamepadHelp,
                this.mnuHelpOtherLanguages,
                new ToolStripSeparator(),
                this.mnuHelpCheckForUpdate,
                new ToolStripSeparator(),
                this.mnuHelpVisitWebSite,
                this.mnuHelpAbout
            });

            this.mnuHelp.Text = Localization.Get(UI_Key.Menu_Help);
            this.mnuHelp.DropDownOpening += new System.EventHandler(this.mnuHelp_DropDownOpening);

            this.mnuHelpOnlineHelp.Text = Localization.Get(UI_Key.Menu_Help_Online_Help);
            this.mnuHelpOnlineHelp.Click += new System.EventHandler(this.mnuHelpOnlineHelp_Click);

            this.mnuHelpShowGamepadHelp.Text = Localization.Get(UI_Key.Menu_Help_Gamepad_Help);
            this.mnuHelpShowGamepadHelp.Click += new System.EventHandler(this.mnuHelpShowGamePadHelp_Click);

            this.mnuHelpOtherLanguages.Text = Localization.Get(UI_Key.Menu_Help_Other_Languages);
            this.mnuHelpOtherLanguages.Click += new EventHandler(this.mnuHelpOtherLanguages_Click);

            this.mnuHelpCheckForUpdate.Text = Localization.Get(UI_Key.Menu_Help_Check_For_Update);
            this.mnuHelpCheckForUpdate.Click += new System.EventHandler(this.mnuHelpCheckForUpdate_Click);

            this.mnuHelpVisitWebSite.Text = Localization.Get(UI_Key.Menu_Help_Visit_Company_Website, Application.CompanyName);
            this.mnuHelpVisitWebSite.Click += new System.EventHandler(this.mnuHelpVisitWebSite_Click);

            this.mnuHelpAbout.Text = Localization.Get(UI_Key.Menu_Help_About, Application.ProductName);
            this.mnuHelpAbout.Click += new System.EventHandler(this.mnuHelpAbout_Click);

            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.mnuMain);
            this.Icon = Properties.Resources.quuxplayer;
            this.KeyPreview = true;
            this.MainMenuStrip = this.mnuMain;
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Text = "QuuxPlayer";
            this.WindowState = System.Windows.Forms.FormWindowState.Normal;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.mnuMain.ResumeLayout(false);
            this.mnuMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void mnuFiltersShowFilterIndex_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.ShowFilterIndex);
        }
        private void mnuPlayAddThisAlbumToNowPlaying_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.AddAlbumToNowPlaying);
        }
        private void mnuProPurchase_Click(object sender, EventArgs e)
        {
            controller.RequestAction(QActionType.BuyPro);
        }
    }
}