/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Tags;

namespace QuuxPlayer
{
    internal abstract class AudioStreamBass : AudioStream
    {
        private static readonly char[] gainChars = new char[] { ':', '=', '+' };

        protected const int DEFAULT_FRAME_SIZE = 0x4000;

        private static bool bassRegistered = false;
        private static List<int> plugInRefs;
        private static string userAgent = "QuuxPlayer " + System.Windows.Forms.Application.ProductVersion;
        [FixedAddressValueType()]
        public static IntPtr userAgentPtr;

        protected int streamRef;
        protected int frequency;
        protected float decoderGainDB;
        protected float totalGainDB;
        protected float gainLinear;
        protected float replayGainDB;
        protected int totalSamples;
        protected int clippedSamples;
        protected long samplesDecoded;

        private float[] equalizer;
        private int numEqBands;
        private bool eqOn;
        private float[] bufferF;
        private float[] eqBufferF;
        protected EqualizerDSP eq;
        protected bool mono;

        protected Track track;

        protected BASS_CHANNELINFO info;

        public AudioStreamBass(Track Track,
                               float GainDB,
                               float[] Equalizer,
                               int NumEqBands,
                               bool EqualizerOn,
                               ReplayGainMode ReplayGain)
        {

            this.track = Track;
            this.streamRef = 0;
            setupArrays();

            this.ReplayGain = ReplayGain;
            this.DecoderGainDB = GainDB;
            this.EqualizerOn = EqualizerOn;
            this.numEqBands = NumEqBands;
            this.equalizer = Equalizer;
        }

        protected void setupEqualizer()
        {
            this.eq = new EqualizerDSP(numEqBands, frequency, equalizer);
            updateEqGain();
        }

        public override void Close()
        {
            base.Close();
            Bass.BASS_ChannelStop(streamRef);
            Bass.BASS_MusicFree(streamRef);
            Bass.BASS_PluginFree(streamRef);
            Bass.BASS_StreamFree(streamRef);
        }
        public override int Frequency
        {
            get { return frequency; }
        }
        public override bool EqualizerOn
        {
            set { eqOn = value; }
        }
        public override void ResetEQ(int NumBands, float[] EqValues)
        {
            if (numEqBands != NumBands)
            {
                numEqBands = NumBands;
                eq = new EqualizerDSP(numEqBands, frequency, EqValues);
                updateEqGain();
            }
            else
            {
                eq.EqValues = EqValues;
            }
            updateGain();
        }
        public override float DecoderGainDB
        {
            set
            {
                decoderGainDB = value;
                updateGain();
            }
        }

        static AudioStreamBass()
        {
            RegisterBASS();
        }
        public override int ElapsedTime
        {
            get { return (int)(samplesDecoded * 500L / (long)frequency); }
        }
        public override int Read(byte[] Buffer, int Offset, int Length)
        {
            totalSamples = 0;
            clippedSamples = 0;

            if (streamRef != 0)
            {
                totalSamples = Bass.BASS_ChannelGetData(streamRef, bufferF, mono ? Length : Length * 2) / 4;

                if (totalSamples <= 0)
                {
                    totalSamples = 0;
                    return 0;
                }

                int j;
                if (mono)
                {
                    j = totalSamples * 2 - 2;
                    for (int i = totalSamples - 1; i >= 0; i--)
                    {
                        eqBufferF[j] = bufferF[i];
                        eqBufferF[j + 1] = bufferF[i];
                        j -= 2;
                    }
                    totalSamples *= 2;
                }

                if (eqOn)
                {
                    eq.EQ(eqBufferF, totalSamples);
                }
                else if (totalGainDB != 0.0f)
                {
                    for (int i = 0; i < totalSamples; i++)
                        eqBufferF[i] *= gainLinear;
                }

                for (int i = 0; i < totalSamples; i++)
                {
                    eqBufferF[i] *= 32768f;

                    if (eqBufferF[i] > 32767f)
                    {
                        eqBufferF[i] = 32767f;
                        clippedSamples++;
                    }
                    else if (eqBufferF[i] < -32768f)
                    {
                        eqBufferF[i] = -32768f;
                        clippedSamples++;
                    }
                }

                j = 0;
                int sample;
                int totalBytes = totalSamples * 2;
                for (int i = 0; i < totalBytes; i += 2)
                {
                    sample = to16Bit(eqBufferF[j]);

                    Buffer[i] = (byte)(sample & 0xFF);
                    Buffer[i + 1] = (byte)((sample >> 8) & 0xFF);
                    j++;
                }
                samplesDecoded += totalSamples;

                return totalBytes;
            }
            else
            {
                return 0;
            }
        }
        public override int Read24(byte[] Buffer, int LengthIf16Bit)
        {
            totalSamples = 0;
            clippedSamples = 0;

            if (streamRef != 0)
            {
                totalSamples = Bass.BASS_ChannelGetData(streamRef, bufferF, mono ? LengthIf16Bit : LengthIf16Bit * 2) / 4;

                if (totalSamples <= 0)
                {
                    totalSamples = 0;
                    return 0;
                }

                int j;
                if (mono)
                {
                    j = totalSamples * 2 - 2;
                    for (int i = totalSamples - 1; i >= 0; i--)
                    {
                        eqBufferF[j] = bufferF[i];
                        eqBufferF[j + 1] = bufferF[i];
                        j -= 2;
                    }
                    totalSamples *= 2;
                }

                if (eqOn)
                {
                    eq.EQ(eqBufferF, totalSamples);
                }
                else if (totalGainDB != 0.0f)
                {
                    for (int i = 0; i < totalSamples; i++)
                        eqBufferF[i] *= gainLinear;
                }

                for (int i = 0; i < totalSamples; i++)
                {
                    eqBufferF[i] *= 32768f;

                    if (eqBufferF[i] > 32767f)
                    {
                        eqBufferF[i] = 32767f;
                        clippedSamples++;
                    }
                    else if (eqBufferF[i] < -32768f)
                    {
                        eqBufferF[i] = -32768f;
                        clippedSamples++;
                    }
                }

                j = 0;
                int sample;
                int totalBytes = totalSamples * 3;
                for (int i = 0; i < totalBytes; i += 3)
                {
                    sample = to24Bit(eqBufferF[j]);

                    Buffer[i] = (byte)(sample & 0xFF);
                    Buffer[i + 1] = (byte)((sample >> 8) & 0xFF);
                    Buffer[i + 2] = (byte)((sample >> 16) & 0xFF);
                    j++;
                }

                samplesDecoded += totalSamples;

                return totalBytes;
            }
            else
            {
                return 0;
            }
        }

