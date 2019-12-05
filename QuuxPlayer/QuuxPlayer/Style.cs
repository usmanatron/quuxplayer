/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;

namespace QuuxPlayer
{
    internal static class Style
    {
        // Linear Gradient Brushes go from top to bottom
        public static LinearGradientBrush GetSelectedRowBrush(int Height, int Baseline)
        {
            return new LinearGradientBrush(new Point(0, Baseline), new Point(0, Height + Baseline), Styles.Medium, Styles.MediumDark);
        }
        public static LinearGradientBrush GetSelectedHoverRowBrush(int Height, int Baseline)
        {
            return new LinearGradientBrush(new Point(0, Baseline), new Point(0, Height + Baseline), Styles.MediumLight, Styles.Medium);
        }
        public static LinearGradientBrush GetHoverRowBrush(int Height, int Baseline)
        {
            return new LinearGradientBrush(new Point(0, Baseline), new Point(0, Height + Baseline), Styles.MediumDark, Styles.Dark);
        }
        public static LinearGradientBrush GetHeaderRowBrush(int Height, int Baseline)
        {
            return new LinearGradientBrush(new Point(0, Baseline), new Point(0, Height), Styles.TitleRow2, Styles.TitleRow1);
        }
        public static LinearGradientBrush GetProgressBackground(int Height)
        {
            return new LinearGradientBrush(Point.Empty, new Point(0, Height), Styles.Dark, Styles.MediumDark);
        }
        public static LinearGradientBrush GetProgressHoverBackground(int Height)
        {
            return new LinearGradientBrush(Point.Empty, new Point(0, Height), Styles.MediumDark, Styles.Medium);
        }
        public static LinearGradientBrush GetProgressProgress(int Height)
        {
            return new LinearGradientBrush(Point.Empty, new Point(0, Height), Styles.Medium, Styles.Light);
        }
        public static LinearGradientBrush GetProgressHoverProgress(int Height)
        {
            return new LinearGradientBrush(Point.Empty, new Point(0, Height), Styles.MediumLight, Styles.VeryLight);
        }
        public static LinearGradientBrush GetTrackDisplayBackgroundBrush(int Height)
        {
            return new LinearGradientBrush(Point.Empty, new Point(0, Height), Styles.MediumDark, Styles.Dark);
        }
        public static LinearGradientBrush GetDialogBackgroundBrush(int Height)
        {
            return new LinearGradientBrush(Point.Empty, new Point(0, Height), Styles.MediumDark, Styles.Dark);
        }

        public static LinearGradientBrush GetScrollBarHandleBrush(int Width, int Baseline)
        {
            return new LinearGradientBrush(new Point(Baseline, 0), new Point(Width + Baseline, 0), Styles.MediumDark, Styles.Dark);
        }
    }
}
