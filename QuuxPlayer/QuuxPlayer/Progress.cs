/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal sealed class ProgressBar : Control
    {
        public delegate void TrackProgress(float Percentage);
        
        public event TrackProgress SetTrackProgress = null;

        private int totalTime; // msec
        private string totalTimeString;
        private int elapsedTime;
        private const TextFormatFlags tff = TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;
        private Rectangle rect;
        private bool showTime = true;
        private bool hovering = false;
        private bool playing = false;

        private Brush backgroundBrush;
        private Brush backgroundHoverBrush;
        private Brush progressBrush;
        private Brush progressHoverBrush;

        public ProgressBar()
        {
            this.DoubleBuffered = true;
            this.BackColor = Styles.MediumDark;
            this.Height = 23;
            totalTime = 0;
            totalTimeString = String.Empty;
            elapsedTime = 0;

            backgroundBrush = Style.GetProgressBackground(this.Height);
            backgroundHoverBrush = Style.GetProgressHoverBackground(this.Height);
            progressBrush = Style.GetProgressProgress(this.Height);
            progressHoverBrush = Style.GetProgressHoverProgress(this.Height);
        }
        public int ElapsedTime
        {
            get { return elapsedTime; }
            set
            {
                elapsedTime = value;
                this.Invalidate();
            }
        }

        public int TotalTime
        {
            get { return totalTime; }
            set
            {
                if (totalTime != value)
                {
                    totalTime = value;
                    totalTimeString = Lib.GetTimeStringFractional(totalTime);
                    this.Invalidate();
                }
            }
        }
        public bool Locked { get; set; }

        public bool ShowTime
        {
            get { return showTime; }
            set
            {
                showTime = value;
                this.Invalidate();
            }
        }
        public bool Playing
        {
            set
            {
                playing = value;
            }
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            if (!Locked)
            {
                this.Cursor = playing ? Cursors.Hand : Cursors.Default;

                if (playing && !hovering)
                {
                    hovering = true;
                    this.Invalidate();
                }
            }
            base.OnMouseEnter(e);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            if (hovering)
            {
                hovering = false;
                this.Invalidate();
            }
            base.OnMouseLeave(e);
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            rect = new Rectangle(Point.Empty, this.Size);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (hovering && totalTime > 0)
                e.Graphics.Clear(Styles.ProgressBackgroundHover);
            else
                e.Graphics.FillRectangle(backgroundBrush, this.ClientRectangle);

            if (totalTime > 0)
            {
                int total = Math.Min(totalTime, elapsedTime);

                if (total > 0)
                {
                    e.Graphics.FillRectangle(hovering ? progressHoverBrush : progressBrush,
                                             0,
                                             0,
                                             (long)this.ClientRectangle.Width * elapsedTime / TotalTime,
                                             this.ClientRectangle.Height);
                }
                if (showTime)
                {
                    string textToDraw = Lib.GetTimeStringFractional(elapsedTime) + " / " + Lib.GetTimeStringFractional(totalTime - elapsedTime) + " / " + totalTimeString;

                    if (textToDraw.Length > 0)
                    {
                        TextRenderer.DrawText(e.Graphics, textToDraw, Styles.FontProgress, rect, Styles.LightText, tff);
                    }
                }
            }
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (!Locked)
            {
                if (SetTrackProgress != null)
                    SetTrackProgress.Invoke(100f * (float)e.X / (float)this.ClientRectangle.Width);
            }
        }
    }
}
