/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal enum QMessageBoxButtons { OK, OKCancel, YesNo }
    internal enum QMessageBoxIcon { None, Error, Information, Question, Warning }
    internal enum QMessageBoxButton { YesOK, NoCancel }

    internal class QMessageBox : QFixedDialog
    {
        private QLabel lblMain;
        private QButton btn1;
        private QButton btn2;
        private QMessageBoxIcon icon;
        private QMessageBoxButtons qmbButtons;
        private QMessageBoxButton defaultButton;
        private int iconTop;
        
        protected QMessageBox(string Text, string Title, QMessageBoxButtons Buttons, QMessageBoxIcon Icon, QMessageBoxButton DefaultButton) : base(Title, (Buttons == QMessageBoxButtons.OK) ? ButtonCreateType.OKOnly : ButtonCreateType.OKAndCancel)
        {
            this.SPACING = 8;

            this.ClientSize = new Size(400, 300);

            icon = Icon;

            int textWidth = TextRenderer.MeasureText(Text, Styles.Font).Width;
            int iconWidth = (icon == QMessageBoxIcon.None) ? 0 : getIcon().Width + 5;

            int width;

            int baseWidth = textWidth + iconWidth;

            if (baseWidth < 300)
                width = Math.Max(150, baseWidth + 50);
            else if (baseWidth > 400)
                width = 400 + SPACING + iconWidth;
            else
                width = 350;

            lblMain = new QLabel(Text);
            this.Controls.Add(lblMain);
            lblMain.Location = new Point(SPACING + iconWidth, SPACING);
            lblMain.SetWidth(width - lblMain.Left - 2 * SPACING);

            iconTop = Math.Max(lblMain.Top, lblMain.Top + lblMain.Height / 2 - getIcon().Height);

            qmbButtons = Buttons;

            switch (qmbButtons)
            {
                case QMessageBoxButtons.OK:
                    btn1 = btnOK;
                    break;
                case QMessageBoxButtons.OKCancel:
                    btn1 = btnCancel;
                    btn2 = btnOK;
                    break;
                case QMessageBoxButtons.YesNo:
                    btn1 = btnCancel;
                    btn1.Text = Localization.NO;
                    btn2 = btnOK;
                    btn2.Text = Localization.YES;
                    break;
            }
            PlaceButtons(width, lblMain.Bottom + SPACING);
            this.ClientSize = new Size(width, btn1.Bottom + SPACING);

            Lib.DoEvents();
            Lib.Beep();

            defaultButton = DefaultButton;

            Clock.DoOnMainThread(doFocus);
        }
        private void doFocus()
        {
            if (btn2 != null)
            {
                if (defaultButton == QMessageBoxButton.NoCancel)
                    btn1.Focus();
                else
                    btn2.Focus();
            }
            else
            {
                btn1.Focus();
            }
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Y:
                    if (qmbButtons == QMessageBoxButtons.YesNo)
                        ok();
                    return true;
                case Keys.N:
                    if (qmbButtons == QMessageBoxButtons.YesNo)
                        cancel();
                    return true;
                case Keys.Escape:
                    cancel();
                    return true;
                case Keys.Enter:
                    if (btnOK != null && btnOK.Focused)
                        ok();
                    else if (btnCancel != null && btnCancel.Focused)
                        cancel();
                    return true;
                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (icon != QMessageBoxIcon.None)
            {
                Icon i = getIcon();
                e.Graphics.DrawImageUnscaled(i.ToBitmap(), SPACING, iconTop);
            }
        }

        private Icon getIcon()
        {
            Icon i = null;
            switch (this.icon)
            {
                case QMessageBoxIcon.Error:
                    i = SystemIcons.Error;
                    break;
                case QMessageBoxIcon.Information:
                    i = SystemIcons.Information;
                    break;
                case QMessageBoxIcon.Question:
                    i = SystemIcons.Question;
                    break;
                case QMessageBoxIcon.Warning:
                    i = SystemIcons.Warning;
                    break;
                default:
                    return null;

            }
            return i;
        }

        public static DialogResult Show(IWin32Window Owner, string Text, string Title, QMessageBoxIcon Icon)
        {
            return Show(Owner, Text, Title, QMessageBoxButtons.OK, Icon, QMessageBoxButton.YesOK);
        }
        public static DialogResult Show(IWin32Window Owner, string Text, string Title, QMessageBoxButtons Buttons, QMessageBoxIcon Icon, QMessageBoxButton DefaultButton)
        {
            QMessageBox box = new QMessageBox(Text, Title, Buttons, Icon, DefaultButton);
            box.ShowDialog(Owner);
            return box.DialogResult;
        }
    }
}
