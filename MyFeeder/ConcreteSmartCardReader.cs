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
 * $Id: ConcreteSmartCardReader.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.SmartCards;

namespace MyFeeder
{
    class ConcreteSmartCardReader : AbstractReader
    {
        SmartCardConnection conn;
        SmartCardReader reader;
        byte [] atr;
        bool inTransaction = false,mustDispose=false;
        List<TaskCompletionSource<bool>> waiter = new List<TaskCompletionSource<bool>>();

        Task<bool> waitForChange()
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            waiter.Add(tcs);
            return tcs.Task;
        }

        void notifyChange()
        {
            System.Diagnostics.Debug.WriteLine("SmartCardReader notifyChange...");
            if (waiter.Count>0)
            {
                List<TaskCompletionSource<bool>> w = waiter;
                waiter = new List<TaskCompletionSource<bool>>();

                foreach (TaskCompletionSource<bool> t in w)
                {
                    t.SetResult(true);
                }
            }
        }

        internal ConcreteSmartCardReader(App a,string id,SmartCardReader r) : base(a,id)
        {
            reader = r;
            reader.CardAdded += CardAdded;
            reader.CardRemoved += CardRemoved;
            SmartCardReaderKind k = r.Kind;
            isNFC=k.Equals(SmartCardReaderKind.Nfc);
            isUICC = k.Equals(SmartCardReaderKind.Uicc);
        }

        public async override Task<bool> beginTransaction()
        {
            bool result = false;

            while (inTransaction)
            {
                System.Diagnostics.Debug.WriteLine("SmartCardReader waiting for transaction...");
                await waitForChange();
            }

            System.Diagnostics.Debug.WriteLine("SmartCardReader begin transaction...");

            try
            {
                inTransaction = true;

                if (conn == null)
                {
                    IReadOnlyList<SmartCard> list = await reader.FindAllCardsAsync();

                    foreach (SmartCard c in list)
                    {
                        byte[] a = (await c.GetAnswerToResetAsync()).ToArray();

                        conn = await c.ConnectAsync();

                        if (conn != null)
                        {
                            atr = a;
                            result = true;
                            isCardPresent = true;

#if DEBUG
                            System.Diagnostics.Debug.WriteLine("Card Reader Kind " + reader.Kind.ToString());
#endif

                            break;
                        }
                    }
                }
                else
                {
                    result = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Connect to card failed " + ex.Message);
            }
            finally
            {
                if (!result)
                {
                    inTransaction = false;
                    notifyChange();
                }
            }

            System.Diagnostics.Debug.WriteLine("SmartCardReader begin transaction result="+result);

            return result;
        }

        public override void endTransaction(bool keep)
        {
            System.Diagnostics.Debug.WriteLine("SmartCardReader end transaction...");

            if (inTransaction)
            {
                if (conn != null)
                {
//                    System.Diagnostics.Debug.WriteLine("SmartCardReader end transaction... closing connection, mustDispose=" + mustDispose);
//                    System.Diagnostics.Debug.WriteLine("SmartCardReader end transaction... closing connection, isUICC=" + isUICC);
//                    System.Diagnostics.Debug.WriteLine("SmartCardReader end transaction... closing connection, isNFC=" + isNFC);
//                    System.Diagnostics.Debug.WriteLine("SmartCardReader end transaction... closing connection, keep=" + keep);

                    if (mustDispose || ( !keep && (isUICC || !isNFC )))
                    {
                        System.Diagnostics.Debug.WriteLine("SmartCardReader end transaction... closing connection " + this.DisplayName);
                        mustDispose = false;
                        SmartCardConnection c = conn;
                        conn = null;
                        if (c!=null)
                        {
                            c.Dispose();
                        }
                    }
                }

                if (inTransaction)
                {
                    inTransaction = false;
                    notifyChange();
                }
            }
        }

        public override void Dispose()
        {
            SmartCardConnection c = conn;
            conn = null;

            reader.CardAdded -= CardAdded;
            reader.CardRemoved -= CardRemoved;
            reader = null;

            if (c != null)
            {
                c.Dispose();
            }

            notifyChange();
        }

        public async override Task<byte[]> TransmitAsync(byte[] apdu)
        {
            byte[] res = null;
            bool failed = true;
            SmartCardConnection c = conn;

            if (c!=null)
            {
                try
                {
                    res = await CardConnection.SendAPDU(c, apdu);

                    if ((res!= null)&&(res.Length==2)&&(res[0]==0x61))
                    {
                        /* may detect SIM and Poko, or card in contact reader */

                        isT0 = true;

                        res=await CardConnection.SendAPDU(c, new byte[] { 0x00, 0xC0, 0x00, 0x00, res[1] });
                    }

                    failed = false;
                }
                finally
                {
                    if (failed)
                    {
                        if (c==conn)
                        {
                            System.Diagnostics.Debug.WriteLine("Disposing due to transmit error");

                            conn = null;
                            c.Dispose();
                        }
                    }
                }
            }

            return res;
        }

        internal void CardAdded(SmartCardReader sender, CardAddedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("CardAdded " + sender.Name);

            queueTask(new AppTaskCardFound(app, this));                    
        }

        async void queueTask(AppTask t)
        {
            await app.dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                                        () =>
                                        {
                                            app.beginTask(t);
                                            notifyChange();
                                        }
                                    );
        }

        internal void CardRemoved(SmartCardReader sender, CardRemovedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("CardRemoved " + sender.Name);

            if (conn!=null)
            {
                if (inTransaction)
                {
                    System.Diagnostics.Debug.WriteLine("CardRemoved, setting mustDispose");
                    mustDispose = true;
                }
                else
                {
                    SmartCardConnection c = conn;
                    conn = null;
                    System.Diagnostics.Debug.WriteLine("CardRemoved, disposing "+c);
                    c.Dispose();
                }
            }

            queueTask(new AppTaskCardLost(app,this));
        }

        internal Task<byte []> GetATR()
        {
            TaskCompletionSource<byte[]> a = new TaskCompletionSource<byte[]>();
            a.SetResult(atr);
   //         System.Diagnostics.Debug.WriteLine("ATR " + Hex.bytesToHex(atr, 0, atr.Length));
            return a.Task;
        }
    }
}
