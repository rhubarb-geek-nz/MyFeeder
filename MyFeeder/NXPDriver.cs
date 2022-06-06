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
 * $Id: NXPDriver.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System;
using System.Collections.Generic;
using nz.geek.rhubarb.utils;

namespace MyFeeder
{
    public class PacketError : Exception
    {
        public PacketError(string s) : base(s)
        {
        }
    }

    public class NXPDriver
    {
        public delegate void NXPCardFound(CardSession d);
        static Boolean doDebug = false;
//        System.Threading.AutoResetEvent closeEvent = new System.Threading.AutoResetEvent(false);
        NXPCardFound cardFound;
        int notCompleteCount;

        static int REG_CIU_BIT_FRAMING = 0x633D;
        static int REG_CIU_TX_MODE = 0x6302;
        static int REG_CIU_RX_MODE = 0x6303;
        static int REG_CIU_MANUAL_RCV = 0x630D;
        static int REG_CIU_STATUS2 = 0x6338;
        static int REG_CIU_TX_AUTO = 0x6305;
        static int REG_CIU_CONTROL = 0x633C;

        static byte SYMBOL_TX_CRC_ENABLE = 0x80;
        static byte SYMBOL_TX_LAST_BITS = 0x07;
        static byte SYMBOL_RX_CRC_ENABLE = 0x80;
        static byte SYMBOL_PARITY_DISABLE = 0x10;
        static byte SYMBOL_MF_CRYPTO1_ON = 0x08;
        static byte SYMBOL_FORCE_100_ASK = 0x40;
        static byte SYMBOL_INITIATOR = 0x10;

        public const int NDO_ACTIVATE_FIELD = 0x10;
        public const int NDO_INFINITE_SELECT = 0x20;
        static byte[] ACK = { 0x00, 0x00, 0xff, 0x00, 0xff, 0x00 };

        static void PrintHex(byte[] data, int length)
        {
            System.Diagnostics.Debug.WriteLine(Hex.bytesToHex(data, 0, length));
        }

        static byte[] get_firmware = { 0xD4, 0x02 };

        IDeviceStream stream;

        interface IDriverTask
        {
            Boolean Read(byte[] data); // true means task has completed
            void Start();
            void Complete();
        }

        public class TaskMessage : IDriverTask
        {
            byte[] msg;
            NXPDriver driver;
            int count = 0;

            public Boolean Read(byte[] data)
            {
                Boolean done = false;

                if (isACK(data))
                {
                    count++;
                }
                else
                {
                    byte[] response = ReadMessage(data);

                    if (doDebug)
                    {
                        System.Diagnostics.Debug.WriteLine("TaskMessage.Msg " + count);
                        PrintHex(response, msg.Length);
                    }

                    if (isResponse(msg, response))
                    {
                        done = true;
                    }
                }

                return done;
            }

            public TaskMessage(NXPDriver d,byte[] m)
            {
                msg = m;
                driver = d;
            }

            public void Start()
            {
                driver.WriteMessage(msg);
            }

            public void Complete()
            {
            }
        }

        public class TaskTransmit : IDriverTask
        {
            byte[] msg;
            NXPDriver driver;
            int count = 0;
            ReadCallback readCallback;
            byte[] response;
            Boolean hasTB;

            public Boolean Read(byte[] data)
            {
                Boolean done = false;

                if (isACK(data))
                {
                    count++;
                }
                else
                {
                    response = ReadMessage(data);

                    if (doDebug)
                    {
                        System.Diagnostics.Debug.WriteLine("TaskTransmit.Msg " + count);
                        PrintHex(response, response.Length);
                    }

                    done = true;
                }
                return done;
            }

            public TaskTransmit(Boolean tB,NXPDriver d, byte[] m, ReadCallback cb)
            {
                hasTB = tB;
                msg = new byte[3+m.Length];
                msg[0] = 0xD4;
                msg[1] = (byte)(tB ? 0x40 : 0x42);
                msg[2] = (byte)(tB ? 0x01 : 0x02);

                Buffer.BlockCopy(m, 0, msg, 3, m.Length);

                driver = d;
                readCallback=cb;
            }

