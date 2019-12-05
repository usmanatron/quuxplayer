/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectSound;

namespace QuuxPlayer
{
    internal sealed class OutputDX : Output
    {
        private Microsoft.DirectX.DirectSound.Device device;
        private Microsoft.DirectX.DirectSound.SecondaryBuffer dxOutputBuffer;
        private Thread playThread;

        private int dxWriteCursor = 0;

        private const int DX_BUFFER_SIZE = Player.READ_BUFFER_SIZE * 8;
        private const int DX_BITS_PER_SAMPLE = 16;
        private const int DX_NUM_CHANNELS = 2;
        private const int MIN_READ_BUFFER_TIME_IN_MSEC = Player.READ_BUFFER_SIZE * 1000 / DX_NUM_CHANNELS * 8 / DX_BITS_PER_SAMPLE / 48000; // at 48K sample rate
        public const int SLEEP_TIME_BETWEEN_BUFFER_CHECKS = MIN_READ_BUFFER_TIME_IN_MSEC / 4;
        
        private const int DEFAULT_SAMPLE_RATE = 44100;

        private byte[] emptyWriteBuffer = new byte[DX_BUFFER_SIZE];
        private bool kill = false;
        
        private Form parentForm;

        public OutputDX(string DeviceName, int Frequency, GetSampleDelegate Callback, Form ParentForm, Player Player)
            : base(DeviceName, Player)
        {
            frequency = Frequency;
            callback = Callback;
            parentForm = ParentForm;
            createDevice();
            playStartThread();

        }

        public static string GetDefaultDeviceName()
        {
            DevicesCollection dc = new DevicesCollection();
            if (dc.Count > 0)
                return dc[0].Description;
            else
                return String.Empty;
        }
        public static string[] GetDeviceNames()
        {
            DevicesCollection dc = new DevicesCollection();

            string[] res = new string[dc.Count];
            int i = 0;
            foreach (DeviceInformation di in dc)
                res[i++] = di.Description;

            return res;
        }
        public override int BufferSize
        {
            get { return DX_BUFFER_SIZE; }
        }
        public override int Frequency
        {
            get
            {
                return frequency;
            }
            set
            {
                if (frequency != value)
                {
                    this.frequency = value;
                    createOutputBuffer();
                }
            }
        }
        public override bool Playing
        {
            get { return !kill; }
        }
        public override void Pause()
        {
            if (this.paused)
                return;

            this.paused = true;
        }
        public override void Resume()
        {
            if (!this.paused)
                return;

            this.paused = false;
        }
        public override void Kill()
        {
            kill = true;
        }
        public override void Dispose()
        {
            kill = true;
            killDXOutputBuffer();
        }

