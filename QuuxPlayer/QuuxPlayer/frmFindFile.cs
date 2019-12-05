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
using System.Threading;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal sealed partial class frmFindFile : QFixedDialog
    {
        private const int PADDING = 5;
        private const TextFormatFlags tff = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix;

        private Label lblInstructions;
        private Label lblFileInfo;
        private TextBox txtFilePath;
        private QButton btnFind;
        private QButton btnThisOnly;

        private Track track;
        private Font font;
        private bool initialized;
        private string oldPath;
        private string newPath;

        public frmFindFile() : base(String.Empty, ButtonCreateType.OKAndCancel)
        {
            initialized = false;

            lblInstructions = new Label();
            lblInstructions.Text = Localization.Get(UI_Key.Find_File_Instructions, Application.ProductName);
            lblInstructions.AutoSize = false;
            lblInstructions.Visible = true;
            lblInstructions.ForeColor = Styles.LightText;
            this.Controls.Add(lblInstructions);

            lblFileInfo = new Label();
            lblFileInfo.AutoSize = false;
            lblFileInfo.Visible = true;
            lblFileInfo.ForeColor = Styles.LightText;
            this.Controls.Add(lblFileInfo);

            btnFind = new QButton(Localization.Get(UI_Key.Find_File_Find), false, false);
            AddButton(btnFind, btnFind_Click);

            btnThisOnly = new QButton(Localization.Get(UI_Key.Find_File_This_Track_Only), false, false);
            AddButton(btnThisOnly, btnThisOnly_Click);

            txtFilePath = new TextBox();
            txtFilePath.Visible = true;
            txtFilePath.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(txtFilePath);

            btnOK.Text = Localization.Get(UI_Key.Find_File_All_Tracks);

            lblInstructions.Font = Styles.Font;
            lblFileInfo.Font = Styles.FontItalic;
            txtFilePath.Font = Styles.Font;
            this.font = Styles.Font;

            int tabIndex = 0;
            btnFind.TabIndex = tabIndex++;
            btnOK.TabIndex = tabIndex++;
            btnThisOnly.TabIndex = tabIndex++;
            btnCancel.TabIndex = tabIndex++;
            txtFilePath.TabIndex = tabIndex++;
        }
        
        public Track Track
        {
            set
            {
                this.track = value;
                lblFileInfo.Text = track.ToString();
                txtFilePath.Text = track.FilePath;
                this.Text = Localization.Get(UI_Key.Find_File_Update_Path, value.ToShortString());
                initialized = true;

                setMetrics();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            setMetrics();
        }

        private void btnThisOnly_Click()
        {
            if (updateThisOnly())
                this.Close();
        }
        private void btnFind_Click()
        {
            string mydocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string path = txtFilePath.Text;

            if (txtFilePath.Text.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                path = mydocs;
            else if (!Directory.Exists(Path.GetDirectoryName(txtFilePath.Text)))
                path = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            else
                path = txtFilePath.Text;

            OpenFileDialog ofd = Lib.GetOpenFileDialog(path,
                                                       false,
                                                       Localization.Get(UI_Key.Lib_File_Filter),
                                                       Database.GetSetting(SettingType.FileDialogFilterIndex, 12));

            try
            {
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    if (File.Exists(ofd.FileName))
                    {
                        txtFilePath.Text = ofd.FileName;
                        Database.SaveSetting(SettingType.FileDialogFilterIndex, ofd.FilterIndex);
                    }
                    else
                    {
                        QMessageBox.Show(this,
                                         Localization.Get(UI_Key.Find_File_File_Does_Not_Exist),
                                         Localization.Get(UI_Key.Find_File_File_Does_Not_Exist_Title),
                                         QMessageBoxIcon.Error);
                    }
                }
            }
            catch
            {
                if (txtFilePath.Text != mydocs)
                {
                    txtFilePath.Text = mydocs;
                    btnFind_Click();
                }
            }
        }
        protected override void ok()
        {
            oldPath = Path.GetFullPath(track.FilePath);

            if (updateThisOnly())
            {
                btnOK.Enabled = false;
                btnThisOnly.Enabled = false;
                btnCancel.Enabled = false;
                btnFind.Enabled = false;
                txtFilePath.Enabled = false;

                newPath = Path.GetFullPath(track.FilePath);

                Clock.DoOnNewThread(doBulkUpdate);
                //System.Threading.Thread t = new System.Threading.Thread(doBulkUpdate);
                //t.Name = "Find File Thread";
                //t.Priority = ThreadPriority.BelowNormal;
                //t.IsBackground = true;
                //t.Start();

                this.Close();
            }
        }
        private void setMetrics()
        {
            if (initialized)
            {
                int h;

                lblInstructions.Bounds = new Rectangle(PADDING, PADDING, this.ClientRectangle.Width - 2 * PADDING, 100);

                h = TextRenderer.MeasureText(lblInstructions.Text, this.font, lblInstructions.Size, tff).Height;

                lblInstructions.Height = h;

                lblFileInfo.Bounds = new Rectangle(PADDING, lblInstructions.Bottom + PADDING + PADDING + PADDING, lblInstructions.Width, 100);

                h = TextRenderer.MeasureText(lblFileInfo.Text, this.font, lblFileInfo.Size, tff).Height;

                lblFileInfo.Height = h;

                btnFind.Location = new Point(this.ClientRectangle.Width - PADDING - btnFind.Width, lblFileInfo.Bottom + PADDING + PADDING + PADDING);
                txtFilePath.Location = new Point(PADDING, btnFind.Top + btnFind.Height / 2 - txtFilePath.Height / 2);
                txtFilePath.Width = btnFind.Left - 2 * PADDING;

                PlaceButtons(this.ClientRectangle.Width,
                             btnFind.Bottom + 4 * PADDING,
                             btnCancel,
                             btnThisOnly,
                             btnOK);

                this.Size = new Size(Math.Max(this.Width, this.Width - btnOK.Left + PADDING),
                                     this.Height - this.ClientRectangle.Height + btnOK.Bottom + PADDING);

            }
        }
        private void doBulkUpdate()
        {
            int i = newPath.Length - 1;
            int lenDiff = newPath.Length - oldPath.Length;

            while (i >= lenDiff && newPath[i] == oldPath[i - lenDiff])
            {
                i--;
            }
            i++;
            string oldStart = oldPath.Substring(0, i - lenDiff);
            string newStart = newPath.Substring(0, i);
            int oldStartLength = oldStart.Length;

            int count = 0;
            int badCount = 0;

            List<Track> tracks = Database.LibrarySnapshot;

            for (int j = 0; j < tracks.Count; j++)
            {
                Track tt = tracks[j];

                if (tt.Exists != true && tt.FilePath.StartsWith(oldStart, StringComparison.OrdinalIgnoreCase) && !tt.ConfirmExists)
                {
                    string s = newStart + tt.FilePath.Substring(oldStartLength);
                    if (File.Exists(s))
                    {
                        tt.FilePath = s;
                        tt.ConfirmExists = true;
                        count++;
                        Controller.ShowMessage(Localization.Get(UI_Key.Find_File_Message_Updating, count.ToString(), tt.ToShortString()));
                        Controller.GetInstance().Invalidate();
                    }
                    else
                    {
                        badCount++;
                    }
                }
                else
                {
                    badCount++;
                }
            }
            Database.IncrementDatabaseVersion(true);
        }
        private bool updateThisOnly()
        {
            string path;
            try
            {
                path = Path.GetFullPath(txtFilePath.Text);
            }
            catch
            {
                QMessageBox.Show(this,
                                 Localization.Get(UI_Key.Find_File_Illegal_Path),
                                 Localization.Get(UI_Key.Find_File_Illegal_Path_Title),
                                 QMessageBoxIcon.Error);
                return false;
            }

            if (!File.Exists(txtFilePath.Text))
            {
                QMessageBox.Show(this,
                                 Localization.Get(UI_Key.Find_File_File_Not_Found, txtFilePath.Text),
                                 Localization.Get(UI_Key.Find_File_File_Not_Found_Title),
                                 QMessageBoxIcon.Error);
                return false;
            }
            else if (Database.TrackExists(t => t.FilePath == path))
            {
                QMessageBox.Show(this,
                                 Localization.Get(UI_Key.Find_File_File_Already_Exists, txtFilePath.Text),
                                 Localization.Get(UI_Key.Find_File_File_Already_Exists_Title),
                                 QMessageBoxIcon.Error);
                return false;
            }
            else
            {
                Track t = Track.Load(txtFilePath.Text);
                if (t == null)
                {
                    QMessageBox.Show(this,
                                     Localization.Get(UI_Key.Find_File_File_Error),
                                     Localization.Get(UI_Key.Find_File_File_Error_Title),
                                     QMessageBoxIcon.Error);
                    return false;
                }
                else if (t.Artist != track.Artist || t.Title != track.Title || t.Album != track.Album)
                {
                    if (QMessageBox.Show(this,
                                         Localization.Get(UI_Key.Find_File_Different_Information,
                                                       track.Artist,
                                                       track.Title,
                                                       track.Album,
                                                       t.Artist,
                                                       t.Title,
                                                       t.Album,
                                                       Environment.NewLine),
                                         Localization.Get(UI_Key.Find_File_Different_Information_Title),
                                         QMessageBoxButtons.OKCancel,
                                         QMessageBoxIcon.Question,
                                         QMessageBoxButton.NoCancel)
                                                == DialogResult.Cancel)

                        return false;
                }
                track.FilePath = t.FilePath;
                track.ConfirmExists = true;
                return true;
            }
        }
    }
}
