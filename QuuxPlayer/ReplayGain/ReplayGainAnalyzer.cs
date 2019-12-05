/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;

namespace ReplayGainAnalyzer
{
    public class ReplayGainAnalyzer
    {
        private const int READ_BUFFER_SIZE = 0x2000;

        private const float RMS_PERCENTILE = 0.95f;
        private const int MAX_SAMP_FREQ = 48000;
        private const float RMS_WINDOW_TIME_SLICE = 0.050f;
        private const int STEPS_PER_dB = 100;
        private const int MAX_dB = 120;

        private const int INIT_OFFSET_LENGTH = 10;
        private const int MAX_SAMPLES_PER_TIME_SLICE = (int)((float)MAX_SAMP_FREQ * RMS_WINDOW_TIME_SLICE) + 11;
        private const float REFERENCE_DB_VAL = 64.82f;

        private float[] leftInPreBuffer = new float[INIT_OFFSET_LENGTH * 2];
        private int leftInPreBufferCursor;                                          // left input samples, with pre-buffer
        private float[] leftFirstPassBuffer = new float[MAX_SAMPLES_PER_TIME_SLICE + INIT_OFFSET_LENGTH];
        private int leftFirstPassBufferCursor;                                           // left "first step" (i.e. post first filter) samples
        private float[] leftOutBuffer = new float[MAX_SAMPLES_PER_TIME_SLICE + INIT_OFFSET_LENGTH];
        private int leftOutBufferCursor;                                            // left "out" (i.e. post second filter) samples
        private float[] rightInPreBuffer = new float[INIT_OFFSET_LENGTH * 2];
        private int rightInPreBufferCursor;                                          // right input samples ...
        private float[] rightFirstPassBuffer = new float[MAX_SAMPLES_PER_TIME_SLICE + INIT_OFFSET_LENGTH];
        private int rightFirstPassBufferCursor;
        private float[] rightOutBuffer = new float[MAX_SAMPLES_PER_TIME_SLICE + INIT_OFFSET_LENGTH];
        private int rightOutBufferCursor;
        private int sampleWindow;                                    // number of samples required to reach number of milliseconds required for RMS window
        private int totalSamples;
        private double leftSum;
        private double rightSum;
        private int freqIndex;

        private uint[] trackResults = new uint[STEPS_PER_dB * MAX_dB];
        private uint[] albumResults = new uint[STEPS_PER_dB * MAX_dB];

