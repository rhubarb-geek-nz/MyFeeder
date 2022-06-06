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
 * $Id: UnknownCardType.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System.Threading.Tasks;

namespace MyFeeder
{
    public class UnknownCardType: CardType
	{
        internal UnknownCardType()
        {
        }

        internal override async Task<bool> CardFound(AbstractReader r)
        {
            return await TaskQueue.asBoolAsync(true);
        }

        internal override string GetExpiry()
        {
            return "";
        }

        internal override string GetIssuerName()
        {
            return "unknown";
        }

        internal override string GetPAN()
        {
            return "";
        }

        internal override bool isSameCard(CardType c)
        {
            return c == this;
        }
    }
}
