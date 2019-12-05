/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal static class Styles
    {
        public static Font Font;
        public static Font FontHTPC;

        public static Font FontBold;
        public static Font FontButtonHTPC;
        public static Font FontControlPanelHTPC;
        public static Font FontBoldHTPC;
        public static Font FontItalicHTPC;

        public static Font FontItalic;
        public static Font FontUnderline;
        public static Font FontItalicUnderline;
        public static Font FontLarge;
        public static Font FontSmall;
        public static Font FontProgress;

        public static Font FontSmaller;
        public static Font FontSemiMono;
        public static Font FontMono;

        public static Font FontHeading;
        public static Font FontSubHeading;
        public static Font FontSubSubHeading;

        public static string FontNameAlt;

        public static string FontName;

        private static float fontSize;
        private static float fontSizeButtonHTPC = 14f;
        private static float fontSizeHTPC = 18f;
        private static float fontControlPanelHTPCSize = 14f;
        
        private const int R = 0x30;
        private const int G = 0x30;
        private const int B = 0x80;

        private static Color getColorTransform(Color c)
        {
            return Color.FromArgb(c.R * 4 / 5, c.G * 4 / 5, Math.Min(c.B * 5 / 4, 0xFF));
        }

        public static Color VeryDark = Color.FromArgb(R / 3, G / 3, B / 3);
        public static Color Dark = Color.FromArgb(R / 2, G / 2, B / 2);
        public static Color MediumDark = Color.FromArgb(R, G, B);
        public static Color Medium = Color.FromArgb(Math.Min(0xFF, R * 3 / 2), Math.Min(0xFF, G * 3 / 2), Math.Min(0xFF, B * 3 / 2));
        public static Color MediumLight = Color.FromArgb(Math.Min(0xFF, R * 15 / 8), Math.Min(0xFF, G * 15 / 8), Math.Min(0xFF, B * 15 / 8));
        public static Color Light = Color.FromArgb(Math.Min(0xFF, R * 2), Math.Min(0xFF, G * 2), Math.Min(0xFF, B * 2));
        public static Color VeryLight = Color.FromArgb(Math.Min(0xFF, R * 3), Math.Min(0xFF, G * 3), Math.Min(0xFF, B * 3));

        //public static Color TrackDisplayBackground = Color.FromArgb(R * 2 / 3, G * 2 / 3, B * 2 / 3);
        public static Color NonExistentTrack = Color.FromArgb(0x48, 0x48, 0x48);
        public static Color LightText = Color.LightGray;
        public static Color Playing = Color.Goldenrod;
        public static Color Highlight = Color.FromArgb(0x97, 0xF8, 0xC8);
        public static Color DisabledText = Color.Gray;
        public static Color WarningText = Color.Red;
        public static Color GamePadHelpColor = Color.FromArgb(0xFF, 0x50, 0x50);
        
        public static Color TitleRow1 = Color.FromArgb(0x20, 0x20, 0x20);
        public static Color TitleRow2 = Color.FromArgb(0x40, 0x40, 0x40);

        public static Color DarkBorder = Color.FromArgb(0x20, 0x20, 0x20);
        public static Color ActiveBackground = Color.FromArgb(R / 0x0E, G / 0x0E, B / 0x0E);

        public static Color DisabledButtonText = Color.FromArgb(0x70, 0x70, 0x70);
        public static Color Watermark = Color.DarkGray;
        public static Color HighlightEditControl = Color.FromArgb(0xE0, 0xE0, 0x90);

        // List / filter view
        public static Color ColumnHeader = Light;
        public static Color ColumnHeaderHover = VeryLight;
        public static Color ToolTipText = SystemColors.InfoText;

        public static SolidBrush DarkBrush = new SolidBrush(Dark);
        public static SolidBrush DeepBrush = new SolidBrush(MediumDark);
        public static SolidBrush MediumBrush = new SolidBrush(Medium);
        public static SolidBrush MediumLightBrush = new SolidBrush(MediumLight);
        public static SolidBrush LightBrush = new SolidBrush(Light);
        public static SolidBrush ToolTipBrush = new SolidBrush(SystemColors.Info);

        // Progress bar
        public static SolidBrush ProgressProgress = new SolidBrush(Light);
        public static SolidBrush ProgressProgressHovering = new SolidBrush(VeryLight);
        public static Color ProgressBackground = MediumDark;
        public static SolidBrush ProgressBackgroundBrush = new SolidBrush(ProgressBackground);
        public static Color ProgressBackgroundHover = Medium;

        // Scrollbars
        public static Color ScrollBarBackground = Medium;
        public static Color ScrollBarBackgroundHover = Light;
        public static Color ScrollBarBackgroundDisabled = MediumDark;
        public static SolidBrush ScrollBarButtons = new SolidBrush(Dark);
        public static SolidBrush ScrollBarButtonsDisabled = new SolidBrush(VeryDark);
        public static Pen ScrollBarArrowPen = new Pen(ScrollBarBackground);
        public static Pen ScrollBarArrowPenHover = new Pen(ScrollBarBackgroundHover);
        public static Pen ScrollBarArrowPenDisabled = new Pen(ScrollBarBackgroundDisabled);

        public static SolidBrush ComboBoxButtonBackgroundBrush = ScrollBarButtons;
        public static Pen ComboBoxButtonBorderPen = new Pen(ScrollBarBackground);

        public static Color SpectrumLeft = Color.FromArgb(0xD5, 0x1B, 0x00);
        public static Color SpectrumRight = Color.FromArgb(0x20, 0xA0, 0x60);
        public static Pen SpectrumGreenPen = Pens.DarkGreen;
        public static Pen SpectrumYellowPen = Pens.Olive;
        public static Pen SpectrumRedPen = Pens.DarkRed;
        public static Pen SpectrumPeakPen = Pens.Gray;
        public static SolidBrush SpectrumLeftBrush = new SolidBrush(SpectrumLeft);
        public static SolidBrush SpectrumRightBrush = new SolidBrush(SpectrumRight);

        public static Pen DarkBorderPen = new Pen(DarkBorder);
        public static Pen ThickPen = new Pen(DarkBorder, 3);
        public static Pen TrackDragLinePen = new Pen(LightText);
        public static Pen FilterButtonSelectedPen = new Pen(MediumDark);
        public static Pen FilterButtonHasValuePen = new Pen(Color.SlateGray);
        public static Pen ControlFocusedPen = new Pen(MediumDark);
        public static Pen MenuSeparatorPen = new Pen(Medium);

        public static Pen SortArrowPen = new Pen(Light);

        public static Pen SpinControlPen = new Pen(LightText);
        public static Pen SpinControlPenHover = new Pen(VeryLight);
        public static Pen SpinControlPenDisabled = new Pen(Medium);

        public static Pen ListBoxBorderPen = new Pen(VeryDark);

        private static List<Bitmap> images = new List<Bitmap>();

        public static Bitmap cpl_background = null;
        public static Bitmap cpl_center = null;
        public static Bitmap cpl_play_highlighted = null;
        public static Bitmap cpl_pause = null;
        public static Bitmap cpl_pause_highlighted = null;
        public static Bitmap cpl_back_disabled = null;
        public static Bitmap cpl_back_highlighted = null;
        public static Bitmap cpl_fwd_disabled = null;
        public static Bitmap cpl_fwd_highlighted = null;
        public static Bitmap cpl_stop_disabled = null;
        public static Bitmap cpl_stop_highlighted = null;
        public static Bitmap cpl_volume_slider = null;
        public static Bitmap cpl_volume_ball = null;
        public static Bitmap cpl_volume_ball_highlighted = null;
        public static Bitmap cpl_shuffle_highlighted = null;
        public static Bitmap cpl_shuffle_disabled = null;
        public static Bitmap cpl_advance_highlighted = null;
        public static Bitmap cpl_repeat_highlighted = null;
        public static Bitmap cpl_repeat_on = null;
        public static Bitmap cpl_repeat_on_highlighted = null;
        public static Bitmap cpl_repeat_disabled = null;
        public static Bitmap cpl_now_playing_on = null;
        public static Bitmap cpl_now_playing_on_highlighted = null;
        public static Bitmap cpl_now_playing_highlighted = null;
        public static Bitmap cpl_now_playing_disabled = null;
        public static Bitmap BitmapControlPanelMuteHighlighted = null;
        public static Bitmap BitmapControlPanelMute = null;
        public static Bitmap BitmapControlPanelNoMuteHighlighted = null;
        public static Bitmap BitmapFilterOutlinebackground = null;
        public static Bitmap button_outline_right = null;
        public static Bitmap filter_outline_left = null;
        public static Bitmap filter_outline_right = null;
        public static Bitmap filter_button_background = null;
        public static Bitmap BitmapFilterBarBackground = null;
        public static Bitmap BitmapFilterBarX = null;
        public static Bitmap BitmapFilterBarXHighlighted = null;
        public static Bitmap BitmapStars = null;
        //public static Bitmap BitmapNag = null;
        public static Bitmap BitmapGamePad = null;
        public static Bitmap BitmapMiniPlayer = null;
        public static Bitmap BitmapMiniPlayerAdvanceHover = null;
        public static Bitmap BitmapMiniPlayerBackHighlighted = null;
        public static Bitmap BitmapMiniPlayerBackDisabled = null;
        public static Bitmap BitmapMiniPlayerPlayHighlighted = null;
        public static Bitmap BitmapMiniPlayerPauseHighlighted = null;
        public static Bitmap BitmapMiniPlayerPause = null;
        public static Bitmap BitmapMiniPlayerFwdHighlighted = null;
        public static Bitmap BitmapMiniPlayerFwdDisabled = null;
        public static Bitmap BitmapMiniPlayerMuteOn = null;
        public static Bitmap BitmapMiniPlayerMuteOffHighlighted = null;
        public static Bitmap BitmapMiniPlayerMuteOnHighlighted = null;
        public static Bitmap BitmapMiniPlayerVolumeUpHighlighted = null;
        public static Bitmap BitmapMiniPlayerVolumeDownHighlighted = null;
        public static Bitmap BitmapMiniPlayerExit = null;
        public static Bitmap BitmapMiniPlayerExitHighlighted = null;
        public static Bitmap BitmapScrollBarUp = null;
        public static Bitmap BitmapScrollBarDown = null;
        public static Bitmap BitmapScrollBarUpHighlighted = null;
        public static Bitmap BitmapScrollBarDownHighlighted = null;
        public static Bitmap BitmapScrollBarA = null;
        public static Bitmap BitmapScrollBarAHighlighted = null;
        public static Bitmap BitmapScrollBarZ = null;
        public static Bitmap BitmapScrollBarZHighlighted = null;
        public static Bitmap BitmapRadioOn = null;
        public static Bitmap BitmapRadioHighlighted = null;
        public static Bitmap BitmapRadioOnHighlighted = null;
        
        public static Bitmap BitmapTaskDialogArrow = null;

        public static Bitmap BitmapRSS = null;

        public static readonly int TextHeight;
        public static readonly int TextHeightHTPC;

        private static System.Drawing.FontFamily[] fff = new System.Drawing.Text.InstalledFontCollection().Families;

        private static Font getFont(string FontName, string FontNameAlt, float Size, float SizeAlt, FontStyle Style)
        {
            foreach (FontFamily ff in fff)
                if (ff.Name == FontName)
                    return new Font(FontName, Size, Style);

            return new Font(FontNameAlt, SizeAlt, Style);
        }
        static Styles()
        {
            fontSize = 10f;

            Font = getFont("Corbel", "Arial", fontSize, fontSize - 1.0f, FontStyle.Regular);

            fontSize = Font.Size;
            FontName = Font.Name;

            Font alt = getFont("Candara", "Times New Roman", 10, 10, FontStyle.Regular);
            FontNameAlt = alt.Name;

            FontMono = getFont("Consolas", "Courier New", fontSize, fontSize, FontStyle.Regular);

            FontSemiMono = getFont("Consolas", "Arial", fontSize, fontSize, FontStyle.Regular);

            FontSmaller = getFont("Corbel", "Arial", 9.0f, 8.0f, FontStyle.Regular);

           
            FontHTPC = new Font(FontName, fontSizeHTPC, FontStyle.Regular);
            FontBold = new Font(FontName, fontSize, FontStyle.Bold);
            FontControlPanelHTPC = new Font(FontName, fontControlPanelHTPCSize, FontStyle.Bold);
            FontBoldHTPC = new Font(FontName, fontSizeHTPC, FontStyle.Bold);
            FontButtonHTPC = new Font(FontName, fontSizeButtonHTPC, FontStyle.Bold);
            FontItalicHTPC = new Font(FontName, fontSizeHTPC, FontStyle.Italic);

            FontItalic = new Font(FontName, fontSize, FontStyle.Italic);
            FontUnderline = new Font(FontName, fontSize, FontStyle.Underline);
            FontItalicUnderline = new Font(FontName, fontSize, FontStyle.Underline | FontStyle.Italic);
            FontLarge = new Font(FontName, 15f, FontStyle.Bold);
            FontSmall = new Font(FontName, 7f, FontStyle.Regular);
            FontProgress = new Font(FontSemiMono.Name, 15f, FontStyle.Bold);
            
            FontHeading = new Font(FontName, 28f, FontStyle.Bold);
            FontSubHeading = new Font(FontName, 20f, FontStyle.Bold);
            FontSubSubHeading = new Font(FontName, 15f, FontStyle.Bold | FontStyle.Italic);

            TextHeight = TextRenderer.MeasureText("X", Font).Height;
            TextHeightHTPC = TextRenderer.MeasureText("X", FontHTPC).Height;

            cpl_background = convertBitmap(Properties.Resources.cpl_background);
            cpl_center = convertBitmap(Properties.Resources.cpl_center);
            cpl_play_highlighted = convertBitmap(Properties.Resources.cpl_play_highlighted);
            cpl_pause = convertBitmap(Properties.Resources.cpl_pause);
            cpl_pause_highlighted = convertBitmap(Properties.Resources.cpl_pause_highlighted);
            cpl_back_disabled = convertBitmap(Properties.Resources.cpl_back_disabled);
            cpl_back_highlighted = convertBitmap(Properties.Resources.cpl_back_highlighted);
            cpl_fwd_disabled = convertBitmap(Properties.Resources.cpl_fwd_disabled);
            cpl_fwd_highlighted = convertBitmap(Properties.Resources.cpl_fwd_highlighted);
            cpl_stop_disabled = convertBitmap(Properties.Resources.cpl_stop_disabled);
            cpl_stop_highlighted = convertBitmap(Properties.Resources.cpl_stop_highlighted);
            cpl_volume_slider = convertBitmap(Properties.Resources.cpl_volume_slider);
            cpl_volume_ball = convertBitmap(Properties.Resources.cpl_volume_ball);
            cpl_volume_ball_highlighted = convertBitmap(Properties.Resources.cpl_volume_ball_highlighted);
            cpl_shuffle_highlighted = convertBitmap(Properties.Resources.cpl_shuffle_highlighted);
            cpl_shuffle_disabled = convertBitmap(Properties.Resources.cpl_shuffle_disabled);
            cpl_advance_highlighted = convertBitmap(Properties.Resources.cpl_advance_highlighted);
            cpl_repeat_highlighted = convertBitmap(Properties.Resources.cpl_repeat_highlighted);
            cpl_repeat_on = convertBitmap(Properties.Resources.cpl_repeat_on);
            cpl_repeat_on_highlighted = convertBitmap(Properties.Resources.cpl_repeat_on_highlighted);
            cpl_repeat_disabled = convertBitmap(Properties.Resources.cpl_repeat_disabled);
            cpl_now_playing_highlighted = convertBitmap(Properties.Resources.cpl_now_playing_highlighted);
            cpl_now_playing_disabled = convertBitmap(Properties.Resources.cpl_now_playing_disabled);
            cpl_now_playing_on = convertBitmap(Properties.Resources.cpl_now_playing_on);
            cpl_now_playing_on_highlighted = convertBitmap(Properties.Resources.cpl_now_playing_on_highlighted);

            BitmapControlPanelMuteHighlighted = convertBitmap(Properties.Resources.cpl_mute_highlighted);
            BitmapControlPanelNoMuteHighlighted = convertBitmap(Properties.Resources.cpl_nomute_highlighted);
            BitmapControlPanelMute = convertBitmap(Properties.Resources.cpl_mute);
            BitmapFilterOutlinebackground = convertBitmap(Properties.Resources.filter_outline_background);
            button_outline_right = convertBitmap(Properties.Resources.button_outline_right);
            filter_outline_left = convertBitmap(Properties.Resources.filter_outline_left);
            filter_outline_right = convertBitmap(Properties.Resources.filter_outline_right);
            filter_button_background = convertBitmap(Properties.Resources.filter_button_background);
            BitmapFilterBarBackground = convertBitmap(Properties.Resources.filter_bar_background);
            BitmapFilterBarX = convertBitmap(Properties.Resources.filter_bar_x);
            BitmapFilterBarXHighlighted = convertBitmap(Properties.Resources.filter_bar_x_highlighted);
            BitmapStars = convertBitmap(Properties.Resources.stars);
            //BitmapNag = convertBitmap(Properties.Resources.nag);
            BitmapGamePad = convertBitmap(Properties.Resources.gamepad);
            
            BitmapMiniPlayer = convertBitmap(Properties.Resources.mini_player);

            BitmapMiniPlayerAdvanceHover = convertBitmap(Properties.Resources.mini_player_advance_hover);
            BitmapMiniPlayerBackHighlighted = convertBitmap(Properties.Resources.mini_player_back_highlighted);
            BitmapMiniPlayerPlayHighlighted = convertBitmap(Properties.Resources.mini_player_play_highlighted);
            BitmapMiniPlayerPauseHighlighted = convertBitmap(Properties.Resources.mini_player_pause_highlighted);
            BitmapMiniPlayerPause = convertBitmap(Properties.Resources.mini_player_pause);
            BitmapMiniPlayerFwdHighlighted = convertBitmap(Properties.Resources.mini_player_fwd_highlighted);
            BitmapMiniPlayerMuteOn = convertBitmap(Properties.Resources.mini_player_mute_on);
            BitmapMiniPlayerMuteOnHighlighted = convertBitmap(Properties.Resources.mini_player_mute_on_highlighted);
            BitmapMiniPlayerMuteOffHighlighted = convertBitmap(Properties.Resources.mini_player_mute_off_highlighted);
            BitmapMiniPlayerFwdDisabled = convertBitmap(Properties.Resources.mini_player_fwd_disabled);
            BitmapMiniPlayerBackDisabled = convertBitmap(Properties.Resources.mini_player_back_disabled);
            BitmapMiniPlayerVolumeUpHighlighted = convertBitmap(Properties.Resources.mini_player_volume_up_highlighted);
            BitmapMiniPlayerVolumeDownHighlighted = convertBitmap(Properties.Resources.mini_player_volume_down_highlighted);
            BitmapMiniPlayerExit = convertBitmap(Properties.Resources.mini_player_exit);
            BitmapMiniPlayerExitHighlighted = convertBitmap(Properties.Resources.mini_player_exit_highlighted);

            BitmapScrollBarUp = convertBitmap(Properties.Resources.scroll_bar_up);
            BitmapScrollBarDown = convertBitmap(Properties.Resources.scroll_bar_down);
            BitmapScrollBarUpHighlighted = convertBitmap(Properties.Resources.scroll_bar_up_highlighted);
            BitmapScrollBarDownHighlighted = convertBitmap(Properties.Resources.scroll_bar_down_highlighted);

            BitmapScrollBarA = convertBitmap(Properties.Resources.scroll_bar_skip_a);
            BitmapScrollBarAHighlighted = convertBitmap(Properties.Resources.scroll_bar_skip_a_highlighted);
            BitmapScrollBarZ = convertBitmap(Properties.Resources.scroll_bar_skip_z);
            BitmapScrollBarZHighlighted = convertBitmap(Properties.Resources.scroll_bar_skip_z_highlighted);

            BitmapRadioOn = convertBitmap(Properties.Resources.radio_on);
            BitmapRadioHighlighted = convertBitmap(Properties.Resources.radio_highlighted);
            BitmapRadioOnHighlighted = convertBitmap(Properties.Resources.radio_on_highlighted);
            
            BitmapTaskDialogArrow = convertBitmap(Properties.Resources.TaskDialogArrow);

            BitmapRSS = convertBitmap(Properties.Resources.rss);
        }

        //public static void AlterPicture(string s, float Brightness)
        //{
        //    Bitmap b = new Bitmap(s);

        //    for (int i = 0; i < b.Width; i++)
        //        for (int j = 0; j < b.Height; j++)
        //        {
        //            b.SetPixel(i, j, getColorTransform(b.GetPixel(i, j)));
        //            //Color c = b.GetPixel(i, j);
        //            //int avg = (c.R + c.G + c.B) / 3;
        //            //b.SetPixel(i, j, Color.FromArgb(Math.Min(0xFF, R * avg / avgColor), Math.Min(0xFF, G * avg / avgColor), Math.Min(0xFF, B * avg / avgColor)));
        //            //b.SetPixel(i, j, Color.FromArgb(c.R, c.G, Math.Min(0xFF, c.B * 4 / 3)));
        //        }

        //    s = s + "___.bmp";
        //    b.Save(s, System.Drawing.Imaging.ImageFormat.Bmp);
        //}
        public static void DrawArrow(Graphics g, Pen P, bool Up, int X, int Y)
        {
            if (Up)
            {
                for (int i = 0; i < 4; i++)
                    g.DrawLine(P, X - i, Y + i, X + i + 1, Y + i);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                    g.DrawLine(P, X - i, Y + 4 - i, X + i + 1, Y + 4 - i);
            }
        }
        private static Bitmap convertBitmap(Bitmap b)
        {
            //int avgColor = (R + G + B) / 3;
            //for (int i = 0; i < b.Width; i++)
            //    for (int j = 0; j < b.Height; j++)
            //    {
            //        Color c = b.GetPixel(i, j);
            //        if (c.R + c.G + c.B > 300)
            //        {
            //            b.SetPixel(i, j, Color.FromArgb(255 - (255 - c.R) * 2 / 4,
            //                       c.G * 3 / 6,
            //                       c.B * 3 / 6));
            //        }
            //        //b.SetPixel(i, j, getColorTransform(b.GetPixel(i, j)));
            //        //Color c = b.GetPixel(i, j);
            //        //int avg = (c.R + c.G + c.B) / 3;
            //        //b.SetPixel(i, j, Color.FromArgb(Math.Min(0xFF, R * avg / avgColor), Math.Min(0xFF, G * avg / avgColor), Math.Min(0xFF, B * avg / avgColor)));
            //        //b.SetPixel(i, j, Color.FromArgb(c.R, c.G, Math.Min(0xFF, c.B * 4 / 3)));
            //    }
            //b.SetResolution(frmMain.DPI, frmMain.DPI);
            images.Add(b);
            return b;
        }
        public static void SetDPI(float dpi)
        {
            foreach (Bitmap b in images)
            {
                b.SetResolution(dpi, dpi);
            }
        }
    }
}
