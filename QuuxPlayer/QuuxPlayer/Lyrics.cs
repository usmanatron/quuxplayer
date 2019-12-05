/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxPlayer
{
    internal delegate void LyricsCallback(string Lyrics);
    internal interface Lyrics
    {
        void GetLyrics(Track Track, LyricsCallback Callback);
    }
}
