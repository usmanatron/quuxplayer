/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal sealed partial class Controller
    {
        public void CreateNewDatabase()
        {
            Crawler.Stop();
            TrackWriter.Stop();
            
            if (rg != null)
                rg.Cancel();

            iTunes.Cancel = true;

            System.Threading.Thread.Sleep(200);

            normal.AllowEvents = false;
            normal.ReleaseAllFilters();
            normal.AllowEvents = true;

            artwork.CurrentTrack = null;
            artwork.TemporaryTrack = null;
            normal.Artwork.CurrentTrack = null;
            normal.Artwork.TemporaryTrack = null;

            Database.CreateNewDatabase(false);

            TargetPlaylistName = Localization.MY_PLAYLIST;
            
            normal.RefreshAll(true);
        }
        public void Close()
        {
            exiting = true;

            Lib.EnableNavKeys(true);
            normal.RemoveFilterIndex();

            Crawler.Stop();
            TrackWriter.Stop();
            
            if (rg != null)
                rg.Cancel();
            
            iTunes.Cancel = true;

            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon = null;
            }

            if (normal.Queue.Reordered && normal.Queue.PlaylistBasis.Length > 0 && (Database.GetPlaylistType(normal.Queue.PlaylistBasis) == PlaylistType.Standard))
                Database.SaveStandardPlaylist(normal.Queue);

            player.Dispose();
            
            Database.SaveSetting(SettingType.ShowSpectrumGrid, spectrumView.ShowGrid);
            Database.SaveSetting(SettingType.SpectrumGain, spectrumView.Gain);
            Database.SaveSetting(SettingType.ViewMode, (HTPCMode == HTPCMode.Normal ? "Normal" : "HTPC"));
            Database.SaveSetting(SettingType.CurrentFilter, normal.CurrentFilterName);
            Database.SaveSetting(SettingType.DecoderGain, DecoderGainDB);
            Database.SaveSetting(SettingType.ColumnStatus, normal.SerializeColumnStatus(HTPCMode.Normal));
            Database.SaveSetting(SettingType.ColumnStatusHTPC, normal.SerializeColumnStatus(HTPCMode.HTPC));
            Database.SaveSetting(SettingType.AskAboutMissingFiles, askAboutMissingFiles);
            Database.SaveSetting(SettingType.EqualizerOn, equalizer.On);
            Database.SaveSetting(SettingType.EqualizerTenBands, equalizer.NumBands == 10);
            Database.SaveSetting(SettingType.EqualizerFineControl, equalizer.FineControl);
            Database.SaveSetting(SettingType.CurrentEqualizer, equalizer.CurrentEqualizer.Name);
            Database.SaveSetting(SettingType.DownloadAlbumCovers, Track.DownloadCoverArt);
            Database.SaveSetting(SettingType.UseGlobalHotKeys, UseGlobalHotKeys);
            Database.SaveSetting(SettingType.FullScreen, Lib.FullScreen || !Lib.ManualReleaseFullScreen);
            Database.SaveSetting(SettingType.SpectrumSmall, (player.SpectrumMode == SpectrumMode.Small));
            Database.SaveSetting(SettingType.ArtSave, (int)ArtSaveOption);
            Database.SaveSetting(SettingType.TwitterOn, Twitter.On);
            Database.SaveSetting(SettingType.TwitterUserName, Twitter.UserName);
            Database.SaveSetting(SettingType.TwitterPassword, Twitter.Password);
            
            Database.SaveSetting(SettingType.LocalVolumeControlOnly, LocalVolumeControl);
            Database.SaveSetting(SettingType.LocalVolumeLevel, localVolumeLevel);
            Database.SaveSetting(SettingType.RunNumber, runNumber);
            Database.SaveSetting(SettingType.OutputDeviceName, player.OutputDeviceName);
            Database.SaveSetting(SettingType.TwitterMode, (int)Twitter.TwitterMode);
            Database.SaveSetting(SettingType.TagCloudMaxItems, tagCloud.MaxItems);
            Database.SaveSetting(SettingType.TagCloudColor, tagCloud.UseColor);
            Setting.Save();

            System.Drawing.Rectangle r;
            if (!Lib.FullScreen && mainForm.WindowState == FormWindowState.Normal)
                r = mainForm.Bounds;
            else
                r = System.Drawing.Rectangle.Empty;

            Database.SaveSetting(SettingType.NormalWindowBoundsX, r.X);
            Database.SaveSetting(SettingType.NormalWindowBoundsY, r.Y);
            Database.SaveSetting(SettingType.NormalWindowBoundsWidth, r.Width);
            Database.SaveSetting(SettingType.NormalWindowBoundsHeight, r.Height);

            Database.SaveSetting(SettingType.LastFMOn, lastFMOn);
            Database.SaveSetting(SettingType.LastFMUserName, lastFMUserName);
            Database.SaveSetting(SettingType.LastFMPassword, lastFMPassword);

            Database.SaveSetting(SettingType.MiniPlayerXPos, frmMiniPlayer.DefaultLocation.X);
            Database.SaveSetting(SettingType.MiniPlayerYPos, frmMiniPlayer.DefaultLocation.Y);

            Database.Close(mainForm);

            Clock.DoOnNewThreadNotBackground(TrackWriter.DeleteItemsLastChance);

            Clock.Close();
        }
        public bool PlaylistExists(string PlaylistName)
        {
            if (Database.PlaylistExists(PlaylistName))
            {
                return true;
            }
            else
            {
                if (TargetPlaylistName == PlaylistName)
                    TargetPlaylistName = String.Empty;

                return false;
            }
        }
        public void AddToPlaylist(string PlaylistName, List<Track> Queue)
        {
            if (Queue.Count > 0)
            {
                Database.AddToPlaylist(PlaylistName, Queue);

                if (normal.ActivePlaylist == PlaylistName)
                {
                    normal.RefreshTrackList(true);
                }
            }
        }
        public void AddToPlaylist(string PlaylistName, Track Track)
        {
            Database.AddToPlaylist(PlaylistName, Track);
            
            if (normal.ActivePlaylist.Length > 0)
                normal.RefreshTrackList(true);
        }
        private void MakeTrackNextToPlay(Track Track)
        {
            TrackQueue tq = Database.GetPlaylistTracks(Localization.NOW_PLAYING);

            if (tq.Count == 0)
            {
                tq.Append(Track);
                AddToPlaylist(Localization.NOW_PLAYING, Track);
            }
            else
            {
                if (tq[0] == player.PlayingTrack)
                    Database.InsertInNowPlaying(Track, 1);
                else
                    Database.InsertInNowPlaying(Track, 0);
            }
            if (normal.NowPlayingVisible)
            {
                normal.RefreshTrackList(true);
            }
        }
        public void DeletePlaylist(string PlaylistName)
        {
            bool sort = (String.Compare(CurrentPlaylist, PlaylistName, true) == 0);

            if (Database.DeletePlaylist(PlaylistName))
            {
                if (String.Compare(TargetPlaylistName, PlaylistName, true) == 0)
                    TargetPlaylistName = Localization.NOW_PLAYING;

                if (normal.ActivePlaylist == PlaylistName)
                {
                    normal.AllowEvents = false;
                    normal.ReleaseFilter(FilterType.Playlist);
                    normal.AllowEvents = true;
                    normal.RefreshAll(sort);
                }
                else if (normal.CurrentFilterType == FilterType.Playlist)
                {
                    normal.LoadFilterValues();
                }
            }
        }
        public List<Track> GetAlbumTracks(Track Track)
        {
            return Database.FindAllTracks(t => t.MainGroup == Track.MainGroup && t.Album == Track.Album);
        }
        public void SetPlaylistExpression(string PlaylistName, string Expression, bool MakeAuto)
        {
            Database.SetPlaylistExpression(PlaylistName, Expression, MakeAuto);
            if (normal.ActivePlaylist == PlaylistName)
            {
                normal.RefreshTrackList(true);
            }
        }
    }
}