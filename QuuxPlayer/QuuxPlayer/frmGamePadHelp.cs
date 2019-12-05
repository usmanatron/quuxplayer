/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal sealed partial class frmGamepadHelp : QFixedDialog, IActionHandler
    {
        public bool GamepadEnabled { get; set; }
        public Form MainForm { get; set; }

        private static readonly string instructions = Localization.Get(UI_Key.Gamepad_Help_Instructions);

        private QLabel lblInstructions;
        private Bitmap gamePadImage;
        private Rectangle imageRect;
        private string message = String.Empty;
        private ulong msgTick = Clock.NULL_ALARM;
        private Dictionary<QActionType, Point> padLocations;
        private Point anchorPoint;
        private QActionType lastAction = QActionType.None;
        private static SolidBrush brush = new SolidBrush(Styles.GamePadHelpColor);
        private static Pen pen = new Pen(Styles.GamePadHelpColor, 3);

        public frmGamepadHelp() : base(Localization.Get(UI_Key.Gamepad_Help_Title), ButtonCreateType.OKOnly)
        {
            GamepadEnabled = false;

            gamePadImage = Styles.BitmapGamePad;

            this.ClientSize = new Size(gamePadImage.Width + 30, 1000);

            lblInstructions = new QLabel(Localization.Get(UI_Key.Gamepad_Help_Instructions));
            this.Controls.Add(lblInstructions);
            lblInstructions.Location = new Point(MARGIN, MARGIN);
            lblInstructions.SetWidth(this.ClientSize.Width - MARGIN - MARGIN);

            btnOK.Text = Localization.Get(UI_Key.Gamepad_Help_Exit);
            
            this.ClientSize = new Size(this.ClientSize.Width, lblInstructions.Bottom + gamePadImage.Height + MARGIN + MARGIN + btnOK.Height + 5);
            
            PlaceButtons(this.ClientRectangle.Width, this.ClientRectangle.Height - btnOK.Height - MARGIN);

            btnOK.BackColor = Color.Black;
            
            setupPadLocations();

            showMessage(Localization.Get(UI_Key.Gamepad_Help_Press_Button));
        }

        public ActionHandlerType Type { get { return ActionHandlerType.HelpScreen; } }

        public void RequestAction(QActionType ActionType)
        {
            if (ActionType == QActionType.ToggleGamepadHelp)
                this.Close();
            else
                showAction(ActionType);
        }
        public void RequestAction(QAction Action)
        {
            showAction(Action.Type);
            lastAction = Action.Type;
        }

        private void setupPadLocations()
        {
            anchorPoint = new Point(60, btnOK.Top - 5);

            padLocations = new Dictionary<QActionType, Point>();

            // Make sure the alternate function comes after the normal function

            padLocations.Add(QActionType.PlaySelectedTracks, new Point(423, 13));
            padLocations.Add(QActionType.PlayThisAlbum, new Point(423, 13));
            padLocations.Add(QActionType.Pause, new Point(423, 5));
            padLocations.Add(QActionType.Stop, new Point(423, 5));

            padLocations.Add(QActionType.SelectNextItemGamePadRight, new Point(341, 234));
            padLocations.Add(QActionType.SelectPreviousItemGamePadRight, new Point(341, 188));
            padLocations.Add(QActionType.SelectNextItemGamePadLeft, new Point(190, 238));
            padLocations.Add(QActionType.SelectPreviousItemGamePadLeft, new Point(190, 186));

            padLocations.Add(QActionType.Previous, new Point(380, 126));
            padLocations.Add(QActionType.ScanBack, new Point(380, 126));
            padLocations.Add(QActionType.VolumeDown, new Point(417, 164));
            padLocations.Add(QActionType.Next, new Point(456, 125));
            padLocations.Add(QActionType.ScanFwd, new Point(456, 125));
            padLocations.Add(QActionType.VolumeUp, new Point(418, 88));

            padLocations.Add(QActionType.PreviousFilter, new Point(82, 124));
            padLocations.Add(QActionType.ViewNowPlaying, new Point(82, 124));
            padLocations.Add(QActionType.NextFilter, new Point(150, 125));
            padLocations.Add(QActionType.FindPlayingTrack, new Point(150, 125));

            padLocations.Add(QActionType.ReleaseCurrentFilter, new Point(117, 159));
            padLocations.Add(QActionType.ReleaseAllFilters, new Point(117, 159));
            
            padLocations.Add(QActionType.FilterSelectedArtist, new Point(116, 89));
            padLocations.Add(QActionType.FilterSelectedAlbum, new Point(116, 89));

            padLocations.Add(QActionType.AdvanceScreenWithoutMouse, new Point(106, 13));
            padLocations.Add(QActionType.ShowTrackAndAlbumDetails, new Point(106, 13));

            padLocations.Add(QActionType.ToggleEqualizer, new Point(106, 5));
            padLocations.Add(QActionType.SelectNextEqualizer, new Point(106, 5));

            padLocations.Add(QActionType.AddToNowPlayingAndAdvance, new Point(213, 96));
            padLocations.Add(QActionType.ToggleRadioMode, new Point(213, 96));

            padLocations.Add(QActionType.HTPCMode, new Point(320, 95));
            padLocations.Add(QActionType.ToggleGamepadHelp, new Point(320, 95));

            padLocations.Add(QActionType.AdvanceSortColumn, new Point(190, 211));
            padLocations.Add(QActionType.Shuffle, new Point(341, 211));
        }
        private void showHelpByMouse(int X, int Y, bool Alt)
        {
            int lengthSquared = Int32.MaxValue;
            QActionType type = QActionType.None;

            int x = X - imageRect.X;
            int y = Y - imageRect.Y;

            int dist;

            foreach (KeyValuePair<QActionType, Point> kvp in padLocations)
            {
                dist = (((kvp.Value.X - x) * (kvp.Value.X - x)) + ((kvp.Value.Y - y) * (kvp.Value.Y - y)));
                if (lengthSquared > dist || (Alt && lengthSquared == dist))
                {
                    lengthSquared = dist;
                    type = kvp.Key;
                }
            }
            if (lengthSquared < 700)
                showAction(type);
        }
        private void showAction(QActionType ActionType)
        {
            showMessage(QAction.GetHelp(ActionType));
            lastAction = ActionType;
        }
        private void showMessage(string Message)
        {
            message = Message;
            Clock.Update(ref msgTick, clearMessage, 2500, false);

            this.Invalidate();
        }
        private void clearMessage()
        {
            message = String.Empty;
            msgTick = long.MaxValue;
            lastAction = QActionType.None;
            this.Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            showHelpByMouse(e.X, e.Y, true);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            showHelpByMouse(e.X, e.Y, (e.Button != MouseButtons.None));
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (!GamepadEnabled)
            {
                Lib.DoEvents();
                QMessageBox.Show(MainForm,
                                 Localization.Get(UI_Key.Gamepad_Help_No_Gamepad),
                                 Localization.Get(UI_Key.Gamepad_Help_No_Gamepad_Title),
                                 QMessageBoxIcon.Information);
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.Clear(Color.Black);
            e.Graphics.DrawImage(gamePadImage, imageRect);
            //TextRenderer.DrawText(e.Graphics, instructions, Styles.Font, new Rectangle(MARGIN, MARGIN, this.ClientRectangle.Width - MARGIN - MARGIN, 200), Styles.LightText, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            if (message.Length > 0)
            {
                TextRenderer.DrawText(e.Graphics, message, Styles.FontLarge, new Point(MARGIN, anchorPoint.Y + 5), Styles.GamePadHelpColor, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                if (padLocations.ContainsKey(lastAction))
                {
                    e.Graphics.DrawLine(pen, anchorPoint, new Point(padLocations[lastAction].X + imageRect.X, padLocations[lastAction].Y + imageRect.Y));
                    e.Graphics.FillEllipse(brush, new Rectangle(padLocations[lastAction].X + imageRect.X - 5, padLocations[lastAction].Y + imageRect.Y - 5, 10, 10));
                }
            }
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            int instHeight = TextRenderer.MeasureText(instructions, Styles.Font, new Size(this.ClientRectangle.Width - MARGIN - MARGIN, 200), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Height;

            imageRect = new Rectangle(this.ClientRectangle.Width / 2 - gamePadImage.Width / 2,
                                 MARGIN + MARGIN + instHeight,
                                 gamePadImage.Width,
                                 gamePadImage.Height);

        }
    }
}
