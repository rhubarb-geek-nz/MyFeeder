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
 * $Id: USBHandler.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Usb;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using nz.geek.rhubarb.utils;

namespace MyFeeder
{
    class USBHandlerTransaction : TaskQueueTask
    {
        readonly USBHandlerConnection connection;
        internal TaskCompletionSource<bool> tcsWait = new TaskCompletionSource<bool>();
        TaskCompletionSource<bool> tcsDone = new TaskCompletionSource<bool>();

        internal USBHandlerTransaction(USBHandlerConnection c)
        {
            connection = c;
        }

        public override Task<bool> Removed()
        {
            if (this==connection.transaction)
            {
                connection.transaction = null;
            }

            TaskCompletionSource<bool> t = tcsWait;

            tcsWait = null;

            if (t!=null)
            {
                t.SetResult(false);
            }

            t = tcsDone;

            tcsDone = null;

            if (t!=null)
            {
                t.SetResult(true);
            }

            return TaskQueue.asBoolAsync(true);
        }

        public override Task<bool> Run()
        {
            connection.transaction = this;

            TaskCompletionSource<bool> t = tcsWait;

            tcsWait = null;

            if (t != null)
            {
                t.SetResult(true);
            }

            return tcsDone.Task;
        }

        internal void Done()
        {
            TaskCompletionSource<bool> t = tcsWait;

            tcsWait = null;

            if (t != null)
            {
                t.SetResult(false);
            }

            t = tcsDone;

            tcsDone = null;

            if (t != null)
            {
                t.SetResult(true);
            }
        }
    }

    class USBHandlerConnection : IDeviceStream
    {
        internal USBHandlerTransaction transaction;
        static byte[] NXP_ACK = { 0x00, 0x00, 0xff, 0x00, 0xff, 0x00 };
        static byte[] NXP_GETFIRMWARE = { 0x00, 0x00, 0xff, 0x02, 0xfe, 0xd4, 0x02, 0x2a, 0x00 };

        internal Task<bool> beginTransaction()
        {
            USBHandlerTransaction tx = new USBHandlerTransaction(this);
            TaskCompletionSource < bool > tw= tx.tcsWait;
            tasks.Add(tx);
            return tw.Task;
        }

        internal void endTransaction()
        {
            USBHandlerTransaction tx = transaction;
            transaction = null;

            if (tx!=null)
            {
                tx.Done();
            }
        }

        //        static byte[] msg3 = { 0x00, 0x00, 0xff, 0x04, 0xfc, 0xd4, 0x06, 0x63, 0x3d, 0x86, 0x00 };
        UsbDevice device;
        readonly internal USBHandler handler;
        public string manufacturer,product;
        Boolean active = false;
        private IOutputStream ios;
        ReadCallback readCallback;
        NXPDriver driver=null;
        internal TaskQueue tasks = new TaskQueue();
        TaskCompletionSource<byte[]> tcs=null;
        NXPDriver.CardSession card=null;
        public CardType cardType;
        public Boolean alive=true;
        List<TaskCompletionSource<bool>> reapers = new List<TaskCompletionSource<bool>>();
        public string deviceId;

        public USBHandlerConnection(USBHandler h, UsbDevice d,string id)
        {
            handler = h;
            device = d;
            deviceId = id;
        }

