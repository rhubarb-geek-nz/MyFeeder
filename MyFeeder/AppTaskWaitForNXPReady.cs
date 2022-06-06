/**************************************************************************
 *
 *  Copyright 2015, Roger Brown
 *
 *  This file is part of Roger Brown's MyFeeder.
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
 * $Id: AppTaskWaitForNXPReady.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

namespace MyFeeder
{
    class AppTaskWaitForNXPReady : AppTask
    {
        readonly string xid;

        internal AppTaskWaitForNXPReady(App a,string d) : base(a)
        {
            xid = d;
        }

        internal override void beginTask()
        {
        }

        internal void lost(USBHandler h)
        {
            System.Diagnostics.Debug.WriteLine("USB lost "+xid);
            app.readerLost(h);
            finished();
        }

        internal void found(USBHandler h)
        {
            System.Diagnostics.Debug.WriteLine("USB found " + xid);
            app.readerFound(h);
            finished();
        }
    }
}
