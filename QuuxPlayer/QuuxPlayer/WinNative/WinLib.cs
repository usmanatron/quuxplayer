using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace QuuxPlayer
{
    public static class WinLib
    {

        [DllImport("user32.dll", CharSet=CharSet.Auto)]
        public static extern int SystemParametersInfo(int uAction, int uParam, ref int lpvParam, int fuWinIni);

        private const int SPI_GETSCREENSAVERACTIVE = 16;
        private const int SPI_SETSCREENSAVERACTIVE = 17;
        private const int SPIF_SENDWININICHANGE = 2;

        public static void BrowseTo(string URL)
        {
            System.Diagnostics.Process.Start(URL);
        }

        public static bool ScreenSaverIsActive
        {
            get
            {
                int isActive = 0;

                SystemParametersInfo(SPI_GETSCREENSAVERACTIVE, 0,
                   ref isActive, 0);

                return isActive != 0;
            }
            set
            {
                int active = value ? 1 : 0;
                int nullVar = 0;

                SystemParametersInfo(SPI_SETSCREENSAVERACTIVE,
                   active, ref nullVar, SPIF_SENDWININICHANGE);
            }
        }
    }
}