        public async void run()
        {
            active = true;

            try
            {
                UsbDeviceDescriptor desc = device.DeviceDescriptor;

//                System.Diagnostics.Debug.WriteLine("connectFeeder(" + device.ToString() + ")");

                {
                    UsbSetupPacket s = new UsbSetupPacket();
                    UsbControlRequestType crt = new UsbControlRequestType();
                    crt.AsByte = 0x80;
                    //            crt.Direction = UsbTransferDirection.In;

                    s.Index = 0;
                    s.RequestType = crt;
                    s.Request = 0x6;
                    s.Value = 0x100;

                    s.Length = 18;

                    IBuffer b2 = new Windows.Storage.Streams.Buffer(256);

                    byte [] b4 = (await device.SendControlInTransferAsync(s, b2)).ToArray();

//                    System.Diagnostics.Debug.WriteLine("read desc " + Hex.bytesToHex(b4, 0, b4.Length));

                    byte iManufacturer = b4[14];
                    byte iProduct = b4[15];

                    if (iManufacturer > 0)
                    {
                        s = new UsbSetupPacket();
                        s.Index = 0;
                        s.RequestType = crt;
                        s.Request = 0x6;
                        s.Value = (uint)(0x300 + (0xff & iManufacturer));
                        s.Length = 64;

                        b2 = new Windows.Storage.Streams.Buffer(64);

                        b4 = (await device.SendControlInTransferAsync(s, b2)).ToArray();

//                        System.Diagnostics.Debug.WriteLine("read man " + Hex.bytesToHex(b4, 0, b4.Length));

                        manufacturer=fromUsbString(b4);

//                        System.Diagnostics.Debug.WriteLine("Manufacturer " + manufacturer);
                    }
                    else
                    {
                        manufacturer = desc.VendorId.ToString("X4");
                    }

                    if (iProduct > 0)
                    {
                        s = new UsbSetupPacket();
                        s.Index = 0;
                        s.RequestType = crt;
                        s.Request = 0x6;
                        s.Value = (uint)(0x300 + (0xff & iProduct));
                        s.Length = 64;

                        b2 = new Windows.Storage.Streams.Buffer(64);

                        b4 = (await device.SendControlInTransferAsync(s, b2)).ToArray();

//                        System.Diagnostics.Debug.WriteLine("read prod " + Hex.bytesToHex(b4, 0, b4.Length));

                        product=fromUsbString(b4);

//                        System.Diagnostics.Debug.WriteLine("Product " + product);
                    }
                    else
                    {
                        product = desc.ProductId.ToString("X4");
                    }
                }

                handler.readerFound(this);

                UsbInterface iface = device.DefaultInterface;

                UsbBulkOutPipe pipeOut = iface.BulkOutPipes[0];

                ios = pipeOut.OutputStream;

  //              Write(msg2);

                UsbBulkInPipe pipeIn = iface.BulkInPipes[0];

                IInputStream ist = pipeIn.InputStream;

                await WriteAsync(NXP_ACK);

                driver = new NXPDriver(this);

                driver.Initialize(CardFound);
                driver.Configure(NXPDriver.NDO_ACTIVATE_FIELD, false);
                driver.Configure(NXPDriver.NDO_INFINITE_SELECT, true);
                driver.Configure(NXPDriver.NDO_ACTIVATE_FIELD, true);
                driver.Select();

                while ((readCallback!=null)&&(active))
                {
                    IBuffer b2 = new Windows.Storage.Streams.Buffer(256);

                    byte[] b3 = (await ist.ReadAsync(b2, 256, InputStreamOptions.None)).ToArray();

                    if (App.doTraceData)
                    {
                        System.Diagnostics.Debug.WriteLine("read result " + Hex.bytesToHex(b3, 0, b3.Length));
                    }

                    readCallback(b3);
                }
            }
            catch (Exception ex)
            {
               string s = ex.Message.ToString();

                System.Diagnostics.Debug.WriteLine("USB reading exception " + s);
            }
            finally
            {
                UsbDevice d = device;

                device = null;
                ios = null;

//                System.Diagnostics.Debug.WriteLine("USB reading finally");

                Disconnect();

                alive = false;

                d.Dispose();

                if (active)
                {
                    handler.readerLost(this);
                }

                foreach (TaskCompletionSource<bool> tcs in reapers)
                {
                    tcs.SetResult(true);
                }

//                System.Diagnostics.Debug.WriteLine("USB reading finally cleared up");
            }
        }

        static string fromUsbString(byte[] ba)
        {
            string res = null;

            if ((ba != null) && (ba.Length > 1) && (ba.Length >= ba[0]))
            {
                int len = (ba[0] >> 1) - 1;
                int i = 0, j = 2;
                char[] ca = new char[len];

                while (len-- > 0)
                {
                    int c = ba[j++];
                    c += (ba[j++] << 8);
                    ca[i++] = (char)c;
                }

                res = new String(ca);
            }

            return res;
        }

        Task<bool> Wait(TaskCompletionSource<bool> tcs)
        {
            return tcs.Task;
        }

        internal async Task<bool> SuspendConnection()
        {
            bool result = true;
            try
            {

                //            System.Diagnostics.Debug.WriteLine("USBHandlerState.suspend.begin");

                TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

                reapers.Add(tcs);

                active = false;

                await WriteAsync(NXP_ACK);
                await WriteAsync(NXP_GETFIRMWARE);

                if (alive)
                {
                    if (App.doTraceSuspend)
                    {
                        System.Diagnostics.Debug.WriteLine("USBHandlerConnection.SuspendConnection.wait");
                    }

                    result = await Wait(tcs);
                }

                UsbDevice d = device;

                device = null;

                if (d != null)
                {
                    if (App.doTraceSuspend)
                    {
                        System.Diagnostics.Debug.WriteLine("USBHandlerConnection.SuspendConnection.dispose");
                    }

                    d.Dispose();
                }
            }
            finally
            {
                if (App.doTraceSuspend)
                {
                    System.Diagnostics.Debug.WriteLine("USBHandlerConnection.SuspendConnection.finally");
                }
            }

            return result;
        }

        private async Task<uint> WriteAsync(byte[] data)
        {
            if (App.doTraceData)
            {
                System.Diagnostics.Debug.WriteLine("write data " + Hex.bytesToHex(data, 0, data.Length));
            }

            uint x = await ios.WriteAsync(data.AsBuffer());

//            System.Diagnostics.Debug.WriteLine("write result " + x);

            return x;
        }

