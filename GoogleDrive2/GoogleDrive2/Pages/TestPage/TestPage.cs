using System;
using System.Collections.Generic;
using System.Text;
using GoogleDrive2.MyControls;
using Xamarin.Forms;

namespace GoogleDrive2.Pages.TestPage
{
    class TestPage:MyNavigationPage
    {
        public TestPage():base(new TestTabbedPage())
        {
            var a = new MyNavigationPage(new TestTabbedPage());
        }
    }
    class TestTabbedPage:MyTabbedPage
    {
        public TestTabbedPage()
        {
            this.Title = "Test Page";
            this.Children.Add(new ColorTestPage());
            this.Children.Add(new SearchFileListPage());
            this.Children.Add(new TouchEventImplementPage());
            this.Children.Add(new ButtonsPage());
            this.Children.Add(new TouchEventPage());
            this.Children.Add(new FilesListRequestPage());
            this.Children.Add(new HttpRequestPage());
            this.Children.Add(new GetAuthorizationCodePage());
            this.Children.Add(new SemaphoreSlimPage());
        }
    }
}
