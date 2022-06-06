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
 * $Id: CardType.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System.Threading.Tasks;

namespace MyFeeder
{
	abstract public class CardType
	{
        internal abstract Task<bool> CardFound(AbstractReader r);
        internal abstract string GetIssuerName();
        internal abstract string GetPAN();
        internal abstract string GetExpiry();
        internal abstract bool isSameCard(CardType other);
    }
}
