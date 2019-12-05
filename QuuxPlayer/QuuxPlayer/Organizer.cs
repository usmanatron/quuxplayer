/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal class Organizer : QPanel
    {
        private Callback done;
        private QLabel lblTitle;
        private QLabel lblDirectory;
        private QTextBox txtDirectory;
        private QButton btnBrowse;
        private QButton btnHelp;
        private QComboBox cboSubdirectory;
        private QLabel lblSubdirectory;
        private QComboBox cboRename;
        private QLabel lblRename;
        private QCheckBox chkKeepOrganized;
        private QCheckBox chkMoveIntoTopFolder;
        private Track sampleTrack;
        private Rectangle sampleRect;
        private Point sampleCaptionPoint;
        private TrackWriter.RenameFormat renameFormat = TrackWriter.RenameFormat.None;
        private TrackWriter.DirectoryFormat dirFormat = TrackWriter.DirectoryFormat.None;
        private string sampleText = String.Empty;
        private string sample = String.Empty;
        private Font boldFont;
        private Font font;
        private string oldRoot;
        private string invalidPath;
        private string tokenMyMusic;
        private string tokenMyDocuments;
        private string tokenDesktop;

        public Organizer(Callback DoneCallback) : base()
        {
            TrackWriter.Stop();

            done = DoneCallback;

            invalidPath = Localization.Get(UI_Key.Organize_Invalid_Path);
            sample = Localization.Get(UI_Key.Organize_Sample);
            tokenMyMusic = Localization.Get(UI_Key.Organize_Token_My_Music);
            tokenMyDocuments = Localization.Get(UI_Key.Organize_Token_My_Documents);
            tokenDesktop = Localization.Get(UI_Key.Organize_Token_Desktop);

            font = Styles.FontSmaller;
            boldFont = new Font(font, FontStyle.Bold);

            btnOK = new QButton(Localization.OK, false, true);
            btnCancel = new QButton(Localization.CANCEL, false, true);

            btnHelp = new QButton(Localization.Get(UI_Key.Organize_Help), false, true);
            AddButton(btnHelp, help);

            lblTitle = new QLabel(Localization.Get(UI_Key.Organize_Title), font);
            this.Controls.Add(lblTitle);
            lblTitle.Location = new System.Drawing.Point(MARGIN, MARGIN);

            lblDirectory = new QLabel(Localization.Get(UI_Key.Organize_Top_Folder), font);
            lblDirectory.ShowAccellerator();
            this.Controls.Add(lblDirectory);

            txtDirectory = new QTextBox();
            txtDirectory.Font = font;
            this.Controls.Add(txtDirectory);
            
            if (Setting.TopLevelDirectory.Length == 0)
                Setting.TopLevelDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

            oldRoot = Setting.TopLevelDirectory;
            txtDirectory.Text = simplify(oldRoot);

            btnBrowse = new QButton(Localization.Get(UI_Key.Organize_Browse), false, false);
            btnBrowse.ShowAccellerator(Keys.B);
            AddButton(btnBrowse, browse);

            lblSubdirectory = new QLabel(Localization.Get(UI_Key.Organize_Folder_Structure), font);
            lblSubdirectory.ShowAccellerator();
            this.Controls.Add(lblSubdirectory);

            cboSubdirectory = new QComboBox(false);
            cboSubdirectory.Font = font;
            cboSubdirectory.Items.AddRange(TrackWriter.GetDirFormats().ToArray());
            string sub = TrackWriter.GetDirFormat(Setting.DefaultDirectoryFormat);
            if (cboSubdirectory.Items.Contains(sub))
                cboSubdirectory.SelectedIndex = cboSubdirectory.FindStringExact(sub);
            else
                cboSubdirectory.SelectedIndex = 0;
            this.Controls.Add(cboSubdirectory);
            cboSubdirectory.AutoSetWidth();

            lblRename = new QLabel(Localization.Get(UI_Key.Organize_Rename), font);
            lblRename.ShowAccellerator();
            this.Controls.Add(lblRename);

            cboRename = new QComboBox(false);
            cboRename.Font = font;
            string[] renames = TrackWriter.GetRenames().ToArray();
            renames[0] = Localization.Get(UI_Key.Organize_Dont_Change);
            cboRename.Items.AddRange(renames);
            string ren = TrackWriter.GetRenameFormat(Setting.DefaultRenameFormat);
            if (cboRename.Items.Contains(ren))
                cboRename.SelectedIndex = cboRename.FindStringExact(ren);
            else
                cboRename.SelectedIndex = 0;
            this.Controls.Add(cboRename);

            cboRename.AutoSetWidth();

            chkKeepOrganized = new QCheckBox(Localization.Get(UI_Key.Organize_Keep_Organized), this.BackColor);
            chkKeepOrganized.ShowAccellerator();
            chkKeepOrganized.Font = font;
            chkKeepOrganized.Checked = Setting.KeepOrganized;
            this.Controls.Add(chkKeepOrganized);

            chkMoveIntoTopFolder = new QCheckBox(Localization.Get(UI_Key.Organize_Move_Into_Top_Folder), this.BackColor);
            chkMoveIntoTopFolder.Font = font;
            chkMoveIntoTopFolder.Checked = Setting.MoveNewFilesIntoMain;
            this.Controls.Add(chkMoveIntoTopFolder);

            btnOK.Text = Localization.Get(UI_Key.Organize_Organize);
            btnCancel.Text = Localization.Get(UI_Key.Organize_Dont_Organize);
            btnOK.ShowAccellerator(Keys.O);
            btnCancel.ShowAccellerator(Keys.D);

            AddButton(btnOK, ok);
            AddButton(btnCancel, cancel);

            sampleTrack = new Track(-1,
                                    Localization.Get(UI_Key.Organize_Sample_Track_Path),
                                    Track.FileType.MP3,
                                    Localization.Get(UI_Key.Organize_Sample_Track_Title),
                                    Localization.Get(UI_Key.Organize_Sample_Track_Album),
                                    Localization.Get(UI_Key.Organize_Sample_Track_Artist),
                                    String.Empty,
                                    String.Empty,
                                    Localization.Get(UI_Key.Organize_Sample_Track_Grouping),
                                    Localization.Get(UI_Key.Organize_Sample_Track_Genre),
                                    (6 * 60 + 24) * 1000,
                                    5,
                                    0,
                                    1973,
                                    0,
                                    5,
                                    320000,
                                    0,
                                    false,
                                    DateTime.Today,
                                    DateTime.Today,
                                    DateTime.Today,
                                    String.Empty,
                                    2,
                                    44100,
                                    ChangeType.None,
                                    null,
                                    float.MinValue,
                                    float.MinValue);

            resize();
            this.Height = chkKeepOrganized.Bottom + MARGIN;

            cboSubdirectory.SelectedIndexChanged += (s, e) => { updateSample(); };
            cboRename.SelectedIndexChanged += (s, e) => { updateSample(); };
            txtDirectory.TextChanged += (s, e) => { updateSample(); };

            updateSample();

            int tabIndex = 0;

            lblDirectory.TabIndex = tabIndex++;
            txtDirectory.TabIndex = tabIndex++;
            btnBrowse.TabIndex = tabIndex++;
            lblSubdirectory.TabIndex = tabIndex++;
            cboSubdirectory.TabIndex = tabIndex++;
            lblRename.TabIndex = tabIndex++;
            cboRename.TabIndex = tabIndex++;
            chkKeepOrganized.TabIndex = tabIndex++;
            chkMoveIntoTopFolder.TabIndex = tabIndex++;
            btnHelp.TabIndex = tabIndex++;
            btnCancel.TabIndex = tabIndex++;
            btnOK.TabIndex = tabIndex++;

            setWrapAroundTabControl(tabIndex, txtDirectory, null);

            initialized = true;
        }
        private void help()
        {
            Net.BrowseTo(Lib.PRODUCT_URL + "/doc_organizer.php");
        }
        private static bool pathIsValid(string ss)
        {
            return !(ss.IndexOfAny(Path.GetInvalidPathChars()) >= 0 ||
                                !Path.IsPathRooted(ss) ||
                                ss.Contains("\\\\") ||
                                (ss.Length > 2 && ss.IndexOf(':', 2) >= 0) ||
                                ss[0] == '\\');
        }
        private void updateSample()
        {
            renameFormat = TrackWriter.GetRenameFormat(cboRename.Text);
            dirFormat = TrackWriter.GetDirFormat(cboSubdirectory.Text);

            if (pathIsValid(complexify(root())))
            {
                btnOK.Enabled = true;
                sampleText = Path.Combine((root()), TrackWriter.GetPath(sampleTrack, dirFormat, renameFormat));// +sampleTrack.DefaultExtension;
            }
            else
            {
                sampleText = invalidPath;
                btnOK.Enabled = false;
            }
            this.Invalidate();
        }
        private string root()
        {
            string s = txtDirectory.Text.Trim();
            if (s.EndsWith("\\"))
                return s;
            else
                return s + "\\";
        }
        protected override void resize()
        {
            base.resize();

            this.SuspendLayout();

            PlaceButtonsHoriz(this.ClientRectangle.Height - MARGIN / 2);
            btnHelp.Location = new Point(btnCancel.Left - btnHelp.Width - MARGIN, btnOK.Top);

            int labelWidth = Math.Max(400, this.ClientRectangle.Width * 14 / 20);
            int controlWidth = Math.Min(labelWidth, 700);

            lblDirectory.Location = new Point(lblTitle.Left + MARGIN, lblTitle.Bottom + MARGIN);

            txtDirectory.Location = new Point(lblDirectory.Left, lblDirectory.Bottom + MARGIN / 2);
            txtDirectory.Width = controlWidth - MARGIN - btnBrowse.Width;

            btnBrowse.Location = new Point(txtDirectory.Right + 1, txtDirectory.Top + txtDirectory.Height / 2 - btnBrowse.Height / 2);

            lblSubdirectory.Location = new Point(lblDirectory.Left, txtDirectory.Bottom + SPACING);
            cboSubdirectory.Location = new Point(lblSubdirectory.Left, lblSubdirectory.Bottom + SPACING / 2);

            cboRename.Location = new Point(cboSubdirectory.Right + MARGIN, cboSubdirectory.Top);
            lblRename.Location = new Point(cboRename.Left, lblSubdirectory.Top);

            chkKeepOrganized.Location = new Point(lblSubdirectory.Left, cboRename.Bottom + SPACING);
            chkMoveIntoTopFolder.Location = new Point(chkKeepOrganized.Right + MARGIN, chkKeepOrganized.Top);

            int x = Math.Max(cboRename.Right, Math.Max(lblTitle.Right, btnBrowse.Right));
            sampleRect = new Rectangle(x + MARGIN, lblDirectory.Top, this.ClientRectangle.Width - x - MARGIN - MARGIN, btnOK.Top - MARGIN - MARGIN);
            sampleCaptionPoint = new Point(sampleRect.Left, lblTitle.Top);

            this.ResumeLayout();

            this.Invalidate();
        }
        private void browse()
        {
            try
            {
                string d = Setting.TopLevelDirectory;
                d = Lib.GetUserSelectedFolder(Localization.Get(UI_Key.Organize_Get_Folder), d, true);
                
                if (!String.IsNullOrEmpty(d))
                {
                    txtDirectory.Text = simplify(d);
                    createDir();
                }
            }
            catch { }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            TextRenderer.DrawText(e.Graphics, sample, boldFont, sampleCaptionPoint, Styles.LightText, TextFormatFlags.NoPadding);
            TextRenderer.DrawText(e.Graphics, sampleText, font, sampleRect, Styles.LightText, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }
        
        protected override void ok()
        {
            if (createDir())
            {
                this.Enabled = false;

                saveSettings();

                string r = complexify(root());

                List<Track> outsideTracks = Database.FindAllTracks(t => !((oldRoot.Length > 0 && t.FilePath.StartsWith(oldRoot, StringComparison.OrdinalIgnoreCase)) || (t.FilePath.StartsWith(r, StringComparison.OrdinalIgnoreCase))));
                List<Track> tracks = Database.LibrarySnapshot;

                foreach (Track t in tracks)
                    t.ChangeType |= (ChangeType.Move | ChangeType.IgnoreContainment);

                System.Diagnostics.Debug.WriteLine("num: " + tracks.Count(t => (t.ChangeType & ChangeType.IgnoreContainment) != 0));

                if (outsideTracks.Any(t => t.ConfirmExists))
                {
                    List<frmTaskDialog.Option> options = new List<frmTaskDialog.Option>();
                    options.Add(new frmTaskDialog.Option("Move all my files", "All files in your library will be relocated within the top folder. (Files on different drives than your top folder's drive will be copied instead of moved.)", 0));
                    options.Add(new frmTaskDialog.Option("Don't move outside files", "Only files already under the top folder will be organized.", 1));
                    options.Add(new frmTaskDialog.Option("Cancel", "Go back to the organizer panel.", 2));

                    frmTaskDialog td = new frmTaskDialog(Localization.Get(UI_Key.Organize_Move_Files_Title),
                                                         Localization.Get(UI_Key.Organize_Move_Files),
                                                         options);

                    System.Diagnostics.Debug.WriteLine("num: " + tracks.Count(t => (t.ChangeType & ChangeType.IgnoreContainment) != 0));

                    td.ShowDialog(this);

                    switch (td.ResultIndex)
                    {
                        case 0:
                            break;
                        case 1:
                            foreach (Track t in outsideTracks)
                                t.ChangeType &= ~ChangeType.IgnoreContainment;
                            break;
                        case 2:
                            this.Enabled = true;
                            txtDirectory.Focus();
                            return;
                    }
                }

                foreach (Track t in tracks)
                {
                    t.RenameFormat = renameFormat;
                    if (renameFormat != TrackWriter.RenameFormat.None)
                        t.ChangeType |= ChangeType.Rename;
                }

                TrackWriter.AddToUnsavedTracks(tracks);

                done();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Escape:
                    done();
                    return true;
                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        private void saveSettings()
        {
            updateSample(); // sets dirFormat and renameFormat

            string s = complexify(root());

            Setting.TopLevelDirectory = s;
            Setting.DefaultDirectoryFormat = dirFormat;
            Setting.DefaultRenameFormat = renameFormat;

            bool valid = pathIsValid(s);

            Setting.KeepOrganized = valid && chkKeepOrganized.Checked;
            Setting.MoveNewFilesIntoMain = valid && chkMoveIntoTopFolder.Checked;
        }
        protected override void cancel()
        {
            List<Track> t = Database.LibrarySnapshot;

            foreach (Track tt in t)
            {
                tt.ChangeType &= ~(ChangeType.Rename | ChangeType.Move | ChangeType.IgnoreContainment);
            }

            chkMoveIntoTopFolder.Checked = false;
            chkKeepOrganized.Checked = false;

            saveSettings();
            done();
        }
        private bool createDir()
        {
            string s = complexify(txtDirectory.Text.Trim());
            if (!Directory.Exists(s))
            {
                try
                {
                    if (QMessageBox.Show(this,
                                         Localization.Get(UI_Key.Organize_Create_Directory),
                                         Localization.Get(UI_Key.Organize_Create_Directory_Title),
                                         QMessageBoxButtons.YesNo,
                                         QMessageBoxIcon.Question,
                                         QMessageBoxButton.YesOK) == DialogResult.OK)
                    {
                        Directory.CreateDirectory(s);
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    QMessageBox.Show(this,
                                     Localization.Get(UI_Key.Organize_Create_Directory_Failed),
                                     Localization.Get(UI_Key.Organize_Create_Directory_Failed_Title),
                                     QMessageBoxIcon.Error);
                    return false;
                }
            }
            return true;
        }
        private string simplify(string Input)
        {
            if (Input.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)))
                return Input.Replace(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "[My Music]");
            else if (Input.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)))
                return Input.Replace(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "[My Documents]");
            else if (Input.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)))
                return Input.Replace(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "[Desktop]");
            else
                return Input;
        }
        private string complexify(string Input)
        {
            if (Input.StartsWith(tokenMyMusic, StringComparison.OrdinalIgnoreCase))
                return Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) + Input.Substring(tokenMyMusic.Length);
            else if (Input.StartsWith(tokenMyDocuments, StringComparison.OrdinalIgnoreCase))
                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + Input.Substring(tokenMyDocuments.Length);
            else if (Input.StartsWith(tokenDesktop, StringComparison.OrdinalIgnoreCase))
                return Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + Input.Substring(tokenDesktop.Length);
            else
                return Input;
        }
    }
}