            public void Start()
            {
                driver.WriteMessage(msg);
            }

            public void Complete()
            {
                if (doDebug)
                {
                    System.Diagnostics.Debug.WriteLine("TaskTransmit.Complete ");
                    PrintHex(response, response.Length);
                }

                if (response.Length > 3)
                {
                    switch (response[1])
                    {
                        case 0x41:
                            readCallback(BinaryTools.bytesFrom(response,3,response.Length-3));
                            break;
                        case 0x43:
                            readCallback(BinaryTools.bytesFrom(response,4,response.Length-4));
                            break;
                        default:
                            readCallback(null);
                            break;
                    }
                }
                else
                {
                    readCallback(null);
                }
            }
        }

        public class TaskSelect : IDriverTask
        {
            byte[] msg;
            NXPDriver driver;
            int count = 0;
            CardSession card;

            public Boolean Read(byte[] data)
            {
                Boolean done = false;

                if (0 != count++)
                {
                    if (!isACK(data))
                    {
                        byte[] msg = ReadMessage(data);

                        if (doDebug)
                        {
                            System.Diagnostics.Debug.WriteLine("TaskSelect.Msg " + count + " ");
                            PrintHex(msg, msg.Length);
                        }

                        if (isResponse(NFC_SELECT,msg))
                        {
                            if (msg.Length > 5)
                            {
                                card = new CardSession(driver, msg);

                                done = true;
                            }
                            else
                            {
                                Start();
                            }
                        }
                    }
                }

                return done;
            }

            public TaskSelect(NXPDriver d, byte[] m)
            {
                msg = m;
                driver = d;
            }

            public void Start()
            {
                count = 0;
                driver.WriteMessage(msg);
            }

            public void Complete()
            {
                if (card != null)
                {
                    driver.cardFound(card);
                }
            }
        }

        class TaskSetValue : IDriverTask
        {
            NXPDriver driver;
            int count = 0;
            int ui16reg;
            byte mask;
            byte value;

            public Boolean Read(byte[] data)
            {
                Boolean done = false;
                switch (count++)
                {
                    case 0: 
                        break; // ACK # 1
                    case 1:
                        if (isACK(data))
                        {
                            count--;
                        }
                        else
                        {
                            byte[] msg = ReadMessage(data);

                            if (doDebug)
                            {
                                System.Diagnostics.Debug.WriteLine("TaskSetValue.Get " + count + " ");
                                PrintHex(msg, msg.Length);
                            }

                            byte original = msg[2];
                            byte[] cmd = { 0xD4, 0x08, (byte)(ui16reg >> 8), (byte)ui16reg, (byte)(value | (original & ~mask)) };
                            driver.WriteMessage(cmd);
                        }
                        break;
                    case 2:
                        break; // ACK # 2
                    default:
                        if (isACK(data))
                        {
                            count--;
                        }
                        else
                        {
                            byte[] msg = ReadMessage(data);

                            if (doDebug)
                            {
                                System.Diagnostics.Debug.WriteLine("TaskSetValue.Set " + count + " "+Hex.bytesToHex(msg,0,msg.Length));
                            }

                            done = true;
                        }
                        break;
                }
                return done;
            }

            public TaskSetValue(NXPDriver d, int r, byte m, byte v)
            {
                driver = d;
                ui16reg = r;
                mask = m;
                value = v;
            }

            public void Start()
            {
                byte[] cmd = {0xD4,0x06,(byte)(ui16reg>>8),(byte)ui16reg };
                driver.WriteMessage(cmd);
            }

            public void Complete()
            {
            }
        }

        List<IDriverTask> taskList = new List<IDriverTask>();
        List<IDriverTask> completeList = new List<IDriverTask>();
        IDriverTask currentTask = null;
        object taskMutex = new object();

        void QueueTask(IDriverTask task)
        {
            lock(taskMutex)
            {
                if (currentTask!=null)
                {
                    taskList.Add(task);

                    task = null;
                }
                else
                {
                    currentTask = task;
                }
            }

            if (task != null)
            {
                task.Start();
            }
        }