        private static float[][] YuleFilterConstants = new float[][]
        {
            new float[] {0.03857599435200f, -3.84664617118067f, -0.02160367184185f,  7.81501653005538f, -0.00123395316851f, -11.34170355132042f, -0.00009291677959f, 13.05504219327545f, -0.01655260341619f, -12.28759895145294f,  0.02161526843274f,  9.48293806319790f, -0.02074045215285f, -5.87257861775999f,  0.00594298065125f,  2.75465861874613f,  0.00306428023191f, -0.86984376593551f,  0.00012025322027f,  0.13919314567432f,  0.00288463683916f },
            new float[] {0.05418656406430f, -3.47845948550071f, -0.02911007808948f,  6.36317777566148f, -0.00848709379851f, -8.54751527471874f, -0.00851165645469f,  9.47693607801280f, -0.00834990904936f, -8.81498681370155f,  0.02245293253339f,  6.85401540936998f, -0.02596338512915f, -4.39470996079559f,  0.01624864962975f,  2.19611684890774f, -0.00240879051584f, -0.75104302451432f,  0.00674613682247f,  0.13149317958808f, -0.00187763777362f },
            new float[] {0.15457299681924f, -2.37898834973084f, -0.09331049056315f,  2.84868151156327f, -0.06247880153653f, -2.64577170229825f,  0.02163541888798f,  2.23697657451713f, -0.05588393329856f, -1.67148153367602f,  0.04781476674921f,  1.00595954808547f,  0.00222312597743f, -0.45953458054983f,  0.03174092540049f,  0.16378164858596f, -0.01390589421898f, -0.05032077717131f,  0.00651420667831f,  0.02347897407020f, -0.00881362733839f },
            new float[] {0.30296907319327f, -1.61273165137247f, -0.22613988682123f,  1.07977492259970f, -0.08587323730772f, -0.25656257754070f,  0.03282930172664f, -0.16276719120440f, -0.00915702933434f, -0.22638893773906f, -0.02364141202522f,  0.39120800788284f, -0.00584456039913f, -0.22138138954925f,  0.06276101321749f,  0.04500235387352f, -0.00000828086748f,  0.02005851806501f,  0.00205861885564f,  0.00302439095741f, -0.02950134983287f },
            new float[] {0.33642304856132f, -1.49858979367799f, -0.25572241425570f,  0.87350271418188f, -0.11828570177555f,  0.12205022308084f,  0.11921148675203f, -0.80774944671438f, -0.07834489609479f,  0.47854794562326f, -0.00469977914380f, -0.12453458140019f, -0.00589500224440f, -0.04067510197014f,  0.05724228140351f,  0.08333755284107f,  0.00832043980773f, -0.04237348025746f, -0.01635381384540f,  0.02977207319925f, -0.01760176568150f },
            new float[] {0.44915256608450f, -0.62820619233671f, -0.14351757464547f,  0.29661783706366f, -0.22784394429749f, -0.37256372942400f, -0.01419140100551f,  0.00213767857124f,  0.04078262797139f, -0.42029820170918f, -0.12398163381748f,  0.22199650564824f,  0.04097565135648f,  0.00613424350682f,  0.10478503600251f,  0.06747620744683f, -0.01863887810927f,  0.05784820375801f, -0.03193428438915f,  0.03222754072173f,  0.00541907748707f },
            new float[] {0.56619470757641f, -1.04800335126349f, -0.75464456939302f,  0.29156311971249f,  0.16242137742230f, -0.26806001042947f,  0.16744243493672f,  0.00819999645858f, -0.18901604199609f,  0.45054734505008f,  0.30931782841830f, -0.33032403314006f, -0.27562961986224f,  0.06739368333110f,  0.00647310677246f, -0.04784254229033f,  0.08647503780351f,  0.01639907836189f, -0.03788984554840f,  0.01807364323573f, -0.00588215443421f },
            new float[] {0.58100494960553f, -0.51035327095184f, -0.53174909058578f, -0.31863563325245f, -0.14289799034253f, -0.20256413484477f,  0.17520704835522f,  0.14728154134330f,  0.02377945217615f,  0.38952639978999f,  0.15558449135573f, -0.23313271880868f, -0.25344790059353f, -0.05246019024463f,  0.01628462406333f, -0.02505961724053f,  0.06920467763959f,  0.02442357316099f, -0.03721611395801f,  0.01818801111503f, -0.00749618797172f },
            new float[] {0.53648789255105f, -0.25049871956020f, -0.42163034350696f, -0.43193942311114f, -0.00275953611929f, -0.03424681017675f,  0.04267842219415f, -0.04678328784242f, -0.10214864179676f,  0.26408300200955f,  0.14590772289388f,  0.15113130533216f, -0.02459864859345f, -0.17556493366449f, -0.11202315195388f, -0.18823009262115f, -0.04060034127000f,  0.05477720428674f,  0.04788665548180f,  0.04704409688120f, -0.02217936801134f }
        };

