using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

#pragma warning disable 0649

namespace QuuxPlayer
{
    internal static class WinAudioLib
    {
        public static EventHandler VolumeChanged;

        private const int MMSYSERR_NOERROR = 0;
        private const int MAXPNAMELEN = 32;
        private const int MIXER_LONG_NAME_CHARS = 64;
        private const int MIXER_SHORT_NAME_CHARS = 16;
        private const int MIXER_GETLINEINFOF_COMPONENTTYPE = 0x3;
        private const int MIXER_GETCONTROLDETAILSF_VALUE = 0x0;
        private const int MIXER_GETLINECONTROLSF_ONEBYTYPE = 0x2;
        private const int MIXER_SETCONTROLDETAILSF_VALUE = 0x0;
        private const int MIXERLINE_COMPONENTTYPE_DST_FIRST = 0x0;
        private const int MIXERLINE_COMPONENTTYPE_SRC_FIRST = 0x1000;
        private const int MIXERLINE_COMPONENTTYPE_DST_SPEAKERS = (MIXERLINE_COMPONENTTYPE_DST_FIRST + 4);
        private const int MIXERLINE_COMPONENTTYPE_SRC_MICROPHONE = (MIXERLINE_COMPONENTTYPE_SRC_FIRST + 3);
        private const int MIXERLINE_COMPONENTTYPE_SRC_LINE = (MIXERLINE_COMPONENTTYPE_SRC_FIRST + 2);
        private const int MIXERCONTROL_CT_CLASS_FADER = 0x50000000;
        private const int MIXERCONTROL_CT_UNITS_UNSIGNED = 0x30000;
        private const int MIXERCONTROL_CONTROLTYPE_FADER = (MIXERCONTROL_CT_CLASS_FADER | MIXERCONTROL_CT_UNITS_UNSIGNED);
        private const int MIXERCONTROL_CONTROLTYPE_VOLUME = (MIXERCONTROL_CONTROLTYPE_FADER + 1);

        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        private static extern int mixerClose(int hmx);

        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        private static extern int mixerGetControlDetailsA(int hmxobj, ref MIXERCONTROLDETAILS pmxcd, int fdwDetails);

        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        private static extern int mixerGetDevCapsA(int uMxId, MIXERCAPS pmxcaps, int cbmxcaps);

        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        private static extern int mixerGetID(int hmxobj, int pumxID, int fdwId);

        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        private static extern int mixerGetLineControlsA(int hmxobj, ref MIXERLINECONTROLS pmxlc, int fdwControls);

        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        private static extern int mixerGetLineInfoA(int hmxobj, ref MIXERLINE pmxl, int fdwInfo);

        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        private static extern int mixerGetNumDevs();

        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        private static extern int mixerMessage(int hmx, int uMsg, int dwParam1, int dwParam2);

        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        private static extern int mixerOpen(out int phmx, int uMxId,  int dwCallback, int dwInstance, int fdwOpen);

        
        [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
        private static extern int mixerSetControlDetails(int hmxobj, ref MIXERCONTROLDETAILS pmxcd, int fdwDetails);

        private static bool volNeedsRefresh = true;
        private static bool volDBNeedsRefresh = true;
        private static int volume = 0;
        private static float volumeDB = 0;
        private static string volumeDBString = String.Empty;
        private static bool callbackDone = false;
        private static WinAudioCallback wac = null;

        public static int Volume
        {
            get
            {
                if (volNeedsRefresh)
                {
                    try
                    {

                        if (!callbackDone)
                        {
                            setupCallback();
                        }
                        volume = getVolume();
                        volNeedsRefresh = false;
                    }
                    catch
                    {
                        return 0;
                    }
                }
                return volume;
            }
            set
            {
                setVolume(value);
            }
        }

        private static void setupCallback()
        {
            wac = WinAudioCallback.SetCallback(requestVolRefresh);
            callbackDone = true;
        }
        public static float VolumeDB
        {
            get
            {
                if (volDBNeedsRefresh)
                {
                    if (!callbackDone)
                    {
                        setupCallback();
                    }

                    volumeDB = (float)Math.Max(-99.9f, Math.Min(0f, 20f * Math.Log10((double)Volume / 65536.0)));

                    volDBNeedsRefresh = false;

                }
                return volumeDB;
            }
            set
            {
                Volume = (int)(Math.Pow(10, value / 20F) * 65536.0);
            }
        }
        public static string VolumeDBString
        {
            get
            {
                return (VolumeDB < -99.8f) ? "-∞dB" : volumeDB.ToString("0.0dB");
            }
        }
        public static void requestVolRefresh()
        {
            volNeedsRefresh = true;
            volDBNeedsRefresh = true;
            if (VolumeChanged != null)
                VolumeChanged.Invoke(null, EventArgs.Empty);
        }

        private struct MIXERCAPS
        {
            public int wMid;
            public int wPid;
            public int vDriverVersion;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAXPNAMELEN)]
            public string szPname;

