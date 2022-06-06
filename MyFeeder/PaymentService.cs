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
 * $Id: PaymentService.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using nz.geek.rhubarb.soap;

namespace MyFeeder.PaymentService
{
    public class InterfacePaymentClient
    {
        static bool skipPending = false;

	    private static readonly String 
            urlPaymentService=MyFeeder.SyrahService.InternetAddress.SYRAH_ADDRESS+"/PaymentService",
            actionPendingReload="\"https://mysnapper.snapper.co.nz/Interface_Payment/hasPendingReload\"",
            actionMakeProductPayment = "\"https://mysnapper.snapper.co.nz/Interface_Payment/makeProductPayment\"",
			actionGetReloadDetails="\"https://mysnapper.snapper.co.nz/Interface_Payment/getReloadDetails\"";

        private static readonly XNamespace
                mys = "https://mysnapper.snapper.co.nz",
                syr = "http://schemas.datacontract.org/2004/07/SyrahServer.Common";

        private static readonly XName
            mys_cardNumber = mys.GetName("cardNumber"),
            mys_hasPendingReload = mys.GetName("hasPendingReload"),
            mys_hasPendingReloadResponse = mys.GetName("hasPendingReloadResponse"),
            mys_hasPendingReloadResult = mys.GetName("hasPendingReloadResult"),
            mys_getReloadDetails = mys.GetName("getReloadDetails"),
            mys_UMTC=mys.GetName("UMTC"),
            mys_getReloadDetailsResponse = mys.GetName("getReloadDetailsResponse"),
            mys_getReloadDetailsResult=mys.GetName("getReloadDetailsResult"),
            mys_token=mys.GetName("token"),
            mys_purseInfo=mys.GetName("purseInfo"),
            mys_productDetail=mys.GetName("productDetail"),
            mys_paymentInstrumentDetail=mys.GetName("paymentInstrumentDetail"),
            mys_makeProductPayment=mys.GetName("makeProductPayment"),
            mys_makeProductPaymentResponse=mys.GetName("makeProductPaymentResponse"),
            mys_makeProductPaymentResult = mys.GetName("makeProductPaymentResult"),
            syr_CCExpiryMonth = syr.GetName("CCExpiryMonth"),
            syr_CCExpiryYear = syr.GetName("CCExpiryYear"),
            syr_CCHolderName = syr.GetName("CCHolderName"),
            syr_CCNumber = syr.GetName("CCNumber"),
            syr_CCType = syr.GetName("CCType"),
            syr_CCSecurityCode = syr.GetName("CCSecurityCode"),
            syr_message=syr.GetName("message"),
            syr_date=syr.GetName("date"),
            syr_reloadDetail=syr.GetName("reloadDetail"),
            syr_Amount=syr.GetName("Amount"),
            syr_PaymentDateTime=syr.GetName("PaymentDateTime"),
            syr_Refundable=syr.GetName("Refundable"),
            syr_SourceType=syr.GetName("SourceType"),
            syr_UMTC=syr.GetName("UMTC"),
            syr_creditCents = syr.GetName("creditCents"),
            syr_RFPassID = syr.GetName("RFPassID"),
            syr_regionID = syr.GetName("regionID"),
            syr_authCents = syr.GetName("authCents"),
            syr_name = syr.GetName("name"),
            syr_remember = syr.GetName("remember"),
            syr_pin = syr.GetName("pin"),
            syr_fields=syr.GetName("fields"),
            syr_url=syr.GetName("url"),
            syr_session=syr.GetName("session"),
            syr_paymentRedirect = syr.GetName("paymentRedirect");

        private static readonly String templatePendingReload =
		    "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:mys=\"https://mysnapper.snapper.co.nz\">"+
			    "<soapenv:Header/>"+
			    "<soapenv:Body>"+
				    "<mys:hasPendingReload>"+
					    "<mys:cardNumber/>"+
				    "</mys:hasPendingReload>"+
			    "</soapenv:Body>"+
		    "</soapenv:Envelope>";

