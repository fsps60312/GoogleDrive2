using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace GoogleDrive2.MyControls.CloudFileListPanel
{
    partial class CloudFileListPanelViewModel
    {
        public class CloudFolderSearchListViewModel : Api.Files.FullList
        {
            //List<Api.Files.FullList.FullProperties> cloudFiles = new List<FullProperties>();
            //Api.Files.List<CloudFile.TemporaryClass> listApi=new Api.Files.List<CloudFile.TemporaryClass>()
            public CloudFolderSearchListViewModel(string q, List<string> orderBy) : base(q, orderBy)
            {
                //this.CloudFileListCleared += delegate { cloudFiles.Clear(); };
                //this.CloudFilesAdded += (file) => { cloudFiles.AddRange(file); };
                //new Action(async() => { await this.StartAsync(true); })();
            }
        }
        class CloudFolderChain
        {
            List<CloudFolderSearchListViewModel> cloudFolderSearchList = new List<CloudFolderSearchListViewModel>();
        }
        List<CloudFolderChain> cloudFolderChain = new List<CloudFolderChain>();
    }
}
