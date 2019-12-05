/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace QuuxControls
{
    internal delegate void Tick();
    
    internal delegate void EmptyDelegate();
    internal delegate void BooleanDelegate(bool Value);

    internal static class Clock
    {
        internal const ulong NULL_ALARM = ulong.MaxValue;

        private const uint MASK_INT = 0xFFFFFFFE;
        private const ulong MASK_LONG = 0xFFFFFFFFFFFFFFFE;

        private static object llock = new object();

        private static System.Windows.Forms.Timer timer = null;
        //private Tick handler;
        private static ulong ticks;
        private static double msecPerTick;
        private static ulong nextCallback = NULL_ALARM;
        private static Dictionary<ulong, EmptyDelegate> pendingCallbacks;

        static Clock()
        {
            timer = new System.Windows.Forms.Timer();
            
            msecPerTick = 1000.0 / 40.0;
            
            ticks = 0;

            pendingCallbacks = new Dictionary<ulong, EmptyDelegate>();

            timer.Interval = (int)msecPerTick;
            timer.Tick += new EventHandler(tick);
            timer.Start();
        }
        public static void Close()
        {
            timer.Stop();
            timer.Dispose();
            timer = null;
        }
        public static void Update(ref ulong Alarm, EmptyDelegate Callback, uint DelayInMsec, bool SeparateThread)
        {
            if (Alarm != NULL_ALARM)
                doSyncAction(ActionType.RemoveNoUpdate, Alarm, null);

            Alarm = Delay(Callback, DelayInMsec, SeparateThread);
        }
        public static ulong Delay(EmptyDelegate Callback, uint DelayInMsec, bool SeparateThread)
        {
            ulong alarm = ticks + (ulong)(DelayInMsec / msecPerTick) + 2;

            // separate threads have even tick counts

            if (SeparateThread)
                alarm &= MASK_LONG;
            else
                alarm |= 0x01;

            return doSyncAction(ActionType.Add, alarm, Callback);
        }
        public static void RemoveAlarm(ulong Alarm)
        {
            doSyncAction(ActionType.Remove, Alarm, null);
        }
        public static ulong TimeRemaining(ulong Alarm)
        {
            if (Alarm == NULL_ALARM)
                return ulong.MaxValue;
            else if (Alarm <= ticks)
                return 0;
            else
                return (ulong)((Alarm - ticks) * msecPerTick);
        }
        private static void tick(object sender, EventArgs e)
        {
            if (++ticks >= nextCallback)
            {
                doSyncAction(ActionType.Invoke, nextCallback, null);
            }
            //handler();
        }

        private static void updateNextCallback()
        {
            if (pendingCallbacks.Count > 0)
                nextCallback = pendingCallbacks.Keys.Min();
            else
                nextCallback = NULL_ALARM;
        }
        private enum ActionType { Add, Remove, RemoveNoUpdate, GetPendingCallback, Invoke }

        private static ulong doSyncAction(ActionType Type, ulong Tick, EmptyDelegate Callback)
        {
            bool separateThread = false;

            lock (llock)
            {
                switch (Type)
                {
                    case ActionType.Add:

                        while (pendingCallbacks.ContainsKey(Tick))
                            Tick += 2;

                        pendingCallbacks.Add(Tick, Callback);
                        updateNextCallback();
                        return Tick;
                    case ActionType.Remove:
                        if (pendingCallbacks.ContainsKey(Tick))
                        {
                            pendingCallbacks.Remove(Tick);
                            updateNextCallback();
                        }
                        return 0;
                    case ActionType.RemoveNoUpdate:
                        if (pendingCallbacks.ContainsKey(Tick))
                            pendingCallbacks.Remove(Tick);
                        break;
                    case ActionType.Invoke:
                        if (pendingCallbacks.ContainsKey(nextCallback))
                        {
                            ulong key = nextCallback;

                            Callback = pendingCallbacks[key];
                            RemoveAlarm(key);
                            separateThread = ((key & 0x01) == 0);
                        }
                        else
                        {
                            updateNextCallback();
                            return 0;
                        }
                        break;
                    default:
                        return 0;
                }
            }
            if (Type == ActionType.Invoke)
            {
                if (separateThread)
                {
                    Thread t = new Thread(new ThreadStart(Callback));
                    t.IsBackground = true;
                    t.Priority = ThreadPriority.BelowNormal;
                    t.Start();
                }
                else
                {
                    Callback();
                }
            }
            return 0;
        }
    }
}
