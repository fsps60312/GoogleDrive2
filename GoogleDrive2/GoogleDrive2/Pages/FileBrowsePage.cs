using System;
using System.Collections.Generic;
using System.Text;
using GoogleDrive2.MyControls;

namespace GoogleDrive2.Pages
{
    class FileBrowsePage : MyContentPage
    {
        public FileBrowsePage()
        {
            this.Title = "File Browse";
            this.Content = new MyControls.CloudFileListPanel.CloudFileListPanel();
        }
    }
}