        public override float ClippingRate
        {
            get
            {
                if (totalSamples == 0)
                    return 0;
                else
                    return (float)clippedSamples / (float)totalSamples;
            }
        }
        public override float ReplayGainDB
        {
            get { return replayGainDB; }
        }
        public static void UnregisterBASS()
        {
            Bass.BASS_Free();
            bassRegistered = false;
        }
        public static void RegisterBASS()
        {
            if (!bassRegistered)
            {
                BassNet.Registration(StringUtil.Convert(StringUtil.Seq1), StringUtil.Convert(StringUtil.Seq2));
                Bass.BASS_Init(0, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);

                plugInRefs = new List<int>();
                plugInRefs.Add(Bass.BASS_PluginLoad("basswma.dll"));
                plugInRefs.Add(Bass.BASS_PluginLoad("bassflac.dll"));
                plugInRefs.Add(Bass.BASS_PluginLoad("bass_aac.dll"));
                plugInRefs.Add(Bass.BASS_PluginLoad("bass_alac.dll"));
                plugInRefs.Add(Bass.BASS_PluginLoad("bass_mpc.dll"));
                plugInRefs.Add(Bass.BASS_PluginLoad("basswv.dll"));
                plugInRefs.Add(Bass.BASS_PluginLoad("bass_ac3.dll"));
                plugInRefs.Add(Bass.BASS_PluginLoad("bass_ape.dll"));

                userAgentPtr = Marshal.StringToHGlobalAnsi(userAgent);
                Bass.BASS_SetConfigPtr(BASSConfig.BASS_CONFIG_NET_AGENT, userAgentPtr);
                Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_NET_PREBUF, 0); // so that we can display the buffering%
                Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_NET_PLAYLIST, 1);

                if (Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_WMA_PREBUF, 0) == false)
                {
                    Console.WriteLine("ERROR: " + Enum.GetName(typeof(BASSError), Bass.BASS_ErrorGetCode()));
                }

                bassRegistered = true;
            }
        }
        public static void FreeBassPlugins()
        {
            foreach (int i in plugInRefs)
                Bass.BASS_PluginFree(i);
        }
        protected void setupChannelFromStream()
        {
            if (streamRef != 0)
            {
                info = new BASS_CHANNELINFO();
                Bass.BASS_ChannelGetInfo(streamRef, info);
                frequency = info.freq;
                this.mono = (info.chans == 1);
            }
            else
            {
                frequency = 44100;
                mono = false;
            }
            setupEqualizer();
            updateGain();
        }
        protected void setupArrays()
        {
            bufferF = new float[DEFAULT_FRAME_SIZE];

            if (this.mono)
                eqBufferF = new float[DEFAULT_FRAME_SIZE];
            else
                eqBufferF = bufferF;
        }
        public static float from16Bit(int data)
        {
            float ret = (data >= 0x8000 ? (float)(data - 0x10000) : (float)data);
            System.Diagnostics.Debug.Assert(Math.Abs(data - to16Bit(ret)) < 2);
            return ret;
        }
        public static int to16Bit(float data)
        {
            return (data < 0) ? (int)(data + (float)0x10000) : (int)data;
        }
        public static int to24Bit(float data)
        {
            data *= ((float)0x100);
            return (data < 0) ? (int)(data + (float)0x1000000) : (int)data;
        }

        protected void updateGain()
        {
            totalGainDB = decoderGainDB + replayGainDB;
            gainLinear = (float)(Math.Pow(10.0, totalGainDB / 20.0));
            updateEqGain();
        }
        private void updateEqGain()
        {
            if (eq != null)
                eq.GainDB = totalGainDB;
        }
        public static float GetReplayGain(Track Track, ReplayGainMode Type, bool FallBack)
        {
            if (Type == ReplayGainMode.Off)
                return 0.0f;

            switch (Type)
            {
                case ReplayGainMode.Album:
                    if (!Track.HasReplayGainInfoAlbum)
                        if (Track.HasReplayGainInfoTrack)
                            return Track.ReplayGainTrack;
                        else
                            TrackWriter.LoadReplayGain(Track, BassTags.BASS_TAG_GetFromFile(Track.FilePath, true, false));
                    return Track.ReplayGainAlbum;
                case ReplayGainMode.Track:
                    if (!Track.HasReplayGainInfoTrack)
                        if (Track.HasReplayGainInfoAlbum)
                            return Track.ReplayGainAlbum;
                        else
                            TrackWriter.LoadReplayGain(Track, BassTags.BASS_TAG_GetFromFile(Track.FilePath, true, false));
                    return Track.ReplayGainTrack;
                default: // off
                    return 0.0f;
            }
        }
    }
}
