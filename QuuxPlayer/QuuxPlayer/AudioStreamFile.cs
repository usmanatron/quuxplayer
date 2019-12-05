/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

using Un4seen.Bass;
using Un4seen.Bass.AddOn;
using Un4seen.Bass.AddOn.Fx;
using Un4seen.Bass.AddOn.Vis;
using Un4seen.Bass.AddOn.Wma;
using Un4seen.Bass.AddOn.Flac;
using Un4seen.Bass.AddOn.Wv;
using Un4seen.Bass.AddOn.Alac;
using Un4seen.Bass.AddOn.Ac3;
using Un4seen.Bass.AddOn.Aac;
using Un4seen.Bass.AddOn.Mpc;
using Un4seen.Bass.AddOn.Ape;
using Un4seen.Bass.AddOn.Tags;

namespace QuuxPlayer
{
    internal sealed class AudioStreamFile : AudioStreamBass
    {
        public enum TagType { Artist, Album, Title, Composer, Duration, Grouping, Genre, AlbumArtist, BitRate, Encoder, TrackNum, DiskNum, Year, Compilation, Picture, NumChannels, SampleRate, Rating }
        
        private ReplayGainMode replayGain;
        private string filePath;

        public AudioStreamFile(Track Track,
                           float GainDB,
                           float[] Equalizer,
                           int NumEqBands,
                           bool EqualizerOn,
                           ReplayGainMode ReplayGain)
            : base(Track, GainDB, Equalizer, NumEqBands, EqualizerOn, ReplayGain)
        {
            
            this.filePath = track.FilePath;
            
            
            if (filePath != String.Empty)
            {
                Bass.BASS_Init(0, 0, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
                streamRef = Bass.BASS_StreamCreateFile(filePath, 0, 0, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT);
            }

            setupChannelFromStream();
            
            TrackWriter.UpdateTrackInfo(track, info);
        }

        public override StreamType StreamType
        {
            get { return StreamType.Normal; }
        }

        public override int BitRate
        {
            get { return track.Bitrate; }
        }

        public override Track Track
        {
            get { return track; }
        }
        public override string URL
        {
            get { return String.Empty; }
        }
        public override ReplayGainMode ReplayGain
        {
            get { return replayGain; }
            set
            {
                if (replayGain != value)
                {
                    replayGain = value;
                    setReplayGainDB();
                }
            }
        }
        private void setReplayGainDB()
        {
            switch (replayGain)
            {
                case ReplayGainMode.Off:
                    replayGainDB = 0.0f;
                    break;
                default:
                    replayGainDB = GetReplayGain(track, replayGain, true);
                    break;
            }
            updateGain();
        }
        public override int ElapsedTime
        {
            get
            {
                return base.ElapsedTime;
            }
            set
            {
                if (streamRef != 0)
                {
                    Bass.BASS_ChannelSetPosition(streamRef, ((double)value / 1000.0));
                    samplesDecoded = (long)value * (long)frequency / 500L;
                }
            }
        }
    }
}
