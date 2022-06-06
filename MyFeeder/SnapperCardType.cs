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
 * $Id: SnapperCardType.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System.Threading.Tasks;
using nz.geek.rhubarb.utils;

namespace MyFeeder
{
    public class SnapperCardType: CardType
	{
        private byte[] purseInfo;
        private byte[] purse;

        internal SnapperCardType(byte[] r)
        {
            purseInfo = r;
            purse = null;
        }

        internal bool isPrePaid()
        {
            return ((purseInfo[4] & 0xF0) == 0x00);
        }

        internal override async Task<bool> CardFound(AbstractReader connection)
        {
            bool result = false;

            if (isPrePaid())
            {
                purse = await connection.TransmitAsync(APDUdefs.CARD_READ_PURSE);

                if ((purse != null) && (purse.Length > 0))
                {
                    if ((purse.Length == 2) && (purse[0] == 0x6c))
                    {
                        byte[] apdu = BinaryTools.bytesFrom(APDUdefs.CARD_READ_PURSE, 0, APDUdefs.CARD_READ_PURSE.Length);
                        apdu[4] = purse[1];
                        purse = await connection.TransmitAsync(apdu);
                    }

                    result = true;
                }
            }
            else
            {
                result = true;
            }

            return result;
        }

        internal void SetPurse(byte [] p)
        {
            purse = p;
        }

        internal byte GetIDCenter()
        {
            return purseInfo[7];
        }

        internal int GetBalance()
        {
            int balance = 0;

            if ((purse!=null)&&(purse.Length>2))
            {
                balance = BinaryTools.readInt(purse, 2, 4);
            }

            return balance;
        }

        internal int GetMaxBalance()
        {
            int balance = 0;

            if ((purseInfo != null) && (purseInfo.Length > 2))
            {
                balance = BinaryTools.readInt(purseInfo, 31, 4);
            }

            return balance;
        }

        internal byte[] GetPurseInfo()
        {
            return purseInfo;
        }

        internal override string GetIssuerName()
        {
            switch (purseInfo[7])
            {
                case 0:
                    return "blank";
                case 1:
                case 2:
                    return "Snapper";
            }

            return "T-money";
        }

        internal override string GetPAN()
        {
            if (purseInfo[7]!=0)
            {
                return Hex.bytesToHex(purseInfo, 8, 8);
            }

            return "";
        }

        internal override string GetExpiry()
        {
            if (purseInfo[7] != 0)
            {
                string s = Hex.bytesToHex(purseInfo, 0x19, 4);
                return Hex.bytesToHex(purseInfo, 0x1C, 1) + "/" + Hex.bytesToHex(purseInfo, 0x1B, 1) + "/" + Hex.bytesToHex(purseInfo, 0x19, 2);
            }

            return "";
        }

        internal override bool isSameCard(CardType c)
        {
            if (c == null) return false;
            if (c == this) return true;
            if (purseInfo == null) return false;
            SnapperCardType sc = c as SnapperCardType;
            if (sc == null) return false;
            byte[] pi = sc.purseInfo;
            if (pi == null) return false;
            if (pi.Length != purseInfo.Length) return false;
            return BinaryTools.compareBytes(pi,0,purseInfo,0,pi.Length);
        }
    }
}
