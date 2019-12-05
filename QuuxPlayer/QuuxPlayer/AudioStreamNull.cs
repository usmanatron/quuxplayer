/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxPlayer
{
    internal class AudioStreamNull : AudioStream
    {
        public AudioStreamNull()
        {
        }
        public override StreamType StreamType
        {
            get { return StreamType.Null; }
        }
        public override float ReplayGainDB
        {
            get { return 0; }
        }
        public override int BitRate
        {
            get { return 0; }
        }
        public override void Close()
        {
            // do nothing
        }
        protected override void Dispose(bool disposing)
        {
            // do nothing
        }
        public override Track Track
        {
            get { return null; }
        }
        public override string URL
        {
            get { return String.Empty; }
        }
        public override ReplayGainMode ReplayGain
        {
            get { return ReplayGainMode.Off; }
            set { }
        }
        public override float ClippingRate
        {
            get { return 0; }
        }
        public override int Frequency
        {
            get { return Player.DEFAULT_SAMPLE_RATE; }
        }
        public override int ElapsedTime
        {
            get { return 0; }
            set { }
        }
        public override int Read(byte[] Buffer, int Offset, int Length)
        {
            for (int i = 0; i < Length; i++)
                Buffer[i] = 0;

            return Length;
        }
        public override int Read24(byte[] Buffer, int LengthIf16Bit)
        {
            for (int i = 0; i < LengthIf16Bit * 3 / 2; i++)
                Buffer[i] = 0;

            return LengthIf16Bit * 3 / 2;
        }
    }
}
