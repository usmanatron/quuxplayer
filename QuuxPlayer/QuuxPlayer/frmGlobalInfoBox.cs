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
    internal sealed class frmGlobalInfoBox : frmFloatingWindow
    {
        public enum ActionType { None, PlayPause, Stop, Next, Previous, StartOfNextTrack, VolumeUp, VolumeDown, Info }

        private static Controller controller;
        private Track track;
        private Artwork art = null;
        private ActionType action;
        private static TextFormatFlags tff = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
        private int trackNumOffset;
        
        private bool dontClose = false;
        private bool alive = true;
        
        private ulong timer = Clock.NULL_ALARM;

        private string line2;
        private string line3;

        private static frmGlobalInfoBox instance = null;

        private const int HEIGHT = 52;

        public static void Show(Controller Controller, ActionType Action)
        {
            controller = Controller;

            if (instance != null && instance.touch())
            {
                instance.action = Action;
                if (Action == ActionType.VolumeDown || Action == ActionType.VolumeUp)
                    instance.doIt();
                else
                    Clock.DoOnMainThread(instance.doIt, 200);
            }
            else
            {
                instance = new frmGlobalInfoBox(Action);
            }
        }
        private frmGlobalInfoBox(ActionType Action)
        {
            this.Opacity = 0.85;

            action = Action;

            this.Click += (s, e) => { this.click(); };
            this.Cursor = Cursors.Hand;
            
            Clock.DoOnMainThread(doIt, 250);
        }
        private bool touch()
        {
            if (alive && Clock.TimeRemaining(timer) > 700)
            {
                dontClose = true;

                Clock.Update(ref this.timer, close, 5000, false);

                dontClose = false;

                return true;
            }
            else
            {
                return false;
            }
        }
        private void close()
        {
            if (!dontClose)
            {
                alive = false;
                instance = null;
                this.Close();
            }
        }
        private bool Alive
        {
            get { return alive; }
        }
        private void click()
        {
            this.close();
            if (frmMiniPlayer.GetInstance() == null)
                controller.RequestAction(QActionType.MakeForeground);
        }
        private void doIt()
        {
            if ((track = controller.PlayingTrack) != null && (this.Visible || !controller.AppActive) && this.touch())
            {
                int artSize = HEIGHT - MARGIN - MARGIN - 1;

                line2 = track.Title;

                if (track.MainGroup.Length > 0)
                    line3 = track.MainGroup + " / " + Lib.GetTimeString(track.Duration);
                else
                    line3 = Lib.GetTimeString(track.Duration);

                if (track.YearString.Length > 0)
                    line3 += " / " + track.YearString;

                int w = TextRenderer.MeasureText(line2, Styles.Font).Width - 4;

                if (track.TrackNumString.Length > 0)
                {
                    trackNumOffset = TextRenderer.MeasureText(track.TrackNumString, Styles.FontSmall).Width;
                    w += trackNumOffset;
                }

                w = Math.Max(w, TextRenderer.MeasureText(line3, Styles.FontItalic).Width);
                w += MARGIN * 4 + artSize;
                w = Math.Max(200, w);
                w = Math.Min(350, w);

                Rectangle r = new Rectangle(Screen.PrimaryScreen.WorkingArea.Right - w,
                                            Screen.PrimaryScreen.WorkingArea.Height - HEIGHT,
                                            w,
                                            HEIGHT);

                frmMiniPlayer mp;
                if ((mp = frmMiniPlayer.GetInstance()) != null)
                {
                    if (r.IntersectsWith(mp.Bounds))
                        r = new Rectangle(r.X, mp.Top - HEIGHT, r.Width, r.Height);
                }

                this.Bounds = r;

                if (art == null)
                {
                    art = new Artwork();
                    art.Bounds = new Rectangle(MARGIN, MARGIN, artSize, artSize);
                    art.Click += (s, e) => { this.click(); };
                    this.Controls.Add(art);
                }

                art.CurrentTrack = track;

                this.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 5, 5));

                if (this.Visible)
                    this.Invalidate();
                else
                    this.Show();

                this.BringToFront();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Suppress");
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            string act = String.Empty;
            switch (action)
            {
                case ActionType.Stop:
                    act = "Stop";
                    break;
                case ActionType.PlayPause:
                    if (controller.Paused)
                        act = "Pause";
                    else
                        act = "Play";
                    break;
                case ActionType.StartOfNextTrack:
                    act = "Next Track";
                    break;
                case ActionType.Next:
                    act = "Play Next Track";
                    break;
                case ActionType.Previous:
                    act = "Play Previous Track";
                    break;
                case ActionType.VolumeUp:
                    act = "Volume Up: " + WinAudioLib.VolumeDBString;
                    break;
                case ActionType.VolumeDown:
                    act = "Volume Down: " + WinAudioLib.VolumeDBString;
                    break;
                case ActionType.Info:
                    act = "Now Playing";
                    break;
                default:
                    break;
            }

            Rectangle r1 = new Rectangle(art.Right + MARGIN, 2, this.Width - art.Right - MARGIN - MARGIN, 20);
            TextRenderer.DrawText(e.Graphics, act, Styles.FontBold, r1, Styles.LightText, tff);

            if (controller.PlayingTrack != null)
            {
                Rectangle r3 = new Rectangle(r1.Left, 32, r1.Width, 20);

                if (track.TrackNumString.Length > 0)
                {
                    Rectangle r2 = new Rectangle(r1.Left, 20, r1.Width, 20);
                    TextRenderer.DrawText(e.Graphics, track.TrackNumString, Styles.FontSmall, r2, Styles.LightText, tff);
                    r2.Location = new Point(r2.Left + trackNumOffset, 17);
                    TextRenderer.DrawText(e.Graphics, line2, Styles.Font, r2, Styles.LightText, tff);
                }
                else
                {
                    Rectangle r2 = new Rectangle(r1.Left, 17, r1.Width, 20);
                    TextRenderer.DrawText(e.Graphics, line2, Styles.Font, r2, Styles.LightText, tff);
                }
                TextRenderer.DrawText(e.Graphics, line3, Styles.FontItalic, r3, Styles.LightText, tff);
            }
        }
    }
}
