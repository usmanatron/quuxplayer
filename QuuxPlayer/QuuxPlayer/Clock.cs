/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal delegate void Tick();
    internal delegate void Callback();

    internal static class Clock
    {
        private enum ActionType { Add, Remove, RemoveNoUpdate, GetPendingCallback, Invoke }

        public const ulong NULL_ALARM = ulong.MaxValue;
        
        private const uint MASK_INT = 0xFFFFFFFE;
        private const ulong MASK_LONG = 0xFFFFFFFFFFFFFFFE;

        private static object llock = new object();
        private static System.Windows.Forms.Timer timer = null;
        private static Tick handler;
        private static ulong ticks;
        private static double msecPerTick;
        private static ulong nextCallback = NULL_ALARM;
        private static Dictionary<ulong, Callback> pendingCallbacks = new Dictionary<ulong, Callback>();
        private static List<Callback> immediateMainThreadCallbacks = new List<Callback>();
        private static object mainThreadCallbackLock = new object();

        public static void Start(Tick Handler)
        {
            handler = Handler;

            timer = new System.Windows.Forms.Timer();
            msecPerTick = 1000.0 / 29.0; // 29 fps
            
            ticks = 0;
            timer.Interval = (int)msecPerTick * 88 / 100; // adjustment since the windows timer is pretty inaccurate
            timer.Tick += new EventHandler(tick);
            timer.Start();
        }
        public static void Close()
        {
            timer.Stop();
            timer.Dispose();
            timer = null;
        }
        public static void Update(ref ulong Alarm, Callback Callback, uint DelayInMsec, bool SeparateThread)
        {
            if (Alarm != NULL_ALARM)
                doSyncAction(ActionType.RemoveNoUpdate, Alarm, null);

            Alarm = delay(Callback, DelayInMsec, SeparateThread);
        }
        public static void DoOnNewThread(Callback Callback)
        {
            System.Threading.Thread t = new System.Threading.Thread(new ThreadStart(Callback));
            t.Name = "New Thread";
            t.Priority = ThreadPriority.BelowNormal;
            t.IsBackground = true;
            t.Start();
        }
        public static void DoOnMainThread(Callback Callback)
        {
            //delay(Callback, 0, false);
            lock (mainThreadCallbackLock)
            {
                immediateMainThreadCallbacks.Add(Callback);
            }
        }
        public static void DoOnNewThreadNotBackground(Callback Callback)
        {
            Thread t = new Thread(new ThreadStart(Callback));
            t.IsBackground = false;
            t.Priority = ThreadPriority.Normal;
            t.Start();
        }
        public static void RemoveAlarm(ulong Alarm)
        {
            doSyncAction(ActionType.Remove, Alarm, null);
        }
        public static void RemoveAlarm(ref ulong Alarm)
        {
            if (Alarm < NULL_ALARM)
            {
                ulong alarm = Alarm;
                Alarm = NULL_ALARM;
                doSyncAction(ActionType.Remove, alarm, null);
            }
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
        public static ulong DoOnNewThread(Callback Callback, uint DelayInMsec)
        {
            try
            {
                return delay(Callback, DelayInMsec, true);
            }
            catch (Exception ex)
            {
                Lib.ExceptionToClipboard(ex);
                return NULL_ALARM;
            }
        }
        public static ulong DoOnMainThread(Callback Callback, uint DelayInMsec)
        {
            try
            {
                return delay(Callback, DelayInMsec, false);
            }
            catch (Exception ex)
            {
                Lib.ExceptionToClipboard(ex);
                return NULL_ALARM;
            }
        }

        private static ulong delay(Callback Callback, uint DelayInMsec, bool SeparateThread)
        {
            try
            {
                System.Diagnostics.Debug.Assert(msecPerTick > 10);
                ulong alarm = ticks + (ulong)(DelayInMsec / msecPerTick) + 2;

                // separate threads have even tick counts

                if (SeparateThread)
                    alarm &= MASK_LONG;
                else
                    alarm |= 0x01;

                return doSyncAction(ActionType.Add, alarm, Callback);
            }
            catch (Exception ex)
            {
                Lib.ExceptionToClipboard(ex);
                return NULL_ALARM;
            }
        }
        private static void tick(object sender, EventArgs e)
        {
            try
            {
                if (immediateMainThreadCallbacks.Count > 0)
                {
                    // Max one immediate callback per tick FIFO
                    Callback ed;
                    lock (mainThreadCallbackLock)
                    {
                        ed = immediateMainThreadCallbacks[0];
                        immediateMainThreadCallbacks.RemoveAt(0);
                    }
                    ed();
                }
                if (++ticks >= nextCallback)
                {
                    doSyncAction(ActionType.Invoke, nextCallback, null);
                }
                handler();
            }
            catch (Exception ex)
            {
                Lib.ExceptionToClipboard(ex);
            }
        }
        private static void updateNextCallback()
        {
            if (pendingCallbacks.Count > 0)
                nextCallback = pendingCallbacks.Keys.Min();
            else
                nextCallback = Clock.NULL_ALARM;
        }
        
        private static ulong doSyncAction(ActionType Type, ulong Tick, Callback Callback)
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
                    Thread t = new Thread(TryCallback);
#if DEBUG
                    t.Name = "Clock Thread " + (++threadCount).ToString();
#endif
                    t.IsBackground = true;
                    t.Priority = ThreadPriority.BelowNormal;
                    t.Start(Callback);
                }
                else
                {
                    Callback();
                }
            }
            return 0;
        }
        private static void TryCallback(object TheCallback)
        {
            try
            {
                ((Callback)TheCallback)();
            }
            catch (Exception ex)
            {
                Lib.ExceptionToClipboard(ex);
            }
        }
#if DEBUG
        private static int threadCount = 0;
#endif
    }
}
