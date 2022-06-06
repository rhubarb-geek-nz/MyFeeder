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
 * $Id: AppTaskReadCardState.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System;

namespace MyFeeder
{
    internal class AppTaskReadCardState : AppTask
    {
        readonly AbstractReader reader;
        readonly bool newCard;
        static readonly bool enableEMV = true;

        internal AppTaskReadCardState(App a,AbstractReader r,bool n) : base(a)
        {
            reader = r;
            newCard = n;
        }

        internal async override void beginTask()
        {
            bool wasFound = false,keep=false;

            try
            {
                try
                {
                    try
                    {
                        try
                        {
                            if (await reader.beginTransaction())
                            {
                                byte[] adf = null;
                                CardType ct = null;

                                if (enableEMV && !reader.isUICC)
                                {
                                    if (reader.isNFC)
                                    {
                                        /* if already know it is NFC then treat as T=CL and attach LE */

                                        adf = await reader.TransmitAsync(APDUdefs.SELECT_PPSE_LE);
                                    }
                                    else
                                    {
                                        /* treat as T=0 and don't include LE */
                                        adf = await reader.TransmitAsync(APDUdefs.SELECT_PPSE);
                                    }

                                    if ((adf != null) && (adf.Length == 2) && ((adf[0] == 0x6C) || (adf[0] == 0x67)))
                                    {
                                        /* telling us to re-ask but with a specific length */
                                        byte[] apdu = new byte[APDUdefs.SELECT_PPSE.Length + 1];
                                        Buffer.BlockCopy(APDUdefs.SELECT_PPSE, 0, apdu, 0, APDUdefs.SELECT_PPSE.Length);
                                        apdu[APDUdefs.SELECT_PPSE.Length] = adf[1];

                                        adf = await reader.TransmitAsync(apdu);
                                    }

                                    if ((adf != null) && (adf.Length == 2) && (!reader.isNFC))
                                    {
                                        adf = await reader.TransmitAsync(APDUdefs.SELECT_PSE);
                                    }

                                    if ((adf != null) && (adf.Length > 2))
                                    {
                                        ct = new EMVCardType(adf);
                                    }
                                }

                                if (ct == null)
                                {
                                    adf = await reader.TransmitAsync(APDUdefs.SELECT_SNAPPER);

                                    if ((adf != null) && (adf.Length > 2))
                                    {
                                        ct = new SnapperCardType(adf);
                                    }
                                }

                                if (ct==null)
                                {
                                    adf = await reader.TransmitAsync(APDUdefs.SELECT_CALYPSO);

                                    if ((adf!=null) && (adf.Length>1) && (adf[adf.Length-2]==(byte)0x90))
                                    {
                                        ct = new CalypsoCardType(adf);
                                    }
                                }

                                if (ct != null)
                                {
                                    if (await ct.CardFound(reader))
                                    {
                                        reader.currentCard = ct;
                                        wasFound = true;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("AppTaskReadCardState " + ex);
                        }
                        finally
                        {
                            if (newCard && wasFound)
                            {
                                keep=app.CardFound(reader);
                            }
                        }
                    }
                    finally
                    {
                        reader.endTransaction(keep);
                    }
                }
                finally
                {
                    finished();
                }
            }
            catch
            {

            }
        }
    }
}
