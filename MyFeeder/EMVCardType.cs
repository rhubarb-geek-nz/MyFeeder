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
 * $Id: EMVCardType.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System;
using System.Threading.Tasks;
using nz.geek.rhubarb.utils;

namespace MyFeeder
{
	public class EMVCardType: CardType
	{
        private AbstractReader connection;
        private byte[] adf;
        private byte[] aid;
        private string pan, issuer;
        private Boolean hasMonth=false, hasYear = false;
        private int expiryMonth, expiryYear;
        public CreditCardState cardState;

        Boolean hasCardInfo()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("hasCardInfo hasYear=" + hasYear + ", hasMonth=" + hasMonth + ", pan=" + pan + ", issuer=" + issuer);
#endif
            return (hasYear) && (hasMonth) && (pan != null) && (issuer != null);
        }

        internal EMVCardType(byte [] a)
        {
            adf = a;
        }

        private async Task parse(EMVRecord ppse)
        {
            while (ppse.Next() && !hasCardInfo())
            {
                switch (ppse.tag())
                {
                    case 0x6F:
                        {
                            EMVRecord rec = ppse.dataRecord();

                            while (rec.Next() && !hasCardInfo())
                            {
                                switch (rec.tag())
                                {
                                    case 0xA5:
                                        await readPCIPropTemplate(rec.dataRecord());
                                        break;
                                }
                            }
                        }
                        break;
                }
            }
        }

        internal override async Task<bool> CardFound(AbstractReader r)
        {
            Boolean result = false;

            connection = r;

            try
            {
                EMVRecord ppse = new EMVRecord(adf, 0, adf.Length - 2);

                await parse(ppse);

                result = hasCardInfo();

                if (result)
                {
                    cardState = new CreditCardState(issuer, pan, expiryMonth, expiryYear);
                }
            }
            finally
            {
                connection = null;
            }

            return result;
        }

        private async Task readPCIPropTemplate(EMVRecord pci)
        {
            while (pci.Next() && !hasCardInfo())
            {
                switch (pci.tag())
                {
                    case 0xBF0C:
                        await readFCIIssuerDiscretionaryData(pci.dataRecord());
                        break;
                    case 0x88:
                        if (pci.dataLen()==1)
                        {
                            byte[] data = pci.dataBytes();
                            await readPSERecord(data[0]);
                        }
                        break;
                }
            }
        }

        private async Task parsePSERecord(EMVRecord pci)
        {
            while (pci.Next() && !hasCardInfo())
            {
                switch (pci.tag())
                {
                    case 0x70:
                        await readFCIIssuerDiscretionaryData(pci.dataRecord());
                        break;
                }
            }
        }

        private async Task readPSERecord(byte sfi)
        {
            byte num = 1;
            byte[] apdu = { 0x00, (byte)0xB2, num, (byte)((sfi << 3) + 4), 0x00 };

            byte[] data = await connection.TransmitAsync(apdu);

            if ((data!=null)&&(data.Length==2)&&(data[0]==0x6C))
            {
                apdu[4] = data[1];
                data = await connection.TransmitAsync(apdu);
            }

            if ((data != null)&&(data.Length>2))
            {
                EMVRecord pse = new EMVRecord(data,0,data.Length);
                await parsePSERecord(pse);
            }
        }

        private async Task readFCIIssuerDiscretionaryData(EMVRecord fci)
        {
            while (fci.Next() && !hasCardInfo())
            {
                switch (fci.tag())
                {
                    case 0x61:
                        aid = null;
                        issuer = null;
                        await readApplicationTemplate(fci.dataRecord());
                        break;
                }
            }
        }

        private async Task readApplicationTemplate(EMVRecord appTemp)
        {
            while (appTemp.Next() && !hasCardInfo())
            {
                switch (appTemp.tag())
                {
                case 0x6f:
                case 0x70:
                    await readApplicationTemplate(appTemp.dataRecord());
                    break;
                case 0x4F:
                    aid = appTemp.dataBytes();
                    break;
                case 0x50:
                    issuer = appTemp.dataString();
                    break;
                }
            }

            if (aid!=null)
            {
                byte[] apdu = getSelectAPDU(aid);

                byte[] a = await connection.TransmitAsync(apdu);

                a = await handle67and6C(apdu, a);

                if ((a!=null)&&(a.Length>2))
                {
//                    System.Diagnostics.Debug.WriteLine("ADF="+Hex.bytesToHex(a,0,a.Length));

                    EMVRecord r = new EMVRecord(a, 0, a.Length - 2);

                    while (r.Next() && !hasCardInfo())
                    {
                        switch (r.tag())
                        {
                        case 0x6f:
                        case 0x70:
                            await readApplicationDataFile(r.dataRecord());
                            break;
                        }
                    }
                }
            }
        }

