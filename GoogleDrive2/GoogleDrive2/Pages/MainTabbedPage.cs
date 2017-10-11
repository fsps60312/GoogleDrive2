using System;
using System.Collections.Generic;
using System.Text;
using GoogleDrive2.MyControls;

namespace GoogleDrive2.Pages
{
    class MainTabbedPage:MyTabbedPage
    {
        public MainTabbedPage()
        {
            //this.Children.Add(new MyContentPage { Title = "A" });
            //this.Children.Add(new MyContentPage { Title = "B" });
            this.Children.Add(new FileBrowsePage());
            this.Children.Add(new NetworkStatusPage.NetworkStatusPage());
            this.Children.Add(new TestPage.TestTabbedPage());
        }
    }
}
