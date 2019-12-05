/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal sealed class frmFileAssociations : QFixedDialog
    {
        private const string APP_PREFIX = "QuuxPlayer.";
        private const string MP3 = "MP3File";
        private const string FLAC = "FLACFile";
        private const string OGG = "OGGFile";
        private const string WMA = "WMAFile";
        private const string AAC = "AACFile";
        private const string WV = "WVFile";
        private const string WAV = "WAVFile";
        private const string AC3 = "AC3File";
        private const string MPC = "MPCFile";
        private const string ALAC = "ALACFile";
        private const string AIFF = "AIFFFile";
        private const string APE = "APEFile";
        private const string PLS = "PLSFile";
        private const string M3U = "M3UFile";

        private QLabel lblInstructions;
        private QCheckBox chkMP3;
        private QCheckBox chkWMA;
        private QCheckBox chkOGG;
        private QCheckBox chkFLAC;
        private QCheckBox chkM4A;
        private QCheckBox chkWV;
        private QCheckBox chkWAV;
        private QCheckBox chkAC3;
        private QCheckBox chkMPC;
        private QCheckBox chkALAC;
        private QCheckBox chkAIFF;
        private QCheckBox chkAPE;
        private QCheckBox chkPLS;
        private QCheckBox chkM3U;

        private QButton btnAll;
        private QButton btnNone;

        private QLabel lblAudioFiles;
        private QLabel lblPlaylistFiles;

        public frmFileAssociations() : base(Localization.Get(UI_Key.File_Associations_Title), ButtonCreateType.OKAndCancel)
        {
            this.Size = new System.Drawing.Size(430, this.Size.Height);

            lblInstructions = new QLabel(Localization.Get(UI_Key.File_Associations_Instructions));
            this.Controls.Add(lblInstructions);
            lblInstructions.Location = new System.Drawing.Point(MARGIN, MARGIN);
            lblInstructions.SetWidth(this.Width - MARGIN - MARGIN);

            int x = MARGIN * 3;

            lblAudioFiles = new QLabel("Audio Files", Styles.FontBold);
            this.Controls.Add(lblAudioFiles);
            lblAudioFiles.Location = new System.Drawing.Point(MARGIN, lblInstructions.Bottom + MARGIN + MARGIN);

            chkMP3 = new QCheckBox(Localization.Get(UI_Key.File_Associations_MP3), this.BackColor);
            this.Controls.Add(chkMP3);
            chkMP3.Location = new System.Drawing.Point(x, lblAudioFiles.Bottom + MARGIN);

            chkWMA = new QCheckBox(Localization.Get(UI_Key.File_Associations_WMA), this.BackColor);
            this.Controls.Add(chkWMA);
            chkWMA.Location = new System.Drawing.Point(x, chkMP3.Bottom + MARGIN);

            chkOGG = new QCheckBox(Localization.Get(UI_Key.File_Associations_OGG), this.BackColor);
            this.Controls.Add(chkOGG);
            chkOGG.Location = new System.Drawing.Point(x, chkWMA.Bottom + MARGIN);

            chkFLAC = new QCheckBox(Localization.Get(UI_Key.File_Associations_FLAC), this.BackColor);
            this.Controls.Add(chkFLAC);
            chkFLAC.Location = new System.Drawing.Point(x, chkOGG.Bottom + MARGIN);

            chkM4A = new QCheckBox(Localization.Get(UI_Key.File_Associations_iTunes), this.BackColor);
            this.Controls.Add(chkM4A);
            chkM4A.Location = new System.Drawing.Point(x, chkFLAC.Bottom + MARGIN);

            chkWV = new QCheckBox(Localization.Get(UI_Key.File_Associations_WV), this.BackColor);
            this.Controls.Add(chkWV);
            chkWV.Location = new System.Drawing.Point(x, chkM4A.Bottom + MARGIN);

            chkAPE = new QCheckBox(Localization.Get(UI_Key.File_Associations_APE), this.BackColor);
            this.Controls.Add(chkAPE);
            chkAPE.Location = new System.Drawing.Point(x, chkWV.Bottom + MARGIN);

            lblPlaylistFiles = new QLabel("Playlist Files", Styles.FontBold);
            this.Controls.Add(lblPlaylistFiles);
            lblPlaylistFiles.Location = new System.Drawing.Point(MARGIN, chkAPE.Bottom + MARGIN + MARGIN);

            x = this.Width / 2;

            chkWAV = new QCheckBox(Localization.Get(UI_Key.File_Associations_WAV), this.BackColor);
            this.Controls.Add(chkWAV);
            chkWAV.Location = new System.Drawing.Point(x, chkMP3.Top);

            chkAC3 = new QCheckBox(Localization.Get(UI_Key.File_Associations_AC3), this.BackColor);
            this.Controls.Add(chkAC3);
            chkAC3.Location = new System.Drawing.Point(x, chkWAV.Bottom + MARGIN);

            chkMPC = new QCheckBox(Localization.Get(UI_Key.File_Associations_MPC), this.BackColor);
            this.Controls.Add(chkMPC);
            chkMPC.Location = new System.Drawing.Point(x, chkAC3.Bottom + MARGIN);

            chkALAC = new QCheckBox(Localization.Get(UI_Key.File_Associations_ALAC), this.BackColor);
            this.Controls.Add(chkALAC);
            chkALAC.Location = new System.Drawing.Point(x, chkMPC.Bottom + MARGIN);

            chkAIFF = new QCheckBox(Localization.Get(UI_Key.File_Associations_AIFF), this.BackColor);
            this.Controls.Add(chkAIFF);
            chkAIFF.Location = new System.Drawing.Point(x, chkALAC.Bottom + MARGIN);

            chkPLS = new QCheckBox(Localization.Get(UI_Key.File_Associations_PLS), this.BackColor);
            this.Controls.Add(chkPLS);
            chkPLS.Location = new System.Drawing.Point(chkWV.Left, lblPlaylistFiles.Bottom + MARGIN);

            chkM3U = new QCheckBox("M3U Files", this.BackColor);
            this.Controls.Add(chkM3U);
            chkM3U.Location = new System.Drawing.Point(chkAIFF.Left, chkPLS.Top);

            btnNone = new QButton(Localization.Get(UI_Key.File_Associations_Check_None), false, false);
            AddButton(btnNone, none);

            btnAll = new QButton(Localization.Get(UI_Key.File_Associations_Check_All), false, false);
            AddButton(btnAll, all);
            
            PlaceButtons(this.ClientRectangle.Width, chkPLS.Bottom + MARGIN, btnCancel, btnOK, btnNone, btnAll);

            this.ClientSize = new System.Drawing.Size(this.ClientRectangle.Width, btnCancel.Bottom + MARGIN);

            int tabIndex = 0;

            chkMP3.TabIndex = tabIndex++;
            chkWMA.TabIndex = tabIndex++;
            chkOGG.TabIndex = tabIndex++;
            chkFLAC.TabIndex = tabIndex++;
            chkM4A.TabIndex = tabIndex++;
            chkWV.TabIndex = tabIndex++;
            chkWAV.TabIndex = tabIndex++;
            chkAC3.TabIndex = tabIndex++;
            chkMPC.TabIndex = tabIndex++;
            chkALAC.TabIndex = tabIndex++;
            chkAIFF.TabIndex = tabIndex++;
            chkAPE.TabIndex = tabIndex++;
            chkPLS.TabIndex = tabIndex++;
            chkM3U.TabIndex = tabIndex++;

            btnAll.TabIndex = tabIndex++;
            btnNone.TabIndex = tabIndex++;
            btnOK.TabIndex = tabIndex++;
            btnCancel.TabIndex = tabIndex++;

            updateCheckboxes();
        }
        private void updateCheckboxes()
        {
            chkMP3.Checked = AssociationManager.IsAssociated(APP_PREFIX + MP3, Application.ExecutablePath, ".mp3");
            chkFLAC.Checked = AssociationManager.IsAssociated(APP_PREFIX + FLAC, Application.ExecutablePath, ".flac");
            chkOGG.Checked = AssociationManager.IsAssociated(APP_PREFIX + OGG, Application.ExecutablePath, ".ogg");
            chkWMA.Checked = AssociationManager.IsAssociated(APP_PREFIX + WMA, Application.ExecutablePath, ".wma");
            chkM4A.Checked = AssociationManager.IsAssociated(APP_PREFIX + AAC, Application.ExecutablePath, ".aac");
            chkWV.Checked = AssociationManager.IsAssociated(APP_PREFIX + WV, Application.ExecutablePath, ".wv");
            chkWAV.Checked = AssociationManager.IsAssociated(APP_PREFIX + WAV, Application.ExecutablePath, ".wav");
            chkAC3.Checked = AssociationManager.IsAssociated(APP_PREFIX + AC3, Application.ExecutablePath, ".ac3");
            chkMPC.Checked = AssociationManager.IsAssociated(APP_PREFIX + MPC, Application.ExecutablePath, ".mpc");
            chkALAC.Checked = AssociationManager.IsAssociated(APP_PREFIX + ALAC, Application.ExecutablePath, ".alac");
            chkAIFF.Checked = AssociationManager.IsAssociated(APP_PREFIX + AIFF, Application.ExecutablePath, ".aiff");
            chkAPE.Checked = AssociationManager.IsAssociated(APP_PREFIX + APE, Application.ExecutablePath, ".ape");
            chkPLS.Checked = AssociationManager.IsAssociated(APP_PREFIX + PLS, Application.ExecutablePath, ".pls");
            chkM3U.Checked = AssociationManager.IsAssociated(APP_PREFIX + M3U, Application.ExecutablePath, ".m3u");

            if (Environment.OSVersion.Version.Major >= 6) // vista
            {
                chkAC3.Enabled = !chkAC3.Checked;
                chkAIFF.Enabled = !chkAIFF.Checked;
                chkALAC.Enabled = !chkALAC.Checked;
                chkAPE.Enabled = !chkAPE.Checked;
                chkFLAC.Enabled = !chkFLAC.Checked;
                chkM4A.Enabled = !chkM4A.Checked;
                chkMP3.Enabled = !chkMP3.Checked;
                chkMPC.Enabled = !chkMPC.Checked;
                chkOGG.Enabled = !chkOGG.Checked;
                chkWAV.Enabled = !chkWAV.Checked;
                chkWMA.Enabled = !chkWMA.Checked;
                chkWV.Enabled = !chkWV.Checked;
                chkPLS.Enabled = !chkPLS.Checked;
                chkM3U.Enabled = !chkM3U.Checked;
            }
        }
        private void check(bool All)
        {
            if (Environment.OSVersion.Version.Major >= 6) // vista
            {
                chkMP3.Checked  = All | !chkMP3.Enabled;
                chkOGG.Checked  = All | !chkOGG.Enabled;
                chkFLAC.Checked = All | !chkFLAC.Enabled;
                chkWMA.Checked  = All | !chkWMA.Enabled;
                chkM4A.Checked  = All | !chkM4A.Enabled;
                chkWV.Checked   = All | !chkWV.Enabled;
                chkWAV.Checked  = All | !chkWAV.Enabled;
                chkAC3.Checked  = All | !chkAC3.Enabled;
                chkMPC.Checked  = All | !chkMPC.Enabled;
                chkALAC.Checked = All | !chkALAC.Enabled;
                chkAIFF.Checked = All | !chkAIFF.Enabled;
                chkAPE.Checked = All | !chkAPE.Enabled;
                chkPLS.Checked = All | !chkPLS.Enabled;
                chkM3U.Checked = All | !chkM3U.Enabled;
            }
            else
            {
                chkMP3.Checked  = All;
                chkOGG.Checked  = All;
                chkFLAC.Checked = All;
                chkWMA.Checked  = All;
                chkM4A.Checked  = All;
                chkWV.Checked   = All;
                chkWAV.Checked  = All;
                chkAC3.Checked  = All;
                chkMPC.Checked  = All;
                chkALAC.Checked = All;
                chkAPE.Checked = All;
                chkAIFF.Checked = All;
                chkPLS.Checked = All;
                chkM3U.Checked = All;
            }
        }
        protected override void ok()
        {
            foreach (Control c in this.Controls)
                c.Enabled = false;

            AssociationManager.Associate(chkMP3.Checked, APP_PREFIX + MP3, Application.ExecutablePath, ".mp3");
            AssociationManager.Associate(chkFLAC.Checked, APP_PREFIX + FLAC, Application.ExecutablePath, ".flac", ".fla");
            AssociationManager.Associate(chkOGG.Checked, APP_PREFIX + OGG, Application.ExecutablePath, ".ogg");
            AssociationManager.Associate(chkWMA.Checked, APP_PREFIX + WMA, Application.ExecutablePath, ".wma");
            AssociationManager.Associate(chkM4A.Checked, APP_PREFIX + AAC, Application.ExecutablePath, ".m4a", ".m4b", ".aac");
            AssociationManager.Associate(chkWV.Checked, APP_PREFIX + WV, Application.ExecutablePath, ".wv");
            AssociationManager.Associate(chkWAV.Checked, APP_PREFIX + WAV, Application.ExecutablePath, ".wav");
            AssociationManager.Associate(chkAC3.Checked, APP_PREFIX + AC3, Application.ExecutablePath, ".ac3");
            AssociationManager.Associate(chkMPC.Checked, APP_PREFIX + MPC, Application.ExecutablePath, ".mpc");
            AssociationManager.Associate(chkALAC.Checked, APP_PREFIX + ALAC, Application.ExecutablePath, ".alac");
            AssociationManager.Associate(chkAIFF.Checked, APP_PREFIX + AIFF, Application.ExecutablePath, ".aiff", ".aif");
            AssociationManager.Associate(chkAPE.Checked, APP_PREFIX + APE, Application.ExecutablePath, ".ape");

            AssociationManager.Associate(chkPLS.Checked, APP_PREFIX + PLS, Application.ExecutablePath, ".pls");
            AssociationManager.Associate(chkM3U.Checked, APP_PREFIX + M3U, Application.ExecutablePath, ".m3u");

            AssociationManager.NotifyShellOfChange();

            this.Close();
        }
        private void none()
        {
            check(false);
        }
        private void all()
        {
            check(true);
        }
    }
}
