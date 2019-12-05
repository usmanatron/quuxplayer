/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal sealed class AlbumDetails : Control, IActionHandler, IMainView
    {
        private enum AlbumDetailView { Lyrics, AlbumInfo, ArtistInfo }

        private const int VPADDING_BOTTOM = 5;
        private const int VPADDING_TOP = 25;
        private const int VPADDING_TOP_EXTRA_FOR_TRACK_TITLE = 40;
        private const int HPADDING = 50;

        private static int starsWidth;
        
        static AlbumDetails()
        {
            lyricsProvider = new LyricWiki();
            starsWidth = Styles.BitmapStars.Width;
        }

        private Controller controller;
        private Artwork artwork;
        private QTextArea txtDescription;
        private QButton btnLink;
        private QButton btnPlay;
        private QButton btnNext;
        private QButton btnCopyToClipboard;
        private Rectangle headingRect = Rectangle.Empty;
        private Track currentTrack = null;
        private AlbumDetailView view;
        private Track pendingCurrentTrack = null;
        private static Lyrics lyricsProvider = null;

        private string lyrics = String.Empty;
        private string albumInfo = String.Empty;
        private string artistInfo = String.Empty;
        private string albumURL = String.Empty;
        private string artistURL = String.Empty;

        private DateTime albumReleaseDate = DateTime.MinValue;

        private const TextFormatFlags tff = TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak;

        public AlbumDetails()
        {
            this.BackColor = Color.Black;

            this.Click += (s, e) => { this.RequestAction(QActionType.AdvanceScreen); };

            artwork = new Artwork();
            artwork.Click += (s, e) => { this.RequestAction(QActionType.AdvanceScreen); };
            artwork.HideMousePointer = false;
            this.Controls.Add(artwork);
            
            txtDescription = new QTextArea();
            this.Controls.Add(txtDescription);

            btnLink = new QButton(Localization.Get(UI_Key.Album_Details_View_On_Web), false, false);
            btnLink.BackColor = this.BackColor;
            btnLink.ButtonPressed += new QButton.ButtonDelegate(link_ButtonPressed);
            btnLink.Enabled = false;
            this.Controls.Add(btnLink);

            btnPlay = new QButton(Localization.Get(UI_Key.Album_Details_Play_This_Album), false, false);
            btnPlay.BackColor = this.BackColor;
            btnPlay.ButtonPressed += new QButton.ButtonDelegate(btnLink_ButtonPressed);
            btnPlay.Enabled = true;
            this.Controls.Add(btnPlay);

            btnNext = new QButton(Localization.Get(UI_Key.Album_Details_Next_Screen), false, false);
            btnNext.BackColor = this.BackColor;
            btnNext.ButtonPressed += new QButton.ButtonDelegate(btnNext_ButtonPressed);
            btnNext.Enabled = true;
            this.Controls.Add(btnNext);

            btnCopyToClipboard = new QButton(Localization.Get(UI_Key.Album_Details_Copy_Info_To_Clipboard), false, false);
            btnCopyToClipboard.BackColor = this.BackColor;
            btnCopyToClipboard.ButtonPressed += new QButton.ButtonDelegate(btnCopyToClipboard_ButtonPressed);
            btnCopyToClipboard.Enabled = true;
            this.Controls.Add(btnCopyToClipboard);

            this.view = AlbumDetailView.Lyrics;

            artwork.SendToBack();

            txtDescription.Focus();
        }

        public ViewType ViewType { get { return ViewType.AlbumDetails; } }

        private void btnCopyToClipboard_ButtonPressed(QButton Button)
        {
            if (currentTrack != null)
            {
                string text = currentTrack.Artist +
                              Environment.NewLine +
                              Environment.NewLine +
                              ((view == AlbumDetailView.Lyrics) ? currentTrack.Title : currentTrack.Album) + 
                              Environment.NewLine +
                              Environment.NewLine +
                              txtDescription.Text;

                text = text.Replace(Environment.NewLine, "{{newline}}");
                text = text.Replace("\r", Environment.NewLine);
                text = text.Replace("\n", Environment.NewLine);
                text = text.Replace("{{newline}}", Environment.NewLine);
                Clipboard.SetText(text, TextDataFormat.UnicodeText);
            }
        }
        
        private AlbumDetailView View
        {
            get { return view; }
            set
            {
                if (view != value)
                {
                    view = value;
                    switch (view)
                    {
                        case AlbumDetailView.Lyrics:
                            updateLyrics();
                            break;
                        case AlbumDetailView.AlbumInfo:
                            updateAlbum();
                            break;
                        case AlbumDetailView.ArtistInfo:
                            updateArtist();
                            break;
                    }
                    Clock.DoOnMainThread(setMetrics);
                }
            }
        }

        private void btnNext_ButtonPressed(QButton Button)
        {
            this.RequestAction(QActionType.AdvanceScreen);
        }
        private void btnLink_ButtonPressed(QButton Button)
        {
            controller.RequestAction(new QAction(QActionType.PlayThisAlbum, CurrentTrack));
        }
        private void link_ButtonPressed(QButton Button)
        {
            switch (this.View)
            {
                case AlbumDetailView.AlbumInfo:
                case AlbumDetailView.Lyrics:
                    if (albumURL.Length > 0)
                        Net.BrowseTo(albumURL);
                    else
                        Net.BrowseTo(LastFM.GetLastFMAlbumURL(CurrentTrack));
                    break;
                case AlbumDetailView.ArtistInfo:
                    if (artistURL.Length > 0)
                        Net.BrowseTo(artistURL);
                    else
                        Net.BrowseTo(LastFM.GetLastFMArtistURL(CurrentTrack));
                    break;
            }
        }

        public void RequestAction(QAction Action)
        {
            RequestAction(Action.Type);
        }
        public void RequestAction(QActionType Type)
        {
            switch (Type)
            {
                case QActionType.MoveDown:
                case QActionType.MoveTracksDown:
                case QActionType.PageDown:
                    txtDescription.FirstVisibleYPixel += 20;
                    break;
                case QActionType.SelectNextItemGamePadRight:
                    txtDescription.FirstVisibleYPixel += 5;
                    break;
                case QActionType.MoveUp:
                case QActionType.MoveTracksUp:
                case QActionType.PageUp:
                    txtDescription.FirstVisibleYPixel -= 20;
                    break;
                case QActionType.SelectPreviousItemGamePadRight:
                    txtDescription.FirstVisibleYPixel -= 5;
                    break;
                case QActionType.SelectNextItemGamePadLeft:
                case QActionType.SelectPreviousItemGamePadLeft:
                case QActionType.ReleaseAllFilters:
                case QActionType.ReleaseCurrentFilter:
                case QActionType.NextFilter:
                case QActionType.PreviousFilter:
                    // suppress
                    break;
                case QActionType.HTPCMode:
                    controller.RequestActionNoRedirect(QActionType.HTPCMode);
                    setViewMode();
                    break;
                case QActionType.AdvanceScreen:
                case QActionType.AdvanceScreenWithoutMouse:
                case QActionType.ShowTrackAndAlbumDetails:
                    if (currentTrack == null)
                    {
                        // no reason to stay here
                        controller.RequestActionNoRedirect(QActionType.AdvanceScreen);

                    }
                    else
                    {
                        switch (View)
                        {
                            case AlbumDetailView.Lyrics:
                                if (currentTrack.Album.Length == 0)
                                    this.View = AlbumDetailView.ArtistInfo;
                                else
                                    this.View = AlbumDetailView.AlbumInfo;
                                break;
                            case AlbumDetailView.AlbumInfo:
                                this.View = AlbumDetailView.ArtistInfo;
                                break;
                            case AlbumDetailView.ArtistInfo:
                                this.View = AlbumDetailView.Lyrics; // for when we come back
                                controller.RequestActionNoRedirect(QActionType.AdvanceScreen);
                                if (currentTrack != null)
                                {
                                    Clock.DoOnNewThread(updateLyrics, 30);
                                }
                                break;
                        }
                    }
                    break;
                default:
                    controller.RequestActionNoRedirect(Type);
                    break;
            }
        }

        public Controller Controller
        {
            set { this.controller = value; }
        }
        public ActionHandlerType Type
        { get { return ActionHandlerType.AlbumDetails; } }
        
        public Track CurrentTrack
        {
            get { return pendingCurrentTrack; }
            set
            {
                // decouple for thread safety

                pendingCurrentTrack = value;
                this.View = AlbumDetailView.Lyrics;
                if (pendingCurrentTrack != currentTrack)
                {
                    Clock.DoOnNewThread(makePendingTrackCurrent);
                }
                else if (currentTrack != null)
                {
                    Clock.DoOnNewThread(updateLyrics);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int ypos = VPADDING_TOP;

            if (currentTrack != null)
            {
                TextRenderer.DrawText(e.Graphics, currentTrack.Album, Styles.FontHeading, headingRect, Styles.Light, tff);
                ypos += TextRenderer.MeasureText(e.Graphics, currentTrack.Album, Styles.FontHeading, headingRect.Size, tff).Height + 10;
                TextRenderer.DrawText(e.Graphics, currentTrack.Artist, Styles.FontSubHeading, new Point(HPADDING, ypos), Styles.LightText, tff);
                ypos += TextRenderer.MeasureText(e.Graphics, currentTrack.Artist, Styles.FontSubSubHeading, headingRect.Size, tff).Height + 10;
            }

            string s = String.Empty;
            
            if (albumReleaseDate > DateTime.MinValue)
            {
                if (s.Length > 0)
                    s += " / ";
                s += albumReleaseDate.ToString("yyyy");
            }

            if (s.Length > 0)
            {
                TextRenderer.DrawText(e.Graphics, s, Styles.FontSubSubHeading, new Point(HPADDING, ypos), Styles.LightText, tff);
                ypos += TextRenderer.MeasureText(e.Graphics, s, Styles.FontSubSubHeading, headingRect.Size, tff).Height + 10;
            }

            if (currentTrack != null)
            {
                string heading = String.Empty;
                switch (View)
                {
                    case AlbumDetailView.Lyrics:
                        heading = currentTrack.Title;
                        break;
                    case AlbumDetailView.AlbumInfo:
                        heading = currentTrack.Album;
                        break;
                    case AlbumDetailView.ArtistInfo:
                        heading = currentTrack.Artist;
                        break;
                }
                TextRenderer.DrawText(e.Graphics, heading, Styles.FontSubSubHeading, new Point(txtDescription.Left, VPADDING_TOP), Styles.LightText, tff);
            }
            btnLink.Enabled = currentTrack != null;
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            setMetrics();
        }

        private void makePendingTrackCurrent()
        {
            setViewMode();

            lyrics = String.Empty;
            albumInfo = String.Empty;
            artistInfo = String.Empty;
            albumURL = String.Empty;
            artistURL = String.Empty;
            albumReleaseDate = DateTime.MinValue;

            currentTrack = pendingCurrentTrack;

            this.View = AlbumDetailView.Lyrics;
            
            if (currentTrack == null)
            {
                artwork.CurrentTrack = null;
                txtDescription.SetTextThreadSafe(String.Empty);
                btnPlay.SetEnabledThreadSafe(false);
                btnLink.SetEnabledThreadSafe(false);
                this.Invalidate();
            }
            else
            {
                if (currentTrack.Cover == null)
                {
                    currentTrack.AllowAlbumCoverDownloadThisTrack = true;
                }
                if (currentTrack.Year > 1900 && currentTrack.Year < 2020)
                {
                    albumReleaseDate = new DateTime(currentTrack.Year, 1, 1);
                }
                artwork.CurrentTrack = currentTrack;

                this.Invalidate();

                txtDescription.SetTextThreadSafe(Environment.NewLine +
                                                 Environment.NewLine +
                                                 Localization.Get(UI_Key.Album_Details_Loading_Lyrics));


                btnLink.SetEnabledThreadSafe(false);
                btnPlay.SetEnabledThreadSafe(currentTrack.Album.Length > 0);

                Clock.DoOnNewThread(initUpdateLyrics);
                Clock.DoOnNewThread(initUpdateAlbum);
                Clock.DoOnNewThread(initUpdateArtist);
            }
        }
        private void setViewMode()
        {
            switch (controller.HTPCMode)
            {
                case HTPCMode.Normal:
                    txtDescription.SetFontThreadSafe(Styles.Font);
                    //txtDescription.Font = Styles.Font;
                    break;
                case HTPCMode.HTPC:
                    txtDescription.SetFontThreadSafe(Styles.FontLarge);
                    //txtDescription.Font = Styles.FontLarge;
                    break;
            }
        }
        private void initUpdateLyrics()
        {
            lyricsProvider.GetLyrics(CurrentTrack, updateLyrics);
        }
        private void updateLyrics(string Lyrics)
        {
            lyrics = Lyrics;

            /*
            if (lyrics.Length > 0 && lyrics != Net.FAILED_TOKEN)
                lyrics += Environment.NewLine +
                          Environment.NewLine +
                          lyricsProvider.Credits;
            */
            updateLyrics();
        }
        private void updateLyrics()
        {
            if (this.View == AlbumDetailView.Lyrics)
            {
                if (lyrics == Net.FAILED_TOKEN)
                {
                    txtDescription.SetTextThreadSafe(Environment.NewLine +
                                                     Environment.NewLine +
                                                     "Lyrics not found.");
                }
                else if (lyrics.Length > 0)
                {
                    txtDescription.SetTextThreadSafe(lyrics);
                }
                else
                {
                    txtDescription.SetTextThreadSafe(Environment.NewLine +
                                                     Environment.NewLine +
                                                     Localization.Get(UI_Key.Album_Details_Loading_Lyrics));
                }
                this.Invalidate();
            }
        }
        private void initUpdateArtist()
        {
            LastFM.UpdateArtistInfo(CurrentTrack, updateArtist);
        }
        private void updateArtist(string Info, string URL)
        {
            artistInfo = Info;
            artistURL = URL;
            updateArtist();
        }
        private void updateArtist()
        {
            if (this.View == AlbumDetailView.ArtistInfo)
            {
                if (artistInfo == Net.FAILED_TOKEN)
                {
                    txtDescription.SetTextThreadSafe(Environment.NewLine +
                                                     Environment.NewLine + 
                                                     "Artist information not found.");
                }
                else if (artistInfo.Length > 0)
                {
                    txtDescription.SetTextThreadSafe("Artist Details" + Environment.NewLine + Environment.NewLine + artistInfo);
                }
                else
                {
                    txtDescription.SetTextThreadSafe(Environment.NewLine +
                                                     Environment.NewLine +
                                                     "Loading artist details...");
                }
                this.Invalidate();
            }
        }
        private void initUpdateAlbum()
        {
            LastFM.UpdateAlbumInfo(CurrentTrack, updateAlbum);
        }
        
        private void updateAlbum(string Info, DateTime Date, string URL)
        {
            albumInfo = Info;
            albumURL = URL;

            if (albumReleaseDate != Date && albumReleaseDate == DateTime.MinValue)
            {
                albumReleaseDate = Date;
                this.Invalidate();
            }

            updateAlbum();
        }
        private void updateAlbum()
        {
            if (this.View == AlbumDetailView.AlbumInfo)
            {
                if (albumInfo == Net.FAILED_TOKEN)
                {
                    txtDescription.SetTextThreadSafe(Environment.NewLine +
                                                     Environment.NewLine +
                                                     "Album information not found.");
                }
                else if (albumInfo.Length > 0)
                {
                    txtDescription.SetTextThreadSafe("Album Details" + Environment.NewLine + Environment.NewLine + albumInfo);
                }
                else
                {
                    txtDescription.SetTextThreadSafe(Environment.NewLine +
                                                     Environment.NewLine + 
                                                     "Loading album details...");
                }
                this.Invalidate();
            }
        }
        private void setMetrics()
        {
            int w = this.ClientRectangle.Width;
            int h = this.ClientRectangle.Height;

            txtDescription.Location = new Point(Math.Max(HPADDING, (w - HPADDING) * 19 / 30),
                                               VPADDING_TOP + VPADDING_TOP_EXTRA_FOR_TRACK_TITLE);
            txtDescription.Size = new Size(Math.Max(15, w - txtDescription.Location.X - 10),
                                               Math.Max(VPADDING_TOP, h - VPADDING_BOTTOM - VPADDING_BOTTOM - btnLink.Height - 30 - VPADDING_TOP_EXTRA_FOR_TRACK_TITLE));

            headingRect = new Rectangle(HPADDING,
                                        VPADDING_TOP,
                                        Math.Max(HPADDING, w * 2 / 3 - HPADDING - HPADDING),
                                        h);

            btnNext.Location = new Point(txtDescription.Right - btnNext.Width,
                                         Math.Max(VPADDING_TOP, h - VPADDING_BOTTOM - btnLink.Height));

            btnCopyToClipboard.Location = new Point(btnNext.Left - btnCopyToClipboard.Width - 15,
                                                    btnNext.Top);

            btnLink.Location = new Point(btnCopyToClipboard.Left - btnLink.Width - 5,
                                         btnNext.Top);

            btnPlay.Location = new Point(btnLink.Left - btnPlay.Width - 5,
                                         btnNext.Top);

            artwork.Location = new Point(3 * HPADDING + (txtDescription.Left / 5),
                                           Math.Max(VPADDING_TOP, h / 3));

            int artWidth = Math.Max(10, txtDescription.Left - artwork.Left - 10);

            artwork.Size = new Size(artWidth,
                                    Math.Max(10, (artwork.Left + artWidth > btnPlay.Left) ? btnPlay.Top - artwork.Top - 2: h - artwork.Top - VPADDING_BOTTOM ));

            this.Invalidate();

        }
    }
}
