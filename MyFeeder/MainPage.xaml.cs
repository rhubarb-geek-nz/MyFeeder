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
 * $Id: MainPage.xaml.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System.Collections.Generic;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MyFeeder
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        readonly App app;
        public List<ReaderPivotItem> readerItems { get; }  = new List<ReaderPivotItem>();

        public MainPage()
        {
            app = (App)Application.Current;

            this.InitializeComponent();

            topCommandBar.Visibility = Visibility.Visible;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            app.pageEventHandler = new MainPageHandler(this);

            this.Frame.BackStack.Clear();
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;

            refreshView(app.IsInProgress());
        }
        
        private class MainPageHandler : PageEventHandler
        {
            private MainPage mainPage;

            public MainPageHandler(MainPage m)
            {
                mainPage = m;
            }

            internal override void AppBusy(bool b)
            {
                mainPage.refreshView(b);
            }

            internal override bool CardFound(AbstractReader card)
            {
                return false;
            }

            internal override void CardLost(AbstractReader s)
            {
            }
        }

        private void refreshView(bool bBusy)
        {
            foreach (AbstractReader ar in app.readers)
            {
                string id = ar.DeviceId;

                foreach (ReaderPivotItem item in readerItems)
                {
                    if (id.Equals(item.DeviceId))
                    {
                        item.setState(app,ar.currentCard,bBusy,ar.isNFC);

                        id = null;
                        break;
                    }
                }

                if (id!=null)
                {
                    ReaderPivotItem pi = new ReaderPivotItem();
                    pi.DeviceId = id;
                    pi.Title = ar.DisplayName;
                    pi.setState(app,ar.currentCard,bBusy,ar.isNFC);

                    readerItems.Add(pi);

                    readerPivot.Items.Add(createPivotItem(pi,bBusy));
                }
            }

            List<ReaderPivotItem> toBeRemoved = new List<ReaderPivotItem>();

            foreach (ReaderPivotItem item in readerItems)
            {
                if (null==app.getReaderFromDeviceId(item.DeviceId))
                {
                    readerPivot.Items.Remove(item.item);
                    toBeRemoved.Add(item);
                }
            }

            foreach (ReaderPivotItem item in toBeRemoved)
            {
                readerItems.Remove(item);
            }

            BusyProgress.IsIndeterminate= bBusy;
            BusyProgress.Visibility =(bBusy ? Visibility.Visible : Visibility.Collapsed);

            AbstractReader lastCard = app.lastCardFound;
            app.lastCardFound = null;

            if (lastCard==null)
            {
                lastCard = app.lastMainReader;
            }

            app.lastMainReader = null;

            if (lastCard != null)
            {
                foreach (ReaderPivotItem item in readerItems)
                {
                    if (lastCard.DeviceId.Equals(item.DeviceId))
                    {
                        readerPivot.SelectedItem = item.item;
                        break;
                    }
                }
            }

            RefreshMenuBar(bBusy);
        }

        ReaderPivotItem getCurrentPivotItem()
        {
            PivotItem cur = readerPivot.SelectedItem as PivotItem;
            ReaderPivotItem result = null;

            if (cur!=null)
            {
                foreach (ReaderPivotItem item in readerItems)
                {
                    if (item.item == cur)
                    {
                        result = item;
                        break;
                    }
                }
            }
            else
            {
                if (readerItems.Count>0)
                {
                    result = readerItems[0];
                }
            }

            return result;
        }

        AbstractReader getReader(ReaderPivotItem item)
        {
            if (item != null)
            {
                return app.getReaderFromDeviceId(item.DeviceId);
            }

            return null;
        }

        PivotItem createPivotItem(ReaderPivotItem item,bool busy)
        {
            PivotItem it = new PivotItem();

            item.item = it;

            it.Header = item.Title;
            item.readerPivotControl = new ReaderPivotControl();
            it.Content = item.readerPivotControl;
            item.readerPivotControl.setReaderState(item,busy);

            return it;
        }

        private void AppBarButton_Refresh_Click(object sender, RoutedEventArgs e)
        {
            AbstractReader abs = this.getReader(getCurrentPivotItem());

            if (abs != null)
            {
                app.beginTask(new AppTaskReadCardState(app, abs, false));
            }
        }
        private void AppBarButton_Help_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(HelpPage));
        }

        private void AppBarButton_Add_Click(object sender, RoutedEventArgs e)
        {
            AbstractReader abs = this.getReader(getCurrentPivotItem());

            if (abs!=null)
            {
                SnapperCardType sct = abs.currentCard as SnapperCardType;

                if (sct != null)
                {
                    TopupTransaction txn = new TopupTransaction(app, sct);

                    app.currentTransaction = txn;

                    this.Frame.Navigate(typeof(TopupPage));
                }
            }
        }

        private void readerPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshMenuBar(app.IsInProgress());
        }

        void RefreshMenuBar(bool bBusy)
        {
            bool bTopup = false,bDownload=false,bRefresh=false;
            Visibility vTopup = Visibility.Collapsed;
            Visibility vRefresh = Visibility.Collapsed;
            Visibility vUSB = Visibility.Collapsed;

            if ((app.readers.Count==0)&&(!bBusy))
            {
                vUSB = Visibility.Visible;
            }

            {
                AbstractReader abs = getReader(getCurrentPivotItem());

                if (abs != null)
                {
                    if (abs.currentCard==null)
                    {
                        if (abs.isUICC)
                        {
                            vRefresh = Visibility.Visible;
                            bRefresh = !bBusy;
                        }
                    }
                    else
                    {
                        SnapperCardType snapper = abs.currentCard as SnapperCardType;

                        vRefresh = Visibility.Visible;

                        if (snapper != null)
                        {
                            switch (snapper.GetIDCenter())
                            {
                                case 0:
                                    bDownload = (abs.isCardPresent && !bBusy);
                                    break;
                                case 1:
                                case 2:
                                    if (snapper.isPrePaid())
                                    {
                                        vTopup = Visibility.Visible;
                                        bTopup = !bBusy;
                                    }
                                    break;
                            }
                        }

                        if ((abs.isCardPresent || abs.isUICC || abs.isT0) && (!abs.isNFC))
                        {
                            bRefresh = !bBusy;
                        }
                    }
                }
            }

            topAppBarRefresh.IsEnabled = bRefresh;

            topAppBarRefresh.Visibility = vRefresh;

            topAppBarAdd.IsEnabled = bTopup;

            topAppBarAdd.Visibility = vTopup;

            usbIcon.Visibility = vUSB;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            ReaderPivotItem rpi = getCurrentPivotItem();

            if (rpi!=null)
            {
                app.lastMainReader = getReader(rpi);
            }

            base.OnNavigatingFrom(e);
        }
    }
}
