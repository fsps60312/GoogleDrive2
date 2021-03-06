﻿using System;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.Threading;

using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace GoogleDrive2.RestRequests
{
    partial class RestRequestsAuthorizer
    {
        partial class DriveAuthorizer
        {
            public static async Task OpenWebWindowToGetAuthorizationCode(string title, string uri, SemaphoreSlim semaphoreSlim, Action<string> eventAction)
            {
                await MyLogger.SemaphoreDialogBox.WaitAsync();
                try
                {
                    CoreApplicationView newView = CoreApplication.CreateNewView();
                    int newViewId = 0;
                    var originViewId = ApplicationView.GetForCurrentView().Id;
                    bool windowClosed = false;
                    await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        Frame frame = new Frame();
                        frame.Navigate(typeof(UWP.MyControls.WebViewWindow), null);
                        Window.Current.Content = frame;
                    // You have to activate the window in order to show it later.
                    Window.Current.Activate();

                        var currentView = ApplicationView.GetForCurrentView();
                        newViewId = currentView.Id;
                        currentView.Title = title;
                        currentView.Consolidated += delegate { windowClosed = true; semaphoreSlim.Release(); };
                    });
                    if (!await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId))
                    {
                        MyLogger.LogError("Failed to show window!");
                        return;
                    }
                    var eventHandler = new Windows.Foundation.TypedEventHandler<WebView, WebViewNavigationStartingEventArgs>((sender, e) =>
                    {
                        eventAction(e.Uri.AbsoluteUri);
                    });
                    await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        UWP.MyControls.WebViewWindow.Instance.webView.NavigationStarting += eventHandler;
                        UWP.MyControls.WebViewWindow.Instance.webView.Source = new Uri(uri);
                    });
                    await semaphoreSlim.WaitAsync();
                    await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        UWP.MyControls.WebViewWindow.Instance.webView.NavigationStarting -= eventHandler;
                    });
                    if (!windowClosed) await ApplicationViewSwitcher.SwitchAsync(originViewId, newViewId, ApplicationViewSwitchingOptions.ConsolidateViews);
                }
                finally { MyLogger.SemaphoreDialogBox.Release(); }
            }
        }
    }
}
