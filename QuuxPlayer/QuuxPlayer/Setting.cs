/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxPlayer
{
    internal static class Setting
    {
        public static void Start()
        {
            Mute = Database.GetSetting(SettingType.Mute, false);
            AutoClipControl = Database.GetSetting(SettingType.AutoClipControl, true);

            MoveNewFilesIntoMain = Database.GetSetting(SettingType.CopyNewFilesIntoMain, false);
            DefaultRenameFormat = (TrackWriter.RenameFormat)Database.GetSetting(SettingType.DefaultRenameFormat, (int)TrackWriter.RenameFormat.None);
            KeepOrganized = Database.GetSetting(SettingType.KeepOrganized, false);
            DefaultDirectoryFormat = (TrackWriter.DirectoryFormat)Database.GetSetting(SettingType.DefaultDirectoryFormat, (int)0);
            TopLevelDirectory = Database.GetSetting(SettingType.TopLevelDirectory, String.Empty);

            DisableScreensaver = Database.GetSetting(SettingType.DisableWindowsScreensavers, true);
            DisableScreensaverOnlyWhenPlaying = Database.GetSetting(SettingType.DisableScreensaverOnlyWhenPlaying, true);
            IncludeTagCloud = Database.GetSetting(SettingType.IncludeTagCloud, false);
            ShortTrackCutoff = Database.GetSetting(SettingType.ShortTrackCutoff, 5);
            SplitterDistance = Database.GetSetting(SettingType.SplitterDistance, 180);

            ShowAlbumArtOnMainScreen = Database.GetSetting(SettingType.ShowAlbumArtOnMainScreen, true);
            
            PodcastDownloadDirectory = Database.GetSetting(SettingType.PodcastDownloadDirectory, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            PodcastDownloadSchedule = (PodcastDownloadSchedule)Database.GetSetting(SettingType.PodcastDownloadSchedule, (int)PodcastDownloadSchedule.EveryTime);
            PodcastDownloadDisposition = (PodcastDownloadDisposition)Database.GetSetting(SettingType.PodcastDownloadDisposition, (int)PodcastDownloadDisposition.DownloadNone);
            PodcastMaxConcurrentDownloads = Database.GetSetting(SettingType.PodcastMaxConcurrentDownloads, (int)1);

            UseMiniPlayer = Database.GetSetting(SettingType.UseMiniPlayer, true);
            WriteReplayGainTags = Database.GetSetting(SettingType.WriteReplayGainTags, false);

            ReplayGain = (ReplayGainMode)Database.GetSetting(SettingType.ReplayGain, (int)ReplayGainMode.Off);

            AutoCheckForUpdates = Database.GetSetting(SettingType.AutoCheckUpdates, true);
            StopClearsNowPlaying = Database.GetSetting(SettingType.StopClearsNowPlaying, true);
            SaveNowPlayingOnExit = Database.GetSetting(SettingType.SaveNowPlayingOnExit, false);
        }
        public static void Save()
        {
            Database.SaveSetting(SettingType.Mute, Mute);
            Database.SaveSetting(SettingType.AutoClipControl, AutoClipControl);

            Database.SaveSetting(SettingType.CopyNewFilesIntoMain, MoveNewFilesIntoMain);
            Database.SaveSetting(SettingType.DefaultRenameFormat, (int)DefaultRenameFormat);
            Database.SaveSetting(SettingType.KeepOrganized, KeepOrganized);
            Database.SaveSetting(SettingType.DefaultDirectoryFormat, (int)DefaultDirectoryFormat);
            Database.SaveSetting(SettingType.TopLevelDirectory, TopLevelDirectory);

            Database.SaveSetting(SettingType.DisableWindowsScreensavers, DisableScreensaver);
            Database.SaveSetting(SettingType.IncludeTagCloud, IncludeTagCloud);
            Database.SaveSetting(SettingType.ShortTrackCutoff, ShortTrackCutoff);
            Database.SaveSetting(SettingType.SplitterDistance, SplitterDistance);

            Database.SaveSetting(SettingType.ShowAlbumArtOnMainScreen, ShowAlbumArtOnMainScreen);
            
            Database.SaveSetting(SettingType.PodcastDownloadDirectory, PodcastDownloadDirectory);
            Database.SaveSetting(SettingType.PodcastDownloadSchedule, (int)PodcastDownloadSchedule);
            Database.SaveSetting(SettingType.PodcastDownloadDisposition, (int)PodcastDownloadDisposition);
            Database.SaveSetting(SettingType.PodcastMaxConcurrentDownloads, PodcastMaxConcurrentDownloads);

            Database.SaveSetting(SettingType.UseMiniPlayer, UseMiniPlayer);
            Database.SaveSetting(SettingType.WriteReplayGainTags, WriteReplayGainTags);
            Database.SaveSetting(SettingType.ReplayGain, (int)ReplayGain);
            Database.SaveSetting(SettingType.AutoCheckUpdates, AutoCheckForUpdates);
            Database.SaveSetting(SettingType.StopClearsNowPlaying, StopClearsNowPlaying);
            Database.SaveSetting(SettingType.SaveNowPlayingOnExit, SaveNowPlayingOnExit);
            Database.SaveSetting(SettingType.DisableScreensaverOnlyWhenPlaying, DisableScreensaverOnlyWhenPlaying);
        }
    
        public static bool Mute { get; set; }
        public static bool AutoClipControl { get; set; }

        public static bool MoveNewFilesIntoMain { get; set; }
        public static bool KeepOrganized { get; set; }
        public static TrackWriter.RenameFormat DefaultRenameFormat { get; set; }
        public static TrackWriter.DirectoryFormat DefaultDirectoryFormat { get; set; }
        public static string TopLevelDirectory { get; set; }

        public static bool DisableScreensaver { get; set; }
        public static bool DisableScreensaverOnlyWhenPlaying { get; set; }
        public static bool IncludeTagCloud { get; set; }
        public static int ShortTrackCutoff { get; set; }

        private static int splitterDistance;
        public static int SplitterDistance
        {
            get { return splitterDistance; }
            set { splitterDistance = value; }
        }
        public static bool ShowAlbumArtOnMainScreen { get; set; }
        public static string PodcastDownloadDirectory { get; set; }
        public static PodcastDownloadSchedule PodcastDownloadSchedule { get; set; }
        public static PodcastDownloadDisposition PodcastDownloadDisposition { get; set; }
        public static int PodcastMaxConcurrentDownloads { get; set; }
        public static bool UseMiniPlayer { get; set; }
        public static bool WriteReplayGainTags { get; set; }
        public static ReplayGainMode ReplayGain { get; set; }
        public static bool AutoCheckForUpdates { get; set; }
        public static bool StopClearsNowPlaying { get; set; }
        public static bool SaveNowPlayingOnExit { get; set; }
    }
}
