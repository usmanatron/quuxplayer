/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal sealed class TrackDisplay : Control
    {
        private Track currentTrack = null;
        private String temporaryMessage = String.Empty;
        private bool priorityMessage = false;
        private static TextFormatFlags tff = TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
        private ulong temporaryDisplayRevert = Clock.NULL_ALARM;
        private Brush backgroundBrush;

        public TrackDisplay()
        {
            this.DoubleBuffered = true;
            this.Height = 30;
            backgroundBrush = Style.GetTrackDisplayBackgroundBrush(this.Height);
        }
        public Track CurrentTrack
        {
            get { return currentTrack; }
            set
            {
                currentTrack = value;
                this.Invalidate();
            }
        }
        public void ShowPriorityMessage(string Message)
        {
            priorityMessage = true;
            temporaryMessage = Message;
            Clock.Update(ref temporaryDisplayRevert, revertTemporaryMessage, 5000, false);
            this.Invalidate();
        }
        public void ShowMessageUntilReplaced(string Message)
        {
            if (!priorityMessage)
            {
                temporaryMessage = Message;
                Clock.RemoveAlarm(ref temporaryDisplayRevert);
                this.Invalidate();
            }
        }
        public string TemporaryMessage
        {
            get { return temporaryMessage; }
            set
            {
                if (!priorityMessage)
                {
                    temporaryMessage = value;
                    Clock.Update(ref temporaryDisplayRevert, revertTemporaryMessage, 2500, false);
                    this.Invalidate();
                }
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.FillRectangle(backgroundBrush, this.ClientRectangle);

            if (temporaryMessage.Length > 0)
                TextRenderer.DrawText(e.Graphics, temporaryMessage, Styles.FontLarge, this.ClientRectangle, Styles.LightText, tff);
            else if (currentTrack != null)
                TextRenderer.DrawText(e.Graphics, currentTrack.ToString(), Styles.FontLarge, this.ClientRectangle, Styles.VeryLight, tff);

            e.Graphics.DrawLine(Pens.Black, 0, this.ClientRectangle.Height - 1, this.ClientRectangle.Width, this.ClientRectangle.Height - 1);
        }
        
        private void revertTemporaryMessage()
        {
            priorityMessage = false;
            temporaryMessage = String.Empty;
            this.Invalidate();
        }
    }
}
