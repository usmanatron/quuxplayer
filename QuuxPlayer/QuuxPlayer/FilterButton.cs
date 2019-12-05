/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal enum FilterType { None, Genre, Artist, Album, Year, Grouping, Text, Playlist }
    internal enum FilterValueType { None, SpecificValue, StartChar }

    internal sealed class FilterButton : Control
    {
        public delegate void FilterSelected(FilterButton FilterButton);
        public delegate void FilterChanged(FilterButton FilterButton, bool WasOff);
        public delegate void FilterIndexSelected(FilterButton FilterButton);

        public event FilterChanged FilterValueChanged;
        public event FilterSelected SelectedEvent;
        public event FilterIndexSelected IndexSelected;
        public event FilterSelected ClickedWithoutSelected;

        private FilterValueType valueType;

        private bool isSelected = false;  // Is this the filter that is displayed
        private bool mouseHovering = false;
        private bool dropDownHovering = false;
        private string filterValue = String.Empty;
        private string filterName = String.Empty;
        private string filterNamePlural = String.Empty;
        private FilterType filterType = FilterType.None;
        private Rectangle textRectangle;
        private const TextFormatFlags tff = TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
        private Font font;
        private QToolTip toolTip;
        private char startWithChar = '\0';

        public FilterButton(FilterType Type)
        {
            this.filterType = Type;

            this.Height = 25;
            this.Top = 3;
            this.DoubleBuffered = true;
            this.font = Styles.FontBold;
            this.valueType = FilterValueType.None;

            toolTip = new QToolTip(this, "foo");

            updateToolTip();
        
            this.Invalidate();
        }
        public bool Locked { get; set; }
        public override Font Font
        {
            set
            {
                base.Font = value;
                font = value;
                this.Invalidate();
            }
        }
        public bool Selected
        {
            get { return isSelected; }
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    if (isSelected)
                        SelectedEvent.Invoke(this);
                    this.Invalidate();
                }
            }
        }
        public FilterType FilterType
        {
            get { return filterType; }
        }
        public string FilterName
        {
            get { return filterName; }
            set
            {
                filterName = value;
                filterNamePlural = filterName + "s";
            }
        }
        public string FilterValue
        {
            get
            {
                return filterValue;
            }
            set
            {
                if (value.Length == 0)
                {
                    this.ReleaseFilter();
                }
                else
                {
                    if (this.ValueType != FilterValueType.SpecificValue || filterValue != value)
                    {
                        bool wasOff = this.ValueType != FilterValueType.SpecificValue;

                        filterValue = value;
                        this.ValueType = FilterValueType.SpecificValue;
                        
                        FilterValueChanged.Invoke(this, wasOff);

                        updateToolTip();

                        this.Invalidate();
                    }
                }
            }
        }
        public FilterValueType ValueType
        {
            get { return valueType; }
            set
            {
                if (valueType != value)
                {
                    valueType = value;
                    updateToolTip();
                }
            }
        }
        public char StartChar
        {
            get { return startWithChar; }
            set
            {
                startWithChar = value;

                updateToolTip();
                this.Invalidate();
            }
        }

        private void updateToolTip()
        {
            if (this.DropDownHovering)
            {
                toolTip.SetToolTip(this, Localization.Get(UI_Key.ToolTip_Filter_Button_Index));
                toolTip.Active = true;
            }
            else if (this.MouseHovering && this.ValueType != FilterValueType.None)
            {
                toolTip.SetToolTip(this, Localization.Get(UI_Key.ToolTip_Filter_Button_Release));
                toolTip.Active = true;
            }
            else
            {
                toolTip.Active = false;
            }
        }

        public new void Select()
        {
            this.Selected = true;
        }
        public void ReleaseFilter()
        {
            if (this.ValueType != FilterValueType.None)
            {
                filterValue = String.Empty;
                this.ValueType = FilterValueType.None;

                FilterValueChanged.Invoke(this, false);

                updateToolTip();

                this.Invalidate();
            }
        }
        public override string ToString()
        {
            switch (this.valueType)
            {
                case FilterValueType.SpecificValue:
                    return ((this.FilterValue == String.Empty) ? Localization.Get(UI_Key.Filter_No, filterName) : filterValue);
                case FilterValueType.None:
                    return Localization.Get(UI_Key.Filter_Any, filterName);
                case FilterValueType.StartChar:
                    if (this.filterType == FilterType.Year)
                        return filterNamePlural + ": " + startWithChar.ToString() + "0s";
                    else
                        return filterNamePlural + ": " + startWithChar;
                default:
                    throw new Exception();
            }
        }
        public void SetWidth()
        {
            this.Width = 
            Math.Min(200,
                            TextRenderer.MeasureText(this.ToString(),
                                                     font,
                                                     Size.Empty,
                                                     TextFormatFlags.NoPrefix).Width) + 20;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!Locked)
            {
                if (e.Button == MouseButtons.Right)
                {
                    this.ReleaseFilter();
                }
                else
                {
                    if (e.X > this.ClientRectangle.Width - 15)
                        IndexSelected.Invoke(this);
                    else if (this.Selected)
                        ClickedWithoutSelected.Invoke(this);
                    else
                        this.Selected = true;
                }
            }
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if (!Locked)
            {
                MouseHovering = true;
                DropDownHovering = false;
                updateToolTip();
                this.Invalidate();
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            DropDownHovering = e.X > this.ClientRectangle.Width - 15;
        }
        private bool MouseHovering
        {
            get { return mouseHovering; }
            set
            {
                if (mouseHovering != value)
                {
                    mouseHovering = value;
                    if (!mouseHovering)
                        dropDownHovering = false;
                    updateToolTip();
                    this.Invalidate();
                }
            }
        }
        private bool DropDownHovering
        {
            get { return dropDownHovering; }
            set
            {
                if (dropDownHovering != value)
                {
                    dropDownHovering = value;
                    updateToolTip();
                }
            }
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            MouseHovering = false;
            updateToolTip();
            this.Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (MouseHovering)
            {
                e.Graphics.DrawImageUnscaled(Styles.BitmapFilterOutlinebackground, 3, 0);
                e.Graphics.DrawImageUnscaled(Styles.filter_outline_left, Point.Empty);
                e.Graphics.DrawImageUnscaled(Styles.filter_outline_right, this.Width - 15, 0);
            }
            else
            {
                e.Graphics.DrawImageUnscaled(Styles.filter_button_background, Point.Empty);
            }

            Color c;

            if (this.Selected && this.ValueType != FilterValueType.None)
                c = Color.White;
            else if (this.ValueType != FilterValueType.None)
                c = Styles.Light;
            else if (this.Selected)
                c = Styles.VeryLight;
            else
                c = Styles.LightText;

            TextRenderer.DrawText(e.Graphics, this.ToString(), font, textRectangle, c, tff);

            if (this.ValueType != FilterValueType.None && !MouseHovering)
                e.Graphics.DrawRectangle(Styles.FilterButtonHasValuePen, textRectangle);
            else if (isSelected && !MouseHovering)
                e.Graphics.DrawRectangle(Styles.FilterButtonSelectedPen, textRectangle);
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            textRectangle = new Rectangle(new Point(1, 0), new Size(this.ClientRectangle.Width - 15, this.ClientRectangle.Height - 1));
        }
    }
}
