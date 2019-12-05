/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxControls
{
    internal class FontInfo
    {
        public static EmptyDelegate FontMetricsChanged;

        private static string fontName;
        private static float fontSize;

        private static FontInfo defaultFontInfo;
        private static FontInfo quoteFontInfo;
        private static FontInfo singleLineCommentFontInfo;

        public static int Height { get; private set; }
        public static int Width { get; private set; }
        public static int WidthTimes100 { get; private set; }

        private static readonly Rectangle measureDescentRenderRect = new Rectangle(2, 2, Int32.MaxValue, Int32.MaxValue);

        private FontStyle fontStyle;
        private Font font;

        public Font Font
        {
            get { return font; }
            private set
            {
                font = value;
            }
        }
        public static void SetFont(Font Font)
        {
            if (Font.Size != fontSize || Font.Name != fontName)
            {
                fontSize = Font.Size;
                fontName = Font.Name;
                updateFonts();
                if (FontMetricsChanged != null)
                    FontMetricsChanged.Invoke();
            }
        }
        public Color Color { get; private set; }
        
        private static List<FontInfo> allFontInfo;

        public FontInfo(FontStyle FontStyle, Color Color)
        {
            this.fontStyle = FontStyle;
            this.Font = new Font(fontName, fontSize, fontStyle);
            this.Color = Color;

            allFontInfo.Add(this);
        }
        static FontInfo()
        {
            fontName = "Courier New";
            
            fontSize = 12f;

            allFontInfo = new List<FontInfo>();

            defaultFontInfo = new FontInfo(FontStyle.Regular, Color.Black);
            allFontInfo.Add(defaultFontInfo);

            quoteFontInfo = new FontInfo(FontStyle.Regular, Color.Purple);
            allFontInfo.Add(quoteFontInfo);

            singleLineCommentFontInfo = new FontInfo(FontStyle.Regular, Color.Green);
            allFontInfo.Add(singleLineCommentFontInfo);

            updateFonts();
        }
        public static FontInfo Default
        {
            get { return defaultFontInfo; }
        }
        public static FontInfo Quote
        {
            get { return quoteFontInfo; }
        }
        public static FontInfo SingleLineComment
        {
            get { return singleLineCommentFontInfo; }
        }
        public FontStyle FontStyle
        {
            get { return this.fontStyle; }
            set
            {
                if (this.fontStyle != value)
                {
                    this.fontStyle = value;
                    this.Font = new Font(fontName, fontSize, this.fontStyle);
                }
            }
        }
        public static string FontName
        {
            get { return fontName; }
            set
            {
                if (fontName != value)
                {
                    fontName = value;
                    updateFonts();
                    FontMetricsChanged.Invoke();
                }
            }
        }
        public static float FontSize
        {
            get { return fontSize; }
            set
            {
                if (fontSize != value)
                {
                    fontSize = value;
                    updateFonts();
                    FontMetricsChanged.Invoke();
                }
            }
        }
        public bool Matches(FontInfo other)
        {
            return other.Color == this.Color && other.fontStyle == this.fontStyle;
        }
        private static void updateFonts()
        {
            foreach (FontInfo fi in allFontInfo)
            {
                fi.Font = new Font(fontName, fontSize, fi.fontStyle);
            }

            FontInfo.Height = defaultFontInfo.Font.Height;

            string s = new string('x', 110);
            string s2 = new string('x', 10);

            FontInfo.WidthTimes100 = TextRenderer.MeasureText(s, defaultFontInfo.font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.NoClipping | TextFormatFlags.NoPrefix).Width -
                TextRenderer.MeasureText(s2, defaultFontInfo.font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.NoClipping | TextFormatFlags.NoPrefix).Width;

            FontInfo.Width = WidthTimes100 / 100;
        }
    }
}