        private static readonly String templateGetReloadDetails =
			"<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:mys=\"https://mysnapper.snapper.co.nz\">"+
				"<soapenv:Header/>"+
   				"<soapenv:Body>"+
      				"<mys:getReloadDetails>"+
         				"<mys:UMTC/>"+
      				"</mys:getReloadDetails>"+
   				"</soapenv:Body>"+
			"</soapenv:Envelope>";

        private static readonly String templateMakeProductPayment =
			"<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:mys=\"https://mysnapper.snapper.co.nz\" xmlns:syr=\"http://schemas.datacontract.org/2004/07/SyrahServer.Common\">"+
   				"<soapenv:Header/>"+
   				"<soapenv:Body>"+
      				"<mys:makeProductPayment>"+
         				"<mys:token/>"+
         				"<mys:purseInfo/>"+
         				"<mys:productDetail>"+
            				"<syr:creditCents/>"+
            				"<syr:RFPassID/>"+
            				"<syr:regionID/>"+
            				"<syr:authCents/>"+
         				"</mys:productDetail>"+
         				"<mys:paymentInstrumentDetail>"+
            				"<syr:name/>"+
            				"<syr:remember/>"+
            				"<syr:pin/>"+
         				"</mys:paymentInstrumentDetail>"+
      				"</mys:makeProductPayment>"+
   				"</soapenv:Body>"+
			"</soapenv:Envelope>";

        private readonly SOAPConnection soap = new SOAPConnection();
        public async Task<PaymentResult> makeProductPaymentAsync(string token, string purseInfo, ProductDetail productDetail, PaymentInstrumentDetail paymentInstrumentDetail)
        {
            XDocument data = XDocument.Load(SOAPConnection.CreateMemoryStreamFromText(templateMakeProductPayment));
            XElement envelope = data.Root;
            XElement body = envelope.Element(SOAPConnection.soap_Body);
            XElement makeProductPayment = body.Element(mys_makeProductPayment);
            XElement pd = makeProductPayment.Element(mys_productDetail);
            XElement pi = makeProductPayment.Element(mys_paymentInstrumentDetail);
            XElement tok = makeProductPayment.Element(mys_token);

            if (token!=null)
            {
                tok.Value = token;
            }
            else
            {
                tok.Remove();
            }

            makeProductPayment.Element(mys_purseInfo).Value = purseInfo;

            if (productDetail==null)
            {
                pd.Remove();
            }
            else
            {
                XElement authCents = pd.Element(syr_authCents);
                XElement creditCents = pd.Element(syr_creditCents);
                XElement rfpassid = pd.Element(syr_RFPassID);
                XElement regionId = pd.Element(syr_regionID);

                if (productDetail.authCentsSpecified)
                {
                    authCents.Value = productDetail.authCents.ToString();
                }
                else
                {
                    authCents.Remove();
                }

                if (productDetail.creditCentsSpecified)
                {
                    creditCents.Value = productDetail.creditCents.ToString();
                }
                else
                {
                    creditCents.Remove();
                }

                if (productDetail.regionIDSpecified)
                {
                    regionId.Value = productDetail.regionID.ToString();
                }
                else
                {
                    regionId.Remove();
                }

                if (productDetail.RFPassIDSpecified)
                {
                    rfpassid.Value = productDetail.RFPassID.ToString();
                }
                else
                {
                    rfpassid.Remove();
                }
            }

            if (paymentInstrumentDetail==null)
            {
                pi.Remove();
            }
            else
            {
                XElement remember = pi.Element(syr_remember);
                XElement pin = pi.Element(syr_pin);
                XElement name = pi.Element(syr_name);

                if (paymentInstrumentDetail.rememberSpecified)
                {
                    remember.Value = SOAPConnection.XmlBoolean(paymentInstrumentDetail.remember);
                }
                else
                {
                    remember.Remove();
                }

                if (paymentInstrumentDetail.name!=null)
                {
                    name.Value = paymentInstrumentDetail.name;
                }
                else
                {
                    name.Remove();
                }

                if (paymentInstrumentDetail.pin!=null)
                {
                    pin.Value = paymentInstrumentDetail.pin;
                }
                else
                {
                    pin.Remove();
                }
            }

            data = await soap.sendReceive(urlPaymentService, actionMakeProductPayment, data);

            envelope = data.Root;
            body = envelope.Element(SOAPConnection.soap_Body);

            XElement makeProductPaymentResponse = body.Element(mys_makeProductPaymentResponse);
            XElement makeProductPaymentResult = makeProductPaymentResponse.Element(mys_makeProductPaymentResult);

            return decodePaymentResult(makeProductPaymentResult);
        }

