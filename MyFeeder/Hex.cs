/**************************************************************************
 *
 *  Copyright 2012, Roger Brown
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
 * $Id: Hex.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System;

namespace nz.geek.rhubarb.utils
{
    public class Hex
    {
        static char[] hexChars ={
                                          '0','1','2','3',
                                          '4','5','6','7',
                                          '8','9','A','B',
                                          'C','D','E','F'
                                    };

        public static String bytesToHex(byte [] data,int offset,int len)
        {
            char [] a=new char[len<<1];
            int i = 0;

            while (0 != len--)
            {
                byte b=data[offset++];
                a[i++] = hexChars[0xF & (b >> 4)];
                a[i++] = hexChars[0xF & b];
            }

            return new String(a);
        }

        public static byte [] bytesToASCII(byte[] data, int offset, int len)
        {
            byte[] a = new byte[len << 1];
            int i = 0;

            while (0 != len--)
            {
                byte b = data[offset++];
                a[i++] = (byte)hexChars[0xF & (b >> 4)];
                a[i++] = (byte)hexChars[0xF & b];
            }

            return a;
        }

        static int ordinal(char c)
        {
            int i = hexChars.Length;

            while (0 != i--)
            {
                if (hexChars[i] == c) break;
            }

            return i;
        }

        public static byte[] bytesFromASCII(byte[] d)
        {
            int maxLen=d.Length >> 1;
            byte[] result = new byte[maxLen];
            Boolean isHigh=true;
            int offset=0;
            byte b=0;

            foreach(byte c in d)
            {
                int i=ordinal((char)c);

                if (i>=0)
                {
                    if (isHigh)
                    {
                        b=(byte)(i<<4);
                    }
                    else
                    {
                        result[offset++] = (byte)(b | i);
                    }

                    isHigh = !isHigh;
                }
            }

            if (offset != maxLen)
            {
                byte[] a = new byte[offset];

                System.Array.Copy(result, 0, a, 0, offset);

                result = a;
            }

            return result;
        }

        public static byte[] bytesFromHex(String d)
        {
            int maxLen = d.Length >> 1;
            byte[] result = new byte[maxLen];
            Boolean isHigh = true;
            int offset = 0;
            byte b = 0;

            foreach (char c in d)
            {
                int i = ordinal(c);

                if (i >= 0)
                {
                    if (isHigh)
                    {
                        b = (byte)(i << 4);
                    }
                    else
                    {
                        result[offset++] = (byte)(b | i);
                    }

                    isHigh = !isHigh;
                }
            }

            if (offset != maxLen)
            {
                byte[] a = new byte[offset];

                System.Array.Copy(result, 0, a, 0, offset);

                result = a;
            }

            return result;
        }
    }
}
