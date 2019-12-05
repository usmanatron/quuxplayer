/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal delegate int GetSampleDelegate(out byte[] Data);

    internal abstract class Output : IDisposable
    {
        protected int frequency;
        protected GetSampleDelegate callback;
        protected Player player;
        protected bool paused = false;
        protected string deviceName;

        protected Output(string DeviceName, Player Player)
        {
            this.deviceName = DeviceName;
            this.player = Player;
        }
        public static Output GetOutput(string Name, int Frequency, Form ParentForm, GetSampleDelegate Callback16, GetSampleDelegate Callback24, Player Player)
        {
            try
            {
                if (OutputASIO.DeviceExists(Name))
                {
                    Output o = new OutputASIO(Name, Frequency, Callback24, Callback16, Player);

                    if (o != null && o.Playing)
                    {
                        return o;
                    }
                    else
                    {
                        Controller.ShowMessage("ASIO output failed; using default output device.");
                        return new OutputDX(String.Empty, Frequency, Callback16, ParentForm, Player);
                    }
                }
            }
            catch { }

            return new OutputDX(Name, Frequency, Callback16, ParentForm, Player);
        }
        public virtual int Frequency
        {
            get { return frequency; }
            set { }
        }
        public virtual bool Paused
        {
            get
            {
                System.Diagnostics.Debug.Assert(this is OutputDX);
                return paused;
            }
            set
            {
                if (this.Paused != value)
                {
                    if (this.Paused)
                        Resume();
                    else
                        Pause();
                }
            }
        }
        public bool Mute { get; set; }
        public abstract bool Playing { get; }
        public abstract int BufferSize { get; }
        public abstract void Pause();
        public abstract void Resume();
        public abstract void Kill();

        public abstract void Dispose();
    }
}
