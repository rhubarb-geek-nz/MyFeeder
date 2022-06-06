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
 * $Id: CalypsoCardType.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using nz.geek.rhubarb.utils;
using System;
using System.Threading.Tasks;

namespace MyFeeder
{
    class CalypsoCardType : CardType
    {
        internal byte[] adf,environment,load,csn;
        string expiry = String.Empty, issuer="Calypso";

        public CalypsoCardType(byte[] a)
        {
            adf = a;
        }

        internal override async Task<bool> CardFound(AbstractReader r)
        {
            bool result = false;

            if (adf.Length==2)
            {
                adf = await r.TransmitAsync(APDUdefs.GET_RESPONSE);

                if ((adf.Length==2) && (adf[0]==0x6C))
                {
                    adf = await r.TransmitAsync(new byte[] { 0x00,0xC0,0x00,0x00,adf[1]});
                }
            }

            if (adf.Length>2)
            {
                EMVRecord p1 = new EMVRecord(adf, 0, adf.Length - 2);

                System.Diagnostics.Debug.WriteLine(Hex.bytesToHex(adf, 0, adf.Length));

                while (p1.Next())
                {
                    switch (p1.tag())
                    {
                        case 0x6F:
                            EMVRecord p2 = p1.dataRecord();

                            while (p2.Next())
                            {
                                switch (p2.tag())
                                {
                                    case 0xA5:
                                        EMVRecord p3 = p2.dataRecord();

                                        while (p3.Next())
                                        {
                                            switch (p3.tag())
                                            {
                                                case 0xBF0C:
                                                    EMVRecord p4 = p3.dataRecord();
                                                    while (p4.Next())
                                                    {
                                                        switch (p4.tag())
                                                        {
                                                            case 0xC7:
                                                                csn = p4.dataBytes();
                                                                break;
                                                            default:
                                                                break;
                                                        }
                                                    }
                                                    break;
                                                default:
                                                    break;
                                            }
                                        }
                                        break;
                                    default:
                                        break;
                                }

                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            if (csn!=null)
            {
                environment = await r.TransmitAsync(APDUdefs.CALYPSO_READ_ENVIRONMENT);

                readEnvironment();

                load = await r.TransmitAsync(APDUdefs.CALYPSO_SV_GET_LOAD_00);

                if (load.Length==2 && load[0]==0x6E && load[1]==0x00)
                {
                    load = await r.TransmitAsync(APDUdefs.CALYPSO_SV_GET_LOAD_FA);
                }

                result = true;
            }

            return result;
        }

        private void readEnvironment()
        {
            if (environment.Length > 2)
            {
                int bitoff = 0;
                int version = BinaryTools.readIntBits(environment, bitoff, 6); bitoff += 6;
                int bitmap = BinaryTools.readIntBits(environment, bitoff, 7); bitoff += 7;

                if (0 != (bitmap & 1))
                {
                    int net= BinaryTools.readIntBits(environment, bitoff, 24);
                    byte[] b = new byte[3];
                    BinaryTools.writeInt(b, 0, b.Length, net);
                    issuer = Hex.bytesToHex(b, 0, b.Length);
                    bitoff += 24;
                }

                if (0 != (bitmap & 2))
                {
                    int issuerId= BinaryTools.readIntBits(environment, bitoff, 8);
                    bitoff += 8;

//                    if ((bitmap&1)==0)
                    {
                        byte[] b = new byte[1];
                        BinaryTools.writeInt(b, 0, b.Length, issuerId);
                        /* only populate if network was not present */
                        issuer = Hex.bytesToHex(b,0,b.Length);
                    }
                }

                if (0 != (bitmap & 4))
                {
                    int days = BinaryTools.readIntBits(environment, bitoff, 14); bitoff += 14;
                    DateTime when = new DateTime(1997, 1, 1);
                    System.Diagnostics.Debug.WriteLine("Base date " + when);
                    when = when.AddDays(days);
                    System.Diagnostics.Debug.WriteLine("Expiry date " + when);
                    expiry = when.Day.ToString("D2") + "/" + when.Month.ToString("D2") + "/" + when.Year.ToString("D4");
                }
            }
        }

        internal override string GetExpiry()
        {

            return expiry;
        }

        internal override string GetIssuerName()
        {
            return issuer;
        }

        internal override string GetPAN()
        {
            long num=BinaryTools.readLong(csn, 0, csn.Length);
            String pan = "" + num;
            int i = 10 - pan.Length;
            if (i > 0)
            {
                char[] pad = new char[i];
                while (0 != i--) pad[i] = '0';
                pan = new String(pad) + pan;
            }

            return pan;
        }

        internal override bool isSameCard(CardType other)
        {
            if (other == this) return true;
            CalypsoCardType ct = other as CalypsoCardType;
            if (ct == null) return false;
            if (ct.adf.Length != adf.Length) return false;
            return BinaryTools.compareBytes(adf, 0, ct.adf, 0, adf.Length);
        }

        internal int GetBalance()
        {
            int bal = 0;
        
            if ((load!=null)&&(load.Length>11))
            {
                return BinaryTools.readInt(load, 8, 3);
            }

            return bal;
        }

        internal bool hasBalance()
        {
            return ((load != null) && (load.Length > 11));
        }
    }
}
