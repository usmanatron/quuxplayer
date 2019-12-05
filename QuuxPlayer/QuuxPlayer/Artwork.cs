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
    internal sealed class Artwork : Control, IMainView
    {
        private ImageItem image;
        private ImageItem temporaryImage;
        private ImageItem overrideImage;

        private Track currentTrack;
        private Track trackCandidate;
        private Track temporaryTrack;
        private Track temporaryTrackCandidate;
        private float aspectRatio = 1.0f;
        private ulong temporaryDisplayRevert = Clock.NULL_ALARM;
        private ulong temporaryCandidatePromote = Clock.NULL_ALARM;
        private ulong candidatePromote = Clock.NULL_ALARM;
        private ulong scrollHintLetterClearTick = Clock.NULL_ALARM;

        private Rectangle imageRect = Rectangle.Empty;
        private Rectangle tempImageRect = Rectangle.Empty;
        private Rectangle overrideImageRect = Rectangle.Empty;

        private static string defaultBlankMessage = Localization.Get(UI_Key.Artwork_No_Cover_Found);

        private Font tempLetterFont = Styles.Font;

        private TextFormatFlags tff = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;

        private char scrollHintLetter = '\0';

        public Artwork()
        {
            this.BackColor = Color.Black;
            this.DoubleBuffered = true;
            this.image = null;
            this.temporaryImage = null;
            this.overrideImage = null;
            this.currentTrack = null;
            this.temporaryTrack = null;
            this.HideMousePointer = true;
            this.BlankMessage = defaultBlankMessage;
            this.OverlayInfo = false;
        }
        public ViewType ViewType { get { return ViewType.Artwork; } }
        public bool HideMousePointer { get; set; }
        public bool HasImage
        {
            get { return image != null; }
        }
        public override void Refresh()
        {
            base.Refresh();
            Track t = currentTrack;
            currentTrack = null;
            CurrentTrack = t;
        }
        public Track CurrentTrack
        {
            get { return currentTrack; }
            set
            {
                if (currentTrack != value)
                {
                    System.Diagnostics.Debug.Assert((currentTrack != null && value == null) ||
                    (currentTrack == null && value != null) ||
                    (currentTrack != null && value != null && currentTrack != value));

                    trackCandidate = value;

                    Clock.DoOnNewThread(promoteCandidatePreload);
                }
                else
                {
                    System.Diagnostics.Debug.Assert(!((currentTrack != null && value == null) ||
                    (currentTrack == null && value != null) ||
                    (currentTrack != null && value != null && currentTrack != value)));
                }
            }
        }
        private void promoteCandidatePreload()
        {
            currentTrack = trackCandidate;

            if (candidatePromote != Clock.NULL_ALARM)
            {
                Clock.RemoveAlarm(ref candidatePromote);
            }
            if (temporaryCandidatePromote != Clock.NULL_ALARM)
            {
                Clock.RemoveAlarm(ref temporaryCandidatePromote);
            }
            temporaryTrack = null;
            TemporaryImage = null;
            
            candidatePromote = Clock.DoOnMainThread(promoteCandidate, 200);
        }
        private void promoteCandidate()
        {
            candidatePromote = Clock.NULL_ALARM;

            if (currentTrack != null && currentTrack.PreLoadImage(callback) == true)
            {
                Image = currentTrack.Cover;
            }
            else
            {
                Image = null;
            }
        }
        public Track TemporaryTrack
        {
            set
            {
                if (scrollHintLetter == '\0')
                {
                    temporaryTrackCandidate = value;
                    Clock.Update(ref temporaryCandidatePromote, promoteTemporaryCandidate, 250, true);
                }
            }
        }
        public bool OverlayInfo
        {
            get;
            set;
        }
        public string BlankMessage
        {
            get;
            set;
        }
        public void ShowLetter(char Letter)
        {
            if (Letter > '\0')
            {
                Clock.Update(ref scrollHintLetterClearTick, clearScrollHintLetter, 1300, false);

                if (scrollHintLetter != Letter)
                {
                    scrollHintLetter = Letter;
                    this.Invalidate();
                }
            }
        }
        private void clearScrollHintLetter()
        {
            scrollHintLetter = '\0';
            scrollHintLetterClearTick = Clock.NULL_ALARM;

            if (currentTrack != null)
            {
                temporaryTrack = null;
                temporaryImage = null;
            }

            this.Invalidate();
        }
        private bool promotingTemporaryCandidate = false;
        private void promoteTemporaryCandidate()
        {
            while (promotingTemporaryCandidate)
                System.Threading.Thread.Sleep(0);

            promotingTemporaryCandidate = true;

            if (temporaryTrackCandidate != null)
            {
                bool? res = temporaryTrackCandidate.PreLoadImage(tempCallback);

                if (res == true)
                {
                    tempCallback(temporaryTrackCandidate);
                }
                else if (res == false)
                {
                    temporaryTrack = null;
                    this.Invalidate();
                }
                else // null
                {
                    temporaryTrackCandidate.PreLoadImage(tempCallback);
                }
            }
            else if (temporaryTrack != null)
            {
                temporaryTrack = null;
                this.Invalidate();
            }
            temporaryCandidatePromote = Clock.NULL_ALARM;
            temporaryTrackCandidate = null;

            promotingTemporaryCandidate = false;
        }
        private void revertTemporaryTrack()
        {
            this.TemporaryTrack = null;
            this.TemporaryImage = null;
            temporaryDisplayRevert = Clock.NULL_ALARM;
        }
        private void tempCallback(Track Track)
        {
            if (Track != null && Track.Cover != null)
            {
                temporaryTrack = Track;

                Clock.Update(ref temporaryDisplayRevert, revertTemporaryTrack, 5000, false);

                TemporaryImage = temporaryTrack.Cover;
            }
            else
            {
                temporaryTrack = null;
                TemporaryImage = null;
            }
        }
        private void callback(Track Track)
        {
            if (Track == null)
                Image = null;
            else if (Track == currentTrack)
                Image = currentTrack.Cover;
        }
        
        public ImageItem Image
        {
            get
            {
                return image;
            }
            private set
            {
                this.image = value;
                setImageRect(this.image, ref imageRect);
            }
        }
        private ImageItem TemporaryImage
        {
            get { return temporaryImage; }
            set
            {
                this.temporaryImage = value;
                setImageRect(temporaryImage, ref tempImageRect);
            }
        }
        public ImageItem OverrideImage
        {
            get { return overrideImage ?? image; }
            set
            {
                overrideImage = value;
                setImageRect(overrideImage, ref overrideImageRect);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            this.aspectRatio = (float)this.Width / (float)this.Height;
            
            setImageRect(image, ref imageRect);
            setImageRect(temporaryImage, ref tempImageRect);
            setImageRect(overrideImage, ref overrideImageRect);

            tempLetterFont = new Font(Styles.FontName, Math.Max(10, this.Height * 6 / 10), FontStyle.Bold);

            if (this.Height < 90)
                this.Font = Styles.FontSmall;
            else
                this.Font = Styles.Font;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            try
            {
                if (this.overrideImage != null)
                {
                    e.Graphics.DrawImage(this.OverrideImage.Image, overrideImageRect);
                    if (OverlayInfo)
                        overlay(e.Graphics, this.OverrideImage, overrideImageRect);
                }
                else if (this.scrollHintLetter != '\0')
                {
                    TextRenderer.DrawText(e.Graphics, scrollHintLetter.ToString(), tempLetterFont, new Rectangle(Point.Empty, this.Size), Styles.LightText, tff);
                }
                else if (this.TemporaryImage != null)
                {
                    e.Graphics.DrawImage(this.TemporaryImage.Image, tempImageRect);
                    if (OverlayInfo)
                        overlay(e.Graphics, this.TemporaryImage, tempImageRect);
                }
                else if (this.image != null)
                {
                    e.Graphics.DrawImage(this.image.Image, imageRect);
                    if (OverlayInfo)
                        overlay(e.Graphics, this.image, imageRect);
                }
                else
                {
                    TextRenderer.DrawText(e.Graphics, BlankMessage, this.Font, new Rectangle(Point.Empty, this.Size), Styles.LightText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
                }
            }
            catch { }
        }
        private void overlay(Graphics e, ImageItem i, Rectangle rr)
        {
            Color cc = Styles.LightText;

            if (i.AspectRatio < 1.15 && i.Image is Bitmap)
            {
                Bitmap bb = i.Image as Bitmap;
                int r = 0;
                int g = 0;
                int b = 0;
                int y = Math.Max(0, i.Image.Height - 7);
                int w = i.Image.Width;
                for (int x = 0; x < w; x++)
                {
                    Color c = bb.GetPixel(x, y);
                    r += c.R;
                    g += c.G;
                    b += c.B;
                }
                r /= w;
                g /= w;
                b /= w;
                if (r + g + b < 0x180)
                    cc = Color.White;
                else
                    cc = Color.Black;
            }

            string s = String.Format("{0} x {1}", i.Image.Width, i.Image.Height);
            TextRenderer.DrawText(e, s, this.Font, rr, cc, TextFormatFlags.Bottom | TextFormatFlags.Right);
        }
        private void setImageRect(ImageItem Image, ref Rectangle Rectangle)
        {
            if (Image != null)
            {
                Size s;
                if (Image.AspectRatio > this.aspectRatio)
                {
                    // constrained by width
                    s = new Size(this.Width, (int)((float)this.Width / Image.AspectRatio));

                    Rectangle = new Rectangle(new Point(0, (this.ClientRectangle.Height - s.Height) / 2), s);
                }
                else
                {
                    // constrained by height
                    s = new Size((int)((float)this.Height * Image.AspectRatio), this.Height);
                    Rectangle = new Rectangle(new Point((this.ClientRectangle.Width - s.Width) / 2, 0), s);
                }
            }
            this.Invalidate();
        }
    }
}