        static PaymentResult decodePaymentResult(XElement el)
        {
            PaymentResult result = new PaymentResult();

            result.message = namedElementValue(el, syr_message);

            XElement v = el.Element(syr_date);

            if (v != null)
            {
                result.date = (DateTime)v;
                result.dateSpecified = true;
            }

            v = el.Element(syr_paymentRedirect);

            if (v!=null)
            {
                result.paymentRedirect = decodePaymentRedirect(v);
            }

            return result;
        }

        static PaymentRedirectResult decodePaymentRedirect(XElement el)
        {
            PaymentRedirectResult result = new PaymentRedirectResult();

            result.session = namedElementValue(el,syr_session);
            result.url = namedElementValue(el, syr_url);

            XElement v = el.Element(syr_fields);

            if (v!=null)
            {
                result.fields = decodeCreditCardFields(v);
            }

            return result;
        }

        private static CreditCardFields decodeCreditCardFields(XElement v)
        {
            CreditCardFields result = new CreditCardFields();
            result.CCExpiryMonth = namedElementValue(v,syr_CCExpiryMonth);
            result.CCExpiryYear = namedElementValue(v,syr_CCExpiryYear);
            result.CCHolderName = namedElementValue(v,syr_CCHolderName);
            result.CCNumber = namedElementValue(v,syr_CCNumber);
            result.CCSecurityCode = namedElementValue(v,syr_CCSecurityCode);
            result.CCType = namedElementValue(v, syr_CCType);
            return result;
        }

        static String namedElementValue(XElement v,XName n)
        {
            String res = null;
            if (v!=null) v = v.Element(n);
            if (v != null) res = v.Value;
            return res;
        }

        public async Task<hasPendingReloadResponse> hasPendingReloadAsync(string cardNumber)
        {
            hasPendingReloadResponse result = new hasPendingReloadResponse();

            if (skipPending)
            {
                result.hasPendingReloadResult = new ReloadDetail[0];
            }
            else
            {
                XDocument data = XDocument.Load(SOAPConnection.CreateMemoryStreamFromText(templatePendingReload));
                XElement envelope = data.Root;
                XElement body = envelope.Element(SOAPConnection.soap_Body);
                XElement hasPendingReload = body.Element(mys_hasPendingReload);

                hasPendingReload.Element(mys_cardNumber).Value = cardNumber;

                data = await soap.sendReceive(urlPaymentService, actionPendingReload, data);


                envelope = data.Root;
                body = envelope.Element(SOAPConnection.soap_Body);

                XElement hasPendingReloadResponse = body.Element(mys_hasPendingReloadResponse);
                XElement hasPendingReloadResult = hasPendingReloadResponse.Element(mys_hasPendingReloadResult);

                if (hasPendingReloadResult != null)
                {
                    IEnumerable<XElement> reloadDetail = hasPendingReloadResult.Elements(syr_reloadDetail);
                    List<ReloadDetail> list = new List<ReloadDetail>();

                    foreach (XElement el in reloadDetail)
                    {
                        list.Add(decodeReloadDetail(el));
                    }

                    ReloadDetail[] details = new ReloadDetail[list.Count];

                    int i = 0;

                    while (i < details.Length)
                    {
                        details[i] = list[i];
                        i++;
                    }

                    result.hasPendingReloadResult = details;
                }
            }

            return result;
        }

