/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace QuuxPlayer
{
    internal sealed class Sleep
    {
        public enum ActionType { None, ShutDown, ExitApp, StandBy, Hibernate }

        public bool Active { get; set; }
        public bool FadeActive { get; set; }

        public DateTime Alarm { get; set; }
        public DateTime Fade { get; set; }

        public ActionType Action { get; set; }

        public bool Force { get; set; }

        public float OriginalVolumeDB { get; private set; }

        private ulong nextTick = Clock.NULL_ALARM;

        private bool fading = false;

        public override string ToString()
        {
            if (Active)
            {
                float time = (float)(Math.Round((Alarm - DateTime.Now).TotalMinutes, 1));

                if (time > 10f)
                    time = (float)Math.Round(time, 0);

                string preface = String.Empty;

                switch (Action)
                {
                    case ActionType.ShutDown:
                        preface = Localization.Get(UI_Key.Sleep_Shutdown);
                        break;
                    case ActionType.ExitApp:
                        preface = Localization.Get(UI_Key.Sleep_Exit);
                        break;
                    case ActionType.Hibernate:
                        preface = Localization.Get(UI_Key.Sleep_Hibernate);
                        break;
                    case ActionType.StandBy:
                        preface = Localization.Get(UI_Key.Sleep_Standby);
                        break;
                }
                if (time == 1.0f)
                    return preface + time.ToString() + " " + Localization.Get(UI_Key.Sleep_Minute) + ": " + Alarm.ToShortTimeString();
                else
                    return preface + time.ToString() + " " + Localization.Get(UI_Key.Sleep_Minutes) + ": " + Alarm.ToShortTimeString();
            }
            else
            {
                return Localization.Get(UI_Key.Sleep_Off);
            }
        }

        private Controller controller;

        public Sleep(Controller Controller)
        {
            this.controller = Controller;

            Active = true;
            FadeActive = true;
            Alarm = DateTime.Now + TimeSpan.FromMinutes(60);
            Fade = DateTime.Now + TimeSpan.FromMinutes(0);
            Force = false;

            Action = ActionType.ShutDown;
        }
        public Sleep(Controller Controller, bool Active, int Alarm, bool Fade, int FadeAlarm, ActionType Action, bool Force) : this(Controller)
        {
            this.Active = Active;
            this.FadeActive = Fade;
            this.Alarm = DateTime.Now + TimeSpan.FromMinutes(Alarm);
            this.Fade = DateTime.Now + TimeSpan.FromMinutes(FadeAlarm);
            this.Action = Action;
            this.Force = Force;
        }
        public void Go(float CurrentVolume)
        {
            OriginalVolumeDB = CurrentVolume;

            fading = false;

            if (!Active)
                return;

            Stop();

            this.Active = true;

            if (DateTime.Now > Alarm)
            {
                doIt();
            }
            else
            {
                nextTick = Clock.DoOnMainThread(tick, getTickDelay());
                //updateDisplay();
            }
        }
        private void tick()
        {
            if (Active)
            {
                if (FadeActive && !fading && DateTime.Now > Fade)
                {
                    fading = true;
                }
                if (fading)
                {
                    controller.RequestAction(QActionType.VolumeDownForSleep);
                }
                if (DateTime.Now > Alarm)
                {
                    doIt();
                }
                nextTick = Clock.DoOnMainThread(tick, getTickDelay());
                controller.RequestAction(QActionType.SetMainFormTitle);
            }
        }
        private uint getTickDelay()
        {
            if (FadeActive && !fading)
            {
                return (uint)Math.Min(60000, Math.Max(2000, (Fade - DateTime.Now).TotalMilliseconds / 100));
            }
            else
            {
                return (uint)Math.Min(60000, Math.Max(2000, (Alarm - DateTime.Now).TotalMilliseconds / 100));
            }
        }
        public void Stop()
        {
            fading = false;

            if (nextTick != Clock.NULL_ALARM)
            {
                Clock.RemoveAlarm(ref nextTick);
            }
            Active = false;
        }
        private void doIt()
        {
            Stop();

            controller.RequestAction(QActionType.Stop);
            WinAudioLib.VolumeDB = OriginalVolumeDB;

            controller.RequestAction(QActionType.Exit);

            switch (Action)
            {
                case ActionType.StandBy:
                    Lib.ExitWindows(Force, Lib.ShutDownMethod.Standby);
                    break;
                case ActionType.Hibernate:
                    Lib.ExitWindows(Force, Lib.ShutDownMethod.Hibernate);
                    break;
                case ActionType.ShutDown:
                    Lib.ExitWindows(Force, Lib.ShutDownMethod.Shutdown);
                    break;
            }
        }
    }
}
