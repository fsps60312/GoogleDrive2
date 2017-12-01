using System;
using System.Collections.Generic;
using System.Text;
using GoogleDrive2.MyControls;

namespace GoogleDrive2.Pages.TestPage
{
    class FilesListRequestPage : MyControls.ApiPage.PPage
    {
        public FilesListRequestPage()
        {
            this.Title = "File List";
            DoInitializeTasks();
        }
        async void DoInitializeTasks()
        {
            await this.Update<Api.Files.ListParameters, Api.Files.ListRequest>();
        }
    }
}
