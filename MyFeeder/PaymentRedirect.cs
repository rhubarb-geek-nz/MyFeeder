/**************************************************************************
 *
 *  Copyright 2014, Roger Brown
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
 * $Id: PaymentRedirect.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System;
using System.Net;
using Windows.Web.Http;
using System.IO;
using MyFeeder.PaymentService;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace MyFeeder
{
    internal class PaymentRedirect
    {
        static internal async Task handleRedirect(PaymentRedirectResult paymentRedirectResult, CreditCardState creditCard, string cvvArg)
        {
            string pan = WebUtility.UrlEncode(creditCard.pan);
            string month = WebUtility.UrlEncode(creditCard.month.ToString("00"));
            string year = WebUtility.UrlEncode(creditCard.year.ToString("00"));
            string cvv = WebUtility.UrlEncode(cvvArg);

            string url = paymentRedirectResult.url + "&" +
                            paymentRedirectResult.fields.CCNumber + "=" + pan + "&" +
                            paymentRedirectResult.fields.CCExpiryMonth + "=" + month + "&" +
                            paymentRedirectResult.fields.CCExpiryYear + "=" + year + "&" +
                            paymentRedirectResult.fields.CCSecurityCode + "=" + cvv;

            HttpClient request = new HttpClient();

            try
            {
                IHttpContent content = null;
                Uri uri = new Uri(url);

                HttpResponseMessage response = await request.PostAsync(uri, content);

                try
                {
                    IInputStream s = await response.Content.ReadAsInputStreamAsync();

                    try
                    {
                        MemoryStream data = await Utils.ReadMemoryStreamAsync(s);

                        parseRedirectResponse(data);
                    }
                    finally
                    {
                        s.Dispose();
                    }
                }
                finally
                {
                    response.Dispose();
                }
            }
            finally
            {
                request.Dispose();
            }
        }

        static private byte [] GetBuffer(MemoryStream data)
        {
            long  llen = data.Length;
            if (llen > Int32.MaxValue) llen = Int32.MaxValue;
            int len = (int)llen;
            byte[] b = new byte[len];
            int x=data.Read(b, 0, len);
            if (x != len) return null;
            data.Position = 0;
            return b;
        }

        static private void parseRedirectResponse(MemoryStream data)
        {
            byte[] b = GetBuffer(data);
            int offset = 0;

            while (offset < b.Length)
            {
                int i = 0;

                while ((offset + i) < b.Length)
                {
                    if (b[offset + i] == '=')
                    {
                        break;
                    }

                    i++;
                }

                string label = Utils.StringFromBytes(b, offset, i);

                offset += i;

                if (offset < b.Length)
                {
                    if (b[offset] == '=')
                    {
                        offset++;

                        i = 0;

                        while ((offset + i) < b.Length)
                        {
                            if (b[offset + i] == '&')
                            {
                                break;
                            }

                            i++;
                        }

                        string valEnc = Utils.StringFromBytes(b, offset, i);

                        offset += i;

                        if ((offset + i) < b.Length)
                        {
                            offset++;
                        }

                        if ("ERROR".Equals(label))
                        {
                            throw new Exception(WebUtility.UrlDecode(valEnc));
                        }
                    }
                }
            }
        }
    }
}
