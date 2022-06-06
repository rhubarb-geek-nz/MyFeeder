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
 * $Id: ReaderPivotControl.xaml.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace MyFeeder
{
    public sealed partial class ReaderPivotControl : UserControl
    {
        public ReaderPivotControl()
        {
            this.InitializeComponent();
        }

        internal void setReaderState(ReaderPivotItem item,bool busy)
        {
            Visibility vis = item.isEmpty ? Visibility.Collapsed : Visibility.Visible;
            Visibility visBalance= (item.Balance.Length > 0) ? vis : Visibility.Collapsed;
            Visibility visExpiry= (item.ExpiryValue.Length>0) ? vis : Visibility.Collapsed;

            ReaderBalanceValue.Text = item.Balance;
            ReaderIssuerValue.Text = item.IssuerName;
            ReaderExpiryValue.Text = item.ExpiryValue;
            ReaderPANValue.Text = item.CardPAN;

            ReaderIssuerName.Visibility = vis;
            ReaderIssuerValue.Visibility = vis;

            ReaderPANName.Visibility = vis;
            ReaderPANValue.Visibility = vis;

            ReaderExpiryValue.Visibility = visExpiry;
            ReaderExpiryName.Visibility = visExpiry;

            ReaderBalanceValue.Visibility = visBalance;
            ReaderBalanceName.Visibility = visBalance;

            nfcIcon.Visibility = (item.isEmpty && item.isNFC && !busy) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
