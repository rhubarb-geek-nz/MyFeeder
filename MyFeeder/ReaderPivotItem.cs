/**************************************************************************
 *
 *  Copyright 2015, Roger Brown
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
 * $Id: ReaderPivotItem.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using Windows.UI.Xaml.Controls;

namespace MyFeeder
{
    public class ReaderPivotItem
    {
        static readonly string emptyString = string.Empty;

        public string Title { get; set; }
        public string Type { get; set;  }
        public string IssuerName { get; set; }
        public string CardPAN { get; set; }
        public string ExpiryValue { get; set; }
        public string DeviceId { get; set; }
        public string Balance { get; set; }

        internal PivotItem item;
        internal ReaderPivotControl readerPivotControl;
        internal bool isSnapper = false,isEmpty=true,isNFC=false;

        internal void setState(App app,CardType currentCard,bool bBusy,bool nfc)
        {
            isNFC = nfc;

            if (currentCard==null)
            {
                IssuerName = emptyString;
                CardPAN = emptyString;
                ExpiryValue = emptyString;
                Balance = emptyString;
                isSnapper = false;
                isEmpty = true;
            }
            else
            {
                isEmpty = false;
                IssuerName = currentCard.GetIssuerName();
                string pan = currentCard.GetPAN();
                SnapperCardType snapper = currentCard as SnapperCardType;
               
                if (snapper!=null)
                {
                    isSnapper = true;
                    ExpiryValue = currentCard.GetExpiry();

                    switch (snapper.GetIDCenter())
                    {
                        case 0:
                            Balance = emptyString;
                            break;
                        case 1:
                        case 2:
                            if (snapper.isPrePaid())
                            {
                                Balance = "$" + Utils.toMoney(snapper.GetBalance());
                            }
                            else
                            {
                                Balance = emptyString;
                            }
                            IssuerName = app.resourceLoader.GetString("IssuerSnapper");
                            break;
                        default:
                            if (snapper.isPrePaid())
                            {
                                Balance = "\u20A9" + snapper.GetBalance();
                            }
                            else
                            {
                                Balance = emptyString;
                            }
                            break;
                    }

                    CardPAN = Utils.cardNumberWithSpace(pan);
                }
                else
                {
                    CalypsoCardType ct = currentCard as CalypsoCardType;

                    isSnapper = false;
                    ExpiryValue = currentCard.GetExpiry();
                    Balance = emptyString;

                    if (ct==null)
                    {
                        pan = Utils.pciObscure(pan);

                        CardPAN = Utils.cardNumberWithSpace(pan);
                    }
                    else
                    {
                        if (ct.hasBalance())
                        {
                            Balance="\u20AC" + Utils.toMoney(ct.GetBalance());
                        }

                        CardPAN = pan;
                    }
                }

            }

            if (readerPivotControl != null)
            {
                readerPivotControl.setReaderState(this,bBusy);
            }
        }
    }
}
