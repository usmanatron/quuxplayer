/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace QuuxPlayer
{
    internal static class Notices
    {
        private static Random r = new Random();

        private static string actStatusString = String.Empty;

        public static string CopyrightNotice
        {
            get
            {
                return Localization.Get(UI_Key.General_Copyright, DateTime.Now.Year.ToString(), Application.CompanyName, Application.ProductName);
            }
        }
        public static string ActivationInfo
        {
            get
            {
                return actStatusString;
            }
        }
        
        public static string MD5Hash(string input)
        {
            MD5 md5Hasher = MD5.Create();

            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));

            StringBuilder sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString().ToLowerInvariant();
        }
    }
}
