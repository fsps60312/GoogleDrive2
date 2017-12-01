using System;
using System.Collections.Generic;
using System.Text;
using GoogleDrive2.MyControls;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleDrive2.Pages.TestPage
{
    class CancellationTokenTestPage : MyContentPage
    {
        MyButton BTNwait, BTNrelease, BTNcancel;
        MyStackPanel SPmain;
        CancellationTokenSource cancellationTokenSource;
        Libraries.MySemaphore semaphore = new Libraries.MySemaphore(0);
        void RegisterEvents()
        {
            BTNwait.Clicked += async delegate
              {
                  BTNwait.IsEnabled = false;
                  BTNwait.Text = "Running...";
                  cancellationTokenSource = new CancellationTokenSource();
                  BTNwait.Text = await semaphore.WaitAsync(cancellationTokenSource.Token) ? "Completed" : "Canceled";
                  BTNwait.IsEnabled = true;
              };
            BTNrelease.Clicked += delegate
              {
                  BTNrelease.IsEnabled = false;
                  semaphore.Release();
                  BTNrelease.IsEnabled = true;
              };
            BTNcancel.Clicked += delegate
              {
                  BTNcancel.IsEnabled = false;
                  cancellationTokenSource.Cancel();
                  BTNcancel.IsEnabled = true;
              };
        }
        void InitializeViews()
        {
            this.Title = "CancellationToken Test";
            {
                SPmain = new MyStackPanel(Xamarin.Forms.ScrollOrientation.Vertical);
                {
                    BTNwait = new MyButton { Text = "Wait" };
                    SPmain.Children.Add(BTNwait);
                }
                {
                    BTNrelease = new MyButton { Text = "Release" };
                    SPmain.Children.Add(BTNrelease);
                }
                {
                    BTNcancel = new MyButton { Text = "Cancel" };
                    SPmain.Children.Add(BTNcancel);
                }
                this.Content = SPmain;
            }
        }
        public CancellationTokenTestPage()
        {
            InitializeViews();
            RegisterEvents();
        }
    }
}