        public async void Write(byte[] data)
        {
            if (ios != null)
            {
                uint i=await WriteAsync(data);
            }
        }

        public void Read(ReadCallback cb)
        {
            readCallback = cb;
        }

        public void Close(byte [] data,CloseCallback cb)
        {

        }

        public void CardFound(NXPDriver.CardSession cs)
        {
            card = cs;

            System.Diagnostics.Debug.WriteLine("USBHandler.CardFound " + cs);

            runCardLoop();
        }

        private async void runCardLoop()
        {
            try
            {
                handler.CardFound();

                while (card != null)
                {
                    TimeSpan ts = new TimeSpan(5000000);

                    TaskQueueTask task = await tasks.NextAsync(ts);

                    if (task != null)
                    {
                        bool result = await task.Run();

                        if (!result)
                        {
                            break;
                        }
                    }
                    else
                    {
                        byte[] resp = await TransmitAsync(APDUdefs.GET_RESPONSE);

                        if (resp != null)
                        {
//                            System.Diagnostics.Debug.WriteLine("TransmitAsync result " + Hex.bytesToHex(resp, 0, resp.Length));
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                 System.Diagnostics.Debug.WriteLine("USBHandler result " + ex);
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine("runCardLoop finally");

                handler.CardLost(this);

                Disconnect();
            }
        }

        public void Disconnect()
        {
            NXPDriver.CardSession cs = card;
            card = null;
            cardType = null;

            tasks.Clear();

            TaskCompletionSource<byte[]> t = tcs;

            tcs = null;

            if (t != null)
            {
                byte[] b = null;
                t.SetResult(b);
            }

            if (cs != null)
            {
                cs.Disconnect();
            }
        }

        internal Task<byte[]> TransmitAsync(byte[] apdu)
        {
            if (tcs!=null)
            {
                return null;
            }

            if (card==null)
            {
                return null;
            }

            tcs=new TaskCompletionSource<byte[]>();

            card.Transmit(apdu, transmitCallback);

            return tcs.Task;
        }

        void transmitCallback(byte[] b)
        {
//            System.Diagnostics.Debug.WriteLine("USBHandlerConnection.readCallback "+b);

            if (b != null)
            {
//              System.Diagnostics.Debug.WriteLine("actual response... " + Hex.bytesToHex(b, 0, b.Length));
            }

            TaskCompletionSource<byte[]> t = tcs;

            tcs = null;

            if (t != null)
            {
                t.SetResult(b);
            }
        }
    }
     
    internal class USBHandler
    {
        internal readonly App app;
        internal readonly string deviceId;
        public USBHandlerConnection connection;
        internal readonly AppTaskWaitForNXPReady waiter;
        internal ConcreteNXPReader reader;

        public USBHandler(App a, string s, UsbDevice d, AppTaskWaitForNXPReady w)
        {
            app = a;
            deviceId = s;
            waiter = w;

            if (d != null)
            {
                connection = new USBHandlerConnection(this, d,s);
            }
        }

        internal void onResuming()
        {
            System.Diagnostics.Debug.WriteLine("USBHandler.onResuming");

            connect();
        }

        internal async Task<bool> onSuspending()
        {
            bool result = false;

            System.Diagnostics.Debug.WriteLine("USBHandler.onSuspending begin");

            USBHandlerConnection s = connection;

            connection = null;

            if (s!=null)
            {
                result=await s.SuspendConnection();
            }

            System.Diagnostics.Debug.WriteLine("USBHandler.onSuspending done");

            return result;
        }

        private async void connect()
        {
            Boolean connected = false;

            try
            {
                try
                {
                    var obj = await UsbDevice.FromIdAsync(deviceId);

                    if (obj != null)
                    {
                        UsbDevice dev = obj;

                        if (dev != null)
                        {
                            connection = new USBHandlerConnection(this, dev, deviceId);

                            connected = true;

                            connection.run();
                        }
                    }
                }
                catch (Exception ex)
                {
                    string m = ex.Message;
                    System.Diagnostics.Debug.WriteLine("connect error for " + deviceId + "," + m);
                }
            }
            finally
            {
                if (!connected)
                {
//                    System.Diagnostics.Debug.WriteLine("reader lost for " + deviceId);

                    waiter.lost(this);
                }
            }
        }

        internal void run()
        {
            connection.run();
        }

        internal void readerLost(USBHandlerConnection s)
        {
            //            System.Diagnostics.Debug.WriteLine("readerLost " + s.product);

            if (connection == s)
            {
                waiter.lost(this);
            }
        }

        internal void readerFound(USBHandlerConnection s)
        {
//            System.Diagnostics.Debug.WriteLine("readerFound " + s.product);

            waiter.found(this);
        }

        internal void CardLost(USBHandlerConnection s)
        {
            app.beginTask(new AppTaskCardLost(app, reader));
        }

        internal void CardFound()
        {
            app.beginTask(new AppTaskCardFound(app, reader));
        }
    }
}
