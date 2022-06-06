/**************************************************************************
 *
 *  Copyright 2013, Roger Brown
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
 * $Id: EMVRecord.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System;
using System.Collections.Generic;
using System.Text;
using nz.geek.rhubarb.utils;

namespace MyFeeder
{
    public class EMVRecord
    {
        internal byte[] data;
        private int offset,length,cursor;

        internal EMVRecord(byte[] adf, int p1, int p2)
        {
            data = adf;
            offset = p1;
            length = p2;
            cursor = -1;
        }

        internal void rewind()
        {
            cursor = -1;
        }

        internal int tagLen()
        {
            if ((data[offset+cursor]&0x1f)==0x1f)
            {
                return 2;
            }

            return 1;
        }

        internal int lenLen()
        {
            int o = offset + cursor + tagLen();

            if (data[o] < 0x80)
            {
                return 1;
            }

            return 1 + (data[o] & 0x7f); ;
        }

        internal int dataLen()
        {
            int o = offset + cursor + tagLen();
            int i = data[o];

            if (i >= 0x80)
            {
                i = BinaryTools.readInt(data, o + 1, i & 0x7f);            
            }

            return i;
        }

        internal int dataOffset()
        {
            return offset+cursor+tagLen()+lenLen();
        }

        internal int tag()
        {
            return BinaryTools.readInt(data,offset+cursor,tagLen());
        }

        internal Boolean Next()
        {
            int c=0;

            if (cursor >= 0)
            {
                c = cursor + tagLen() + lenLen() + dataLen();
            }

            if (c >= length)
            {
                return false;
            }

            cursor = c;

#if DEBUG
            System.Diagnostics.Debug.WriteLine("TLV: "+tag().ToString("X4")+","+dataLen()+","+Hex.bytesToHex(data,dataOffset(),dataLen()));
#endif

            return true;
        }

        internal EMVRecord dataRecord()
        {
            return new EMVRecord(data, dataOffset(), dataLen());
        }

        internal byte [] dataBytes()
        {
            return BinaryTools.bytesFrom(data, dataOffset(), dataLen());
        }

        internal string dataHex()
        {
            return Hex.bytesToHex(data, dataOffset(), dataLen());
        }

        internal string dataString()
        {
            int o = dataOffset();
            int l = dataLen();
            char[] r = new char[l];

            while (l-- > 0)
            {
                r[l] = (char)data[o + l];
            }

            return new string(r);
        }
    }
}