        private static float[][] ButterworthFilterConstants = new float[][]
        {
            new float[] {0.98621192462708f, -1.97223372919527f, -1.97242384925416f,  0.97261396931306f,  0.98621192462708f },
            new float[] {0.98500175787242f, -1.96977855582618f, -1.97000351574484f,  0.97022847566350f,  0.98500175787242f },
            new float[] {0.97938932735214f, -1.95835380975398f, -1.95877865470428f,  0.95920349965459f,  0.97938932735214f },
            new float[] {0.97531843204928f, -1.95002759149878f, -1.95063686409857f,  0.95124613669835f,  0.97531843204928f },
            new float[] {0.97316523498161f, -1.94561023566527f, -1.94633046996323f,  0.94705070426118f,  0.97316523498161f },
            new float[] {0.96454515552826f, -1.92783286977036f, -1.92909031105652f,  0.93034775234268f,  0.96454515552826f },
            new float[] {0.96009142950541f, -1.91858953033784f, -1.92018285901082f,  0.92177618768381f,  0.96009142950541f },
            new float[] {0.95856916599601f, -1.91542108074780f, -1.91713833199203f,  0.91885558323625f,  0.95856916599601f },
            new float[] {0.94597685600279f, -1.88903307939452f, -1.89195371200558f,  0.89487434461664f,  0.94597685600279f }
        };

        private static void filterYule(float[] Input, int InputOffset, float[] Output, int OutputOffset, int NumSamples, float[] FilterConstant)
        {
            while (NumSamples-- > 0)
            {
                Output[OutputOffset] =
                   Input[InputOffset] * FilterConstant[0]
                 - Output[OutputOffset - 1] * FilterConstant[1]
                 + Input[InputOffset - 1] * FilterConstant[2]
                 - Output[OutputOffset - 2] * FilterConstant[3]
                 + Input[InputOffset - 2] * FilterConstant[4]
                 - Output[OutputOffset - 3] * FilterConstant[5]
                 + Input[InputOffset - 3] * FilterConstant[6]
                 - Output[OutputOffset - 4] * FilterConstant[7]
                 + Input[InputOffset - 4] * FilterConstant[8]
                 - Output[OutputOffset - 5] * FilterConstant[9]
                 + Input[InputOffset - 5] * FilterConstant[10]
                 - Output[OutputOffset - 6] * FilterConstant[11]
                 + Input[InputOffset - 6] * FilterConstant[12]
                 - Output[OutputOffset - 7] * FilterConstant[13]
                 + Input[InputOffset - 7] * FilterConstant[14]
                 - Output[OutputOffset - 8] * FilterConstant[15]
                 + Input[InputOffset - 8] * FilterConstant[16]
                 - Output[OutputOffset - 9] * FilterConstant[17]
                 + Input[InputOffset - 9] * FilterConstant[18]
                 - Output[OutputOffset - 10] * FilterConstant[19]
                 + Input[InputOffset - 10] * FilterConstant[20];

                ++OutputOffset;
                ++InputOffset;
            }
        }

        private static void filterButterworth(float[] Input, int InputOffset, float[] Output, int OutputOffset, int NumSamples, float[] FilterConstant)
        {
            while (NumSamples-- > 0)
            {
                Output[OutputOffset] =
                   Input[InputOffset]       * FilterConstant[0]
                 - Output[OutputOffset - 1] * FilterConstant[1]
                 + Input[InputOffset - 1]   * FilterConstant[2]
                 - Output[OutputOffset - 2] * FilterConstant[3]
                 + Input[InputOffset - 2]   * FilterConstant[4];

                ++OutputOffset;
                ++InputOffset;
            }
        }

        public bool ResetSampleFrequency(int SampleFreq)
        {
            clearArrays();

            switch (SampleFreq)
            {
                case 48000:
                    freqIndex = 0;
                    break;
                case 44100:
                    freqIndex = 1;
                    break;
                case 32000:
                    freqIndex = 2;
                    break;
                case 24000:
                    freqIndex = 3;
                    break;
                case 22050:
                    freqIndex = 4;
                    break;
                case 16000:
                    freqIndex = 5;
                    break;
                case 12000:
                    freqIndex = 6;
                    break;
                case 11025:
                    freqIndex = 7;
                    break;
                case 8000:
                    freqIndex = 8;
                    break;
                default:
                    return false;
            }

            sampleWindow = (int)Math.Ceiling(SampleFreq * RMS_WINDOW_TIME_SLICE);

            leftSum = 0.0;
            rightSum = 0.0;
            totalSamples = 0;

            Array.Clear(trackResults, 0, trackResults.Length);

            return true;
        }

