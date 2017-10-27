using System;
using System.Collections.Generic;
using System.Text;
using GoogleDrive2.MyControls;

namespace GoogleDrive2.Pages.NetworkStatusPage
{
    class FileUploadBarViewModel:MyControls.BarsListPanel.MyDisposable
    {
    }
    class FileUploadPage:MyContentPage
    {
        public FileUploadPage()
        {
            this.Title = "File Upload";
        }
    }
}
