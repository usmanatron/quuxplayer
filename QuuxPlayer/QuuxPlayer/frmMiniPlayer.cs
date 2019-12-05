/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal class frmMiniPlayer : frmFloatingWindow
    {
        private const int VOL_ADJUST_RATE = 100;
        
        private const double OPAQUE = 1.00;
        private const double TRANSPARENT = 0.72;

        private static frmMiniPlayer instance = null;

        private bool mouseDown = false;
        private bool dragging = false;
        private Point lastPoint = Point.Empty;
        private Point initialPoint = Point.Empty;
        private bool stopped = false;
        private bool muted = false;
        private bool paused = false;
        private int time = 0;
        private int timeSecs = 0;

        private bool showVol = false;
        private ulong volTimer = Clock.NULL_ALARM;

        private ulong volChangeTimer = Clock.NULL_ALARM;

        private ulong showInfoTimer = Clock.NULL_ALARM;

        private Rectangle exitRect;
        private Rectangle backRect;
        private Rectangle playPauseRect;
        private Rectangle nextRect;
        private Rectangle muteRect;
        private Rectangle volUpRect;
        private Rectangle volDownRect;
        private Rectangle timeRect;

        private Controller controller;
        private ControlPanel controlPanel;

        private const int TOP = 3;
        private const int HEIGHT = 22;
        private const int WIDTH = 22;
        private const int EXTRA_WIDTH = 33;

        private const int EXIT = 4;
        private const int BACK = 25;
        private const int PLAY_PAUSE = 58;
        private const int NEXT = 80;
        private const int MUTE = 114;
        private const int VOL = 136;
        private const int TIME = 158;

        private bool exitHover = false;
        private bool backHover = false;
        private bool playPauseHover = false;
        private bool nextHover = false;
        private bool muteHover = false;
        private bool volUpHover = false;
        private bool volDownHover = false;

        private bool volDownMouseDown;
        private bool volUpMouseDown;

        private bool radio;

        private string toolTipText = String.Empty;
        private QToolTip toolTip = null;

        private static string radioString = Localization.Get(UI_Key.Mini_Player_Radio);
        private static string muteString = Localization.Get(UI_Key.Mini_Player_Mute);

        private const TextFormatFlags tff = TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.NoPadding;

        public frmMiniPlayer(ControlPanel ControlPanel, bool Radio)
        {
            this.Cursor = Cursors.Hand;
            this.Opacity = TRANSPARENT;

            this.radio = Radio;

            Rectangle r = new Rectangle(DefaultLocation, Styles.BitmapMiniPlayer.Size);
            r = Lib.MakeVisible(r);

            this.Bounds = r;

            this.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 5, 5));

            controller = Controller.GetInstance();
            
            controlPanel = ControlPanel;
            controlPanel.Subscribe(this);

            exitRect = new Rectangle(EXIT, TOP, WIDTH, HEIGHT);
            backRect = new Rectangle(BACK, TOP, EXTRA_WIDTH, HEIGHT);
            playPauseRect = new Rectangle(PLAY_PAUSE, TOP, HEIGHT, WIDTH);
            nextRect = new Rectangle(NEXT, TOP, EXTRA_WIDTH, HEIGHT);
            muteRect = new Rectangle(MUTE, TOP, WIDTH, HEIGHT);
            volUpRect = new Rectangle(VOL, TOP, WIDTH, HEIGHT / 2);
            volDownRect = new Rectangle(VOL, TOP + HEIGHT / 2, WIDTH, HEIGHT / 2);

            timeRect = new Rectangle(TIME, 0, this.ClientRectangle.Width - TIME - 4, this.ClientRectangle.Height - 2);

            WinAudioLib.VolumeChanged += volUpdate;

            instance = this;
        }

        internal static frmMiniPlayer GetInstance()
        {
            return instance;
        }

        private void volUpdate(object sender, EventArgs e)
        {
            showVol = true;
            Clock.Update(ref volTimer, noVol, 2000, false);
            this.Invalidate(timeRect);
        }
        private void noVol()
        {
            volTimer = Clock.NULL_ALARM;
            showVol = false;
            this.Invalidate(timeRect);
        }

        public static Point DefaultLocation { get; set; }
        public static Point BottomRightDock
        {
            get
            {
                Rectangle r = Screen.PrimaryScreen.WorkingArea;
                return new Point(r.Right - Properties.Resources.mini_player.Width,
                                 r.Bottom - Properties.Resources.mini_player.Height);
            }
        }
        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.DrawImageUnscaled(Styles.BitmapMiniPlayer, Point.Empty);

            if (showVol)
            {
                if (Muted)
                    TextRenderer.DrawText(e.Graphics, muteString, Styles.Font, timeRect, Styles.LightText, tff);
                
                else
                    TextRenderer.DrawText(e.Graphics, WinAudioLib.VolumeDBString, Styles.Font, timeRect, Styles.LightText, tff);
            }
            else
            {
                if (radio)
                    TextRenderer.DrawText(e.Graphics, radioString, Styles.Font, timeRect, Styles.LightText, tff);
                else
                    TextRenderer.DrawText(e.Graphics, Lib.GetTimeString(Time), Styles.Font, timeRect, Styles.LightText, tff);
            }

            if (e.ClipRectangle != timeRect)
            {
                if (ExitHover)
                {
                    if (Keyboard.Shift)
                        e.Graphics.DrawImageUnscaled(Styles.BitmapMiniPlayerExitHighlighted, exitRect.Location);
                    else
                        e.Graphics.DrawImageUnscaled(Styles.BitmapMiniPlayerAdvanceHover, exitRect.Location);
                }
                else if (BackHover)
                {
                    if (!stopped)
                        e.Graphics.DrawImageUnscaled(Styles.BitmapMiniPlayerBackHighlighted, backRect.Location);
                }
                else if (PlayPauseHover)
                {
                    if (Stopped || paused)
                        e.Graphics.DrawImageUnscaled(Styles.BitmapMiniPlayerPlayHighlighted, playPauseRect.Location);
                    else
                        e.Graphics.DrawImageUnscaled(Styles.BitmapMiniPlayerPauseHighlighted, playPauseRect.Location);
                }
                else if (NextHover)
                {
                    if (!Stopped)
                        e.Graphics.DrawImageUnscaled(Styles.BitmapMiniPlayerFwdHighlighted, nextRect.Location);
                }
                else if (MuteHover)
                {
                    if (Muted)
                        e.Graphics.DrawImageUnscaled(Styles.BitmapMiniPlayerMuteOnHighlighted, muteRect.Location);
                    else
                        e.Graphics.DrawImageUnscaled(Styles.BitmapMiniPlayerMuteOffHighlighted, muteRect.Location);
                }
                else if (VolUpHover)
                {
                    e.Graphics.DrawImageUnscaled(Styles.BitmapMiniPlayerVolumeUpHighlighted, volUpRect.Location);
                }
                else if (VolDownHover)
                {
                    e.Graphics.DrawImageUnscaled(Styles.BitmapMiniPlayerVolumeDownHighlighted, volUpRect.Location);
                }
                if (!ExitHover && Keyboard.Shift)
                {
                    e.Graphics.DrawImageUnscaled(Styles.BitmapMiniPlayerExit, exitRect.Location);
                }
                if (Muted && !MuteHover)
                    e.Graphics.DrawImageUnscaled(Styles.BitmapMiniPlayerMuteOn, muteRect.Location);

                if (!Stopped && !Paused && !PlayPauseHover)
                    e.Graphics.DrawImageUnscaled(Styles.BitmapMiniPlayerPause, playPauseRect.Location);

                if (Stopped || controller.RadioMode)
                {
                    e.Graphics.DrawImageUnscaled(Styles.BitmapMiniPlayerBackDisabled, backRect.Location);
                    e.Graphics.DrawImageUnscaled(Styles.BitmapMiniPlayerFwdDisabled, nextRect.Location);
                }
            }
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            Clock.RemoveAlarm(ref volChangeTimer);

            mouseDown = false;

            if (dragging)
            {
                dragging = false;
            }
            else
            {
                if (exitRect.Contains(e.Location))
                {
                    exit();
                }
                else if (backRect.Contains(e.Location))
                {
                    back();
                }
                else if (playPauseRect.Contains(e.Location))
                {
                    playPause();
                }
                else if (nextRect.Contains(e.Location))
                {
                    next();
                }
                else if (muteRect.Contains(e.Location))
                {
                    mute();
                }
                else if (e.Location.X > timeRect.X)
                {
                    if (!this.radio)
                        Clock.Update(ref showInfoTimer, showInfo, 300, false);
                }
            }
        }

        private void showInfo()
        {
            showInfoTimer = Clock.NULL_ALARM;
            frmGlobalInfoBox.Show(controller, frmGlobalInfoBox.ActionType.Info);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            volUpMouseDown = false;
            volDownMouseDown = false;
            mouseDown = false;
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            mouseDown = true;
            lastPoint = e.Location;
            initialPoint = this.Location;

            volUpMouseDown = false;
            volDownMouseDown = false;
            if (volUpRect.Contains(e.Location))
            {
                volUpMouseDown = true;
                controller.RequestAction(QActionType.VolumeUpLarge);
                Clock.Update(ref volChangeTimer, repeatVolChange, VOL_ADJUST_RATE, false);
            }
            else if (volDownRect.Contains(e.Location))
            {
                volDownMouseDown = true;
                controller.RequestAction(QActionType.VolumeDownLarge);
                Clock.Update(ref volChangeTimer, repeatVolChange, VOL_ADJUST_RATE, false);
            }
        }
        private void repeatVolChange()
        {
            if (volDownMouseDown)
            {
                controller.RequestAction(QActionType.VolumeDownLarge);
                Clock.Update(ref volChangeTimer, repeatVolChange, VOL_ADJUST_RATE, false);
            }
            else if (volUpMouseDown)
            {
                controller.RequestAction(QActionType.VolumeUpLarge);
                Clock.Update(ref volChangeTimer, repeatVolChange, VOL_ADJUST_RATE, false);
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (mouseDown && !volUpMouseDown && !volDownMouseDown && (dragging || (distSqr(e.Location, lastPoint) > 12)))
            {
                if (!dragging)
                {
                    dragging = true;
                    //volUpMouseDown = false;
                    //volDownMouseDown = false;
                }
                this.Location = delta(this.Location, e.Location, lastPoint);
                ExitHover = false;
                BackHover = false;
                PlayPauseHover = false;
                NextHover = false;
                MuteHover = false;
                VolUpHover = false;
                VolDownHover = false;
            }
            else
            {
                ExitHover = exitRect.Contains(e.Location);
                BackHover = backRect.Contains(e.Location);
                PlayPauseHover = playPauseRect.Contains(e.Location);
                NextHover = nextRect.Contains(e.Location);
                MuteHover = muteRect.Contains(e.Location);
                VolUpHover = volUpRect.Contains(e.Location);
                VolDownHover = volDownRect.Contains(e.Location);
            }
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);

            this.Opacity = OPAQUE;

            Track t = controller.PlayingTrack;

            if (t != null)
            {
                toolTipText = t.ToShortString();
                toolTip = new QToolTip(this, toolTipText);
                toolTip.SetToolTip(this, toolTipText);
            }
            else
            {
                killToolTip();
            }
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            this.Opacity = TRANSPARENT;

            ExitHover = false;
            BackHover = false;
            PlayPauseHover = false;
            NextHover = false;
            MuteHover = false;
            VolUpHover = false;
            VolDownHover = false;
            killToolTip();
            dragging = false;
            mouseDown = false;
            volDownMouseDown = false;
            volUpMouseDown = false;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case KeyDefs.MiniPlayer:
                    exit();
                    return true;
                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        private void killToolTip()
        {
            if (toolTip != null)
            {
                toolTip.Dispose();
                toolTip = null;
            }
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            this.Invalidate();
        }
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            this.Invalidate();
        }
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            Clock.RemoveAlarm(ref showInfoTimer);
            
            base.OnMouseDoubleClick(e);

            if (!volDownRect.Contains(e.Location) && !volUpRect.Contains(e.Location))
            {
                Point p = BottomRightDock;
                if (this.Location != p)
                {
                    this.Location = p;

                    OnMouseLeave(EventArgs.Empty);
                    if (this.Bounds.Contains(this.PointToScreen(e.Location)))
                        this.Opacity = OPAQUE;
                }
            }
        }

        public bool Stopped
        {
            private get { return stopped; }
            set
            {
                if (stopped != value)
                {
                    stopped = value;
                    this.Invalidate();
                }
            }
        }
        public bool Muted
        {
            private get { return muted; }
            set
            {
                if (muted != value)
                {
                    muted = value;
                    this.Invalidate();
                }
            }
        }
        public bool Paused
        {
            private get { return paused; }
            set
            {
                if (paused != value)
                {
                    paused = value;
                    this.Invalidate();
                }
            }
        }
        public int Time
        {
            private get { return time; }
            set
            {
                time = value;
                TimeInWholeSeconds = ((time + 500) / 1000);
            }
        }
        private int TimeInWholeSeconds
        {
            // only invalidate when the actual second changes
            set
            {
                if (timeSecs != value)
                {
                    timeSecs = value;
                    this.Invalidate(timeRect);
                }
            }
        }

        private bool ExitHover
        {
            get { return exitHover; }
            set
            {
                if (exitHover != value)
                {
                    exitHover = value;
                    this.Invalidate();
                }
            }
        }
        private bool BackHover
        {
            get { return backHover; }
            set
            {
                if (backHover != value)
                {
                    backHover = value;
                    this.Invalidate();
                }
            }
        }
        private bool PlayPauseHover
        {
            get { return playPauseHover; }
            set
            {
                if (playPauseHover != value)
                {
                    playPauseHover = value;
                    this.Invalidate();
                }
            }
        }
        private bool NextHover
        {
            get { return nextHover; }
            set
            {
                if (nextHover != value)
                {
                    nextHover = value;
                    this.Invalidate();
                }
            }
        }
        private bool MuteHover
        {
            get { return muteHover; }
            set
            {
                if (muteHover != value)
                {
                    muteHover = value;
                    this.Invalidate();
                }
            }
        }
        private bool VolUpHover
        {
            get { return volUpHover; }
            set
            {
                if (volUpHover != value)
                {
                    volUpHover = value;
                    this.Invalidate();
                }
            }
        }
        private bool VolDownHover
        {
            get { return volDownHover; }
            set
            {
                if (volDownHover != value)
                {
                    volDownHover = value;
                    this.Invalidate();
                }
            }
        }

        private void exit()
        {
            this.Close();
            if (Keyboard.Shift)
                controller.RequestAction(QActionType.Exit);
            else
                controller.RequestAction(QActionType.HideMiniPlayer);
        }
        private void back()
        {
            if (!stopped)
            {
                frmGlobalInfoBox.Show(controller, frmGlobalInfoBox.ActionType.Previous);
                controller.RequestAction(QActionType.Previous);
            }
        }
        private void playPause()
        {
            frmGlobalInfoBox.Show(controller, frmGlobalInfoBox.ActionType.PlayPause);
            if (stopped)
            {
                controller.RequestAction(QActionType.Play);
            }
            else
            {
                controller.RequestAction(QActionType.Pause);
            }
        }
        private void next()
        {
            if (!stopped)
            {
                frmGlobalInfoBox.Show(controller, frmGlobalInfoBox.ActionType.Next);
                controller.RequestAction(QActionType.Next);
            }
        }
        private void mute()
        {
            controller.RequestAction(QActionType.Mute);
            volUpdate(this, EventArgs.Empty);
        }

        private Point delta(Point Orig, Point Add, Point Sub)
        {
            return new Point(Orig.X + Add.X - Sub.X,
                             Orig.Y + Add.Y - Sub.Y);
        }
        private int distSqr(Point P1, Point P2)
        {
            return (P1.X - P2.X) * (P1.X - P2.X) +
                   (P1.Y - P2.Y) * (P1.Y - P2.Y);
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            WinAudioLib.VolumeChanged -= volUpdate;

            DefaultLocation = this.Location;

            base.OnFormClosing(e);
            controlPanel.Unsubscribe();

            instance = null;
        }
    }
}
