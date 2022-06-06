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
 * $Id: APDUdefs.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

namespace MyFeeder
{
    public class APDUdefs
    {
        public static readonly byte[] SNAPPER_AID = { 0xd4, 0x10, 0x00, 0x00, 0x03, 0x00, 0x01 };
        public static readonly byte[] SELECT_SNAPPER = { 0x00, 0xa4, 0x04, 0x00, 0x07, 0xd4, 0x10, 0x00, 0x00, 0x03, 0x00, 0x01 };
        public static readonly byte[] GET_RESPONSE = { 0x00, 0xC0, 0x00, 0x00, 0x00 };
        public static readonly byte[] CARD_READ_PURSE={0,0xB2,1,0x24,0x1A};

        public static readonly byte[] SELECT_PPSE = { 0x00, 0xA4, 0x04, 0x00, 0x0E, 
               (byte)'2', (byte)'P', (byte)'A', (byte)'Y', (byte)'.', (byte)'S', (byte)'Y', (byte)'S', (byte)'.', (byte)'D', (byte)'D', (byte)'F', (byte)'0', (byte)'1' };

        public static readonly byte[] SELECT_PPSE_LE = { 0x00, 0xA4, 0x04, 0x00, 0x0E,
               (byte)'2', (byte)'P', (byte)'A', (byte)'Y', (byte)'.', (byte)'S', (byte)'Y', (byte)'S', (byte)'.', (byte)'D', (byte)'D', (byte)'F', (byte)'0', (byte)'1',0 };

        public static readonly byte[] SELECT_PSE = { 0x00, (byte)0xA4, 0x04, 0x00, 0x0E, 
               (byte)'1', (byte)'P', (byte)'A', (byte)'Y', (byte)'.', (byte)'S', (byte)'Y', (byte)'S', (byte)'.', (byte)'D', (byte)'D', (byte)'F', (byte)'0', (byte)'1'};

        public static readonly byte[] SELECT_CALYPSO = { 0x00, 0xA4, 0x04, 0x00, 0x08, 0x31, 0x54, 0x49, 0x43, 0x2E, 0x49, 0x43, 0x41 };
        public static readonly byte[] CALYPSO_READ_ENVIRONMENT = { 0x94, 0xB2, 0x01, 0x38, 0x19 };
        public static readonly byte[] CALYPSO_SV_GET_LOAD_00 = { 0x00, 0x7C, 0x00, 0x07, 0x21 };
        public static readonly byte[] CALYPSO_SV_GET_LOAD_FA = { 0xFA, 0x7C, 0x00, 0x07, 0x21 };
    }
}
