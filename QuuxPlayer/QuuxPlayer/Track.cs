/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

using System.Text;

namespace QuuxPlayer
{
    internal sealed class Track
    {
        public delegate void TrackEventDelegate(Track Track);

        public TrackEventDelegate TrackPlayed;
        public TrackEventDelegate TrackDeleted;

        public delegate void TrackCallback(Track Track);

        // Don't change the order of these: part of file schema
        public enum FileType { None, MP3, AAC, OGG, FLAC, WAV, WMA, ALAC, M4A, AIFF, WV, MPC, AC3, MP1, MP2, APE }
        
        private enum DecadeEnum { None = 0, Thirties = 1, Fourties = 2, Fifties = 3, Sixties = 4, Seventies = 5, Eighties = 6, Nineties = 7, TwoThousands = 8, Tens = 9, NotSet = 10 }
        public static readonly string[] DecadeText = { String.Empty, "1930s", "1940s", "1950s", "1960s", "1970s", "1980s", "1990s", "2000s", "2010s" };

        private static Dictionary<FileType, String> fileTypeStrings;
        private static Controller controller;
        private static readonly int[] wmpRatingTable = new int[0x100];
        private static readonly string[] RatingStrings = { String.Empty, String.Empty, "*", "**", "***", "****", "*****" };
        private static bool downloadCoverOK = false;

        private static string compilationString = String.Empty;
        private const string separator = " - ";
        private static string monoString = String.Empty;
        private static string stereoString = String.Empty;

        private bool downloadCoverOKThisTrack = false;
        private int id = -1;
        private string filePath;
        private string fileName;
        private FileType type;
        private string title;
        private string album;
        private string artist;
        private string artistNoThe;
        private string albumArtistNoThe;
        private string albumArtist;
        private string composer;
        private string grouping;
        private string genre;
        private EqualizerSetting equalizer = null;
        private int duration = 0; // in msec
        private int trackNum;
        private int diskNum;
        private int playCount;
        private int rating;
        private string encoder;
        private int year;
        private int bitrate;
        private long fileSize;
        private bool compilation;
        public ChangeType ChangeType { get; set; }
        private DateTime fileDate;
        private DateTime lastPlayedDate;
        private DateTime addDate;
        private string yearString = String.Empty;
        private string mainGroup = String.Empty;
        private string mainGroupNoThe = String.Empty;
        private int numChannels;
        private int sampleRate;
        private string durationInfo = null;
        private string durationInfoLong = null;
        private int sortKey;
        private DecadeEnum decade = DecadeEnum.NotSet;
        private char decadeChar = '\0';
        private ImageItem cover = null;
        public bool ForceEmbeddedImageNull { get; set; }
        private bool _coverLoadAttempted = false;
        private bool toStringDirty = true;
        private bool searchStringDirty = true;
        private string toString;
        private string searchString;
        private bool selected = false;
        private bool loadingImage = false;
        private float replayGainTrack;
        private float replayGainAlbum;

        private List<TrackCallback> callbacks = new List<TrackCallback>();

        public static Controller Controller
        {
            set { controller = value; }
        }

        static Track()
        {
            fileTypeStrings = new Dictionary<FileType, string>();
            fileTypeStrings.Add(FileType.AAC, "AAC / iTunes");
            fileTypeStrings.Add(FileType.FLAC, "FLAC: Lossless");
            fileTypeStrings.Add(FileType.MP3, "MP3");
            fileTypeStrings.Add(FileType.OGG, "OGG: OGG/Vorbis");
            fileTypeStrings.Add(FileType.WAV, "WAV: Windows WAV");
            fileTypeStrings.Add(FileType.WMA, "WMA: Windows Media");
            fileTypeStrings.Add(FileType.ALAC, "Apple Lossless");
            fileTypeStrings.Add(FileType.M4A, "iTunes");
            fileTypeStrings.Add(FileType.WV, "WavPack");
            fileTypeStrings.Add(FileType.MPC, "Musepack");
            fileTypeStrings.Add(FileType.AIFF, "AIFF");
            fileTypeStrings.Add(FileType.AC3, "Dolby Digital");
            fileTypeStrings.Add(FileType.APE, "APE: Monkey's Audio");
            fileTypeStrings.Add(FileType.MP2, "MP2");
            fileTypeStrings.Add(FileType.MP1, "MP1");

            TimeLastTrackSelectChanged = DateTime.Now;

            for (int i = 0; i < 0x100; i++)
            {
                if (i == 0xFF)
                    wmpRatingTable[i] = 5;
                else if (i > 0xC3)
                    wmpRatingTable[i] = 4;
                else if (i > 0x7F)
                    wmpRatingTable[i] = 3;
                else if (i > 0x3F)
                    wmpRatingTable[i] = 2;
                else if (i > 0x00)
                    wmpRatingTable[i] = 1;
                else
                    wmpRatingTable[i] = 0;
            }
            compilationString = Localization.Get(UI_Key.Track_Compilation);
            monoString = Localization.Get(UI_Key.Track_Mono);
            stereoString = Localization.Get(UI_Key.Track_Stereo);
        }

