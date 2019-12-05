/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal enum HTPCMode { Normal, HTPC }
    internal enum CommandLineActionType { Play, Enqueue, PlayNext, Add }

    internal sealed partial class Controller : IActionHandler
    {
        private const int VOLUME_ADJUST_FRACTIONAL_CHANGE = 50; // lower is coarser (1 part in...)

        private enum PlayType { SelectedTracks, NextTrackManual, PreviousTrack, Resume, FirstTrack, RequestNext }
        
        private Player player;
        private frmMain mainForm;
        private NormalView normal;
        private Gamepad pad;
        private static TrackDisplay trackDisplay;
        private ControlPanel controlPanel;
        private ProgressBar progressBar;
        private Artwork artwork;
        private Equalizer equalizer;
        private AlbumDetails albumDetails;
        private SpectrumView spectrumView;
        private TagCloud tagCloud;
        private Radio radio;
        private PodcastManager podcastManager;
        private ViewType currentView = ViewType.None;
        private HTPCMode htpcMode = HTPCMode.Normal;
        private NotifyIcon notifyIcon = null;
        private frmSplash splashScreen;
        private Sleep sleep = null;
        private QLock qLock = null;
        private KeyboardHook keyHook = null;
        private Track pendingScrobbleTrack = null;
        private frmMiniPlayer miniPlayer = null;
        private ReplayGain rg = null;
        private string playlistTargetForDraggedTracks = String.Empty;
        private string targetPlaylistName = String.Empty;
        private bool askAboutMissingFiles = true;
        private bool localVolumeControl;
        private float localVolumeLevel = -100.0f;
        private string htmlResp;
        private bool urlResponded;
        private string url;
        private bool useGlobalHotKeys = false;
        private bool exiting = false;

        private ulong preloadTimer = Clock.NULL_ALARM;
        private ulong updateTimer = Clock.NULL_ALARM;
        private ulong lastFMTimer = Clock.NULL_ALARM;
        private ulong trackCountTimer = Clock.NULL_ALARM;
        private bool lastFMOn;
        private string lastFMUserName;
        private string lastFMPassword;
        private IActionHandler tempHandler = null;
        private int runNumber = 0;

        private static Controller instance;

        public Controller(frmMain MainForm,
                          NormalView Normal,
                          ControlPanel ControlPanel,
                          TrackDisplay TrackDisplay,
                          ProgressBar ProgressBar,
                          Artwork Artwork,
                          Equalizer Equalizer,
                          AlbumDetails AlbumDetails,
                          SpectrumView SpectrumView,
                          TagCloud TagCloud,
                          Radio Radio,
                          PodcastManager PodcastManager)
        {
            instance = this;

            this.normal = Normal;
            Database.Start();
            this.normal.Controller = this;

            Clock.Start(clockTick);

            splashScreen = new frmSplash();
            DateTime now = DateTime.Now;
            
            mainForm = MainForm;

            splashScreen.Show(mainForm);

            setMainFormTitle();
            
            trackDisplay = TrackDisplay;
            controlPanel = ControlPanel;
            progressBar = ProgressBar;
            artwork = Artwork;
            equalizer = Equalizer;
            albumDetails = AlbumDetails;
            spectrumView = SpectrumView;
            tagCloud = TagCloud;
            radio = Radio;
            radio.Controller = this;

            podcastManager = PodcastManager;

            player = new Player(mainForm,
                                Database.GetSetting(SettingType.OutputDeviceName, String.Empty),
                                (Database.GetSetting(SettingType.SpectrumSmall, false) ? SpectrumMode.Small : SpectrumMode.Normal));

            Track.Controller = this;
            
            podcastManager.Controller = this;
            controlPanel.Controller = this;
            controlPanel.Player = player;
            albumDetails.Controller = this;

            initGamepad();

            TargetPlaylistName = String.Empty;

            runNumber = Database.GetSetting(SettingType.RunNumber, 0) + 1;
            askAboutMissingFiles = Database.GetSetting(SettingType.AskAboutMissingFiles, true);
            UseGlobalHotKeys = Database.GetSetting(SettingType.UseGlobalHotKeys, true);
            LocalVolumeControl = Database.GetSetting(SettingType.LocalVolumeControlOnly, false);
            LocalVolumeLevel = Database.GetSetting(SettingType.LocalVolumeLevel, 0f);
            ArtSaveOption = (ArtSaveOption)Database.GetSetting(SettingType.ArtSave, (int)ArtSaveOption.Artist_Album);
            DecoderGainDB = Database.GetSetting(SettingType.DecoderGain, 0f);
            spectrumView.ShowGrid = Database.GetSetting(SettingType.ShowSpectrumGrid, true);
            spectrumView.Gain = Database.GetSetting(SettingType.SpectrumGain, SpectrumView.DEFAULT_GAIN);

            Setting.Start();

            player.ReplayGain = Setting.ReplayGain;

            this.Mute(Setting.Mute);

            equalizer.EqChangePreset += new Equalizer.EqualizerChanged(updateVolume);
            equalizer.EqChanged += () => { player.ResetEqualizer(equalizer.NumBands, equalizer.ValueDB); };
            equalizer.EqToggleOnOff += () => { player.EqualizerOn = equalizer.On; };

            equalizer.On = Database.GetSetting(SettingType.EqualizerOn, false);
            equalizer.NumBands = Database.GetSetting(SettingType.EqualizerTenBands, false) ? 10 : 30;
            equalizer.FineControl = Database.GetSetting(SettingType.EqualizerFineControl, false);

            equalizer.SetEqualizer(Database.GetSetting(SettingType.CurrentEqualizer, Equalizer.DEFAULT_EQ_NAME));

            player.ResetEqualizer(equalizer.NumBands, equalizer.ValueDB);
            player.EqualizerOn = equalizer.On;

            WinAudioLib.VolumeChanged += (s, e) => { if (!LocalVolumeControl) updateVolume(); };
            
            updateVolume();
            
            progressBar.SetTrackProgress += new ProgressBar.TrackProgress(progressBarClick);

            spectrumView.Click += (s, e) => { RequestAction(QActionType.AdvanceScreen); };
            artwork.Click += (s, e) => { RequestAction(QActionType.AdvanceScreen); };

            setView(ViewType.Normal, false, false);

            switch (Database.GetSetting(SettingType.ViewMode, "Normal"))
            {
                case "Normal":
                    HTPCMode = HTPCMode.Normal;
                    break;
                case "HTPC":
                    HTPCMode = HTPCMode.HTPC;
                    break;
            }
            Track.DownloadCoverArt = Database.GetSetting(SettingType.DownloadAlbumCovers, false);
            initGamepad();

            if (Setting.AutoCheckForUpdates)
            {
                Clock.DoOnMainThread(checkForUpdate, 8000);
            }
            
            Clock.DoOnMainThread(RemoveSplashScreen, 1000);

            setupWindow();

            tagCloud.Start(mainForm.getMainArea(true), Database.GetSetting(SettingType.TagCloudMaxItems, 100), Database.GetSetting(SettingType.TagCloudColor, false));

            Twitter.On = Database.GetSetting(SettingType.TwitterOn, false);
            Twitter.UserName = Database.GetSetting(SettingType.TwitterUserName, String.Empty);
            Twitter.Password = Database.GetSetting(SettingType.TwitterPassword, String.Empty);
            Twitter.TwitterMode = (Twitter.Mode)Database.GetSetting(SettingType.TwitterMode, (int)0);

            lastFMOn = Database.GetSetting(SettingType.LastFMOn, false);
            lastFMUserName = Database.GetSetting(SettingType.LastFMUserName, String.Empty);
            lastFMPassword = Database.GetSetting(SettingType.LastFMPassword, String.Empty);

            frmMiniPlayer.DefaultLocation = new Point(Database.GetSetting(SettingType.MiniPlayerXPos, Int32.MinValue),
                                                      Database.GetSetting(SettingType.MiniPlayerYPos, 0));

            if (frmMiniPlayer.DefaultLocation.X == Int32.MinValue)
            {
                frmMiniPlayer.DefaultLocation = frmMiniPlayer.BottomRightDock;
            }

            Lib.MainForm = mainForm;

            doCommandLine();

            Clock.DoOnNewThread(processUnsavedTracks, 1000);

            if (Database.CrawlDirs.Count > 0)
                Clock.DoOnMainThread(crawl, 5000);

            Clock.DoOnNewThread(FileSoftRefresher.RefreshTracks, 2000);

            normal.CurrentFilterName = Database.GetSetting(SettingType.CurrentFilter, Localization.Get(UI_Key.Filter_Playlist));

            Clock.DoOnNewThread(PodcastManager.StartRefreshSubscriptionInfo, 5000);
            
        }

        private void setupWindow()
        {
            try
            {
                if (Database.GetSetting(SettingType.FullScreen, false))
                {
                    Lib.SetWindowFullScreen(mainForm, true, FormBorderStyle.None, false);
                }
                else
                {
                    Lib.ManualReleaseFullScreen = true;
                    bool isOK = false;
                    if (Database.GetSetting(SettingType.NormalWindowBoundsWidth, 0) > 10)
                    {
                        mainForm.WindowState = FormWindowState.Normal;
                        System.Drawing.Rectangle r = new System.Drawing.Rectangle(Database.GetSetting(SettingType.NormalWindowBoundsX, 0),
                                                                                  Database.GetSetting(SettingType.NormalWindowBoundsY, 0),
                                                                                  Database.GetSetting(SettingType.NormalWindowBoundsWidth, 0),
                                                                                  Database.GetSetting(SettingType.NormalWindowBoundsHeight, 0));
                        foreach (Screen s in Screen.AllScreens)
                        {
                            if (s.Bounds.Contains(r))
                            {
                                isOK = true;
                                break;
                            }
                        }
                        if (isOK)
                        {
                            mainForm.Bounds = r;
                        }
                    }
                    if (!isOK)
                    {
                        mainForm.Bounds = new System.Drawing.Rectangle(20,
                                                                       20,
                                                                       Screen.PrimaryScreen.WorkingArea.Width - 40,
                                                                       Screen.PrimaryScreen.WorkingArea.Height - 40);
                    }
                    mainForm.WindowState = FormWindowState.Normal;
                }
            }
            catch { }
        }
        
        public string TargetPlaylistName
        {
            get { return targetPlaylistName; }
            set
            {
                if (String.IsNullOrEmpty(value)) // value == null || value == String.Empty)
                    targetPlaylistName = String.Empty;
                else if (Database.GetPlaylistType(value) == PlaylistType.Standard)
                    targetPlaylistName = value;
            }
        }
        public string CurrentPlaylist
        {
            get
            {
                if (normal.IsFilterActive(FilterType.Playlist))
                    return normal.GetFilterValue(FilterType.Playlist);
                else
                    return String.Empty;
            }
        }
        public HTPCMode HTPCMode
        {
            get { return htpcMode; }
            set
            {
                htpcMode = value;
                normal.HTPCMode = value;
                controlPanel.HTPCMode = value;
                radio.HTPCMode = value;
            }
        }
        public ViewType CurrentView
        {
            get { return currentView; }
            private set { currentView = value; }
        }
        public void Mute(bool Muted)
        {
            player.Mute(Muted);
            controlPanel.Mute = Muted;
            updateVolume();
        }
        public bool EqualizerOn
        {
            get { return equalizer.On; }
        }

        public bool AppActive
        {
            get { return GetForegroundWindow() == mainForm.Handle; }
        }
        public string OutputDeviceName
        {
            get { return player.OutputDeviceName; }
            set { player.OutputDeviceName = value; }
        }
        public bool UseGlobalHotKeys
        {
            get { return useGlobalHotKeys; }
            set
            {
                if (useGlobalHotKeys != value)
                {
                    useGlobalHotKeys = value;
                    if (useGlobalHotKeys)
                    {
                        System.Diagnostics.Debug.Assert(keyHook == null);
                        keyHook = new KeyboardHook(this);
                    }
                    else if (keyHook != null)
                    {
                        keyHook.Dispose();
                        keyHook = null;
                    }
                }
            }
        }
        public void NotifyShift()
        {
            if (miniPlayer != null)
                miniPlayer.Invalidate();
        }
        public bool LocalVolumeControl
        {
            get { return localVolumeControl; }
            set
            {
                if (localVolumeControl != value)
                {
                    localVolumeControl = value;
                    if (localVolumeControl)
                    {
                        player.VolumeDB = LocalVolumeLevel;
                    }
                    else
                    {
                        WinAudioLib.VolumeDB = LocalVolumeLevel;
                        player.VolumeDB = 0.0f;
                    }
                    updateVolume();
                }
            }
        }
        public bool Playing
        {
            get { return player.Playing; }
        }
        public bool StopAfterThisTrack
        {
            get { return player.StopAfterThisTrack; }
        }
        public bool Paused
        {
            get { return player.Paused; }
        }
        public bool GamepadEnabled
        {
            get { return pad != null && pad.Enabled; }
        }
        public float LocalVolumeLevel
        {
            get { return localVolumeLevel; }
            set
            {
                value = Math.Min(0, Math.Max(-100, value));

                if (localVolumeLevel != value)
                {
                    localVolumeLevel = value;
                    
                    if (LocalVolumeControl)
                        player.VolumeDB = value;
                    else
                        player.VolumeDB = 0.0f;
                    
                    updateVolume();
                }
            }
        }
        public void ShowTrack(Track Track, bool Select)
        {
            setView(ViewType.Normal, false, false);
            normal.MakeVisible(Track, Select);
        }
        public float DecoderGainDB
        {
            get { return player.GainDB; }
            set
            {
                if (player.GainDB != value)
                {
                    player.GainDB = value;
                    updateGainStatus();
                }
            }
        }
        public string LocalVolumeString
        {
            get { return (LocalVolumeLevel < -99.8f) ? "-∞dB" : LocalVolumeLevel.ToString("0.0dB"); }
        }
        public ActionHandlerType Type
        { get { return ActionHandlerType.Default; } }
        public Track UsersMostInterestingTrack
        {
            get
            {
                if (player.PlayingRadio)
                {
                    return RadioTrack;
                }
                else
                {
                    bool hst = normal.HasSelectedTracks;

                    if (player.Playing && (Track.TimeSinceLastTrackSelectedChanged > 10000 || !hst))
                        return player.PlayingTrack;
                    else if (hst)
                        return normal.SelectedTracks[0];
                    else if (player.Playing)
                        return player.PlayingTrack;
                    else if (normal.HasTracks)
                        return normal[0];
                    else
                        return null;
                }
            }
        }
        public Track GetTracklistFirstSelectedOrFirst
        {
            get { return normal.FirstSelectedOrFirst; }
        }
        public ReplayGainMode ReplayGain
        {
            get { return player.ReplayGain; }
        }
        public bool ReplayGainAnalysisInProgress
        {
            get { return rg != null; }
        }
        public SpectrumMode SpectrumMode
        {
            get { return player.SpectrumMode; }
        }
        public Sleep Sleep
        {
            get { return sleep; }
        }
        public Track PlayingTrack
        {
            get { return player.PlayingTrack; }
        }
        public bool AllowTagEditing
        {
            get
            {
                switch (CurrentView)
                {
                    case ViewType.Radio:
                        return radio.AllowTagEditing;
                    case ViewType.Podcast:
                        return podcastManager.AllowTagEditing;
                    case ViewType.Normal:
                        if (!normal.HasSelectedTracks)
                            return false;

                        List<Track> tl = normal.SelectedTracks.Take(10).ToList();

                        return !tl.Exists(t => !t.ConfirmExists);
                    default:
                        return false;
                }
            }
        }
        public void SetEqualizer(EqualizerSetting ES)
        {
            if (ES.IsOff)
            {
                equalizer.On = false;
            }
            else
            {
                equalizer.CurrentEqualizer = ES;
                equalizer.On = true;
            }
        }
        public void Play(PodcastEpisode PE)
        {
            try
            {
                if (PE != null && PE.Track != null)
                {
                    string path = PE.Track.FilePath;

                    if (!Database.TrackExists(t => String.Compare(t.FilePath, path, StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        PE.AddToLibrary(PE.Track.FilePath);
                        play(PE.Track);
                    }
                    else
                    {
                        Track t = Database.GetMatchingTrack(tt => String.Compare(tt.FilePath, path, StringComparison.OrdinalIgnoreCase) == 0);
                        if (t != null)
                            play(t);
                    }
                }
            }
            catch { }
        }

        public static Controller GetInstance()
        {
            return instance;
        }

        public ArtSaveOption ArtSaveOption
        { get; set; }

        public void RemoveSplashScreen()
        {
            if (splashScreen != null)
            {
                splashScreen.Close();
                splashScreen = null;
            }
            if (!Database.HasTracks)
            {
                doGettingStarted();
            }
            //else if (!Activation.IsPro && runNumber > 10 && ((runNumber % 3) == 0))
            //{
            //    frmNag fn = new frmNag();
            //    fn.ShowDialog(mainForm);
            //    if (fn.DialogResult == DialogResult.OK)
            //        RequestAction(QActionType.BuyPro);
            //}
        }
        private void doGettingStarted()
        {
            List<frmTaskDialog.Option> options = new List<frmTaskDialog.Option>();
            options.Add(new frmTaskDialog.Option("Add a folder", "Choose a folder in Windows and QuuxPlayer will add all the tracks within that folder and any subfolders.", 0));
            options.Add(new frmTaskDialog.Option("Add specific files", "Find individual files on your computer and add them to QuuxPlayer.", 1));
            //options.Add(new frmTaskDialog.Option("Import your iTunes library", "Add any tracks in your iTunes library to QuuxPlayer", 2));
            options.Add(new frmTaskDialog.Option("Don't add files now", "Just let me explore.", 3));

            frmTaskDialog td = new frmTaskDialog("Getting Started", "To get started with QuuxPlayer, choose an option:", options);
            //QMessageBox.Show(mainForm,
            //                     Localization.Get(UI_Key.Dialog_To_Get_Started),
            //                     Localization.Get(UI_Key.Dialog_To_Get_Started_Title),
            //                     QMessageBoxIcon.Information);

            td.ShowDialog(mainForm);

            switch (td.ResultIndex)
            {
                case 0:
                    this.RequestAction(QActionType.AddFolderToLibrary);
                    break;
                case 1:
                    this.RequestAction(QActionType.AddFileToLibrary);
                    break;

                case 2:
                    iTunes.GetLibrary();
                    break;
                case 3:
                    break;
            }
        }
        public void Invalidate()
        {
            normal.InvalidateAll();
            radio.Invalidate();
        }
        public static void ShowPriorityMessage(string Message)
        {
            trackDisplay.ShowPriorityMessage(Message);
        }
        public static void ShowMessageUntilReplaced(string Message)
        {
            trackDisplay.ShowMessageUntilReplaced(Message);
        }
        public static void ShowMessage(string Message)
        {
            trackDisplay.TemporaryMessage = Message;
        }
        private Track radioTrack = null;
        public Track RadioTrack
        {
            get { return radioTrack; }
            set
            {
                if (radioTrack != value)
                {
                    radioTrack = value;

                    if (radioTrack != null)
                    {
                        trackDisplay.CurrentTrack = radioTrack;
                        progressBar.Playing = false;
                        if (this.CurrentView == ViewType.AlbumDetails)
                            albumDetails.CurrentTrack = radioTrack;
                    }
                }
            }
        }
        public void RequestAction(QActionType Type)
        {
            if (tempHandler == null)
                RequestActionNoRedirect(Type);
            else
                tempHandler.RequestAction(Type);
        }
        public void ShowLetter(char C)
        {
            normal.ShowLetter(C);
        }
        public void RequestActionNoRedirect(QActionType Type)
        {
            Track t = null;

            switch (Type)
            {
                case QActionType.Play:
                    if (player.Paused)
                        play(PlayType.Resume);
                    else if (normal.HasSelectedTracks)
                        play(PlayType.SelectedTracks);
                    else
                        play(PlayType.FirstTrack);
                    break;
                case QActionType.PlaySelectedTracks:
                    if (player.Paused && !normal.HasSelectedTracks)
                    {
                        player.Resume();
                    }
                    else
                    {
                        play(PlayType.SelectedTracks);
                    }
                    break;
                case QActionType.LoadFilterValues:
                    normal.LoadFilterValues();
                    break;
                case QActionType.RefreshAll:
                    normal.RefreshAll(false);
                    break;
                case QActionType.ReleaseAllFilters:
                    ShowMessage(Localization.Get(UI_Key.Message_Release_All_Filters));
                    normal.ReleaseAllFilters();
                    break;
                case QActionType.ShowFilterIndex:
                    normal.ShowFilterIndex();
                    break;
                case QActionType.FilterSelectedArtist:
                    normal.ShowAllByType(FilterType.Artist);
                    break;
                case QActionType.FilterSelectedAlbum:
                    normal.ShowAllByType(FilterType.Album);
                    break;
                case QActionType.FilterSelectedGenre:
                    normal.ShowAllByType(FilterType.Genre);
                    break;
                case QActionType.FilterSelectedYear:
                    normal.ShowAllByType(FilterType.Year);
                    break;
                case QActionType.FilterSelectedGrouping:
                    normal.ShowAllByType(FilterType.Grouping);
                    break;
                case QActionType.PreviousFilter:
                    ShowMessage(Localization.Get(UI_Key.Message_Show_Previous_Filter));
                    normal.AdvanceFilter(false);
                    break;
                case QActionType.NextFilter:
                    ShowMessage(Localization.Get(UI_Key.Message_Show_Next_Filter));
                    normal.AdvanceFilter(true);
                    break;
                case QActionType.ReleaseCurrentFilter:
                    ShowMessage(Localization.Get(UI_Key.Message_Release_Current_Filter));
                    normal.ReleaseCurrentFilter();
                    break;
                case QActionType.SelectNextItemGamePadRight:
                    {
                        int numToMove = pad.GetJoystickAccelleration(1);
                        if (normal.MoveDown(numToMove, false))
                        {
                            normal.TempDisplayTrackInfo(normal.FirstSelectedTrack);

                            if ((numToMove > 4) && (this.CurrentView == ViewType.Normal))
                                //ShowLetter(normal.CurrentLetterIndex);
                                normal.ShowIndexLetterTrackList();
                        }
                    }
                    break;
                case QActionType.SelectPreviousItemGamePadRight:
                    {
                        int numToMove = -pad.GetJoystickAccelleration(1);
                        if (normal.MoveUp(numToMove, false))
                        {
                            normal.TempDisplayTrackInfo(normal.FirstSelectedTrack);

                            if ((numToMove > 4) && (this.CurrentView == ViewType.Normal))
                                //ShowLetter(normal.CurrentLetterIndex);
                                normal.ShowIndexLetterTrackList();
                        }
                    }
                    break;
                case QActionType.SelectNextItemGamePadLeft:
                case QActionType.SelectPreviousItemGamePadLeft:
                    {
                        int numToMove = pad.GetJoystickAccelleration(0);
                        normal.ChangeFilterIndex(numToMove);
                        if ((Math.Abs(numToMove) > 0) && (this.CurrentView == ViewType.Normal))
                        {
                            normal.ShowIndexLetterFilterValue();
                        }
                    }
                    break;
                case QActionType.InitializeGamepad:
                    if (!initGamepad())
                        QMessageBox.Show(mainForm,
                                         Localization.Get(UI_Key.Dialog_No_Gamepad_Detected),
                                         Localization.Get(UI_Key.Dialog_No_Gamepad_Detected_Title),
                                         QMessageBoxIcon.Information);
                    break;
                case QActionType.Next:
                    play(PlayType.NextTrackManual);
                    break;
                case QActionType.Previous:
                    play(PlayType.PreviousTrack);
                    break;
                case QActionType.MoveTracksUp:
                    normal.MoveTracksUp();
                    
                    break;
                case QActionType.MoveTracksDown:
                    normal.MoveTracksDown();
                    
                    break;
                case QActionType.ConvertToStandardPlaylist:
                    convertToStandardPlaylist();
                    break;
                case QActionType.EndOfAllTracks:
                    cleanupAfterStop();
                    break;
                case QActionType.Paused:
                    controlPanel.Paused = true;
                    controlPanel.Stopped = false;
                    break;
                case QActionType.Resumed:
                    controlPanel.Paused = false;
                    controlPanel.Stopped = false;
                    break;
                case QActionType.Stopped:
                    cleanupAfterStop();
                    break;
                case QActionType.Delete:
                    normal.RemoveTracks();
                    break;
                case QActionType.ShowAboutScreen:
                    frmAbout about = new frmAbout(this);
                    about.Icon = mainForm.Icon;
                    about.ShowDialog(mainForm);
                    break;
                case QActionType.EditAutoPlaylist:
                    editAutoPlaylist();
                    break;
                case QActionType.RenameSelectedPlaylist:
                    renameSelectedPlaylist();
                    break;
                case QActionType.RenameSelectedRadioGenre:
                    radio.RenameSelectedGenre();
                    break;
                case QActionType.RemoveSelectedPlaylist:
                    removeSelectedPlaylist();
                    break;
                case QActionType.CreateNewPlaylist:
                    createNewPlaylist();
                    break;
                case QActionType.ClearTargetPlaylist:
                    Database.ClearPlaylist(TargetPlaylistName, null);
                    normal.RefreshAll(false);
                    break;
                case QActionType.AddToTargetPlaylist:
                    addSelectedTracksToTargetPlaylist(false);
                    break;
                case QActionType.AddToTargetPlaylistAndAdvance:
                    addSelectedTracksToTargetPlaylist(true);
                    break;
                case QActionType.AddToNowPlaying:
                    addSelectedTracksToNowPlaying(false);
                    break;
                case QActionType.AddToNowPlayingAndAdvance:
                    addSelectedTracksToNowPlaying(true);
                    break;
                case QActionType.PageDown:
                    switch (CurrentView)
                    {
                        case ViewType.Normal:
                            normal.PageDown();
                            break;
                        case ViewType.Radio:
                            radio.PageDown();
                            break;
                    }
                    break;
                case QActionType.PageUp:
                    if (CurrentView == ViewType.Normal)
                    {
                        normal.PageUp();
                        break;
                    }
                    break;
                case QActionType.Home:
                    normal.Home();
                    
                    break;
                case QActionType.End:
                    normal.End();
                    break;
                case QActionType.AdvanceScreenWithoutMouse:
                    ShowMessage(Localization.Get(UI_Key.Message_Advance_View));
                    advanceView(true);
                    break;
                case QActionType.AdvanceScreen:
                    ShowMessage(Localization.Get(UI_Key.Message_Advance_View));
                    advanceView(false);
                    break;
                case QActionType.Mute:
                    this.Mute(!Setting.Mute);
                    ShowMessage(Setting.Mute ? Localization.Get(UI_Key.Message_Mute) : Localization.Get(UI_Key.Message_Unmute));
                    break;
                case QActionType.SpectrumViewGainUp:
                    if (CurrentView == ViewType.Spectrum)
                    {
                        spectrumView.Gain *= 1.05f;
                        ShowMessage(Localization.Get(UI_Key.Message_Spectrum_Gain, ((int)(100.0f * spectrumView.Gain / SpectrumView.DEFAULT_GAIN)).ToString()));
                    }
                    break;
                case QActionType.SpectrumViewGainDown:
                    if (CurrentView == ViewType.Spectrum)
                    {
                        spectrumView.Gain *= 0.95f;
                        ShowMessage(Localization.Get(UI_Key.Message_Spectrum_Gain, ((int)(100.0f * spectrumView.Gain / SpectrumView.DEFAULT_GAIN)).ToString()));
                    }
                    break;
                case QActionType.MoveLeft:
                    if (this.CurrentView == ViewType.Normal)
                        normal.SetFilterValueListActive();
                    break;
                case QActionType.MoveRight:
                    if (this.CurrentView == ViewType.Normal)
                        normal.SetTrackListActive();
                    break;
                case QActionType.ViewNowPlaying:
                    if (ViewState.PreviousViewState == null && !controlPanel.NowPlaying)
                    {
                        saveViewState();
                        ShowMessage(Localization.Get(UI_Key.Message_Viewing_Now_Playing));
                        viewNowPlaying();
                    }
                    else
                    {
                        ShowMessage(Localization.Get(UI_Key.Message_Restoring_View));
                        restoreViewState();
                    }
                    break;
                case QActionType.MoveUp:
                    if (this.CurrentView == ViewType.Normal)
                    {
                        normal.MoveUp();
                        
                    }
                    break;
                case QActionType.MoveDown:
                    if (this.CurrentView == ViewType.Normal)
                    {
                        normal.MoveDown();
                    }
                    break;
                case QActionType.Cancel:
                    
                    FileAdder.Cancel();
                    iTunes.Cancel = true;

                    if (CurrentView == ViewType.Equalizer || CurrentView == ViewType.AlbumDetails)
                        setView(ViewType.Normal, false, false);
                    
                    break;

                case QActionType.HTPCMode:
                    HTPCMode = (HTPCMode == HTPCMode.Normal) ? HTPCMode.HTPC : HTPCMode.Normal;
                    ShowMessage((HTPCMode == HTPCMode.Normal) ? Localization.Get(UI_Key.Message_Normal_Mode) : Localization.Get(UI_Key.Message_HTPC_Mode));
                    break;
                case QActionType.ShowEqualizer:
                    if (CurrentView == ViewType.Equalizer)
                        setView(ViewType.Normal, false, false);
                    else
                        setView(ViewType.Equalizer, false, false);
                    break;
                case QActionType.ShowTrackAndAlbumDetails:
                    if (CurrentView == ViewType.AlbumDetails)
                        setView(ViewType.Normal, false, false);
                    else if (this.UsersMostInterestingTrack != null)
                        setView(ViewType.AlbumDetails, false, true);
                    break;
                case QActionType.AddFolderToLibrary:
                    FileAdder.AddFolder(RefreshAll);
                    break;
                case QActionType.AddFileToLibrary:
                    addFileToLibrary();
                    break;
                case QActionType.RemoveNonExistentTracks:
                    FileRemover.RemoveGhosts(RefreshAll);
                    break;
                case QActionType.RefreshLibrary:
                    if (normal.TrackCount < 50)
                    {
                        List<Track> tt = normal.Queue.ToList();
                        foreach (Track t2 in tt)
                        {
                            t2.ForceLoad();
                        }
                        normal.InvalidateAll();
                    }
                    FileRefresher.RefreshTracks(RefreshAll);
                    break;
                case QActionType.RefreshSelectedTracks:
                    Clock.DoOnNewThread(refreshSelectedTracks);
                    break;
                case QActionType.PlayThisAlbum:
                    ShowMessage(Localization.Get(UI_Key.Message_Play_This_Album));
                    if (normal.HasSelectedTracks)
                        playThisAlbum(normal.FirstSelectedTrack);
                    else if (normal.HasTracks)
                        playThisAlbum(normal[0]);
                    break;
                case QActionType.AddAlbumToNowPlaying:
                    if (normal.HasSelectedTracks)
                        addAlbumToNowPlaying(normal.FirstSelectedTrack);
                    else if (normal.HasTracks)
                        addAlbumToNowPlaying(normal[0]);
                    break;
                case QActionType.LastFMShowArtist:
                    if (normal.HasSelectedTracks)
                    {
                        Net.BrowseTo(LastFM.GetLastFMArtistURL(normal.FirstSelectedTrack));
                    }
                    break;
                case QActionType.LastFMShowAlbum:
                    if (normal.HasSelectedTracks)
                    {
                        Net.BrowseTo(LastFM.GetLastFMAlbumURL(normal.FirstSelectedTrack));
                    }
                    break;
                case QActionType.PlaySelectedTrackNext:
                    if (normal.HasSelectedTracks)
                    {
                        if (player.Playing)
                        {
                            t = normal.SelectedTracks[0];
                            playTrackNext(t);
                        }
                        else
                        {
                            play(PlayType.SelectedTracks);
                        }
                    }
                    break;
                case QActionType.PlayRandomAlbum:
                    ShowMessage(Localization.Get(UI_Key.Message_Play_Random_Album));
                    playRandomAlbum();
                    break;
                case QActionType.FocusSearchBox:
                    normal.FocusSearchBox();
                    break;
                case QActionType.ScanBack:
                    ShowMessage(Localization.Get(UI_Key.Message_Scan_Backward));
                    player.ElapsedTime -= 2000;
                    break;
                case QActionType.ScanFwd:
                    ShowMessage(Localization.Get(UI_Key.Message_Scan_Forward));
                    player.ElapsedTime += 2000;
                    break;
                case QActionType.VolumeDown:
                    volumeAdjust(false);
                    break;
                case QActionType.VolumeDownLarge:
                    volumeAdjustLarge(false);
                    break;
                case QActionType.VolumeDownForSleep:
                    volumeAdjust(false);
                    Clock.DoOnMainThread(showSleepMessage, 50); // delay to let initial volume update display
                    setMainFormTitle();
                    break;
                case QActionType.VolumeUp:
                    volumeAdjust(true);
                    break;
                case QActionType.VolumeUpLarge:
                    volumeAdjustLarge(true);
                    break;
                case QActionType.SetVolume:
                    if (LocalVolumeControl)
                        LocalVolumeLevel = controlPanel.Volume;
                    else
                        WinAudioLib.VolumeDB = controlPanel.Volume;
                    break;
                case QActionType.ToggleEqualizer:
                    equalizer.On = !equalizer.On;
                    ShowMessage(equalizer.On ? Localization.Get(UI_Key.Message_Equalizer_On) : Localization.Get(UI_Key.Message_Equalizer_Off));
                    break;
                case QActionType.SelectNextEqualizer:
                    if (!equalizer.On)
                        equalizer.On = true;
                    else
                        equalizer.SelectNextEqualizer();

                    ShowMessage(Localization.Get(UI_Key.Message_Selected_Equalizer_Setting, equalizer.CurrentEqualizer.Name));
                    break;
                case QActionType.ShowInWindowsExplorer:
                    t = normal.FirstSelectedOrFirst;
                    if (t != null)
                    {
                        Lib.SetWindowFullScreen(mainForm, false, (this.CurrentView == ViewType.Normal) ? FormBorderStyle.Sizable : FormBorderStyle.None, false);
                        Lib.Run("explorer.exe", "/Select,\"" + t.FilePath + "\"");
                    }
                    break;
                case QActionType.Pause:
                    player.Paused = !player.Paused;
                    ShowMessage(player.Paused ? Localization.Get(UI_Key.Message_Pause) : Localization.Get(UI_Key.Message_Resume));
                    break;
                case QActionType.PlayPause:
                    if (player.Playing)
                        RequestActionNoRedirect(QActionType.Pause);
                    else
                        RequestActionNoRedirect(QActionType.Play);
                    break;
                case QActionType.Stop:
                    ShowMessage(Localization.Get(UI_Key.Message_Stop));
                    clearNowPlaying();
                    player.Stop();
                    normal.RefreshAll(false);
                    player.StopAfterThisTrack = false;
                    break;
                case QActionType.StopAfterThisTrack:
                    player.StopAfterThisTrack = !player.StopAfterThisTrack;
                    if (player.StopAfterThisTrack)
                    {
                        ShowMessage(Localization.Get(UI_Key.Message_Stop_After_This_Track));
                    }
                    else
                    {
                        ShowMessage(Localization.Get(UI_Key.Message_Cancel_Stop_After_This_Track));
                    }
                    break;
                case QActionType.Shuffle:
                    ShowMessage(Localization.Get(UI_Key.Message_Shuffle));
                    normal.Shuffle();
                    UpdateNextUpOrRadioBitrate();
                    break;
                case QActionType.AdvanceSortColumn:
                    ShowMessage(Localization.Get(UI_Key.Message_Advance_Sort_Column));
                    normal.AdvanceSortColumn();
                    UpdateNextUpOrRadioBitrate();
                    break;
                case QActionType.RepeatToggle:
                    controlPanel.Repeat = !controlPanel.Repeat;
                    ShowMessage(controlPanel.Repeat ? Localization.Get(UI_Key.Message_Repeat) : Localization.Get(UI_Key.Message_No_Repeat));
                    UpdateNextUpOrRadioBitrate();
                    break;
                case QActionType.FindPlayingTrack:
                    if (player.Playing)
                    {
                        normal.MakeVisible(player.PlayingTrack, true);
                        ShowMessage(Localization.Get(UI_Key.Message_Show_Playing_Track));
                    }
                    break;
                case QActionType.SelectAll:
                    normal.SelectAll();
                    break;
                case QActionType.SelectNone:
                    normal.SelectNone();
                    break;
                case QActionType.InvertSelection:
                    normal.InvertSelection();
                    break;
                case QActionType.ExportCSV:
                    ImportExport.ExportCSV(normal.Queue, mainForm);
                    break;
                case QActionType.ExportCurrentView:
                    ImportExport.ExportPlaylist(Localization.Get(UI_Key.General_Playlist), normal.Queue, mainForm);
                    break;
                case QActionType.ExportPlaylist:
                    ImportExport.ExportPlaylist(CurrentPlaylist, Database.GetPlaylistTracks(CurrentPlaylist), mainForm);
                    break;
                case QActionType.ImportPlaylist:
                    List<Track> trax;
                    string playlistName = ImportExport.ImportPlaylist(mainForm, out trax);
                    if (playlistName.Length > 0)
                    {
                        addPlaylistToLibrary(trax, playlistName);
                    }
                    break;
                case QActionType.TogglePlayPause:
                    if (player.Playing)
                    {
                        RequestAction(QActionType.Pause);
                    }
                    else
                    {
                        ShowMessage(Localization.Get(UI_Key.Message_Resume));
                        play(PlayType.Resume);
                    }
                    break;
                case QActionType.SavePlaylist:
                    Database.SaveStandardPlaylist(normal.Queue);
                    break;
                case QActionType.PlayFirstTrack:
                    play(PlayType.FirstTrack);
                    break;
                case QActionType.RequestNextTrack:
                    play(PlayType.RequestNext);
                    break;
                case QActionType.EndOfTrack:
                    trackDisplay.CurrentTrack = null;
                    break;
                case QActionType.StartOfTrack:
                case QActionType.StartOfTrackAuto:

                    updateAfterTrackStarts();
                    UpdateNextUpOrRadioBitrate();
                    updateGainStatus();
                    
                    if (Type == QActionType.StartOfTrackAuto)
                        frmGlobalInfoBox.Show(this, frmGlobalInfoBox.ActionType.StartOfNextTrack);

                    Preload();
                    break;
                case QActionType.KeyPreviewChange:
                    setKeyPreview();
                    break;
                case QActionType.CheckForUpdate:
                    ShowMessage("Checking for update...");
                    checkForUpdate(true);
                    break;
                case QActionType.VisitWebSite:
                    Net.BrowseTo(Lib.PRODUCT_URL);
                    break;
                case QActionType.ShowFileDetails:
                    if (normal.HasSelectedTracks)
                        t = normal.SelectedTracks[0];
                    else if (player.Playing)
                        t = player.PlayingTrack;
                    else
                        t = null;

                    if (t != null)
                    {
                        frmFileInfo fi = new frmFileInfo(t);
                        fi.Icon = mainForm.Icon;
                        fi.ShowDialog(mainForm);
                        if (fi.EditFile)
                            RequestAction(new QAction(QActionType.EditTags, t));
                    }
                    break;
                case QActionType.ToggleGamepadHelp:
                    bool ae = pad.AnalogEnabled;
                    pad.AnalogEnabled = true;
                    frmGamepadHelp frmGPH = new frmGamepadHelp();
                    frmGPH.GamepadEnabled = initGamepad();
                    frmGPH.MainForm = mainForm;
                    pad.TempHandler = frmGPH;
                    frmGPH.ShowDialog(mainForm);
                    pad.TempHandler = null;
                    pad.AnalogEnabled = ae;
                
                    break;
                case QActionType.SetFullScreen:
                    if (!Lib.FullScreen)
                    {
                        Lib.SetWindowFullScreen(mainForm, true, FormBorderStyle.None, false);
                    }
                    setView(CurrentView, true, false);
                    break;
                case QActionType.ReleaseFullScreen:
                case QActionType.ReleaseFullScreenAuto:
                    if (Lib.FullScreen)
                    {
                        Lib.SetWindowFullScreen(mainForm, false, (this.CurrentView == ViewType.Normal) ? FormBorderStyle.Sizable : FormBorderStyle.None, Type != QActionType.ReleaseFullScreenAuto);
                        setView(CurrentView, true, false);
                    }
                    break;
                case QActionType.ShowCrawlDialog:
                    frmMonitor frmMon = new frmMonitor();
                    frmMon.Icon = mainForm.Icon;
                    frmMon.Directories = Database.CrawlDirs;
                    frmMon.ShowDialog(mainForm);
                    if (frmMon.DialogResult == DialogResult.OK)
                    {
                        Database.CrawlDirs = frmMon.Directories;
                    }
                    Clock.DoOnMainThread(crawl, 1000);
                    break;
                case QActionType.ResetPlayHistory:
                    if (QMessageBox.Show(mainForm, Localization.Get(UI_Key.General_Reset_Play_History), Localization.Get(UI_Key.General_Reset_Play_History_Title), QMessageBoxButtons.OKCancel, QMessageBoxIcon.Question, QMessageBoxButton.NoCancel) == DialogResult.OK)
                    {
                        List<Track> q = normal.SelectedTracks.ToList();

                        foreach (Track tt in q)
                            tt.ResetPlayHistory();

                        normal.RefreshTrackList(false);
                    }
                    break;
                case QActionType.ReplayGainOff:
                    player.ReplayGain = ReplayGainMode.Off;
                    Setting.ReplayGain = ReplayGainMode.Off;
                    updateGainStatus();
                    break;
                case QActionType.ReplayGainTrack:
                    player.ReplayGain = ReplayGainMode.Track;
                    Setting.ReplayGain = ReplayGainMode.Track;
                    updateGainStatus();
                    break;
                case QActionType.ReplayGainAlbum:
                    player.ReplayGain = ReplayGainMode.Album;
                    Setting.ReplayGain = ReplayGainMode.Album;
                    updateGainStatus();
                    break;
                case QActionType.ReplayGainAnalyzeSelectedTracks:
                    if (rg == null && normal.HasSelectedTracks)
                    {
                        doReplayGain();
                    }
                    break;
                case QActionType.ReplayGainAnalyzeCancel:
                    if (rg != null)
                        rg.Cancel();
                    break;
                case QActionType.UpdateTrackCount:
                    Clock.Update(ref trackCountTimer, updateTrackOrStationCount, 250, false);
                    break;
                case QActionType.AskAboutMissingFiles:
                    RequestAction(new QAction(QActionType.AskAboutMissingFiles));
                    break;
                case QActionType.ToggleSpectrumResolution:
                    player.SpectrumMode = (player.SpectrumMode == SpectrumMode.Normal) ? SpectrumMode.Small : SpectrumMode.Normal;
                    break;
                case QActionType.SetSleepOptions:
                    setSleepOptions();
                    break;
                case QActionType.Exit:
                    forceUnlockControls();
                    mainForm.Close();
                    if (!mainForm.IsClosed && !mainForm.Visible)
                    {
                        RequestActionNoRedirect(QActionType.HideMiniPlayer);
                    }
                    break;
                case QActionType.SetLockOptions:
                    frmLock fl = new frmLock(qLock);
                    fl.Icon = mainForm.Icon;
                    fl.ShowDialog(mainForm);
                    if (fl.DialogResult == DialogResult.OK)
                    {
                        qLock = fl.Lock;
                        setControlLock();
                    }
                    break;
                case QActionType.UnlockControls:
                    if (qLock != null && qLock.Locked)
                    {
                        if (qLock.Code.Length > 0)
                        {
                            QInputBox ib = new QInputBox(mainForm,
                                                         Localization.Get(UI_Key.Dialog_Lock_Enter_Unlock_Code),
                                                         Localization.Get(UI_Key.Dialog_Lock_Enter_Unlock_Code_Title),
                                                         String.Empty,
                                                         QLock.MAX_CODE_LENGTH,
                                                         1);
                            if (ib.DialogResult == DialogResult.OK)
                            {
                                if (ib.Value.Trim().ToLowerInvariant() != qLock.Code.ToLowerInvariant())
                                {
                                    QMessageBox.Show(mainForm,
                                                     Localization.Get(UI_Key.Dialog_Lock_Incorrect_Code),
                                                     Localization.Get(UI_Key.Dialog_Lock_Incorrect_Code_Title),
                                                     QMessageBoxIcon.Error);
                                }
                                else
                                {
                                    qLock.Unlock();
                                    setControlLock();
                                }
                            }
                        }
                        else
                        {
                            qLock.Unlock();
                            setControlLock();
                        }
                    }
                    break;
                case QActionType.ForceUnlockControls:
                    forceUnlockControls();
                    break;
                case QActionType.SetMainFormTitle:
                    setMainFormTitle();
                    break;
                case QActionType.ToggleDownloadCoverArt:
                    Track.DownloadCoverArt = !Track.DownloadCoverArt;
                    if (Track.DownloadCoverArt)
                    {
                        normal.CurrentTrack = player.PlayingTrack ?? UsersMostInterestingTrack;
                    }
                    break;
                case QActionType.TwitterOptions:
                    frmTwitter ft = new frmTwitter();
                    ft.ShowDialog(mainForm);
                    break;
                case QActionType.LastFMOptions:
                    frmLastFM flfm = new frmLastFM(lastFMOn, lastFMUserName, lastFMPassword);
                    flfm.ShowDialog(mainForm);
                    if (flfm.DialogResult == DialogResult.OK)
                    {
                        lastFMOn = flfm.On;
                        lastFMUserName = flfm.UserName;
                        lastFMPassword = flfm.Password;
                    }
                    break;
                case QActionType.MakeForeground:
                    mainForm.BringToFront();
                    break;
                case QActionType.SetFileAssociations:
                    setFileAssociations();
                    break;
                case QActionType.BuyPro:
                    Net.BrowseTo(Lib.PRODUCT_URL + "/order.php");
                    break;
                case QActionType.ShowTagCloud:
                    setView(ViewType.TagCloud, true, false);
                    break;
                case QActionType.ToggleRadioMode:
                    toggleRadioMode();
                    break;
                case QActionType.ShowMiniPlayer:
                    showMiniPlayer(true);
                    break;
                case QActionType.HideMiniPlayer:
                    showMiniPlayer(false);
                    break;
                case QActionType.EditTags:
                    if (this.CurrentView == ViewType.Normal)
                        editTags(null);
                    break;
                case QActionType.OrganizeLibrary:
                    if (this.CurrentView == ViewType.Normal)
                        organizeLibrary();
                    break;
                case QActionType.SuggestDuplicatesBitrate:
                case QActionType.SuggestDuplicatesOldest:
                case QActionType.SuggestDuplicatesNewest:
                case QActionType.SuggestDuplicatesHighestRated:
                    suggestDuplicates(Type);
                    break;
                case QActionType.UpdateRadioStation:
                    if (this.CurrentView != ViewType.Radio)
                        setView(ViewType.Radio, false, false);
                    if (player.Play(radio.Station))
                    {
                        trackDisplay.CurrentTrack = null;
                        updateGainStatus();
                    }
                    break;
                case QActionType.DisplayRadioInfo:
                    UpdateNextUpOrRadioBitrate();
                    updateTrackOrStationCount();
                    break;
                case QActionType.RadioFailed:
                    trackDisplay.ShowPriorityMessage(Localization.Get(UI_Key.Radio_Connection_Failed));
                    player.Stop();
                    break;
                case QActionType.RadioStreamStarted:
                    progressBar.TotalTime = 0;
                    break;
                case QActionType.ReloadRadioStations:
                    if (this.CurrentView == ViewType.Radio)
                        radio.ReloadStations();
                    break;
                case QActionType.TogglePodcastView:
                    if (CurrentView == ViewType.Podcast)
                        setView(ViewType.Normal, true, false);
                    else
                        setView(ViewType.Podcast, true, false);
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false, "Action Type Not Found");
                    break;
            }
        }

        private void doReplayGain()
        {
            List<frmTaskDialog.Option> options = new List<frmTaskDialog.Option>();

            options.Add(new frmTaskDialog.Option("Analyze all selected tracks", "Analyze each selected track, even if it has been analyzed before.", 0));
            options.Add(new frmTaskDialog.Option("Analyze only unanalyzed tracks", "Skip those tracks that already have volume leveling information.", 1));
            options.Add(new frmTaskDialog.Option("Learn More", "Read about replay gain on the web.", 2));
            options.Add(new frmTaskDialog.Option("Cancel", "Don't analyze tracks now.", 3));

            frmTaskDialog td = new frmTaskDialog("Volume Leveling / Replay Gain", "Analyze Tracks for Volume Leveling Information", options);

            td.ShowDialog(mainForm);

            if (td.ResultIndex < 2)
            {
                List<Track> tracks = normal.SelectedTracks;

                if (td.ResultIndex == 1)
                    tracks = tracks.FindAll(t => !t.HasReplayGainInfoTrack || !t.HasReplayGainInfoAlbum);
                
                rg = new ReplayGain(tracks, doneReplayGain);
                Clock.DoOnNewThread(rg.DoReplayGainAnalysis);
            }
            else if (td.ResultIndex == 2)
            {
                Net.BrowseTo(Lib.PRODUCT_URL + "/features_replay_gain.php");
            }
        }
        private void doneReplayGain()
        {
            ShowMessage("Volume leveling analysis complete.");
            RefreshAll();
            rg = null;
        }
        private void toggleRadioMode()
        {
            if (this.CurrentView == ViewType.Radio)
                setView(ViewType.Normal, false, false);
            else
                setView(ViewType.Radio, false, false);
        }

        private void addPlaylistToLibrary(List<Track> Tracks, string playlistName)
        {
            Database.AddPlaylist(playlistName);
            normal.LoadFilterValues();

            addToLibrary(Tracks);

            Database.AddToPlaylist(playlistName, Database.Validate(Tracks));
            normal.SetFilterValue(FilterType.Playlist, playlistName, true);
            normal.RefreshAll(true);
        }

        private void refreshSelectedTracks()
        {
            try
            {
                List<Track> tt = normal.SelectedTracks;
                foreach (Track t in tt)
                {
                    t.ForceLoad();
                    ShowMessage("Refreshed '" + t.ToShortString() + "'");
                }
                ShowMessage("Done refreshing tracks.");
                Clock.DoOnMainThread(normal.InvalidateAll);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }
        private int existOrder(Track Track)
        {
            if (Track.ConfirmExists)
                return 0;
            else
                return 1;
        }
        private void suggestDuplicates(QActionType Type)
        {
            normal.SetFilterValue(FilterType.Playlist, Localization.DUPLICATES, true);

            List<Track> tracks = Database.GetPlaylistTracks(Localization.DUPLICATES);

            List<Track> tt;

            switch (Type)
            {
                case QActionType.SuggestDuplicatesBitrate:
                    tt = (from t in tracks
                          orderby t.SearchString, existOrder(t), t.Bitrate descending, t.Rating descending, t.Duration descending
                          select t).ToList();
                    break;
                case QActionType.SuggestDuplicatesOldest:
                    tt = (from t in tracks
                          orderby t.SearchString, existOrder(t), t.FileDate ascending, t.Rating descending, t.Duration descending
                          select t).ToList();
                    break;
                case QActionType.SuggestDuplicatesNewest:
                    tt = (from t in tracks
                          orderby t.SearchString, existOrder(t), t.FileDate descending, t.Rating descending, t.Duration descending
                          select t).ToList();
                    break;
                default:
                    tt = (from t in tracks
                          orderby t.SearchString, existOrder(t), t.Rating descending, t.Bitrate descending, t.Duration descending
                          select t).ToList();
                    break;
            }

            bool first;
            for (int i = 0; i < tt.Count; i++)
            {
                first = (i == 0) ||
                        (String.Compare(tt[i].SearchString, tt[i - 1].SearchString, StringComparison.OrdinalIgnoreCase) != 0) ||
                        (!tt[i].DurationIsSimilar(tt[i - 1])) ||
                        (tt[i].TrackNum != tt[i - 1].TrackNum);
                tt[i].Selected = !first;
            }
            normal.LoadFilterValues();
            UpdateNextUpOrRadioBitrate();
            normal.NoSort();
            normal.Queue = tt;
            normal.InvalidateAll();
        }
        private void organizeLibrary()
        {
            normal.ShowPanel(new Organizer(doneOrganizing));
        }
        private void doneOrganizing()
        {
            DoneShowPanel();
        }
        private void editTags(Track Track)
        {
            List<Track> tracks;
            if (Track == null)
                tracks = normal.SelectedTracks.ToList();
            else
                tracks = new List<Track>() { Track };

            if (tracks.Count > 0)
            {
                normal.ShowTagEditor(tracks, doneEditingTags);
            }
        }
        
        public void LockForPanel()
        {
            if (qLock == null)
            {
                qLock = new QLock(true, String.Empty, true);
            }
            else
            {
                qLock.Locked = true;
                qLock.GamepadLock = true;
            }

            setControlLock();
        }
        
        private void doneEditingTags(TagEditor.TagEditAction TEA)
        {
            if (TEA != TagEditor.TagEditAction.Cancel)
            {
                normal.RefreshAll(true);
                artwork.Refresh();
                normal.Artwork.Refresh();
            }

            if (TEA == TagEditor.TagEditAction.OK)
                if (!normal.HasTracks)
                    normal.ReleaseAllFilters();

            if (TEA != TagEditor.TagEditAction.Apply)
            {
                DoneShowPanel();
            }
        }
        
        public void DoneShowPanel()
        {
            normal.Panel = null;
            UnlockForPanel();
        }
        public void UnlockForPanel()
        {
            qLock.Unlock();
            setControlLock();
        }

        private void showMiniPlayer(bool Show)
        {
            if (Show && miniPlayer == null)
            {
                miniPlayer = new frmMiniPlayer(controlPanel, player.PlayingRadio);
                miniPlayer.Icon = mainForm.Icon;
                miniPlayer.Show();
                mainForm.Visible = false;
            }
            else if (!Show)
            {
                if (miniPlayer != null)
                {
                    miniPlayer.Close();
                    miniPlayer = null;
                }
                if (mainForm.WindowState == FormWindowState.Minimized || !mainForm.Visible)
                {
                    Lib.MakeMainFormForeground();
                }
            }
        }
        private void setSleepOptions()
        {
            frmSleep fs = new frmSleep(this);

            if (sleep == null)
            {
                sleep = new Sleep(this);
                sleep.Alarm = DateTime.Now + TimeSpan.FromMinutes(Database.GetSetting(SettingType.SleepDelay, 60.0f));
                sleep.Fade = DateTime.Now + TimeSpan.FromMinutes(Database.GetSetting(SettingType.SleepFadeDelay, 0.0f));
                sleep.Action = (Sleep.ActionType)Database.GetSetting(SettingType.SleepAction, (int)Sleep.ActionType.ShutDown);
                sleep.Force = Database.GetSetting(SettingType.SleepForce, false);
            }

            fs.Sleep = sleep;
            fs.Icon = mainForm.Icon;
            fs.ShowDialog(mainForm);

            if (fs.DialogResult == DialogResult.OK)
            {
                if (sleep != null)
                    sleep.Stop();

                sleep = fs.Sleep;
                sleep.Go(WinAudioLib.VolumeDB);

                if (sleep.Active)
                {
                    trackDisplay.TemporaryMessage = sleep.ToString();
                    
                    Database.SaveSetting(SettingType.SleepAction, (int)sleep.Action);
                    Database.SaveSetting(SettingType.SleepForce, sleep.Force);
                    Database.SaveSetting(SettingType.SleepDelay, (float)((sleep.Alarm - DateTime.Now).TotalMinutes));
                    Database.SaveSetting(SettingType.SleepFadeDelay, (float)((sleep.Fade - DateTime.Now).TotalMinutes));
                }
                setMainFormTitle();
            }
        }
        private void cleanupAfterStop()
        {
            artwork.CurrentTrack = null;
            controlPanel.Stopped = true;
            normal.PlayingTrack = null;
            progressBar.TotalTime = 0;
            progressBar.Playing = false;
            UpdateNextUpOrRadioBitrate();
            updateGainStatus();

            if (Setting.StopClearsNowPlaying && !exiting)
                clearNowPlaying();
            
            Clock.RemoveAlarm(lastFMTimer);
            normal.RefreshAll(false);
            PodcastManager.InvalidateAll();
        }

        private void playTrackNext(Track t)
        {
            ShowMessage(Localization.Get(UI_Key.Message_Track_Next_Up, t.ToShortString()));
            MakeTrackNextToPlay(t);
            UpdateNextUpOrRadioBitrate();
        }

        public void RequestAction(QAction Action)
        {
            if (tempHandler == null)
                RequestActionNoRedirect(Action);
            else
                tempHandler.RequestAction(Action);
        }
        public void RequestActionNoRedirect(QAction Action)
        {
            Track t = Action.Track;
            switch (Action.Type)
            {
                case QActionType.ShowTrackDetails:
                    normal.TempDisplayTrackInfo(t);
                    break;
                case QActionType.TrackFailed:
                    ShowMessage(Localization.Get(UI_Key.Message_Track_Failed, t.FilePath));
                    if (Database.GetFirstTrackFromPlaylist(Localization.NOW_PLAYING) == t)
                        Database.RemoveFirstTrackFromPlaylist(Localization.NOW_PLAYING);
                    player.Stop();
                    break;
                case QActionType.PlayThisAlbum:
                    playThisAlbum(t);
                    break;
                case QActionType.AddAlbumToNowPlaying:
                    addAlbumToNowPlaying(t);
                    break;
                case QActionType.AskAboutMissingFiles:
                    if (t == null && normal.HasSelectedTracks && !normal.FirstSelectedTrack.ConfirmExists)
                        t = normal.FirstSelectedTrack;

                    if (t != null)
                    {
                        frmFindFile ff = new frmFindFile();
                        ff.Track = normal.FirstSelectedTrack;
                        ff.ShowDialog(mainForm);
                    }
                    break;
                case QActionType.ShowAllOfArtist:
                    normal.Filter(FilterType.Artist, Action.Value);
                    setView(ViewType.Normal, true, false);
                    break;
                case QActionType.ShowAllOfAlbum:
                    if (Action.AltValue.Length > 0)
                        normal.FilterArtistAndAlbum(Action.AltValue, Action.Value);
                    else
                        normal.Filter(FilterType.Album, Action.Value);
                    setView(ViewType.Normal, true, false);
                    break;
                case QActionType.ShowAllOfGenre:
                    normal.Filter(FilterType.Genre, Action.Value);
                    setView(ViewType.Normal, true, false);
                    break;
                case QActionType.ShowAllOfGrouping:
                    normal.Filter(FilterType.Grouping, Action.Value);
                    setView(ViewType.Normal, true, false);
                    break;
                case QActionType.EditTags:
                    editTags(Action.Track);
                    break;
                default:
                    RequestAction(Action.Type);
                    break;
            }
        }
        public void PopulateEqualizerSelections(ToolStripMenuItem mi)
        {
            string currentEqName = String.Empty;

            List<Track> st = normal.SelectedTracks;

            EqualizerSetting es = null;

            if (st.Count > 0 && st.Count < 50)
            {
                es = st[0].Equalizer;
                currentEqName = EqualizerSetting.GetString(es);
                foreach (Track t in st)
                    if (!object.Equals(t.Equalizer, es))
                    {
                        currentEqName = String.Empty;
                        break;
                    }
            }

            List<string> eqs = (from eq in Equalizer.GetEqualizerSettings()
                                orderby eq.Name
                                select eq.Name).ToList();

            eqs.Insert(0, EqualizerSetting.DontChange);
            eqs.Insert(1, "{}{}");
            eqs.Insert(2, EqualizerSetting.TurnOff);
            eqs.Insert(3, "{}{}");

            mi.DropDownItems.Clear();

            foreach (string s in eqs)
            {
                if (s == "{}{}")
                {
                    mi.DropDownItems.Add(new ToolStripSeparator());
                }
                else
                {
                    ToolStripMenuItem mi2 = new ToolStripMenuItem(s);
                    mi2.Tag = s;
                    if (s == currentEqName)
                        mi2.Checked = true;
                    mi2.Click += (o, ee) =>
                    {
                        string ss = mi2.Tag.ToString();
                        foreach (Track t in normal.SelectedTracks)
                            t.SetEqualizer(ss);
                        normal.RefreshAll(false);
                    };
                    mi.DropDownItems.Add(mi2);
                }
            }
        }
        public void PopulateRatingSelections(ToolStripMenuItem mi)
        {
            string[] ratings = new string[] { Localization.Get(UI_Key.Rating_Zero), "*", "**", "***", "****", "*****" };

            if (mi.DropDownItems.Count < 6)
            {
                mi.DropDownItems.Clear();

                for (int i = 0; i <= 5; i++)
                {
                    ToolStripMenuItem mi2 = new ToolStripMenuItem(ratings[i]);
                    mi2.Tag = i;
                    mi2.Click += (s, ee) =>
                    {
                        int r = (int)mi2.Tag;
                        SetRatingOfSelectedTracks(r);
                    };
                    mi2.ShortcutKeyDisplayString = "Shift+" + i.ToString();
                    mi.DropDownItems.Add(mi2);
                }
            }
        }
        public void SetRatingOfSelectedTracks(int Rating)
        {
            foreach (Track t in normal.SelectedTracks)
                t.Rating = Rating;
            normal.RefreshAll(false);
        }
        public void RemoveFromLibrary(List<Track> Tracks)
        {
            Database.RemoveFromLibrary(Tracks);
            normal.RefreshAll(false);
        }
        public void AddToLibrarySilent(List<Track> Tracks, bool AllowDuplicates)
        {
            Database.AddToLibrary(Tracks, Setting.ShortTrackCutoff * 1000, AllowDuplicates);
            normal.InvalidateAll();
        }
        public void AddToLibrary(string[] FileNames)
        {
            FileAdder.AddItemsToLibrary(FileNames, String.Empty, true, RefreshAll);
        }
        public void AddToLibraryOrPlaylist(string[] FileNames, string PlaylistTarget)
        {
            if (PlaylistTarget.Length == 0)
                PlaylistTarget = normal.ActivePlaylist;

            FileAdder.AddItemsToLibrary(FileNames, PlaylistTarget, true, RefreshAll);
        }
        public void RefreshIfGhosts()
        {
            if (Database.GetPlaylistType(CurrentPlaylist) == PlaylistType.Ghosts)
            {
                Database.IncrementDatabaseVersion(false);
                normal.RefreshAll(false);
            }
            else
            {
                mainForm.Invalidate();
            }
        }
        public void RefreshAll()
        {
            normal.RefreshAll(false);
        }
        public void AddAndPlay(string FilePath, CommandLineActionType ActionType)
        {
            try
            {
                Track t = Database.GetTrackWithFilePath(FilePath);

                if (t == null)
                {
                    t = Track.Load(FilePath);
                    
                    if (t == null)
                        return;

                    Database.AddToLibrary(new List<Track>() { t }, 0, true);
                }

                if (ActionType == CommandLineActionType.Add) // All done
                    return;

                if (t != null)
                {
                    if (!player.Playing && (ActionType == CommandLineActionType.Enqueue || ActionType == CommandLineActionType.PlayNext))
                        ActionType = CommandLineActionType.Play;

                    switch (ActionType)
                    {
                        case CommandLineActionType.Play:
                            play(t);
                            break;
                        case CommandLineActionType.PlayNext:
                            playTrackNext(t);
                            break;
                        case CommandLineActionType.Enqueue:
                            AddToPlaylist(Localization.NOW_PLAYING, t);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }
        public void UpdateNextUpOrRadioBitrate()
        {
            if (player.PlayingRadio)
            {
                int bitrate = player.BitRate;
                if (bitrate > 0 && Radio.PlayingStation != null)
                    controlPanel.NextUpStatus = Localization.Get(UI_Key.Control_Panel_Station_Info, player.BitRate.ToString(), Radio.PlayingStation.Name);
                else
                    controlPanel.NextUpStatus = String.Empty;
            }
            else if (player.Playing)
            {
                Track t = this.PeekNextTrack(false);

                if (t == null)
                {
                    controlPanel.NextUpStatus = Localization.Get(UI_Key.Control_Panel_Next_Up_None);
                    player.NextTrack = null;
                }
                else
                {
                    controlPanel.NextUpStatus = Localization.Get(UI_Key.Control_Panel_Next_Up, t.ToShortString());
                }
            }
            else
            {
                controlPanel.NextUpStatus = String.Empty;
            }
        }

        private string gainStatus
        {
            get
            {
                if (ReplayGain == ReplayGainMode.Off || player.PlayingRadio)
                    return Localization.Get(UI_Key.Control_Panel_Decoder_Gain,
                                            DecoderGainDB.ToString("0.0"));
                else
                    return Localization.Get(UI_Key.Control_Panel_Decoder_Gain_And_Replay_Gain,
                                            DecoderGainDB.ToString("0.0"),
                                            player.ReplayGainDB.ToString("0.0"));
            }
        }
        private Track PeekNextTrack(bool Advance)
        {
            if (!Playing)
                return null;

            if (normal.NowPlayingVisible)
            {
                Database.SaveStandardPlaylist(normal.Queue);
            }

            Track t = null;

            TrackQueue q = Database.GetPlaylistTracks(Localization.NOW_PLAYING);

            t = q.FirstOrDefault(tt => tt != player.PlayingTrack);

            if (t != null)
            {
                return t;
            }
            else
            {
                if (Advance)
                    return normal.Advance(controlPanel.Repeat);
                else
                    return normal.Peek(controlPanel.Repeat);
            }
        }
        private void setFileAssociations()
        {
            //if (Environment.OSVersion.Version.Major == 5) // XP
            {
                frmFileAssociations fa = new frmFileAssociations();
                fa.ShowDialog(mainForm);
            }
            //else
            //{
            //    AssociationManager.Register();
                
            //    AssociationManager.NotifyShellOfChange();
            //    AssociationManager.ShowAssociationUI();
            //}
        }
        private bool initGamepad()
        {
            if (pad == null)
            {
                pad = new Gamepad(mainForm);
            }
            if (!pad.Enabled && !pad.Start())
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        private void volumeAdjust(bool Up)
        {
            doVolumeAdjust(Up ? 1 : -1);
        }
        private void volumeAdjustLarge(bool Up)
        {
            doVolumeAdjust(Up ? 8 : -8);
        }
        private void doVolumeAdjust(int Amount)
        {
            if (LocalVolumeControl)
                LocalVolumeLevel = (LocalVolumeLevel * (VOLUME_ADJUST_FRACTIONAL_CHANGE + Amount)) / VOLUME_ADJUST_FRACTIONAL_CHANGE + Amount;
            else
                WinAudioLib.Volume = (WinAudioLib.Volume * (VOLUME_ADJUST_FRACTIONAL_CHANGE + Amount)) / VOLUME_ADJUST_FRACTIONAL_CHANGE + Amount;
        }
        private void forceUnlockControls()
        {
            if (qLock != null && qLock.Locked)
            {
                qLock.Unlock();
                setControlLock();
            }
        }
        
        private void showSleepMessage()
        {
            if (sleep != null)
                ShowMessage(Localization.Get(UI_Key.Message_Volume, WinAudioLib.VolumeDBString) + "  " + sleep.ToString());
        }
        private void setControlLock()
        {
            controlPanel.Locked = qLock.Locked;
            mainForm.Locked = qLock.Locked;
            normal.Locked = qLock.Locked;
            radio.Locked = qLock.Locked;
            podcastManager.Locked = qLock.Locked;
            progressBar.Locked = qLock.Locked;
            pad.Locked = qLock.Locked && qLock.GamepadLock;
        }
        private void updateGainStatus()
        {
            controlPanel.GainStatus = this.gainStatus;
        }
        
        private void play(PlayType PlayType)
        {
            Track t;
            switch (PlayType)
            {
                case PlayType.SelectedTracks:
                    if (normal.NowPlayingVisible)
                    {
                        TrackQueue tq = normal.SelectedTracks;

                        if (tq.Count == 0)
                            return;

                        if (tq.Contains(normal[0]) && player.Playing && player.PlayingTrack == normal[0])
                            return;

                        Database.RemoveFromPlaylist(Localization.NOW_PLAYING, tq);

                        for (int i = tq.Count - 1; i >= 0; i--)
                            Database.InsertInNowPlaying(tq[i], 0);

                        normal.RefreshTrackList(false);
                        player.Play(normal[0]);
                        Clock.DoOnMainThread(normal.SelectTracklistIndexZero, 20);
                    }
                    else
                    {
                        removeCurrentlyPlayingFromNowPlaying();
                        TrackQueue tq = normal.SelectedTracks;
                        if (tq.Count > 0)
                        {
                            t = tq[0];
                            if (!t.ConfirmExists)
                            {
                                if (askAboutMissingFiles)
                                {
                                    RequestAction(new QAction(QActionType.AskAboutMissingFiles, t));
                                }
                                else
                                {
                                    normal.InvalidateAll();
                                    ShowMessage(Localization.Get(UI_Key.Message_Track_Not_Available, t.Title, t.Artist));
                                }
                                return;
                            }
                            if (tq.Count == 1)
                                ShowMessage(Localization.Get(UI_Key.Message_Now_Playing, t.ToString()));
                            else
                                ShowMessage(Localization.Get(UI_Key.Message_Play_Selected_Tracks));

                            Database.InsertAtBeginningOfNowPlaying(tq);
                            player.Play(Database.GetFirstTrackFromPlaylist(Localization.NOW_PLAYING));
                            normal.EnsureVisible(Database.GetFirstTrackFromPlaylist(Localization.NOW_PLAYING));
                        }
                        else
                        {
                        }
                    }
                    break;
                case PlayType.NextTrackManual:
                case PlayType.RequestNext:

                    // Zap the current track

                    removeCurrentlyPlayingFromNowPlaying();

                    // Check the next track on now playing

                    t = PeekNextTrack(true);

                    if (t != null)
                    {
                        if (PlayType == PlayType.NextTrackManual)
                        {
                            player.Play(t);
                            ShowMessage(Localization.Get(UI_Key.Message_Play_Next_Track, t.ToString()));
                        }
                        else
                        {
                            player.NextTrack = t;
                        }

                        if (normal.NowPlayingVisible)
                            normal.RefreshTrackList(false);

                        if (PlayType == PlayType.NextTrackManual)
                        {
                            normal.PlayingTrack = t;

                            if (Track.TimeSinceLastTrackSelectedChanged > 10000)
                                normal.EnsurePlayingTrackVisible();
                        }
                    }
                    break;
                case PlayType.PreviousTrack:

                    if (player.ElapsedTime > 2000 || (player.ElapsedTime > 0 && player.Paused))
                    {
                        ShowMessage(Localization.Get(UI_Key.Message_Rewind_To_Start));
                        player.ElapsedTime = 0;
                    }
                    else
                    {
                        t = normal.Queue.GetPrevious();
                        if (t == null)
                        {
                            Stack<Track> playedTracks = player.PlayedTracks;

                            if (playedTracks.Count > 0)
                            {
                                Track playing = null;
                                if (player.PlayingTrack != null && playedTracks.Peek() == player.PlayingTrack)
                                {
                                    playing = playedTracks.Pop();
                                }
                                if ((playedTracks.Count > 0) && ((t = playedTracks.Pop()) != null))
                                {
                                    ShowMessage(Localization.Get(UI_Key.Message_Play_Previous_Track, t.ToString()));
                                    Database.InsertInNowPlaying(t, 0);
                                    player.Play(t);
                                    normal.RefreshAll(false);
                                }
                                else
                                {
                                    if (playing != null)
                                    {
                                        playedTracks.Push(playing);
                                    }

                                    ShowMessage(Localization.Get(UI_Key.Message_Rewind_To_Start));
                                    player.ElapsedTime = 0;
                                }
                            }
                            else
                            {
                                ShowMessage(Localization.Get(UI_Key.Message_Rewind_To_Start));
                                player.ElapsedTime = 0;
                            }
                        }
                        else
                        {
                            removeCurrentlyPlayingFromNowPlaying();
                            player.Play(t);
                            normal.EnsurePlayingTrackVisible();
                            ShowMessage(Localization.Get(UI_Key.Message_Play_Previous_Track, t.ToString()));
                        }
                    }
                    break;
                case PlayType.Resume:
                    player.Resume();
                    break;
                case PlayType.FirstTrack:
                    if (normal.TrackCount > 0)
                    {
                        if (!normal.NowPlayingVisible)
                            clearNowPlaying();
                        player.Play(normal[0]);
                    }
                    break;
            }
        }
        private void play(Track Track)
        {
            if (player.Playing)
            {
                removeCurrentlyPlayingFromNowPlaying();
            }
            player.Play(Track);
        }
        
        private void removeCurrentlyPlayingFromNowPlaying()
        {
            Track t = player.PlayingTrack;

            if (t != null)
            {
                Database.RemoveFromNowPlaying(t);

                if (normal.NowPlayingVisible)
                {
                    normal.RemoveTrack(t);
                    t.Selected = false;
                }
            }
        }
        private void crawl()
        {
            Crawler.Directories = Database.CrawlDirs;
            Crawler.Start();
        }
        private void setMainFormTitle()
        {
            if (sleep != null && sleep.Active)
            {
                mainForm.Text = Application.ProductName + " - " + sleep.ToString(); ;
            }
            else
            {
                mainForm.Text = Application.ProductName;
            }
        }
        private void addFileToLibrary()
        {
            string path = String.Empty;
            Track t = normal.FirstSelectedOrFirst;
            if (t != null)
                path = t.FilePath;

            if (path.Length == 0 || !File.Exists(path))
                path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            else
                path = Path.GetDirectoryName(path);

            OpenFileDialog ofd = Lib.GetOpenFileDialog(path,
                                                       true,
                                                       Localization.Get(UI_Key.Lib_File_Filter),
                                                       (int)Database.GetSetting(SettingType.FileDialogFilterIndex, 12));

            ofd.Multiselect = true;

            if (ofd.ShowDialog(mainForm) == DialogResult.OK)
            {
                if (ofd.FileNames.Length > 0)
                {
                    AddToLibrary(ofd.FileNames);
                    Database.SaveSetting(SettingType.FileDialogFilterIndex, ofd.FilterIndex);
                }
            }
        }

        public void UpdateTrackOrStationCount()
        {
            Clock.Update(ref trackCountTimer, updateTrackOrStationCount, 250, false);
        }
        private void updateTrackOrStationCount()
        {
            trackCountTimer = Clock.NULL_ALARM;

            if (player.PlayingRadio || this.CurrentView == ViewType.Radio)
            {
                int numStations = radio.NumStationsDisplayed;
                if (numStations == 1)
                    controlPanel.TrackCountStatus = Localization.Get(UI_Key.Control_Panel_Station_Count_Singular, radio.NumStationsDisplayed.ToString());
                else
                    controlPanel.TrackCountStatus = Localization.Get(UI_Key.Control_Panel_Station_Count, radio.NumStationsDisplayed.ToString());
            }
            else
            {
                TrackQueue tq = normal.SelectedTracks;

                if (tq.Count < 2)
                {
                    tq = normal.Queue;
                }

                int numTracks = tq.Count;

                string status;
                if (numTracks == 1)
                    status = "1 Track";
                else
                    status = numTracks.ToString() + " Tracks";

                controlPanel.TrackCountStatus = String.Format("{0}: {1} ({2})", status, Lib.GetTimeStringLong(tq.TotalTime), Lib.GetTotalFileSizeString(tq.TotalSize));
            }
        }
        private void convertToStandardPlaylist()
        {
            string playlistName = normal.GetFilterValue(FilterType.Playlist);

            if (playlistName.Length > 0)
            {
                PlaylistType pt = Database.GetPlaylistType(playlistName);
                if (pt == PlaylistType.Auto)
                {
                    Database.ConvertPlaylistToStandard(playlistName);
                    TargetPlaylistName = playlistName;
                    ShowMessage(Localization.Get(UI_Key.Message_Converted_To_Standard, playlistName));
                }
            }
        }
        public void Preload()
        {
            Clock.Update(ref preloadTimer, doPreload, 600, true);
        }
        private void doPreload()
        {
            try
            {
                TrackQueue q = Database.GetPlaylistTracks(Localization.NOW_PLAYING);

                if (q.Count > 0)
                {
                    if (player.PlayingTrack == q[0])
                    {
                        if (q.Count > 1)
                        {
                            player.PreloadNextTrack(q[1]);
                            return;
                        }
                    }
                    else
                    {
                        player.PreloadNextTrack(q[0]);
                        return;
                    }
                }
                if (PlayingTrack != null && normal.Queue.Contains(PlayingTrack))
                {
                    int i = normal.Queue.IndexOf(PlayingTrack);
                    if (normal.Queue.Count > i + 1)
                    {
                        player.PreloadNextTrack(normal[i + 1]);
                        return;
                    }
                }
            }
            catch { }
        }
        private void updateAfterTrackStarts()
        {
            progressBar.TotalTime = player.TotalTime;
            artwork.CurrentTrack = player.PlayingTrack;
            normal.CurrentTrack = player.PlayingTrack;
            trackDisplay.CurrentTrack = player.PlayingTrack;
            controlPanel.Stopped = false;
            controlPanel.Paused = false;
            progressBar.Playing = true;

            PodcastManager.InvalidateAll();

            normal.PlayingTrack = player.PlayingTrack;

            TrackQueue q = Database.GetPlaylistTracks(Localization.NOW_PLAYING);

            if (q.Count < 1 || q[0] != player.PlayingTrack)
            {
                Database.InsertInNowPlaying(player.PlayingTrack, 0);
                if (normal.NowPlayingVisible)
                {
                    normal.RefreshTrackList(true);
                }
            }

            if (this.CurrentView == ViewType.AlbumDetails)
                albumDetails.CurrentTrack = player.PlayingTrack;

            Clock.Update(ref updateTimer, updateAfterTrackPlayingForABit, 10000, true);
        }
        private void updateAfterTrackPlayingForABit()
        {
            updateTimer = Clock.NULL_ALARM;

            Track t = player.PlayingTrack;
            
            if (t != null)
            {
                t.MarkAsPlayed();
                Twitter.SendTwitterMessage(t);
                if (lastFMOn)
                {
                    LastFM.Scrobble(t, lastFMUserName, lastFMPassword, true);
                    if (t.Duration > 30000)
                    {
                        pendingScrobbleTrack = t;
                        Clock.Update(ref lastFMTimer,
                                     scrobbleLastFM,
                                     (uint)Math.Max(0, Math.Min(t.Duration / 2, 240000) - (DateTime.Now - t.LastPlayedDate).TotalMilliseconds),
                                     true);
                    }
                }
            }
        }

        private void scrobbleLastFM()
        {
            lastFMTimer = Clock.NULL_ALARM;
            if (pendingScrobbleTrack == player.PlayingTrack)
                LastFM.Scrobble(pendingScrobbleTrack, lastFMUserName, lastFMPassword, false);
        }
        
        private void editAutoPlaylist()
        {
            string pl = normal.ActivePlaylist;

            if (pl.Length > 0)
            {
                PlaylistType pt = Database.GetPlaylistType(pl);
                if (pt == PlaylistType.Auto || pt == PlaylistType.Standard)
                {
                    frmEditAutoPlaylist eap = new frmEditAutoPlaylist();
                    eap.Icon = mainForm.Icon;
                    eap.PlaylistName = pl;
                    eap.Expression = Database.GetPlaylistExpression(pl);
                    eap.ShowDialog(mainForm);
                    Lib.DoEvents();
                    if (eap.DialogResult == DialogResult.OK)
                    {
                        SetPlaylistExpression(pl, eap.Expression, eap.Valid);
                        if (eap.Convert)
                        {
                            Database.ConvertPlaylistToStandard(pl);
                            TargetPlaylistName = pl;
                        }
                    }
                }
            }
        }
        private void removeSelectedPlaylist()
        {
            if (normal.CurrentFilterType == FilterType.Playlist && !normal.NowPlayingVisible)
            {
                DeletePlaylist(normal.CurrentFilterValue);
            }
        }
        private void renameSelectedPlaylist()
        {
            normal.RenameSelectedPlaylist();

            
        }
        private void createNewPlaylist()
        {
            QInputBox ib = new QInputBox(mainForm,
                                         Localization.Get(UI_Key.Dialog_Create_New_Playlist),
                                         Localization.Get(UI_Key.Dialog_Create_New_Playlist_Title),
                                         String.Empty,
                                         Localization.Get(UI_Key.Dialog_Create_Auto_Playlist),
                                         false,
                                         40,
                                         1);

            if (ib.DialogResult == DialogResult.OK)
            {
                string result = ib.Value.Trim();
                if (result.Length == 0)
                {
                    QMessageBox.Show(mainForm,
                                    Localization.Get(UI_Key.Dialog_Blank_Playlist_Name),
                                    Localization.Get(UI_Key.Dialog_Blank_Playlist_Name_Title),
                                    QMessageBoxIcon.Warning);
                }
                else if (Database.PlaylistExists(result))
                {
                    QMessageBox.Show(mainForm,
                                    Localization.Get(UI_Key.Dialog_Duplicate_Playlist, ib.Value),
                                    Localization.Get(UI_Key.Dialog_Duplicate_Playlist_Title),
                                    QMessageBoxIcon.Warning);
                }
                else
                {
                    if (Database.AddPlaylist(result))
                    {
                        normal.CurrentFilterType = FilterType.Playlist;
                        normal.ReleaseAllFilters();
                        normal.LoadFilterValues();
                        normal.FindFilterValue(result);
                                                if (ib.CheckboxChecked)
                            this.RequestAction(QActionType.EditAutoPlaylist);
                        else
                            TargetPlaylistName = result;
                    }
                }
            }
        }
        private void addSelectedTracksToTargetPlaylist(bool AdvanceSelection)
        {
            addSelectedTracksToPlaylist(TargetPlaylistName, AdvanceSelection);
        }
        private void addSelectedTracksToPlaylist(string Playlist, bool AdvanceSelection)
        {
            TrackQueue q = normal.SelectedTracks;

            if (q.Count > 0)
            {
                AddToPlaylist(Playlist, q);
                if (q.Count == 1)
                {
                    ShowMessage(Localization.Get(UI_Key.Message_Track_Added_To_Playlist, q[0].Title, Playlist));
                    if (AdvanceSelection)
                        normal.SelectTrack(normal.FirstSelectedIndex + 1, false, true, true);
                }
                else if (q.Count > 1)
                {
                    ShowMessage(Localization.Get(UI_Key.Message_Tracks_Added_To_Playlist, q.Count.ToString(), Playlist));
                }
            }
        }
        private void addSelectedTracksToNowPlaying(bool AdvanceSelection)
        {
            addSelectedTracksToPlaylist(Localization.NOW_PLAYING, AdvanceSelection);
            UpdateNextUpOrRadioBitrate();
        }
        private void viewNowPlaying()
        {
            normal.AllowEvents = false;
            normal.SetFilterValue(FilterType.Playlist, Localization.NOW_PLAYING, true);
            normal.CurrentFilterType = FilterType.Playlist;
            setView(ViewType.Normal, false, false);
            normal.AllowEvents = true;
            controlPanel.NowPlaying = true;
            normal.RefreshAll(false);
        }
        private void setView(ViewType NewView, bool Force, bool HideMousePointer)
        {
            if (Force || CurrentView != NewView)
            {
                this.CurrentView = NewView;

                mainForm.SetView(CurrentView, HideMousePointer);
                switch (this.CurrentView)
                {
                    case ViewType.Normal:
                        normal.FreshView();
                        
                        pad.AnalogEnabled = true;
                        controlPanel.RadioView = false;
                        if (player.Playing && normal.Queue.Contains(player.PlayingTrack))
                        {
                            normal.SelectTrack(normal.Queue.IndexOf(player.PlayingTrack), true, true, true);
                            normal.CurrentTrack = player.PlayingTrack;
                        }
                        break;
                    case ViewType.Radio:
                        pad.AnalogEnabled = true;
                        controlPanel.RadioView = true;
                        break;
                    case ViewType.Spectrum:
                        spectrumView.Spectrum = player.Spectrum;
                        pad.AnalogEnabled = false;
                        controlPanel.RadioView = player.PlayingRadio;
                        break;
                    case ViewType.Artwork:
                        pad.AnalogEnabled = false;
                        controlPanel.RadioView = false;
                        break;
                    case ViewType.Equalizer:
                        pad.AnalogEnabled = false;
                        controlPanel.RadioView = player.PlayingRadio;
                        break;
                    case ViewType.AlbumDetails:
                        pad.AnalogEnabled = true;
                        albumDetails.CurrentTrack = this.UsersMostInterestingTrack;
                        controlPanel.RadioView = player.PlayingRadio;
                        break;
                    case ViewType.TagCloud:
                        pad.AnalogEnabled = false;
                        controlPanel.RadioView = false;
                        break;
                    case ViewType.Podcast:
                        pad.AnalogEnabled = false;
                        break;
                }
                switch (this.CurrentView)
                {
                    case ViewType.Radio:
                        pad.TempHandler = radio;
                        this.tempHandler = radio;
                        break;
                    case ViewType.AlbumDetails:
                        pad.TempHandler = albumDetails;
                        this.tempHandler = albumDetails;
                        break;
                    case ViewType.Podcast:
                        pad.TempHandler = null;
                        this.tempHandler = podcastManager;
                        break;
                    default:
                        if (pad.TempHandler != null && pad.TempHandler.Type != ActionHandlerType.HelpScreen)
                        {
                            pad.TempHandler = null;
                            this.tempHandler = null;
                        }
                        break;
                }
                UpdateNowPlaying();
            }
        }
        public void UpdateNowPlaying()
        {
            controlPanel.NowPlaying = (CurrentView == ViewType.Normal) && normal.NowPlayingVisible;
        }

        private void advanceView(bool HideMousePointerIfAppropriate)
        {
            if (player.PlayingRadio)
            {
                switch (this.CurrentView)
                {
                    case ViewType.Radio:
                        setView(ViewType.Spectrum, false, HideMousePointerIfAppropriate);
                        break;
                    default:
                        setView(ViewType.Radio, false, false);
                        break;
                }
            }
            else
            {
                switch (CurrentView)
                {
                    case ViewType.Normal:
                        if (Setting.IncludeTagCloud)
                            setView(ViewType.TagCloud,
                                    false,
                                    false);
                        else
                            setView(ViewType.Spectrum,
                                    false,
                                    HideMousePointerIfAppropriate);
                        break;
                    case ViewType.Radio:
                        setView(ViewType.Normal, false, true);
                        break;
                    case ViewType.TagCloud:
                        if (Playing)
                            setView(ViewType.Spectrum,
                                    false,
                                    HideMousePointerIfAppropriate);
                        else if (artwork.HasImage)
                            setView(ViewType.Artwork,
                                    false,
                                    HideMousePointerIfAppropriate);
                        else
                            setView(ViewType.Normal,
                                    false,
                                    false);
                        break;
                    case ViewType.Spectrum:
                        if (artwork.HasImage)
                        {
                            setView(ViewType.Artwork,
                                    false,
                                    HideMousePointerIfAppropriate);
                        }
                        else
                        {
                            setView(ViewType.Normal,
                                    false,
                                    false);
                        }
                        break;
                    case ViewType.Artwork:
                    case ViewType.Equalizer:
                    case ViewType.AlbumDetails:
                    case ViewType.Podcast:
                        setView(ViewType.Normal,
                                false,
                                false);
                        break;
                }
            }
        }
        public bool RadioMode
        {
            get { return this.CurrentView == ViewType.Radio || (this.CurrentView == ViewType.Spectrum && player.PlayingRadio); }
        }
        private void updateVolume()
        {
            string s;
            if (LocalVolumeControl)
            {
                s = LocalVolumeString;
                controlPanel.Volume = LocalVolumeLevel;
            }
            else
            {
                s = WinAudioLib.VolumeDBString;
                controlPanel.Volume = WinAudioLib.VolumeDB;
            }

            if (Setting.Mute)
                controlPanel.VolumeStatus = Localization.MUTE;
            else if (equalizer.On)
                controlPanel.VolumeStatus = String.Format(Localization.VOL_WITH_EQ, s, equalizer.CurrentEqualizer.Name);
            else
                controlPanel.VolumeStatus = String.Format(Localization.VOLUME, s);

            ShowMessage(Localization.Get(UI_Key.Message_Volume, s));
        }
        private void addToLibrary(List<Track> tracks)
        {
            Database.AddToLibrary(tracks, Setting.ShortTrackCutoff * 1000, true);
        }
        private void clockTick()
        {
            progressBar.ElapsedTime = player.ElapsedTime;
            
            if (miniPlayer != null)
                miniPlayer.Time = player.ElapsedTime;

            if (CurrentView == ViewType.Spectrum)
            {
                spectrumView.Spectrum = player.GetSpectrumData();
            }
            if (player.Clipping)
            {
                controlPanel.ShowGainWarning = true;
                if (Setting.AutoClipControl & DecoderGainDB > -6.0f)
                {
                    DecoderGainDB = Math.Max(-6.0f, DecoderGainDB - 0.1f);
                    controlPanel.GainStatus = Localization.Get(UI_Key.Control_Panel_Clipping) + " / " + this.gainStatus;
                }
                else
                {
                    controlPanel.GainStatus = Localization.Get(UI_Key.Control_Panel_Clipping);
                }
            }
            else if (controlPanel.ShowGainWarning)
            {
                controlPanel.ShowGainWarning = false;
                updateGainStatus();
            }
        }
        private void playThisAlbum(Track Track)
        {
            if (Track.Album.Length > 0)
            {
                List<Track> tracks;
                
                if (Track.MainGroup.Length > 0)
                    tracks = Database.FindAllTracks(t => t.Album == Track.Album && t.MainGroup == Track.MainGroup && t.ConfirmExists);
                else
                    tracks = Database.FindAllTracks(t => t.Album == Track.Album && t.ConfirmExists);
                
                playAlbum(tracks, Track.MainGroup, Track.Album);
            }
        }
        private void addAlbumToNowPlaying(Track Track)
        {
            if (Track.Album.Length > 0)
            {
                string message;

                List<Track> album;

                if (Track.MainGroup.Length > 0)
                {
                    album = Database.FindAllTracks(t => t.Album == Track.Album && t.MainGroup == Track.MainGroup && t.ConfirmExists);
                    message = Track.Album + " - " + Track.MainGroup;
                }
                else
                {
                    album = Database.FindAllTracks(t => t.Album == Track.Album && t.ConfirmExists);
                    message = Track.Album;
                }

                album.Sort((t1, t2) => t1.AlbumComparer(t2));

                AddToPlaylist(Localization.NOW_PLAYING, album);

                ShowMessage(Localization.Get(UI_Key.Message_Add_This_Album_To_Now_Playing, message));
                
                UpdateNextUpOrRadioBitrate();
            }
        }
        private void playRandomAlbum()
        {
            List<string> a = Database.GetAlbums();

            if (a.Count > 0)
            {
                try
                {
                    Random r = new Random();
                    string albumName = a.ElementAt(r.Next(a.Count));
                    List<Track> b = Database.FindAllTracks(t => String.Compare(t.Album, albumName, StringComparison.OrdinalIgnoreCase) == 0);

                    if (b.Count > 0)
                    {
                        Track tt = b.ElementAt(r.Next(b.Count));
                        List<Track> trax = Database.FindAllTracks(t => String.Compare(t.Album, tt.Album, StringComparison.OrdinalIgnoreCase) == 0 && String.Compare(t.MainGroup, tt.MainGroup, StringComparison.OrdinalIgnoreCase) == 0);

                        if (trax.Count > 0)
                            playAlbum(trax, tt.MainGroup, tt.Album);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
            }
        }
        private void clearNowPlaying()
        {
            Database.ClearPlaylist(Localization.NOW_PLAYING, null);
        }
        private void playAlbum(List<Track> Tracks, string Artist, string AlbumName)
        {
            Tracks.Sort((t1, t2) => t1.AlbumComparer(t2));

            player.Stop();

            clearNowPlaying();
            
            normal.NoSort();

            AddToPlaylist(Localization.NOW_PLAYING, Tracks);

            normal.SelectNone();

            normal.AllowEvents = false;
            normal.SetFilterValue(FilterType.Playlist, Localization.NOW_PLAYING, true);
            normal.AllowEvents = true;
            normal.RefreshAll(true);

            RequestAction(QActionType.Play);

            ShowMessage("Playing Album: " + Artist + " - " + AlbumName);
        }
        private void setKeyPreview()
        {
            mainForm.KeyPreview = !normal.KeyPreview && !radio.KeyPreview && !podcastManager.KeyPreview;
        }
        private void progressBarClick(float Percentage)
        {
            player.Seek(Percentage);
        }
        
        private void checkForUpdate()
        {
            checkForUpdate(false);
        }
        private void getHTML()
        {
            htmlResp = Net.Get(url);
            urlResponded = true;
        }
        private void checkForUpdate(bool UserInvoked)
        {
            url = Lib.PRODUCT_URL + "/quuxplayercurrentversion.php?version=" + Lib.GetVersion();
            htmlResp = String.Empty;
            urlResponded = false;

            Clock.DoOnNewThread(getHTML, 50);

            const int delay = 10;
            const int maxLoops = 10000 / delay;
            int loops = 0;
            while (!urlResponded && !exiting && (++loops < maxLoops))
            {
                Lib.DoEvents();
                System.Threading.Thread.Sleep(delay);
            }

            string version = Net.GetContentBetweenTags(htmlResp, "<!--Build", "-->");

            bool updateAvail = false;
            bool error = false;
            bool minor = false;

            if (version.Length > 0)
            {
                string[] verAvail = version.Split(new char[] { '.' });
                string[] thisVer = Application.ProductVersion.Split(new char[] { '.' });

                int a = 0;
                int b = 0;

                for (int i = 0; i < 4; i++)
                {
                    if (verAvail.Length > i && thisVer.Length > i)
                    {
                        if (!Int32.TryParse(verAvail[i], out a))
                            break;
                        if (!Int32.TryParse(thisVer[i], out b))
                            break;
                        if (a > b)
                        {
                            minor = (i == 3);
                            updateAvail = true;
                            break;
                        }
                        if (a < b)
                        {
                            updateAvail = false;
                            break;
                        }
                    }
                }
            }
            else
            {
                error = true;
            }
            if (updateAvail)
            {
                if (UserInvoked || !minor)
                {
                    if (UserInvoked || Lib.FullScreen)
                    {
                        if (QMessageBox.Show(mainForm,
                                             Localization.Get(minor ? UI_Key.Dialog_Update_Available_Minor : UI_Key.Dialog_Update_Available, Application.ProductName, version),
                                             Localization.Get(UI_Key.Dialog_Update_Available_Title),
                                             QMessageBoxButtons.OKCancel,
                                             QMessageBoxIcon.Information,
                                             QMessageBoxButton.YesOK)
                                                == DialogResult.OK)
                        {
                            mainForm.Invalidate();
                            Lib.DoEvents();
                            Net.BrowseTo(url);
                        }
                    }
                    else
                    {
                        Clock.DoOnMainThread(showUpdateBalloon, 500);
                    }
                }
            }
            else if (UserInvoked)
            {
                if (!error)
                {
                    System.Diagnostics.Debug.Assert(!updateAvail);
                    QMessageBox.Show(mainForm,
                                     Localization.Get(UI_Key.Dialog_No_Update_Available),
                                     Localization.Get(UI_Key.Dialog_No_Update_Available_Title),
                                     QMessageBoxIcon.Information);
                }
                else
                {
                    QMessageBox.Show(mainForm,
                                     Localization.Get(UI_Key.Dialog_Update_Check_Error),
                                     Localization.Get(UI_Key.Dialog_Update_Check_Error_Title),
                                     QMessageBoxIcon.Error);
                }
            }
        }
        private void showUpdateBalloon()
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
            }

            notifyIcon = new NotifyIcon();
            notifyIcon.BalloonTipText = Localization.Get(UI_Key.Balloon_Update_Available, Application.ProductName);
            notifyIcon.BalloonTipTitle = Localization.Get(UI_Key.Balloon_Update_Available_Title);
            notifyIcon.Icon = mainForm.Icon;
            notifyIcon.Visible = true;
            notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            EventHandler click = (s, e) =>
            {
                Net.BrowseTo(url);
                notifyIcon.Visible = false;
            };

            notifyIcon.Click += click;
            notifyIcon.BalloonTipClicked += click;

            notifyIcon.ShowBalloonTip(20000);
        }
        private void doCommandLine()
        {
            try
            {
                if (Environment.GetCommandLineArgs().Count() > 1)
                {
                    List<string> ls = Environment.GetCommandLineArgs().ToList();
                    ls.RemoveAt(0);

                    List<Track> lt = new List<Track>();
                    List<Track> toAdd = new List<Track>();

                    Track t;

                    foreach (string s in ls)
                        if (File.Exists(s))
                        {
                            switch (Path.GetExtension(s).ToLowerInvariant())
                            {
                                case ".pls":
                                case ".m3u":
                                    List<Track> trax;
                                    string playlistName = ImportExport.ImportPlaylist(s, out trax);
                                    if (playlistName.Length > 0)
                                    {
                                        addPlaylistToLibrary(trax, playlistName);
                                    }
                                    break;
                                default:
                                    t = Database.GetTrackWithFilePath(s);

                                    if (t == null)
                                    {
                                        t = Track.Load(s);
                                        if (t != null)
                                            toAdd.Add(t);
                                    }

                                    if (t != null)
                                        lt.Add(t);
                                    break;
                            }
                        }

                    if (toAdd.Count > 0)
                        AddToLibrarySilent(toAdd, true);

                    if (lt.Count > 0)
                    {
                        Database.AddToPlaylist(Localization.NOW_PLAYING, lt);
                        player.Play(lt[0]);
                    }
                }
            }
            catch { }
        }
        private void saveViewState()
        {
            ViewState vs = new ViewState();

            vs.ViewType = this.CurrentView;
            normal.SaveViewState(vs);
            
            ViewState.PreviousViewState = vs;
        }
        private void restoreViewState()
        {
            if (ViewState.PreviousViewState == null)
            {
                normal.ReleaseAllFilters();
            }
            else
            {
                setView(ViewState.PreviousViewState.ViewType, false, false);
                Lib.DoEvents();
                normal.RestoreViewState(ViewState.PreviousViewState);
                normal.RefreshAll(false);
                ViewState.PreviousViewState = null;
                controlPanel.NowPlaying = normal.NowPlayingVisible && (CurrentView == ViewType.Normal);
            }
        }
        private void processUnsavedTracks()
        {
            // process even if writing disabled b/c will reset unsaved flag
            TrackWriter.AddToUnsavedTracks(Database.FindAllTracks(t => t.ChangeType != ChangeType.None));
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
    }
}
