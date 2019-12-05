/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectSound;

namespace QuuxPlayer
{
    internal sealed class Player : IDisposable
    {
        private enum PlayMode { Stopped, Playing, Paused, Radio, RadioPaused }

        public const int READ_BUFFER_SIZE = 0x2000;
        public const int READ_BUFFER_SIZE_24 = 0x3000;
        public const int SPECTRUM_SAMPLE_SIZE_SMALL = 0x200;
        public const int SPECTRUM_SAMPLE_SIZE_NORMAL = 0x800;
        public const int DEFAULT_SAMPLE_RATE = 44100;

        public const int SCALE_FACTOR = 0x7FFF;

        private byte[] readBuffer16 = new byte[READ_BUFFER_SIZE];
        private byte[] readBuffer24 = new byte[READ_BUFFER_SIZE_24];

        private byte[] emptyReadBuffer24 = new byte[Player.READ_BUFFER_SIZE_24];
        private byte[] emptyReadBuffer16 = new byte[Player.READ_BUFFER_SIZE];

        private short[] emptySpectrum = new short[SPECTRUM_SAMPLE_SIZE_NORMAL];

        private AudioStream stream;

        private int matchFrequenciesDelayBytes = 0;

        private AudioStream Stream
        {
            get { return stream; }
            set
            {
                System.Diagnostics.Debug.Assert(value != null);
                stream = value;
            }
        }
        private AudioStream preloadStream = null;
        private AudioStream PreloadStream
        {
            get { return preloadStream; }
            set
            {
                System.Diagnostics.Debug.Assert(value != null);
                preloadStream = value;
            }
        }
        private bool latestSampleIs24Bit = false;

        private Output output = null;

        private int spectrumSampleSize;
        private SpectrumMode spectrumMode;

        private Stack<Track> playedTracks = new Stack<Track>();

        private int totalTime;

        private PlayMode playMode = PlayMode.Stopped;

        private bool playNextTrackNow = false;

        private short[] pcmLeft = new short[SPECTRUM_SAMPLE_SIZE_NORMAL];
        private short[] pcmRight = new short[SPECTRUM_SAMPLE_SIZE_NORMAL];
        private FFT fft;
        private SpectrumData spectrum = null;

        private float gainDB = 0.0f;
        private float volumeDB = 0.0f;
        private bool equalizerOn = false;
        private int numEqBands = 30;
        private float[] equalizer = null;
        private Controller controller;
        private System.Windows.Forms.Form parentForm;

        private ReplayGainMode replayGain;

        private Track playingTrack;

        private object preloadLock = new object();
        private string outputDeviceName = String.Empty;

        public Player(System.Windows.Forms.Form OwnerForm, string OutputDeviceName, SpectrumMode SpectrumMode)
        {
            Stream = AudioStream.NullStream;
            PreloadStream = AudioStream.NullStream;

            outputDeviceName = OutputDeviceName;
            parentForm = OwnerForm;

            PlayingTrack = null;
            NextTrack = null;

            spectrumMode = SpectrumMode;
            createSpectrum();

            System.Diagnostics.Debug.Assert(READ_BUFFER_SIZE / spectrumSampleSize >= 4);

            controller = Controller.GetInstance();

            Clock.DoOnMainThread(createOutput, 1000);
        }

        private void createOutput()
        {
            output = Output.GetOutput(outputDeviceName,
                                      DEFAULT_SAMPLE_RATE,
                                      parentForm,
                                      GetData16,
                                      GetData24,
                                      this);

            output.Mute = Setting.Mute;
            createSpectrumData();
        }
        public Track PlayingTrack
        {
            get { return playingTrack; }
            set
            {
                if (playingTrack != value)
                {
                    if (value != null && value.Equalizer != null)
                        controller.SetEqualizer(value.Equalizer);

                    playingTrack = value;
                    if ((value != null) && (playedTracks.Count == 0 || playedTracks.Peek() != value))
                        playedTracks.Push(value);
                }
            }
        }

        public Stack<Track> PlayedTracks
        {
            get { return playedTracks; }
        }
        public bool StopAfterThisTrack { get; set; }

        public Track NextTrack { get; set; }

        private void createSpectrum()
        {
            spectrumSampleSize = (spectrumMode == SpectrumMode.Small) ? SPECTRUM_SAMPLE_SIZE_SMALL : SPECTRUM_SAMPLE_SIZE_NORMAL;
            fft = new FFT(spectrumSampleSize);
        }

