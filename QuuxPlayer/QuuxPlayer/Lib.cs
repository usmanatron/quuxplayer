/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal static class Lib
    {
        public delegate void MessageDelegate(string Message);

        private const string MutexName = "quuxplayer";
        private const int SPI_GETSCREENSAVERACTIVE = 16;
        private const int SPI_SETSCREENSAVERACTIVE = 17;
        private const int SPIF_SENDWININICHANGE = 2;

        //GUID_VIDEO_POWERDOWN_TIMEOUT

        // not const -- prevents obfuscation
        public static readonly string PRODUCT_URL = "http://www.quuxplayer.com";
        private static readonly string DATA_PATH = "QuuxPlayer";

        private static string programPath = Path.GetDirectoryName(Application.ExecutablePath);

        private static string commonPath;
        private static string localPath;

        public static Form MainForm { get; set; }

        private static bool isVista = (Environment.OSVersion.Version.Major >= 6);

        static Lib()
        {
            commonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), DATA_PATH);
            localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DATA_PATH);

            try
            {
                if (!Directory.Exists(commonPath))
                    Directory.CreateDirectory(commonPath);
            }
            catch { }

            try
            {
                if (!Directory.Exists(localPath))
                    Directory.CreateDirectory(localPath);
            }
            catch { }

            Lib.FullScreen = false;

            ManualReleaseFullScreen = false;
        }
        public static string ReplaceBadFilenameChars(string s)
        {
            int badChar;

            char[] badChars = Path.GetInvalidFileNameChars();

            do
            {
                badChar = s.IndexOfAny(badChars);

                if (badChar >= 0)
                    s = s.Substring(0, badChar) + ((s.Length > badChar) ? s.Substring(badChar + 1) : String.Empty);
            }
            while (badChar >= 0);
            return s;
        }
        public static void ExceptionToClipboard(Exception Exception)
        {
            {
                Clipboard.SetText("Error Starting QuuxPlayer" + Environment.NewLine + Environment.NewLine +
                                  "Message:" + Exception.Message + Environment.NewLine + Environment.NewLine +
                                  "Source: " + Exception.Source + Environment.NewLine +
                                  "Stack Trace: " + Exception.StackTrace +
                                  "Target Site: " + Exception.TargetSite +
                                  Exception.ToString());

            }
            QMessageBox.Show(null, "Error in QuuxPlayer. Details have been copied to the Windows clipboard.", "QuuxPlayer Error", QMessageBoxIcon.Error);
        }
        public static bool IsVistaOrLater
        {
            get { return isVista; }
        }
        public static string CommonPath(string FileName)
        {
            return Path.Combine(commonPath, FileName);
        }
        public static string LocalPath(string FileName)
        {
            return Path.Combine(localPath, FileName);
        }
        
        public static string ProgramPath(string LocalFile)
        {
            return Path.Combine(programPath, LocalFile);
        }
        public static void Run(string ExecutablePath)
        {
            Run(ExecutablePath, String.Empty);
        }
        public static void Run(string ExecutablePath, string Args)
        {
            System.Diagnostics.Process p;
            if (Args.Length > 0)
                p = System.Diagnostics.Process.Start(ExecutablePath, Args);
            else
                p = System.Diagnostics.Process.Start(ExecutablePath);
        }
        private static float terabyte = 1024f * 1024f * 1024f * 1024f;
        private static float gigabyte = 1024f * 1024f * 1024f;
        private static float megabyte = 1024f * 1024f;
        private static float kilobyte = 1024f;
        public static string GetTotalFileSizeString(float Bytes)
        {
            if (Bytes > terabyte)
                return (Bytes / terabyte).ToString("0.0") + " TB";
            if (Bytes > gigabyte)
                return (Bytes / gigabyte).ToString("0.0") + " GB";
            else if (Bytes > megabyte)
                return (Bytes / megabyte).ToString("0.0") + " MB";
            else if (Bytes > kilobyte)
                return (Bytes / kilobyte).ToString("0.0") + " KB";
            else
                return ((int)Bytes).ToString() + " Bytes";

        }
        public static string GetTimeStringLong(long Milliseconds)
        {
            if (Milliseconds < 0)
                return Localization.ZERO_TIME;

            Milliseconds += 500;

            long seconds = (Milliseconds / 1000) % 60;
            long minutes = (Milliseconds / (1000 * 60)) % 60;
            long hours = (Milliseconds / (1000 * 60 * 60)) % 24;
            long days = Milliseconds / (1000 * 60 * 60 * 24);

            if (days > 0)
                return days.ToString() + " " + (days == 1 ? Localization.DAY : Localization.DAYS) + ", " + hours.ToString() + " " + (hours == 1 ? Localization.HOUR : Localization.HOURS) + ", " + minutes.ToString() + " " + (minutes == 1 ? Localization.MINUTE : Localization.MINUTES);
                //return days.ToString() + " " + (days == 1 ? Localization.DAY : Localization.DAYS) + ", " + hours.ToString() + " Hr, " + minutes.ToString() + " Min";
            else if (hours > 0)
                return hours.ToString() + " " + (hours == 1 ? Localization.HOUR : Localization.HOURS) + ", " + minutes.ToString() + " " + (minutes == 1 ? Localization.MINUTE : Localization.MINUTES);
            else
                return minutes.ToString() + " " + (minutes == 1 ? Localization.MINUTE : Localization.MINUTES);
        }
        public static string GetTimeString(int Milliseconds)
        {
            if (Milliseconds < 0)
                return "0:00";

            Milliseconds += 500;

            int hours = Milliseconds / (1000 * 60 * 60);
            int minutes = (Milliseconds % (1000 * 60 * 60)) / (1000 * 60);
            int seconds = (Milliseconds % (1000 * 60)) / 1000;

            if (hours > 0)
                return hours.ToString() + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");
            else
                return minutes.ToString() + ":" + seconds.ToString("00");
        }
        public static string GetTimeStringFractional(int Milliseconds)
        {
            if (Milliseconds < 0)
                return "0:00:00";

            int hours = (int)(Milliseconds / (1000 * 60 * 60));
            int minutes = (int)((Milliseconds % (1000 * 60 * 60)) / (1000 * 60));
            int seconds = (int)((Milliseconds % (1000 * 60)) / 1000);
            int fractions = (int)((Milliseconds % 1000) / 10);

            if (hours > 0)
                return hours.ToString() + ":" + minutes.ToString("00") + ":" + seconds.ToString("00") + ":" + fractions.ToString("00");
            else
                return minutes.ToString() + ":" + seconds.ToString("00") + ":" + fractions.ToString("00");
        }

        public static void AddTracksToList(DirectoryInfo DI, List<Track> Tracks, MessageDelegate ShowMessage)
        {
            string adding = Localization.Get(UI_Key.Lib_Adding) + " ";

            foreach (FileInfo fi in DI.GetFiles())
            {
                Track t = Track.Load(fi.FullName);
                if (t != null)
                {
                    Tracks.Add(t);
                    ShowMessage(adding + Tracks.Count.ToString() + ": " + t.ToString());
                    Lib.DoEvents();
                }
            }
            foreach (DirectoryInfo di in DI.GetDirectories())
            {
                AddTracksToList(di, Tracks, ShowMessage);
            }
        }

        public static OpenFileDialog GetOpenFileDialog(string StartingPath, bool IsDirectory, string Filter, int FilterIndex)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            if (IsDirectory)
            {
                ofd.FileName = String.Empty;
                ofd.InitialDirectory = StartingPath;
            }
            else
            {
                ofd.FileName = StartingPath;
            }
            ofd.AutoUpgradeEnabled = true;
            ofd.ValidateNames = true;
            ofd.Multiselect = false;
            ofd.Filter = Filter;
            try
            {
                ofd.FilterIndex = FilterIndex;
            }
            catch { }
            ofd.CheckPathExists = true;
            ofd.AddExtension = true;
            return ofd;
        }

        public static char FirstCharUpper(string Input)
        {
            if (Input.Length > 0)
                return Char.ToUpperInvariant(Input[0]);
            else
                return '\0';
        }
        public static char FirstCharNoTheUpper(string Input)
        {
            Char c = '\0';
            if (Input.StartsWith("the ", StringComparison.InvariantCultureIgnoreCase) && Input.Length > 4)
            {
                c = Char.ToUpperInvariant(Input[4]);
            }
            else if (Input.Length > 0)
            {
                c = Char.ToUpperInvariant(Input[0]);
            }
            return c;
        }

        [DllImport("shlwapi.dll")]
        public static extern int ColorHLSToRGB(int H, int L, int S);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SystemParametersInfo(int uAction, int uParam, ref int lpvParam, int fuWinIni);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        
        private const byte VK_LSHIFT = 0xA0;
        private const int KEYEVENTF_KEYUP = 0x02;

        public static void DoFakeKeystroke()
        {
            keybd_event(VK_LSHIFT, 0x45, 0, UIntPtr.Zero);
            keybd_event(VK_LSHIFT, 0x45, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        public static bool ScreenSaverIsActive
        {
            get
            {
                int isActive = 0;

                SystemParametersInfo(SPI_GETSCREENSAVERACTIVE, 0, ref isActive, 0);

                return isActive != 0;
            }
            set
            {
                return;
                /*
                int active = value ? 1 : 0;
                int nullVar = 0;

                SystemParametersInfo(SPI_SETSCREENSAVERACTIVE, active, ref nullVar, SPIF_SENDWININICHANGE);
                 */
            }
        }

        [DllImport("user32.dll", EntryPoint = "GetSystemMetrics")]
        public static extern int GetSystemMetrics(int which);

        [DllImport("user32.dll")]
        public static extern void SetWindowPos(IntPtr hwnd, IntPtr hwnInsertAfter, int X, int Y, int width, int height, uint flags);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;
        private static IntPtr HWND_TOP = IntPtr.Zero;
        private const int SWP_SHOWWINDOW = 64;

        public static bool FullScreen { get; private set; }

        public static bool ManualReleaseFullScreen { get; set; }
        public static void SetWindowFullScreen(Form Form, bool MakeFullScreen, FormBorderStyle BorderStyle, bool ManualRelease)
        {
            try
            {
                if (Lib.FullScreen != MakeFullScreen)
                {
                    Screen s;

                    s = getContainingScreen(Form);

                    Lib.FullScreen = MakeFullScreen;

                    if (Lib.FullScreen)
                    {
                        Form.WindowState = FormWindowState.Maximized;
                        Form.FormBorderStyle = FormBorderStyle.None;
                        Form.TopMost = true;

                        SetWindowPos(Form.Handle,
                             HWND_TOP,
                             s.Bounds.Left,
                             s.Bounds.Top,
                             s.Bounds.Width,
                             s.Bounds.Height,
                             SWP_SHOWWINDOW);

                    }
                    else
                    {
                        Form.TopMost = false;
                        Form.FormBorderStyle = BorderStyle;
                        Form.WindowState = FormWindowState.Normal;

                        Rectangle r = Form.Bounds;

                        r.Intersect(getLargeArea(s));
                        Form.Bounds = r;
                    }
                    EnableNavKeys(!Lib.FullScreen);
                    ManualReleaseFullScreen = ManualRelease;
                }
            }
            catch { }
        }
        public static void MakeMainFormForeground()
        {
            MainForm.Visible = true;
            if (Lib.FullScreen)
                MainForm.WindowState = FormWindowState.Maximized;
            else
                MainForm.WindowState = FormWindowState.Normal;
            SetForegroundWindow(MainForm.Handle);
        }
        private static Rectangle getLargeArea(Screen s)
        {
            return new Rectangle(s.WorkingArea.Left + 8,
                                                     s.WorkingArea.Top + 8,
                                                     s.WorkingArea.Width - 16,
                                                     s.WorkingArea.Height - 16);
        }
        private static Screen getContainingScreen(Form Form)
        {
            Screen s;
            s = Screen.PrimaryScreen;

            Point p = new Point(Form.Location.X + 30, Form.Location.Y + 30);

            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                if (Screen.AllScreens[i].Bounds.Contains(p))
                {
                    s = Screen.AllScreens[i];
                    break;
                }
            }
            return s;
        }
        public static Rectangle MakeVisible(Rectangle Input)
        {
            foreach (Screen s in Screen.AllScreens)
                if (s.Bounds.Contains(Input))
                    return Input;

            foreach (Screen s in Screen.AllScreens)
                if (s.Bounds.IntersectsWith(Input))
                    return makeVisible(Input, s.Bounds);

            Rectangle r = Screen.PrimaryScreen.Bounds;

            return new Rectangle((r.Width - Input.Width) / 2, (r.Height - Input.Height) / 2, Input.Width, Input.Height);
        }
        private static Rectangle makeVisible(Rectangle Input, Rectangle CandidateArea)
        {
            Rectangle r = CandidateArea;
            r.Intersect(Input);

            r = new Rectangle(r.Location, Input.Size);

            if (r.Right > CandidateArea.Right)
                r = new Rectangle(new Point(CandidateArea.Right - r.Width, r.Top), r.Size);

            if (r.Bottom > CandidateArea.Bottom)
                r = new Rectangle(new Point(r.Left, CandidateArea.Bottom - r.Height), r.Size);

            return r;
        }
        public static string GetVersion()
        {
            return Application.ProductVersion;
        }
        public static string GetUserSelectedFolder(string Caption, string Default, bool ShowNewFolderButton)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();

            fbd.Description = Caption;
            fbd.ShowNewFolderButton = false;

            Track t = Controller.GetInstance().GetTracklistFirstSelectedOrFirst;

            fbd.RootFolder = Environment.SpecialFolder.Desktop;

            fbd.ShowNewFolderButton = ShowNewFolderButton;

            if (Default.Length > 0)
                fbd.SelectedPath = Default;
            else if (t == null || !File.Exists(t.FilePath))
                fbd.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            else
                fbd.SelectedPath = Path.GetDirectoryName(t.FilePath);

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                return fbd.SelectedPath;
            }
            else
            {
                return String.Empty;
            }
        }

        // SHUTDOWN CODE

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct TokPriv1Luid
        {
            public int Count;
            public long Luid;
            public int Attr;
        }
        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool OpenProcessToken(IntPtr h, int acc, ref IntPtr phtok);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool LookupPrivilegeValue(string host, string name, ref long pluid);

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool AdjustTokenPrivileges(IntPtr htok,
                                                          bool disall,
                                                          ref TokPriv1Luid newst,
                                                          int len,
                                                          IntPtr prev,
                                                          IntPtr relen);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool ExitWindowsEx(int flg, int rea);

        internal const int SE_PRIVILEGE_ENABLED = 0x00000002;
        internal const int TOKEN_QUERY = 0x00000008;
        internal const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;
        internal const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
        internal const int EWX_LOGOFF = 0x00000000;
        internal const int EWX_SHUTDOWN = 0x00000001;
        internal const int EWX_REBOOT = 0x00000002;
        internal const int EWX_FORCE = 0x00000004;
        internal const int EWX_POWEROFF = 0x00000008;
        internal const int EWX_FORCEIFHUNG = 0x00000010;

        public enum ShutDownMethod { Shutdown, Standby, Hibernate }
        public static void ExitWindows(bool Force, ShutDownMethod Method)
        {
            if (Method == ShutDownMethod.Standby)
            {
                Application.SetSuspendState(PowerState.Suspend, Force, false);
            }
            else if (Method == ShutDownMethod.Hibernate)
            {
                Application.SetSuspendState(PowerState.Hibernate, Force, false);
            }
            else
            {
                bool ok;

                TokPriv1Luid tp;
                IntPtr hproc = GetCurrentProcess();
                IntPtr htok = IntPtr.Zero;

                ok = OpenProcessToken(hproc, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref htok);
                tp.Count = 1;
                tp.Luid = 0;
                tp.Attr = SE_PRIVILEGE_ENABLED;
                ok = LookupPrivilegeValue(null, SE_SHUTDOWN_NAME, ref tp.Luid);
                ok = AdjustTokenPrivileges(htok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
                if (Force)
                    ok = ExitWindowsEx(EWX_POWEROFF | EWX_FORCE, 0);
                else
                    ok = ExitWindowsEx(EWX_POWEROFF, 0);
            }
        }

        // SINGLETON CODE

        public static System.Threading.Mutex GetMutex()
        {
            System.Threading.Mutex mutex = null;

            try
            {
                mutex = System.Threading.Mutex.OpenExisting(MutexName);
            }
            catch (System.Threading.WaitHandleCannotBeOpenedException)
            {
                // Cannot open the mutex because it doesn't exist
            }

            if (mutex == null)
            {
                mutex = new System.Threading.Mutex(true, MutexName);
                GC.KeepAlive(mutex);
                return mutex;
            }
            else
            {
                mutex.Close();
                return null;
            }
        }
        
        public static void DoEvents()
        {
            Application.DoEvents();
        }

        // BEEP

        public static void Beep()
        {
            System.Media.SoundPlayer myPlayer = new System.Media.SoundPlayer();
            myPlayer.SoundLocation = Lib.ProgramPath("beep.wav");
            myPlayer.Play();
        }

        // LOCKDOWN

        private static KeyboardLock keyLock = null;
        public static void EnableNavKeys(bool Enable)
        {
            if (!Enable)
            {
                if (keyLock == null)
                    keyLock = new KeyboardLock();
            }
            else
            {
                if (keyLock != null)
                {
                    keyLock.Dispose();
                    keyLock = null;
                }
            }
        }

        // IMAGES

        public static Image MakeSafeImage(Image Input)
        {
            Image i = null;

            System.Diagnostics.Debug.WriteLine(Input.RawFormat.ToString());

            Guid g = Input.RawFormat.Guid;

            if (g == ImageFormat.Jpeg.Guid ||
                g == ImageFormat.Gif.Guid ||
                g == ImageFormat.Png.Guid ||
                g == ImageFormat.Tiff.Guid ||
                g == ImageFormat.Bmp.Guid)
            {
                i = Input;
            }
            else
            {
                try
                {
                    i = convertToJpeg(Input);
                    System.Diagnostics.Debug.Assert(i.RawFormat.Guid == ImageFormat.Jpeg.Guid);

                    if (i == null)
                    {
                        string fn = Path.GetTempFileName();
                        Input.Save(fn, ImageFormat.Jpeg);

                        Clock.DoOnNewThread(deleteFile, 1000); // can't delete right away
                        i = Image.FromFile(fn);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
            }
            System.Diagnostics.Debug.Assert(i.RawFormat.Guid != ImageFormat.MemoryBmp.Guid);
            return i;
        }
        private static List<string> filesToDelete = new List<string>();
        public static Image convertToJpeg(Image Input)
        {
            MemoryStream ms = new MemoryStream();
            ImageCodecInfo ici = ImageCodecInfo.GetImageEncoders().First(ie => ie.FilenameExtension.ToLowerInvariant().Contains("jpg"));
            EncoderParameters ep = new EncoderParameters();
            ep.Param = new EncoderParameter[1];
            ep.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)90);
            Input.Save(ms,
                   ici,
                   ep);
            ms.Position = 0;
            return Image.FromStream(ms);
        }
        private static void deleteFile()
        {
            try
            {
                if (filesToDelete.Count > 0)
                {
                    File.Delete(filesToDelete[0]);
                    filesToDelete.RemoveAt(0);
                }
            }
            catch
            {
                Clock.DoOnNewThread(deleteFile, 60000);
            }
        }
    }
}