        public async Task<ReloadDetail> getReloadDetailsAsync(string umtc)
        {
            XDocument data = XDocument.Load(SOAPConnection.CreateMemoryStreamFromText(templateGetReloadDetails));
            XElement envelope = data.Root;
            XElement body = envelope.Element(SOAPConnection.soap_Body);
            XElement hasPendingReload = body.Element(mys_getReloadDetails);

            hasPendingReload.Element(mys_UMTC).Value = umtc;

            data = await soap.sendReceive(urlPaymentService, actionGetReloadDetails, data);

            envelope = data.Root;
            body = envelope.Element(SOAPConnection.soap_Body);

            XElement getReloadDetailsResponse = body.Element(mys_getReloadDetailsResponse);
            XElement getReloadDetailsResult = getReloadDetailsResponse.Element(mys_getReloadDetailsResult);

            return decodeReloadDetail(getReloadDetailsResult);
        }

        static ReloadDetail decodeReloadDetail(XElement el)
        {
            ReloadDetail detail = new ReloadDetail();

            XElement v = el.Element(syr_Amount);

            if (v != null)
            {
                detail.AmountSpecified = true;
                detail.Amount = Int32.Parse(v.Value);
            }

            v = el.Element(syr_PaymentDateTime);

            if (v != null)
            {
                detail.PaymentDateTimeSpecified = true;
                detail.PaymentDateTime = (DateTime)v;
            }

            v = el.Element(syr_Refundable);

            if (v != null)
            {
                detail.RefundableSpecified = true;
                detail.Refundable = Boolean.Parse(v.Value);
            }

            v = el.Element(syr_SourceType);

            if (v != null)
            {
                detail.SourceType = v.Value;
            }

            v = el.Element(syr_UMTC);

            if (v != null)
            {
                detail.UMTC = v.Value;
            }

            return detail;
        }

        internal void CloseAsync()
        {
            soap.CloseAsync();
        }
    }

    public class CreditCardDetails
    {
        public int CCExpiryMonth=0;
        public bool CCExpiryMonthSpecified=false;
        public int CCExpiryYear=0;
        public bool CCExpiryYearSpecified=false;
        public string CCHolderName=null;
        public string CCNumber=null;
        public string CCSecurityCode=null;
        public string CCType=null;
    }

    public class PaymentResult
    {
        public string companyName=null;
        public DateTime date;
        public bool dateSpecified=false;
        public string GSTnumber=null;
        public string message=null;
        public PaymentRedirectResult paymentRedirect=null;
    }

    public class ProductDetail
    {
        public int authCents=0;
        public bool authCentsSpecified=false;
        public int creditCents=0;
        public bool creditCentsSpecified=false;
        public int regionID=0;
        public bool regionIDSpecified=false;
        public int RFPassID=0;
        public bool RFPassIDSpecified=false;
    }

    public class PaymentInstrumentDetail
    {
        public string name=null;
        public string pin=null;
        public bool remember=false;
        public bool rememberSpecified=false;
    }

    public class PaymentRedirectResult
    {
        public string session=null;
        public string url=null;
        public CreditCardFields fields=null;
    }

    public class CreditCardFields
    {
        public string CCExpiryMonth=null;
        public string CCExpiryYear=null;
        public string CCHolderName=null;
        public string CCNumber=null;
        public string CCSecurityCode=null;
        public string CCType=null;
    }

    public class hasPendingReloadResponse
    {
        public ReloadDetail[] hasPendingReloadResult;
    }

    public class ReloadDetail
    {
        public int Amount=0;
        public bool AmountSpecified=false;
        public DateTime PaymentDateTime;
        public bool PaymentDateTimeSpecified=false;
        public bool Refundable=false;
        public bool RefundableSpecified = false;
        public string SourceType = null;
        public string UMTC=null;
    }
}