        public int TotalTime
        {
            get { return totalTime; }
        }
        public int ElapsedTime
        {
            get
            {
                return Stream.ElapsedTime;
            }
            set
            {
                Stream.ElapsedTime = Math.Max(0, Math.Min(totalTime, value));
            }
        }
        public bool Playing
        {
            get { return playMode != PlayMode.Stopped; }
        }
        public float GainDB
        {
            get { return gainDB; }
            set
            {
                if (gainDB != value)
                {
                    gainDB = value;
                    updateGainAndVolume();
                }
            }
        }
        public float VolumeDB
        {
            set
            {
                if (volumeDB != value)
                {
                    volumeDB = value;
                    updateGainAndVolume();
                }
            }
        }
        private void updateGainAndVolume()
        {
            Stream.DecoderGainDB = gainDB + volumeDB;
            PreloadStream.DecoderGainDB = gainDB + volumeDB;
        }
        public void ResetEqualizer(int NumBands, float[] EqValues)
        {
            equalizer = EqValues;
            numEqBands = NumBands;
            Stream.ResetEQ(NumBands, EqValues);
            PreloadStream.ResetEQ(NumBands, EqValues);
        }
        public bool EqualizerOn
        {
            get { return equalizerOn; }
            set
            {
                if (equalizerOn != value)
                {
                    equalizerOn = value;
                    Stream.EqualizerOn = value;
                    PreloadStream.EqualizerOn = value;
                }
            }
        }
        public void Mute(bool Muted)
        {
            Setting.Mute = Muted;
            if (output != null)
                output.Mute = Muted;
        }
        public SpectrumData Spectrum
        {
            get { return spectrum; }
        }
        public bool Clipping
        {
            get { return Stream.ClippingRate > 0.001f; }
        }
        public SpectrumMode SpectrumMode
        {
            get { return spectrumMode; }
            set
            {
                if (spectrumMode != value)
                {
                    spectrumMode = value;
                    createSpectrum();
                    createSpectrumData();
                }
            }
        }
        public byte[] EmptyReadBuffer16
        {
            get { return emptyReadBuffer16; }
        }
        public byte[] EmptyReadBuffer24
        {
            get { return emptyReadBuffer24; }
        }
        public void PreloadNextTrack(Track Track)
        {
            lock (preloadLock)
            {
                if (PreloadStream.Track != Track)
                {
                    PreloadStream.Close();
                    PreloadStream.Dispose();

                    if (Track != null && Track.ConfirmExists)
                    {
                        PreloadStream = new AudioStreamFile(Track,
                                                        gainDB + volumeDB,
                                                        equalizer,
                                                        numEqBands,
                                                        equalizerOn,
                                                        replayGain);

                        if (PreloadStream.Frequency < 0)
                        {
                            PreloadStream = AudioStream.NullStream;
                        }
                        NextTrack = Track;
                    }
                }
            }
        }
        public string OutputDeviceName
        {
            get { return outputDeviceName; }
            set
            {
                if (outputDeviceName != value)
                {
                    outputDeviceName = value;

                    if (output != null)
                    {
                        output.Kill();
                        Output old = output;
                        output = Output.GetOutput(OutputDeviceName,
                                                  Stream.Frequency,
                                                  parentForm,
                                                  GetData16,
                                                  GetData24,
                                                  this);
                        output.Mute = Setting.Mute;
                        old.Dispose();
                    }
                }
            }
        }
        public bool Play(Track Track)
        {
            while (output == null)
            {
                Lib.DoEvents(); // don't delete; could prevent a race condition w/ output startup
                System.Threading.Thread.Sleep(100);
            }

            if (Track == null)
            {
                return false;
            }
            else
            {
                if (playMode == PlayMode.Playing || playMode == PlayMode.Paused)
                {
                    NextTrack = Track;
                    playNextTrackNow = true;
                    if (output.Paused)
                        Resume();
                }
                else
                {
                    if (!startFileStream(Track))
                    {
                        killAudioStream();

                        controller.RequestAction(new QAction(QActionType.TrackFailed, Track));

                        return false;
                    }

                    if (output.Paused)
                        output.Resume();

                    playMode = PlayMode.Playing;

                    controller.RequestAction(QActionType.StartOfTrack);
                }
            }
            return true;
        }
        public bool PlayingRadio
        {
            get { return playMode == PlayMode.Radio || playMode == PlayMode.RadioPaused; }
        }
        public int BitRate
        {
            get
            {
                return stream.BitRate;
            }
        }
        public bool Play(RadioStation Station)
        {
            bool newStation = false;

            if (Stream.URL != Station.URL)
            {
                controller.RadioTrack = null;

                newStation = true;

                Stream s = Stream;
                Stream = new AudioStreamRadio(Station,
                                              output.Frequency,
                                              matchFrequencies,
                                              gainDB + volumeDB,
                                              equalizer,
                                              numEqBands,
                                              equalizerOn,
                                              controller.ReplayGain);
                s.Close();
                s.Dispose();
            }

            playMode = PlayMode.Radio;
            Paused = false;
            return newStation;
        }
        public void Seek(float Percentage)
        {
            this.ElapsedTime = (int)(Percentage * totalTime / 100);
        }
        public void Stop()
        {
            PlayingTrack = null;

            System.Threading.Thread.Sleep(100);

            killAudioStream();

            controller.RequestAction(QActionType.Stopped);

            if (output != null)
                output.Paused = false;
        }
        public void Pause()
        {
            output.Pause();
            controller.RequestAction(QActionType.Paused);
        }
        public void Resume()
        {
            output.Resume();
            controller.RequestAction(QActionType.Resumed);
        }
        public bool Paused
        {
            get { return playMode == PlayMode.Paused || playMode == PlayMode.RadioPaused; }
            set
            {
                switch (playMode)
                {
                    case PlayMode.Paused:
                        if (!value)
                        {
                            if (output.Paused)
                                Resume();
                            playMode = PlayMode.Playing;
                        }
                        break;
                    case PlayMode.Playing:
                        if (value)
                        {
                            if (!output.Paused)
                                Pause();
                            playMode = PlayMode.Paused;
                        }
                        break;
                    case PlayMode.Stopped:
                        break;
                    case PlayMode.Radio:
                        if (value)
                        {
                            if (!output.Paused)
                                Pause();
                            playMode = PlayMode.RadioPaused;
                        }
                        break;
                    case PlayMode.RadioPaused:
                        if (!value)
                        {
                            if (output.Paused)
                                Resume();
                            playMode = PlayMode.Radio;
                        }
                        break;
                }
            }
        }
        public ReplayGainMode ReplayGain
        {
            get { return replayGain; }
            set
            {
                if (replayGain != value)
                {
                    replayGain = value;
                    Stream.ReplayGain = value;
                    PreloadStream.ReplayGain = value;
                }
            }
        }
        public float ReplayGainDB
        {
            get { return Stream.ReplayGainDB; }
        }
        public SpectrumData GetSpectrumData()
        {
            if (spectrum == null)
            {
                return null;
            }
            else
            {
                if (playMode == PlayMode.Stopped || playMode == PlayMode.Paused || playMode == PlayMode.RadioPaused)
                {
                    for (int i = 0; i < spectrumSampleSize / 2; i++)
                    {
                        spectrum.LeftSpectrumBase[i] = 0f;
                        spectrum.RightSpectrumBase[i] = 0f;
                    }
                    spectrum.LeftVUBase = 0f;
                    spectrum.RightVUBase = 0f;
                }
                else if (latestSampleIs24Bit)
                {
                    int sampleNum = 0;
                    for (int i = 0; i < spectrumSampleSize * 6; i += 6)
                    {
                        pcmLeft[sampleNum] = (short)((readBuffer24[i + 2] << 8) + readBuffer24[i + 1]);
                        pcmRight[sampleNum] = (short)((readBuffer24[i + 5] << 8) + (readBuffer24[i + 4]));

                        sampleNum++;
                    }

                    fft.DoFFT(pcmLeft, spectrum.LeftSpectrumBase);
                    fft.DoFFT(pcmRight, spectrum.RightSpectrumBase);
                }
                else
                {
                    int sampleNum = 0;
                    for (int i = 0; i < spectrumSampleSize * 4; i += 4)
                    {
                        pcmLeft[sampleNum] = (short)((readBuffer16[i + 1] << 8) + readBuffer16[i]);
                        pcmRight[sampleNum] = (short)((readBuffer16[i + 3] << 8) + (readBuffer16[i + 2]));

                        sampleNum++;
                    }

                    fft.DoFFT(pcmLeft, spectrum.LeftSpectrumBase);
                    fft.DoFFT(pcmRight, spectrum.RightSpectrumBase);
                }
                return spectrum;
            }
        }
        public void Dispose()
        {
            try
            {
                Stop();
                System.Threading.Thread.Sleep(250);
                if (output != null)
                {
                    output.Kill();
                    output.Dispose();
                }
                stream.Close();
                stream.Dispose();
                stream = AudioStream.NullStream;
                preloadStream.Close();
                preloadStream.Dispose();
                preloadStream = AudioStream.NullStream;
                AudioStreamFile.FreeBassPlugins();
                AudioStreamFile.UnregisterBASS();
                OutputASIO.Unload();
            }
            catch { }
        }

