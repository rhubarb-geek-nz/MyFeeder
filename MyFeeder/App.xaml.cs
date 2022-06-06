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
 * $Id: App.xaml.cs 45 2022-06-06 12:15:22Z rhubarb-geek-nz $
 */

using System;
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Core;
using nz.geek.rhubarb.utils;
using Windows.Foundation.Metadata;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace MyFeeder
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        public static readonly bool doTraceSuspend = false, doTraceData = false;
        public readonly bool isPaymentApiSupported = ApiInformation.IsTypePresent("Windows.ApplicationModel.Payments.PaymentMethodData");
        internal List<USBHandler> usbReaders = new List<USBHandler>();
        public PageEventHandler pageEventHandler = null;
        internal CardTransaction currentTransaction = null;
        internal readonly List<AbstractReader> readers = new List<AbstractReader>();
        internal readonly Windows.ApplicationModel.Resources.ResourceLoader resourceLoader;
        internal readonly List<AppTask> runningTasks = new List<AppTask>();
        internal CoreDispatcher dispatcher;
        internal AbstractReader lastCardFound,lastMainReader;
        internal USBDeviceWatcher usbWatcher;
        internal SmartCardWatcher smartCardWatcher;
        internal readonly int platformCode;
        internal const int WINDOWS_DESKTOP = 1;
        internal const int WINDOWS_MOBILE = 2;
        internal const int WINDOWS_IOT = 3;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.Resuming += OnResuming;
            this.resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();

            string platformFamily = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily;

            System.Diagnostics.Debug.WriteLine("DeviceFamily=" + platformFamily);

            /*
                DeviceFamily=Windows.Mobile
                DeviceFamily=Windows.Desktop
                DeviceFamily=Windows.IoT
            */

            if ("Windows.Desktop".Equals(platformFamily))
            {
                platformCode = WINDOWS_DESKTOP;
            }
            else if ("Windows.Mobile".Equals(platformFamily))
            {
                platformCode = WINDOWS_MOBILE;
            }
            else if ("Windows.IoT".Equals(platformFamily))
            {
                platformCode = WINDOWS_IOT;
            }
            else
            {
                platformCode = 0;
            }
        }

        internal bool CardFound(AbstractReader reader)
        {
            lastCardFound = reader;

            if (pageEventHandler!=null)
            {
                return pageEventHandler.CardFound(reader);
            }

            return false;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            this.dispatcher = Window.Current.Dispatcher;
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                // Set the default language PORTING
		// rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                rootFrame.NavigationFailed += OnNavigationFailed;
                rootFrame.Navigated += OnNavigated;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;

                // Register a handler for BackRequested events and set the
                // visibility of the Back button
                SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;

                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    rootFrame.CanGoBack ?
                    AppViewBackButtonVisibility.Visible :
                    AppViewBackButtonVisibility.Collapsed;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();

            onLaunchAsync();
        }

        private void onLaunchAsync()
        {
            usbWatcher = new USBDeviceWatcher(this);
            usbWatcher.start();
            smartCardWatcher = new SmartCardWatcher(this);
            smartCardWatcher.start();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
//            pageEventHandler = null;

            if (doTraceSuspend)
            {
                System.Diagnostics.Debug.WriteLine("App Suspension Initiated");
            }

            beginSuspend(e.SuspendingOperation.GetDeferral());
        }

        private async void beginSuspend(SuspendingDeferral deferral)
        {
            Boolean noError = false;

            try
            {
                SmartCardWatcher w2 = smartCardWatcher;
                smartCardWatcher = null;

                if (w2!=null)
                {
                    await w2.stop();
                }

                USBDeviceWatcher w = usbWatcher;
                usbWatcher = null;

                if (w!=null)
                {
                    await w.stop();
                }

                List<string> oldReaders = new List<string>();

                foreach (USBHandler handler in usbReaders)
                {
                    oldReaders.Add(handler.deviceId);

                    await handler.onSuspending();
                }

                foreach (string xid in oldReaders)
                {
                    AbstractReader rdr = getReaderFromDeviceId(xid);

                    if (rdr!=null)
                    {
                        readers.Remove(rdr);
                        rdr.Dispose();
                    }
                }

                usbReaders.Clear();

                while (readers.Count > 0)
                {
                    AbstractReader rdr = readers[0];
                    readers.Remove(rdr);
                    rdr.Dispose();
                }

                noError = true;
            }
            finally
            {
                if (doTraceSuspend)
                {
                    System.Diagnostics.Debug.WriteLine("App Suspension Complete noErrors="+noError);
                }

                deferral.Complete();
            }
        }

        private void OnResuming(object sender, object e)
        {
            if (doTraceSuspend)
            {
                System.Diagnostics.Debug.WriteLine("OnResuming....");
            }

            if (smartCardWatcher == null)
            {
                smartCardWatcher = new SmartCardWatcher(this);
                smartCardWatcher.start();
            }

            if (usbWatcher == null)
            {
                usbWatcher = new USBDeviceWatcher(this);
                usbWatcher.start();
            }
        }

        internal bool IsInProgress()
        {
            return (runningTasks.Count > 0);
        }

        internal AbstractReader findReaderForSnapperCard(byte [] c)
        {
            AbstractReader result = null;

            foreach (AbstractReader r in readers)
            {
                if (r.isCardPresent)
                {
                    CardType card = r.currentCard;

                    if (card!=null)
                    {
                        SnapperCardType s = card as SnapperCardType;

                        if (s!=null)
                        {
                            byte[] p = s.GetPurseInfo();

                            if (p.Length==c.Length)
                            {
                                if (BinaryTools.compareBytes(p,0,c,0,p.Length))
                                {
                                    result = r;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.Device)
            {
                // Load the UI
                if (Window.Current.Content == null)
                {
                    Frame rootFrame = new Frame();
                    rootFrame.Navigate(typeof(MainPage));

                    // Place the frame in the current Window
                    Window.Current.Content = rootFrame;
                }

                // Ensure the current window is active or else the app will freeze at the splash screen
                Window.Current.Activate();

                // Launched from Autoplay for device, notify the app what device launched this app
                DeviceActivatedEventArgs deviceArgs = (DeviceActivatedEventArgs)args;

                if (deviceArgs != null)
                {
                    // The DeviceInformationId is the same id found in a DeviceInformation object, so it can potentially be used
                    // with UsbDevice.FromIdAsync()
                    // The deviceArgs->Verb is the verb that is provided in the appxmanifest for this specific device
                    /*                   MainPage.Current.NotifyUser(
                                           "The app was launched by device id: " + deviceArgs.DeviceInformationId
                                           + "\nVerb: " + deviceArgs.Verb,
                                           NotifyType.StatusMessage);
                     * *
                     */

                    string xid = deviceArgs.DeviceInformationId;

                    System.Diagnostics.Debug.WriteLine("OnActivated: Connect " + xid);

                    beginTask(new AppTaskConnectFeeder(this, xid));
                }
            }
        }

        internal void readerFound(USBHandler handler)
        {
            if (!usbReaders.Contains(handler))
            {
                usbReaders.Add(handler);

                ConcreteNXPReader nxp = new ConcreteNXPReader(this,handler.connection.deviceId, handler);

                nxp.DisplayName = handler.connection.product;

                handler.reader = nxp;

                readers.Add(nxp);

                if (pageEventHandler!=null)
                {
                    pageEventHandler.AppBusy(IsInProgress());
                }
            }
        }

        internal void readerLost(USBHandler handler)
        {
            AbstractReader abs = getReaderFromDeviceId(handler.deviceId);

            usbReaders.Remove(handler);

            if (abs!=null)
            {
                readers.Remove(abs);
                abs.Dispose();
            }

            if (pageEventHandler != null)
            {
                pageEventHandler.AppBusy(IsInProgress());
            }
        }

        private void OnNavigated(object sender, NavigationEventArgs e)
        {
            // Each time a navigation event occurs, update the Back button's visibility
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                ((Frame)sender).CanGoBack ?
                AppViewBackButtonVisibility.Visible :
                AppViewBackButtonVisibility.Collapsed;
        }

        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame.CanGoBack)
            {
                e.Handled = true;
                rootFrame.GoBack();
            }
        }

        internal AbstractReader getReaderFromDeviceId(string dis)
        {
            foreach (AbstractReader r in readers)
            {
                if (dis.Equals(r.DeviceId))
                {
                    return r;
                }
            }

            return null;
        }

        internal void beginTask(AppTask task)
        {
            int count = runningTasks.Count;

            runningTasks.Add(task);

            if ((count==0)&&(pageEventHandler!=null))
            {
                pageEventHandler.AppBusy(true);
            }

            task.beginTask();
        }

        internal void completedTask(AppTask task)
        {
            runningTasks.Remove(task);

            int count = runningTasks.Count;

            if ((count == 0) && (pageEventHandler != null))
            {
                pageEventHandler.AppBusy(false);
                lastCardFound = null;
            }
        }
    }
}
