/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal sealed partial class frmFileInfo : QFixedDialog
    {
        private const int ART_SIZE = 150;
        private const int SECTION_SPACING = 7;
        private const int MULTI_LINE_SPACING = 4;
        private const int MIN_LINE_SPACING = 17;
        private const int MAX_LINE_HEIGHT = 100;

        private int secondColPosn;
        private int maxWidth;

        private TextFormatFlags tff = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.WordEllipsis | TextFormatFlags.NoClipping;

        private Track track;
        private Artwork art;

        private QButton btnEdit;

        public bool EditFile { get; set; }

        public frmFileInfo(Track Track) : base(Track.ToShortString(), ButtonCreateType.OKOnly)
        {
            this.ClientSize = new System.Drawing.Size(550, 400);

            SPACING = 4;

            this.KeyPreview = true;

            btnOK.Text = Localization.Get(UI_Key.File_Info_Done);

            track = Track;
            track.Load();

            art = new Artwork();
            art.CurrentTrack = track;
            art.Size = new Size(ART_SIZE, ART_SIZE);
            art.Location = new Point(this.ClientRectangle.Width - ART_SIZE - MARGIN, MARGIN);
            this.Controls.Add(art);

            if (track.ConfirmExists)
            {
                btnEdit = new QButton(Localization.Get(UI_Key.File_Info_Edit), false, false);
                btnEdit.ButtonPressed += (s) => { edit(); };
                this.Controls.Add(btnEdit);
            }

            this.EditFile = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            maxWidth = art.Left - MARGIN;
            
            int ypos = MARGIN;

            secondColPosn = TextRenderer.MeasureText(Localization.Get(UI_Key.File_Info_Num_Channels_Sample_Rate), Styles.Font).Width + 15;

            int w = this.ClientRectangle.Width - MARGIN - MARGIN;

            ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Title), track.Title);

            ypos += SECTION_SPACING;

            ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Artist), track.Artist);
            ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Album), track.Album);

            if (track.AlbumArtist.Length > 0)
                ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Album_Artist), track.AlbumArtist);

            if (track.Compilation)
                ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Compilation), Localization.Get(UI_Key.File_Info_Yes));

            if (track.MainGroup != track.Artist)
                ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Referenced_As), track.MainGroup);

            ypos += SECTION_SPACING;

            ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Length), track.DurationInfoLong);

            ypos += SECTION_SPACING;

            if (track.TrackNum > 0)
                ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Track_Number), track.TrackNumString);
            
            if (track.DiskNum > 0)
                ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Disk_Number), track.DiskNumString);

            if (track.TrackNum > 0 || track.DiskNum > 0)
                ypos += SECTION_SPACING;

            if (track.Genre.Length > 0)
                ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Genre), track.Genre);

            if (track.Grouping.Length > 0)
                ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Grouping), track.Grouping);

            if (track.Composer.Length > 0)
                ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Composer), track.Composer);

            if (track.YearString.Length > 0)
                ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Year), track.YearString);

            if (track.RatingString.Length > 0)
                ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Rating), track.RatingString);

            ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Play_Count), track.PlayCountString);

            ypos += SECTION_SPACING;

            ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_File_Type_File_Size), track.TypeString + " / " + track.FileSizeString);
            ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Bit_Rate_Encoder), (track.BitrateString + " / ") + ((track.Encoder.Length > 0) ? track.Encoder : "Unknown"));

            ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Num_Channels_Sample_Rate), track.NumChannelsString + " / " + track.SampleRateString + " Hz");

            float trg = track.ReplayGainTrack; //AudioStreamFile.GetReplayGain(track, ReplayGain.Track, false);
            float arg = track.ReplayGainAlbum; //AudioStreamFile.GetReplayGain(track, ReplayGain.Album, false);

            if (trg != 0.0f || arg != 0.0f)
            {
                ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Replay_Gain), "Track: " + trg.ToString("0.0") + "dB / Album: " + arg.ToString("0.0") + "dB");
            }
            ypos += SECTION_SPACING;

            ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Last_Played), track.LastPlayedDate.Year > 1980 ? track.LastPlayedDate.ToString() + " (" + (DateTime.Now - track.LastPlayedDate).TotalDays.ToString("0.00") + " Days Ago)" : "Never");
            ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Date_Modified), track.FileDate.ToString());
            ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Date_Added), track.AddDate.ToString());

            if (track.Equalizer != null)
            {
                ypos += SECTION_SPACING;
                ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Equalizer), EqualizerSetting.GetString(track.Equalizer));
            }

            ypos += SECTION_SPACING;

            ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_File_Path), track.FilePath);

            if (!track.ConfirmExists)
                ypos = renderLine(e, ypos, Localization.Get(UI_Key.File_Info_Ghost), Localization.YES);

            this.Height = this.SizeFromClientSize(new Size(this.ClientRectangle.Width, ypos + btnOK.Height + MARGIN + MARGIN)).Height;
            
            if (btnEdit == null)
                PlaceButtons(this.ClientRectangle.Width,
                             this.ClientRectangle.Height - MARGIN - btnOK.Height);
            else
                PlaceButtons(this.ClientRectangle.Width,
                             this.ClientRectangle.Height - MARGIN - btnOK.Height,
                             btnOK,
                             btnEdit);
        }
        private void edit()
        {
            this.EditFile = true;
            this.Close();
        }
        private int renderLine(PaintEventArgs e, int ypos, string Caption, string Value)
        {
            Rectangle r = new Rectangle(MARGIN, ypos, (secondColPosn - MARGIN), MAX_LINE_HEIGHT);
            TextRenderer.DrawText(e.Graphics, Caption, Styles.Font, r, Styles.LightText, tff);
            r = new Rectangle(secondColPosn, ypos, maxWidth - secondColPosn - MARGIN, MAX_LINE_HEIGHT);
            TextRenderer.DrawText(e.Graphics, Value, Styles.FontBold, r, Styles.LightText, tff);
            ypos += Math.Max(MIN_LINE_SPACING, TextRenderer.MeasureText(Value, Styles.FontBold, r.Size, tff).Height + MULTI_LINE_SPACING);
            
            if (ypos > art.Bottom)
                maxWidth = this.ClientRectangle.Width - MARGIN - MARGIN;

            return ypos;
        }
    }
}
