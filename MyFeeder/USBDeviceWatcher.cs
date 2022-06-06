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
 * $Id: USBDeviceWatcher.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System;
using Windows.Devices.Usb;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace MyFeeder
{
	public class USBDeviceWatcher
	{
        readonly App app;
        DeviceWatcher watcher;

        internal USBDeviceWatcher(App a)
        {
            app = a;
        }

        public void start()
        {
            string sel = UsbDevice.GetDeviceSelector(0x4cc, 0x531);
            watcher = Windows.Devices.Enumeration.DeviceInformation.CreateWatcher(sel);
            watcher.Added += onAdded;
            watcher.Removed += onRemoved;
            watcher.Updated += onUpdated;
            watcher.EnumerationCompleted += onComplete;
            watcher.Stopped += onStopped;
            watcher.Start();
        }

        private void onComplete(DeviceWatcher sender, object args)
        {
            System.Diagnostics.Debug.WriteLine("USB onComplete " + args);
        }

        private void onUpdated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            System.Diagnostics.Debug.WriteLine("USB onUpdated " + args.Id);
        }

        private async void onRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            string id = args.Id;

            System.Diagnostics.Debug.WriteLine("USB onRemoved " + id);

            await app.dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                  {
                      app.beginTask(new USBRemoveTask(app, id));
                  }
            );
        }

        private async void onAdded(DeviceWatcher sender, DeviceInformation args)
        {
            string id = args.Id;

            System.Diagnostics.Debug.WriteLine("USB onAdded " + id);

            await app.dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    app.beginTask(new USBConnectTask(app, id));
                }
            );
        }

        TaskCompletionSource<bool> stopTCS = new TaskCompletionSource<bool>();

        public Task<bool> stop()
        {
            watcher.Stop();
            return stopTCS.Task;
        }

        private void onStopped(DeviceWatcher sender, object args)
        {
            System.Diagnostics.Debug.WriteLine("USB watcher stopped");
            stopTCS.SetResult(true);
        }
    }

    class USBConnectTask : AppTask
    {
        readonly string deviceId;

        internal USBConnectTask(App a,string d) : base(a)
        {
            deviceId = d;
        }

        internal override void beginTask()
        {
            try
            {
                AbstractReader rdr = app.getReaderFromDeviceId(deviceId);

                if (null == rdr)
                {
                    app.beginTask(new AppTaskConnectFeeder(app, deviceId));
                }
            }
            finally
            {
                finished();
            }
        }
    }

    class USBRemoveTask : AppTask
    {
        readonly string deviceId;

        internal USBRemoveTask(App a, string d) : base(a)
        {
            deviceId = d;
        }

        internal override void beginTask()
        {
            try
            {
                bool anyFound = true;

                while (anyFound)
                {
                    anyFound = false;

                    foreach (USBHandler handler in app.usbReaders)
                    {
                        if (handler.deviceId.Equals(deviceId))
                        {
                            app.readerLost(handler);
                            anyFound = true;
                            break;
                        }
                    }
                }
            }
            finally
            {
                finished();
            }
        }
    }
}
