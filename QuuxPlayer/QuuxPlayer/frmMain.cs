/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal sealed partial class frmMain : Form
    {
        private static readonly float[] decoderGainSettingsInDB = new float[] { 15f, 12f, 9f, 6f, 3f, 1.5f, 0f, -0.5f, -1f, -1.5f, -2f, -3f, -6f, -9f, -12f, -15f };

        private delegate void EmptyDelegate();
                
        private bool screenSaverWasActive;
        private bool settingView;

        private Controller controller;

        private Equalizer equalizer;
        private ControlPanel controlPanel;
        private AlbumDetails albumDetails;
        private NormalView normal;
        private ProgressBar progressBar;
        private SpectrumView spectrumView;
        private TagCloud tagCloud;
        private Radio radio;
        private PodcastManager podCastManager;
        private Artwork artwork;
        private TrackDisplay trackDisplay;
        private bool locked;
        private bool initialized = false;
        private static frmMain instance = null;
        private ulong powerEventAlarm = Clock.NULL_ALARM;

        private List<IMainView> views = new List<IMainView>();

        private bool abort = false;

#if DEBUG
        public static bool Started = false;
#endif

        public frmMain()
        {
            instance = this;

            if (SingletonApp.AlreadyExists)
            {
                if (Environment.GetCommandLineArgs().Length < 2)
                {
                    QMessageBox.Show(this,
                                     Localization.Get(UI_Key.Dialog_Already_Running, Application.ProductName),
                                     Localization.Get(UI_Key.Dialog_Already_Running_Title),
                                     QMessageBoxIcon.Information);
                }
                else
                {
                    SingletonApp.NotifyExistingInstance(Environment.GetCommandLineArgs());
                }
                abort = true;
                return;
            }
            else
            {
                string arg = String.Empty;
                if (Environment.GetCommandLineArgs().Length > 1)
                    arg = Environment.GetCommandLineArgs()[1];

                switch (arg.ToLowerInvariant())
                {
                    case "/reinstall":
                    case "/hideicons":
                    case "/showicons":
                        abort = true;
                        return;
                    default:
                        SingletonApp.Initialize();
                        SingletonApp.NewInstanceMessage += new NewInstanceMessageEventHandler(SingleInstanceApplication_NewInstanceMessage);
                        break;
                }
            }
            
            settingView = false;
            locked = false;
            
            setupControls();
            
            init();

            initialized = true;

            initalizeController();

            mnuMain.Renderer = new MenuItemRenderer();

            screenSaverWasActive = Lib.ScreenSaverIsActive;
            
            if (Setting.DisableScreensaver)
                Lib.ScreenSaverIsActive = false;

            this.KeyPreview = true;

            SetView(ViewType.Normal, false);
            
            preventPowerEvent();

            QSplitContainer.Initialized = true;
#if DEBUG
            setupTestMenu();

            Started = true;
#endif
        }
        public static frmMain GetInstance()
        {
            return instance;
        }
        
        private bool forceNoBorder = false;
#if DEBUG
        private ToolStripMenuItem mnuTest;
        private ToolStripMenuItem mnuTestSet1024x768;
        private ToolStripMenuItem mnuTestSet800x600;
        

        private void setupTestMenu()
        {
            this.mnuTestSet1024x768 = new ToolStripMenuItem();
            mnuTestSet1024x768.Text = "Set 1024 x 768 resolution";
            mnuTestSet1024x768.Click += (s, e) =>
            {
                forceNoBorder = true;
                SetView(controller.CurrentView, false);
                this.Size = new Size(1024, 768);
                SetView(controller.CurrentView, false);
            };

            this.mnuTestSet800x600 = new ToolStripMenuItem();
            mnuTestSet800x600.Text = "Set 800 x 600 resolution";
            mnuTestSet800x600.Click += (s, e) =>
            {
                forceNoBorder = true;
                SetView(controller.CurrentView, false);
                this.Size = new Size(800, 600);
                SetView(controller.CurrentView, false);
            };

            this.mnuTest = new ToolStripMenuItem();
            mnuTest.Text = "Test";
            mnuTest.DropDownItems.Add(mnuTestSet1024x768);
            mnuTest.DropDownItems.Add(mnuTestSet800x600);
            mnuMain.Items.Add(mnuTest);
        }
#endif
        
        
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (abort)
            {
                this.Close();
                return;
            }
        }

        private void SingleInstanceApplication_NewInstanceMessage(object sender, object message)
        {
            string[] s = message as string[];

            if (s != null)
            {
                if (s.Length == 2)
                {
                    controller.AddAndPlay(s[1], CommandLineActionType.Play);
                }
                else if (s.Length >= 3)
                {
                    switch (s[1].ToLowerInvariant())
                    {
                        case "/enqueue":
                            for (int i = 2; i < s.Length; i++)
                                controller.AddAndPlay(s[i], CommandLineActionType.Enqueue);
                            break;
                        case "/playnext":
                            for (int i = s.Length - 1; i >= 2; i--)
                                controller.AddAndPlay(s[i], CommandLineActionType.PlayNext);
                            break;
                        case "/add":
                            for (int i = 2; i < s.Length; i++)
                                controller.AddAndPlay(s[i], CommandLineActionType.Add);
                            break;
                        case "/hideicons":
                            break;
                        case "/showicons":
                            break;
                        case "/reinstall":
                            break;
                        default:
                            for (int i = 3; i < s.Length; i++)
                                controller.AddAndPlay(s[i], CommandLineActionType.Enqueue);
                            controller.AddAndPlay(s[2], CommandLineActionType.Play);
                            break;
                    }
                }
            }
        }
        
        public bool Locked
        {
            get { return locked; }
            set
            {
                if (locked != value)
                {
                    locked = value;
                    mnuEdit.Enabled = !locked;
                    mnuPlay.Enabled = !locked;
                    mnuPlaylists.Enabled = !locked;
                    mnuFilters.Enabled = !locked;
                    mnuView.Enabled = !locked;
                    mnuOptions.Enabled = !locked;
                    mnuInternet.Enabled = !locked;
                    mnuHelp.Enabled = !locked;
                }
            }
        }
        private void setupControls()
        {
            this.SuspendLayout();

            this.AutoScaleMode = AutoScaleMode.None;

            Styles.SetDPI(this.CreateGraphics().DpiX);

            Controls.Add(normal);

            trackDisplay = new QuuxPlayer.TrackDisplay();
            this.Controls.Add(this.trackDisplay);

            progressBar = new QuuxPlayer.ProgressBar();
            this.Controls.Add(this.progressBar);

            controlPanel = new QuuxPlayer.ControlPanel();
            this.Controls.Add(this.controlPanel);

            equalizer = new Equalizer();
            equalizer.Visible = false;
            this.Controls.Add(equalizer);
            views.Add(equalizer);

            albumDetails = new AlbumDetails();
            albumDetails.Visible = false;
            this.Controls.Add(albumDetails);
            views.Add(albumDetails);

            spectrumView = new QuuxPlayer.SpectrumView();
            spectrumView.Visible = false;
            this.Controls.Add(this.spectrumView);
            views.Add(spectrumView);

            tagCloud = new TagCloud();
            tagCloud.Visible = false;
            this.Controls.Add(this.tagCloud);
            views.Add(tagCloud);

            radio = new Radio();
            radio.Visible = false;
            this.Controls.Add(radio);
            views.Add(radio);

            podCastManager = new PodcastManager();
            podCastManager.Visible = false;
            this.Controls.Add(podCastManager);
            views.Add(podCastManager);

            artwork = new QuuxPlayer.Artwork();
            artwork.Visible = false;
            this.Controls.Add(this.artwork);
            views.Add(artwork);

            normal = new NormalView(this);
            this.Controls.Add(normal);
            views.Add(normal);

            this.ResumeLayout();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (KeyPreview && !normal.HasPanel)
            {
                switch (keyData)
                {
                    case KeyDefs.MoveDown:
                        controller.RequestAction(QActionType.MoveDown);
                        return true;
                    case KeyDefs.MoveUp:
                        controller.RequestAction(QActionType.MoveUp);
                        return true;
                    case KeyDefs.MoveLeft:
                        controller.RequestAction(QActionType.MoveLeft);
                        return true;
                    case KeyDefs.MoveRight:
                        controller.RequestAction(QActionType.MoveRight);
                        return true;
                    case Keys.Control | KeyDefs.MoveUp:
                        if (normal.NondynamicPlaylistBasedView)
                        {
                            if (!Locked)
                                controller.RequestAction(QActionType.MoveTracksUp);
                        }
                        else
                        {
                            controller.RequestAction(QActionType.MoveUp);
                        }
                        return true;
                    case Keys.Control | KeyDefs.MoveDown:
                        if (normal.NondynamicPlaylistBasedView)
                        {
                            if (!Locked)
                                controller.RequestAction(QActionType.MoveTracksDown);
                        }
                        else
                        {
                            controller.RequestAction(QActionType.MoveDown);
                        }
                        return true;
                    case Keys.Shift | KeyDefs.MoveDown:
                        normal.MoveDown(1, true);
                        return true;
                    case Keys.Shift | KeyDefs.MoveUp:
                        normal.MoveUp(1, true);
                        return true;
                    case Keys.Alt | KeyDefs.MoveDown:
                        controller.RequestAction(QActionType.VolumeDown);
                        return true;
                    case Keys.Alt | KeyDefs.MoveUp:
                        controller.RequestAction(QActionType.VolumeUp);
                        return true;
                    case KeyDefs.Rename:
                        if (!Locked)
                        {
                            switch (controller.CurrentView)
                            {
                                case ViewType.Normal:
                                    controller.RequestAction(QActionType.RenameSelectedPlaylist);
                                    break;
                                case ViewType.Radio:
                                    controller.RequestAction(QActionType.RenameSelectedRadioGenre);
                                    break;
                            }
                        }
                        return true;
                    default:
                        return base.ProcessCmdKey(ref msg, keyData);
                }
            }
            else
            {
                return false;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            normal.RemoveFilterIndex();

            if (Locked && e.KeyCode != KeyDefs.AdvanceScreen)
            {
                base.OnKeyDown(e);
                return;
            }
            if (e.Alt)
            {
                if (e.KeyCode == KeyDefs.Enter)
                {
                    if (Lib.FullScreen)
                        controller.RequestAction(QActionType.ReleaseFullScreen);
                    else
                        controller.RequestAction(QActionType.SetFullScreen);
                    e.Handled = true;
                }
            }
            else
            {
                e.Handled = true;
                switch (e.KeyCode)
                {
                    case KeyDefs.Enter:
                        if (e.Shift)
                        {
                            if (controller.AllowTagEditing)
                                controller.RequestAction(QActionType.EditTags);
                        }
                        else
                        {
                            controller.RequestAction(QActionType.PlaySelectedTracks);
                        }
                        break;
                    case KeyDefs.Play:
                    case KeyDefs.Play2:
                        controller.RequestAction(QActionType.Play);
                        break;
                    case KeyDefs.Pause:
                        controller.RequestAction(QActionType.Pause);
                        break;
                    case KeyDefs.PlayPause:
                        controller.RequestAction(QActionType.TogglePlayPause);
                        break;
                    case KeyDefs.Stop:
                    case KeyDefs.Stop2:
                        if (e.Shift)
                            controller.RequestAction(QActionType.StopAfterThisTrack);
                        else
                            controller.RequestAction(QActionType.Stop);
                        break;
                    case KeyDefs.Next:
                    case KeyDefs.Next2:
                        controller.RequestAction(QActionType.Next);
                        break;
                    case KeyDefs.Previous:
                    case KeyDefs.Previous2:
                        controller.RequestAction(QActionType.Previous);
                        break;
                    case KeyDefs.ScanFwd:
                        controller.RequestAction(QActionType.ScanFwd);
                        break;
                    case KeyDefs.ScanBack:
                        controller.RequestAction(QActionType.ScanBack);
                        break;
                    case KeyDefs.PlayThisAlbum:
                        if (e.Shift)
                            controller.RequestAction(QActionType.AddAlbumToNowPlaying);
                        else
                            controller.RequestAction(QActionType.PlayThisAlbum);
                        break;
                    case KeyDefs.FocusSearchBox:
                        controller.RequestAction(QActionType.FocusSearchBox);
                        break;
                    case KeyDefs.ShowPlayingTrack:
                        controller.RequestAction(QActionType.FindPlayingTrack);
                        break;
                    case KeyDefs.Podcasts:
                        controller.RequestAction(QActionType.TogglePodcastView);
                        break;
                    case KeyDefs.Delete:
                        controller.RequestAction(QActionType.Delete);
                        break;
                    case KeyDefs.SelectAllOrNone:
                        if (e.Control)
                        {
                            if (e.Shift)
                                controller.RequestAction(QActionType.SelectNone);
                            else
                                controller.RequestAction(QActionType.SelectAll);
                        }
                        else
                        {
                            e.Handled = false;
                        }
                        break;
                    case KeyDefs.Shuffle:
                        if (e.Shift)
                        {
                            Lib.DoEvents();
                            controller.RequestAction(QActionType.PlayRandomAlbum);
                        }
                        else
                        {
                            controller.RequestAction(QActionType.Shuffle);
                        }
                        break;
                    case KeyDefs.AutoPlaylistAction:
                        if (e.Shift)
                            controller.RequestAction(QActionType.ConvertToStandardPlaylist);
                        else
                            controller.RequestAction(QActionType.EditAutoPlaylist);
                        break;
                    case KeyDefs.ClearAllFilters:
                        controller.RequestAction(QActionType.ReleaseAllFilters);
                        break;
                    case KeyDefs.FilterSelected:
                        if (e.Shift)
                            controller.RequestAction(QActionType.FilterSelectedAlbum);
                        else if (e.Control)
                            controller.RequestAction(QActionType.FilterSelectedGenre);
                        else
                            controller.RequestAction(QActionType.FilterSelectedArtist);
                        break;
                    case KeyDefs.ShowInfoFromInternet:
                        if (e.Control)
                        {
                            controller.RequestAction(QActionType.LastFMShowAlbum);
                        }
                        else if (e.Shift)
                        {
                            controller.RequestAction(QActionType.LastFMShowArtist);
                        }
                        else
                        {
                            controller.RequestAction(QActionType.ShowTrackAndAlbumDetails);
                        }
                        break;
                    case KeyDefs.PlaySelectedNext:
                        controller.RequestAction(QActionType.PlaySelectedTrackNext);
                        break;
                    case KeyDefs.PlaylistAction:
                        if (e.Shift)
                        {
                            controller.RequestAction(QActionType.ViewNowPlaying);
                        }
                        else if (e.Control)
                        {
                            if (controller.PlaylistExists(controller.TargetPlaylistName) && normal.HasSelectedTracks)
                            {
                                if (normal.SelectedTracks.Count == 1)
                                    controller.RequestAction(QActionType.AddToTargetPlaylistAndAdvance);
                                else
                                    controller.RequestAction(QActionType.AddToTargetPlaylist);
                            }
                        }
                        else
                        {
                            controller.RequestAction(QActionType.AddToNowPlayingAndAdvance);
                        }
                        break;
                    case KeyDefs.FileInfo:
                        if (e.Shift)
                            controller.RequestAction(QActionType.RefreshSelectedTracks);
                        else
                            controller.RequestAction(QActionType.ShowFileDetails);
                        break;
                    case KeyDefs.ShowEqualizer:
                        if (e.Control)
                            controller.RequestAction(QActionType.ShowEqualizer);
                        else if (e.Shift)
                            controller.RequestAction(QActionType.SelectNextEqualizer);
                        else
                            controller.RequestAction(QActionType.ToggleEqualizer);
                        break;
                    case KeyDefs.MiniPlayer:
                        controller.RequestAction(QActionType.ShowMiniPlayer);
                        break;
                    case KeyDefs.HTPCMode:
                        if (e.Shift)
                            controller.RequestAction(QActionType.ShowTagCloud);
                        else
                            controller.RequestAction(QActionType.HTPCMode);
                        break;
                    case KeyDefs.AdvanceScreen:
                        controller.RequestAction(QActionType.AdvanceScreenWithoutMouse);
                        break;
                    case KeyDefs.GainUp:
                        controller.RequestAction(QActionType.SpectrumViewGainUp);
                        break;
                    case KeyDefs.GainDown:
                        controller.RequestAction(QActionType.SpectrumViewGainDown);
                        break;
                    case KeyDefs.VolUp:
                    case KeyDefs.VolUp2:
                        controller.RequestAction(QActionType.VolumeUp);
                        break;
                    case KeyDefs.VolDown:
                    case KeyDefs.VolDown2:
                        controller.RequestAction(QActionType.VolumeDown);
                        break;
                    case KeyDefs.Mute:
                        controller.RequestAction(QActionType.Mute);
                        break;
                    case KeyDefs.Radio:
                        if (e.Shift)
                            controller.RequestAction(QActionType.RepeatToggle);
                        else
                            controller.RequestAction(QActionType.ToggleRadioMode);
                        break;
                    case KeyDefs.Gain:
                        if (e.Shift)
                            controller.RequestAction(QActionType.ReplayGainAnalyzeSelectedTracks);
                        break;
                    case KeyDefs.Exit:
                        if (e.Shift)
                        {
                            Controller.ShowMessage(Localization.Get(UI_Key.General_Exit));
                            controller.RequestAction(QActionType.Exit);
                        }
                        break;
                    case KeyDefs.Cancel:
                        controller.RequestAction(QActionType.Cancel);
                        break;
                    case KeyDefs.PreviousFilter:
                        controller.RequestAction(QActionType.PreviousFilter);
                        break;
                    case KeyDefs.NextFilter:
                        controller.RequestAction(QActionType.NextFilter);
                        break;
                    case KeyDefs.ReleaseCurrentFilter:
                        controller.RequestAction(QActionType.ReleaseCurrentFilter);
                        break;
                    case KeyDefs.ReleaseAllFilters:
                        controller.RequestAction(QActionType.ReleaseAllFilters);
                        break;
                    case KeyDefs.ShowFilterIndex:
                        controller.RequestAction(QActionType.ShowFilterIndex);
                        break;
                    case KeyDefs.PageUp:
                        controller.RequestAction(QActionType.PageUp);
                        break;
                    case KeyDefs.PageDown:
                        controller.RequestAction(QActionType.PageDown);
                        break;
                    case KeyDefs.Home:
                        controller.RequestAction(QActionType.Home);
                        break;
                    case KeyDefs.End:
                        controller.RequestAction(QActionType.End);
                        break;
                    case KeyDefs.Playlists:
                        if (e.Shift)
                        {
                            System.Diagnostics.Debug.Assert(e.KeyCode == KeyDefs.One);
                            controller.SetRatingOfSelectedTracks(1);
                        }
                        else
                        {
                            normal.CurrentFilterType = FilterType.Playlist;
                        }
                        break;
                    case KeyDefs.Genres:
                        if (e.Shift)
                        {
                            System.Diagnostics.Debug.Assert(e.KeyCode == KeyDefs.Two);
                            controller.SetRatingOfSelectedTracks(2);
                        }
                        else
                        {
                            normal.CurrentFilterType = FilterType.Genre;
                        }
                        break;
                    case KeyDefs.Artists:
                        if (e.Shift)
                        {
                            System.Diagnostics.Debug.Assert(e.KeyCode == KeyDefs.Three);
                            controller.SetRatingOfSelectedTracks(3);
                        }
                        else
                        {
                            normal.CurrentFilterType = FilterType.Artist;
                        }
                        break;
                    case KeyDefs.Albums:
                        if (e.Shift)
                        {
                            System.Diagnostics.Debug.Assert(e.KeyCode == KeyDefs.Four);
                            controller.SetRatingOfSelectedTracks(4);
                        }
                        else
                        {
                            normal.CurrentFilterType = FilterType.Album;
                        }
                        break;
                    case KeyDefs.Years:
                        if (e.Shift)
                        {
                            System.Diagnostics.Debug.Assert(e.KeyCode == KeyDefs.Five);
                            controller.SetRatingOfSelectedTracks(5);
                        }
                        else
                        {
                            normal.CurrentFilterType = FilterType.Year;
                        }
                        break;
                    case KeyDefs.Zero:
                        if (e.Shift)
                        {
                            controller.SetRatingOfSelectedTracks(0);
                        }
                        break;
                    case KeyDefs.Groupings:
                        normal.CurrentFilterType = FilterType.Grouping;
                        break;
                    default:
                        e.Handled = false;
                        break;
                }
            }
            base.OnKeyDown(e);
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (initialized)
            {
                normal.RemoveFilterIndex();

                if (controller != null)
                {
                    if (this.WindowState == FormWindowState.Minimized)
                    {
                        if (Setting.UseMiniPlayer)
                            controller.RequestAction(QActionType.ShowMiniPlayer);
                    }
                    else
                    {
                        SetView(controller.CurrentView, false);
                        controller.RequestAction(QActionType.HideMiniPlayer);
                    }
                }
            }
        }
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            normal.FocusPanel();
            
        }
        public void UpdateForPanel()
        {
            mnuMain.Enabled = !normal.HasPanel;
            KeyPreview = !normal.HasPanel;
        }
        internal void SetView(ViewType ViewType, bool HideMousePointer)
        {
            if (settingView)
                return;

            settingView = true;

            if (HideMousePointer)
            {
                Cursor.Position = this.PointToScreen(new Point(this.ClientRectangle.Width + 10, this.ClientRectangle.Height / 2));
            }

            bool noBorder = Lib.FullScreen || forceNoBorder;
            bool noMenu = (ViewType == ViewType.Spectrum || ViewType == ViewType.Artwork || ViewType == ViewType.AlbumDetails || ViewType == ViewType.TagCloud);

            this.FormBorderStyle = noBorder ? FormBorderStyle.None : FormBorderStyle.Sizable;

            this.mnuMain.Visible = !noMenu;

            /*

            radio.Visible = false;
            normal.Visible = false;

            switch (View)
            {
                case ViewType.Normal:
                    spectrumView.Visible = false;
                    artwork.Visible = false;
                    equalizer.Visible = false;
                    albumDetails.Visible = false;
                    tagCloud.Visible = false;
                    break;
                case ViewType.Spectrum:
                    spectrumView.Visible = true;
                    artwork.Visible = false;
                    equalizer.Visible = false;
                    albumDetails.Visible = false;
                    tagCloud.Visible = false;
                    break;
                case ViewType.Artwork:
                    spectrumView.Visible = false;
                    artwork.Visible = true;
                    equalizer.Visible = false;
                    albumDetails.Visible = false;
                    tagCloud.Visible = false;
                    break;
                case ViewType.Equalizer:
                    spectrumView.Visible = false;
                    artwork.Visible = false;
                    equalizer.Visible = true;
                    albumDetails.Visible = false;
                    tagCloud.Visible = false;
                    break;
                case ViewType.AlbumDetails:
                    spectrumView.Visible = false;
                    artwork.Visible = false;
                    equalizer.Visible = false;
                    albumDetails.Visible = true;
                    tagCloud.Visible = false;
                    break;
                case ViewType.TagCloud:
                    spectrumView.Visible = false;
                    artwork.Visible = false;
                    equalizer.Visible = false;
                    albumDetails.Visible = false;
                    tagCloud.Visible = true;
                    break;
                case ViewType.Radio:
                    spectrumView.Visible = false;
                    artwork.Visible = false;
                    equalizer.Visible = false;
                    albumDetails.Visible = false;
                    tagCloud.Visible = false;
                    break;
            }
            */
            Rectangle rr = this.ClientRectangle;
            
            controlPanel.Bounds = new Rectangle(rr.X, rr.Height - controlPanel.Height, rr.Width, controlPanel.Height);
            progressBar.Bounds = new Rectangle(rr.X, controlPanel.Top - progressBar.Height, rr.Width, progressBar.Height);
            trackDisplay.Bounds = new Rectangle(rr.X, progressBar.Top - trackDisplay.Height, rr.Width, trackDisplay.Height);

            Rectangle rrr = getMainArea(noMenu);

            switch (ViewType)
            {
                case ViewType.Normal:
                    switch (this.WindowState)
                    {
                        case FormWindowState.Normal:
                            normal.Bounds = new Rectangle(rr.X, mnuMain.Bottom, rr.Width - 1, trackDisplay.Top - mnuMain.Bottom);
                            break;
                        case FormWindowState.Maximized:
                            normal.Bounds = new Rectangle(rr.X, mnuMain.Bottom, rr.Width, trackDisplay.Top - mnuMain.Bottom);
                            break;
                    }
                    tagCloud.Bounds = getMainArea(true);
              //      normal.Visible = true;
                    break;
                case ViewType.Spectrum:
                    spectrumView.Bounds = rrr;
                    tagCloud.Bounds = rrr;
                    break;
                case ViewType.Artwork:
                    artwork.Bounds = rrr;
                    break;
                case ViewType.Equalizer:
                    equalizer.Bounds = rrr;
                    break;
                case ViewType.AlbumDetails:
                    albumDetails.Bounds = rrr;
                    break;
                case ViewType.TagCloud:
                    tagCloud.Bounds = rrr;                    
                    break;
                case ViewType.Radio:
                    radio.Bounds = rrr;
                    break;
                case ViewType.Podcast:
                    podCastManager.Bounds = rrr;
                    break;
            }

            foreach (IMainView mv in views)
                mv.Visible = mv.ViewType == ViewType;

            settingView = false;
        }
        public Rectangle getMainArea(bool MenuHidden)
        {
            Rectangle rr = this.ClientRectangle;

            if (MenuHidden)
                return new Rectangle(rr.X, rr.Y, rr.Width, trackDisplay.Top);
            else
                return new Rectangle(rr.X, mnuMain.Bottom, rr.Width, trackDisplay.Top - mnuMain.Bottom);
        }
        private void initalizeController()
        {
            controller = new Controller(this,
                                        normal,
                                        controlPanel,
                                        trackDisplay,
                                        progressBar,
                                        artwork,
                                        equalizer,
                                        albumDetails,
                                        spectrumView,
                                        tagCloud,
                                        radio,
                                        podCastManager);
        }
        private bool closed = false;
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (podCastManager.DownloadInProgress && e.CloseReason != CloseReason.WindowsShutDown)
            {
                List<frmTaskDialog.Option> options = new List<frmTaskDialog.Option>();
                options.Add(new frmTaskDialog.Option("Stop Podcast Download", "QuuxPlayer will exit.", 0));
                options.Add(new frmTaskDialog.Option("Continue Podcast Download", "QuuxPlayer will not exit.", 1));
                frmTaskDialog td = new frmTaskDialog("Podcast Download in Progress", "A podcast download is in progress. Do you want to quit anyway?", options);
                td.ShowDialog(this);
                switch (td.ResultIndex)
                {
                    case 1:
                        e.Cancel = true;
                        break;
                }
            }
            
            // mnuMain is disabled if a panel is showing
            if (Locked && mnuMain.Enabled && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
            }
            if (!e.Cancel)
            {
                this.Visible = false;
                
                SingletonApp.Close();
                controller.Close();
                
                Lib.ScreenSaverIsActive = screenSaverWasActive;
                closed = true;
            }
        }
        public bool IsClosed
        {
            get { return closed; }
        }
        private void playRandomAlbumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Lib.DoEvents();
            controller.RequestAction(QActionType.PlayRandomAlbum);
        }
        private void preventPowerEvent()
        {
            if (Setting.DisableScreensaver)
            {
                if (controller.Playing || !Setting.DisableScreensaverOnlyWhenPlaying)
                    Lib.DoFakeKeystroke();
                
                Clock.Update(ref powerEventAlarm, preventPowerEvent, 200000, true); // 3.5 min
            }
        }
    }
}
