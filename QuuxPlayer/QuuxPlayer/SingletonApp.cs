/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal delegate void NewInstanceMessageEventHandler(object sender, object message);

    internal sealed class SingletonApp
    {
        public static event NewInstanceMessageEventHandler NewInstanceMessage;

        private const string APP_ID = "QuuxPlayer_AppID";

        private static SingletonApp instance = new SingletonApp();

        private Mutex mutex;
        private bool isFirstInstance;
        private SINativeWindow notifcationWindow;

        private SingletonApp()
        {
            mutex = new Mutex(false, APP_ID, out isFirstInstance);
        }

        public static bool AlreadyExists
        {
            get { return instance.Exists; }
        }
        public static bool NotifyExistingInstance(object message)
        {
            if (instance.Exists)
                return instance.notifyPreviousInstance(message);
            else
                return false;
        }
        public static void Initialize()
        {
            instance.init();
        }
        public static void Close()
        {
            instance.dispose();
        }

        private bool Exists
        {
            get { return !isFirstInstance; }
        }

        private void dispose()
        {
            //release the mutex handle
            mutex.Close();
            //and destroy the window
            if (notifcationWindow != null)
                notifcationWindow.DestroyHandle();
        }
        private void init()
        {
            notifcationWindow = new SINativeWindow();
        }     
        private bool notifyPreviousInstance(object Message)
        {
            //Find the window of the previous instance
            IntPtr winHandle = NativeMethods.FindWindow(null, APP_ID);
            if (winHandle != IntPtr.Zero)
            {
                //create a GCHandle to hold the serialized object. 
                GCHandle bufferHandle = new GCHandle();
                try
                {
                    byte[] buffer;
                    NativeMethods.COPYDATASTRUCT data = new NativeMethods.COPYDATASTRUCT();
                    if (Message != null)
                    {
                        //serialize the object into a byte array
                        buffer = serialize(Message);
                        //pin the byte array in memory
                        bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                        data.dwData = 0;
                        data.cbData = buffer.Length;
                        //get the address of the pinned buffer
                        data.lpData = bufferHandle.AddrOfPinnedObject();
                    }

                    GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                    try
                    {
                        NativeMethods.SendMessage(winHandle, NativeMethods.WM_COPYDATA, IntPtr.Zero, dataHandle.AddrOfPinnedObject());
                        return true;
                    }
                    finally
                    {
                        dataHandle.Free();
                    }
                }
                finally
                {
                    if (bufferHandle.IsAllocated)
                        bufferHandle.Free();
                }
            }
            return false;
        }

        private static object deserialize(byte[] buffer)
        {
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                return new BinaryFormatter().Deserialize(stream);
            }
        }
        private static byte[] serialize(Object obj)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, obj);
                return stream.ToArray();
            }
        }

        private class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
            [DllImport("user32.dll")]
            public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

            public const short WM_COPYDATA = 74;

            public struct COPYDATASTRUCT
            {
                public int dwData;
                public int cbData;
                public IntPtr lpData;
            }
        }
        private class SINativeWindow : NativeWindow
        {
            public SINativeWindow()
            {
                System.Windows.Forms.CreateParams cp = new System.Windows.Forms.CreateParams();
                cp.Caption = APP_ID; //The window title is the same as the Id
                CreateHandle(cp);
            }

            //The window procedure that handles notifications from new application instances
            protected override void WndProc(ref System.Windows.Forms.Message m)
            {
                if (m.Msg == NativeMethods.WM_COPYDATA)
                {
                    //convert the message LParam to the WM_COPYDATA structure
                    NativeMethods.COPYDATASTRUCT data = (NativeMethods.COPYDATASTRUCT)Marshal.PtrToStructure(m.LParam, typeof(NativeMethods.COPYDATASTRUCT));
                    object obj = null;
                    if (data.cbData > 0 && data.lpData != IntPtr.Zero)
                    {
                        //copy the native byte array to a .net byte array
                        byte[] buffer = new byte[data.cbData];
                        Marshal.Copy(data.lpData, buffer, 0, buffer.Length);
                        //deserialize the buffer to a new object
                        obj = deserialize(buffer);
                    }
                    if (NewInstanceMessage != null)
                        NewInstanceMessage.Invoke(this, obj);
                }
                else
                {
                    base.WndProc(ref m);
                }
            }
        }
    }
}