        public static bool IsValidExtension(string Extension)
        {
            string ext = Extension.ToLowerInvariant();

            return (ext == ".mp3" ||
                    ext == ".aac" ||
                    ext == ".flac" ||
                    ext == ".fla" ||
                    ext == ".ogg" ||
                    ext == ".wav" ||
                    ext == ".wma" ||
                    ext == ".alac" ||
                    ext == ".aiff" ||
                    ext == ".aif" ||
                    ext == ".wv" ||
                    ext == ".mpc" ||
                    ext == ".ape" ||
                    ext == ".ac3" ||
                    ext == ".m4a") ||
                    ext == ".m4b";
        }

        public bool IsStrongMatch(Track Other)
        {
            return (this.TrackNum == Other.TrackNum) &&
                   (Math.Abs(this.Duration - Other.Duration) < 3000) &&
                   (this.SearchString == Other.SearchString);
        }
        public bool ReadOnly
        {
            get { return this.ConfirmExists && ((File.GetAttributes(this.FilePath) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly); }
        }
        public static bool DownloadCoverArt
        {
            get { return downloadCoverOK; }
            set
            {
                if (downloadCoverOK != value)
                {
                    downloadCoverOK = value;
                    if (downloadCoverOK)
                    {
                        foreach (Track t in Database.LibrarySnapshot)
                            if (t.cover == null)
                                t._coverLoadAttempted = false;

                        NetworkUtil.RemoveNullAlbumCache();
                    }
                }
            }
        }
        public bool IsPlaying
        {
            get { return controller.PlayingTrack == this; }
        }
        public static long TimeSinceLastTrackSelectedChanged
        {
            get { return (long)(DateTime.Now - TimeLastTrackSelectChanged).TotalMilliseconds; }
        }
        
        private Track(string FilePath)
        {
            switch (Path.GetExtension(FilePath).ToLowerInvariant())
            {
                case ".mp3":
                    this.type = FileType.MP3;
                    break;
                case ".wma":
                    this.type = FileType.WMA;
                    break;
                case ".wav":
                    this.type = FileType.WAV;
                    break;
                case ".flac":
                    this.type = FileType.FLAC;
                    break;
                case ".fla":
                    this.type = FileType.FLAC;
                    break;
                case ".ogg":
                    this.type = FileType.OGG;
                    break;
                case ".ac3":
                    this.type = FileType.AC3;
                    break;
                case ".mpc":
                    this.type = FileType.MPC;
                    break;
                case ".wv":
                    this.type = FileType.WV;
                    break;
                case ".m4b":
                case ".m4a":
                    this.type = FileType.M4A;
                    break;
                case ".aif":
                case ".aiff":
                    this.type = FileType.AIFF;
                    break;
                case ".alac":
                    this.type = FileType.ALAC;
                    break;
                case ".ape":
                    this.type = FileType.APE;
                    break;
                default:
                    this.type = FileType.None;
                    break;
            }

            this.filePath = FilePath;
            this.fileName = Path.GetFileName(filePath);
            this.rating = -1;
            this.encoder = String.Empty;
            this.artist = String.Empty;
            this.album = String.Empty;
            this.albumArtist = String.Empty;
            this.title = String.Empty;
            this.grouping = String.Empty;
            this.genre = String.Empty;
            this.composer = String.Empty;
            this.replayGainAlbum = float.MinValue;
            this.replayGainTrack = float.MinValue;
            this.Exists = null;
            this.ChangeType = ChangeType.None;
        }
        public Track(int ID, string FilePath, FileType Type, string Title, string Album,
             string Artist, string AlbumArtist, string Composer,
             string Grouping, string Genre, int Length, int TrackNum,
             int DiskNum, int Year, int PlayCount,
             int Rating, int Bitrate, long FileSize, bool Compilation, DateTime FileDate,
             DateTime LastPlayedDate, DateTime AddDate, string Encoder, int NumChannels,
             int SampleRate, ChangeType ChangeType, EqualizerSetting Eq,
             float ReplayGainAlbum, float ReplayGainTrack)
        {
            this.id = ID;
            this.type = Type;
            this.filePath = FilePath;
            this.fileName = Path.GetFileName(FilePath);
            this.title = Title;
            this.album = Album;
            this.artist = Artist;
            this.albumArtist = AlbumArtist;
            this.composer = Composer;
            this.grouping = Grouping;
            this.genre = Genre;
            this.duration = Length;
            this.trackNum = TrackNum;
            this.diskNum = DiskNum;
            this.year = Year;
            this.playCount = PlayCount;
            this.rating = Rating;
            this.bitrate = Bitrate;
            this.fileSize = FileSize;
            this.compilation = Compilation;
            this.fileDate = FileDate;
            this.lastPlayedDate = LastPlayedDate;
            this.addDate = AddDate;
            this.encoder = Encoder;
            this.numChannels = NumChannels;
            this.sampleRate = SampleRate;
            this.ChangeType = ChangeType;
            this.equalizer = Eq;
            this.replayGainAlbum = ReplayGainAlbum;
            this.replayGainTrack = ReplayGainTrack;

            this.Exists = null;

            UpdateMainGroup();

            this.toStringDirty = true;
            this.searchStringDirty = true;
        }

        public Track(Track Template)
        {
            this.Update(Template);
        }

        public static DateTime TimeLastTrackSelectChanged
        { get; private set; }
        
        public int Index
        { get; set; }        
        public int ID
        {
            get { return id; }
            set { id = value; }
        }
        public bool? Exists
        { get; private set; }
        public int RandomKey
        {
            get { return sortKey; }
            set { sortKey = value; }
        }
        public string FilePath
        {
            get { return filePath; }
            set
            {
                filePath = value;
                fileName = Path.GetFileNameWithoutExtension(value);
            }
        }
        public string FileName
        {
            get { return fileName; }
        }
        public FileType Type
        {
            get { return this.type; }
            set { this.type = value; }
        }
        public string TypeString
        {
            get { return fileTypeStrings[this.type]; }
        }
        public string Title
        {
            get { return title; }
            set
            {
                title = (value ?? String.Empty).Trim();
                
                toStringDirty = true;
                searchStringDirty = true;
            }
        }
        public string Album
        {
            get { return album; }
            set
            {
                album = (value ?? String.Empty).Trim();
            }
        }
        public string Artist
        {
            get { return artist; }
            set
            {
                artist = (value ?? String.Empty).Trim();
            }
        }
        public string ArtistNoThe
        {
            get { return artistNoThe; }
        }
        public string AlbumArtist
        {
            get { return albumArtist; }
            set
            {
                albumArtist = (value ?? String.Empty).Trim();
            }
        }
        public string AlbumArtistNoThe
        {
            get { return albumArtistNoThe; }
        }
        public string Grouping
        {
            get { return grouping; }
            set
            {
                grouping = (value ?? String.Empty).Trim();
            }
        }
        public string Composer
        {
            get { return composer; }
            set
            {
                composer = value ?? String.Empty;
            }
        }
        public string Genre
        {
            get { return genre; }
            set
            {
                if (value == null || value.Length == 0)
                    genre = Localization.NO_GENRE;
                else
                    genre = value.Trim();
            }
        }
        public string GenreAllowBlank
        {
            get
            {
                if (this.genre == Localization.NO_GENRE)
                    return String.Empty;
                else
                    return genre;
            }
        }
        public int Year
        {
            get
            {
                return year;
            }
            set
            {
                year = value;
                toStringDirty = true;
            }
        }
        public int PlayCount
        {
            get { return playCount; }
            private set { playCount = value; }
        }
        public string PlayCountString
        {
            get { return playCount.ToString(); }
        }
        public int Rating
        {
            get { return Math.Max(0, rating); }
            set
            {
                rating = value;
                Database.IncrementDatabaseVersion(false);
            }
        }
        public int InternalRating
        {
            get { return rating; }
        }
        public string RatingString
        {
            get
            {
                if (rating >= -1 && rating < 6)
                    return RatingStrings[rating + 1];
                else
                    return rating.ToString();
            }
        }
        public float ReplayGainTrackInternal { get { return replayGainTrack; } }
        public float ReplayGainAlbumInternal { get { return replayGainAlbum; } }
        public float ReplayGain
        {
            get
            {
                switch (Setting.ReplayGain)
                {
                    case ReplayGainMode.Album:
                        return replayGainAlbum;
                    default:
                        return replayGainTrack;
                }
            }
        }
        public float ReplayGainTrack
        {
            get
            {
                return (replayGainTrack < -100.0f) ? 0.0f : replayGainTrack;
            }
            set
            {
                replayGainTrack = value;
            }
        }
        public float ReplayGainAlbum
        {
            get
            {

                return (replayGainAlbum < -100.0f) ? 0.0f : replayGainAlbum;
            }
            set
            {
                replayGainAlbum = value;
            }
        }
        public string ReplayGainString
        {
            get
            {
                switch (Setting.ReplayGain)
                {
                    case ReplayGainMode.Album:
                        if (HasReplayGainInfoAlbum)
                            return String.Format("{0:0.00} dB", replayGainAlbum);
                        break;
                    case ReplayGainMode.Off:
                    case ReplayGainMode.Track:
                        if (HasReplayGainInfoTrack)
                            return String.Format("{0:0.00} dB", replayGainTrack);
                        break;
                }
                return String.Empty;
            }
        }
        public bool HasReplayGainInfoTrack
        {
            get { return replayGainTrack > -100.0f; }
        }
        public bool HasReplayGainInfoAlbum
        {
            get { return replayGainAlbum > -100.0f; }
        }
        public string Encoder
        {
            get { return encoder; }
            set { encoder = value ?? String.Empty; }
        }
        public int Bitrate
        {
            get { return bitrate; }
            set { bitrate = value; }
        }
        public int NumChannels
        {
            get { return numChannels; }
            set { numChannels = value; }
        }
        public string NumChannelsString
        {
            get
            {
                switch (numChannels)
                {
                    case 1:
                        return monoString;
                    case 2:
                        return stereoString;
                    default:
                        return numChannels.ToString();
                }
            }
        }
        public int SampleRate
        {
            get { return sampleRate; }
            set { sampleRate = value; }
        }
        public string SampleRateString
        {
            get { return sampleRate.ToString(); }
        }
        public long FileSize
        {
            get { return fileSize; }
            private set { fileSize = value; }
        }
        public string BitrateString
        {
            get { return bitrate.ToString() + "k"; }
        }
        public string FileSizeString
        {
            get
            {
                /*if (fileSize > (1024 * 1024))
                {
                    return (fileSize / (1024 * 1024)).ToString() + "M";
                }
                else */if (fileSize > 1024)
                {
                    return (fileSize / 1024).ToString() + "K";
                }
                else
                {
                    return fileSize.ToString() + "B";
                }
            }
        }
        public DateTime FileDate
        {
            get { return fileDate; }
            set { fileDate = value; }
        }
        public DateTime LastPlayedDate
        {
            get { return lastPlayedDate; }
            set { lastPlayedDate = value; }
        }
        public void MarkAsPlayed()
        {
            LastPlayedDate = DateTime.Now;
            PlayCount++;
            Database.IncrementDatabaseVersion(false);
            if (TrackPlayed != null)
                TrackPlayed(this);
        }
        public DateTime AddDate
        {
            get { return addDate; }
            private set { addDate = value; }
        }
        public int DaysSinceLastPlayed
        {
            get { return (DateTime.Now - lastPlayedDate).Days; }
        }
        public double DaysSinceLastPlayedDouble
        {
            get { return (DateTime.Now - lastPlayedDate).TotalDays; }
        }
        public string DaysSinceLastPlayedString
        {
            get
            {
                if (lastPlayedDate == DateTime.MinValue)
                    return String.Empty;
                else
                    return ((DateTime.Now - lastPlayedDate).TotalDays).ToString("0.00");
            }
        }
        public bool HasBeenPlayed
        {
            get { return this.LastPlayedDate > DateTime.MinValue; }
        }
        public int FileAgeInDays
        {
            get { return (DateTime.Now - fileDate).Days; }
        }
        public double FileAgeInDaysDouble
        {
            get { return (DateTime.Now - fileDate).TotalDays; }
        }
        public int DaysSinceFileAdded
        {
            get { return (DateTime.Now - addDate).Days; }
        }
        public double DaysSinceFileAddedDouble
        {
            get { return (DateTime.Now - addDate).TotalDays; }
        }
        public string DaysSinceAddedString
        {
            get
            {
                return ((float)(DateTime.Now - addDate).TotalMinutes / (60f * 24f)).ToString("0.00");
            }
        }
        public string FileDateString
        {
            get { return fileDate.ToString("g"); }
        }
        public string YearString
        {
            get
            {
                if (toStringDirty)
                {
                    updateToString();
                }
                return yearString;
            }
        }
        public int Duration
        {
            get { return duration; }
            set
            {
                duration = value;
                durationInfo = null;
                durationInfoLong = null;
            }
        }
        public bool DurationIsSimilar(Track Other)
        {
            return (Math.Abs(this.Duration - Other.Duration) <= 5000);
        }
        public string DurationInfo
        {
            get
            {
                if (durationInfo == null)
                    durationInfo = Lib.GetTimeString(duration);

                return durationInfo;
            }
        }
        public string DurationInfoLong
        {
            get
            {
                if (durationInfoLong == null)
                    durationInfoLong = Lib.GetTimeStringFractional(duration);

                return durationInfoLong;
            }
        }
        public int TrackNum
        {
            get { return trackNum; }
            set { trackNum = value; }
        }
        public string TrackNumStringLeadingZero
        {
            get
            {
                if (trackNum == 0)
                    return String.Empty;
                else if (trackNum > 9)
                    return TrackNumString;
                else
                    return "0" + TrackNumString;

            }
        }
        public string TrackNumString
        {
            get { return trackNum > 0 ? trackNum.ToString() : String.Empty; }
        }
        public int DiskNum
        {
            get { return diskNum; }
            set { diskNum = value; }
        }
        public string DiskNumString
        {
            get { return diskNum > 0 ? diskNum.ToString() : String.Empty; }
        }
        public string MainGroup
        {
            get
            {
                return mainGroup;
            }
        }
        public string MainGroupNoThe
        {
            get
            {
                return mainGroupNoThe;
            }
        }
        private bool deleted = false;
        public bool Deleted
        {
            get { return deleted; }
            set
            {
                if (deleted != value)
                {
                    deleted = value;
                    if (deleted)
                    {
                        this.ID = -1;
                        if (TrackDeleted != null)
                            TrackDeleted(this);
                    }
                }
            }
        }
        public void AllowCoverLoad()
        {
            if (this.cover == null)
                _coverLoadAttempted = false;
        }
        public bool AllowAlbumCoverDownloadThisTrack
        {
            get { return downloadCoverOKThisTrack; }
            set
            {
                downloadCoverOKThisTrack = value;
                if (downloadCoverOKThisTrack)
                    _coverLoadAttempted = false;
            }
        }
        public bool Compilation
        {
            get { return compilation; }
            set
            {
                compilation = value;
            }
        }
        public string Decade
        {
            get
            {
                if (decade == DecadeEnum.NotSet)
                {
                    setDecade();
                }
                return DecadeText[(int)decade];
            }
        }
        public char DecadeChar
        {
            get
            {
                if (decade == DecadeEnum.NotSet)
                {
                    setDecade();
                }
                return decadeChar;
            }
        }
        private void setDecade()
        {
            if (this.year >= 2010 && this.year <= 2019)
            {
                decade = DecadeEnum.Tens;
                decadeChar = '1';
            }
            else if (this.year >= 2000)
            {
                decade = DecadeEnum.TwoThousands;
                decadeChar = '0';
            }
            else if (this.year >= 1990)
            {
                decade = DecadeEnum.Nineties;
                decadeChar = '9';
            }
            else if (this.year >= 1980)
            {
                decade = DecadeEnum.Eighties;
                decadeChar = '8';
            }
            else if (this.year >= 1970)
            {
                decade = DecadeEnum.Seventies;
                decadeChar = '7';
            }
            else if (this.year >= 1960)
            {
                decade = DecadeEnum.Sixties;
                decadeChar = '6';
            }
            else if (this.year >= 1950)
            {
                decade = DecadeEnum.Fifties;
                decadeChar = '5';
            }
            else if (this.year >= 1940)
            {
                decade = DecadeEnum.Fourties;
                decadeChar = '4';
            }
            else if (this.year >= 1930)
            {
                decade = DecadeEnum.Thirties;
                decadeChar = '3';
            }
            else
            {
                decade = DecadeEnum.None;
                decadeChar = '\0';
            }

        }
        public bool FilterBy(string Input)
        {
            if (Input[0] == '-')
                return !SearchString.Contains(Input.Substring(1).Trim());
            else
                return SearchString.Contains(Input);
        }
        public string SearchString
        {
            get
            {
                if (searchStringDirty)
                    updateSearchString();

                return searchString;
            }
        }
        public TrackWriter.RenameFormat RenameFormat { get; set; }
        public ImageItem Cover
        {
            // warning: can take a while if not preloaded
            get
            {
                while (loadingImage)
                    Thread.Sleep(150);

                if (cover == null && !_coverLoadAttempted)
                {
                    preLoadImage();
                }
                return cover;
            }
        }
        public EqualizerSetting Equalizer
        {
            get { return equalizer; }
            set { equalizer = value; }
        }
        public string EqualizerString
        {
            get { return (this.Equalizer == null) ? String.Empty : EqualizerSetting.GetString(this.Equalizer); }
        }
        public void SetEqualizer(string EqName)
        {
            this.Equalizer = EqualizerSetting.GetSetting(EqName, QuuxPlayer.Equalizer.GetEqualizerSettings());
        }
        public bool SoftComfirmExists()
        {
            if (Exists.HasValue)
                return Exists.Value;
            else
                return ConfirmExists;
        }
        public void SetConfirmExists()
        {
            this.Exists = File.Exists(this.FilePath);
        }
        public bool ConfirmExists
        {
            get
            {
                SetConfirmExists();
                return this.Exists.Value;
            }
            set
            {
                // careful with this
                this.Exists = value;
            }
        }
        public static string CSVHeader
        {
            get
            {
                return "Title, Album, Artist, Album Artist, Track Number, Disk Number, Length (Sec), Genre, Year, Bitrate, Compilation, File Path";
            }
        }
        public string CSV
        {
            get
            {
                StringBuilder sb = new StringBuilder(200);
                sb.Append("\"" + this.Title + "\"");
                sb.Append(",");
                sb.Append("\"" + this.Album + "\"");
                sb.Append(",");
                sb.Append("\"" + this.Artist + "\"");
                sb.Append(",");
                sb.Append("\"" + this.AlbumArtist + "\"");
                sb.Append(",");
                sb.Append(this.TrackNum.ToString());
                sb.Append(",");
                sb.Append(this.DiskNum.ToString());
                sb.Append(",");
                sb.Append((this.Duration / 1000).ToString());
                sb.Append(",");
                sb.Append("\"" + this.Genre + "\"");
                sb.Append(",");
                sb.Append(this.YearString);
                sb.Append(",");
                sb.Append(this.BitrateString);
                sb.Append(",");
                sb.Append(this.Compilation ? "Yes" : "No");
                sb.Append(",");
                sb.Append("\"" + this.filePath + "\"");

                return sb.ToString();
            }
        }
        public bool Selected
        {
            get { return selected; }
            set
            {
                if (selected != value)
                {
                    TimeLastTrackSelectChanged = DateTime.Now;
                    selected = value;
                }
            }
        }
#if PERF
        public void updateAlbumAndArtist(string Album, string Artist)
        {
            this.Album = Album;
            this.Artist = Artist;
        }
#endif
        public static Track Load(string FilePath)
        {
            if (!IsValidExtension(Path.GetExtension(FilePath)))
                return null;

            if (File.Exists(FilePath))
            {
                Track t = Database.GetTrackWithFilePath(FilePath);

                if (t == null)
                {
                    t = new Track(FilePath);
                    t.AddDate = DateTime.Now;
                    
                    if (!t.Load())
                        return null;
                }
                else if (t.FileDate != File.GetLastWriteTime(FilePath))
                {
                    if (!t.Load())
                        return null;
                }

                return t;
            }
            else
            {
                return null;
            }
        }
        
        public void Update(Track Template)
        {
            if ((this.ChangeType & ChangeType.WriteTags) != ChangeType.WriteTags)
            {
                this.filePath = Template.filePath;
                this.fileName = Template.fileName;
                this.type = Template.type;
                this.title = Template.title;
                this.album = Template.album;
                this.artist = Template.artist;
                this.albumArtist = Template.albumArtist;
                this.composer = Template.composer;
                this.grouping = Template.grouping;
                this.genre = Template.genre;
                this.duration = Template.duration;
                this.trackNum = Template.trackNum;
                this.diskNum = Template.diskNum;
                this.year = Template.year;
                this.bitrate = Template.bitrate;
                this.compilation = Template.compilation;
                this.fileDate = Template.fileDate;
                this.encoder = Template.encoder;
                this.numChannels = Template.numChannels;
                this.sampleRate = Template.sampleRate;
                this.equalizer = Template.equalizer;
                this.ChangeType = Template.ChangeType;
                this.replayGainAlbum = Template.replayGainAlbum;
                this.replayGainTrack = Template.replayGainTrack;

                UpdateMainGroup();

                this.toStringDirty = true;
                this.searchStringDirty = true;
            }
        }
        public bool? PreLoadImage(TrackCallback Callback)
        {
            if (cover != null)
            {
                callbacks.Add(Callback);
                return true;
            }
            else if (_coverLoadAttempted)
            {
                return false;
            }
            else if (loadingImage)
            {
                callbacks.Add(Callback);
                return null;
            }

            loadingImage = true;

            callbacks.Add(Callback);

            Thread t = new System.Threading.Thread(preLoadImage);
#if DEBUG
            t.Name = "Preload Image Thread";
#endif
            t.Priority = ThreadPriority.Normal;
            t.IsBackground = true;
            t.Start();

            return null;
        }
        public int ArtistComparer(Track Other)
        {
            int comp = String.Compare(this.artistNoThe, Other.artistNoThe, StringComparison.OrdinalIgnoreCase);

            if (comp == 0)
            {
                comp = String.Compare(this.Album, Other.Album, StringComparison.OrdinalIgnoreCase);
                if (comp == 0)
                {
                    comp = this.DiskNum.CompareTo(Other.DiskNum);
                    if (comp == 0)
                    {
                        comp = this.TrackNum.CompareTo(Other.TrackNum);
                        if (comp == 0)
                            comp = this.id.CompareTo(Other.id);
                    }
                }
            }
            return comp;
        }
        
        public int AlbumArtistComparer(Track Other)
        {
            int comp = String.Compare(this.albumArtistNoThe, Other.albumArtistNoThe, StringComparison.OrdinalIgnoreCase);

            if (comp == 0)
            {
                comp = String.Compare(this.Album, Other.Album, StringComparison.OrdinalIgnoreCase);
                if (comp == 0)
                {
                    comp = this.DiskNum.CompareTo(Other.DiskNum);
                    if (comp == 0)
                    {
                        comp = this.TrackNum.CompareTo(Other.TrackNum);
                        if (comp == 0)
                            comp = this.id.CompareTo(Other.id);
                    }
                }
            }
            return comp;
        }
        public int DiskNumComparer(Track Other)
        {
            int comp = this.DiskNum.CompareTo(Other.DiskNum);
            if (comp == 0)
            {
                comp = this.TrackNum.CompareTo(Other.TrackNum);
            }
            return comp;
        }
        public int AlbumComparer(Track Other)
        {
            int comp = String.Compare(this.Album, Other.Album, true);
            if (comp == 0)
            {
                comp = this.DiskNum.CompareTo(Other.DiskNum);
                if (comp == 0)
                {
                    comp = this.TrackNum.CompareTo(Other.TrackNum);
                    if (comp == 0)
                        comp = this.id.CompareTo(Other.id);
                }
            }
            return comp;
        }
        public void ResetPlayHistory()
        {
            lastPlayedDate = DateTime.MinValue;
            playCount = 0;
        }
        public bool ForceLoad()
        {
            this.FileDate = DateTime.MinValue;
            return this.Load();
        }
        public bool Load()
        {
            if (!this.ConfirmExists)
            {
                return false;
            }
            if (this.FileDate != File.GetLastWriteTime(this.FilePath))
            {
                this.toStringDirty = true;
                this.searchStringDirty = true;

                TrackWriter.GetTags(this);

                FileInfo fi = new FileInfo(this.FilePath);
                this.FileSize = fi.Length;
                this.FileDate = fi.LastWriteTime;

                if (Setting.MoveNewFilesIntoMain)
                {
                    this.ChangeType |= (ChangeType.Rename | ChangeType.Move | ChangeType.IgnoreContainment);
                    this.RenameFormat = Setting.DefaultRenameFormat;
                    TrackWriter.AddToUnsavedTracks(this);
                }
                else if (Setting.KeepOrganized)
                {
                    this.ChangeType |= (ChangeType.Rename | ChangeType.Move);
                    this.RenameFormat = Setting.DefaultRenameFormat;
                    TrackWriter.AddToUnsavedTracks(this);
                }

                if (this.Type == FileType.None)
                    return false;

                UpdateMainGroup();

                Database.IncrementDatabaseVersion(false);

                System.Threading.Thread.Sleep(0);
            }
            return true;
        }
        public void SetCover(ImageItem Image)
        {
            cover = Image;
            if (Image == null)
                ForceEmbeddedImageNull = true;
        }

        public override string ToString()
        {
            if (toStringDirty)
            {
                updateToString();
            }
            return toString;
        }
        public string ToShortString()
        {
            return this.Title + " - " + this.Artist;
        }
        
        public override bool Equals(object obj)
        {
            return (obj is Track) && this.Equals(obj as Track);
        }
        public bool Equals(Track TI)
        {
            return TI != null && String.Compare(this.filePath, TI.filePath, StringComparison.OrdinalIgnoreCase) == 0;
        }
        public override int GetHashCode()
        {
            return this.FilePath.ToLowerInvariant().GetHashCode();
        }
        public static int NoTheComparer(string A, string B)
        {
            if (A.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
            {
                if (B.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
                    return A.Substring(4).CompareTo(B.Substring(4));
                else
                    return A.Substring(4).CompareTo(B);
            }
            else if (B.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
            {
                return A.CompareTo(B.Substring(4));
            }
            else
            {
                return A.CompareTo(B);
            }
                
        }

        private string ImageFilePath
        {
            get
            {
                string s = ((this.Album.Length > 0) ? this.Artist + " - " + this.Album : this.Artist).Trim();
                s = Lib.ReplaceBadFilenameChars(s);

                return Path.Combine(Path.GetDirectoryName(this.FilePath), s + ".jpg");
            }
        }

        
        private string ImageFilePath2
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(this.filePath), "folder.jpg");
            }
        }

        public void UpdateMainGroup()
        {
            if (artist.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
                artistNoThe = Artist.Substring(4);
            else
                artistNoThe = Artist;

            if (albumArtist.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
                albumArtistNoThe = AlbumArtist.Substring(4);
            else
                albumArtistNoThe = AlbumArtist;

            if (Compilation)
            {
                mainGroup = Album;
            }
            else if (AlbumArtist.Length > 0)
            {
                mainGroup = AlbumArtist;
            }
            else
            {
                mainGroup = Artist;
            }

            if (mainGroup.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
                mainGroupNoThe = mainGroup.Substring(4);
            else
                mainGroupNoThe = mainGroup;

            toStringDirty = true;
            searchStringDirty = true;
        }
        private void updateSearchString()
        {
            // also used for duplicate detection
            // try to avoid > 4 strings (.Net perf)
            if (albumArtist.Length > 0 && artist != albumArtist)
                searchString = (title + artist + albumArtist + album).ToLowerInvariant();
            else
                searchString = (title + artist + album).ToLowerInvariant();

            searchStringDirty = false;
        }
        private void updateToString()
        {
            toStringDirty = false;

            yearString = year > 0 ? year.ToString() : String.Empty;

            StringBuilder sb = new StringBuilder();
            if (Artist.Length > 0)
                sb.Append(Artist);
            
            if (Title.Length > 0)
            {
                if (sb.Length > 0)
                    sb.Append(separator);
                
                sb.Append(Title);
            }
            if (!Compilation && Album.Length > 0)
            {
                if (sb.Length > 0)
                    sb.Append(separator);

                sb.Append(Album);
            }
            if (this.Duration > 0)
            {
                sb.Append(separator);
                sb.Append(this.DurationInfo);
            }
            if (this.Genre.Length > 0)
            {
                sb.Append(separator);
                sb.Append(Genre);
            }
            if (YearString.Length > 0)
            {
                sb.Append(separator);
                sb.Append(YearString);
            }
            if (trackNum > 0)
            {
                sb.Append(" - Track ");
                sb.Append(TrackNumString);
            }
            if (compilation)
            {
                sb.Append(" (");
                sb.Append(compilationString);
                sb.Append(")");
            }
            toString = sb.ToString();
        }
        private void preLoadImage()
        {
            loadingImage = true;

            setCoverInternal();

            if (cover == null)
            {
                setCoverLocal();

                if (cover == null && (Track.DownloadCoverArt || AllowAlbumCoverDownloadThisTrack))
                {
                    setCoverDownload();
                }
            }

            _coverLoadAttempted = true;

            loadingImage = false;

            for (int i = 0; i < callbacks.Count; i++)
                callbacks[i](this);
        }
        private void setCoverLocal()
        {
            try
            {
                string fileName = ImageFilePath;
                if (File.Exists(fileName))
                {
                    cover = ImageItem.ImageItemFromGraphicsFile(fileName);
                }
                else
                {
                    fileName = ImageFilePath2;
                    if (File.Exists(fileName))
                    {
                        cover = ImageItem.ImageItemFromGraphicsFile(fileName);
                    }
                }
            }
            catch
            {
            }
        }
        private void setCoverDownload()
        {
            if (this.cover == null)
            {
                this.cover = ImageItem.ImageFromLastFM(this.Artist,
                                                       this.Album);

                if (cover != null)
                {
                    switch (controller.ArtSaveOption)
                    {
                        case ArtSaveOption.Artist_Album:
                            this.cover.Save(this.ImageFilePath);
                            break;
                        case ArtSaveOption.Folder_JPG:
                            this.cover.Save(this.ImageFilePath2);
                            break;
                    }
                }
            }
        }
        private void setCoverInternal()
        {
            if (cover == null && !ForceEmbeddedImageNull)
            {
                if (!ConfirmExists)
                {
                    return;
                }
                this.cover = ImageItem.ImageItemFromAudioFile(this.FilePath);
            }
        }
    }
    
    internal class CompareByFilePath : IEqualityComparer<Track>
    {
        public bool Equals(Track A, Track B)
        {
            return String.Compare(A.FilePath, B.FilePath, StringComparison.OrdinalIgnoreCase) == 0;
        }
        public int GetHashCode(Track A)
        {
            return A.FilePath.ToLower().GetHashCode();
        }
    }
}
