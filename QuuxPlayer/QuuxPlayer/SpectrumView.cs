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
    internal enum SpectrumMode { None, Normal, Small }

    internal sealed class SpectrumView : Control, IMainView
    {
        public bool ShowGrid { get; set; }

        private const int FREQ_BANDS_NORMAL = 84;
        private const int FREQ_BANDS_SMALL = 60;

        private int TOP_MARGIN = 10;

        private const int CHANNEL_MARGIN = 1;
        private const int BAND_MARGIN = 2;
        private const int DB_MARKER_WIDTH = 30;
        private const int DEFAULT_SCALE = 100;
        public const float DEFAULT_GAIN = 9.0f;

        private const TextFormatFlags tff = TextFormatFlags.NoPrefix | TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoClipping;
        private const TextFormatFlags tffr = TextFormatFlags.NoPrefix | TextFormatFlags.Right | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoClipping;

        private SpectrumData spectrum = null;
        private SpectrumData.SampleRates sampleRate = SpectrumData.SampleRates.None;
        private SpectrumMode mode = SpectrumMode.None;

        private Brush leftBrush = Styles.SpectrumLeftBrush;
        private Brush rightBrush = Styles.SpectrumRightBrush;

        private int[] bandPosn;
        private float gain;
        private float gainTimesScale;
        private int baseline;
        private int bandDist;
        private int rightChannelOffset;
        private int rightChannelOffsetPeak;
        private float scale = DEFAULT_SCALE;
        private int leftMargin;
        private int rightMargin;
        private int yellowZone;
        private int greenZone;
        private string[] freqMarkers;
        private Rectangle[] freqMarkerPositions;
        private string[] dbMarkers;
        private Rectangle[] dbMarkerPositions;
        private bool initialized = false;
        private int bandPixels = 1;
        private int vuPixels = 1;

        private Rectangle spectrumRectangle;

        public SpectrumView()
        {
            this.DoubleBuffered = true;
            this.BackColor = Color.Black;

            this.Gain = DEFAULT_GAIN;
            this.ShowGrid = false;
        }
        public ViewType ViewType { get { return ViewType.Spectrum; } }
        public SpectrumData Spectrum
        {
            get { return spectrum; }
            set
            {
                spectrum = value;
                if (!initialized || (spectrum.SampleRate != sampleRate) || (spectrum.Mode != mode))
                {
                    reinitialize();
                    this.Invalidate();
                }
                else
                {
                    this.Invalidate(spectrumRectangle);
                }
            }
        }
        public SpectrumMode Mode
        {
            get { return mode; }
        }
        private void reinitialize()
        {
            mode = spectrum.Mode;
            sampleRate = spectrum.SampleRate;

            setupMetrics2();
            setupMarkers();
            setupMetrics1();

            initialized = true;
        }
        public float Gain
        {
            get { return gain * (float)Player.SCALE_FACTOR; }
            set
            {
                value = Math.Max(DEFAULT_GAIN / 2f, Math.Min(DEFAULT_GAIN * 2f, value));

                if (Math.Abs(value - DEFAULT_GAIN) / DEFAULT_GAIN < 0.03f)
                    value = DEFAULT_GAIN;

                gain = value / (float)Player.SCALE_FACTOR;

                setGainTimesScale();
            }
        }

        private void setupMetrics1()
        {
            setupMarkers();
            setupMetrics();
            setGainTimesScale();
        }
        private void setupMarkers()
        {
            dbMarkers = new string[] { "+3 dB", "0 dB", "-1 dB", "-2 dB", "-3 dB", "-4.5 dB", "-6 dB", "-8 dB", "-12 dB", "-20 dB" };
            dbMarkerPositions = new Rectangle[dbMarkers.Length];

            freqMarkers = new string[0x10 + 2];
            freqMarkers[0] = "VU/L";
            freqMarkers[1] = "VU/R";
            for (int i = 2; i < freqMarkers.Length; i++)
            {
                freqMarkers[i] = roundOff((int)spectrum.FrequenciesTranslated[Math.Min(spectrum.FrequenciesTranslated.Length - 1, (i - 2) * spectrum.FrequenciesTranslated.Length / (freqMarkers.Length - 3))]).ToString() + " Hz";
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            setupMetrics2();

            if (initialized)
            {
                setupMetrics();
                //leftBrush = Style.GetSpectrumLeftBrush((int)scale, (int)(baseline - scale));
                //rightBrush = Style.GetSpectrumRightBrush((int)scale, (int)(baseline - scale));
                this.Invalidate();
            }
        }

        private void setupMetrics2()
        {
            vuPixels = this.ClientRectangle.Width / 40;

            int horizWidth = this.ClientRectangle.Width - (vuPixels + CHANNEL_MARGIN) * 2 - 2 - DB_MARKER_WIDTH - 10;

            int freqBands = (mode == SpectrumMode.Normal) ? FREQ_BANDS_NORMAL : FREQ_BANDS_SMALL;

            bandPixels = Math.Max(1, (horizWidth - freqBands * BAND_MARGIN - freqBands * CHANNEL_MARGIN - DB_MARKER_WIDTH) / (freqBands * 2));

            bandDist = bandPixels * 2 + CHANNEL_MARGIN + BAND_MARGIN;

            rightChannelOffset = bandPixels + CHANNEL_MARGIN;
            rightChannelOffsetPeak = bandPixels + bandPixels + CHANNEL_MARGIN - 1;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                SpectrumData spec = this.spectrum;

                if (spec != null)
                {
                    float h;

                    spec.Translate();

                    if (ShowGrid)
                    {
                        for (int i = baseline; i > TOP_MARGIN; i -= 12)
                        {
                            if (i > greenZone)
                                e.Graphics.DrawLine(Styles.SpectrumGreenPen, leftMargin, i, rightMargin, i);
                            else if (i > yellowZone)
                                e.Graphics.DrawLine(Styles.SpectrumYellowPen, leftMargin, i, rightMargin, i);
                            else
                                e.Graphics.DrawLine(Styles.SpectrumRedPen, leftMargin, i, rightMargin, i);
                        }
                    }

                    h = Math.Min(scale, spec.LeftVUTranslated * gainTimesScale);
                    e.Graphics.FillRectangle(leftBrush, leftMargin, baseline - h, vuPixels, h);

                    h = Math.Min(scale, spec.RightVUTranslated * gainTimesScale);
                    e.Graphics.FillRectangle(rightBrush, leftMargin + vuPixels + CHANNEL_MARGIN, baseline - h, vuPixels, h);

                    for (int i = 0; i < spec.NumBandsTranslated; i++)
                    {
                        h = Math.Min(scale, spec.LeftSpectrumTranslated[i] * gainTimesScale);
                        e.Graphics.FillRectangle(leftBrush, bandPosn[i], baseline - h, bandPixels, h);

                        h = Math.Min(scale, spec.RightSpectrumTranslated[i] * gainTimesScale);
                        e.Graphics.FillRectangle(rightBrush, bandPosn[i] + rightChannelOffset, baseline - h, bandPixels, h);
                    }

                    // PEAK LINES

                    h = baseline - Math.Min(scale, spec.LeftVUPeak * gainTimesScale);
                    e.Graphics.DrawLine(Styles.SpectrumPeakPen, leftMargin, h, leftMargin + vuPixels - 1, h);

                    h = baseline - Math.Min(scale, spec.RightVUPeak * gainTimesScale);
                    e.Graphics.DrawLine(Styles.SpectrumPeakPen, leftMargin + vuPixels + CHANNEL_MARGIN, h, leftMargin + vuPixels + vuPixels + CHANNEL_MARGIN - 1, h);

                    for (int i = 0; i < spec.NumBandsTranslated; i++)
                    {
                        h = baseline - Math.Min(scale, spec.LeftSpectrumPeak[i] * gainTimesScale);
                        e.Graphics.DrawLine(Styles.SpectrumPeakPen, bandPosn[i], h, bandPosn[i] + bandPixels - 1, h);

                        h = baseline - Math.Min(scale, spec.RightSpectrumPeak[i] * gainTimesScale);
                        e.Graphics.DrawLine(Styles.SpectrumPeakPen, bandPosn[i] + bandPixels + 1, h, bandPosn[i] + rightChannelOffsetPeak, h);
                    }
                    if (!spectrumRectangle.Contains(e.ClipRectangle))
                    {
                        // FREQUENCY MARKERS

                        for (int i = 0; i < freqMarkers.Length; i++)
                        {
                            TextRenderer.DrawText(e.Graphics,
                                                  freqMarkers[i],
                                                  Styles.FontSmall,
                                                  freqMarkerPositions[i],
                                                  Color.White,
                                                  tff);
                        }

                        // DB MARKERS

                        for (int i = 0; i < dbMarkers.Length; i++)
                        {
                            TextRenderer.DrawText(e.Graphics,
                                                  dbMarkers[i],
                                                  Styles.FontSmall,
                                                  dbMarkerPositions[i],
                                                  Color.White,
                                                  tffr);
                        }
                    }
                }
            }
            catch { }
        }

        private void setupMetrics()
        {
            baseline = this.ClientRectangle.Height - 20;

            scale = (float)Math.Floor(Math.Max(100f, baseline - TOP_MARGIN));

            if (((int)scale % 12) != 0)
                scale = (float)Math.Floor(scale / 12f) * 12f;
            
            if (spectrum != null && spectrum.NumBandsTranslated > 0)
            {
                bandPosn = new int[spectrum.NumBandsTranslated];
                bandPosn[0] = vuPixels * 2 + CHANNEL_MARGIN + BAND_MARGIN + 2;
                for (int i = 1; i < spectrum.NumBandsTranslated; i++)
                    bandPosn[i] = bandPosn[i - 1] + bandDist;

                int totWidth = this.ClientRectangle.Width;

                leftMargin = (totWidth - bandPosn[bandPosn.Length - 1] - bandDist + DB_MARKER_WIDTH) / 2;
                rightMargin = totWidth - leftMargin + DB_MARKER_WIDTH;

                greenZone = (baseline - TOP_MARGIN) * 19 / 80 + TOP_MARGIN;
                yellowZone = (baseline - TOP_MARGIN) * 9 / 80 + TOP_MARGIN;

                for (int i = 0; i < bandPosn.Length; i++)
                    bandPosn[i] += leftMargin;

                setupFreqMarkersPositions();

            }
            setGainTimesScale();

            setupDBRectangles();

            spectrumRectangle = new Rectangle(leftMargin, 0, this.ClientRectangle.Width - leftMargin, baseline + 1);
        }
        private void setGainTimesScale()
        {
            gainTimesScale = gain * scale;
        }
        private void setupFreqMarkersPositions()
        {
            freqMarkerPositions = new Rectangle[freqMarkers.Length];
            float spacing = (bandPosn[bandPosn.Length - 1] - bandPosn[0] - 20) / (float)(freqMarkerPositions.Length - 3);

            freqMarkerPositions[0] = new Rectangle(leftMargin, (int)baseline, vuPixels, 20);
            freqMarkerPositions[1] = new Rectangle(leftMargin + vuPixels + CHANNEL_MARGIN, (int)baseline, vuPixels, 20);
            for (int i = 2; i < freqMarkerPositions.Length; i++)
            {
                freqMarkerPositions[i] = new Rectangle((int)(bandPosn[0] - 20 + bandPixels + (i - 2) * spacing), (int)baseline, 60, 20);
            }
        }
        private void setupDBRectangles()
        {
            int s = (int)((scale - 20f) / 9f);

            for (int i = 0; i < dbMarkerPositions.Length; i++)
            {
                dbMarkerPositions[i] = new Rectangle(leftMargin - DB_MARKER_WIDTH, TOP_MARGIN + i * s, DB_MARKER_WIDTH - 3, 20);
            }
        }
        private static int roundOff(int val)
        {
            if (val > 10000)
                val = 1000 * (int)Math.Round((double)val / 1000.0, MidpointRounding.ToEven);
            else if (val > 950)
                val = 100 * (int)Math.Round((double)val / 100.0, MidpointRounding.ToEven);
            else if (val > 80)
                val = 10 * (int)Math.Round((double)val / 10.0, MidpointRounding.ToEven);
            else
                val = 5 * (int)Math.Round((double)val / 5.0, MidpointRounding.ToEven);
            return val;
        }
    }
}