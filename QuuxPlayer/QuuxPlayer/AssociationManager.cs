/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace QuuxPlayer
{
    internal static class AssociationManager
    {
        private static RegistryKey FileExts
        {
            get
            {
                RegistryKey extKey = GetRegKey(Registry.CurrentUser, "Software", true);
                extKey = GetRegKey(extKey, "Microsoft", true);
                extKey = GetRegKey(extKey, "Windows", true);
                extKey = GetRegKey(extKey, "CurrentVersion", true);
                extKey = GetRegKey(extKey, "Explorer", true);
                extKey = GetRegKey(extKey, "FileExts", true);

                return extKey;
            }
        }
        private static RegistryKey HKCU_S_C
        {
            get
            {
                try
                {
                    return Registry.CurrentUser.CreateSubKey("Software").CreateSubKey("Classes");
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.ToString());
                    return Registry.ClassesRoot;
                }
            }
        }
        
        public static void ShowAssociationUI()
        {
            try
            {
                IApplicationAssociationRegistrationUI aar;
                if ((aar = new ApplicationAssociationRegistrationUI() as IApplicationAssociationRegistrationUI) != null)
                {
                    aar.LaunchAdvancedAssociationUI("QuuxPlayer");
                }
            }
            catch
            {
            }
        }
        public static bool IsAssociated(string ProgramID, string ExecutablePath, string Extension)
        {
            if (!Lib.IsVistaOrLater) // XP
            {
                RegistryKey k = GetRegKey(HKCU_S_C, Extension, false);
                return GetRegValue(k, String.Empty) == ProgramID;
            }
            else // Vista+
            {
                return isAssociatedVista(Extension);
            }
        }
        public static void Associate(bool Associate, string ProgramID, string ExecutablePath, params string[] Extensions)
        {
            foreach (string ext in Extensions)
            {
                if (Lib.IsVistaOrLater)
                {
                    setAssociationVista(Associate, ProgramID, ext);
                }
                else
                {
                    RegistryKey root = HKCU_S_C;
                    RegistryKey k4 = GetRegKey(root, ext, true);

                    if (Associate)
                    {
                        if (GetRegValue(k4, String.Empty) != ProgramID)
                        {
                            k4.SetValue("QuuxBackup", GetRegValue(k4, String.Empty));
                            k4.SetValue(String.Empty, ProgramID);
                            k4.Close();
                        }
                    }
                    else
                    {
                        if (GetRegValue(k4, "QuuxBackup").Length > 0)
                        {
                            k4.SetValue(String.Empty, GetRegValue(k4, "QuuxBackup"));
                            k4.DeleteValue("QuuxBackup");
                            k4.Close();
                        }
                    }
                }
            }
        }
        public static void NotifyShellOfChange()
        {
            ShellNotification.NotifyOfChange();
        }
        private static string getAvailLetter(string Input)
        {
            char c = 'a';

            while (c < 'z' && Input.IndexOf(c) >= 0)
                c++;

            return c.ToString();
        }
        private static string makeLetterFirst(char Letter, string List)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < List.Length; i++)
                if (List[i] != Letter)
                    sb.Append(List[i]);

            return Letter.ToString() + sb.ToString();
        }
        private static RegistryKey GetRegKey(RegistryKey Parent, string Name, bool Writable)
        {
            if (Writable)
            {
                return Parent.CreateSubKey(Name);
            }
            else
            {
                RegistryKey rk = Parent.OpenSubKey(Name, false);

                if (rk == null)
                {
                    rk = Parent.CreateSubKey(Name);
                }
                return rk;
            }
        }
        private static RegistryKey SetRegKey(RegistryKey Parent, string Name)
        {
            if (!Parent.GetSubKeyNames().Contains(Name))
                Parent.CreateSubKey(Name);

            return Parent.OpenSubKey(Name, true);
        }
        private static string GetRegValue(RegistryKey Key, string Name)
        {
            try
            {
                return Key.GetValue(Name).ToString();
            }
            catch
            {
                return String.Empty;
            }
        }
        private enum ASSOCIATIONLEVEL : int { AL_MACHINE = 0, AL_EFFECTIVE = 1, AL_USER = 2 }
        private enum ASSOCIATIONTYPE : int { AT_FILEEXTENSION = 0, AT_URLPROTOCOL = 1, AT_STARTMENUCLIENT = 2, AT_MIMETYPE = 3}

        private static void setAssociationVista(bool Associate, string ProgramID, string Extension)
        {
            try
            {
                IApplicationAssociationRegistration aar;
                
                if ((aar = new ApplicationAssociationRegistration() as IApplicationAssociationRegistration) != null)
                {
                    if (Associate)
                    {
                        string current = associatedProgramVista(Extension);

                        if (current != ProgramID && current != "QuuxPlayer")
                        {
                            RegistryKey root = HKCU_S_C;
                            RegistryKey k4 = GetRegKey(root, Extension, true);
                            k4.SetValue("QuuxBackup", current, RegistryValueKind.String);
                        }
                        aar.SetAppAsDefault("QuuxPlayer",
                                            Extension,
                                            ASSOCIATIONTYPE.AT_FILEEXTENSION);
                    }
                    else
                    {
                        RegistryKey root = HKCU_S_C;
                        RegistryKey k4 = GetRegKey(root, Extension, true);
                        string restore = GetRegValue(k4, "QuuxBackup");

                        if (restore.Length > 0)
                        {
                            aar.SetAppAsDefault(restore,
                                                Extension,
                                                ASSOCIATIONTYPE.AT_FILEEXTENSION);
                        }
                    }
                }
            }
            catch
            {
            }
        }
        private static bool isAssociatedVista(string Extension)
        {
            try
            {
                bool res = false;

                IApplicationAssociationRegistration aar;
                if ((aar = new ApplicationAssociationRegistration() as IApplicationAssociationRegistration) != null)
                {
                    aar.QueryAppIsDefault(Extension,
                         ASSOCIATIONTYPE.AT_FILEEXTENSION,
                         ASSOCIATIONLEVEL.AL_EFFECTIVE,
                         "QuuxPlayer",
                         ref res);

                    return res;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return false;
            }
        }
        private static string associatedProgramVista(string Extension)
        {
            try
            {
                IApplicationAssociationRegistration aar;
                if ((aar = new ApplicationAssociationRegistration() as IApplicationAssociationRegistration) != null)
                {
                    StringBuilder progID = new StringBuilder();

                    Int32 result = aar.QueryCurrentDefault(Extension,
                                                           ASSOCIATIONTYPE.AT_FILEEXTENSION,
                                                           ASSOCIATIONLEVEL.AL_EFFECTIVE,
                                                           ref progID);

                    return progID.ToString();
                }
                else
                {
                    return String.Empty;
                }
            }
            catch
            {
                return String.Empty;
            }
        }


        [ComImport(), Guid("1968106d-f3b5-44cf-890e-116fcb9ecef1")]
        public class ApplicationAssociationRegistrationUI // IApplicationAssociationUI
        {
        }

        [ComImport(), Guid("591209c7-767b-42b2-9fba-44ee4615f2c7")]
        public class ApplicationAssociationRegistration // : IApplicationAssociation
        {
        }

        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComImport(), Guid("1f76a169-f994-40ac-8fc8-0959e8874710")]
        private interface IApplicationAssociationRegistrationUI
        {
            Int32 LaunchAdvancedAssociationUI([MarshalAs(UnmanagedType.LPWStr)] string pszAppRegistryName);
        }

        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComImport(), Guid("4e530b0a-e611-4c77-a3ac-9031d022281b")]
        private interface IApplicationAssociationRegistration
        {
            Int32 QueryCurrentDefault([MarshalAs(UnmanagedType.LPWStr)] string pszQuery,
                                      ASSOCIATIONTYPE atQueryType,
                                      ASSOCIATIONLEVEL alQueryLevel,
                                      [MarshalAs(UnmanagedType.LPWStr)] ref StringBuilder ppszAssociation);
            
            Int32 QueryAppIsDefault([MarshalAs(UnmanagedType.LPWStr)] string pszQuery,
                                    ASSOCIATIONTYPE atQueryType,
                                    ASSOCIATIONLEVEL alQueryLevel,
                                    [MarshalAs(UnmanagedType.LPWStr)] string pszAppRegistryName,
                                    [MarshalAs(UnmanagedType.Bool)] ref bool pfDefault);

            Int32 QueryAppIsDefaultAll(ASSOCIATIONLEVEL alQueryLevel,
                                       [MarshalAs(UnmanagedType.LPWStr)] string pszAppRegistryName,
                                       [MarshalAs(UnmanagedType.Bool)] ref bool pfDefault);

            Int32 SetAppAsDefault([MarshalAs(UnmanagedType.LPWStr)] string pszAppRegistryName,
                                  [MarshalAs(UnmanagedType.LPWStr)] string pszSet,
                                  ASSOCIATIONTYPE atSetType);
            
            Int32 SetAppAsDefaultAll([MarshalAs(UnmanagedType.LPWStr)] string pszAppRegistryName);

            Int32 ClearUserAssociations();
            
        }
        private static class ShellNotification
        {
            [DllImport("shell32.dll")]
            private static extern void SHChangeNotify(
                UInt32 wEventId,
                UInt32 uFlags,
                IntPtr dwItem1,
                IntPtr dwItem2);

            public static void NotifyOfChange()
            {
                SHChangeNotify(
                    (uint)ShellChangeNotificationEvents.SHCNE_ASSOCCHANGED,
                    (uint)(ShellChangeNotificationFlags.SHCNF_IDLIST | ShellChangeNotificationFlags.SHCNF_FLUSHNOWAIT),
                    IntPtr.Zero,
                    IntPtr.Zero);
            }

            [Flags]
            private enum ShellChangeNotificationEvents : uint
            {
                SHCNE_RENAMEITEM = 0x00000001,
                SHCNE_CREATE = 0x00000002,
                SHCNE_DELETE = 0x00000004,
                SHCNE_MKDIR = 0x00000008,
                SHCNE_RMDIR = 0x00000010,
                SHCNE_MEDIAINSERTED = 0x00000020,
                SHCNE_MEDIAREMOVED = 0x00000040,
                SHCNE_DRIVEREMOVED = 0x00000080,
                SHCNE_DRIVEADD = 0x00000100,
                SHCNE_NETSHARE = 0x00000200,
                SHCNE_NETUNSHARE = 0x00000400,
                SHCNE_ATTRIBUTES = 0x00000800,
                SHCNE_UPDATEDIR = 0x00001000,
                SHCNE_UPDATEITEM = 0x00002000,
                SHCNE_SERVERDISCONNECT = 0x00004000,
                SHCNE_UPDATEIMAGE = 0x00008000,
                SHCNE_DRIVEADDGUI = 0x00010000,
                SHCNE_RENAMEFOLDER = 0x00020000,
                SHCNE_FREESPACE = 0x00040000,
                SHCNE_EXTENDED_EVENT = 0x04000000,
                SHCNE_ASSOCCHANGED = 0x08000000,
                SHCNE_DISKEVENTS = 0x0002381F,
                SHCNE_GLOBALEVENTS = 0x0C0581E0,
                SHCNE_ALLEVENTS = 0x7FFFFFFF,
                SHCNE_INTERRUPT = 0x80000000
            }

            private enum ShellChangeNotificationFlags
            {
                SHCNF_IDLIST = 0x0000,
                SHCNF_PATHA = 0x0001,
                SHCNF_PRINTERA = 0x0002,
                SHCNF_DWORD = 0x0003,
                SHCNF_PATHW = 0x0005,
                SHCNF_PRINTERW = 0x0006,
                SHCNF_TYPE = 0x00FF,
                SHCNF_FLUSH = 0x1000,
                SHCNF_FLUSHNOWAIT = 0x2000
            }
        }
    }
}
