using System;
using System.Collections.Generic;
using System.Text;
using GoogleDrive2.MyControls;

namespace GoogleDrive2.Pages.NetworkStatusPage
{
    class NetworkStatusPage:MyTabbedPage
    {
        public NetworkStatusPage()
        {
            this.Title = "Network Status";
            this.Children.Add(new RequestWithParametersPage());
            this.Children.Add(new RequestWithHeadersPage());
            this.Children.Add(new RequestWithBodyPage());
        }
    }
}
