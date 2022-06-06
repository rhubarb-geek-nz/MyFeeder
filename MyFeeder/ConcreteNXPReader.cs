/**************************************************************************
 *
 *  Copyright 2015, Roger Brown
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
 * $Id: ConcreteNXPReader.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System.Threading.Tasks;

namespace MyFeeder
{
    class ConcreteNXPReader: AbstractReader
    {
        USBHandler handler;

        internal ConcreteNXPReader(App a,string id, USBHandler c) : base(a,id)
        {
            handler = c;
            isNFC = true;
        }

        public override void Dispose()
        {
            USBHandler h = handler;
            handler = h;

            if (h!=null)
            {
                app.usbReaders.Remove(h);
            }
        }

        public override Task<byte[]> TransmitAsync(byte[] apdu)
        {
            return handler.connection.TransmitAsync(apdu);
        }

        public override Task<bool> beginTransaction()
        {
            return handler.connection.beginTransaction();
        }

        public override void endTransaction(bool keep)
        {
            handler.connection.endTransaction();
        }
    }
}
