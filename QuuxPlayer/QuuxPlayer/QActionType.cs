﻿/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxPlayer
{
    internal enum QActionType
    {
        None,
        AddFileToLibrary,
        AdvanceScreenWithoutMouse,
        AdvanceScreen,
        AskAboutMissingFiles,
        Play,
        Stop,
        StopAfterThisTrack,
        Pause,
        PlayPause,
        Previous,
        Next,
        ReleaseAllFilters,
        ShowFilterIndex,
        SelectAll,
        SelectNone,
        InvertSelection,
        CreateNewPlaylist,
        FindPlayingTrack,
        ScanBack,
        ScanFwd,
        VolumeDown,
        VolumeUp,
        VolumeDownLarge,
        VolumeUpLarge,
        SetVolume,
        Mute,
        SpectrumViewGainDown,
        AdvanceSortColumn,
        SpectrumViewGainUp,
        Shuffle,
        RepeatToggle,
        SelectNextItemGamePadRight,
        SelectPreviousItemGamePadRight,
        NextFilter,
        LoadFilterValues,
        PreviousFilter,
        SelectNextItemGamePadLeft,
        SelectPreviousItemGamePadLeft,
        ReleaseCurrentFilter,
        ShowInWindowsExplorer,
        ShowAboutScreen,
        MoveRight,
        MoveLeft,
        ViewNowPlaying,
        PlayThisAlbum,
        PlayRandomAlbum,
        ClearTargetPlaylist,
        FilterSelectedAlbum,
        FilterSelectedArtist,
        FilterSelectedGenre,
        FilterSelectedYear,
        FilterSelectedGrouping,
        RemoveNonExistentTracks,
        RefreshAll,
        MoveUp,
        MoveDown,
        MoveTracksUp,
        MoveTracksDown,
        Cancel,
        Delete,
        HTPCMode,
        ShowEqualizer,
        ToggleEqualizer,
        PageUp,
        PageDown,
        AddToTargetPlaylist,
        AddToTargetPlaylistAndAdvance,
        AddToNowPlaying,
        AddToNowPlayingAndAdvance,
        EditAutoPlaylist,
        KeyPreviewChange,
        ConvertToStandardPlaylist,
        RenameSelectedPlaylist,
        RenameSelectedRadioGenre,
        RepeatAction,
        RemoveSelectedPlaylist,
        InitializeGamepad,
        AddFolderToLibrary,
        RefreshLibrary,
        RefreshSelectedTracks,
        ExportCSV,
        ExportCurrentView,
        TogglePlayPause,
        ShowTrackDetails,
        SavePlaylist,
        PlayFirstTrack,
        PlaySelectedTracks, 
        ExportPlaylist,
        ImportPlaylist,
        Home,
        End,
        SelectPlayingTrack,
        ShowTrackAndAlbumDetails,
        EndOfTrack,
        RequestNextTrack,
        StartOfTrack,
        StartOfTrackAuto,
        EndOfAllTracks,
        TrackFailed,
        Paused,
        Resumed,
        Stopped,
        CheckForUpdate,
        ToggleGamepadHelp,
        ShowFileDetails,
        VisitWebSite,
        SelectNextEqualizer,
        SetFullScreen,
        ReleaseFullScreen,
        ReleaseFullScreenAuto,
        ShowCrawlDialog,
        ResetPlayHistory,
        ReplayGainOff,
        ReplayGainTrack,
        ReplayGainAlbum,
        ReplayGainAnalyzeSelectedTracks,
        ReplayGainAnalyzeCancel,
        UpdateTrackCount,
        ToggleSpectrumResolution,
        SetSleepOptions,
        Exit,
        SetLockOptions,
        UnlockControls,
        ForceUnlockControls,
        VolumeDownForSleep,
        SetMainFormTitle,
        FocusSearchBox,
        ToggleDownloadCoverArt,
        TwitterOptions,
        LastFMOptions,
        MakeForeground,
        PlaySelectedTrackNext,
        SetFileAssociations,
        TogglePodcastView,
        AddAlbumToNowPlaying,
        BuyPro,
        LastFMShowArtist,
        LastFMShowAlbum,
        ShowTagCloud,
        ToggleRadioMode,
        ShowAllOfArtist,
        ShowAllOfAlbum,
        ShowAllOfGrouping,
        ShowAllOfGenre,
        ShowMiniPlayer,
        HideMiniPlayer,
        EditTags,
        OrganizeLibrary,
        SuggestDuplicatesBitrate,
        SuggestDuplicatesOldest,
        SuggestDuplicatesNewest,
        SuggestDuplicatesHighestRated,
        UpdateRadioStation,
        DisplayRadioInfo,
        RadioFailed,
        RadioStreamStarted,
        ReloadRadioStations
    }
}
