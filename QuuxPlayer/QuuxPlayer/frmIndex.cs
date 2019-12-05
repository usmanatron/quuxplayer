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
    internal sealed class frmIndex : QFixedDialog
    {
        public delegate void IndexCallback(frmIndex IndexForm);

        public const char CLEAR_CHAR = '_';

        private const int BORDER_WIDTH = 2;
        private const int MAX_COLS = 12;

        public FilterType FilterTypeBasis { get; private set; }
        public bool NoData { get; private set; }

        private List<QButton> buttons;
        private char filterChar;
        private int desiredWidth = 0;
        private IndexCallback callback;
        private FilterButton button;
        private bool closeButtonHovering = false;
        private Rectangle closeButtonRect;
        private QToolTip tooltip;
        private int rows;
        private int cols;
        private string caption;
        private Rectangle captionRect;

        public frmIndex(FilterBar Parent, Point Anchor, IndexCallback Callback, FilterButton Button) : base(String.Empty, ButtonCreateType.None)
        {
            this.DoubleBuffered = true;

            callback = Callback;
            button = Button;

            this.filterChar = '\0';

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.KeyPreview = true;
            this.FilterTypeBasis = button.FilterType;

            setCaption();

            buttons = new List<QButton>();

            var aa = getChars(Parent, Button.FilterType).ToList();

            aa.RemoveAll(c => c <= ' ' || c == CLEAR_CHAR);

            if (aa.Count() == 0)
            {
                this.NoData = true;
                this.Close();
                this.callback(this);
                return;
            }

            this.NoData = false;

            aa.Sort();

            if (button.ValueType != FilterValueType.None)
                aa.Add(CLEAR_CHAR);

            aa = aa.Take(55).ToList();
            
            bool filterBadChars;

            switch (System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName)
            {
                case "en":
                case "de":
                case "it":
                case "cs":
                case "fr":
                case "pt":
                    filterBadChars = true;
                    break;
                default:
                    filterBadChars = false;
                    break;
            }

            foreach (char c in aa)
            {
                QButton b;
                if (Button.FilterType == FilterType.Year && c != CLEAR_CHAR)
                    b = new QButton(c.ToString() + "0s", false, true);
                else
                    b = new QButton(c.ToString(), false, true);
               
                if ((!filterBadChars) || (c < 0xFF))
                {
                    b.Tag = c;
                    buttons.Add(b);
                    b.BackColor = this.BackColor;
                    b.ButtonPressed += new QButton.ButtonDelegate(click);
                    this.Controls.Add(b);
                }
            }

            this.MinimumSize = Size.Empty;

            buttons.Add(new QButton("~", false, true)); // placeholder for close button

            desiredWidth = arrangeButtons() + BORDER_WIDTH + 2;

            closeButtonRect = new Rectangle(buttons[buttons.Count - 1].Location, Properties.Resources.filter_index_close_hover.Size);

            buttons.RemoveAt(buttons.Count - 1);

            this.ClientSize = new System.Drawing.Size(desiredWidth,
                                                      rows * buttons[0].Height + BORDER_WIDTH * 2);

            
            Point p = Parent.PointToScreen(new Point(Math.Min(Anchor.X - this.Width / 2, Parent.Right - this.Width), Anchor.Y));
            p = new Point(Math.Max(0, p.X), p.Y);
            this.Location = p;

            this.Owner = Lib.MainForm;

            this.Show(Lib.MainForm);

            QButton lastButton = buttons[buttons.Count - 1];
            if (lastButton.Text[0] == '_')
            {
                QToolTip clearToolTip = new QToolTip(lastButton, String.Empty);
                clearToolTip.SetToolTip(lastButton, Localization.Get(UI_Key.Filter_Index_Clear));
            }
            tooltip = new QToolTip(this, Localization.Get(UI_Key.Filter_Index_Cancel));
            tooltip.Active = false;
        }
        private void setCaption()
        {
            switch (button.FilterType)
            {
                case FilterType.Playlist:
                    caption = Localization.Get(UI_Key.Filter_Playlist);
                    break;
                case FilterType.Artist:
                    caption = Localization.Get(UI_Key.Filter_Artist);
                    break;
                case FilterType.Album:
                    caption = Localization.Get(UI_Key.Filter_Album);
                    break;
                case FilterType.Genre:
                    caption = Localization.Get(UI_Key.Filter_Genre);
                    break;
                case FilterType.Year:
                    caption = Localization.Get(UI_Key.Filter_Year);
                    break;
                case FilterType.Grouping:
                    caption = Localization.Get(UI_Key.Filter_Grouping);
                    break;
            }
            caption += " " + Localization.Get(UI_Key.Filter_Index_Caption);
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.Activate();
        }

        private int arrangeButtons()
        {
            if (buttons.Count > MAX_COLS)
            {
                rows = (int)Math.Ceiling(((double)buttons.Count + 1.0) / MAX_COLS);
                cols = (int)Math.Ceiling((double)buttons.Count / rows);
            }
            else
            {
                rows = 1;
                cols = buttons.Count;
            }

            captionRect = new Rectangle(new Point(BORDER_WIDTH, BORDER_WIDTH),
                                        new Size(TextRenderer.MeasureText(caption, Styles.FontItalic, new Size(Int32.MaxValue, rows * buttons[0].Height), TextFormatFlags.NoPrefix).Width, rows * buttons[0].Height));

            List<List<QButton>> cells = new List<List<QButton>>();

            int k = 0;

            for (int j = 0; j < rows; j++)
            {
                for (int i = 0; i < cols; i++)
                {
                    while (cells.Count <= i)
                        cells.Add(new List<QButton>());

                    if (k < buttons.Count)
                        cells[i].Add(buttons[k++]);
                    else
                        break;
                }
            }

            int x = captionRect.Right;
            int maxX = 0;
            for (int i = 0; i < cols; i++)
            {
                int newX = x;
                int maxWidth = 0;
                for (int j = 0; j < cells[i].Count; j++)
                    maxWidth = Math.Max(maxWidth, cells[i][j].Width);

                for (int j = 0; j < cells[i].Count; j++)
                {
                    cells[i][j].Location = new Point(x + (maxWidth - cells[i][j].Width) / 2,
                                                     j * buttons[0].Height + BORDER_WIDTH);
                    newX = Math.Max(newX, cells[i][j].Right);
                }
                x = newX;
                maxX = Math.Max(maxX, newX);
            }

            return maxX;
        }
        public char Char
        {
            get { return filterChar; }
        }
        public FilterButton Button
        {
            get { return button; }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            CloseButtonHovering = closeButtonRect.Contains(e.Location);
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (closeButtonRect.Contains(e.Location))
                this.close();
        }
        private static IEnumerable<char> getChars(FilterBar FilterBar, FilterType FilterType)
        {
            switch (FilterType)
            {
                case FilterType.Playlist:
                    return from pl in Database.GetPlaylists(FilterBar.GetTracksWithoutFiltering(FilterType.Playlist, false))
                           group pl by Char.ToUpperInvariant(pl[0])
                               into g
                               select g.Key;
                case FilterType.Artist:
                    return from t in FilterBar.GetTracksWithoutFiltering(FilterType.Artist, false)
                            where t.MainGroupNoThe.Length > 0
                            group t by Char.ToUpperInvariant(t.MainGroupNoThe[0])
                                into g
                                select g.Key;
                case FilterType.Album:
                    return from t in FilterBar.GetTracksWithoutFiltering(FilterType.Album, false)
                             where t.Album.Length > 0
                             group t by Char.ToUpperInvariant(t.Album[0])
                                 into g
                                 select g.Key;
                case FilterType.Genre:
                    return from t in FilterBar.GetTracksWithoutFiltering(FilterType.Genre, false)
                             where t.Genre.Length > 0
                             group t by Char.ToUpperInvariant(t.Genre[0])
                                 into g
                                 select g.Key;
                case FilterType.Year:
                    return from t in FilterBar.GetTracksWithoutFiltering(FilterType.Year, false)
                           group t by t.DecadeChar
                               into g
                               select g.Key;
                case FilterType.Grouping:
                    return from t in FilterBar.GetTracksWithoutFiltering(FilterType.Grouping, false)
                             where t.Grouping.Length > 0
                             group t by Char.ToUpperInvariant(t.Grouping[0])
                                 into g
                                 select g.Key;
                default:
                    return new List<char>();
            }
        }

        private void click(QButton Button)
        {
            invoke((char)Button.Tag);
        }
        private void invoke(char c)
        {
            filterChar = c;
            close();
        }
        private void close()
        {
            //this.Visible = false;
            this.Close();
            callback(this);
        }

        private bool CloseButtonHovering
        {
            get { return closeButtonHovering; }
            set
            {
                if (closeButtonHovering != value)
                {
                    closeButtonHovering = value;
                    tooltip.SetToolTip(this, Localization.Get(UI_Key.Filter_Index_Cancel));
                    tooltip.Active = value;
                    this.Invalidate();
                }
            }
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Enter:
                case Keys.Space:
                    foreach (QButton b in buttons)
                        if (b.Focused)
                            invoke(b.Text[0]);
                    return true;
                case Keys.Escape:
                case KeyDefs.ShowFilterIndex:
                    close();
                    return true;
                default:
                    
                    return base.ProcessCmdKey(ref msg, keyData);
            }
        }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if (buttons.Exists(b => b.Text[0] == Char.ToUpperInvariant(e.KeyChar)))
            {
                e.Handled = true;
                invoke(Char.ToUpperInvariant(e.KeyChar));
            }
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.Activate();
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            CloseButtonHovering = false;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            TextRenderer.DrawText(e.Graphics, caption, Styles.FontItalic, captionRect, Styles.LightText, TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);

            Pen p = new Pen(Styles.LightText, BORDER_WIDTH);

            e.Graphics.DrawRectangle(p, new System.Drawing.Rectangle(BORDER_WIDTH - 1, BORDER_WIDTH - 1, this.ClientRectangle.Width - BORDER_WIDTH, this.ClientRectangle.Height - BORDER_WIDTH));

            p.Dispose();

            if (closeButtonHovering)
                e.Graphics.DrawImageUnscaled(Properties.Resources.filter_index_close_hover,
                                             closeButtonRect.Location);
            else
                e.Graphics.DrawImageUnscaled(Properties.Resources.filter_index_close,
                                             closeButtonRect.Location);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // frmIndex
            // 
            this.ClientSize = new System.Drawing.Size(292, 273);
            this.Name = "frmIndex";
            this.ResumeLayout(false);

        }
        
    }
}
