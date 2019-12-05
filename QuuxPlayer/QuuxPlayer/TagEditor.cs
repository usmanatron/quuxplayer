/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal class TagEditor : QPanel
    {
        public enum TagEditAction { OK, Cancel, Apply }
        public delegate void TagEditComplete(TagEditAction TEA);

        private QTextBox txtTitle;
        private QTextBox txtArtist;
        private QTextBox txtAlbum;
        private QTextBox txtAlbumArtist;
        private QComboBox cboGenre;
        private QComboBox cboGrouping;
        private QComboBox cboRename;
        private QTextBox txtComposer;
        private QTextBox txtTrackNum;
        private QTextBox txtDiskNum;
        private QTextBox txtYear;
        private QComboBox cboCompilation;

        private QButton btnPrevious = null;
        private QButton btnNext = null;
        private QButton btnAutoNumber = null;
        private QButton btnLoadArt = null;
        private QButton btnClearArt = null;
        private QButton btnCopyArt = null;
        private QButton btnPasteArt = null;
        private QCheckBox chkArt = null;

        private Dictionary<IEditControl, QLabel> labels;
        private Dictionary<IEditControl, QCheckBox> checkboxes;
        private List<IEditControl> editControls = new List<IEditControl>();

        private Artwork art;

        private bool isMultiple;
        private bool settingUp = false;

        private const string VARIES_TOKEN = "{{~}}";
        private static string MULTIPLE_VALUES = Localization.Get(UI_Key.Edit_Tags_Multiple_Values);

        private List<Track> tracks;

        private const float FIRST_COL_SHORT = 0.58f;
        private const float SECOND_COL_SINGLE = 0.73f;
        private const float SECOND_COL_MULTIPLE = 0.68f;

        private TrackList tracklist;

        private bool dirty = false;
        private bool imageDirty = false;

        private TagEditComplete tec;
        private Track template;

        public TagEditor(TrackList TrackList, List<Track> Tracks, TagEditComplete TEC)
        {
            this.tec = TEC;
            this.MARGIN = 4;

            this.SuspendLayout();

            System.Diagnostics.Debug.Assert(Tracks.Count > 0);

            tracks = Tracks;

            template = new Track(tracks[0]);

            tracklist = TrackList;

            isMultiple = (tracks.Count > 1);

            labels = new Dictionary<IEditControl, QLabel>();
            checkboxes = new Dictionary<IEditControl, QCheckBox>();

            setupTextBox(out txtTitle, Localization.Get(UI_Key.Edit_Tags_Title), getDefaultText(new Func<Track, string>(t => t.Title)), false);
            setupTextBox(out txtArtist, Localization.Get(UI_Key.Edit_Tags_Artist), getDefaultText(new Func<Track, string>(t => t.Artist)), isMultiple);
            setupTextBox(out txtAlbum, Localization.Get(UI_Key.Edit_Tags_Album), getDefaultText(new Func<Track, string>(t => t.Album)), isMultiple);
            setupTextBox(out txtAlbumArtist, Localization.Get(UI_Key.Edit_Tags_Album_Artist), getDefaultText(new Func<Track, string>(t => t.AlbumArtist)), isMultiple);
            setupTextBox(out txtComposer, Localization.Get(UI_Key.Edit_Tags_Composer), getDefaultText(new Func<Track, string>(t => t.Composer)), isMultiple);
            setupComboBox(out cboGrouping, Localization.Get(UI_Key.Edit_Tags_Grouping), getDefaultText(new Func<Track, string>(t => t.Grouping)), Database.GetGroupings().ToArray(), isMultiple);
            setupComboBox(out cboGenre, Localization.Get(UI_Key.Edit_Tags_Genre), getDefaultText(new Func<Track, string>(t => t.GenreAllowBlank)), Database.GetGenres().ToArray(),isMultiple);
            setupRename(Localization.Get(UI_Key.Edit_Tags_Rename), isMultiple);
            setupTextBox(out txtYear, Localization.Get(UI_Key.Edit_Tags_Year), getDefaultText(new Func<Track, string>(t => t.YearString)), isMultiple);
            setupTextBox(out txtTrackNum, Localization.Get(UI_Key.Edit_Tags_Track_Num), getDefaultText(new Func<Track, string>(t => t.TrackNumString)), isMultiple);
            setupTextBox(out txtDiskNum, Localization.Get(UI_Key.Edit_Tags_Disk_Num), getDefaultText(new Func<Track, string>(t => t.DiskNumString)), isMultiple);
            setupCompilation(out cboCompilation, Localization.Get(UI_Key.Edit_Tags_Compilation), isMultiple);

            txtTitle.MaxLength = 100;
            txtArtist.MaxLength = 100;
            txtAlbumArtist.MaxLength = 100;
            txtAlbum.MaxLength = 100;
            cboGenre.MaxLength = 100;
            cboGrouping.MaxLength = 100;
            txtComposer.MaxLength = 100;

            this.Height = (txtTitle.Height + labels[txtTitle].Height) * 4 + MARGIN + MARGIN;

            setupArtwork();

            setupAutoComplete();

            if (isMultiple)
            {
                labels[txtTitle].Enabled = false;
                txtTitle.Enabled = false;
            }

            txtYear.NumericOnly = true;
            txtYear.MaxLength = 4;

            txtTrackNum.NumericOnly = true;
            txtTrackNum.MaxLength = 4;

            txtDiskNum.NumericOnly = true;
            txtDiskNum.MaxLength = 4;

            btnOK = new QButton(Localization.Get(UI_Key.Edit_Tags_Save), false, true);
            btnOK.ShowAccellerator(Keys.S);
            AddButton(btnOK, ok);

            btnCancel = new QButton(Localization.Get(UI_Key.Edit_Tags_Done), false, true);
            AddButton(btnCancel, cancel);

            if (!isMultiple && tracklist.Queue.Count > 1)
            {
                btnPrevious = new QButton(Localization.Get(UI_Key.Edit_Tags_Previous), false, true);
                btnPrevious.ShowAccellerator(Keys.P);
                AddButton(btnPrevious, previous);

                btnNext = new QButton(Localization.Get(UI_Key.Edit_Tags_Next), false, true);
                btnNext.ShowAccellerator(Keys.N);
                AddButton(btnNext, next);

                int i = tracklist.Queue.IndexOf(tracks[0]);

                if (i < 1)
                    btnPrevious.Enabled = false;
                if (i >= tracklist.Queue.Count - 1)
                    btnNext.Enabled = false;
            }
            else if (isMultiple && Tracks.Count <= 200)
            {
                btnAutoNumber = new QButton(Localization.Get(UI_Key.Edit_Tags_AutoNumber), false, true);
                AddButton(btnAutoNumber, autoNumber);
            }

            if (isMultiple)
                foreach (KeyValuePair<IEditControl, QCheckBox> kvp in checkboxes)
                {
                    kvp.Value.Checked = false;
                }

            saveCurrentValues();

            this.ResumeLayout();

            Dirty = false;
           
            setTabIndexes();

            resize();

            if (isMultiple)
                txtArtist.Focus();
            else
                txtTitle.Focus();

            if (!isMultiple)
            {
                txtTitle.TextChanged += (s, e) => { template.Title = txtTitle.Text.Trim(); updateFilenames(cboRename.SelectedIndex); };
                txtArtist.TextChanged += (s, e) => { template.Artist = txtArtist.Text.Trim(); updateFilenames(cboRename.SelectedIndex); };
                txtAlbum.TextChanged += (s, e) => { template.Album = txtAlbum.Text.Trim(); updateFilenames(cboRename.SelectedIndex); };
                txtAlbumArtist.TextChanged += (s, e) => { template.AlbumArtist = txtAlbumArtist.Text.Trim(); updateFilenames(cboRename.SelectedIndex); };
                cboCompilation.SelectedIndexChanged += (s, e) => { template.Compilation = ((cboCompilation.Text == Localization.YES) ? true : false); updateFilenames(cboRename.SelectedIndex); };
                txtTrackNum.TextChanged += (s, e) =>
                {
                    int tn = template.TrackNum;
                    Int32.TryParse(txtTrackNum.Text.Trim(), out tn);
                    template.TrackNum = tn;
                    updateFilenames(cboRename.SelectedIndex);
                };
            }
            initialized = true;
        }
        protected override bool IsInputChar(char charCode)
        {
            return true;
        }
        private void killCboSelect()
        {
            cboGrouping.SelectionLength = 0;
            cboGenre.SelectionLength = 0;
        }
        private void setTabIndex(IEditControl C, ref int TabIndex)
        {
            labels[C].TabIndex = TabIndex++;
            C.TabIndex = TabIndex++;
            if (checkboxes.ContainsKey(C))
                checkboxes[C].TabIndex = TabIndex++;
        }
        private void setTabIndexes()
        {
            int idx = 0;

            btnOK.TabIndex = idx++;
            btnCancel.TabIndex = idx++;
            if (btnAutoNumber != null)
                btnAutoNumber.TabIndex = idx++;
            if (btnPrevious != null)
                btnPrevious.TabIndex = idx++;
            if (btnNext != null)
                btnNext.TabIndex = idx++;

            setTabIndex(txtTitle, ref idx);
            setTabIndex(txtArtist, ref idx);
            setTabIndex(txtAlbumArtist, ref idx);
            setTabIndex(txtAlbum, ref idx);
            setTabIndex(txtComposer, ref idx);
            setTabIndex(cboGrouping, ref idx);
            setTabIndex(cboGenre, ref idx);
            setTabIndex(cboRename, ref idx);
            setTabIndex(txtYear, ref idx);
            setTabIndex(txtTrackNum, ref idx);
            setTabIndex(txtDiskNum, ref idx);
            setTabIndex(cboCompilation, ref idx);

            if (isMultiple)
                chkArt.TabIndex = idx++;

            btnLoadArt.TabIndex = idx++;
            btnClearArt.TabIndex = idx++;
            btnCopyArt.TabIndex = idx++;
            btnPasteArt.TabIndex = idx++;

            art.TabStop = false;
            this.TabStop = false;

            setWrapAroundTabControl(idx, btnOK, btnCancel);
        }
        
        
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Escape:
                    cancel();
                    return true;
                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        protected override void resize()
        {
            base.resize();
            
            this.SuspendLayout();

            int BTN_WIDTH = 75;

            btnOK.Bounds = new Rectangle(MARGIN, MARGIN, BTN_WIDTH, btnOK.Height);
            btnCancel.Bounds = new Rectangle(MARGIN, btnOK.Bottom, BTN_WIDTH, btnCancel.Height);
            if (isMultiple)
            {
                if (btnAutoNumber != null)
                    btnAutoNumber.Bounds = new Rectangle(MARGIN, btnCancel.Bottom, BTN_WIDTH, btnAutoNumber.Height);
            }
            else
            {
                if (btnPrevious != null)
                    btnPrevious.Bounds = new Rectangle(MARGIN, btnCancel.Bottom, BTN_WIDTH, btnPrevious.Height);

                if (btnNext != null)
                    btnNext.Bounds = new Rectangle(MARGIN, btnPrevious.Bottom, BTN_WIDTH, btnNext.Height);
            }

            chkArt.Location = new Point(this.ClientRectangle.Width - btnLoadArt.Width - MARGIN,
                                        MARGIN);

            btnLoadArt.Bounds = new Rectangle(this.ClientRectangle.Width - btnLoadArt.Width - MARGIN,
                                             /* (isMultiple ? */chkArt.Bottom /*+ MARGIN : MARGIN) */,
                                              btnLoadArt.Width,
                                              btnLoadArt.Height);

            btnClearArt.Bounds = new Rectangle(btnLoadArt.Left,
                                               btnLoadArt.Bottom,
                                               btnLoadArt.Width,
                                               btnLoadArt.Height);

            btnCopyArt.Bounds = new Rectangle(btnClearArt.Left,
                                              btnClearArt.Bottom,
                                              btnClearArt.Width,
                                              btnClearArt.Height);

            btnPasteArt.Bounds = new Rectangle(btnCopyArt.Left,
                                               btnCopyArt.Bottom,
                                               btnCopyArt.Width,
                                               btnCopyArt.Height);

            int artSize = this.ClientRectangle.Height - MARGIN - MARGIN;
            art.Bounds = new Rectangle(btnLoadArt.Left - MARGIN - artSize,
                                       MARGIN,
                                       artSize,
                                       artSize);

            int totWidth = art.Left - MARGIN - MARGIN - MARGIN - BTN_WIDTH;

            int width = totWidth * 5 / 10 - 30;

            int x = MARGIN + BTN_WIDTH + MARGIN;
            int y = MARGIN - 1;

            y = placeControl(txtTitle, x, y, width);
            y = placeControl(txtArtist, x, y, width);
            y = placeControl(txtAlbumArtist, x, y, width);
            placeControl(txtAlbum, x, y, width);

            y = MARGIN;
            x += width + MARGIN + MARGIN;

            width = totWidth * 7 / 20 - 30;

            y = placeControl(txtComposer, x, y, width);
            y = placeControl(cboGrouping, x, y, width);
            placeControl(cboGenre, x, y, width);

            y = MARGIN;
            x += width + MARGIN + MARGIN;

            width = art.Left - x - MARGIN - MARGIN;

            y = placeControl(txtYear, x, y, width);
            placeControl(txtTrackNum, x, y, (width - MARGIN) / 2);
            y = placeControl(txtDiskNum, x + (width + MARGIN) / 2, y, (width - MARGIN) / 2);
            placeControl(cboCompilation, x, y, width);

            placeControl(cboRename, cboGenre.Left, labels[txtAlbum].Top, (isMultiple ? checkboxes[txtYear].Right : txtYear.Right) - cboGenre.Left);

            killCboSelect();

            this.ResumeLayout();
        }
        
        private int placeControl(IEditControl Control, int X, int Y, int Width)
        {
            bool checkbox = checkboxes.ContainsKey(Control);

            labels[Control].Location = new Point(X, Y);
            Control.Bounds = new Rectangle(X,
                                           labels[Control].Bottom,
                                           Width - (checkbox ? chkArt.Width : -MARGIN / 2),
                                           Control.Height);

            if (checkbox)
                checkboxes[Control].Location = new Point(Control.Right + MARGIN / 3,
                                                         Control.Top);

            return Control.Bottom;
        }
        
        private void setupArtwork()
        {
            if (art == null)
            {
                art = new Artwork();

                art.BlankMessage = Localization.Get(UI_Key.Edit_Tags_Artwork_Blank_Message);

                art.DragEnter += artDragEnter;
                art.DragDrop += artDragDrop;
                art.MouseDown += artDoDrag;
                art.AllowDrop = true;
                art.OverlayInfo = true;

                this.Controls.Add(art);

                //if (isMultiple)
                {
                    chkArt = new QCheckBox(String.Empty, this.BackColor);
                    chkArt.CheckedChanged += (s, e) =>
                    {
                        ImageDirty = chkArt.Checked;
                        Dirty |= chkArt.Checked;
                    };
                    this.Controls.Add(chkArt);
                }

                btnLoadArt = new QButton(Localization.Get(UI_Key.Edit_Tags_Load), false, true);
                btnLoadArt.ButtonPressed += (s) => { loadArt(); };
                this.Controls.Add(btnLoadArt);

                btnClearArt = new QButton(Localization.Get(UI_Key.Edit_Tags_Clear), false, true);
                btnClearArt.ButtonPressed += (s) => { clearArt(); };
                this.Controls.Add(btnClearArt);

                btnCopyArt = new QButton(Localization.Get(UI_Key.Edit_Tags_Copy), false, true);
                btnCopyArt.ButtonPressed += (s) => { copyArt(); };
                this.Controls.Add(btnCopyArt);

                btnPasteArt = new QButton(Localization.Get(UI_Key.Edit_Tags_Paste), false, true);
                btnPasteArt.ButtonPressed += (s) => { pasteArt(); };
                this.Controls.Add(btnPasteArt);
            }
            Image = null;
            art.CurrentTrack = tracks[0];
        }
        private bool Dirty
        {
            get { return dirty; }
            set
            {
                dirty = value;
                btnOK.Enabled = dirty;
            }
        }
        private bool ImageDirty
        {
            get { return imageDirty; }
            set
            {
                if (imageDirty != value)
                {
                    imageDirty = value;
                    if (imageDirty)
                    {
                        Dirty = true;
                        chkArt.Checked = true;
                    }
                }
            }
        }
        private void updateColor(object sender, EventArgs e)
        {
            if (isMultiple)
            {
                IEditControl c = checkboxes.First(kvp => kvp.Value.Equals(sender)).Key;
                QCheckBox cb = sender as QCheckBox;

                updateColor(c, cb);
            }
            else
            {
                IEditControl it = sender as IEditControl;
                it.Highlighted = it.Changed;
            }
        }
        
        private static void updateColor(IEditControl c, bool Highlight)
        {
            c.Highlighted = Highlight;
        }
        private static void updateColor(IEditControl c, QCheckBox cb)
        {
            updateColor(c, cb.Checked);
            if (c is IWatermarkable)
            {
                (c as IWatermarkable).WatermarkEnabled = !cb.Checked;
                c.ForeColor = cb.Checked ? Color.Black : Styles.Watermark;
            }
        }
        private static string getTitle(List<Track> Tracks)
        {
            return String.Format(Localization.Get(UI_Key.Edit_Tags_Title_1), ((Tracks.Count > 1) ? String.Format(Localization.Get(UI_Key.Edit_Tags_Title_2), Tracks.Count) : Tracks[0].ToShortString()));
        }
        
        private void setupAutoComplete()
        {
            setupAutoComplete(txtArtist, Database.GetArtists);
            setupAutoComplete(txtAlbum, Database.GetAlbums);
            setupAutoComplete(txtAlbumArtist, Database.GetArtists);
            setupAutoComplete(cboCompilation, null);
            setupAutoComplete(cboGrouping, Database.GetGroupings);
            setupAutoComplete(cboGenre, null);
            setupAutoComplete(txtYear, Database.GetYears);
            setupAutoComplete(txtComposer, Database.GetComposers);
        }
        private void previous()
        {
            apply();

            List<Track> tl = tracklist.Queue;

            int i = tl.IndexOf(tracks[0]);

            if (i > 0)
            {
                tracks[0].Selected = false;
                tracks = new List<Track>() { tl[i - 1] };
                template = new Track(tracks[0]);
                refresh();
                btnNext.Enabled = true;
                if (i == 1)
                    btnPrevious.Enabled = false;
            }

            foreach (IEditControl it in editControls)
            {
                it.SetOriginalText();
                updateColor(it, false);
            }
        }
        private void next()
        {
            apply();

            List<Track> tl = tracklist.Queue;

            int i = tl.IndexOf(tracks[0]);

            if (i >= 0 && i < tl.Count - 1)
            {
                tracks[0].Selected = false;
                tracks = new List<Track>() { tl[i + 1] };
                template = new Track(tracks[0]);
                refresh();
                btnPrevious.Enabled = true;
                if (i >= tl.Count - 2)
                    btnNext.Enabled = false;
            }
            foreach (IEditControl it in editControls)
            {
                it.SetOriginalText();
                updateColor(it, false);
            }
        }
        private void refresh()
        {
            tracks[0].Selected = true;
            tracklist.EnsureVisible(tracks[0]);
            tracklist.Invalidate();
            NormalView.GetInstance().TempDisplayTrackInfo(tracks[0]);

            txtTitle.Text = template.Title;
            txtArtist.Text = template.Artist;
            txtAlbum.Text = template.Album;
            txtAlbumArtist.Text = template.AlbumArtist;
            if (template.Compilation)
                cboCompilation.Text = "Yes";
            else
                cboCompilation.Text = "No";
            txtComposer.Text = template.Composer;
            cboGrouping.Text = template.Grouping;
            cboGenre.Text = template.Genre;
            txtYear.Text = template.YearString;
            txtTrackNum.Text = template.TrackNumString;
            txtDiskNum.Text = template.DiskNumString;

            if (!isMultiple)
            {
                updateFilenames(0);
            }
            saveCurrentValues();
            this.Text = getTitle(tracks);

            setupArtwork();

            Dirty = false;
            ImageDirty = false;
        }
        private void autoNumber()
        {
            List<Track> tl = tracklist.SelectedTracks;

#if DEBUG
            List<Track> q = tracklist.Queue.ToList();
            q.RemoveAll(t => tl.IndexOf(t) < 0);
            for (int i = 0; i < tl.Count; i++)
                System.Diagnostics.Debug.Assert(tl[i] == q[i]);
#endif

            int low = 1;
            IEnumerable<Track> sum = tl.FindAll(t => t.TrackNum > 0);
            if (sum.Any())
                low = Math.Max(1, sum.Min(t => t.TrackNum));

            frmNumberTracks nt = new frmNumberTracks(low, tl.Count);
            nt.ShowDialog(this);
            if (nt.DialogResult == DialogResult.OK)
            {
                int num = nt.First;
                for (int i = 0; i < tl.Count; i++)
                {
                    tl[i].TrackNum = num++;
                    tl[i].ChangeType |= ChangeType.WriteTags;
                }
                tracklist.Invalidate();
                TrackWriter.AddToUnsavedTracks(tl);
            }
        }
        private void saveCurrentValues()
        {
            foreach (IEditControl it in editControls)
                it.SetOriginalText();
        }
        private void apply()
        {
            if (Dirty)
            {
                writeTracks();

                foreach (IEditControl ia in editControls)
                    if (ia.Changed && ia.Text.Length > 0)
                    {
                        if (ia is QComboBox)
                            (ia as QComboBox).Items.Add(ia.Text);
                        else
                            ia.AutoCompleteCustomSource.Add(ia.Text);
                    }

                tec(TagEditAction.Apply);

                Dirty = false;
            }
        }

        private bool write(IEditControl c)
        {
            return !isMultiple || checkboxes[c].Checked;
        }
        protected override void cancel()
        {
            tec(TagEditAction.Cancel);
            this.Dispose();
        }
        protected override void ok()
        {
            writeTracks();
            tec(TagEditAction.OK);
        }

        private void updateFilenames(int Idx)
        {
            if (!isMultiple)
            {
                template.UpdateMainGroup();
                string[] ss = TrackWriter.GetRenames(template).Distinct().ToArray();
                cboRename.Items.Clear();
                cboRename.Items.AddRange(ss);
                cboRename.SelectedIndex = Idx;
            }
        }

        private void writeTracks()
        {
            if (Dirty)
            {
                bool writeArtist = write(txtArtist);
                bool writeAlbum = write(txtAlbum);
                bool writeAlbumArtist = write(txtAlbumArtist);
                bool writeGenre = write(cboGenre);
                bool writeGrouping = write(cboGrouping);
                bool writeComposer = write(txtComposer);
                bool writeYear = write(txtYear);
                bool writeTrackNum = write(txtTrackNum);
                bool writeDiskNum = write(txtDiskNum);
                bool? writeCompilation = write(cboCompilation) ? (cboCompilation.Text == Localization.YES) : (bool?)null;
                
                string title;

                if (!isMultiple)
                    title = txtTitle.Text.Trim();
                string artist = txtArtist.Text.Trim();
                string album = txtAlbum.Text.Trim();
                string albumArtist = txtAlbumArtist.Text.Trim();
                string genre = cboGenre.Text.Trim();
                string grouping = cboGrouping.Text.Trim();
                string composer = txtComposer.Text.Trim();

                int year = 0;
                Int32.TryParse(txtYear.Text.Trim(), out year);

                int trackNum = 0;
                Int32.TryParse(txtTrackNum.Text.Trim(), out trackNum);

                int diskNum = 0;
                Int32.TryParse(txtDiskNum.Text.Trim(), out diskNum);

                TrackWriter.RenameFormat rf = TrackWriter.RenameFormat.None;
                if (cboRename.SelectedIndex > 0 && (!isMultiple || checkboxes[cboRename].Checked))
                {
                    if (isMultiple)
                        rf = TrackWriter.GetRenameFormat(cboRename.Text);
                    else
                        rf = TrackWriter.GetRenameFormat(template, cboRename.Text);
                }

                bool writeImage = chkArt.Checked && ImageDirty;

                foreach (Track t in tracks)
                {
                    if (!isMultiple)
                    {
                        string s;
                        if (t.Title != (s = txtTitle.Text.Trim()))
                        {
                            t.Title = s;
                            t.ChangeType |= ChangeType.WriteTags;
                        }
                    }

                    if (writeArtist)
                    {
                        t.ChangeType |= ChangeType.WriteTags;
                        t.Artist = artist;
                    }

                    if (writeAlbum)
                    {
                        t.ChangeType |= ChangeType.WriteTags;
                        t.Album = album;
                    }

                    if (writeAlbumArtist)
                    {
                        t.ChangeType |= ChangeType.WriteTags;
                        t.AlbumArtist = albumArtist;
                    }

                    if (writeComposer)
                    {
                        t.ChangeType |= ChangeType.WriteTags;
                        t.Composer = composer;
                    }

                    if (writeGenre)
                    {
                        t.ChangeType |= ChangeType.WriteTags;
                        t.Genre = genre;
                    }

                    if (writeGrouping)
                    {
                        t.ChangeType |= ChangeType.WriteTags;
                        t.Grouping = grouping;
                    }

                    if (writeYear)
                    {
                        t.ChangeType |= ChangeType.WriteTags;
                        t.Year = year;
                    }

                    if (writeTrackNum)
                    {
                        t.ChangeType |= ChangeType.WriteTags;
                        t.TrackNum = trackNum;
                    }

                    if (writeDiskNum)
                    {
                        t.ChangeType |= ChangeType.WriteTags;
                        t.DiskNum = diskNum;
                    }

                    if (writeCompilation.HasValue)
                    {
                        t.ChangeType |= ChangeType.WriteTags;
                        t.Compilation = writeCompilation.Value;
                    }

                    t.UpdateMainGroup();
                    Database.IncrementDatabaseVersion(true);

                    if (writeImage)
                    {
                        t.SetCover(Image);
                        t.ChangeType |= ChangeType.EmbedImage;
                        this.Parent.Invalidate();
                    }
                    else
                    {
                        t.AllowCoverLoad(); // tags might make it accessible now
                    }
                    if (rf != TrackWriter.RenameFormat.None)
                    {
                        t.RenameFormat = rf;
                        t.ChangeType |= (ChangeType.Rename | ChangeType.IgnoreContainment);
                    }
                    if (Setting.KeepOrganized)
                    {
                        t.ChangeType |= ChangeType.Move;
                    }
                }

                TrackWriter.AddToUnsavedTracks(tracks);
            }
        }

        private void setupAutoComplete(Control Sender, Func<List<string>> Values)
        {
            if (settingUp)
                return;

            settingUp = true;

            TextBox tb;
            if ((tb = Sender as TextBox) != null)
            {
                tb.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                tb.AutoCompleteSource = AutoCompleteSource.CustomSource;
                tb.AutoCompleteCustomSource = new AutoCompleteStringCollection();
                tb.AutoCompleteCustomSource.AddRange(Values().ToArray());
            }
            else
            {
                ComboBox cb;
                if ((cb = Sender as ComboBox) != null)
                {
                    cb.AutoCompleteSource = AutoCompleteSource.ListItems;
                    cb.AutoCompleteMode = AutoCompleteMode.Append;
                }
            }
            settingUp = false;
        }
        private void setupCompilation(out QComboBox ComboBox, string Caption, bool MakeCheckbox)
        {
            ComboBox = new QComboBox(false, Styles.FontSmaller);

            bool? isComp = tracks[0].Compilation;

            foreach (Track t in tracks)
                if (t.Compilation != isComp)
                {
                    isComp = null;
                    break;
                }

            if (isComp.HasValue)
            {
                ComboBox.Items.AddRange(new string[] { Localization.YES, Localization.NO });
                if (isComp.Value)
                    ComboBox.SelectedIndex = ComboBox.FindStringExact(Localization.YES);
                else
                    ComboBox.SelectedIndex = ComboBox.FindStringExact(Localization.NO);
            }
            else
            {
                System.Diagnostics.Debug.Assert(isMultiple);
                ComboBox.Items.AddRange(new string[] { MULTIPLE_VALUES, Localization.YES, Localization.NO });
                ComboBox.SelectedIndex = 0;
                ComboBox.EnableWatermark(this, MULTIPLE_VALUES, MULTIPLE_VALUES);
            }

            setupControl(ComboBox, Caption, MakeCheckbox);
        }
        private void setupRename(string Caption, bool MakeCheckbox)
        {
            cboRename = new QComboBox(false, Styles.FontSmaller);
            setupControl(cboRename, Caption, MakeCheckbox);

            if (isMultiple)
            {
                string[] ss = TrackWriter.GetRenames().ToArray();
                cboRename.Items.AddRange(ss);
                cboRename.SelectedIndex = 0; // cboRename.FindStringExact(ss[0]);
                cboRename.LostFocus += (s, e) =>
                {
                    if (cboRename.Text.Length == 0)
                        checkboxes[cboRename].Checked = false;
                };
                cboRename.EnableWatermark(this, MULTIPLE_VALUES, MULTIPLE_VALUES);
            }
            else
            {
                updateFilenames(0);
            }

        }
        private void setupComboBox(out QComboBox ComboBox, string Caption, string DefaultText, string[] Values, bool MakeCheckbox)
        {
            ComboBox = new QComboBox(true, Styles.FontSmaller);
            ComboBox.Items.AddRange(Values);
            ComboBox.Text = DefaultText;

            setupControl(ComboBox, Caption, MakeCheckbox);
        }
        private void setupTextBox(out QTextBox TextBox, string Caption, string DefaultText, bool MakeCheckbox)
        {
            TextBox = new QTextBox(Styles.FontSmaller);
            TextBox.Text = DefaultText;
            TextBox.MaxLength = 255;
            setupControl(TextBox, Caption, MakeCheckbox);
        }

        private void setupControl(IEditControl Control, string Caption, bool MakeCheckbox)
        {
            QLabel label = new QLabel(Caption, Styles.FontSmaller);

            this.Controls.Add(label);

            labels.Add(Control, label);
            label.ShowAccellerator();

            if (MakeCheckbox)
            {
                QCheckBox cb = new QCheckBox(String.Empty, this.BackColor);
                cb.CheckedChanged += (s, e) =>
                {
                    updateColor(s, e);
                    Dirty = true;
                };
                this.Controls.Add(cb);
                checkboxes.Add(Control, cb);
                Control.ForeColor = Styles.Watermark;
            }

            this.Controls.Add(Control as Control);

            if (Control is QTextBox)
            {
                Control.TextChanged += textChanged;
                QTextBox tb = Control as QTextBox;
                tb.GotFocus += (s, e) => { Clock.DoOnMainThread(tb.SelectAll, 30); };
                if (tb.Text == VARIES_TOKEN)
                {
                    tb.EnableWatermark(this, MULTIPLE_VALUES, String.Empty);
                    tb.Text = String.Empty;
                }
                editControls.Add(tb);
            }
            else if (Control is QComboBox)
            {
                QComboBox cb = Control as QComboBox;

                // need both, depends on editable vs. uneditable
                cb.TextChanged += textChanged;
                cb.SelectedIndexChanged += textChanged;

                if (cb.Text == VARIES_TOKEN)
                {
                    cb.EnableWatermark(this, MULTIPLE_VALUES, String.Empty);
                    cb.Text = String.Empty;
                }
                cb.DropDown += (s, e) =>
                {
                    if (isMultiple)
                        cb.ForeColor = Color.Black;
                };
                cb.DropDownClosed += (s, e) =>
                {
                    if (isMultiple)
                        updateColor(cb, checkboxes[cb]);
                    else
                        updateColor(cb, EventArgs.Empty);
                };
                editControls.Add(cb);
            }
        }

        private static void selectAll(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;

            if (tb != null)
            {
                tb.SelectAll();
            }
        }

        private void textChanged(object sender, EventArgs e)
        {
            if (initialized)
            {
                IEditControl c = sender as IEditControl;

                if (c != null)
                {
                    if (c.Changed)
                    {
                        if (isMultiple)
                        {
                            if (checkboxes.ContainsKey(c))
                                checkboxes[c].Checked = true;
                        }
                        Dirty = true;
                    }
                    if (!isMultiple)
                        updateColor(c, c.Changed);
                }
            }
        }
        private string getDefaultText(Func<Track, string> Function)
        {
            string s = Function(template);

            foreach (Track t in tracks)
            {
                if (s != Function(t))
                    return VARIES_TOKEN;
            }
            return s;
        }

        private void loadArt()
        {
            try
            {
                string path = String.Empty;
                Track t = template;

                if (t != null)
                    path = t.FilePath;

                if (path.Length == 0 || !File.Exists(path))
                    path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                else
                    path = Path.GetDirectoryName(path);

                OpenFileDialog ofd = Lib.GetOpenFileDialog(path,
                                                           true,
                                                           "Image Files (*.jpg;*.jpeg;*.gif;*.png;*.bmp;*.tif;*.tiff)|*.jpg;*.jpeg;*.gif;*.png;*.bmp;*.tif;*.tiff|All Files (*.*)|*.*",
                                                           1);

                ofd.Multiselect = false;

                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    Image = ImageItem.ImageItemFromGraphicsFile(ofd.FileName);
                    ImageDirty = true;
                }
            }
            catch { }
        }
        private void clearArt()
        {
            try
            {
                art.CurrentTrack = null;
                Image = null;
                ImageDirty = true;
            }
            catch { }
        }
        private void copyArt()
        {
            try
            {
                if (Image != null)
                {
                    clipboard = Image;
                    Clipboard.SetImage(Image.Image);
                }
            }
            catch { }
        }
        private void pasteArt()
        {
            try
            {
                if (clipboard != null)
                {
                    Image = clipboard;
                }
                else if (Clipboard.ContainsImage())
                {
                    Image = ImageItem.ImageItemFromClipboard();
                }
                Dirty = true;
                ImageDirty = true;
            }
            catch { }
        }

        private void artDoDrag(object sender, MouseEventArgs e)
        {
            if (Image != null)
            {
                DragDropEffects dde = DoDragDrop(Image, DragDropEffects.Copy);
            }
        }
        private void artDragEnter(object sender, DragEventArgs e)
        {
            if ((e.AllowedEffect & DragDropEffects.Copy) == DragDropEffects.Copy)
               if (ImageItem.DragHasImage(e.Data))
                    e.Effect = DragDropEffects.Copy;
        }
        private void artDragDrop(object sender, DragEventArgs e)
        {
            Image = ImageItem.ImageItemFromDrag(e.Data);

            Dirty = true;
            ImageDirty = true;
            this.Invalidate();
        }

        private static ImageItem clipboard = null;
        private bool IsEmbedded
        {
            get
            {
                return Image.Src == ImageItem.Source.Embedded;
            }
        }
        private ImageItem Image
        {
            get
            {
                return art.OverrideImage;
            }
            set
            {
                art.OverrideImage = value;
            }
        }
    }
}