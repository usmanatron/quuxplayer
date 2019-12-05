/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Un4seen.Bass;

namespace QuuxPlayer
{
    internal enum StreamType { Null, Normal, Radio }

    internal abstract class AudioStream : Stream
    {
        public static AudioStream NullStream = new AudioStreamNull();

        public abstract Track Track
        { get; }
        public abstract int ElapsedTime
        { get; set; }
        public abstract ReplayGainMode ReplayGain
        { get; set; }
        public abstract float ReplayGainDB
        { get; }
        public abstract float ClippingRate
        { get; }
        public abstract StreamType StreamType
        { get; }
        public abstract string URL
        { get; }
        public abstract int Frequency
        { get; }

        public virtual bool EqualizerOn
        {
            set { }
        }
        public virtual void ResetEQ(int NumBands, float[] EqValues)
        {
        }
        public abstract int Read24(byte[] Buffer, int LengthIf16Bit);

        public override bool CanRead
        { get { return true; } }
        public override bool CanSeek
        { get { return false; } }
        public override bool CanWrite
        { get { return false; } }
        public override void Write(byte[] buffer, int offset, int count)
        {
        }
        public override long Length
        {
            get { return 0; }
        }
        public override long Position
        {
            get { return 0; }
            set { }
        }
        public abstract int BitRate
        { get; }
        
        public virtual float DecoderGainDB
        {
            set { }
        }
        
        public override void Flush()
        {
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }
        public override void SetLength(long value)
        {
        }

        protected int readNull(byte[] Buffer, int Length)
        {
            for (int i = 0; i < Length; i++)
                Buffer[i] = 0;

            return Length;
        }
        protected int readNull24(byte[] Buffer, int LengthIf16Bit)
        {
            for (int i = 0; i < LengthIf16Bit * 3 / 2; i++)
                Buffer[i] = 0;

            return LengthIf16Bit * 3 / 2;
        }

    }
}
