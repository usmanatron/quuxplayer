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

    internal sealed class Equalizer : Control, IMainView
    {
        private static Equalizer instance = null;

        public delegate void EqualizerChanged();

        public const int MAX_NUM_BANDS = 30;
        private static int buttonSpacing = 3;
        private const int BUTTON_TOPS = 8;

        private int fullButtonsWidth = 0;

        private static readonly string[] freqMarkers30 = new string[] { "20", "25", "32", "40", "50", "63",
                                                                      "80", "100", "125", "160", "200", "250",
                                                                      "320", "400", "500", "630", "800", "1000",
                                                                      "1250", "1600", "2000", "2500", "3200",
                                                                      "4000", "5000", "6300", "8000", "10000",
                                                                      "12500", "16000" };

        private static readonly string[] freqMarkers10 = new string[] { "32Hz", "63Hz", "125Hz", "250Hz", "500Hz",
                                                                        "1000Hz", "2000Hz", "4000Hz", "8000Hz", "16000Hz" };

        private string[] freqMarkers;
        private Rectangle[] freqRects = new Rectangle[MAX_NUM_BANDS];

        private int numBands = 2;

        public static readonly string DEFAULT_EQ_NAME = Localization.Get(UI_Key.Equalizer_Flat);

        public event EqualizerChanged EqChanged;
        public event EqualizerChanged EqToggleOnOff;
        public event EqualizerChanged EqChangePreset;

        private static Controller controller;

        private const int LEFT_MARGIN = 50;
        private const int HORIZ_MARGIN = 15;
        private const int BOTTOM_MARGIN = 40;
        private const int TOP_MARGIN = 60;
        private const int SCALE = 200;
        private const int LARGE_CHANGE = SCALE / 6;
        private const float SCALE_FLOAT = 200.0f;

        private float scrollFactor;
        private bool on = false;
        private EqualizerSetting _currentEqualizer;

        private QButton btnReset;
        private QButton btnOn;
        private QButton btnFineControl;
        private QButton btnAllTogether;
        private QButton btnLockPreset;
        private QButton btnRemovePreset;
        private QButton btnNewPreset;
        private QButton btnExpand;
        private QButton btnCompress;
        private QButton btnNumBands;
        private QComboBox cboName;
        
        private ulong eqChangeTimer = Clock.NULL_ALARM;
        private QScrollBar[] scrollBars;
        private static Dictionary<string, EqualizerSetting> equalizers;
        private int zeroLine;
        
        public Equalizer()
        {
            instance = this;

            this.DoubleBuffered = true;

            _currentEqualizer = EqualizerSetting.Off;

            scrollBars = new QScrollBar[MAX_NUM_BANDS];

            for (int i = 0; i < MAX_NUM_BANDS; i++)
            {
                QScrollBar qsb = new QScrollBar(false);
                qsb.Max = SCALE;
                qsb.Min = -SCALE;
                qsb.LargeChange = LARGE_CHANGE; 
                qsb.Value = 0;
                qsb.Brightness = QScrollBar.SBBrightness.Dim;
                qsb.UserScroll += new QScrollBar.ScrollDelegate(scroll);
                this.Controls.Add(qsb);
                qsb.Tag = i;
                scrollBars[i] = qsb;
            }

            btnOn = new QButton(Localization.Get(UI_Key.Equalizer_Eq_Off), true, false);
            btnOn.BackColor = Color.Black;

            cboName = new QComboBox(true);
            cboName.Location = new Point(HORIZ_MARGIN, BUTTON_TOPS + btnOn.Height / 2 - cboName.Height / 2);
            cboName.DropDownStyle = ComboBoxStyle.DropDownList;
            cboName.SelectedValueChanged += new EventHandler(cboName_SelectedValueChanged);
            this.Controls.Add(cboName);

            btnOn.ButtonPressed += new QButton.ButtonDelegate(turnOn);
            this.Controls.Add(btnOn);

            btnLockPreset = new QButton(Localization.Get(UI_Key.Equalizer_Lock), true, false);
            btnLockPreset.BackColor = Color.Black;
            btnLockPreset.ButtonPressed += new QButton.ButtonDelegate(btnLockPreset_ButtonPressed);
            this.Controls.Add(btnLockPreset);

            btnFineControl = new QButton(Localization.Get(UI_Key.Equalizer_Fine_Control), true, false);
            btnFineControl.BackColor = Color.Black;
            btnFineControl.Value = false;
            btnFineControl.ButtonPressed += (s) => { if (btnFineControl.Value) btnAllTogether.Value = false; };
            this.Controls.Add(btnFineControl);

            btnAllTogether = new QButton(Localization.Get(UI_Key.Equalizer_All_Together), true, false);
            btnAllTogether.BackColor = Color.Black;
            btnAllTogether.Value = false;
            btnAllTogether.ButtonPressed += (s) => { if (btnAllTogether.Value) btnFineControl.Value = false; };
            this.Controls.Add(btnAllTogether);

            btnExpand = new QButton(Localization.Get(UI_Key.Equalizer_Expand), false, false);
            btnExpand.BackColor = Color.Black;
            btnExpand.ButtonPressed += new QButton.ButtonDelegate(expand);
            this.Controls.Add(btnExpand);

            btnCompress = new QButton(Localization.Get(UI_Key.Equalizer_Compress), false, false);
            btnCompress.BackColor = Color.Black;
            btnCompress.ButtonPressed += new QButton.ButtonDelegate(compress);
            this.Controls.Add(btnCompress);

            btnReset = new QButton(Localization.Get(UI_Key.Equalizer_Reset), false, false);
            btnReset.BackColor = Color.Black;
            btnReset.ButtonPressed += new QButton.ButtonDelegate(reset);
            this.Controls.Add(btnReset);

            btnRemovePreset = new QButton(Localization.Get(UI_Key.Equalizer_Remove), false, false);
            btnRemovePreset.BackColor = Color.Black;
            btnRemovePreset.ButtonPressed += new QButton.ButtonDelegate(btnRemovePreset_ButtonPressed);
            this.Controls.Add(btnRemovePreset);
            
            btnNewPreset = new QButton(Localization.Get(UI_Key.Equalizer_New), false, false);
            btnNewPreset.BackColor = Color.Black;
            btnNewPreset.ButtonPressed += new QButton.ButtonDelegate(btnNewPreset_ButtonPressed);
            this.Controls.Add(btnNewPreset);

            btnNumBands = new QButton(Localization.Get(UI_Key.Equalizer_Bands, "10"), false, false);
            btnNumBands.BackColor = Color.Black;
            btnNumBands.ButtonPressed += new QButton.ButtonDelegate(btnNumBands_ButtonPressed);
            this.Controls.Add(btnNumBands);

            fullButtonsWidth = getButtonsWidth();
        }
        public ViewType ViewType { get { return ViewType.Equalizer; } }
        static Equalizer()
        {
            controller = Controller.GetInstance();
        }

        public int NumBands
        {
            get { return numBands; }
            set
            {
                if (numBands != value)
                {
                    numBands = value;
                    btnNumBands.Text = Localization.Get(UI_Key.Equalizer_Bands, numBands.ToString());
                    freqMarkers = (numBands == 10) ? freqMarkers10 : freqMarkers30;
                    scrollFactor = 0.5f + (float)numBands / 100.0f;
                    updateForEqualizerChange();
                    setMetrics();
                }
            }
        }
        public void SetEqualizer(string Name)
        {
            if (equalizers.ContainsKey(Name))
                CurrentEqualizer = equalizers[Name];
        }
        public EqualizerSetting CurrentEqualizer
        {
            get { return _currentEqualizer; }
            set
            {
                if (value.IsOff)
                {
                    this.On = false;
                }
                else if (!_currentEqualizer.Equals(value))
                {
                    _currentEqualizer = value;
                    updateScrollBars();
                    cboName.Text = _currentEqualizer.Name;
                }
            }
        }
        public float[] ValueDB
        {
            get
            {
                float[] d = new float[numBands];
                for (int i = 0; i < numBands; i++)
                {
                    d[i] = (12.0f * (-scrollBars[i].Value) / SCALE_FLOAT);
                }
                return d;
            }
        }
        public static Equalizer GetInstance()
        {
            return instance;
        }
        public static List<EqualizerSetting> GetEqualizerSettings()
        {
            return equalizers.Values.ToList();
        }
        public List<EqualizerSetting> EqualizerSettings
        {
            get
            {
                return equalizers.Values.ToList();
            }
            set
            {
                equalizers = new Dictionary<string, EqualizerSetting>(StringComparer.OrdinalIgnoreCase);
                foreach (EqualizerSetting es in value)
                {
                    if (!equalizers.ContainsKey(es.Name))
                        equalizers.Add(es.Name, es);
                }

                populateEqualizerList();

                if (!equalizers.ContainsKey(DEFAULT_EQ_NAME))
                {
                    equalizers.Add(DEFAULT_EQ_NAME, new EqualizerSetting(DEFAULT_EQ_NAME, new float[MAX_NUM_BANDS], false));
                }
                CurrentEqualizer = equalizers[DEFAULT_EQ_NAME];
            }
        }
        public bool On
        {
            get { return on; }
            set
            {
                if (on != value)
                {
                    on = value;
                    btnOn.Value = value;
                    btnOn.Text = on ? Localization.Get(UI_Key.Equalizer_Eq_On) : Localization.Get(UI_Key.Equalizer_Eq_Off);

                    foreach (QScrollBar sb in scrollBars)
                        sb.Brightness = (on ? QScrollBar.SBBrightness.Bright : QScrollBar.SBBrightness.Dim);

                    EqToggleOnOff.Invoke();
                    EqChangePreset.Invoke();
                }
            }
        }
        public bool FineControl
        {
            get { return btnFineControl.Value; }
            set { btnFineControl.Value = value; }
        }

        public static List<EqualizerSetting> DefaultEqualizerSettings
        {
            get
            {
                List<EqualizerSetting> equalizers = new List<EqualizerSetting>();

                equalizers.Add(new EqualizerSetting(Equalizer.DEFAULT_EQ_NAME, new float[Equalizer.MAX_NUM_BANDS], false));

                equalizers.Add(new EqualizerSetting(Localization.Get(UI_Key.Equalizer_Loudness), new float[] { -41.474f, -54.884f, -61.938f, -67.505f, -65.000f, -62.452f, -46.303f, -31.570f, -17.578f, -04.306f, 07.697f, 17.989f, 26.298f, 32.476f, 36.449f, 38.171f, 37.597f, 34.662f, 29.270f, 21.296f, 10.608f, -02.790f, -18.282f, -33.702f, -44.804f, -47.692f, -44.712f, -42.151f, -34.782f, -16.656f }, true));
                equalizers.Add(new EqualizerSetting(Localization.Get(UI_Key.Equalizer_Bass_Boost), new float[] { -68.732f, -83.877f, -91.421f, -96.585f, -92.528f, -87.193f, -67.632f, -49.955f, -33.570f, -18.685f, -06.025f, 03.881f, 10.776f, 14.637f, 15.645f, 14.186f, 10.861f, 06.505f, 02.164f, -01.050f, -02.374f, -01.941f, -00.778f, 00.165f, 00.776f, 01.376f, 02.056f, 02.654f, 02.764f, 01.450f }, true));
                equalizers.Add(new EqualizerSetting(Localization.Get(UI_Key.Equalizer_Treble_Boost), new float[] { 00.947f, -03.429f, -04.396f, -04.128f, -03.403f, -02.487f, -01.766f, -01.676f, -02.223f, -02.749f, -02.326f, -00.430f, 02.719f, 06.310f, 09.306f, 10.727f, 09.803f, 06.011f, -00.915f, -10.961f, -23.744f, -38.261f, -52.613f, -64.420f, -72.530f, -78.242f, -83.581f, -88.646f, -90.753f, -86.506f }, true));
                equalizers.Add(new EqualizerSetting(Localization.Get(UI_Key.Equalizer_Vocal), new float[] { 78.975f, 70.034f, 64.084f, 56.056f, 48.208f, 40.042f, 29.437f, 16.106f, 02.114f, -10.475f, -20.925f, -29.280f, -35.789f, -40.878f, -45.103f, -48.768f, -51.412f, -51.546f, -47.527f, -38.660f, -25.792f, -10.372f, 06.266f, 23.030f, 39.017f, 53.329f, 65.209f, 74.991f, 85.852f, 109.000f }, true));

                return equalizers;
            }
        }

        public void SelectNextEqualizer()
        {
            int eqIndex = -1;

            List<EqualizerSetting> es = equalizers.Values.ToList();

            es.Sort();

            eqIndex = es.IndexOf(CurrentEqualizer);
            
            if (eqIndex == -1)
                eqIndex = 0;

            if (eqIndex >= 0)
            {
                eqIndex = (eqIndex + 1) % es.Count;
                CurrentEqualizer = es[eqIndex];
            }
        }
        public void CompressTo10()
        {
            if (NumBands == 30)
            {
                switchNumBands(10);
            }
        }
        
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            setMetrics();
            layoutButtons();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);

            int numDivs = 12;
            float inc = ((float)(this.ClientRectangle.Height - TOP_MARGIN - BOTTOM_MARGIN)) / ((float)((numDivs * 2) + 2));
            float yDiff = inc;

            for (int i = 0; i < numDivs; i++)
            {
                e.Graphics.DrawLine(Styles.DarkBorderPen, LEFT_MARGIN, zeroLine - yDiff, this.ClientRectangle.Width, zeroLine - yDiff);
                e.Graphics.DrawLine(Styles.DarkBorderPen, LEFT_MARGIN, zeroLine + yDiff, this.ClientRectangle.Width, zeroLine + yDiff);
                yDiff += inc;
            }

            e.Graphics.DrawLine(Styles.ThickPen, LEFT_MARGIN, zeroLine, this.ClientRectangle.Width, zeroLine);

            int inc2 = (this.ClientRectangle.Height - TOP_MARGIN - BOTTOM_MARGIN - Styles.TextHeight - 27) / 4;
            int center = zeroLine - Styles.TextHeight / 2;

            TextRenderer.DrawText(e.Graphics, "+12dB", Styles.Font, new Point(5, center - inc2 - inc2), Styles.LightText);
            TextRenderer.DrawText(e.Graphics, " +6dB", Styles.Font, new Point(5, center - inc2), Styles.LightText);
            TextRenderer.DrawText(e.Graphics, "  0dB", Styles.Font, new Point(5, center), Styles.LightText);
            TextRenderer.DrawText(e.Graphics, " -6dB", Styles.Font, new Point(5, center + inc2), Styles.LightText);
            TextRenderer.DrawText(e.Graphics, "-12dB", Styles.Font, new Point(5, center + inc2 + inc2), Styles.LightText);

            for (int i = 0; i < numBands; i++)
            {
                TextRenderer.DrawText(e.Graphics, freqMarkers[i], Styles.FontSmall, freqRects[i], Styles.LightText, TextFormatFlags.HorizontalCenter);
            }

        }

        private static float[] expandBands(float[] Values)
        {
            float[] f = new float[30];
            for (int i = 0; i < 10; i++)
            {
                f[i * 3] = Values[i];
                f[i * 3 + 1] = Values[i];
                f[i * 3 + 2] = Values[i];
            }
            for (int i = 2; i < 28; i += 3)
            {
                f[i] = f[i - 1] * 2f / 3f + f[i + 2] * 1f / 3f;
                f[i + 1] = f[i - 1] * 1f / 3f + f[i + 2] * 2f / 3f;
                f[0] = 2 * f[1] - f[2];
                f[29] = 2 * f[28] - f[27];
            }
            return f;
        }
        private static float[] compressBands(float[] Values)
        {
            float[] f = new float[30];
            for (int i = 0; i < 30; i += 3)
            {
                float avg = (Values[i] + Values[i + 1] + Values[i + 2]) / 3;
                f[i / 3] = avg;
            }
            return f;
        }

        private void btnRemovePreset_ButtonPressed(QButton Button)
        {
            if (cboName.Text != DEFAULT_EQ_NAME)
            {
                if (QMessageBox.Show(this,
                                     Localization.Get(UI_Key.Dialog_Equalizer_Remove_Preset, cboName.Text),
                                     Localization.Get(UI_Key.Dialog_Equalizer_Remove_Preset_Title),
                                     QMessageBoxButtons.OKCancel,
                                     QMessageBoxIcon.Question,
                                     QMessageBoxButton.NoCancel)
                                        == DialogResult.OK)
                {
                    string s = cboName.Text;

                    cboName.SelectedItem = DEFAULT_EQ_NAME;
                    cboName.Items.Remove(s);
                    if (equalizers.ContainsKey(s))
                        equalizers.Remove(s);
                }
            }
        }
        private void cboName_SelectedValueChanged(object sender, EventArgs e)
        {
            if (equalizers.ContainsKey(cboName.Text))
            {
                CurrentEqualizer = equalizers[cboName.Text];
            }
            if (EqChangePreset != null)
                EqChangePreset();

            if (EqChanged != null)
                EqChanged();
        }
        private void btnNewPreset_ButtonPressed(QButton Button)
        {
            QInputBox ib = new QInputBox(this,
                                         Localization.Get(UI_Key.Dialog_Equalizer_New_Preset),
                                         Localization.Get(UI_Key.Dialog_Equalizer_New_Preset_Title),
                                         String.Empty,
                                         24,
                                         1);

            if (ib.DialogResult == DialogResult.OK)
            {
                if (equalizers.ContainsKey(ib.Value))
                {
                    QMessageBox.Show(this,
                                     Localization.Get(UI_Key.Dialog_Equalizer_Duplicate_Preset, ib.Value),
                                     Localization.Get(UI_Key.Dialog_Equalizer_Duplicate_Preset_Title),
                                     QMessageBoxIcon.Error);
                }
                else
                {
                    float[] d = new float[MAX_NUM_BANDS];
                    Array.Copy(CurrentEqualizer.Values, d, MAX_NUM_BANDS);
                    EqualizerSetting es = new EqualizerSetting(ib.Value, d, false);
                    equalizers.Add(ib.Value, es);
                    populateEqualizerList();
                    CurrentEqualizer = es;
                }
            }
        }
        private void btnLockPreset_ButtonPressed(QButton Button)
        {
            CurrentEqualizer.Locked = !CurrentEqualizer.Locked;
            updateForEqualizerChange();
        }
        private void btnNumBands_ButtonPressed(QButton Button)
        {
            switchNumBands((numBands == 10) ? 30 : 10);
            EqChanged.Invoke();
        }

        private void switchNumBands(int Bands)
        {
            if (Bands == 30)
            {
                if (numBands != 30)
                {
                    foreach (KeyValuePair<string, EqualizerSetting> kvp in equalizers)
                    {
                        kvp.Value.Values = expandBands(kvp.Value.Values);
                    }
                    this.NumBands = 30;
                }
            }
            else
            {
                if (numBands != 10)
                {
                    foreach (KeyValuePair<string, EqualizerSetting> kvp in equalizers)
                    {
                        kvp.Value.Values = compressBands(kvp.Value.Values);
                    }
                    this.NumBands = 10;
                }
            }
        }
        private void setMetrics()
        {
            int bandPixWidth = Math.Max(QScrollBar.MIN_WIDTH, this.ClientRectangle.Width / 2 / numBands);

            float spacing = (this.ClientRectangle.Width - HORIZ_MARGIN - HORIZ_MARGIN - LEFT_MARGIN - bandPixWidth) / (numBands - 1);

            int freqRectTop = this.ClientRectangle.Height - BOTTOM_MARGIN + 10;

            for (int i = 0; i < numBands; i++)
            {
                QScrollBar s = scrollBars[i];
                s.Bounds = new Rectangle((int)((float)i * spacing) + HORIZ_MARGIN + LEFT_MARGIN,
                                         TOP_MARGIN,
                                         bandPixWidth,
                                         this.ClientRectangle.Height - TOP_MARGIN - BOTTOM_MARGIN);

                int w = TextRenderer.MeasureText(freqMarkers[i], Styles.FontSmall).Width;
                freqRects[i] = new Rectangle(s.Left + s.Width / 2 - w / 2, freqRectTop, w, 30);
                scrollBars[i].Visible = true;
            }
            for (int i = numBands; i < MAX_NUM_BANDS; i++)
            {
                scrollBars[i].Visible = false;
            }

            zeroLine = TOP_MARGIN + (this.ClientRectangle.Height - TOP_MARGIN - BOTTOM_MARGIN) / 2;
            this.Invalidate();
        }
        private void updateForEqualizerChange()
        {
            cboName.SelectedItem = CurrentEqualizer.Name;

            bool enabled = !CurrentEqualizer.Locked;

            btnLockPreset.Value = CurrentEqualizer.Locked;

            btnFineControl.Enabled = enabled;
            btnAllTogether.Enabled = enabled;
            btnCompress.Enabled = enabled;
            btnExpand.Enabled = enabled;
            btnReset.Enabled = enabled;
            btnRemovePreset.Enabled = enabled;
            btnNumBands.Enabled = enabled;

            for (int i = 0; i < numBands; i++)
                scrollBars[i].Enabled = enabled;

            updateScrollBars();
        }
        private void populateEqualizerList()
        {
            cboName.Items.Clear();

            List<string> keys = new List<string>();

            foreach (KeyValuePair<string, EqualizerSetting> kvp in equalizers)
                keys.Add(kvp.Key);

            keys.Sort();

            cboName.Items.AddRange(keys.ToArray());

            cboName.SelectedItem = CurrentEqualizer.Name;
        }
        private void updateScrollBars()
        {
            for (int i = 0; i < numBands; i++)
                scrollBars[i].Value = (int)CurrentEqualizer.Values[i];

            this.Invalidate();
        }
        private void scroll(QScrollBar Sender, int Value)
        {

            int band = (int)(Sender.Tag);

            if (btnFineControl.Value)
            {
                CurrentEqualizer.Values[band] = Value;
            }
            else if (btnAllTogether.Value)
            {
                float oldVal = CurrentEqualizer.Values[band];
                float newVal = Sender.Value;
                float diff = newVal - oldVal;

                for (int i = 0; i <numBands; i++)
                {
                    CurrentEqualizer.Values[i] = Math.Max(-SCALE_FLOAT, Math.Min(SCALE_FLOAT, CurrentEqualizer.Values[i] + diff));
                    scrollBars[i].Value = (int)CurrentEqualizer.Values[i];
                }
            }
            else
            {
                float oldVal = CurrentEqualizer.Values[band];

                CurrentEqualizer.Values[band] = Value;

                float newVal = Value;
                float factor = Math.Min(scrollFactor, Math.Abs(4.0f * (newVal - oldVal) / SCALE_FLOAT));

                for (int i = 1; i < numBands; i++)
                {
                    if (band + i < numBands)
                    {
                        CurrentEqualizer.Values[band + i] += ((((CurrentEqualizer.Values[band + i - 1] + newVal) / 2) - CurrentEqualizer.Values[band + i]) * factor);
                    }
                    if (band - i >= 0)
                    {
                        CurrentEqualizer.Values[band - i] += ((((CurrentEqualizer.Values[band - i + 1] + newVal) / 2) - CurrentEqualizer.Values[band - i]) * factor);
                    }
                    factor *= scrollFactor;
                }

                for (int i = 0; i < numBands; i++)
                    scrollBars[i].Value = (int)CurrentEqualizer.Values[i];
            }
            Clock.Update(ref eqChangeTimer, eqChanged, 360, false);
        }
        private void eqChanged()
        {
            eqChangeTimer = Clock.NULL_ALARM;
            EqChanged.Invoke();
        }
        private void reset(QButton Sender)
        {
            EqualizerSetting es = DefaultEqualizerSettings.FirstOrDefault(s => s.Name == CurrentEqualizer.Name);

            if (es == null)
            {
                for (int i = 0; i < numBands; i++)
                {
                    scrollBars[i].Value = 0;
                    CurrentEqualizer.Values[i] = 0;
                }
            }
            else
            {
                float[] f = (numBands == 10) ? compressBands(es.Values) : es.Values;

                for (int i = 0; i < numBands; i++)
                {
                    scrollBars[i].Value = (int)f[i];
                    CurrentEqualizer.Values[i] = f[i];
                }
            }
            EqChanged.Invoke();
        }
        private void turnOn(QButton Sender)
        {
            this.On = Sender.Value;
        }
        private void expand(QButton Sender)
        {
            for (int i = 0; i < numBands; i++)
            {
                CurrentEqualizer.Values[i] = Math.Max(-SCALE_FLOAT, Math.Min(SCALE_FLOAT, CurrentEqualizer.Values[i] * 1.1f));
                scrollBars[i].Value = (int)CurrentEqualizer.Values[i];
            }
            EqChanged.Invoke();
        }
        private void compress(QButton Sender)
        {
            for (int i = 0; i < numBands; i++)
            {
                CurrentEqualizer.Values[i] = Math.Max(-SCALE_FLOAT, Math.Min(SCALE_FLOAT, CurrentEqualizer.Values[i] * 0.9f));
                scrollBars[i].Value = (int)CurrentEqualizer.Values[i];
            }
            EqChanged.Invoke();
        }
        private void layoutButtons()
        {
            int extraWidth = Math.Max(10, this.ClientRectangle.Width - cboName.Right - HORIZ_MARGIN - HORIZ_MARGIN - fullButtonsWidth);

            bool compact = btnOn.Compact;

            compact = (extraWidth < 11);

            if (btnOn.Compact != compact)
            {
                btnOn.Compact = compact;
                btnLockPreset.Compact = compact;
                btnFineControl.Compact = compact;
                btnAllTogether.Compact = compact;
                btnCompress.Compact = compact;
                btnReset.Compact = compact;
                btnRemovePreset.Compact = compact;
                btnNewPreset.Compact = compact;
                btnNumBands.Compact = compact;
            }
            int width = compact ? getButtonsWidth() : fullButtonsWidth;

            extraWidth = Math.Max(0, this.ClientRectangle.Width - cboName.Right - HORIZ_MARGIN - HORIZ_MARGIN - width);

            buttonSpacing = extraWidth / 9;

            btnOn.Location = new Point(cboName.Right + HORIZ_MARGIN, BUTTON_TOPS);
            btnLockPreset.Location = new Point(btnOn.Right + buttonSpacing, BUTTON_TOPS);
            btnFineControl.Location = new Point(btnLockPreset.Right + buttonSpacing, BUTTON_TOPS);
            btnAllTogether.Location = new Point(btnFineControl.Right + buttonSpacing, BUTTON_TOPS);
            btnExpand.Location = new Point(btnAllTogether.Right + buttonSpacing, BUTTON_TOPS);
            btnCompress.Location = new Point(btnExpand.Right + buttonSpacing, BUTTON_TOPS);
            btnReset.Location = new Point(btnCompress.Right + buttonSpacing, BUTTON_TOPS);
            btnNewPreset.Location = new Point(btnReset.Right + buttonSpacing, BUTTON_TOPS);
            btnRemovePreset.Location = new Point(btnNewPreset.Right + buttonSpacing, BUTTON_TOPS);
            btnNumBands.Location = new Point(btnRemovePreset.Right + buttonSpacing, BUTTON_TOPS);
        }
        private int getButtonsWidth()
        {
            return btnOn.Width +
                   btnLockPreset.Width +
                   btnFineControl.Width +
                   btnAllTogether.Width +
                   btnExpand.Width +
                   btnCompress.Width +
                   btnReset.Width +
                   btnRemovePreset.Width +
                   btnNewPreset.Width +
                   btnNumBands.Width;
        }
        
    }
}