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
 * $Id: SOAPConnection.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Web.Http;
using Windows.Storage.Streams;
using MyFeeder;
using System.Runtime.InteropServices.WindowsRuntime;

namespace nz.geek.rhubarb.soap
{
    public class SOAPFault : Exception
    {
        public SOAPFault(String s) : base(s)
        {

        }
    }

	public class SOAPConnection
	{
        HttpClient http = new HttpClient();

        public readonly static XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";

        public readonly static XName 
                soap_Body = soapenv.GetName("Body"),
                soap_Fault = soapenv.GetName("Fault"),
                faultString = "faultstring";

        public async Task<XDocument> sendReceive(String url,String soapAction,XDocument doc)
        {
            byte[] dataB = XDocToByteArray(doc);

#if DEBUG
            {
                System.Diagnostics.Debug.WriteLine("raw " + System.Text.Encoding.UTF8.GetString(dataB, 0, dataB.Length));
            }
#endif
            UInt32 len = (UInt32)dataB.Length;

            HttpBufferContent content = new HttpBufferContent(dataB.AsBuffer());

            content.Headers.Add("Content-Type", "text/xml");

            Uri uri = new Uri(url);

            HttpRequestMessage request = new HttpRequestMessage(
                HttpMethod.Post, uri);

            request.Content = content;

            if (soapAction != null)
            {
                request.Headers.Add("SOAPAction", soapAction);
            }

            HttpResponseMessage response = await http.SendRequestAsync(request);

            response.EnsureSuccessStatusCode();

            IInputStream stream = await response.Content.ReadAsInputStreamAsync();

            MemoryStream data = await Utils.ReadMemoryStreamAsync(stream);

            XDocument reply = XDocument.Load(data);

#if DEBUG
            {
                byte[] ba = XDocToByteArray(reply);

                System.Diagnostics.Debug.WriteLine("xml " + System.Text.Encoding.UTF8.GetString(ba, 0, ba.Length));
            }
#endif
            XElement body = reply.Root.Element(soap_Body);
            XElement fault = body.Element(soap_Fault);

            if (fault!=null)
            {
                String fs=fault.Element(faultString).Value;

                if ((fs==null)||(fs.Length==0))
                {
                    fs = "SOAP fault";
                }

                throw new SOAPFault(fs);
            }

            return reply;
        }

        public static byte[] XDocToByteArray(XDocument doc)
        {
            MemoryStream m = new MemoryStream();
            doc.Save(m);
            long len = m.Position;

            m.Position = 0;

            byte[] ba = new byte[len];

            m.Read(ba, 0, ba.Length);

            if (ba[0]==239 && ba[1] == 187 && ba[2] == 191 && ba[3] == 60)
            {
                m.Position = 3;
                len -= 3;

                ba = new byte[len];
                m.Read(ba, 0, ba.Length);
            }

            return ba;
        }

        public static Stream CreateMemoryStreamFromText(string s)
        {
            MemoryStream ms = new MemoryStream();

            byte [] ba=System.Text.Encoding.UTF8.GetBytes(s);

            ms.Write(ba, 0, ba.Length);
            ms.Position = 0;

            return ms;
        }

        static public string XmlBoolean(bool p)
        {
            return p ? "true" : "false";
        }

        internal void CloseAsync()
        {
            http.Dispose();
        }
    }
}