        private async Task readApplicationDataFile(EMVRecord r)
        {
            while (r.Next() && !hasCardInfo())
            {
                switch (r.tag())
                {
                    case 0x6f:
                    case 0x70:
                        await readApplicationDataFile(r.dataRecord());
                        break;
                    case 0xA5:
                        await readAppFCI(r.dataRecord());
                        break;
                }
            }
        }

        private async Task readAFL(byte [] data,int offset,int length)
        {
            while ((length > 0) && !hasCardInfo())
            {
                byte sfi = data[offset++];
                byte firstRec = data[offset++];
                byte lastRec = data[offset++];
                byte authNum = data[offset++];

                byte num = firstRec;

                while ((num <= lastRec) && !hasCardInfo())
                {
                    byte [] apdu = new byte[] { 0x00, 0xB2, num, (byte)((sfi & 0xF8) | 4), 0x00 };

                    byte [] resp = await connection.TransmitAsync(apdu);

                    if ((resp!=null)&&(resp.Length==2)&&(resp[0]==0x6C))
                    {
                        apdu[4] = resp[1];
                        resp= await connection.TransmitAsync(apdu);
                    }

                    if ((resp != null) && (resp.Length > 2))
                    {
                        // System.Diagnostics.Debug.WriteLine("AFL response " + Hex.bytesToHex(resp, 0, resp.Length));

                        EMVRecord afl = new EMVRecord(resp, 0, resp.Length - 2);

                        while (afl.Next() && !hasCardInfo())
                        {
                            switch (afl.tag())
                            {
                                case 0x70:
                                    {
                                        EMVRecord aflr = afl.dataRecord();

                                        while (aflr.Next() && !hasCardInfo())
                                        {
                                            switch (aflr.tag())
                                            {
                                                case 0x57:
                                                case 0x9F6B:
                                                    readTrack2Equivalent(aflr.dataHex());
                                                    break;
                                                case 0x5A:
                                                    pan = Hex.bytesToHex(aflr.data, aflr.dataOffset(), aflr.dataLen());
                                                    break;
                                                case 0x5F24:
                                                    expiryMonth = Int16.Parse(Hex.bytesToHex(aflr.data, aflr.dataOffset() + 1, 1));
                                                    expiryYear = Int16.Parse(Hex.bytesToHex(aflr.data, aflr.dataOffset(), 1));
                                                    hasMonth = true;
                                                    hasYear = true;
                                                    break;
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                    }

                    num++;
                }

                length -= 4;
            }
        }

        private async Task readAppFCI(EMVRecord r)
        {
            byte[] pdol = null;

            while (r.Next() && !hasCardInfo())
            {
                switch (r.tag())
                {
                case 0x6f:
                case 0x70:
                    await readAppFCI(r.dataRecord());
                    break;
                case 0x50:
                    issuer = r.dataString();
                    break;
                case 0x9F38:
                    pdol = r.dataBytes();
                    break;
                }
            }

            byte[] pdolData = null;

            if (pdol!=null)
            {
                int pdolLen = getPDOLlength(pdol, 0, pdol.Length);

                pdolData = new byte[pdolLen + 2];
                pdolData[0] = 0x83;
                pdolData[1] = (byte)pdolLen;

                fillPDOL(pdol, 0, pdol.Length, pdolData, 2);
            }

            if (pdolData==null)
            {
                pdolData = new byte[] { 0x83, 0x00 };
            }

            byte[] apdu = new byte[5 + pdolData.Length + (connection.isNFC ? 1 : 0)];
            apdu[0] = 0x80;
            apdu[1] = 0xa8;
            apdu[2] = 0x00;
            apdu[3] = 0x00;
            apdu[4] = (byte)pdolData.Length;
            Buffer.BlockCopy(pdolData, 0, apdu, 5, pdolData.Length);

#if DEBUG
            System.Diagnostics.Debug.WriteLine("PDOL request " + Hex.bytesToHex(apdu, 0, apdu.Length));
#endif
            byte[] resp = await connection.TransmitAsync(apdu);

            resp = await handle67and6C(apdu, resp);

            /* A response on 6984 means use another interface, hence get this on T=CL, and can read on T=0 */

            if ((resp != null)&&(resp.Length>2))
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("PDOL response " + Hex.bytesToHex(resp, 0, resp.Length));
#endif

                EMVRecord pdo = new EMVRecord(resp, 0, resp.Length-2);

                while (pdo.Next() && !hasCardInfo())
                {
                    switch (pdo.tag())
                    {
                    case 0x70:
                    case 0x77:
                        {
                            EMVRecord pdoc = pdo.dataRecord();

                            while (pdoc.Next() && !hasCardInfo())
                            {
                                switch (pdoc.tag())
                                {
                                case 0x57:
                                    readTrack2Equivalent(pdoc.dataHex());
                                    break;
                                case 0x94:
                                    await readAFL(pdoc.data, pdoc.dataOffset(), pdoc.dataLen());
                                    break;
                                }
                            }
                        }
                        break;
                    case 0x80:
                        {
                            EMVRecord pdoc = pdo.dataRecord();
                            await readAFL(pdoc.data, pdoc.dataOffset() + 1, pdoc.dataLen() - 2);
                        }
                        break;
                    }
                }
            }
        }

        void readTrack2Equivalent(string data)
        {
//            System.Diagnostics.Debug.WriteLine("TRACK 2 EQUIV " + data);

            int i = data.IndexOf('D');

            if (i > 0)
            {
                pan = data.Substring(0, i);
                expiryYear = Int16.Parse(data.Substring(i + 1, 2));
                expiryMonth = Int16.Parse(data.Substring(i + 3, 2));
                hasMonth = true;
                hasYear = true;
            }
        }

        int getPDOLlength(byte [] pdol,int offset,int length)
        {
            int tot = 0;

            while (length > 0)
            {
                int tagLen = ((pdol[offset] & 0x1f) == 0x1f) ? 2 : 1;
                int tag = BinaryTools.readInt(pdol, offset, tagLen);
                offset += tagLen;
                length -= tagLen;
                int optLen = pdol[offset++]; length--;
                if (optLen>0x7f)
                {
                    int el = optLen & 0x7f;
                    optLen = BinaryTools.readInt(pdol, offset, el);
                    offset += el;
                    length -= el;
                }

                tot += optLen;
            }

            return tot;
        }

        void fillPDOL(byte[] pdol, int offset, int length,byte [] pdolData,int i)
        {
            while (length > 0)
            {
                int tagLen = ((pdol[offset] & 0x1f) == 0x1f) ? 2 : 1;
                int tag = BinaryTools.readInt(pdol, offset, tagLen);
                offset += tagLen;
                length -= tagLen;
                int optLen = pdol[offset++]; length--;
                if (optLen > 0x7f)
                {
                    int el = optLen & 0x7f;
                    optLen = BinaryTools.readInt(pdol, offset, el);
                    offset += el;
                    length -= el;
                }

                switch (tag)
                {
                    case 0x9F02:
                        /* amount, want zero */
                        break;
                    case 0x9F1A:
                        if (optLen == 2)
                        {
                            pdolData[i] = 0x5;
                            pdolData[i+1] = 0x54;
                        }
                        break;
                    case 0x5F2A:
                    case 0x9F2A:
                        if (optLen == 2)
                        {
                            pdolData[i] = 0x5;
                            pdolData[i+1] = 0x54;
                        }
                        break;
                    case 0x9F66:
                        switch (optLen)
                        {
                            case 4:
                                pdolData[i] = 0x30;
                                pdolData[i+1] = 0x00;
                                pdolData[i+2] = 0x00;
                                pdolData[i+3] = 0x00;
                                break;
                        }
                        break;
                    case 0x9F37:
                        if (optLen>0)
                        {
                            Random rnd = new Random();
                            byte [] r=new byte[optLen];
                            rnd.NextBytes(r);
                            Buffer.BlockCopy(r,0,pdolData,i,optLen);
                        }
                        break;
                    default:
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("PDOL tag ignored " + tag.ToString("X")+", len="+optLen);
#endif
                        break;

                }

                i += optLen;
            }
        }

        byte [] getSelectAPDU(byte [] a)
        {
            byte[] apdu = new byte[5 + a.Length+(connection.isNFC ? 1 : 0)];

            apdu[0] = 0x00;
            apdu[1] = 0xA4;
            apdu[2] = 0x04;
            apdu[3] = 0x00;
            apdu[4] = (byte)a.Length;
            Buffer.BlockCopy(a, 0, apdu, 5, a.Length);

            return apdu;
        }

        internal override string GetIssuerName()
        {
            return issuer;
        }

        internal override string GetPAN()
        {
            return pan;
        }

        internal override string GetExpiry()
        {
            return expiryMonth.ToString("D2") + "/" + expiryYear.ToString("D2");
        }

        internal override bool isSameCard(CardType c)
        {
            if (c == null) return false;
            if (pan == null) return false;
            if (c == this) return true;
            EMVCardType e = c as EMVCardType;
            if (e == null) return false;
            if (!pan.Equals(e.pan))
            {
                return false;
            }
            return true;
        }

        async Task<byte[]> handle67and6C(byte[] apdu, byte[] resp)
        {
            if ((resp != null) && (resp.Length == 2) && ((resp[0] == 0x67) || (resp[0] == 0x6C)))
            {
                int apduLen = 5 + apdu[4];
                byte[] apduLE = new byte[apduLen + 1];
                Buffer.BlockCopy(apdu, 0, apduLE, 0, apduLen);
                apduLE[apduLen] = resp[1];
                resp = await connection.TransmitAsync(apduLE);    
            }

            return resp;
        }
    }
}