        public NXPDriver(IDeviceStream s)
        {
            stream = s;
        }

        public void Initialize(NXPCardFound cb)
        {
            cardFound = cb;

            stream.Read(Read);
//            stream.Write(ACK);

            QueueTask(new TaskMessage(this,get_firmware));

            set_reg(REG_CIU_BIT_FRAMING, SYMBOL_TX_LAST_BITS, 0x00);

            /* handle CRC */

            set_reg(REG_CIU_TX_MODE, SYMBOL_TX_CRC_ENABLE, SYMBOL_TX_CRC_ENABLE);
            set_reg(REG_CIU_RX_MODE, SYMBOL_RX_CRC_ENABLE, SYMBOL_RX_CRC_ENABLE);

            /* handle parity */

            set_reg(REG_CIU_MANUAL_RCV, SYMBOL_PARITY_DISABLE, 0x00);

            /* disable crypto1 */

            set_reg(REG_CIU_STATUS2, SYMBOL_MF_CRYPTO1_ON, 0x00);

            /* initiator */

            set_reg(REG_CIU_TX_AUTO, SYMBOL_FORCE_100_ASK, 0x40);
            set_reg(REG_CIU_CONTROL, SYMBOL_INITIATOR, 0x10);
        }

        void WriteMessage(byte[] msg)
        {
            if (doDebug)
            {
                System.Diagnostics.Debug.WriteLine("WriteMessage: ");
                PrintHex(msg, msg.Length);
            }

            byte i=0,j=0;;
            byte csum = 0;
            byte len = (byte)msg.Length;
            byte[] packet = new byte[len + 7];
            packet[i++] = 0;
            packet[i++] = 0;
            packet[i++] = 0xFF;
            packet[i++] = len;
            packet[i++] = (byte)-len;
            while (j < len)
            {
                byte b = msg[j++];
                packet[i++] = b;
                csum -= b;
            }
            packet[i++] = csum;
            packet[i++] = 0;

            stream.Write(packet);
        }

        static byte [] ReadMessage(byte [] packet)
        {
            int plen = packet.Length;

            if (plen < 6)
            {
                throw new PacketError("incorrect response length: " + plen);
            }

            if ((packet[0] != 0x00) ||
                (packet[1] != 0x00) ||
                (packet[2] != 0xFF))
            {
                throw new PacketError("incorrect response prefix: " + Hex.bytesToHex(packet, 0, 3));
            }

            byte len = packet[3];
            byte nlen = (byte)(0 - len);

            if (nlen != packet[4])
            {
                throw new PacketError("incorrect response length: " + Hex.bytesToHex(packet, 0, 5));
            }

            if (plen != (len + 7))
            {
                throw new PacketError("incorrect response length: " + Hex.bytesToHex(packet, 0, packet.Length));
            }

            if (packet[plen - 1]!=0x00)
            {
                throw new PacketError("incorrect response trailer: " + Hex.bytesToHex(packet, 0, packet.Length));
            }

            byte[] data = new byte[len];

            byte i = 5,j=0,csum=0;

            while (j < len)
            {
                byte b = packet[i++];
                csum -= b;
                data[j++] = b;
            }

            if (csum != packet[i])
            {
                throw new PacketError("incorrect response checksum: " + Hex.bytesToHex(packet, 0, packet.Length));
            }

            if (doDebug)
            {
                System.Diagnostics.Debug.WriteLine("ReadMessage "+Hex.bytesToHex(data,0,data.Length));
            }

            return data;
        }

        void set_reg(int ui16reg, byte mask, byte value)
        {
            QueueTask(new TaskSetValue(this,ui16reg,mask,value));
        }

