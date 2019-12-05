/* This file is part of QuuxPlayer and is copyright (c) 2008, 2009 by Matthew Hamilton (mhamilton@quuxsoftware.com)
 * Use of this file is subject to the terms of the General Public License Version 3.
 * See License.txt included in this package for full terms and conditions. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace QuuxPlayer
{
    internal static class StringUtil
    {
        private const int OFFSET = 97;
        private static char[] parentheticalChars = new char[] { '(', '[' };

        static StringUtil()
        {
            for (int i = 0; i < seedInfo.Length; i++)
            {
                seedInfo[i] = (byte)((seedInfo[i] + OFFSET) & 0xFF);
            }
        }
        
        public static string Convert(byte[] Input)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < Input.Length; i++)
            {
                sb.Append((char)((byte)((Input[i] + 0x100 - seedInfo[i] + i) & 0xFF)));
            }

            return sb.ToString();
        }

        public static bool HasParentheticalChars(string Input)
        {
            return Input.IndexOfAny(parentheticalChars) >= 0;
        }
        public static string RemoveParentheticalChars(string Input)
        {
            if (!HasParentheticalChars(Input))
                return Input;

            int removing = 0;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Input.Length; i++)
            {
                switch (Input[i])
                {
                    case '(':
                    case '[':
                        removing++;
                        continue;
                    case ')':
                    case ']':
                        removing = Math.Max(0, removing - 1);
                        continue;
                }
                if (removing == 0)
                {
                    sb.Append(Input[i]);
                }
            }
            return sb.ToString().Trim();
        }
        public static byte[] Seq1 = { 0xcc, 0x73, 0xe7, 0xe4, 0x00, 0x88, 0x47, 0x4e,
                                                     0x30, 0x6d, 0x49, 0x9c, 0xd7, 0xaf, 0x7e, 0x43,
                                                     0x80, 0x2d, 0x70, 0x2b, 0x7b, 0x3c, 0x90, 0xf5,
                                                     0x43, 0x18, 0xbd, 0xba, 0x38, 0x44, 0xe0, 0xb7,
                                                     0x23 };

        public static byte[] Seq2 = { 0x91, 0x68, 0xb1, 0xb5, 0xc5, 0x50, 0x0f, 0x0d, 0xf4, 0x32, 0x3e, 0x6c, 0xa0 };
        
        private static byte[] seedInfo = new byte[]   { 0xfe, 0xb0, 0x20, 0x25, 0x36, 0xc3, 0x80, 0x80, 
                                                        0x68, 0xa7, 0xb2, 0xe5, 0x16, 0xe6, 0xbe, 0x83, 
                                                        0xc6, 0xaf, 0xae, 0x69, 0xcd, 0x82, 0xdf, 0x3c, 
                                                        0x88, 0x6c, 0x0f, 0x01, 0x91, 0xd2, 0x2e, 0x03, 
                                                        0x7b, 0x7f, 0x5b, 0x7f, 0x4f, 0x0d, 0xfb, 0x78, 
                                                        0x15, 0x03, 0xa4, 0x3e, 0x07, 0x77, 0xb6, 0x27, 
                                                        0xdf, 0x2d, 0x21, 0x83, 0x1d, 0x3e, 0xec, 0x7b, 
                                                        0xd1, 0xe3, 0x9c, 0xca, 0xb4, 0x7e, 0xef, 0x95, 
                                                        0x79, 0xae, 0x6a, 0x96, 0x8a, 0x3e, 0x33, 0xb9, 
                                                        0xb4, 0x35, 0x53, 0xca, 0xde, 0xa0, 0x35, 0x11, 
                                                        0xb5, 0xe7, 0x21, 0x63, 0xb0, 0xab, 0xe6, 0x3d, 
                                                        0x93, 0xdf, 0xae, 0x6b, 0x70, 0x31, 0xc4, 0x97, 
                                                        0x13, 0x0e, 0xc5, 0x58, 0x0d, 0x1f, 0x9d, 0xa0, 
                                                        0xf9, 0x68, 0xce, 0xe8, 0xea, 0x22, 0x9d, 0x30, 
                                                        0xae, 0x8a, 0x14, 0xcc, 0x5c, 0x8b, 0xe5, 0xcd, 
                                                        0xc8, 0x2c, 0x03, 0xaa, 0x90, 0xc8, 0x48, 0x83, 
                                                        0x70, 0xbc, 0xb6, 0xcf, 0xda, 0xdd, 0x03, 0x96, 
                                                        0x49, 0x81, 0x6a, 0x48, 0xdc, 0xfd, 0x53, 0x70, 
                                                        0x41, 0x7d, 0xbd, 0xe6, 0x1c, 0xf7, 0x3a, 0x88, 
                                                        0x29, 0xf7, 0x8f, 0xe0, 0x1b, 0xf2, 0x10, 0x31, 
                                                        0x20, 0x4b, 0x78, 0x2d, 0x6c, 0xce, 0x56, 0xd1, 
                                                        0x87, 0x7e, 0x83, 0xdb, 0x21, 0x9c, 0xf1, 0xca, 
                                                        0xd9, 0x93, 0x68, 0x12, 0x0a, 0x62, 0x66, 0x78, 
                                                        0x82, 0x2e, 0xa5, 0xe2, 0x4d, 0x23, 0x7a, 0x57, 
                                                        0x71, 0x38, 0x28, 0x90, 0x85, 0x25, 0x03, 0x73, 
                                                        0x27, 0xec, 0x5e, 0x9e, 0x74, 0x5e, 0x66, 0x8c, 
                                                        0x06, 0xc5, 0x06, 0x88, 0x8a, 0xfd, 0x26, 0xbe, 
                                                        0xe5, 0xff, 0xaa, 0x3e, 0x28, 0x73, 0x84, 0x64, 
                                                        0x04, 0x2c, 0x69, 0xe9, 0x74, 0x61, 0x45, 0xb4, 
                                                        0x8f, 0xf4, 0xeb, 0x1e, 0x03, 0xc8, 0x03, 0x23, 
                                                        0xc7, 0x19, 0xdc, 0x87, 0x1c, 0xf1, 0xcc, 0x74, 
                                                        0x11, 0x6a, 0xab, 0x86, 0x7a, 0xc5, 0x4b, 0xb3 
                                                    };
    }
}
