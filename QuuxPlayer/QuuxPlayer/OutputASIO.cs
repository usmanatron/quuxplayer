/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Un4seen;
using Un4seen.Bass;
using Un4seen.BassAsio;

namespace QuuxPlayer
{
    internal sealed class OutputASIO : Output
    {
        private ASIOPROC proc;
        private int deviceNum;
        private bool playing;
        private byte[] data;
        private bool twentyFourBit = false;
        private GetSampleDelegate callback16;
        private GetSampleDelegate callback24;
        private static bool needsUnload = false;

        private static int initializedDevice = -1;

        public OutputASIO(string DeviceName, int Frequency, GetSampleDelegate Callback24, GetSampleDelegate Callback16, Player Player)
            : base(DeviceName, Player)
        {
            needsUnload = true;

            playing = false;

            callback16 = Callback16;
            callback24 = Callback24;

            deviceNum = getDeviceNum(DeviceName);

            if (initializedDevice >= 0)
            {
                BassAsio.BASS_ASIO_Free();
                initializedDevice = -1;
            }

            if (deviceNum < 0 || !BassAsio.BASS_ASIO_Init(deviceNum))
            {
                System.Diagnostics.Debug.WriteLine(BassAsio.BASS_ASIO_ErrorGetCode());
                return;
            }

            initializedDevice = deviceNum;

            if (!setup(Frequency))
                return;

            playing = true;
        }

        private bool setup(int Frequency)
        {
            frequency = Frequency;
            callback = callback24;
            twentyFourBit = true;

            if (setupChannel(Frequency, BASSASIOFormat.BASS_ASIO_FORMAT_24BIT) &&
                BassAsio.BASS_ASIO_ChannelGetFormat(false, 0) == BASSASIOFormat.BASS_ASIO_FORMAT_24BIT)
            {
                Controller.ShowMessage("24-bit ASIO");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(BassAsio.BASS_ASIO_ErrorGetCode());

                callback = callback16;
                twentyFourBit = false;

                if (setupChannel(Frequency, BASSASIOFormat.BASS_ASIO_FORMAT_16BIT))
                {
                    Controller.ShowMessage("16-bit ASIO");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(BassAsio.BASS_ASIO_ErrorGetCode());
                    return false;
                }
            }
            return true;
        }

        private bool setupChannel(int Frequency, BASSASIOFormat Format)
        {
            proc = new ASIOPROC(AsioCallback);

            BassAsio.BASS_ASIO_Stop();

            if (!BassAsio.BASS_ASIO_ChannelReset(false,
                                                 -1,
                                                 BASSASIOReset.BASS_ASIO_RESET_PAUSE | BASSASIOReset.BASS_ASIO_RESET_JOIN))
                return false;

            if (!BassAsio.BASS_ASIO_ChannelEnable(false, 0, proc, IntPtr.Zero))
                return false;

            if (!BassAsio.BASS_ASIO_ChannelJoin(false, 1, 0))
                return false;

            if (!BassAsio.BASS_ASIO_ChannelSetFormat(false, 0, Format))
                return false;
            
            BassAsio.BASS_ASIO_SetRate((double)Frequency);

            if (!BassAsio.BASS_ASIO_ChannelSetRate(false, 0, (double)Frequency))
                return false;

            return BassAsio.BASS_ASIO_Start(0);
        }

        private int readBufferCursor = -1;
        private int readBufferSize = -1;

        public override int BufferSize
        {
            get
            {
                BassAsio.BASS_ASIO_Init(0);
                BASS_ASIO_INFO info = new BASS_ASIO_INFO();
                if (BassAsio.BASS_ASIO_GetInfo(info))
                {
                    return info.bufpref;
                }
                else
                {
                    // probably wrong
                    return 0x4000;
                }
            }
        }
        private int AsioCallback(bool input, int channel, IntPtr buffer, int length, IntPtr user)
        {
            byte[] ret = new byte[length];
            int retCursor = 0;

            while (retCursor < length)
            {
                if (readBufferCursor < 0)
                {
                    readBufferSize = callback(out data);
                    readBufferCursor = 0;
                    if (Mute)
                        Array.Copy(twentyFourBit ? player.EmptyReadBuffer24 : player.EmptyReadBuffer16,
                                   data,
                                   readBufferSize);
                }

                int numBytes = Math.Min(readBufferSize - readBufferCursor, length - retCursor);
                
                Array.Copy(data, readBufferCursor, ret, retCursor, numBytes);
                
                readBufferCursor += numBytes;
                retCursor += numBytes;

                if (readBufferCursor >= readBufferSize)
                    readBufferCursor = -1;
            }

            Marshal.Copy(ret, 0, buffer, length);

            return length;
        }

        public override void Pause()
        {
            BassAsio.BASS_ASIO_ChannelPause(false, 0);
        }
        public override void Resume()
        {
            BassAsio.BASS_ASIO_ChannelReset(false, -1, BASSASIOReset.BASS_ASIO_RESET_PAUSE);
        }
        public override void Kill()
        {
            BassAsio.BASS_ASIO_Stop();
        }
        public override bool Paused
        {
            get
            {
                return BassAsio.BASS_ASIO_ChannelIsActive(false, 0) == BASSASIOActive.BASS_ASIO_ACTIVE_PAUSED;
            }
        }
        private int newFrequency = -1;
        public override int Frequency
        {
            get
            {
                // Don't use just "GetRate" -- may be erroneous due to resampling to 44.1
                return (int)BassAsio.BASS_ASIO_ChannelGetRate(false, 0);
            }
            set
            {
                if (frequency != value)
                {
                    newFrequency = value;
                    Clock.DoOnMainThread(setNewFreq, 10);
                }
            }
        }
        private void setNewFreq()
        {
            if (newFrequency > 0 && newFrequency != frequency)
            {
                frequency = newFrequency;

                if (!BassAsio.BASS_ASIO_SetRate((double)frequency) ||
                    !BassAsio.BASS_ASIO_ChannelSetRate(false, 0, (double)frequency))
                        setupChannel(frequency, twentyFourBit ? BASSASIOFormat.BASS_ASIO_FORMAT_24BIT : BASSASIOFormat.BASS_ASIO_FORMAT_16BIT);
                
                newFrequency = -1;
            }
        }
        public override bool Playing
        {
            get { return playing; }
        }
        public override void Dispose()
        {
            this.Kill();
            Unload();
        }
        public static void Unload()
        {
            if (needsUnload)
                BassAsio.BASS_ASIO_Free();
        }
        public static bool DeviceExists(string Name)
        {
            needsUnload = true;

            BASS_ASIO_DEVICEINFO[] devices = BassAsio.BASS_ASIO_GetDeviceInfos();
            foreach (BASS_ASIO_DEVICEINFO di in devices)
                if (di.name == Name)
                    return true;

            return false;
        }
        public static string[] GetDeviceNames()
        {
            needsUnload = true;

            BASS_ASIO_DEVICEINFO[] devices = BassAsio.BASS_ASIO_GetDeviceInfos();
            string[] devs = new string[devices.Length];
            for (int i = 0; i < devices.Length; i++)
                devs[i] = devices[i].name;

            return devs;
        }
        private int getDeviceNum(string DeviceName)
        {
            BASS_ASIO_DEVICEINFO[] devices = BassAsio.BASS_ASIO_GetDeviceInfos();
            for (int i = 0; i < devices.Length; i++)
                if (devices[i].name == deviceName)
                    return i;

            return -1;
        }
    }
}
