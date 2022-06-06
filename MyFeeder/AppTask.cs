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
 * $Id: AppTask.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

namespace MyFeeder
{
    internal abstract class AppTask
    {
        public readonly App app;
        bool active = true;

        internal AppTask(App a)
        {
            app = a;
        }

        internal abstract void beginTask();

        internal void finished()
        {
            if (active)
            {
                active = false;

                app.completedTask(this);
            }
        }
    }
}
