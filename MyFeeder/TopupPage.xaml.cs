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
 * $Id: TopupPage.xaml.cs 46 2022-06-07 23:21:59Z rhubarb-geek-nz $
 */

using nz.geek.rhubarb.utils;
using System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace MyFeeder
{
    class TopupPageEventHandler : PageEventHandler
    {
        TopupPage topupPage;

        internal TopupPageEventHandler(TopupPage p)
        {
            topupPage = p;
        }

        internal override bool CardFound(AbstractReader card)
        {
            return topupPage.CardFound(card);
        }

        internal override void CardLost(AbstractReader s)
        {
            topupPage.CardLost();
        }

        internal override void AppBusy(bool b)
        {
            topupPage.RefreshView(b);
        }
    }

    public sealed partial class TopupPage : Page
    {
        App app;
        TopupPageEventHandler eventHandler;
        TopupTransaction transaction;
        Boolean bRefreshDisabled = false;

        public TopupPage()
        {
            app = (App)Application.Current;
            this.InitializeComponent();
            topCommandBar.Visibility = Visibility.Visible;
            topBackButton.Visibility = ((app.platformCode == App.WINDOWS_IOT) ? Visibility.Visible : Visibility.Collapsed);

            eventHandler = new TopupPageEventHandler(this);

//            topPayButton.Visibility = Visibility.Collapsed;
//            topPayButton.Icon = new SymbolIcon((Symbol)0xE7BF);
        }

        public void helpButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(HelpPage));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            app.pageEventHandler = eventHandler;

            if (app.currentTransaction != null)
            {
                transaction = (TopupTransaction)app.currentTransaction;

                if (transaction != null)
                {
                    if (transaction.cvv != null)
                    {
                        cardCVV.Text = transaction.cvv;
                    }

                    if (transaction.amount == 0)
                    {
                        transaction.amount = Int32.Parse(amountCombo.SelectedValue.ToString());
                    }
                    else
                    {
                        amountCombo.SelectedValue = transaction.amount.ToString();
                    }

                    dispatchBeginTransaction();
                }
            }
        }

        async void dispatchBeginTransaction()
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    this.beginTransaction();
                }
            );
        }

        async void dispatchReaderPage()
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    this.Frame.Navigate(typeof(MainPage));
                }
            );
        }

        private void beginTransaction()
        {
            if (transaction.step == TopupTransaction.STEP_INIT)
            {
                transaction.step = TopupTransaction.STEP_PENDING;

                TopupTransactionTask task = new TopupTransactionTask(app, transaction);

                app.beginTask(task);
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (app.pageEventHandler == eventHandler)
            {
                app.pageEventHandler = eventHandler;
            }

            base.OnNavigatingFrom(e);
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }

        private void acceptButton_Click(object sender, RoutedEventArgs e)
        {
            topupButton_Click(sender, e);
        }

        private void payButton_Click(object sender, RoutedEventArgs e)
        {
            topupButton_Click(sender, e);
        }

        private void topupButton_Click(object sender, RoutedEventArgs e)
        {
            if ((transaction!=null)&&(transaction.step==TopupTransaction.STEP_ENTRY))
            {
                transaction.step = TopupTransaction.STEP_PAYMENT;
                transaction.busyStep = 0;
                transaction.busyMax = 4;

                TopupTransactionTask task = new TopupTransactionTask(app, transaction);

                app.beginTask(task);
            }
        }

        internal void RefreshView(bool busy)
        {
            if ((transaction!=null)&&!bRefreshDisabled)
            {
                Visibility visEntryGrid = Visibility.Visible;
                string textInformation = null;
                string textInstruction = null;
                Boolean enableEditButtons = false;
                string resultMessage = null;
                Visibility visInformation = Visibility.Collapsed;
                Visibility visInstruction = Visibility.Collapsed;
                Visibility visNFC = Visibility.Collapsed;
                bool ccEntryEnable = false;
                Visibility ccEntryVisibility = Visibility.Collapsed;

                switch (transaction.step)
                {
                    case TopupTransaction.STEP_INIT:
                        visEntryGrid = Visibility.Collapsed;
//                        visTopupButton = Visibility.Collapsed;
                        textInformation = app.resourceLoader.GetString("PreparingForCredit");
                        visInformation = Visibility.Visible;
                        busy = true;
                        break;
                    case TopupTransaction.STEP_PENDING:
                        visEntryGrid = Visibility.Collapsed;
                        //                        visTopupButton = Visibility.Collapsed;
                        textInformation = app.resourceLoader.GetString("PendingReloadPrompt");
                        visInformation = Visibility.Visible;
                        busy = true;
                        break;
                    case TopupTransaction.STEP_ENTRY:
                        textInstruction = app.resourceLoader.GetString("ProvidePaymentDetails");
                        visInstruction = Visibility.Visible;
                        enableEditButtons = true;
                        ccEntryVisibility = Visibility.Visible;

                        if (!transaction.bAutomaticEntryCC)
                        {
                            if (transaction.entryMode!=TopupTransaction.ENTRYMODE_MANUAL)
                            {
                                /* if not manual then suggest using NFC */

                                visNFC = Visibility.Visible;
                            }
                            else
                            {
                                ccEntryEnable = true;

                                transaction.creditCard = null;

                                try
                                {
                                    String pan = cardNumber.Text;
                                    String month = cardExpiryMonth.Text;
                                    String year = cardExpiryYear.Text;

                                    if ((pan.Length==16)
                                        && (month.Length>0)
                                        && (year.Length>0)
                                        && Utils.isAllDigits(pan)
                                        && Utils.isAllDigits(month)
                                        && Utils.isAllDigits(year))
                                    {
                                        int monthInt = Int32.Parse(month);
                                        int yearInt = Int32.Parse(year);
                                        if ((monthInt > 0)
                                        && (monthInt <= 12)
                                        && (yearInt > 0)
                                        && (yearInt < 100))
                                        {
                                            transaction.creditCard = new CreditCardState("*", pan, monthInt, yearInt);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine("exception " + ex.Message);
                                }
                            }
                        }
                        break;
                    case TopupTransaction.STEP_CARDWAIT:
                        textInstruction = app.resourceLoader.GetString("PresentSnapper") +Utils.cardNumberWithSpace(Hex.bytesToHex(transaction.purseInfo,8,8));
                        visInstruction = Visibility.Visible;
                        visNFC = Visibility.Visible;
                        if (transaction.creditCard==null)
                        {
                            visEntryGrid = Visibility.Collapsed;
                        }
                        break;
                    case TopupTransaction.STEP_PAYMENT:
                        textInstruction = app.resourceLoader.GetString("PerformingPayment");
                        visInstruction = Visibility.Visible;
                        break;
                    case TopupTransaction.STEP_TOPUP:
                        textInstruction = app.resourceLoader.GetString("PerformingCredit");
                        visInstruction = Visibility.Visible;
                        if (transaction.creditCard == null)
                        {
                            visEntryGrid = Visibility.Collapsed;
                        }
                        break;
                    case TopupTransaction.STEP_COMPLETE:
                        if (transaction.total > 0)
                        {
                            textInformation = app.resourceLoader.GetString("CreditComplete1") + Utils.toMoney(transaction.total) + app.resourceLoader.GetString("CreditComplete2");
                        }
                        else
                        {
                            textInformation = app.resourceLoader.GetString("CreditComplete");
                        }

                        visInformation = Visibility.Visible;
                        visEntryGrid = Visibility.Collapsed;
//                        visTopupButton = Visibility.Collapsed;
                        break;
                    case TopupTransaction.STEP_PENDING_FAILED:
                        textInformation = app.resourceLoader.GetString("AnErrorOccurredDuringPendingCredit");
                        visInformation = Visibility.Visible;
                        visEntryGrid = Visibility.Collapsed;
//                        visTopupButton = Visibility.Collapsed;
                        break;
                    case TopupTransaction.STEP_UNEXPECTED_FAILED:
                        textInformation = app.resourceLoader.GetString("AnUnexpectedErrorOccurredDuringCredit");
                        visInformation = Visibility.Visible;
                        visEntryGrid = Visibility.Collapsed;
//                        visTopupButton = Visibility.Collapsed;
                        break;
                    case TopupTransaction.STEP_TOPUP_FAILED:
                        textInstruction = app.resourceLoader.GetString("AnErrorOccurredDuringCredit");
                        visInstruction = Visibility.Visible;
                        break;
                    case TopupTransaction.STEP_PAYMENT_FAILED:
                        textInstruction = app.resourceLoader.GetString("AnErrorOccurredDuringPayment");
                        visInstruction = Visibility.Visible;
                        break;
                }

                if (busy)
                {
                    if (transaction.busyMax > 0)
                    {
                        busyProgress.Maximum = transaction.busyMax;
                        busyProgress.Value = transaction.busyStep;
                        busyProgress.IsIndeterminate = false;
                    }
                    else
                    {
                        busyProgress.IsIndeterminate = true;
                    }

                }

                busyProgress.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
                nfcIcon.Visibility = busy ? Visibility.Collapsed : visNFC;

//                busyProgress.IsActive = busy;

                resultMessage = transaction.resultMessage;
                transaction.resultMessage = null;

                if (resultMessage!=null)
                {
                    transaction.dialogBoxMessage = resultMessage;
                    transaction.dialogBoxVisible = true;
                }

                if (transaction.dialogBoxVisible)
                {
                    ShowMessageBox(transaction.dialogBoxMessage);

                    enableEditButtons = false;
                }

                entryGrid.Visibility = visEntryGrid;
                informationText.Visibility = visInformation;
                instructionText.Visibility = visInstruction;

//                visTopupButton = Visibility.Collapsed;

//                topupButton.Visibility = visTopupButton;

                bool txValid = transaction.Validate();

                //                topupButton.IsEnabled = txValid;

                topAcceptButton.IsEnabled = txValid;

                amountCombo.IsEnabled = enableEditButtons;
                cardCVV.IsEnabled = enableEditButtons;

                if (textInstruction!=null)
                {
                    instructionText.Text = textInstruction;
                }

                if (textInformation != null)
                {
                    informationText.Text = textInformation;
                }

                if (transaction.bAutomaticEntryCC)
                {
                    CreditCardState cc = transaction.creditCard;

                    cardNumber.Text = Utils.cardNumberWithSpace(Utils.pciObscure(cc.pan));
                    cardExpiryMonth.Text = cc.month.ToString("D2");
                    cardExpiryYear.Text = cc.year.ToString("D2");
                    ccEntryEnable = false;
                }

                cardExpiryMonth.IsEnabled = ccEntryEnable;
                cardExpiryYear.IsEnabled = ccEntryEnable;
                cardNumber.IsEnabled = ccEntryEnable;

                UIElement []list = new UIElement[] {
                    cardExpiryMonth,
                    cardExpiryYear,
                    cardExpiryBlock,
                    cardCVV,
                    cardCVVBlock,
                    cardNumber,
                    cardNumberBlock
                };

                foreach (UIElement e in list)
                {
                    e.Visibility = ccEntryVisibility;
                }

                if (transaction.bFocusWanted && 
                    (transaction.step==TopupTransaction.STEP_ENTRY)
                    )
                {
                    transaction.bFocusWanted = false;

                    switch (transaction.entryMode)
                    {
                        case TopupTransaction.ENTRYMODE_EMV:
                            if (enableEditButtons)
                            {
                                cardCVV.Focus(FocusState.Programmatic);
                            }
                            break;
                        case TopupTransaction.ENTRYMODE_MANUAL:
                            if (ccEntryEnable)
                            {
                                cardNumber.Focus(FocusState.Programmatic);
                            }
                            break;
                    }
                }
            }
        }

        private void cardNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (transaction.bAutomaticEntryCC)
            {

            }
            else
            {
                RefreshView(app.IsInProgress());
            }
        }

        private void cardExpiryMonth_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (transaction.bAutomaticEntryCC)
            {

            }
            else
            {
                RefreshView(app.IsInProgress());
            }
        }

        private void cardExpiryYear_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (transaction.bAutomaticEntryCC)
            {

            }
            else
            {
                RefreshView(app.IsInProgress());
            }
        }

        private void cardCVV_TextChanged(object sender, TextChangedEventArgs e)
        {
            String s = cardCVV.Text;
            bool wasThree = Utils.isValidCVV(transaction.cvv);
            transaction.cvv = s;
            bool isThree = Utils.isValidCVV(transaction.cvv);

//            System.Diagnostics.Debug.WriteLine("CVV changed to " + s);

            if (isThree!=wasThree)
            {
                RefreshView(app.IsInProgress());
            }
        }

        internal bool CardFound(AbstractReader card)
        {
            bool result = false;
            CardType ct = card.currentCard;

            if (ct != null)
            {
                SnapperCardType sct = ct as SnapperCardType;

                if (sct !=null)
                {
                    byte[] pi = sct.GetPurseInfo();
                    if (pi.Length==transaction.purseInfo.Length)
                    {
                        if (BinaryTools.compareBytes(pi,0,transaction.purseInfo,0,pi.Length))
                        {
                            if (transaction.step==TopupTransaction.STEP_CARDWAIT)
                            {
                                if (transaction.reloadDetails!=null)
                                {
                                    transaction.busyStep = 0;
                                    transaction.busyMax = 0;

                                    foreach (PaymentService.ReloadDetail detail in transaction.reloadDetails)
                                    {
                                        if (detail!=null)
                                        {
                                            transaction.busyMax+=2;
                                        }
                                    }
                                }

                                TopupTransactionTask task = new TopupTransactionTask(app, transaction);

                                app.beginTask(task);

                                result = true;
                            }
                        }
                    }
                }

                EMVCardType ect = ct as EMVCardType;

                if (ect!=null)
                {
                    if (transaction.step==TopupTransaction.STEP_ENTRY)
                    {
                        transaction.creditCard = ect.cardState;
                        transaction.bAutomaticEntryCC = true;

                        /* have read an EMV card, switch down to EMV mode to prevent MS-PAY */

                        transaction.entryMode = TopupTransaction.ENTRYMODE_EMV;
                        transaction.bFocusWanted = true;
                    }
                }
            }

            RefreshView(app.IsInProgress());

            return result;
        }

        internal void CardLost()
        {
            RefreshView(app.IsInProgress());
        }

        private void amountCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((transaction!=null)&&(amountCombo!=null))
            {
                transaction.amount = Int32.Parse(amountCombo.SelectedValue.ToString());
            }
        }

        private void dismissDialog_Click(object sender, RoutedEventArgs e)
        {
            transaction.dialogBoxVisible = false;

            switch (transaction.step)
            {
                case TopupTransaction.STEP_TOPUP_FAILED:
                    transaction.step = TopupTransaction.STEP_INIT;
                    transaction.total = 0;

                    RefreshView(true);

                    dispatchBeginTransaction();
                    break;
                case TopupTransaction.STEP_PAYMENT_FAILED:
                    transaction.step = TopupTransaction.STEP_ENTRY;
                    transaction.total = 0;
                    RefreshView(false);
                    break;
                default:
                    this.Frame.GoBack();
                    break;
            }
        }

        MessageDialog msgDialog = null;

        async void ShowMessageBox(String message)
        {
            if (msgDialog==null)
            {
                try
                {
                    msgDialog = new MessageDialog(message);

                    await msgDialog.ShowAsync();
                }
                finally
                {
                    msgDialog = null;
                }

                if (transaction.step==TopupTransaction.STEP_PAYMENT_FAILED)
                {
                    transaction.dialogBoxMessage = null;
                    transaction.dialogBoxVisible = false;
                    transaction.step = TopupTransaction.STEP_ENTRY;
                    transaction.busyMax = 0;
                    RefreshView(false);
                }
                else
                {
                    this.Frame.GoBack();
                }
            }
            else
            {
                msgDialog.Content = message;
            }
        }

        private void Page_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Page_RightTapped");

            changeToManual();
        }

        void changeToManual()
        {
            if ((transaction.entryMode!=TopupTransaction.ENTRYMODE_MANUAL)
                &&
                (transaction.step==TopupTransaction.STEP_ENTRY))
            {
                Boolean bOld = bRefreshDisabled;
                bRefreshDisabled = true;

                transaction.entryMode = TopupTransaction.ENTRYMODE_MANUAL;
                transaction.bAutomaticEntryCC = false;
                transaction.creditCard = null;
                transaction.cvv = null;

                cardCVV.Text = "";
                cardNumber.Text = "";
                cardExpiryYear.Text = "";
                cardExpiryMonth.Text = "";

                bRefreshDisabled = bOld;

                transaction.bFocusWanted = true;

                RefreshView(app.IsInProgress());
            }
        }
    }
}
