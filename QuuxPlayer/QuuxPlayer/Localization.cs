/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal static class Localization
    {
        private static string[] displayValues;

        public static string NOW_PLAYING;
        public static string DUPLICATES;
        public static string GHOSTS;
        public static string MY_PLAYLIST;
        public static string MUTE;
        public static string VOLUME;
        public static string VOL_WITH_EQ;
        public static string OK;
        public static string CANCEL;
        public static string YES;
        public static string NO;
        public static string NO_GENRE;
        public static string ZERO_TIME;
        public static string DAY;
        public static string DAYS;
        public static string HOUR;
        public static string HOURS;
        public static string MINUTE;
        public static string MINUTES;

        static Localization()
        {
            displayValues = new string[(int)UI_Key.Max];

            loadKeys();
            
            if (File.Exists(Lib.ProgramPath("local.cfg")))
                overrideKeys();

            NOW_PLAYING = displayValues[(int)UI_Key.General_Now_Playing];
            DUPLICATES = displayValues[(int)UI_Key.General_Duplicates];
            GHOSTS = displayValues[(int)UI_Key.General_Ghosts];
            MY_PLAYLIST = displayValues[(int)UI_Key.General_My_Playlist];
            MUTE = displayValues[(int)UI_Key.Control_Panel_Mute];
            VOLUME = displayValues[(int)UI_Key.Control_Panel_Volume];
            VOL_WITH_EQ = displayValues[(int)UI_Key.Control_Panel_Vol_With_Eq];
            OK = displayValues[(int)UI_Key.Button_OK];
            CANCEL = displayValues[(int)UI_Key.Button_Cancel];
            YES = displayValues[(int)UI_Key.Button_Yes];
            NO = displayValues[(int)UI_Key.Button_No];
            NO_GENRE = displayValues[(int)UI_Key.General_No_Genre];
            ZERO_TIME = displayValues[(int)UI_Key.Lib_Zero_Time];
            DAY = displayValues[(int)UI_Key.Lib_Day];
            DAYS = displayValues[(int)UI_Key.Lib_Days];
            HOUR = displayValues[(int)UI_Key.Lib_Hour];
            HOURS = displayValues[(int)UI_Key.Lib_Hours];
            MINUTE = displayValues[(int)UI_Key.Lib_Minute];
            MINUTES = displayValues[(int)UI_Key.Lib_Minutes];

#if DEBUG
            writeKeys();
#endif

        }
        public static string Get(UI_Key Key)
        {
            return displayValues[(int)Key];
        }
        public static string Get(UI_Key Key, float Param)
        {
            try
            {
                return String.Format(displayValues[(int)Key], Param);
            }
            catch
            {
                return displayValues[(int)Key];
            }
        }
        public static string Get(UI_Key Key, string Param0)
        {
            try
            {
                return String.Format(displayValues[(int)Key], Param0);
            }
            catch
            {
                return displayValues[(int)Key];
            }
        }
        public static string Get(UI_Key Key, string Param0, string Param1)
        {
            try
            {
                return String.Format(displayValues[(int)Key], Param0, Param1);
            }
            catch
            {
                return displayValues[(int)Key];
            }
        }
        public static string Get(UI_Key Key, string Param0, string Param1, string Param2)
        {
            try
            {
                return String.Format(displayValues[(int)Key], Param0, Param1, Param2);
            }
            catch
            {
                return displayValues[(int)Key];
            }
        }
        public static string Get(UI_Key Key, string Param0, string Param1, string Param2, string Param3, string Param4, string Param5, string Param6)
        {
            try
            {
                return String.Format(displayValues[(int)Key], Param0, Param1, Param2, Param3, Param4, Param5, Param6);
            }
            catch
            {
                return displayValues[(int)Key];
            }
        }
        private static void loadKeys()
        {

            displayValues[(int)UI_Key.About_Version] = "Version {0}";
            displayValues[(int)UI_Key.About_License_Agreement] = "View License Agreement...";

            displayValues[(int)UI_Key.Action_Help_Play_Next_Track] = "Play Next Track";
            displayValues[(int)UI_Key.Action_Help_Play_Previous_Track] = "Play Previous Track";
            displayValues[(int)UI_Key.Action_Help_Volume_Up] = "Volume Up";
            displayValues[(int)UI_Key.Action_Help_Volume_Down] = "Volume Down";
            displayValues[(int)UI_Key.Action_Help_Scan_Reverse] = "Scan Reverse";
            displayValues[(int)UI_Key.Action_Help_Scan_Forward] = "Scan Forward";
            displayValues[(int)UI_Key.Action_Help_Previous_Filter] = "Show Previous Filter List";
            displayValues[(int)UI_Key.Action_Help_Next_Filter] = "Show Next Filter List";
            displayValues[(int)UI_Key.Action_Help_Release_All_Filters] = "Release All Filters";
            displayValues[(int)UI_Key.Action_Help_Release_Current_Filter] = "Release Current Filter";
            displayValues[(int)UI_Key.Action_Help_Play_Selected_Tracks] = "Play Selected Tracks";
            displayValues[(int)UI_Key.Action_Help_Play_Album] = "Play Album of Selected Track";
            displayValues[(int)UI_Key.Action_Help_Pause] = "Pause";
            displayValues[(int)UI_Key.Action_Help_Stop] = "Stop";
            displayValues[(int)UI_Key.Action_Help_Advance_View] = "Advance to Next View";
            displayValues[(int)UI_Key.Action_Help_HTPC_Mode] = "Switch HTPC / Normal Mode";
            displayValues[(int)UI_Key.Action_Help_Add_To_Now_Playing] = "Add to 'Now Playing'";
            displayValues[(int)UI_Key.Action_Help_Add_Album_To_Now_Playing] = "Add Album to 'Now Playing'";
            displayValues[(int)UI_Key.Action_Help_Toggle_Radio_Mode] = "Internet Radio Mode On/Off";
            displayValues[(int)UI_Key.Action_Help_Shuffle] = "Shuffle Tracks";
            displayValues[(int)UI_Key.Action_Help_Advance_Sort_Column] = "Advance Sort Column";
            displayValues[(int)UI_Key.Action_Help_View_Now_Playing] = "View 'Now Playing'";
            displayValues[(int)UI_Key.Action_Help_Select_Next_Track] = "Select Next Track";
            displayValues[(int)UI_Key.Action_Help_Select_Previous_Track] = "Select Previous Track";
            displayValues[(int)UI_Key.Action_Help_Select_Next_Filter_Value] = "Select Next Filter Value";
            displayValues[(int)UI_Key.Action_Help_Select_Previous_Filter_Value] = "Select Previous Filter Value";
            displayValues[(int)UI_Key.Action_Help_Filter_Selected_Artist] = "Show All Tracks for Selected Track's Artist";
            displayValues[(int)UI_Key.Action_Help_Filter_Selected_Album] = "Show All Tracks on Selected Track's Album";
            displayValues[(int)UI_Key.Action_Help_Show_Track_And_Album_Details] = "Show Lyrics and Album Info (from internet)";
            displayValues[(int)UI_Key.Action_Help_Find_Currently_Playing_Track] = "Find Currently Playing Track";
            displayValues[(int)UI_Key.Action_Help_Toggle_Equalizer] = "Turn Equalizer On / Off";
            displayValues[(int)UI_Key.Action_Help_Select_Next_Equalizer] = "Select Next Equalizer Preset";
            displayValues[(int)UI_Key.Action_Help_Toggle_Gamepad_Help] = "Show / Hide Gamepad Help";

            displayValues[(int)UI_Key.Activation_Title] = "Activation - {0} {1}";
            displayValues[(int)UI_Key.Activation_Instructions] = "To permanently activate {0} Pro for unlimited use, please enter your name, email address, and key below.";
            displayValues[(int)UI_Key.Activation_Name] = "Name";
            displayValues[(int)UI_Key.Activation_Email] = "Email (used for activation only)";
            displayValues[(int)UI_Key.Activation_Key] = "Activation Key";
            displayValues[(int)UI_Key.Activation_Purchase_Key] = "Purchase...";
            displayValues[(int)UI_Key.Activation_Activation_File] = "Have File...";
            displayValues[(int)UI_Key.Activation_Invalid_Key] = "Invalid Key";
            displayValues[(int)UI_Key.Activation_Key_Wrong_Length] = "The key you have entered is the wrong length. Keys contain sixteen letters and numbers. Please check your key and try again.";
            displayValues[(int)UI_Key.Activation_Key_Invalid_Chars] = "The key you have entered contains invalid characters. Keys contain only letters and numbers. Please check your key and try again.";
            displayValues[(int)UI_Key.Activation_Email_Invalid] = "The email address you have entered is invalid. Please check the email address and try again.";
            displayValues[(int)UI_Key.Activation_Email_Invalid_Title] = "Invalid Email Address";
            displayValues[(int)UI_Key.Activation_Name_Invalid] = "The name you have entered contains invalid characters. Please use standard characters and try again.";
            displayValues[(int)UI_Key.Activation_Name_Invalid_Title] = "Invalid Characters In Name";
            displayValues[(int)UI_Key.Activation_Activated_Successfully] = "{0} Pro has been activated successfully.";
            displayValues[(int)UI_Key.Activation_Activated_Successfully_Title] = "Activation Successful";
            displayValues[(int)UI_Key.Activation_Failed] = "Activation failed. Please check your name and key and try again.";
            displayValues[(int)UI_Key.Activation_File_Failed] = "Activation file not valid. Please contact support@quuxplayer.com for assistance.";
            displayValues[(int)UI_Key.Activation_Failed_Title] = "Activation Failed";
            displayValues[(int)UI_Key.Activation_Not_Activated] = "{0} Pro is not activated. Do you wish to attempt activation again?";
            displayValues[(int)UI_Key.Activation_Not_Activated_Title] = "Not Activated";
            displayValues[(int)UI_Key.Activation_Error] = "Error connecting the activation server. Please check your internet connection and try again.";

            displayValues[(int)UI_Key.Album_Details_View_On_Web] = "View on Web";
            displayValues[(int)UI_Key.Album_Details_Play_This_Album] = "Play this Album";
            displayValues[(int)UI_Key.Album_Details_Next_Screen] = "Next Screen...";
            displayValues[(int)UI_Key.Album_Details_Copy_Info_To_Clipboard] = "Copy Info to Clipboard";
            displayValues[(int)UI_Key.Album_Details_No_Online_Info_Found] = "No online details found.";
            displayValues[(int)UI_Key.Album_Details_Average_Rating] = "Avg Rating: ";
            displayValues[(int)UI_Key.Album_Details_Loading_Lyrics] = "Loading Lyrics...";

            displayValues[(int)UI_Key.Artwork_No_Cover_Found] = "No Cover Found";

            displayValues[(int)UI_Key.Balloon_Update_Available] = "There is a new version of {0} available. Click here for details.";
            displayValues[(int)UI_Key.Balloon_Update_Available_Title] = "Update Available";

            displayValues[(int)UI_Key.Background_Cataloging_Tracks] = "Cataloging Tracks (press escape key to stop)...";

            displayValues[(int)UI_Key.Button_OK] = "OK";
            displayValues[(int)UI_Key.Button_Cancel] = "Cancel";
            displayValues[(int)UI_Key.Button_Yes] = "Yes";
            displayValues[(int)UI_Key.Button_No] = "No";

            displayValues[(int)UI_Key.Lib_Zero_Time] = "0 Minutes, 0 Seconds.";
            displayValues[(int)UI_Key.Lib_Day] = "Day";
            displayValues[(int)UI_Key.Lib_Days] = "Days";
            displayValues[(int)UI_Key.Lib_Hour] = "Hour";
            displayValues[(int)UI_Key.Lib_Hours] = "Hours";
            displayValues[(int)UI_Key.Lib_Minute] = "Minute";
            displayValues[(int)UI_Key.Lib_Minutes] = "Minutes";
            displayValues[(int)UI_Key.Lib_Adding] = "Adding";
            //note: change the default ofd filter index if new items are added:
            displayValues[(int)UI_Key.Lib_File_Filter] = "MP3 Files (*.mp3)|*.mp3|OGG Files (*.ogg)|*.ogg|FLAC Files (*.flac;*.fla)|*.flac;*.fla|WMA Files (*.wma)|*.wma|WAV Files (*.wav)|*.wav|iTunes Files (*.m4a;*.m4b;*.aac)|*.m4a;*.m4b;*.aac|AIFF Files (*.aiff;*.aif)|*.aiff;*.aif|WavPack Files (*.wv)|*.wv|Musepack Files (*.mpc)|*.mpc|Dolby Digital Files (*.ac3)|*.ac3|Monkey's Audio Files (*.ape)|*.ape|All Audio Files (*.mp3;*.ogg;*.flac;*.fla;*.wma;*.wav;*.m4a;*.aac;*.aiff;*.aif;*.wv;*.mpc;*.ac3;*.ape)|*.mp3;*.ogg;*.flac;*.fla;*.wma;*.wav;*.m4a;*.m4b;*.aac;*.aiff;*.aif;*.wv;*.mpc;*.ac3;*.ape";
            displayValues[(int)UI_Key.Dialog_Already_Running] = "{0} is already running.";
            displayValues[(int)UI_Key.Dialog_Already_Running_Title] = "Already Running";
            displayValues[(int)UI_Key.Dialog_Auto_Monitor_Pro] = "{0} can automatically monitor the folders in your library and add new files and update existing files. This feature is available only in {0} Pro Edition.";
            displayValues[(int)UI_Key.Dialog_Auto_Monitor_Pro_Title] = "Auto File Monitoring - {0} Pro Edition";
            displayValues[(int)UI_Key.Dialog_Playlist_Export_Pro] = "You can export the tracks shown in your tracklist as a standard playlist (m3u) file. This feature is available only in {0} Pro Edition.";
            displayValues[(int)UI_Key.Dialog_Playlist_Export_Pro_Title] = "Export As Playlist - {0} Pro Edition";
            displayValues[(int)UI_Key.Dialog_Replay_Gain_Pro] = "Replay gain is a method to smooth out volume fluxuations caused by different tracks being encoded at different volumes. This feature is available only in {0} Pro Edition.";
            displayValues[(int)UI_Key.Dialog_Replay_Gain_Pro_Title] = "Replay Gain - {0} Pro Edition";
            displayValues[(int)UI_Key.Dialog_Sleep_Pro] = "Sleep will turn off your PC after a set amount of time. You can also optionally tell {0} to gradually reduce the volume before going to sleep. This feature is available only in {0} Pro Edition.";
            displayValues[(int)UI_Key.Dialog_Sleep_Pro_Title] = "Sleep - {0} Pro Edition";
            displayValues[(int)UI_Key.Dialog_To_Get_Started] = "To get started] = click the File menu and then choose either 'Add Folder to Library' or 'Add File to Library.'";
            displayValues[(int)UI_Key.Dialog_To_Get_Started_Title] = "Add Tracks to Library";
            displayValues[(int)UI_Key.Dialog_Lock_Enter_Unlock_Code] = "Enter the unlock code:";
            displayValues[(int)UI_Key.Dialog_Lock_Enter_Unlock_Code_Title] = "Unlock Code";
            displayValues[(int)UI_Key.Dialog_Lock_Incorrect_Code] = "Incorrect unlock code.";
            displayValues[(int)UI_Key.Dialog_Lock_Incorrect_Code_Title] = "Incorrect Code";
            displayValues[(int)UI_Key.Dialog_Create_New_Playlist] = "&Enter a name for your playlist:";
            displayValues[(int)UI_Key.Dialog_Create_New_Playlist_Title] = "Enter playlist name";
            displayValues[(int)UI_Key.Dialog_Create_Auto_Playlist] = "Make This An &Automatic Playlist";
            displayValues[(int)UI_Key.Dialog_Blank_Playlist_Name] = "Playlist name must not be blank.";
            displayValues[(int)UI_Key.Dialog_Blank_Playlist_Name_Title] = "Invalid Playlist Name";
            displayValues[(int)UI_Key.Dialog_Duplicate_Playlist] = "The playlist '{0}' already exists. Please choose a new name.";
            displayValues[(int)UI_Key.Dialog_Duplicate_Playlist_Title] = "Duplicate Playlist Name";
            displayValues[(int)UI_Key.Dialog_Update_Available] = "An update to {0} is available ({1}). Click OK to visit the download site.";
            displayValues[(int)UI_Key.Dialog_Update_Available_Title] = "Update Available";
            displayValues[(int)UI_Key.Dialog_Update_Available_Minor] = "A minor update to {0} is available ({1}). Click OK to visit the download site.";
            displayValues[(int)UI_Key.Dialog_No_Update_Available] = "No update is available at this time.";
            displayValues[(int)UI_Key.Dialog_No_Update_Available_Title] = "No Update Available";
            displayValues[(int)UI_Key.Dialog_Update_Check_Error] = "Error checking for update. Please ensure you are connected to the internet and try again.";
            displayValues[(int)UI_Key.Dialog_Update_Check_Error_Title] = "Error Checking For Update";
            displayValues[(int)UI_Key.Dialog_Gamepad_Enabled] = "Gamepad found and enabled.";
            displayValues[(int)UI_Key.Dialog_Gamepad_Enabled_Title] = "Gamepad Enabled";
            displayValues[(int)UI_Key.Dialog_No_Gamepad_Detected] = "Gamepad was not detected.";
            displayValues[(int)UI_Key.Dialog_No_Gamepad_Detected_Title] = "No Gamepad Detected";
            displayValues[(int)UI_Key.Dialog_Import_Playlist_No_Tracks_Found] = "No tracks found for playlist.";
            displayValues[(int)UI_Key.Dialog_Import_Playlist_No_Tracks_Found_Title] = "No Tracks Found";
            displayValues[(int)UI_Key.Dialog_Equalizer_Remove_Preset] = "Remove the preset '{0}'?";
            displayValues[(int)UI_Key.Dialog_Equalizer_Remove_Preset_Title] = "Remove Preset?";
            displayValues[(int)UI_Key.Dialog_Equalizer_New_Preset] = "Enter a name for your new preset:";
            displayValues[(int)UI_Key.Dialog_Equalizer_New_Preset_Title] = "New Preset Name";
            displayValues[(int)UI_Key.Dialog_Equalizer_Duplicate_Preset] = "The equalizer name '{0}' is already in use.";
            displayValues[(int)UI_Key.Dialog_Equalizer_Duplicate_Preset_Title] = "Duplicate Name";
            displayValues[(int)UI_Key.Dialog_Equalizer_30_Bands_Pro] = "{0} Pro Edition supports a switchable 10 or 30 band equalizer.";
            displayValues[(int)UI_Key.Dialog_Equalizer_30_Bands_Pro_Title] = "{0} Pro Edition";
            displayValues[(int)UI_Key.Dialog_Clear_Database] = "This action will delete your existing library including track and playlist information. Are you sure you wish to continue? No settings or purchase information will be removed] = nor will any music files be erased from your computer.";
            displayValues[(int)UI_Key.Dialog_Clear_Database_Title] = "Erase Library?";
            displayValues[(int)UI_Key.Dialog_Remove_Ghost_Tracks] = "This action will remove all tracks from your library that cannot be found. Are you sure you wish to continue?";
            displayValues[(int)UI_Key.Dialog_Remove_Ghost_Tracks_Title] = "Remove Ghost Tracks?";
            displayValues[(int)UI_Key.Dialog_Refresh_Tracks] = "This action will update all the tags for the tracks in your library. It will not change statistics such as play counts. Continue?";
            displayValues[(int)UI_Key.Dialog_Refresh_Tracks_Title] = "Refresh Tracks?";
            displayValues[(int)UI_Key.Dialog_Spreadsheet_Export_Pro] = "{0} can export the information shown in the current view in spreadsheet format] = such as for Microsoft Excel. This feature is available only in {0} Pro Edition.";
            displayValues[(int)UI_Key.Dialog_Spreadsheet_Export_Pro_Title] = "Spreadsheet Export - {0} Pro Edition";
            displayValues[(int)UI_Key.Dialog_Remove_Playlist] = "Are you sure you want to remove the playlist '{0}'?";
            displayValues[(int)UI_Key.Dialog_Remove_Playlist_Title] = "Remove Playlist?";

            displayValues[(int)UI_Key.Filter_Playlist] = "Playlist";
            displayValues[(int)UI_Key.Filter_Artist] = "Artist";
            displayValues[(int)UI_Key.Filter_Album] = "Album";
            displayValues[(int)UI_Key.Filter_Genre] = "Genre";
            displayValues[(int)UI_Key.Filter_Year] = "Year";
            displayValues[(int)UI_Key.Filter_Grouping] = "Grouping";
            displayValues[(int)UI_Key.Filter_Any] = "Any {0}";
            displayValues[(int)UI_Key.Filter_No] = "No {0}";
            displayValues[(int)UI_Key.Filter_Search] = "[Search]";

            displayValues[(int)UI_Key.Filter_Index_Cancel] = "Cancel Setting Quick Index";
            displayValues[(int)UI_Key.Filter_Index_Clear] = "Clear Quick Index";
            displayValues[(int)UI_Key.Filter_Index_Caption] = "Quick Index";

            displayValues[(int)UI_Key.ToolTip_Filter_Button_Index] = "Show Quick Index";
            displayValues[(int)UI_Key.ToolTip_Filter_Button_Release] = "Right Click to Release";
            displayValues[(int)UI_Key.ToolTip_Filter_Textbox] = "Enter Search Words";
            displayValues[(int)UI_Key.ToolTip_ReleaseFilters] = "Release All Filters";
            displayValues[(int)UI_Key.ToolTip_ReleaseTextFilter] = "Clear Text Filter";

            displayValues[(int)UI_Key.ToolTip_To_Now_Playing] = "Switch To Now Playing";
            displayValues[(int)UI_Key.ToolTip_From_Now_Playing] = "Switch From Now Playing";
            displayValues[(int)UI_Key.ToolTip_Advance_View] = "Advance View";
            displayValues[(int)UI_Key.ToolTip_Mute] = "Mute";
            displayValues[(int)UI_Key.ToolTip_Shuffle] = "Shuffle Tracks";
            displayValues[(int)UI_Key.ToolTip_Repeat] = "Repeat";
            displayValues[(int)UI_Key.ToolTip_Previous_Track] = "Previous Track";
            displayValues[(int)UI_Key.ToolTip_Next_Track] = "Next Track";
            displayValues[(int)UI_Key.ToolTip_Play] = "Play";
            displayValues[(int)UI_Key.ToolTip_Pause] = "Pause";
            displayValues[(int)UI_Key.ToolTip_Stop] = "Stop";
            displayValues[(int)UI_Key.ToolTip_Radio] = "Radio";
            displayValues[(int)UI_Key.ToolTip_Volume] = "Adjust Volume";

            displayValues[(int)UI_Key.Equalizer_Flat] = "Flat";
            displayValues[(int)UI_Key.Equalizer_Loudness] = "Loudness";
            displayValues[(int)UI_Key.Equalizer_Bass_Boost] = "Bass Boost";
            displayValues[(int)UI_Key.Equalizer_Treble_Boost] = "Treble Boost";
            displayValues[(int)UI_Key.Equalizer_Vocal] = "Vocal";
            displayValues[(int)UI_Key.Equalizer_Eq_Off] = "EQ Off";
            displayValues[(int)UI_Key.Equalizer_Eq_On] = "EQ On";
            displayValues[(int)UI_Key.Equalizer_Lock] = "Lock";
            displayValues[(int)UI_Key.Equalizer_Fine_Control] = "Fine Control";
            displayValues[(int)UI_Key.Equalizer_All_Together] = "All Together";
            displayValues[(int)UI_Key.Equalizer_Expand] = "Expand";
            displayValues[(int)UI_Key.Equalizer_Compress] = "Compress";
            displayValues[(int)UI_Key.Equalizer_Reset] = "Reset";
            displayValues[(int)UI_Key.Equalizer_Remove] = "Remove";
            displayValues[(int)UI_Key.Equalizer_New] = "New";
            displayValues[(int)UI_Key.Equalizer_Bands] = "{0} Bands";
            displayValues[(int)UI_Key.Equalizer_Dont_Change] = "Don't Change";
            displayValues[(int)UI_Key.Equalizer_Turn_Off] = "Equalizer Off";

            displayValues[(int)UI_Key.Go_Pro_Instructions] = "To find out more about {0} Pro Edition, click the 'Learn More' Button.";
            displayValues[(int)UI_Key.Go_Pro_Learn_More] = "Learn More";

            displayValues[(int)UI_Key.Find_File_Instructions] = "The following file was not found. Use the file selector to tell {0} where the file is. {0} can also update other missing files with this information if you choose.";
            displayValues[(int)UI_Key.Find_File_This_Track_Only] = "Update This Track Only";
            displayValues[(int)UI_Key.Find_File_All_Tracks] = "Update All Missing Tracks";
            displayValues[(int)UI_Key.Find_File_Update_Path] = "Update File Path: {0}";
            displayValues[(int)UI_Key.Find_File_Illegal_Path] = "Illegal File Path";
            displayValues[(int)UI_Key.Find_File_Illegal_Path_Title] = "Illegal Path";
            displayValues[(int)UI_Key.Find_File_File_Not_Found] = "The path {0} does not exist.";
            displayValues[(int)UI_Key.Find_File_File_Not_Found_Title] = "File Not Found";
            displayValues[(int)UI_Key.Find_File_File_Already_Exists] = "The file {0} already exists in the library.";
            displayValues[(int)UI_Key.Find_File_File_Already_Exists_Title] = "File Already Exists";
            displayValues[(int)UI_Key.Find_File_File_Error] = "Error opening file.";
            displayValues[(int)UI_Key.Find_File_File_Error_Title] = "Audio File Error";
            displayValues[(int)UI_Key.Find_File_Different_Information] = "The track you selected has different information than the missing track. Do you wish to continue?{6}{6}Old Track: {0} - {1} - {2}{6}{6}New Track: {3} - {4} - {5}";
            displayValues[(int)UI_Key.Find_File_Different_Information_Title] = "Different Track Information";
            displayValues[(int)UI_Key.Find_File_File_Does_Not_Exist] = "The file you selected does not exist";
            displayValues[(int)UI_Key.Find_File_File_Does_Not_Exist_Title] = "File not found";
            displayValues[(int)UI_Key.Find_File_Message_Updating] = "Updating {0}: {1}";
            displayValues[(int)UI_Key.Find_File_Find] = "Find...";

            displayValues[(int)UI_Key.Gamepad_Help_Title] = "Gamepad Help";
            displayValues[(int)UI_Key.Gamepad_Help_Instructions] = "Many buttons have a main and an alternate function. Press a button quickly to invoke the main function] = or hold the button down to invoke the alternate function. Alternatively] = hover the mouse pointer over a button, or hold down the mouse button while hovering to see the alternate functions.";
            displayValues[(int)UI_Key.Gamepad_Help_Exit] = "Exit Help";
            displayValues[(int)UI_Key.Gamepad_Help_Get_Gamepad] = "Get a Wireless Gamepad...";
            displayValues[(int)UI_Key.Gamepad_Help_Press_Button] = "Press a gamepad button...";
            displayValues[(int)UI_Key.Gamepad_Help_No_Gamepad] = "No gamepad detected.";
            displayValues[(int)UI_Key.Gamepad_Help_No_Gamepad_Title] = "No Gamepad";

            displayValues[(int)UI_Key.Sleep_Shutdown_Computer] = "Shutdown Computer";
            displayValues[(int)UI_Key.Sleep_Exit_App] = "Exit QuuxPlayer";
            displayValues[(int)UI_Key.Sleep_Computer_StandBy] = "Computer Standby";
            displayValues[(int)UI_Key.Sleep_Computer_Hibernate] = "Hibernate";
            displayValues[(int)UI_Key.Sleep_Shutdown] = "Computer Shutdown in ";
            displayValues[(int)UI_Key.Sleep_Exit] = "Exit in ";
            displayValues[(int)UI_Key.Sleep_Hibernate] = "Computer Hibernate in ";
            displayValues[(int)UI_Key.Sleep_Standby] = "Standby in ";
            displayValues[(int)UI_Key.Sleep_Minute] = "minute";
            displayValues[(int)UI_Key.Sleep_Minutes] = "minutes";
            displayValues[(int)UI_Key.Sleep_Off] = "Off";
            displayValues[(int)UI_Key.Sleep_Title] = "Sleep";
            displayValues[(int)UI_Key.Sleep_Instructions] = "You can set {0} to automatically exit or shut down your PC after a set time.";
            displayValues[(int)UI_Key.Sleep_Play_For_Another] = "Play for another";
            displayValues[(int)UI_Key.Sleep_And_Then] = "And then";
            displayValues[(int)UI_Key.Sleep_Gradually_Reduce_Volume_After] = "Gradually reduce volume after";
            displayValues[(int)UI_Key.Sleep_Force_Shutdown] = "Force shutdown";
            displayValues[(int)UI_Key.Sleep_Force] = "Force";
            displayValues[(int)UI_Key.Sleep_Force_Hibernate] = "Force Hibernate";
            displayValues[(int)UI_Key.Sleep_Force_StandBy] = "Force Computer Standby";

            displayValues[(int)UI_Key.Auto_Monitor_Title] = "Auto Monitor Folders";
            displayValues[(int)UI_Key.Auto_Monitor_Instructions] = "Here you can specify Windows folders that {0} should monitor to find new tracks. Your library will be updated automatically when new tracks are found or changes are detected.";
            displayValues[(int)UI_Key.Auto_Monitor_Add_Folder] = "Add Folder...";
            displayValues[(int)UI_Key.Auto_Monitor_Remove_Folder] = "Remove Folder";
            displayValues[(int)UI_Key.Auto_Monitor_Select_Folder] = "Select a Folder for {0} to Monitor:";

            displayValues[(int)UI_Key.Lyrics_Credits] = "Lyrics provided courtesy {0}";

            displayValues[(int)UI_Key.Menu_File] = "&File";
            displayValues[(int)UI_Key.Menu_File_Add_Folder] = "&Add Folder To Library...";
            displayValues[(int)UI_Key.Menu_File_Update_File_Paths] = "Update File Paths...";
            displayValues[(int)UI_Key.Menu_File_Add_File] = "Add &File to Library...";
            displayValues[(int)UI_Key.Menu_File_Auto_Monitor] = "Auto &Monitor Folders...";
            displayValues[(int)UI_Key.Menu_File_Library_Maintenance] = "L&ibrary Maintenance";
            displayValues[(int)UI_Key.Menu_File_Set_File_Associations] = "Set &Windows Audio File Associations...";
            displayValues[(int)UI_Key.Menu_File_Refresh_Tracks] = "Re&fresh Tracks";
            displayValues[(int)UI_Key.Menu_File_Refresh_All_Tracks] = "All Tracks in Library";
            displayValues[(int)UI_Key.Menu_File_Refresh_Selected_Tracks] = "Selected Tracks";
            displayValues[(int)UI_Key.Menu_File_Remove_Ghost_Tracks] = "&Remove Ghost Tracks...";
            displayValues[(int)UI_Key.Menu_File_Suggest_Delete_Duplicates] = "&Suggest Duplicate Deletion";
            displayValues[(int)UI_Key.Menu_File_Suggest_Delete_Duplicates_Keep_High_BitRate] = "Keep Highest &Bitrate...";
            displayValues[(int)UI_Key.Menu_File_Suggest_Delete_Duplicates_Keep_Oldest] = "Keep &Oldest Files...";
            displayValues[(int)UI_Key.Menu_File_Suggest_Delete_Duplicates_Keep_Newest] = "Keep &Newest Files...";
            displayValues[(int)UI_Key.Menu_File_Suggest_Delete_Duplicates_Keep_Highest_Rated] = "Keep Highest &Rated...";
            displayValues[(int)UI_Key.Menu_File_Clear_Database] = "&Clear Database...";
            displayValues[(int)UI_Key.Menu_File_Podcasts] = "Podcasts...";
            displayValues[(int)UI_Key.Menu_File_Organize_Library] = "&Organize Library...";
            displayValues[(int)UI_Key.Menu_File_Import_Playlist] = "&Import Playlist...";
            displayValues[(int)UI_Key.Menu_File_Export_View_As_Playlist] = "&Export Current View as Playlist...";
            displayValues[(int)UI_Key.Menu_File_Export_Current_View_As_Spreadsheet] = "Export Current View as &Spreadsheet...";
            displayValues[(int)UI_Key.Menu_File_Show_File_Details] = "Show File &Details...";
            displayValues[(int)UI_Key.Menu_File_Sleep] = "Slee&p...";
            displayValues[(int)UI_Key.Menu_File_Lock_Controls] = "&Lock Controls...";
            displayValues[(int)UI_Key.Menu_File_Exit] = "E&xit";

            displayValues[(int)UI_Key.Menu_Edit] = "&Edit";
            displayValues[(int)UI_Key.Menu_Edit_Select_All] = "&Select All";
            displayValues[(int)UI_Key.Menu_Edit_Select_None] = "Select &None";
            displayValues[(int)UI_Key.Menu_Edit_Invert_Selection] = "&Invert Selection";
            displayValues[(int)UI_Key.Menu_Edit_Set_Rating] = "Set &Rating for Selected Tracks";
            displayValues[(int)UI_Key.Menu_Edit_Equalizer] = "Select &Equalizer for Selected Tracks";
            displayValues[(int)UI_Key.Menu_Edit_Reset_Play_History] = "Reset Play &History for Selected Tracks...";
            displayValues[(int)UI_Key.Menu_Edit_Tags] = "Edit &Tags for Selected Tracks...";

            displayValues[(int)UI_Key.Menu_Play] = "&Play";
            displayValues[(int)UI_Key.Menu_Play_Play] = "Pla&y";
            displayValues[(int)UI_Key.Menu_Play_Pause] = "Pa&use";
            displayValues[(int)UI_Key.Menu_Play_Stop] = "&Stop";
            displayValues[(int)UI_Key.Menu_Play_Stop_After_This_Track] = "Stop &After This Track Ends";
            displayValues[(int)UI_Key.Menu_Play_Previous_Track] = "Play Pre&vious Track";
            displayValues[(int)UI_Key.Menu_Play_Next_Track] = "Play &Next Track";
            displayValues[(int)UI_Key.Menu_Play_Mute] = "&Mute Volume";
            displayValues[(int)UI_Key.Menu_Play_Repeat] = "&Repeat Mode";
            displayValues[(int)UI_Key.Menu_Play_Volume_Up] = "Volume &Up";
            displayValues[(int)UI_Key.Menu_Play_Volume_Down] = "Volume &Down";
            displayValues[(int)UI_Key.Menu_Play_Scan_Fwd] = "Scan &Forward";
            displayValues[(int)UI_Key.Menu_Play_Scan_Back] = "Scan &Backward";
            displayValues[(int)UI_Key.Menu_Play_This_Album] = "Play This Album Now";
            displayValues[(int)UI_Key.Menu_Play_Add_This_Album_To_Now_Playing] = "Add This Album To Now Playing";
            displayValues[(int)UI_Key.Menu_Play_Random_Album] = "Play Random Album";
            displayValues[(int)UI_Key.Menu_Play_Shuffle_Tracks] = "S&huffle Tracks";
            displayValues[(int)UI_Key.Menu_Play_Play_Selected_Track_Next] = "Play Selected Track Next";
            displayValues[(int)UI_Key.Menu_Play_Add_To_Now_Playing] = "Add Selected Tracks To &Now Playing";

            displayValues[(int)UI_Key.Menu_Playlists] = "Play&lists";
            displayValues[(int)UI_Key.Menu_Playlist_Add_Playlist] = "&Add New Playlist...";
            displayValues[(int)UI_Key.Menu_Playlist_Remove_Playlist] = "&Remove Playlist";
            displayValues[(int)UI_Key.Menu_Playlist_Add_Selected_Tracks_To] = "Add &Selected Tracks To";
            displayValues[(int)UI_Key.Menu_Playlist_Remove_Selected_Tracks_From] = "Remove Selected Tracks &From Playlist";
            displayValues[(int)UI_Key.Menu_Playlist_Switch_To_Now_Playing] = "Switch To / From N&ow Playing";
            displayValues[(int)UI_Key.Menu_Playlist_Clear_Now_Playing] = "Clear No&w Playing";
            displayValues[(int)UI_Key.Menu_Playlist_Edit_Auto_Playlist] = "Edit Auto Playlist...";
            displayValues[(int)UI_Key.Menu_Playlist_Convert_To_Standard_Playlist] = "Convert to Standard Playlist";
            displayValues[(int)UI_Key.Menu_Playlist_Convert_To_Auto_Playlist] = "Convert to Auto Playlist...";
            displayValues[(int)UI_Key.Menu_Playlist_Rename_Selected_Playlist] = "Rename Selected Playlist";
            displayValues[(int)UI_Key.Menu_Playlist_Add_Tracks_To_Playlist] = "Add Tracks to Playlist '{0}'";
            displayValues[(int)UI_Key.Menu_Playlist_Add_Tracks_To] = "Add Tracks to...";

            displayValues[(int)UI_Key.Menu_Filters] = "F&ilters";
            displayValues[(int)UI_Key.Menu_Filters_Select_Next_Filter] = "Select Next Filter";
            displayValues[(int)UI_Key.Menu_Filters_Select_Previous_Filter] = "Select Previous Filter";
            displayValues[(int)UI_Key.Menu_Filters_Release_Selected_Filter] = "Release Selected Filter";
            displayValues[(int)UI_Key.Menu_Filters_Release_All_Filters] = "Release All Filters";
            displayValues[(int)UI_Key.Menu_Filters_Show_Filter_Index] = "Show Filter Quick Index";
            displayValues[(int)UI_Key.Menu_Filters_Select_Filter] = "Select Filter";
            displayValues[(int)UI_Key.Menu_Filters_Select_Filter_Playlists] = "Playlists";
            displayValues[(int)UI_Key.Menu_Filters_Select_Filter_Artists] = "Artists";
            displayValues[(int)UI_Key.Menu_Filters_Select_Filter_Albums] = "Albums";
            displayValues[(int)UI_Key.Menu_Filters_Select_Filter_Genres] = "Genres";
            displayValues[(int)UI_Key.Menu_Filters_Select_Filter_Years] = "Years";
            displayValues[(int)UI_Key.Menu_Filters_Select_Filter_Groupings] = "Groupings";

            displayValues[(int)UI_Key.Menu_View] = "&View";
            displayValues[(int)UI_Key.Menu_View_Show_Full_Screen] = "Show Full Screen";
            displayValues[(int)UI_Key.Menu_View_Find_Currently_Playing_Track] = "Find Currently Playing Track";
            displayValues[(int)UI_Key.Menu_View_Show_All_Of] = "Show All Of...";
            displayValues[(int)UI_Key.Menu_View_This_Artist] = "This Artist";
            displayValues[(int)UI_Key.Menu_View_This_Album] = "This Album";
            displayValues[(int)UI_Key.Menu_View_This_Genre] = "This Genre";
            displayValues[(int)UI_Key.Menu_View_This_Year] = "This Year";
            displayValues[(int)UI_Key.Menu_View_This_Grouping] = "This Grouping";
            displayValues[(int)UI_Key.Menu_View_Show_Columns] = "Show Columns";
            displayValues[(int)UI_Key.Menu_View_Reset_Columns] = "Reset Columns";
            displayValues[(int)UI_Key.Menu_View_HTPC_Mode] = "&HTPC Mode";
            displayValues[(int)UI_Key.Menu_View_Tag_Cloud] = "Show Tag Cloud";
            displayValues[(int)UI_Key.Menu_View_Advance_View] = "&Advance View";
            displayValues[(int)UI_Key.Menu_View_Album_Art_On_Main_Screen] = "Show Album Art on Main Screen";

            displayValues[(int)UI_Key.Menu_Options] = "&Options";
            displayValues[(int)UI_Key.Menu_View_Use_Mini_Player] = "Use Mini-Player When Minimized";
            displayValues[(int)UI_Key.Menu_View_Show_Mini_Player] = "Show Mini-Player Now";
            displayValues[(int)UI_Key.Menu_Options_Detect_Gamepad] = "&Detect Gamepad...";
            displayValues[(int)UI_Key.Menu_Options_Decoder_Gain] = "Decoder &Gain";
            displayValues[(int)UI_Key.Menu_Options_Replay_Gain] = "Volume Leveling (Replay Gain)";
            displayValues[(int)UI_Key.Menu_Options_Replay_Gain_Album] = "By Album";
            displayValues[(int)UI_Key.Menu_Options_Replay_Gain_Track] = "By Track";
            displayValues[(int)UI_Key.Menu_Options_Replay_Gain_Off] = "Off";
            displayValues[(int)UI_Key.Menu_Options_Replay_Gain_Analyze_Selected_Tracks] = "Analyze Selected Tracks...";
            displayValues[(int)UI_Key.Menu_Options_Replay_Gain_Analyze_Cancel] = "Cancel Analysis";
            displayValues[(int)UI_Key.Menu_Options_Replay_Gain_Write_Tags] = "Save Replay Gain Tags in Files After Analysis";
            displayValues[(int)UI_Key.Menu_Options_Auto_Clipping_Control] = "Auto Clipping Control";
            displayValues[(int)UI_Key.Menu_Options_Use_Equalizer] = "Use Equalizer";
            displayValues[(int)UI_Key.Menu_Options_Select_Next_Equalizer_Setting] = "Select Next Equalizer Setting";
            displayValues[(int)UI_Key.Menu_Options_Equalizer_Settings] = "Equalizer Settings...";
            displayValues[(int)UI_Key.Menu_Options_Spectrum_View_Gain] = "Spectrum View Gain";
            displayValues[(int)UI_Key.Menu_Options_Spectrum_View_Gain_Up] = "Up";
            displayValues[(int)UI_Key.Menu_Options_Spectrum_View_Gain_Down] = "Down";
            displayValues[(int)UI_Key.Menu_Options_High_Resolution_Spectrum_Analyzer] = "&High Resolution Spectrum Analyzer";
            displayValues[(int)UI_Key.Menu_Options_Twitter] = "Twitter Options...";
            displayValues[(int)UI_Key.Menu_Options_Last_FM] = "Last.fm Options...";
            displayValues[(int)UI_Key.Menu_Options_More_Options] = "More Options...";

            displayValues[(int)UI_Key.Menu_Internet] = "&Internet";
            displayValues[(int)UI_Key.Menu_Internet_Radio] = "Internet &Radio";
            displayValues[(int)UI_Key.Menu_Internet_Radio_Reload] = "Reload Default Station List...";
            displayValues[(int)UI_Key.Menu_Internet_Download_Cover_Art] = "Do&wnload Missing Cover Art From Internet";
            displayValues[(int)UI_Key.Menu_Internet_Artist_Info] = "Artist Info From last.fm...";
            displayValues[(int)UI_Key.Menu_Internet_Album_Info] = "Album Info From last.fm...";
            displayValues[(int)UI_Key.Menu_Internet_Show_Track_And_Album_Details] = "Show Lyrics and Album Info...";
            
            displayValues[(int)UI_Key.Menu_Help] = "&Help";
            displayValues[(int)UI_Key.Menu_Help_Online_Help] = "Online Help...";
            displayValues[(int)UI_Key.Menu_Help_Gamepad_Help] = "Show Gamepad Help...";
            displayValues[(int)UI_Key.Menu_Help_Other_Languages] = "Other Language Info (on Web)...";
            displayValues[(int)UI_Key.Menu_Help_Check_For_Update] = "Check for Update...";
            displayValues[(int)UI_Key.Menu_Help_Visit_Company_Website] = "Visit {0} Web Site...";
            displayValues[(int)UI_Key.Menu_Help_About] = "&About {0}...";

            displayValues[(int)UI_Key.Mini_Player_Radio] = "Radio";
            displayValues[(int)UI_Key.Mini_Player_Mute] = "Mute";

            displayValues[(int)UI_Key.General_Copyright] = "© {0} {1}. All rights reserved. This program may only be copied or distributed in accordance with the included license agreement which must be included with any redistribution. {2} is a trademark of {1}. Twitter integration by permission of Twitter.com. Last.fm integration by permission of last.fm. Decoding components are used under license from Un4seen Developments, all rights reserved.";
            displayValues[(int)UI_Key.General_Playlist] = "Playlist";
            displayValues[(int)UI_Key.General_Connection_Error] = "Connection Error";
            displayValues[(int)UI_Key.General_No_Genre] = "none";
            displayValues[(int)UI_Key.General_Now_Playing] = "Now Playing";
            displayValues[(int)UI_Key.General_Duplicates] = "Duplicates";
            displayValues[(int)UI_Key.General_Ghosts] = "Ghosts";
            displayValues[(int)UI_Key.General_My_Playlist] = "My Playlist";
            displayValues[(int)UI_Key.General_Also_Recycle] = "Also &recycle files to Windows recycle bin.";
            displayValues[(int)UI_Key.General_Reset_Play_History] = "Reset play history for selected tracks? This will reset the play count and date last played information.";
            displayValues[(int)UI_Key.General_Reset_Play_History_Title] = "Reset Play History?";
            displayValues[(int)UI_Key.General_Exit] = "Exit";

            displayValues[(int)UI_Key.Message_Is_System_Playlist] = "'{0}' is a system playlist";
            displayValues[(int)UI_Key.Message_Is_Standard_Playlist] = "'{0}' is a standard playlist";
            displayValues[(int)UI_Key.Message_Is_Auto_Playlist] = "'{0}' is an automatic playlist";
            displayValues[(int)UI_Key.Message_Converted_To_Standard] = "'{0}' converted to a standard playlist";
            displayValues[(int)UI_Key.Message_Track_Added_To_Playlist] = "\"{0}\" added to {1}";
            displayValues[(int)UI_Key.Message_Tracks_Added_To_Playlist] = "{0} tracks added to {1}";
            displayValues[(int)UI_Key.Message_Track_Next_Up] = "Next Up: {0}";
            displayValues[(int)UI_Key.Message_Volume] = "Volume: {0}";
            displayValues[(int)UI_Key.Message_Removed_Ghost_Tracks] = "Removed {0} ghost tracks from library.";
            displayValues[(int)UI_Key.Message_Rewind_To_Start] = "Rewind to Start";
            displayValues[(int)UI_Key.Message_Play_Selected_Tracks] = "Play Selected Tracks";
            displayValues[(int)UI_Key.Message_Track_Not_Available] = "'{0} - {1}' is not availble";
            displayValues[(int)UI_Key.Message_Play_Previous_Track] = "Play Previous Track - {0}";
            displayValues[(int)UI_Key.Message_Play_Next_Track] = "Play Next Track - {0}";
            displayValues[(int)UI_Key.Message_Track_Failed] = "Track Failed to Play: {0}";
            displayValues[(int)UI_Key.Message_Repeat] = "Repeat";
            displayValues[(int)UI_Key.Message_No_Repeat] = "No Repeat";
            displayValues[(int)UI_Key.Message_Equalizer_On] = "Equalizer On";
            displayValues[(int)UI_Key.Message_Equalizer_Off] = "Equalizer Off";
            displayValues[(int)UI_Key.Message_Selected_Equalizer_Setting] = "Selected Equalizer Setting: {0}";
            displayValues[(int)UI_Key.Message_Scan_Backward] = "Scan Backward";
            displayValues[(int)UI_Key.Message_Scan_Forward] = "Scan Forward";
            displayValues[(int)UI_Key.Message_Play_Random_Album] = "Play Random Album";
            displayValues[(int)UI_Key.Message_Play_This_Album] = "Play Album";
            displayValues[(int)UI_Key.Message_Normal_Mode] = "Normal Mode";
            displayValues[(int)UI_Key.Message_HTPC_Mode] = "HTPC Mode";
            displayValues[(int)UI_Key.Message_Viewing_Now_Playing] = "Viewing Now Playing";
            displayValues[(int)UI_Key.Message_Restoring_View] = "Restoring View";
            displayValues[(int)UI_Key.Message_Advance_View] = "Advance View";
            displayValues[(int)UI_Key.Message_Mute] = "Mute";
            displayValues[(int)UI_Key.Message_Unmute] = "Unmute";
            displayValues[(int)UI_Key.Message_Spectrum_Gain] = "Spectrum Gain: {0}%";
            displayValues[(int)UI_Key.Message_Show_Previous_Filter] = "Show Previous Filter";
            displayValues[(int)UI_Key.Message_Show_Next_Filter] = "Show Next Filter";
            displayValues[(int)UI_Key.Message_Release_Current_Filter] = "Release Current Filter";
            displayValues[(int)UI_Key.Message_Release_All_Filters] = "Release All Filters";
            displayValues[(int)UI_Key.Message_Resume] = "Resume";
            displayValues[(int)UI_Key.Message_Pause] = "Pause";
            displayValues[(int)UI_Key.Message_Stop] = "Stop";
            displayValues[(int)UI_Key.Message_Stop_After_This_Track] = "Stopping After This Track Ends";
            displayValues[(int)UI_Key.Message_Cancel_Stop_After_This_Track] = "Cancelled Stop After This Track Ends";
            displayValues[(int)UI_Key.Message_Shuffle] = "Shuffle Tracks";
            displayValues[(int)UI_Key.Message_Advance_Sort_Column] = "Advance Sort Column";
            displayValues[(int)UI_Key.Message_Now_Playing] = "Now Playing: {0}";
            displayValues[(int)UI_Key.Message_Add_This_Album_To_Now_Playing] = "Added '{0}' To Now Playing";
            displayValues[(int)UI_Key.Message_No_Index_Items_Available] = "No Index Items Available";
            displayValues[(int)UI_Key.Message_Show_Playing_Track] = "Show Currently Playing Track";

            displayValues[(int)UI_Key.Control_Panel_Mute] = "Mute";
            displayValues[(int)UI_Key.Control_Panel_Volume] = "Vol: {0} / EQ Off";
            displayValues[(int)UI_Key.Control_Panel_Vol_With_Eq] = "Vol: {0} / EQ On ({1})";
            displayValues[(int)UI_Key.Control_Panel_Decoder_Gain] = "Decoder Gain: {0}dB";
            displayValues[(int)UI_Key.Control_Panel_Decoder_Gain_And_Replay_Gain] = "Decoder Gain: {0}dB / Replay Gain: {1}dB";
            displayValues[(int)UI_Key.Control_Panel_Clipping] = "Clipping";
            displayValues[(int)UI_Key.Control_Panel_Next_Up] = "Next Up: {0}";
            displayValues[(int)UI_Key.Control_Panel_Next_Up_None] = "Next Up: None";
            displayValues[(int)UI_Key.Control_Panel_Station_Info] = "Streaming {0} kbps - {1}";
            displayValues[(int)UI_Key.Control_Panel_Station_Count] = "{0} Stations";
            displayValues[(int)UI_Key.Control_Panel_Station_Count_Singular] = "{0} Station";

            displayValues[(int)UI_Key.Lock_Title] = "Lock Controls";
            displayValues[(int)UI_Key.Lock_Instructions] = "Locking {0} will disable all controls except for 'Advance Screen' (Space Bar). You can optionally set a code to prevent unauthorized unlocking.";
            displayValues[(int)UI_Key.Lock_Checkbox] = "Lock Controls";
            displayValues[(int)UI_Key.Lock_Code] = "Protect with Code:";
            displayValues[(int)UI_Key.Lock_Gamepad] = "Lock Gamepad Controls";
            displayValues[(int)UI_Key.Rating_Zero] = "Zero";

            displayValues[(int)UI_Key.Export_CSV_Title] = "Enter a File Name For CSV File";
            displayValues[(int)UI_Key.Export_CSV_Filter] = "CSV / Excel Files|*.csv|All Files|*.*";
            displayValues[(int)UI_Key.Export_CSV_Default_Filename] = "Track List.csv";
            displayValues[(int)UI_Key.Export_Playlist_Filter] = "M3U Files (*.m3u)|*.m3u|All Files|*.*";
            displayValues[(int)UI_Key.Export_Playlist_Title] = "Enter a File Name For M3U File";
            displayValues[(int)UI_Key.Import_Playlist_Filter] = "M3U Files (*.m3u)|*.m3u|PLS Files (*.pls)|*.pls|All Playlist Files (*.m3u;*.pls)|*.m3u;*.pls";
            displayValues[(int)UI_Key.Import_Playlist_Title] = "Select an M3U File";

            displayValues[(int)UI_Key.Options_Title] = "{0} Options";
            displayValues[(int)UI_Key.Options_Label_Sound] = "Sound";
            displayValues[(int)UI_Key.Options_Label_Display] = "Display";
            displayValues[(int)UI_Key.Options_Label_Internet] = "Internet";
            displayValues[(int)UI_Key.Options_Label_Other] = "Other Options";
            displayValues[(int)UI_Key.Options_Spectrum_Show_Grid] = "Show grid on spectrum analyzer";
            displayValues[(int)UI_Key.Options_Download_Cover_Art] = "Download cover art from Internet";
            displayValues[(int)UI_Key.Options_Auto_Clipping_Control] = "Use automatic clipping control";
            displayValues[(int)UI_Key.Options_Auto_Check_Updates] = "Automatically check online for updates";
            displayValues[(int)UI_Key.Options_Use_Global_Hotkeys] = "Use global multimedia hotkeys";
            displayValues[(int)UI_Key.Options_Include_Tag_Cloud] = "Include 'Tag Cloud' view in screen sequence";
            displayValues[(int)UI_Key.Options_Dont_Load_Shorter_Than] = "Don't load tracks shorter than";
            displayValues[(int)UI_Key.Options_Dont_Load_Seconds] = "seconds";
            displayValues[(int)UI_Key.Options_Save_Art_Caption] = "Save downloaded cover art:";
            displayValues[(int)UI_Key.Options_Save_Art_Folder_JPG] = "As \"folder.jpg\"";
            displayValues[(int)UI_Key.Options_Save_Art_Artist_Album] = "As \"<artist> - <album>.jpg\"";
            displayValues[(int)UI_Key.Options_Save_Art_None] = "Don't Save";
            displayValues[(int)UI_Key.Options_Disable_Screensaver] = "Disable Windows screensavers";
            displayValues[(int)UI_Key.Options_Volume_Controls_Windows_Volume] = "Volume controls Windows Volume setting";

            displayValues[(int)UI_Key.Track_Limit_Instructions] = "To add more than {0} tracks to your library, you'll need {1} Pro Edition. Click \"Learn More\" for details.";
            displayValues[(int)UI_Key.Track_Limit_Learn_More] = "Learn More...";

            displayValues[(int)UI_Key.Filter_Value_List_Play] = "Play";
            displayValues[(int)UI_Key.Filter_Value_List_Add_Playlist] = "Add New Playlist...";
            displayValues[(int)UI_Key.Filter_Value_List_Rename_Playlist] = "Rename Playlist";
            displayValues[(int)UI_Key.Filter_Value_List_Remove_Playlist] = "Remove Playlist...";
            displayValues[(int)UI_Key.Filter_Value_List_Remove_Playlist_Dialog] = "Are you sure you want to remove the playlist '{0}'?";
            displayValues[(int)UI_Key.Filter_Value_List_Remove_Playlist_Dialog_Title] = "Remove Playlist?";
            displayValues[(int)UI_Key.Filter_Value_List_Edit_Auto_Playlist] = "Edit Auto Playlist...";
            displayValues[(int)UI_Key.Filter_Value_List_Convert_To_Auto_Playlist] = "Convert to Auto Playlist...";
            displayValues[(int)UI_Key.Filter_Value_List_Convert_To_Standard_Playlist] = "Convert to Standard Playlist";
            displayValues[(int)UI_Key.Filter_Value_List_Export_Playlist] = "Export as m3u file...";
            displayValues[(int)UI_Key.Filter_Value_List_Export_Playlist_Pro_Dialog] = "You can export this playlist in standard playlist (m3u) format. This feature is available only in {0} Pro Edition.";
            displayValues[(int)UI_Key.Filter_Value_List_Export_Playlist_Pro_Dialog_Title] = "Export As Playlist - {0} Pro Edition";
            displayValues[(int)UI_Key.Filter_Value_List_Release_Filter] = "Release Filter";
            displayValues[(int)UI_Key.Filter_Value_List_Any] = "<Any {0}>";

            displayValues[(int)UI_Key.Edit_Auto_Playlist_Instructions] = "Enter a playlist definition; for example: Album Contains \"Greatest Hits\"";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Title] = "Edit Auto Playlist";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Save] = "Save";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Test] = "Test";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Help] = "Help (on Web)";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Save_As_Standard] = "Save As Standard";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Valid] = "Valid.";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Bad_Comparitor] = "Invalid: Bad Comparitor";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Bad_Field] = "Invalid: Bad Field";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Playlist_Error] = "Invalid: \"Track\" uses is[not]containedin";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Bad_Expression] = "Invalid: Bad Expression";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Unmatched_Parens] = "Invalid: Unmatched Parentheses";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Short_Expression] = "Invalid: Expressions require three terms.";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Unknown_Keyword] = "Invalid: Unknown Keyword";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Boolean_Value_Error] = "Invalid: Field compares to 'true' or 'false'";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Numeric_Value_Error] = "Invalid: Field compares to a numeric value";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Numeric_Comparitor_Error] = "Invalid: Need a numeric comparator";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Bad_SelectBy] = "Invalid: Bad SelectBy Modifier";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Bad_LimitTo] = "Invalid: Bad LimitTo Modifier";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Bad_SortBy] = "Invalid: Bad SortBy Modifier";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Bad_ThenBy] = "Invalid: Bad ThenBy Modifier";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_ThenBy_Without_SortBy_Or_SelectBy] = "Invalid: ThenBy without SortBy or SelectBy";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Bad_Modifier] = "Invalid: Bad Modifier";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Invalid] = "Invalid.";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Invalid_Dialog] = "This expression is invalid. Click Cancel to return to editing the expression, or OK to save the expression. The playlist will remain a standard playlist until the expression is edited and made valid.";
            displayValues[(int)UI_Key.Edit_Auto_Playlist_Invalid_Dialog_Title] = "Save as Standard?";

            displayValues[(int)UI_Key.Track_List_Column_Artist] = "Artist";
            displayValues[(int)UI_Key.Track_List_Column_Artist_Short] = "Artist";
            displayValues[(int)UI_Key.Track_List_Column_Album_Artist] = "Album Artist";
            displayValues[(int)UI_Key.Track_List_Column_Album_Artist_Short] = "Album Artist";
            displayValues[(int)UI_Key.Track_List_Column_Album] = "Album";
            displayValues[(int)UI_Key.Track_List_Column_Album_Short] = "Album";
            displayValues[(int)UI_Key.Track_List_Column_Title] = "Title";
            displayValues[(int)UI_Key.Track_List_Column_Title_Short] = "Title";
            displayValues[(int)UI_Key.Track_List_Column_Genre] = "Genre";
            displayValues[(int)UI_Key.Track_List_Column_Genre_Short] = "Genre";
            displayValues[(int)UI_Key.Track_List_Column_Composer] = "Composer";
            displayValues[(int)UI_Key.Track_List_Column_Composer_Short] = "Composer";
            displayValues[(int)UI_Key.Track_List_Column_Grouping] = "Grouping";
            displayValues[(int)UI_Key.Track_List_Column_Grouping_Short] = "Grouping";
            displayValues[(int)UI_Key.Track_List_Column_TrackNum] = "Track Number";
            displayValues[(int)UI_Key.Track_List_Column_TrackNum_Short] = "Trk";
            displayValues[(int)UI_Key.Track_List_Column_DiskNum] = "Disk Number";
            displayValues[(int)UI_Key.Track_List_Column_DiskNum_Short] = "Dsk";
            displayValues[(int)UI_Key.Track_List_Column_Length] = "Length";
            displayValues[(int)UI_Key.Track_List_Column_Length_Short] = "Len";
            displayValues[(int)UI_Key.Track_List_Column_Year] = "Year";
            displayValues[(int)UI_Key.Track_List_Column_Year_Short] = "Year";
            displayValues[(int)UI_Key.Track_List_Column_Bitrate] = "Bit Rate";
            displayValues[(int)UI_Key.Track_List_Column_Bitrate_Short] = "Bit Rate";
            displayValues[(int)UI_Key.Track_List_Column_Size] = "Size";
            displayValues[(int)UI_Key.Track_List_Column_Size_Short] = "Size";
            displayValues[(int)UI_Key.Track_List_Column_File_Date] = "File Date";
            displayValues[(int)UI_Key.Track_List_Column_File_Date_Short] = "Date";
            displayValues[(int)UI_Key.Track_List_Column_Days_Since_Last_Played] = "Days Since Last Played";
            displayValues[(int)UI_Key.Track_List_Column_Days_Since_Last_Played_Short] = "DSLP";
            displayValues[(int)UI_Key.Track_List_Column_Days_Since_Added] = "Days Since File Added";
            displayValues[(int)UI_Key.Track_List_Column_Days_Since_Added_Short] = "Days Since Added";
            displayValues[(int)UI_Key.Track_List_Column_Play_Count] = "Play Count";
            displayValues[(int)UI_Key.Track_List_Column_Play_Count_Short] = "Plays";
            displayValues[(int)UI_Key.Track_List_Column_Rating] = "Rating";
            displayValues[(int)UI_Key.Track_List_Column_Rating_Short] = "Rating";
            displayValues[(int)UI_Key.Track_List_Column_Encoder] = "Encoder";
            displayValues[(int)UI_Key.Track_List_Column_Encoder_Short] = "Encoder";
            displayValues[(int)UI_Key.Track_List_Column_File_Type] = "File Type";
            displayValues[(int)UI_Key.Track_List_Column_File_Type_Short] = "Type";
            displayValues[(int)UI_Key.Track_List_Column_Num_Channels] = "Channels";
            displayValues[(int)UI_Key.Track_List_Column_Num_Channels_Short] = "Chan";
            displayValues[(int)UI_Key.Track_List_Column_Sample_Freq] = "Sample Frequency";
            displayValues[(int)UI_Key.Track_List_Column_Sample_Freq_Short] = "Freq";
            displayValues[(int)UI_Key.Track_List_Column_File_Name] = "File Name";
            displayValues[(int)UI_Key.Track_List_Column_File_Name_Short] = "File Name";
            displayValues[(int)UI_Key.Track_List_Column_Equalizer] = "Equalizer";
            displayValues[(int)UI_Key.Track_List_Column_Equalizer_Short] = "Equalizer";
            displayValues[(int)UI_Key.Track_List_Column_Replay_Gain] = "Replay Gain";
            displayValues[(int)UI_Key.Track_List_Column_Replay_Gain_Short] = "Gain";
            displayValues[(int)UI_Key.Track_List_No_Sort] = "No Sort Column Selected.";
            displayValues[(int)UI_Key.Track_List_Play] = "Play";
            displayValues[(int)UI_Key.Track_List_Play_This_Album] = "Play This Album";
            displayValues[(int)UI_Key.Track_List_Play_Next] = "Play This Track Next";
            displayValues[(int)UI_Key.Track_List_Show_All_Of] = "Show All Of";
            displayValues[(int)UI_Key.Track_List_This_Artist] = "This Artist";
            displayValues[(int)UI_Key.Track_List_This_Album] = "This Album";
            displayValues[(int)UI_Key.Track_List_This_Genre] = "This Genre";
            displayValues[(int)UI_Key.Track_List_This_Year] = "This Year";
            displayValues[(int)UI_Key.Track_List_This_Grouping] = "This Grouping";
            displayValues[(int)UI_Key.Track_List_Release_All_Filters] = "Release All Filters";
            displayValues[(int)UI_Key.Track_List_Show_Columns] = "Show Columns";
            displayValues[(int)UI_Key.Track_List_Reset_Columns] = "Reset Columns";
            displayValues[(int)UI_Key.Track_List_Show_In_Windows_Explorer] = "Show in Windows Explorer...";
            displayValues[(int)UI_Key.Track_List_Add_To_Playlist] = "Add to Playlist '{0}'";
            displayValues[(int)UI_Key.Track_List_Add_To_Now_Playing] = "Add to Now Playing";
            displayValues[(int)UI_Key.Track_List_Add_Album_To_Now_Playing] = "Add Album to Now Playing";
            displayValues[(int)UI_Key.Track_List_Remove_From_Playlist] = "Remove from Playlist";
            displayValues[(int)UI_Key.Track_List_Remove_From_Library] = "Remove from Library...";
            displayValues[(int)UI_Key.Track_List_Set_Rating] = "Set Rating";
            displayValues[(int)UI_Key.Track_List_Show_File_Details] = "Show File Details...";
            displayValues[(int)UI_Key.Track_List_Edit_File_Info] = "Edit File Info...";

            displayValues[(int)UI_Key.Track_Compilation] = "Compilation";
            displayValues[(int)UI_Key.Track_Mono] = "Mono";
            displayValues[(int)UI_Key.Track_Stereo] = "Stereo";

            displayValues[(int)UI_Key.File_Info_Title] = "Title";
            displayValues[(int)UI_Key.File_Info_Artist] = "Artist";
            displayValues[(int)UI_Key.File_Info_Album] = "Album";
            displayValues[(int)UI_Key.File_Info_Album_Artist] = "Album Artist";
            displayValues[(int)UI_Key.File_Info_Compilation] = "On A Compilation";
            displayValues[(int)UI_Key.File_Info_Yes] = "Yes";
            displayValues[(int)UI_Key.File_Info_Referenced_As] = "Referenced As";
            displayValues[(int)UI_Key.File_Info_Length] = "Length";
            displayValues[(int)UI_Key.File_Info_Track_Number] = "Track Number";
            displayValues[(int)UI_Key.File_Info_Disk_Number] = "Disk Number";
            displayValues[(int)UI_Key.File_Info_Genre] = "Genre";
            displayValues[(int)UI_Key.File_Info_Grouping] = "Grouping";
            displayValues[(int)UI_Key.File_Info_Composer] = "Composer";
            displayValues[(int)UI_Key.File_Info_Year] = "Year";
            displayValues[(int)UI_Key.File_Info_Rating] = "Rating";
            displayValues[(int)UI_Key.File_Info_Play_Count] = "Play Count";
            displayValues[(int)UI_Key.File_Info_File_Type_File_Size] = "File Type / File Size";
            displayValues[(int)UI_Key.File_Info_Bit_Rate_Encoder] = "Bit Rate / Encoder";
            displayValues[(int)UI_Key.File_Info_Num_Channels_Sample_Rate] = "Channels / Sample Rate";
            displayValues[(int)UI_Key.File_Info_Replay_Gain] = "Replay Gain";
            displayValues[(int)UI_Key.File_Info_Last_Played] = "Last Played";
            displayValues[(int)UI_Key.File_Info_Date_Modified] = "Date File Modified";
            displayValues[(int)UI_Key.File_Info_Date_Added] = "Date Added";
            displayValues[(int)UI_Key.File_Info_Equalizer] = "Equalizer";
            displayValues[(int)UI_Key.File_Info_File_Path] = "File Path";
            displayValues[(int)UI_Key.File_Info_Ghost] = "Is Ghost File";
            displayValues[(int)UI_Key.File_Info_Done] = "Done";
            displayValues[(int)UI_Key.File_Info_Edit] = "Edit Tags...";

            displayValues[(int)UI_Key.Twitter_Title] = "Twitter Options";
            displayValues[(int)UI_Key.Twitter_Instructions] = "You can post an update to twitter.com every time you play a track on {0}. Your credentials will not be sent to anyone other than twitter.com. Set your username and password here:";
            displayValues[(int)UI_Key.Twitter_Enable] = "Enable Twitter Posting";
            displayValues[(int)UI_Key.Twitter_User_Name] = "Twitter User Name";
            displayValues[(int)UI_Key.Twitter_Password] = "Twitter Password";
            displayValues[(int)UI_Key.Twitter_Test] = "Test...";
            displayValues[(int)UI_Key.Twitter_View_On_Web] = "View on Web...";
            displayValues[(int)UI_Key.Twitter_Test_Message] = "Test message from QuuxPlayer";
            displayValues[(int)UI_Key.Twitter_Test_Message_Sent] = "Test message sent. Please check your twitter account.";
            displayValues[(int)UI_Key.Twitter_Test_Message_Sent_Title] = "Message Sent";
            displayValues[(int)UI_Key.Twitter_Test_Message_Failed] = "Test message failed. Please check your twitter account information and internet connection";
            displayValues[(int)UI_Key.Twitter_Test_Message_Failed_Title] = "Message Failed";
            displayValues[(int)UI_Key.Twitter_Mode_Label] = "Post A Tweet With Each:";
            displayValues[(int)UI_Key.Twitter_Mode_Song] = "Song";
            displayValues[(int)UI_Key.Twitter_Mode_Album] = "Album";

            displayValues[(int)UI_Key.LastFM_Title] = "Last.fm Options";
            displayValues[(int)UI_Key.LastFM_Instructions] = "You can post an update (a 'scrobble') to last.fm every time you play a track on {0}. Your credentials will not be sent to anyone other than last.fm. Set your username and password here:";
            displayValues[(int)UI_Key.LastFM_Enable] = "Enable last.fm Scrobbling";
            displayValues[(int)UI_Key.LastFM_User_Name] = "Last.fm User Name";
            displayValues[(int)UI_Key.LastFM_Password] = "Last.fm Password";
            displayValues[(int)UI_Key.LastFM_Go_To_Account] = "Go to Account...";

            displayValues[(int)UI_Key.File_Associations_Title] = "Windows File Associations";
            displayValues[(int)UI_Key.File_Associations_Instructions] = "Select which file types should be opened in QuuxPlayer when invoked in Windows. You can play a file by double clicking it in Windows or by right-clicking and choosing other options from the context menu.";
            displayValues[(int)UI_Key.File_Associations_MP3] = "MP3";
            displayValues[(int)UI_Key.File_Associations_WMA] = "WMA (Windows Media)";
            displayValues[(int)UI_Key.File_Associations_OGG] = "OGG (OGG/Vorbis)";
            displayValues[(int)UI_Key.File_Associations_FLAC] = "FLAC (Lossless)";
            displayValues[(int)UI_Key.File_Associations_iTunes] = "M4A / AAC (iTunes)";
            displayValues[(int)UI_Key.File_Associations_WV] = "WV (WavPack)";
            displayValues[(int)UI_Key.File_Associations_WAV] = "WAV (Windows WAV)";
            displayValues[(int)UI_Key.File_Associations_AC3] = "AC3 (Dolby Digital)";
            displayValues[(int)UI_Key.File_Associations_MPC] = "MPC (Musepack)";
            displayValues[(int)UI_Key.File_Associations_ALAC] = "ALAC (Apple Lossless)";
            displayValues[(int)UI_Key.File_Associations_AIFF] = "AIFF";
            displayValues[(int)UI_Key.File_Associations_APE] = "APE (Monkey's Audio)";
            displayValues[(int)UI_Key.File_Associations_PLS] = "PLS";
            displayValues[(int)UI_Key.File_Associations_Check_All] = "Check All";
            displayValues[(int)UI_Key.File_Associations_Check_None] = "Check None";

            displayValues[(int)UI_Key.Tag_Cloud_Show_At_Most] = "Show At Most:";
            displayValues[(int)UI_Key.Tag_Cloud_Genres] = "Genres";
            displayValues[(int)UI_Key.Tag_Cloud_Artists] = "Artists";
            displayValues[(int)UI_Key.Tag_Cloud_Albums] = "Albums";
            displayValues[(int)UI_Key.Tag_Cloud_Groupings] = "Groupings";
            displayValues[(int)UI_Key.Tag_Cloud_Choose_Top] = "Choose Top";
            displayValues[(int)UI_Key.Tag_Cloud_Choose_Random] = "Choose Random";
            displayValues[(int)UI_Key.Tag_Cloud_Mouseover_Hint_Singular] = "{0} ({1} Track)";
            displayValues[(int)UI_Key.Tag_Cloud_Mouseover_Hint_Plural] = "{0} ({1} Tracks)";

            displayValues[(int)UI_Key.Edit_Tags_Title_1] = "Edit File Tags - {0}";
            displayValues[(int)UI_Key.Edit_Tags_Title_2] = "{0} Tracks";
            displayValues[(int)UI_Key.Edit_Tags_Multiple_Values] = "Multiple Values";
            displayValues[(int)UI_Key.Edit_Tags_Title] = "&Title";
            displayValues[(int)UI_Key.Edit_Tags_Artist] = "A&rtist";
            displayValues[(int)UI_Key.Edit_Tags_Album] = "A&lbum";
            displayValues[(int)UI_Key.Edit_Tags_Album_Artist] = "Al&bum Artist";
            displayValues[(int)UI_Key.Edit_Tags_Composer] = "&Composer";
            displayValues[(int)UI_Key.Edit_Tags_Grouping] = "Gro&uping";
            displayValues[(int)UI_Key.Edit_Tags_Year] = "&Year";
            displayValues[(int)UI_Key.Edit_Tags_Track_Num] = "Tr&k #";
            displayValues[(int)UI_Key.Edit_Tags_Disk_Num] = "&Dsk #";
            displayValues[(int)UI_Key.Edit_Tags_Genre] = "&Genre";
            displayValues[(int)UI_Key.Edit_Tags_Compilation] = "On a Co&mpilation";
            displayValues[(int)UI_Key.Edit_Tags_Rename] = "Rename &File";
            displayValues[(int)UI_Key.Edit_Tags_Save] = "&Save";
            displayValues[(int)UI_Key.Edit_Tags_Done] = "Done";
            displayValues[(int)UI_Key.Edit_Tags_Previous] = "< &Previous";
            displayValues[(int)UI_Key.Edit_Tags_Next] = "&Next >";
            displayValues[(int)UI_Key.Edit_Tags_AutoNumber] = "AutoNum...";
            displayValues[(int)UI_Key.Edit_Tags_Artwork_Blank_Message] = "No Image";
            displayValues[(int)UI_Key.Edit_Tags_Load] = "Load";
            displayValues[(int)UI_Key.Edit_Tags_Clear] = "Clear";
            displayValues[(int)UI_Key.Edit_Tags_Copy] = "Copy";
            displayValues[(int)UI_Key.Edit_Tags_Paste] = "Paste";

            displayValues[(int)UI_Key.Number_Tracks_Title] = "AutoNumber Tracks";
            displayValues[(int)UI_Key.Number_Tracks_Heading] = "Choose a Range for Numbering Selected Tracks:";
            displayValues[(int)UI_Key.Number_Tracks_First] = "First Track Number:";
            displayValues[(int)UI_Key.Number_Tracks_Last] = "Last Track Number:   {0}";

            displayValues[(int)UI_Key.Edit_File_Multiple_Values] = "Multiple Values";
            displayValues[(int)UI_Key.Edit_File_TK_AR_TI] =    "{TrackNum} - {Artist} - {Title}";
            displayValues[(int)UI_Key.Edit_File_TK_TI] =       "{TrackNum} - {Title}";
            displayValues[(int)UI_Key.Edit_File_TK_AR_AL_TI] = "{TrackNum} - {Artist} - {Album} - {Title}";
            displayValues[(int)UI_Key.Edit_File_TK_AL_AR_TI] = "{TrackNum} - {Album} - {Artist} - {Title}";
            displayValues[(int)UI_Key.Edit_File_AR_AL_TK_TI] = "{Artist} - {Album} - {TrackNum} - {Title}";
            displayValues[(int)UI_Key.Edit_File_AL_TK_TI] =    "{Album} - {TrackNum} - {Title}";
            displayValues[(int)UI_Key.Edit_File_TI] =          "{Title}";

            displayValues[(int)UI_Key.Organize_File_AR] =          "{Artist}";
            displayValues[(int)UI_Key.Organize_File_AL] =          "{Album}";
            displayValues[(int)UI_Key.Organize_File_AR_AL] =       "{Artist} > {Album}";
            displayValues[(int)UI_Key.Organize_File_GE_AR_AL] =    "{Genre} > {Artist} > {Album}";
            displayValues[(int)UI_Key.Organize_File_GR_GE_AR_AL] = "{Grouping} > {Genre} > {Artist} > {Album}";
            displayValues[(int)UI_Key.Organize_File_GE_GR_AR_AL] = "{Genre} > {Grouping} > {Artist} > {Album}";
            displayValues[(int)UI_Key.Organize_File_GE_GR_AR] =    "{Genre} > {Grouping} > {Artist}";
            displayValues[(int)UI_Key.Organize_File_GR_AR] =       "{Grouping} > {Artist}";
            displayValues[(int)UI_Key.Organize_File_GR] =          "{Grouping}";
            displayValues[(int)UI_Key.Organize_File_GE] =          "{Genre}";
            displayValues[(int)UI_Key.Organize_File_GR_AR_AL] =    "{Grouping} > {Artist} > {Album}";
            displayValues[(int)UI_Key.Organize_File_GR_GE_AR] =    "{Grouping} > {Genre} > {Artist}";
            displayValues[(int)UI_Key.Organize_File_GE_AR] =       "{Genre} > {Artist}";
            displayValues[(int)UI_Key.Organize_Help] = "Help (on Web)";
            displayValues[(int)UI_Key.Organize_Title] = "This will organize all your tracks within a single folder. Choose a structure:";
            displayValues[(int)UI_Key.Organize_Top_Folder] = "&Top Folder:";
            displayValues[(int)UI_Key.Organize_Browse] = "&Browse...";
            displayValues[(int)UI_Key.Organize_Folder_Structure] = "Choose a &folder structure:";
            displayValues[(int)UI_Key.Organize_Rename] = "Choose a &pattern for naming your files:";
            displayValues[(int)UI_Key.Organize_Dont_Change] = "Don't Change";
            displayValues[(int)UI_Key.Organize_Keep_Organized] = "Or&ganize files when tags change";
            displayValues[(int)UI_Key.Organize_Move_Into_Top_Folder] = "&Move new files into my main folder";
            displayValues[(int)UI_Key.Organize_Sample_Track_Path] = @"c:\money.mp3";
            displayValues[(int)UI_Key.Organize_Sample_Track_Title] = "Money";
            displayValues[(int)UI_Key.Organize_Sample_Track_Artist] = "Pink Floyd";
            displayValues[(int)UI_Key.Organize_Sample_Track_Album] = "Dark Side of the Moon";
            displayValues[(int)UI_Key.Organize_Sample_Track_Genre] = "Rock";
            displayValues[(int)UI_Key.Organize_Sample_Track_Grouping] = "70's Songs";
            displayValues[(int)UI_Key.Organize_Invalid_Path] = "Invalid Path";
            displayValues[(int)UI_Key.Organize_Get_Folder] = "Select the main folder to contain your library:";
            displayValues[(int)UI_Key.Organize_Sample] = "Sample:";
            displayValues[(int)UI_Key.Organize_Move_Files] = "Some of your files are not contained under your top folder. Do you want to move them there?";
            displayValues[(int)UI_Key.Organize_Move_Files_Title] = "Move files?";
            displayValues[(int)UI_Key.Organize_Create_Directory] = "The directory does not exist. Create it?";
            displayValues[(int)UI_Key.Organize_Create_Directory_Title] = "Create Directory?";
            displayValues[(int)UI_Key.Organize_Create_Directory_Failed] = "Failed to create directory.";
            displayValues[(int)UI_Key.Organize_Create_Directory_Failed_Title] = "Failed";
            displayValues[(int)UI_Key.Organize_Token_My_Music] = "[My Music]";
            displayValues[(int)UI_Key.Organize_Token_My_Documents] = "[My Documents]";
            displayValues[(int)UI_Key.Organize_Token_Desktop] = "[Desktop]";
            displayValues[(int)UI_Key.Organize_Organize] = "&Organize";
            displayValues[(int)UI_Key.Organize_Dont_Organize] = "&Don't Organize";

            displayValues[(int)UI_Key.Radio_Connection_Failed] = "Radio connection failed.";
            displayValues[(int)UI_Key.Radio_Connecting] = "Connecting with radio stream...";
            displayValues[(int)UI_Key.Radio_Buffering] = "Buffering... {0:0.0}%";
            displayValues[(int)UI_Key.Radio_Buffered] = "Radio buffer complete.";
            displayValues[(int)UI_Key.Radio_Change_Station_Name] = "Change the name of this station from '{0}' to:";
            displayValues[(int)UI_Key.Radio_Change_Station_Name_Title] = "Change Station Name?";
            displayValues[(int)UI_Key.Radio_Blank_Name] = "<Unnamed Station>";
            displayValues[(int)UI_Key.Radio_Rename_Genre] = "Rename Genre...";
            displayValues[(int)UI_Key.Radio_Remove_Genre] = "Remove All Stations in Genre: {0}";
            displayValues[(int)UI_Key.Radio_URL_Watermark] = "[Enter Radio Station URL]";
            displayValues[(int)UI_Key.Radio_Filter_Watermark] = "[Search]";
            displayValues[(int)UI_Key.Radio_Add_And_Play] = "Add and Play";
            displayValues[(int)UI_Key.Radio_Genres] = "Genres";
            displayValues[(int)UI_Key.Radio_Restore_Default_Stations] = "Restore default stations to library?";
            displayValues[(int)UI_Key.Radio_Restore_Default_Stations_Title] = "Restore Stations";
            displayValues[(int)UI_Key.Radio_Restore_Default_Stations_Checkbox] = "&Remove stations not in default list";
            displayValues[(int)UI_Key.Radio_Play] = "Play Station";
            displayValues[(int)UI_Key.Radio_Edit_Station] = "Edit Station Details";
            displayValues[(int)UI_Key.Radio_Remove_Station] = "Remove Station";

        }
        private static void writeKeys()
        {
            Dictionary<int, string> keys = new Dictionary<int, string>();

            for (int i = 0; i < displayValues.Length; i++)
            {
                keys.Add(i, displayValues[i]);
            }
            
            StreamWriter sw = new StreamWriter(Lib.ProgramPath("local.cfg"), false, Encoding.UTF8);
            
            for (int i = 0; i < keys.Count; i++)
                sw.WriteLine(((UI_Key)i).ToString() + " " + keys[i]);
            
            sw.Close();
        }
        private static void overrideKeys()
        {
            StreamReader sr = null;
            try
            {
                sr = new StreamReader(Lib.ProgramPath("local.cfg"), Encoding.UTF8);

                Dictionary<string, UI_Key> revLookup = new Dictionary<string, UI_Key>();
                for (int i = 0; i < (int)UI_Key.Max; i++)
                {
                    revLookup.Add(((UI_Key)i).ToString(), (UI_Key)i);
                }

                while (!sr.EndOfStream)
                {
                    string item = sr.ReadLine();
                    int io = item.IndexOf(' ');
                    if (io > 0)
                    {
                        string key = item.Substring(0, io);
                        string val = item.Substring(io + 1);

                        System.Diagnostics.Debug.WriteLine("key: " + key + " value: " + val);

                        if (revLookup.ContainsKey(key))
                        {
                            displayValues[(int)revLookup[key]] = val;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                loadKeys();
                return;
            }
            finally
            {
                sr.Close();
            }
        }
    }
}