        public void Configure(int what, Boolean bEnable)
        {
            switch (what)
            {
                case NDO_ACTIVATE_FIELD:
                    {
                        byte value = (byte)(bEnable ? 0x1 : 0x00);
                        byte[] cmd = { 0xD4, 0x32, 0x01, value };

                        QueueTask(new TaskMessage(this, cmd));
                    }
                    break;
                case NDO_INFINITE_SELECT:
                    if (true)
                    {  
                        /* FF 01 C0 waits around a second */
                        /* FF FE FE waits around a second */

                        byte MxRtyATR = (byte)(bEnable ? 0xFF : 0x00);
                        byte MxRtyPSL = (byte)(bEnable ? 0xFF : 0x00);
                        byte MxRtyPassiveActivation = (byte)(bEnable ? 0xFF : 0x00);
                        byte[] cmd = { 0xD4, 0x32, 0x05, MxRtyATR,MxRtyPSL,MxRtyPassiveActivation};
                        QueueTask(new TaskMessage(this, cmd));
                    }
                    break;
            }
        }

        static byte[] NFC_SELECT = { 0xD4, 0x4A, 0x01, 0x00 };
 //       static byte[] NFC_DESELECT = { 0xD4, 0x44, 0x00 };

        public void Select()
        {
            QueueTask(new TaskSelect(this, NFC_SELECT));
        }

        public void Deselect()
        {
//            QueueTask(new TaskMessage(this, NFC_DESELECT));
            QueueTask(new TaskSelect(this, NFC_SELECT));
        }

        public void Read(byte[] b)
        {
            int offset = 0;

            while (offset < b.Length)
            {
                IDriverTask task;

                int len = b.Length - offset;

                if (len >= ACK.Length)
                {
                    if (BinaryTools.compareBytes(ACK, 0, b, offset, ACK.Length))
                    {
                        len = ACK.Length;
                    }
                }

                byte[] data = BinaryTools.bytesFrom(b, offset, len);

                offset += len;

                lock (taskMutex)
                {
                    task = currentTask;
                }

                if (task != null)
                {
                    if (task.Read(data))
                    {
                        lock (taskMutex)
                        {
                            currentTask = null;

                            completeList.Add(task);

                            if (taskList.Count > 0)
                            {
                                currentTask = taskList[0];

                                taskList.Remove(currentTask);
                            }

                            task = currentTask;
                        }

                        if (task != null)
                        {
                            task.Start();
                        }
                    }
                    else
                    {
                        if (doDebug)
                        {
                            System.Diagnostics.Debug.WriteLine("task not complete " + notCompleteCount++);
                        }
                    }
                }
                else
                {
                    if (doDebug)
                    {
                        System.Diagnostics.Debug.WriteLine("no current task to receive message ");
                    }
                }
            }

            while (true)
            {
                IDriverTask task;

                task = null;

                lock (taskMutex)
                {
                    if (completeList.Count > 0)
                    {
                        task = completeList[0];
                        completeList.Remove(task);
                    }
                }

                if (task == null) break;

                if (doDebug)
                {
                    System.Diagnostics.Debug.WriteLine("task complete " + task);
                }

                task.Complete();
            }
        }

//        public void Close()
//        {
//            stream.Close(ACK, closeComplete);
//            closeEvent.WaitOne();
//        }

//       public void closeComplete()
//       {
//           closeEvent.Set();
//        }

        static Boolean isACK(byte[] data)
        {
           return ((data.Length == ACK.Length) &&
                    BinaryTools.compareBytes(data, 0, ACK, 0, ACK.Length));
        }

        static Boolean isResponse(byte [] req,byte [] resp)
        {
            return ((req[0] + 1) == resp[0]) && ((req[1] + 1) == resp[1]);
        }

        public class CardSession
        {
            NXPDriver driver;
            byte[] ats;
            byte[] atr;
            Boolean hasTB;

            public CardSession(NXPDriver d, byte[] p)
            {
                driver = d;
                ats = p;

                int offset = 8 + ats[7];
                int len = ats[offset++];

                atr = BinaryTools.bytesFrom(ats, offset, len - 1);

                byte T0 = atr[0];

                hasTB = ((T0 & 0x20) != 0);
            }

            public byte[] ATR()
            {
                return atr;
            }

            public void Transmit(byte[] apdu, ReadCallback cb)
            {
                driver.QueueTask(new TaskTransmit(hasTB,driver, apdu, cb));
            }

            public void Disconnect()
            {
                driver.Deselect();
                driver = null;
            }
        }
    }
}
