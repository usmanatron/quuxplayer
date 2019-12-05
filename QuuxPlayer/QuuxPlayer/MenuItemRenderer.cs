/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal sealed class MenuItemRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.Graphics.FillRectangle(Styles.DarkBrush, e.AffectedBounds);
        }
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected)
            {
                Rectangle r;

                if (e.Item.IsOnDropDown)
                    r = new Rectangle(2, 0, e.Item.Width - 4, e.Item.Height - 1);
                else
                    r = new Rectangle(0, 0, e.Item.Width, e.Item.Height - 1);

                e.Graphics.FillRectangle(Styles.MediumLightBrush, r);
                e.Graphics.DrawRectangle(Styles.DarkBorderPen, r);
            }
        }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextFont = Styles.FontSmaller;
            if (e.Item.Enabled)
            {
                e.TextColor = Styles.VeryLight;
                base.OnRenderItemText(e);
            }
            else
            {
                TextRenderer.DrawText(e.Graphics, e.Text, Styles.FontSmaller, e.TextRectangle, Styles.Medium, e.TextFormat);
            }
        }
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            e.Graphics.DrawRectangle(Styles.DarkBorderPen, e.AffectedBounds);
        }
        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            e.Graphics.FillRectangle(Styles.DeepBrush, e.AffectedBounds);
        }
        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            e.Graphics.DrawLine(Styles.MenuSeparatorPen,
                                e.ToolStrip.DisplayRectangle.X - 3,
                                e.Item.ContentRectangle.Y,
                                e.Item.ContentRectangle.Right,
                                e.Item.ContentRectangle.Y);
        }
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            base.OnRenderItemCheck(e);
        }
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = Styles.VeryLight;
            base.OnRenderArrow(e);
        }
    }
}


