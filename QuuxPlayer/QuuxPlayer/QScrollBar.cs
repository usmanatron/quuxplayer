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
    internal sealed class QScrollBar : Control
    {
        private const int MIN_BUTTON_SIZE = 12;
        public const int MIN_WIDTH = MIN_BUTTON_SIZE + 2;

        public enum SBBrightness { Auto, Dim, Bright }
        private enum CursorPosition { None, OnTopButton, OnBottomButton, OnSkipUp, OnSkipDown, OnHandle, AboveHandle, BelowHandle };

        public delegate void ScrollDelegate(QScrollBar Sender, int Value);
        public delegate void ScrollSkipDelegate(bool Up);

        private static Bitmap upArrow = Styles.BitmapScrollBarUp;
        private static Bitmap downArrow = Styles.BitmapScrollBarDown;
        private static Bitmap upArrowHighlighted = Styles.BitmapScrollBarUpHighlighted;
        private static Bitmap downArrowHighlighted = Styles.BitmapScrollBarDownHighlighted;
        private static Bitmap upSkip = Styles.BitmapScrollBarA;
        private static Bitmap upSkipHighlighted = Styles.BitmapScrollBarAHighlighted;
        private static Bitmap downSkip = Styles.BitmapScrollBarZ;
        private static Bitmap downSkipHighlighted = Styles.BitmapScrollBarZHighlighted;

        public event ScrollDelegate UserScroll;
        public event ScrollSkipDelegate ScrollSkip;

        private int min = 0;
        private int max = 100;
        private int range = 100;
        private int val = 0;
        private float pixPerIncrement = 1;
        private int handleHeight = 10;
        private int buttonHeight = MIN_BUTTON_SIZE + 1;
        private int skipButtonHeight = MIN_BUTTON_SIZE + 1;
        private int buttonAreaHeight;
        private int scrollablePixels = 10;
        private int buttonWidth = MIN_BUTTON_SIZE;
        private bool hovering = false;
        private int dragStart = -1;
        private int dragOffset = 0;
        private bool enabled = true;
        private int largeChange = 0;
        private Timer repeatTimer;
        private bool skipButtons = false;
        private bool needsRectPaint = false;
        private Color backgroundHoverColor;
        private Color backgroundColor;
        private SolidBrush buttonBrush;
        private Brush handleBrush;
        private Pen arrowPen;
        private Pen arrowPenHover;
        private SBBrightness brightness;
        private Point upArrowLocation = Point.Empty;
        private Point downArrowLocation = Point.Empty;
        private Point upSkipLocation = Point.Empty;
        private Point downSkipLocation = Point.Empty;

        public QScrollBar(bool SkipButtons)
        {
            this.skipButtons = SkipButtons;

            this.DoubleBuffered = true;
            this.Cursor = Cursors.Default;
            brightness = SBBrightness.Auto;
            setColors();
        }

        public SBBrightness Brightness
        {
            get { return brightness; }
            set
            {
                if (this.brightness != value)
                {
                    brightness = value;
                    setColors();
                    this.Invalidate();
                }
            }
        }
        public int Min
        {
            get { return min; }
            set
            {
                if (min != value)
                {
                    int v = this.Value;

                    min = value;
                    Range = max - min;

                    this.Value = v;
                }
            }
        }
        public int Max
        {
            get { return max; }
            set
            {
                killRepeatTimer();
                if (value != max)
                {
                    max = value;
                    Range = max - min;
                    
                    if (Value > value)
                        Value = value;

                    this.Invalidate();
                }
            }
        }
        public int Value
        {
            get { return val + Min; }
            set
            {
                if (val != value - Min)
                {
                    val = Math.Max(0, Math.Min(range, value - Min));
                    this.Invalidate();
                }
            }
        }
        public new bool Enabled
        {
            get { return enabled; }
            set
            {
                if (enabled != value)
                {
                    enabled = value;
                    setColors();
                    this.Invalidate();
                }
            }
        }
        public int LargeChange
        {
            get { return largeChange; }
            set
            {
                value = Math.Max(0, value);
                if (largeChange != value)
                {
                    largeChange = value;
                    setMetrics();
                    this.Invalidate();
                }
            }
        }

        public override string ToString()
        {
            return "ScrollBar Value: " + this.Value.ToString();
        }
        private int oldWidth = -1;
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            setMetrics();
            if (this.Width != oldWidth)
            {
                oldWidth = this.Width;
                setHandleBrushGradientStyle();
            }
            this.Invalidate();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (dragStart >= 0 && this.Enabled && Range > 0)
            {
                int oldVal = valFromHandlePosition(e.Y - dragOffset);
                if (oldVal != val)
                {
                    val = oldVal;
                    this.Invalidate();
                    Lib.DoEvents();
                    UserScroll.Invoke(this, Value);
                }
            }
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (this.Enabled && Range > 0)
            {
                int handlePos = (int)handlePosition();
                
                switch (getCursorPosn(e.Y, handlePos))
                {
                    case CursorPosition.OnHandle:
                        dragStart = e.Y;
                        dragOffset = e.Y - (handlePos + handleHeight / 2);
                        break;
                    case CursorPosition.OnTopButton:
                        if (e.Button != MouseButtons.Right)
                            setupTimer(-1, 5, 250, 18);
                        break;
                    case CursorPosition.OnBottomButton:
                        if (e.Button != MouseButtons.Right)
                            setupTimer(1, 5, 250, 18);
                        break;
                    case CursorPosition.AboveHandle:
                        if (e.Button != MouseButtons.Right)
                            setupTimer(-LargeChange, 5, 250, 25);
                        break;
                    case CursorPosition.BelowHandle:
                        if (e.Button != MouseButtons.Right)
                            setupTimer(LargeChange, 5, 250, 25);
                        break;
                    case CursorPosition.OnSkipDown:
                        setupTimer(Int32.MaxValue, 5, 250, 75);
                        break;
                    case CursorPosition.OnSkipUp:
                        setupTimer(Int32.MinValue, 5, 250, 75);
                        break;
                }
            }
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            if (!hovering)
            {
                hovering = true;
                this.Invalidate();
            }
            base.OnMouseEnter(e);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            if (hovering)
            {
                hovering = false;
                this.Invalidate();
            }
            base.OnMouseLeave(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (this.Enabled && Range > 0)
            {
                int oldVal = val;
                if (dragStart >= 0)
                {
                    val = valFromHandlePosition(e.Y - dragOffset);
                    dragStart = -1;
                }
                else
                {
                    if (!killRepeatTimer())
                    {
                        switch (getCursorPosn(e.Y, (int)handlePosition()))
                        {
                            case CursorPosition.OnTopButton:
                                if (e.Button == MouseButtons.Right)
                                    val = 0;
                                else
                                    val = Math.Max(0, val - 1);
                                break;
                            case CursorPosition.OnBottomButton:
                                if (e.Button == MouseButtons.Right)
                                    val = Range;
                                else
                                    val = Math.Min(Range, val + 1);
                                break;
                            case CursorPosition.AboveHandle:
                                if (e.Button == MouseButtons.Right || LargeChange == 0)
                                    val = valFromHandlePosition(e.Y);
                                else
                                    Value -= LargeChange;
                                break;
                            case CursorPosition.BelowHandle:
                                if (e.Button == MouseButtons.Right || LargeChange == 0)
                                    val = valFromHandlePosition(e.Y);
                                else
                                    Value += LargeChange;
                                break;
                        }
                    }
                }
                if (oldVal != val)
                {
                    UserScroll.Invoke(this, Value);
                    this.Invalidate();
                }
            }
            else
            {
                dragStart = -1;
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.Clear(hovering ? backgroundHoverColor : backgroundColor);

            if (needsRectPaint)
            {
                e.Graphics.FillRectangle(buttonBrush, 1, upArrowLocation.Y, buttonWidth, buttonHeight - 1);
                e.Graphics.FillRectangle(buttonBrush, 1, downArrowLocation.Y, buttonWidth, buttonHeight - 1);
                if (SkipButtons)
                {
                    e.Graphics.FillRectangle(buttonBrush, 1, upSkipLocation.Y, buttonWidth, buttonHeight - 1);
                    e.Graphics.FillRectangle(buttonBrush, 1, downSkipLocation.Y, buttonWidth, buttonHeight - 1);
                }
            }
            e.Graphics.FillRectangle(handleBrush, 1, handlePosition(), buttonWidth, handleHeight);

            if (hovering)
            {
                e.Graphics.DrawImageUnscaled(upArrowHighlighted, upArrowLocation);
                e.Graphics.DrawImageUnscaled(downArrowHighlighted, downArrowLocation);
                if (SkipButtons)
                {
                    e.Graphics.DrawImageUnscaled(upSkipHighlighted, upSkipLocation);
                    e.Graphics.DrawImageUnscaled(downSkipHighlighted, downSkipLocation);
                }
            }
            else
            {
                e.Graphics.DrawImageUnscaled(upArrow, upArrowLocation);
                e.Graphics.DrawImageUnscaled(downArrow, downArrowLocation);
                if (SkipButtons)
                {
                    e.Graphics.DrawImageUnscaled(upSkip, upSkipLocation);
                    e.Graphics.DrawImageUnscaled(downSkip, downSkipLocation);
                }
            }
        }
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            this.Parent.Focus();
        }

        private int Range
        {
            get { return range; }
            set
            {
                range = value;
                setMetrics();
                setColors();
                this.Invalidate();
            }
        }
        private bool SkipButtons
        {
            get { return skipButtons && range > 2; }
            set
            {
                if (skipButtons != value)
                {
                    skipButtons = value;
                    setMetrics();
                }
            }
        }
        private CursorPosition getCursorPosn(int Y, int HandlePosn)
        {
            if (Y < buttonHeight)
                return CursorPosition.OnTopButton;
            else if (Y > this.ClientRectangle.Height - buttonHeight)
                return CursorPosition.OnBottomButton;
            else if (SkipButtons && Y < buttonAreaHeight)
                return CursorPosition.OnSkipUp;
            else if (SkipButtons && Y > this.ClientRectangle.Height - buttonAreaHeight)
                return CursorPosition.OnSkipDown;
            else if (Y >= HandlePosn && Y <= (HandlePosn + handleHeight))
                return CursorPosition.OnHandle;
            else if (Y < HandlePosn)
                return CursorPosition.AboveHandle;
            else if (Y > HandlePosn + handleHeight)
                return CursorPosition.BelowHandle;
            else
                return CursorPosition.None;
        }
        private void setupTimer(int ValueChange, int FirstInterval, int SecondInterval, int ThereafterInterval)
        {
            repeatTimer = new Timer();
            repeatTimer.Interval = FirstInterval;
            repeatTimer.Tick += (s, ee) =>
            {
                switch (ValueChange)
                {
                    case Int32.MaxValue:
                        if (ScrollSkip != null)
                            ScrollSkip.Invoke(false);
                        break;
                    case Int32.MinValue:
                        if (ScrollSkip != null)
                            ScrollSkip.Invoke(true);
                        break;
                    default:
                        int v = Value;
                        Value += ValueChange;
                        if (v != Value)
                        {
                            UserScroll.Invoke(this, Value);
                        }
                        else
                        {
                            repeatTimer.Stop();
                        }
                        break;
                }
                if (repeatTimer != null)
                {
                    if (repeatTimer.Interval == FirstInterval)
                        repeatTimer.Interval = SecondInterval;
                    else if (repeatTimer.Interval == SecondInterval)
                        repeatTimer.Interval = ThereafterInterval;
                }
            };
            repeatTimer.Start();
        }
        private bool isOnBottomButton(int Y)
        {
            return Y > this.ClientRectangle.Height - buttonHeight - 2;
        }
        private bool isOnTopButton(int Y)
        {
            return Y < buttonHeight + 2;
        }
        private bool isOnHandle(int Y, int handlePos)
        {
            return Y >= handlePos && Y <= (handlePos + handleHeight);
        }
        private bool isBelowHandle(int Y, int HandlePosition)
        {
            return Y > HandlePosition + handleHeight;
        }
        private bool isAboveHandle(int Y, int HandlePosition)
        {
            return Y < HandlePosition;
        }        
        private bool killRepeatTimer()
        {
            if (repeatTimer != null)
            {
                repeatTimer.Stop();
                repeatTimer.Dispose();
                repeatTimer = null;
                return true;
            }
            else
            {
                return false;
            }
        }
        private void setColors()
        {
            if ((brightness == SBBrightness.Bright) || ((brightness == SBBrightness.Auto) && this.enabled && (Range > 0)))
            {
                backgroundHoverColor = Styles.ScrollBarBackgroundHover;
                backgroundColor = Styles.ScrollBarBackground;
                buttonBrush = Styles.ScrollBarButtons;
                arrowPen = Styles.ScrollBarArrowPen;
                arrowPenHover = Styles.ScrollBarArrowPenHover;
                setHandleBrushGradientStyle();
            }
            else
            {
                backgroundHoverColor = Styles.ScrollBarBackgroundDisabled;
                backgroundColor = Styles.ScrollBarBackgroundDisabled;
                buttonBrush = Styles.ScrollBarButtonsDisabled;
                arrowPen = Styles.ScrollBarArrowPenDisabled;
                arrowPenHover = Styles.ScrollBarArrowPenDisabled;
                handleBrush = buttonBrush;
            }
        }

        private void setHandleBrushGradientStyle()
        {
            handleBrush = Style.GetScrollBarHandleBrush(buttonWidth, 1);
        }
        private void setMetrics()
        {
            this.needsRectPaint = this.Width > 14;

            if (this.SkipButtons)
                buttonAreaHeight = buttonHeight + skipButtonHeight + 1;
            else
                buttonAreaHeight = buttonHeight + 1;

            scrollablePixels = this.ClientRectangle.Height - buttonAreaHeight - buttonAreaHeight;

            if (LargeChange > 0 && Range > 0)
            {
                handleHeight = Math.Max(10, Math.Min(scrollablePixels * 2 / 3, scrollablePixels * LargeChange / Range));
            }
            else
            {
                handleHeight = 10;
            }

            scrollablePixels -= handleHeight;

            buttonWidth = Math.Max(MIN_BUTTON_SIZE, this.ClientRectangle.Width - 2);
            
            if (Range > 0)
                pixPerIncrement = scrollablePixels / (float)Range;
            else
                pixPerIncrement = float.MaxValue;

            upArrowLocation = new Point((this.ClientRectangle.Width - upArrow.Width) / 2, 1);
            downArrowLocation = new Point(upArrowLocation.X, this.ClientRectangle.Height - downArrow.Height - 1);
            upSkipLocation = new Point(upArrowLocation.X, upArrowLocation.X + upArrow.Height + 1);
            downSkipLocation = new Point(upArrowLocation.X, downArrowLocation.Y - downSkip.Height - 1);
        }
        private float handlePosition()
        {
            if (this.Value == this.Max && Range > 0)
                return buttonAreaHeight + scrollablePixels;
            else if (pixPerIncrement < float.MaxValue)
                return buttonAreaHeight + pixPerIncrement * (float)val;
            else
                return buttonAreaHeight;
        }
        private int valFromHandlePosition(int Position)
        {
            return (int)((Math.Max(0, 
                                   Math.Min(this.ClientRectangle.Height - buttonAreaHeight - buttonAreaHeight - handleHeight,
                                   Position - buttonAreaHeight - handleHeight / 2)))
                          * Range / scrollablePixels);
        }
    }
}
