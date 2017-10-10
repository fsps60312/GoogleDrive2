using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace GoogleDrive2
{
    partial class MyLogger
    {
        static Libraries.MySemaphore SemaphoreDialogBox = new Libraries.MySemaphore(1);
        public static async Task<bool> Ask(string msg, string positiveText = "Yes", string negativeText = "No")
        {
            await SemaphoreDialogBox.WaitAsync();
            try
            {
                CoreApplicationView newView = CoreApplication.CreateNewView();
                int newViewId = 0;
                var originViewId = ApplicationView.GetForCurrentView().Id;
                await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Frame frame = new Frame();
                    frame.Navigate(typeof(UWP.MyControls.AlertDialog), new List<string> { positiveText, negativeText });
                    Window.Current.Content = frame;
                    // You have to activate the window in order to show it later.
                    Window.Current.Activate();

                    newViewId = ApplicationView.GetForCurrentView().Id;
                    //ApplicationView.GetForCurrentView().Title = title;
                });
                if (!await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId))
                {
                    GoogleDrive2.MyLogger.LogError("Failed to show window!");
                    return false;
                }
                var semaphoreSlim = new Libraries.MySemaphore(0);
                bool? ans = null;
                var eventHandler = new Libraries.Events.MyEventHandler<string>((response) =>
                {
                    if (response == positiveText) ans = true;
                    else if (response == negativeText) ans = false;
                    semaphoreSlim.Release();
                });
                await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UWP.MyControls.AlertDialog.Instance.OKClicked += eventHandler;
                    UWP.MyControls.AlertDialog.Instance.TXBmain.Text = msg;
                });
                await semaphoreSlim.WaitAsync();
                await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UWP.MyControls.AlertDialog.Instance.OKClicked -= eventHandler;
                });
                await ApplicationViewSwitcher.SwitchAsync(originViewId, newViewId, ApplicationViewSwitchingOptions.ConsolidateViews);
                MyLogger.Assert(ans.HasValue);
                return ans.Value;
            }
            finally { SemaphoreDialogBox.Release(); }
        }
        public static async Task Alert(string msg)
        {
            await SemaphoreDialogBox.WaitAsync();
            try
            {
                CoreApplicationView newView = CoreApplication.CreateNewView();
                int newViewId = 0;
                var originViewId = ApplicationView.GetForCurrentView().Id;
                await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Frame frame = new Frame();
                    frame.Navigate(typeof(UWP.MyControls.AlertDialog), null);
                    Window.Current.Content = frame;
                // You have to activate the window in order to show it later.
                Window.Current.Activate();

                    newViewId = ApplicationView.GetForCurrentView().Id;
                //ApplicationView.GetForCurrentView().Title = title;
            });
                if (!await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId))
                {
                    GoogleDrive2.MyLogger.LogError("Failed to show window!");
                    return;
                }
                var semaphoreSlim = new Libraries.MySemaphore(0);
                var eventHandler = new Libraries.Events.MyEventHandler<string>(delegate
                {
                    semaphoreSlim.Release();
                });
                await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UWP.MyControls.AlertDialog.Instance.OKClicked += eventHandler;
                    UWP.MyControls.AlertDialog.Instance.TXBmain.Text = msg;
                });
                await semaphoreSlim.WaitAsync();
                await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UWP.MyControls.AlertDialog.Instance.OKClicked -= eventHandler;
                });
                await ApplicationViewSwitcher.SwitchAsync(originViewId, newViewId, ApplicationViewSwitchingOptions.ConsolidateViews);
            }
            finally { SemaphoreDialogBox.Release(); }
        }
    }
}