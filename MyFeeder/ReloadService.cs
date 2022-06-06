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
 * $Id: ReloadService.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using nz.geek.rhubarb.soap;

namespace MyFeeder.ReloadService
{
    class InterfaceAPDUClient
    {
        private static readonly XNamespace 
                mys = "https://mysnapper.snapper.co.nz",
                syr = "http://schemas.datacontract.org/2004/07/SyrahServer.Common";

        private static readonly XName 
            X_checkClientVersion=mys.GetName("checkClientVersion"),
            X_applicationName=mys.GetName("applicationName"),
            X_applicationVersion=mys.GetName("applicationVersion"),
            X_checkClientVersionResponse = mys.GetName("checkClientVersionResponse"),
            X_checkClientVersionResult = mys.GetName("checkClientVersionResult"),
            X_code = syr.GetName("code"),
            X_message = syr.GetName("message"),
            X_clientAppId = syr.GetName("clientAppId"),
            X_beginReload=mys.GetName("beginReload"),
            X_umtc=mys.GetName("umtc"),
            X_purseInfo=mys.GetName("purseInfo"),
            X_apduinitdata=mys.GetName("apduinitdata"),
            X_apducompletedata=mys.GetName("apducompletedata"),
            X_beginReloadResponse=mys.GetName("beginReloadResponse"),
            X_beginReloadResult=mys.GetName("beginReloadResult"),
            X_completeReload=mys.GetName("completeReload");

        private static readonly String
            urlReloadService = MyFeeder.SyrahService.InternetAddress.SYRAH_ADDRESS+"/ReloadService",
            actionCheckClientVersion = "\"https://mysnapper.snapper.co.nz/Interface_APDU/checkClientVersion\"",
            actionCompleteReload = "\"https://mysnapper.snapper.co.nz/Interface_APDU/completeReload\"",
            actionBeginReload = "\"https://mysnapper.snapper.co.nz/Interface_APDU/beginReload\"";

        private static readonly String templateCheckClientVersion =
            "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:mys=\"https://mysnapper.snapper.co.nz\">" +
                "<soapenv:Header/>" +
                "<soapenv:Body>" +
                    "<mys:checkClientVersion>" +
                        "<mys:applicationName/>" +
                        "<mys:applicationVersion/>" +
                    "</mys:checkClientVersion>" +
                "</soapenv:Body>" +
            "</soapenv:Envelope>";

        private static readonly String templateBeginReload =
		    "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:mys=\"https://mysnapper.snapper.co.nz\">"+
			    "<soapenv:Header/>"+
			    "<soapenv:Body>"+
		      	    "<mys:beginReload>"+
		      		    "<mys:umtc/>"+
		      		    "<mys:purseInfo/>"+
		      		    "<mys:apduinitdata/>"+
		      	    "</mys:beginReload>"+
		        "</soapenv:Body>"+
		    "</soapenv:Envelope>";

	    private static readonly String templateCompleteReload=
		    "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:mys=\"https://mysnapper.snapper.co.nz\">"+
			    "<soapenv:Header/>"+
			    "<soapenv:Body>"+
				    "<mys:completeReload>"+
					    "<mys:umtc/>"+
					    "<mys:apducompletedata/>"+
				    "</mys:completeReload>"+
			    "</soapenv:Body>"+
		    "</soapenv:Envelope>";

        private readonly SOAPConnection soap = new SOAPConnection();

        public async Task<string> beginReloadAsync(string umtc, string purseInfo, string apduInitData)
        {
            XDocument data = XDocument.Load(SOAPConnection.CreateMemoryStreamFromText(templateBeginReload));
            XElement envelope = data.Root;
            XElement body = envelope.Element(SOAPConnection.soap_Body);
            XElement beginReload = body.Element(X_beginReload);

            beginReload.Element(X_umtc).Value=umtc;
            beginReload.Element(X_purseInfo).Value=purseInfo;
            beginReload.Element(X_apduinitdata).Value = apduInitData;

            data = await soap.sendReceive(urlReloadService, actionBeginReload, data);

            envelope = data.Root;
            body = envelope.Element(SOAPConnection.soap_Body);
            XElement beginReloadResponse=body.Element(X_beginReloadResponse);
            XElement beginReloadResult=beginReloadResponse.Element(X_beginReloadResult);

            return beginReloadResult.Value;
        }

        public async Task<bool> completeReloadAsync(string umtc, string apduCompleteData)
        {
            XDocument data = XDocument.Load(SOAPConnection.CreateMemoryStreamFromText(templateCompleteReload));
            XElement envelope = data.Root;
            XElement body = envelope.Element(SOAPConnection.soap_Body);
            XElement beginReload = body.Element(X_completeReload);

            beginReload.Element(X_umtc).Value = umtc;
            beginReload.Element(X_apducompletedata).Value = apduCompleteData;

            data = await soap.sendReceive(urlReloadService, actionCompleteReload, data);

            return true;
        }

        public async Task<ClientVersionResponse> checkClientVersionAsync(string app, string vers)
        {
            XDocument data = XDocument.Load(SOAPConnection.CreateMemoryStreamFromText(templateCheckClientVersion));
            XElement envelope = data.Root;
            XElement body = envelope.Element(SOAPConnection.soap_Body);
            XElement checkClientVersion = body.Element(X_checkClientVersion);
            XElement applicationName = checkClientVersion.Element(X_applicationName);
            XElement applicationVersion = checkClientVersion.Element(X_applicationVersion);

            applicationName.Value = app;
            applicationVersion.Value = vers;

            data = await soap.sendReceive(urlReloadService, actionCheckClientVersion, data);

            ClientVersionResponse resp = new ClientVersionResponse();

            envelope = data.Root;
            body = envelope.Element(SOAPConnection.soap_Body);
            XElement checkClientVersionResponse = body.Element(X_checkClientVersionResponse);
            XElement checkClientVersionResult = checkClientVersionResponse.Element(X_checkClientVersionResult);
            XElement code = checkClientVersionResult.Element(X_code);
            XElement message = checkClientVersionResult.Element(X_message);
            XElement clientAppId = checkClientVersionResult.Element(X_clientAppId);

            if (code!=null)
            {
                resp.codeSpecified = true;
                resp.code = Int32.Parse(code.Value);
            }

            if (message!=null)
            {
                resp.message = message.Value;
            }

            if (clientAppId!=null)
            {
                resp.clientAppIdSpecified = true;
                resp.clientAppId = Int32.Parse(clientAppId.Value);
            }

            return resp;
        }

        internal void CloseAsync()
        {
            soap.CloseAsync();
        }
    }

    public class ClientVersionResponse
    {
        public int clientAppId=0;
        public bool clientAppIdSpecified=false;
        public int code=0;
        public bool codeSpecified=false;
        public string message=null;
    }
}
