/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ReplayGainAnalyzer;

namespace QuuxPlayer
{
    internal enum ReplayGainMode { Off, Album, Track }

    internal class ReplayGain
    {
        private const int READ_BUFFER_SIZE = 0x2000;

        private List<Track> tracks;
        private bool cancel;
        private Callback callback;
        private int numTracks;
        private int numTracksDone;
        private ReplayGainAnalyzer.ReplayGainAnalyzer rga;

        public ReplayGain(List<Track> Tracks, Callback Callback)
        {
            rga = new ReplayGainAnalyzer.ReplayGainAnalyzer();

            callback = Callback;
            tracks = Tracks;
            cancel = false;
            numTracks = Tracks.Count;
            numTracksDone = 0;
        }

        public void DoReplayGainAnalysis()
        {
            try
            {
                Controller.ShowMessage("Starting volume leveling analysis...");

                List<List<Track>> albums = partitionByAlbum(tracks);

                byte[] buffer = new byte[READ_BUFFER_SIZE];
                float[] left = new float[READ_BUFFER_SIZE / 4];
                float[] right = new float[READ_BUFFER_SIZE / 4];
                int numSamples;

                foreach (List<Track> album in albums)
                {
                    try
                    {
                        if (rga.InitGainAnalysis(album[0].SampleRate))
                        {
                            foreach (Track t in album)
                            {
                                AudioStream stream = new AudioStreamFile(t, 0, new float[10], 10, false, ReplayGainMode.Off);

                                while ((numSamples = stream.Read(buffer, 0, READ_BUFFER_SIZE)) > 0)
                                {
                                    int inputBufferSize = numSamples / 4;
                                    for (int i = 0; i < inputBufferSize; i++)
                                    {
                                        left[i] = AudioStreamBass.from16Bit(buffer[i * 4 + 1] * 0x100 + buffer[i * 4]);
                                        right[i] = AudioStreamBass.from16Bit(buffer[i * 4 + 3] * 0x100 + buffer[i * 4 + 2]);
                                    }
                                    rga.AnalyzeSamples(left, right, inputBufferSize);
                                    if (cancel)
                                    {
                                        callback();
                                        return;
                                    }
                                }
                                stream.Close();
                                t.ReplayGainTrack = rga.GetTrackGain();
                                numTracksDone++;
                                Controller.ShowMessage(String.Format("{0}/{1}: Volume analyzed for '{2}'", numTracksDone.ToString(), numTracks.ToString(), t.ToShortString()));
                            }
                            float albumGain = rga.GetAlbumGain();
                            foreach (Track t in album)
                            {
                                t.ReplayGainAlbum = albumGain;
                            }
                            if (Setting.WriteReplayGainTags)
                            {
                                foreach (Track t in album)
                                {
                                    t.ChangeType |= ChangeType.WriteTags;
                                }
                                TrackWriter.AddToUnsavedTracks(album);
                                TrackWriter.Start();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            callback();
        }
        public void Cancel()
        {
            cancel = true;
        }
        private static List<List<Track>> partitionByAlbum(List<Track> Tracks)
        {
            List<Track> tracks = Tracks.ToList();

            tracks.Sort(new Comparison<Track>((a, b) => MainGroupComparer(a, b)));

            List<List<Track>> ret = new List<List<Track>>();

            if (tracks.Count < 1)
                return ret;

            if (tracks.Count == 1)
            {
                ret.Add(new List<Track>() { tracks[0] });
                return ret;
            }

            int i = 0;
            List<Track> tt = new List<Track>();
            ret.Add(tt);
            do
            {
                tt.Add(tracks[i++]);
                if (!isSameAlbum(tracks[i], tracks[i - 1]))
                {
                    tt = new List<Track>();
                    ret.Add(tt);
                }
            }
            while (i < tracks.Count - 1);
            
            tt.Add(tracks.Last());

            return ret;
        }
        public static int MainGroupComparer(Track T1, Track T2)
        {
            int comp = String.Compare(T1.MainGroup, T2.MainGroup, StringComparison.OrdinalIgnoreCase);

            if (comp == 0)
            {
                comp = String.Compare(T1.Album, T2.Album, StringComparison.OrdinalIgnoreCase);
                if (comp == 0)
                {
                    comp = T1.SampleRate.CompareTo(T2.SampleRate);
                    if (comp == 0)
                        comp = T1.TrackNum.CompareTo(T2.TrackNum);
                }
            }
            return comp;
        }
        private static bool isSameAlbum(Track T1, Track T2)
        {
            return String.Compare(T1.MainGroup, T2.MainGroup, StringComparison.OrdinalIgnoreCase) == 0 &&
                   String.Compare(T1.Album, T2.Album, StringComparison.OrdinalIgnoreCase) == 0 &&
                   T1.SampleRate == T2.SampleRate;
        }
    }
}
