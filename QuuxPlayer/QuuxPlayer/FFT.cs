/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Diagnostics;

namespace QuuxPlayer
{
    internal sealed class FFT
    {
        private int frameSize;
        private int resultSize;
        private int logFrameSize;
        private float[] xReal;
        private float[] xImg;
        private float[] fftL;
        private float[] fftR;

        private float[][] cosTable;
        private float[][] sinTable;
        private int[] bitRevTable;

        public FFT(int FrameSize)
        {
            frameSize = FrameSize;
            resultSize = frameSize / 2;
            logFrameSize = (int)Math.Log(FrameSize, 2.0);

            xReal = new float[frameSize];
            xImg = new float[frameSize];
            fftL = new float[resultSize];
            fftR = new float[resultSize];

            cosTable = new float[logFrameSize][];
            sinTable = new float[logFrameSize][];
            bitRevTable = new int[frameSize];

            for (int i = 0; i < logFrameSize; i++)
            {
                sinTable[i] = new float[frameSize];
                cosTable[i] = new float[frameSize];
            }

            for (int i = 0; i < frameSize; i++)
            {
                for (int j = 0; j < logFrameSize; j++)
                {
                    float arg = (float)(2.0 * Math.PI * bitReverse(i >> j, logFrameSize) / frameSize);
                    cosTable[j][i] = (float)Math.Cos(arg);
                    sinTable[j][i] = (float)Math.Sin(arg);
                }
                bitRevTable[i] = bitReverse(i, logFrameSize);
            }
        }

        public void DoFFT(short[] Input, float[] Result)
        {
            int halfFrameSize = resultSize;
            int logHalfFrameSize = logFrameSize - 1;
            float tReal;
            float tImg;
            float cos;
            float sin;
            int j;

            for (int i = 0; i < frameSize; i++)
            {
                xReal[i] = (float)Input[i];
                xImg[i] = 0.0f;
            }

            for (int i = 0; i < logFrameSize; i++)
            {
                j = 0;
                while (j < frameSize)
                {
                    for (int k = 1; k <= halfFrameSize; k++)
                    {
                        cos = cosTable[logHalfFrameSize][j];
                        sin = sinTable[logHalfFrameSize][j];
                        tReal = xReal[j + halfFrameSize] * cos + xImg[j + halfFrameSize] * sin;
                        tImg = xImg[j + halfFrameSize] * cos - xReal[j + halfFrameSize] * sin;
                        xReal[j + halfFrameSize] = xReal[j] - tReal;
                        xImg[j + halfFrameSize] = xImg[j] - tImg;
                        xReal[j] += tReal;
                        xImg[j] += tImg;
                        j++;
                    }
                    j += halfFrameSize;
                }
                logHalfFrameSize--;
                halfFrameSize /= 2;
            }
            j = 0;
            int r;
            while (j < frameSize)
            {
                r = bitRevTable[j];
                if (r > j)
                {
                    tReal = xReal[j];
                    tImg = xImg[j];
                    xReal[j] = xReal[r];
                    xImg[j] = xImg[r];
                    xReal[r] = tReal;
                    xImg[r] = tImg;
                }
                j++;
            }

            for (int i = 0; i < frameSize / 2; i++)
                Result[i] = 0.5f * Result[i] + (float)(Math.Sqrt(xReal[i] * xReal[i] + xImg[i] * xImg[i])) / frameSize;
        }

        private static int bitReverse(int Input, int LogN)
        {
            int halfJ;
            int j = Input;
            int k = 0;
            for (int i = 0; i < LogN; i++)
            {
                halfJ = j / 2;
                k = 2 * k + j - 2 * halfJ;
                j = halfJ;
            }
            return k;
        }
    }
}