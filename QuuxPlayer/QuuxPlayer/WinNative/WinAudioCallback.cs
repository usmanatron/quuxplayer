using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal class WinAudioCallback : NativeWindow, IDisposable
    {
        public delegate void AudioCallback();
        
        private AudioCallback ac;

        private const int MM_MIXM_LINE_CHANGE = 0x3D0;
        private const int MM_MIXM_CONTROL_CHANGE = 0x3D1;
        private const int CALLBACK_WINDOW = 0x00010000;

        public static WinAudioCallback SetCallback(AudioCallback AC)
        {
            int mix;

            WinAudioCallback wac = new WinAudioCallback(AC);
            mixerOpen(out mix, 0, wac.Handle, 0, CALLBACK_WINDOW);

            return wac;
        }
        public void Dispose()
        {
            if (this.Handle != IntPtr.Zero)
            {
                this.DestroyHandle();
            }
        }

        private WinAudioCallback(AudioCallback AC)
        {
            this.ac = AC;
            this.CreateHandle(new CreateParams());
        }

        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case MM_MIXM_LINE_CHANGE:
                case MM_MIXM_CONTROL_CHANGE:
                    ac();
                    break;
                default:
                    break;
            }
            base.WndProc(ref m);
        }

        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        private static extern int mixerOpen(out int phmx, int uMxId, IntPtr dwCallback, int dwInstance, int fdwOpen);

    }
}
