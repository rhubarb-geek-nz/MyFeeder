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
 * $Id: TopupTransactionTask.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System;
using System.Threading.Tasks;
using nz.geek.rhubarb.utils;
using MyFeeder.ReloadService;
using MyFeeder.PaymentService;
using Windows.ApplicationModel.Payments;

namespace MyFeeder
{
    internal class TopupTransactionTask : AppTask
	{
        private readonly TopupTransaction transaction;
        private readonly InterfaceAPDUClient interfaceAPDU=new InterfaceAPDUClient();
        private readonly InterfacePaymentClient interfacePayment = new InterfacePaymentClient();
        private AbstractReader reader = null;

        internal TopupTransactionTask(App a, TopupTransaction t): base(a)
        {
            transaction = t;
        }

        internal override async void beginTask()
        {
            bool bTransaction = false;

            try
            {
                try
                {
                    Boolean bContinue = true;

                    while (bContinue)
                    {
                        bContinue = false;

                        switch (transaction.step)
                        {
                            case TopupTransaction.STEP_PENDING:
                                System.Diagnostics.Debug.WriteLine("task: TopupTransaction.STEP_PENDING");

                                try
                                {
                                    PaymentService.hasPendingReloadResponse resp = await interfacePayment.hasPendingReloadAsync(Hex.bytesToHex(transaction.purseInfo, 8, 8));
                                    int count = resp.hasPendingReloadResult.Length;

                                    transaction.busyMax = (count << 1);

                                    if (transaction.busyMax > 0)
                                    {
                                        busyChanged();
                                    }

                                    if (count > 0)
                                    {
                                        transaction.reloadDetails = resp.hasPendingReloadResult;
                                        bContinue = true;
                                        transaction.step = TopupTransaction.STEP_CARDWAIT;
                                    }
                                    else
                                    {
                                        transaction.reloadDetails = null;

                                        if (transaction.creditCard == null)
                                        {
                                            foreach (AbstractReader rdr in app.readers)
                                            {
                                                CardType ct = rdr.currentCard;

                                                if (ct != null)
                                                {
                                                    EMVCardType emv = ct as EMVCardType;

                                                    if (emv != null)
                                                    {
                                                        transaction.creditCard = emv.cardState;
                                                        transaction.bAutomaticEntryCC = true;
                                                        break;
                                                    }
                                                }
                                            }
                                        }

                                        transaction.step = TopupTransaction.STEP_ENTRY;

                                        System.Diagnostics.Debug.WriteLine("task: TopupTransaction.STEP_ENTRY");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    transaction.step = TopupTransaction.STEP_PENDING_FAILED;
                                    transaction.resultMessage = ex.Message;

                                    System.Diagnostics.Debug.WriteLine("task: TopupTransaction.STEP_PENDING_FAILED " + transaction.resultMessage);

                                    Exception ex2 = ex.InnerException;

                                    if (ex2 != null)
                                    {
                                        System.Diagnostics.Debug.WriteLine("task: inner exception was " + ex2);

                                        //                            transaction.resultMessage = ex2.Message;
                                    }
                                }
                                finally
                                {
                                    if (transaction.step == TopupTransaction.STEP_PENDING)
                                    {
                                        transaction.step = TopupTransaction.STEP_PENDING_FAILED;
                                        transaction.resultMessage = "Internal error";
                                        System.Diagnostics.Debug.WriteLine("task: TopupTransaction.STEP_PENDING_FAILED " + transaction.resultMessage);
                                    }
                                }

                                break;

                            case TopupTransaction.STEP_PAYMENT:
                                {
                                    MicrosoftPay msPay = null;

                                    System.Diagnostics.Debug.WriteLine("task: TopupTransaction.STEP_PAYMENT");
                                    try
                                    {
                                        PaymentResult pay = null;
                                        CreditCardState creditCard = transaction.creditCard;
                                        string cvv = transaction.cvv;
                                        string cardHolderName = "myfeeder";

                                        if (app.isPaymentApiSupported && (creditCard == null))
                                        {
                                            System.Diagnostics.Debug.WriteLine("payment without a credit card");
                                            string total = app.resourceLoader.GetString("PaymentTotal");
                                            msPay = new MicrosoftPay();

                                            string item = Utils.cardNumberWithSpace(Hex.bytesToHex(transaction.purseInfo, 8, 8));
                                            string amount = Utils.toMoney(transaction.amount);

                                            msPay.request = msPay.makeTopupPaymentRequest(total, item,amount);
                                            msPay.mediator = new PaymentMediator();
                                            msPay.submit = await msPay.mediator.SubmitPaymentRequestAsync(msPay.request);
                                            PaymentResponse response = msPay.submit.Response;
                                            PaymentRequestStatus status = msPay.submit.Status;

                                            System.Diagnostics.Debug.WriteLine("Microsoft Pay "+status);

                                            switch (status)
                                            {
                                                case PaymentRequestStatus.Succeeded:
                                                    System.Diagnostics.Debug.WriteLine("PaymentMethodId " + response.PaymentToken.PaymentMethodId);

                                                    if (MicrosoftPay.BASIC_CARD.Equals(response.PaymentToken.PaymentMethodId))
                                                    {
                                                        BasicCardResponse basicCard = msPay.ReadCardResponse(response.PaymentToken.JsonDetails);
                                                        string issuer ;
                                                        string pan = basicCard.CardNumber;
                                                        int month = Int32.Parse(basicCard.ExpiryMonth);
                                                        int year = Int32.Parse(basicCard.ExpiryYear) % 100; /* just last two digits */
                                                        switch (pan[0])
                                                        {
                                                            case '4': issuer = "VISA"; break;
                                                            case '5': issuer = "MASTERCARD"; break;
                                                            default: issuer = "UNKNOWN"; break;
                                                        }
                                                        creditCard = new CreditCardState(issuer,pan,month,year);
                                                        cvv = basicCard.CardSecurityCode;
                                                        cardHolderName = basicCard.CardholderName;
                                                    }
                                                    else
                                                    {
                                                        throw new Exception("Unknown payment method : " + response.PaymentToken.PaymentMethodId);
                                                    }
                                                    break;
                                                case PaymentRequestStatus.Canceled:
                                                    msPay = null;
                                                    throw new Exception(app.resourceLoader.GetString("PaymentCancelled"));
                                                default:
                                                    msPay = null;
                                                    throw new Exception(app.resourceLoader.GetString("PaymentError"));
                                            }
                                        }

                                        {
                                            PaymentInstrumentDetail pid = null; //new PaymentInstrumentDetail();
                                            ProductDetail prod = new ProductDetail();
                                            prod.creditCents = transaction.amount;
                                            prod.creditCentsSpecified = true;

                                            pay = await interfacePayment.makeProductPaymentAsync(null,
                                                    encode(transaction.purseInfo, 0, transaction.purseInfo.Length),
                                                    prod, pid);

                                            transaction.busyStep++;

                                            if (pay.paymentRedirect != null)
                                            {
                                                busyChanged();

                                                await PaymentRedirect.handleRedirect(pay.paymentRedirect, creditCard, cvv);

                                                transaction.busyStep++;
                                            }

                                            transaction.busyMax = transaction.busyStep + 3;

                                            busyChanged();
                                        }

                                        // confirm the reload amount 
                                        PaymentService.ReloadDetail reload = await interfacePayment.getReloadDetailsAsync(pay.message);
                                        transaction.busyStep++;
                                        busyChanged();

                                        transaction.reloadDetails = new PaymentService.ReloadDetail[] { reload };

                                        transaction.step = TopupTransaction.STEP_CARDWAIT;

                                        bContinue = true;

                                        if (msPay != null)
                                        {
                                            MicrosoftPay msp = msPay;
                                            msPay = null;
                                            await msp.submit.Response.CompleteAsync(PaymentRequestCompletionStatus.Succeeded);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        transaction.step = TopupTransaction.STEP_PAYMENT_FAILED;
                                        transaction.resultMessage = ex.Message;

                                        System.Diagnostics.Debug.WriteLine("task: TopupTransaction.STEP_PAYMENT_FAILED " + transaction.resultMessage);

                                        if (msPay != null)
                                        {
                                            MicrosoftPay msp = msPay;
                                            msPay = null;
                                            await msp.submit.Response.CompleteAsync(PaymentRequestCompletionStatus.Failed);
                                        }
                                    }
                                    finally
                                    {
                                        if (!bContinue)
                                        {
                                            if (transaction.step == TopupTransaction.STEP_PAYMENT)
                                            {
                                                transaction.step = TopupTransaction.STEP_TOPUP_FAILED;
                                                transaction.resultMessage = "Internal error";
                                            }

                                            System.Diagnostics.Debug.WriteLine("task: TopupTransaction.STEP_TOPUP_PAYMENT " + transaction.resultMessage);
                                        }

                                        if (msPay != null)
                                        {
                                            /* if this is still left over, get rid of it */
                                            MicrosoftPay msp = msPay;
                                            msPay = null;
                                            Windows.Foundation.IAsyncAction task = msp.submit.Response.CompleteAsync(PaymentRequestCompletionStatus.Failed);
                                        }
                                    }
                                }
                                break;

                            case TopupTransaction.STEP_TOPUP:
                                System.Diagnostics.Debug.WriteLine("task: TopupTransaction.STEP_TOPUP");

                                try
                                {
                                    if ((reader!=null)&&!bTransaction)
                                    {
                                        bTransaction = await reader.beginTransaction();
                                    }

                                    if (bTransaction)
                                    {
                                        byte[] pi = await reader.TransmitAsync(APDUdefs.SELECT_SNAPPER);

                                        if ((pi.Length == transaction.purseInfo.Length) && (BinaryTools.compareBytes(pi, 0, transaction.purseInfo, 0, pi.Length)))
                                        {
                                            if (transaction.reloadDetails != null)
                                            {
                                                int i = 0;

                                                while (i < transaction.reloadDetails.Length)
                                                {
                                                    PaymentService.ReloadDetail reload = transaction.reloadDetails[i];

                                                    if (reload != null)
                                                    {
                                                        await performReload(reload.UMTC, reload.Amount);
                                                    }

                                                    transaction.reloadDetails[i++] = null;
                                                }

                                                transaction.reloadDetails = null;
                                            }

                                            transaction.step = TopupTransaction.STEP_COMPLETE;

                                            System.Diagnostics.Debug.WriteLine("task: TopupTransaction.STEP_COMPLETE");
                                        }
                                        else
                                        {
                                            transaction.step = TopupTransaction.STEP_CARDWAIT;

                                            System.Diagnostics.Debug.WriteLine("task: TopupTransaction.STEP_CARDWAIT");
                                        }
                                    }
                                    else
                                    {
                                        transaction.step = TopupTransaction.STEP_CARDWAIT;

                                        System.Diagnostics.Debug.WriteLine("task: TopupTransaction.STEP_CARDWAIT");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    transaction.step = TopupTransaction.STEP_TOPUP_FAILED;
                                    transaction.resultMessage = ex.Message;

                                    System.Diagnostics.Debug.WriteLine("task: TopupTransaction.STEP_TOPUP_FAILED " + transaction.resultMessage);
                                }
                                finally
                                {
                                    if (transaction.step == TopupTransaction.STEP_TOPUP)
                                    {
                                        transaction.step = TopupTransaction.STEP_TOPUP_FAILED;
                                        transaction.resultMessage = "Internal error";

                                        System.Diagnostics.Debug.WriteLine("task: TopupTransaction.STEP_TOPUP_FAILED " + transaction.resultMessage);
                                    }
                                }
                                break;

                            case TopupTransaction.STEP_CARDWAIT:
                                System.Diagnostics.Debug.WriteLine("task: TopupTransaction.STEP_CARDWAIT");
                                /* if the card is present in a reader then switch to STEP_TOPUP */

                                reader = app.findReaderForSnapperCard(transaction.purseInfo);

                                if (reader!=null)
                                {
                                    System.Diagnostics.Debug.WriteLine("task: TopupTransaction.STEP_CARDWAIT found card in "+reader.DisplayName);
                                    bContinue = true;
                                    transaction.step = TopupTransaction.STEP_TOPUP;
                                }

                                break;

                            case TopupTransaction.STEP_INIT:
                            case TopupTransaction.STEP_ENTRY:
                                transaction.step = TopupTransaction.STEP_UNEXPECTED_FAILED;
                                transaction.resultMessage = "Internal transaction state error, please try again.";
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Transaction exception " + ex.Message);
                }
                finally
                {
                    if (bTransaction)
                    {
                        reader.endTransaction(false);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Transaction exception " + ex.Message);
            }
            finally
            {
                transaction.busyMax = 0;
                transaction.busyStep = 0;

                finished();
            }
        }

        /*
            >> 00A4040007D4100000030001
            << 6F31B02F001001011010000000346072000004289720110216201602152000000186A0000300000000000000000000000000009000
            task: performReload 27245a25-b1b1-4879-9967-18e579585247,1000
            >> 90400000 04 000003E8
            << 10 00000000 01 1010000000346072 00000003 F04130754BF661A0 E886AA2A 9000
            >> 90420000 10 1010000030000317 00003278 404A3D16
            << 000003E8 26FC1BC1 9000
            >> 00B201241A
            << 02 2C 000003E8 00000003 000003E8 1010000030000317 00003278 9000
        */

        static readonly byte[] CARD_CREDIT = { 0x90, 0x42, 0x00, 0x00, 0x10 };

        private async Task<bool> performReload(string umtc, int amount)
        {
            System.Diagnostics.Debug.WriteLine("task: performReload "+umtc+","+amount);

            SnapperCardType sct = reader.currentCard as SnapperCardType;
            byte[] adf = transaction.purseInfo;

            byte []apdu=new byte[]{0x90,0x40,0,0,4,
                (byte)(amount>>24),
                (byte)(amount>>16),
                (byte)(amount>>8),
                (byte)amount};

            byte []init = await reader.TransmitAsync(apdu);

            /* ALG[1],BAL[4],ID_CENTER[1],ID_EP[8],NT_EP[4],R_EP[8],SIGN1[4] */

            string res = await interfaceAPDU.beginReloadAsync(umtc, encode(adf,0,adf.Length),encode(init, 0, init.Length));

            transaction.busyStep++;
            busyChanged();

            byte[] credit = decode(res);

            byte []resp = await reader.TransmitAsync(credit);

            if ((resp != null)&&(resp.Length>2))
            {
                transaction.total += amount;

                byte[] purse;

                if (BinaryTools.compareBytes(credit,0,CARD_CREDIT,0,CARD_CREDIT.Length))
                {
                    purse = new byte[0x1A];

                    purse[0] = 2; /* TRT */
                    purse[1] = 0x1A;  /* LEN */
                    System.Array.Copy(resp, 0, purse, 2, 4); /* BAL */
                    System.Array.Copy(init, 14, purse, 6, 4); /* NT */
                    System.Array.Copy(apdu, 5, purse, 10, 4); /* M */
                    System.Array.Copy(credit, 5, purse, 14, 12); /* SAM */

                    System.Diagnostics.Debug.WriteLine("EF_PURSE predicted to be " + Hex.bytesToHex(purse,0,purse.Length));
                }
                else
                {
                    purse=await reader.TransmitAsync(APDUdefs.CARD_READ_PURSE);
                }

                if ((purse != null) && (purse.Length > 2))
                {
                    sct.SetPurse(purse);
                }
            }

            bool b = await interfaceAPDU.completeReloadAsync(umtc, encode(resp, 0, resp.Length));

            transaction.busyStep++;
            busyChanged();

            return true;
        }

        public static String encode(byte[] a, int offset, int len)
        {
            return System.Convert.ToBase64String(Hex.bytesToASCII(a, offset, len));
        }

        public static byte[] decode(string res)
        {
            byte[] d = System.Convert.FromBase64String(res);
            return Hex.bytesFromASCII(d);
        }

        void busyChanged()
        {
            PageEventHandler peh = app.pageEventHandler;

            if (peh!=null)
            {
                peh.AppBusy(app.IsInProgress());
            }
        }
    }
}
