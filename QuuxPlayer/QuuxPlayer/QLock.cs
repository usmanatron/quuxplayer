/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuuxPlayer
{
    internal sealed class QLock
    {
        public const int MAX_CODE_LENGTH = 20;

        public bool Locked { get; set; }
        public string Code { get; set; }
        public bool GamepadLock { get; set; }
        public QLock(bool Locked, string Code, bool GamepadLock)
        {
            this.Locked = Locked;
            this.Code = Code;
            this.GamepadLock = GamepadLock;
        }
        public void Unlock()
        {
            this.Locked = false;
            this.GamepadLock = false;
        }
    }
}
