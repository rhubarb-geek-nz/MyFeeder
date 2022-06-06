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
 * $Id: AppTaskConnectFeeder.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System;
using Windows.Devices.Usb;

namespace MyFeeder
{
    class AppTaskConnectFeeder : AppTask
    {
        readonly string xid;

        public AppTaskConnectFeeder(App a,string i) : base(a)
        {
            xid = i;
        }

        override internal async void beginTask()
        {
            System.Diagnostics.Debug.WriteLine("connectFeeder(" + xid + ")");

            try
            {
                UsbDevice device = await UsbDevice.FromIdAsync(xid);

                if (device != null)
                {
                    System.Diagnostics.Debug.WriteLine("UsbDevice type=" + device.GetType());
                    System.Diagnostics.Debug.WriteLine("UsbDevice string=" + device.ToString());

                    AppTaskWaitForNXPReady waiter = new AppTaskWaitForNXPReady(app,xid);

                    app.beginTask(waiter);

                    USBHandler h = new USBHandler(app, xid, device,waiter);

                    h.run();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AppTaskConnectFeeder " + ex.Message);
            }
            finally
            {
                finished();
            }
        }
    }
}
