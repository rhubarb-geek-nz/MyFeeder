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
 * $Id: CardConnection.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.SmartCards;

namespace MyFeeder
{
    class CardConnection
    {
        static internal async Task<byte[]> SendAPDU(SmartCardConnection conn,byte[] capdu)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(">> " + Hex.bytesToHex(capdu, 0, capdu.Length));
#endif
            byte [] resp = (await conn.TransmitAsync(capdu.AsBuffer())).ToArray();
#if DEBUG
            System.Diagnostics.Debug.WriteLine("<< " + Hex.bytesToHex(resp, 0, resp.Length));
#endif
            return resp;
        }
    }
}
