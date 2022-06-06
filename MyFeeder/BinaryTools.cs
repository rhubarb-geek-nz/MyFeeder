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
 * $Id: BinaryTools.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System;

namespace nz.geek.rhubarb.utils
{
    public class BinaryTools
    {
        public static long readLong(byte[] b, int off, int len)
        {
            ulong val = 0;

            while (0 != (len--))
            {
                val = (val<<8) + (ulong)(0xff & b[off++]);
            }

            return (long)val;
        }

        public static int readInt(byte[] b, int off, int len)
        {
            return (int)readLong(b, off, len);
        }

        public static long readLongBits(byte[] b, int off, int len)
        {
            long val = 0;

            while (0 != len--)
            {
                int boff = off >> 3;
                int mask = 1 << (7 - (off & 7));

                val = (val << 1);

                if (0 != (b[boff] & mask))
                {
                    val++;
                }

                off++;
            }

            return val;
        }

        public static int readIntBits(byte[] b, int off, int len)
        {
            return (int)readLongBits(b, off, len);
        }

        public static void writeBits(byte[] d, int offset, int count, long value)
        {
            offset += count;

            while (0 != count--)
            {
                offset--;

                int boff = offset >> 3;
                int mask = 1 << (7 - (offset & 7));

                if (0 == (value & 1))
                {
                    d[boff] &= (byte)~mask;
                }
                else
                {
                    d[boff] |= (byte)mask;
                }

                value = value >> 1;
            }
        }

        public static void writeInt(byte[] ba, int offset, int length, int value)
        {
            offset += length;

            while (0 != length--)
            {
                ba[--offset] = (byte)value;
                value >>= 8;
            }
        }

        public static void writeLong(byte[] ba, int offset, int length, long value)
        {
            offset += length;

            while (0 != length--)
            {
                ba[--offset] = (byte)value;
                value >>= 8;
            }
        }

        public static void rotateLeft(byte[] b)
        {
            int i = 0, j = b.Length - 1;
            byte c = b[0];

            while (i < j)
            {
                b[i] = b[i + 1];
                i++;
            }

            b[b.Length - 1] = c;
        }

        public static void rotateRight(byte[] b)
        {
            byte c = b[b.Length - 1];
            int i = b.Length;

            while (0 != --i)
            {
                b[i] = b[i - 1];
            }

            b[0] = c;
        }

        public static byte[] bytesFrom(byte[] data, int from, int len)
        {
            byte[] res = new byte[len];
            Buffer.BlockCopy(data, from, res,0,len);
            return res;
        }

        public static Boolean compareBytes(byte[] mac, int x, byte[] mac2, int y, int len)
        {
            while (0 != (len--))
            {
                if (mac[x++] != mac2[y++])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool compareBytes(byte[] a1, byte[] a2)
        {
            if (a1 == null) return (a2 == null);
            if (a2 == null) return false;
            if (a1.Length != a2.Length) return false;
            return compareBytes(a1, 0, a2, 0, a1.Length);
        }

        public static int readIntLE(byte[] data, int p, int len)
        {
            int res = 0;

            while (0 != len--)
            {
                res = (res << 8) + data[p + len];
            }

            return res;
        }
    }
}
