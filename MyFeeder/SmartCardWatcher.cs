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
 * $Id: SmartCardWatcher.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

 /* you can't use smart card readers unless have company account Shared User Certificates
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SmartCards;

namespace MyFeeder
{
    public class SmartCardWatcher
    {
        readonly App app;
        DeviceWatcher watcher;
        internal List<AppTask> pendingTasks = new List<AppTask>();
        internal AppTask currentTask;

        internal SmartCardWatcher(App a)
        {
            app = a;
        }

        public void start()
        {
            string sel = SmartCardReader.GetDeviceSelector();
            watcher = Windows.Devices.Enumeration.DeviceInformation.CreateWatcher(sel);
            watcher.Added += onAdded;
            watcher.Removed += onRemoved;
            watcher.Updated += onUpdated;
            watcher.EnumerationCompleted += onComplete;
            watcher.Stopped += onStopped;
            watcher.Start();
        }

        async void beginTask(AppTask task)
        {
            await app.dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (currentTask==null)
                    {
                        currentTask = task;
                        app.beginTask(task);
                    }
                    else
                    {
                        pendingTasks.Add(task);
                    }
                }
            );

        }

        private void onComplete(DeviceWatcher sender, object args)
        {
            System.Diagnostics.Debug.WriteLine("SCW onComplete " + args);
        }

        private void onUpdated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            System.Diagnostics.Debug.WriteLine("SCW onUpdated " + args.Id);
        }

        private void onRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            string id = args.Id;

            System.Diagnostics.Debug.WriteLine("SmartCard onRemoved " + id);

            beginTask(new SmartCardRemoveTask(app,this, id));
        }

        private void onAdded(DeviceWatcher sender, DeviceInformation args)
        {
            string id = args.Id;

            System.Diagnostics.Debug.WriteLine("SmartCard onAdded " + id);

            beginTask(new SmartCardAddTask(app,this, id));
        }

        TaskCompletionSource<bool> stopTCS = new TaskCompletionSource<bool>();

        public Task<bool> stop()
        {
            watcher.Stop();
            return stopTCS.Task;
        }

        private void onStopped(DeviceWatcher sender, object args)
        {
            System.Diagnostics.Debug.WriteLine("SmartCard watcher stopped");
            stopTCS.SetResult(true);
        }
    }

    abstract class SmartCardTask : AppTask
    {
        readonly SmartCardWatcher watcher;

        internal SmartCardTask(App a,SmartCardWatcher w) : base(a)
        {
            watcher = w;
        }

        internal void taskFinished()
        {
            if (watcher.currentTask == this)
            {
                AppTask t = null;

                if (watcher.pendingTasks.Count > 0)
                {
                    t = watcher.pendingTasks[0];
                    watcher.pendingTasks.Remove(t);
                }

                watcher.currentTask = t;

                if (t != null)
                {
                    app.beginTask(t);
                }
            }

            finished();
        }
    }

    class SmartCardAddTask : SmartCardTask
    {
        readonly string deviceId;

        internal SmartCardAddTask(App a, SmartCardWatcher w,string id) : base(a,w)
        {
            deviceId = id;
        }

        internal async override void beginTask()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("SCW onAdded " + deviceId);

                AbstractReader rdr = app.getReaderFromDeviceId(deviceId);

                if (null == rdr)
                {
                    await connectSmartCardReader(deviceId, true);
                }
                else
                {
                    if (!rdr.isNFC)
                    {
                        app.beginTask(new AppTaskReadCardState(app, rdr, false));
                    }
                }
            }
            finally
            {
                taskFinished();
            }
        }

        async Task connectSmartCardReader(String dis, Boolean pn547)
        {
            System.Diagnostics.Debug.WriteLine("connectSmartCard(" + dis + ")");

            try
            {
                SmartCardReader reader = await SmartCardReader.FromIdAsync(dis);

                if (reader != null)
                {
                    if (pn547 || !SmartCardReaderKind.Nfc.Equals(reader.Kind))
                    {
                        String name = reader.Name;
                        System.Diagnostics.Debug.WriteLine("reader.name(" + name + ")");
                        System.Diagnostics.Debug.WriteLine("reader.kind(" + reader.Kind.ToString() + ")");
                        System.Diagnostics.Debug.WriteLine("reader.id(" + reader.DeviceId.ToString() + ")");

                        ConcreteSmartCardReader csc = new ConcreteSmartCardReader(app, dis, reader);

                        csc.DisplayName = name;

                        app.readers.Add(csc);

                        app.beginTask(new AppTaskReadCardState(app, csc, false));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("connectSmartCard ex=" + ex.Message);
            }
        }
    }

    class SmartCardRemoveTask : SmartCardTask
    {
        readonly string deviceId;

        internal SmartCardRemoveTask(App a, SmartCardWatcher w,string id) : base(a,w)
        {
            deviceId = id;
        }

        internal override void beginTask()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("SCW onRemove " + deviceId);

                AbstractReader rdr = app.getReaderFromDeviceId(deviceId);

                if (rdr != null)
                {
                    app.readers.Remove(rdr);

                    rdr.Dispose();
                }
            }
            finally
            {
                taskFinished();
            }
        }
    }
}
