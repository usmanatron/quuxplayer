/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
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
    internal class AudioStreamRadio : AudioStreamBass
    {
        private bool isWMA;
        private bool closing = false;
        private RadioStation station;
        private Controller controller;
        public bool buffered = false;
        private int presumedFreq = 0;
        private Callback frequencyMismatchDelegate;

        public AudioStreamRadio(RadioStation Station,
                                int PresumedFrequency,
                                Callback FrequencyMismatch,
                                float GainDB,
                                float[] Equalizer,
                                int NumEqBands,
                                bool EqualizerOn,
                                ReplayGainMode ReplayGain) : base(null, GainDB, Equalizer, NumEqBands, EqualizerOn, ReplayGain)
        {

            this.replayGainDB = -6.0f; // Radio is loud, clips a bunch

            controller = Controller.GetInstance();
            Controller.ShowMessageUntilReplaced(Localization.Get(UI_Key.Radio_Connecting));
            
            this.station = Station;
            
            presumedFreq = PresumedFrequency;
            frequencyMismatchDelegate = FrequencyMismatch;

            Clock.DoOnNewThread(setup);
        }
        private void setup()
        {
            streamRef = 0;
            
            controller.RequestAction(QActionType.Resumed);

            int maxLoops = 4;

            for (int i = 0; (i < maxLoops) && (streamRef == 0); i++)
            {
                streamRef = Bass.BASS_StreamCreateURL(station.URL, 0, BASSFlag.BASS_STREAM_STATUS | BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT, myStreamCreateURL, IntPtr.Zero);

                if (closing)
                {
                    controller.RequestAction(QActionType.Stop);
                    return;
                }

                if (streamRef == 0)
                {
                    isWMA = true;
                    streamRef = BassWma.BASS_WMA_StreamCreateURL(station.URL, 0, 0, BASSFlag.BASS_STREAM_STATUS | BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT);
                }

                if (closing)
                {
                    controller.RequestAction(QActionType.Stop);
                    return;
                }

                if (streamRef == 0 && i < maxLoops - 1)
                {
                    System.Threading.Thread.Sleep(500);
                }
            }

            if (streamRef == 0)
            {
                Radio.PlayingStation = null;
                controller.RequestAction(QActionType.RadioFailed);
                return;
            }

            setupChannelFromStream();

            if (Frequency != presumedFreq)
                frequencyMismatchDelegate();

            syncEvent = new SYNCPROC(MetaSync);
            Bass.BASS_ChannelSetSync(streamRef, BASSSync.BASS_SYNC_META, 0, syncEvent, IntPtr.Zero);
            Bass.BASS_ChannelSetSync(streamRef, BASSSync.BASS_SYNC_WMA_CHANGE, 0, syncEvent, IntPtr.Zero);

            myStreamCreateURL = new DOWNLOADPROC(MyDownloadProc);

            try
            {
                // might throw up on certains chars
                tagInfo = new TAG_INFO(station.URL);
            }
            catch
            {
            }

            prebuffer();

            buffered = true;

            Radio.PlayingStation = station;
            controller.RadioTrack = null;

            readStreamTags();

            if (station.StreamType == StationStreamType.None)
                setStationStreamType();

            controller.RequestAction(QActionType.RadioStreamStarted);
        }
        private void setStationStreamType()
        {
            switch (info.ctype)
            {
                case BASSChannelType.BASS_CTYPE_UNKNOWN:
                    station.StreamType = StationStreamType.None;
                    break;
                case BASSChannelType.BASS_CTYPE_STREAM_OGG:
                    station.StreamType = StationStreamType.OGG;
                    break;
                case BASSChannelType.BASS_CTYPE_STREAM_MP3:
                case BASSChannelType.BASS_CTYPE_STREAM_WMA_MP3:
                    station.StreamType = StationStreamType.MP3;
                    break;
                case BASSChannelType.BASS_CTYPE_STREAM_WMA:
                    station.StreamType = StationStreamType.WMA;
                    break;
                case BASSChannelType.BASS_CTYPE_STREAM_AAC:
                    station.StreamType = StationStreamType.AAC;
                    break;
                default:
                    break;
            }
        }

        public override int Read(byte[] Buffer, int Offset, int Length)
        {
            if (buffered)
                return base.Read(Buffer, Offset, Length);
            else
                return readNull(Buffer, Length);
        }
        public override int Read24(byte[] Buffer, int LengthIf16Bit)
        {
            if (buffered)
                return base.Read24(Buffer, LengthIf16Bit);
            else
                return readNull24(Buffer, LengthIf16Bit);
        }

        public override StreamType StreamType
        {
            get { return StreamType.Radio; }
        }

        public override float ReplayGainDB
        {
            get { return 0; }
        }
        public override Track Track
        {
            get { return null; }
        }
        public override string URL
        {
            get { return station.URL; }
        }
        public override int BitRate
        {
            get { return station.BitRate; }
        }
        public override ReplayGainMode ReplayGain
        {
            get { return ReplayGainMode.Off; }
            set { }
        }
        public override int ElapsedTime
        {
            get { return 0; }
            set { }
        }

		private DOWNLOADPROC myStreamCreateURL;
		private TAG_INFO tagInfo;
		private SYNCPROC syncEvent;
        private bool readTags(string[] Tags)
        {
            List<string> tags = new List<string>(Tags);

            if (station.Name.Length == 0)
            {
                string nameMarker = "icy-name:";
                string name = tags.FirstOrDefault(s => s.StartsWith(nameMarker, StringComparison.OrdinalIgnoreCase));
                if (name != null)
                {
                    station.Name = name.Substring(nameMarker.Length).Trim();
                    Radio.StationNameChanged();
                }
            }
            if (station.BitRate == 0)
            {
                string bitRateMarker = "icy-br:";
                string bitRate = tags.FirstOrDefault(s => s.StartsWith(bitRateMarker, StringComparison.OrdinalIgnoreCase));
                if (bitRate != null)
                {
                    int br;
                    if (Int32.TryParse(bitRate.Substring(bitRateMarker.Length).Trim(), out br))
                    {
                        station.BitRate = br;
                        Radio.InvalidateInstance();
                    }
                }
            }
            if (station.Genre.Length == 0)
            {
                string genreMarker = "icy-genre:";
                string genre = tags.FirstOrDefault(s => s.StartsWith(genreMarker, StringComparison.OrdinalIgnoreCase));
                if (genre != null)
                {
                    station.Genre = genre.Substring(genreMarker.Length).Trim();
                    Radio.StationGenreChanged();
                }
            }
            return false;
        }
        private void readStreamTags()
        {
            // get the meta tags (manually - will not work for WMA streams here)
            string[] icy = Bass.BASS_ChannelGetTagsICY(streamRef);
            if (icy == null)
            {
                // try http...
                icy = Bass.BASS_ChannelGetTagsHTTP(streamRef);
            }

            if (icy != null && readTags(icy))
                return;

            // get the initial meta data (streamed title...)
            icy = Bass.BASS_ChannelGetTagsMETA(streamRef);
            if (icy != null && readTags(icy))
                return;

            // an ogg stream meta can be obtained here
            icy = Bass.BASS_ChannelGetTagsOGG(streamRef);
            if (icy != null && readTags(icy))
                return;

            getTagsFromURL();
        }
        public override void Close()
        {
            closing = true;

            base.Close();
            controller.RadioTrack = null;
            Radio.PlayingStation = null;
        }
        private void prebuffer()
        {
            int maxBufferTime = 4000;
            int sleepTime = 50;
            int maxCount = maxBufferTime / sleepTime;
            int count = 0;

            if (isWMA)
            {
                long oldLen = -10000L;

                // display buffering for WMA...
                while (count++ < maxCount)
                {
                    long len = Bass.BASS_StreamGetFilePosition(streamRef, BASSStreamFilePosition.BASS_FILEPOS_WMA_BUFFER);
                
                    if (len == -1L) // typical for WMA
                        break;
                    // percentage of buffer filled
                    if (len > 75L)
                        break; // over 75% full, enough

                    if (len != oldLen) // don't slam the updates if no change
                    {
                        Controller.ShowMessage(Localization.Get(UI_Key.Radio_Buffering, len));
                        oldLen = len;
                    }
                    System.Threading.Thread.Sleep(sleepTime);
                }
            }
            else
            {
                float oldProgress = -10000F;

                while (count++ < maxCount)
                {
                    long len = Bass.BASS_StreamGetFilePosition(streamRef, BASSStreamFilePosition.BASS_FILEPOS_END);
                    
                    if (len == -1)
                        break;
                    
                    float progress =
                        (
                            Bass.BASS_StreamGetFilePosition(streamRef, BASSStreamFilePosition.BASS_FILEPOS_DOWNLOAD) -
                            Bass.BASS_StreamGetFilePosition(streamRef, BASSStreamFilePosition.BASS_FILEPOS_CURRENT)
                        ) * 100f / len;

                    if (progress > 75f)
                    {
                        break; // over 75% full, enough
                    }

                    if (Math.Abs(progress - oldProgress) > 0.1F)
                    {
                        Controller.ShowMessage(Localization.Get(UI_Key.Radio_Buffering, progress));
                        oldProgress = progress;
                    }
                    System.Threading.Thread.Sleep(sleepTime);
                }
            }
            Controller.ShowMessage(Localization.Get(UI_Key.Radio_Buffered));
        }
       
        private void MyDownloadProc(IntPtr buffer, int length, IntPtr user)
		{
            if (buffer != IntPtr.Zero && length == 0)
            {
                Controller.ShowMessage(Marshal.PtrToStringAnsi(buffer));
            }
		}
        private void MetaSync(int handle, int channel, int data, IntPtr user)
        {
            if (tagInfo != null)
            {
                if (tagInfo.UpdateFromMETA(Bass.BASS_ChannelGetTags(channel, BASSTag.BASS_TAG_META), false, false))
                {
                    getTagsFromURL();
                }
            }
        }

        private void getTagsFromURL()
        {
            if (BassTags.BASS_TAG_GetFromURL(streamRef, tagInfo))
            {
                int trackNum = 0;
                Int32.TryParse(tagInfo.track, out trackNum);
                int year = 0;
                Int32.TryParse(tagInfo.year, out year);
                Track t = new Track(0,
                                    tagInfo.title + tagInfo.artist, // need something since equals uses filepath
                                    Track.FileType.None,
                                    tagInfo.title,
                                    String.Empty,
                                    tagInfo.artist,
                                    tagInfo.albumartist,
                                    tagInfo.composer,
                                    String.Empty,
                                    String.Empty,
                                    0,
                                    trackNum,
                                    0,
                                    year,
                                    0,
                                    0,
                                    tagInfo.bitrate,
                                    0,
                                    false,
                                    DateTime.Now,
                                    DateTime.Now,
                                    DateTime.Now,
                                    tagInfo.encodedby,
                                    tagInfo.channelinfo.chans,
                                    tagInfo.channelinfo.sample,
                                    ChangeType.None,
                                    null,
                                    float.MinValue,
                                    float.MinValue);

                if (station.Name.Length == 0 && tagInfo.album.Trim().Length > 0)
                {
                    station.Name = tagInfo.album.Trim();
                    Radio.StationNameChanged();
                }
                if (station.Name.Length == 0 && tagInfo.title.Trim().Length > 0)
                {
                    station.Name = tagInfo.title.Trim();
                    Radio.StationNameChanged();
                }
                if (station.Genre.Length == 0 && tagInfo.genre.Trim().Length > 0)
                {
                    station.Genre = tagInfo.genre.Trim();
                    Radio.StationGenreChanged();
                }

                if (tagInfo.bitrate > 0)
                    station.BitRate = tagInfo.bitrate;

                Radio.InvalidateInstance();
                controller.RadioTrack = t;
                controller.Invalidate();
            }
        }
    }
}
