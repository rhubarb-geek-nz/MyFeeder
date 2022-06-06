/**************************************************************************
 *
 *  Copyright 2014, Roger Brown
 *
 *  This file is part of Roger Brown's Toolkit.
 *
 *  This program is free software: you can redistribute it and/or modify it
 *  under the terms of the GNU Lesser General Public License as published by the
 *  Free Software Foundation, either version 3 of the License, or (at your
 *  option) any later version.
 * 
 *  This program is distributed in the hope that it will be useful, but WITHOUT
 *  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 *  FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for
 *  more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>
 *
 */

/* 
 * $Id: Utils.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace MyFeeder
{
    public class Utils
    {
        public static string cardNumberWithSpace(string s)
        {
            string res = null;

            if (s!=null)
            {
                int len = s.Length;
                char[] a = new char[len+(len>>2)];
                int i = 0, j = 0;

                while (i < len)
                {
                    char c = s[i++];
                    a[j++] = c;
                    if ((0==(i&3))&&(i < len))
                    {
                        a[j++] = ' ';
                    }
                }

                res = new string(a,0,j);
            }

            return res;
        }

        internal static string toMoney(int p)
        {
            int dollars = p / 100;
            int cents = p % 100;

            return dollars.ToString("D") + "." + cents.ToString("D2");
        }

        internal static string pciObscure(string s)
        {
            int len = s.Length;
            char[] a = new char[len];
            int j = len - 4;
            int i = 0;

            while ((i < 6)&&(i<len))
            {
                a[i] = s[i];
                i++;
            }

            while ((i < j)&&(i<len))
            {
                a[i] = '\u2022';
                i++;
            }

            while (i < len)
            {
                a[i] = s[i];
                i++;
            }

            return new string(a);
        }

        internal static Boolean isValidCVV(string s)
        {
            if ((s != null) && (s.Length == 3))
            {
                foreach (char c in s)
                {
                    if ((c < '0') || (c > '9'))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        internal static Boolean isAllDigits(string s)
        {
            if (s != null)
            {
                foreach (char c in s)
                {
                    if ((c < '0') || (c > '9'))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }


        internal static string StringFromBytes(byte[] data, int offset, int len)
        {
            MemoryStream ms = new MemoryStream();
            ms.Write(data, offset, len);
            ms.Position = 0;
            StreamReader rdr = new StreamReader(ms);
            return rdr.ReadToEnd();
        }

        internal static async Task<MemoryStream> ReadMemoryStreamAsync(Windows.Storage.Streams.IInputStream d)
        {
            MemoryStream ms = new MemoryStream();

            while (true)
            {
                uint bufLen = 4096;
                IBuffer b = new Windows.Storage.Streams.Buffer(bufLen);

                byte [] p = (await d.ReadAsync(b, bufLen, InputStreamOptions.Partial).AsTask()).ToArray();

                int i = p.Length;

                if (i > 0)
                {
                    ms.Write(p, 0, i);
                }
                else
                {
                    break;
                }
            }

            ms.Position = 0;

            return ms;
        }
    }
}
