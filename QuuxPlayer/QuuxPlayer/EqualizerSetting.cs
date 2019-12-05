/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;

namespace QuuxPlayer
{
    internal sealed class EqualizerSetting : IComparable
    {
        public static EqualizerSetting Off;
        public static string DontChange = Localization.Get(UI_Key.Equalizer_Dont_Change);
        public static string TurnOff = Localization.Get(UI_Key.Equalizer_Turn_Off);

        public string Name { get; set; }
        public bool Locked { get; set; }
        public float[] Values { get; set; }
        public bool IsOff { get; set; }

        public EqualizerSetting(string Name, float[] Values, bool Locked)
        {
            this.Name = Name;
            this.Locked = Locked;
            this.Values = Values;
            this.IsOff = false;
        }
        private EqualizerSetting()
        {
            this.IsOff = true;
            this.Name = "Off";
            this.Values = new float[Equalizer.MAX_NUM_BANDS];
            this.Locked = true;
        }
        static EqualizerSetting()
        {
            Off = new EqualizerSetting();
        }
        public override string ToString()
        {
            return Name;
        }
        public static string GetString(EqualizerSetting ES)
        {
            if (ES == null)
                return DontChange;
            else if (ES.IsOff)
                return TurnOff;
            else
                return ES.ToString();
        }
        public static EqualizerSetting GetSetting(string Name, List<EqualizerSetting> Settings)
        {
            if (Name == DontChange)
                return null;
            else if (Name == TurnOff)
                return Off;
            else
                return Settings.Find(eq => eq.Name == Name);
        }
        public static List<string> GetAllSettingStrings(List<EqualizerSetting> Settings)
        {
            List<string> settingStrings = (from s in Settings
                                           select s.Name).ToList();

            settingStrings.Add(TurnOff);
            settingStrings.Sort();
            return settingStrings;
        }
        public int CompareTo(object Other)
        {
            return this.Name.CompareTo(Other.ToString());
        }
    }
}
