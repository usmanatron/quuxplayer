/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal sealed partial class frmOptions : QFixedDialog
    {
        private Dictionary<QButton, List<Control>> tabs;

        private const int HPADDING2 = 20;
        private const int PADDING = 7;
        private const int VSPACING2 = 6;
        private int topMargin;

        private QLabel lblSound;
        private QLabel lblDisplay;
        private QLabel lblInternet;
        private QLabel lblMisc;

        private QCheckBox chkShowGridOnSpectrum;
        private QCheckBox chkDownloadCoverArt;
        private QCheckBox chkAutoClippingControl;
        private QCheckBox chkAutoCheckUpdates;
        private QCheckBox chkUseGlobalHotkeys;
        private QCheckBox chkVolumeControlsWindowsVolume;
        private QCheckBox chkDisableScreensavers;
        private QCheckBox chkDisableScreensaversOnlyWhenPlaying;
        private QCheckBox chkIncludeTagCloud;

        private QLabel lblSoundDevice;

        private QSpin spnShortTracks;
        private QLabel lblArtSave;
        private QComboBox cboArtSave;
        private QComboBox cboSoundDevice;

        private QCheckBox chkStopClearsNowPlaying;
        private QCheckBox chkSaveNowPlayingOnExit;

        private string requestedDeviceName = String.Empty;
        private string defaultDeviceName = String.Empty;
        private bool deviceWasSubstituted = false;

        private string[] asioDevices;

        public frmOptions() : base(Localization.Get(UI_Key.Options_Title, Application.ProductName), ButtonCreateType.OKAndCancel)
        {
            this.SPACING = 4;

            tabs = new Dictionary<QButton, List<Control>>();

            InitializeComponent();

            QButton button = new QButton("Primary Options", true, false);
            List<Control> controls = new List<Control>();
            tabs.Add(button, controls);
            button.Value = true;
            button.ButtonPressed += clickTab;

            topMargin = PADDING + button.Height + SPACING + SPACING;

            lblSound = new QLabel(Localization.Get(UI_Key.Options_Label_Sound), Styles.FontBold);
            controls.Add(lblSound);

            chkAutoClippingControl = new QCheckBox(Localization.Get(UI_Key.Options_Auto_Clipping_Control), this.BackColor);
            controls.Add(chkAutoClippingControl);

            lblSoundDevice = new QLabel("Specify a sound output device:");
            controls.Add(lblSoundDevice);

            cboSoundDevice = new QComboBox(false);
            cboSoundDevice.Items.AddRange(OutputDX.GetDeviceNames());
            cboSoundDevice.SelectedIndexChanged += new EventHandler(cboSoundDevice_SelectedIndexChanged);

            asioDevices = OutputASIO.GetDeviceNames().ToArray();
            cboSoundDevice.Items.AddRange(asioDevices);
            
            chkVolumeControlsWindowsVolume = new QCheckBox(Localization.Get(UI_Key.Options_Volume_Controls_Windows_Volume, Application.ProductName), this.BackColor);

            if (!Lib.IsVistaOrLater)
            {
                controls.Add(chkVolumeControlsWindowsVolume);
            }
            else
            {
                chkVolumeControlsWindowsVolume.Visible = false;
            }
            
            if (cboSoundDevice.Items.Count > 0)
            {
                cboSoundDevice.SelectedIndex = 0;
                cboSoundDevice.AutoSetWidth();
            }
            controls.Add(cboSoundDevice);

            lblDisplay = new QLabel(Localization.Get(UI_Key.Options_Label_Display), Styles.FontBold);
            controls.Add(lblDisplay);

            chkIncludeTagCloud = new QCheckBox(Localization.Get(UI_Key.Options_Include_Tag_Cloud), this.BackColor);
            controls.Add(chkIncludeTagCloud);

            chkDisableScreensavers = new QCheckBox(Localization.Get(UI_Key.Options_Disable_Screensaver), this.BackColor);
            controls.Add(chkDisableScreensavers);

            chkDisableScreensaversOnlyWhenPlaying = new QCheckBox("Only When Playing", this.BackColor);
            controls.Add(chkDisableScreensaversOnlyWhenPlaying);

            chkShowGridOnSpectrum = new QCheckBox(Localization.Get(UI_Key.Options_Spectrum_Show_Grid), this.BackColor);
            controls.Add(chkShowGridOnSpectrum);

            button = new QButton("Secondary Options", true, false);
            button.Value = false;
            button.ButtonPressed += clickTab;
            controls = new List<Control>();
            tabs.Add(button, controls);

            lblInternet = new QLabel(Localization.Get(UI_Key.Options_Label_Internet), Styles.FontBold);
            controls.Add(lblInternet);

            chkAutoCheckUpdates = new QCheckBox(Localization.Get(UI_Key.Options_Auto_Check_Updates), this.BackColor);
            controls.Add(chkAutoCheckUpdates);

            chkDownloadCoverArt = new QCheckBox(Localization.Get(UI_Key.Options_Download_Cover_Art), this.BackColor);
            controls.Add(chkDownloadCoverArt);

            lblArtSave = new QLabel(Localization.Get(UI_Key.Options_Save_Art_Caption));
            lblArtSave.Enabled = false;
            controls.Add(lblArtSave);

            cboArtSave = new QComboBox(false);
            cboArtSave.Items.Add(Localization.Get(UI_Key.Options_Save_Art_Folder_JPG));
            cboArtSave.Items.Add(Localization.Get(UI_Key.Options_Save_Art_Artist_Album));
            cboArtSave.Items.Add(Localization.Get(UI_Key.Options_Save_Art_None));
            cboArtSave.AutoSetWidth();
            cboArtSave.Enabled = false;
            controls.Add(cboArtSave);

            lblMisc = new QLabel(Localization.Get(UI_Key.Options_Label_Other), Styles.FontBold);
            controls.Add(lblMisc);

            chkUseGlobalHotkeys = new QCheckBox(Localization.Get(UI_Key.Options_Use_Global_Hotkeys), this.BackColor);
            controls.Add(chkUseGlobalHotkeys);

            spnShortTracks = new QSpin(true,
                                       true,
                                       Localization.Get(UI_Key.Options_Dont_Load_Shorter_Than),
                                       Localization.Get(UI_Key.Options_Dont_Load_Seconds),
                                       1,
                                       60,
                                       10,
                                       5,
                                       this.BackColor);

            spnShortTracks.OffEquivalent = 0;
            controls.Add(spnShortTracks);

            chkDownloadCoverArt.CheckedChanged += (s, e) =>
            {
                lblArtSave.Enabled = chkDownloadCoverArt.Checked;
                cboArtSave.Enabled = chkDownloadCoverArt.Checked;
            };

            chkStopClearsNowPlaying = new QCheckBox("Stop clears Now Playing", this.BackColor);
            controls.Add(chkStopClearsNowPlaying);

            chkSaveNowPlayingOnExit = new QCheckBox("Save Now Playing on exit", this.BackColor);
            controls.Add(chkSaveNowPlayingOnExit);

            bool isFirst = true;
            int tabIndex = 0;

            foreach (KeyValuePair<QButton, List<Control>> kvp in tabs)
            {
                this.Controls.Add(kvp.Key);
                kvp.Key.TabIndex = tabIndex++;
                foreach (Control c in kvp.Value)
                {
                    c.Visible = isFirst;
                    this.Controls.Add(c);
                }
                isFirst = false;
            }

            cboSoundDevice.TabIndex = tabIndex++;

            if (!Lib.IsVistaOrLater)
                chkVolumeControlsWindowsVolume.TabIndex = tabIndex++;

            chkAutoClippingControl.TabIndex = tabIndex++;

            chkIncludeTagCloud.TabIndex = tabIndex++;
            chkDisableScreensavers.TabIndex = tabIndex++;
            chkDisableScreensaversOnlyWhenPlaying.TabIndex = tabIndex++;
            chkShowGridOnSpectrum.TabIndex = tabIndex++;            
            chkAutoCheckUpdates.TabIndex = tabIndex++;
            chkDownloadCoverArt.TabIndex = tabIndex++;
            cboArtSave.TabIndex = tabIndex++;
            
            chkUseGlobalHotkeys.TabIndex = tabIndex++;
            chkStopClearsNowPlaying.TabIndex = tabIndex++;
            chkSaveNowPlayingOnExit.TabIndex = tabIndex++;
            spnShortTracks.TabIndex = tabIndex++;

            btnOK.TabIndex = tabIndex++;
            btnCancel.TabIndex = tabIndex++;

            arrangeLayout();
        }

        private void cboSoundDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateVolumeOptions();
        }
        private void clickTab(QButton Button)
        {
            foreach (KeyValuePair<QButton, List<Control>> kvp in tabs)
            {
                kvp.Key.Value = Button.Equals(kvp.Key);

                foreach (Control c in kvp.Value)
                    c.Visible = kvp.Key.Value;
            }
        }
        private void updateVolumeOptions()
        {
            if (asioDevices.Contains(cboSoundDevice.Text))
            {
                chkVolumeControlsWindowsVolume.Checked = false;
                chkVolumeControlsWindowsVolume.Enabled = false;
            }
            else if (Lib.IsVistaOrLater)
            {
                System.Diagnostics.Debug.Assert(!chkVolumeControlsWindowsVolume.Visible);
                chkVolumeControlsWindowsVolume.Checked = true;
            }
            else
            {
                if (chkVolumeControlsWindowsVolume.Enabled == false && !chkVolumeControlsWindowsVolume.Checked)
                {
                    // probably disabled b/c of asio, assume we want this checked
                    chkVolumeControlsWindowsVolume.Checked = true;
                }
                chkVolumeControlsWindowsVolume.Enabled = true;
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.DrawLine(Styles.FilterButtonHasValuePen, PADDING, topMargin - SPACING, this.ClientRectangle.Width - PADDING - PADDING, topMargin - SPACING);
        }
        public string OutputDeviceName
        {
            get
            {
                string val = cboSoundDevice.Text;
                if (deviceWasSubstituted && val == defaultDeviceName)
                    return requestedDeviceName;
                else
                    return cboSoundDevice.Text;
            }
            set
            {
                int index = cboSoundDevice.FindStringExact(value);
                if (index >= 0)
                {
                    cboSoundDevice.SelectedIndex = cboSoundDevice.FindStringExact(value);
                }
                else
                {
                    defaultDeviceName = OutputDX.GetDefaultDeviceName();
                    requestedDeviceName = value;
                    deviceWasSubstituted = true;

                    if (!cboSoundDevice.Items.Contains(defaultDeviceName))
                        cboSoundDevice.Items.Insert(0, defaultDeviceName);
                    
                    cboSoundDevice.SelectedIndex = cboSoundDevice.FindStringExact(defaultDeviceName);
                }
                cboSoundDevice.Width = this.ClientRectangle.Width - cboSoundDevice.Left - PADDING;
            }
        }
        public bool ShowGridOnSpectrum
        {
            get { return chkShowGridOnSpectrum.Checked; }
            set { chkShowGridOnSpectrum.Checked = value; }
        }
        public bool DownloadCovertArt
        {
            get { return chkDownloadCoverArt.Checked; }
            set { chkDownloadCoverArt.Checked = value; }
        }
        public bool AutoClippingControl
        {
            get { return chkAutoClippingControl.Checked; }
            set { chkAutoClippingControl.Checked = value; }
        }
        public bool AutoCheckForUpdates
        {
            get { return chkAutoCheckUpdates.Checked; }
            set { chkAutoCheckUpdates.Checked = value; }
        }
        public bool UseGlobalHotKeys
        {
            get { return chkUseGlobalHotkeys.Checked; }
            set { chkUseGlobalHotkeys.Checked = value; }
        }
        public bool IncludeTagCloud
        {
            get { return chkIncludeTagCloud.Checked; }
            set { chkIncludeTagCloud.Checked = value; }
        }
        public bool DisableScreensaver
        {
            get { return chkDisableScreensavers.Checked; }
            set { chkDisableScreensavers.Checked = value; }
        }
        public bool DisableScreensaverOnlyWhenPlaying
        {
            get { return chkDisableScreensaversOnlyWhenPlaying.Checked; }
            set { chkDisableScreensaversOnlyWhenPlaying.Checked = value; }
        }
        public bool LocalVolumeControl
        {
            get
            {
                return !chkVolumeControlsWindowsVolume.Checked;
            }
            set
            {
                if (chkVolumeControlsWindowsVolume.Enabled)
                    chkVolumeControlsWindowsVolume.Checked = !value;
            }
        }
        public bool StopClearsNowPlaying
        {
            get { return chkStopClearsNowPlaying.Checked; }
            set { chkStopClearsNowPlaying.Checked = value; }
        }
        public bool SaveNowPlayingOnExit
        {
            get { return chkSaveNowPlayingOnExit.Checked; }
            set { chkSaveNowPlayingOnExit.Checked = value; }
        }
        public int ShortTrackCutoff
        {
            get { return spnShortTracks.Checked ? spnShortTracks.Value : 0; }
            set
            {
                spnShortTracks.Value = value;
            }
        }
        public ArtSaveOption ArtSaveOption
        {
            get
            {
                string val = cboArtSave.Items[cboArtSave.SelectedIndex].ToString();
                if (val == Localization.Get(UI_Key.Options_Save_Art_Folder_JPG))
                    return ArtSaveOption.Folder_JPG;
                else if (val == Localization.Get(UI_Key.Options_Save_Art_Artist_Album))
                    return ArtSaveOption.Artist_Album;
                else 
                    return ArtSaveOption.None;
            }
            set
            {
                switch (value)
                {
                    case ArtSaveOption.Folder_JPG:
                        cboArtSave.SelectedIndex = cboArtSave.FindStringExact(Localization.Get(UI_Key.Options_Save_Art_Folder_JPG));
                        break;
                    case ArtSaveOption.Artist_Album:
                        cboArtSave.SelectedIndex = cboArtSave.FindStringExact(Localization.Get(UI_Key.Options_Save_Art_Artist_Album));
                        break;
                    default:
                        cboArtSave.SelectedIndex = cboArtSave.FindStringExact(Localization.Get(UI_Key.Options_Save_Art_None));
                        break;
                }
            }
        }

        private void arrangeLayout()
        {
            int ypos = PADDING;
            int yposMax = 0;

            int x = PADDING;

            foreach (KeyValuePair<QButton, List<Control>> kvp in tabs)
            {
                kvp.Key.Location = new Point(x, ypos);
                x = kvp.Key.Right + SPACING;
            }

            ypos = topMargin;

            lblSound.Location = new Point(PADDING, ypos);
            ypos = lblSound.Bottom + SPACING;

            lblSoundDevice.Location = new Point(HPADDING2, ypos);
            cboSoundDevice.Location = new Point(lblSoundDevice.Right + 5, lblSoundDevice.Top + lblSoundDevice.Height / 2 - cboSoundDevice.Height / 2);
            ypos = cboSoundDevice.Bottom + VSPACING2;

            if (!Lib.IsVistaOrLater)
            {
                chkVolumeControlsWindowsVolume.Location = new Point(HPADDING2, ypos);
                ypos = chkVolumeControlsWindowsVolume.Bottom + SPACING;
            }

            chkAutoClippingControl.Location = new Point(HPADDING2, ypos);
            ypos = chkAutoClippingControl.Bottom + SPACING;

            lblDisplay.Location = new Point(PADDING, ypos);
            ypos = lblDisplay.Bottom + SPACING;

            chkIncludeTagCloud.Location = new Point(HPADDING2, ypos);
            ypos = chkIncludeTagCloud.Bottom + SPACING;

            chkDisableScreensavers.Location = new Point(HPADDING2, ypos);
            ypos = chkDisableScreensavers.Bottom + SPACING;

            chkDisableScreensaversOnlyWhenPlaying.Location = new Point(HPADDING2 * 2, ypos);
            ypos = chkDisableScreensaversOnlyWhenPlaying.Bottom + SPACING;

            chkShowGridOnSpectrum.Location = new Point(HPADDING2, ypos);
            ypos = chkShowGridOnSpectrum.Bottom + VSPACING2;

            yposMax = Math.Max(yposMax, ypos);
            ypos = topMargin;

            lblInternet.Location = new Point(PADDING, ypos);
            ypos = lblInternet.Bottom + SPACING;

            chkAutoCheckUpdates.Location = new Point(HPADDING2, ypos);
            ypos = chkAutoCheckUpdates.Bottom + SPACING;

            chkDownloadCoverArt.Location = new Point(HPADDING2, ypos);
            ypos = chkDownloadCoverArt.Bottom + SPACING;

            lblArtSave.Location = new Point(2 * HPADDING2, ypos);
            cboArtSave.Location = new Point(lblArtSave.Right + 5, lblArtSave.Top + lblArtSave.Height / 2 - cboArtSave.Height / 2);
            ypos = cboArtSave.Bottom + VSPACING2;

            lblMisc.Location = new Point(PADDING, ypos);
            ypos = lblMisc.Bottom + SPACING;
            
            chkUseGlobalHotkeys.Location = new Point(HPADDING2, ypos);
            ypos = chkUseGlobalHotkeys.Bottom + SPACING;
            
            chkStopClearsNowPlaying.Location = new Point(HPADDING2, ypos);
            chkStopClearsNowPlaying.Visible = false;
            ypos = chkStopClearsNowPlaying.Bottom + SPACING;

            chkSaveNowPlayingOnExit.Location = new Point(HPADDING2, ypos);
            chkSaveNowPlayingOnExit.Visible = false;
            ypos = chkSaveNowPlayingOnExit.Bottom + SPACING;

            spnShortTracks.Location = new Point(HPADDING2, ypos);
            ypos = spnShortTracks.Bottom + SPACING;

            yposMax = Math.Max(yposMax, ypos);

            this.Size = new Size(cboArtSave.Right + PADDING + 4,
                                 this.Height - this.ClientRectangle.Height + yposMax + SPACING + btnOK.Height);

            PlaceButtons(this.ClientRectangle.Width, this.ClientRectangle.Height - btnCancel.Height - MARGIN);
        }
    }
}
