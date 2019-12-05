/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace QuuxPlayer
{
    internal class QComboBox : ComboBox, IWatermarkable, IEditControl
    {
        private class ComboInfoHelper
        {
            [DllImport("user32")]
            private static extern bool GetComboBoxInfo(IntPtr hwndCombo, ref ComboBoxInfo info);

            #region RECT struct
            [StructLayout(LayoutKind.Sequential)]
            private struct RECT
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;
            }
            #endregion

            #region ComboBoxInfo Struct
            [StructLayout(LayoutKind.Sequential)]
            private struct ComboBoxInfo
            {
                public int cbSize;
                public RECT rcItem;
                public RECT rcButton;
                public IntPtr stateButton;
                public IntPtr hwndCombo;
                public IntPtr hwndEdit;
                public IntPtr hwndList;
            }
            #endregion

            public static int GetComboDropDownWidth()
            {
                ComboBox cb = new ComboBox();
                int width = GetComboDropDownWidth(cb.Handle);
                cb.Dispose();
                return width;
            }
            public static int GetComboDropDownWidth(IntPtr handle)
            {
                ComboBoxInfo cbi = new ComboBoxInfo();
                cbi.cbSize = Marshal.SizeOf(cbi);
                GetComboBoxInfo(handle, ref cbi);
                int width = cbi.rcButton.Right - cbi.rcButton.Left;
                return width;
            }
        }

        public const int WM_ERASEBKGND = 0x14;
        public const int WM_PAINT = 0x0F;
        public const int WM_NC_PAINT = 0x85;
        public const int WM_PRINTCLIENT = 0x318;
        private static int DropDownButtonWidth = 17;

        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, object lParam);

        [DllImport("user32")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        private bool highlighted = false;
        private QWatermark watermark = null;

        static QComboBox()
        {
            DropDownButtonWidth = ComboInfoHelper.GetComboDropDownWidth() + 2;
        }

        public QComboBox(bool Editable, Font Font) : base()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.DoubleBuffer, true);

            if (Editable)
                this.DropDownStyle = ComboBoxStyle.DropDown;
            else
                this.DropDownStyle = ComboBoxStyle.DropDownList;

            this.Font = Font;
        }
        public QComboBox(bool Editable) : this(Editable, Styles.Font)
        {
        }
        protected override void OnDropDown(EventArgs e)
        {
            base.OnDropDown(e);
            this.BackColor = Color.White;
        }
        protected override void OnDropDownClosed(EventArgs e)
        {
            base.OnDropDownClosed(e);
            if (this.Highlighted)
                this.BackColor = Styles.HighlightEditControl;
        }
        public bool Highlighted
        {
            get { return highlighted; }
            set
            {
                if (value != highlighted)
                {
                    highlighted = value;
                    if (!this.DroppedDown)
                        this.BackColor = highlighted ? Styles.HighlightEditControl : Color.White;
                }
            }
        }

        public void AutoSetWidth()
        {
            int width = this.Width - 10;
            foreach (object o in this.Items)
                width = Math.Max(width, TextRenderer.MeasureText(o.ToString(), this.Font).Width);
            
            this.Width = width + 18;
        }

        public void SetOriginalText()
        {
            this.OriginalText = this.Text;
        }
        public string OriginalText { get; private set; }
        public bool Changed
        {
            get { return this.Text != this.OriginalText; }
        }

        protected override void OnSelectedValueChanged(EventArgs e)
        {
            base.OnSelectedValueChanged(e);
            this.Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            if (this.DropDownStyle == ComboBoxStyle.Simple)
            {
                base.WndProc(ref m);
                return;
            }

            IntPtr hDC = IntPtr.Zero;
            Graphics gdc = null;
            switch (m.Msg)
            {
                case WM_NC_PAINT:

                    hDC = GetWindowDC(this.Handle);
                    gdc = Graphics.FromHdc(hDC);
                    SendMessage(this.Handle, WM_ERASEBKGND, hDC, 0);
                    SendPrintClientMsg();	// send to draw client area
                    PaintFlatControlBorder(gdc);
                    m.Result = (IntPtr)1;	// indicate msg has been processed			
                    ReleaseDC(m.HWnd, hDC);
                    gdc.Dispose();

                    break;
                case WM_PAINT:
                    base.WndProc(ref m);
                    // flatten the border area again
                    hDC = GetWindowDC(this.Handle);
                    gdc = Graphics.FromHdc(hDC);
                    Pen p = Styles.DarkBorderPen;
                    PaintDropdown(gdc);
                    PaintFlatControlBorder(gdc);
                    ReleaseDC(m.HWnd, hDC);
                    gdc.Dispose();

                    break;
                case WM_PRINTCLIENT:
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }
        private void SendPrintClientMsg()
        {
            // We send this message for the control to redraw the client area
            Graphics gClient = this.CreateGraphics();
            IntPtr ptrClientDC = gClient.GetHdc();
            SendMessage(this.Handle, WM_PRINTCLIENT, ptrClientDC, 0);
            gClient.ReleaseHdc(ptrClientDC);
            gClient.Dispose();
        }
        
        private void PaintFlatControlBorder(Graphics g)
        {
            ControlPaint.DrawBorder(g,
                                    new Rectangle(Point.Empty, this.Size),
                                    Styles.Dark,
                                    ButtonBorderStyle.Solid);
        }
        public void PaintDropdown(Graphics g)
        {
            Rectangle rect = new Rectangle(this.Width - DropDownButtonWidth,
                                           1,
                                           DropDownButtonWidth - 2,
                                           this.Height - 3);

            g.FillRectangle(Styles.ComboBoxButtonBackgroundBrush, rect);
            
            Styles.DrawArrow(g,
                             Styles.ScrollBarArrowPen,
                             false,
                             rect.Left + rect.Width / 2 - 1,
                             9);
            
            g.DrawRectangle(Styles.ComboBoxButtonBorderPen, rect);
        }

        protected override void OnLostFocus(System.EventArgs e)
        {
            base.OnLostFocus(e);
            updateWatermark();
            this.Invalidate();
        }
        protected override void OnGotFocus(System.EventArgs e)
        {
            base.OnGotFocus(e);
            updateWatermark();
            this.Invalidate();
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            updateWatermark();
            this.Invalidate();
        }
        
        private string nullText = String.Empty;
        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            updateWatermark();
        }

        public void EnableWatermark(Control Parent, string WatermarkText, string NullText)
        {
            watermark = new QWatermark(Parent, this, new Font(this.Font, FontStyle.Italic), focus, WatermarkText);
            this.nullText = NullText;
            updateWatermark();
        }
        public bool WatermarkEnabled
        {
            get { return watermark != null; }
            set { watermarkDisabled = !value; updateWatermark(); }
        }
        private void focus() { this.Focus(); }
        private bool watermarkDisabled = false;
        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            updateWatermark();
        }
        private void updateWatermark()
        {
            setWatermarkLabel(WatermarkEnabled &&
                              this.Text == nullText &&
                              !this.Focused);
        }
        private void setWatermarkLabel(bool On)
        {
            if (watermark != null)
            {

                watermark.Visible = On;
                if (On)
                {
                    watermark.Bounds = new Rectangle(this.Location.X + 2,
                                                     this.Location.Y + 2,
                                                     this.ClientRectangle.Width - 3 - DropDownButtonWidth,
                                                     this.ClientRectangle.Height - 3);
                    watermark.BringToFront();
                    //watermark.Invalidate();
                }
            }
        }
    }
}
