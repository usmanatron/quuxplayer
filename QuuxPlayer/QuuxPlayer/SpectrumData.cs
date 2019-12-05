/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Text;

namespace QuuxPlayer
{
    internal sealed class SpectrumData
    {
        public enum SampleRates { None, ThiryTwo, FortyFourPointOne, FortyEight }

        public const int LOWER_FREQ_BOUND = 20;
        public const int UPPER_FREQ_BOUND = 18000;

        public SampleRates SampleRate { get; private set; }

        public int NumBandsTranslated { get; private set; }
        public int NumBandsBase { get; private set; }
        public float[] FrequenciesTranslated { get; private set; }
        public float[] FrequenciesBase { get; private set; }
        public float[] LeftSpectrumBase { get; private set; }
        public float[] LeftSpectrumTranslated { get; private set; }
        public float[] RightSpectrumBase { get; private set; }
        public float[] RightSpectrumTranslated { get; private set; }
        public float LeftVUBase { get; set; }
        public float RightVUBase { get; set; }
        public float LeftVUTranslated { get; private set; }
        public float RightVUTranslated { get; private set; }
        public float[] LeftSpectrumPeak { get; private set; }
        public float[] RightSpectrumPeak { get; private set; }
        public float LeftVUPeak { get; private set; }
        public float RightVUPeak { get; private set; }

        private int[][] translationMap;
        private float[] translationWeights;

        private const float decayFactor = 0.90f;
        private const float VU_DISPLAY_FACTOR = 2.5f;

        private SpectrumMode mode;

        public SpectrumData(int NumberBaseBands, float SampleRate, SpectrumMode Mode)
        {
            mode = Mode;

            if (SampleRate < 33000)
                this.SampleRate = SampleRates.ThiryTwo;
            else if (SampleRate > 47000)
                this.SampleRate = SampleRates.FortyEight;
            else
                this.SampleRate = SampleRates.FortyFourPointOne;

            setTranslationMap(NumberBaseBands, SampleRate);
        }
        public SpectrumMode Mode
        {
            get { return mode; }
        }
        public void Translate()
        {
            LeftVUTranslated = 0f;
            RightVUTranslated = 0f;

            for (int i = 0; i < NumBandsTranslated; i++)
            {
                LeftSpectrumTranslated[i] = 0f;
                RightSpectrumTranslated[i] = 0f;
                for (int j = 0; j < this.translationMap[i].Length; j++)
                {
                    LeftSpectrumTranslated[i] += LeftSpectrumBase[translationMap[i][j]];
                    RightSpectrumTranslated[i] += RightSpectrumBase[translationMap[i][j]];
                }

                // uncomment to use weights
                LeftSpectrumTranslated[i] *= translationWeights[i];
                RightSpectrumTranslated[i] *= translationWeights[i];

                LeftVUTranslated += LeftSpectrumTranslated[i];
                RightVUTranslated += RightSpectrumTranslated[i];

                LeftSpectrumPeak[i] = Math.Max(LeftSpectrumPeak[i] * decayFactor, LeftSpectrumTranslated[i]);
                RightSpectrumPeak[i] = Math.Max(RightSpectrumPeak[i] * decayFactor, RightSpectrumTranslated[i]);
            }

            LeftVUTranslated /= NumBandsTranslated;
            RightVUTranslated /= NumBandsTranslated;
            LeftVUTranslated *= VU_DISPLAY_FACTOR;
            RightVUTranslated *= VU_DISPLAY_FACTOR;

            LeftVUPeak = Math.Max(LeftVUPeak * decayFactor, LeftVUTranslated);
            RightVUPeak = Math.Max(RightVUPeak * decayFactor, RightVUTranslated);
        }

        private const double SEMITONE = 1.0 / 12.0;

        private void setTranslationMap(int NumBandsBase, float SampleRate)
        {
            this.NumBandsBase = NumBandsBase;

            double lowerBound = 440.0 / 32.0; // 5 octaves below middle A

            System.Diagnostics.Debug.Assert(LOWER_FREQ_BOUND > lowerBound);

            while (lowerBound < LOWER_FREQ_BOUND)
            {
                lowerBound *= Math.Pow(2.0, SEMITONE);
            }

            lowerBound *= Math.Pow(2.0, 0.5 * SEMITONE); // set to center of range

            List<double> translatedFreqs = new List<double>();
            List<List<int>> freqBuckets = new List<List<int>>();

            for (double f = lowerBound; f < SampleRate * 1.1f; f *= Math.Pow(2.0, SEMITONE))
                translatedFreqs.Add(f);

            float baseFreqIncrement = SampleRate / NumBandsBase;
            FrequenciesBase = new float[this.NumBandsBase];

            for (int i = 0; i < this.NumBandsBase; i++)
                FrequenciesBase[i] = ((float)i) * baseFreqIncrement;

            int baseCursor = 0;
            freqBuckets.Add(new List<int>());

            int maxBaseBand = Math.Min(this.NumBandsBase, (int)(UPPER_FREQ_BOUND / baseFreqIncrement));

            for (int i = Math.Max(1, (int)(LOWER_FREQ_BOUND / baseFreqIncrement)); i < maxBaseBand; i++)
            {
                while (translatedFreqs[baseCursor] < ((float)i) * baseFreqIncrement)
                {
                    baseCursor++;
                    freqBuckets.Add(new List<int>());
                }
                freqBuckets[baseCursor].Add(i);
            }

            for (int i = freqBuckets.Count - 1; i >= 0; i--)
            {
                if (freqBuckets[i].Count == 0)
                    freqBuckets.RemoveAt(i);
            }

            FrequenciesTranslated = new float[freqBuckets.Count];
            NumBandsTranslated = FrequenciesTranslated.Length;
            for (int i = 0; i < NumBandsTranslated; i++)
            {
                FrequenciesTranslated[i] = ((FrequenciesBase[freqBuckets[i][0]] + FrequenciesBase[freqBuckets[i][freqBuckets[i].Count - 1]]) / 2.0f);
            }

            translationMap = new int[freqBuckets.Count][];
            translationWeights = new float[NumBandsTranslated];
            for (int i = 0; i < NumBandsTranslated; i++)
            {
                translationMap[i] = freqBuckets[i].ToArray();
                translationWeights[i] = (float)(equalLoudness(FrequenciesTranslated[i]) * (1.0 / Math.Pow(translationMap[i].Length, 0.1)));
            }

            this.LeftSpectrumBase = new float[NumBandsBase];
            this.RightSpectrumBase = new float[NumBandsBase];

            this.LeftSpectrumTranslated = new float[NumBandsTranslated];
            this.RightSpectrumTranslated = new float[NumBandsTranslated];

            this.LeftSpectrumPeak = new float[NumBandsTranslated];
            this.RightSpectrumPeak = new float[NumBandsTranslated];
        }
        private static float equalLoudness(float Frequency)
        {
            double logFreq = Math.Log10(Frequency);
            double deviation = (logFreq - 3.5);
            double devSqr = Math.Pow(deviation, 2.0);
            double coeff = (7.0 - devSqr) / 3.5 - 0.2;

            return Math.Min(2.0f, Math.Max(0.2f, (float)coeff));
        }
    }
}
