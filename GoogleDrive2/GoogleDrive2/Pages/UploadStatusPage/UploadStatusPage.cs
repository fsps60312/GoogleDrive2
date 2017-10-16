using System;
using System.Collections.Generic;
using System.Text;
using GoogleDrive2.MyControls;

namespace GoogleDrive2.Pages.UploadStatusPage
{
    class UploadStatusPage:MyTabbedPage
    {
        public UploadStatusPage()
        {
            this.Title = "Upload Status";
            this.Children.Add(new OperationalPage());
        }
    }
}
