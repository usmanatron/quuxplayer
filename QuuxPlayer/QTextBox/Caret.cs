/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace QuuxControls
{
    public class Caret
    {
        [DllImport("User32.dll")]
        static extern bool CreateCaret(IntPtr hWnd, int hBitmap, int nWidth, int nHeight);
        [DllImport("User32.dll")]
        static extern bool SetCaretPos(int x, int y);
        [DllImport("User32.dll")]
        static extern bool DestroyCaret();
        [DllImport("User32.dll")]
        static extern bool ShowCaret(IntPtr hWnd);
        [DllImport("User32.dll")]
        static extern bool HideCaret(IntPtr hWnd);

        private IntPtr hWnd;
        private Size size;
        private Point location;
        private bool valid;
        private bool wasHidden;

        public Caret(IntPtr hWnd)
        {
            this.hWnd = hWnd;
            location = new Point(-1, -1);
            valid = false;
            wasHidden = false;
        }

        public Size Size
        {
            get { return size; }
            set
            {
                valid = true;
                if (size != value)
                {
                    DestroyCaret();
                    size = value;
                    moveCaret();
                }
            }
        }
        public void Invalidate()
        {
            valid = false;
            hideCaret();
        }
        public bool Valid
        {
            get { return valid; }
        }
        public Point Location
        {
            get
            {
                return location;
            }
            set
            {
                if (location != value)
                {
                    location = value;
                    moveCaret();
                }
            }
        }

        public void Show()
        {
            if (location.X >= 0)
            {
                showCaret();
            }
        }
        public void Hide()
        {
            hideCaret();
        }

        private void createCaret()
        {
            bool success = false;

            while (!success)
            {
                success = CreateCaret(hWnd, 0, size.Width, size.Height);
                System.Threading.Thread.Sleep(5);
            }
        }
        private void moveCaret()
        {
            bool success = false;

            while (!success)
            {
                success = SetCaretPos(location.X, location.Y);
                if (!success)
                {
                    DestroyCaret();
                    createCaret();
                }
            }
        }
        private void showCaret()
        {
            bool success = false;

            while (!success)
            {
                success = ShowCaret(hWnd);
                if (!success)
                {
                    DestroyCaret();
                    createCaret();
                    System.Threading.Thread.Sleep(5);
                }
            }
            wasHidden = false;
        }
        private void hideCaret()
        {
            if (!wasHidden)
            {
                wasHidden = true;
                bool success = false;

                success = HideCaret(hWnd);
                if (!success)
                {
                    DestroyCaret();
                }
            }
        }

        ~Caret()
        {
            DestroyCaret();
        }
    }
}