        private void clearArrays()
        {
            Array.Clear(leftInPreBuffer,      0, INIT_OFFSET_LENGTH);
            Array.Clear(leftFirstPassBuffer,  0, INIT_OFFSET_LENGTH);
            Array.Clear(leftOutBuffer,        0, INIT_OFFSET_LENGTH);
            Array.Clear(rightInPreBuffer,     0, INIT_OFFSET_LENGTH);
            Array.Clear(rightFirstPassBuffer, 0, INIT_OFFSET_LENGTH);
            Array.Clear(rightOutBuffer,       0, INIT_OFFSET_LENGTH);
        }

        public bool InitGainAnalysis(int sampleFreq)
        {
            if (!ResetSampleFrequency(sampleFreq))
            {
                return false;
            }

            leftInPreBufferCursor      = INIT_OFFSET_LENGTH;
            rightInPreBufferCursor     = INIT_OFFSET_LENGTH;
            leftFirstPassBufferCursor  = INIT_OFFSET_LENGTH;
            rightFirstPassBufferCursor = INIT_OFFSET_LENGTH;
            leftOutBufferCursor        = INIT_OFFSET_LENGTH;
            rightOutBufferCursor       = INIT_OFFSET_LENGTH;

            Array.Clear(albumResults, 0, albumResults.Length);

            return true;
        }

        private static float Square(float d)
        {
            return d * d;
        }

