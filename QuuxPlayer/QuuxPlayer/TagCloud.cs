/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal class TagCloud : Control, IMainView
    {
        private delegate void NeedRefreshDelegate(Item Item);

        private static object @lock = new object();

        private enum ViewModeEnum { None, Artist, Genre, Grouping, Album };
        private enum ChooseModeEnum { Top, Random };

        private class Item
        {
            private const int NUM_COLORS = 20;
            private const double EXPONENT = 0.45;
            private const int MAX_FONT_SIZE = 60;
            private const int MIN_FONT_SIZE = 4;

            private static Font[] fonts;

            private const TextFormatFlags tff = TextFormatFlags.NoClipping | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.HorizontalCenter;

            public static event NeedRefreshDelegate NeedRefresh;

            private static Rectangle clientArea = Rectangle.Empty;
            private static Color[] colors;
            private static Item hoverItem = null;
            private static Random rand = new Random();
            private static bool useColor = false;
            private static float fontScale;
            private static string mouseoverHintFormatStringSingular = Localization.Get(UI_Key.Tag_Cloud_Mouseover_Hint_Singular);
            private static string mouseoverHintFormatStringPlural = Localization.Get(UI_Key.Tag_Cloud_Mouseover_Hint_Plural);

            private Font font = null;

            public static List<Item> Items
            { get; set; }
            public static int TotalBasis
            { get; set; }
            public static Control Parent
            { get; set; }
            public static Rectangle ClientArea
            {
                set
                {
                    if (value != clientArea)
                    {
                        clientArea = value;
                        areaFactor = (float)Math.Max(0.4, Math.Sqrt((float)(clientArea.Height * clientArea.Width) / (float)(1280 * 614)));
                    }
                }
            }

            public string Text
            { get; set; }
            public bool LocationFinal
            { get; set; }
            public float Percentage
            { get; set; }
            public int Count
            { get; set; }
            public Color Color
            { get; set; }

            private Rectangle rect
            { get; set; }

            public Item(string Artist, int Basis)
            {
                this.Text = Artist;
                this.LocationFinal = false;
                this.Count = Basis;
                this.rect = Rectangle.Empty;
                Items.Add(this);
            }
            static Item()
            {
                Items = new List<Item>();
                areaFactor = 1.0f;

                colors = new Color[NUM_COLORS];
                int h = 0x00;
                int l = 0x80;
                int s = 0xA0;

                int offset = 0xE0 / NUM_COLORS;

                for (int i = 0; i < NUM_COLORS; i++)
                {
                    colors[i] = ColorTranslator.FromWin32(Lib.ColorHLSToRGB(h, l, s));
                    h += offset;
                    //l -= 0x18;
                }
                fonts = new Font[MAX_FONT_SIZE + 1];
                for (int i = MIN_FONT_SIZE; i <= MAX_FONT_SIZE; i++)
                    fonts[i] = new Font(Styles.FontNameAlt, i, FontStyle.Bold);
            }

            public static void Clear()
            {
                hoverItem = null;
                Items.Clear();
            }
            public static void Arrange()
            {
                if (Items.Count > 0)
                {
                    Items.Sort((a, b) => b.Count.CompareTo(a.Count));

                    TotalBasis = Items.Sum(ii => ii.Count);

                    int avgCount = TotalBasis / Items.Count;
                    int minCount = Items.Min(ii => ii.Count);
                    int maxCount = Items.Max(ii => ii.Count);

                    double low = (double)minCount / (double)avgCount; //0.3;
                    double high = (double)maxCount / (double)avgCount; //2.0f;
                    double offset = Math.Pow(Math.Max(2.0, (double)maxCount / (double)minCount), 1.0 / (double)NUM_COLORS);

                    CountLevels = new int[NUM_COLORS];

                    double level = Math.Pow(maxCount, 1.0 - 0.5 / (double)NUM_COLORS);//high * (double)avgCount;

                    for (int i = 0; i < NUM_COLORS; i++)
                    {
                        CountLevels[i] = (int)(level);
                        level /= offset;
                    }

                    CountLevels[NUM_COLORS - 1] = -1;

                    textLengthFactor = (float)Math.Sqrt(13f / Items.Average(ii => (float)ii.Text.Length));
                    fontScale = areaFactor * textLengthFactor * (float)(12.0 / Math.Pow(0.005, EXPONENT));

                    foreach (Item i in Items)
                    {
                        i.setup();
                    }
                    arrange();
                }
            }
            public static Item ItemAt(Point P)
            {
                return Items.FirstOrDefault(i => i.R.Contains(P));
            }
            
            private void setup()
            {
                if (this.R == Rectangle.Empty)
                {
                    this.Percentage = (float)this.Count / (float)TotalBasis;

                    float scale = fontScale * (float)Math.Pow(Percentage, EXPONENT);

                    this.font = fonts[Math.Max(Math.Min(MAX_FONT_SIZE, (int)scale), MIN_FONT_SIZE)];

                    Size sz = new Size(int.MaxValue, int.MaxValue);

                    this.R = new Rectangle(Point.Empty,
                                           TextRenderer.MeasureText(this.Text, this.font, sz, tff));

                    this.R = RandomPosition();

                    for (int i = 0; i < NUM_COLORS; i++)
                        if (this.Count > CountLevels[i])
                        {
                            this.Color = colors[i];
                            break;
                        }
                }
            }
            
            private int IntersectedArea()
            {
                return IntersectedArea(this.R);
            }
            private int IntersectedArea(Rectangle r)
            {
                int tot = 0;

                foreach (Item i in Items)
                    if (!i.Equals(this))
                    {
                        if (i.R.IntersectsWith(r))
                        {
                            Rectangle rr = i.R;
                            rr.Intersect(r);
                            tot += (rr.Height * rr.Width);

                            //tot +=
                            //    (Math.Max(i.R.Left, r.Left) - Math.Min(i.R.Right, r.Right)) *
                            //    (Math.Max(i.R.Top, r.Top) - Math.Min(i.R.Bottom, r.Bottom));
                        }
                    }

                return tot;
            }
                        
            public static Item HoverItem
            {
                get { return hoverItem; }
                set
                {
                    if (hoverItem != value)
                    {
                        NeedRefresh(value);
                        NeedRefresh(hoverItem);
                        hoverItem = value;
                    }
                }
            }
            public static bool UseColor
            {
                get { return useColor; }
                set
                {
                    if (useColor != value)
                    {
                        useColor = value;
                    }
                }
            }
            
            private static int[] CountLevels
            { get; set; }
            private static float textLengthFactor
            { get; set; }
            private static float areaFactor
            { get; set; }

            public Rectangle R
            {
                get { return rect; }
                set
                {
                    rect = value;
                }
            }

            private static void arrange()
            {
                bool done = false;
                int limit = Math.Max(5, Math.Min(100, 2500 / Items.Count));
                int j = 0;

                while (!done)
                {
                    done = true;
                    foreach (Item i in Items)
                    {
                        if (!i.LocationFinal && Items.Exists(ii => (!ii.Equals(i)) && ii.R.IntersectsWith(i.R)))
                        {
                            done = false;

                            Rectangle r = i.RandomPosition();

                            if (i.IntersectedArea(r) < i.IntersectedArea())
                            {
                                i.R = r;
                            }
                            else // try again
                            {
                                r = i.RandomPosition();

                                if (i.IntersectedArea(r) < i.IntersectedArea())
                                    i.R = r;
                            }
                        }
                        else
                        {
                            i.LocationFinal = true;
                        }
                    }
                    if (++j > limit)
                        done = true;
                }
            }
            
            public static Item MouseMove(Point P)
            {
                lock (@lock)
                {
                    HoverItem = Items.LastOrDefault(ii => ii.R.Contains(P));
                }
                return HoverItem;
            }
            
            public void Render(Graphics g)
            {
                TextRenderer.DrawText(g,
                                      this.Text,
                                      this.font,
                                      this.R,
                                      useColor ? (this.Equals(HoverItem) ? Styles.LightText : this.Color) : (this.Equals(HoverItem) ? Styles.Playing : Styles.LightText),
                                      tff);
            }
            public Rectangle TransformRect(int Type)
            {
                int inflateFactor = 10;

                Size s = new Size(R.Width * (inflateFactor + 1) / inflateFactor, R.Height * (inflateFactor + 1) / inflateFactor);

                inflateFactor = 8;

                Point p;

                switch (Type)
                {
                    case 0:
                        p = new Point(R.X - R.Width / (inflateFactor * 2), R.Y - R.Height / (inflateFactor * 2));
                        break;
                    case 1:
                        p = new Point(R.X , R.Y - R.Height / (inflateFactor * 2));
                        break;
                    case 2:
                        p = new Point(R.X - R.Width / (inflateFactor * 2), R.Y);
                        break;
                    case 3:
                        p = new Point(R.X - R.Width / (inflateFactor), R.Y - R.Height / (inflateFactor * 2));
                        break;
                    case 4:
                        p = new Point(R.X - R.Width / (inflateFactor * 2), R.Y - R.Height / (inflateFactor));
                        break;
                    case 5:
                        p = new Point(R.X - R.Width / (inflateFactor), R.Y - R.Height / (inflateFactor));
                        break;
                    case 6:
                        p = new Point(R.X, R.Y - R.Height / (inflateFactor));
                        break;
                    case 7:
                        p = new Point(R.X - R.Width / (inflateFactor), R.Y);
                        break;
                    case 8:
                        p = R.Location;
                        break;

                    default:
                        throw new Exception();
                }
                if (p.X < 0)
                    p.X = 0;
                if (p.Y < 0)
                    p.Y = 0;
                if (p.X + s.Width > Parent.Right)
                    p.X = Parent.Right - s.Width;
                if (p.Y + s.Height > Parent.Height)
                    p.Y = Parent.Height - s.Height;

                return new Rectangle(p, s);
            }
            public override string ToString()
            {
                return String.Format((this.Count == 1) ? mouseoverHintFormatStringSingular : mouseoverHintFormatStringPlural, this.Text, this.Count);
            }

            private Rectangle RandomPosition()
            {
                return new Rectangle(new Point((int)(clientArea.Left + rand.NextDouble() * (clientArea.Width - this.R.Width)),
                                               (int)(clientArea.Top + rand.NextDouble() * (clientArea.Height - this.R.Height))),
                                     this.R.Size);

            }
        }

        private QButton btnArtists;
        private QButton btnGenres;
        private QButton btnGroupings;
        private QButton btnAlbums;
        private QButton btnChooseTop;
        private QButton btnChooseRandom;
        private QButton btnUseColor;
        private QLabel lblShow;
        private QComboBox cboType;

        private uint dbVersion = 0;
        private int totalTracks;

        private string currentArtist = String.Empty;
        private string currentGenre = String.Empty;
        private string currentGrouping = String.Empty;

        private const int MARGIN = 8;

        private int maxItems = 100;

        private ulong refreshAllDelay = Clock.NULL_ALARM;
        private ulong setupDelay = Clock.NULL_ALARM;

        private ViewModeEnum viewMode = ViewModeEnum.None;
        private ViewModeEnum ViewMode
        {
            get { return viewMode; }
            set
            {
                if (viewMode != value)
                {
                    viewMode = value;
                    btnArtists.Value = (viewMode == ViewModeEnum.Artist);
                    btnAlbums.Value = (viewMode == ViewModeEnum.Album);
                    btnGenres.Value = (viewMode == ViewModeEnum.Genre);
                    btnGroupings.Value = (viewMode == ViewModeEnum.Grouping);
                }
            }
        }
        private ChooseModeEnum chooseMode = ChooseModeEnum.Top;
        private ChooseModeEnum ChooseMode
        {
            get { return chooseMode; }
            set
            {
                if (chooseMode != value)
                {
                    chooseMode = value;
                    btnChooseTop.Value = (chooseMode == ChooseModeEnum.Top);
                    btnChooseRandom.Value = (chooseMode == ChooseModeEnum.Random);
                }
            }
        }

        private string artists;
        private string albums;
        private string groupings;
        private string genres;

        private Item renderItem = null;

        public TagCloud()
        {
            Item.Parent = this;
            Item.NeedRefresh += (i) => { this.invalidate(i); };
            this.DoubleBuffered = true;

            artists = Localization.Get(UI_Key.Tag_Cloud_Artists);
            albums = Localization.Get(UI_Key.Tag_Cloud_Albums);
            groupings = Localization.Get(UI_Key.Tag_Cloud_Groupings);
            genres = Localization.Get(UI_Key.Tag_Cloud_Genres);

            lblShow = new QLabel(Localization.Get(UI_Key.Tag_Cloud_Show_At_Most));
            lblShow.Location = new Point(MARGIN, MARGIN + 4);
            lblShow.ForeColor = Styles.LightText;
            this.Controls.Add(lblShow);

            cboType = new QComboBox(false);
            cboType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboType.Items.AddRange(new string[] { "10", "25", "50", "75", "100", "125", "150", "200", "300", "500", "1000" });
            cboType.SelectedIndex = 4;
            
            cboType.Location = new Point(lblShow.Right, MARGIN);
            this.Controls.Add(cboType);

            btnGenres = new QButton(genres, false, false);
            btnGenres.Value = false;
            btnGenres.ButtonPressed += (s) =>
            {
                this.ViewMode = ViewModeEnum.Genre;
                this.setupItems();
            };
            btnGenres.Location = new Point(cboType.Right + MARGIN + MARGIN, MARGIN);
            this.Controls.Add(btnGenres);

            btnArtists = new QButton(artists, false, false);
            btnArtists.Value = true;
            btnArtists.ButtonPressed += (s) =>
            {
                if (this.ViewMode == ViewModeEnum.Artist)
                {
                    currentGenre = String.Empty;
                    currentGrouping = String.Empty; }
                else
                {
                    currentArtist = String.Empty;
                    this.ViewMode = ViewModeEnum.Artist;
                }
                this.setupItems();
            };
            btnArtists.Location = new Point(btnGenres.Right + MARGIN, btnGenres.Top);
            this.Controls.Add(btnArtists);

            btnAlbums = new QButton(albums, false, false);
            btnArtists.Value = false;
            btnAlbums.ButtonPressed += (s) =>
            {
                if (this.ViewMode == ViewModeEnum.Album)
                    currentArtist = String.Empty;
                else
                    this.ViewMode = ViewModeEnum.Album;
                this.setupItems();
            };
            btnAlbums.Location = new Point(btnArtists.Right + MARGIN, btnGenres.Top);
            this.Controls.Add(btnAlbums);

            btnGroupings = new QButton(groupings, false, false);
            btnGroupings.Value = false;
            btnGroupings.ButtonPressed += (s) =>
            {
                this.ViewMode = ViewModeEnum.Grouping;
                this.setupItems();
            };
            btnGroupings.Location = new Point(btnAlbums.Right + MARGIN, btnGenres.Top);
            this.Controls.Add(btnGroupings);

            btnChooseTop = new QButton(Localization.Get(UI_Key.Tag_Cloud_Choose_Top), false, false);
            btnChooseTop.Value = true;
            btnChooseTop.ButtonPressed += (s) =>
            {
                clearCurrentValues();
                this.ChooseMode = ChooseModeEnum.Top;
                this.setupItems();
            };
            btnChooseTop.Location = new Point(btnGroupings.Right + MARGIN + MARGIN, btnGenres.Top);
            this.Controls.Add(btnChooseTop);

            btnChooseRandom = new QButton(Localization.Get(UI_Key.Tag_Cloud_Choose_Random), false, false);
            btnChooseRandom.Value = false;
            btnChooseRandom.ButtonPressed += (s) =>
            {
                clearCurrentValues();
                this.ChooseMode = ChooseModeEnum.Random;
                this.setupItems();
            };
            btnChooseRandom.Location = new Point(btnChooseTop.Right + MARGIN, btnGenres.Top);
            this.Controls.Add(btnChooseRandom);

            btnUseColor = new QButton("Use Color", true, false);
            btnUseColor.Value = false;
            btnUseColor.ButtonPressed += (s) =>
            {
                Item.UseColor = btnUseColor.Value;
                this.Invalidate();
            };
            btnUseColor.Location = new Point(btnChooseRandom.Right + MARGIN + MARGIN, btnGenres.Top);
            this.Controls.Add(btnUseColor);
        }

        public ViewType ViewType { get { return ViewType.TagCloud; } }

        public int MaxItems
        {
            get { return maxItems; }
        }
        public bool UseColor
        {
            get { return Item.UseColor; }
        }
        public void Start(Rectangle Rect, int MaxItems, bool UseColor)
        {
            this.Bounds = Rect;
            this.ViewMode = ViewModeEnum.Artist;
            this.ChooseMode = ChooseModeEnum.Top;

            btnUseColor.Value = UseColor;
            Item.UseColor = UseColor;

            int index = cboType.FindStringExact(MaxItems.ToString());

            if (index >= 0)
            {
                cboType.SelectedIndex = index;
                maxItems = MaxItems;
            }

            cboType.SelectedValueChanged += new EventHandler(cboType_SelectedValueChanged);

            //Clock.DoOnMainThread(setupItems, 1000);
        }
        
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            //base.OnPaintBackground(pevent);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            //base.OnPaint(e);

            e.Graphics.Clear(Styles.Dark);

            if (renderItem == null || renderItem.R != e.ClipRectangle)
            {
                lock (@lock)
                {
                    foreach (Item ii in Item.Items)
                    {
                        ii.Render(e.Graphics);
                    }
                }
            }
            if (renderItem != null)
            {
                renderItem.Render(e.Graphics);
                renderItem = null;
            }
            if (Item.HoverItem != null)
                Item.HoverItem.Render(e.Graphics);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Item i = Item.MouseMove(e.Location);
            if (i == null)
            {
                this.Cursor = Cursors.Default;
            }
            else
            {
                this.Cursor = Cursors.Hand;
                Controller.ShowMessage(i.ToString());
            }
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            Item i;

            lock (@lock)
            {
                i = Item.ItemAt(e.Location);
            }
            Controller c = Controller.GetInstance();
            if (i == null)
            {
                c.RequestAction(QActionType.AdvanceScreen);
            }
            else
            {
                switch (this.ViewMode)
                {
                    case ViewModeEnum.Artist:

                        if ((e.Button == MouseButtons.Right) || (i.Count < 2) || (Database.FindAllTracks(t => t.MainGroup == i.Text && t.Album.Length > 0).GroupBy(t => t.Album).Count() < 2))
                        {
                            c.RequestAction(new QAction(QActionType.ShowAllOfArtist, i.Text));
                        }
                        else
                        {
                            this.ViewMode = ViewModeEnum.Album;
                            currentArtist = i.Text;
                            setupItems();
                        }
                        break;
                    case ViewModeEnum.Album:
                        c.RequestAction(new QAction(QActionType.ShowAllOfAlbum, i.Text, currentArtist));
                        break;
                    case ViewModeEnum.Genre:

                        if (e.Button == MouseButtons.Right)
                        {
                            c.RequestAction(new QAction(QActionType.ShowAllOfGenre, i.Text));
                        }
                        else
                        {
                            this.ViewMode = ViewModeEnum.Artist;

                            currentGrouping = String.Empty;
                            currentGenre = i.Text;

                            setupItems();
                        }
                        break;
                    case ViewModeEnum.Grouping:

                        if (e.Button == MouseButtons.Right)
                        {
                            c.RequestAction(new QAction(QActionType.ShowAllOfGrouping, i.Text));
                        }
                        else
                        {
                            this.ViewMode = ViewModeEnum.Artist;

                            currentGrouping = i.Text;
                            currentGenre = String.Empty;

                            setupItems();
                        }
                        break;
                }
            }
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Clock.Update(ref setupDelay, setupItems, 100, false);
            System.Diagnostics.Debug.WriteLine("resize");
        }
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible && Database.LatestLibraryAddOrRemove > dbVersion)
            {
                this.invalidate();
                setupItems();
            }
        }

        private void cboType_SelectedValueChanged(object sender, EventArgs e)
        {
            maxItems = Int32.Parse(cboType.SelectedItem.ToString());
            setupItems();
        }
        private void invalidateAsync()
        {
            renderItem = null;
            Clock.DoOnMainThread(this.invalidate);
        }
        private void invalidate()
        {
            this.Invalidate();
            this.UseWaitCursor = false;
            this.Cursor = Cursors.Default;
        }
        private void invalidate(Item i)
        {
            if (i != null)
            {
                if (renderItem != null)
                {
                    renderItem = null;
                    this.Invalidate();
                }
                else
                {
                    Clock.Update(ref refreshAllDelay, this.refreshAll, 50, false);
                    renderItem = i;
                    this.Invalidate(i.R);
                }
            }
        }
        private void refreshAll()
        {
            refreshAllDelay = Clock.NULL_ALARM;
            this.invalidateAsync();
        }
        private void reset()
        {
            if (currentArtist.Length > 0 || currentGenre.Length > 0 || currentGrouping.Length > 0)
            {
                clearCurrentValues();
                setupItems();
            }
        }
        private void clearCurrentValues()
        {
            currentArtist = String.Empty;
            currentGenre = String.Empty;
            currentGrouping = String.Empty;
        }
        private void setupItems()
        {
            setupDelay = Clock.NULL_ALARM;

            if (this.Visible)
                this.UseWaitCursor = true;

            renderItem = null;

            Item.ClientArea = new Rectangle(0, btnGenres.Bottom + 5, this.ClientRectangle.Width, this.ClientRectangle.Height - btnGenres.Bottom - 10);
            Clock.DoOnNewThread(setupItemsAsync);
        }
        private void setupItemsAsync()
        {
            lock (@lock)
            {
                try
                {
                    Item.Clear();

                    lock (Database.LibraryLock)
                    {
                        switch (ChooseMode)
                        {
                            case ChooseModeEnum.Top:
                                setupItemsTop();
                                break;
                            case ChooseModeEnum.Random:
                                setupItemsRandom();
                                break;
                        }
                    }
                    Item.Arrange();
                }
                catch { }
            }
            this.invalidateAsync();
            dbVersion = Database.Version;
        }
        private void setupItemsTop()
        {
            switch (this.ViewMode)
            {
                // Argh, anonymous types cause duplication
                case ViewModeEnum.Artist:
                    var v =
                        (from t in Database.Library
                         where (t.MainGroup.Length > 0) && (currentGenre.Length == 0 || t.Genre == currentGenre) && (currentGrouping.Length == 0 || t.Grouping == currentGrouping)
                         group t by t.MainGroup
                             into g
                             where g.Key.Length > 0
                             select new { text = g.Key, count = g.Count() }).ToList();

                    v.Sort((a, b) => b.count.CompareTo(a.count));

                    v = v.Take(maxItems).ToList();

                    totalTracks = v.Sum(vv => vv.count);

                    foreach (var vv in v)
                    {
                        new Item(vv.text,
                                 vv.count);
                    }
                    break;
                case ViewModeEnum.Album:
                    currentGenre = String.Empty;
                    currentGrouping = String.Empty;
                    var v2 =
                        (from t in Database.Library
                         where (t.Album.Length > 0) && (currentArtist.Length == 0 || t.MainGroup == currentArtist)
                         group t by t.Album
                             into g
                             where g.Key.Length > 0
                             select new { text = g.Key, count = g.Count() }).ToList();

                    v2.Sort((a, b) => b.count.CompareTo(a.count));

                    v2 = v2.Take(maxItems).ToList();

                    totalTracks = v2.Sum(vv2 => vv2.count);

                    foreach (var vv2 in v2)
                    {
                        new Item(vv2.text,
                                 vv2.count);
                    }
                    break;
                case ViewModeEnum.Genre:
                    currentGrouping = String.Empty;
                    var v3 =
                    (from t in Database.Library
                     where t.Genre.Length > 0
                     group t by t.Genre
                         into g
                         where g.Key.Length > 0
                         select new { text = g.Key, count = g.Count() }).ToList();

                    v3.Sort((a, b) => b.count.CompareTo(a.count));

                    v3 = v3.Take(maxItems).ToList();

                    totalTracks = v3.Sum(vv3 => vv3.count);

                    foreach (var vv3 in v3)
                    {
                        new Item(vv3.text,
                                 vv3.count);
                    }
                    break;
                case ViewModeEnum.Grouping:
                    currentGenre = String.Empty;
                    var v4 =
                    (from t in Database.Library
                     where t.Grouping.Length > 0
                     group t by t.Grouping
                         into g
                         where g.Key.Length > 0
                         select new { text = g.Key, count = g.Count() }).ToList();

                    v4.Sort((a, b) => b.count.CompareTo(a.count));

                    v4 = v4.Take(maxItems).ToList();

                    totalTracks = v4.Sum(vv4 => vv4.count);

                    foreach (var vv4 in v4)
                    {
                        new Item(vv4.text,
                                 vv4.count);
                    }
                    break;
            }
        }
        private void setupItemsRandom()
        {
            Random rand = new Random();

            switch (this.ViewMode)
            {
                // Argh, anonymous types cause duplication
                case ViewModeEnum.Artist:
                    var v =
                        (from t in Database.Library
                         where (t.MainGroup.Length > 0) && (currentGenre.Length == 0 || t.Genre == currentGenre) && (currentGrouping.Length == 0 || t.Grouping == currentGrouping)
                         group t by t.MainGroup
                             into g
                             where g.Key.Length > 0
                             select new { text = g.Key, count = g.Count(), rand = rand.Next(100000) }).ToList();

                    v.Sort((a, b) => a.rand.CompareTo(b.rand));

                    v = v.Take(maxItems).ToList();

                    totalTracks = v.Sum(vv => vv.count);

                    foreach (var vv in v)
                    {
                        new Item(vv.text,
                                 vv.count);
                    }
                    break;
                case ViewModeEnum.Album:
                    var v2 =
                        (from t in Database.Library
                         where (t.Album.Length > 0) && (currentArtist.Length == 0 || t.MainGroup == currentArtist)
                         group t by t.Album
                             into g
                             where g.Key.Length > 0
                             select new { text = g.Key, count = g.Count(), rand = rand.Next(100000) }).ToList();

                    v2.Sort((a, b) => a.rand.CompareTo(b.rand));

                    v2 = v2.Take(maxItems).ToList();

                    totalTracks = v2.Sum(vv => vv.count);

                    foreach (var vv in v2)
                    {
                        new Item(vv.text,
                                 vv.count);
                    }
                    break;
                case ViewModeEnum.Genre:
                    currentGrouping = String.Empty;
                    var v3 =
                        (from t in Database.Library
                         where t.Genre.Length > 0
                         group t by t.Genre
                             into g
                             where g.Key.Length > 0
                             select new { text = g.Key, count = g.Count(), rand = rand.Next(100000) }).ToList();

                    v3.Sort((a, b) => b.rand.CompareTo(a.rand));

                    v3 = v3.Take(maxItems).ToList();

                    totalTracks = v3.Sum(vv3 => vv3.count);

                    foreach (var vv3 in v3)
                    {
                        new Item(vv3.text,
                                 vv3.count);
                    }
                    break;
                case ViewModeEnum.Grouping:
                    currentGenre = String.Empty;
                    var v4 =
                    (from t in Database.Library
                     where t.Grouping.Length > 0
                     group t by t.Grouping
                         into g
                         where g.Key.Length > 0
                         select new { text = g.Key, count = g.Count(), rand = rand.Next(100000) }).ToList();

                    v4.Sort((a, b) => b.rand.CompareTo(a.rand));

                    v4 = v4.Take(maxItems).ToList();

                    totalTracks = v4.Sum(vv4 => vv4.count);

                    foreach (var vv4 in v4)
                    {
                        new Item(vv4.text,
                                 vv4.count);
                    }
                    break;
            }
        }
    }
}