            public int fdwSupport;
            public int cDestinations;
        }

        private struct MIXERCONTROL
        {
            public int cbStruct;
            public int dwControlID;
            public int dwControlType;
            public int fdwControl;
            public int cMultipleItems;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MIXER_SHORT_NAME_CHARS)]
            public string szShortName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MIXER_LONG_NAME_CHARS)]
            public string szName;

            public int lMinimum;
            public int lMaximum;

            [MarshalAs(UnmanagedType.U4, SizeConst = 10)]
            public int reserved;
        }

        private struct MIXERCONTROLDETAILS
        {
            public int cbStruct;
            public int dwControlID;
            public int cChannels;
            public int item;
            public int cbDetails;
            public IntPtr paDetails;
        }

        private struct MIXERCONTROLDETAILS_UNSIGNED
        {
            public int dwValue;
        }

        private struct MIXERLINE
        {
            public int cbStruct;
            public int dwDestination;
            public int dwSource;
            public int dwLineID;
            public int fdwLine;
            public int dwUser;
            public int dwComponentType;
            public int cChannels;
            public int cConnections;
            public int cControls;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MIXER_SHORT_NAME_CHARS)]
            public string szShortName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MIXER_LONG_NAME_CHARS)]
            public string szName;

            public int dwType;
            public int dwDeviceID;
            public int wMid;
            public int wPid;
            public int vDriverVersion;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAXPNAMELEN)]
            public string szPname;
        }

        private struct MIXERLINECONTROLS
        {
            public int cbStruct;
            public int dwLineID;
            public int dwControl;
            public int cControls;
            public int cbmxctrl;
            public IntPtr pamxctrl;
        }

        private static bool getVolumeControl(int hmixer, int componentType, int ctrlType, out MIXERCONTROL mxc, out int vCurrentVol)
        {
            // This function attempts to obtain a mixer control.
            // Returns True if successful.

            MIXERLINECONTROLS mxlc = new MIXERLINECONTROLS();
            MIXERLINE mxl = new MIXERLINE();
            MIXERCONTROLDETAILS pmxcd = new MIXERCONTROLDETAILS();
            MIXERCONTROLDETAILS_UNSIGNED du = new MIXERCONTROLDETAILS_UNSIGNED();

            mxc = new MIXERCONTROL();
            
            int rc;
            bool retValue;

            vCurrentVol = -1;
            mxl.cbStruct = Marshal.SizeOf(mxl);
            mxl.dwComponentType = componentType;
            rc = mixerGetLineInfoA(hmixer, ref mxl, MIXER_GETLINEINFOF_COMPONENTTYPE);

            if (MMSYSERR_NOERROR == rc)
            {
                int sizeofMIXERCONTROL = 152;
                int ctrl = Marshal.SizeOf(typeof(MIXERCONTROL));
                mxlc.pamxctrl = Marshal.AllocCoTaskMem(sizeofMIXERCONTROL);
                mxlc.cbStruct = Marshal.SizeOf(mxlc);
                mxlc.dwLineID = mxl.dwLineID;
                mxlc.dwControl = ctrlType;
                mxlc.cControls = 1;
                mxlc.cbmxctrl = sizeofMIXERCONTROL;

                // Allocate a buffer for the control
                mxc.cbStruct = sizeofMIXERCONTROL;

                // Get the control
                rc = mixerGetLineControlsA(hmixer, ref mxlc, MIXER_GETLINECONTROLSF_ONEBYTYPE);

                if (MMSYSERR_NOERROR == rc)
                {
                    retValue = true;
                    // Copy the control into the destination structure
                    mxc = (MIXERCONTROL)Marshal.PtrToStructure(mxlc.pamxctrl, typeof(MIXERCONTROL));
                }
                else
                {
                    retValue = false;
                }

                int sizeofMIXERCONTROLDETAILS = Marshal.SizeOf(typeof(MIXERCONTROLDETAILS));
                int sizeofMIXERCONTROLDETAILS_UNSIGNED = Marshal.SizeOf(typeof(MIXERCONTROLDETAILS_UNSIGNED));

                pmxcd.cbStruct = sizeofMIXERCONTROLDETAILS;
                pmxcd.dwControlID = mxc.dwControlID;
                pmxcd.paDetails = Marshal.AllocCoTaskMem(sizeofMIXERCONTROLDETAILS_UNSIGNED);
                pmxcd.cChannels = 1;
                pmxcd.item = 0;
                pmxcd.cbDetails = sizeofMIXERCONTROLDETAILS_UNSIGNED;

                rc = mixerGetControlDetailsA(hmixer, ref pmxcd, MIXER_GETCONTROLDETAILSF_VALUE);

                du = (MIXERCONTROLDETAILS_UNSIGNED)Marshal.PtrToStructure(pmxcd.paDetails, typeof(MIXERCONTROLDETAILS_UNSIGNED));

                vCurrentVol = du.dwValue;

                return retValue;
            }

            retValue = false;

            return retValue;

        }
        private static bool setVolumeControl(int hmixer, MIXERCONTROL mxc, int volume)
        {
            // This function sets the value for a volume control.
            // Returns True if successful

            bool retValue;
            int rc;

            MIXERCONTROLDETAILS mxcd = new MIXERCONTROLDETAILS();
            MIXERCONTROLDETAILS_UNSIGNED vol = new MIXERCONTROLDETAILS_UNSIGNED();

            mxcd.item = 0;
            mxcd.dwControlID = mxc.dwControlID;
            mxcd.cbStruct = Marshal.SizeOf(mxcd);
            mxcd.cbDetails = Marshal.SizeOf(vol);

            // Allocate a buffer for the control value buffer
            mxcd.cChannels = 1;
            vol.dwValue = volume;

            // Copy the data into the control value buffer
            mxcd.paDetails = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(MIXERCONTROLDETAILS_UNSIGNED)));

            Marshal.StructureToPtr(vol, mxcd.paDetails, false);

            // Set the control value
            rc = mixerSetControlDetails(hmixer, ref mxcd, MIXER_SETCONTROLDETAILSF_VALUE);

            if (MMSYSERR_NOERROR == rc)
            {
                retValue = true;
            }
            else
            {
                retValue = false;
            }
            return retValue;
        }
        private static int getVolume()
        {
            int mixer;

            MIXERCONTROL volCtrl = new MIXERCONTROL();

            int currentVol;

            mixerOpen(out mixer, 0, 0, 0, 0);

            int type = MIXERCONTROL_CONTROLTYPE_VOLUME;

            getVolumeControl(mixer, MIXERLINE_COMPONENTTYPE_DST_SPEAKERS, type, out volCtrl, out currentVol);

            mixerClose(mixer);

            System.Diagnostics.Debug.WriteLine("Vol: " + currentVol.ToString());

            return currentVol;
        }
        private static void setVolume(int vVolume)
        {
            try
            {
                int mixer;

                MIXERCONTROL volCtrl = new MIXERCONTROL();

                int currentVol;

                mixerOpen(out mixer, 0, 0, 0, 0);

                int type = MIXERCONTROL_CONTROLTYPE_VOLUME;

                getVolumeControl(mixer, MIXERLINE_COMPONENTTYPE_DST_SPEAKERS, type, out volCtrl, out currentVol);

                if (vVolume > volCtrl.lMaximum)
                    vVolume = volCtrl.lMaximum;

                if (vVolume < volCtrl.lMinimum)
                    vVolume = volCtrl.lMinimum;

                setVolumeControl(mixer, volCtrl, vVolume);

                getVolumeControl(mixer, MIXERLINE_COMPONENTTYPE_DST_SPEAKERS, type, out volCtrl, out currentVol);

                mixerClose(mixer);
            }
            catch
            {
            }
        }
    }
}
