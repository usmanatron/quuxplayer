/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal delegate void UserAction(QActionType Type);

    internal sealed class ControlPanel : Control
    {
        public bool ShowGainWarning { get; set; }

        private const TextFormatFlags tff = TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;
        private const TextFormatFlags tffr = TextFormatFlags.NoPrefix | TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;
        
        private const int CONTROL_TOP = 8;
        private const int CONTROL_WIDTH = 27;
        private const int WIDE_CONTROL_WIDTH = 43;
        private const int CONTROL_HEIGHT = 27;
        private const int VOLUME_WIDTH = 110;

        private const int NOW_PLAYING_LEFT = 11;
        private const int RADIO_LEFT = 38;
        private const int ADVANCE_LEFT = 67;
        private const int REPEAT_LEFT = 95;
        private const int SHUFFLE_LEFT = 123;
        private const int BACK_LEFT = 151;
        private const int PLAY_LEFT = 194;
        private const int FWD_LEFT = 221;
        private const int STOP_LEFT = 265;
        private const int MUTE_LEFT = 296;
        private const int VOLUME_LEFT = 328;

        private int controlPanelLeft = 0;
        private bool paused = false;
        private bool stopped = true;
        private bool muted = false;
        private bool repeat = false;
        private bool radioView = false;
        private bool nowPlaying = false;
        private float volume = 0;
        private int volumeWidth = 100;

        private bool nowPlayingHover = false;
        private bool radioHover = false;
        private bool stopHover = false;
        private bool repeatHover = false;
        private bool advanceHover = false;
        private bool playHover = false;
        private bool fwdHover = false;
        private bool backHover = false;
        private bool muteHover = false;
        private bool shuffleHover = false;
        private bool volumeHover = false;

        private bool volumeDragging = false;
        private int volumeDragStartXOffset = 0;

        private Rectangle nowPlayingRectangle;
        private Rectangle radioRectangle;
        private Rectangle shuffleRectangle;
        private Rectangle repeatRectangle;
        private Rectangle advanceRectangle;
        private Rectangle stopRectangle;
        private Rectangle playRectangle;
        private Rectangle fwdRectangle;
        private Rectangle backRectangle;
        private Rectangle muteRectangle;
        private Rectangle volumeRectangle;

        private Rectangle statusBar1Rect;
        private Rectangle statusBar2Rect;
        private Rectangle statusBar3Rect;
        private Rectangle statusBar4Rect;
        private Rectangle statusBar1RectNormal;
        private Rectangle statusBar2RectNormal;
        private Rectangle statusBar3RectNormal;
        private Rectangle statusBar4RectNormal;
        private Rectangle statusBar1RectHTPC;
        private Rectangle statusBar2RectHTPC;
        private Rectangle statusBar3RectHTPC;
        private Rectangle statusBar4RectHTPC;

        private Font font = Styles.Font;
        private HTPCMode viewMode = HTPCMode.Normal;

        private Controller controller;

        private QToolTip toolTip;

        private frmMiniPlayer miniPlayer = null;
        private Player player;

        public ControlPanel()
        {
            this.DoubleBuffered = true;

            this.Height = 45;
            this.Locked = false;

            nowPlayingRectangle = new Rectangle(NOW_PLAYING_LEFT, CONTROL_TOP, CONTROL_WIDTH, CONTROL_HEIGHT);
            radioRectangle = new Rectangle(RADIO_LEFT, CONTROL_TOP, CONTROL_WIDTH, CONTROL_HEIGHT);
            shuffleRectangle = new Rectangle(SHUFFLE_LEFT, CONTROL_TOP, CONTROL_WIDTH, CONTROL_HEIGHT);
            repeatRectangle = new Rectangle(REPEAT_LEFT, CONTROL_TOP, CONTROL_WIDTH, CONTROL_HEIGHT);
            advanceRectangle = new Rectangle(ADVANCE_LEFT, CONTROL_TOP, CONTROL_WIDTH, CONTROL_HEIGHT);
            stopRectangle = new Rectangle(STOP_LEFT, CONTROL_TOP, CONTROL_WIDTH, CONTROL_HEIGHT);
            backRectangle = new Rectangle(BACK_LEFT, CONTROL_TOP, WIDE_CONTROL_WIDTH, CONTROL_HEIGHT);
            playRectangle = new Rectangle(PLAY_LEFT, CONTROL_TOP, CONTROL_WIDTH, CONTROL_HEIGHT);
            fwdRectangle = new Rectangle(FWD_LEFT, CONTROL_TOP, WIDE_CONTROL_WIDTH, CONTROL_HEIGHT);
            muteRectangle = new Rectangle(MUTE_LEFT, CONTROL_TOP, CONTROL_WIDTH, CONTROL_HEIGHT);
            volumeRectangle = new Rectangle(VOLUME_LEFT, CONTROL_TOP, VOLUME_WIDTH, CONTROL_HEIGHT);

            toolTip = new QToolTip(this, String.Empty);
        }
        public Player Player
        {
            set { player = value; }
        }
        public Controller Controller
        {
            set { controller = value; }
        }
        public HTPCMode HTPCMode
        {
            set
            {
                if (this.viewMode != value)
                {
                    this.viewMode = value;
                    font = (this.viewMode == HTPCMode.HTPC) ? Styles.FontControlPanelHTPC : Styles.Font;
                    if (this.viewMode == HTPCMode.Normal)
                    {
                        statusBar1Rect = statusBar1RectNormal;
                        statusBar2Rect = statusBar2RectNormal;
                    }
                    else
                    {
                        statusBar1Rect = statusBar1RectHTPC;
                        statusBar2Rect = statusBar2RectHTPC;
                    }
                    this.Invalidate();
                }
            }
        }
        public bool Locked { get; set; }

        public bool RadioView
        {
            set
            {
                if (radioView != value)
                {
                    radioView = value;
                    this.Invalidate();
                }
            }
            get { return radioView; }
        }
        public bool Paused
        {
            set
            {
                if (paused != value)
                {
                    paused = value;
                    this.Invalidate();
                }
                if (miniPlayer != null)
                {
                    miniPlayer.Paused = value;
                }
            }
            get { return paused; }
        }
        public bool Stopped
        {
            set
            {
                if (stopped != value)
                {
                    stopped = value;
                    
                    if (value == true)
                        paused = false;


                    this.Invalidate();
                }
                if (miniPlayer != null)
                    miniPlayer.Stopped = value;
            }
            get { return stopped; }
        }
        public bool Mute
        {
            set
            {
                if (muted != value)
                {
                    muted = value;
                    this.Invalidate();
                }
                if (miniPlayer != null)
                    miniPlayer.Muted = value;
            }
            get { return muted; }
        }
        public float Volume
        {
            set
            {
                volume = value;
                setVolumeWidth();
                this.Invalidate();
            }
            get
            {
                return volume;
            }
        }
        public bool Repeat
        {
            get { return repeat; }
            set
            {
                repeat = value;
                this.Invalidate();
            }
        }
        public bool NowPlaying
        {
            get { return nowPlaying;  }
            set
            {
                if (nowPlaying != value)
                {
                    nowPlaying = value;
                    this.Invalidate();
                }
            }
        }

        public string TrackCountStatus
        {
            set
            {
                trackCountStatus = value;
                this.Invalidate();
            }
        }
        public string GainStatus
        {
            get { return gainStatus; }
            set
            {
                gainStatus = value;
                this.Invalidate();
            }
        }
        public string NextUpStatus
        {
            set
            {
                upNextStatus = value;
                this.Invalidate();
            }
        }
        public string VolumeStatus
        {
            set
            {
                volumeStatus = value;
                this.Invalidate();
            }
        }

        public void Subscribe(frmMiniPlayer MiniPlayer)
        {
            miniPlayer = MiniPlayer;
            miniPlayer.Stopped = stopped;
            miniPlayer.Muted = muted;
        }
        public void Unsubscribe()
        {
            miniPlayer = null;
        }

        private bool NowPlayingHover
        {
            set
            {
                if (nowPlayingHover != value)
                {
                    nowPlayingHover = value;
                    this.Invalidate();
                    if (value)
                        setToolTip(value, Localization.Get(NowPlaying ? UI_Key.ToolTip_From_Now_Playing : UI_Key.ToolTip_To_Now_Playing));
                }
            }
            get { return nowPlayingHover; }
        }
        private bool RadioHover
        {
            set
            {
                if (radioHover != value)
                {
                    radioHover = value;
                    this.Invalidate();
                    if (value)
                        setToolTip(value, Localization.Get(UI_Key.ToolTip_Radio));
                }
            }
            get { return radioHover; }
        }
        private bool StopHover
        {
            set
            {
                if (stopHover != value)
                {
                    stopHover = value;
                    this.Invalidate();
                    if (value)
                        setToolTip(value, Localization.Get(UI_Key.ToolTip_Stop));
                }
            }
            get { return stopHover; }
        }
        private bool PlayHover
        {
            set
            {
                if (playHover != value)
                {
                    playHover = value;
                    this.Invalidate();

                    if (value)
                    {
                        if (!Stopped && !Paused)
                            setToolTip(value, Localization.Get(UI_Key.ToolTip_Pause));
                        else
                            setToolTip(value, Localization.Get(UI_Key.ToolTip_Play));
                    }
                }
            }
            get { return playHover; }
        }
        private bool FwdHover
        {
            set
            {
                if (fwdHover != value)
                {
                    fwdHover = value;
                    this.Invalidate();
                    if (value)
                        setToolTip(value, Localization.Get(UI_Key.ToolTip_Next_Track));

                }
            }
            get { return fwdHover; }
        }
        private bool BackHover
        {
            set
            {
                if (backHover != value)
                {
                    backHover = value;
                    this.Invalidate();
                    if (value)
                        setToolTip(value, Localization.Get(UI_Key.ToolTip_Previous_Track));
                }
            }
            get { return backHover; }
        }
        private bool MuteHover
        {
            set
            {
                if (muteHover != value)
                {
                    muteHover = value;
                    this.Invalidate();
                    if (value)
                        setToolTip(value, Localization.Get(UI_Key.ToolTip_Mute));
                }
            }
            get { return muteHover; }
        }
        private bool RepeatHover
        {
            set
            {
                if (repeatHover != value)
                {
                    repeatHover = value;
                    this.Invalidate();
                    if (value)
                        setToolTip(value, Localization.Get(UI_Key.ToolTip_Repeat));
                }
            }
            get { return repeatHover; }
        }
        private bool AdvanceHover
        {
            set
            {
                if (advanceHover != value)
                {
                    advanceHover = value;
                    this.Invalidate();
                    if (value)
                        setToolTip(value, Localization.Get(UI_Key.ToolTip_Advance_View));
                }
            }
            get { return advanceHover; }
        }
        private bool ShuffleHover
        {
            set
            {
                if (shuffleHover != value)
                {
                    shuffleHover = value;
                    this.Invalidate();
                    if (value)
                        setToolTip(value, Localization.Get(UI_Key.ToolTip_Shuffle));
                }
            }
            get { return shuffleHover; }
        }
        private bool VolumeHover
        {
            set
            {
                if (volumeHover != value)
                {
                    volumeHover = value;
                    this.Invalidate();
                    if (value)
                        setToolTip(value, Localization.Get(UI_Key.ToolTip_Volume));
                }
            }
            get { return volumeHover; }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (!Locked)
            {
                Point p = new Point(e.X - controlPanelLeft, e.Y);

                int volLeft = volumeRectangle.Left + volumeWidth;

                if (new Rectangle(volLeft, CONTROL_TOP, 12, CONTROL_HEIGHT).Contains(p))
                {
                    volumeDragging = true;
                    volumeDragStartXOffset = p.X - volLeft;
                }
            }
            base.OnMouseDown(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (!Locked)
            {
                Point p = new Point(e.X - controlPanelLeft, e.Y);

                if (volumeDragging)
                {
                    userSetVolume(getVolumeFromPosn(p.X - volumeRectangle.Left - volumeDragStartXOffset));
                    volumeDragging = false;
                }
                else
                {
                    if (nowPlayingRectangle.Contains(p))
                    {
                        controller.RequestAction(QActionType.ViewNowPlaying);
                    }
                    else if (radioRectangle.Contains(p))
                    {
                        controller.RequestAction(QActionType.ToggleRadioMode);
                    }
                    else if (advanceRectangle.Contains(p))
                    {
                        controller.RequestAction(QActionType.AdvanceScreen);
                    }
                    else if (stopRectangle.Contains(p))
                    {
                        if (!stopped)
                            controller.RequestAction(QActionType.Stop);
                    }
                    else if (playRectangle.Contains(p))
                    {
                        if (stopped)
                            controller.RequestAction(QActionType.Play);
                        else
                            controller.RequestAction(QActionType.Pause);
                    }
                    else if (fwdRectangle.Contains(p))
                    {
                        if (!stopped)
                            controller.RequestAction(QActionType.Next);
                    }
                    else if (backRectangle.Contains(p))
                    {
                        if (!stopped)
                            controller.RequestAction(QActionType.Previous);
                    }
                    else if (muteRectangle.Contains(p))
                    {
                        controller.RequestAction(QActionType.Mute);
                    }
                    else if (shuffleRectangle.Contains(p))
                    {
                        controller.RequestAction(QActionType.Shuffle);
                    }
                    else if (repeatRectangle.Contains(p))
                    {
                        controller.RequestAction(QActionType.RepeatToggle);
                    }
                    else if (volumeRectangle.Contains(p))
                    {
                        userSetVolume(getVolumeFromPosn(p.X - 6 - volumeRectangle.Left));
                        controller.RequestAction(QActionType.SetVolume);
                    }
                }
            }
            base.OnMouseUp(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!Locked)
            {
                Point p = new Point(e.X - controlPanelLeft, e.Y);

                if (volumeDragging)
                {
                    userSetVolume(getVolumeFromPosn(p.X - volumeRectangle.Left - volumeDragStartXOffset));
                    NowPlayingHover = false;
                    RadioHover = false;
                    ShuffleHover = false;
                    RepeatHover = false;
                    AdvanceHover = false;
                    StopHover = false;
                    PlayHover = false;
                    FwdHover = false;
                    BackHover = false;
                    MuteHover = false;
                    VolumeHover = true;
                }
                else
                {
                    NowPlayingHover = nowPlayingRectangle.Contains(p);
                    RadioHover = radioRectangle.Contains(p);
                    ShuffleHover = shuffleRectangle.Contains(p);
                    RepeatHover = repeatRectangle.Contains(p);
                    AdvanceHover = advanceRectangle.Contains(p);
                    StopHover = stopRectangle.Contains(p);
                    PlayHover = playRectangle.Contains(p);
                    FwdHover = fwdRectangle.Contains(p);
                    BackHover = backRectangle.Contains(p);
                    MuteHover = muteRectangle.Contains(p);
                    VolumeHover = volumeRectangle.Contains(p);
                }
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            NowPlayingHover = false;
            RadioHover = false;
            ShuffleHover = false;
            RepeatHover = false;
            AdvanceHover = false;
            StopHover = false;
            PlayHover = false;
            FwdHover = false;
            BackHover = false;
            MuteHover = false;

            setToolTip(false, String.Empty);
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // background
            e.Graphics.DrawImageUnscaled(Styles.cpl_background, Point.Empty);

            TextRenderer.DrawText(e.Graphics, upNextStatus, font, statusBar1Rect, Styles.LightText, tff);
            TextRenderer.DrawText(e.Graphics, gainStatus, font, statusBar3Rect, ShowGainWarning ? Styles.WarningText : Styles.LightText, tffr);
            TextRenderer.DrawText(e.Graphics, trackCountStatus, font, statusBar2Rect, Styles.LightText, tff);
            TextRenderer.DrawText(e.Graphics, volumeStatus, font, statusBar4Rect, Styles.LightText, tffr);

            e.Graphics.DrawImageUnscaled(Styles.cpl_center, controlPanelLeft, CONTROL_TOP - 2);

            // volume slider
            
            e.Graphics.DrawImageUnscaledAndClipped(Styles.cpl_volume_slider, new Rectangle(controlPanelLeft + VOLUME_LEFT + 4, CONTROL_TOP + 12, volumeWidth, 3));

            if (VolumeHover)
                e.Graphics.DrawImageUnscaled(Styles.cpl_volume_ball_highlighted,
                                             new Point(controlPanelLeft + VOLUME_LEFT + volumeWidth, 14 + 1));
            else
                e.Graphics.DrawImageUnscaled(Styles.cpl_volume_ball,
                                             new Point(controlPanelLeft + VOLUME_LEFT + volumeWidth, 14 + 1));
            
            if (AdvanceHover)
                e.Graphics.DrawImageUnscaled(Styles.cpl_advance_highlighted, controlPanelLeft + ADVANCE_LEFT, CONTROL_TOP);

            if (RadioView)
            {
                if (RadioHover)
                    e.Graphics.DrawImageUnscaled(Styles.BitmapRadioOnHighlighted, controlPanelLeft + RADIO_LEFT, CONTROL_TOP);
                else
                    e.Graphics.DrawImageUnscaled(Styles.BitmapRadioOn, controlPanelLeft + RADIO_LEFT, CONTROL_TOP);
            }
            else if (RadioHover)
            {
                e.Graphics.DrawImageUnscaled(Styles.BitmapRadioHighlighted, controlPanelLeft + RADIO_LEFT, CONTROL_TOP);
            }

            if (player.PlayingRadio)
            {
                e.Graphics.DrawImageUnscaled(Styles.cpl_now_playing_disabled, controlPanelLeft + NOW_PLAYING_LEFT, CONTROL_TOP);
                e.Graphics.DrawImageUnscaled(Styles.cpl_repeat_disabled, controlPanelLeft + REPEAT_LEFT, CONTROL_TOP);
                e.Graphics.DrawImageUnscaled(Styles.cpl_back_disabled, controlPanelLeft + BACK_LEFT, CONTROL_TOP);
                e.Graphics.DrawImageUnscaled(Styles.cpl_fwd_disabled, controlPanelLeft + FWD_LEFT, CONTROL_TOP);
                e.Graphics.DrawImageUnscaled(Styles.cpl_shuffle_disabled, controlPanelLeft + SHUFFLE_LEFT, CONTROL_TOP);
                if (Stopped)
                    e.Graphics.DrawImageUnscaled(Styles.cpl_stop_disabled, controlPanelLeft + STOP_LEFT + 7, CONTROL_TOP + 6);
            }
            else
            {
                if (NowPlaying)
                {
                    if (NowPlayingHover)
                        e.Graphics.DrawImageUnscaled(Styles.cpl_now_playing_on_highlighted, controlPanelLeft + NOW_PLAYING_LEFT, CONTROL_TOP);
                    else
                        e.Graphics.DrawImageUnscaled(Styles.cpl_now_playing_on, controlPanelLeft + NOW_PLAYING_LEFT, CONTROL_TOP);
                }
                else
                {
                    if (NowPlayingHover)
                        e.Graphics.DrawImageUnscaled(Styles.cpl_now_playing_highlighted, controlPanelLeft + NOW_PLAYING_LEFT, CONTROL_TOP);
                }

                // shuffle
                if (ShuffleHover)
                    e.Graphics.DrawImageUnscaled(Styles.cpl_shuffle_highlighted, controlPanelLeft + SHUFFLE_LEFT, CONTROL_TOP);


                // repeat
                if (Repeat)
                {
                    if (RepeatHover)
                        e.Graphics.DrawImageUnscaled(Styles.cpl_repeat_on_highlighted, controlPanelLeft + REPEAT_LEFT, CONTROL_TOP);
                    else
                        e.Graphics.DrawImageUnscaled(Styles.cpl_repeat_on, controlPanelLeft + REPEAT_LEFT, CONTROL_TOP);
                }
                else if (RepeatHover)
                {
                    e.Graphics.DrawImageUnscaled(Styles.cpl_repeat_highlighted, controlPanelLeft + REPEAT_LEFT, CONTROL_TOP);
                }

                // fwd and back
                if (Stopped)
                {
                    e.Graphics.DrawImageUnscaled(Styles.cpl_back_disabled, controlPanelLeft + BACK_LEFT, CONTROL_TOP);
                    e.Graphics.DrawImageUnscaled(Styles.cpl_fwd_disabled, controlPanelLeft + FWD_LEFT, CONTROL_TOP);
                }
                else
                {
                    if (BackHover)
                        e.Graphics.DrawImageUnscaled(Styles.cpl_back_highlighted, controlPanelLeft + BACK_LEFT, CONTROL_TOP);
                    else if (FwdHover)
                        e.Graphics.DrawImageUnscaled(Styles.cpl_fwd_highlighted, controlPanelLeft + FWD_LEFT, CONTROL_TOP);
                }
            }

            // stop status
            if (Stopped)
                e.Graphics.DrawImageUnscaled(Styles.cpl_stop_disabled, controlPanelLeft + STOP_LEFT + 7, CONTROL_TOP + 6);
            else if (StopHover)
                e.Graphics.DrawImageUnscaled(Styles.cpl_stop_highlighted, controlPanelLeft + STOP_LEFT, CONTROL_TOP);

            
            if (!Paused && !Stopped)
            {
                if (PlayHover)
                    e.Graphics.DrawImageUnscaled(Styles.cpl_pause_highlighted, controlPanelLeft + PLAY_LEFT, CONTROL_TOP);
                else
                    e.Graphics.DrawImageUnscaled(Styles.cpl_pause, controlPanelLeft + PLAY_LEFT, CONTROL_TOP);
            }
            else if (PlayHover)
            {
                e.Graphics.DrawImageUnscaled(Styles.cpl_play_highlighted, controlPanelLeft + PLAY_LEFT, CONTROL_TOP);
            }
            if (Mute)
            {
                if (MuteHover)
                    e.Graphics.DrawImageUnscaled(Styles.BitmapControlPanelMuteHighlighted, controlPanelLeft + MUTE_LEFT, CONTROL_TOP);
                else
                    e.Graphics.DrawImageUnscaled(Styles.BitmapControlPanelMute, controlPanelLeft + MUTE_LEFT, CONTROL_TOP);
            }
            else if (MuteHover)
            {
                e.Graphics.DrawImageUnscaled(Styles.BitmapControlPanelNoMuteHighlighted, controlPanelLeft + MUTE_LEFT, CONTROL_TOP);
            }
        }
        
        private const float MIN_VOL_LEVEL = -40f; // dB
        private const float VOL_PIX_WIDTH = 100f;
        private const int VOL_PIX_WIDTH_INT = 100;

        private void setVolumeWidth()
        {
            volumeWidth = Math.Max(1, Math.Min(VOL_PIX_WIDTH_INT, (int)((Math.Max(0f, Volume - MIN_VOL_LEVEL)) * VOL_PIX_WIDTH / (-MIN_VOL_LEVEL))));
        }
        private float getVolumeFromPosn(int Posn)
        {
            Posn = Math.Max(0, Math.Min((int)VOL_PIX_WIDTH, Posn));
            return (-MIN_VOL_LEVEL) / VOL_PIX_WIDTH * (float)Posn + MIN_VOL_LEVEL;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            controlPanelLeft = this.ClientRectangle.Width / 2 - Properties.Resources.cpl_center.Width / 2;

            int controlPanelRight = controlPanelLeft + Properties.Resources.cpl_center.Width;
            
            const int margin = 2;
            const int rectheight = 22;

            statusBar1RectNormal = new Rectangle(margin, 2, controlPanelLeft - margin, rectheight);
            statusBar2RectNormal = new Rectangle(margin, 22, controlPanelLeft - margin, rectheight);
            statusBar3RectNormal = new Rectangle(controlPanelRight + margin - 1, 2, statusBar1RectNormal.Width, rectheight);
            statusBar4RectNormal = new Rectangle(controlPanelRight + margin - 1, 22, statusBar3RectNormal.Width, rectheight);

            statusBar1RectHTPC = new Rectangle(margin, -1, controlPanelLeft - margin, rectheight);
            statusBar2RectHTPC = new Rectangle(margin, 18, controlPanelLeft - margin, rectheight);
            statusBar3RectHTPC = new Rectangle(margin, -1, statusBar3RectNormal.Width, rectheight);
            statusBar4RectHTPC = new Rectangle(margin, 18, statusBar3RectHTPC.Width, rectheight);

            statusBar1Rect = statusBar1RectNormal;
            statusBar2Rect = statusBar2RectNormal;
            statusBar3Rect = statusBar3RectNormal;
            statusBar4Rect = statusBar4RectNormal;

            this.Invalidate();
        }

        private string trackCountStatus = String.Empty;
        private string gainStatus = String.Empty;
        private string upNextStatus = String.Empty;
        private string volumeStatus = String.Empty;
        private void userSetVolume(float Vol)
        {
            if (this.volume != Vol)
            {
                this.Volume = Vol;
                controller.RequestAction(QActionType.SetVolume);
            }
        }
        private void setToolTip(bool value, string Text)
        {
            if (value)
            {
                toolTip.SetToolTip(this, Text);
                toolTip.Active = true;
            }
            else
            {
                toolTip.Active = false;
            }
        }
    }
}
