using System;
using System.Collections.Generic;
using System.Text;
using GoogleDrive2.MyControls;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleDrive2.Pages.TestPage
{
    class SemaphoreSlimPage:MyContentPage
    {
        class SemaphoreSlimContentView:MyStackLayout
        {
            MyLabel LBcount;
            MyButton BTNreleaseOriginal, BTNwaitOriginal, BTNreleaseCopy, BTNwaitCopy;
            private void InitializeViews()
            {
                this.Children.Add(LBcount = new MyLabel { Text = "(Count)" });
                this.Children.Add(BTNreleaseOriginal = new MyButton { Text = "Release Original" });
                this.Children.Add(BTNwaitOriginal = new MyButton { Text = "Wait Original" });
                this.Children.Add(BTNreleaseCopy = new MyButton { Text = "Release Copy" });
                this.Children.Add(BTNwaitCopy = new MyButton { Text = "Wait Copy" });
            }
            class FakeClass
            {
                SemaphoreSlim semaphoreSlim;
                public FakeClass(SemaphoreSlim _semaphoreSlim)
                {
                    semaphoreSlim = _semaphoreSlim;
                }
                public async Task WaitAsync()
                {
                    await semaphoreSlim.WaitAsync();
                }
                public void Release()
                {
                    semaphoreSlim.Release();
                }
            }
            private void RegisterEvents()
            {
                var semaphoreSlim = new SemaphoreSlim(0);
                var showCount = new Action(delegate
                  {
                      LBcount.Text = $"semaphoreSlim.CurrentCount: {semaphoreSlim.CurrentCount}";
                  });
                BTNreleaseOriginal.Clicked += delegate
                  {
                      BTNreleaseOriginal.IsEnabled = false;
                      semaphoreSlim.Release();
                      showCount();
                      BTNreleaseOriginal.IsEnabled = true;
                  };
                BTNreleaseCopy.Clicked += delegate
                  {
                      BTNreleaseCopy.IsEnabled = false;
                      var c = new FakeClass(semaphoreSlim);
                      c.Release();
                      showCount();
                      BTNreleaseCopy.IsEnabled = true;
                  };
                BTNwaitOriginal.Clicked += async delegate
                  {
                      BTNwaitOriginal.IsEnabled = false;
                      await semaphoreSlim.WaitAsync();
                      showCount();
                      BTNwaitOriginal.IsEnabled = true;
                  };
                BTNwaitCopy.Clicked += async delegate
                  {
                      BTNwaitCopy.IsEnabled = false;
                      var c = new FakeClass(semaphoreSlim);
                      await c.WaitAsync();
                      showCount();
                      BTNwaitCopy.IsEnabled = true;
                  };
            }
            public SemaphoreSlimContentView()
            {
                InitializeViews();
                RegisterEvents();
            }
        }
        public SemaphoreSlimPage()
        {
            this.Title = "Semaphore Slim";
            this.Content = new SemaphoreSlimContentView();
        }
    }
}
