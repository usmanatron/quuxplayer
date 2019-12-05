/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal static class KeyDefs
    {
        public const Keys AdvanceScreen = Keys.Space;
        public const Keys Exit = Keys.X;
        public const Keys Cancel = Keys.Escape;

        public const Keys Enter = Keys.Enter;
        public const Keys Play = Keys.P;
        public const Keys Pause = Keys.U;
        public const Keys Stop = Keys.S;
        public const Keys Next = Keys.N;
        public const Keys Previous = Keys.V;
        public const Keys PlayThisAlbum = Keys.Z;
        public const Keys FocusSearchBox = Keys.E;

        public const Keys Playlists = Keys.D1;
        public const Keys Genres = Keys.D2;
        public const Keys Artists = Keys.D3;
        public const Keys Albums = Keys.D4;
        public const Keys Years = Keys.D5;
        public const Keys Groupings = Keys.D6;
        
        public const Keys One = Keys.D1;
        public const Keys Two = Keys.D2;
        public const Keys Three = Keys.D3;
        public const Keys Four = Keys.D4;
        public const Keys Five = Keys.D5;
        public const Keys Zero = Keys.D0;

        public const Keys SelectAllOrNone = Keys.A;
        public const Keys ShowPlayingTrack = Keys.C;

        public const Keys Podcasts = Keys.O;

        public const Keys Rename = Keys.F2;
        public const Keys ClearAllFilters = Keys.F3;
        public const Keys FilterSelected = Keys.F4;
        public const Keys Shuffle = Keys.F5;
        public const Keys AutoPlaylistAction = Keys.F6;
        public const Keys PlaylistAction = Keys.F7;
        public const Keys FileInfo = Keys.F8;
        public const Keys ShowInfoFromInternet = Keys.F9;
        public const Keys PlaySelectedNext = Keys.F10;
        public const Keys HTPCMode = Keys.F11;
        public const Keys ShowEqualizer = Keys.F12;

        public const Keys Gain = Keys.G;

        public const Keys MiniPlayer = Keys.OemPipe;

        public const Keys VolUp = Keys.OemPeriod;
        public const Keys VolDown = Keys.Oemcomma;
        public const Keys Mute = Keys.M;
        public const Keys Radio = Keys.R;

        public const Keys Play2 = Keys.Play;
        public const Keys PlayPause = Keys.MediaPlayPause;
        public const Keys Stop2 = Keys.MediaStop;
        public const Keys Next2 = Keys.MediaNextTrack;
        public const Keys Previous2 = Keys.MediaPreviousTrack;
        public const Keys VolDown2 = Keys.VolumeDown;
        public const Keys VolUp2 = Keys.VolumeUp;

        public const Keys ScanBack = Keys.T;
        public const Keys ScanFwd = Keys.Y;

        public const Keys GainDown = Keys.OemOpenBrackets;
        public const Keys GainUp = Keys.OemCloseBrackets;

        public const Keys Delete = Keys.Delete;

        public const Keys PageUp = Keys.PageUp;
        public const Keys PageDown = Keys.PageDown;
        public const Keys Home = Keys.Home;
        public const Keys End = Keys.End;

        public const Keys MoveUp = Keys.Up;
        public const Keys MoveDown = Keys.Down;
        public const Keys MoveLeft = Keys.Left;
        public const Keys MoveRight = Keys.Right;

        public const Keys PreviousFilter = Keys.J;
        public const Keys NextFilter = Keys.L;
        public const Keys ReleaseCurrentFilter = Keys.K;
        public const Keys ReleaseAllFilters = Keys.I;
        public const Keys ShowFilterIndex = Keys.OemQuestion;
    }
}
