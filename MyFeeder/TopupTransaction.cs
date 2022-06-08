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
 * $Id: TopupTransaction.cs 46 2022-06-07 23:21:59Z rhubarb-geek-nz $
 */

namespace MyFeeder
{
    internal class TopupTransaction: CardTransaction
    {
        public const int
            STEP_INIT = 0,                        /* initialised */
            STEP_PENDING = 1,                     /* doing the initial pending check */
            STEP_ENTRY = 2,                       /* waiting for data entry */
            STEP_PAYMENT = 3,                     /* performing payment */
            STEP_CARDWAIT = 4,                    /* waiting for correct card */
            STEP_TOPUP = 5,                       /* performing topup */
            STEP_PAYMENT_FAILED = 96,             /* payment failed */
            STEP_UNEXPECTED_FAILED = 97,          /* unexpected failure */
            STEP_PENDING_FAILED = 98,             /* pending failed with error */
            STEP_TOPUP_FAILED = 99,               /* topup failed */
            STEP_COMPLETE = 100;                  /* transaction complete */

        public const int
            ENTRYMODE_MANUAL = 0,                 /* all fields are editable */
            ENTRYMODE_EMV = 1;                    /* read EMV card */

        internal int entryMode = ENTRYMODE_MANUAL;
        internal CreditCardState creditCard;
        internal int step = STEP_INIT;
        internal string cvv;
        internal int amount,total;
        internal byte[] purseInfo;
        internal string resultMessage;
        internal int busyMax = 0, busyStep = 0;
        internal string dialogBoxMessage;
        internal bool dialogBoxVisible=false;
        internal PaymentService.ReloadDetail[] reloadDetails;
        internal bool bAutomaticEntryCC,bFocusWanted;

        internal TopupTransaction(App a, SnapperCardType s): base(a)
        {
            purseInfo = s.GetPurseInfo();
            entryMode = ENTRYMODE_EMV;
        }

        internal bool Validate()
        {
            return
                (step == STEP_ENTRY) &&
                (!dialogBoxVisible) &&
                (purseInfo != null) &&
                (
                    Utils.isValidCVV(cvv) &&
                    (creditCard != null)
                );
        }
    }
}
