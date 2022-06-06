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
 * $Id: TaskQueue.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace MyFeeder
{
    abstract public class TaskQueueTask
    {
          public abstract Task<bool> Run();
          public abstract Task<bool> Removed();
    }

	public class TaskQueue
	{
        private List<TaskQueueTask> tasks = new List<TaskQueueTask>();
        private TaskCompletionSource<TaskQueueTask> tcs=null;
        private DispatcherTimer timer=null;

		public TaskQueue()
		{
		}

        public void Add(TaskQueueTask task)
        {
            DispatcherTimer t = timer;
            timer = null;

            if (t != null)
            {
                t.Stop();
            }

            TaskCompletionSource<TaskQueueTask> p = tcs;

            tcs=null;

            if (p!=null)
            {
                p.SetResult(task);
            }
            else
            {
                tasks.Add(task);
            }
        }

        public Task<TaskQueueTask> NextAsync(TimeSpan ts)
        {
            TaskCompletionSource<TaskQueueTask> p=new TaskCompletionSource<TaskQueueTask>();

            if (tasks.Count > 0)
            {
                DispatcherTimer t=timer;
                timer=null;

                if (t!=null)
                {
                    t.Stop();
                }

                TaskQueueTask task=tasks[0];
                tasks.Remove(task);
                p.SetResult(task);
            }
            else
            {
                if (ts == null)
                {
                    TaskQueueTask t = null;

                    p.SetResult(t);
                }
                else
                {
                    tcs = p;

                    timer = new DispatcherTimer();

                    timer.Interval = ts;
                    timer.Tick += timer_Tick;
                    timer.Start();
                }
            }

            return p.Task;
        }

        public void timer_Tick(object sender,object e)
        {
            DispatcherTimer t=timer;
            timer=null;

            if (t!=null)
            {
                t.Stop();
            }

            TaskCompletionSource<TaskQueueTask> p=tcs;
            tcs=null;

            if (p!=null)
            {
                TaskQueueTask result=null;
                p.SetResult(result);
            }
        }

        internal void Clear()
        {
            DispatcherTimer t = timer;

            timer = null;

            if (t != null)
            {
                t.Stop();
            }

            TaskCompletionSource<TaskQueueTask> p = tcs;

            tcs=null;

            if (tcs != null)
            {
                TaskQueueTask task = null;
                tcs.SetResult(task);
            }

            while (tasks.Count>0)
            {
                TaskQueueTask task = tasks[0];
                tasks.Remove(task);
                task.Removed();
            }
        }

        public static Task<bool> asBoolAsync(bool value)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            tcs.SetResult(value);

            return tcs.Task;
        }
    }
}