        private void createSpectrumData()
        {
            spectrum = new SpectrumData(spectrumSampleSize / 2, (float)(output.Frequency / 2), spectrumMode);
        }
        private AudioStream oldStream = null;
        private bool startFileStream(Track Track)
        {
            try
            {
                oldStream = Stream;

                if ((Track == PreloadStream.Track) || File.Exists(Track.FilePath))
                {
                    PlayingTrack = Track;
                    totalTime = PlayingTrack.Duration;

                    if (oldStream != null && oldStream.Frequency != Track.SampleRate && output != null)
                    {
                        matchFrequenciesDelayBytes = output.BufferSize * 3 / 2; // factor of 3 to prevent 24 bit meltdown
                    }

                    if (Track == PreloadStream.Track)
                    {
                        Stream = PreloadStream;
                        System.Diagnostics.Debug.WriteLine("Preload Track Hit");
                    }
                    else
                    {
                        Stream = new AudioStreamFile(PlayingTrack,
                                                 gainDB + volumeDB,
                                                 equalizer,
                                                 numEqBands,
                                                 equalizerOn,
                                                 controller.ReplayGain);

                        System.Diagnostics.Debug.WriteLine("Preload Track Miss");

                        if (Stream.Frequency < 0)
                            return false;
                    }

                    PreloadStream = AudioStream.NullStream;

                    Clock.DoOnMainThread(closeOldStream, 1000);

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch { return false; }
        }

        private void matchFrequencies()
        {
            if (output.Frequency != Stream.Frequency)
            {
                output.Frequency = Stream.Frequency;
                createSpectrumData();
            }
        }
        private void closeOldStream()
        {
            if (oldStream != null)
            {
                oldStream.Close();
                oldStream.Dispose();
                oldStream = AudioStream.NullStream;
            }
        }

        private void informNextTrackStarting()
        {
            controller.RequestAction(QActionType.StartOfTrackAuto);
        }
        private void killAudioStream()
        {
            try
            {
                Stream s = Stream;
                Stream = AudioStream.NullStream;
                s.Close();
                s.Dispose();
                playMode = PlayMode.Stopped;
            }
            catch
            {
            }
        }
        private int GetData16(out byte[] Data)
        {
            latestSampleIs24Bit = false;

            bool done = playNextTrackNow;

            if (matchFrequenciesDelayBytes > 0)
            {
                int bytes = Math.Min(matchFrequenciesDelayBytes, READ_BUFFER_SIZE);
                
                matchFrequenciesDelayBytes -= bytes;
                
                bytes = AudioStream.NullStream.Read(readBuffer16, 0, bytes);
                
                Data = readBuffer16;

                if (matchFrequenciesDelayBytes == 0)
                    matchFrequencies();

                return bytes;
            }
            else
            {

                int bytes = Stream.Read(readBuffer16, 0, READ_BUFFER_SIZE);

                done |= (bytes == 0);

                if (done)
                {
                    if (!PlayingRadio)
                        advanceTrack();

                    bytes = AudioStream.NullStream.Read(readBuffer16, 0, READ_BUFFER_SIZE);
                }

                Data = readBuffer16;

                return bytes;
            }
        }
        private int GetData24(out byte[] Data)
        {
            latestSampleIs24Bit = true;

            bool done = playNextTrackNow;

            if (matchFrequenciesDelayBytes > 0)
            {
                int bytes = Math.Min(matchFrequenciesDelayBytes, READ_BUFFER_SIZE_24);

                matchFrequenciesDelayBytes -= bytes;

                bytes = AudioStream.NullStream.Read(readBuffer24, 0, bytes);

                Data = readBuffer24;

                if (matchFrequenciesDelayBytes == 0)
                    matchFrequencies();

                return bytes;
            }
            else
            {
                int bytes = Stream.Read24(readBuffer24, READ_BUFFER_SIZE);

                done |= (bytes == 0);

                if (done)
                {
                    if (!PlayingRadio)
                        advanceTrack();

                    bytes = AudioStream.NullStream.Read24(readBuffer24, READ_BUFFER_SIZE);
                }

                Data = readBuffer24;

                return bytes;
            }
        }
        private void advanceTrack()
        {
            controller.RequestAction(QActionType.EndOfTrack);

            if (!playNextTrackNow && !StopAfterThisTrack)
            {
                controller.RequestAction(QActionType.RequestNextTrack);
            }
            if (NextTrack != null && !StopAfterThisTrack)
            {
                Track next = NextTrack;
                NextTrack = null;

                if (startFileStream(next))
                {
                    if (playNextTrackNow)
                    {
                        controller.RequestAction(QActionType.StartOfTrack);
                    }
                    else
                    {
                        Clock.DoOnMainThread(informNextTrackStarting, 100);
                    }
                }
                else
                {
                    killAudioStream();
                    controller.RequestAction(new QAction(QActionType.TrackFailed, next));
                }
                playNextTrackNow = false;
            }
            else
            {
                this.Stop();
                this.StopAfterThisTrack = false;
                controller.RequestAction(QActionType.EndOfAllTracks);
            }
        }
    }
}