        public bool AnalyzeSamples(float[] LeftSamples, float[] RightSamples, int NumSamples)
        {
            int currentLeftBufferCursor;
            int currentRightBufferCursor;
            float[] currentLeftBuffer;
            float[] currentRightBuffer;
            int samplesRemaining;
            int currentSamples;
            int currentSamplePosition;
            int i;

            if (NumSamples == 0)
                return true;

            currentSamplePosition = 0;
            samplesRemaining = NumSamples;

            if (NumSamples < INIT_OFFSET_LENGTH)
            {
                Array.Copy(LeftSamples,  0, leftInPreBuffer,  INIT_OFFSET_LENGTH, NumSamples);
                Array.Copy(RightSamples, 0, rightInPreBuffer, INIT_OFFSET_LENGTH, NumSamples);
            }
            else
            {
                Array.Copy(LeftSamples,  0, leftInPreBuffer,  INIT_OFFSET_LENGTH, INIT_OFFSET_LENGTH);
                Array.Copy(RightSamples, 0, rightInPreBuffer, INIT_OFFSET_LENGTH, INIT_OFFSET_LENGTH);
            }

            while (samplesRemaining > 0)
            {
                currentSamples = Math.Min(samplesRemaining, sampleWindow - totalSamples);

                if (currentSamplePosition < INIT_OFFSET_LENGTH)
                {
                    currentLeftBuffer = leftInPreBuffer;
                    currentLeftBufferCursor = currentSamplePosition + leftInPreBufferCursor;
                    currentRightBuffer = rightInPreBuffer;
                    currentRightBufferCursor = currentSamplePosition + rightInPreBufferCursor;

                    if (currentSamples > INIT_OFFSET_LENGTH - currentSamplePosition)
                        currentSamples = INIT_OFFSET_LENGTH - currentSamplePosition;
                }
                else
                {
                    currentLeftBuffer = LeftSamples;
                    currentLeftBufferCursor = currentSamplePosition;
                    currentRightBuffer = RightSamples;
                    currentRightBufferCursor = currentSamplePosition;
                }

                filterYule(currentLeftBuffer, currentLeftBufferCursor, leftFirstPassBuffer, totalSamples + leftFirstPassBufferCursor, currentSamples, YuleFilterConstants[freqIndex]);
                filterYule(currentRightBuffer, currentRightBufferCursor, rightFirstPassBuffer, totalSamples + rightFirstPassBufferCursor, currentSamples, YuleFilterConstants[freqIndex]);

                filterButterworth(leftFirstPassBuffer, totalSamples + leftFirstPassBufferCursor, leftOutBuffer, totalSamples + leftOutBufferCursor, currentSamples, ButterworthFilterConstants[freqIndex]);
                filterButterworth(rightFirstPassBuffer, totalSamples + rightFirstPassBufferCursor, rightOutBuffer, totalSamples + leftOutBufferCursor, currentSamples, ButterworthFilterConstants[freqIndex]);

                currentLeftBuffer = leftOutBuffer;
                currentLeftBufferCursor = totalSamples + leftOutBufferCursor;
                currentRightBuffer = rightOutBuffer;
                currentRightBufferCursor = totalSamples + rightOutBufferCursor;

                i = currentSamples % 16;
                while (i-- > 0)
                {
                    leftSum += Square(currentLeftBuffer[currentLeftBufferCursor++]);
                    rightSum += Square(currentRightBuffer[currentRightBufferCursor++]);
                }

                i = currentSamples / 16;
                while (i-- > 0)
                {
                    leftSum += Square(currentLeftBuffer[currentLeftBufferCursor])
                          + Square(currentLeftBuffer[currentLeftBufferCursor + 1])
                          + Square(currentLeftBuffer[currentLeftBufferCursor + 2])
                          + Square(currentLeftBuffer[currentLeftBufferCursor + 3])
                          + Square(currentLeftBuffer[currentLeftBufferCursor + 4])
                          + Square(currentLeftBuffer[currentLeftBufferCursor + 5])
                          + Square(currentLeftBuffer[currentLeftBufferCursor + 6])
                          + Square(currentLeftBuffer[currentLeftBufferCursor + 7])
                          + Square(currentLeftBuffer[currentLeftBufferCursor + 8])
                          + Square(currentLeftBuffer[currentLeftBufferCursor + 9])
                          + Square(currentLeftBuffer[currentLeftBufferCursor + 10])
                          + Square(currentLeftBuffer[currentLeftBufferCursor + 11])
                          + Square(currentLeftBuffer[currentLeftBufferCursor + 12])
                          + Square(currentLeftBuffer[currentLeftBufferCursor + 13])
                          + Square(currentLeftBuffer[currentLeftBufferCursor + 14])
                          + Square(currentLeftBuffer[currentLeftBufferCursor + 15]);

                    currentLeftBufferCursor += 16;

                    rightSum += Square(currentRightBuffer[currentRightBufferCursor])
                          + Square(currentRightBuffer[currentRightBufferCursor + 1])
                          + Square(currentRightBuffer[currentRightBufferCursor + 2])
                          + Square(currentRightBuffer[currentRightBufferCursor + 3])
                          + Square(currentRightBuffer[currentRightBufferCursor + 4])
                          + Square(currentRightBuffer[currentRightBufferCursor + 5])
                          + Square(currentRightBuffer[currentRightBufferCursor + 6])
                          + Square(currentRightBuffer[currentRightBufferCursor + 7])
                          + Square(currentRightBuffer[currentRightBufferCursor + 8])
                          + Square(currentRightBuffer[currentRightBufferCursor + 9])
                          + Square(currentRightBuffer[currentRightBufferCursor + 10])
                          + Square(currentRightBuffer[currentRightBufferCursor + 11])
                          + Square(currentRightBuffer[currentRightBufferCursor + 12])
                          + Square(currentRightBuffer[currentRightBufferCursor + 13])
                          + Square(currentRightBuffer[currentRightBufferCursor + 14])
                          + Square(currentRightBuffer[currentRightBufferCursor + 15]);

                    currentRightBufferCursor += 16;
                }

                samplesRemaining -= currentSamples;
                currentSamplePosition += currentSamples;
                totalSamples += currentSamples;
                if (totalSamples == sampleWindow)
                {
                    int idx = (int)((double)STEPS_PER_dB * 10.0 * Math.Log10((leftSum + rightSum) / totalSamples * 0.5 + 1.0e-37));

                    if (idx < 0)
                        idx = 0;

                    if (idx >= trackResults.Length)
                        idx = trackResults.Length;

                    trackResults[idx]++;
                    leftSum = 0f;
                    rightSum = 0f;
                    moveInArray(leftOutBuffer, leftOutBufferCursor, leftOutBufferCursor + totalSamples, INIT_OFFSET_LENGTH);
                    moveInArray(rightOutBuffer, rightOutBufferCursor, rightOutBufferCursor + totalSamples, INIT_OFFSET_LENGTH);
                    moveInArray(leftFirstPassBuffer, leftFirstPassBufferCursor, leftFirstPassBufferCursor + totalSamples, INIT_OFFSET_LENGTH);
                    moveInArray(rightFirstPassBuffer, rightFirstPassBufferCursor, rightFirstPassBufferCursor + totalSamples, INIT_OFFSET_LENGTH);
                    totalSamples = 0;
                }
                if (totalSamples > sampleWindow)
                    return false;
            }
            if (NumSamples < INIT_OFFSET_LENGTH)
            {
                moveInArray(leftInPreBuffer, leftInPreBufferCursor, leftInPreBufferCursor + NumSamples, INIT_OFFSET_LENGTH - NumSamples);
                moveInArray(rightInPreBuffer, rightInPreBufferCursor, rightInPreBufferCursor + NumSamples, INIT_OFFSET_LENGTH - NumSamples);
                Array.Copy(LeftSamples, 0, leftInPreBuffer, leftInPreBufferCursor + INIT_OFFSET_LENGTH - NumSamples, NumSamples);
                Array.Copy(RightSamples, 0, rightInPreBuffer, rightInPreBufferCursor + INIT_OFFSET_LENGTH - NumSamples, NumSamples);
            }
            else
            {
                Array.Copy(LeftSamples, NumSamples - INIT_OFFSET_LENGTH, leftInPreBuffer, leftInPreBufferCursor, INIT_OFFSET_LENGTH);
                Array.Copy(RightSamples, NumSamples - INIT_OFFSET_LENGTH, rightInPreBuffer, rightInPreBufferCursor, INIT_OFFSET_LENGTH);
            }

            return true;
        }
        private static void moveInArray(float[] Array, int Offset1, int Offset2, int Num)
        {
            if (Offset1 != Offset2)
            {
                System.Diagnostics.Debug.Assert(Offset2 > Offset1);
                for (int i = 0; i < Num; i++)
                    Array[i + Offset1] = Array[i + Offset2];
            }
        }
        private static float analyzeResult(uint[] Array)
        {
            uint elems = 0;
            int upper;
            int i;

            for (i = 0; i < Array.Length; i++)
                elems += Array[i];
         
            if (elems == 0)
                return 0;

            upper = (int)Math.Ceiling(elems * (1.0 - RMS_PERCENTILE));
            for (i = Array.Length; i-- > 0; )
            {
                if ((upper -= (int)Array[i]) <= 0)
                    break;
            }

            return (float)(REFERENCE_DB_VAL - (float)i / (float)STEPS_PER_dB);
        }

        public float GetTrackGain()
        {
            float retval;
            int i;

            retval = analyzeResult(trackResults);

            for (i = 0; i < trackResults.Length; i++)
            {
                albumResults[i] += trackResults[i];
                trackResults[i] = 0;
            }

            clearArrays();

            totalSamples = 0;
            leftSum = 0.0f;
            rightSum = 0.0f;
            return retval;
        }
        public float GetAlbumGain()
        {
            return analyzeResult(albumResults);
        }
    }
}
