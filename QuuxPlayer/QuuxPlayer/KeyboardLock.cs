/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;

namespace QuuxPlayer
{
    internal sealed class KeyboardLock : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x100;
        private const int WM_KEYUP = 0x101;
        private const int WM_SYSKEYDOWN = 0x104;
        private const int WM_SYSKEYUP = 0x105;
        private const int VK_SHIFT = 0x10;
        private const int VK_CONTROL = 0x11;
        private const int VK_ALT = 0x12;
        private const int VK_CAPITAL = 0x14;

        private HookHandlerDelegate proc;
        private IntPtr hookID = IntPtr.Zero;

        internal delegate IntPtr HookHandlerDelegate(int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

        internal struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int flags;

            private int scanCode;
            private int time;
            private int dwExtraInfo;
        }

        public KeyboardLock()
        {
            proc = new HookHandlerDelegate(HookCallback);
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                hookID = SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }
        private IntPtr HookCallback(int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam)
        {
            if (nCode >= 0)
            {
                if (wParam == (IntPtr)WM_SYSKEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN || wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                {
                    if ((GetKeyState(VK_CONTROL) & 0x8000) != 0)
                    {
                        if (lParam.vkCode == 0x1B) // ctrl-esc
                            return (IntPtr)1;
                    }
                    if ((GetKeyState(VK_ALT) & 0x8000) != 0)
                    {
                        if (lParam.vkCode == 0x1B) // alt-esc
                            return (IntPtr)1;
                        if (lParam.vkCode == 0x09) // alt-tab
                            return (IntPtr)1;
                    }
                    if (lParam.vkCode == 91 || lParam.vkCode == 92) //windows
                        return (IntPtr)1;
                }
            }
            return CallNextHookEx(hookID, nCode, wParam, ref lParam);
        }
        public void Dispose()
        {
            UnhookWindowsHookEx(hookID);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookHandlerDelegate lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        private static extern short GetKeyState(int keyCode);
    }
}