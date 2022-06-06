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
 * $Id: MicrosoftPay.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using Windows.ApplicationModel.Payments;

namespace MyFeeder
{
    internal class MicrosoftPay
    {
        internal BasicCardResponse ReadCardResponse(string s)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(BasicCardResponse));
            byte[] b = System.Text.Encoding.UTF8.GetBytes(s);
            MemoryStream ms = new MemoryStream(b);
            ms.Position = 0;
            BasicCardResponse result = ser.ReadObject(ms) as BasicCardResponse;

            return result;
        }

        internal static string data = "{\"supportedNetworks\": [ \"visa\", \"mastercard\" ],\"supportedTypes\": [ \"credit\" ]}";
        internal static string BASIC_CARD = "basic-card";
        internal static string NZD = "NZD";
        internal PaymentRequest request;
        internal PaymentMediator mediator;
        internal PaymentRequestSubmitResult submit;

        internal PaymentRequest makeTopupPaymentRequest(string totalLabel, string itemLabel, string amount)
        {
            List<PaymentMethodData> acceptedPaymentMethodsAll = new List<PaymentMethodData>()
            {
                new PaymentMethodData(new List<String>() { BASIC_CARD},data)
            };

            PaymentCurrencyAmount amountItem = new PaymentCurrencyAmount(amount, NZD);
            PaymentCurrencyAmount amountTotal = new PaymentCurrencyAmount(amount, NZD);
            PaymentItem item = new PaymentItem(itemLabel, amountItem);

            PaymentItem totalItem = new PaymentItem(totalLabel, amountTotal);

            List<PaymentItem> displayItems = new List<PaymentItem>() { item };

            PaymentDetails details = new PaymentDetails()
            {
                DisplayItems = displayItems,
                Total = totalItem,
                ShippingOptions = new List<PaymentShippingOption>(),
                Modifiers = new List<PaymentDetailsModifier>()
                {
                       new PaymentDetailsModifier(new List<String> { BASIC_CARD }, totalItem)
                }
            };

            PaymentMerchantInfo merchantInfo = new PaymentMerchantInfo(new Uri(SyrahService.InternetAddress.SYRAH_ADDRESS));
            PaymentOptions options = new PaymentOptions();

            return new PaymentRequest(details, acceptedPaymentMethodsAll, merchantInfo, options);
        }
    }
}
