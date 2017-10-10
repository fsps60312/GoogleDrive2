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
        public static async Task<Tuple<string, string>> ActionSheet(string title, string msg, List<string> buttons)
        {
            await SemaphoreDialogBox.WaitAsync();
            try
            {
                CoreApplicationView newView = CoreApplication.CreateNewView();
                int newViewId = 0;
                var originViewId = ApplicationView.GetForCurrentView().Id;
                var semaphoreSlim = new Libraries.MySemaphore(0);
                bool windowClosed = false;
                await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Frame frame = new Frame();
                    frame.Navigate(typeof(UWP.MyControls.AlertDialog));
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
                    GoogleDrive2.MyLogger.LogError("Failed to show window!");
                    return new Tuple<string, string>(null, null);
                }
                Tuple<string, string> ans = new Tuple<string, string>(null, null);
                var eventHandler = new Libraries.Events.MyEventHandler<Tuple<string, string>>((response) =>
                 {
                     ans = response;
                     semaphoreSlim.Release();
                 });
                await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UWP.MyControls.AlertDialog.Instance.ButtonClicked += eventHandler;
                    UWP.MyControls.AlertDialog.Instance.TXBmain.Text = msg;
                    UWP.MyControls.AlertDialog.Instance.AddButtons(buttons);
                });
                await semaphoreSlim.WaitAsync();
                await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UWP.MyControls.AlertDialog.Instance.ButtonClicked -= eventHandler;
                });
                if (!windowClosed) await ApplicationViewSwitcher.SwitchAsync(originViewId, newViewId, ApplicationViewSwitchingOptions.ConsolidateViews);
                return ans;
            }
            finally { SemaphoreDialogBox.Release(); }
        }
        public static async Task<string> Input(string msg)
        {
            var response = await ActionSheet("", msg, new List<string> { "OK" });
            return response.Item1;
        }
        public static async Task<bool> Ask(string msg, string positiveText = "Yes", string negativeText = "No")
        {
            var response = await ActionSheet("", msg, new List<string> { positiveText, negativeText });
            if (response.Item2 == positiveText) return true;
            else
            {
                if (response.Item2 != negativeText) MyLogger.LogError($"Expected: {negativeText}, Get: {response}");
                return false;
            }
        }
        public static async Task Alert(string msg)
        {
            await ActionSheet("", msg, new List<string> { "OK" });
        }
    }
}