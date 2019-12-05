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
    internal sealed class KeyboardHook : IDisposable
    {
        private Controller controller;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x100;
        private const int WM_KEYUP = 0x101;
        private const int WM_SYSKEYDOWN = 0x104;
        private const int WM_SYSKEYUP = 0x105;
        private const int VK_SHIFT = 0x10;
        private const int VK_CONTROL = 0x11;
        private const int VK_ALT = 0x12;
        private const int VK_CAPITAL = 0x14;

        private const int VK_MEDIA_NEXT_TRACK = 0xB0;
        private const int VK_MEDIA_PREV_TRACK = 0xB1;
        private const int VK_MEDIA_STOP = 0xB2;
        private const int VK_MEDIA_PLAY_PAUSE = 0xB3;
        private const int VK_VOLUME_MUTE = 0xAD;
        private const int VK_VOLUME_DOWN = 0xAE;
        private const int VK_VOLUME_UP = 0xAF;

        private HookHandlerDelegate proc;
        private IntPtr hookID = IntPtr.Zero;

        internal delegate IntPtr HookHandlerDelegate(int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

        private QActionType action = QActionType.None;
        private frmGlobalInfoBox.ActionType popupAction = frmGlobalInfoBox.ActionType.None;

        internal struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int flags;

            private int scanCode;
            private int time;
            private int dwExtraInfo;
        }

        public KeyboardHook(Controller Controller)
        {
            controller = Controller;

            proc = new HookHandlerDelegate(HookCallback);
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                hookID = SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }
        private IntPtr HookCallback(int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam)
        {
             if (nCode < 0)
                return CallNextHookEx(hookID, nCode, wParam, ref lParam);

            bool doAction = (wParam == (IntPtr)WM_KEYUP);
            bool done = false;

            action = QActionType.None;
            popupAction = frmGlobalInfoBox.ActionType.None;

            switch (lParam.vkCode)
            {
                case 160:
                case 161:
                    controller.NotifyShift();
                    break;
                case VK_MEDIA_NEXT_TRACK:
                    if (doAction)
                    {
                        action = QActionType.Next;
                        popupAction = frmGlobalInfoBox.ActionType.Next;
                    }
                    done = true;
                    break;
                case VK_MEDIA_PREV_TRACK:
                    if (doAction)
                    {
                        action = QActionType.Previous;
                        popupAction = frmGlobalInfoBox.ActionType.Previous;
                    }
                    done = true;
                    break;
                case VK_MEDIA_STOP:
                    if (doAction)
                    {
                        action = QActionType.Stop;
                        popupAction = frmGlobalInfoBox.ActionType.Stop;
                    }
                    done = true;
                    break;
                case VK_MEDIA_PLAY_PAUSE:
                    if (doAction)
                    {
                        action = QActionType.PlayPause;
                        popupAction = frmGlobalInfoBox.ActionType.PlayPause;
                    }
                    done = true;
                    break;
                case VK_VOLUME_UP:
                    if (doAction && controller.LocalVolumeControl && controller.Playing)
                    {
                        action = QActionType.VolumeUp;
                        popupAction = frmGlobalInfoBox.ActionType.VolumeUp;
                        done = true;
                    }
                    break;
                case VK_VOLUME_DOWN:
                    if (doAction && controller.LocalVolumeControl && controller.Playing)
                    {
                        action = QActionType.VolumeDown;
                        popupAction = frmGlobalInfoBox.ActionType.VolumeDown;
                        done = true;
                    }
                    break;
                default:
                    doAction = false;
                    break;
            }

            if (action != QActionType.None)
            {
                controller.RequestAction(action);
            }
            if (popupAction != frmGlobalInfoBox.ActionType.None)
            {
                frmGlobalInfoBox.Show(controller, popupAction);
            }
         
            if (done)
                return (IntPtr)1;
            else
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