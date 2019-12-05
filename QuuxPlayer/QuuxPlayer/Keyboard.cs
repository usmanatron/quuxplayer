/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxPlayer
{
    using System;
    using System.Windows.Forms;
    using System.Runtime.InteropServices;
    public static class Keyboard
    {
        [DllImport("user32")]
        private static extern short GetKeyState(int vKey);
        public static KeyStateInfo GetKeyState(Keys key)
        {
            short keyState = GetKeyState((int)key);
            int low = Low(keyState),
                high = High(keyState);
            bool toggled = low == 1 ? true : false,
                 pressed = high == 1;
            return new KeyStateInfo(key, pressed, toggled);
        }
        public static bool Alt
        {
            get
            {
                System.Diagnostics.Debug.WriteLine(GetKeyState(Keys.Menu).IsPressed.ToString());
                return GetKeyState(Keys.Menu).IsPressed;
            }
        }
        public static bool Control
        {
            get { return GetKeyState(Keys.ControlKey).IsPressed; }
        }
        public static bool Shift
        {
            get
            {
                return GetKeyState(Keys.ShiftKey).IsPressed;
            }
        }
        public static bool Escape
        {
            get { return GetKeyState(Keys.Escape).IsPressed; }
        }
        private static int High(int keyState)
        {
            return keyState > 0 ? keyState >> 0x10
                    : (keyState >> 0x10) & 0x1;
        }
        private static int Low(int keyState)
        {
            return keyState & 0xffff;
        }
    }


    public struct KeyStateInfo
    {
        Keys _key;
        bool _isPressed,
            _isToggled;
        public KeyStateInfo(Keys key,
                        bool ispressed,
                        bool istoggled)
        {
            _key = key;
            _isPressed = ispressed;
            _isToggled = istoggled;
        }
        public static KeyStateInfo Default
        {
            get
            {
                return new KeyStateInfo(Keys.None,
                                            false,
                                            false);
            }
        }
        public Keys Key
        {
            get { return _key; }
        }
        public bool IsPressed
        {
            get { return _isPressed; }
        }
        public bool IsToggled
        {
            get { return _isToggled; }
        }
    }

}
