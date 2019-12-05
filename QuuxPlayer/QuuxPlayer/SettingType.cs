/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;

namespace QuuxPlayer
{
    internal enum ArtSaveOption { Folder_JPG, Artist_Album, None }

    internal enum SettingType
    {
        // Only add to the end to ensure database upgradability

        DownloadAlbumCovers,
        AskAboutMissingFiles,
        AutoCheckUpdates,
        AutoClipControl,
        ColumnStatus,
        ColumnStatusHTPC,
        CurrentEqualizer,
        CurrentFilter,
        DecoderGain,
        EqualizerOn,
        EqualizerFineControl,
        EqualizerTenBands,
        Mute,
        ShowSpectrumGrid,
        ShowAlbumArtOnMainScreen,
        SpectrumGain,
        ViewMode,
        FullScreen,
        ReplayGain,
        SpectrumSmall,
        SleepAction,
        SleepDelay,
        SleepFadeDelay,
        ShortTrackCutoff,
        SleepForce,
        NormalWindowBoundsX,
        NormalWindowBoundsY,
        NormalWindowBoundsWidth,
        NormalWindowBoundsHeight,
        FileDialogFilterIndex,
        ArtSave,
        UseGlobalHotKeys,
        TwitterOn,
        TwitterUserName,
        TwitterPassword,
        LocalVolumeControlOnly,
        DisableWindowsScreensavers,
        LocalVolumeLevel,
        LastFMOn,
        LastFMUserName,
        LastFMPassword,
        RunNumber,
        OutputDeviceName,
        TwitterMode,
        TagCloudMaxItems,
        TagCloudColor,
        IncludeTagCloud,
        SplitterDistance,
        MiniPlayerXPos,
        MiniPlayerYPos,
        UseMiniPlayer,
        TopLevelDirectory,
        DefaultDirectoryFormat,
        DefaultRenameFormat,
        KeepOrganized,
        CopyNewFilesIntoMain,
        PodcastDownloadDirectory,
        PodcastDownloadSchedule,
        PodcastDownloadDisposition,
        PodcastMaxConcurrentDownloads,
        WriteReplayGainTags,
        StopClearsNowPlaying,
        SaveNowPlayingOnExit,
        DisableScreensaverOnlyWhenPlaying
    }
}
