/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxPlayer
{
    internal enum StationStreamType { None, MP3, WMA, OGG, AAC };

    internal class RadioStation
    {
        private static string[] streamTypeArray;

        public string Name { get; set; }
        public string URL { get; set; }
        public string Genre { get; set; }
        public int BitRate { get; set; }
        public StationStreamType StreamType { get; set; }

        static RadioStation()
        {
            streamTypeArray = new string[] { String.Empty, "MP3", "WMA", "OGG", "AAC+" };
        }

        public RadioStation(string URL)
        {
            this.URL = URL;
            this.Name = String.Empty;
            this.Genre = String.Empty;
            this.BitRate = 0;
            this.StreamType = StationStreamType.None;
        }
        public RadioStation(string URL, string Name)
        {
            this.URL = URL;
            this.Name = Name;
            this.Genre = String.Empty;
            this.BitRate = 0;
            this.StreamType = StationStreamType.None;
        }
        public RadioStation(string Name, string URL, string Genre, int BitRate, StationStreamType StreamType)
        {
            this.Name = Name;
            this.URL = URL;
            this.Genre = Genre;
            this.BitRate = BitRate;
            this.StreamType = StreamType;
        }
        public override string ToString()
        {
            return this.Name + " " + this.URL;
        }
        public string BitRateString
        {
            get { return BitRate.ToString() + " kbps " + this.StreamTypeString; }
        }
        public string StreamTypeString
        {
            get { return StreamTypeArray[(int)this.StreamType]; }
        }

        public static string[] StreamTypeArray
        {
            get { return streamTypeArray; }
        }
        private static char[] splitChars = new char[] { ' ' };
        public bool Matches(string Input)
        {
            if (Input.Contains(' '))
            {
                string[] input = Input.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in input)
                    if (!this.matches(s))
                        return false;

                return true;
            }
            else
            {
                return matches(Input);
            }
        }
        private bool matches(string Input)
        {
            if (Input[0] == '-')
                return this.Name.IndexOf(Input.Substring(1), StringComparison.OrdinalIgnoreCase) < 0 &&
                       this.Genre.IndexOf(Input.Substring(1), StringComparison.OrdinalIgnoreCase) < 0;
            else
                return this.Name.IndexOf(Input, StringComparison.OrdinalIgnoreCase) >= 0 ||
                       this.Genre.IndexOf(Input, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        public static List<RadioStation> DefaultList
        {
            get
            {
                List<RadioStation> rr = new List<RadioStation>();
                addStations(rr);
                return rr;
            }
        }

        public override bool Equals(object obj)
        {
            RadioStation other = obj as RadioStation;
            if (other == null)
                return false;
            else
                return String.Compare(this.URL, other.URL, StringComparison.OrdinalIgnoreCase) == 0;
        }
        public override int GetHashCode()
        {
            return this.URL.GetHashCode();
        }

        private static void addStations(List<RadioStation> rr)
        {
            rr.Add(new RadioStation(
                "BBC Radio 1",
                "http://bbc.co.uk/radio/listen/live/r1.asx",
                "Pop",
                0,
                (StationStreamType)0));
            rr.Add(new RadioStation(
                "BBC Radio 2",
                "http://bbc.co.uk/radio/listen/live/r2.asx",
                "Pop",
                0,
                (StationStreamType)0));
            rr.Add(new RadioStation(
                "BBC Radio 3",
                "http://bbc.co.uk/radio/listen/live/r3.asx",
                "Classical and Jazz",
                0,
                (StationStreamType)0));
            rr.Add(new RadioStation(
                "BBC Radio 4",
                "http://bbc.co.uk/radio/listen/live/r4.asx",
                "News",
                0,
                (StationStreamType)0));
            //rr.Add(new RadioStation(
            //    ".977 - Jayne.fm - The best mix of music with an attitude",
            //    "http://www.977music.com/tunein/web/jaynefm.asx",
            //    "Pop",
            //    65,
            //    (StationStreamType)2));
            rr.Add(new RadioStation(
                ".977 - The Alternative Channel",
                "http://www.977music.com/tunein/web/rock.asx",
                "Alternative",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                ".977 - The Classic Rock Channel",
                "http://www.977music.com/tunein/web/classicrock.asx",
                "Rock",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                ".977 - The Comedy Channel (Explicit Content)",
                "http://www.977music.com/tunein/web/comedy.asx",
                "Comedy",
                96,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                ".977 - The Hitz Channel",
                "http://scfire-ntc-aa01.stream.aol.com:80/stream/1074",
                "Top 40",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                ".977 - The Mix Channel",
                "http://www.977music.com/tunein/web/mix.asx",
                "Pop",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                ".977 - The Oldies Channel",
                "http://www.977music.com/tunein/web/oldies128.asx",
                "Oldies",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                ".977 - The Smooth Jazz Channel",
                "http://www.977music.com/tunein/web/smoothjazz.asx",
                "Jazz",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                ".977 The 80s Channel",
                "http://www.977music.com/tunein/web/80s.asx",
                "1980s",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                ".977 The 90s Channel",
                "http://www.977music.com/tunein/web/90s.asx",
                "1990s",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                ".977 The Country Channel",
                "http://www.977music.com/tunein/web/country.asx",
                "Country",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                ".977 The Hitz Channel",
                "http://www.977music.com/tunein/web/hitz.asx",
                "Pop",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "1.FM -  Dance Hits",
                "http://www.1.fm/TuneIn/SC/dance64k/Listen.aspx",
                "Dance",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - 50s and 60s",
                "http://www.1.fm/TuneIn/SC/60s64k/Listen.aspx",
                "Oldies",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - 70s",
                "http://www.1.fm/TuneIn/SC/70s64k/Listen.aspx",
                "1970s",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - 80s Channel",
                "http://www.1.fm/TuneIn/SC/80s64k/Listen.aspx",
                "1980s",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Absolute Country Hits",
                "http://www.1.fm/TuneIn/SC/acountry64k/Listen.aspx",
                "Country",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Acappella",
                "http://www.1.fm/TuneIn/SC/acpl64k/Listen.aspx",
                "Spiritual",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Afterbeat Electronica",
                "http://www.1.fm/TuneIn/SC/electronica64k/Listen.aspx",
                "Electronic",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Always Christmas",
                "http://www.1.fm/TuneIn/SC/christmas64k/Listen.aspx",
                "Holiday",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Back To The 80s",
                "http://www.1.fm/TuneIn/SC/back280s64k/Listen.aspx",
                "1980s",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Bay Smooth Jazz",
                "http://www.1.fm/TuneIn/SC/sjazz64k/Listen.aspx",
                "Jazz",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Blues",
                "http://www.1.fm/TuneIn/SC/blues64k/Listen.aspx",
                "Blues",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Bombay Beats",
                "http://www.1.fm/TuneIn/SC/bb64k/Listen.aspx",
                "World",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Channel X",
                "http://www.1.fm/TuneIn/SC/x64k/Listen.aspx",
                "Alternative",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Classic Country",
                "http://www.1.fm/TuneIn/SC/ccountry64k/Listen.aspx",
                "Country",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Club!",
                "http://www.1.fm/TuneIn/SC/club164k/Listen.aspx",
                "Dance",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Country",
                "http://www.1.fm/TuneIn/SC/country64k/Listen.aspx",
                "Country",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Disco Ball",
                "http://www.1.fm/TuneIn/SC/disco64k/Listen.aspx",
                "1970s",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Flashback Alternatives",
                "http://www.1.fm/TuneIn/SC/fa64k/Listen.aspx",
                "1980s",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - FTV Radio",
                "http://www.1.fm/TuneIn/SC/ftv64k/Listen.aspx",
                "Electronic",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Fuego FM",
                "http://www.1.fm/TuneIn/SC/energylatin128k/Listen.aspx",
                "Latin",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - High Voltage",
                "http://www.1.fm/TuneIn/SC/hv64k/Listen.aspx",
                "Rock",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Indie104 - iRadio LA",
                "http://www.1.fm/TuneIn/SC/indie64k/Listen.aspx",
                "Independant",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Jamz",
                "http://www.1.fm/TuneIn/SC/jamz64k/Listen.aspx",
                "Urban",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Jazz",
                "http://www.1.fm/TuneIn/SC/ajazz64k/Listen.aspx",
                "Jazz",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - LuxuriaMusic - Exotica, Lounge, etc.",
                "http://www.1.fm/TuneIn/SC/lux64k/Listen.aspx",
                "Lounge",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - One-Live",
                "http://www.1.fm/TuneIn/SC/onelive64k/Listen.aspx",
                "Various",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Otto's Baroque Musick",
                "http://www.1.fm/TuneIn/SC/baroque64k/Listen.aspx",
                "Classical",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Otto's Classical Musick",
                "http://www.1.fm/TuneIn/SC/classical64k/Listen.aspx",
                "Classical",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Otto's Opera House",
                "http://www.1.fm/TuneIn/SC/opera64k/Listen.aspx",
                "Classical",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - ReggaeTrade",
                "http://www.1.fm/TuneIn/SC/reggae64k/Listen.aspx",
                "Reggae",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - The 90's",
                "http://www.1.fm/TuneIn/SC/90s64k/Listen.aspx",
                "1990s",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - The Chillout Lounge",
                "http://www.1.fm/TuneIn/SC/tcl64k/Listen.aspx",
                "Ambient",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Top 40",
                "http://www.1.fm/TuneIn/SC/top4064k/Listen.aspx",
                "Top 40",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Trance",
                "http://www.1.fm/TuneIn/SC/trance64k/Listen.aspx",
                "Ambient",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Urban Adult Choice",
                "http://www.1.fm/TuneIn/SC/uac64k/Listen.aspx",
                "Urban",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "1.FM - Urban Gospel",
                "http://www.1.fm/TuneIn/SC/gospel64k/Listen.aspx",
                "Spiritual",
                64,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "107.7 FM: The Bone",
                "http://player.cumulusstreaming.com/stations/KSAN-FM/PubPoint.asx",
                "Rock",
                0,
                (StationStreamType)2));
            rr.Add(new RadioStation(
                "181.FM - 80's Hairband Channel",
                "http://www.181.fm/winamp.pls?station=181-hairband",
                "1980s",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - 90's Country",
                "http://www.181.fm/winamp.pls?station=181-90scountry",
                "Country",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - Awesome 80's",
                "http://www.181.fm/winamp.pls?station=181-awesome80s",
                "1980s",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - Chilled",
                "http://www.181.fm/winamp.pls?station=181-chilled",
                "Ambient",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - Classic Buzz - Classic Alternative Rock",
                "http://www.181.fm/winamp.pls?station=181-classicbuzz",
                "Alternative",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - Classic Hits - Home of The 60's and 70's",
                "http://www.181.fm/winamp.pls?station=181-greatoldies",
                "Oldies",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - Classical Guitar",
                "http://www.181.fm/winamp.pls?station=181-classicalguitar",
                "Classical",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - Comedy Club (Explicit Content)",
                "http://www.181.fm/winamp.pls?station=181-comedy",
                "Comedy",
                64,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - Energy 93 - Euro Dance",
                "http://www.181.fm/winamp.pls?station=181-energy93",
                "Dance",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - Energy 98 - Dance Hits",
                "http://www.181.fm/winamp.pls?station=181-energy98",
                "Dance",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - Good Time Oldies",
                "http://www.181.fm/winamp.pls?station=181-goodtime",
                "Oldies",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - Jammin 181",
                "http://www.181.fm/winamp.pls?station=181-jammin",
                "Urban",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - Kickin' Country - Today's Best Country!",
                "http://www.181.fm/winamp.pls?station=181-kickincountry",
                "Country",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - Lite 80's",
                "http://www.181.fm/winamp.pls?station=181-lite80s",
                "1980s",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - Punk | Hardcore",
                "http://www.181.fm/winamp.pls?station=181-punk",
                "Punk",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - Real Country",
                "http://www.181.fm/winamp.pls?station=181-realcountry",
                "Country",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - Rock 181 (Active Rock)",
                "http://www.181.fm/winamp.pls?station=181-rock",
                "Rock",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - Rock 40",
                "http://www.181.fm/winamp.pls?station=181-rock40",
                "Rock",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - Star 90's",
                "http://www.181.fm/winamp.pls?station=181-star90s",
                "1990s",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - The BEAT - #1 For HipHop and R&B",
                "http://www.181.fm/winamp.pls?station=181-beat",
                "Urban",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - The BOX - #1 For Hip-Hop",
                "http://www.181.fm/winamp.pls?station=181-thebox",
                "Urban",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - The Breeze",
                "http://www.181.fm/winamp.pls?station=181-breeze",
                "Jazz",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - The Buzz (Your Alternative Station!)",
                "http://www.181.fm/winamp.pls?station=181-buzz",
                "Alternative",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - The Eagle: Your Home For REAL Classic Rock!",
                "http://www.181.fm/winamp.pls?station=181-eagle",
                "Rock",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - The FrontPorch - Bluegrass From The FrontPorch",
                "http://www.181.fm/winamp.pls?station=181-frontporch",
                "Bluegrass",
                64,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - The Mix Channel (70s, 80s, 90s and Today's Best Music)",
                "http://www.181.fm/winamp.pls?station=181-themix",
                "Pop",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - The Office : Your Office Friendly Station!",
                "http://www.181.fm/winamp.pls?station=181-office",
                "Rock",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - The Point - Your Hit Music Alternative",
                "http://www.181.fm/winamp.pls?station=181-thepoint",
                "Top 40",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - True R&B",
                "http://www.181.fm/winamp.pls?station=181-rnb",
                "Urban",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "181.FM - US 181 Hot Country",
                "http://www.181.fm/winamp.pls?station=181-us181",
                "Country",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "Absolute Classic Rock",
                "http://network.absoluteradio.co.uk/core/audio/ogg/live.pls?service=vc",
                "Classic Rock",
                32,
                (StationStreamType)3));
            rr.Add(new RadioStation(
                "Absolute Radio, London - Discover Real Music",
                "http://network.absoluteradio.co.uk/core/audio/aacplus/live.pls?service=vrbb",
                "Rock",
                32,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "Absolute Xtreme",
                "http://network.absoluteradio.co.uk/core/audio/ogg/live.pls?service=vx",
                "Alternative",
                32,
                (StationStreamType)3));
            rr.Add(new RadioStation(
                "BBC World Service",
                "http://www.bbc.co.uk/worldservice/meta/tx/nb/live/www11.asx",
                "News",
                32,
                (StationStreamType)2));
            rr.Add(new RadioStation(
                "Boomer Radio: Acoustic Cafe",
                "http://www.boomerradio.com/cafe.asx",
                "Easy Listening",
                0,
                (StationStreamType)2));
            rr.Add(new RadioStation(
                "Boomer Radio: All Hit Oldies",
                "http://www.boomerradio.com/hitoldies.asx",
                "Oldies",
                0,
                (StationStreamType)2));
            rr.Add(new RadioStation(
                "Boomer Radio: Classic Mix",
                "http://www.boomerradio.com/mix.asx",
                "Rock",
                0,
                (StationStreamType)2));
            rr.Add(new RadioStation(
                "Boomer Radio: Electric Eighties",
                "http://www.boomerradio.com/eighties.asx",
                "1980s",
                0,
                (StationStreamType)2));
            rr.Add(new RadioStation(
                "Boomer Radio: Sizzlin' Seventies",
                "http://www.boomerradio.com/seventies.asx",
                "1970s",
                0,
                (StationStreamType)0));
            rr.Add(new RadioStation(
                "Boomer Radio: Smooth Jazz Favorites",
                "http://www.boomerradio.com/sjazz.asx",
                "Jazz",
                0,
                (StationStreamType)0));
            rr.Add(new RadioStation(
                "Boomer Radio: Sweet Soul Music",
                "http://www.boomerradio.com/soul.asx",
                "R & B and Soul",
                0,
                (StationStreamType)2));
            rr.Add(new RadioStation(
                "Boomer Radio: The Lush Life",
                "http://www.boomerradio.com/lushlife.asx",
                "Lounge",
                0,
                (StationStreamType)2));
            rr.Add(new RadioStation(
                "Boomer Radio: Vintage Rock",
                "http://www.boomerradio.com/vintagerock.asx",
                "Rock",
                0,
                (StationStreamType)2));
            rr.Add(new RadioStation(
                "CINEMIX - The Spirit of Soundtracks - All requestable",
                "http://loudcity.com/stations/cinemix/files/show/cine-high.asx",
                "Soundtrack",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "DI.fm - Ambient - A blend of ambient, downtempo, and chillout",
                "http://www.di.fm/aacplus/ambient.pls",
                "Ambient",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "DI.fm - Chillout - Ambient psy chillout, check out our trippy flavors!",

                "http://www.di.fm/aacplus/chillout.pls",
                "Ambient",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "DI.fm - Electro House - An eclectic mix of electro and house",
                "http://www.di.fm/aacplus/electro.pls",
                "Electronic",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "DI.fm - EuroDance & HiNRG - Finest imported cheese on the net!",
                "http://www.di.fm/aacplus/eurodance.pls",
                "Dance",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "DI.fm - European Trance, Techno, Hi-NRG... we can't define it!",
                "http://www.di.fm/aacplus/trance.pls",
                "Electronic",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "DI.fm - House - Silky sexy deep house music direct from New York city!",
                "http://www.di.fm/aacplus/house.pls",
                "Electronic",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "DI.fm - Soulful House - House music selected from Paris with love!",
                "http://www.di.fm/aacplus/soulfulhouse.pls",
                "Electronic",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "Groove Salad: A nicely chilled plate of ambient beats and grooves.",
                "http://scfire-ntc-aa01.stream.aol.com:80/stream/1018",
                "Ambient",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "Illinois Street Lounge - Classic bachelor pad, playful exotica and vintage music of tomorrow",
                "http://somafm.com/wma128/illstreet.asx",
                "Lounge",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "KFOG 104.5 FM - San Francisco's World Class Rock",
                "http://player.cumulusstreaming.com/stations/KFOG-FM/PubPoint.asx",
                "Rock",
                0,
                (StationStreamType)2));
            rr.Add(new RadioStation(
                "KNBR 680 AM - The Sports Leader",
                "Http://live.cumulusstreaming.com/KNBR-AM",
                "Sports",
                0,
                (StationStreamType)2));
            rr.Add(new RadioStation(
                "KQED 88.5 FM - Public Radio San Francisco",
                "http://205.234.249.144:8000",
                "News",
                32,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "NPR: National Public Radio",
                "http://www.npr.org/templates/dmg/dmg.php?getProgramStream=true&NPRMediaPref=WM",
                "News",
                20,
                (StationStreamType)2));
            rr.Add(new RadioStation(
                "Radio Disney",
                "http://wdig-radiodisneydotcom.wm.llnwd.net/wdig_radiodisneydotcom",
                "Kids",
                74,
                (StationStreamType)2));
            rr.Add(new RadioStation(
                "Radio Paradise",
                "http://www.radioparadise.com/musiclinks/rp_128aac-2.m3u",
                "Various",
                128,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "Radio Paradise - DJ-mixed modern & classic rock, world, electronica & more",
                "http://207.200.96.226:8048",
                "Pop",
                128,
                (StationStreamType)0));
            rr.Add(new RadioStation(
                "RockRadio1.Com - Classic Hard Rock & Heavy Metal Mix, 24/7 Live Requests",
                "http://www.rockradio1.com/listen.pls",
                "Rock",
                192,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "SKY fm - SalsaStream",
                "http://www.sky.fm/aacplus/salsa.pls",
                "Latin",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - 80s, 80s, 80s!",
                "http://www.sky.fm/aacplus/the80s.pls",
                "1980s",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - A Beatles Tribute - Beatles Hits, Remakes, Tributes",
                "http://www.sky.fm/aacplus/beatles.pls",
                "Oldies",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - Absolutely Smooth Jazz",
                "http://www.sky.fm/aacplus/smoothjazz.pls",
                "Jazz",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - All Hit 70s - All Hits, All The Time!",
                "http://www.sky.fm/aacplus/hit70s.pls",
                "1970s",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - Alternative Rock - Alternative rock hits",
                "http://www.sky.fm/aacplus/altrock.pls",
                "Alternative",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - Bossa Nova Jazz",
                "http://www.sky.fm/aacplus/bossanova.pls",
                "Jazz",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - Classic Rap & Hip Hop",
                "http://www.sky.fm/aacplus/classicrap.pls",
                "Urban",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - Classic Rock",
                "http://www.sky.fm/aacplus/classicrock.pls",
                "Rock",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - Classical & Flamenco Guitar",
                "http://www.sky.fm/aacplus/guitar.pls",
                "Classical",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - Contemporary Christian",
                "http://www.sky.fm/aacplus/christian.pls",
                "Spiritual",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - Country - Today's Hit Country and Older Favorites",
                "http://www.sky.fm/aacplus/country.pls",
                "Country",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - Datempo Lounge - Jazz, lounge, bossa nova, etc",
                "http://www.di.fm/aacplus/lounge.pls",
                "Lounge",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - Indie Rock - Indie, Alternative, and Underground Rock",
                "http://www.sky.fm/aacplus/indierock.pls",
                "Independant",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - Love Music - Easy Listening and Romantic hits from the heart",
                "http://www.sky.fm/aacplus/lovemusic.pls",
                "Easy Listening",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - Modern Jazz - Bright music from the likes of Coltrane, Ornette Coleman, Eric Dolphy, & Charles Mingus",
                "http://www.sky.fm/aacplus/jazz.pls",
                "Jazz",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - Mostly Classical - Listen and Relax, it's good for you!",
                "http://www.sky.fm/aacplus/classical.pls",
                "Classical",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - New Age - Soothing sounds of new age and world music!",
                "http://www.sky.fm/aacplus/newage.pls",
                "New Age",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - Oldies - The 50s, 60s, & 70s - three decades of great oldies",
                "http://www.sky.fm/aacplus/oldies.pls",
                "Oldies",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - Piano Jazz - Piano jazz from historic and modern masters",
                "http://www.sky.fm/aacplus/pianojazz.pls",
                "Jazz",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - Roots Reggae",
                "http://www.sky.fm/aacplus/rootsreggae.pls",
                "Reggae",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - Simply Soundtracks - Soundtracks from movies, show themes, & more",
                "http://www.sky.fm/aacplus/soundtracks.pls",
                "Soundtrack",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - Solo Piano",
                "http://www.sky.fm/aacplus/solopiano.pls",
                "Classical",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - Top Hits",
                "http://www.sky.fm/aacplus/tophits.pls",
                "Top 40",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - Uptempo Smooth Jazz",
                "http://www.sky.fm/aacplus/uptemposmoothjazz.pls",
                "Jazz",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - Urban Jamz - Kickin' with the baddest beats on the 'net",
                "http://www.sky.fm/aacplus/urbanjamz.pls",
                "Urban",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SKY.fm - World Music",
                "http://www.sky.fm/aacplus/world.pls",
                "World",
                24,
                (StationStreamType)4));
            rr.Add(new RadioStation(
                "SomaFM: Beat Blender - A late night blend of deep-house & downtempo chill",
                "http://somafm.com/wma128/beatblender.asx",
                "Dance",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "SomaFM: Boot Liquor - Americana roots music for Cowhands, Cowpokes and Cowtippers",
                "http://somafm.com/wma128/bootliquor.asx",
                "Country",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "SomaFM: CliqHop - blips 'n' bleeps backed w/ beats",
                "http://somafm.com/wma128/cliqhop.asx",
                "Electronic",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "SomaFM: Digitalis - Analog rock, digitally-affected, to calm the agitated heart",
                "http://somafm.com/wma128/digitalis.asx",
                "Electronic",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "SomaFM: Doomed - Dark music for tortured souls",
                "http://somafm.com/wma128/doomed.asx",
                "Industrial",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "SomaFM: Drone Zone - Atmospheric ambient space music. Serve Best Chilled. Safe with most medications",
                "http://somafm.com/wma128/dronezone.asx",
                "Ambient",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "SomaFM: Groove Salad - a nicely chilled plate of ambient beats and grooves",
                "http://somafm.com/wma128/groovesalad.asx",
                "Ambient",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "SomaFM: Indie Pop Rocks!",
                "http://somafm.com/wma128/indiepop.asx",
                "Independant",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "SomaFM: Lush - Mostly female vocals with an electronic influence",
                "http://somafm.com/wma128/lush.asx",
                "Pop",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(

                "SomaFM: Secret Agent - The soundtrack for your stylish, mysterious, dangerous life. For Spys and P.I.'s too!",
                "http://somafm.com/wma128/secretagent.asx",
                "Lounge",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "SomaFM: Sonic Universe. Nu Jazz plus avant-garde Euro Jazz",
                "http://somafm.com/wma128/sonicuniverse.asx",
                "Jazz",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "SomaFM: Space Station Soma: Tune in, turn on, space out. Ambient and mid-tempo electronica",
                "http://somafm.com/wma128/spacestation.asx",
                "Electronic",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "SomaFM: Tag's Trip - Progressive house / trance - Tip top tunes",
                "http://somafm.com/wma128/tags.asx",
                "Electronic",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "The Chillout Lounge",
                "http://64.71.145.130:8010",
                "Ambient",
                128,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "WFAN - Sports Radio 66 New York City",
                "http://provisioning.streamtheworld.com/pls/WFANAMDIALUP.pls",
                "Sports",
                0,
                (StationStreamType)1));
            rr.Add(new RadioStation(
                "WNYC - New York Public Radio",
                "http://www.wnyc.org/stream/fm.asx",
                "News",
                0,
                (StationStreamType)2));
            rr.Add(new RadioStation(
                "WUWM 89.7 FM - Public Radio Milwaukee",
                "http://129.89.70.125:80",
                "News",
                64,
                (StationStreamType)1));
        }
    }
}