        private void play()
        {
            dxWriteCursor = 0;
            feedBuffer();
            dxOutputBuffer.Play(0, BufferPlayFlags.Looping);

            try
            {
                while (!kill)
                {
                    if (needsFeed)
                    {
                        if (paused)
                        {
                            feedSilence();
                        }
                        else
                        {
                            feedBuffer();
                        }
                    }
                    System.Threading.Thread.Sleep(OutputDX.SLEEP_TIME_BETWEEN_BUFFER_CHECKS);
                }
            }
            catch (Microsoft.DirectX.DirectSound.NoDriverException e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
                resetDevice();
                playStartThread();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }
        private void resetDevice()
        {
            try
            {
                Kill();
                killDXOutputBuffer();
            }
            catch
            {
            }
            Clock.DoOnMainThread(createDevice, 10);
        }
        private void createDevice()
        {
            int tries = 0;

            while (tries < 2)
            {
                try
                {
                    if (deviceName.Length > 0)
                    {
                        Guid g = getDeviceGUID();
                        if (g == Guid.Empty)
                            device = new Device();
                        else
                            device = new Device(g);
                    }
                    else
                    {
                        device = new Device();
                    }

                    device.SetCooperativeLevel(parentForm, CooperativeLevel.Priority);
                    if (createOutputBuffer())
                        return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
                tries++;
                System.Threading.Thread.Sleep(1000);
            }
        }
        public bool createOutputBuffer()
        {
            int tries = 0;

            while (tries < 2)
            {
                try
                {
                    if (dxOutputBuffer == null)
                    {
                        BufferDescription bufDesc = new BufferDescription();

                        bufDesc.Format = createWaveFormat(Frequency, DX_BITS_PER_SAMPLE, DX_NUM_CHANNELS);

                        bufDesc.BufferBytes           = DX_BUFFER_SIZE;
                        bufDesc.ControlFrequency      = true;
                        bufDesc.ControlPositionNotify = false;
                        bufDesc.ControlEffects        = true;
                        bufDesc.ControlPan            = false;
                        bufDesc.ControlVolume         = true;
                        bufDesc.GlobalFocus           = true;
                        bufDesc.PrimaryBuffer         = false;

                        dxOutputBuffer = new Microsoft.DirectX.DirectSound.SecondaryBuffer(bufDesc, device);
                    }
                    else if (dxOutputBuffer.Frequency != frequency)
                    {
                        dxOutputBuffer.Frequency = frequency;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    tries++;
                    System.Threading.Thread.Sleep(2000);
                }
            }
            return false;
        }
        public static WaveFormat createWaveFormat(int samplingRate, short bitsPerSample, short numChannels)
        {
            WaveFormat wf = new WaveFormat();

            wf.FormatTag = WaveFormatTag.Pcm;
            wf.SamplesPerSecond = samplingRate;
            wf.BitsPerSample = bitsPerSample;
            wf.Channels = numChannels;

            wf.BlockAlign = (short)(wf.Channels * (wf.BitsPerSample / 8));
            wf.AverageBytesPerSecond = wf.SamplesPerSecond * wf.BlockAlign;

            return wf;
        }
        public void feedSilence()
        {
            dxOutputBuffer.Write(0, emptyWriteBuffer, LockFlag.None);
            dxWriteCursor = 0;
        }
        public bool needsFeed
        {
            get
            {
                return (dxOutputBuffer.PlayPosition > dxWriteCursor + Player.READ_BUFFER_SIZE) ||
                        ((dxOutputBuffer.PlayPosition < dxWriteCursor) &&
                         (dxOutputBuffer.PlayPosition > Player.READ_BUFFER_SIZE));
            }
        }
        public void feedBuffer()
        {
            byte[] buffer;

            int bytesReturned = callback(out buffer);

            if (Mute)
            {
                buffer = player.EmptyReadBuffer16;
            }

            if (bytesReturned > 0)
            {
                if (bytesReturned < Player.READ_BUFFER_SIZE)
                {
                    byte[] from = buffer;
                    buffer = new byte[bytesReturned];
                    Array.Copy(from, buffer, bytesReturned);
                }
                dxOutputBuffer.Write(dxWriteCursor, buffer, LockFlag.None);
                dxWriteCursor = (dxWriteCursor + bytesReturned) % DX_BUFFER_SIZE;
            }
        }
        private void playStartThread()
        {
            playThread = new System.Threading.Thread(play);
            playThread.Name = "DX Output Thread";
            playThread.Priority = ThreadPriority.Highest;
            playThread.IsBackground = true;
            playThread.Start();
        }
        private void killDXOutputBuffer()
        {
            if (dxOutputBuffer != null)
            {
                try
                {
                    dxOutputBuffer.Stop();
                    dxOutputBuffer.Dispose();
                }
                catch
                {
                }
                finally
                {
                    dxOutputBuffer = null;
                }
            }
        }
        private Guid getDeviceGUID()
        {
            foreach (DeviceInformation di in new DevicesCollection())
                if (di.Description == deviceName)
                    return di.DriverGuid;

            return Guid.Empty;
        }
    }
}
