/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxPlayer
{
    internal sealed class QAction
    {
        public QActionType Type { get; private set; }
        public Track Track { get; private set; }
        public string Value { get; private set; }
        public string AltValue { get; private set; }

        private static Dictionary<QActionType, string> actionHelp;

        public QAction(QActionType Type)
        {
            this.Type = Type;
            this.Track = null;
            this.Value = String.Empty;
        }
        public QAction(QActionType Type, Track Track)
        {
            this.Type = Type;
            this.Track = Track;
            this.Value = String.Empty;
        }
        public QAction(QActionType Type, string Value)
        {
            this.Type = Type;
            this.Track = null;
            this.Value = Value;
        }
        public QAction(QActionType Type, string Value, string AltValue)
        {
            this.Type = Type;
            this.Track = null;
            this.Value = Value;
            this.AltValue = AltValue;
        }
        static QAction()
        {
            actionHelp = new Dictionary<QActionType, string>();

            actionHelp.Add(QActionType.Next, Localization.Get(UI_Key.Action_Help_Play_Next_Track));
            actionHelp.Add(QActionType.Previous, Localization.Get(UI_Key.Action_Help_Play_Previous_Track));
            actionHelp.Add(QActionType.VolumeUp, Localization.Get(UI_Key.Action_Help_Volume_Up));
            actionHelp.Add(QActionType.VolumeDown, Localization.Get(UI_Key.Action_Help_Volume_Down));
            actionHelp.Add(QActionType.ScanBack, Localization.Get(UI_Key.Action_Help_Scan_Reverse));
            actionHelp.Add(QActionType.ScanFwd, Localization.Get(UI_Key.Action_Help_Scan_Forward));
            actionHelp.Add(QActionType.NextFilter, Localization.Get(UI_Key.Action_Help_Next_Filter));
            actionHelp.Add(QActionType.PreviousFilter, Localization.Get(UI_Key.Action_Help_Previous_Filter));
            actionHelp.Add(QActionType.ReleaseAllFilters, Localization.Get(UI_Key.Action_Help_Release_All_Filters));
            actionHelp.Add(QActionType.ReleaseCurrentFilter, Localization.Get(UI_Key.Action_Help_Release_Current_Filter));
            actionHelp.Add(QActionType.PlaySelectedTracks, Localization.Get(UI_Key.Action_Help_Play_Selected_Tracks));
            actionHelp.Add(QActionType.PlayThisAlbum, Localization.Get(UI_Key.Action_Help_Play_Album));
            actionHelp.Add(QActionType.Pause, Localization.Get(UI_Key.Action_Help_Pause));
            actionHelp.Add(QActionType.Stop, Localization.Get(UI_Key.Action_Help_Stop));
            actionHelp.Add(QActionType.AdvanceScreenWithoutMouse, Localization.Get(UI_Key.Action_Help_Advance_View));
            actionHelp.Add(QActionType.HTPCMode, Localization.Get(UI_Key.Action_Help_HTPC_Mode));
            actionHelp.Add(QActionType.AddToNowPlayingAndAdvance, Localization.Get(UI_Key.Action_Help_Add_To_Now_Playing));
            actionHelp.Add(QActionType.Shuffle, Localization.Get(UI_Key.Action_Help_Shuffle));
            actionHelp.Add(QActionType.AdvanceSortColumn, Localization.Get(UI_Key.Action_Help_Advance_Sort_Column));
            actionHelp.Add(QActionType.ViewNowPlaying, Localization.Get(UI_Key.Action_Help_View_Now_Playing));
            actionHelp.Add(QActionType.SelectNextItemGamePadRight, Localization.Get(UI_Key.Action_Help_Select_Next_Track));
            actionHelp.Add(QActionType.SelectPreviousItemGamePadRight, Localization.Get(UI_Key.Action_Help_Select_Previous_Track));
            actionHelp.Add(QActionType.SelectNextItemGamePadLeft, Localization.Get(UI_Key.Action_Help_Select_Next_Filter_Value));
            actionHelp.Add(QActionType.SelectPreviousItemGamePadLeft, Localization.Get(UI_Key.Action_Help_Select_Previous_Filter_Value));
            actionHelp.Add(QActionType.FilterSelectedArtist, Localization.Get(UI_Key.Action_Help_Filter_Selected_Artist));
            actionHelp.Add(QActionType.FilterSelectedAlbum, Localization.Get(UI_Key.Action_Help_Filter_Selected_Album));
            actionHelp.Add(QActionType.ShowTrackAndAlbumDetails, Localization.Get(UI_Key.Action_Help_Show_Track_And_Album_Details));
            actionHelp.Add(QActionType.FindPlayingTrack, Localization.Get(UI_Key.Action_Help_Find_Currently_Playing_Track));
            actionHelp.Add(QActionType.ToggleEqualizer, Localization.Get(UI_Key.Action_Help_Toggle_Equalizer));
            actionHelp.Add(QActionType.SelectNextEqualizer, Localization.Get(UI_Key.Action_Help_Select_Next_Equalizer));
            actionHelp.Add(QActionType.ToggleGamepadHelp, Localization.Get(UI_Key.Action_Help_Toggle_Gamepad_Help));
            actionHelp.Add(QActionType.AddAlbumToNowPlaying, Localization.Get(UI_Key.Action_Help_Add_Album_To_Now_Playing));
            actionHelp.Add(QActionType.ToggleRadioMode, Localization.Get(UI_Key.Action_Help_Toggle_Radio_Mode));
        }
        public static string GetHelp(QActionType Type)
        {
            return actionHelp[Type];
        }
    }
}
